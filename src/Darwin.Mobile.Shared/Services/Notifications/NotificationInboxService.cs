using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Notifications;
using Darwin.Mobile.Shared.Api;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Shared.Services.Notifications;

public sealed class NotificationInboxService : INotificationInboxService
{
    private readonly IApiClient _apiClient;

    public NotificationInboxService(IApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public Task<Result<NotificationInboxListResponse>> GetAsync(
        NotificationInboxTargetApp targetApp,
        NotificationInboxCategory? category,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var route = string.Create(CultureInfo.InvariantCulture,
            $"{ApiRoutes.Notifications.List}?targetApp={(short)targetApp}&page={Math.Max(1, page)}&pageSize={Math.Clamp(pageSize, 1, 100)}");
        if (category.HasValue)
        {
            route += string.Create(CultureInfo.InvariantCulture, $"&category={(short)category.Value}");
        }

        return _apiClient.GetResultAsync<NotificationInboxListResponse>(route, ct);
    }

    public async Task<Result<int>> GetUnreadCountAsync(NotificationInboxTargetApp targetApp, CancellationToken ct)
    {
        var route = string.Create(CultureInfo.InvariantCulture, $"{ApiRoutes.Notifications.UnreadCount}?targetApp={(short)targetApp}");
        var result = await _apiClient.GetResultAsync<NotificationUnreadCountResponse>(route, ct).ConfigureAwait(false);
        return result.Succeeded && result.Value is not null
            ? Result<int>.Ok(result.Value.UnreadCount)
            : Result<int>.Fail(result.Error ?? "Could not load unread notifications.");
    }

    public async Task<Result<int>> MarkReadAsync(Guid id, NotificationInboxTargetApp targetApp, CancellationToken ct)
    {
        var route = string.Create(CultureInfo.InvariantCulture, $"{ApiRoutes.Notifications.MarkRead(id)}?targetApp={(short)targetApp}");
        var result = await _apiClient.PostResultAsync<object, NotificationReadResponse>(route, new { }, ct).ConfigureAwait(false);
        return result.Succeeded && result.Value is not null
            ? Result<int>.Ok(result.Value.UnreadCount)
            : Result<int>.Fail(result.Error ?? "Could not mark notification as read.");
    }

    public async Task<Result<int>> MarkAllReadAsync(NotificationInboxTargetApp targetApp, CancellationToken ct)
    {
        var route = string.Create(CultureInfo.InvariantCulture, $"{ApiRoutes.Notifications.MarkAllRead}?targetApp={(short)targetApp}");
        var result = await _apiClient.PostResultAsync<object, NotificationReadResponse>(route, new { }, ct).ConfigureAwait(false);
        return result.Succeeded && result.Value is not null
            ? Result<int>.Ok(result.Value.UnreadCount)
            : Result<int>.Fail(result.Error ?? "Could not mark notifications as read.");
    }
}
