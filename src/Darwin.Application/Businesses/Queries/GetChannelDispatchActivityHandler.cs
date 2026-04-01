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

            var query = _db.Set<ChannelDispatchAudit>().AsNoTracking();
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
                    Channel = x.Channel,
                    Provider = x.Provider,
                    FlowKey = x.FlowKey,
                    BusinessId = x.BusinessId,
                    RecipientAddress = x.RecipientAddress,
                    MessagePreview = x.MessagePreview,
                    Status = x.Status,
                    AttemptedAtUtc = x.AttemptedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc,
                    FailureMessage = x.FailureMessage
                })
                .ToList();

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

            var query = _db.Set<ChannelDispatchAudit>().AsNoTracking();
            if (filter.BusinessId.HasValue)
            {
                query = query.Where(x => x.BusinessId == filter.BusinessId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var q = filter.Query.Trim();
                query = query.Where(x =>
                    x.RecipientAddress.Contains(q) ||
                    x.Provider.Contains(q) ||
                    x.MessagePreview.Contains(q) ||
                    (x.FlowKey != null && x.FlowKey.Contains(q)));
            }

            if (!string.IsNullOrWhiteSpace(filter.RecipientAddress))
            {
                var normalizedRecipientAddress = filter.RecipientAddress.Trim();
                query = query.Where(x => x.RecipientAddress == normalizedRecipientAddress);
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
            var providerContext = BuildProviderContext(rows);
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
                filteredRows = filteredRows.Where(x => BuildActionPolicy(x, chainContext[x.Id]).CanRerunNow);
            }

            if (filter.ActionBlockedOnly)
            {
                filteredRows = filteredRows.Where(x => !BuildActionPolicy(x, chainContext[x.Id]).CanRerunNow);
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
            var total = rows.Count;
            var items = filteredRowsList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x =>
                {
                    var actionPolicy = BuildActionPolicy(x, chainContext[x.Id]);
                    var escalationPolicy = BuildEscalationPolicy(x, chainContext[x.Id]);
                    return new ChannelDispatchAuditListItemDto
                    {
                        Id = x.Id,
                        Channel = x.Channel,
                        Provider = x.Provider,
                        FlowKey = x.FlowKey,
                        BusinessId = x.BusinessId,
                        RecipientAddress = x.RecipientAddress,
                        MessagePreview = x.MessagePreview,
                        Status = x.Status,
                        AttemptedAtUtc = x.AttemptedAtUtc,
                        CompletedAtUtc = x.CompletedAtUtc,
                        FailureMessage = x.FailureMessage,
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

            var summary = BuildSummary(rows, chainContext, providerContext);
            ChannelDispatchAuditChainSummaryDto? chainSummary = null;
            ChannelDispatchProviderSummaryDto? providerSummary = null;
            if (!string.IsNullOrWhiteSpace(filter.RecipientAddress))
            {
                chainSummary = BuildChainSummary(rows);
            }

            if (!string.IsNullOrWhiteSpace(filter.Provider))
            {
                providerSummary = BuildProviderSummary(rows, filter.Provider, filter.Channel, filter.FlowKey);
            }

            return (items, filteredRowsList.Count, summary, chainSummary, providerSummary);
        }

        private static ChannelDispatchAuditSummaryDto BuildSummary(
            List<ChannelDispatchAudit> rows,
            Dictionary<Guid, ChannelDispatchAuditChainContext> chainContext,
            Dictionary<Guid, ChannelDispatchProviderContext> providerContext)
        {
            var recentThresholdUtc = DateTime.UtcNow.AddHours(-24);
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
                ActionReadyCount = rows.Count(x => BuildActionPolicy(x, chainContext[x.Id]).CanRerunNow),
                ActionBlockedCount = rows.Count(x => !BuildActionPolicy(x, chainContext[x.Id]).CanRerunNow),
                EscalationCandidateCount = rows.Count(x => BuildEscalationPolicy(x, chainContext[x.Id]).NeedsEscalationReview),
                HeavyChainCount = rows.Count(x => IsHeavyChain(chainContext[x.Id])),
                ProviderReviewCount = rows.Count(x => NeedsProviderReview(providerContext[x.Id])),
                ProviderRecoveredCount = rows.Count(x => string.Equals(providerContext[x.Id].RecoveryState, "Recovered", StringComparison.OrdinalIgnoreCase))
            };
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
                        MessagePreview = x.MessagePreview,
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
            string? flowKey)
        {
            var recentThresholdUtc = DateTime.UtcNow.AddHours(-24);
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
            if (string.Equals(flowKey, "PhoneVerification", StringComparison.OrdinalIgnoreCase))
            {
                var recommended = string.Equals(pressureState, "Elevated", StringComparison.OrdinalIgnoreCase)
                    ? "Review SMS/WhatsApp readiness, fallback policy, and current verification channel choice before issuing another canonical verification code."
                    : "Keep verification traffic on the canonical flow, then review recent provider failures before switching channel policy or escalating.";
                var escalation = failureCount >= 2 && sentCount == 0
                    ? "Escalate as provider or channel-policy instability if verification traffic continues to fail without any successful recovery in this provider lane."
                    : "Escalate only if the provider lane keeps degrading after readiness, fallback, and current phone verification path have been checked.";
                return (recommended, escalation);
            }

            if (string.Equals(flowKey, "AdminCommunicationTest", StringComparison.OrdinalIgnoreCase))
            {
                var recommended = string.Equals(pressureState, "Elevated", StringComparison.OrdinalIgnoreCase)
                    ? "Correct provider credentials, sender identity, or reserved test-target setup before rerunning more diagnostics on this lane."
                    : "Use the reserved test target for a controlled rerun only after checking provider config and template state.";
                var escalation = failureCount >= 2 && sentCount == 0
                    ? "Escalate as provider/configuration debt when this diagnostic lane keeps failing without a successful send."
                    : "Escalate only when repeated transport-test failures continue after configuration corrections.";
                return (recommended, escalation);
            }

            var genericRecommended = pendingCount > 0
                ? "Review the pending and failed traffic in this provider lane before taking another manual action."
                : "Review recent failures in this provider lane before escalating.";
            var genericEscalation = string.Equals(pressureState, "Elevated", StringComparison.OrdinalIgnoreCase)
                ? "Escalate this provider lane if failures keep accumulating without recovery."
                : "Escalate only if this provider lane continues degrading after basic transport checks.";
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
            if (string.Equals(flowKey, "PhoneVerification", StringComparison.OrdinalIgnoreCase))
            {
                var recommended = hasSent
                    ? "Do not replay historical verification messages. If the user is still blocked, confirm the current phone number and request a fresh code through the canonical verification flow."
                    : "Do not replay historical verification messages. Confirm the current phone number, review preferred-vs-fallback channel policy, then request a fresh code through the canonical verification flow.";

                var escalation = failedCount >= 3 && !hasSent
                    ? "Repeated verification failures without a successful send indicate a likely transport or channel-policy issue. Escalate after confirming SMS/WhatsApp readiness and fallback policy."
                    : "Escalate only if the canonical verification flow keeps failing after channel readiness and policy have been checked.";

                return (recommended, escalation);
            }

            if (string.Equals(flowKey, "AdminCommunicationTest", StringComparison.OrdinalIgnoreCase))
            {
                var recommended = "Rerun diagnostics only to the reserved channel test target after correcting provider settings, templates, or channel policy.";
                var escalation = failedCount >= 2 && !hasSent
                    ? "Repeated admin-test failures without any successful send suggest provider/configuration debt. Escalate as transport setup or provider issue instead of repeatedly rerunning tests."
                    : "If a rerun succeeds, treat this as an isolated incident. Escalate only when repeated failures continue after config fixes.";

                return (recommended, escalation);
            }

            var genericRecommended = hasPending
                ? "Review the latest pending or failed non-email attempts before taking manual action."
                : "Review recent non-email delivery history before escalating.";
            var genericEscalation = lastSuccessfulAttemptAtUtc.HasValue
                ? "Escalate only if the chain continues to fail after a previously successful path has been revalidated."
                : "Escalate when the same non-email path fails repeatedly without a verified successful send.";
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
                             x.RecipientAddress
                         }))
            {
                var ordered = group.OrderBy(x => x.AttemptedAtUtc).ToList();
                var hasSent = ordered.Any(x => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase));
                var hasFailed = ordered.Any(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase));
                var hasPending = ordered.Any(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase));
                var chainStatusMix =
                    hasSent && hasFailed ? "Mixed success/failure" :
                    hasFailed && hasPending ? "Open failure chain" :
                    hasFailed ? "Failure-only chain" :
                    hasPending ? "Pending-only chain" :
                    hasSent ? "Success-only chain" :
                    "Single attempt";
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
            ChannelDispatchAuditChainContext context)
        {
            if (string.Equals(row.FlowKey, "PhoneVerification", StringComparison.OrdinalIgnoreCase))
            {
                return new ChannelDispatchActionPolicy
                {
                    CanRerunNow = false,
                    State = "Canonical flow",
                    BlockedReason = "Do not replay historical verification messages. Request a fresh code through the canonical phone-verification flow.",
                    AvailableAtUtc = null
                };
            }

            if (string.Equals(row.FlowKey, "AdminCommunicationTest", StringComparison.OrdinalIgnoreCase))
            {
                var cooldownUntil = row.AttemptedAtUtc.AddMinutes(5);
                if (cooldownUntil > DateTime.UtcNow)
                {
                    return new ChannelDispatchActionPolicy
                    {
                        CanRerunNow = false,
                        State = "Cooldown",
                        BlockedReason = "Wait for the transport cooldown window before rerunning the same diagnostic channel test.",
                        AvailableAtUtc = cooldownUntil
                    };
                }

                return new ChannelDispatchActionPolicy
                {
                    CanRerunNow = true,
                    State = context.PriorFailureCount > 0 ? "Retry ready" : "Ready",
                    BlockedReason = null,
                    AvailableAtUtc = null
                };
            }

            return new ChannelDispatchActionPolicy
            {
                CanRerunNow = false,
                State = "Unsupported",
                BlockedReason = "No operator rerun path is defined for this non-email flow yet.",
                AvailableAtUtc = null
            };
        }

        private static ChannelDispatchEscalationPolicy BuildEscalationPolicy(
            ChannelDispatchAudit row,
            ChannelDispatchAuditChainContext context)
        {
            if (string.Equals(row.FlowKey, "PhoneVerification", StringComparison.OrdinalIgnoreCase))
            {
                if (context.PriorFailureCount >= 2 && !context.LastSuccessfulAttemptAtUtc.HasValue)
                {
                    return new ChannelDispatchEscalationPolicy
                    {
                        NeedsEscalationReview = true,
                        Reason = "Repeated verification failures without any successful send. Review transport readiness and fallback policy, then escalate if the canonical flow is still blocked."
                    };
                }
            }

            if (string.Equals(row.FlowKey, "AdminCommunicationTest", StringComparison.OrdinalIgnoreCase))
            {
                if (context.PriorFailureCount >= 1 && !context.LastSuccessfulAttemptAtUtc.HasValue)
                {
                    return new ChannelDispatchEscalationPolicy
                    {
                        NeedsEscalationReview = true,
                        Reason = "Repeated diagnostic transport failures without a successful send. Treat this as provider/config debt rather than another routine rerun."
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

        private static Dictionary<Guid, ChannelDispatchProviderContext> BuildProviderContext(List<ChannelDispatchAudit> rows)
        {
            var recentThresholdUtc = DateTime.UtcNow.AddHours(-24);
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
                    failureCount >= 2 && !hasRecentSuccess ? "Elevated" :
                    failureCount > 0 ? "Recovering" :
                    "Stable";
                var recoveryState =
                    failureCount > 0 && hasRecentSuccess ? "Recovered" :
                    hasRecentSuccess ? "Stable success" :
                    "No recovery yet";

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
