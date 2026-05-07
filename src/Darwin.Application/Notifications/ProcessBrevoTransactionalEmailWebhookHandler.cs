using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Integration;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Notifications;

public sealed class ProcessBrevoTransactionalEmailWebhookHandler
{
    private const string ProviderName = "Brevo";

    private static readonly HashSet<string> FailureEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "hard_bounce",
        "soft_bounce",
        "spam",
        "blocked",
        "invalid",
        "error"
    };

    private static readonly HashSet<string> DeliveryEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "request",
        "delivered",
        "opened",
        "unique_opened",
        "proxy_open",
        "unique_proxy_open",
        "click",
        "deferred",
        "unsubscribed"
    };

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public ProcessBrevoTransactionalEmailWebhookHandler(
        IAppDbContext db,
        IClock clock,
        IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public async Task<Result> HandleAsync(string rawPayloadJson, CancellationToken ct = default)
    {
        if (!TryParsePayload(rawPayloadJson, out var payload))
        {
            return Result.Fail(_localizer["BrevoWebhookPayloadInvalid"]);
        }

        var audit = await FindAuditAsync(payload, ct).ConfigureAwait(false);
        if (audit is null)
        {
            return Result.Ok();
        }

        if (FailureEvents.Contains(payload.Event))
        {
            audit.Status = "Failed";
            audit.CompletedAtUtc = payload.OccurredAtUtc ?? _clock.UtcNow;
            audit.FailureMessage = BuildFailureMessage(payload);
        }
        else if (DeliveryEvents.Contains(payload.Event))
        {
            if (string.Equals(audit.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                audit.Status = "Sent";
            }

            audit.CompletedAtUtc ??= payload.OccurredAtUtc ?? _clock.UtcNow;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Ok();
    }

    private async Task<EmailDispatchAudit?> FindAuditAsync(BrevoWebhookPayload payload, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(payload.MessageId))
        {
            var messageId = payload.MessageId.Trim();
            var byMessageId = await _db.Set<EmailDispatchAudit>()
                .Where(x => !x.IsDeleted && x.Provider == ProviderName && x.ProviderMessageId == messageId)
                .OrderByDescending(x => x.AttemptedAtUtc)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (byMessageId is not null)
            {
                return byMessageId;
            }
        }

        if (!string.IsNullOrWhiteSpace(payload.CorrelationKey))
        {
            var correlationKey = payload.CorrelationKey.Trim();
            var byCorrelation = await _db.Set<EmailDispatchAudit>()
                .Where(x => !x.IsDeleted && x.Provider == ProviderName && x.CorrelationKey == correlationKey)
                .OrderByDescending(x => x.AttemptedAtUtc)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (byCorrelation is not null)
            {
                return byCorrelation;
            }
        }

        if (!string.IsNullOrWhiteSpace(payload.Email) && !string.IsNullOrWhiteSpace(payload.Subject))
        {
            var email = payload.Email.Trim();
            var subject = payload.Subject.Trim();
            var cutoffUtc = _clock.UtcNow.AddDays(-7);
            return await _db.Set<EmailDispatchAudit>()
                .Where(x => !x.IsDeleted &&
                            x.Provider == ProviderName &&
                            x.AttemptedAtUtc >= cutoffUtc &&
                            (x.IntendedRecipientEmail == email || x.RecipientEmail == email) &&
                            x.Subject == subject)
                .OrderByDescending(x => x.AttemptedAtUtc)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
        }

        return null;
    }

    private static string BuildFailureMessage(BrevoWebhookPayload payload)
    {
        var reason = string.IsNullOrWhiteSpace(payload.Reason) ? "No provider reason supplied." : payload.Reason.Trim();
        var value = $"Brevo event '{payload.Event}': {reason}";
        return value.Length <= 2000 ? value : value[..2000];
    }

    private static bool TryParsePayload(string rawPayloadJson, out BrevoWebhookPayload payload)
    {
        payload = new BrevoWebhookPayload();
        if (string.IsNullOrWhiteSpace(rawPayloadJson))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(rawPayloadJson);
            var root = document.RootElement;
            payload.Event = NormalizeEvent(ReadStringAny(root, "event", "Event"));
            payload.Email = ReadStringAny(root, "email", "Email");
            payload.Subject = ReadStringAny(root, "subject", "Subject");
            payload.MessageId = ReadStringAny(root, "message-id", "messageId", "message_id", "messageIdLong");
            payload.CorrelationKey = ReadStringAny(root, "X-Correlation-Key", "x-correlation-key", "Idempotency-Key", "idempotency-key", "X-Mailin-custom", "x-mailin-custom");
            payload.Reason = ReadStringAny(root, "reason", "Reason", "message", "Message");
            payload.OccurredAtUtc = ReadUnixSeconds(root, "ts_event") ??
                                    ReadUnixSeconds(root, "ts") ??
                                    ReadUnixMilliseconds(root, "ts_epoch") ??
                                    ReadUnixSeconds(root, "date");

            return !string.IsNullOrWhiteSpace(payload.Event);
        }
        catch (JsonException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static string NormalizeEvent(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static string? ReadStringAny(JsonElement root, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var value = ReadString(root, propertyName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        return TryGetProperty(root, propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static DateTime? ReadUnixSeconds(JsonElement root, string propertyName)
    {
        if (!TryGetProperty(root, propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetInt64(out var seconds) => TryFromUnixSeconds(seconds),
            JsonValueKind.String when long.TryParse(property.GetString(), out var seconds) => TryFromUnixSeconds(seconds),
            _ => null
        };
    }

    private static DateTime? ReadUnixMilliseconds(JsonElement root, string propertyName)
    {
        if (!TryGetProperty(root, propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetInt64(out var milliseconds) => TryFromUnixMilliseconds(milliseconds),
            JsonValueKind.String when long.TryParse(property.GetString(), out var milliseconds) => TryFromUnixMilliseconds(milliseconds),
            _ => null
        };
    }

    private static DateTime? TryFromUnixSeconds(long seconds)
    {
        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static DateTime? TryFromUnixMilliseconds(long milliseconds)
    {
        try
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static bool TryGetProperty(JsonElement root, string propertyName, out JsonElement property)
    {
        property = default;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var item in root.EnumerateObject())
        {
            if (string.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                property = item.Value;
                return true;
            }
        }

        return false;
    }

    private sealed class BrevoWebhookPayload
    {
        public string Event { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Subject { get; set; }
        public string? MessageId { get; set; }
        public string? CorrelationKey { get; set; }
        public string? Reason { get; set; }
        public DateTime? OccurredAtUtc { get; set; }
    }
}
