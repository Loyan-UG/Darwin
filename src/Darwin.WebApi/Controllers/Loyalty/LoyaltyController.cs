using Darwin.Application;
using Darwin.Application.Loyalty.Campaigns;
using Darwin.Application.Loyalty.Commands;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Queries;
using Darwin.Contracts.Common;
using Darwin.Contracts.Loyalty;
using Darwin.WebApi.Controllers.Businesses;
using Darwin.WebApi.Mappers;
using Darwin.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Darwin.WebApi.Controllers.Loyalty;

/// <summary>
/// Member-facing loyalty endpoints used by the consumer mobile application and member portal.
/// </summary>
[ApiController]
[Route("api/v1/member/loyalty")]
[Route("api/v1/loyalty")]
[Authorize]
public sealed class LoyaltyController : ApiControllerBase
{
    private readonly PrepareScanSessionHandler _prepareScanSessionHandler;
    private readonly GetMyLoyaltyAccountsHandler _getMyLoyaltyAccountsHandler;
    private readonly GetMyLoyaltyOverviewHandler _getMyLoyaltyOverviewHandler;
    private readonly GetMyLoyaltyHistoryHandler _getMyLoyaltyHistoryHandler;
    private readonly GetMyLoyaltyBusinessDashboardHandler _getMyLoyaltyBusinessDashboardHandler;
    private readonly GetMyLoyaltyAccountForBusinessHandler _getMyLoyaltyAccountForBusinessHandler;
    private readonly GetAvailableLoyaltyRewardsForBusinessHandler _getAvailableLoyaltyRewardsForBusinessHandler;
    private readonly GetMyLoyaltyBusinessesHandler _getMyLoyaltyBusinessesHandler;
    private readonly GetMyLoyaltyTimelinePageHandler _getMyLoyaltyTimelinePageHandler;
    private readonly CreateLoyaltyAccountHandler _createLoyaltyAccountHandler;
    private readonly GetMyPromotionsHandler _getMyPromotionsHandler;
    private readonly TrackPromotionInteractionHandler _trackPromotionInteractionHandler;
    private readonly ILoyaltyPresentationService _presentationService;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoyaltyController"/> class.
    /// </summary>
    public LoyaltyController(
        PrepareScanSessionHandler prepareScanSessionHandler,
        GetMyLoyaltyAccountsHandler getMyLoyaltyAccountsHandler,
        GetMyLoyaltyOverviewHandler getMyLoyaltyOverviewHandler,
        GetMyLoyaltyHistoryHandler getMyLoyaltyHistoryHandler,
        GetMyLoyaltyBusinessDashboardHandler getMyLoyaltyBusinessDashboardHandler,
        GetMyLoyaltyAccountForBusinessHandler getMyLoyaltyAccountForBusinessHandler,
        GetAvailableLoyaltyRewardsForBusinessHandler getAvailableLoyaltyRewardsForBusinessHandler,
        GetMyLoyaltyBusinessesHandler getMyLoyaltyBusinessesHandler,
        GetMyPromotionsHandler getMyPromotionsHandler,
        TrackPromotionInteractionHandler trackPromotionInteractionHandler,
        GetMyLoyaltyTimelinePageHandler getMyLoyaltyTimelinePageHandler,
        CreateLoyaltyAccountHandler createLoyaltyAccountHandler,
        ILoyaltyPresentationService presentationService,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _prepareScanSessionHandler = prepareScanSessionHandler ?? throw new ArgumentNullException(nameof(prepareScanSessionHandler));
        _getMyLoyaltyAccountsHandler = getMyLoyaltyAccountsHandler ?? throw new ArgumentNullException(nameof(getMyLoyaltyAccountsHandler));
        _getMyLoyaltyOverviewHandler = getMyLoyaltyOverviewHandler ?? throw new ArgumentNullException(nameof(getMyLoyaltyOverviewHandler));
        _getMyLoyaltyHistoryHandler = getMyLoyaltyHistoryHandler ?? throw new ArgumentNullException(nameof(getMyLoyaltyHistoryHandler));
        _getMyLoyaltyBusinessDashboardHandler = getMyLoyaltyBusinessDashboardHandler ?? throw new ArgumentNullException(nameof(getMyLoyaltyBusinessDashboardHandler));
        _getMyLoyaltyAccountForBusinessHandler = getMyLoyaltyAccountForBusinessHandler ?? throw new ArgumentNullException(nameof(getMyLoyaltyAccountForBusinessHandler));
        _getAvailableLoyaltyRewardsForBusinessHandler = getAvailableLoyaltyRewardsForBusinessHandler ?? throw new ArgumentNullException(nameof(getAvailableLoyaltyRewardsForBusinessHandler));
        _getMyLoyaltyBusinessesHandler = getMyLoyaltyBusinessesHandler ?? throw new ArgumentNullException(nameof(getMyLoyaltyBusinessesHandler));
        _getMyPromotionsHandler = getMyPromotionsHandler ?? throw new ArgumentNullException(nameof(getMyPromotionsHandler));
        _trackPromotionInteractionHandler = trackPromotionInteractionHandler ?? throw new ArgumentNullException(nameof(trackPromotionInteractionHandler));
        _getMyLoyaltyTimelinePageHandler = getMyLoyaltyTimelinePageHandler ?? throw new ArgumentNullException(nameof(getMyLoyaltyTimelinePageHandler));
        _createLoyaltyAccountHandler = createLoyaltyAccountHandler ?? throw new ArgumentNullException(nameof(createLoyaltyAccountHandler));
        _presentationService = presentationService ?? throw new ArgumentNullException(nameof(presentationService));
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    /// <summary>
    /// Prepares a member scan session for a selected business and optional reward redemption payload.
    /// </summary>
    [HttpPost("scan/prepare")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(PrepareScanSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PrepareScanSessionAsync(
        [FromBody] PrepareScanSessionRequest? request,
        CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        if (request.BusinessId == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["BusinessIdRequired"]);
        }

        var dto = new PrepareScanSessionDto
        {
            BusinessId = request.BusinessId,
            BusinessLocationId = request.BusinessLocationId,
            Mode = LoyaltyContractsMapper.ToDomain(request.Mode),
            SelectedRewardTierIds = request.SelectedRewardTierIds?
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList() ?? new List<Guid>(),
            DeviceId = NormalizeText(request.DeviceId)
        };

        var result = await _prepareScanSessionHandler.HandleAsync(dto, ct).ConfigureAwait(false);
        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result);
        }

        IReadOnlyList<LoyaltyRewardSummary> selectedRewards = Array.Empty<LoyaltyRewardSummary>();
        if (result.Value.Mode == Darwin.Domain.Enums.LoyaltyScanMode.Redemption &&
            result.Value.SelectedRewardTierIds is { Count: > 0 })
        {
            var enrichResult = await _presentationService
                .EnrichSelectedRewardsAsync(request.BusinessId, result.Value.SelectedRewardTierIds, failIfMissing: true, ct)
                .ConfigureAwait(false);

            if (!enrichResult.Succeeded)
            {
                return ProblemFromResult(enrichResult);
            }

            selectedRewards = enrichResult.Value ?? Array.Empty<LoyaltyRewardSummary>();
        }

        return Ok(new PrepareScanSessionResponse
        {
            ScanSessionToken = result.Value.ScanSessionToken,
            Mode = LoyaltyContractsMapper.ToContract(result.Value.Mode),
            ExpiresAtUtc = result.Value.ExpiresAtUtc,
            CurrentPointsBalance = result.Value.CurrentPointsBalance,
            SelectedRewards = selectedRewards
        });
    }

