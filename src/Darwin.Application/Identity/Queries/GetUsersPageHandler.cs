using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Application.Identity.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Queries
{
    /// <summary>
    /// Returns a paged set of users for Admin listing. Uses simple filters/sorts for now.
    /// </summary>
    public sealed class GetUsersPageHandler
    {
        private readonly IAppDbContext _db;
        public GetUsersPageHandler(IAppDbContext db) => _db = db;

        public async Task<(IReadOnlyList<UserListItemDto> Items, int Total)> HandleAsync(
            int page, int pageSize, string? emailFilter, UserQueueFilter filter = UserQueueFilter.All, CancellationToken ct = default)
        {
            var q = _db.Set<Darwin.Domain.Entities.Identity.User>()
                .AsNoTracking()
                .Where(u => !u.IsDeleted);

            var nowUtc = DateTime.UtcNow;
            q = filter switch
            {
                UserQueueFilter.Unconfirmed => q.Where(u => !u.EmailConfirmed),
                UserQueueFilter.Locked => q.Where(u => u.LockoutEndUtc.HasValue && u.LockoutEndUtc.Value > nowUtc),
                UserQueueFilter.Inactive => q.Where(u => !u.IsActive),
                UserQueueFilter.MobileLinked => q.Where(u => _db.Set<Darwin.Domain.Entities.Identity.UserDevice>().Any(d => d.UserId == u.Id && !d.IsDeleted && d.IsActive)),
                _ => q
            };

            if (!string.IsNullOrWhiteSpace(emailFilter))
            {
                var term = QueryLikePattern.Contains(emailFilter);
                q = q.Where(u =>
                    EF.Functions.Like(u.Email, term, QueryLikePattern.EscapeCharacter) ||
                    (u.FirstName != null && EF.Functions.Like(u.FirstName, term, QueryLikePattern.EscapeCharacter)) ||
                    (u.LastName != null && EF.Functions.Like(u.LastName, term, QueryLikePattern.EscapeCharacter)));
            }

            var total = await q.CountAsync(ct);

            var items = await q.OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(u => new UserListItemDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    PhoneE164 = u.PhoneE164,
                    IsActive = u.IsActive,
                    IsSystem = u.IsSystem,
                    EmailConfirmed = u.EmailConfirmed,
                    LockoutEndUtc = u.LockoutEndUtc,
                    MobileDeviceCount = _db.Set<Darwin.Domain.Entities.Identity.UserDevice>().Count(d => d.UserId == u.Id && !d.IsDeleted && d.IsActive),
                    RowVersion = u.RowVersion
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }

    public sealed class GetUserOpsSummaryHandler
    {
        private readonly IAppDbContext _db;

        public GetUserOpsSummaryHandler(IAppDbContext db) => _db = db;

        public async Task<UserOpsSummaryDto> HandleAsync(CancellationToken ct = default)
        {
            var users = _db.Set<Darwin.Domain.Entities.Identity.User>()
                .AsNoTracking()
                .Where(u => !u.IsDeleted);

            var nowUtc = DateTime.UtcNow;
            return new UserOpsSummaryDto
            {
                TotalCount = await users.CountAsync(ct).ConfigureAwait(false),
                UnconfirmedCount = await users.CountAsync(u => !u.EmailConfirmed, ct).ConfigureAwait(false),
                LockedCount = await users.CountAsync(u => u.LockoutEndUtc.HasValue && u.LockoutEndUtc.Value > nowUtc, ct).ConfigureAwait(false),
                InactiveCount = await users.CountAsync(u => !u.IsActive, ct).ConfigureAwait(false),
                MobileLinkedCount = await users.CountAsync(u => _db.Set<Darwin.Domain.Entities.Identity.UserDevice>().Any(d => d.UserId == u.Id && !d.IsDeleted && d.IsActive), ct).ConfigureAwait(false)
            };
        }
    }
}
