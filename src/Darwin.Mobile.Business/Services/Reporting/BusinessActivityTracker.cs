using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace Darwin.Mobile.Business.Services.Reporting;

/// <summary>
/// Preference-backed implementation of business activity tracker.
/// </summary>
/// <remarks>
/// Persistence design:
/// - Stores a rolling activity list in <see cref="Preferences"/> as JSON.
/// - Keeps write path simple and deterministic to avoid additional local database dependencies.
/// - Trims old entries to protect storage size and keep reporting queries lightweight.
/// </remarks>
public sealed class BusinessActivityTracker : IBusinessActivityTracker
{
    private const string StorageKey = "business.activity.log.v1";
    private const int MaxStoredEntries = 500;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public Task RecordSessionLoadedAsync(string? customerDisplayName, CancellationToken cancellationToken)
        => AppendAsync(BusinessActivityKind.SessionLoaded, customerDisplayName, pointsDelta: 0, cancellationToken);

    public Task RecordAccrualConfirmedAsync(string? customerDisplayName, int pointsAccrued, CancellationToken cancellationToken)
        => AppendAsync(BusinessActivityKind.AccrualConfirmed, customerDisplayName, Math.Max(0, pointsAccrued), cancellationToken);

    public Task RecordRedemptionConfirmedAsync(string? customerDisplayName, int pointsRedeemed, CancellationToken cancellationToken)
        => AppendAsync(BusinessActivityKind.RedemptionConfirmed, customerDisplayName, Math.Max(0, pointsRedeemed), cancellationToken);

    public Task RecordSubscriptionStatusRefreshAsync(bool succeeded, CancellationToken cancellationToken)
        => AppendAsync(
            succeeded ? BusinessActivityKind.SubscriptionStatusRefreshSucceeded : BusinessActivityKind.SubscriptionStatusRefreshFailed,
            customerDisplayName: "Subscription settings",
            pointsDelta: 0,
            cancellationToken);

    public Task RecordSubscriptionPlansLoadedAsync(int availablePlansCount, CancellationToken cancellationToken)
        => AppendAsync(
            BusinessActivityKind.SubscriptionPlansLoaded,
            customerDisplayName: "Subscription settings",
            pointsDelta: Math.Max(0, availablePlansCount),
            cancellationToken);

    public Task RecordSubscriptionCheckoutStartedAsync(string? targetPlanCode, CancellationToken cancellationToken)
        => AppendAsync(
            BusinessActivityKind.SubscriptionCheckoutStarted,
            customerDisplayName: string.IsNullOrWhiteSpace(targetPlanCode) ? "Subscription checkout" : targetPlanCode,
            pointsDelta: 0,
            cancellationToken);

    public Task RecordSubscriptionCheckoutFailedAsync(string? targetPlanCode, CancellationToken cancellationToken)
        => AppendAsync(
            BusinessActivityKind.SubscriptionCheckoutFailed,
            customerDisplayName: string.IsNullOrWhiteSpace(targetPlanCode) ? "Subscription checkout" : targetPlanCode,
            pointsDelta: 0,
            cancellationToken);

    public Task RecordSubscriptionCancelPreferenceChangedAsync(bool cancelAtPeriodEnd, CancellationToken cancellationToken)
        => AppendAsync(
            cancelAtPeriodEnd
                ? BusinessActivityKind.SubscriptionCancelAtPeriodEndEnabled
                : BusinessActivityKind.SubscriptionCancelAtPeriodEndDisabled,
            customerDisplayName: "Subscription settings",
            pointsDelta: 0,
            cancellationToken);

