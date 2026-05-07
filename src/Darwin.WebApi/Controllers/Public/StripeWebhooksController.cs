using System.Text.Json;
using Darwin.Application;
using Darwin.Application.Settings.Queries;
using Darwin.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace Darwin.WebApi.Controllers.Public;

/// <summary>
/// Public provider callback endpoints used for Stripe payment and billing lifecycle webhooks.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/public/billing/stripe/webhooks")]
public sealed class StripeWebhooksController : ApiControllerBase
{
    private const int MaxEventIdLength = 100;
    private const int MaxEventTypeLength = 64;

    private readonly ProviderCallbackInboxWriter _inboxWriter;
    private readonly GetSiteSettingHandler _getSiteSettingHandler;
    private readonly StripeWebhookSignatureVerifier _signatureVerifier;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    public StripeWebhooksController(
        ProviderCallbackInboxWriter inboxWriter,
        GetSiteSettingHandler getSiteSettingHandler,
        StripeWebhookSignatureVerifier signatureVerifier,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _inboxWriter = inboxWriter ?? throw new ArgumentNullException(nameof(inboxWriter));
        _getSiteSettingHandler = getSiteSettingHandler ?? throw new ArgumentNullException(nameof(getSiteSettingHandler));
        _signatureVerifier = signatureVerifier ?? throw new ArgumentNullException(nameof(signatureVerifier));
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    [HttpPost]
    [HttpPost("/api/v1/billing/stripe/webhooks")]
    [EnableRateLimiting("provider-webhook")]
    [RequestTimeout("provider-webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveAsync(CancellationToken ct = default)
    {
        var siteSetting = await _getSiteSettingHandler.HandleAsync(ct).ConfigureAwait(false);
        if (siteSetting is null || string.IsNullOrWhiteSpace(siteSetting.StripeWebhookSecret))
        {
            return BadRequestProblem(_validationLocalizer["StripeWebhookSecretNotConfigured"]);
        }

        var payloadRead = await ProviderWebhookPayloadReader.ReadAsync(Request, ct).ConfigureAwait(false);
        if (payloadRead.PayloadTooLarge)
        {
            return PayloadTooLargeProblem(_validationLocalizer["ProviderWebhookPayloadTooLarge"]);
        }

        var rawPayload = payloadRead.Payload;

        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return BadRequestProblem(_validationLocalizer["StripeWebhookPayloadInvalid"]);
        }

        var signatureHeader = Request.Headers["Stripe-Signature"].ToString();
        if (!_signatureVerifier.TryVerify(rawPayload, signatureHeader, siteSetting.StripeWebhookSecret, out var errorKey))
        {
            return BadRequestProblem(_validationLocalizer[string.IsNullOrWhiteSpace(errorKey) ? "StripeWebhookSignatureInvalid" : errorKey]);
        }

        if (!TryParseStripeEnvelope(rawPayload, out var eventId, out var eventType))
        {
            return BadRequestProblem(_validationLocalizer["StripeWebhookPayloadInvalid"]);
        }

        var existing = await _inboxWriter.AddIfNewAsync(
            provider: "Stripe",
            callbackType: eventType,
            idempotencyKey: eventId,
            rawPayload,
            ct).ConfigureAwait(false);

        return Ok(new
        {
            received = true,
            duplicate = existing
        });
    }

    private static bool TryParseStripeEnvelope(string rawPayload, out string eventId, out string eventType)
    {
        eventId = string.Empty;
        eventType = string.Empty;

        try
        {
            using var document = JsonDocument.Parse(rawPayload);
            var root = document.RootElement;
            if (!root.TryGetProperty("id", out var idProperty) || idProperty.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            if (!root.TryGetProperty("type", out var typeProperty) || typeProperty.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            eventId = idProperty.GetString()?.Trim() ?? string.Empty;
            eventType = typeProperty.GetString()?.Trim() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(eventId) &&
                   eventId.Length <= MaxEventIdLength &&
                   !string.IsNullOrWhiteSpace(eventType) &&
                   eventType.Length <= MaxEventTypeLength;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
