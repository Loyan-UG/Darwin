using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Returns aggregated business-level communication configuration gaps for operator dashboards.
    /// </summary>
    public sealed class GetBusinessCommunicationOpsSummaryHandler
    {
        private readonly IAppDbContext _db;

        public GetBusinessCommunicationOpsSummaryHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<BusinessCommunicationOpsSummaryDto> HandleAsync(CancellationToken ct = default)
        {
            var baseQuery = _db.Set<Business>().AsNoTracking().Where(x => !x.IsDeleted);

            var businessSummary = await baseQuery
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    TransactionalEnabledCount = group.Count(x => x.CustomerEmailNotificationsEnabled),
                    MarketingEnabledCount = group.Count(x => x.CustomerMarketingEmailsEnabled),
                    OperationalAlertsEnabledCount = group.Count(x => x.OperationalAlertEmailsEnabled),
                    MissingSupportEmailCount = group.Count(x => x.SupportEmail == null || x.SupportEmail.Trim() == string.Empty),
                    MissingSenderIdentityCount = group.Count(x =>
                        x.CommunicationSenderName == null ||
                        x.CommunicationSenderName.Trim() == string.Empty ||
                        x.CommunicationReplyToEmail == null ||
                        x.CommunicationReplyToEmail.Trim() == string.Empty),
                    RequiresEmailSetupCount = group.Count(x =>
                        (x.CustomerEmailNotificationsEnabled || x.CustomerMarketingEmailsEnabled || x.OperationalAlertEmailsEnabled) &&
                        (x.SupportEmail == null ||
                         x.SupportEmail.Trim() == string.Empty ||
                         x.CommunicationSenderName == null ||
                         x.CommunicationSenderName.Trim() == string.Empty ||
                         x.CommunicationReplyToEmail == null ||
                         x.CommunicationReplyToEmail.Trim() == string.Empty))
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            var failedEmailAuditQuery = _db.Set<EmailDispatchAudit>()
                .AsNoTracking()
                .Where(x => x.Status == "Failed" && !x.IsDeleted);
            var failedAuditSummary = await failedEmailAuditQuery
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    FailedInvitationCount = group.Count(x => x.FlowKey == "BusinessInvitation"),
                    FailedActivationCount = group.Count(x => x.FlowKey == "AccountActivation"),
                    FailedPasswordResetCount = group.Count(x => x.FlowKey == "PasswordReset"),
                    FailedAdminTestCount = group.Count(x => x.FlowKey == "AdminCommunicationTest")
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            return new BusinessCommunicationOpsSummaryDto
            {
                BusinessesWithCustomerEmailNotificationsEnabledCount = businessSummary?.TransactionalEnabledCount ?? 0,
                BusinessesWithMarketingEmailsEnabledCount = businessSummary?.MarketingEnabledCount ?? 0,
                BusinessesWithOperationalAlertEmailsEnabledCount = businessSummary?.OperationalAlertsEnabledCount ?? 0,
                BusinessesMissingSupportEmailCount = businessSummary?.MissingSupportEmailCount ?? 0,
                BusinessesMissingSenderIdentityCount = businessSummary?.MissingSenderIdentityCount ?? 0,
                BusinessesRequiringEmailSetupCount = businessSummary?.RequiresEmailSetupCount ?? 0,
                FailedInvitationCount = failedAuditSummary?.FailedInvitationCount ?? 0,
                FailedActivationCount = failedAuditSummary?.FailedActivationCount ?? 0,
                FailedPasswordResetCount = failedAuditSummary?.FailedPasswordResetCount ?? 0,
                FailedAdminTestCount = failedAuditSummary?.FailedAdminTestCount ?? 0
            };
        }
    }
}
