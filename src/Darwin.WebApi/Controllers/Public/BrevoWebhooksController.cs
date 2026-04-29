using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Integration;
using Darwin.Infrastructure.Notifications.Brevo;
using Darwin.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    private readonly IAppDbContext _db;
    private readonly BrevoEmailOptions _options;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    public BrevoWebhooksController(
        IAppDbContext db,
        IOptions<BrevoEmailOptions> options,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    [HttpPost]
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
        var existing = await AddInboxMessageIfNewAsync(
            provider: "Brevo",
            callbackType: eventName,
            idempotencyKey,
            rawPayload,
            ct).ConfigureAwait(false);

        return Ok(new
        {
            received = true,
            duplicate = existing,
            eventName,
            messageId,
            instance = Request.GetDisplayUrl()
        });
    }

    private async Task<bool> AddInboxMessageIfNewAsync(
        string provider,
        string callbackType,
        string idempotencyKey,
        string rawPayload,
        CancellationToken ct)
    {
        var existing = await InboxMessageExistsAsync(provider, idempotencyKey, ct).ConfigureAwait(false);
        if (existing)
        {
            return true;
        }

        _db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = provider,
            CallbackType = callbackType,
            IdempotencyKey = idempotencyKey,
            PayloadJson = rawPayload,
            Status = "Pending"
        });

        try
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return false;
        }
        catch (DbUpdateException)
        {
            if (await InboxMessageExistsAsync(provider, idempotencyKey, ct).ConfigureAwait(false))
            {
                return true;
            }

            throw;
        }
    }

    private Task<bool> InboxMessageExistsAsync(string provider, string idempotencyKey, CancellationToken ct)
    {
        return _db.Set<ProviderCallbackInboxMessage>()
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.Provider == provider && x.IdempotencyKey == idempotencyKey, ct);
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
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length &&
               CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
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
            eventName = ReadString(root, "event")?.Trim() ?? string.Empty;
            messageId = ReadString(root, "message-id")?.Trim() ?? string.Empty;
            eventTimestamp = ReadScalarAsString(root, "ts_event") ??
                             ReadScalarAsString(root, "ts") ??
                             ReadScalarAsString(root, "ts_epoch") ??
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
        return value.Length <= 256 ? value : value[..256];
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static string? ReadScalarAsString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
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
}
