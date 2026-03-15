using System;
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

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
