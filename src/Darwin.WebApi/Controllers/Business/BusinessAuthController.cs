using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Identity;
using Darwin.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.WebApi.Controllers.Business;

/// <summary>
/// Business-app onboarding endpoints for invitation preview and acceptance.
/// These endpoints are intentionally anonymous because invited operators have not authenticated yet.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/business/auth")]
public sealed class BusinessAuthController : ApiControllerBase
{
    private readonly GetBusinessInvitationPreviewHandler _getBusinessInvitationPreviewHandler;
    private readonly AcceptBusinessInvitationHandler _acceptBusinessInvitationHandler;

    public BusinessAuthController(
        GetBusinessInvitationPreviewHandler getBusinessInvitationPreviewHandler,
        AcceptBusinessInvitationHandler acceptBusinessInvitationHandler)
    {
        _getBusinessInvitationPreviewHandler = getBusinessInvitationPreviewHandler ?? throw new ArgumentNullException(nameof(getBusinessInvitationPreviewHandler));
        _acceptBusinessInvitationHandler = acceptBusinessInvitationHandler ?? throw new ArgumentNullException(nameof(acceptBusinessInvitationHandler));
    }

    [HttpGet("invitations/preview")]
    [HttpGet("/api/v1/auth/business-invitations/preview")]
    [ProducesResponseType(typeof(BusinessInvitationPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PreviewInvitationAsync([FromQuery] string? token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequestProblem("Invitation token is required.");
        }

        var result = await _getBusinessInvitationPreviewHandler.HandleAsync(token, ct).ConfigureAwait(false);
        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result);
        }

        var dto = result.Value;
        return Ok(new BusinessInvitationPreviewResponse
        {
            InvitationId = dto.InvitationId,
            BusinessId = dto.BusinessId,
            BusinessName = dto.BusinessName,
            Email = dto.Email,
            Role = dto.Role,
            Status = dto.Status,
            ExpiresAtUtc = dto.ExpiresAtUtc,
            HasExistingUser = dto.HasExistingUser
        });
    }

    [HttpPost("invitations/accept")]
    [HttpPost("/api/v1/auth/business-invitations/accept")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptInvitationAsync([FromBody] AcceptBusinessInvitationRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequestProblem("Request body is required.");
        }

        var result = await _acceptBusinessInvitationHandler.HandleAsync(new BusinessInvitationAcceptDto
        {
            Token = request.Token ?? string.Empty,
            DeviceId = request.DeviceId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Password = request.Password
        }, ct).ConfigureAwait(false);

        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result);
        }

        return Ok(new TokenResponse
        {
            AccessToken = result.Value.AccessToken,
            AccessTokenExpiresAtUtc = result.Value.AccessTokenExpiresAtUtc,
            RefreshToken = result.Value.RefreshToken,
            RefreshTokenExpiresAtUtc = result.Value.RefreshTokenExpiresAtUtc,
            UserId = result.Value.UserId,
            Email = result.Value.Email
        });
    }
}
