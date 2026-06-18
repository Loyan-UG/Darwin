using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Notifications;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Shared.Services.Notifications;

public interface INotificationInboxService
{
    Task<Result<NotificationInboxListResponse>> GetAsync(
        NotificationInboxTargetApp targetApp,
        NotificationInboxCategory? category,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<Result<int>> GetUnreadCountAsync(NotificationInboxTargetApp targetApp, CancellationToken ct);

    Task<Result<int>> MarkReadAsync(System.Guid id, NotificationInboxTargetApp targetApp, CancellationToken ct);

    Task<Result<int>> MarkAllReadAsync(NotificationInboxTargetApp targetApp, CancellationToken ct);
}
