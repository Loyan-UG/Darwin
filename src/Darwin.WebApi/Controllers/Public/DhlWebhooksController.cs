using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Darwin.Application;
using Darwin.Application.Settings.Queries;
using Darwin.Contracts.Shipping;
using Darwin.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace Darwin.WebApi.Controllers.Public;

/// <summary>
/// Public provider callback endpoints used for DHL shipment lifecycle events.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/public/shipping/dhl/webhooks")]
public sealed class DhlWebhooksController : ApiControllerBase
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private const int Sha256HexLength = 64;
    private const int MaxCallbackTypeLength = 64;

    private readonly ProviderCallbackInboxWriter _inboxWriter;
    private readonly GetSiteSettingHandler _getSiteSettingHandler;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    public DhlWebhooksController(
        ProviderCallbackInboxWriter inboxWriter,
        GetSiteSettingHandler getSiteSettingHandler,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _inboxWriter = inboxWriter ?? throw new ArgumentNullException(nameof(inboxWriter));
        _getSiteSettingHandler = getSiteSettingHandler ?? throw new ArgumentNullException(nameof(getSiteSettingHandler));
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    [HttpPost]
    [HttpPost("/api/v1/shipping/dhl/webhooks")]
    [EnableRateLimiting("provider-webhook")]
    [RequestTimeout("provider-webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveAsync(CancellationToken ct = default)
    {
        var siteSetting = await _getSiteSettingHandler.HandleAsync(ct).ConfigureAwait(false);
        if (siteSetting is null ||
            !siteSetting.DhlEnabled ||
            string.IsNullOrWhiteSpace(siteSetting.DhlApiKey) ||
            string.IsNullOrWhiteSpace(siteSetting.DhlApiSecret))
        {
            return BadRequestProblem(_validationLocalizer["DhlWebhookAuthenticationNotConfigured"]);
        }

        var payloadRead = await ProviderWebhookPayloadReader.ReadAsync(Request, ct).ConfigureAwait(false);
        if (payloadRead.PayloadTooLarge)
        {
            return PayloadTooLargeProblem(_validationLocalizer["ProviderWebhookPayloadTooLarge"]);
        }

        var rawPayload = payloadRead.Payload;

        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return BadRequestProblem(_validationLocalizer["DhlWebhookPayloadInvalid"]);
        }

        var apiKeyHeader = Request.Headers["X-DHL-Key"].ToString();
        if (!FixedTimeSha256Equals(apiKeyHeader, siteSetting.DhlApiKey))
        {
            return BadRequestProblem(_validationLocalizer["DhlWebhookApiKeyInvalid"]);
        }

        var signatureHeader = Request.Headers["X-DHL-Signature"].ToString();
        if (!TryVerifySignature(rawPayload, signatureHeader, siteSetting.DhlApiSecret))
        {
            return BadRequestProblem(_validationLocalizer["DhlWebhookSignatureInvalid"]);
        }

        DhlShipmentCallbackRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<DhlShipmentCallbackRequest>(rawPayload, SerializerOptions);
        }
        catch (JsonException)
        {
            return BadRequestProblem(_validationLocalizer["DhlWebhookPayloadInvalid"]);
        }

        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["DhlWebhookPayloadInvalid"]);
        }

        var providerShipmentReference = NormalizeText(request.ProviderShipmentReference);
        var carrierEventKey = NormalizeText(request.CarrierEventKey);
        if (providerShipmentReference is null ||
            carrierEventKey is null ||
            carrierEventKey.Length > MaxCallbackTypeLength)
        {
            return BadRequestProblem(_validationLocalizer["DhlWebhookPayloadInvalid"]);
        }

        var idempotencyKey = BuildIdempotencyKey(request);
        var existing = await _inboxWriter.AddIfNewAsync(
            provider: "DHL",
            callbackType: carrierEventKey,
            idempotencyKey,
            rawPayload,
            ct).ConfigureAwait(false);

        return Ok(new
        {
            received = true,
            duplicate = existing
        });
    }

    private static bool TryVerifySignature(string rawPayload, string signatureHeader, string secret)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        var normalizedHeader = signatureHeader.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)
            ? signatureHeader["sha256=".Length..]
            : signatureHeader;
        normalizedHeader = normalizedHeader.Trim();
        if (normalizedHeader.Length != Sha256HexLength)
        {
            return false;
        }

        var key = Encoding.UTF8.GetBytes(secret);
        var payload = Encoding.UTF8.GetBytes(rawPayload);
        using var hmac = new HMACSHA256(key);
        var computedHash = hmac.ComputeHash(payload);

        try
        {
            var providedHash = Convert.FromHexString(normalizedHeader);
            return CryptographicOperations.FixedTimeEquals(computedHash, providedHash);
        }
        catch (Exception ex) when (ex is FormatException || ex is ArgumentException)
        {
            return false;
        }
    }

    private static bool FixedTimeSha256Equals(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        var leftHash = SHA256.HashData(Encoding.UTF8.GetBytes(left.Trim()));
        var rightHash = SHA256.HashData(Encoding.UTF8.GetBytes(right.Trim()));
        return CryptographicOperations.FixedTimeEquals(leftHash, rightHash);
    }

    private static string BuildIdempotencyKey(DhlShipmentCallbackRequest request)
    {
        var value = string.Concat(
            NormalizeText(request.ProviderShipmentReference) ?? string.Empty,
            "::",
            NormalizeText(request.CarrierEventKey) ?? string.Empty,
            "::",
            request.OccurredAtUtc.ToUniversalTime().ToString("O"));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
        return string.Concat("dhl::", hash);
    }

    private static string? NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
