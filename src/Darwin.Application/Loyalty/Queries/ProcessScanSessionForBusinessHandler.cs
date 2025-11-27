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

namespace Darwin.Application.Loyalty.Queries
{
    /// <summary>
    /// Processes a scan session from the perspective of a business device.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The business app provides the scan session identifier extracted from the
    /// QR code and its own authenticated business context. The handler validates
    /// ownership, expiry, and status of the session, then returns a business-
    /// facing view that contains the mode, current points, and optional reward
    /// selections.
    /// </para>
    /// <para>
    /// No points are accrued or redeemed in this handler. It only prepares
    /// the information required for the cashier to decide what to do next.
    /// Subsequent calls to <c>ConfirmAccrualFromSessionHandler</c> or
    /// <c>ConfirmRedemptionFromSessionHandler</c> perform the actual mutation.
    /// </para>
    /// </remarks>
    public sealed class ProcessScanSessionForBusinessHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUserService;
        private readonly IClock _clock;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessScanSessionForBusinessHandler"/> class.
        /// </summary>
        public ProcessScanSessionForBusinessHandler(
            IAppDbContext db,
            ICurrentUserService currentUserService,
            IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// Validates and materializes a scan session for the specified business.
        /// </summary>
        /// <param name="scanSessionId">Identifier of the scan session that was scanned.</param>
        /// <param name="businessId">
        /// Identifier of the business from the authenticated context of the business app.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<Result<ScanSessionBusinessViewDto>> HandleAsync(
            Guid scanSessionId,
            Guid businessId,
            CancellationToken ct = default)
        {
            if (scanSessionId == Guid.Empty)
            {
                return Result<ScanSessionBusinessViewDto>.Fail("ScanSessionId is required.");
            }

            if (businessId == Guid.Empty)
            {
                return Result<ScanSessionBusinessViewDto>.Fail("BusinessId is required.");
            }

            var session = await _db.Set<ScanSession>()
                .AsQueryable()
                .SingleOrDefaultAsync(s => s.Id == scanSessionId && !s.IsDeleted, ct)
                .ConfigureAwait(false);

            if (session is null)
            {
                return Result<ScanSessionBusinessViewDto>.Fail("Scan session not found.");
            }

            if (session.BusinessId != businessId)
            {
                return Result<ScanSessionBusinessViewDto>.Fail("Scan session does not belong to this business.");
            }

            var now = _clock.UtcNow;
            if (session.ExpiresAtUtc <= now)
            {
                session.Status = LoyaltyScanStatus.Expired;
                session.Outcome = "Expired";
                session.FailureReason = "Session expired before use.";

                await _db.SaveChangesAsync(ct).ConfigureAwait(false);

                return Result<ScanSessionBusinessViewDto>.Fail("Scan session has expired.");
            }

            if (session.Status != LoyaltyScanStatus.Pending)
            {
                return Result<ScanSessionBusinessViewDto>.Fail("Scan session is not in a pending state.");
            }

            var account = await _db.Set<LoyaltyAccount>()
                .AsQueryable()
                .SingleOrDefaultAsync(a => a.Id == session.LoyaltyAccountId && !a.IsDeleted, ct)
                .ConfigureAwait(false);

            if (account is null)
            {
                return Result<ScanSessionBusinessViewDto>.Fail("Loyalty account for scan session not found.");
            }

            // TODO (long-term): Join with Users to obtain a friendly display name
            // rather than exposing any PII here.
            var customerDisplayName = (string?)null;

            var dto = new ScanSessionBusinessViewDto
            {
                ScanSessionId = session.Id,
                Mode = session.Mode,
                LoyaltyAccountId = account.Id,
                CurrentPointsBalance = account.PointsBalance,
                CustomerDisplayName = customerDisplayName,
                SelectedRewards = ParseSelectedRewards(session.SelectedRewardsJson)
            };

            return Result<ScanSessionBusinessViewDto>.Ok(dto);
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
                // In case of malformed JSON, fall back to an empty list but do not
                // break the core flow. The cashier can still choose accrual only.
                return new List<SelectedRewardItemDto>();
            }
        }
    }
}
