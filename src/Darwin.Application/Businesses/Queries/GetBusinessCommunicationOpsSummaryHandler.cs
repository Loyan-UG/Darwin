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
            var baseQuery = _db.Set<Business>().AsNoTracking();

            var transactionalEnabledCount = await baseQuery
                .CountAsync(x => x.CustomerEmailNotificationsEnabled, ct)
                .ConfigureAwait(false);
            var marketingEnabledCount = await baseQuery
                .CountAsync(x => x.CustomerMarketingEmailsEnabled, ct)
                .ConfigureAwait(false);
            var operationalAlertsEnabledCount = await baseQuery
                .CountAsync(x => x.OperationalAlertEmailsEnabled, ct)
                .ConfigureAwait(false);
            var missingSupportEmailCount = await baseQuery
                .CountAsync(x => string.IsNullOrWhiteSpace(x.SupportEmail), ct)
                .ConfigureAwait(false);
            var missingSenderIdentityCount = await baseQuery
                .CountAsync(x =>
                    string.IsNullOrWhiteSpace(x.CommunicationSenderName) ||
                    string.IsNullOrWhiteSpace(x.CommunicationReplyToEmail), ct)
                .ConfigureAwait(false);
            var requiresEmailSetupCount = await baseQuery
                .CountAsync(x =>
                    (x.CustomerEmailNotificationsEnabled || x.CustomerMarketingEmailsEnabled || x.OperationalAlertEmailsEnabled) &&
                    (string.IsNullOrWhiteSpace(x.SupportEmail) ||
                     string.IsNullOrWhiteSpace(x.CommunicationSenderName) ||
                     string.IsNullOrWhiteSpace(x.CommunicationReplyToEmail)), ct)
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

            return new BusinessCommunicationOpsSummaryDto
            {
                BusinessesWithCustomerEmailNotificationsEnabledCount = transactionalEnabledCount,
                BusinessesWithMarketingEmailsEnabledCount = marketingEnabledCount,
                BusinessesWithOperationalAlertEmailsEnabledCount = operationalAlertsEnabledCount,
                BusinessesMissingSupportEmailCount = missingSupportEmailCount,
                BusinessesMissingSenderIdentityCount = missingSenderIdentityCount,
                BusinessesRequiringEmailSetupCount = requiresEmailSetupCount,
                FailedInvitationCount = failedInvitationCount,
                FailedActivationCount = failedActivationCount,
                FailedPasswordResetCount = failedPasswordResetCount,
                FailedAdminTestCount = failedAdminTestCount
            };
        }
    }
}
