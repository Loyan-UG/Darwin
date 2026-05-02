using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Business.Services.Identity;
using Darwin.Mobile.Business.Services.Reporting;
using Darwin.Mobile.Shared.Collections;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// Business dashboard view model for lightweight KPI and reporting cards.
/// </summary>
public sealed class DashboardViewModel : BaseViewModel
{
    private const string DashboardExportFilePrefix = "business-dashboard-";
    private const int MaxDisplayedTopCustomers = 10;
    private const int MaxDisplayedRecentActivities = 25;

    public sealed class DashboardChartPoint
    {
        public required string Label { get; init; }

        public required int Value { get; init; }
    }

    private readonly IBusinessActivityTracker _activityTracker;
    private readonly IBusinessAccessService _businessAccessService;
    private readonly TimeProvider _timeProvider;
    private BusyOperationScope? _currentOperation;

    private bool _loadedOnce;
    private bool _isOperationsAllowed = true;
    private int _lookbackDays = 7;
    private int _selectedLookbackIndex = 1;
    private BusinessDashboardSnapshot? _lastSnapshot;

    private int _totalSessions;
    private int _accrualCount;
    private int _redemptionCount;
    private int _subscriptionStatusRefreshFailures;
    private int _campaignTargetingFixAppliedCount;
    private int _campaignTargetingFixNoChangeCount;
    private int _campaignTargetingFixMetricsResetCount;
    private int _totalAccruedPoints;
    private int _totalRedeemedPoints;

