using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Darwin.Application;
using Darwin.Infrastructure.Notifications.Brevo;
using Darwin.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Darwin.WebApi.Controllers.Public;

/// <summary>
/// Public provider callback endpoint used for Brevo transactional email lifecycle webhooks.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/public/notifications/brevo/webhooks")]
public sealed class BrevoWebhooksController : ApiControllerBase
{
    private readonly ProviderCallbackInboxWriter _inboxWriter;
    private readonly BrevoEmailOptions _options;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    public BrevoWebhooksController(
        ProviderCallbackInboxWriter inboxWriter,
        IOptions<BrevoEmailOptions> options,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _inboxWriter = inboxWriter ?? throw new ArgumentNullException(nameof(inboxWriter));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    [HttpPost]
    [EnableRateLimiting("provider-webhook")]
    [RequestTimeout("provider-webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveAsync(CancellationToken ct = default)
    {
        if (!HasWebhookCredentials())
        {
            return BadRequestProblem(_validationLocalizer["BrevoWebhookAuthenticationNotConfigured"]);
        }

        if (!TryVerifyBasicAuth(Request.Headers.Authorization.ToString()))
        {
            return BadRequestProblem(_validationLocalizer["BrevoWebhookAuthenticationInvalid"]);
        }

        var payloadRead = await ProviderWebhookPayloadReader.ReadAsync(Request, ct).ConfigureAwait(false);
        if (payloadRead.PayloadTooLarge)
        {
            return PayloadTooLargeProblem(_validationLocalizer["ProviderWebhookPayloadTooLarge"]);
        }

        var rawPayload = payloadRead.Payload;
        if (!TryParseEnvelope(rawPayload, out var eventName, out var messageId, out var eventTimestamp))
        {
            return BadRequestProblem(_validationLocalizer["BrevoWebhookPayloadInvalid"]);
        }

        var idempotencyKey = BuildIdempotencyKey(eventName, messageId, eventTimestamp);
        var existing = await _inboxWriter.AddIfNewAsync(
            provider: "Brevo",
            callbackType: eventName,
            idempotencyKey,
            rawPayload,
            ct).ConfigureAwait(false);

        return Ok(new
        {
            received = true,
            duplicate = existing
        });
    }

    private bool HasWebhookCredentials()
    {
        return !string.IsNullOrWhiteSpace(_options.WebhookUsername) &&
               !string.IsNullOrWhiteSpace(_options.WebhookPassword);
    }

    private bool TryVerifyBasicAuth(string authorizationHeader)
    {
        if (!AuthenticationHeaderValue.TryParse(authorizationHeader, out var header) ||
            !string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(header.Parameter))
        {
            return false;
        }

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter));
        }
        catch (FormatException)
        {
            return false;
        }

        var separatorIndex = decoded.IndexOf(':', StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            return false;
        }

        var username = decoded[..separatorIndex];
        var password = decoded[(separatorIndex + 1)..];
        return FixedTimeEquals(username, _options.WebhookUsername!) &&
               FixedTimeEquals(password, _options.WebhookPassword!);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = SHA256.HashData(Encoding.UTF8.GetBytes(left));
        var rightBytes = SHA256.HashData(Encoding.UTF8.GetBytes(right));
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static bool TryParseEnvelope(string rawPayload, out string eventName, out string messageId, out string eventTimestamp)
    {
        eventName = string.Empty;
        messageId = string.Empty;
        eventTimestamp = string.Empty;
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(rawPayload);
            var root = document.RootElement;
            eventName = NormalizeCallbackType(ReadStringAny(root, "event", "Event"));
            messageId = ReadStringAny(root, "message-id", "messageId", "message_id", "messageIdLong")?.Trim() ?? string.Empty;
            eventTimestamp = ReadScalarAsStringAny(root, "ts_event", "ts", "ts_epoch", "date", "Date") ??
                             string.Empty;

            return !string.IsNullOrWhiteSpace(eventName) &&
                   !string.IsNullOrWhiteSpace(messageId) &&
                   !string.IsNullOrWhiteSpace(eventTimestamp);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string BuildIdempotencyKey(string eventName, string messageId, string eventTimestamp)
    {
        var value = string.Concat(eventName.Trim(), "::", messageId.Trim(), "::", eventTimestamp.Trim());
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
        return string.Concat("brevo::", hash);
    }

    private static string NormalizeCallbackType(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized.Length <= 64 ? normalized : normalized[..64];
    }

    private static string? ReadStringAny(JsonElement root, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var value = ReadString(root, propertyName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
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

    private static string? ReadScalarAsStringAny(JsonElement root, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var value = ReadScalarAsString(root, propertyName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string? ReadScalarAsString(JsonElement root, string propertyName)
    {
        if (!TryGetProperty(root, propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.String => property.GetString(),
            _ => null
        };
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
}
