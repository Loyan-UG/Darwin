using Darwin.Application.Billing;
using Darwin.Contracts.Billing;
using Darwin.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
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

    public BillingController(
        GetBusinessSubscriptionStatusHandler getBusinessSubscriptionStatusHandler,
        SetCancelAtPeriodEndHandler setCancelAtPeriodEndHandler)
    {
        _getBusinessSubscriptionStatusHandler = getBusinessSubscriptionStatusHandler ?? throw new ArgumentNullException(nameof(getBusinessSubscriptionStatusHandler));
        _setCancelAtPeriodEndHandler = setCancelAtPeriodEndHandler ?? throw new ArgumentNullException(nameof(setCancelAtPeriodEndHandler));
    }

    /// <summary>
    /// Returns current subscription snapshot for the authenticated business.
    /// </summary>
    [HttpGet("business/subscription/current")]
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
