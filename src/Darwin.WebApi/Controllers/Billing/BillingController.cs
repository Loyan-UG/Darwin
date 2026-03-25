using Darwin.Application.Billing;
using Darwin.Contracts.Billing;
using Darwin.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.WebApi.Controllers.Billing;

/// <summary>
/// Business billing endpoints for mobile operators.
/// </summary>
[ApiController]
[Route("api/v1/billing")]
[Authorize]
public sealed class BillingController : ApiControllerBase
{
    private readonly GetBusinessSubscriptionStatusHandler _getBusinessSubscriptionStatusHandler;
    private readonly SetCancelAtPeriodEndHandler _setCancelAtPeriodEndHandler;
    private readonly GetBillingPlansHandler _getBillingPlansHandler;
    private readonly CreateSubscriptionCheckoutIntentHandler _createSubscriptionCheckoutIntentHandler;
    private readonly IConfiguration _configuration;

    public BillingController(
        GetBusinessSubscriptionStatusHandler getBusinessSubscriptionStatusHandler,
        SetCancelAtPeriodEndHandler setCancelAtPeriodEndHandler,
        GetBillingPlansHandler getBillingPlansHandler,
        CreateSubscriptionCheckoutIntentHandler createSubscriptionCheckoutIntentHandler,
        IConfiguration configuration)
    {
        _getBusinessSubscriptionStatusHandler = getBusinessSubscriptionStatusHandler ?? throw new ArgumentNullException(nameof(getBusinessSubscriptionStatusHandler));
        _setCancelAtPeriodEndHandler = setCancelAtPeriodEndHandler ?? throw new ArgumentNullException(nameof(setCancelAtPeriodEndHandler));
        _getBillingPlansHandler = getBillingPlansHandler ?? throw new ArgumentNullException(nameof(getBillingPlansHandler));
        _createSubscriptionCheckoutIntentHandler = createSubscriptionCheckoutIntentHandler ?? throw new ArgumentNullException(nameof(createSubscriptionCheckoutIntentHandler));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Returns current subscription snapshot for the authenticated business.
    /// </summary>
    [HttpGet("business/subscription/current")]
    [HttpGet("/api/v1/business/billing/subscription/current")]
    [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
    [ProducesResponseType(typeof(BusinessSubscriptionStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCurrentBusinessSubscriptionAsync(CancellationToken ct = default)
    {
        if (!TryGetCurrentBusinessId(out var businessId, out var errorResult))
        {
            return errorResult ?? Forbid();
        }

        var result = await _getBusinessSubscriptionStatusHandler
            .HandleAsync(businessId, ct)
            .ConfigureAwait(false);

        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result, "Failed to retrieve business subscription status.");
        }

        return Ok(MapStatus(result.Value));
    }

    /// <summary>
    /// Updates cancel-at-period-end preference for authenticated business subscription.
    /// </summary>
    [HttpPost("business/subscription/cancel-at-period-end")]
    [HttpPost("/api/v1/business/billing/subscription/cancel-at-period-end")]
    [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
    [ProducesResponseType(typeof(SetCancelAtPeriodEndResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SetCancelAtPeriodEndAsync([FromBody] SetCancelAtPeriodEndRequest request, CancellationToken ct = default)
    {
        if (!TryGetCurrentBusinessId(out var businessId, out var errorResult))
        {
            return errorResult ?? Forbid();
        }

        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
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
            return ProblemFromResult(result, "Failed to update subscription cancellation preference.");
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
    [HttpGet("/api/v1/business/billing/plans")]
    [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
    [ProducesResponseType(typeof(GetBillingPlansResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBillingPlansAsync([FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        // Business claim gate remains explicit to keep endpoint visibility consistent with other business billing operations.
        if (!TryGetCurrentBusinessId(out _, out var errorResult))
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
    [HttpPost("business/subscription/checkout-intent")]
    [HttpPost("/api/v1/business/billing/subscription/checkout-intent")]
    [Authorize(Policy = "perm:AccessLoyaltyBusiness")]
    [ProducesResponseType(typeof(CreateSubscriptionCheckoutIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateSubscriptionCheckoutIntentAsync([FromBody] CreateSubscriptionCheckoutIntentRequest request, CancellationToken ct = default)
    {
        if (!TryGetCurrentBusinessId(out var businessId, out var errorResult))
        {
            return errorResult ?? Forbid();
        }

        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
        }

        var validation = await _createSubscriptionCheckoutIntentHandler
            .ValidateAsync(businessId, request.PlanId, ct)
            .ConfigureAwait(false);

        if (!validation.Succeeded)
        {
            return ProblemFromResult(validation, "Unable to create checkout intent.");
        }

        var checkoutBaseUrl = _configuration["Billing:CheckoutBaseUrl"];
        if (string.IsNullOrWhiteSpace(checkoutBaseUrl) || !Uri.TryCreate(checkoutBaseUrl, UriKind.Absolute, out var baseUri))
        {
            return BadRequestProblem("Billing checkout endpoint is not configured.");
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

    private bool TryGetCurrentBusinessId(out Guid businessId, out IActionResult? errorResult)
    {
        businessId = Guid.Empty;
        errorResult = null;

        var claimValue = User?.FindFirst("business_id")?.Value;
        if (string.IsNullOrWhiteSpace(claimValue) || !Guid.TryParse(claimValue, out businessId))
        {
            errorResult = Forbid();
            businessId = Guid.Empty;
            return false;
        }

        return true;
    }
}
