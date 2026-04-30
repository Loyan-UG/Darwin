using Darwin.Application;
using Darwin.Application.Shipping.DTOs;
using Darwin.Application.Shipping.Queries;
using Darwin.Application.Settings.DTOs;
using Darwin.Contracts.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace Darwin.WebApi.Controllers.Public;

/// <summary>
/// Public storefront shipping quote endpoints.
/// </summary>
[ApiController]
[AllowAnonymous]
[EnableRateLimiting("public-storefront")]
[RequestTimeout("public-storefront")]
[Route("api/v1/public/shipping")]
public sealed class PublicShippingController : ApiControllerBase
{
    private const int MaxShippingRateRequestBytes = 8 * 1024;
    private const int CountryCodeLength = 2;
    private const int CurrencyCodeLength = 3;

    private readonly RateShipmentHandler _rateShipmentHandler;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublicShippingController"/> class.
    /// </summary>
    public PublicShippingController(RateShipmentHandler rateShipmentHandler, IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _rateShipmentHandler = rateShipmentHandler ?? throw new ArgumentNullException(nameof(rateShipmentHandler));
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    /// <summary>
    /// Returns shipping options for storefront checkout estimation.
    /// </summary>
    [HttpPost("rates")]
    [HttpPost("/api/v1/shipping/rates")]
    [RequestSizeLimit(MaxShippingRateRequestBytes)]
    [ProducesResponseType(typeof(IReadOnlyList<PublicShippingOption>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRatesAsync([FromBody] PublicShippingRateRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        var normalizedCountry = (request.Country ?? string.Empty).Trim();
        if (normalizedCountry.Length != CountryCodeLength)
        {
            return BadRequestProblem(_validationLocalizer["ShippingRateCountryCodeInvalid"]);
        }

        var normalizedCurrency = string.IsNullOrWhiteSpace(request.Currency)
            ? SiteSettingDto.DefaultCurrencyDefault
            : request.Currency.Trim();
        if (normalizedCurrency.Length != CurrencyCodeLength)
        {
            return BadRequestProblem(_validationLocalizer["ShippingRateCurrencyCodeInvalid"]);
        }

        try
        {
            var items = await _rateShipmentHandler.HandleAsync(new RateShipmentInputDto
            {
                Country = normalizedCountry.ToUpperInvariant(),
                SubtotalNetMinor = request.SubtotalNetMinor,
                ShipmentMass = request.ShipmentMass,
                Currency = normalizedCurrency.ToUpperInvariant()
            }, normalizedCurrency.ToUpperInvariant(), ct).ConfigureAwait(false);

            return Ok(items.Select(MapOption).ToList());
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is FluentValidation.ValidationException)
        {
            return BadRequestProblem(_validationLocalizer["ShippingOptionsCouldNotBeCalculated"]);
        }
    }

    private static PublicShippingOption MapOption(ShippingOptionDto dto)
        => new()
        {
            MethodId = dto.MethodId,
            Name = dto.Name,
            PriceMinor = dto.PriceMinor,
            Currency = dto.Currency,
            Carrier = dto.Carrier,
            Service = dto.Service
        };
}
