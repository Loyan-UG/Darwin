using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Common;
using Darwin.WebApi.Controllers.Businesses;
using Darwin.WebApi.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.WebApi.Controllers.Public;

/// <summary>
/// Public business discovery and business detail endpoints for anonymous and storefront consumers.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/public/businesses")]
public sealed class PublicBusinessesController : ApiControllerBase
{
    private const int MaxPageSize = 100;

    private readonly GetBusinessesForDiscoveryHandler _getBusinessesForDiscovery;
    private readonly GetBusinessesForMapDiscoveryHandler _getBusinessesForMapDiscovery;
    private readonly GetBusinessPublicDetailHandler _getBusinessPublicDetail;

    public PublicBusinessesController(
        GetBusinessesForDiscoveryHandler getBusinessesForDiscovery,
        GetBusinessesForMapDiscoveryHandler getBusinessesForMapDiscovery,
        GetBusinessPublicDetailHandler getBusinessPublicDetail)
    {
        _getBusinessesForDiscovery = getBusinessesForDiscovery ?? throw new ArgumentNullException(nameof(getBusinessesForDiscovery));
        _getBusinessesForMapDiscovery = getBusinessesForMapDiscovery ?? throw new ArgumentNullException(nameof(getBusinessesForMapDiscovery));
        _getBusinessPublicDetail = getBusinessPublicDetail ?? throw new ArgumentNullException(nameof(getBusinessPublicDetail));
    }

    [HttpPost("list")]
    [HttpPost("/api/v1/businesses/list")]
    [ProducesResponseType(typeof(PagedResponse<BusinessSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListAsync([FromBody] BusinessListRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
        if (pageSize > MaxPageSize)
        {
            pageSize = MaxPageSize;
        }

        var queryText = BusinessControllerConventions.NormalizeNullable(request.Query) ?? BusinessControllerConventions.NormalizeNullable(request.Search);

        if (!BusinessControllerConventions.TryParseBusinessCategoryKind(request.CategoryKindKey, out var categoryKind, out var categoryError))
        {
            return BadRequestProblem(categoryError);
        }

        var (coordinate, radiusKm, proximityError) = BusinessControllerConventions.TryMapProximity(request.Near, request.RadiusMeters);
        if (proximityError is not null)
        {
            return BadRequestProblem(proximityError);
        }

        var (minRating, ratingError) = BusinessControllerConventions.TryNormalizeMinRating(request.MinRating);
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
            RadiusKm = radiusKm
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
    [ProducesResponseType(typeof(PagedResponse<BusinessSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MapAsync([FromBody] BusinessMapDiscoveryRequest? request, CancellationToken ct = default)
    {
        if (request?.Bounds is null)
        {
            return BadRequestProblem("Map bounds are required.");
        }

        var page = request.Page.GetValueOrDefault(1);
        if (page <= 0)
        {
            return BadRequestProblem("Page must be a positive integer.");
        }

        var pageSize = request.PageSize.GetValueOrDefault(200);
        if (pageSize <= 0 || pageSize > 500)
        {
            return BadRequestProblem("PageSize must be between 1 and 500.");
        }

        if (!BusinessControllerConventions.TryParseBusinessCategoryKind(request.Category, out var categoryKind, out var categoryError))
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
    public async Task<IActionResult> GetAsync([FromRoute] Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequestProblem("Business id must be a non-empty GUID.");
        }

        var dto = await _getBusinessPublicDetail.HandleAsync(id, ct).ConfigureAwait(false);
        if (dto is null)
        {
            return NotFoundProblem("Business was not found.");
        }

        return Ok(BusinessContractsMapper.ToContract(dto));
    }
}
