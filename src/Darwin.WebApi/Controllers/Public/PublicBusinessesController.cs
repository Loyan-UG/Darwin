using Darwin.Application;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Common;
using Darwin.WebApi.Controllers.Businesses;
using Darwin.WebApi.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace Darwin.WebApi.Controllers.Public;

/// <summary>
/// Public business discovery and business detail endpoints for anonymous and storefront consumers.
/// </summary>
[ApiController]
[AllowAnonymous]
[EnableRateLimiting("public-storefront")]
[RequestTimeout("public-storefront")]
[Route("api/v1/public/businesses")]
public sealed class PublicBusinessesController : ApiControllerBase
{
    private const int MaxPublicPage = 10_000;
    private const int MaxBusinessDiscoveryRequestBytes = 16 * 1024;
    private const int MaxPageSize = 100;

    private readonly GetBusinessesForDiscoveryHandler _getBusinessesForDiscovery;
    private readonly GetBusinessesForMapDiscoveryHandler _getBusinessesForMapDiscovery;
    private readonly GetBusinessPublicDetailHandler _getBusinessPublicDetail;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    public PublicBusinessesController(
        GetBusinessesForDiscoveryHandler getBusinessesForDiscovery,
        GetBusinessesForMapDiscoveryHandler getBusinessesForMapDiscovery,
        GetBusinessPublicDetailHandler getBusinessPublicDetail,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _getBusinessesForDiscovery = getBusinessesForDiscovery ?? throw new ArgumentNullException(nameof(getBusinessesForDiscovery));
        _getBusinessesForMapDiscovery = getBusinessesForMapDiscovery ?? throw new ArgumentNullException(nameof(getBusinessesForMapDiscovery));
        _getBusinessPublicDetail = getBusinessPublicDetail ?? throw new ArgumentNullException(nameof(getBusinessPublicDetail));
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    [HttpPost("list")]
    [HttpPost("/api/v1/businesses/list")]
    [RequestSizeLimit(MaxBusinessDiscoveryRequestBytes)]
    [ProducesResponseType(typeof(PagedResponse<BusinessSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListAsync([FromBody] BusinessListRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        var page = request.Page < 1 ? 1 : request.Page;
        if (page > MaxPublicPage)
        {
            return BadRequestProblem(_validationLocalizer["PageMustBeBetween1And10000"]);
        }

        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
        if (pageSize > MaxPageSize)
        {
            pageSize = MaxPageSize;
        }

        var queryText = BusinessControllerConventions.NormalizeNullable(request.Query) ?? BusinessControllerConventions.NormalizeNullable(request.Search);

        if (!BusinessControllerConventions.TryParseBusinessCategoryKind(request.CategoryKindKey, _validationLocalizer, out var categoryKind, out var categoryError))
        {
            return BadRequestProblem(categoryError);
        }

        var (coordinate, radiusKm, proximityError) = BusinessControllerConventions.TryMapProximity(request.Near, _validationLocalizer, request.RadiusMeters);
        if (proximityError is not null)
        {
            return BadRequestProblem(proximityError);
        }

        var (minRating, ratingError) = BusinessControllerConventions.TryNormalizeMinRating(request.MinRating, _validationLocalizer);
        if (ratingError is not null)
        {
            return BadRequestProblem(ratingError);
        }

        var appRequest = new BusinessDiscoveryRequestDto
        {
            Page = page,
            PageSize = pageSize,
            Query = queryText,
            City = BusinessControllerConventions.NormalizeNullable(request.City),
            CountryCode = BusinessControllerConventions.NormalizeNullable(request.CountryCode),
            AddressQuery = BusinessControllerConventions.NormalizeNullable(request.AddressQuery),
            Category = categoryKind,
            MinRating = minRating,
            HasActiveLoyaltyProgram = request.HasActiveLoyaltyProgram,
            Coordinate = coordinate,
            RadiusKm = radiusKm,
            Culture = BusinessControllerConventions.NormalizeNullable(request.Culture)
        };

        var (items, total) = await _getBusinessesForDiscovery.HandleAsync(appRequest, ct).ConfigureAwait(false);

        return Ok(new PagedResponse<BusinessSummary>
        {
            Total = total,
            Items = (items ?? new List<BusinessDiscoveryListItemDto>())
                .Select(BusinessContractsMapper.ToContract)
                .ToList(),
            Request = new PagedRequest
            {
                Page = page,
                PageSize = pageSize,
                Search = queryText
            }
        });
    }

    [HttpPost("map")]
    [HttpPost("/api/v1/businesses/map")]
    [RequestSizeLimit(MaxBusinessDiscoveryRequestBytes)]
    [ProducesResponseType(typeof(PagedResponse<BusinessSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MapAsync([FromBody] BusinessMapDiscoveryRequest? request, CancellationToken ct = default)
    {
        if (request?.Bounds is null)
        {
            return BadRequestProblem(_validationLocalizer["MapBoundsRequired"]);
        }

        var page = request.Page.GetValueOrDefault(1);
        if (page <= 0)
        {
            return BadRequestProblem(_validationLocalizer["PageMustBePositiveInteger"]);
        }
        if (page > MaxPublicPage)
        {
            return BadRequestProblem(_validationLocalizer["PageMustBeBetween1And10000"]);
        }

        var pageSize = request.PageSize.GetValueOrDefault(200);
        if (pageSize <= 0 || pageSize > 500)
        {
            return BadRequestProblem(_validationLocalizer["PageSizeMustBeBetween1And500"]);
        }

        if (!BusinessControllerConventions.TryParseBusinessCategoryKind(request.Category, _validationLocalizer, out var categoryKind, out var categoryError))
        {
            return BadRequestProblem(categoryError);
        }

        var dto = new BusinessMapDiscoveryRequestDto
        {
            Page = page,
            PageSize = pageSize,
            Query = BusinessControllerConventions.NormalizeNullable(request.Query),
            CountryCode = BusinessControllerConventions.NormalizeNullable(request.CountryCode),
            Category = categoryKind,
            Culture = BusinessControllerConventions.NormalizeNullable(request.Culture),
            Bounds = new GeoBoundsDto
            {
                NorthLat = request.Bounds.NorthLat,
                SouthLat = request.Bounds.SouthLat,
                EastLon = request.Bounds.EastLon,
                WestLon = request.Bounds.WestLon
            }
        };

        var (items, total) = await _getBusinessesForMapDiscovery.HandleAsync(dto, ct).ConfigureAwait(false);
        return Ok(new PagedResponse<BusinessSummary>
        {
            Total = total,
            Items = (items ?? new List<BusinessDiscoveryListItemDto>())
                .Select(BusinessContractsMapper.ToContract)
                .ToList(),
            Request = new PagedRequest
            {
                Page = page,
                PageSize = pageSize,
                Search = dto.Query
            }
        });
    }

    [HttpGet("{id:guid}")]
    [HttpGet("/api/v1/businesses/{id:guid}")]
    [ProducesResponseType(typeof(BusinessDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] Guid id,
        [FromQuery] string? culture = null,
        CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["BusinessIdValidWhenProvided"]);
        }

        var dto = await _getBusinessPublicDetail
            .HandleAsync(id, BusinessControllerConventions.NormalizeNullable(culture), ct)
            .ConfigureAwait(false);
        if (dto is null)
        {
            return NotFoundProblem(_validationLocalizer["BusinessNotFound"]);
        }

        return Ok(BusinessContractsMapper.ToContract(dto));
    }
}
