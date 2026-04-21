using System.Text;
using System.Text.Json;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing;
using Darwin.Application.Settings.Queries;
using Darwin.Domain.Entities.Integration;
using Darwin.WebApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
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
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IAppDbContext _db;
    private readonly GetSiteSettingHandler _getSiteSettingHandler;
    private readonly StripeWebhookSignatureVerifier _signatureVerifier;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    public StripeWebhooksController(
        IAppDbContext db,
        GetSiteSettingHandler getSiteSettingHandler,
        StripeWebhookSignatureVerifier signatureVerifier,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _getSiteSettingHandler = getSiteSettingHandler ?? throw new ArgumentNullException(nameof(getSiteSettingHandler));
        _signatureVerifier = signatureVerifier ?? throw new ArgumentNullException(nameof(signatureVerifier));
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    [HttpPost]
    [HttpPost("/api/v1/billing/stripe/webhooks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveAsync(CancellationToken ct = default)
    {
        var siteSetting = await _getSiteSettingHandler.HandleAsync(ct).ConfigureAwait(false);
        if (siteSetting is null || string.IsNullOrWhiteSpace(siteSetting.StripeWebhookSecret))
        {
            return BadRequestProblem(_validationLocalizer["StripeWebhookSecretNotConfigured"]);
        }

        Request.EnableBuffering();
        string rawPayload;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        {
            rawPayload = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        }

        Request.Body.Position = 0;

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

        var existing = await _db.Set<ProviderCallbackInboxMessage>()
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.Provider == "Stripe" && x.IdempotencyKey == eventId, ct)
            .ConfigureAwait(false);

        if (!existing)
        {
            _db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
            {
                Provider = "Stripe",
                CallbackType = eventType,
                IdempotencyKey = eventId,
                PayloadJson = rawPayload,
                Status = "Pending"
            });
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return Ok(new
        {
            received = true,
            duplicate = existing,
            eventId,
            eventType,
            instance = Request.GetDisplayUrl()
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

            eventId = idProperty.GetString() ?? string.Empty;
            eventType = typeProperty.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(eventId) && !string.IsNullOrWhiteSpace(eventType);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
