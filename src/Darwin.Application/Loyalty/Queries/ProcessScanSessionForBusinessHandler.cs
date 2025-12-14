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
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Queries
{
    /// <summary>
    /// Validates and materializes a scan session for business processing after scanning a QR token.
    /// </summary>
    public sealed class ProcessScanSessionForBusinessHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUserService;
        private readonly IClock _clock;
        private readonly ScanSessionTokenResolver _tokenResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessScanSessionForBusinessHandler"/> class.
        /// </summary>
        public ProcessScanSessionForBusinessHandler(
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
        /// Validates and materializes a scan session for the specified business.
        /// </summary>
        /// <param name="scanSessionToken">Opaque token scanned from the QR code.</param>
        /// <param name="businessId">Business id from authenticated context.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<Result<ScanSessionBusinessViewDto>> HandleAsync(
            string scanSessionToken,
            Guid businessId,
            CancellationToken ct = default)
        {
            var resolved = await _tokenResolver.ResolveForBusinessAsync(scanSessionToken, businessId, ct)
                .ConfigureAwait(false);

            if (!resolved.Succeeded || resolved.Value is null)
            {
                return Result<ScanSessionBusinessViewDto>.Fail(resolved.Error ?? "Failed to resolve scan session token.");
            }

            var session = resolved.Value.Session;

            var account = await _db.Set<LoyaltyAccount>()
                .AsQueryable()
                .SingleOrDefaultAsync(a => a.Id == session.LoyaltyAccountId && !a.IsDeleted, ct)
                .ConfigureAwait(false);

            if (account is null)
            {
                return Result<ScanSessionBusinessViewDto>.Fail("Loyalty account for scan session not found.");
            }

            // TODO (long-term): Join with Users to obtain a friendly display name rather than exposing PII.
            var customerDisplayName = (string?)null;

            var dto = new ScanSessionBusinessViewDto
            {
                ScanSessionToken = scanSessionToken,
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
                return new List<SelectedRewardItemDto>();
            }
        }
    }
}
