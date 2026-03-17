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
using Darwin.Mobile.Business.Services.Reporting;
using Darwin.Mobile.Shared.Commands;
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
    private readonly IBusinessActivityTracker _activityTracker;

    private bool _loadedOnce;
    private int _lookbackDays = 7;
    private BusinessDashboardSnapshot? _lastSnapshot;

    private int _totalSessions;
    private int _accrualCount;
    private int _redemptionCount;
    private int _subscriptionStatusRefreshFailures;
    private int _subscriptionCheckoutStarts;
    private int _subscriptionCheckoutFailures;
    private int _campaignTargetingFixAppliedCount;
    private int _campaignTargetingFixNoChangeCount;
    private int _campaignTargetingFixMetricsResetCount;
    private int _totalAccruedPoints;
    private int _totalRedeemedPoints;

    public DashboardViewModel(IBusinessActivityTracker activityTracker)
    {
        _activityTracker = activityTracker ?? throw new ArgumentNullException(nameof(activityTracker));

        TopCustomers = new ObservableCollection<BusinessTopCustomerItem>();
        RecentActivities = new ObservableCollection<BusinessActivityFeedItem>();
        LookbackDayOptions = new ObservableCollection<int> { 1, 7, 14, 30 };

        RefreshCommand = new AsyncCommand(LoadAsync, () => !IsBusy);
        ExportCsvCommand = new AsyncCommand(ExportCsvAsync, () => !IsBusy && _lastSnapshot is not null);
        ExportPdfCommand = new AsyncCommand(ExportPdfAsync, () => !IsBusy && _lastSnapshot is not null);
    }

    public int LookbackDays
    {
        get => _lookbackDays;
        set
        {
            if (SetProperty(ref _lookbackDays, value))
            {
                _ = LoadAsync();
            }
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

    public int SubscriptionCheckoutStarts
    {
        get => _subscriptionCheckoutStarts;
        private set => SetProperty(ref _subscriptionCheckoutStarts, value);
    }

    public int SubscriptionCheckoutFailures
    {
        get => _subscriptionCheckoutFailures;
        private set => SetProperty(ref _subscriptionCheckoutFailures, value);
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

    public ObservableCollection<BusinessTopCustomerItem> TopCustomers { get; }

    public ObservableCollection<BusinessActivityFeedItem> RecentActivities { get; }

    public ObservableCollection<int> LookbackDayOptions { get; }

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

    private async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        RefreshCommand.RaiseCanExecuteChanged();
        ExportCsvCommand.RaiseCanExecuteChanged();
        ExportPdfCommand.RaiseCanExecuteChanged();

        try
        {
            var window = TimeSpan.FromDays(Math.Clamp(LookbackDays, 1, 30));
            var snapshot = await _activityTracker
                .GetDashboardSnapshotAsync(window, CancellationToken.None)
                .ConfigureAwait(false);

            RunOnMain(() =>
            {
                _lastSnapshot = snapshot;
                TotalSessions = snapshot.TotalSessions;
                AccrualCount = snapshot.AccrualCount;
                RedemptionCount = snapshot.RedemptionCount;
                SubscriptionStatusRefreshFailures = snapshot.SubscriptionStatusRefreshFailures;
                SubscriptionCheckoutStarts = snapshot.SubscriptionCheckoutStarts;
                SubscriptionCheckoutFailures = snapshot.SubscriptionCheckoutFailures;
                CampaignTargetingFixAppliedCount = snapshot.CampaignTargetingFixAppliedCount;
                CampaignTargetingFixNoChangeCount = snapshot.CampaignTargetingFixNoChangeCount;
                CampaignTargetingFixMetricsResetCount = snapshot.CampaignTargetingFixMetricsResetCount;
                TotalAccruedPoints = snapshot.TotalAccruedPoints;
                TotalRedeemedPoints = snapshot.TotalRedeemedPoints;

                TopCustomers.Clear();
                foreach (var item in snapshot.TopCustomers)
                {
                    TopCustomers.Add(item);
                }

                RecentActivities.Clear();
                foreach (var item in snapshot.RecentActivities.OrderByDescending(x => x.OccurredAtUtc))
                {
                    RecentActivities.Add(item);
                }

                OnPropertyChanged(nameof(HasTopCustomers));
                OnPropertyChanged(nameof(HasRecentActivities));
                ErrorMessage = null;
                ExportCsvCommand.RaiseCanExecuteChanged();
                ExportPdfCommand.RaiseCanExecuteChanged();
            });
        }
        catch
        {
            RunOnMain(() => ErrorMessage = AppResources.DashboardLoadFailed);
        }
        finally
        {
            IsBusy = false;
            RefreshCommand.RaiseCanExecuteChanged();
            ExportCsvCommand.RaiseCanExecuteChanged();
            ExportPdfCommand.RaiseCanExecuteChanged();
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

        try
        {
            var csv = BuildDashboardCsv(_lastSnapshot, LookbackDays);
            var fileName = $"business-dashboard-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            await File.WriteAllTextAsync(filePath, csv, Encoding.UTF8).ConfigureAwait(false);

            await MainThread.InvokeOnMainThreadAsync(() => Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = AppResources.DashboardExportCsvShareTitle,
                File = new ShareFile(filePath)
            })).ConfigureAwait(false);
        }
        catch
        {
            RunOnMain(() => ErrorMessage = AppResources.DashboardExportCsvFailed);
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

        try
        {
            var pdfBytes = BuildDashboardPdfDocument(_lastSnapshot, LookbackDays);
            var fileName = $"business-dashboard-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            await File.WriteAllBytesAsync(filePath, pdfBytes, CancellationToken.None).ConfigureAwait(false);

            await MainThread.InvokeOnMainThreadAsync(() => Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = AppResources.DashboardExportPdfShareTitle,
                File = new ShareFile(filePath)
            })).ConfigureAwait(false);
        }
        catch
        {
            RunOnMain(() => ErrorMessage = AppResources.DashboardExportPdfFailed);
        }
    }

    /// <summary>
    /// Creates a stable CSV payload containing KPI summary, top customers, and recent activity rows.
    /// </summary>
    private static string BuildDashboardCsv(BusinessDashboardSnapshot snapshot, int lookbackDays)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Section,Metric,Value");
        builder.AppendLine($"Summary,LookbackDays,{lookbackDays}");
        builder.AppendLine($"Summary,TotalSessions,{snapshot.TotalSessions}");
        builder.AppendLine($"Summary,AccrualCount,{snapshot.AccrualCount}");
        builder.AppendLine($"Summary,RedemptionCount,{snapshot.RedemptionCount}");
        builder.AppendLine($"Summary,SubscriptionStatusRefreshFailures,{snapshot.SubscriptionStatusRefreshFailures}");
        builder.AppendLine($"Summary,SubscriptionCheckoutStarts,{snapshot.SubscriptionCheckoutStarts}");
        builder.AppendLine($"Summary,SubscriptionCheckoutFailures,{snapshot.SubscriptionCheckoutFailures}");
        builder.AppendLine($"Summary,CampaignTargetingFixAppliedCount,{snapshot.CampaignTargetingFixAppliedCount}");
        builder.AppendLine($"Summary,CampaignTargetingFixNoChangeCount,{snapshot.CampaignTargetingFixNoChangeCount}");
        builder.AppendLine($"Summary,CampaignTargetingFixMetricsResetCount,{snapshot.CampaignTargetingFixMetricsResetCount}");
        builder.AppendLine($"Summary,TotalAccruedPoints,{snapshot.TotalAccruedPoints}");
        builder.AppendLine($"Summary,TotalRedeemedPoints,{snapshot.TotalRedeemedPoints}");

        builder.AppendLine();
        builder.AppendLine("TopCustomers,CustomerDisplayName,InteractionsCount");
        foreach (var customer in snapshot.TopCustomers)
        {
            builder.Append("TopCustomers,")
                .Append(EscapeCsv(customer.CustomerDisplayName)).Append(',')
                .Append(customer.InteractionsCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine();
        }

        builder.AppendLine();
        builder.AppendLine("RecentActivities,OccurredAtUtc,ActivityKind,CustomerDisplayName,PointsDelta");
        foreach (var activity in snapshot.RecentActivities.OrderByDescending(x => x.OccurredAtUtc))
        {
            builder.Append("RecentActivities,")
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
    private static byte[] BuildDashboardPdfDocument(BusinessDashboardSnapshot snapshot, int lookbackDays)
    {
        var lines = BuildDashboardReportLines(snapshot, lookbackDays);
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
    private static IReadOnlyList<string> BuildDashboardReportLines(BusinessDashboardSnapshot snapshot, int lookbackDays)
    {
        var lines = new List<string>
        {
            "Darwin Business Dashboard Report",
            $"Generated (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
            $"Window: {lookbackDays} day(s)",
            string.Empty,
            "Summary",
            $"- Total sessions: {snapshot.TotalSessions}",
            $"- Accrual count: {snapshot.AccrualCount}",
            $"- Redemption count: {snapshot.RedemptionCount}",
            $"- Subscription refresh failures: {snapshot.SubscriptionStatusRefreshFailures}",
            $"- Subscription checkout starts: {snapshot.SubscriptionCheckoutStarts}",
            $"- Subscription checkout failures: {snapshot.SubscriptionCheckoutFailures}",
            $"- Campaign quick-fix applied: {snapshot.CampaignTargetingFixAppliedCount}",
            $"- Campaign quick-fix no-change: {snapshot.CampaignTargetingFixNoChangeCount}",
            $"- Campaign quick-fix resets: {snapshot.CampaignTargetingFixMetricsResetCount}",
            $"- Total accrued points: {snapshot.TotalAccruedPoints}",
            $"- Total redeemed points: {snapshot.TotalRedeemedPoints}",
            string.Empty,
            "Top customers"
        };

        if (snapshot.TopCustomers.Count == 0)
        {
            lines.Add("- No customer interactions in selected window.");
        }
        else
        {
            lines.AddRange(snapshot.TopCustomers.Select(c => $"- {c.CustomerDisplayName} ({c.InteractionsCount})"));
        }

        lines.Add(string.Empty);
        lines.Add("Recent activity");

        if (snapshot.RecentActivities.Count == 0)
        {
            lines.Add("- No recent activity recorded.");
        }
        else
        {
            lines.AddRange(snapshot.RecentActivities
                .OrderByDescending(x => x.OccurredAtUtc)
                .Select(a => $"- {a.OccurredAtUtc:yyyy-MM-dd HH:mm} | {a.ActivityKind} | {a.CustomerDisplayName} | Δ {a.PointsDelta}"));
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
}
