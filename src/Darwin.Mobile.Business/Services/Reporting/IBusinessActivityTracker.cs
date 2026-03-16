using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Business.Services.Reporting;

/// <summary>
/// Tracks business-side scanner/session activities for dashboard and lightweight reporting use cases.
/// </summary>
public interface IBusinessActivityTracker
{
    /// <summary>
    /// Records a successful session load event.
    /// </summary>
    Task RecordSessionLoadedAsync(string? customerDisplayName, CancellationToken cancellationToken);

    /// <summary>
    /// Records a successful accrual confirmation.
    /// </summary>
    Task RecordAccrualConfirmedAsync(string? customerDisplayName, int pointsAccrued, CancellationToken cancellationToken);

    /// <summary>
    /// Records a successful redemption confirmation.
    /// </summary>
    Task RecordRedemptionConfirmedAsync(string? customerDisplayName, int pointsRedeemed, CancellationToken cancellationToken);

    /// <summary>
    /// Records subscription status refresh result for business settings telemetry.
    /// </summary>
    Task RecordSubscriptionStatusRefreshAsync(bool succeeded, CancellationToken cancellationToken);

    /// <summary>
    /// Records available subscription plans load result.
    /// </summary>
    Task RecordSubscriptionPlansLoadedAsync(int availablePlansCount, CancellationToken cancellationToken);

    /// <summary>
    /// Records checkout flow start intent from subscription settings.
    /// </summary>
    Task RecordSubscriptionCheckoutStartedAsync(string? targetPlanCode, CancellationToken cancellationToken);

    /// <summary>
    /// Records checkout flow failure event from subscription settings.
    /// </summary>
    Task RecordSubscriptionCheckoutFailedAsync(string? targetPlanCode, CancellationToken cancellationToken);

    /// <summary>
    /// Records cancel-at-period-end preference mutation result.
    /// </summary>
    Task RecordSubscriptionCancelPreferenceChangedAsync(bool cancelAtPeriodEnd, CancellationToken cancellationToken);

    /// <summary>
    /// Returns an aggregated dashboard snapshot for the specified lookback window.
    /// </summary>
    Task<BusinessDashboardSnapshot> GetDashboardSnapshotAsync(TimeSpan lookbackWindow, CancellationToken cancellationToken);
}

/// <summary>
/// Immutable dashboard projection used by the business dashboard screen.
/// </summary>
public sealed class BusinessDashboardSnapshot
{
    public int TotalSessions { get; init; }
    public int AccrualCount { get; init; }
    public int RedemptionCount { get; init; }
    public int TotalAccruedPoints { get; init; }
    public int TotalRedeemedPoints { get; init; }
    public IReadOnlyList<BusinessTopCustomerItem> TopCustomers { get; init; } = Array.Empty<BusinessTopCustomerItem>();
    public IReadOnlyList<BusinessActivityFeedItem> RecentActivities { get; init; } = Array.Empty<BusinessActivityFeedItem>();
}

/// <summary>
/// Aggregated top-customer row for dashboard list.
/// </summary>
public sealed class BusinessTopCustomerItem
{
    public string CustomerDisplayName { get; init; } = string.Empty;
    public int InteractionsCount { get; init; }
}

/// <summary>
/// Human-readable activity feed item used by dashboard timeline.
/// </summary>
public sealed class BusinessActivityFeedItem
{
    public DateTime OccurredAtUtc { get; init; }
    public string ActivityKind { get; init; } = string.Empty;
    public string CustomerDisplayName { get; init; } = string.Empty;
    public int PointsDelta { get; init; }
}
