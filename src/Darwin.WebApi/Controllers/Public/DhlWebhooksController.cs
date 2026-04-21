using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Settings.Queries;
using Darwin.Contracts.Shipping;
using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
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

    private readonly IAppDbContext _db;
    private readonly GetSiteSettingHandler _getSiteSettingHandler;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    public DhlWebhooksController(
        IAppDbContext db,
        GetSiteSettingHandler getSiteSettingHandler,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _getSiteSettingHandler = getSiteSettingHandler ?? throw new ArgumentNullException(nameof(getSiteSettingHandler));
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    [HttpPost]
    [HttpPost("/api/v1/shipping/dhl/webhooks")]
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

        Request.EnableBuffering();
        string rawPayload;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        {
            rawPayload = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        }

        Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return BadRequestProblem(_validationLocalizer["DhlWebhookPayloadInvalid"]);
        }

        var apiKeyHeader = Request.Headers["X-DHL-Key"].ToString();
        if (!string.Equals(apiKeyHeader, siteSetting.DhlApiKey, StringComparison.Ordinal))
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

        var idempotencyKey = BuildIdempotencyKey(request);
        var existing = await _db.Set<ProviderCallbackInboxMessage>()
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.Provider == "DHL" && x.IdempotencyKey == idempotencyKey, ct)
            .ConfigureAwait(false);

        if (!existing)
        {
            _db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
            {
                Provider = "DHL",
                CallbackType = request.CarrierEventKey,
                IdempotencyKey = idempotencyKey,
                PayloadJson = rawPayload,
                Status = "Pending"
            });
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return Ok(new
        {
            received = true,
            duplicate = existing,
            providerShipmentReference = request.ProviderShipmentReference,
            carrierEventKey = request.CarrierEventKey,
            instance = Request.GetDisplayUrl()
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

        var key = Encoding.UTF8.GetBytes(secret);
        var payload = Encoding.UTF8.GetBytes(rawPayload);
        using var hmac = new HMACSHA256(key);
        var computedHash = hmac.ComputeHash(payload);

        try
        {
            var providedHash = Convert.FromHexString(normalizedHeader);
            return CryptographicOperations.FixedTimeEquals(computedHash, providedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string BuildIdempotencyKey(DhlShipmentCallbackRequest request)
    {
        return string.Concat(
            request.ProviderShipmentReference.Trim(),
            "::",
            request.CarrierEventKey.Trim(),
            "::",
            request.OccurredAtUtc.ToUniversalTime().ToString("O"));
    }
}
