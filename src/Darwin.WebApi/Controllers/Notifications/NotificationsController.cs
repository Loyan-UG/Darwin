using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Contracts.Notifications;
using Darwin.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Darwin.WebApi.Controllers.Notifications;

/// <summary>
/// Endpoints for client notification/device registration operations.
/// </summary>
[ApiController]
[Route("api/v1/member/notifications")]
[Authorize]
public sealed class NotificationsController : ApiControllerBase
{
    private readonly RegisterOrUpdateUserDeviceHandler _registerOrUpdateUserDeviceHandler;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    public NotificationsController(
        RegisterOrUpdateUserDeviceHandler registerOrUpdateUserDeviceHandler,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _registerOrUpdateUserDeviceHandler = registerOrUpdateUserDeviceHandler
            ?? throw new ArgumentNullException(nameof(registerOrUpdateUserDeviceHandler));
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    /// <summary>
    /// Registers or updates the authenticated user's mobile device installation for push delivery.
    /// </summary>
    [HttpPost("devices/register")]
    [HttpPost("/api/v1/notifications/devices/register")]
    [ProducesResponseType(typeof(RegisterPushDeviceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterDeviceAsync([FromBody] RegisterPushDeviceRequest? request, CancellationToken ct)
    {
        if (request is null)
        {
            return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
        }

        var userId = GetUserIdFromClaims(User);
        if (userId is null)
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new Darwin.Contracts.Common.ProblemDetails
            {
                Status = 401,
                Title = _validationLocalizer["UnauthorizedTitle"],
                Detail = _validationLocalizer["AuthenticatedUserIdentifierNotResolved"],
                Instance = HttpContext.Request?.Path.Value
            });
        }

        var dto = new RegisterUserDeviceDto
        {
            UserId = userId.Value,
            DeviceId = request.DeviceId,
            Platform = ToDomainPlatform(request.Platform),
            PushToken = request.PushToken,
            NotificationsEnabled = request.NotificationsEnabled,
            AppVersion = request.AppVersion,
            DeviceModel = request.DeviceModel
        };

        var result = await _registerOrUpdateUserDeviceHandler.HandleAsync(dto, ct).ConfigureAwait(false);
        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result);
        }

        return Ok(new RegisterPushDeviceResponse
        {
            DeviceId = result.Value.DeviceId,
            RegisteredAtUtc = result.Value.RegisteredAtUtc
        });
    }

    private static MobilePlatform ToDomainPlatform(MobileDevicePlatform platform)
        => platform switch
        {
            MobileDevicePlatform.Android => MobilePlatform.Android,
            MobileDevicePlatform.iOS => MobilePlatform.iOS,
            _ => MobilePlatform.Unknown
        };

    private static Guid? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var id =
            user.FindFirstValue(ClaimTypes.NameIdentifier) ??
            user.FindFirstValue("sub") ??
            user.FindFirstValue("uid");

        return Guid.TryParse(id, out var guid) ? guid : null;
    }
}
