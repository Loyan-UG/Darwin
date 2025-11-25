using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Validators;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Command handler that implements the "two-step" confirmation flow for loyalty reward redemptions.
    ///
    /// Background:
    ///  - For simple, low-value rewards, the system may immediately confirm a redemption at scan time
    ///    (e.g., in ProcessQrScanHandler) and directly deduct points. This is suitable when
    ///    LoyaltyRewardTier.AllowSelfRedemption == true.
    ///  - For higher-value or sensitive rewards, the system can instead create a
    ///    LoyaltyRewardRedemption with Status = Pending at scan time when
    ///    AllowSelfRedemption == false. In that case, no points are deducted yet.
    ///
    /// This handler is responsible for Step 2 of that flow:
    ///  - It is invoked from a business/staff UI after the reward has actually been fulfilled.
    ///  - It confirms a pending redemption, deducts points from the loyalty account,
    ///    and records a LoyaltyPointsTransaction of type Redemption.
    ///
    /// Human usage guidance:
    ///  - Call this when a staff member clicks something like "Confirm reward" in the business app
    ///    for an entry that is currently in Pending state.
    ///
    /// AI usage guidance:
    ///  - Use this handler to finalize redemptions that were previously created (e.g., by a QR scan
    ///    use-case) with Status = Pending.
    ///  - Do NOT use it to start new redemptions; that is handled by scan-time or other
    ///    reward-request flows.
    ///  - Always pass the BusinessId from the business context to prevent cross-tenant access.
    ///  - If you have a row version from a prior read, pass it in dto.RowVersion to enable
    ///    optimistic concurrency checks.
    /// </summary>
    public sealed class ConfirmLoyaltyRewardRedemptionHandler
    {
        private readonly IAppDbContext _db;
        private readonly ConfirmLoyaltyRewardRedemptionValidator _validator = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmLoyaltyRewardRedemptionHandler"/> class.
        /// </summary>
        /// <param name="db">Application database abstraction used to access loyalty entities.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="db"/> is null.</exception>
        public ConfirmLoyaltyRewardRedemptionHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Confirms a pending loyalty reward redemption and applies the points deduction.
        ///
        /// The method performs the following steps:
        ///  1. Validates the incoming DTO using <see cref="ConfirmLoyaltyRewardRedemptionValidator"/>.
        ///  2. Loads the <see cref="LoyaltyRewardRedemption"/> by id and checks:
        ///     - That it exists.
        ///     - That it belongs to the specified business.
        ///     - That its current Status is Pending.
        ///  3. Optionally performs an optimistic concurrency check if a row version is supplied.
        ///  4. Loads the associated <see cref="LoyaltyAccount"/> and ensures:
        ///     - The account exists and is Active.
        ///     - The account has enough points to cover the redemption.
        ///  5. Updates:
        ///     - account.PointsBalance is decremented by PointsSpent.
        ///     - redemption.Status is set to Confirmed.
        ///     - redemption.BusinessLocationId may be overridden by the DTO.
        ///  6. Creates a <see cref="LoyaltyPointsTransaction"/> of type Redemption to keep the ledger auditable.
        ///  7. Persists all changes with a single SaveChangesAsync call.
        ///
        /// If any of the checks fail, the method returns a failed <see cref="Result{T}"/>
        /// without modifying the database.
        /// </summary>
        /// <param name="dto">Confirmation request containing the redemption id and contextual data.</param>
        /// <param name="ct">Cancellation token used to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Result{T}"/> containing a <see cref="ConfirmLoyaltyRewardRedemptionResultDto"/> on success,
        /// or an error description when the operation cannot be completed.
        /// </returns>
        public async Task<Result<ConfirmLoyaltyRewardRedemptionResultDto>> HandleAsync(
            ConfirmLoyaltyRewardRedemptionDto dto,
            CancellationToken ct = default)
        {
            // Step 1: basic DTO validation.
            var validationResult = _validator.Validate(dto);
            if (!validationResult.IsValid)
            {
                // ValidationException is thrown to integrate with the existing global validation pipeline.
                throw new ValidationException(validationResult.Errors);
            }

            // Step 2: load redemption and ensure it exists and belongs to the given business.
            var redemption = await _db.Set<LoyaltyRewardRedemption>()
                .FirstOrDefaultAsync(r => r.Id == dto.RedemptionId, ct)
                .ConfigureAwait(false);

            if (redemption is null)
            {
                return Result<ConfirmLoyaltyRewardRedemptionResultDto>.Fail("Redemption not found.");
            }

            if (redemption.BusinessId != dto.BusinessId)
            {
                // Protects against cross-business access or mismatched UI context.
                return Result<ConfirmLoyaltyRewardRedemptionResultDto>.Fail("Business mismatch for redemption.");
            }

            // Step 3: optional optimistic concurrency check on the redemption row.
            if (dto.RowVersion is not null &&
                !redemption.RowVersion.SequenceEqual(dto.RowVersion))
            {
                return Result<ConfirmLoyaltyRewardRedemptionResultDto>.Fail(
                    "Concurrency conflict. The redemption was modified by another process.");
            }

            if (redemption.Status == LoyaltyRedemptionStatus.Cancelled)
            {
                return Result<ConfirmLoyaltyRewardRedemptionResultDto>.Fail(
                    "The redemption has been cancelled and cannot be confirmed.");
            }

            if (redemption.Status == LoyaltyRedemptionStatus.Confirmed)
            {
                // We choose to treat re-confirm attempts as an error rather than silently succeeding.
                // This makes it easier to spot unintended double-invocations in UIs and tests.
                return Result<ConfirmLoyaltyRewardRedemptionResultDto>.Fail(
                    "The redemption is already confirmed.");
            }

            if (redemption.Status != LoyaltyRedemptionStatus.Pending)
            {
                // Future-proofing: in case new statuses are introduced, this guards the invariants.
                return Result<ConfirmLoyaltyRewardRedemptionResultDto>.Fail(
                    "The redemption is not in a pending state.");
            }

            // Step 4: load the associated loyalty account and verify status and balance.
            var account = await _db.Set<LoyaltyAccount>()
                .FirstOrDefaultAsync(a => a.Id == redemption.LoyaltyAccountId, ct)
                .ConfigureAwait(false);

            if (account is null)
            {
                return Result<ConfirmLoyaltyRewardRedemptionResultDto>.Fail(
                    "Loyalty account associated with the redemption was not found.");
            }

            if (account.Status != LoyaltyAccountStatus.Active)
            {
                return Result<ConfirmLoyaltyRewardRedemptionResultDto>.Fail(
                    "The loyalty account is not active and cannot be used for redemption.");
            }

            var pointsToSpend = redemption.PointsSpent;
            if (pointsToSpend <= 0)
            {
                // Defensive check: a redemption with zero or negative points is inconsistent.
                return Result<ConfirmLoyaltyRewardRedemptionResultDto>.Fail(
                    "The redemption has an invalid points amount.");
            }

            if (account.PointsBalance < pointsToSpend)
            {
                // At scan time the balance might have been sufficient, but in a delayed confirmation
                // scenario it is possible that the account no longer has enough points.
                return Result<ConfirmLoyaltyRewardRedemptionResultDto>.Fail(
                    "Insufficient points to confirm the redemption.");
            }

            // Step 5: apply the points deduction and update redemption status.
            account.PointsBalance -= pointsToSpend;

            // LifetimePoints tracks "ever earned" points in the current model and is
            // not decreased on redemption; we keep that behavior here.
            // account.LifetimePoints remains unchanged.

            // Allow the confirmation request to override the location for attribution purposes.
            if (dto.BusinessLocationId.HasValue)
            {
                redemption.BusinessLocationId = dto.BusinessLocationId.Value;
            }

            redemption.Status = LoyaltyRedemptionStatus.Confirmed;

            // Step 6: create a ledger entry of type Redemption.
            var transaction = new LoyaltyPointsTransaction
            {
                LoyaltyAccountId = account.Id,
                BusinessId = redemption.BusinessId,
                Type = LoyaltyPointsTransactionType.Redemption,
                PointsDelta = -pointsToSpend,
                RewardRedemptionId = redemption.Id,
                BusinessLocationId = redemption.BusinessLocationId,
                PerformedByUserId = dto.PerformedByUserId
            };

            _db.Set<LoyaltyPointsTransaction>().Add(transaction);

            // Note:
            //  - ScanSession.ResultingTransactionId is not updated here, because the current domain
            //    model does not link a redemption back to a specific ScanSession. If such a link
            //    is added in the future, this handler can be extended to fill that relationship.

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            // Step 7: build and return the result DTO.
            var resultDto = new ConfirmLoyaltyRewardRedemptionResultDto
            {
                RedemptionId = redemption.Id,
                LoyaltyAccountId = account.Id,
                BusinessId = redemption.BusinessId,
                BusinessLocationId = redemption.BusinessLocationId,
                TransactionId = transaction.Id,
                NewPointsBalance = account.PointsBalance,
                NewLifetimePoints = account.LifetimePoints
            };

            return Result<ConfirmLoyaltyRewardRedemptionResultDto>.Ok(resultDto);
        }
    }
}
