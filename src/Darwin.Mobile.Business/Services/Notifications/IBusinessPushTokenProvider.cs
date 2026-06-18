using System.Threading;
using System.Threading.Tasks;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Business.Services.Notifications;

/// <summary>
/// Resolves current push-token state for the running Business installation.
/// </summary>
public interface IBusinessPushTokenProvider
{
    Task<Result<BusinessPushTokenState>> GetCurrentAsync(CancellationToken cancellationToken);
}

public sealed class BusinessPushTokenState
{
    public string? PushToken { get; init; }
    public bool NotificationsEnabled { get; init; } = true;
}
