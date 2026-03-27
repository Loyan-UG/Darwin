using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
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

            var baseQuery = _db.Set<EmailDispatchAudit>().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                baseQuery = baseQuery.Where(x =>
                    x.RecipientEmail.Contains(q) ||
                    x.Subject.Contains(q) ||
                    x.Status.Contains(q) ||
                    x.Provider.Contains(q));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalized = status.Trim();
                baseQuery = baseQuery.Where(x => x.Status == normalized);
            }

            if (!string.IsNullOrWhiteSpace(flowKey))
            {
                var normalized = flowKey.Trim();
                baseQuery = baseQuery.Where(x => x.FlowKey == normalized);
            }

            if (businessId.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.BusinessId == businessId.Value);
            }

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await baseQuery
                .OrderByDescending(x => x.AttemptedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new EmailDispatchAuditListItemDto
                {
                    Id = x.Id,
                    Provider = x.Provider,
                    FlowKey = x.FlowKey,
                    BusinessId = x.BusinessId,
                    RecipientEmail = x.RecipientEmail,
                    Subject = x.Subject,
                    Status = x.Status,
                    AttemptedAtUtc = x.AttemptedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc,
                    FailureMessage = x.FailureMessage
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }
}
