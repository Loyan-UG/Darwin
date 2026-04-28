using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    public sealed class GetProviderCallbackInboxPageHandler
    {
        private static readonly TimeSpan StalePendingThreshold = TimeSpan.FromMinutes(30);
        private readonly IAppDbContext _db;

        public GetProviderCallbackInboxPageHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<(List<ProviderCallbackInboxListItemDto> Items, int Total, ProviderCallbackInboxSummaryDto Summary, List<string> Providers)> HandleAsync(
            int page,
            int pageSize,
            ProviderCallbackInboxFilterDto filter,
            CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 10, 100);
            filter ??= new ProviderCallbackInboxFilterDto();

            var now = DateTime.UtcNow;
            var staleBeforeUtc = now.Subtract(StalePendingThreshold);
            var baseQuery = _db.Set<ProviderCallbackInboxMessage>().AsNoTracking().Where(x => !x.IsDeleted);

            var summary = new ProviderCallbackInboxSummaryDto
            {
                TotalCount = await baseQuery.CountAsync(ct).ConfigureAwait(false),
                PendingCount = await baseQuery.CountAsync(x => x.Status == "Pending", ct).ConfigureAwait(false),
                FailedCount = await baseQuery.CountAsync(x => x.Status == "Failed", ct).ConfigureAwait(false),
                ProcessedCount = await baseQuery.CountAsync(x => x.Status == "Processed", ct).ConfigureAwait(false),
                StalePendingCount = await baseQuery.CountAsync(x => x.Status == "Pending" && x.CreatedAtUtc <= staleBeforeUtc, ct).ConfigureAwait(false),
                RetriedCount = await baseQuery.CountAsync(x => x.AttemptCount > 0, ct).ConfigureAwait(false)
            };

            var providers = await baseQuery
                .Select(x => x.Provider)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var query = baseQuery;

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var q = filter.Query.Trim();
                query = query.Where(x =>
                    x.Provider.Contains(q) ||
                    x.CallbackType.Contains(q) ||
                    (x.IdempotencyKey != null && x.IdempotencyKey.Contains(q)) ||
                    (x.FailureReason != null && x.FailureReason.Contains(q)) ||
                    x.PayloadJson.Contains(q));
            }

            if (!string.IsNullOrWhiteSpace(filter.Provider))
            {
                var normalizedProvider = filter.Provider.Trim();
                query = query.Where(x => x.Provider == normalizedProvider);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var normalizedStatus = filter.Status.Trim();
                query = query.Where(x => x.Status == normalizedStatus);
            }

            if (filter.FailedOnly)
            {
                query = query.Where(x => x.Status == "Failed");
            }

            if (filter.StalePendingOnly)
            {
                query = query.Where(x => x.Status == "Pending" && x.CreatedAtUtc <= staleBeforeUtc);
            }

            var total = await query.CountAsync(ct).ConfigureAwait(false);
            var filteredRows = await query
                .OrderByDescending(x => x.Status == "Pending")
                .ThenByDescending(x => x.Status == "Failed")
                .ThenBy(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var items = filteredRows
                .Select(x => Map(x, now, staleBeforeUtc))
                .ToList();

            return (items, total, summary, providers);
        }

        private static ProviderCallbackInboxListItemDto Map(
            ProviderCallbackInboxMessage row,
            DateTime now,
            DateTime staleBeforeUtc)
        {
            return new ProviderCallbackInboxListItemDto
            {
                Id = row.Id,
                RowVersion = row.RowVersion,
                Provider = row.Provider,
                CallbackType = row.CallbackType,
                Status = row.Status,
                IdempotencyKey = row.IdempotencyKey,
                AttemptCount = row.AttemptCount,
                LastAttemptAtUtc = row.LastAttemptAtUtc,
                ProcessedAtUtc = row.ProcessedAtUtc,
                CreatedAtUtc = row.CreatedAtUtc,
                AgeMinutes = Math.Max(0, (int)(now - row.CreatedAtUtc).TotalMinutes),
                IsStalePending = IsStalePending(row, staleBeforeUtc),
                FailureReason = row.FailureReason,
                PayloadPreview = Summarize(row.PayloadJson, 220)
            };
        }

        private static bool IsStalePending(ProviderCallbackInboxMessage row, DateTime staleBeforeUtc)
        {
            return string.Equals(row.Status, "Pending", StringComparison.OrdinalIgnoreCase) &&
                   row.CreatedAtUtc <= staleBeforeUtc;
        }

        private static string Summarize(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : string.Concat(trimmed.AsSpan(0, maxLength - 3), "...");
        }
    }
}
