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

            var filteredRows = rows.AsEnumerable();
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
                    FailureMessage = x.FailureMessage,
                    NeedsOperatorFollowUp = NeedsOperatorFollowUp(x)
                })
                .ToList();

            var summary = BuildSummary(rows);
            ChannelDispatchAuditChainSummaryDto? chainSummary = null;
            if (!string.IsNullOrWhiteSpace(filter.RecipientAddress))
            {
                chainSummary = BuildChainSummary(rows);
            }

            return (items, filteredRowsList.Count, summary, chainSummary);
        }

        private static ChannelDispatchAuditSummaryDto BuildSummary(List<ChannelDispatchAudit> rows)
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
                AdminTestCount = rows.Count(x => string.Equals(x.FlowKey, "AdminCommunicationTest", StringComparison.OrdinalIgnoreCase))
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
    }
}
