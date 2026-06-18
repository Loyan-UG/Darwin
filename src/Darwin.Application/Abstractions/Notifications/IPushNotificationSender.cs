using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Shared.Results;

namespace Darwin.Application.Abstractions.Notifications;

public interface IPushNotificationSender
{
    Task<Result<PushNotificationSendResult>> SendAsync(PushNotificationSendRequest request, CancellationToken ct);
}

public sealed class PushNotificationSendRequest
{
    public Guid NotificationId { get; init; }
    public Guid UserId { get; init; }
    public string DeviceId { get; init; } = string.Empty;
    public string PushToken { get; init; } = string.Empty;
    public string Platform { get; init; } = "Unknown";
    public string TargetApp { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? DeepLink { get; init; }
    public string? SourceType { get; init; }
    public Guid? SourceId { get; init; }
    public string? CollapseKey { get; init; }
    public string? AnalyticsLabel { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed class PushNotificationSendResult
{
    public int? ResponseCode { get; init; }
    public string? ProviderMessageId { get; init; }
    public string? FailureCode { get; init; }
    public bool IsTransientFailure { get; init; }
    public bool IsInvalidTokenFailure { get; init; }
}
