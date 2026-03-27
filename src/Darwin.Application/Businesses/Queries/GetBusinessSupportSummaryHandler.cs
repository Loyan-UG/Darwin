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

            var pendingApprovalTask = _db.Set<Business>().AsNoTracking()
                .CountAsync(x => x.OperationalStatus == BusinessOperationalStatus.PendingApproval, ct);

            var suspendedTask = _db.Set<Business>().AsNoTracking()
                .CountAsync(x => x.OperationalStatus == BusinessOperationalStatus.Suspended, ct);

            var missingOwnerTask = _db.Set<Business>().AsNoTracking()
                .CountAsync(x => !_db.Set<BusinessMember>().Any(m => m.BusinessId == x.Id && m.IsActive && m.Role == BusinessMemberRole.Owner), ct);

            var attentionTask = attentionBusinessQuery.CountAsync(ct);

            var openInvitationsTask = _db.Set<BusinessInvitation>().AsNoTracking()
                .CountAsync(x => x.Status == BusinessInvitationStatus.Pending || x.Status == BusinessInvitationStatus.Expired, ct);

            var pendingActivationTask =
                (from member in _db.Set<BusinessMember>().AsNoTracking()
                 join user in _db.Set<User>().AsNoTracking() on member.UserId equals user.Id
                 where !user.IsDeleted && member.IsActive && !user.EmailConfirmed
                 select member.Id)
                .CountAsync(ct);

            var lockedMembersTask =
                (from member in _db.Set<BusinessMember>().AsNoTracking()
                 join user in _db.Set<User>().AsNoTracking() on member.UserId equals user.Id
                 where !user.IsDeleted && user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > nowUtc
                 select member.Id)
                .CountAsync(ct);

            await Task.WhenAll(
                pendingApprovalTask,
                suspendedTask,
                missingOwnerTask,
                attentionTask,
                openInvitationsTask,
                pendingActivationTask,
                lockedMembersTask).ConfigureAwait(false);

            var dto = new BusinessSupportSummaryDto
            {
                PendingApprovalBusinessCount = pendingApprovalTask.Result,
                SuspendedBusinessCount = suspendedTask.Result,
                MissingOwnerBusinessCount = missingOwnerTask.Result,
                AttentionBusinessCount = attentionTask.Result,
                OpenInvitationCount = openInvitationsTask.Result,
                PendingActivationMemberCount = pendingActivationTask.Result,
                LockedMemberCount = lockedMembersTask.Result
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
