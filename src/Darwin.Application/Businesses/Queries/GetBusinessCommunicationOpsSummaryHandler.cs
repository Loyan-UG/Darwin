using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
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

            var transactionalEnabledTask = baseQuery.CountAsync(x => x.CustomerEmailNotificationsEnabled, ct);
            var marketingEnabledTask = baseQuery.CountAsync(x => x.CustomerMarketingEmailsEnabled, ct);
            var operationalAlertsEnabledTask = baseQuery.CountAsync(x => x.OperationalAlertEmailsEnabled, ct);
            var missingSupportEmailTask = baseQuery.CountAsync(x => string.IsNullOrWhiteSpace(x.SupportEmail), ct);
            var missingSenderIdentityTask = baseQuery.CountAsync(x =>
                string.IsNullOrWhiteSpace(x.CommunicationSenderName) ||
                string.IsNullOrWhiteSpace(x.CommunicationReplyToEmail), ct);
            var requiresEmailSetupTask = baseQuery.CountAsync(x =>
                (x.CustomerEmailNotificationsEnabled || x.CustomerMarketingEmailsEnabled || x.OperationalAlertEmailsEnabled) &&
                (string.IsNullOrWhiteSpace(x.SupportEmail) ||
                 string.IsNullOrWhiteSpace(x.CommunicationSenderName) ||
                 string.IsNullOrWhiteSpace(x.CommunicationReplyToEmail)), ct);

            await Task.WhenAll(
                transactionalEnabledTask,
                marketingEnabledTask,
                operationalAlertsEnabledTask,
                missingSupportEmailTask,
                missingSenderIdentityTask,
                requiresEmailSetupTask).ConfigureAwait(false);

            return new BusinessCommunicationOpsSummaryDto
            {
                BusinessesWithCustomerEmailNotificationsEnabledCount = transactionalEnabledTask.Result,
                BusinessesWithMarketingEmailsEnabledCount = marketingEnabledTask.Result,
                BusinessesWithOperationalAlertEmailsEnabledCount = operationalAlertsEnabledTask.Result,
                BusinessesMissingSupportEmailCount = missingSupportEmailTask.Result,
                BusinessesMissingSenderIdentityCount = missingSenderIdentityTask.Result,
                BusinessesRequiringEmailSetupCount = requiresEmailSetupTask.Result
            };
        }
    }
}
