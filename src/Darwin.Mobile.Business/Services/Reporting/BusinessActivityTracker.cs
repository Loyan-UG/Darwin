using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Shared.Storage.Abstractions;
using Microsoft.Maui.Storage;

namespace Darwin.Mobile.Business.Services.Reporting;

/// <summary>
/// SQLite-backed implementation of business activity tracker.
/// </summary>
/// <remarks>
/// Persistence design:
/// - Stores a rolling activity list inside the shared local database through <see cref="IKeyValueStore"/>.
/// - Migrates the legacy <see cref="Preferences"/> payload once so existing operators do not lose
///   dashboard history during the storage transition.
/// - Trims old entries to protect storage size and keep reporting queries lightweight.
/// </remarks>
public sealed class BusinessActivityTracker : IBusinessActivityTracker
{
    private const string StorageKey = "business.activity.log.v1";
    private const int MaxStoredEntries = 500;
    private const string LegacyMigrationFlagKey = "business.activity.log.v1.migrated";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly IKeyValueStore _keyValueStore;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessActivityTracker"/> class.
    /// </summary>
    public BusinessActivityTracker(IKeyValueStore keyValueStore)
    {
        _keyValueStore = keyValueStore ?? throw new ArgumentNullException(nameof(keyValueStore));
    }

    /// <inheritdoc />
    public Task RecordSessionLoadedAsync(string? customerDisplayName, CancellationToken cancellationToken)
        => AppendAsync(BusinessActivityKind.SessionLoaded, customerDisplayName, pointsDelta: 0, cancellationToken);

    /// <inheritdoc />
    public Task RecordAccrualConfirmedAsync(string? customerDisplayName, int pointsAccrued, CancellationToken cancellationToken)
        => AppendAsync(BusinessActivityKind.AccrualConfirmed, customerDisplayName, Math.Max(0, pointsAccrued), cancellationToken);

    /// <inheritdoc />
    public Task RecordRedemptionConfirmedAsync(string? customerDisplayName, int pointsRedeemed, CancellationToken cancellationToken)
        => AppendAsync(BusinessActivityKind.RedemptionConfirmed, customerDisplayName, Math.Max(0, pointsRedeemed), cancellationToken);

    /// <inheritdoc />
    public Task RecordSubscriptionStatusRefreshAsync(bool succeeded, CancellationToken cancellationToken)
        => AppendAsync(
            succeeded ? BusinessActivityKind.SubscriptionStatusRefreshSucceeded : BusinessActivityKind.SubscriptionStatusRefreshFailed,
            customerDisplayName: AppResources.ReportingSubscriptionSettingsDisplayName,
            pointsDelta: 0,
            cancellationToken);

    /// <inheritdoc />
    public Task RecordCampaignTargetingSchemaFixAsync(bool changed, CancellationToken cancellationToken)
        => AppendAsync(
            changed
                ? BusinessActivityKind.CampaignTargetingFixApplied
                : BusinessActivityKind.CampaignTargetingFixNoChange,
            customerDisplayName: AppResources.ReportingCampaignTargetingDisplayName,
            pointsDelta: 0,
            cancellationToken);

    /// <inheritdoc />
    public Task RecordCampaignTargetingFixMetricsResetAsync(CancellationToken cancellationToken)
        => AppendAsync(
            BusinessActivityKind.CampaignTargetingFixMetricsReset,
            customerDisplayName: AppResources.ReportingCampaignTargetingDisplayName,
            pointsDelta: 0,
            cancellationToken);

    /// <inheritdoc />
    public async Task<BusinessDashboardSnapshot> GetDashboardSnapshotAsync(TimeSpan lookbackWindow, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTime.UtcNow;
        var fromUtc = now - (lookbackWindow <= TimeSpan.Zero ? TimeSpan.FromDays(7) : lookbackWindow);

        var entries = await LoadEntriesAsync(cancellationToken);
        var filteredEntries = entries
            .Where(x => x.OccurredAtUtc >= fromUtc)
            .OrderByDescending(x => x.OccurredAtUtc)
            .ToList();

        var totalSessions = filteredEntries.Count(x => x.Kind == BusinessActivityKind.SessionLoaded);
        var accrualEntries = filteredEntries.Where(x => x.Kind == BusinessActivityKind.AccrualConfirmed).ToList();
        var redemptionEntries = filteredEntries.Where(x => x.Kind == BusinessActivityKind.RedemptionConfirmed).ToList();
        var subscriptionRefreshFailures = filteredEntries.Count(x => x.Kind == BusinessActivityKind.SubscriptionStatusRefreshFailed);
        var campaignTargetingFixAppliedCount = filteredEntries.Count(x => x.Kind == BusinessActivityKind.CampaignTargetingFixApplied);
        var campaignTargetingFixNoChangeCount = filteredEntries.Count(x => x.Kind == BusinessActivityKind.CampaignTargetingFixNoChange);
        var campaignTargetingFixMetricsResetCount = filteredEntries.Count(x => x.Kind == BusinessActivityKind.CampaignTargetingFixMetricsReset);

        var topCustomers = filteredEntries
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

        var recentActivities = filteredEntries
            .Take(20)
            .Select(x => new BusinessActivityFeedItem
            {
                OccurredAtUtc = x.OccurredAtUtc,
                ActivityKind = ResolveActivityKindDisplayName(x.Kind),
                CustomerDisplayName = NormalizeCustomer(x.CustomerDisplayName),
                PointsDelta = x.PointsDelta
            })
            .ToList();

        return new BusinessDashboardSnapshot
        {
            TotalSessions = totalSessions,
            AccrualCount = accrualEntries.Count,
            RedemptionCount = redemptionEntries.Count,
            SubscriptionStatusRefreshFailures = subscriptionRefreshFailures,
            CampaignTargetingFixAppliedCount = campaignTargetingFixAppliedCount,
            CampaignTargetingFixNoChangeCount = campaignTargetingFixNoChangeCount,
            CampaignTargetingFixMetricsResetCount = campaignTargetingFixMetricsResetCount,
            TotalAccruedPoints = accrualEntries.Sum(x => x.PointsDelta),
            TotalRedeemedPoints = redemptionEntries.Sum(x => x.PointsDelta),
            TopCustomers = topCustomers,
            RecentActivities = recentActivities
        };
    }

