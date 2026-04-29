using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Common;
using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    public sealed class GetProviderCallbackInboxPageHandler
    {
        private static readonly TimeSpan StalePendingThreshold = TimeSpan.FromMinutes(30);
        private static readonly HashSet<string> BrevoDeliveryFailureEvents = new(StringComparer.OrdinalIgnoreCase)
        {
            "hard_bounce",
            "soft_bounce",
            "spam",
            "blocked",
            "invalid",
            "error"
        };
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
                ProcessedCount = await baseQuery.CountAsync(x => x.Status == "Processed" || x.Status == "Succeeded", ct).ConfigureAwait(false),
                StalePendingCount = await baseQuery.CountAsync(x => x.Status == "Pending" && x.CreatedAtUtc <= staleBeforeUtc, ct).ConfigureAwait(false),
                RetriedCount = await baseQuery.CountAsync(x => x.AttemptCount > 0, ct).ConfigureAwait(false),
                BrevoTotalCount = await baseQuery.CountAsync(x => x.Provider == "Brevo", ct).ConfigureAwait(false),
                BrevoPendingCount = await baseQuery.CountAsync(x => x.Provider == "Brevo" && x.Status == "Pending", ct).ConfigureAwait(false),
                BrevoFailedCount = await baseQuery.CountAsync(x => x.Provider == "Brevo" && x.Status == "Failed", ct).ConfigureAwait(false),
                BrevoProcessedCount = await baseQuery.CountAsync(x => x.Provider == "Brevo" && (x.Status == "Processed" || x.Status == "Succeeded"), ct).ConfigureAwait(false),
                BrevoStalePendingCount = await baseQuery.CountAsync(x => x.Provider == "Brevo" && x.Status == "Pending" && x.CreatedAtUtc <= staleBeforeUtc, ct).ConfigureAwait(false),
                BrevoRecent24HourCount = await baseQuery.CountAsync(x => x.Provider == "Brevo" && x.CreatedAtUtc >= now.AddHours(-24), ct).ConfigureAwait(false)
            };
            summary.BrevoDeliveryFailureEventCount = await baseQuery
                .CountAsync(x => x.Provider == "Brevo" && BrevoDeliveryFailureEvents.Contains(x.CallbackType), ct)
                .ConfigureAwait(false);

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
                var q = QueryLikePattern.Contains(filter.Query);
                query = query.Where(x =>
                    EF.Functions.Like(x.Provider, q, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.CallbackType, q, QueryLikePattern.EscapeCharacter) ||
                    (x.IdempotencyKey != null && EF.Functions.Like(x.IdempotencyKey, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.FailureReason != null && EF.Functions.Like(x.FailureReason, q, QueryLikePattern.EscapeCharacter)) ||
                    EF.Functions.Like(x.PayloadJson, q, QueryLikePattern.EscapeCharacter));
            }

            if (!string.IsNullOrWhiteSpace(filter.Provider))
            {
                var normalizedProvider = filter.Provider.Trim();
                query = query.Where(x => x.Provider == normalizedProvider);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var normalizedStatus = filter.Status.Trim();
                query = string.Equals(normalizedStatus, "Processed", StringComparison.OrdinalIgnoreCase)
                    ? query.Where(x => x.Status == "Processed" || x.Status == "Succeeded")
                    : query.Where(x => x.Status == normalizedStatus);
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
                Status = NormalizeStatus(row.Status),
                IdempotencyKey = row.IdempotencyKey,
                AttemptCount = row.AttemptCount,
                LastAttemptAtUtc = row.LastAttemptAtUtc,
                ProcessedAtUtc = row.ProcessedAtUtc,
                CreatedAtUtc = row.CreatedAtUtc,
                AgeMinutes = Math.Max(0, (int)(now - row.CreatedAtUtc).TotalMinutes),
                IsStalePending = IsStalePending(row, staleBeforeUtc),
                FailureReason = row.FailureReason,
                PayloadPreview = BuildPayloadPreview(row)
            };
        }

        private static string BuildPayloadPreview(ProviderCallbackInboxMessage row)
        {
            if (string.IsNullOrWhiteSpace(row.PayloadJson))
            {
                return string.Empty;
            }

            try
            {
                using var document = JsonDocument.Parse(row.PayloadJson);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    return Summarize(row.PayloadJson, 220);
                }

                if (string.Equals(row.Provider, "Stripe", StringComparison.OrdinalIgnoreCase))
                {
                    return Summarize(BuildStripePayloadPreview(root), 220);
                }

                if (string.Equals(row.Provider, "DHL", StringComparison.OrdinalIgnoreCase))
                {
                    return Summarize(BuildDhlPayloadPreview(root), 220);
                }

                if (string.Equals(row.Provider, "Brevo", StringComparison.OrdinalIgnoreCase))
                {
                    return Summarize(BuildBrevoPayloadPreview(root), 220);
                }

                return Summarize(BuildGenericPayloadPreview(root), 220);
            }
            catch (JsonException)
            {
                return Summarize(row.PayloadJson, 220);
            }
        }

        private static string BuildStripePayloadPreview(JsonElement root)
        {
            var parts = new List<string>();
            AddPreviewPart(parts, "event", GetString(root, "id"));
            AddPreviewPart(parts, "type", GetString(root, "type"));
            AddPreviewPart(parts, "created", GetScalarText(root, "created"));

            if (TryGetObject(root, out var stripeObject, "data", "object"))
            {
                AddPreviewPart(parts, "object", GetString(stripeObject, "object"));
                AddPreviewPart(parts, "objectId", GetString(stripeObject, "id"));
                AddPreviewPart(parts, "paymentIntent", GetString(stripeObject, "payment_intent"));
                AddPreviewPart(parts, "checkoutSession", GetString(stripeObject, "checkout_session"));
                AddPreviewPart(parts, "status", GetString(stripeObject, "status"));
            }

            return parts.Count == 0 ? root.GetRawText() : string.Join("; ", parts);
        }

        private static string BuildDhlPayloadPreview(JsonElement root)
        {
            var parts = new List<string>();
            AddPreviewPart(parts, "shipmentRef", GetString(root, "providerShipmentReference"));
            AddPreviewPart(parts, "tracking", GetString(root, "trackingNumber"));
            AddPreviewPart(parts, "event", GetString(root, "carrierEventKey"));
            AddPreviewPart(parts, "status", GetString(root, "providerStatus"));
            AddPreviewPart(parts, "occurredAtUtc", GetString(root, "occurredAtUtc"));
            AddPreviewPart(parts, "exception", GetString(root, "exceptionCode"));
            return parts.Count == 0 ? root.GetRawText() : string.Join("; ", parts);
        }

        private static string BuildBrevoPayloadPreview(JsonElement root)
        {
            var parts = new List<string>();
            AddPreviewPart(parts, "event", GetString(root, "event"));
            AddPreviewPart(parts, "messageId", GetString(root, "message-id"));
            AddPreviewPart(parts, "email", GetString(root, "email"));
            AddPreviewPart(parts, "subject", GetString(root, "subject"));
            AddPreviewPart(parts, "reason", GetString(root, "reason"));
            AddPreviewPart(parts, "ts", GetScalarText(root, "ts_event") ?? GetScalarText(root, "ts"));
            return parts.Count == 0 ? root.GetRawText() : string.Join("; ", parts);
        }

        private static string BuildGenericPayloadPreview(JsonElement root)
        {
            var parts = new List<string>();
            foreach (var property in root.EnumerateObject())
            {
                if (parts.Count >= 8)
                {
                    break;
                }

                if (IsSensitiveKey(property.Name))
                {
                    AddPreviewPart(parts, property.Name, "[redacted]");
                    continue;
                }

                var value = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => null,
                    _ => null
                };

                AddPreviewPart(parts, property.Name, value);
            }

            return parts.Count == 0 ? root.GetRawText() : string.Join("; ", parts);
        }

        private static bool TryGetObject(JsonElement root, out JsonElement value, params string[] path)
        {
            value = root;
            foreach (var segment in path)
            {
                if (value.ValueKind != JsonValueKind.Object ||
                    !value.TryGetProperty(segment, out value))
                {
                    value = default;
                    return false;
                }
            }

            return value.ValueKind == JsonValueKind.Object;
        }

        private static string? GetString(JsonElement root, string propertyName)
        {
            return root.ValueKind == JsonValueKind.Object &&
                   root.TryGetProperty(propertyName, out var value) &&
                   value.ValueKind == JsonValueKind.String
                ? value.GetString()?.Trim()
                : null;
        }

        private static string? GetScalarText(JsonElement root, string propertyName)
        {
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty(propertyName, out var value))
            {
                return null;
            }

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString()?.Trim(),
                JsonValueKind.Number => value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
        }

        private static void AddPreviewPart(List<string> parts, string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                parts.Add($"{key}: {value.Trim()}");
            }
        }

        private static bool IsSensitiveKey(string key)
        {
            return key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                   key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                   key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                   key.Contains("signature", StringComparison.OrdinalIgnoreCase) ||
                   key.Contains("api_key", StringComparison.OrdinalIgnoreCase) ||
                   key.Contains("apikey", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStalePending(ProviderCallbackInboxMessage row, DateTime staleBeforeUtc)
        {
            return string.Equals(row.Status, "Pending", StringComparison.OrdinalIgnoreCase) &&
                   row.CreatedAtUtc <= staleBeforeUtc;
        }

        private static string NormalizeStatus(string status)
        {
            return string.Equals(status, "Succeeded", StringComparison.OrdinalIgnoreCase)
                ? "Processed"
                : status;
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
