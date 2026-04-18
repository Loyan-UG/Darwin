using Darwin.Application.Shipping.DTOs;
using Darwin.Application.Shipping.Queries;
using Darwin.Application.Settings.DTOs;
using Darwin.Contracts.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.WebApi.Controllers.Public;

/// <summary>
/// Public storefront shipping quote endpoints.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/public/shipping")]
public sealed class PublicShippingController : ApiControllerBase
{
    private readonly RateShipmentHandler _rateShipmentHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublicShippingController"/> class.
    /// </summary>
    public PublicShippingController(RateShipmentHandler rateShipmentHandler)
    {
        _rateShipmentHandler = rateShipmentHandler ?? throw new ArgumentNullException(nameof(rateShipmentHandler));
    }

    /// <summary>
    /// Returns shipping options for storefront checkout estimation.
    /// </summary>
    [HttpPost("rates")]
    [HttpPost("/api/v1/shipping/rates")]
    [ProducesResponseType(typeof(IReadOnlyList<PublicShippingOption>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRatesAsync([FromBody] PublicShippingRateRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
        }

        try
        {
            var items = await _rateShipmentHandler.HandleAsync(new RateShipmentInputDto
            {
                Country = request.Country,
                SubtotalNetMinor = request.SubtotalNetMinor,
                ShipmentMass = request.ShipmentMass,
                Currency = request.Currency
            }, request.Currency ?? SiteSettingDto.DefaultCurrencyDefault, ct).ConfigureAwait(false);

            return Ok(items.Select(MapOption).ToList());
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is FluentValidation.ValidationException)
        {
            return BadRequestProblem("Shipping options could not be calculated.", ex.Message);
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
