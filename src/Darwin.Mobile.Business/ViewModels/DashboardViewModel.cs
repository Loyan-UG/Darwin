using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Business.Services.Reporting;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// Business dashboard view model for lightweight KPI and reporting cards.
/// </summary>
public sealed class DashboardViewModel : BaseViewModel
{
    private readonly IBusinessActivityTracker _activityTracker;

    private bool _loadedOnce;
    private int _lookbackDays = 7;

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

        try
        {
            var window = TimeSpan.FromDays(Math.Clamp(LookbackDays, 1, 30));
            var snapshot = await _activityTracker
                .GetDashboardSnapshotAsync(window, CancellationToken.None)
                .ConfigureAwait(false);

            RunOnMain(() =>
            {
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
            });
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = $"{AppResources.DashboardLoadFailed} {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            RefreshCommand.RaiseCanExecuteChanged();
        }
    }
}
