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
using Darwin.Domain.Entities.Identity;
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

            // Build a minimal, business-safe customer label.
            // We intentionally avoid exposing raw identifiers and only provide a friendly hint
            // that helps the cashier confirm the customer verbally.
            var customerDisplayName = await BuildCustomerDisplayNameAsync(account.UserId, ct).ConfigureAwait(false);

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



        /// <summary>
        /// Resolves a privacy-safe customer display label for business-side confirmation.
        /// </summary>
        /// <remarks>
        /// Priority:
        /// 1) FirstName + last-name initial (if available)
        /// 2) FirstName only
        /// 3) Masked email local-part + domain
        /// 4) Null (UI may render fallback label)
        /// </remarks>
        private async Task<string?> BuildCustomerDisplayNameAsync(Guid userId, CancellationToken ct)
        {
            if (userId == Guid.Empty)
            {
                return null;
            }

            var user = await _db.Set<User>()
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct)
                .ConfigureAwait(false);

            if (user is null)
            {
                return null;
            }

            var first = (user.FirstName ?? string.Empty).Trim();
            var last = (user.LastName ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(last))
            {
                return $"{first} {char.ToUpperInvariant(last[0])}.";
            }

            if (!string.IsNullOrWhiteSpace(first))
            {
                return first;
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return null;
            }

            return MaskEmail(user.Email);
        }

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 0 || at == email.Length - 1)
            {
                return "Customer";
            }

            var local = email[..at];
            var domain = email[(at + 1)..];

            var first = local[0];
            return $"{first}***@{domain}";
        }
    }
}
