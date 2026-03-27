using Darwin.Application.Businesses.Queries;
using Darwin.Contracts.Businesses;
using Darwin.WebApi.Controllers;
using Darwin.WebApi.Controllers.Businesses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessAccountController"/> class.
    /// </summary>
    public BusinessAccountController(GetCurrentBusinessAccessStateHandler getCurrentBusinessAccessStateHandler)
    {
        _getCurrentBusinessAccessStateHandler = getCurrentBusinessAccessStateHandler ?? throw new ArgumentNullException(nameof(getCurrentBusinessAccessStateHandler));
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
            return BadRequestProblem("Business context is required.");
        }

        var dto = await _getCurrentBusinessAccessStateHandler.HandleAsync(businessId, ct).ConfigureAwait(false);
        if (dto is null)
        {
            return NotFoundProblem("Business was not found.");
        }

        return Ok(new BusinessAccessStateResponse
        {
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
            IsOperationsAllowed = dto.IsOperationsAllowed,
            IsSetupComplete = dto.IsSetupComplete,
            BlockingReason = dto.BlockingReason
        });
    }
}
