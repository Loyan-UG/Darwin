using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Integration;
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

            var approvedInactiveCount = await _db.Set<Business>().AsNoTracking()
                .CountAsync(x => x.OperationalStatus == BusinessOperationalStatus.Approved && !x.IsActive, ct)
                .ConfigureAwait(false);

            var missingOwnerCount = await _db.Set<Business>().AsNoTracking()
                .CountAsync(x => !_db.Set<BusinessMember>().Any(m => m.BusinessId == x.Id && m.IsActive && m.Role == BusinessMemberRole.Owner), ct)
                .ConfigureAwait(false);

            var missingPrimaryLocationCount = await _db.Set<Business>().AsNoTracking()
                .CountAsync(x => !_db.Set<BusinessLocation>().Any(l => l.BusinessId == x.Id && !l.IsDeleted && l.IsPrimary), ct)
                .ConfigureAwait(false);

            var missingContactEmailCount = await _db.Set<Business>().AsNoTracking()
                .CountAsync(x => string.IsNullOrWhiteSpace(x.ContactEmail), ct)
                .ConfigureAwait(false);

            var missingLegalNameCount = await _db.Set<Business>().AsNoTracking()
                .CountAsync(x => string.IsNullOrWhiteSpace(x.LegalName), ct)
                .ConfigureAwait(false);

            var attentionCount = await attentionBusinessQuery.CountAsync(ct).ConfigureAwait(false);

            var pendingInvitationsCount = await _db.Set<BusinessInvitation>().AsNoTracking()
                .CountAsync(x => x.Status == BusinessInvitationStatus.Pending, ct)
                .ConfigureAwait(false);

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

            var failedEmailAuditQuery = _db.Set<EmailDispatchAudit>()
                .AsNoTracking()
                .Where(x => x.Status == "Failed");

            var failedInvitationCount = await failedEmailAuditQuery
                .CountAsync(x => x.FlowKey == "BusinessInvitation", ct)
                .ConfigureAwait(false);

            var failedActivationCount = await failedEmailAuditQuery
                .CountAsync(x => x.FlowKey == "AccountActivation", ct)
                .ConfigureAwait(false);

            var failedPasswordResetCount = await failedEmailAuditQuery
                .CountAsync(x => x.FlowKey == "PasswordReset", ct)
                .ConfigureAwait(false);

            var failedAdminTestCount = await failedEmailAuditQuery
                .CountAsync(x => x.FlowKey == "AdminCommunicationTest", ct)
                .ConfigureAwait(false);

            var dto = new BusinessSupportSummaryDto
            {
                PendingApprovalBusinessCount = pendingApprovalCount,
                SuspendedBusinessCount = suspendedCount,
                ApprovedInactiveBusinessCount = approvedInactiveCount,
                MissingOwnerBusinessCount = missingOwnerCount,
                MissingPrimaryLocationBusinessCount = missingPrimaryLocationCount,
                MissingContactEmailBusinessCount = missingContactEmailCount,
                MissingLegalNameBusinessCount = missingLegalNameCount,
                PendingInvitationCount = pendingInvitationsCount,
                AttentionBusinessCount = attentionCount,
                OpenInvitationCount = openInvitationsCount,
                PendingActivationMemberCount = pendingActivationCount,
                LockedMemberCount = lockedMembersCount,
                FailedInvitationCount = failedInvitationCount,
                FailedActivationCount = failedActivationCount,
                FailedPasswordResetCount = failedPasswordResetCount,
                FailedAdminTestCount = failedAdminTestCount
            };

            if (selectedBusinessId.HasValue)
            {
                var businessId = selectedBusinessId.Value;

                dto.SelectedBusinessPendingInvitationCount = await _db.Set<BusinessInvitation>().AsNoTracking()
                    .CountAsync(x => x.BusinessId == businessId && x.Status == BusinessInvitationStatus.Pending, ct)
                    .ConfigureAwait(false);

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

                var selectedBusinessFailedEmailAuditQuery = failedEmailAuditQuery
                    .Where(x => x.BusinessId == businessId);

                dto.SelectedBusinessFailedInvitationCount = await selectedBusinessFailedEmailAuditQuery
                    .CountAsync(x => x.FlowKey == "BusinessInvitation", ct)
                    .ConfigureAwait(false);

                dto.SelectedBusinessFailedActivationCount = await selectedBusinessFailedEmailAuditQuery
                    .CountAsync(x => x.FlowKey == "AccountActivation", ct)
                    .ConfigureAwait(false);

                dto.SelectedBusinessFailedPasswordResetCount = await selectedBusinessFailedEmailAuditQuery
                    .CountAsync(x => x.FlowKey == "PasswordReset", ct)
                    .ConfigureAwait(false);

                dto.SelectedBusinessFailedAdminTestCount = await selectedBusinessFailedEmailAuditQuery
                    .CountAsync(x => x.FlowKey == "AdminCommunicationTest", ct)
                    .ConfigureAwait(false);
            }

            return dto;
        }
    }
}
