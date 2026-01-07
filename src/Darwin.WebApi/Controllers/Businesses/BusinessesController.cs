using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Application.Common.DTOs;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Common;
using Darwin.Domain.Enums;
using Darwin.WebApi.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.WebApi.Controllers.Businesses;

/// <summary>
/// Public (consumer-facing) business discovery and detail endpoints.
/// Contract-first: request/response models are always taken from Darwin.Contracts,
/// while Application DTOs remain internal and are mapped explicitly.
/// </summary>
/// <remarks>
/// IMPORTANT:
/// This controller must remain thin. It should contain no business rules.
/// All query logic belongs to Application handlers.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/v1/businesses")]
public sealed class BusinessesController : ApiControllerBase
{
    private const int MaxPageSize = 100;

    private readonly GetBusinessesForDiscoveryHandler _getBusinessesForDiscovery;
    private readonly GetBusinessPublicDetailHandler _getBusinessPublicDetail;
    private readonly GetBusinessPublicDetailWithMyAccountHandler _getBusinessPublicDetailWithMyAccountHandler;
    private readonly GetBusinessesForMapDiscoveryHandler _getBusinessesForMapDiscoveryHandler;

    public BusinessesController(
        GetBusinessesForDiscoveryHandler getBusinessesForDiscovery,
        GetBusinessPublicDetailHandler getBusinessPublicDetail,
        GetBusinessesForMapDiscoveryHandler getBusinessesForMapDiscoveryHandler,
        GetBusinessPublicDetailWithMyAccountHandler getBusinessPublicDetailWithMyAccountHandler)
    {
        _getBusinessesForDiscovery = getBusinessesForDiscovery ?? throw new ArgumentNullException(nameof(getBusinessesForDiscovery));
        _getBusinessPublicDetail = getBusinessPublicDetail ?? throw new ArgumentNullException(nameof(getBusinessPublicDetail));
        _getBusinessesForMapDiscoveryHandler = getBusinessesForMapDiscoveryHandler ?? throw new ArgumentNullException(nameof(getBusinessesForMapDiscoveryHandler));
        _getBusinessPublicDetailWithMyAccountHandler = getBusinessPublicDetailWithMyAccountHandler ?? throw new ArgumentNullException(nameof(getBusinessPublicDetailWithMyAccountHandler));
    }

    [HttpPost("list")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<BusinessSummary>), 200)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), 400)]
    public async Task<IActionResult> ListAsync(
        [FromBody] BusinessListRequest? request,
        CancellationToken ct = default)
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

        var queryText = NormalizeNullable(request.Query) ?? NormalizeNullable(request.Search);

        if (!TryParseBusinessCategoryKind(request.CategoryKindKey, out var categoryKind, out var categoryError))
        {
            return BadRequestProblem(categoryError);
        }

        var (coordinate, radiusKm, proximityError) = TryMapProximity(request.Near, request.RadiusMeters);
        if (proximityError is not null)
        {
            return BadRequestProblem(proximityError);
        }

        var appRequest = new BusinessDiscoveryRequestDto
        {
            Page = page,
            PageSize = pageSize,
            Query = queryText,
            City = NormalizeNullable(request.City),
            CountryCode = NormalizeNullable(request.CountryCode),
            AddressQuery = NormalizeNullable(request.AddressQuery),
            Category = categoryKind,
            Coordinate = coordinate,
            RadiusKm = radiusKm
        };

        var (items, total) = await _getBusinessesForDiscovery.HandleAsync(appRequest, ct).ConfigureAwait(false);

        var response = new PagedResponse<BusinessSummary>
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
        };

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BusinessDetail), 200)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), 404)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), 400)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] Guid id,
        CancellationToken ct = default)
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

        var contract = BusinessContractsMapper.ToContract(dto);
        return Ok(contract);
    }

    [HttpGet("{id:guid}/with-my-account")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(BusinessDetailWithMyAccount), 200)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), 400)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), 404)]
    public async Task<IActionResult> GetWithMyAccountAsync(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return NotFoundProblem("Business not found.");
        }

        var result = await _getBusinessPublicDetailWithMyAccountHandler
            .HandleAsync(id, ct)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return ProblemFromResult(result);
        }

        var dto = result.Value;
        if (dto is null || dto.Business is null)
        {
            return NotFoundProblem("Business not found.");
        }

        var contract = BusinessContractsMapper.ToContract(dto);
        return Ok(contract);
    }

    // NOTE: GetBusinessesForMapDiscoveryHandler endpoint isn't shown in this controller file snippet,
    // but if/when you add it, it should also map using a dedicated mapper to keep the controller thin.

    private static string? NormalizeNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool TryParseBusinessCategoryKind(
        string? category,
        out BusinessCategoryKind? kind,
        out string error)
    {
        kind = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(category))
        {
            return true;
        }

        if (Enum.TryParse<BusinessCategoryKind>(category.Trim(), ignoreCase: true, out var parsed))
        {
            kind = parsed;
            return true;
        }

        error = "Invalid category value. It must match a known BusinessCategoryKind enum name.";
        return false;
    }

    private static (GeoCoordinateDto? Coordinate, double? RadiusKm, string? Error) TryMapProximity(
        GeoCoordinateModel? near,
        int? radiusMeters)
    {
        if (near is null && radiusMeters is null)
        {
            return (null, null, null);
        }

        if (near is null)
        {
            return (null, null, "Near must be provided when RadiusMeters is provided.");
        }

        if (radiusMeters.HasValue && radiusMeters.Value < 0)
        {
            return (null, null, "RadiusMeters must be zero or a positive integer.");
        }

        var coordinate = new GeoCoordinateDto
        {
            Latitude = near.Latitude,
            Longitude = near.Longitude,
            AltitudeMeters = near.AltitudeMeters
        };

        var radiusKm = radiusMeters.HasValue
            ? radiusMeters.Value / 1000.0
            : (double?)null;

        return (coordinate, radiusKm, null);
    }
}