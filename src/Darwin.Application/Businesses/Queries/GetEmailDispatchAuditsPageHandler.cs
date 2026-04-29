using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Common;
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
        private const int MaxPageSize = 200;
        private readonly IAppDbContext _db;

        public GetEmailDispatchAuditsPageHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<(List<EmailDispatchAuditListItemDto> Items, int Total, EmailDispatchAuditChainSummaryDto? ChainSummary)> HandleAsync(
            int page,
            int pageSize,
            string? query = null,
            string? recipientEmail = null,
            string? status = null,
            string? flowKey = null,
            bool stalePendingOnly = false,
            bool businessLinkedFailuresOnly = false,
            bool repeatedFailuresOnly = false,
            bool priorSuccessOnly = false,
            bool retryReadyOnly = false,
            bool retryBlockedOnly = false,
            bool highChainVolumeOnly = false,
            bool chainFollowUpOnly = false,
            bool chainResolvedOnly = false,
            Guid? businessId = null,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;
            var nowUtc = DateTime.UtcNow;
            var stalePendingThresholdUtc = nowUtc.AddMinutes(-15);
            const int slowDeliveryThresholdSeconds = 60;

            var baseQuery =
                from audit in _db.Set<EmailDispatchAudit>().AsNoTracking()
                join business in _db.Set<Business>().AsNoTracking() on audit.BusinessId equals business.Id into businessJoin
                from business in businessJoin.DefaultIfEmpty()
                where !audit.IsDeleted && (business == null || !business.IsDeleted)
                select new
                {
                    Audit = audit,
                    BusinessName = business == null ? null : business.Name
                };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = QueryLikePattern.Contains(query);
                baseQuery = baseQuery.Where(x =>
                    EF.Functions.Like(x.Audit.RecipientEmail, q, QueryLikePattern.EscapeCharacter) ||
                    (x.Audit.IntendedRecipientEmail != null && EF.Functions.Like(x.Audit.IntendedRecipientEmail, q, QueryLikePattern.EscapeCharacter)) ||
                    EF.Functions.Like(x.Audit.Subject, q, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.Audit.Status, q, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.Audit.Provider, q, QueryLikePattern.EscapeCharacter) ||
                    (x.Audit.FlowKey != null && EF.Functions.Like(x.Audit.FlowKey, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.Audit.TemplateKey != null && EF.Functions.Like(x.Audit.TemplateKey, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.Audit.CorrelationKey != null && EF.Functions.Like(x.Audit.CorrelationKey, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.Audit.ProviderMessageId != null && EF.Functions.Like(x.Audit.ProviderMessageId, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.BusinessName != null && EF.Functions.Like(x.BusinessName, q, QueryLikePattern.EscapeCharacter)));
            }

            if (!string.IsNullOrWhiteSpace(recipientEmail))
            {
                var normalizedRecipientEmail = recipientEmail.Trim();
                baseQuery = baseQuery.Where(x => (x.Audit.IntendedRecipientEmail ?? x.Audit.RecipientEmail) == normalizedRecipientEmail);
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
                    RowVersion = x.Audit.RowVersion,
                    IsQueueOperation = false,
                    Provider = x.Audit.Provider,
                    FlowKey = x.Audit.FlowKey,
                    TemplateKey = x.Audit.TemplateKey,
                    CorrelationKey = x.Audit.CorrelationKey,
                    BusinessId = x.Audit.BusinessId,
                    BusinessName = x.BusinessName,
                    RecipientEmail = x.Audit.RecipientEmail,
                    IntendedRecipientEmail = x.Audit.IntendedRecipientEmail,
                    Subject = x.Audit.Subject,
                    ProviderMessageId = x.Audit.ProviderMessageId,
                    Status = x.Audit.Status,
                    AttemptedAtUtc = x.Audit.AttemptedAtUtc,
                    CompletedAtUtc = x.Audit.CompletedAtUtc,
                    FailureMessage = x.Audit.FailureMessage,
                    QueueAttemptCount = 0
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var item in baseItems)
            {
                var chainRecipientEmail = string.IsNullOrWhiteSpace(item.IntendedRecipientEmail)
                    ? item.RecipientEmail
                    : item.IntendedRecipientEmail;
                var priorRows = await _db.Set<EmailDispatchAudit>()
                    .AsNoTracking()
                    .Where(x =>
                        x.Id != item.Id &&
                        !x.IsDeleted &&
                        (x.IntendedRecipientEmail ?? x.RecipientEmail) == chainRecipientEmail &&
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

            await AttachBrevoCallbackStateAsync(baseItems, ct).ConfigureAwait(false);

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

            if (chainFollowUpOnly)
            {
                filteredItems = filteredItems.Where(x => x.NeedsOperatorFollowUp);
            }

            if (chainResolvedOnly)
            {
                filteredItems = filteredItems.Where(x => !x.NeedsOperatorFollowUp);
            }

            var filteredList = filteredItems.ToList();
            var queuedItems = await BuildQueuedOperationItemsAsync(
                    query,
                    recipientEmail,
                    status,
                    flowKey,
                    stalePendingOnly,
                    businessLinkedFailuresOnly,
                    repeatedFailuresOnly,
                    priorSuccessOnly,
                    retryReadyOnly,
                    retryBlockedOnly,
                    highChainVolumeOnly,
                    chainResolvedOnly,
                    businessId,
                    stalePendingThresholdUtc,
                    nowUtc,
                    ct)
                .ConfigureAwait(false);

            EmailDispatchAuditChainSummaryDto? chainSummary = null;
            if (!string.IsNullOrWhiteSpace(recipientEmail))
            {
                chainSummary = await BuildChainSummaryAsync(recipientEmail.Trim(), flowKey, businessId, stalePendingThresholdUtc, ct)
                    .ConfigureAwait(false);
            }

            var total = filteredList.Count + queuedItems.Count;
            var items = filteredList
                .Concat(queuedItems)
                .OrderByDescending(x => x.AttemptedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (items, total, chainSummary);
        }

        public async Task<EmailDispatchAuditSummaryDto> GetSummaryAsync(Guid? businessId = null, CancellationToken ct = default)
        {
            var audits = _db.Set<EmailDispatchAudit>().AsNoTracking().Where(x => !x.IsDeleted);
            var queued = _db.Set<EmailDispatchOperation>().AsNoTracking().Where(x => !x.IsDeleted);
            if (businessId.HasValue)
            {
                audits = audits.Where(x => x.BusinessId == businessId.Value);
                queued = queued.Where(x => x.BusinessId == businessId.Value);
            }

            var nowUtc = DateTime.UtcNow;
            var recentThresholdUtc = nowUtc.AddHours(-24);
            var stalePendingThresholdUtc = nowUtc.AddMinutes(-15);
            var retryCooldownThresholdUtc = nowUtc.Subtract(RetryCooldown);
            const int slowDeliveryThresholdSeconds = 60;

            var summaryRows = await audits
                .Select(x => new
                {
                    x.Status,
                    x.FlowKey,
                    x.BusinessId,
                    x.RecipientEmail,
                    x.IntendedRecipientEmail,
                    x.AttemptedAtUtc,
                    x.CompletedAtUtc
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);
            var queuedRows = await queued
                .Where(x => x.Status == "Pending" || x.Status == "Failed")
                .Select(x => new
                {
                    x.Status
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
                    .GroupBy(x => new { x.FlowKey, x.BusinessId, RecipientEmail = x.IntendedRecipientEmail ?? x.RecipientEmail })
                    .Count(g => g.Count() > 1 && !string.IsNullOrWhiteSpace(g.Key.FlowKey)),
                PriorSuccessContextCount = summaryRows
                    .GroupBy(x => new { x.FlowKey, x.BusinessId, RecipientEmail = x.IntendedRecipientEmail ?? x.RecipientEmail, x.Status })
                    .Where(g => string.Equals(g.Key.Status, "Sent", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(g.Key.FlowKey))
                    .Count(),
                RepeatedFailureCount = summaryRows
                    .Where(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                    .GroupBy(x => new { x.FlowKey, x.BusinessId, RecipientEmail = x.IntendedRecipientEmail ?? x.RecipientEmail })
                    .Count(g => g.Count() > 1 && !string.IsNullOrWhiteSpace(g.Key.FlowKey)),
                RetryReadyCount = summaryRows.Count(x =>
                    IsSupportedRetryFlow(x.FlowKey) &&
                    CanRetryStatus(x.Status) &&
                    x.AttemptedAtUtc <= retryCooldownThresholdUtc),
                RetryBlockedCount = summaryRows.Count(x =>
                    IsSupportedRetryFlow(x.FlowKey) &&
                    CanRetryStatus(x.Status) &&
                    x.AttemptedAtUtc > retryCooldownThresholdUtc),
                HighChainVolumeCount = summaryRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.FlowKey) && x.AttemptedAtUtc >= recentThresholdUtc)
                    .GroupBy(x => new { x.FlowKey, x.BusinessId, RecipientEmail = x.IntendedRecipientEmail ?? x.RecipientEmail })
                    .Count(g => g.Count() >= 3),
                QueuedPendingCount = queuedRows.Count(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
                QueuedFailedCount = queuedRows.Count(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase))
            };
        }

        private async Task<List<EmailDispatchAuditListItemDto>> BuildQueuedOperationItemsAsync(
            string? query,
            string? recipientEmail,
            string? status,
            string? flowKey,
            bool stalePendingOnly,
            bool businessLinkedFailuresOnly,
            bool repeatedFailuresOnly,
            bool priorSuccessOnly,
            bool retryReadyOnly,
            bool retryBlockedOnly,
            bool highChainVolumeOnly,
            bool chainResolvedOnly,
            Guid? businessId,
            DateTime stalePendingThresholdUtc,
            DateTime nowUtc,
            CancellationToken ct)
        {
            if (repeatedFailuresOnly ||
                priorSuccessOnly ||
                retryReadyOnly ||
                retryBlockedOnly ||
                highChainVolumeOnly ||
                chainResolvedOnly)
            {
                return new List<EmailDispatchAuditListItemDto>();
            }

            var queuedQuery =
                from operation in _db.Set<EmailDispatchOperation>().AsNoTracking()
                join business in _db.Set<Business>().AsNoTracking() on operation.BusinessId equals business.Id into businessJoin
                from business in businessJoin.DefaultIfEmpty()
                where !operation.IsDeleted &&
                      (business == null || !business.IsDeleted) &&
                      (operation.Status == "Pending" || operation.Status == "Failed")
                select new
                {
                    Operation = operation,
                    BusinessName = business == null ? null : business.Name
                };

            if (businessId.HasValue)
            {
                queuedQuery = queuedQuery.Where(x => x.Operation.BusinessId == businessId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = QueryLikePattern.Contains(query);
                queuedQuery = queuedQuery.Where(x =>
                    EF.Functions.Like(x.Operation.RecipientEmail, q, QueryLikePattern.EscapeCharacter) ||
                    (x.Operation.IntendedRecipientEmail != null && EF.Functions.Like(x.Operation.IntendedRecipientEmail, q, QueryLikePattern.EscapeCharacter)) ||
                    EF.Functions.Like(x.Operation.Subject, q, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.Operation.Provider, q, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.Operation.Status, q, QueryLikePattern.EscapeCharacter) ||
                    (x.Operation.FlowKey != null && EF.Functions.Like(x.Operation.FlowKey, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.Operation.TemplateKey != null && EF.Functions.Like(x.Operation.TemplateKey, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.Operation.CorrelationKey != null && EF.Functions.Like(x.Operation.CorrelationKey, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.BusinessName != null && EF.Functions.Like(x.BusinessName, q, QueryLikePattern.EscapeCharacter)));
            }

            if (!string.IsNullOrWhiteSpace(recipientEmail))
            {
                var normalizedRecipientEmail = recipientEmail.Trim();
                queuedQuery = queuedQuery.Where(x => (x.Operation.IntendedRecipientEmail ?? x.Operation.RecipientEmail) == normalizedRecipientEmail);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = status.Trim();
                queuedQuery = queuedQuery.Where(x => x.Operation.Status == normalizedStatus);
            }

            if (!string.IsNullOrWhiteSpace(flowKey))
            {
                var normalizedFlowKey = flowKey.Trim();
                queuedQuery = queuedQuery.Where(x => x.Operation.FlowKey == normalizedFlowKey);
            }

            if (stalePendingOnly)
            {
                queuedQuery = queuedQuery.Where(x => x.Operation.Status == "Pending" && x.Operation.CreatedAtUtc <= stalePendingThresholdUtc);
            }

            if (businessLinkedFailuresOnly)
            {
                queuedQuery = queuedQuery.Where(x => x.Operation.Status == "Failed" && x.Operation.BusinessId != null);
            }

            var rows = await queuedQuery
                .OrderByDescending(x => x.Operation.LastAttemptAtUtc ?? x.Operation.CreatedAtUtc)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return rows.Select(x => new EmailDispatchAuditListItemDto
                {
                    Id = x.Operation.Id,
                    RowVersion = x.Operation.RowVersion,
                    IsQueueOperation = true,
                    Provider = x.Operation.Provider,
                    FlowKey = x.Operation.FlowKey,
                    TemplateKey = x.Operation.TemplateKey,
                    CorrelationKey = x.Operation.CorrelationKey,
                    BusinessId = x.Operation.BusinessId,
                    BusinessName = x.BusinessName,
                    RecipientEmail = x.Operation.RecipientEmail,
                    IntendedRecipientEmail = x.Operation.IntendedRecipientEmail,
                    Subject = x.Operation.Subject,
                    ProviderMessageId = null,
                    Status = x.Operation.Status,
                    AttemptedAtUtc = x.Operation.LastAttemptAtUtc ?? x.Operation.CreatedAtUtc,
                    CompletedAtUtc = x.Operation.ProcessedAtUtc,
                    FailureMessage = x.Operation.FailureReason,
                    QueueAttemptCount = x.Operation.AttemptCount,
                    AttemptAgeMinutes = (int)Math.Max(0, (nowUtc - (x.Operation.LastAttemptAtUtc ?? x.Operation.CreatedAtUtc)).TotalMinutes),
                    CompletionLatencySeconds = x.Operation.ProcessedAtUtc.HasValue
                        ? (int)Math.Max(0, (x.Operation.ProcessedAtUtc.Value - x.Operation.CreatedAtUtc).TotalSeconds)
                        : null,
                    NeedsOperatorFollowUp = true,
                    Severity = string.Equals(x.Operation.Status, "Failed", StringComparison.OrdinalIgnoreCase) ? "High" : "Watch",
                    CanRetryNow = false,
                    RetryPolicyState = "Queued",
                    RetryBlockedReason = null,
                    RetryAvailableAtUtc = null,
                    RecentAttemptCount24h = 0,
                    ChainStartedAtUtc = x.Operation.CreatedAtUtc,
                    ChainLastAttemptAtUtc = x.Operation.LastAttemptAtUtc ?? x.Operation.CreatedAtUtc,
                    ChainSpanHours = 0,
                    ChainStatusMix = string.Equals(x.Operation.Status, "Failed", StringComparison.OrdinalIgnoreCase)
                        ? "Failure-only chain"
                        : "Pending-only chain",
                    PriorAttemptCount = 0,
                    PriorFailureCount = 0,
                    LastSuccessfulAttemptAtUtc = null
                })
                .ToList();
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

        private async Task AttachBrevoCallbackStateAsync(List<EmailDispatchAuditListItemDto> items, CancellationToken ct)
        {
            var messageIds = items
                .Where(x =>
                    !x.IsQueueOperation &&
                    string.Equals(x.Provider, "Brevo", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(x.ProviderMessageId))
                .Select(x => x.ProviderMessageId!.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (messageIds.Count == 0)
            {
                return;
            }

            var predicate = BuildBrevoCallbackPredicate(messageIds);
            var callbackRows = await _db.Set<ProviderCallbackInboxMessage>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Provider == "Brevo")
                .Where(predicate)
                .Select(x => new BrevoCallbackInboxProjection
                {
                    CallbackType = x.CallbackType,
                    Status = x.Status,
                    FailureReason = x.FailureReason,
                    PayloadJson = x.PayloadJson,
                    CreatedAtUtc = x.CreatedAtUtc,
                    ProcessedAtUtc = x.ProcessedAtUtc
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var latestByMessageId = callbackRows
                .Select(TryMapBrevoCallbackState)
                .Where(x => x is not null)
                .Select(x => x!)
                .Where(x => messageIds.Contains(x.MessageId, StringComparer.Ordinal))
                .GroupBy(x => x.MessageId, StringComparer.Ordinal)
                .ToDictionary(
                    x => x.Key,
                    x => x
                        .OrderByDescending(y => y.OccurredAtUtc ?? y.ProcessedAtUtc ?? y.CreatedAtUtc)
                        .First(),
                    StringComparer.Ordinal);

            foreach (var item in items)
            {
                if (item.ProviderMessageId is null ||
                    !latestByMessageId.TryGetValue(item.ProviderMessageId.Trim(), out var callback))
                {
                    continue;
                }

                item.LatestProviderCallbackEvent = callback.EventName;
                item.LatestProviderCallbackStatus = callback.Status;
                item.LatestProviderCallbackReason = callback.Reason;
                item.LatestProviderCallbackOccurredAtUtc = callback.OccurredAtUtc ?? callback.ProcessedAtUtc ?? callback.CreatedAtUtc;
            }
        }

        private static Expression<Func<ProviderCallbackInboxMessage, bool>> BuildBrevoCallbackPredicate(IReadOnlyList<string> messageIds)
        {
            var parameter = Expression.Parameter(typeof(ProviderCallbackInboxMessage), "x");
            Expression body = Expression.Constant(false);
            var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })
                ?? throw new InvalidOperationException("String.Contains(string) method was not found.");
            var idempotencyKey = Expression.Property(parameter, nameof(ProviderCallbackInboxMessage.IdempotencyKey));
            var payloadJson = Expression.Property(parameter, nameof(ProviderCallbackInboxMessage.PayloadJson));

            foreach (var messageId in messageIds)
            {
                var value = Expression.Constant(messageId);
                var idempotencyContains = Expression.AndAlso(
                    Expression.NotEqual(idempotencyKey, Expression.Constant(null, typeof(string))),
                    Expression.Call(idempotencyKey, containsMethod, value));
                var payloadContains = Expression.Call(payloadJson, containsMethod, value);
                body = Expression.OrElse(body, Expression.OrElse(idempotencyContains, payloadContains));
            }

            return Expression.Lambda<Func<ProviderCallbackInboxMessage, bool>>(body, parameter);
        }

        private static BrevoCallbackState? TryMapBrevoCallbackState(BrevoCallbackInboxProjection row)
        {
            string? messageId = null;
            string? reason = null;
            DateTime? occurredAtUtc = null;

            try
            {
                using var document = JsonDocument.Parse(row.PayloadJson);
                var root = document.RootElement;
                messageId = ReadString(root, "message-id");
                reason = ReadString(root, "reason");
                occurredAtUtc = ReadUnixSeconds(root, "ts_event") ??
                                ReadUnixSeconds(root, "ts") ??
                                ReadUnixMilliseconds(root, "ts_epoch");
            }
            catch (JsonException)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(messageId))
            {
                return null;
            }

            return new BrevoCallbackState
            {
                MessageId = messageId.Trim(),
                EventName = row.CallbackType.Trim(),
                Status = row.Status.Trim(),
                Reason = string.IsNullOrWhiteSpace(reason) ? row.FailureReason : reason.Trim(),
                CreatedAtUtc = row.CreatedAtUtc,
                ProcessedAtUtc = row.ProcessedAtUtc,
                OccurredAtUtc = occurredAtUtc
            };
        }

        private static string? ReadString(JsonElement root, string propertyName)
        {
            return root.ValueKind == JsonValueKind.Object &&
                   root.TryGetProperty(propertyName, out var value) &&
                   value.ValueKind == JsonValueKind.String
                ? value.GetString()?.Trim()
                : null;
        }

        private static DateTime? ReadUnixSeconds(JsonElement root, string propertyName)
        {
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty(propertyName, out var value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var seconds))
            {
                return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
            }

            if (value.ValueKind == JsonValueKind.String &&
                long.TryParse(value.GetString(), out seconds))
            {
                return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
            }

            return null;
        }

        private static DateTime? ReadUnixMilliseconds(JsonElement root, string propertyName)
        {
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty(propertyName, out var value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var milliseconds))
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
            }

            if (value.ValueKind == JsonValueKind.String &&
                long.TryParse(value.GetString(), out milliseconds))
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
            }

            return null;
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

        private sealed class BrevoCallbackState
        {
            public string MessageId { get; set; } = string.Empty;
            public string EventName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string? Reason { get; set; }
            public DateTime CreatedAtUtc { get; set; }
            public DateTime? ProcessedAtUtc { get; set; }
            public DateTime? OccurredAtUtc { get; set; }
        }

        private sealed class BrevoCallbackInboxProjection
        {
            public string CallbackType { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string? FailureReason { get; set; }
            public string PayloadJson { get; set; } = string.Empty;
            public DateTime CreatedAtUtc { get; set; }
            public DateTime? ProcessedAtUtc { get; set; }
        }

        private async Task<EmailDispatchAuditChainSummaryDto?> BuildChainSummaryAsync(
            string recipientEmail,
            string? flowKey,
            Guid? businessId,
            DateTime stalePendingThresholdUtc,
            CancellationToken ct)
        {
            var chainQuery = _db.Set<EmailDispatchAudit>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && (x.IntendedRecipientEmail ?? x.RecipientEmail) == recipientEmail);

            if (!string.IsNullOrWhiteSpace(flowKey))
            {
                var normalizedFlowKey = flowKey.Trim();
                chainQuery = chainQuery.Where(x => x.FlowKey == normalizedFlowKey);
            }

            if (businessId.HasValue)
            {
                chainQuery = chainQuery.Where(x => x.BusinessId == businessId.Value);
            }

            var chainRows = await chainQuery
                .OrderBy(x => x.AttemptedAtUtc)
                .Select(x => new
                {
                    x.Status,
                    x.AttemptedAtUtc,
                    x.Provider,
                    x.TemplateKey,
                    x.CorrelationKey,
                    x.Subject,
                    x.IntendedRecipientEmail,
                    x.ProviderMessageId,
                    x.FailureMessage,
                    x.CompletedAtUtc
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (chainRows.Count == 0)
            {
                return null;
            }

            var hasSent = chainRows.Any(x => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase));
            var hasFailed = chainRows.Any(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase));
            var hasPending = chainRows.Any(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase));

            var statusMix =
                hasSent && hasFailed ? "Mixed success/failure" :
                hasFailed && hasPending ? "Open failure chain" :
                hasFailed ? "Failure-only chain" :
                hasPending ? "Pending-only chain" :
                hasSent ? "Success-only chain" :
                "Single attempt";

            return new EmailDispatchAuditChainSummaryDto
            {
                TotalAttempts = chainRows.Count,
                FailedCount = chainRows.Count(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase)),
                SentCount = chainRows.Count(x => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase)),
                PendingCount = chainRows.Count(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
                NeedsOperatorFollowUpCount = chainRows.Count(x =>
                    string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase) ||
                    (string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase) && x.AttemptedAtUtc <= stalePendingThresholdUtc)),
                FirstAttemptAtUtc = chainRows.First().AttemptedAtUtc,
                LastAttemptAtUtc = chainRows.Last().AttemptedAtUtc,
                LastSuccessfulAttemptAtUtc = chainRows
                    .Where(x => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase))
                    .Select(x => (DateTime?)x.AttemptedAtUtc)
                    .LastOrDefault(),
                StatusMix = statusMix,
                RecentHistory = chainRows
                    .OrderByDescending(x => x.AttemptedAtUtc)
                    .Take(8)
                    .Select(x => new EmailDispatchAuditChainHistoryItemDto
                    {
                        AttemptedAtUtc = x.AttemptedAtUtc,
                        Status = x.Status,
                        Provider = x.Provider,
                        TemplateKey = x.TemplateKey,
                        CorrelationKey = x.CorrelationKey,
                        Subject = x.Subject,
                        IntendedRecipientEmail = x.IntendedRecipientEmail,
                        ProviderMessageId = x.ProviderMessageId,
                        FailureMessage = x.FailureMessage,
                        CompletedAtUtc = x.CompletedAtUtc
                    })
                    .ToList()
            };
        }
    }
}
