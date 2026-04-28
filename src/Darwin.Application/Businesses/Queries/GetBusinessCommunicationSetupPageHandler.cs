using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Returns a paged list of businesses that need phase-1 communication setup attention.
    /// </summary>
    public sealed class GetBusinessCommunicationSetupPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;

        public GetBusinessCommunicationSetupPageHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<(List<BusinessCommunicationSetupListItemDto> Items, int Total)> HandleAsync(
            int page,
            int pageSize,
            string? query = null,
            bool setupOnly = true,
            BusinessCommunicationSetupFilter filter = BusinessCommunicationSetupFilter.NeedsSetup,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var baseQuery = _db.Set<Business>().AsNoTracking().Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim().ToLowerInvariant();
                baseQuery = baseQuery.Where(x =>
                    x.Name.ToLower().Contains(q) ||
                    (x.LegalName != null && x.LegalName.ToLower().Contains(q)) ||
                    (x.SupportEmail != null && x.SupportEmail.ToLower().Contains(q)) ||
                    (x.CommunicationSenderName != null && x.CommunicationSenderName.ToLower().Contains(q)) ||
                    (x.CommunicationReplyToEmail != null && x.CommunicationReplyToEmail.ToLower().Contains(q)));
            }

            if (setupOnly)
            {
                baseQuery = baseQuery.Where(x =>
                    (x.CustomerEmailNotificationsEnabled || x.CustomerMarketingEmailsEnabled || x.OperationalAlertEmailsEnabled) &&
                    (string.IsNullOrWhiteSpace(x.SupportEmail) ||
                     string.IsNullOrWhiteSpace(x.CommunicationSenderName) ||
                     string.IsNullOrWhiteSpace(x.CommunicationReplyToEmail)));
            }

            baseQuery = filter switch
            {
                BusinessCommunicationSetupFilter.MissingSupportEmail => baseQuery.Where(x => string.IsNullOrWhiteSpace(x.SupportEmail)),
                BusinessCommunicationSetupFilter.MissingSenderIdentity => baseQuery.Where(x =>
                    string.IsNullOrWhiteSpace(x.CommunicationSenderName) ||
                    string.IsNullOrWhiteSpace(x.CommunicationReplyToEmail)),
                BusinessCommunicationSetupFilter.TransactionalEnabled => baseQuery.Where(x => x.CustomerEmailNotificationsEnabled),
                BusinessCommunicationSetupFilter.MarketingEnabled => baseQuery.Where(x => x.CustomerMarketingEmailsEnabled),
                BusinessCommunicationSetupFilter.OperationalAlertsEnabled => baseQuery.Where(x => x.OperationalAlertEmailsEnabled),
                BusinessCommunicationSetupFilter.All => baseQuery,
                _ => baseQuery
            };

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await baseQuery
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new BusinessCommunicationSetupListItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    LegalName = x.LegalName,
                    SupportEmail = x.SupportEmail,
                    CommunicationSenderName = x.CommunicationSenderName,
                    CommunicationReplyToEmail = x.CommunicationReplyToEmail,
                    CustomerEmailNotificationsEnabled = x.CustomerEmailNotificationsEnabled,
                    CustomerMarketingEmailsEnabled = x.CustomerMarketingEmailsEnabled,
                    OperationalAlertEmailsEnabled = x.OperationalAlertEmailsEnabled,
                    MissingSupportEmail = string.IsNullOrWhiteSpace(x.SupportEmail),
                    MissingSenderIdentity = string.IsNullOrWhiteSpace(x.CommunicationSenderName) || string.IsNullOrWhiteSpace(x.CommunicationReplyToEmail)
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }
}
