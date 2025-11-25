using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
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
    /// Processes a QR scan on a business device.
    /// 
    /// High-level behavior:
    ///  - Validates the incoming scan request for structural correctness.
    ///  - Locates and validates the QR token (<see cref="QrCodeToken"/>).
    ///  - Marks the token as consumed (one-time use, regardless of business outcome).
    ///  - Creates a <see cref="ScanSession"/> to record what happened on the business device.
    ///  - Resolves or creates the <see cref="LoyaltyAccount"/> for the user and business.
    ///  - Depending on <see cref="QrTokenPurpose"/>:
    ///    * IdentityOnly:
    ///        - Only returns loyalty identity info; no balance change, no ledger entry.
    ///    * Accrual:
    ///        - Creates a <see cref="LoyaltyPointsTransaction"/> of type Accrual.
    ///        - Increments account.PointsBalance and account.LifetimePoints.
    ///    * Redemption:
    ///        - If the reward tier has AllowSelfRedemption == true:
    ///            - Creates a <see cref="LoyaltyRewardRedemption"/> with Status = Confirmed.
    ///            - Deducts points and creates a <see cref="LoyaltyPointsTransaction"/> of type Redemption.
    ///        - If AllowSelfRedemption == false:
    ///            - Creates a <see cref="LoyaltyRewardRedemption"/> with Status = Pending.
    ///            - Does NOT deduct points and does NOT create a ledger entry yet.
    ///            - The separate <see cref="ConfirmLoyaltyRewardRedemptionHandler"/> is responsible
    ///              for finalizing the redemption later (two-step confirmation flow).
    ///
    /// Human usage guidance:
    ///  - This handler is invoked from the "business app" when a QR code is scanned.
    ///  - It returns a <see cref="ProcessQrScanResultDto"/> describing whether the scan was accepted,
    ///    and if so, what happened with the loyalty account.
    ///
    /// AI usage guidance:
    ///  - Use this handler as the primary entry point for processing a QR scan on the business side.
    ///  - For two-step redemption flows:
    ///      * Let this handler create a pending redemption (AllowSelfRedemption == false).
    ///      * Later, call <see cref="ConfirmLoyaltyRewardRedemptionHandler"/> to finalize.
    ///  - Do not attempt to re-use or re-issue the same token; the handler marks it as consumed
    ///    on first use and will treat replays as invalid.
    /// </summary>
    public sealed class ProcessQrScanHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly ProcessQrScanValidator _validator = new();

        /// <summary>
        /// Initializes the handler.
        /// </summary>
        /// <param name="db">Application database abstraction used to access loyalty entities.</param>
        /// <param name="clock">Clock abstraction used to obtain current UTC time.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="db"/> or <paramref name="clock"/> is null.
        /// </exception>
        public ProcessQrScanHandler(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// Processes a scan and returns the outcome for the business app.
        /// </summary>
        /// <param name="dto">
        /// Scan payload including:
        ///  - The QR token string (dto.Token).
        ///  - The business and (optional) location context.
        ///  - The expected token purpose (Accrual, Redemption, IdentityOnly).
        ///  - Optional reward tier id for redemptions.
        ///  - Optional business user performing the scan.
        /// </param>
        /// <param name="ct">Cancellation token used to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Result{T}"/> whose value is a <see cref="ProcessQrScanResultDto"/>.
        /// The result reports whether the scan was accepted and, when applicable,
        /// the resulting loyalty account id and new points balance.
        /// </returns>
        public async Task<Result<ProcessQrScanResultDto>> HandleAsync(
            ProcessQrScanDto dto,
            CancellationToken ct = default)
        {
            // Step 1: basic DTO validation.
            var vr = _validator.Validate(dto);
            if (!vr.IsValid)
            {
                return Result<ProcessQrScanResultDto>.Fail("Invalid scan request.");
            }

            var now = _clock.UtcNow;

            // Step 2: lookup token by its opaque value.
            var tokenEntity = await _db.Set<QrCodeToken>()
                .FirstOrDefaultAsync(x => x.Token == dto.Token, ct)
                .ConfigureAwait(false);

            if (tokenEntity is null)
            {
                // Token does not exist at all.
                return Result<ProcessQrScanResultDto>.Ok(new ProcessQrScanResultDto
                {
                    Accepted = false,
                    Outcome = "Rejected",
                    FailureReason = "Token not found."
                });
            }

            if (tokenEntity.ConsumedAtUtc is not null)
            {
                // Token has already been used (replay).
                return Result<ProcessQrScanResultDto>.Ok(new ProcessQrScanResultDto
                {
                    Accepted = false,
                    Outcome = "Replayed",
                    FailureReason = "Token already consumed."
                });
            }

            if (tokenEntity.ExpiresAtUtc <= now)
            {
                // Token expired (too old).
                return Result<ProcessQrScanResultDto>.Ok(new ProcessQrScanResultDto
                {
                    Accepted = false,
                    Outcome = "Expired",
                    FailureReason = "Token expired."
                });
            }

            if (tokenEntity.Purpose != dto.ExpectedPurpose)
            {
                // Business app expects a different purpose than the token was issued for.
                return Result<ProcessQrScanResultDto>.Ok(new ProcessQrScanResultDto
                {
                    Accepted = false,
                    Outcome = "Rejected",
                    FailureReason = "Token purpose mismatch."
                });
            }

            // From this point on we treat the token as consumed (one-time use),
            // even if later business rules cause us to reject the scan.
            tokenEntity.ConsumedAtUtc = now;
            tokenEntity.ConsumedByBusinessId = dto.BusinessId;
            tokenEntity.ConsumedByBusinessLocationId = dto.BusinessLocationId;

            // Step 3: create a scan session entry to log what happens on the business device.
            var scanSession = new ScanSession
            {
                QrCodeTokenId = tokenEntity.Id,
                BusinessId = dto.BusinessId,
                BusinessLocationId = dto.BusinessLocationId,
                Outcome = "Accepted"
            };

            _db.Set<ScanSession>().Add(scanSession);

            // Step 4: resolve or create the loyalty account.
            LoyaltyAccount account;

            if (tokenEntity.LoyaltyAccountId.HasValue)
            {
                // Token is bound to a specific account.
                account = await _db.Set<LoyaltyAccount>()
                    .FirstOrDefaultAsync(a => a.Id == tokenEntity.LoyaltyAccountId.Value, ct)
                    .ConfigureAwait(false)
                    ?? throw new ValidationException("Loyalty account not found.");
            }
            else
            {
                // Token carries only the user id; resolve account for this business+user,
                // or create a new active account if none exists yet.
                account = await _db.Set<LoyaltyAccount>()
                    .FirstOrDefaultAsync(
                        a => a.BusinessId == dto.BusinessId && a.UserId == tokenEntity.UserId,
                        ct)
                    .ConfigureAwait(false)
                    ?? new LoyaltyAccount
                    {
                        BusinessId = dto.BusinessId,
                        UserId = tokenEntity.UserId,
                        Status = LoyaltyAccountStatus.Active
                    };

                // If the account is newly constructed (no Id assigned yet),
                // attach it to the DbContext so it will be saved.
                if (account.Id == Guid.Empty)
                {
                    _db.Set<LoyaltyAccount>().Add(account);
                }
            }

            if (account.Status != LoyaltyAccountStatus.Active)
            {
                // Account is currently deactivated/suspended and cannot be used.
                scanSession.Outcome = "Rejected";
                scanSession.FailureReason = "Account is not active.";

                await _db.SaveChangesAsync(ct).ConfigureAwait(false);

                return Result<ProcessQrScanResultDto>.Ok(new ProcessQrScanResultDto
                {
                    Accepted = false,
                    Outcome = scanSession.Outcome,
                    FailureReason = scanSession.FailureReason,
                    LoyaltyAccountId = account.Id
                });
            }

            ProcessQrScanResultDto result;

            // Step 5: handle the specific token purpose.

            if (dto.ExpectedPurpose == QrTokenPurpose.Accrual)
            {
                // Simple per-visit accrual: +1 point per successful scan.
                var tx = new LoyaltyPointsTransaction
                {
                    LoyaltyAccountId = account.Id,
                    BusinessId = dto.BusinessId,
                    Type = LoyaltyPointsTransactionType.Accrual,
                    PointsDelta = 1,
                    BusinessLocationId = dto.BusinessLocationId,
                    PerformedByUserId = dto.PerformedByUserId
                };

                account.PointsBalance += 1;
                account.LifetimePoints += 1;

                _db.Set<LoyaltyPointsTransaction>().Add(tx);

                scanSession.ResultingTransactionId = tx.Id;

                result = new ProcessQrScanResultDto
                {
                    Accepted = true,
                    Outcome = "Accepted",
                    LoyaltyAccountId = account.Id,
                    NewPointsBalance = account.PointsBalance
                };
            }
            else if (dto.ExpectedPurpose == QrTokenPurpose.Redemption)
            {
                // Redemption flow, which may be single-step (self-redemption)
                // or two-step (pending + later confirmation), depending on the tier.

                if (!dto.LoyaltyRewardTierId.HasValue)
                {
                    throw new ValidationException("Reward tier id is required for redemption.");
                }

                var tier = await _db.Set<LoyaltyRewardTier>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        t => t.Id == dto.LoyaltyRewardTierId.Value && !t.IsDeleted,
                        ct)
                    .ConfigureAwait(false);

                if (tier is null)
                {
                    throw new ValidationException("Reward tier not found.");
                }

                if (account.PointsBalance < tier.PointsRequired)
                {
                    // Not enough points to proceed with the requested reward.
                    scanSession.Outcome = "Rejected";
                    scanSession.FailureReason = "Insufficient points.";

                    await _db.SaveChangesAsync(ct).ConfigureAwait(false);

                    return Result<ProcessQrScanResultDto>.Ok(new ProcessQrScanResultDto
                    {
                        Accepted = false,
                        Outcome = scanSession.Outcome,
                        FailureReason = scanSession.FailureReason,
                        LoyaltyAccountId = account.Id,
                        NewPointsBalance = account.PointsBalance
                    });
                }

                // Two possible paths:
                //  - AllowSelfRedemption == true  -> immediate confirmation (single-step).
                //  - AllowSelfRedemption == false -> pending redemption (two-step).
                if (tier.AllowSelfRedemption)
                {
                    // Single-step: confirm and consume points immediately.
                    var redemption = new LoyaltyRewardRedemption
                    {
                        LoyaltyAccountId = account.Id,
                        BusinessId = dto.BusinessId,
                        LoyaltyRewardTierId = tier.Id,
                        PointsSpent = tier.PointsRequired,
                        Status = LoyaltyRedemptionStatus.Confirmed,
                        BusinessLocationId = dto.BusinessLocationId
                    };

                    var tx = new LoyaltyPointsTransaction
                    {
                        LoyaltyAccountId = account.Id,
                        BusinessId = dto.BusinessId,
                        Type = LoyaltyPointsTransactionType.Redemption,
                        PointsDelta = -tier.PointsRequired,
                        RewardRedemptionId = redemption.Id,
                        BusinessLocationId = dto.BusinessLocationId,
                        PerformedByUserId = dto.PerformedByUserId
                    };

                    account.PointsBalance -= tier.PointsRequired;

                    _db.Set<LoyaltyRewardRedemption>().Add(redemption);
                    _db.Set<LoyaltyPointsTransaction>().Add(tx);

                    scanSession.ResultingTransactionId = tx.Id;

                    result = new ProcessQrScanResultDto
                    {
                        Accepted = true,
                        Outcome = "Accepted",
                        LoyaltyAccountId = account.Id,
                        NewPointsBalance = account.PointsBalance
                    };
                }
                else
                {
                    // Two-step flow: only create a pending redemption.
                    // No points are deducted yet; no ledger entry is created.
                    // The ConfirmLoyaltyRewardRedemptionHandler will later:
                    //  - verify that the account still has enough points,
                    //  - set Status = Confirmed,
                    //  - create a LoyaltyPointsTransaction,
                    //  - and deduct points at that time.
                    var redemption = new LoyaltyRewardRedemption
                    {
                        LoyaltyAccountId = account.Id,
                        BusinessId = dto.BusinessId,
                        LoyaltyRewardTierId = tier.Id,
                        PointsSpent = tier.PointsRequired,
                        Status = LoyaltyRedemptionStatus.Pending,
                        BusinessLocationId = dto.BusinessLocationId
                    };

                    _db.Set<LoyaltyRewardRedemption>().Add(redemption);

                    // For now there is no resulting transaction, because no points were consumed yet.
                    scanSession.ResultingTransactionId = null;

                    result = new ProcessQrScanResultDto
                    {
                        Accepted = true,
                        // Distinct outcome string so clients (including AI) can recognize
                        // that a second step (confirmation) is required.
                        Outcome = "PendingConfirmation",
                        LoyaltyAccountId = account.Id,
                        NewPointsBalance = account.PointsBalance
                    };
                }
            }
            else
            {
                // IdentityOnly or any other non-mutating purpose:
                // token is consumed and a scan session is recorded, but the account
                // balance is not changed and no ledger entry is written.
                result = new ProcessQrScanResultDto
                {
                    Accepted = true,
                    Outcome = "Accepted",
                    LoyaltyAccountId = account.Id,
                    NewPointsBalance = account.PointsBalance
                };
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            return Result<ProcessQrScanResultDto>.Ok(result);
        }
    }
}