    public Task<BusinessDashboardSnapshot> GetDashboardSnapshotAsync(TimeSpan lookbackWindow, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTime.UtcNow;
        var fromUtc = now - (lookbackWindow <= TimeSpan.Zero ? TimeSpan.FromDays(7) : lookbackWindow);

        var entries = LoadEntries()
            .Where(x => x.OccurredAtUtc >= fromUtc)
            .OrderByDescending(x => x.OccurredAtUtc)
            .ToList();

        var totalSessions = entries.Count(x => x.Kind == BusinessActivityKind.SessionLoaded);
        var accrualEntries = entries.Where(x => x.Kind == BusinessActivityKind.AccrualConfirmed).ToList();
        var redemptionEntries = entries.Where(x => x.Kind == BusinessActivityKind.RedemptionConfirmed).ToList();

        var topCustomers = entries
            .GroupBy(x => NormalizeCustomer(x.CustomerDisplayName))
            .Select(g => new BusinessTopCustomerItem
            {
                CustomerDisplayName = g.Key,
                InteractionsCount = g.Count()
            })
            .OrderByDescending(x => x.InteractionsCount)
            .ThenBy(x => x.CustomerDisplayName)
            .Take(5)
            .ToList();

        var recentActivities = entries
            .Take(20)
            .Select(x => new BusinessActivityFeedItem
            {
                OccurredAtUtc = x.OccurredAtUtc,
                ActivityKind = x.Kind.ToString(),
                CustomerDisplayName = NormalizeCustomer(x.CustomerDisplayName),
                PointsDelta = x.PointsDelta
            })
            .ToList();

        var snapshot = new BusinessDashboardSnapshot
        {
            TotalSessions = totalSessions,
            AccrualCount = accrualEntries.Count,
            RedemptionCount = redemptionEntries.Count,
            TotalAccruedPoints = accrualEntries.Sum(x => x.PointsDelta),
            TotalRedeemedPoints = redemptionEntries.Sum(x => x.PointsDelta),
            TopCustomers = topCustomers,
            RecentActivities = recentActivities
        };

        return Task.FromResult(snapshot);
    }

    private Task AppendAsync(BusinessActivityKind kind, string? customerDisplayName, int pointsDelta, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entries = LoadEntries();
        entries.Add(new BusinessActivityEntry
        {
            OccurredAtUtc = DateTime.UtcNow,
            Kind = kind,
            CustomerDisplayName = NormalizeCustomer(customerDisplayName),
            PointsDelta = pointsDelta
        });

        var trimmed = entries
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(MaxStoredEntries)
            .OrderBy(x => x.OccurredAtUtc)
            .ToList();

        SaveEntries(trimmed);
        return Task.CompletedTask;
    }

    private static List<BusinessActivityEntry> LoadEntries()
    {
        var raw = Preferences.Default.Get(StorageKey, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<BusinessActivityEntry>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<BusinessActivityEntry>>(raw, JsonOptions)
                   ?? new List<BusinessActivityEntry>();
        }
        catch
        {
            // Corrupted local payload should not crash the app. We recover with an empty log.
            return new List<BusinessActivityEntry>();
        }
    }

    private static void SaveEntries(List<BusinessActivityEntry> entries)
    {
        var serialized = JsonSerializer.Serialize(entries, JsonOptions);
        Preferences.Default.Set(StorageKey, serialized);
    }

    private static string NormalizeCustomer(string? customerDisplayName)
    {
        return string.IsNullOrWhiteSpace(customerDisplayName)
            ? "Unknown customer"
            : customerDisplayName.Trim();
    }
}

/// <summary>
/// Internal persisted activity log item.
/// </summary>
internal sealed class BusinessActivityEntry
{
    public DateTime OccurredAtUtc { get; init; }
    public BusinessActivityKind Kind { get; init; }
    public string CustomerDisplayName { get; init; } = string.Empty;
    public int PointsDelta { get; init; }
}

/// <summary>
/// Internal activity kind flags used for aggregation.
/// </summary>
internal enum BusinessActivityKind
{
    SessionLoaded = 0,
    AccrualConfirmed = 1,
    RedemptionConfirmed = 2,
    SubscriptionStatusRefreshSucceeded = 3,
    SubscriptionStatusRefreshFailed = 4,
    SubscriptionPlansLoaded = 5,
    SubscriptionCheckoutStarted = 6,
    SubscriptionCheckoutFailed = 7,
    SubscriptionCancelAtPeriodEndEnabled = 8,
    SubscriptionCancelAtPeriodEndDisabled = 9
}
