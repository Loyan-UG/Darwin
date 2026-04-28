using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Returns paged owner-override audit rows for a single business.
    /// </summary>
    public sealed class GetBusinessOwnerOverrideAuditsPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;

        public GetBusinessOwnerOverrideAuditsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<BusinessOwnerOverrideAuditListItemDto> Items, int Total)> HandleAsync(
            Guid businessId,
            int page,
            int pageSize,
            string? query = null,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var baseQuery =
                from audit in _db.Set<BusinessOwnerOverrideAudit>().AsNoTracking()
                join user in _db.Set<User>().AsNoTracking() on audit.AffectedUserId equals user.Id into userJoin
                from user in userJoin.DefaultIfEmpty()
                where audit.BusinessId == businessId &&
                      !audit.IsDeleted &&
                      (user == null || !user.IsDeleted)
                select new BusinessOwnerOverrideAuditListItemDto
                {
                    Id = audit.Id,
                    BusinessId = audit.BusinessId,
                    BusinessMemberId = audit.BusinessMemberId,
                    AffectedUserId = audit.AffectedUserId,
                    AffectedUserDisplayName = user == null
                        ? "Deleted user"
                        : string.IsNullOrWhiteSpace(((user.FirstName ?? string.Empty) + " " + (user.LastName ?? string.Empty)).Trim())
                            ? user.Email
                            : ((user.FirstName ?? string.Empty) + " " + (user.LastName ?? string.Empty)).Trim(),
                    AffectedUserEmail = user == null ? string.Empty : user.Email,
                    ActionKind = audit.ActionKind,
                    Reason = audit.Reason,
                    ActorDisplayName = audit.ActorDisplayName,
                    CreatedAtUtc = audit.CreatedAtUtc
                };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim().ToLowerInvariant();
                baseQuery = baseQuery.Where(x =>
                    x.AffectedUserDisplayName.ToLower().Contains(q) ||
                    x.AffectedUserEmail.ToLower().Contains(q) ||
                    x.Reason.ToLower().Contains(q) ||
                    (x.ActorDisplayName != null && x.ActorDisplayName.ToLower().Contains(q)));
            }

            var total = await baseQuery.CountAsync(ct);
            var items = await baseQuery
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }
    }
}
