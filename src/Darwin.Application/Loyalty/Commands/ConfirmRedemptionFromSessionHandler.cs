using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Confirms a redemption operation for a previously prepared scan session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler is invoked by the business app after scanning the consumer's QR
    /// and reviewing the pre-selected rewards. It re-validates the loyalty account
    /// points balance to guard against race conditions and then creates a
    /// <see cref="LoyaltyRewardRedemption"/> and a corresponding
    /// <see cref="LoyaltyPointsTransaction"/> entry.
    /// </para>
    /// <para>
    /// The session is one-time use. After a successful redemption the session
    /// status becomes <see cref="LoyaltyScanStatus.Completed"/> and any subsequent
    /// attempt to use it is rejected.
    /// </para>
    /// <para>
    /// The <see cref="ScanSession.SelectedRewardsJson"/> payload stores the full
    /// list of selected rewards as an array of <see cref="SelectedRewardItemDto"/>.
    /// The handler aggregates the total points required. Because the domain model
    /// stores only a single <see cref="LoyaltyRewardRedemption.LoyaltyRewardTierId"/>,
    /// the first selected tier is used as the primary identifier and the full list
    /// is preserved in <see cref="LoyaltyRewardRedemption.MetadataJson"/>.
    /// </para>
    /// </remarks>
    public sealed class ConfirmRedemptionFromSessionHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUserService;
        private readonly IClock _clock;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmRedemptionFromSessionHandler"/> class.
        /// </summary>
        /// <param name="db">Application database context abstraction.</param>
        /// <param name="currentUserService">Provides the user id of the business staff member.</param>
        /// <param name="clock">Time abstraction used to obtain UTC timestamps.</param>
        public ConfirmRedemptionFromSessionHandler(
            IAppDbContext db,
            ICurrentUserService currentUserService,
            IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// Confirms a redemption based on the specified scan session.
        /// </summary>
        /// <param name="dto">
        /// Data describing the scan session to confirm.
        /// </param>
        /// <param name="businessId">
        /// Identifier of the business from the authenticated context of the
        /// business app. The handler verifies that the scan session belongs
        /// to this business.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="Result{T}"/> containing <see cref="ConfirmRedemptionResultDto"/>
        /// on success or an error message on failure.
        /// </returns>
        public async Task<Result<ConfirmRedemptionResultDto>> HandleAsync(
            ConfirmRedemptionFromSessionDto dto,
            Guid businessId,
            CancellationToken ct = default)
        {
            if (dto.ScanSessionId == Guid.Empty)
            {
                return Result<ConfirmRedemptionResultDto>.Fail("ScanSessionId is required.");
            }

            if (businessId == Guid.Empty)
            {
                return Result<ConfirmRedemptionResultDto>.Fail("BusinessId is required.");
            }

            var session = await _db.Set<ScanSession>()
                .AsQueryable()
                .SingleOrDefaultAsync(
                    s => s.Id == dto.ScanSessionId && !s.IsDeleted,
                    ct)
                .ConfigureAwait(false);

            if (session is null)
            {
                return Result<ConfirmRedemptionResultDto>.Fail("Scan session not found.");
            }

            if (session.BusinessId != businessId)
            {
                return Result<ConfirmRedemptionResultDto>.Fail("Scan session does not belong to this business.");
            }

            if (session.Mode != LoyaltyScanMode.Redemption)
            {
                return Result<ConfirmRedemptionResultDto>.Fail("Scan session is not in redemption mode.");
            }

            var now = _clock.UtcNow;
            if (session.ExpiresAtUtc <= now)
            {
                session.Status = LoyaltyScanStatus.Expired;
                session.Outcome = "Expired";
                session.FailureReason = "Session expired before redemption confirmation.";

                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return Result<ConfirmRedemptionResultDto>.Fail("Scan session has expired.");
            }

            if (session.Status != LoyaltyScanStatus.Pending)
            {
                return Result<ConfirmRedemptionResultDto>.Fail("Scan session is not in a pending state.");
            }

            var account = await _db.Set<LoyaltyAccount>()
                .AsQueryable()
                .SingleOrDefaultAsync(
                    a => a.Id == session.LoyaltyAccountId && !a.IsDeleted,
                    ct)
                .ConfigureAwait(false);

            if (account is null)
            {
                return Result<ConfirmRedemptionResultDto>.Fail("Loyalty account for scan session not found.");
            }

            if (account.Status != LoyaltyAccountStatus.Active)
            {
                return Result<ConfirmRedemptionResultDto>.Fail("Loyalty account is not active.");
            }

            var selections = ParseSelectedRewards(session.SelectedRewardsJson);
            if (selections.Count == 0)
            {
                return Result<ConfirmRedemptionResultDto>.Fail("Scan session does not contain any selected rewards.");
            }

            var totalRequiredPoints = 0;
            foreach (var item in selections)
            {
                if (item.Quantity <= 0 || item.RequiredPointsPerUnit <= 0)
                {
                    continue;
                }

                checked
                {
                    totalRequiredPoints += item.Quantity * item.RequiredPointsPerUnit;
                }
            }

            if (totalRequiredPoints <= 0)
            {
                return Result<ConfirmRedemptionResultDto>.Fail("Selected rewards do not require any points.");
            }

            // Re-check points balance at confirmation time to guard against race conditions.
            if (account.PointsBalance < totalRequiredPoints)
            {
                session.Status = LoyaltyScanStatus.Cancelled;
                session.Outcome = "InsufficientPoints";
                session.FailureReason = "Account does not have enough points at confirmation time.";

                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return Result<ConfirmRedemptionResultDto>.Fail("Insufficient points for selected rewards.");
            }

            var staffUserId = _currentUserService.GetCurrentUserId();

            // Use the first selected tier as the primary identifier, while keeping the
            // full selection payload in MetadataJson.
            var primaryTierId = selections[0].LoyaltyRewardTierId;

            var redemption = new LoyaltyRewardRedemption
            {
                LoyaltyAccountId = account.Id,
                BusinessId = session.BusinessId,
                LoyaltyRewardTierId = primaryTierId,
                PointsSpent = totalRequiredPoints,
                Status = LoyaltyRedemptionStatus.Confirmed,
                BusinessLocationId = session.BusinessLocationId,
                MetadataJson = session.SelectedRewardsJson
            };

            _db.Set<LoyaltyRewardRedemption>().Add(redemption);

            var transaction = new LoyaltyPointsTransaction
            {
                LoyaltyAccountId = account.Id,
                BusinessId = session.BusinessId,
                Type = LoyaltyPointsTransactionType.Redemption,
                PointsDelta = -totalRequiredPoints,
                RewardRedemptionId = redemption.Id,
                BusinessLocationId = session.BusinessLocationId,
                PerformedByUserId = staffUserId,
                Reference = null,
                Notes = "Redemption confirmed from scan session."
            };

            _db.Set<LoyaltyPointsTransaction>().Add(transaction);

            // Update the account balance. LifetimePoints is not changed because it
            // tracks total points ever earned, not the current spendable balance.
            account.PointsBalance -= totalRequiredPoints;

            // Mark the session as completed and link the resulting transaction.
            session.Status = LoyaltyScanStatus.Completed;
            session.Outcome = "Redeemed";
            session.FailureReason = null;
            session.ResultingTransactionId = transaction.Id;

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            var result = new ConfirmRedemptionResultDto
            {
                LoyaltyAccountId = account.Id,
                PointsSpent = totalRequiredPoints,
                NewPointsBalance = account.PointsBalance,
                RewardRedemptionId = redemption.Id,
                PointsTransactionId = transaction.Id
            };

            return Result<ConfirmRedemptionResultDto>.Ok(result);
        }

        /// <summary>
        /// Parses the JSON payload of selected rewards (if any) into DTOs.
        /// </summary>
        private static List<SelectedRewardItemDto> ParseSelectedRewards(string? selectedRewardsJson)
        {
            if (string.IsNullOrWhiteSpace(selectedRewardsJson))
            {
                return new List<SelectedRewardItemDto>();
            }

            try
            {
                var items = JsonSerializer.Deserialize<List<SelectedRewardItemDto>>(selectedRewardsJson);
                return items ?? new List<SelectedRewardItemDto>();
            }
            catch
            {
                // If we cannot parse the stored payload, treat it as no selection.
                return new List<SelectedRewardItemDto>();
            }
        }
    }
}
