using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Returns a paged list of invitations for a business.
    /// Expired pending invitations are projected as expired for operational visibility.
    /// </summary>
    public sealed class GetBusinessInvitationsPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;

        public GetBusinessInvitationsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<BusinessInvitationListItemDto> Items, int Total)> HandleAsync(
            Guid businessId,
            int page,
            int pageSize,
            string? query = null,
            BusinessInvitationQueueFilter filter = BusinessInvitationQueueFilter.All,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var utcNow = DateTime.UtcNow;

            var baseQuery =
                from invitation in _db.Set<BusinessInvitation>().AsNoTracking()
                join inviter in _db.Set<User>().AsNoTracking() on invitation.InvitedByUserId equals inviter.Id into inviterJoin
                from inviter in inviterJoin.DefaultIfEmpty()
                where invitation.BusinessId == businessId &&
                      !invitation.IsDeleted &&
                      (inviter == null || !inviter.IsDeleted)
                select new
                {
                    Invitation = invitation,
                    InvitedByDisplayName = inviter == null
                        ? "Unknown admin"
                        : string.IsNullOrWhiteSpace(((inviter.FirstName ?? string.Empty) + " " + (inviter.LastName ?? string.Empty)).Trim())
                            ? inviter.Email
                            : ((inviter.FirstName ?? string.Empty) + " " + (inviter.LastName ?? string.Empty)).Trim(),
                    EffectiveStatus = invitation.Status == BusinessInvitationStatus.Pending && invitation.ExpiresAtUtc < utcNow
                        ? BusinessInvitationStatus.Expired
                        : invitation.Status
                };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim().ToLowerInvariant();
                var statusMatches = BusinessInvitationSearch.ResolveStatusSearch(q);
                var roleMatches = BusinessInvitationSearch.ResolveRoleSearch(q);
                baseQuery = baseQuery.Where(x =>
                    x.Invitation.Email.ToLower().Contains(q) ||
                    x.InvitedByDisplayName.ToLower().Contains(q) ||
                    statusMatches.Contains(x.EffectiveStatus) ||
                    roleMatches.Contains(x.Invitation.Role));
            }

            baseQuery = filter switch
            {
                BusinessInvitationQueueFilter.Open => baseQuery.Where(x =>
                    x.EffectiveStatus == BusinessInvitationStatus.Pending ||
                    x.EffectiveStatus == BusinessInvitationStatus.Expired),
                BusinessInvitationQueueFilter.Pending => baseQuery.Where(x => x.EffectiveStatus == BusinessInvitationStatus.Pending),
                BusinessInvitationQueueFilter.Expired => baseQuery.Where(x => x.EffectiveStatus == BusinessInvitationStatus.Expired),
                BusinessInvitationQueueFilter.Accepted => baseQuery.Where(x => x.EffectiveStatus == BusinessInvitationStatus.Accepted),
                BusinessInvitationQueueFilter.Revoked => baseQuery.Where(x => x.EffectiveStatus == BusinessInvitationStatus.Revoked),
                _ => baseQuery
            };

            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderByDescending(x => x.Invitation.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new BusinessInvitationListItemDto
                {
                    Id = x.Invitation.Id,
                    BusinessId = x.Invitation.BusinessId,
                    Email = x.Invitation.Email,
                    Role = x.Invitation.Role,
                    Status = x.EffectiveStatus,
                    InvitedByDisplayName = x.InvitedByDisplayName,
                    ExpiresAtUtc = x.Invitation.ExpiresAtUtc,
                    AcceptedAtUtc = x.Invitation.AcceptedAtUtc,
                    RevokedAtUtc = x.Invitation.RevokedAtUtc,
                    CreatedAtUtc = x.Invitation.CreatedAtUtc,
                    Note = x.Invitation.Note
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }

    internal static class BusinessInvitationSearch
    {
        public static IReadOnlyList<BusinessInvitationStatus> ResolveStatusSearch(string term)
        {
            return Enum.GetValues<BusinessInvitationStatus>()
                .Where(status => status.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        public static IReadOnlyList<BusinessMemberRole> ResolveRoleSearch(string term)
        {
            return Enum.GetValues<BusinessMemberRole>()
                .Where(role => role.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }
}
