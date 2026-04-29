using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Common;
using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    public sealed class GetChannelDispatchActivityHandler
    {
        private readonly IAppDbContext _db;

        public GetChannelDispatchActivityHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<(List<ChannelDispatchAuditListItemDto> Items, ChannelDispatchAuditSummaryDto Summary)> HandleAsync(
            Guid? businessId = null,
            int take = 10,
            CancellationToken ct = default)
        {
            if (take < 1)
            {
                take = 10;
            }

            var query = _db.Set<ChannelDispatchAudit>().AsNoTracking().Where(x => !x.IsDeleted);
            if (businessId.HasValue)
            {
                query = query.Where(x => x.BusinessId == businessId.Value);
            }

            var rows = await query
                .OrderByDescending(x => x.AttemptedAtUtc)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var recentThresholdUtc = DateTime.UtcNow.AddHours(-24);
            var summary = new ChannelDispatchAuditSummaryDto
            {
                TotalCount = rows.Count,
                FailedCount = rows.Count(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase)),
                PendingCount = rows.Count(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
                Recent24HourCount = rows.Count(x => x.AttemptedAtUtc >= recentThresholdUtc),
                SmsCount = rows.Count(x => string.Equals(x.Channel, "SMS", StringComparison.OrdinalIgnoreCase)),
                WhatsAppCount = rows.Count(x => string.Equals(x.Channel, "WhatsApp", StringComparison.OrdinalIgnoreCase)),
                PhoneVerificationCount = rows.Count(x => string.Equals(x.FlowKey, "PhoneVerification", StringComparison.OrdinalIgnoreCase)),
                AdminTestCount = rows.Count(x => string.Equals(x.FlowKey, "AdminCommunicationTest", StringComparison.OrdinalIgnoreCase))
            };

            var items = rows
                .Take(take)
                .Select(x => new ChannelDispatchAuditListItemDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    IsQueueOperation = false,
                    Channel = x.Channel,
                    Provider = x.Provider,
                    FlowKey = x.FlowKey,
                    TemplateKey = x.TemplateKey,
                    CorrelationKey = x.CorrelationKey,
                    BusinessId = x.BusinessId,
                    RecipientAddress = x.RecipientAddress,
                    IntendedRecipientAddress = x.IntendedRecipientAddress,
                    MessagePreview = x.MessagePreview,
                    ProviderMessageId = x.ProviderMessageId,
                    Status = x.Status,
                    AttemptedAtUtc = x.AttemptedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc,
                    FailureMessage = x.FailureMessage,
                    QueueAttemptCount = 0
                })
                .ToList();

            var queuedItems = await BuildQueuedOperationItemsAsync(
                    new ChannelDispatchAuditFilterDto
                    {
                        BusinessId = businessId
                    },
                    rows,
                    ct)
                .ConfigureAwait(false);
            items = items
                .Concat(queuedItems)
                .OrderByDescending(x => x.AttemptedAtUtc)
                .Take(take)
                .ToList();
            summary.QueuedPendingCount = queuedItems.Count(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase));
            summary.QueuedFailedCount = queuedItems.Count(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase));

            return (items, summary);
        }

        public async Task<(List<ChannelDispatchAuditListItemDto> Items, int Total, ChannelDispatchAuditSummaryDto Summary, ChannelDispatchAuditChainSummaryDto? ChainSummary, ChannelDispatchProviderSummaryDto? ProviderSummary)> HandlePageAsync(
            int page,
            int pageSize,
            ChannelDispatchAuditFilterDto filter,
            CancellationToken ct = default)
        {
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 20;
            }

            var nowUtc = DateTime.UtcNow;
            var query = _db.Set<ChannelDispatchAudit>().AsNoTracking().Where(x => !x.IsDeleted);
            if (filter.BusinessId.HasValue)
            {
                query = query.Where(x => x.BusinessId == filter.BusinessId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var q = QueryLikePattern.Contains(filter.Query);
                query = query.Where(x =>
                    EF.Functions.Like(x.RecipientAddress, q, QueryLikePattern.EscapeCharacter) ||
                    (x.IntendedRecipientAddress != null && EF.Functions.Like(x.IntendedRecipientAddress, q, QueryLikePattern.EscapeCharacter)) ||
                    EF.Functions.Like(x.Provider, q, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.MessagePreview, q, QueryLikePattern.EscapeCharacter) ||
                    (x.FlowKey != null && EF.Functions.Like(x.FlowKey, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.TemplateKey != null && EF.Functions.Like(x.TemplateKey, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.CorrelationKey != null && EF.Functions.Like(x.CorrelationKey, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.ProviderMessageId != null && EF.Functions.Like(x.ProviderMessageId, q, QueryLikePattern.EscapeCharacter)));
            }

            if (!string.IsNullOrWhiteSpace(filter.RecipientAddress))
            {
                var normalizedRecipientAddress = filter.RecipientAddress.Trim();
                query = query.Where(x => (x.IntendedRecipientAddress ?? x.RecipientAddress) == normalizedRecipientAddress);
            }

            if (!string.IsNullOrWhiteSpace(filter.Channel))
            {
                var normalizedChannel = filter.Channel.Trim();
                query = query.Where(x => x.Channel == normalizedChannel);
            }

            if (!string.IsNullOrWhiteSpace(filter.Provider))
            {
                var normalizedProvider = filter.Provider.Trim();
                query = query.Where(x => x.Provider == normalizedProvider);
            }

            if (!string.IsNullOrWhiteSpace(filter.FlowKey))
            {
                var normalizedFlowKey = filter.FlowKey.Trim();
                query = query.Where(x => x.FlowKey == normalizedFlowKey);
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

            if (filter.PhoneVerificationOnly)
            {
                query = query.Where(x => x.FlowKey == "PhoneVerification");
            }

            if (filter.AdminTestOnly)
            {
                query = query.Where(x => x.FlowKey == "AdminCommunicationTest");
            }

            var rows = await query
                .OrderByDescending(x => x.AttemptedAtUtc)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var chainContext = BuildChainContext(rows);
            var filteredRows = rows.AsEnumerable();
            var providerContext = BuildProviderContext(rows, nowUtc);
            if (filter.RepeatedFailuresOnly)
            {
                filteredRows = filteredRows.Where(x => IsRepeatedFailure(chainContext[x.Id]));
            }

            if (filter.PriorSuccessOnly)
            {
                filteredRows = filteredRows.Where(x => chainContext[x.Id].LastSuccessfulAttemptAtUtc.HasValue);
            }

            if (filter.ActionReadyOnly)
            {
                filteredRows = filteredRows.Where(x => BuildActionPolicy(x, chainContext[x.Id], nowUtc).CanRerunNow);
            }

            if (filter.ActionBlockedOnly)
            {
                filteredRows = filteredRows.Where(x => !BuildActionPolicy(x, chainContext[x.Id], nowUtc).CanRerunNow);
            }

            if (filter.EscalationCandidatesOnly)
            {
                filteredRows = filteredRows.Where(x => BuildEscalationPolicy(x, chainContext[x.Id]).NeedsEscalationReview);
            }

            if (filter.HeavyChainsOnly)
            {
                filteredRows = filteredRows.Where(x => IsHeavyChain(chainContext[x.Id]));
            }

            if (filter.ProviderReviewOnly)
            {
                filteredRows = filteredRows.Where(x => NeedsProviderReview(providerContext[x.Id]));
            }

            if (filter.ChainFollowUpOnly)
            {
                filteredRows = filteredRows.Where(NeedsOperatorFollowUp);
            }

            if (filter.ChainResolvedOnly)
            {
                filteredRows = filteredRows.Where(x => !NeedsOperatorFollowUp(x));
            }

            var filteredRowsList = filteredRows.ToList();
            var auditItems = filteredRowsList
                .Select(x =>
                {
                    var actionPolicy = BuildActionPolicy(x, chainContext[x.Id], nowUtc);
                    var escalationPolicy = BuildEscalationPolicy(x, chainContext[x.Id]);
                    return new ChannelDispatchAuditListItemDto
                    {
                        Id = x.Id,
                        RowVersion = x.RowVersion,
                        IsQueueOperation = false,
                        Channel = x.Channel,
                        Provider = x.Provider,
                        FlowKey = x.FlowKey,
                        TemplateKey = x.TemplateKey,
                        CorrelationKey = x.CorrelationKey,
                        BusinessId = x.BusinessId,
                        RecipientAddress = x.RecipientAddress,
                        IntendedRecipientAddress = x.IntendedRecipientAddress,
                        MessagePreview = x.MessagePreview,
                        ProviderMessageId = x.ProviderMessageId,
                        Status = x.Status,
                        AttemptedAtUtc = x.AttemptedAtUtc,
                        CompletedAtUtc = x.CompletedAtUtc,
                        FailureMessage = x.FailureMessage,
                        QueueAttemptCount = 0,
                        NeedsOperatorFollowUp = NeedsOperatorFollowUp(x),
                        ChainAttemptCount = chainContext[x.Id].ChainAttemptCount,
                        ChainStatusMix = chainContext[x.Id].ChainStatusMix,
                        PriorAttemptCount = chainContext[x.Id].PriorAttemptCount,
                        PriorFailureCount = chainContext[x.Id].PriorFailureCount,
                        LastSuccessfulAttemptAtUtc = chainContext[x.Id].LastSuccessfulAttemptAtUtc,
                        CanRerunNow = actionPolicy.CanRerunNow,
                        ActionPolicyState = actionPolicy.State,
                        ActionBlockedReason = actionPolicy.BlockedReason,
                        ActionAvailableAtUtc = actionPolicy.AvailableAtUtc,
                        NeedsEscalationReview = escalationPolicy.NeedsEscalationReview,
                        EscalationReason = escalationPolicy.Reason,
                        ProviderRecentAttemptCount24h = providerContext[x.Id].RecentAttemptCount24h,
                        ProviderFailureCount24h = providerContext[x.Id].FailureCount24h,
                        ProviderPressureState = providerContext[x.Id].PressureState,
                        ProviderRecoveryState = providerContext[x.Id].RecoveryState,
                        ProviderLastSuccessfulAttemptAtUtc = providerContext[x.Id].LastSuccessfulAttemptAtUtc
                    };
                })
                .ToList();
            var queuedItems = await BuildQueuedOperationItemsAsync(filter, rows, ct).ConfigureAwait(false);
            var total = filteredRowsList.Count + queuedItems.Count;
            var items = auditItems
                .Concat(queuedItems)
                .OrderByDescending(x => x.AttemptedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var summary = BuildSummary(rows, chainContext, providerContext, nowUtc);
            summary.QueuedPendingCount = queuedItems.Count(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase));
            summary.QueuedFailedCount = queuedItems.Count(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase));
            ChannelDispatchAuditChainSummaryDto? chainSummary = null;
            ChannelDispatchProviderSummaryDto? providerSummary = null;
            if (!string.IsNullOrWhiteSpace(filter.RecipientAddress))
            {
                chainSummary = BuildChainSummary(rows);
            }

            if (!string.IsNullOrWhiteSpace(filter.Provider))
            {
                providerSummary = BuildProviderSummary(rows, filter.Provider, filter.Channel, filter.FlowKey, nowUtc);
            }

            return (items, total, summary, chainSummary, providerSummary);
        }

        private static ChannelDispatchAuditSummaryDto BuildSummary(
            List<ChannelDispatchAudit> rows,
            Dictionary<Guid, ChannelDispatchAuditChainContext> chainContext,
            Dictionary<Guid, ChannelDispatchProviderContext> providerContext,
            DateTime nowUtc)
        {
            var recentThresholdUtc = nowUtc.AddHours(-24);
            return new ChannelDispatchAuditSummaryDto
            {
                TotalCount = rows.Count,
                FailedCount = rows.Count(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase)),
                PendingCount = rows.Count(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
                Recent24HourCount = rows.Count(x => x.AttemptedAtUtc >= recentThresholdUtc),
                SmsCount = rows.Count(x => string.Equals(x.Channel, "SMS", StringComparison.OrdinalIgnoreCase)),
                WhatsAppCount = rows.Count(x => string.Equals(x.Channel, "WhatsApp", StringComparison.OrdinalIgnoreCase)),
                PhoneVerificationCount = rows.Count(x => string.Equals(x.FlowKey, "PhoneVerification", StringComparison.OrdinalIgnoreCase)),
                AdminTestCount = rows.Count(x => string.Equals(x.FlowKey, "AdminCommunicationTest", StringComparison.OrdinalIgnoreCase)),
                RepeatedFailureCount = rows.Count(x => IsRepeatedFailure(chainContext[x.Id])),
                PriorSuccessContextCount = rows.Count(x => chainContext[x.Id].LastSuccessfulAttemptAtUtc.HasValue),
                ActionReadyCount = rows.Count(x => BuildActionPolicy(x, chainContext[x.Id], nowUtc).CanRerunNow),
                ActionBlockedCount = rows.Count(x => !BuildActionPolicy(x, chainContext[x.Id], nowUtc).CanRerunNow),
                EscalationCandidateCount = rows.Count(x => BuildEscalationPolicy(x, chainContext[x.Id]).NeedsEscalationReview),
                HeavyChainCount = rows.Count(x => IsHeavyChain(chainContext[x.Id])),
                ProviderReviewCount = rows.Count(x => NeedsProviderReview(providerContext[x.Id])),
                ProviderRecoveredCount = rows.Count(x => string.Equals(providerContext[x.Id].RecoveryState, "Recovered", StringComparison.OrdinalIgnoreCase))
            };
        }

        private async Task<List<ChannelDispatchAuditListItemDto>> BuildQueuedOperationItemsAsync(
            ChannelDispatchAuditFilterDto filter,
            IReadOnlyCollection<ChannelDispatchAudit> auditRows,
            CancellationToken ct)
        {
            if (filter.ActionReadyOnly ||
                filter.ActionBlockedOnly ||
                filter.EscalationCandidatesOnly ||
                filter.HeavyChainsOnly ||
                filter.ProviderReviewOnly ||
                filter.ChainFollowUpOnly ||
                filter.ChainResolvedOnly)
            {
                return new List<ChannelDispatchAuditListItemDto>();
            }

            var query = _db.Set<ChannelDispatchOperation>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Where(x => x.Status == "Pending" || x.Status == "Failed");

            if (filter.BusinessId.HasValue)
            {
                query = query.Where(x => x.BusinessId == filter.BusinessId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var q = QueryLikePattern.Contains(filter.Query);
                query = query.Where(x =>
                    EF.Functions.Like(x.RecipientAddress, q, QueryLikePattern.EscapeCharacter) ||
                    (x.IntendedRecipientAddress != null && EF.Functions.Like(x.IntendedRecipientAddress, q, QueryLikePattern.EscapeCharacter)) ||
                    EF.Functions.Like(x.Provider, q, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.MessageText, q, QueryLikePattern.EscapeCharacter) ||
                    (x.FlowKey != null && EF.Functions.Like(x.FlowKey, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.TemplateKey != null && EF.Functions.Like(x.TemplateKey, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.CorrelationKey != null && EF.Functions.Like(x.CorrelationKey, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.FailureReason != null && EF.Functions.Like(x.FailureReason, q, QueryLikePattern.EscapeCharacter)));
            }

            if (!string.IsNullOrWhiteSpace(filter.RecipientAddress))
            {
                var normalizedRecipientAddress = filter.RecipientAddress.Trim();
                query = query.Where(x => (x.IntendedRecipientAddress ?? x.RecipientAddress) == normalizedRecipientAddress);
            }

            if (!string.IsNullOrWhiteSpace(filter.Channel))
            {
                var normalizedChannel = filter.Channel.Trim();
                query = query.Where(x => x.Channel == normalizedChannel);
            }

            if (!string.IsNullOrWhiteSpace(filter.Provider))
            {
                var normalizedProvider = filter.Provider.Trim();
                query = query.Where(x => x.Provider == normalizedProvider);
            }

            if (!string.IsNullOrWhiteSpace(filter.FlowKey))
            {
                var normalizedFlowKey = filter.FlowKey.Trim();
                query = query.Where(x => x.FlowKey == normalizedFlowKey);
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

            if (filter.PhoneVerificationOnly)
            {
                query = query.Where(x => x.FlowKey == "PhoneVerification");
            }

            if (filter.AdminTestOnly)
            {
                query = query.Where(x => x.FlowKey == "AdminCommunicationTest");
            }

            var auditCorrelationKeys = auditRows
                .Where(x => !string.IsNullOrWhiteSpace(x.CorrelationKey))
                .Select(x => x.CorrelationKey!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (auditCorrelationKeys.Count > 0)
            {
                query = query.Where(x => string.IsNullOrWhiteSpace(x.CorrelationKey) || !auditCorrelationKeys.Contains(x.CorrelationKey));
            }

            var rows = await query
                .OrderByDescending(x => x.LastAttemptAtUtc ?? x.CreatedAtUtc)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return rows.Select(x => new ChannelDispatchAuditListItemDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    IsQueueOperation = true,
                    Channel = x.Channel,
                    Provider = x.Provider,
                    FlowKey = x.FlowKey,
                    TemplateKey = x.TemplateKey,
                    CorrelationKey = x.CorrelationKey,
                    BusinessId = x.BusinessId,
                    RecipientAddress = x.RecipientAddress,
                    IntendedRecipientAddress = x.IntendedRecipientAddress,
                    MessagePreview = TruncateMessagePreview(x.MessageText),
                    ProviderMessageId = null,
                    Status = x.Status,
                    AttemptedAtUtc = x.LastAttemptAtUtc ?? x.CreatedAtUtc,
                    CompletedAtUtc = x.ProcessedAtUtc,
                    FailureMessage = x.FailureReason,
                    QueueAttemptCount = x.AttemptCount,
                    NeedsOperatorFollowUp = true,
                    ChainAttemptCount = 0,
                    ChainStatusMix = string.Empty,
                    PriorAttemptCount = 0,
                    PriorFailureCount = 0,
                    LastSuccessfulAttemptAtUtc = null,
                    CanRerunNow = false,
                    ActionPolicyState = string.Empty,
                    ActionBlockedReason = null,
                    ActionAvailableAtUtc = null,
                    NeedsEscalationReview = false,
                    EscalationReason = null,
                    ProviderRecentAttemptCount24h = 0,
                    ProviderFailureCount24h = 0,
                    ProviderPressureState = string.Empty,
                    ProviderRecoveryState = string.Empty,
                    ProviderLastSuccessfulAttemptAtUtc = null
                })
                .ToList();
        }

        private static string TruncateMessagePreview(string? messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText))
            {
                return string.Empty;
            }

            var normalized = messageText.Trim();
            return normalized.Length <= 180 ? normalized : normalized[..180];
        }

        private static bool NeedsOperatorFollowUp(ChannelDispatchAudit row)
        {
            return string.Equals(row.Status, "Failed", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(row.Status, "Pending", StringComparison.OrdinalIgnoreCase);
        }

        private static ChannelDispatchAuditChainSummaryDto BuildChainSummary(List<ChannelDispatchAudit> rows)
        {
            var chainRows = rows
                .OrderBy(x => x.AttemptedAtUtc)
                .ToList();
            var flowKey = chainRows.FirstOrDefault()?.FlowKey;

            var hasSent = chainRows.Any(x => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase));
            var hasFailed = chainRows.Any(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase));
            var hasPending = chainRows.Any(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase));
            var lastSuccessfulAttemptAtUtc = chainRows
                .Where(x => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.AttemptedAtUtc)
                .Select(x => (DateTime?)x.AttemptedAtUtc)
                .FirstOrDefault();

            var statusMix =
                hasSent && hasFailed ? "Mixed success/failure" :
                hasFailed && hasPending ? "Open failure chain" :
                hasFailed ? "Failure-only chain" :
                hasPending ? "Pending-only chain" :
                hasSent ? "Success-only chain" :
                "Single attempt";

            var (recommendedAction, escalationHint) = BuildChainGuidance(
                flowKey,
                chainRows.Count,
                chainRows.Count(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase)),
                hasSent,
                hasPending,
                lastSuccessfulAttemptAtUtc);

            return new ChannelDispatchAuditChainSummaryDto
            {
                TotalAttempts = chainRows.Count,
                FailedCount = chainRows.Count(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase)),
                SentCount = chainRows.Count(x => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase)),
                PendingCount = chainRows.Count(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
                NeedsOperatorFollowUpCount = chainRows.Count(NeedsOperatorFollowUp),
                FirstAttemptAtUtc = chainRows.FirstOrDefault()?.AttemptedAtUtc,
                LastAttemptAtUtc = chainRows.LastOrDefault()?.AttemptedAtUtc,
                LastSuccessfulAttemptAtUtc = lastSuccessfulAttemptAtUtc,
                StatusMix = statusMix,
                RecommendedAction = recommendedAction,
                EscalationHint = escalationHint,
                RecentHistory = chainRows
                    .OrderByDescending(x => x.AttemptedAtUtc)
                    .Take(8)
                    .Select(x => new ChannelDispatchAuditChainHistoryItemDto
                    {
                        AttemptedAtUtc = x.AttemptedAtUtc,
                        Channel = x.Channel,
                        Status = x.Status,
                        Provider = x.Provider,
                        TemplateKey = x.TemplateKey,
                        CorrelationKey = x.CorrelationKey,
                        MessagePreview = x.MessagePreview,
                        IntendedRecipientAddress = x.IntendedRecipientAddress,
                        ProviderMessageId = x.ProviderMessageId,
                        FailureMessage = x.FailureMessage,
                        CompletedAtUtc = x.CompletedAtUtc
                    })
                    .ToList()
            };
        }

        private static ChannelDispatchProviderSummaryDto BuildProviderSummary(
            List<ChannelDispatchAudit> rows,
            string provider,
            string? channel,
            string? flowKey,
            DateTime nowUtc)
        {
            var recentThresholdUtc = nowUtc.AddHours(-24);
            var recentRows = rows
                .Where(x => x.AttemptedAtUtc >= recentThresholdUtc)
                .ToList();

            var failureCount = recentRows.Count(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase));
            var sentCount = recentRows.Count(x => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase));
            var pendingCount = recentRows.Count(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase));
            var lastSuccessfulAttemptAtUtc = recentRows
                .Where(x => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.AttemptedAtUtc)
                .Select(x => (DateTime?)x.AttemptedAtUtc)
                .FirstOrDefault();
            var pressureState =
                failureCount >= 2 && sentCount == 0
                    ? "Elevated"
                    : failureCount > 0
                        ? "Recovering"
                        : "Stable";
            var recoveryState =
                failureCount > 0 && sentCount > 0
                    ? "Recovered"
                    : sentCount > 0
                        ? "Stable success"
                        : "No recovery yet";
            var (recommendedAction, escalationHint) = BuildProviderGuidance(flowKey, pressureState, failureCount, sentCount, pendingCount);

            return new ChannelDispatchProviderSummaryDto
            {
                Provider = provider,
                Channel = channel ?? string.Empty,
                FlowKey = flowKey ?? string.Empty,
                RecentAttemptCount24h = recentRows.Count,
                FailureCount24h = failureCount,
                SentCount24h = sentCount,
                PendingCount24h = pendingCount,
                PressureState = pressureState,
                RecoveryState = recoveryState,
                LastSuccessfulAttemptAtUtc = lastSuccessfulAttemptAtUtc,
                RecommendedAction = recommendedAction,
                EscalationHint = escalationHint
            };
        }

        private static (string RecommendedAction, string EscalationHint) BuildProviderGuidance(
            string? flowKey,
            string pressureState,
            int failureCount,
            int sentCount,
            int pendingCount)
        {
            if (string.Equals(flowKey, ChannelDispatchAuditVocabulary.FlowKeys.PhoneVerification, StringComparison.OrdinalIgnoreCase))
            {
                var recommended = string.Equals(pressureState, ChannelDispatchAuditVocabulary.PressureStates.Elevated, StringComparison.OrdinalIgnoreCase)
                    ? ChannelDispatchAuditVocabulary.Guidance.ProviderRecommendedVerificationElevated
                    : ChannelDispatchAuditVocabulary.Guidance.ProviderRecommendedVerificationStable;
                var escalation = failureCount >= 2 && sentCount == 0
                    ? ChannelDispatchAuditVocabulary.Guidance.ProviderEscalationVerificationElevated
                    : ChannelDispatchAuditVocabulary.Guidance.ProviderEscalationVerificationStable;
                return (recommended, escalation);
            }

            if (string.Equals(flowKey, ChannelDispatchAuditVocabulary.FlowKeys.AdminCommunicationTest, StringComparison.OrdinalIgnoreCase))
            {
                var recommended = string.Equals(pressureState, ChannelDispatchAuditVocabulary.PressureStates.Elevated, StringComparison.OrdinalIgnoreCase)
                    ? ChannelDispatchAuditVocabulary.Guidance.ProviderRecommendedAdminTestElevated
                    : ChannelDispatchAuditVocabulary.Guidance.ProviderRecommendedAdminTestStable;
                var escalation = failureCount >= 2 && sentCount == 0
                    ? ChannelDispatchAuditVocabulary.Guidance.ProviderEscalationAdminTestElevated
                    : ChannelDispatchAuditVocabulary.Guidance.ProviderEscalationAdminTestStable;
                return (recommended, escalation);
            }

            var genericRecommended = pendingCount > 0
                ? ChannelDispatchAuditVocabulary.Guidance.ProviderRecommendedGenericPending
                : ChannelDispatchAuditVocabulary.Guidance.ProviderRecommendedGenericStable;
            var genericEscalation = string.Equals(pressureState, ChannelDispatchAuditVocabulary.PressureStates.Elevated, StringComparison.OrdinalIgnoreCase)
                ? ChannelDispatchAuditVocabulary.Guidance.ProviderEscalationGenericElevated
                : ChannelDispatchAuditVocabulary.Guidance.ProviderEscalationGenericStable;
            return (genericRecommended, genericEscalation);
        }

        private static (string RecommendedAction, string EscalationHint) BuildChainGuidance(
            string? flowKey,
            int totalAttempts,
            int failedCount,
            bool hasSent,
            bool hasPending,
            DateTime? lastSuccessfulAttemptAtUtc)
        {
            if (string.Equals(flowKey, ChannelDispatchAuditVocabulary.FlowKeys.PhoneVerification, StringComparison.OrdinalIgnoreCase))
            {
                var recommended = hasSent
                    ? ChannelDispatchAuditVocabulary.Guidance.ChainRecommendedVerificationRecovered
                    : ChannelDispatchAuditVocabulary.Guidance.ChainRecommendedVerificationBlocked;

                var escalation = failedCount >= 3 && !hasSent
                    ? ChannelDispatchAuditVocabulary.Guidance.ChainEscalationVerificationBlocked
                    : ChannelDispatchAuditVocabulary.Guidance.ChainEscalationVerificationStable;

                return (recommended, escalation);
            }

            if (string.Equals(flowKey, ChannelDispatchAuditVocabulary.FlowKeys.AdminCommunicationTest, StringComparison.OrdinalIgnoreCase))
            {
                var recommended = ChannelDispatchAuditVocabulary.Guidance.ChainRecommendedAdminTest;
                var escalation = failedCount >= 2 && !hasSent
                    ? ChannelDispatchAuditVocabulary.Guidance.ChainEscalationAdminTestBlocked
                    : ChannelDispatchAuditVocabulary.Guidance.ChainEscalationAdminTestStable;

                return (recommended, escalation);
            }

            var genericRecommended = hasPending
                ? ChannelDispatchAuditVocabulary.Guidance.ChainRecommendedGenericPending
                : ChannelDispatchAuditVocabulary.Guidance.ChainRecommendedGenericStable;
            var genericEscalation = lastSuccessfulAttemptAtUtc.HasValue
                ? ChannelDispatchAuditVocabulary.Guidance.ChainEscalationGenericRecovered
                : ChannelDispatchAuditVocabulary.Guidance.ChainEscalationGenericBlocked;
            return (genericRecommended, genericEscalation);
        }

        private static Dictionary<Guid, ChannelDispatchAuditChainContext> BuildChainContext(List<ChannelDispatchAudit> rows)
        {
            var lookup = new Dictionary<Guid, ChannelDispatchAuditChainContext>(rows.Count);
            foreach (var group in rows
                         .GroupBy(x => new
                         {
                             x.BusinessId,
                             FlowKey = x.FlowKey ?? string.Empty,
                             RecipientAddress = x.IntendedRecipientAddress ?? x.RecipientAddress
                         }))
            {
                var ordered = group.OrderBy(x => x.AttemptedAtUtc).ToList();
                var hasSent = ordered.Any(x => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase));
                var hasFailed = ordered.Any(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase));
                var hasPending = ordered.Any(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase));
                var chainStatusMix =
                    hasSent && hasFailed ? ChannelDispatchAuditVocabulary.ChainStatusMixes.Mixed :
                    hasFailed && hasPending ? ChannelDispatchAuditVocabulary.ChainStatusMixes.OpenFailure :
                    hasFailed ? ChannelDispatchAuditVocabulary.ChainStatusMixes.FailureOnly :
                    hasPending ? ChannelDispatchAuditVocabulary.ChainStatusMixes.PendingOnly :
                    hasSent ? ChannelDispatchAuditVocabulary.ChainStatusMixes.SuccessOnly :
                    ChannelDispatchAuditVocabulary.ChainStatusMixes.SingleAttempt;
                var priorFailures = 0;
                var priorAttempts = 0;
                DateTime? lastSuccessfulAttemptAtUtc = null;
                foreach (var row in ordered)
                {
                    lookup[row.Id] = new ChannelDispatchAuditChainContext
                    {
                        PriorAttemptCount = priorAttempts,
                        PriorFailureCount = priorFailures,
                        LastSuccessfulAttemptAtUtc = lastSuccessfulAttemptAtUtc,
                        ChainAttemptCount = ordered.Count,
                        ChainStatusMix = chainStatusMix
                    };

                    priorAttempts++;
                    if (string.Equals(row.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                    {
                        priorFailures++;
                    }

                    if (string.Equals(row.Status, "Sent", StringComparison.OrdinalIgnoreCase))
                    {
                        lastSuccessfulAttemptAtUtc = row.AttemptedAtUtc;
                    }
                }
            }

            return lookup;
        }

        private static bool IsRepeatedFailure(ChannelDispatchAuditChainContext context)
        {
            return context.PriorFailureCount > 0;
        }

        private static bool IsHeavyChain(ChannelDispatchAuditChainContext context)
        {
            return context.ChainAttemptCount >= 3;
        }

        private static bool NeedsProviderReview(ChannelDispatchProviderContext context)
        {
            return context.FailureCount24h >= 2 ||
                   string.Equals(context.PressureState, "Elevated", StringComparison.OrdinalIgnoreCase);
        }

        private static ChannelDispatchActionPolicy BuildActionPolicy(
            ChannelDispatchAudit row,
            ChannelDispatchAuditChainContext context,
            DateTime nowUtc)
        {
            if (string.Equals(row.FlowKey, ChannelDispatchAuditVocabulary.FlowKeys.PhoneVerification, StringComparison.OrdinalIgnoreCase))
            {
                return new ChannelDispatchActionPolicy
                {
                    CanRerunNow = false,
                    State = ChannelDispatchAuditVocabulary.ActionPolicyStates.CanonicalFlow,
                    BlockedReason = ChannelDispatchAuditVocabulary.Guidance.ActionBlockedCanonicalFlow,
                    AvailableAtUtc = null
                };
            }

            if (string.Equals(row.FlowKey, ChannelDispatchAuditVocabulary.FlowKeys.AdminCommunicationTest, StringComparison.OrdinalIgnoreCase))
            {
                var cooldownUntil = row.AttemptedAtUtc.AddMinutes(5);
                if (cooldownUntil > nowUtc)
                {
                    return new ChannelDispatchActionPolicy
                    {
                        CanRerunNow = false,
                        State = ChannelDispatchAuditVocabulary.ActionPolicyStates.Cooldown,
                        BlockedReason = ChannelDispatchAuditVocabulary.Guidance.ActionBlockedCooldown,
                        AvailableAtUtc = cooldownUntil
                    };
                }

                return new ChannelDispatchActionPolicy
                {
                    CanRerunNow = true,
                    State = context.PriorFailureCount > 0 ? ChannelDispatchAuditVocabulary.ActionPolicyStates.RetryReady : ChannelDispatchAuditVocabulary.ActionPolicyStates.Ready,
                    BlockedReason = null,
                    AvailableAtUtc = null
                };
            }

            return new ChannelDispatchActionPolicy
            {
                CanRerunNow = false,
                State = ChannelDispatchAuditVocabulary.ActionPolicyStates.Unsupported,
                BlockedReason = ChannelDispatchAuditVocabulary.Guidance.ActionBlockedUnsupported,
                AvailableAtUtc = null
            };
        }

        private static ChannelDispatchEscalationPolicy BuildEscalationPolicy(
            ChannelDispatchAudit row,
            ChannelDispatchAuditChainContext context)
        {
            if (string.Equals(row.FlowKey, ChannelDispatchAuditVocabulary.FlowKeys.PhoneVerification, StringComparison.OrdinalIgnoreCase))
            {
                if (context.PriorFailureCount >= 2 && !context.LastSuccessfulAttemptAtUtc.HasValue)
                {
                    return new ChannelDispatchEscalationPolicy
                    {
                        NeedsEscalationReview = true,
                        Reason = ChannelDispatchAuditVocabulary.Guidance.EscalationReasonPhoneVerification
                    };
                }
            }

            if (string.Equals(row.FlowKey, ChannelDispatchAuditVocabulary.FlowKeys.AdminCommunicationTest, StringComparison.OrdinalIgnoreCase))
            {
                if (context.PriorFailureCount >= 1 && !context.LastSuccessfulAttemptAtUtc.HasValue)
                {
                    return new ChannelDispatchEscalationPolicy
                    {
                        NeedsEscalationReview = true,
                        Reason = ChannelDispatchAuditVocabulary.Guidance.EscalationReasonAdminTest
                    };
                }
            }

            return new ChannelDispatchEscalationPolicy
            {
                NeedsEscalationReview = false,
                Reason = null
            };
        }

        private sealed class ChannelDispatchAuditChainContext
        {
            public int PriorAttemptCount { get; init; }
            public int PriorFailureCount { get; init; }
            public DateTime? LastSuccessfulAttemptAtUtc { get; init; }
            public int ChainAttemptCount { get; init; }
            public string ChainStatusMix { get; init; } = string.Empty;
        }

        private sealed class ChannelDispatchActionPolicy
        {
            public bool CanRerunNow { get; init; }
            public string State { get; init; } = string.Empty;
            public string? BlockedReason { get; init; }
            public DateTime? AvailableAtUtc { get; init; }
        }

        private sealed class ChannelDispatchEscalationPolicy
        {
            public bool NeedsEscalationReview { get; init; }
            public string? Reason { get; init; }
        }

        private static Dictionary<Guid, ChannelDispatchProviderContext> BuildProviderContext(List<ChannelDispatchAudit> rows, DateTime nowUtc)
        {
            var recentThresholdUtc = nowUtc.AddHours(-24);
            var lookup = new Dictionary<Guid, ChannelDispatchProviderContext>(rows.Count);
            foreach (var group in rows
                         .GroupBy(x => new
                         {
                             Provider = x.Provider ?? string.Empty,
                             Channel = x.Channel ?? string.Empty,
                             FlowKey = x.FlowKey ?? string.Empty
                         }))
            {
                var recentRows = group
                    .Where(x => x.AttemptedAtUtc >= recentThresholdUtc)
                    .ToList();
                var recentAttemptCount = recentRows.Count;
                var failureCount = recentRows.Count(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase));
                var hasRecentSuccess = recentRows.Any(x => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase));
                var lastSuccessfulAttemptAtUtc = recentRows
                    .Where(x => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.AttemptedAtUtc)
                    .Select(x => (DateTime?)x.AttemptedAtUtc)
                    .FirstOrDefault();
                var pressureState =
                    failureCount >= 2 && !hasRecentSuccess ? ChannelDispatchAuditVocabulary.PressureStates.Elevated :
                    failureCount > 0 ? ChannelDispatchAuditVocabulary.PressureStates.Recovering :
                    ChannelDispatchAuditVocabulary.PressureStates.Stable;
                var recoveryState =
                    failureCount > 0 && hasRecentSuccess ? ChannelDispatchAuditVocabulary.RecoveryStates.Recovered :
                    hasRecentSuccess ? ChannelDispatchAuditVocabulary.RecoveryStates.StableSuccess :
                    ChannelDispatchAuditVocabulary.RecoveryStates.NoneYet;

                foreach (var row in group)
                {
                    lookup[row.Id] = new ChannelDispatchProviderContext
                    {
                        RecentAttemptCount24h = recentAttemptCount,
                        FailureCount24h = failureCount,
                        PressureState = pressureState,
                        RecoveryState = recoveryState,
                        LastSuccessfulAttemptAtUtc = lastSuccessfulAttemptAtUtc
                    };
                }
            }

            return lookup;
        }

        private sealed class ChannelDispatchProviderContext
        {
            public int RecentAttemptCount24h { get; init; }
            public int FailureCount24h { get; init; }
            public string PressureState { get; init; } = string.Empty;
            public string RecoveryState { get; init; } = string.Empty;
            public DateTime? LastSuccessfulAttemptAtUtc { get; init; }
        }
    }
}
