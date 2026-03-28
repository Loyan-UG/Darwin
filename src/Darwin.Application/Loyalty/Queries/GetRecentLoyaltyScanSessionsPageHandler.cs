using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Queries
{
    /// <summary>
    /// Returns recent scan sessions for admin diagnostics and support workflows.
    /// </summary>
    public sealed class GetRecentLoyaltyScanSessionsPageHandler
    {
        private readonly IAppDbContext _db;

        public GetRecentLoyaltyScanSessionsPageHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<(IReadOnlyList<LoyaltyScanSessionAdminListItemDto> Items, int Total)> HandleAsync(
            Guid businessId,
            int page = 1,
            int pageSize = 20,
            string? query = null,
            LoyaltyScanMode? mode = null,
            LoyaltyScanStatus? status = null,
            CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var baseQuery =
                from session in _db.Set<ScanSession>().AsNoTracking()
                join account in _db.Set<LoyaltyAccount>().AsNoTracking() on session.LoyaltyAccountId equals account.Id
                join user in _db.Set<User>().AsNoTracking() on account.UserId equals user.Id
                where !session.IsDeleted && !account.IsDeleted && !user.IsDeleted && session.BusinessId == businessId
                select new
                {
                    session,
                    account,
                    user
                };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                baseQuery = baseQuery.Where(x =>
                    x.user.Email.Contains(term) ||
                    ((x.user.FirstName ?? string.Empty) + " " + (x.user.LastName ?? string.Empty)).Contains(term) ||
                    x.session.Outcome.Contains(term));
            }

            if (mode.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.session.Mode == mode.Value);
            }

            if (status.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.session.Status == status.Value);
            }

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await baseQuery
                .OrderByDescending(x => x.session.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new LoyaltyScanSessionAdminListItemDto
                {
                    Id = x.session.Id,
                    BusinessId = x.session.BusinessId,
                    LoyaltyAccountId = x.session.LoyaltyAccountId,
                    CustomerEmail = x.user.Email,
                    CustomerDisplayName = string.IsNullOrWhiteSpace(((x.user.FirstName ?? string.Empty) + " " + (x.user.LastName ?? string.Empty)).Trim())
                        ? x.user.Email
                        : ((x.user.FirstName ?? string.Empty) + " " + (x.user.LastName ?? string.Empty)).Trim(),
                    Mode = x.session.Mode,
                    Status = x.session.Status,
                    Outcome = x.session.Outcome,
                    FailureReason = x.session.FailureReason,
                    CreatedAtUtc = x.session.CreatedAtUtc,
                    ExpiresAtUtc = x.session.ExpiresAtUtc,
                    CompletedAtUtc = x.session.Status == LoyaltyScanStatus.Completed ? x.session.ModifiedAtUtc : null
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }
}
