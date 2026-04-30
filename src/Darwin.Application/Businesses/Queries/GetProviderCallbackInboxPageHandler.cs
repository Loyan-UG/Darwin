using System.Text.Json;
using System.Linq.Expressions;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Common;
using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    public sealed class GetProviderCallbackInboxPageHandler
    {
        private static readonly TimeSpan StalePendingThreshold = TimeSpan.FromMinutes(30);
        private static readonly Expression<Func<ProviderCallbackInboxMessage, bool>> BrevoDeliveryFailurePredicate =
            x => x.Provider == "Brevo" &&
                 (x.CallbackType == "hard_bounce" ||
                  x.CallbackType == "soft_bounce" ||
                  x.CallbackType == "spam" ||
                  x.CallbackType == "blocked" ||
                  x.CallbackType == "invalid" ||
                  x.CallbackType == "error");
        private readonly IAppDbContext _db;
        private readonly IClock _clock;

        public GetProviderCallbackInboxPageHandler(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
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

            var now = _clock.UtcNow;
            var staleBeforeUtc = now.Subtract(StalePendingThreshold);
            var baseQuery = _db.Set<ProviderCallbackInboxMessage>().AsNoTracking().Where(x => !x.IsDeleted);

            var recent24HoursUtc = now.AddHours(-24);
            var summary = await baseQuery
                .GroupBy(_ => 1)
                .Select(group => new ProviderCallbackInboxSummaryDto
                {
                    TotalCount = group.Count(),
                    PendingCount = group.Count(x => x.Status == "Pending"),
                    FailedCount = group.Count(x => x.Status == "Failed"),
                    ProcessedCount = group.Count(x => x.Status == "Processed" || x.Status == "Succeeded"),
                    StalePendingCount = group.Count(x => x.Status == "Pending" && x.CreatedAtUtc <= staleBeforeUtc),
                    RetriedCount = group.Count(x => x.AttemptCount > 0),
                    BrevoTotalCount = group.Count(x => x.Provider == "Brevo"),
                    BrevoPendingCount = group.Count(x => x.Provider == "Brevo" && x.Status == "Pending"),
                    BrevoFailedCount = group.Count(x => x.Provider == "Brevo" && x.Status == "Failed"),
                    BrevoProcessedCount = group.Count(x => x.Provider == "Brevo" && (x.Status == "Processed" || x.Status == "Succeeded")),
                    BrevoStalePendingCount = group.Count(x => x.Provider == "Brevo" && x.Status == "Pending" && x.CreatedAtUtc <= staleBeforeUtc),
                    BrevoDeliveryFailureEventCount = group.Count(x => x.Provider == "Brevo" &&
                        (x.CallbackType == "hard_bounce" ||
                         x.CallbackType == "soft_bounce" ||
                         x.CallbackType == "spam" ||
                         x.CallbackType == "blocked" ||
                         x.CallbackType == "invalid" ||
                         x.CallbackType == "error")),
                    BrevoRecent24HourCount = group.Count(x => x.Provider == "Brevo" && x.CreatedAtUtc >= recent24HoursUtc)
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false) ?? new ProviderCallbackInboxSummaryDto();

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
                    (x.FailureReason != null && EF.Functions.Like(x.FailureReason, q, QueryLikePattern.EscapeCharacter)));
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

            if (filter.DeliveryFailureOnly)
            {
                query = query.Where(BrevoDeliveryFailurePredicate);
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
                FailureReason = OperatorDisplayTextSanitizer.SanitizeFailureText(row.FailureReason),
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
                    return "Payload captured; preview unavailable.";
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
                return "Payload captured; preview unavailable.";
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

            return parts.Count == 0 ? "Stripe payload captured; preview unavailable." : string.Join("; ", parts);
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
            return parts.Count == 0 ? "DHL payload captured; preview unavailable." : string.Join("; ", parts);
        }

        private static string BuildBrevoPayloadPreview(JsonElement root)
        {
            var parts = new List<string>();
            AddPreviewPart(parts, "event", GetString(root, "event"));
            AddPreviewPart(parts, "messageId", GetString(root, "message-id"));
            AddPreviewPart(parts, "email", MaskEmail(GetString(root, "email")));
            AddPreviewPart(parts, "reason", GetString(root, "reason"));
            AddPreviewPart(parts, "ts", GetScalarText(root, "ts_event") ?? GetScalarText(root, "ts"));
            return parts.Count == 0 ? "Brevo payload captured; preview unavailable." : string.Join("; ", parts);
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
                    JsonValueKind.String => SanitizeScalar(property.Name, property.Value.GetString()),
                    JsonValueKind.Number => property.Value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => null,
                    _ => null
                };

                AddPreviewPart(parts, property.Name, value);
            }

            return parts.Count == 0 ? "Payload captured; preview unavailable." : string.Join("; ", parts);
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
                parts.Add($"{key}: {Summarize(value.Trim(), 80)}");
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

        private static string? SanitizeScalar(string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (key.Contains("email", StringComparison.OrdinalIgnoreCase))
            {
                return MaskEmail(value);
            }

            if (key.Contains("phone", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("mobile", StringComparison.OrdinalIgnoreCase))
            {
                return MaskPhone(value);
            }

            return value.Trim();
        }

        private static string? MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var normalized = email.Trim();
            var at = normalized.IndexOf('@');
            if (at <= 0)
            {
                return "***";
            }

            var local = normalized[..at];
            var domain = normalized[(at + 1)..];
            var prefix = local.Length <= 1 ? local : local[..Math.Min(2, local.Length)];
            return $"{prefix}***@{domain}";
        }

        private static string? MaskPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return null;
            }

            var normalized = phone.Trim();
            var visible = Math.Min(4, normalized.Length);
            return $"***{normalized[^visible..]}";
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