    private async Task AppendAsync(BusinessActivityKind kind, string? customerDisplayName, int pointsDelta, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var entries = await LoadEntriesCoreAsync(cancellationToken);
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

            await SaveEntriesCoreAsync(trimmed, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<BusinessActivityEntry>> LoadEntriesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await LoadEntriesCoreAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<BusinessActivityEntry>> LoadEntriesCoreAsync(CancellationToken cancellationToken)
    {
        await EnsureLegacyPayloadMigratedAsync(cancellationToken);

        var raw = await _keyValueStore.GetAsync(StorageKey, cancellationToken);
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

    private Task SaveEntriesCoreAsync(List<BusinessActivityEntry> entries, CancellationToken cancellationToken)
    {
        var serialized = JsonSerializer.Serialize(entries, JsonOptions);
        return _keyValueStore.SetAsync(StorageKey, serialized, cancellationToken);
    }

    private async Task EnsureLegacyPayloadMigratedAsync(CancellationToken cancellationToken)
    {
        var migrationMarker = await _keyValueStore.GetAsync(LegacyMigrationFlagKey, cancellationToken);
        if (string.Equals(migrationMarker, bool.TrueString, StringComparison.Ordinal))
        {
            return;
        }

        var legacyPayload = Preferences.Default.Get(StorageKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(legacyPayload))
        {
            await _keyValueStore.SetAsync(StorageKey, legacyPayload, cancellationToken);
            Preferences.Default.Remove(StorageKey);
        }

        await _keyValueStore.SetAsync(LegacyMigrationFlagKey, bool.TrueString, cancellationToken);
    }

    private static string NormalizeCustomer(string? customerDisplayName)
    {
        return string.IsNullOrWhiteSpace(customerDisplayName)
            ? AppResources.ReportingUnknownCustomer
            : customerDisplayName.Trim();
    }

    /// <summary>
    /// Resolves localized activity captions for dashboard and export projections.
    /// </summary>
    private static string ResolveActivityKindDisplayName(BusinessActivityKind kind)
    {
        return kind switch
        {
            BusinessActivityKind.SessionLoaded => AppResources.DashboardActivityKindSessionLoaded,
            BusinessActivityKind.AccrualConfirmed => AppResources.DashboardActivityKindAccrualConfirmed,
            BusinessActivityKind.RedemptionConfirmed => AppResources.DashboardActivityKindRedemptionConfirmed,
            BusinessActivityKind.SubscriptionStatusRefreshSucceeded => AppResources.DashboardActivityKindSubscriptionRefreshSucceeded,
            BusinessActivityKind.SubscriptionStatusRefreshFailed => AppResources.DashboardActivityKindSubscriptionRefreshFailed,
            BusinessActivityKind.SubscriptionPlansLoaded => AppResources.DashboardActivityKindSubscriptionPlansLoaded,
            BusinessActivityKind.SubscriptionCheckoutStarted => AppResources.DashboardActivityKindSubscriptionCheckoutStarted,
            BusinessActivityKind.SubscriptionCheckoutFailed => AppResources.DashboardActivityKindSubscriptionCheckoutFailed,
            BusinessActivityKind.SubscriptionCancelAtPeriodEndEnabled => AppResources.DashboardActivityKindSubscriptionCancelAtPeriodEndEnabled,
            BusinessActivityKind.SubscriptionCancelAtPeriodEndDisabled => AppResources.DashboardActivityKindSubscriptionCancelAtPeriodEndDisabled,
            BusinessActivityKind.CampaignTargetingFixApplied => AppResources.DashboardActivityKindCampaignTargetingFixApplied,
            BusinessActivityKind.CampaignTargetingFixNoChange => AppResources.DashboardActivityKindCampaignTargetingFixNoChange,
            BusinessActivityKind.CampaignTargetingFixMetricsReset => AppResources.DashboardActivityKindCampaignTargetingFixMetricsReset,
            _ => kind.ToString()
        };
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
    // Legacy values are intentionally preserved so previously persisted local telemetry
    // can still deserialize without remapping when older app versions recorded them.
    SubscriptionPlansLoaded = 5,
    SubscriptionCheckoutStarted = 6,
    SubscriptionCheckoutFailed = 7,
    SubscriptionCancelAtPeriodEndEnabled = 8,
    SubscriptionCancelAtPeriodEndDisabled = 9,
    CampaignTargetingFixApplied = 10,
    CampaignTargetingFixNoChange = 11,
    CampaignTargetingFixMetricsReset = 12
}
