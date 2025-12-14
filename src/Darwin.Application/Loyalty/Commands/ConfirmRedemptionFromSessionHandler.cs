using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Services;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Commands
{
    /// <summary>
    /// Confirms a redemption operation for a previously prepared scan session identified by an opaque token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The business app scans a QR code containing only <c>ScanSessionToken</c>.
    /// The token is resolved to a session and validated (expiry, pending state, business ownership)
    /// by <see cref="ScanSessionTokenResolver"/>.
    /// </para>
    /// <para>
    /// The scan session contains a JSON snapshot (<see cref="ScanSession.SelectedRewardsJson"/>) of the
    /// selected reward tiers (as <see cref="SelectedRewardItemDto"/> items). This snapshot ensures that
    /// redemption confirmation is replay-safe and independent from any client-provided reward list.
    /// </para>
    /// <para>
    /// One-time use is enforced by consuming the token and marking the session as completed.
    /// </para>
    /// </remarks>
    public sealed class ConfirmRedemptionFromSessionHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUserService;
        private readonly IClock _clock;
        private readonly ScanSessionTokenResolver _tokenResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmRedemptionFromSessionHandler"/> class.
        /// </summary>
        public ConfirmRedemptionFromSessionHandler(
            IAppDbContext db,
            ICurrentUserService currentUserService,
            IClock clock,
            ScanSessionTokenResolver tokenResolver)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _tokenResolver = tokenResolver ?? throw new ArgumentNullException(nameof(tokenResolver));
        }

        /// <summary>
        /// Confirms a redemption based on the specified scan session token within the given business context.
        /// </summary>
        public async Task<Result<ConfirmRedemptionResultDto>> HandleAsync(
            ConfirmRedemptionFromSessionDto dto,
            Guid businessId,
            CancellationToken ct = default)
        {
            if (dto is null)
            {
                return Result<ConfirmRedemptionResultDto>.Fail("Request is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.ScanSessionToken))
            {
                return Result<ConfirmRedemptionResultDto>.Fail("ScanSessionToken is required.");
            }

            if (businessId == Guid.Empty)
            {
                return Result<ConfirmRedemptionResultDto>.Fail("BusinessId is required.");
            }

            var resolvedResult = await _tokenResolver
                .ResolveForBusinessAsync(dto.ScanSessionToken, businessId, ct)
                .ConfigureAwait(false);

            if (!resolvedResult.Succeeded || resolvedResult.Value is null)
            {
                return Result<ConfirmRedemptionResultDto>.Fail(resolvedResult.Error ?? "Invalid scan session token.");
            }

            var token = resolvedResult.Value.Token;
            var session = resolvedResult.Value.Session;

            if (session.Mode != LoyaltyScanMode.Redemption)
            {
                return Result<ConfirmRedemptionResultDto>.Fail("Scan session is not in redemption mode.");
            }

            // Consume token early to reduce replay risk. In later failures we cancel the session.
            await _tokenResolver
                .ConsumeAsync(token, businessId, session.BusinessLocationId, ct)
                .ConfigureAwait(false);

            var now = _clock.UtcNow;

            if (session.ExpiresAtUtc <= now)
            {
                session.Status = LoyaltyScanStatus.Expired;
                session.Outcome = "Expired";
                session.FailureReason = "Session expired before redemption confirmation.";
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);

                return Result<ConfirmRedemptionResultDto>.Fail("Scan session has expired.");
            }

            var account = await _db.Set<LoyaltyAccount>()
                .AsQueryable()
                .SingleOrDefaultAsync(a => a.Id == session.LoyaltyAccountId && !a.IsDeleted, ct)
                .ConfigureAwait(false);

            if (account is null)
            {
                session.Status = LoyaltyScanStatus.Cancelled;
                session.Outcome = "AccountNotFound";
                session.FailureReason = "Loyalty account for scan session not found.";
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);

                return Result<ConfirmRedemptionResultDto>.Fail("Loyalty account for scan session not found.");
            }

            if (account.Status != LoyaltyAccountStatus.Active)
            {
                session.Status = LoyaltyScanStatus.Cancelled;
                session.Outcome = "AccountNotActive";
                session.FailureReason = "Loyalty account is not active.";
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);

                return Result<ConfirmRedemptionResultDto>.Fail("Loyalty account is not active.");
            }

            var selections = ParseSelectedRewards(session.SelectedRewardsJson);
            if (selections.Count == 0)
            {
                session.Status = LoyaltyScanStatus.Cancelled;
                session.Outcome = "NoSelections";
                session.FailureReason = "Scan session does not contain any selected rewards.";
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);

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
                session.Status = LoyaltyScanStatus.Cancelled;
                session.Outcome = "InvalidSelections";
                session.FailureReason = "Selected rewards do not require any points.";
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);

                return Result<ConfirmRedemptionResultDto>.Fail("Selected rewards do not require any points.");
            }

            if (account.PointsBalance < totalRequiredPoints)
            {
                session.Status = LoyaltyScanStatus.Cancelled;
                session.Outcome = "InsufficientPoints";
                session.FailureReason = "Account does not have enough points at confirmation time.";
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);

                return Result<ConfirmRedemptionResultDto>.Fail("Insufficient points for selected rewards.");
            }

            var staffUserId = _currentUserService.GetCurrentUserId();
            if (staffUserId == Guid.Empty)
            {
                return Result<ConfirmRedemptionResultDto>.Fail("User is not authenticated.");
            }

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

            account.PointsBalance -= totalRequiredPoints;

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
        /// Parses the stored JSON payload of selected rewards into DTO items.
        /// Malformed or missing JSON is treated as "no selections".
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
                return new List<SelectedRewardItemDto>();
            }
        }
    }
}
