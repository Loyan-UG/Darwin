namespace Darwin.Domain.Enums;

/// <summary>
/// User-facing notification categories used by the internal inbox.
/// </summary>
public enum NotificationCategory : short
{
    System = 0,
    Campaign = 1,
    Reward = 2,
    Billing = 3,
    ScannerSession = 4,
    Account = 5
}

/// <summary>
/// Mobile app audience for an internal notification.
/// </summary>
public enum NotificationTargetApp : short
{
    Both = 0,
    Consumer = 1,
    Business = 2
}