    /// <summary>
    /// Returns loyalty accounts for the current authenticated member.
    /// </summary>
    [HttpGet("my/accounts")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(IReadOnlyList<LoyaltyAccountSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyAccountsAsync(CancellationToken ct = default)
    {
        var result = await _getMyLoyaltyAccountsHandler.HandleAsync(ct).ConfigureAwait(false);
        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result);
        }

        return Ok(result.Value.Select(LoyaltyContractsMapper.ToContract).ToList());
    }

    /// <summary>
    /// Returns an aggregated loyalty overview spanning all businesses for the current authenticated member.
    /// </summary>
    [HttpGet("my/overview")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(MyLoyaltyOverviewResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOverviewAsync([FromQuery] string? culture = null, CancellationToken ct = default)
    {
        var result = await _getMyLoyaltyOverviewHandler
            .HandleAsync(BusinessControllerConventions.NormalizeNullable(culture), ct)
            .ConfigureAwait(false);
        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result);
        }

        return Ok(LoyaltyContractsMapper.ToContract(result.Value));
    }

    /// <summary>
    /// Returns points transaction history for the current member within a single business context.
    /// </summary>
    [HttpGet("my/history/{businessId:guid}")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(IReadOnlyList<PointsTransaction>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMyHistoryAsync(Guid businessId, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["BusinessIdRequired"]);
        }

        var result = await _getMyLoyaltyHistoryHandler.HandleAsync(businessId, ct).ConfigureAwait(false);
        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result);
        }

        return Ok(result.Value.Select(LoyaltyContractsMapper.ToContract).ToList());
    }

    /// <summary>
    /// Returns the current member loyalty account for the specified business.
    /// </summary>
    [HttpGet("account/{businessId:guid}")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(LoyaltyAccountSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentAccountForBusinessAsync(Guid businessId, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["BusinessIdRequired"]);
        }

        var result = await _getMyLoyaltyAccountForBusinessHandler.HandleAsync(businessId, ct).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return ProblemFromResult(result);
        }

        return result.Value is null
            ? NotFoundProblem(_validationLocalizer["LoyaltyAccountNotFoundForSpecifiedBusinessAndUser"])
            : Ok(LoyaltyContractsMapper.ToContract(result.Value));
    }

    /// <summary>
    /// Returns a business-scoped loyalty dashboard for the current member.
    /// </summary>
    [HttpGet("business/{businessId:guid}/dashboard")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(MyLoyaltyBusinessDashboard), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBusinessDashboardAsync(
        Guid businessId,
        [FromQuery] string? culture = null,
        CancellationToken ct = default)
    {
        if (businessId == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["BusinessIdRequired"]);
        }

        var result = await _getMyLoyaltyBusinessDashboardHandler
            .HandleAsync(businessId, BusinessControllerConventions.NormalizeNullable(culture), ct)
            .ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return ProblemFromResult(result);
        }

        return result.Value is null
            ? NotFoundProblem(_validationLocalizer["LoyaltyDashboardNotFoundForSpecifiedBusinessAndUser"])
            : Ok(LoyaltyContractsMapper.ToContract(result.Value));
    }

    /// <summary>
    /// Returns the rewards currently available to the member for the specified business.
    /// </summary>
    [HttpGet("business/{businessId:guid}/rewards")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(IReadOnlyList<LoyaltyRewardSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRewardsForBusinessAsync(
        Guid businessId,
        [FromQuery] string? culture = null,
        CancellationToken ct = default)
    {
        if (businessId == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["BusinessIdRequired"]);
        }

        var result = await _getAvailableLoyaltyRewardsForBusinessHandler
            .HandleAsync(businessId, BusinessControllerConventions.NormalizeNullable(culture), ct)
            .ConfigureAwait(false);
        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result);
        }

        return Ok(result.Value.Select(LoyaltyContractsMapper.ToContract).ToList());
    }

    /// <summary>
    /// Returns the paged list of businesses where the current member has a loyalty relationship.
    /// </summary>
    [HttpGet("my/businesses")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(MyLoyaltyBusinessesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMyBusinessesAsync(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] bool? includeInactiveBusinesses,
        [FromQuery] string? culture,
        CancellationToken ct = default)
    {
        var normalizedPage = page.GetValueOrDefault(1);
        if (normalizedPage <= 0)
        {
            return BadRequestProblem(_validationLocalizer["PageMustBePositiveInteger"]);
        }

        var normalizedPageSize = pageSize.GetValueOrDefault(20);
        if (normalizedPageSize <= 0 || normalizedPageSize > 200)
        {
            return BadRequestProblem(_validationLocalizer["PageSizeMustBeBetween1And200"]);
        }

        var request = new MyLoyaltyBusinessListRequestDto
        {
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            IncludeInactiveBusinesses = includeInactiveBusinesses.GetValueOrDefault(false),
            Culture = BusinessControllerConventions.NormalizeNullable(culture)
        };

        var (items, total) = await _getMyLoyaltyBusinessesHandler.HandleAsync(request, ct).ConfigureAwait(false);
        var safeItems = items ?? new List<MyLoyaltyBusinessListItemDto>();

        return Ok(new MyLoyaltyBusinessesResponse
        {
            Total = total,
            Items = safeItems.Select(LoyaltyContractsMapper.ToContract).ToList(),
            Request = new PagedRequest { Page = normalizedPage, PageSize = normalizedPageSize, Search = null }
        });
    }

    /// <summary>
    /// Returns personalized loyalty promotion cards for the current member.
    /// </summary>
    [HttpPost("my/promotions")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(MyPromotionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMyPromotionsAsync([FromBody] MyPromotionsRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        if (request.BusinessId.HasValue && request.BusinessId.Value == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["BusinessIdValidWhenProvided"]);
        }

        var result = await _getMyPromotionsHandler
            .HandleAsync(new MyPromotionsDto
            {
                BusinessId = request.BusinessId,
                MaxItems = request.MaxItems,
                Culture = BusinessControllerConventions.NormalizeNullable(request.Culture),
                Policy = request.Policy is null
                    ? null
                    : new PromotionFeedPolicyDto
                    {
                        EnableDeduplication = request.Policy.EnableDeduplication,
                        MaxCards = request.Policy.MaxCards,
                        FrequencyWindowMinutes = request.Policy.FrequencyWindowMinutes,
                        SuppressionWindowMinutes = request.Policy.SuppressionWindowMinutes
                    }
            }, ct)
            .ConfigureAwait(false);

        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result);
        }

        return Ok(new MyPromotionsResponse
        {
            AppliedPolicy = new PromotionFeedPolicy
            {
                EnableDeduplication = result.Value.AppliedPolicy.EnableDeduplication,
                MaxCards = result.Value.AppliedPolicy.MaxCards,
                FrequencyWindowMinutes = result.Value.AppliedPolicy.FrequencyWindowMinutes,
                SuppressionWindowMinutes = result.Value.AppliedPolicy.SuppressionWindowMinutes
            },
            Diagnostics = new PromotionFeedDiagnostics
            {
                InitialCandidates = result.Value.Diagnostics.InitialCandidates,
                SuppressedByFrequency = result.Value.Diagnostics.SuppressedByFrequency,
                Deduplicated = result.Value.Diagnostics.Deduplicated,
                TrimmedByCap = result.Value.Diagnostics.TrimmedByCap,
                FinalCount = result.Value.Diagnostics.FinalCount
            },
            Items = result.Value.Items
                .Select(x => new PromotionFeedItem
                {
                    BusinessId = x.BusinessId,
                    BusinessName = x.BusinessName,
                    Title = x.Title,
                    Description = x.Description,
                    CtaKind = x.CtaKind,
                    Priority = x.Priority,
                    CampaignId = x.CampaignId,
                    CampaignState = x.CampaignState,
                    StartsAtUtc = x.StartsAtUtc,
                    EndsAtUtc = x.EndsAtUtc,
                    EligibilityRules = x.EligibilityRules.Select(rule => new PromotionEligibilityRule
                    {
                        AudienceKind = rule.AudienceKind,
                        MinPoints = rule.MinPoints,
                        MaxPoints = rule.MaxPoints,
                        TierKey = rule.TierKey,
                        Note = rule.Note
                    }).ToList()
                })
                .ToList()
        });
    }

    /// <summary>
    /// Records a promotion interaction event for analytics and suppression logic.
    /// </summary>
    [HttpPost("my/promotions/track")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TrackPromotionInteractionAsync(
        [FromBody] TrackPromotionInteractionRequest? request,
        CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        if (request.BusinessId == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["BusinessIdRequired"]);
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequestProblem(_validationLocalizer["TitleRequired"]);
        }

        var result = await _trackPromotionInteractionHandler
            .HandleAsync(new TrackPromotionInteractionDto
            {
                BusinessId = request.BusinessId,
                BusinessName = NormalizeText(request.BusinessName) ?? string.Empty,
                Title = request.Title.Trim(),
                CtaKind = NormalizeText(request.CtaKind) ?? string.Empty,
                EventType = MapPromotionInteractionEventType(request.EventType),
                OccurredAtUtc = request.OccurredAtUtc
            }, ct)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return ProblemFromResult(result);
        }

        return NoContent();
    }

    /// <summary>
    /// Returns a cursor-paged unified loyalty timeline for the current member.
    /// </summary>
    [HttpPost("my/timeline")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(GetMyLoyaltyTimelinePageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMyLoyaltyTimelinePageAsync(
        [FromBody] GetMyLoyaltyTimelinePageRequest? request,
        CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        if (!request.BusinessId.HasValue || request.BusinessId.Value == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["BusinessIdRequired"]);
        }

        if ((request.BeforeAtUtc is null) != (request.BeforeId is null))
        {
            return BadRequestProblem(_validationLocalizer["InvalidTimelineCursor"]);
        }

        var dto = new GetMyLoyaltyTimelinePageDto
        {
            BusinessId = request.BusinessId.Value,
            PageSize = request.PageSize,
            BeforeAtUtc = request.BeforeAtUtc,
            BeforeId = request.BeforeId,
            Culture = BusinessControllerConventions.NormalizeNullable(request.Culture)
        };

        var result = await _getMyLoyaltyTimelinePageHandler.HandleAsync(dto, ct).ConfigureAwait(false);
        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result);
        }

        return Ok(new GetMyLoyaltyTimelinePageResponse
        {
            Items = (result.Value.Items ?? Array.Empty<LoyaltyTimelineEntryDto>())
                .Select(LoyaltyContractsMapper.ToContract)
                .ToList(),
            NextBeforeAtUtc = result.Value.NextBeforeAtUtc,
            NextBeforeId = result.Value.NextBeforeId
        });
    }

    /// <summary>
    /// Creates or returns the current member loyalty account for the specified business.
    /// </summary>
    [HttpPost("account/{businessId:guid}/join")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(LoyaltyAccountSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> JoinLoyaltyAsync(
        [FromRoute] Guid businessId,
        [FromBody] JoinLoyaltyRequest? request,
        CancellationToken ct = default)
    {
        if (businessId == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["BusinessIdRequired"]);
        }

        var result = await _createLoyaltyAccountHandler
            .HandleAsync(businessId, request?.BusinessLocationId, ct)
            .ConfigureAwait(false);

        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result);
        }

        return Ok(LoyaltyContractsMapper.ToContract(result.Value));
    }

    /// <summary>
    /// Returns the next attainable reward for the current member within the specified business.
    /// </summary>
    [HttpGet("account/{businessId:guid}/next-reward")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    [ProducesResponseType(typeof(LoyaltyRewardSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetNextRewardAsync([FromRoute] Guid businessId, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["BusinessIdRequired"]);
        }

        var accountResult = await _getMyLoyaltyAccountForBusinessHandler.HandleAsync(businessId, ct).ConfigureAwait(false);
        if (!accountResult.Succeeded)
        {
            return ProblemFromResult(accountResult);
        }

        var account = accountResult.Value;
        if (account is null)
        {
            return NotFoundProblem(_validationLocalizer["LoyaltyAccountNotFoundForSpecifiedBusinessAndUser"]);
        }

        var availableResult = await _getAvailableLoyaltyRewardsForBusinessHandler.HandleAsync(businessId, ct).ConfigureAwait(false);
        if (!availableResult.Succeeded)
        {
            return ProblemFromResult(availableResult);
        }

        var candidate = (availableResult.Value ?? Array.Empty<LoyaltyRewardSummaryDto>())
            .Where(r => r.RequiredPoints > account.PointsBalance && r.IsActive && r.IsSelectable)
            .OrderBy(r => r.RequiredPoints)
            .FirstOrDefault();

        return candidate is null
            ? NoContent()
            : Ok(LoyaltyContractsMapper.ToContract(candidate));
    }

    /// <summary>
    /// Maps contract interaction events to the application-layer enum.
    /// </summary>
    private static Darwin.Application.Loyalty.DTOs.PromotionInteractionEventType MapPromotionInteractionEventType(
        Darwin.Contracts.Loyalty.PromotionInteractionEventType eventType)
    {
        return eventType switch
        {
            Darwin.Contracts.Loyalty.PromotionInteractionEventType.Open => Darwin.Application.Loyalty.DTOs.PromotionInteractionEventType.Open,
            Darwin.Contracts.Loyalty.PromotionInteractionEventType.Claim => Darwin.Application.Loyalty.DTOs.PromotionInteractionEventType.Claim,
            _ => Darwin.Application.Loyalty.DTOs.PromotionInteractionEventType.Impression
        };
    }

    private static string? NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
