using Darwin.Application.Businesses.Commands;
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
using System.Security.Claims;
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
    private readonly GetBusinessEngagementForMemberHandler _getBusinessEngagementForMemberHandler;
    private readonly ToggleBusinessLikeHandler _toggleBusinessLikeHandler;
    private readonly ToggleBusinessFavoriteHandler _toggleBusinessFavoriteHandler;
    private readonly UpsertBusinessReviewHandler _upsertBusinessReviewHandler;
    private readonly CreateBusinessHandler _createBusinessHandler;
    private readonly CreateBusinessMemberHandler _createBusinessMemberHandler;


    public BusinessesController(
        GetBusinessesForDiscoveryHandler getBusinessesForDiscovery,
        GetBusinessPublicDetailHandler getBusinessPublicDetail,
        GetBusinessesForMapDiscoveryHandler getBusinessesForMapDiscoveryHandler,
        GetBusinessPublicDetailWithMyAccountHandler getBusinessPublicDetailWithMyAccountHandler,
        GetBusinessEngagementForMemberHandler getBusinessEngagementForMemberHandler,
        ToggleBusinessLikeHandler toggleBusinessLikeHandler,
        ToggleBusinessFavoriteHandler toggleBusinessFavoriteHandler,
        UpsertBusinessReviewHandler upsertBusinessReviewHandler,
        CreateBusinessHandler createBusinessHandler,
        CreateBusinessMemberHandler createBusinessMemberHandler)
    {
        _getBusinessesForDiscovery = getBusinessesForDiscovery ?? throw new ArgumentNullException(nameof(getBusinessesForDiscovery));
        _getBusinessPublicDetail = getBusinessPublicDetail ?? throw new ArgumentNullException(nameof(getBusinessPublicDetail));
        _getBusinessesForMapDiscoveryHandler = getBusinessesForMapDiscoveryHandler ?? throw new ArgumentNullException(nameof(getBusinessesForMapDiscoveryHandler));
        _getBusinessPublicDetailWithMyAccountHandler = getBusinessPublicDetailWithMyAccountHandler ?? throw new ArgumentNullException(nameof(getBusinessPublicDetailWithMyAccountHandler));
        _getBusinessEngagementForMemberHandler = getBusinessEngagementForMemberHandler ?? throw new ArgumentNullException(nameof(getBusinessEngagementForMemberHandler));
        _toggleBusinessLikeHandler = toggleBusinessLikeHandler ?? throw new ArgumentNullException(nameof(toggleBusinessLikeHandler));
        _toggleBusinessFavoriteHandler = toggleBusinessFavoriteHandler ?? throw new ArgumentNullException(nameof(toggleBusinessFavoriteHandler));
        _upsertBusinessReviewHandler = upsertBusinessReviewHandler ?? throw new ArgumentNullException(nameof(upsertBusinessReviewHandler));
        _createBusinessHandler = createBusinessHandler ?? throw new ArgumentNullException(nameof(createBusinessHandler));
        _createBusinessMemberHandler = createBusinessMemberHandler ?? throw new ArgumentNullException(nameof(createBusinessMemberHandler));
    }

    /// <summary>
    /// Self-service business onboarding endpoint.
    /// Creates a business and links the authenticated user as owner.
    /// </summary>
    [HttpPost("onboarding")]
    [Authorize]
    [ProducesResponseType(typeof(BusinessOnboardingResponse), 200)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), 400)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), 401)]
    public async Task<IActionResult> OnboardAsync([FromBody] BusinessOnboardingRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequestProblem("Business name is required.");
        }

        if (!TryGetCurrentUserId(User, out var userId))
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new Darwin.Contracts.Common.ProblemDetails
            {
                Status = 401,
                Title = "Unauthorized",
                Detail = "User identifier could not be resolved from the access token.",
                Instance = HttpContext.Request?.Path.Value
            });
        }

        if (!TryParseBusinessCategoryKind(request.CategoryKindKey, out var categoryKind, out var categoryError))
        {
            return BadRequestProblem(categoryError);
        }

        var createBusinessDto = new BusinessCreateDto
        {
            Name = request.Name,
            LegalName = NormalizeNullable(request.LegalName),
            TaxId = NormalizeNullable(request.TaxId),
            ShortDescription = NormalizeNullable(request.ShortDescription),
            WebsiteUrl = NormalizeNullable(request.WebsiteUrl),
            ContactEmail = NormalizeNullable(request.ContactEmail),
            ContactPhoneE164 = NormalizeNullable(request.ContactPhoneE164),
            Category = categoryKind ?? BusinessCategoryKind.Unknown,
            DefaultCurrency = NormalizeNullable(request.DefaultCurrency) ?? "EUR",
            DefaultCulture = NormalizeNullable(request.DefaultCulture) ?? "de-DE",
            IsActive = true
        };

        Guid businessId;
        try
        {
            businessId = await _createBusinessHandler.HandleAsync(createBusinessDto, ct).ConfigureAwait(false);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequestProblem("Invalid business onboarding payload.", ex.Message);
        }

        var createMemberDto = new BusinessMemberCreateDto
        {
            BusinessId = businessId,
            UserId = userId,
            Role = BusinessMemberRole.Owner,
            IsActive = true
        };

        Guid businessMemberId;
        try
        {
            businessMemberId = await _createBusinessMemberHandler.HandleAsync(createMemberDto, ct).ConfigureAwait(false);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequestProblem("Business owner membership could not be created.", ex.Message);
        }

        return Ok(new BusinessOnboardingResponse
        {
            BusinessId = businessId,
            BusinessMemberId = businessMemberId
        });
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

        var (minRating, ratingError) = TryNormalizeMinRating(request.MinRating);
        if (ratingError is not null)
        {
            return BadRequestProblem(ratingError);
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
            MinRating = minRating,
            HasActiveLoyaltyProgram = request.HasActiveLoyaltyProgram,
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







    /// <summary>
    /// Returns a member-scoped engagement snapshot for the target business.
    /// The payload contains aggregate counters (likes/favorites/ratings),
    /// current-user states, my review (if any), and a short recent reviews list.
    /// </summary>
    /// <remarks>
    /// This endpoint is intentionally read-only and optimized for Business Detail screen bootstrap
    /// so mobile clients can render social proof and user state in one call.
    /// </remarks>
    [HttpGet("{id:guid}/engagement/my")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(BusinessEngagementSummaryResponse), 200)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), 400)]
    public async Task<IActionResult> GetMyEngagementAsync(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequestProblem("Business id must be a non-empty GUID.");
        }

        var result = await _getBusinessEngagementForMemberHandler.HandleAsync(id, ct).ConfigureAwait(false);
        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result);
        }

        var dto = result.Value;
        var response = new BusinessEngagementSummaryResponse
        {
            BusinessId = dto.BusinessId,
            LikeCount = dto.LikeCount,
            FavoriteCount = dto.FavoriteCount,
            RatingCount = dto.RatingCount,
            RatingAverage = dto.RatingAverage,
            IsLikedByMe = dto.IsLikedByMe,
            IsFavoritedByMe = dto.IsFavoritedByMe,
            MyReview = dto.MyReview is null
                ? null
                : new BusinessReviewItem
                {
                    Id = dto.MyReview.Id,
                    UserId = dto.MyReview.UserId,
                    AuthorName = dto.MyReview.AuthorName,
                    Rating = dto.MyReview.Rating,
                    Comment = dto.MyReview.Comment,
                    CreatedAtUtc = dto.MyReview.CreatedAtUtc
                },
            RecentReviews = dto.RecentReviews.Select(r => new BusinessReviewItem
            {
                Id = r.Id,
                UserId = r.UserId,
                AuthorName = r.AuthorName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAtUtc = r.CreatedAtUtc
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Toggles current member like state for the requested business.
    /// Creates a like when missing, otherwise removes existing like.
    /// </summary>
    /// <remarks>
    /// Response includes both final state and updated total count to support immediate UI refresh
    /// without requiring an additional read request.
    /// </remarks>
    [HttpPut("{id:guid}/likes/toggle")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(ToggleBusinessReactionResponse), 200)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), 400)]
    public async Task<IActionResult> ToggleLikeAsync([FromRoute] Guid id, CancellationToken ct = default)
    {
        var result = await _toggleBusinessLikeHandler.HandleAsync(id, ct).ConfigureAwait(false);
        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result);
        }

        return Ok(new ToggleBusinessReactionResponse
        {
            IsActive = result.Value.IsActive,
            TotalCount = result.Value.TotalCount
        });
    }

    /// <summary>
    /// Toggles current member favorite state for the requested business.
    /// Creates a favorite when missing, otherwise removes existing favorite relation.
    /// </summary>
    /// <remarks>
    /// Response returns final boolean state plus updated count for optimistic client updates.
    /// </remarks>
    [HttpPut("{id:guid}/favorites/toggle")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(ToggleBusinessReactionResponse), 200)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), 400)]
    public async Task<IActionResult> ToggleFavoriteAsync([FromRoute] Guid id, CancellationToken ct = default)
    {
        var result = await _toggleBusinessFavoriteHandler.HandleAsync(id, ct).ConfigureAwait(false);
        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result);
        }

        return Ok(new ToggleBusinessReactionResponse
        {
            IsActive = result.Value.IsActive,
            TotalCount = result.Value.TotalCount
        });
    }

    /// <summary>
    /// Creates or updates current member review for the requested business.
    /// </summary>
    /// <remarks>
    /// Uses upsert semantics to keep one active review per user/business.
    /// Returns 204 on success to keep payload minimal and deterministic.
    /// </remarks>
    [HttpPut("{id:guid}/my-review")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), 400)]
    public async Task<IActionResult> UpsertMyReviewAsync(
        [FromRoute] Guid id,
        [FromBody] UpsertBusinessReviewRequest? request,
        CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
        }

        var result = await _upsertBusinessReviewHandler
            .HandleAsync(id, new UpsertBusinessReviewDto
            {
                Rating = request.Rating,
                Comment = request.Comment
            }, ct)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return ProblemFromResult(result);
        }

        return NoContent();
    }









    // NOTE: GetBusinessesForMapDiscoveryHandler endpoint isn't shown in this controller file snippet,
    // but if/when you add it, it should also map using a dedicated mapper to keep the controller thin.

    private static string? NormalizeNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool TryGetCurrentUserId(ClaimsPrincipal user, out Guid userId)
    {
        userId = Guid.Empty;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var id =
            user.FindFirstValue(ClaimTypes.NameIdentifier) ??
            user.FindFirstValue("sub") ??
            user.FindFirstValue("uid");

        if (!Guid.TryParse(id, out var parsed))
        {
            return false;
        }

        userId = parsed;
        return true;
    }

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

    private static (double? Value, string? Error) TryNormalizeMinRating(double? minRating)
    {
        if (!minRating.HasValue)
        {
            return (null, null);
        }

        if (double.IsNaN(minRating.Value) || double.IsInfinity(minRating.Value))
        {
            return (null, "MinRating must be a finite number between 0 and 5.");
        }

        if (minRating.Value < 0 || minRating.Value > 5)
        {
            return (null, "MinRating must be between 0 and 5.");
        }

        return (minRating.Value, null);
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