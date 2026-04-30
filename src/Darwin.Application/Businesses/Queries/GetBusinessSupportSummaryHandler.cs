using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
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
        private readonly IClock _clock;

        public GetBusinessSupportSummaryHandler(IAppDbContext db, IClock? clock = null)
        {
            _db = db;
            _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
        }

        /// <summary>
        /// Builds support-focused summary counts globally and, when provided, for a selected business context.
        /// </summary>
        public async Task<BusinessSupportSummaryDto> HandleAsync(Guid? selectedBusinessId = null, CancellationToken ct = default)
        {
            var nowUtc = _clock.UtcNow;

            var businesses = _db.Set<Business>().AsNoTracking().Where(x => !x.IsDeleted);
            var businessSummary = await businesses
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    PendingApprovalCount = g.Count(x => x.OperationalStatus == BusinessOperationalStatus.PendingApproval),
                    SuspendedCount = g.Count(x => x.OperationalStatus == BusinessOperationalStatus.Suspended),
                    ApprovedInactiveCount = g.Count(x => x.OperationalStatus == BusinessOperationalStatus.Approved && !x.IsActive),
                    MissingOwnerCount = g.Count(x => !_db.Set<BusinessMember>().Any(m => m.BusinessId == x.Id && !m.IsDeleted && m.IsActive && m.Role == BusinessMemberRole.Owner)),
                    MissingPrimaryLocationCount = g.Count(x => !_db.Set<BusinessLocation>().Any(l => l.BusinessId == x.Id && !l.IsDeleted && l.IsPrimary)),
                    MissingContactEmailCount = g.Count(x => x.ContactEmail == null || x.ContactEmail.Trim() == string.Empty),
                    MissingLegalNameCount = g.Count(x => x.LegalName == null || x.LegalName.Trim() == string.Empty),
                    AttentionCount = g.Count(x =>
                        x.OperationalStatus != BusinessOperationalStatus.Approved ||
                        !x.IsActive ||
                        !_db.Set<BusinessMember>().Any(m => m.BusinessId == x.Id && !m.IsDeleted && m.IsActive && m.Role == BusinessMemberRole.Owner) ||
                        !_db.Set<BusinessLocation>().Any(l => l.BusinessId == x.Id && !l.IsDeleted && l.IsPrimary) ||
                        x.ContactEmail == null ||
                        x.ContactEmail.Trim() == string.Empty ||
                        x.LegalName == null ||
                        x.LegalName.Trim() == string.Empty)
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            var invitationSummary = await _db.Set<BusinessInvitation>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    PendingInvitationsCount = g.Count(x => x.Status == BusinessInvitationStatus.Pending),
                    OpenInvitationsCount = g.Count(x => x.Status == BusinessInvitationStatus.Pending || x.Status == BusinessInvitationStatus.Expired)
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            var memberSummary = await
                (from member in _db.Set<BusinessMember>().AsNoTracking()
                 join user in _db.Set<User>().AsNoTracking() on member.UserId equals user.Id
                 where !member.IsDeleted && !user.IsDeleted
                 group new { member, user } by 1 into g
                 select new
                 {
                     PendingActivationCount = g.Count(x => x.member.IsActive && !x.user.EmailConfirmed),
                     LockedMembersCount = g.Count(x => x.user.LockoutEndUtc.HasValue && x.user.LockoutEndUtc.Value > nowUtc)
                 })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            var failedEmailAuditQuery = _db.Set<EmailDispatchAudit>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Status == "Failed");

            var failedEmailSummary = await failedEmailAuditQuery
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    FailedInvitationCount = g.Count(x => x.FlowKey == "BusinessInvitation"),
                    FailedActivationCount = g.Count(x => x.FlowKey == "AccountActivation"),
                    FailedPasswordResetCount = g.Count(x => x.FlowKey == "PasswordReset"),
                    FailedAdminTestCount = g.Count(x => x.FlowKey == "AdminCommunicationTest")
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            var dto = new BusinessSupportSummaryDto
            {
                PendingApprovalBusinessCount = businessSummary?.PendingApprovalCount ?? 0,
                SuspendedBusinessCount = businessSummary?.SuspendedCount ?? 0,
                ApprovedInactiveBusinessCount = businessSummary?.ApprovedInactiveCount ?? 0,
                MissingOwnerBusinessCount = businessSummary?.MissingOwnerCount ?? 0,
                MissingPrimaryLocationBusinessCount = businessSummary?.MissingPrimaryLocationCount ?? 0,
                MissingContactEmailBusinessCount = businessSummary?.MissingContactEmailCount ?? 0,
                MissingLegalNameBusinessCount = businessSummary?.MissingLegalNameCount ?? 0,
                PendingInvitationCount = invitationSummary?.PendingInvitationsCount ?? 0,
                AttentionBusinessCount = businessSummary?.AttentionCount ?? 0,
                OpenInvitationCount = invitationSummary?.OpenInvitationsCount ?? 0,
                PendingActivationMemberCount = memberSummary?.PendingActivationCount ?? 0,
                LockedMemberCount = memberSummary?.LockedMembersCount ?? 0,
                FailedInvitationCount = failedEmailSummary?.FailedInvitationCount ?? 0,
                FailedActivationCount = failedEmailSummary?.FailedActivationCount ?? 0,
                FailedPasswordResetCount = failedEmailSummary?.FailedPasswordResetCount ?? 0,
                FailedAdminTestCount = failedEmailSummary?.FailedAdminTestCount ?? 0
            };

            if (selectedBusinessId.HasValue)
            {
                var businessId = selectedBusinessId.Value;

                var selectedInvitationSummary = await _db.Set<BusinessInvitation>()
                    .AsNoTracking()
                    .Where(x => x.BusinessId == businessId && !x.IsDeleted)
                    .GroupBy(_ => 1)
                    .Select(g => new
                    {
                        PendingInvitationCount = g.Count(x => x.Status == BusinessInvitationStatus.Pending),
                        OpenInvitationCount = g.Count(x => x.Status == BusinessInvitationStatus.Pending || x.Status == BusinessInvitationStatus.Expired)
                    })
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                dto.SelectedBusinessPendingInvitationCount = selectedInvitationSummary?.PendingInvitationCount ?? 0;
                dto.SelectedBusinessOpenInvitationCount = selectedInvitationSummary?.OpenInvitationCount ?? 0;

                var selectedMemberSummary =
                    await (from member in _db.Set<BusinessMember>().AsNoTracking()
                           join user in _db.Set<User>().AsNoTracking() on member.UserId equals user.Id
                           where member.BusinessId == businessId && !member.IsDeleted && !user.IsDeleted
                           group new { member, user } by 1 into g
                           select new
                           {
                               PendingActivationCount = g.Count(x => x.member.IsActive && !x.user.EmailConfirmed),
                               LockedMemberCount = g.Count(x => x.user.LockoutEndUtc.HasValue && x.user.LockoutEndUtc.Value > nowUtc)
                           })
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                dto.SelectedBusinessPendingActivationCount = selectedMemberSummary?.PendingActivationCount ?? 0;
                dto.SelectedBusinessLockedMemberCount = selectedMemberSummary?.LockedMemberCount ?? 0;

                var selectedBusinessFailedEmailAuditQuery = failedEmailAuditQuery
                    .Where(x => x.BusinessId == businessId);

                var selectedFailedEmailSummary = await selectedBusinessFailedEmailAuditQuery
                    .GroupBy(_ => 1)
                    .Select(g => new
                    {
                        FailedInvitationCount = g.Count(x => x.FlowKey == "BusinessInvitation"),
                        FailedActivationCount = g.Count(x => x.FlowKey == "AccountActivation"),
                        FailedPasswordResetCount = g.Count(x => x.FlowKey == "PasswordReset"),
                        FailedAdminTestCount = g.Count(x => x.FlowKey == "AdminCommunicationTest")
                    })
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                dto.SelectedBusinessFailedInvitationCount = selectedFailedEmailSummary?.FailedInvitationCount ?? 0;
                dto.SelectedBusinessFailedActivationCount = selectedFailedEmailSummary?.FailedActivationCount ?? 0;
                dto.SelectedBusinessFailedPasswordResetCount = selectedFailedEmailSummary?.FailedPasswordResetCount ?? 0;
                dto.SelectedBusinessFailedAdminTestCount = selectedFailedEmailSummary?.FailedAdminTestCount ?? 0;
            }

            return dto;
        }
    }
}
