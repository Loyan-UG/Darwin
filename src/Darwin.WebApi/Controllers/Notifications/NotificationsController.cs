using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Notifications;
using Darwin.Contracts.Notifications;
using Darwin.Domain.Enums;
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
    private readonly GetNotificationInboxHandler _getNotificationInboxHandler;
    private readonly GetNotificationUnreadCountHandler _getNotificationUnreadCountHandler;
    private readonly MarkNotificationReadHandler _markNotificationReadHandler;
    private readonly MarkAllNotificationsReadHandler _markAllNotificationsReadHandler;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    public NotificationsController(
        RegisterOrUpdateUserDeviceHandler registerOrUpdateUserDeviceHandler,
        GetNotificationInboxHandler getNotificationInboxHandler,
        GetNotificationUnreadCountHandler getNotificationUnreadCountHandler,
        MarkNotificationReadHandler markNotificationReadHandler,
        MarkAllNotificationsReadHandler markAllNotificationsReadHandler,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _registerOrUpdateUserDeviceHandler = registerOrUpdateUserDeviceHandler
            ?? throw new ArgumentNullException(nameof(registerOrUpdateUserDeviceHandler));
        _getNotificationInboxHandler = getNotificationInboxHandler ?? throw new ArgumentNullException(nameof(getNotificationInboxHandler));
        _getNotificationUnreadCountHandler = getNotificationUnreadCountHandler ?? throw new ArgumentNullException(nameof(getNotificationUnreadCountHandler));
        _markNotificationReadHandler = markNotificationReadHandler ?? throw new ArgumentNullException(nameof(markNotificationReadHandler));
        _markAllNotificationsReadHandler = markAllNotificationsReadHandler ?? throw new ArgumentNullException(nameof(markAllNotificationsReadHandler));
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    [HttpGet]
    [ProducesResponseType(typeof(NotificationInboxListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] NotificationInboxTargetApp targetApp = NotificationInboxTargetApp.Both,
        [FromQuery] NotificationInboxCategory? category = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken ct = default)
    {
        var effectiveTargetApp = ResolveAuthorizedTargetApp(User, targetApp);
        var result = await _getNotificationInboxHandler
            .HandleAsync(new NotificationInboxQueryDto
            {
                TargetApp = ToDomainTargetApp(effectiveTargetApp),
                Category = category.HasValue ? ToDomainCategory(category.Value) : null,
                Page = page,
                PageSize = pageSize
            }, ct)
            .ConfigureAwait(false);

        if (!result.Succeeded || result.Value is null)
        {
            return ProblemFromResult(result);
        }

        return Ok(new NotificationInboxListResponse
        {
            Items = result.Value.Items.Select(ToContractItem).ToList(),
            Total = result.Value.Total
        });
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(NotificationUnreadCountResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCountAsync(
        [FromQuery] NotificationInboxTargetApp targetApp = NotificationInboxTargetApp.Both,
        CancellationToken ct = default)
    {
        var effectiveTargetApp = ResolveAuthorizedTargetApp(User, targetApp);
        var result = await _getNotificationUnreadCountHandler.HandleAsync(ToDomainTargetApp(effectiveTargetApp), ct).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return ProblemFromResult(result);
        }

        return Ok(new NotificationUnreadCountResponse { UnreadCount = result.Value });
    }

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(typeof(NotificationReadResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkReadAsync(
        Guid id,
        [FromQuery] NotificationInboxTargetApp targetApp = NotificationInboxTargetApp.Both,
        CancellationToken ct = default)
    {
        var effectiveTargetApp = ResolveAuthorizedTargetApp(User, targetApp);
        var result = await _markNotificationReadHandler.HandleAsync(id, ToDomainTargetApp(effectiveTargetApp), ct).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return ProblemFromResult(result);
        }

        return Ok(new NotificationReadResponse { UnreadCount = result.Value });
    }

    [HttpPost("read-all")]
    [ProducesResponseType(typeof(NotificationReadResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllReadAsync(
        [FromQuery] NotificationInboxTargetApp targetApp = NotificationInboxTargetApp.Both,
        CancellationToken ct = default)
    {
        var effectiveTargetApp = ResolveAuthorizedTargetApp(User, targetApp);
        var result = await _markAllNotificationsReadHandler.HandleAsync(ToDomainTargetApp(effectiveTargetApp), ct).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return ProblemFromResult(result);
        }

        return Ok(new NotificationReadResponse { UnreadCount = result.Value });
    }

    /// <summary>
    /// Registers or updates the authenticated user's mobile device installation for push delivery.
    /// </summary>
    [HttpPost("devices/register")]
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
            DeviceId = NormalizeRequired(request.DeviceId),
            Platform = ToDomainPlatform(request.Platform),
            PushToken = NormalizeNullable(request.PushToken),
            NotificationsEnabled = request.NotificationsEnabled,
            AppVersion = NormalizeNullable(request.AppVersion),
            DeviceModel = NormalizeNullable(request.DeviceModel)
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

    private static NotificationTargetApp ToDomainTargetApp(NotificationInboxTargetApp targetApp)
        => targetApp switch
        {
            NotificationInboxTargetApp.Consumer => NotificationTargetApp.Consumer,
            NotificationInboxTargetApp.Business => NotificationTargetApp.Business,
            _ => NotificationTargetApp.Both
        };

    private static NotificationInboxTargetApp ResolveAuthorizedTargetApp(ClaimsPrincipal user, NotificationInboxTargetApp requested)
    {
        var hasBusinessContext = user.FindFirstValue("business_id") is { Length: > 0 };
        var allowed = hasBusinessContext ? NotificationInboxTargetApp.Business : NotificationInboxTargetApp.Consumer;
        return requested == NotificationInboxTargetApp.Both || requested == allowed
            ? allowed
            : allowed;
    }

    private static NotificationCategory ToDomainCategory(NotificationInboxCategory category)
        => category switch
        {
            NotificationInboxCategory.Campaign => NotificationCategory.Campaign,
            NotificationInboxCategory.Reward => NotificationCategory.Reward,
            NotificationInboxCategory.Billing => NotificationCategory.Billing,
            NotificationInboxCategory.ScannerSession => NotificationCategory.ScannerSession,
            NotificationInboxCategory.Account => NotificationCategory.Account,
            _ => NotificationCategory.System
        };

    private static NotificationInboxItem ToContractItem(NotificationInboxItemDto item)
        => new()
        {
            Id = item.Id,
            MessageId = item.MessageId,
            Title = item.Title,
            Body = item.Body,
            Category = item.Category switch
            {
                NotificationCategory.Campaign => NotificationInboxCategory.Campaign,
                NotificationCategory.Reward => NotificationInboxCategory.Reward,
                NotificationCategory.Billing => NotificationInboxCategory.Billing,
                NotificationCategory.ScannerSession => NotificationInboxCategory.ScannerSession,
                NotificationCategory.Account => NotificationInboxCategory.Account,
                _ => NotificationInboxCategory.System
            },
            CreatedAtUtc = item.CreatedAtUtc,
            ReadAtUtc = item.ReadAtUtc,
            DeepLink = item.DeepLink,
            SourceType = item.SourceType,
            SourceId = item.SourceId
        };

    private static string NormalizeRequired(string? value)
        => value?.Trim() ?? string.Empty;

    private static string? NormalizeNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
