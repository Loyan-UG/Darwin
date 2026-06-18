using System.Threading;
using System.Threading.Tasks;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Business.Services.Notifications;

/// <summary>
/// Coordinates notification disclosure and platform permission requests for Business app.
/// </summary>
public interface IBusinessNotificationPermissionService
{
    Task<Result<bool>> EnsurePermissionAsync(CancellationToken cancellationToken);
}
