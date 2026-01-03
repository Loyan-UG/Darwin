using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Application.Common.DTOs;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Common;
using Darwin.Contracts.Loyalty;
using Darwin.Domain.Enums;
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
/// <para>
/// IMPORTANT: This controller is intentionally thin and contains no business rules.
/// All filtering/searching logic is handled in the Application layer.
/// </para>
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

    /// <summary>
    /// Creates a new instance of <see cref="BusinessesController"/>.
    /// </summary>
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

    /// <summary>
    /// Returns a paged list of businesses for consumer/mobile discovery.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This endpoint is Contract-first and maps to <see cref="GetBusinessesForDiscoveryHandler"/> in Application.
    /// </para>
    /// <para>
    /// We intentionally return <see cref="IActionResult"/> (instead of <c>ActionResult&lt;T&gt;</c>) to keep
    /// error shaping consistent across controllers via <see cref="ApiControllerBase"/> helper methods
    /// (<see cref="ApiControllerBase.BadRequestProblem"/> / <see cref="ApiControllerBase.NotFoundProblem"/>).
    /// Typed response shapes are still documented via <see cref="ProducesResponseTypeAttribute"/>.
    /// </para>
    /// </remarks>
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

        // Query text: keep backward compatibility by allowing either Query or Search.
        // BusinessListRequest has Query; PagedRequest has Search. If both are set, Query wins.
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
                .Select(MapToContractSummary)
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



    /// <summary>
    /// Returns the public detail view for a business.
    /// </summary>
    /// <remarks>
    /// <para>
    /// We return <see cref="IActionResult"/> to keep error shaping consistent through <see cref="ApiControllerBase"/>.
    /// </para>
    /// </remarks>
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

        var contract = MapToContractDetail(dto);
        return Ok(contract);
    }


    /// <summary>
    /// Returns public business detail along with the current user's loyalty account summary for that business (if any).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a consumer/mobile endpoint. It requires member access because it depends on the current user
    /// for the "MyAccount" portion.
    /// </para>
    /// <para>
    /// If the business is not visible/active, the handler returns <c>Ok(null)</c> and WebApi translates it to 404.
    /// </para>
    /// </remarks>
    /// <param name="id">Public business identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A combined contract model for business detail + my account.</returns>
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

        // Defensive: even on success, Value may be null (documented behavior for not visible / not found).
        var dto = result.Value;
        if (dto is null)
        {
            return NotFoundProblem("Business not found.");
        }

        if (dto.Business is null)
        {
            // Defensive: should not happen based on handler implementation, but avoid null leakage.
            return NotFoundProblem("Business not found.");
        }

        var contract = new BusinessDetailWithMyAccount
        {
            Business = MapBusinessDetail(dto.Business),
            HasAccount = dto.HasAccount,
            MyAccount = dto.MyAccount is null
                ? null
                : new LoyaltyAccountSummary
                {
                    LoyaltyAccountId = dto.MyAccount.LoyaltyAccountId,
                    BusinessId = dto.MyAccount.BusinessId,
                    BusinessName = dto.MyAccount.BusinessName,
                    Status = dto.MyAccount.Status,
                    PointsBalance = dto.MyAccount.PointsBalance,
                    LifetimePoints = dto.MyAccount.LifetimePoints,
                    LastAccrualAtUtc = dto.MyAccount.LastAccrualAtUtc
                }
        };

        return Ok(contract);
    }


    private static BusinessSummary MapToContractSummary(BusinessDiscoveryListItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new BusinessSummary
        {
            Id = dto.Id,
            Name = dto.Name ?? string.Empty,
            ShortDescription = dto.ShortDescription,
            LogoUrl = dto.PrimaryImageUrl,
            Category = dto.Category.ToString(),
            Location = dto.Coordinate is null ? null : new GeoCoordinateModel
            {
                Latitude = dto.Coordinate.Latitude,
                Longitude = dto.Coordinate.Longitude,
                AltitudeMeters = dto.Coordinate.AltitudeMeters
            },
            City = dto.City,
            IsOpenNow = dto.IsOpenNow,
            IsActive = dto.IsActive,

            // Application provides DistanceKm; API standardizes on meters.
            DistanceMeters = dto.DistanceKm.HasValue
                    ? (int?)Math.Round(dto.DistanceKm.Value * 1000.0, MidpointRounding.AwayFromZero)
                    : null,

            // TODO: Ratings
            // Rating fields are not provided by the current discovery handler output.
            Rating = null,
            RatingCount = null
        };
    }


    private static BusinessDetail MapToContractDetail(BusinessPublicDetailDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var primaryLocation = dto.Locations?.FirstOrDefault(l => l.IsPrimary) ?? dto.Locations?.FirstOrDefault();

        return new BusinessDetail
        {
            Id = dto.Id,
            Name = dto.Name ?? string.Empty,
            Category = dto.Category.ToString(),

            // Prefer explicit short description; do not fabricate long description.
            ShortDescription = dto.ShortDescription,
            Description = null,

            PrimaryImageUrl = dto.PrimaryImageUrl,
            GalleryImageUrls = dto.GalleryImageUrls,

            // Legacy combined list preserved for backward compatibility.
            ImageUrls = BuildImageUrls(dto.PrimaryImageUrl, dto.GalleryImageUrls),

            City = primaryLocation?.City,
            Coordinate = primaryLocation?.Coordinate is null ? null : new GeoCoordinateModel
            {
                Latitude = primaryLocation.Coordinate.Latitude,
                Longitude = primaryLocation.Coordinate.Longitude,
                AltitudeMeters = primaryLocation.Coordinate.AltitudeMeters
            },

            OpeningHours = null,
            PhoneE164 = dto.ContactPhoneE164,
            DefaultCurrency = string.IsNullOrWhiteSpace(dto.DefaultCurrency) ? "EUR" : dto.DefaultCurrency,
            DefaultCulture = string.IsNullOrWhiteSpace(dto.DefaultCulture) ? "de-DE" : dto.DefaultCulture,
            WebsiteUrl = dto.WebsiteUrl,
            ContactEmail = dto.ContactEmail,
            ContactPhoneE164 = dto.ContactPhoneE164,

            Locations = (dto.Locations ?? new List<BusinessPublicLocationDto>())
                .Select(MapToContractLocation)
                .ToList(),

            LoyaltyProgram = null,
            LoyaltyProgramPublic = dto.LoyaltyProgram is null ? null : MapToContractLoyaltyProgramPublic(dto.LoyaltyProgram)
        };
    }

    private static BusinessLocation MapToContractLocation(BusinessPublicLocationDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new BusinessLocation
        {
            BusinessLocationId = dto.Id,
            Name = dto.Name ?? string.Empty,
            AddressLine1 = dto.AddressLine1,
            AddressLine2 = dto.AddressLine2,
            City = dto.City,
            Region = dto.Region,
            CountryCode = dto.CountryCode,
            PostalCode = dto.PostalCode,
            Coordinate = dto.Coordinate is null ? null : new GeoCoordinateModel
            {
                Latitude = dto.Coordinate.Latitude,
                Longitude = dto.Coordinate.Longitude,
                AltitudeMeters = dto.Coordinate.AltitudeMeters
            },
            IsPrimary = dto.IsPrimary,
            OpeningHoursJson = dto.OpeningHoursJson
        };
    }

    private static LoyaltyProgramPublic MapToContractLoyaltyProgramPublic(LoyaltyProgramPublicDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new LoyaltyProgramPublic
        {
            Id = dto.Id,
            BusinessId = dto.BusinessId,
            Name = dto.Name ?? string.Empty,
            IsActive = dto.IsActive,
            RewardTiers = (dto.RewardTiers ?? new List<LoyaltyRewardTierPublicDto>())
                .Select(t => new LoyaltyRewardTierPublic
                {
                    Id = t.Id,
                    PointsRequired = t.PointsRequired,
                    RewardType = t.RewardType.ToString(),
                    RewardValue = t.RewardValue,
                    Description = t.Description,
                    AllowSelfRedemption = t.AllowSelfRedemption
                })
                .ToList()
        };
    }

    private static IReadOnlyList<string> BuildImageUrls(string? primaryImageUrl, List<string>? galleryImageUrls)
    {
        var list = new List<string>(capacity: 1 + (galleryImageUrls?.Count ?? 0));

        if (!string.IsNullOrWhiteSpace(primaryImageUrl))
        {
            list.Add(primaryImageUrl.Trim());
        }

        if (galleryImageUrls is { Count: > 0 })
        {
            foreach (var url in galleryImageUrls)
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    list.Add(url.Trim());
                }
            }
        }

        return list;
    }

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

    /// <summary>
    /// Maps contract proximity fields to Application proximity fields.
    /// Contract uses meters; Application uses kilometers.
    /// </summary>
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

        // Convert meters -> km for Application.
        var radiusKm = radiusMeters.HasValue
            ? radiusMeters.Value / 1000.0
            : (double?)null;

        return (coordinate, radiusKm, null);
    }
}
