using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Returns a paged list of business members enriched with user display data for admin grids.
    /// </summary>
    public sealed class GetBusinessMembersPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;
        private readonly IClock _clock;

        public GetBusinessMembersPageHandler(IAppDbContext db, IClock? clock = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
        }

        public async Task<(List<BusinessMemberListItemDto> Items, int Total)> HandleAsync(
            Guid businessId,
            int page,
            int pageSize,
            string? query = null,
            BusinessMemberSupportFilter filter = BusinessMemberSupportFilter.All,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var baseQuery =
                from member in _db.Set<BusinessMember>().AsNoTracking()
                join user in _db.Set<User>().AsNoTracking() on member.UserId equals user.Id into userJoin
                from user in userJoin.DefaultIfEmpty()
                where member.BusinessId == businessId &&
                      !member.IsDeleted &&
                      (user == null || !user.IsDeleted)
                select new
                {
                    Member = member,
                    UserDisplayName = user == null
                        ? "Deleted user"
                        : string.IsNullOrWhiteSpace(((user.FirstName ?? string.Empty) + " " + (user.LastName ?? string.Empty)).Trim())
                            ? user.Email
                            : ((user.FirstName ?? string.Empty) + " " + (user.LastName ?? string.Empty)).Trim(),
                    UserEmail = user == null ? string.Empty : user.Email,
                    EmailConfirmed = user != null && user.EmailConfirmed,
                    LockoutEndUtc = user == null ? null : user.LockoutEndUtc
                };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = QueryLikePattern.Contains(query);
                var roleMatches = BusinessSearchTermResolver.ResolveMemberRoleSearch(query);
                baseQuery = baseQuery.Where(x =>
                    EF.Functions.Like(x.UserDisplayName, q, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.UserEmail, q, QueryLikePattern.EscapeCharacter) ||
                    roleMatches.Contains(x.Member.Role));
            }

            var nowUtc = _clock.UtcNow;

            baseQuery = filter switch
            {
                BusinessMemberSupportFilter.Attention => baseQuery.Where(x =>
                    !x.EmailConfirmed ||
                    (x.LockoutEndUtc.HasValue && x.LockoutEndUtc.Value > nowUtc)),
                BusinessMemberSupportFilter.PendingActivation => baseQuery.Where(x => !x.EmailConfirmed),
                BusinessMemberSupportFilter.Locked => baseQuery.Where(x =>
                    x.LockoutEndUtc.HasValue && x.LockoutEndUtc.Value > nowUtc),
                _ => baseQuery
            };

            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderBy(x => x.Member.Role)
                .ThenBy(x => x.UserDisplayName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new BusinessMemberListItemDto
                {
                    Id = x.Member.Id,
                    BusinessId = x.Member.BusinessId,
                    UserId = x.Member.UserId,
                    UserDisplayName = x.UserDisplayName,
                    UserEmail = x.UserEmail,
                    EmailConfirmed = x.EmailConfirmed,
                    LockoutEndUtc = x.LockoutEndUtc,
                    Role = x.Member.Role,
                    IsActive = x.Member.IsActive,
                    ModifiedAtUtc = x.Member.ModifiedAtUtc,
                    RowVersion = x.Member.RowVersion
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }

    internal static class BusinessSearchTermResolver
    {
        public static IReadOnlyList<BusinessMemberRole> ResolveMemberRoleSearch(string term)
        {
            var normalized = term.Trim();
            if (normalized.Length == 0)
            {
                return Array.Empty<BusinessMemberRole>();
            }

            return new (BusinessMemberRole Value, string[] Tokens)[]
            {
                (BusinessMemberRole.Owner, ["owner"]),
                (BusinessMemberRole.Manager, ["manager"]),
                (BusinessMemberRole.Staff, ["staff"])
            }
                .Where(entry => entry.Tokens.Any(token => token.Contains(normalized, StringComparison.OrdinalIgnoreCase)))
                .Select(entry => entry.Value)
                .ToArray();
        }
    }
}
