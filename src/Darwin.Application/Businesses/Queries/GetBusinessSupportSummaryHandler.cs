using System;
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
    /// Returns operational counts for delegated business support and onboarding queues.
    /// </summary>
    public sealed class GetBusinessSupportSummaryHandler
    {
        private readonly IAppDbContext _db;

        public GetBusinessSupportSummaryHandler(IAppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Builds support-focused summary counts globally and, when provided, for a selected business context.
        /// </summary>
        public async Task<BusinessSupportSummaryDto> HandleAsync(Guid? selectedBusinessId = null, CancellationToken ct = default)
        {
            var nowUtc = DateTime.UtcNow;

            var attentionBusinessQuery = _db.Set<Business>().AsNoTracking().Where(x =>
                x.OperationalStatus != BusinessOperationalStatus.Approved ||
                !x.IsActive ||
                !_db.Set<BusinessMember>().Any(m => m.BusinessId == x.Id && m.IsActive && m.Role == BusinessMemberRole.Owner) ||
                !_db.Set<BusinessLocation>().Any(l => l.BusinessId == x.Id && !l.IsDeleted && l.IsPrimary) ||
                string.IsNullOrWhiteSpace(x.ContactEmail) ||
                string.IsNullOrWhiteSpace(x.LegalName));

            var pendingApprovalCount = await _db.Set<Business>().AsNoTracking()
                .CountAsync(x => x.OperationalStatus == BusinessOperationalStatus.PendingApproval, ct)
                .ConfigureAwait(false);

            var suspendedCount = await _db.Set<Business>().AsNoTracking()
                .CountAsync(x => x.OperationalStatus == BusinessOperationalStatus.Suspended, ct)
                .ConfigureAwait(false);

            var missingOwnerCount = await _db.Set<Business>().AsNoTracking()
                .CountAsync(x => !_db.Set<BusinessMember>().Any(m => m.BusinessId == x.Id && m.IsActive && m.Role == BusinessMemberRole.Owner), ct)
                .ConfigureAwait(false);

            var attentionCount = await attentionBusinessQuery.CountAsync(ct).ConfigureAwait(false);

            var openInvitationsCount = await _db.Set<BusinessInvitation>().AsNoTracking()
                .CountAsync(x => x.Status == BusinessInvitationStatus.Pending || x.Status == BusinessInvitationStatus.Expired, ct)
                .ConfigureAwait(false);

            var pendingActivationCount = await
                (from member in _db.Set<BusinessMember>().AsNoTracking()
                 join user in _db.Set<User>().AsNoTracking() on member.UserId equals user.Id
                 where !user.IsDeleted && member.IsActive && !user.EmailConfirmed
                 select member.Id)
                .CountAsync(ct)
                .ConfigureAwait(false);

            var lockedMembersCount = await
                (from member in _db.Set<BusinessMember>().AsNoTracking()
                 join user in _db.Set<User>().AsNoTracking() on member.UserId equals user.Id
                 where !user.IsDeleted && user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > nowUtc
                 select member.Id)
                .CountAsync(ct)
                .ConfigureAwait(false);

            var dto = new BusinessSupportSummaryDto
            {
                PendingApprovalBusinessCount = pendingApprovalCount,
                SuspendedBusinessCount = suspendedCount,
                MissingOwnerBusinessCount = missingOwnerCount,
                AttentionBusinessCount = attentionCount,
                OpenInvitationCount = openInvitationsCount,
                PendingActivationMemberCount = pendingActivationCount,
                LockedMemberCount = lockedMembersCount
            };

            if (selectedBusinessId.HasValue)
            {
                var businessId = selectedBusinessId.Value;

                dto.SelectedBusinessOpenInvitationCount = await _db.Set<BusinessInvitation>().AsNoTracking()
                    .CountAsync(x => x.BusinessId == businessId && (x.Status == BusinessInvitationStatus.Pending || x.Status == BusinessInvitationStatus.Expired), ct)
                    .ConfigureAwait(false);

                dto.SelectedBusinessPendingActivationCount =
                    await (from member in _db.Set<BusinessMember>().AsNoTracking()
                           join user in _db.Set<User>().AsNoTracking() on member.UserId equals user.Id
                           where member.BusinessId == businessId && member.IsActive && !user.IsDeleted && !user.EmailConfirmed
                           select member.Id)
                    .CountAsync(ct)
                    .ConfigureAwait(false);

                dto.SelectedBusinessLockedMemberCount =
                    await (from member in _db.Set<BusinessMember>().AsNoTracking()
                           join user in _db.Set<User>().AsNoTracking() on member.UserId equals user.Id
                           where member.BusinessId == businessId && !user.IsDeleted && user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > nowUtc
                           select member.Id)
                    .CountAsync(ct)
                    .ConfigureAwait(false);
            }

            return dto;
        }
    }
}
