using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Returns a paged list of recent phase-1 email delivery audits for operator visibility.
    /// </summary>
    public sealed class GetEmailDispatchAuditsPageHandler
    {
        private readonly IAppDbContext _db;

        public GetEmailDispatchAuditsPageHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<(List<EmailDispatchAuditListItemDto> Items, int Total)> HandleAsync(
            int page,
            int pageSize,
            string? query = null,
            string? status = null,
            string? flowKey = null,
            Guid? businessId = null,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var baseQuery =
                from audit in _db.Set<EmailDispatchAudit>().AsNoTracking()
                join business in _db.Set<Business>().AsNoTracking() on audit.BusinessId equals business.Id into businessJoin
                from business in businessJoin.DefaultIfEmpty()
                select new
                {
                    Audit = audit,
                    BusinessName = business == null ? null : business.Name
                };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                baseQuery = baseQuery.Where(x =>
                    x.Audit.RecipientEmail.Contains(q) ||
                    x.Audit.Subject.Contains(q) ||
                    x.Audit.Status.Contains(q) ||
                    x.Audit.Provider.Contains(q) ||
                    (x.Audit.FlowKey != null && x.Audit.FlowKey.Contains(q)) ||
                    (x.BusinessName != null && x.BusinessName.Contains(q)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalized = status.Trim();
                baseQuery = baseQuery.Where(x => x.Audit.Status == normalized);
            }

            if (!string.IsNullOrWhiteSpace(flowKey))
            {
                var normalized = flowKey.Trim();
                baseQuery = baseQuery.Where(x => x.Audit.FlowKey == normalized);
            }

            if (businessId.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.Audit.BusinessId == businessId.Value);
            }

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await baseQuery
                .OrderByDescending(x => x.Audit.AttemptedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new EmailDispatchAuditListItemDto
                {
                    Id = x.Audit.Id,
                    Provider = x.Audit.Provider,
                    FlowKey = x.Audit.FlowKey,
                    BusinessId = x.Audit.BusinessId,
                    BusinessName = x.BusinessName,
                    RecipientEmail = x.Audit.RecipientEmail,
                    Subject = x.Audit.Subject,
                    Status = x.Audit.Status,
                    AttemptedAtUtc = x.Audit.AttemptedAtUtc,
                    CompletedAtUtc = x.Audit.CompletedAtUtc,
                    FailureMessage = x.Audit.FailureMessage
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }

        public async Task<EmailDispatchAuditSummaryDto> GetSummaryAsync(Guid? businessId = null, CancellationToken ct = default)
        {
            var audits = _db.Set<EmailDispatchAudit>().AsNoTracking();
            if (businessId.HasValue)
            {
                audits = audits.Where(x => x.BusinessId == businessId.Value);
            }

            var recentThresholdUtc = DateTime.UtcNow.AddHours(-24);

            return new EmailDispatchAuditSummaryDto
            {
                TotalCount = await audits.CountAsync(ct).ConfigureAwait(false),
                FailedCount = await audits.CountAsync(x => x.Status == "Failed", ct).ConfigureAwait(false),
                SentCount = await audits.CountAsync(x => x.Status == "Sent", ct).ConfigureAwait(false),
                PendingCount = await audits.CountAsync(x => x.Status == "Pending", ct).ConfigureAwait(false),
                Recent24HourCount = await audits.CountAsync(x => x.AttemptedAtUtc >= recentThresholdUtc, ct).ConfigureAwait(false),
                FailedInvitationCount = await audits.CountAsync(x => x.Status == "Failed" && x.FlowKey == "BusinessInvitation", ct).ConfigureAwait(false),
                FailedActivationCount = await audits.CountAsync(x => x.Status == "Failed" && x.FlowKey == "AccountActivation", ct).ConfigureAwait(false),
                FailedPasswordResetCount = await audits.CountAsync(x => x.Status == "Failed" && x.FlowKey == "PasswordReset", ct).ConfigureAwait(false),
                FailedAdminTestCount = await audits.CountAsync(x => x.Status == "Failed" && x.FlowKey == "AdminCommunicationTest", ct).ConfigureAwait(false)
            };
        }
    }
}