    public DashboardViewModel(
        IBusinessActivityTracker activityTracker,
        IBusinessAccessService businessAccessService,
        TimeProvider timeProvider)
    {
        _activityTracker = activityTracker ?? throw new ArgumentNullException(nameof(activityTracker));
        _businessAccessService = businessAccessService ?? throw new ArgumentNullException(nameof(businessAccessService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        TopCustomers = new RangeObservableCollection<BusinessTopCustomerItem>();
        RecentActivities = new RangeObservableCollection<BusinessActivityFeedItem>();
        ActivityMix = new RangeObservableCollection<DashboardChartPoint>();
        LookbackDayOptions = new ObservableCollection<int> { 1, 7, 14, 30 };
        LookbackDayLabels = new ObservableCollection<string>(LookbackDayOptions.Select(static value => string.Format(
            CultureInfo.CurrentCulture,
            AppResources.DashboardLookbackDaysShortFormat,
            value)));

        RefreshCommand = new AsyncCommand(LoadAsync, () => !IsBusy && _isOperationsAllowed);
        ExportCsvCommand = new AsyncCommand(ExportCsvAsync, () => !IsBusy && _isOperationsAllowed && _lastSnapshot is not null);
        ExportPdfCommand = new AsyncCommand(ExportPdfAsync, () => !IsBusy && _isOperationsAllowed && _lastSnapshot is not null);
    }

    public int LookbackDays
    {
        get => _lookbackDays;
        set
        {
            if (SetProperty(ref _lookbackDays, value))
            {
                SyncSelectedLookbackIndex(value);
                _ = LoadLatestLookbackAsync();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected dashboard lookback segment index.
    /// The segmented control binds to an index, while reporting logic remains based on day count.
    /// </summary>
    public int SelectedLookbackIndex
    {
        get => _selectedLookbackIndex;
        set
        {
            if (!SetProperty(ref _selectedLookbackIndex, value))
            {
                return;
            }

            if ((uint)value >= (uint)LookbackDayOptions.Count)
            {
                return;
            }

            LookbackDays = LookbackDayOptions[value];
        }
    }

    public int TotalSessions
    {
        get => _totalSessions;
        private set => SetProperty(ref _totalSessions, value);
    }

    public int AccrualCount
    {
        get => _accrualCount;
        private set => SetProperty(ref _accrualCount, value);
    }

    public int RedemptionCount
    {
        get => _redemptionCount;
        private set => SetProperty(ref _redemptionCount, value);
    }

    public int SubscriptionStatusRefreshFailures
    {
        get => _subscriptionStatusRefreshFailures;
        private set => SetProperty(ref _subscriptionStatusRefreshFailures, value);
    }

    /// <summary>
    /// Count of targeting schema quick-fix actions that changed the JSON payload.
    /// </summary>
    public int CampaignTargetingFixAppliedCount
    {
        get => _campaignTargetingFixAppliedCount;
        private set
        {
            if (SetProperty(ref _campaignTargetingFixAppliedCount, value))
            {
                OnPropertyChanged(nameof(CampaignTargetingFixMetricsSummary));
            }
        }
    }

    /// <summary>
    /// Count of targeting schema quick-fix actions that produced no change.
    /// </summary>
    public int CampaignTargetingFixNoChangeCount
    {
        get => _campaignTargetingFixNoChangeCount;
        private set
        {
            if (SetProperty(ref _campaignTargetingFixNoChangeCount, value))
            {
                OnPropertyChanged(nameof(CampaignTargetingFixMetricsSummary));
            }
        }
    }

    /// <summary>
    /// Count of telemetry reset actions for targeting quick-fix metrics.
    /// </summary>
    public int CampaignTargetingFixMetricsResetCount
    {
        get => _campaignTargetingFixMetricsResetCount;
        private set
        {
            if (SetProperty(ref _campaignTargetingFixMetricsResetCount, value))
            {
                OnPropertyChanged(nameof(CampaignTargetingFixMetricsSummary));
            }
        }
    }

    /// <summary>
    /// Localized campaign quick-fix telemetry summary for dashboard KPI card.
    /// </summary>
    public string CampaignTargetingFixMetricsSummary => string.Format(
        CultureInfo.CurrentCulture,
        AppResources.DashboardCampaignTargetingFixMetricsFormat,
        CampaignTargetingFixAppliedCount,
        CampaignTargetingFixNoChangeCount,
        CampaignTargetingFixMetricsResetCount);

    public int TotalAccruedPoints
    {
        get => _totalAccruedPoints;
        private set => SetProperty(ref _totalAccruedPoints, value);
    }

    public int TotalRedeemedPoints
    {
        get => _totalRedeemedPoints;
        private set => SetProperty(ref _totalRedeemedPoints, value);
    }

    public RangeObservableCollection<BusinessTopCustomerItem> TopCustomers { get; }

    public RangeObservableCollection<BusinessActivityFeedItem> RecentActivities { get; }

    /// <summary>
    /// Gets the accrual/redemption mix used by the dashboard chart.
    /// </summary>
    public RangeObservableCollection<DashboardChartPoint> ActivityMix { get; }

    public ObservableCollection<int> LookbackDayOptions { get; }

    /// <summary>
    /// Gets display labels for the dashboard lookback segmented control.
    /// </summary>
    public ObservableCollection<string> LookbackDayLabels { get; }

    public bool HasTopCustomers => TopCustomers.Count > 0;

    public bool HasRecentActivities => RecentActivities.Count > 0;

    public AsyncCommand RefreshCommand { get; }

    /// <summary>
    /// Exports the currently loaded dashboard snapshot as a CSV file and opens the native share sheet.
    /// </summary>
    public AsyncCommand ExportCsvCommand { get; }

    /// <summary>
    /// Exports the currently loaded dashboard snapshot as a compact PDF file and opens the native share sheet.
    /// </summary>
    public AsyncCommand ExportPdfCommand { get; }

    public override async Task OnAppearingAsync()
    {
        if (_loadedOnce)
        {
            return;
        }

        _loadedOnce = true;
        await LoadAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Cancels dashboard refresh/export work when the dashboard is no longer visible.
    /// This keeps stale continuations from updating UI-bound KPI state after navigation.
    /// </summary>
    /// <returns>A completed task because cancellation is signaled synchronously.</returns>
    public override Task OnDisappearingAsync()
    {
        CancelCurrentOperation();
        return Task.CompletedTask;
    }

    private async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        await LoadCoreAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Reloads dashboard data for the newest selected lookback window, cancelling any older in-flight load.
    /// </summary>
    private Task LoadLatestLookbackAsync()
    {
        return LoadCoreAsync();
    }

    /// <summary>
    /// Loads dashboard state and owns operation replacement for both command-triggered and selector-triggered refreshes.
    /// </summary>
    private async Task LoadCoreAsync()
    {
        using var operation = BeginBusyOperation();
        var cancellationToken = operation.Token;

        try
        {
            if (!await EnsureOperationsAllowedAsync(cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var window = TimeSpan.FromDays(Math.Clamp(LookbackDays, 1, 30));
            var snapshot = await _activityTracker
                .GetDashboardSnapshotAsync(window, cancellationToken)
                .ConfigureAwait(false);

            RunOnMain(() =>
            {
                _lastSnapshot = snapshot;
                TotalSessions = snapshot.TotalSessions;
                AccrualCount = snapshot.AccrualCount;
                RedemptionCount = snapshot.RedemptionCount;
                SubscriptionStatusRefreshFailures = snapshot.SubscriptionStatusRefreshFailures;
                CampaignTargetingFixAppliedCount = snapshot.CampaignTargetingFixAppliedCount;
                CampaignTargetingFixNoChangeCount = snapshot.CampaignTargetingFixNoChangeCount;
                CampaignTargetingFixMetricsResetCount = snapshot.CampaignTargetingFixMetricsResetCount;
                TotalAccruedPoints = snapshot.TotalAccruedPoints;
                TotalRedeemedPoints = snapshot.TotalRedeemedPoints;

                // The dashboard keeps the full snapshot for exports, while the on-screen lists stay bounded
                // because they are rendered inside the dashboard scroll surface rather than virtualized.
                TopCustomers.ReplaceRange(snapshot.TopCustomers.Take(MaxDisplayedTopCustomers));
                RecentActivities.ReplaceRange(snapshot.RecentActivities
                    .OrderByDescending(x => x.OccurredAtUtc)
                    .Take(MaxDisplayedRecentActivities));
                ActivityMix.ReplaceRange(new[]
                {
                    new DashboardChartPoint
                    {
                        Label = AppResources.DashboardAccrualsTitle,
                        Value = Math.Max(0, snapshot.AccrualCount)
                    },
                    new DashboardChartPoint
                    {
                        Label = AppResources.DashboardRedemptionsTitle,
                        Value = Math.Max(0, snapshot.RedemptionCount)
                    }
                });

                OnPropertyChanged(nameof(HasTopCustomers));
                OnPropertyChanged(nameof(HasRecentActivities));
                ErrorMessage = null;
                ExportCsvCommand.RaiseCanExecuteChanged();
                ExportPdfCommand.RaiseCanExecuteChanged();
            });
        }
        catch (OperationCanceledException)
        {
            // Navigation away from the dashboard intentionally cancels stale work.
        }
        catch
        {
            RunOnMain(() => ErrorMessage = AppResources.DashboardLoadFailed);
        }
        finally
        {
            EndBusyOperation(operation);
        }
    }

    /// <summary>
    /// Builds a CSV document from the latest dashboard snapshot and launches native share flow.
    /// </summary>
    private async Task ExportCsvAsync()
    {
        if (IsBusy || _lastSnapshot is null)
        {
            return;
        }

        using var operation = BeginBusyOperation();
        var cancellationToken = operation.Token;

        try
        {
            if (!await EnsureOperationsAllowedAsync(cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var csv = BuildDashboardCsv(_lastSnapshot, LookbackDays);
            CleanupPreviousDashboardExports(".csv");
            var fileName = $"business-dashboard-{_timeProvider.GetUtcNow().UtcDateTime:yyyyMMdd-HHmmss}.csv";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            await File.WriteAllTextAsync(filePath, csv, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

            await MainThread.InvokeOnMainThreadAsync(() => Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = AppResources.DashboardExportCsvShareTitle,
                File = new ShareFile(filePath)
            })).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Navigation away from the dashboard intentionally cancels stale export work.
        }
        catch
        {
            RunOnMain(() => ErrorMessage = AppResources.DashboardExportCsvFailed);
        }
        finally
        {
            EndBusyOperation(operation);
        }
    }

    /// <summary>
    /// Builds a compact single-page PDF dashboard report and launches native share flow.
    /// </summary>
    private async Task ExportPdfAsync()
    {
        if (IsBusy || _lastSnapshot is null)
        {
            return;
        }

        using var operation = BeginBusyOperation();
        var cancellationToken = operation.Token;

        try
        {
            if (!await EnsureOperationsAllowedAsync(cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var generatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var pdfBytes = BuildDashboardPdfDocument(_lastSnapshot, LookbackDays, generatedAtUtc);
            CleanupPreviousDashboardExports(".pdf");
            var fileName = $"business-dashboard-{_timeProvider.GetUtcNow().UtcDateTime:yyyyMMdd-HHmmss}.pdf";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            await File.WriteAllBytesAsync(filePath, pdfBytes, cancellationToken).ConfigureAwait(false);

            await MainThread.InvokeOnMainThreadAsync(() => Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = AppResources.DashboardExportPdfShareTitle,
                File = new ShareFile(filePath)
            })).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Navigation away from the dashboard intentionally cancels stale export work.
        }
        catch
        {
            RunOnMain(() => ErrorMessage = AppResources.DashboardExportPdfFailed);
        }
        finally
        {
            EndBusyOperation(operation);
        }
    }

    /// <summary>
    /// Creates a stable CSV payload containing KPI summary, top customers, and recent activity rows.
    /// </summary>
    private static string BuildDashboardCsv(BusinessDashboardSnapshot snapshot, int lookbackDays)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',',
            AppResources.DashboardCsvSectionHeader,
            AppResources.DashboardCsvMetricHeader,
            AppResources.DashboardCsvValueHeader));
        builder.AppendLine($"{AppResources.DashboardCsvSummarySection},{AppResources.DashboardCsvLookbackDaysMetric},{lookbackDays}");
        builder.AppendLine($"{AppResources.DashboardCsvSummarySection},{AppResources.DashboardCsvTotalSessionsMetric},{snapshot.TotalSessions}");
        builder.AppendLine($"{AppResources.DashboardCsvSummarySection},{AppResources.DashboardCsvAccrualCountMetric},{snapshot.AccrualCount}");
        builder.AppendLine($"{AppResources.DashboardCsvSummarySection},{AppResources.DashboardCsvRedemptionCountMetric},{snapshot.RedemptionCount}");
        builder.AppendLine($"{AppResources.DashboardCsvSummarySection},{AppResources.DashboardCsvSubscriptionRefreshFailuresMetric},{snapshot.SubscriptionStatusRefreshFailures}");
        builder.AppendLine($"{AppResources.DashboardCsvSummarySection},{AppResources.DashboardCsvCampaignFixAppliedMetric},{snapshot.CampaignTargetingFixAppliedCount}");
        builder.AppendLine($"{AppResources.DashboardCsvSummarySection},{AppResources.DashboardCsvCampaignFixNoChangeMetric},{snapshot.CampaignTargetingFixNoChangeCount}");
        builder.AppendLine($"{AppResources.DashboardCsvSummarySection},{AppResources.DashboardCsvCampaignFixResetMetric},{snapshot.CampaignTargetingFixMetricsResetCount}");
        builder.AppendLine($"{AppResources.DashboardCsvSummarySection},{AppResources.DashboardCsvTotalAccruedPointsMetric},{snapshot.TotalAccruedPoints}");
        builder.AppendLine($"{AppResources.DashboardCsvSummarySection},{AppResources.DashboardCsvTotalRedeemedPointsMetric},{snapshot.TotalRedeemedPoints}");

        builder.AppendLine();
        builder.AppendLine(string.Join(',',
            AppResources.DashboardCsvTopCustomersSection,
            AppResources.DashboardCsvCustomerDisplayNameHeader,
            AppResources.DashboardCsvInteractionsCountHeader));
        foreach (var customer in snapshot.TopCustomers)
        {
            builder.Append(AppResources.DashboardCsvTopCustomersSection).Append(',')
                .Append(EscapeCsv(customer.CustomerDisplayName)).Append(',')
                .Append(customer.InteractionsCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine();
        }

        builder.AppendLine();
        builder.AppendLine(string.Join(',',
            AppResources.DashboardCsvRecentActivitiesSection,
            AppResources.DashboardCsvOccurredAtUtcHeader,
            AppResources.DashboardCsvActivityKindHeader,
            AppResources.DashboardCsvCustomerDisplayNameHeader,
            AppResources.DashboardCsvPointsDeltaHeader));
        foreach (var activity in snapshot.RecentActivities.OrderByDescending(x => x.OccurredAtUtc))
        {
            builder.Append(AppResources.DashboardCsvRecentActivitiesSection).Append(',')
                .Append(EscapeCsv(activity.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture))).Append(',')
                .Append(EscapeCsv(activity.ActivityKind)).Append(',')
                .Append(EscapeCsv(activity.CustomerDisplayName)).Append(',')
                .Append(activity.PointsDelta.ToString(CultureInfo.InvariantCulture))
                .AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>
    /// Builds a minimal PDF document without third-party dependencies.
    /// The payload intentionally uses plain ASCII text to keep output deterministic on all supported targets.
    /// </summary>
    private static byte[] BuildDashboardPdfDocument(BusinessDashboardSnapshot snapshot, int lookbackDays, DateTime generatedAtUtc)
    {
        var lines = BuildDashboardReportLines(snapshot, lookbackDays, generatedAtUtc);
        var content = BuildPdfTextStream(lines);

        var objects = new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n",
            $"4 0 obj\n<< /Length {content.Length} >>\nstream\n{content}\nendstream\nendobj\n",
            "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n"
        };

        var builder = new StringBuilder();
        builder.Append("%PDF-1.4\n");

        var offsets = new List<int> { 0 };
        foreach (var obj in objects)
        {
            offsets.Add(builder.Length);
            builder.Append(obj);
        }

        var xrefStart = builder.Length;
        builder.Append($"xref\n0 {objects.Length + 1}\n");
        builder.Append("0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++)
        {
            builder.Append(offsets[i].ToString("D10", CultureInfo.InvariantCulture));
            builder.Append(" 00000 n \n");
        }

        builder.Append("trailer\n");
        builder.Append($"<< /Size {objects.Length + 1} /Root 1 0 R >>\n");
        builder.Append("startxref\n");
        builder.Append(xrefStart.ToString(CultureInfo.InvariantCulture));
        builder.Append("\n%%EOF");

        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    /// <summary>
    /// Creates business-readable report lines that are used for PDF export.
    /// </summary>
    private static IReadOnlyList<string> BuildDashboardReportLines(BusinessDashboardSnapshot snapshot, int lookbackDays, DateTime generatedAtUtc)
    {
        var lines = new List<string>
        {
            AppResources.DashboardPdfTitle,
            string.Format(CultureInfo.CurrentCulture, AppResources.DashboardPdfGeneratedAtFormat, AppResources.DashboardPdfGeneratedAtTimezoneLabel, generatedAtUtc),
            string.Format(CultureInfo.CurrentCulture, AppResources.DashboardPdfWindowFormat, lookbackDays),
            string.Empty,
            AppResources.DashboardPdfSummarySectionTitle,
            string.Format(CultureInfo.CurrentCulture, AppResources.DashboardPdfTotalSessionsFormat, snapshot.TotalSessions),
            string.Format(CultureInfo.CurrentCulture, AppResources.DashboardPdfAccrualCountFormat, snapshot.AccrualCount),
            string.Format(CultureInfo.CurrentCulture, AppResources.DashboardPdfRedemptionCountFormat, snapshot.RedemptionCount),
            string.Format(CultureInfo.CurrentCulture, AppResources.DashboardPdfSubscriptionRefreshFailuresFormat, snapshot.SubscriptionStatusRefreshFailures),
            string.Format(CultureInfo.CurrentCulture, AppResources.DashboardPdfCampaignFixAppliedFormat, snapshot.CampaignTargetingFixAppliedCount),
            string.Format(CultureInfo.CurrentCulture, AppResources.DashboardPdfCampaignFixNoChangeFormat, snapshot.CampaignTargetingFixNoChangeCount),
            string.Format(CultureInfo.CurrentCulture, AppResources.DashboardPdfCampaignFixResetFormat, snapshot.CampaignTargetingFixMetricsResetCount),
            string.Format(CultureInfo.CurrentCulture, AppResources.DashboardPdfTotalAccruedPointsFormat, snapshot.TotalAccruedPoints),
            string.Format(CultureInfo.CurrentCulture, AppResources.DashboardPdfTotalRedeemedPointsFormat, snapshot.TotalRedeemedPoints),
            string.Empty,
            AppResources.DashboardPdfTopCustomersSectionTitle
        };

        if (snapshot.TopCustomers.Count == 0)
        {
            lines.Add(AppResources.DashboardPdfNoCustomerInteractions);
        }
        else
        {
            lines.AddRange(snapshot.TopCustomers.Select(c =>
                string.Format(CultureInfo.CurrentCulture, AppResources.DashboardPdfTopCustomerLineFormat, c.CustomerDisplayName, c.InteractionsCount)));
        }

        lines.Add(string.Empty);
        lines.Add(AppResources.DashboardPdfRecentActivitySectionTitle);

        if (snapshot.RecentActivities.Count == 0)
        {
            lines.Add(AppResources.DashboardPdfNoRecentActivity);
        }
        else
        {
            lines.AddRange(snapshot.RecentActivities
                .OrderByDescending(x => x.OccurredAtUtc)
                .Select(a => string.Format(
                    CultureInfo.CurrentCulture,
                    AppResources.DashboardPdfRecentActivityLineFormat,
                    a.OccurredAtUtc,
                    a.ActivityKind,
                    a.CustomerDisplayName,
                    a.PointsDelta)));
        }

        return lines;
    }

    /// <summary>
    /// Creates a PDF text stream using simple Helvetica text rows.
    /// </summary>
    private static string BuildPdfTextStream(IReadOnlyList<string> lines)
    {
        var content = new StringBuilder();
        content.Append("BT\n/F1 10 Tf\n40 800 Td\n");

        var remainingLines = 52;
        foreach (var rawLine in lines)
        {
            if (remainingLines <= 0)
            {
                break;
            }

            var safeLine = EscapePdfText(rawLine);
            content.Append('(').Append(safeLine).Append(") Tj\nT*\n");
            remainingLines--;
        }

        content.Append("ET");
        return content.ToString();
    }

    /// <summary>
    /// Escapes characters that are special in PDF text literals and normalizes non-ASCII payload.
    /// </summary>
    private static string EscapePdfText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            normalized.Append(ch <= 0x7F ? ch : '?');
        }

        return normalized
            .ToString()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }

    /// <summary>
    /// Escapes CSV values according to RFC4180-compatible rules.
    /// </summary>
    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    /// <summary>
    /// Removes previous dashboard export files before creating a new one to keep the app cache bounded.
    /// </summary>
    /// <param name="extension">Export file extension, including the leading dot.</param>
    private static void CleanupPreviousDashboardExports(string extension)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(extension) || !Directory.Exists(FileSystem.CacheDirectory))
            {
                return;
            }

            foreach (var filePath in Directory.EnumerateFiles(FileSystem.CacheDirectory, $"{DashboardExportFilePrefix}*{extension}"))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Export cleanup is opportunistic; stale cache files must not block the operator from sharing a fresh report.
        }
    }

    /// <summary>
    /// Keeps segmented-control selection synchronized when lookback days are changed programmatically.
    /// </summary>
    /// <param name="lookbackDays">The active lookback window in days.</param>
    private void SyncSelectedLookbackIndex(int lookbackDays)
    {
        var index = LookbackDayOptions.IndexOf(lookbackDays);
        if (index >= 0 && _selectedLookbackIndex != index)
        {
            _selectedLookbackIndex = index;
            OnPropertyChanged(nameof(SelectedLookbackIndex));
        }
    }

    private async Task<bool> EnsureOperationsAllowedAsync(CancellationToken cancellationToken = default)
    {
        var result = await _businessAccessService.GetCurrentAccessStateAsync(cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded || result.Value is null)
        {
            ApplyOperationsBlockedState(AppResources.BusinessAccessStateLoadFailed);
            return false;
        }

        _isOperationsAllowed = result.Value.IsOperationsAllowed;
        if (_isOperationsAllowed)
        {
            RunOnMain(() =>
            {
                RefreshCommand.RaiseCanExecuteChanged();
                ExportCsvCommand.RaiseCanExecuteChanged();
                ExportPdfCommand.RaiseCanExecuteChanged();
            });

            return true;
        }

        ApplyOperationsBlockedState(BusinessAccessStateUiMapper.GetOperationalStatusMessage(result.Value));
        return false;
    }

    private void ApplyOperationsBlockedState(string message)
    {
        RunOnMain(() =>
        {
            _isOperationsAllowed = false;
            _lastSnapshot = null;
            TotalSessions = 0;
            AccrualCount = 0;
            RedemptionCount = 0;
            SubscriptionStatusRefreshFailures = 0;
            CampaignTargetingFixAppliedCount = 0;
            CampaignTargetingFixNoChangeCount = 0;
            CampaignTargetingFixMetricsResetCount = 0;
            TotalAccruedPoints = 0;
            TotalRedeemedPoints = 0;
            TopCustomers.ClearRange();
            RecentActivities.ClearRange();
            ActivityMix.ClearRange();
            OnPropertyChanged(nameof(HasTopCustomers));
            OnPropertyChanged(nameof(HasRecentActivities));
            ErrorMessage = message;
            RefreshCommand.RaiseCanExecuteChanged();
            ExportCsvCommand.RaiseCanExecuteChanged();
            ExportPdfCommand.RaiseCanExecuteChanged();
        });
    }

    /// <summary>
    /// Marks the dashboard as busy and owns cancellation for one refresh or export operation.
    /// </summary>
    private BusyOperationScope BeginBusyOperation()
    {
        var operation = new BusyOperationScope(this);
        var previousOperation = Interlocked.Exchange(ref _currentOperation, operation);
        previousOperation?.Cancel();
        previousOperation?.Dispose();

        RunOnMain(() =>
        {
            IsBusy = true;
            RefreshCommand.RaiseCanExecuteChanged();
            ExportCsvCommand.RaiseCanExecuteChanged();
            ExportPdfCommand.RaiseCanExecuteChanged();
        });

        return operation;
    }

    /// <summary>
    /// Clears dashboard busy state when the matching operation completes.
    /// </summary>
    /// <param name="operation">Operation scope that owns the current busy state.</param>
    private void EndBusyOperation(BusyOperationScope operation)
    {
        if (operation.IsDisposed)
        {
            return;
        }

        var isCurrentOperation = ReferenceEquals(_currentOperation, operation);
        if (isCurrentOperation)
        {
            _currentOperation = null;
        }

        operation.IsDisposed = true;
        operation.Cancellation.Dispose();

        if (isCurrentOperation)
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RefreshCommand.RaiseCanExecuteChanged();
                ExportCsvCommand.RaiseCanExecuteChanged();
                ExportPdfCommand.RaiseCanExecuteChanged();
            });
        }
    }

    /// <summary>
    /// Cancels the current dashboard operation and releases the visible busy state.
    /// </summary>
    private void CancelCurrentOperation()
    {
        var operation = Interlocked.Exchange(ref _currentOperation, null);
        if (operation is null)
        {
            return;
        }

        operation.Cancel();
        RunOnMain(() =>
        {
            IsBusy = false;
            RefreshCommand.RaiseCanExecuteChanged();
            ExportCsvCommand.RaiseCanExecuteChanged();
            ExportPdfCommand.RaiseCanExecuteChanged();
        });
    }

    /// <summary>
    /// Owns cancellation and busy-state lifetime for one dashboard operation.
    /// </summary>
    private sealed class BusyOperationScope : IDisposable
    {
        private readonly DashboardViewModel _owner;

        public BusyOperationScope(DashboardViewModel owner)
        {
            _owner = owner;
            Cancellation = new CancellationTokenSource();
        }

        public CancellationTokenSource Cancellation { get; }

        public CancellationToken Token => Cancellation.Token;

        public bool IsDisposed { get; set; }

        public void Cancel()
        {
            if (!Cancellation.IsCancellationRequested)
            {
                Cancellation.Cancel();
            }
        }

        public void Dispose()
        {
            _owner.EndBusyOperation(this);
        }
    }
}
