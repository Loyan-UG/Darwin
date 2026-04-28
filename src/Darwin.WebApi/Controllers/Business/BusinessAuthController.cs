using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Identity;
using Darwin.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

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
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    public BusinessAuthController(
        GetBusinessInvitationPreviewHandler getBusinessInvitationPreviewHandler,
        AcceptBusinessInvitationHandler acceptBusinessInvitationHandler,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _getBusinessInvitationPreviewHandler = getBusinessInvitationPreviewHandler ?? throw new ArgumentNullException(nameof(getBusinessInvitationPreviewHandler));
        _acceptBusinessInvitationHandler = acceptBusinessInvitationHandler ?? throw new ArgumentNullException(nameof(acceptBusinessInvitationHandler));
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    [HttpGet("invitations/preview")]
    [HttpGet("/api/v1/auth/business-invitations/preview")]
    [ProducesResponseType(typeof(BusinessInvitationPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PreviewInvitationAsync([FromQuery] string? token, CancellationToken ct = default)
    {
        var normalizedToken = NormalizeText(token);
        if (normalizedToken is null)
        {
            return BadRequestProblem(_validationLocalizer["InvitationTokenRequired"]);
        }

        var result = await _getBusinessInvitationPreviewHandler.HandleAsync(normalizedToken, ct).ConfigureAwait(false);
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
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        var result = await _acceptBusinessInvitationHandler.HandleAsync(new BusinessInvitationAcceptDto
        {
            Token = NormalizeText(request.Token) ?? string.Empty,
            DeviceId = NormalizeText(request.DeviceId),
            FirstName = NormalizeText(request.FirstName),
            LastName = NormalizeText(request.LastName),
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

    private static string? NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
