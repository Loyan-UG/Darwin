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

        public async Task<(List<ChannelDispatchAuditListItemDto> Items, int Total, ChannelDispatchAuditSummaryDto Summary, ChannelDispatchAuditChainSummaryDto? ChainSummary)> HandlePageAsync(
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
                        PriorAttemptCount = chainContext[x.Id].PriorAttemptCount,
                        PriorFailureCount = chainContext[x.Id].PriorFailureCount,
                        LastSuccessfulAttemptAtUtc = chainContext[x.Id].LastSuccessfulAttemptAtUtc,
                        CanRerunNow = actionPolicy.CanRerunNow,
                        ActionPolicyState = actionPolicy.State,
                        ActionBlockedReason = actionPolicy.BlockedReason,
                        ActionAvailableAtUtc = actionPolicy.AvailableAtUtc
                    };
                })
                .ToList();

            var summary = BuildSummary(rows, chainContext);
            ChannelDispatchAuditChainSummaryDto? chainSummary = null;
            if (!string.IsNullOrWhiteSpace(filter.RecipientAddress))
            {
                chainSummary = BuildChainSummary(rows);
            }

            return (items, filteredRowsList.Count, summary, chainSummary);
        }

        private static ChannelDispatchAuditSummaryDto BuildSummary(
            List<ChannelDispatchAudit> rows,
            Dictionary<Guid, ChannelDispatchAuditChainContext> chainContext)
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
                ActionBlockedCount = rows.Count(x => !BuildActionPolicy(x, chainContext[x.Id]).CanRerunNow)
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

            return new ChannelDispatchAuditChainSummaryDto
            {
                TotalAttempts = chainRows.Count,
                FailedCount = chainRows.Count(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase)),
                SentCount = chainRows.Count(x => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase)),
                PendingCount = chainRows.Count(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
                NeedsOperatorFollowUpCount = chainRows.Count(NeedsOperatorFollowUp),
                FirstAttemptAtUtc = chainRows.FirstOrDefault()?.AttemptedAtUtc,
                LastAttemptAtUtc = chainRows.LastOrDefault()?.AttemptedAtUtc,
                StatusMix = statusMix,
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
                var priorFailures = 0;
                var priorAttempts = 0;
                DateTime? lastSuccessfulAttemptAtUtc = null;
                foreach (var row in ordered)
                {
                    lookup[row.Id] = new ChannelDispatchAuditChainContext
                    {
                        PriorAttemptCount = priorAttempts,
                        PriorFailureCount = priorFailures,
                        LastSuccessfulAttemptAtUtc = lastSuccessfulAttemptAtUtc
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

        private sealed class ChannelDispatchAuditChainContext
        {
            public int PriorAttemptCount { get; init; }
            public int PriorFailureCount { get; init; }
            public DateTime? LastSuccessfulAttemptAtUtc { get; init; }
        }

        private sealed class ChannelDispatchActionPolicy
        {
            public bool CanRerunNow { get; init; }
            public string State { get; init; } = string.Empty;
            public string? BlockedReason { get; init; }
            public DateTime? AvailableAtUtc { get; init; }
        }
    }
}
