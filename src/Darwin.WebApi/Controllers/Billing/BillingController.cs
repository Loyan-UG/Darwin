using Darwin.Application.Billing;
using Darwin.Contracts.Billing;
using Darwin.Application;
using Darwin.WebApi.Controllers;
using Darwin.Application.Businesses.Queries;
using Darwin.WebApi.Controllers.Businesses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.WebApi.Controllers.Billing;

/// <summary>
/// Business billing endpoints for mobile operators.
/// </summary>
[ApiController]
[Route("api/v1/business/billing")]
[Authorize]
public sealed class BillingController : ApiControllerBase
{
    private readonly GetBusinessSubscriptionStatusHandler _getBusinessSubscriptionStatusHandler;
    private readonly SetCancelAtPeriodEndHandler _setCancelAtPeriodEndHandler;
    private readonly GetBillingPlansHandler _getBillingPlansHandler;
    private readonly CreateSubscriptionCheckoutIntentHandler _createSubscriptionCheckoutIntentHandler;
    private readonly GetCurrentBusinessAccessStateHandler _getCurrentBusinessAccessStateHandler;
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    public BillingController(
        GetBusinessSubscriptionStatusHandler getBusinessSubscriptionStatusHandler,
        SetCancelAtPeriodEndHandler setCancelAtPeriodEndHandler,
        GetBillingPlansHandler getBillingPlansHandler,
        CreateSubscriptionCheckoutIntentHandler createSubscriptionCheckoutIntentHandler,
        GetCurrentBusinessAccessStateHandler getCurrentBusinessAccessStateHandler,
        IConfiguration configuration,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _getBusinessSubscriptionStatusHandler = getBusinessSubscriptionStatusHandler ?? throw new ArgumentNullException(nameof(getBusinessSubscriptionStatusHandler));
        _setCancelAtPeriodEndHandler = setCancelAtPeriodEndHandler ?? throw new ArgumentNullException(nameof(setCancelAtPeriodEndHandler));
        _getBillingPlansHandler = getBillingPlansHandler ?? throw new ArgumentNullException(nameof(getBillingPlansHandler));
        _createSubscriptionCheckoutIntentHandler = createSubscriptionCheckoutIntentHandler ?? throw new ArgumentNullException(nameof(createSubscriptionCheckoutIntentHandler));
        _getCurrentBusinessAccessStateHandler = getCurrentBusinessAccessStateHandler ?? throw new ArgumentNullException(nameof(getCurrentBusinessAccessStateHandler));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    /// <summary>
    /// Returns current subscription snapshot for the authenticated business.
    /// </summary>
    [HttpGet("subscription/current")]
    [HttpGet("/api/v1/billing/business/subscription/current")]
    [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
    [ProducesResponseType(typeof(BusinessSubscriptionStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCurrentBusinessSubscriptionAsync(CancellationToken ct = default)
    {
        var (hasBusinessAccess, businessId, errorResult) = await TryGetCurrentBusinessIdAsync(requireOperationsAllowed: false, ct).ConfigureAwait(false);
        if (!hasBusinessAccess)
        {
            return errorResult ?? Forbid();
        }

        var result = await _getBusinessSubscriptionStatusHandler
            .HandleAsync(businessId, ct)
            .ConfigureAwait(false);

        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result, _validationLocalizer["BusinessSubscriptionStatusRetrievalFailed"]);
        }

        return Ok(MapStatus(result.Value));
    }

    /// <summary>
    /// Updates cancel-at-period-end preference for authenticated business subscription.
    /// </summary>
    [HttpPost("subscription/cancel-at-period-end")]
    [HttpPost("/api/v1/billing/business/subscription/cancel-at-period-end")]
    [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
    [ProducesResponseType(typeof(SetCancelAtPeriodEndResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SetCancelAtPeriodEndAsync([FromBody] SetCancelAtPeriodEndRequest request, CancellationToken ct = default)
    {
        var (hasBusinessAccess, businessId, errorResult) = await TryGetCurrentBusinessIdAsync(requireOperationsAllowed: false, ct).ConfigureAwait(false);
        if (!hasBusinessAccess)
        {
            return errorResult ?? Forbid();
        }

        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        var result = await _setCancelAtPeriodEndHandler
            .HandleAsync(
                businessId,
                request.SubscriptionId,
                request.CancelAtPeriodEnd,
                request.RowVersion,
                ct)
            .ConfigureAwait(false);

        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result, _validationLocalizer["SubscriptionCancellationUpdateFailed"]);
        }

        return Ok(new SetCancelAtPeriodEndResponse
        {
            SubscriptionId = result.Value.SubscriptionId,
            CancelAtPeriodEnd = result.Value.CancelAtPeriodEnd,
            RowVersion = result.Value.RowVersion
        });
    }


    /// <summary>
    /// Returns available billing plans for subscription upgrade/checkout decisions.
    /// </summary>
    [HttpGet("plans")]
    [HttpGet("/api/v1/billing/plans")]
    [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
    [ProducesResponseType(typeof(GetBillingPlansResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBillingPlansAsync([FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        // Business claim gate remains explicit to keep endpoint visibility consistent with other business billing operations.
        var (hasBusinessAccess, _, errorResult) = await TryGetCurrentBusinessIdAsync(requireOperationsAllowed: false, ct).ConfigureAwait(false);
        if (!hasBusinessAccess)
        {
            return errorResult ?? Forbid();
        }

        var dto = await _getBillingPlansHandler
            .HandleAsync(activeOnly, ct)
            .ConfigureAwait(false);

        var response = new GetBillingPlansResponse
        {
            Items = dto.Items
                .Select(x => new BillingPlanSummary
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Description = x.Description,
                    PriceMinor = x.PriceMinor,
                    Currency = x.Currency,
                    Interval = x.Interval,
                    IntervalCount = x.IntervalCount,
                    TrialDays = x.TrialDays,
                    IsActive = x.IsActive
                })
                .ToList()
        };

        return Ok(response);
    }


    /// <summary>
    /// Creates a checkout-intent URL for subscription upgrade/checkout.
    /// </summary>
    [HttpPost("subscription/checkout-intent")]
    [HttpPost("/api/v1/billing/business/subscription/checkout-intent")]
    [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
    [ProducesResponseType(typeof(CreateSubscriptionCheckoutIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateSubscriptionCheckoutIntentAsync([FromBody] CreateSubscriptionCheckoutIntentRequest request, CancellationToken ct = default)
    {
        var (hasBusinessAccess, businessId, errorResult) = await TryGetCurrentBusinessIdAsync(requireOperationsAllowed: false, ct).ConfigureAwait(false);
        if (!hasBusinessAccess)
        {
            return errorResult ?? Forbid();
        }

        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        var validation = await _createSubscriptionCheckoutIntentHandler
            .ValidateAsync(businessId, request.PlanId, ct)
            .ConfigureAwait(false);

        if (!validation.Succeeded)
        {
            return ProblemFromResult(validation, _validationLocalizer["CheckoutIntentCreationFailed"]);
        }

        var checkoutBaseUrl = _configuration["Billing:CheckoutBaseUrl"];
        if (string.IsNullOrWhiteSpace(checkoutBaseUrl) || !Uri.TryCreate(checkoutBaseUrl, UriKind.Absolute, out var baseUri))
        {
            return BadRequestProblem(_validationLocalizer["BillingCheckoutEndpointNotConfigured"]);
        }

        // Build query string via framework helpers so URL composition remains safe
        // when base URL already contains path/query components.
        var queryBuilder = new QueryBuilder
        {
            { "businessId", businessId.ToString("D") },
            { "planId", request.PlanId.ToString("D") }
        };

        var checkoutUrl = new UriBuilder(baseUri)
        {
            Query = queryBuilder.ToQueryString().Value?.TrimStart('?')
        }.Uri.AbsoluteUri;

        return Ok(new CreateSubscriptionCheckoutIntentResponse
        {
            CheckoutUrl = checkoutUrl,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15),
            Provider = "Stripe"
        });
    }

    private static BusinessSubscriptionStatusResponse MapStatus(BusinessSubscriptionStatusDto dto)
        => new()
        {
            HasSubscription = dto.HasSubscription,
            SubscriptionId = dto.SubscriptionId,
            RowVersion = dto.RowVersion,
            Status = dto.Status,
            Provider = dto.Provider,
            PlanCode = dto.PlanCode,
            PlanName = dto.PlanName,
            UnitPriceMinor = dto.UnitPriceMinor,
            Currency = dto.Currency,
            StartedAtUtc = dto.StartedAtUtc,
            CurrentPeriodEndUtc = dto.CurrentPeriodEndUtc,
            TrialEndsAtUtc = dto.TrialEndsAtUtc,
            CanceledAtUtc = dto.CanceledAtUtc,
            CancelAtPeriodEnd = dto.CancelAtPeriodEnd
        };

    private async Task<(bool Success, Guid BusinessId, IActionResult? ErrorResult)> TryGetCurrentBusinessIdAsync(bool requireOperationsAllowed, CancellationToken ct)
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

        if (!accessState.IsBusinessClientAccessAllowed)
        {
            return (false, Guid.Empty, ForbiddenProblem(detail: BusinessAccessStateMessageLocalizer.LocalizeBlockingReason(accessState, _validationLocalizer)));
        }

        if (requireOperationsAllowed && !accessState.IsOperationsAllowed)
        {
            return (false, Guid.Empty, ForbiddenProblem(detail: BusinessAccessStateMessageLocalizer.LocalizeBlockingReason(accessState, _validationLocalizer)));
        }

        return (true, businessId, null);
    }
}
