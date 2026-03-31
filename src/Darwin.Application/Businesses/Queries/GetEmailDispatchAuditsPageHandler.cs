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
        private static readonly TimeSpan RetryCooldown = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan RetryChainWindow = TimeSpan.FromHours(24);
        private const int MaxRetryAttemptsPerWindow = 3;
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
            bool stalePendingOnly = false,
            bool businessLinkedFailuresOnly = false,
            bool repeatedFailuresOnly = false,
            bool priorSuccessOnly = false,
            bool retryReadyOnly = false,
            bool retryBlockedOnly = false,
            bool highChainVolumeOnly = false,
            Guid? businessId = null,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            var nowUtc = DateTime.UtcNow;
            var stalePendingThresholdUtc = nowUtc.AddMinutes(-15);
            const int slowDeliveryThresholdSeconds = 60;

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

            if (stalePendingOnly)
            {
                baseQuery = baseQuery.Where(x => x.Audit.Status == "Pending" && x.Audit.AttemptedAtUtc <= stalePendingThresholdUtc);
            }

            if (businessLinkedFailuresOnly)
            {
                baseQuery = baseQuery.Where(x => x.Audit.Status == "Failed" && x.Audit.BusinessId != null);
            }

            if (businessId.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.Audit.BusinessId == businessId.Value);
            }

            var baseItems = await baseQuery
                .OrderByDescending(x => x.Audit.AttemptedAtUtc)
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

            foreach (var item in baseItems)
            {
                var priorRows = await _db.Set<EmailDispatchAudit>()
                    .AsNoTracking()
                    .Where(x =>
                        x.Id != item.Id &&
                        x.RecipientEmail == item.RecipientEmail &&
                        x.FlowKey == item.FlowKey &&
                        x.BusinessId == item.BusinessId &&
                        x.AttemptedAtUtc < item.AttemptedAtUtc)
                    .OrderByDescending(x => x.AttemptedAtUtc)
                    .Select(x => new
                    {
                        x.Status,
                        x.AttemptedAtUtc
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                item.AttemptAgeMinutes = (int)Math.Max(0, (nowUtc - item.AttemptedAtUtc).TotalMinutes);
                item.CompletionLatencySeconds = item.CompletedAtUtc.HasValue
                    ? (int)Math.Max(0, (item.CompletedAtUtc.Value - item.AttemptedAtUtc).TotalSeconds)
                    : null;
                item.NeedsOperatorFollowUp =
                    string.Equals(item.Status, "Failed", StringComparison.OrdinalIgnoreCase) ||
                    (string.Equals(item.Status, "Pending", StringComparison.OrdinalIgnoreCase) && item.AttemptedAtUtc <= stalePendingThresholdUtc);
                var recentAttemptCount24h = priorRows.Count(x => x.AttemptedAtUtc >= nowUtc.Subtract(RetryChainWindow)) + 1;
                var chainRows = priorRows
                    .Where(x => x.AttemptedAtUtc >= nowUtc.Subtract(RetryChainWindow))
                    .ToList();
                item.RecentAttemptCount24h = recentAttemptCount24h;
                item.ChainStartedAtUtc = chainRows.Count > 0 ? chainRows.Min(x => x.AttemptedAtUtc) : item.AttemptedAtUtc;
                item.ChainLastAttemptAtUtc = item.AttemptedAtUtc;
                item.ChainSpanHours = item.ChainStartedAtUtc.HasValue
                    ? (int)Math.Max(0, Math.Ceiling((item.AttemptedAtUtc - item.ChainStartedAtUtc.Value).TotalHours))
                    : null;
                var hasSent = chainRows.Any(x => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase)) ||
                              string.Equals(item.Status, "Sent", StringComparison.OrdinalIgnoreCase);
                var hasFailed = chainRows.Any(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase)) ||
                                string.Equals(item.Status, "Failed", StringComparison.OrdinalIgnoreCase);
                var hasPending = chainRows.Any(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase)) ||
                                 string.Equals(item.Status, "Pending", StringComparison.OrdinalIgnoreCase);
                item.ChainStatusMix =
                    hasSent && hasFailed ? "Mixed success/failure" :
                    hasFailed && hasPending ? "Open failure chain" :
                    hasFailed ? "Failure-only chain" :
                    hasPending ? "Pending-only chain" :
                    hasSent ? "Success-only chain" :
                    "Single attempt";
                item.PriorAttemptCount = priorRows.Count;
                item.PriorFailureCount = priorRows.Count(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase));
                item.LastSuccessfulAttemptAtUtc = priorRows
                    .Where(x => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase))
                    .Select(x => (DateTime?)x.AttemptedAtUtc)
                    .FirstOrDefault();
                item.Severity =
                    string.Equals(item.Status, "Failed", StringComparison.OrdinalIgnoreCase) && item.BusinessId != null ? "High" :
                    string.Equals(item.Status, "Failed", StringComparison.OrdinalIgnoreCase) ? "Medium" :
                    string.Equals(item.Status, "Pending", StringComparison.OrdinalIgnoreCase) && item.AttemptedAtUtc <= stalePendingThresholdUtc ? "High" :
                    string.Equals(item.Status, "Pending", StringComparison.OrdinalIgnoreCase) ? "Watch" :
                    item.CompletionLatencySeconds.HasValue && item.CompletionLatencySeconds.Value > slowDeliveryThresholdSeconds ? "Slow" :
                    "Normal";

                ApplyRetryPolicy(item, nowUtc);
            }

            var filteredItems = baseItems.AsEnumerable();

            if (repeatedFailuresOnly)
            {
                filteredItems = filteredItems.Where(x => x.PriorFailureCount > 0 && x.NeedsOperatorFollowUp);
            }

            if (priorSuccessOnly)
            {
                filteredItems = filteredItems.Where(x => x.LastSuccessfulAttemptAtUtc.HasValue);
            }

            if (retryReadyOnly)
            {
                filteredItems = filteredItems.Where(x => x.CanRetryNow);
            }

            if (retryBlockedOnly)
            {
                filteredItems = filteredItems.Where(x =>
                    !x.CanRetryNow &&
                    (string.Equals(x.RetryPolicyState, "Cooldown", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(x.RetryPolicyState, "RateLimited", StringComparison.OrdinalIgnoreCase)));
            }

            if (highChainVolumeOnly)
            {
                filteredItems = filteredItems.Where(x => x.RecentAttemptCount24h >= 3);
            }

            var total = filteredItems.Count();
            var items = filteredItems
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

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
            var stalePendingThresholdUtc = DateTime.UtcNow.AddMinutes(-15);
            const int slowDeliveryThresholdSeconds = 60;

            var summaryRows = await audits
                .Select(x => new
                {
                    x.Status,
                    x.FlowKey,
                    x.BusinessId,
                    x.RecipientEmail,
                    x.AttemptedAtUtc,
                    x.CompletedAtUtc
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return new EmailDispatchAuditSummaryDto
            {
                TotalCount = summaryRows.Count,
                FailedCount = summaryRows.Count(x => x.Status == "Failed"),
                SentCount = summaryRows.Count(x => x.Status == "Sent"),
                PendingCount = summaryRows.Count(x => x.Status == "Pending"),
                StalePendingCount = summaryRows.Count(x => x.Status == "Pending" && x.AttemptedAtUtc <= stalePendingThresholdUtc),
                BusinessLinkedFailureCount = summaryRows.Count(x => x.Status == "Failed" && x.BusinessId != null),
                Recent24HourCount = summaryRows.Count(x => x.AttemptedAtUtc >= recentThresholdUtc),
                FailedInvitationCount = summaryRows.Count(x => x.Status == "Failed" && x.FlowKey == "BusinessInvitation"),
                FailedActivationCount = summaryRows.Count(x => x.Status == "Failed" && x.FlowKey == "AccountActivation"),
                FailedPasswordResetCount = summaryRows.Count(x => x.Status == "Failed" && x.FlowKey == "PasswordReset"),
                FailedAdminTestCount = summaryRows.Count(x => x.Status == "Failed" && x.FlowKey == "AdminCommunicationTest"),
                NeedsOperatorFollowUpCount = summaryRows.Count(
                    x => x.Status == "Failed" || (x.Status == "Pending" && x.AttemptedAtUtc <= stalePendingThresholdUtc)),
                SlowCompletedCount = summaryRows.Count(
                    x => x.CompletedAtUtc.HasValue && (x.CompletedAtUtc.Value - x.AttemptedAtUtc).TotalSeconds > slowDeliveryThresholdSeconds),
                RetriedFlowCount = summaryRows
                    .GroupBy(x => new { x.FlowKey, x.BusinessId })
                    .Count(g => g.Count() > 1 && !string.IsNullOrWhiteSpace(g.Key.FlowKey)),
                PriorSuccessContextCount = summaryRows
                    .GroupBy(x => new { x.FlowKey, x.BusinessId, x.Status })
                    .Where(g => string.Equals(g.Key.Status, "Sent", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(g.Key.FlowKey))
                    .Count(),
                RepeatedFailureCount = summaryRows
                    .Where(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                    .GroupBy(x => new { x.FlowKey, x.BusinessId })
                    .Count(g => g.Count() > 1 && !string.IsNullOrWhiteSpace(g.Key.FlowKey)),
                RetryReadyCount = summaryRows.Count(x =>
                    IsSupportedRetryFlow(x.FlowKey) &&
                    CanRetryStatus(x.Status) &&
                    x.AttemptedAtUtc <= DateTime.UtcNow.Subtract(RetryCooldown)),
                RetryBlockedCount = summaryRows.Count(x =>
                    IsSupportedRetryFlow(x.FlowKey) &&
                    CanRetryStatus(x.Status) &&
                    x.AttemptedAtUtc > DateTime.UtcNow.Subtract(RetryCooldown)),
                HighChainVolumeCount = summaryRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.FlowKey) && x.AttemptedAtUtc >= recentThresholdUtc)
                    .GroupBy(x => new { x.FlowKey, x.BusinessId, x.RecipientEmail })
                    .Count(g => g.Count() >= 3)
            };
        }

        private static void ApplyRetryPolicy(EmailDispatchAuditListItemDto item, DateTime nowUtc)
        {
            if (!IsSupportedRetryFlow(item.FlowKey))
            {
                item.CanRetryNow = false;
                item.RetryPolicyState = "Unsupported";
                item.RetryBlockedReason = "This flow still requires manual operator follow-up.";
                return;
            }

            if (!CanRetryStatus(item.Status))
            {
                item.CanRetryNow = false;
                item.RetryPolicyState = "Closed";
                item.RetryBlockedReason = "Only failed or pending delivery rows can be retried.";
                return;
            }

            if (item.RecentAttemptCount24h >= MaxRetryAttemptsPerWindow)
            {
                item.CanRetryNow = false;
                item.RetryPolicyState = "RateLimited";
                item.RetryBlockedReason = $"Retry limit reached: {item.RecentAttemptCount24h} attempts in the last 24 hours.";
                return;
            }

            var retryAvailableAtUtc = item.AttemptedAtUtc.Add(RetryCooldown);
            if (retryAvailableAtUtc > nowUtc)
            {
                item.CanRetryNow = false;
                item.RetryPolicyState = "Cooldown";
                item.RetryAvailableAtUtc = retryAvailableAtUtc;
                item.RetryBlockedReason = $"Retry cooldown active until {retryAvailableAtUtc:yyyy-MM-dd HH:mm} UTC.";
                return;
            }

            item.CanRetryNow = true;
            item.RetryPolicyState = "Ready";
        }

        private static bool IsSupportedRetryFlow(string? flowKey)
        {
            return string.Equals(flowKey, "BusinessInvitation", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(flowKey, "AccountActivation", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(flowKey, "PasswordReset", StringComparison.OrdinalIgnoreCase);
        }

        private static bool CanRetryStatus(string? status)
        {
            return string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase);
        }
    }
}
