using Darwin.Application;
using Darwin.Application.Businesses.Queries;
using Darwin.Contracts.Businesses;
using Darwin.WebApi.Controllers;
using Darwin.WebApi.Controllers.Businesses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Darwin.WebApi.Controllers.Business;

/// <summary>
/// Business-account endpoints that expose current operational access and onboarding readiness for business clients.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/business/account")]
public sealed class BusinessAccountController : ApiControllerBase
{
    private readonly GetCurrentBusinessAccessStateHandler _getCurrentBusinessAccessStateHandler;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessAccountController"/> class.
    /// </summary>
    public BusinessAccountController(
        GetCurrentBusinessAccessStateHandler getCurrentBusinessAccessStateHandler,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _getCurrentBusinessAccessStateHandler = getCurrentBusinessAccessStateHandler ?? throw new ArgumentNullException(nameof(getCurrentBusinessAccessStateHandler));
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    /// <summary>
    /// Returns the current business access-state snapshot for the authenticated business operator.
    /// </summary>
    [HttpGet("access-state")]
    [ProducesResponseType(typeof(BusinessAccessStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccessStateAsync(CancellationToken ct = default)
    {
        if (!BusinessControllerConventions.TryGetCurrentBusinessId(User, out var businessId))
        {
            return BadRequestProblem(_validationLocalizer["BusinessRequired"]);
        }

        if (!BusinessControllerConventions.TryGetCurrentUserId(User, out var userId))
        {
            return BadRequestProblem(_validationLocalizer["UserRequired"]);
        }

        var dto = await _getCurrentBusinessAccessStateHandler.HandleAsync(businessId, userId, ct).ConfigureAwait(false);
        if (dto is null)
        {
            return NotFoundProblem(_validationLocalizer["BusinessNotFound"]);
        }

        var blockingReason = BusinessAccessStateMessageLocalizer.LocalizeBlockingReason(dto, _validationLocalizer);

        return Ok(new BusinessAccessStateResponse
        {
            UserId = dto.UserId,
            BusinessId = dto.BusinessId,
            BusinessName = dto.BusinessName,
            OperationalStatus = dto.OperationalStatus.ToString(),
            IsActive = dto.IsActive,
            ApprovedAtUtc = dto.ApprovedAtUtc,
            SuspendedAtUtc = dto.SuspendedAtUtc,
            SuspensionReason = dto.SuspensionReason,
            HasActiveOwner = dto.HasActiveOwner,
            HasPrimaryLocation = dto.HasPrimaryLocation,
            HasContactEmail = dto.HasContactEmail,
            HasLegalName = dto.HasLegalName,
            HasActiveMembership = dto.HasActiveMembership,
            IsUserActive = dto.IsUserActive,
            IsUserEmailConfirmed = dto.IsUserEmailConfirmed,
            IsUserLockedOut = dto.IsUserLockedOut,
            IsApprovalPending = dto.IsApprovalPending,
            IsSuspended = dto.IsSuspended,
            IsBusinessClientAccessAllowed = dto.IsBusinessClientAccessAllowed,
            IsOperationsAllowed = dto.IsOperationsAllowed,
            IsSetupComplete = dto.IsSetupComplete,
            HasActivationBlockingIssues = dto.HasActivationBlockingIssues,
            SetupIncompleteItemCount = dto.SetupIncompleteItemCount,
            PrimaryBlockingCode = dto.PrimaryBlockingCode,
            BlockingReason = blockingReason
        });
    }
}
