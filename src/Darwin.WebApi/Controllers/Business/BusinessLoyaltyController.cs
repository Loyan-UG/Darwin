using Darwin.Application.Loyalty.Campaigns;
using Darwin.Application.Loyalty.Commands;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Queries;
using Darwin.Contracts.Loyalty;
using Darwin.Shared.Results;
using Darwin.WebApi.Controllers.Businesses;
using Darwin.WebApi.Mappers;
using Darwin.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        ILoyaltyPresentationService presentationService)
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
        if (!TryGetCurrentBusinessId(out var businessId, out var errorResult))
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
            .HandleAsync(program.Id, page: 1, pageSize: 200, ct)
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
            return BadRequestProblem("Request body is required.");
        }

        if (!TryGetCurrentBusinessId(out var businessId, out var errorResult))
        {
            return errorResult ?? Forbid();
        }

        if (!TryParseRewardType(request.RewardType, out var rewardType))
        {
            return BadRequestProblem("RewardType is invalid. Allowed values: FreeItem, PercentDiscount, AmountDiscount.");
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
                    Description = request.Description,
                    AllowSelfRedemption = request.AllowSelfRedemption,
                    MetadataJson = request.MetadataJson
                }, ct)
                .ConfigureAwait(false);

            return Ok(new BusinessRewardTierMutationResponse { RewardTierId = tierId, Success = true });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequestProblem(ex.Message);
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
            return BadRequestProblem("Request body is required.");
        }

        if (request.RewardTierId == Guid.Empty)
        {
            return BadRequestProblem("RewardTierId is required.");
        }

        if (!TryGetCurrentBusinessId(out var businessId, out var errorResult))
        {
            return errorResult ?? Forbid();
        }

        if (!TryParseRewardType(request.RewardType, out var rewardType))
        {
            return BadRequestProblem("RewardType is invalid. Allowed values: FreeItem, PercentDiscount, AmountDiscount.");
        }

        var programId = await EnsureBusinessProgramAsync(businessId, createIfMissing: false, ct).ConfigureAwait(false);
        if (programId == Guid.Empty)
        {
            return BadRequestProblem("No loyalty program was found for the current business.");
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
                    Description = request.Description,
                    AllowSelfRedemption = request.AllowSelfRedemption,
                    MetadataJson = request.MetadataJson,
                    RowVersion = request.RowVersion
                }, ct)
                .ConfigureAwait(false);

            return Ok(new BusinessRewardTierMutationResponse { RewardTierId = request.RewardTierId, Success = true });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequestProblem(ex.Message);
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
            return BadRequestProblem("Request body is required.");
        }

        if (request.RewardTierId == Guid.Empty)
        {
            return BadRequestProblem("RewardTierId is required.");
        }

        if (!TryGetCurrentBusinessId(out var businessId, out var errorResult))
        {
            return errorResult ?? Forbid();
        }

        var programId = await EnsureBusinessProgramAsync(businessId, createIfMissing: false, ct).ConfigureAwait(false);
        if (programId == Guid.Empty)
        {
            return BadRequestProblem("No loyalty program was found for the current business.");
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
            return BadRequestProblem("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ScanSessionToken))
        {
            return BadRequestProblem("ScanSessionToken is required.");
        }

        if (!TryGetCurrentBusinessId(out var businessId, out var errorResult))
        {
            return errorResult ?? Forbid();
        }

        var result = await _processScanSessionForBusinessHandler
            .HandleAsync(request.ScanSessionToken, businessId, ct)
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
            return BadRequestProblem("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ScanSessionToken))
        {
            return BadRequestProblem("ScanSessionToken is required.");
        }

        if (request.ScanSessionToken.Length > 4000)
        {
            return BadRequestProblem("ScanSessionToken is too long.");
        }

        if (request.Points <= 0)
        {
            return BadRequestProblem("Points must be greater than zero.");
        }

        if (!TryGetCurrentBusinessId(out var businessId, out var errorResult))
        {
            return errorResult ?? Forbid();
        }

        var result = await _confirmAccrualFromSessionHandler
            .HandleAsync(new ConfirmAccrualFromSessionDto
            {
                ScanSessionToken = request.ScanSessionToken,
                Points = request.Points,
                Note = request.Note
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
            return BadRequestProblem("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ScanSessionToken))
        {
            return BadRequestProblem("ScanSessionToken is required.");
        }

        if (request.ScanSessionToken.Length > 4000)
        {
            return BadRequestProblem("ScanSessionToken is too long.");
        }

        if (!TryGetCurrentBusinessId(out var businessId, out var errorResult))
        {
            return errorResult ?? Forbid();
        }

        var result = await _confirmRedemptionFromSessionHandler
            .HandleAsync(new ConfirmRedemptionFromSessionDto
            {
                ScanSessionToken = request.ScanSessionToken
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
        if (!TryGetCurrentBusinessId(out var businessId, out var errorResult))
        {
            return errorResult!;
        }

        var result = await _getBusinessCampaignsHandler.HandleAsync(businessId, page, pageSize, ct).ConfigureAwait(false);
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
        if (!TryGetCurrentBusinessId(out var businessId, out var errorResult))
        {
            return errorResult!;
        }

        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
        }

        var result = await _createBusinessCampaignHandler.HandleAsync(new CreateBusinessCampaignDto
        {
            BusinessId = businessId,
            Name = request.Name,
            Title = request.Title,
            Subtitle = request.Subtitle,
            Body = request.Body,
            MediaUrl = request.MediaUrl,
            LandingUrl = request.LandingUrl,
            Channels = request.Channels,
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = request.EndsAtUtc,
            TargetingJson = request.TargetingJson,
            EligibilityRules = request.EligibilityRules.Select(rule => new Darwin.Application.Loyalty.Campaigns.PromotionEligibilityRuleDto
            {
                AudienceKind = rule.AudienceKind,
                MinPoints = rule.MinPoints,
                MaxPoints = rule.MaxPoints,
                TierKey = rule.TierKey,
                Note = rule.Note
            }).ToList(),
            PayloadJson = request.PayloadJson
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
        if (!TryGetCurrentBusinessId(out var businessId, out var errorResult))
        {
            return errorResult!;
        }

        if (request is null || request.Id != id)
        {
            return BadRequestProblem("Request body is required and route id must match body id.");
        }

        var result = await _updateBusinessCampaignHandler.HandleAsync(new UpdateBusinessCampaignDto
        {
            BusinessId = businessId,
            Id = request.Id,
            Name = request.Name,
            Title = request.Title,
            Subtitle = request.Subtitle,
            Body = request.Body,
            MediaUrl = request.MediaUrl,
            LandingUrl = request.LandingUrl,
            Channels = request.Channels,
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = request.EndsAtUtc,
            TargetingJson = request.TargetingJson,
            EligibilityRules = request.EligibilityRules.Select(rule => new Darwin.Application.Loyalty.Campaigns.PromotionEligibilityRuleDto
            {
                AudienceKind = rule.AudienceKind,
                MinPoints = rule.MinPoints,
                MaxPoints = rule.MaxPoints,
                TierKey = rule.TierKey,
                Note = rule.Note
            }).ToList(),
            PayloadJson = request.PayloadJson,
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
        if (!TryGetCurrentBusinessId(out var businessId, out var errorResult))
        {
            return errorResult!;
        }

        if (request is null || request.Id != id)
        {
            return BadRequestProblem("Request body is required and route id must match body id.");
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
    private bool TryGetCurrentBusinessId(out Guid businessId, out IActionResult? errorResult)
    {
        errorResult = null;
        if (BusinessControllerConventions.TryGetCurrentBusinessId(User, out businessId))
        {
            return true;
        }

        businessId = Guid.Empty;
        errorResult = Forbid();
        return false;
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
            .HandleAsync(programId, page: 1, pageSize: 200, ct)
            .ConfigureAwait(false);

        return tiers.Items.Any(x => x.Id == rewardTierId);
    }

    /// <summary>
    /// Parses the public reward-type string into the domain enum.
    /// </summary>
    private static bool TryParseRewardType(string? rewardType, out DomainLoyaltyRewardType value)
    {
        if (Enum.TryParse(rewardType, ignoreCase: true, out DomainLoyaltyRewardType parsed) &&
            Enum.IsDefined(parsed))
        {
            value = parsed;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Maps common scan-session failures to stable HTTP responses for business clients.
    /// </summary>
    private IActionResult MapScanFailure<T>(Result<T> result)
    {
        var msg = (result.Error ?? "Operation failed.").Trim();
        var text = msg.ToLowerInvariant();

        if (text.Contains("expired") || text.Contains("consumed"))
        {
            return ConflictProblem(msg);
        }

        if (text.Contains("not found"))
        {
            return NotFoundProblem(msg);
        }

        if (text.Contains("belongs to a different business") ||
            text.Contains("bound to a different business") ||
            text.Contains("does not belong to this business"))
        {
            return Forbid();
        }

        return ProblemFromResult(result);
    }

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
