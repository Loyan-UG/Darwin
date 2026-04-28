using Darwin.Application.Loyalty.Campaigns;
using Darwin.Application.Loyalty.Commands;
using Darwin.Application;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Queries;
using Darwin.Application.Businesses.Queries;
using Darwin.Contracts.Loyalty;
using Darwin.Shared.Results;
using Darwin.WebApi.Controllers.Businesses;
using Darwin.WebApi.Mappers;
using Darwin.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using DomainLoyaltyRewardType = Darwin.Domain.Enums.LoyaltyRewardType;

namespace Darwin.WebApi.Controllers.Business;

/// <summary>
/// Business-facing loyalty endpoints used by the business mobile application and admin integrations.
/// </summary>
[ApiController]
[Route("api/v1/business/loyalty")]
[Authorize]
public sealed class BusinessLoyaltyController : ApiControllerBase
{
    private readonly ProcessScanSessionForBusinessHandler _processScanSessionForBusinessHandler;
    private readonly ConfirmAccrualFromSessionHandler _confirmAccrualFromSessionHandler;
    private readonly ConfirmRedemptionFromSessionHandler _confirmRedemptionFromSessionHandler;
    private readonly GetLoyaltyProgramsPageHandler _getLoyaltyProgramsPageHandler;
    private readonly GetLoyaltyRewardTiersPageHandler _getLoyaltyRewardTiersPageHandler;
    private readonly CreateLoyaltyProgramHandler _createLoyaltyProgramHandler;
    private readonly CreateLoyaltyRewardTierHandler _createLoyaltyRewardTierHandler;
    private readonly UpdateLoyaltyRewardTierHandler _updateLoyaltyRewardTierHandler;
    private readonly SoftDeleteLoyaltyRewardTierHandler _softDeleteLoyaltyRewardTierHandler;
    private readonly GetBusinessCampaignsHandler _getBusinessCampaignsHandler;
    private readonly CreateBusinessCampaignHandler _createBusinessCampaignHandler;
    private readonly UpdateBusinessCampaignHandler _updateBusinessCampaignHandler;
    private readonly SetCampaignActivationHandler _setCampaignActivationHandler;
    private readonly ILoyaltyPresentationService _presentationService;
    private readonly GetCurrentBusinessAccessStateHandler _getCurrentBusinessAccessStateHandler;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessLoyaltyController"/> class.
    /// </summary>
    public BusinessLoyaltyController(
        ProcessScanSessionForBusinessHandler processScanSessionForBusinessHandler,
        ConfirmAccrualFromSessionHandler confirmAccrualFromSessionHandler,
        ConfirmRedemptionFromSessionHandler confirmRedemptionFromSessionHandler,
        GetLoyaltyProgramsPageHandler getLoyaltyProgramsPageHandler,
        GetLoyaltyRewardTiersPageHandler getLoyaltyRewardTiersPageHandler,
        CreateLoyaltyProgramHandler createLoyaltyProgramHandler,
        CreateLoyaltyRewardTierHandler createLoyaltyRewardTierHandler,
        UpdateLoyaltyRewardTierHandler updateLoyaltyRewardTierHandler,
        SoftDeleteLoyaltyRewardTierHandler softDeleteLoyaltyRewardTierHandler,
        GetBusinessCampaignsHandler getBusinessCampaignsHandler,
        CreateBusinessCampaignHandler createBusinessCampaignHandler,
        UpdateBusinessCampaignHandler updateBusinessCampaignHandler,
        SetCampaignActivationHandler setCampaignActivationHandler,
        ILoyaltyPresentationService presentationService,
        GetCurrentBusinessAccessStateHandler getCurrentBusinessAccessStateHandler,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _processScanSessionForBusinessHandler = processScanSessionForBusinessHandler ?? throw new ArgumentNullException(nameof(processScanSessionForBusinessHandler));
        _confirmAccrualFromSessionHandler = confirmAccrualFromSessionHandler ?? throw new ArgumentNullException(nameof(confirmAccrualFromSessionHandler));
        _confirmRedemptionFromSessionHandler = confirmRedemptionFromSessionHandler ?? throw new ArgumentNullException(nameof(confirmRedemptionFromSessionHandler));
        _getLoyaltyProgramsPageHandler = getLoyaltyProgramsPageHandler ?? throw new ArgumentNullException(nameof(getLoyaltyProgramsPageHandler));
        _getLoyaltyRewardTiersPageHandler = getLoyaltyRewardTiersPageHandler ?? throw new ArgumentNullException(nameof(getLoyaltyRewardTiersPageHandler));
        _createLoyaltyProgramHandler = createLoyaltyProgramHandler ?? throw new ArgumentNullException(nameof(createLoyaltyProgramHandler));
        _createLoyaltyRewardTierHandler = createLoyaltyRewardTierHandler ?? throw new ArgumentNullException(nameof(createLoyaltyRewardTierHandler));
        _updateLoyaltyRewardTierHandler = updateLoyaltyRewardTierHandler ?? throw new ArgumentNullException(nameof(updateLoyaltyRewardTierHandler));
        _softDeleteLoyaltyRewardTierHandler = softDeleteLoyaltyRewardTierHandler ?? throw new ArgumentNullException(nameof(softDeleteLoyaltyRewardTierHandler));
        _getBusinessCampaignsHandler = getBusinessCampaignsHandler ?? throw new ArgumentNullException(nameof(getBusinessCampaignsHandler));
        _createBusinessCampaignHandler = createBusinessCampaignHandler ?? throw new ArgumentNullException(nameof(createBusinessCampaignHandler));
        _updateBusinessCampaignHandler = updateBusinessCampaignHandler ?? throw new ArgumentNullException(nameof(updateBusinessCampaignHandler));
        _setCampaignActivationHandler = setCampaignActivationHandler ?? throw new ArgumentNullException(nameof(setCampaignActivationHandler));
        _presentationService = presentationService ?? throw new ArgumentNullException(nameof(presentationService));
        _getCurrentBusinessAccessStateHandler = getCurrentBusinessAccessStateHandler ?? throw new ArgumentNullException(nameof(getCurrentBusinessAccessStateHandler));
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    /// <summary>
    /// Returns the current business loyalty program and reward-tier configuration.
    /// </summary>
    [HttpGet("reward-config")]
    [HttpGet("/api/v1/loyalty/business/reward-config")]
    [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
    [ProducesResponseType(typeof(BusinessRewardConfigurationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBusinessRewardConfigurationAsync(CancellationToken ct = default)
    {
        var (hasBusinessAccess, businessId, errorResult) = await TryGetCurrentBusinessIdAsync(ct).ConfigureAwait(false);
        if (!hasBusinessAccess)
        {
            return errorResult ?? Forbid();
        }

        var programResult = await _getLoyaltyProgramsPageHandler
            .HandleAsync(page: 1, pageSize: 1, businessId: businessId, ct: ct)
            .ConfigureAwait(false);

        var program = programResult.Items.FirstOrDefault();
        if (program is null)
        {
            return Ok(new BusinessRewardConfigurationResponse
            {
                LoyaltyProgramId = Guid.Empty,
                ProgramName = string.Empty,
                IsProgramActive = false,
                RewardTiers = Array.Empty<BusinessRewardTierConfigItem>()
            });
        }

        var tiersResult = await _getLoyaltyRewardTiersPageHandler
            .HandleAsync(program.Id, page: 1, pageSize: 200, filter: LoyaltyRewardTierQueueFilter.All, ct: ct)
            .ConfigureAwait(false);

        return Ok(new BusinessRewardConfigurationResponse
        {
            LoyaltyProgramId = program.Id,
            ProgramName = program.Name ?? string.Empty,
            IsProgramActive = program.IsActive,
            RewardTiers = tiersResult.Items.Select(x => new BusinessRewardTierConfigItem
            {
                RewardTierId = x.Id,
                PointsRequired = x.PointsRequired,
                RewardType = x.RewardType.ToString(),
                RewardValue = x.RewardValue,
                Description = x.Description,
                AllowSelfRedemption = x.AllowSelfRedemption,
                RowVersion = x.RowVersion
            }).ToList()
        });
    }

    /// <summary>
    /// Creates a reward tier within the current business loyalty program.
    /// </summary>
    [HttpPost("reward-config/tiers")]
    [HttpPost("/api/v1/loyalty/business/reward-config/tiers")]
    [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
    [ProducesResponseType(typeof(BusinessRewardTierMutationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateBusinessRewardTierAsync([FromBody] CreateBusinessRewardTierRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        var (hasBusinessAccess, businessId, errorResult) = await TryGetCurrentBusinessIdAsync(ct).ConfigureAwait(false);
        if (!hasBusinessAccess)
        {
            return errorResult ?? Forbid();
        }

        if (!TryParseRewardType(request.RewardType, out var rewardType))
        {
            return BadRequestProblem(_validationLocalizer["RewardTypeInvalidAllowedValues"]);
        }

        var programId = await EnsureBusinessProgramAsync(businessId, createIfMissing: true, ct).ConfigureAwait(false);

        try
        {
            var tierId = await _createLoyaltyRewardTierHandler
                .HandleAsync(new LoyaltyRewardTierCreateDto
                {
                    LoyaltyProgramId = programId,
                    PointsRequired = request.PointsRequired,
                    RewardType = rewardType,
                    RewardValue = request.RewardValue,
                    Description = NormalizeText(request.Description),
                    AllowSelfRedemption = request.AllowSelfRedemption,
                    MetadataJson = NormalizeText(request.MetadataJson)
                }, ct)
                .ConfigureAwait(false);

            return Ok(new BusinessRewardTierMutationResponse { RewardTierId = tierId, Success = true });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequestProblem(_validationLocalizer["LoyaltyRewardTierCreateFailed"], ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing reward tier owned by the current business.
    /// </summary>
    [HttpPut("reward-config/tiers")]
    [HttpPut("/api/v1/loyalty/business/reward-config/tiers")]
    [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
    [ProducesResponseType(typeof(BusinessRewardTierMutationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateBusinessRewardTierAsync([FromBody] UpdateBusinessRewardTierRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        if (request.RewardTierId == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["RewardTierIdCannotBeEmpty"]);
        }

        var (hasBusinessAccess, businessId, errorResult) = await TryGetCurrentBusinessIdAsync(ct).ConfigureAwait(false);
        if (!hasBusinessAccess)
        {
            return errorResult ?? Forbid();
        }

        if (!TryParseRewardType(request.RewardType, out var rewardType))
        {
            return BadRequestProblem(_validationLocalizer["RewardTypeInvalidAllowedValues"]);
        }

        var programId = await EnsureBusinessProgramAsync(businessId, createIfMissing: false, ct).ConfigureAwait(false);
        if (programId == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["LoyaltyProgramNotFound"]);
        }

        if (!await IsRewardTierOwnedByBusinessAsync(programId, request.RewardTierId, ct).ConfigureAwait(false))
        {
            return Forbid();
        }

        try
        {
            await _updateLoyaltyRewardTierHandler
                .HandleAsync(new LoyaltyRewardTierEditDto
                {
                    Id = request.RewardTierId,
                    LoyaltyProgramId = programId,
                    PointsRequired = request.PointsRequired,
                    RewardType = rewardType,
                    RewardValue = request.RewardValue,
                    Description = NormalizeText(request.Description),
                    AllowSelfRedemption = request.AllowSelfRedemption,
                    MetadataJson = NormalizeText(request.MetadataJson),
                    RowVersion = request.RowVersion
                }, ct)
                .ConfigureAwait(false);

            return Ok(new BusinessRewardTierMutationResponse { RewardTierId = request.RewardTierId, Success = true });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequestProblem(_validationLocalizer["LoyaltyRewardTierUpdateFailed"], ex.Message);
        }
    }

    /// <summary>
    /// Soft-deletes a reward tier owned by the current business.
    /// </summary>
    [HttpPost("reward-config/tiers/delete")]
    [HttpPost("/api/v1/loyalty/business/reward-config/tiers/delete")]
    [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
    [ProducesResponseType(typeof(BusinessRewardTierMutationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteBusinessRewardTierAsync([FromBody] DeleteBusinessRewardTierRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        if (request.RewardTierId == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["RewardTierIdCannotBeEmpty"]);
        }

        var (hasBusinessAccess, businessId, errorResult) = await TryGetCurrentBusinessIdAsync(ct).ConfigureAwait(false);
        if (!hasBusinessAccess)
        {
            return errorResult ?? Forbid();
        }

        var programId = await EnsureBusinessProgramAsync(businessId, createIfMissing: false, ct).ConfigureAwait(false);
        if (programId == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["LoyaltyProgramNotFound"]);
        }

        if (!await IsRewardTierOwnedByBusinessAsync(programId, request.RewardTierId, ct).ConfigureAwait(false))
        {
            return Forbid();
        }

        var result = await _softDeleteLoyaltyRewardTierHandler
            .HandleAsync(new LoyaltyRewardTierDeleteDto { Id = request.RewardTierId, RowVersion = request.RowVersion }, ct)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return ProblemFromResult(result);
        }

        return Ok(new BusinessRewardTierMutationResponse { RewardTierId = request.RewardTierId, Success = true });
    }

    /// <summary>
    /// Resolves and materializes a member scan session for the current business operator.
    /// </summary>
    [HttpPost("scan/process")]
    [HttpPost("/api/v1/loyalty/scan/process")]
    [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
    [ProducesResponseType(typeof(ProcessScanSessionForBusinessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessScanSessionForBusinessAsync([FromBody] ProcessScanSessionForBusinessRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        var normalizedScanSessionToken = NormalizeText(request.ScanSessionToken);
        if (normalizedScanSessionToken is null)
        {
            return BadRequestProblem(_validationLocalizer["ScanSessionTokenRequired"]);
        }

        var (hasBusinessAccess, businessId, errorResult) = await TryGetCurrentBusinessIdAsync(ct).ConfigureAwait(false);
        if (!hasBusinessAccess)
        {
            return errorResult ?? Forbid();
        }

        var result = await _processScanSessionForBusinessHandler
            .HandleAsync(normalizedScanSessionToken, businessId, ct)
            .ConfigureAwait(false);

        if (!result.Succeeded || result.Value is null)
        {
            return MapScanFailure(result);
        }

        IReadOnlyList<LoyaltyRewardSummary> selectedRewards = Array.Empty<LoyaltyRewardSummary>();
        if (result.Value.Mode == Darwin.Domain.Enums.LoyaltyScanMode.Redemption &&
            result.Value.SelectedRewards is { Count: > 0 })
        {
            var tierIds = result.Value.SelectedRewards
                .Select(x => x.LoyaltyRewardTierId)
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList();

            var enrichResult = await _presentationService
                .EnrichSelectedRewardsAsync(businessId, tierIds, failIfMissing: true, ct)
                .ConfigureAwait(false);

            if (!enrichResult.Succeeded)
            {
                return ProblemFromResult(enrichResult);
            }

            selectedRewards = enrichResult.Value ?? Array.Empty<LoyaltyRewardSummary>();
        }

        var allowedActions =
            result.Value.Mode == Darwin.Domain.Enums.LoyaltyScanMode.Accrual
                ? LoyaltyScanAllowedActions.CanConfirmAccrual
                : LoyaltyScanAllowedActions.CanConfirmRedemption;

        return Ok(new ProcessScanSessionForBusinessResponse
        {
            Mode = LoyaltyContractsMapper.ToContract(result.Value.Mode),
            BusinessId = businessId,
            AccountSummary = LoyaltyContractsMapper.ToContractBusinessAccountSummary(result.Value),
            CustomerDisplayName = result.Value.CustomerDisplayName,
            SelectedRewards = selectedRewards,
            AllowedActions = allowedActions
        });
    }

    /// <summary>
    /// Confirms an accrual flow for a previously processed scan session.
    /// </summary>
    [HttpPost("scan/confirm-accrual")]
    [HttpPost("/api/v1/loyalty/scan/confirm-accrual")]
    [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
    [ProducesResponseType(typeof(ConfirmAccrualResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ConfirmAccrualAsync([FromBody] ConfirmAccrualRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        var normalizedScanSessionToken = NormalizeText(request.ScanSessionToken);
        if (normalizedScanSessionToken is null)
        {
            return BadRequestProblem(_validationLocalizer["ScanSessionTokenRequired"]);
        }

        if (normalizedScanSessionToken.Length > 4000)
        {
            return BadRequestProblem(_validationLocalizer["ScanSessionTokenTooLong"]);
        }

        if (request.Points <= 0)
        {
            return BadRequestProblem(_validationLocalizer["PointsPositiveInteger"]);
        }

        var (hasBusinessAccess, businessId, errorResult) = await TryGetCurrentBusinessIdAsync(ct).ConfigureAwait(false);
        if (!hasBusinessAccess)
        {
            return errorResult ?? Forbid();
        }

        var result = await _confirmAccrualFromSessionHandler
            .HandleAsync(new ConfirmAccrualFromSessionDto
            {
                ScanSessionToken = normalizedScanSessionToken,
                Points = request.Points,
                Note = NormalizeText(request.Note)
            }, businessId, ct)
            .ConfigureAwait(false);

        if (!result.Succeeded || result.Value is null)
        {
            return MapScanFailure(result);
        }

        return Ok(new ConfirmAccrualResponse
        {
            Success = true,
            NewBalance = result.Value.NewPointsBalance,
            UpdatedAccount = null,
            ErrorCode = null,
            ErrorMessage = null
        });
    }

    /// <summary>
    /// Confirms a redemption flow for a previously processed scan session.
    /// </summary>
    [HttpPost("scan/confirm-redemption")]
    [HttpPost("/api/v1/loyalty/scan/confirm-redemption")]
    [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
    [ProducesResponseType(typeof(ConfirmRedemptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ConfirmRedemptionAsync([FromBody] ConfirmRedemptionRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        var normalizedScanSessionToken = NormalizeText(request.ScanSessionToken);
        if (normalizedScanSessionToken is null)
        {
            return BadRequestProblem(_validationLocalizer["ScanSessionTokenRequired"]);
        }

        if (normalizedScanSessionToken.Length > 4000)
        {
            return BadRequestProblem(_validationLocalizer["ScanSessionTokenTooLong"]);
        }

        var (hasBusinessAccess, businessId, errorResult) = await TryGetCurrentBusinessIdAsync(ct).ConfigureAwait(false);
        if (!hasBusinessAccess)
        {
            return errorResult ?? Forbid();
        }

        var result = await _confirmRedemptionFromSessionHandler
            .HandleAsync(new ConfirmRedemptionFromSessionDto
            {
                ScanSessionToken = normalizedScanSessionToken
            }, businessId, ct)
            .ConfigureAwait(false);

        if (!result.Succeeded || result.Value is null)
        {
            return MapScanFailure(result);
        }

        return Ok(new ConfirmRedemptionResponse
        {
            Success = true,
            NewBalance = result.Value.NewPointsBalance,
            UpdatedAccount = null,
            ErrorCode = null,
            ErrorMessage = null
        });
    }

    /// <summary>
    /// Returns campaigns owned by the current business for management views.
    /// </summary>
    [HttpGet("campaigns")]
    [HttpGet("/api/v1/loyalty/business/campaigns")]
    [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
    [ProducesResponseType(typeof(GetBusinessCampaignsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBusinessCampaignsAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (page <= 0)
        {
            return BadRequestProblem(_validationLocalizer["PageMustBePositiveInteger"]);
        }

        if (pageSize <= 0 || pageSize > 200)
        {
            return BadRequestProblem(_validationLocalizer["PageSizeMustBeBetween1And200"]);
        }

        var (hasBusinessAccess, businessId, errorResult) = await TryGetCurrentBusinessIdAsync(ct).ConfigureAwait(false);
        if (!hasBusinessAccess)
        {
            return errorResult ?? Forbid();
        }

        var result = await _getBusinessCampaignsHandler.HandleAsync(businessId, page, pageSize, filter: LoyaltyCampaignQueueFilter.All, ct: ct).ConfigureAwait(false);
        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result);
        }

        return Ok(new GetBusinessCampaignsResponse
        {
            Total = result.Value.Total,
            Items = result.Value.Items.Select(x => new BusinessCampaignItem
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                Name = x.Name,
                Title = x.Title,
                Subtitle = x.Subtitle,
                Body = x.Body,
                MediaUrl = x.MediaUrl,
                LandingUrl = x.LandingUrl,
                Channels = x.Channels,
                StartsAtUtc = x.StartsAtUtc,
                EndsAtUtc = x.EndsAtUtc,
                IsActive = x.IsActive,
                CampaignState = x.CampaignState,
                TargetingJson = x.TargetingJson,
                EligibilityRules = x.EligibilityRules.Select(rule => new PromotionEligibilityRule
                {
                    AudienceKind = rule.AudienceKind,
                    MinPoints = rule.MinPoints,
                    MaxPoints = rule.MaxPoints,
                    TierKey = rule.TierKey,
                    Note = rule.Note
                }).ToList(),
                PayloadJson = x.PayloadJson,
                RowVersion = x.RowVersion
            }).ToList()
        });
    }

    /// <summary>
    /// Creates a new campaign owned by the current business.
    /// </summary>
    [HttpPost("campaigns")]
    [HttpPost("/api/v1/loyalty/business/campaigns")]
    [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
    [ProducesResponseType(typeof(BusinessCampaignMutationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBusinessCampaignAsync([FromBody] CreateBusinessCampaignRequest? request, CancellationToken ct = default)
    {
        var (hasBusinessAccess, businessId, errorResult) = await TryGetCurrentBusinessIdAsync(ct).ConfigureAwait(false);
        if (!hasBusinessAccess)
        {
            return errorResult ?? Forbid();
        }

        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        var result = await _createBusinessCampaignHandler.HandleAsync(new CreateBusinessCampaignDto
        {
            BusinessId = businessId,
            Name = NormalizeText(request.Name) ?? string.Empty,
            Title = NormalizeText(request.Title) ?? string.Empty,
            Subtitle = NormalizeText(request.Subtitle),
            Body = NormalizeText(request.Body),
            MediaUrl = NormalizeText(request.MediaUrl),
            LandingUrl = NormalizeText(request.LandingUrl),
            Channels = request.Channels,
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = request.EndsAtUtc,
            TargetingJson = NormalizeText(request.TargetingJson) ?? "{}",
            EligibilityRules = (request.EligibilityRules ?? new List<PromotionEligibilityRule>()).Select(rule => new Darwin.Application.Loyalty.Campaigns.PromotionEligibilityRuleDto
            {
                AudienceKind = NormalizeText(rule.AudienceKind) ?? PromotionAudienceKind.JoinedMembers,
                MinPoints = rule.MinPoints,
                MaxPoints = rule.MaxPoints,
                TierKey = NormalizeText(rule.TierKey),
                Note = NormalizeText(rule.Note)
            }).ToList(),
            PayloadJson = NormalizeText(request.PayloadJson) ?? "{}"
        }, ct).ConfigureAwait(false);

        if (!result.Succeeded || result.Value == Guid.Empty)
        {
            return ProblemFromResult(result);
        }

        return Created($"/api/v1/business/loyalty/campaigns/{result.Value}", new BusinessCampaignMutationResponse { CampaignId = result.Value });
    }

    /// <summary>
    /// Updates an existing campaign owned by the current business.
    /// </summary>
    [HttpPut("campaigns/{id:guid}")]
    [HttpPut("/api/v1/loyalty/business/campaigns/{id:guid}")]
    [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateBusinessCampaignAsync(Guid id, [FromBody] UpdateBusinessCampaignRequest? request, CancellationToken ct = default)
    {
        var (hasBusinessAccess, businessId, errorResult) = await TryGetCurrentBusinessIdAsync(ct).ConfigureAwait(false);
        if (!hasBusinessAccess)
        {
            return errorResult ?? Forbid();
        }

        if (request is null || request.Id != id)
        {
            return BadRequestProblem(_validationLocalizer["RequestBodyRouteIdMismatch"]);
        }

        if (id == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["IdentifierMustNotBeEmpty"]);
        }

        var result = await _updateBusinessCampaignHandler.HandleAsync(new UpdateBusinessCampaignDto
        {
            BusinessId = businessId,
            Id = request.Id,
            Name = NormalizeText(request.Name) ?? string.Empty,
            Title = NormalizeText(request.Title) ?? string.Empty,
            Subtitle = NormalizeText(request.Subtitle),
            Body = NormalizeText(request.Body),
            MediaUrl = NormalizeText(request.MediaUrl),
            LandingUrl = NormalizeText(request.LandingUrl),
            Channels = request.Channels,
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = request.EndsAtUtc,
            TargetingJson = NormalizeText(request.TargetingJson) ?? "{}",
            EligibilityRules = (request.EligibilityRules ?? new List<PromotionEligibilityRule>()).Select(rule => new Darwin.Application.Loyalty.Campaigns.PromotionEligibilityRuleDto
            {
                AudienceKind = NormalizeText(rule.AudienceKind) ?? PromotionAudienceKind.JoinedMembers,
                MinPoints = rule.MinPoints,
                MaxPoints = rule.MaxPoints,
                TierKey = NormalizeText(rule.TierKey),
                Note = NormalizeText(rule.Note)
            }).ToList(),
            PayloadJson = NormalizeText(request.PayloadJson) ?? "{}",
            RowVersion = request.RowVersion
        }, ct).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return ProblemFromResult(result);
        }

        return NoContent();
    }

    /// <summary>
    /// Activates or deactivates a business campaign.
    /// </summary>
    [HttpPost("campaigns/{id:guid}/activation")]
    [HttpPost("/api/v1/loyalty/business/campaigns/{id:guid}/activation")]
    [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetBusinessCampaignActivationAsync(Guid id, [FromBody] SetCampaignActivationRequest? request, CancellationToken ct = default)
    {
        var (hasBusinessAccess, businessId, errorResult) = await TryGetCurrentBusinessIdAsync(ct).ConfigureAwait(false);
        if (!hasBusinessAccess)
        {
            return errorResult ?? Forbid();
        }

        if (request is null || request.Id != id)
        {
            return BadRequestProblem(_validationLocalizer["RequestBodyRouteIdMismatch"]);
        }

        if (id == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["IdentifierMustNotBeEmpty"]);
        }

        var result = await _setCampaignActivationHandler.HandleAsync(new SetCampaignActivationDto
        {
            BusinessId = businessId,
            Id = request.Id,
            IsActive = request.IsActive,
            RowVersion = request.RowVersion
        }, ct).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return ProblemFromResult(result);
        }

        return NoContent();
    }

    /// <summary>
    /// Resolves the current business identifier from the authenticated principal.
    /// </summary>
    private async Task<(bool Success, Guid BusinessId, IActionResult? ErrorResult)> TryGetCurrentBusinessIdAsync(CancellationToken ct)
    {
        var businessId = Guid.Empty;

        if (!BusinessControllerConventions.TryGetCurrentBusinessId(User, out businessId) ||
            !BusinessControllerConventions.TryGetCurrentUserId(User, out var userId))
        {
            return (false, Guid.Empty, Forbid());
        }

        var accessState = await _getCurrentBusinessAccessStateHandler.HandleAsync(businessId, userId, ct).ConfigureAwait(false);
        if (accessState is null)
        {
            return (false, Guid.Empty, NotFoundProblem(_validationLocalizer["BusinessNotFound"]));
        }

        if (!accessState.IsBusinessClientAccessAllowed || !accessState.IsOperationsAllowed)
        {
            return (false, Guid.Empty, ForbiddenProblem(detail: BusinessAccessStateMessageLocalizer.LocalizeBlockingReason(accessState, _validationLocalizer)));
        }

        return (true, businessId, null);
    }

    /// <summary>
    /// Returns the current business loyalty program or creates a default one when allowed.
    /// </summary>
    private async Task<Guid> EnsureBusinessProgramAsync(Guid businessId, bool createIfMissing, CancellationToken ct)
    {
        var existing = await _getLoyaltyProgramsPageHandler
            .HandleAsync(page: 1, pageSize: 1, businessId: businessId, ct: ct)
            .ConfigureAwait(false);

        var program = existing.Items.FirstOrDefault();
        if (program is not null)
        {
            return program.Id;
        }

        if (!createIfMissing)
        {
            return Guid.Empty;
        }

        return await _createLoyaltyProgramHandler
            .HandleAsync(new LoyaltyProgramCreateDto
            {
                BusinessId = businessId,
                Name = "Default Loyalty Program",
                AccrualMode = Darwin.Domain.Enums.LoyaltyAccrualMode.PerVisit,
                IsActive = true
            }, ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Checks whether a reward tier belongs to the current business program.
    /// </summary>
    private async Task<bool> IsRewardTierOwnedByBusinessAsync(Guid programId, Guid rewardTierId, CancellationToken ct)
    {
        var tiers = await _getLoyaltyRewardTiersPageHandler
            .HandleAsync(programId, page: 1, pageSize: 200, filter: LoyaltyRewardTierQueueFilter.All, ct: ct)
            .ConfigureAwait(false);

        return tiers.Items.Any(x => x.Id == rewardTierId);
    }

    /// <summary>
    /// Parses the public reward-type string into the domain enum.
    /// </summary>
    private static bool TryParseRewardType(string? rewardType, out DomainLoyaltyRewardType value)
    {
        if (Enum.TryParse(NormalizeText(rewardType), ignoreCase: true, out DomainLoyaltyRewardType parsed) &&
            Enum.IsDefined(parsed))
        {
            value = parsed;
            return true;
        }

        value = default;
        return false;
    }

    private static string? NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// Maps common scan-session failures to stable HTTP responses for business clients.
    /// </summary>
    private IActionResult MapScanFailure<T>(Result<T> result)
    {
        var msg = string.IsNullOrWhiteSpace(result.Error)
            ? _validationLocalizer["OperationFailed"].Value.Trim()
            : result.Error!.Trim();
        var normalized = NormalizeToken(msg);

        if (normalized is "expired" or "scansessiontokenexpired" or "tokenalreadyconsumed" or "scansessiontokenalreadyconsumed")
        {
            return ConflictProblem(LocalizeScanFailureMessage(msg));
        }

        if (normalized is "accountnotfound" or "scansessiontokennotfound" or "loyaltyaccountnotfoundforscansession")
        {
            return NotFoundProblem(LocalizeScanFailureMessage(msg));
        }

        if (normalized is "accountnotactive" or "noselections" or "invalidselections" or "insufficientpoints")
        {
            return BadRequestProblem(LocalizeScanFailureMessage(msg));
        }

        if (normalized is "scansessiontokenbusinessmismatch" ||
            normalized.Contains("belongstoadifferentbusiness", StringComparison.Ordinal) ||
            normalized.Contains("boundtoadifferentbusiness", StringComparison.Ordinal) ||
            normalized.Contains("doesnotbelongtothisbusiness", StringComparison.Ordinal))
        {
            return Forbid();
        }

        if (normalized.Contains("expired", StringComparison.Ordinal) || normalized.Contains("consumed", StringComparison.Ordinal))
        {
            return ConflictProblem(LocalizeScanFailureMessage(msg));
        }

        if (normalized.Contains("notfound", StringComparison.Ordinal))
        {
            return NotFoundProblem(LocalizeScanFailureMessage(msg));
        }

        return ProblemFromResult(result, _validationLocalizer["OperationFailed"]);
    }

    private string LocalizeScanFailureMessage(string message)
    {
        return NormalizeToken(message) switch
        {
            "expired" or "scansessiontokenexpired" => _validationLocalizer["ScanSessionTokenExpired"],
            "tokenalreadyconsumed" or "scansessiontokenalreadyconsumed" => _validationLocalizer["ScanSessionTokenAlreadyConsumed"],
            "accountnotfound" or "scansessiontokennotfound" or "loyaltyaccountnotfoundforscansession" => _validationLocalizer["LoyaltyAccountNotFoundForScanSession"],
            "accountnotactive" => _validationLocalizer["LoyaltyAccountInactive"],
            "noselections" => _validationLocalizer["SelectedRewardsMissing"],
            "invalidselections" => _validationLocalizer["SelectedRewardsInvalid"],
            "insufficientpoints" => _validationLocalizer["InsufficientPointsForSelectedRewards"],
            _ => message
        };
    }

    private static string NormalizeToken(string value)
        => new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    /// <summary>
    /// Returns a standardized conflict response for deterministic scan-session state failures.
    /// </summary>
    private IActionResult ConflictProblem(string detail)
    {
        var problem = new Darwin.Contracts.Common.ProblemDetails
        {
            Type = "https://docs.darwincms.com/errors/conflict",
            Title = "Conflict",
            Status = StatusCodes.Status409Conflict,
            Detail = detail
        };

        return StatusCode(StatusCodes.Status409Conflict, problem);
    }
}
