using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Contracts.Businesses;
using Darwin.Application.Settings.DTOs;
using Darwin.Domain.Enums;
using Darwin.WebApi.Controllers.Businesses;
using Darwin.WebApi.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.WebApi.Controllers.Member;

/// <summary>
/// Member-scoped business endpoints for onboarding and authenticated social/account interactions.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/member/businesses")]
public sealed class MemberBusinessesController : ApiControllerBase
{
    private readonly GetBusinessPublicDetailWithMyAccountHandler _getBusinessPublicDetailWithMyAccountHandler;
    private readonly GetBusinessEngagementForMemberHandler _getBusinessEngagementForMemberHandler;
    private readonly ToggleBusinessLikeHandler _toggleBusinessLikeHandler;
    private readonly ToggleBusinessFavoriteHandler _toggleBusinessFavoriteHandler;
    private readonly UpsertBusinessReviewHandler _upsertBusinessReviewHandler;
    private readonly CreateBusinessHandler _createBusinessHandler;
    private readonly CreateBusinessMemberHandler _createBusinessMemberHandler;

    public MemberBusinessesController(
        GetBusinessPublicDetailWithMyAccountHandler getBusinessPublicDetailWithMyAccountHandler,
        GetBusinessEngagementForMemberHandler getBusinessEngagementForMemberHandler,
        ToggleBusinessLikeHandler toggleBusinessLikeHandler,
        ToggleBusinessFavoriteHandler toggleBusinessFavoriteHandler,
        UpsertBusinessReviewHandler upsertBusinessReviewHandler,
        CreateBusinessHandler createBusinessHandler,
        CreateBusinessMemberHandler createBusinessMemberHandler)
    {
        _getBusinessPublicDetailWithMyAccountHandler = getBusinessPublicDetailWithMyAccountHandler ?? throw new ArgumentNullException(nameof(getBusinessPublicDetailWithMyAccountHandler));
        _getBusinessEngagementForMemberHandler = getBusinessEngagementForMemberHandler ?? throw new ArgumentNullException(nameof(getBusinessEngagementForMemberHandler));
        _toggleBusinessLikeHandler = toggleBusinessLikeHandler ?? throw new ArgumentNullException(nameof(toggleBusinessLikeHandler));
        _toggleBusinessFavoriteHandler = toggleBusinessFavoriteHandler ?? throw new ArgumentNullException(nameof(toggleBusinessFavoriteHandler));
        _upsertBusinessReviewHandler = upsertBusinessReviewHandler ?? throw new ArgumentNullException(nameof(upsertBusinessReviewHandler));
        _createBusinessHandler = createBusinessHandler ?? throw new ArgumentNullException(nameof(createBusinessHandler));
        _createBusinessMemberHandler = createBusinessMemberHandler ?? throw new ArgumentNullException(nameof(createBusinessMemberHandler));
    }

    [HttpPost("onboarding")]
    [HttpPost("/api/v1/businesses/onboarding")]
    [ProducesResponseType(typeof(BusinessOnboardingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status401Unauthorized)]
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

        if (!BusinessControllerConventions.TryGetCurrentUserId(User, out var userId))
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new Darwin.Contracts.Common.ProblemDetails
            {
                Status = 401,
                Title = "Unauthorized",
                Detail = "User identifier could not be resolved from the access token.",
                Instance = HttpContext.Request?.Path.Value
            });
        }

        if (!BusinessControllerConventions.TryParseBusinessCategoryKind(request.CategoryKindKey, out var categoryKind, out var categoryError))
        {
            return BadRequestProblem(categoryError);
        }

        var createBusinessDto = new BusinessCreateDto
        {
            Name = request.Name,
            LegalName = BusinessControllerConventions.NormalizeNullable(request.LegalName),
            TaxId = BusinessControllerConventions.NormalizeNullable(request.TaxId),
            ShortDescription = BusinessControllerConventions.NormalizeNullable(request.ShortDescription),
            WebsiteUrl = BusinessControllerConventions.NormalizeNullable(request.WebsiteUrl),
            ContactEmail = BusinessControllerConventions.NormalizeNullable(request.ContactEmail),
            ContactPhoneE164 = BusinessControllerConventions.NormalizeNullable(request.ContactPhoneE164),
            Category = categoryKind ?? BusinessCategoryKind.Unknown,
            DefaultCurrency = BusinessControllerConventions.NormalizeNullable(request.DefaultCurrency) ?? SiteSettingDto.DefaultCurrencyDefault,
            DefaultCulture = BusinessControllerConventions.NormalizeNullable(request.DefaultCulture) ?? SiteSettingDto.DefaultCultureDefault,
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

        Guid businessMemberId;
        try
        {
            businessMemberId = await _createBusinessMemberHandler.HandleAsync(new BusinessMemberCreateDto
            {
                BusinessId = businessId,
                UserId = userId,
                Role = BusinessMemberRole.Owner,
                IsActive = true
            }, ct).ConfigureAwait(false);
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

    [HttpGet("{id:guid}/with-my-account")]
    [HttpGet("/api/v1/businesses/{id:guid}/with-my-account")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(BusinessDetailWithMyAccount), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWithMyAccountAsync([FromRoute] Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return NotFoundProblem("Business not found.");
        }

        var result = await _getBusinessPublicDetailWithMyAccountHandler.HandleAsync(id, ct).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return ProblemFromResult(result);
        }

        var dto = result.Value;
        if (dto is null || dto.Business is null)
        {
            return NotFoundProblem("Business not found.");
        }

        return Ok(BusinessContractsMapper.ToContract(dto));
    }

    [HttpGet("{id:guid}/engagement/my")]
    [HttpGet("/api/v1/businesses/{id:guid}/engagement/my")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(BusinessEngagementSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMyEngagementAsync([FromRoute] Guid id, CancellationToken ct = default)
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
        return Ok(new BusinessEngagementSummaryResponse
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
        });
    }

    [HttpPut("{id:guid}/likes/toggle")]
    [HttpPut("/api/v1/businesses/{id:guid}/likes/toggle")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(ToggleBusinessReactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
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

    [HttpPut("{id:guid}/favorites/toggle")]
    [HttpPut("/api/v1/businesses/{id:guid}/favorites/toggle")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(ToggleBusinessReactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
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

    [HttpPut("{id:guid}/my-review")]
    [HttpPut("/api/v1/businesses/{id:guid}/my-review")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertMyReviewAsync([FromRoute] Guid id, [FromBody] UpsertBusinessReviewRequest? request, CancellationToken ct = default)
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
}
