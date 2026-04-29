using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Queries
{
    /// <summary>
    /// Returns paged loyalty-account rows for admin operations.
    /// </summary>
    public sealed class GetLoyaltyAccountsPageHandler
    {
        private readonly IAppDbContext _db;

        public GetLoyaltyAccountsPageHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<(IReadOnlyList<LoyaltyAccountAdminListItemDto> Items, int Total)> HandleAsync(
            Guid businessId,
            int page = 1,
            int pageSize = 20,
            string? query = null,
            LoyaltyAccountStatus? status = null,
            CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var baseQuery =
                from account in _db.Set<LoyaltyAccount>().AsNoTracking()
                join user in _db.Set<User>().AsNoTracking() on account.UserId equals user.Id
                where account.BusinessId == businessId && !account.IsDeleted && !user.IsDeleted
                select new { account, user };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = QueryLikePattern.Contains(query);
                baseQuery = baseQuery.Where(x =>
                    EF.Functions.Like(x.user.Email, term, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(((x.user.FirstName ?? string.Empty) + " " + (x.user.LastName ?? string.Empty)), term, QueryLikePattern.EscapeCharacter));
            }

            if (status.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.account.Status == status.Value);
            }

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await baseQuery
                .OrderByDescending(x => x.account.LastAccrualAtUtc ?? x.account.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new LoyaltyAccountAdminListItemDto
                {
                    Id = x.account.Id,
                    BusinessId = x.account.BusinessId,
                    UserId = x.account.UserId,
                    UserEmail = x.user.Email,
                    UserDisplayName =
                        string.IsNullOrWhiteSpace(((x.user.FirstName ?? string.Empty) + " " + (x.user.LastName ?? string.Empty)).Trim())
                            ? x.user.Email
                            : ((x.user.FirstName ?? string.Empty) + " " + (x.user.LastName ?? string.Empty)).Trim(),
                    Status = x.account.Status,
                    PointsBalance = x.account.PointsBalance,
                    LifetimePoints = x.account.LifetimePoints,
                    LastAccrualAtUtc = x.account.LastAccrualAtUtc,
                    RowVersion = x.account.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }

        public async Task<LoyaltyAccountOpsSummaryDto> GetSummaryAsync(Guid businessId, CancellationToken ct = default)
        {
            var recentAccrualCutoffUtc = DateTime.UtcNow.AddDays(-30);

            return await _db.Set<LoyaltyAccount>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted)
                .GroupBy(_ => 1)
                .Select(group => new LoyaltyAccountOpsSummaryDto
                {
                    TotalCount = group.Count(),
                    ActiveCount = group.Count(x => x.Status == LoyaltyAccountStatus.Active),
                    SuspendedCount = group.Count(x => x.Status == LoyaltyAccountStatus.Suspended),
                    ZeroBalanceCount = group.Count(x => x.PointsBalance <= 0),
                    RecentAccrualCount = group.Count(x => x.LastAccrualAtUtc.HasValue && x.LastAccrualAtUtc >= recentAccrualCutoffUtc)
                })
                .SingleOrDefaultAsync(ct)
                .ConfigureAwait(false)
                ?? new LoyaltyAccountOpsSummaryDto();
        }
    }
}
