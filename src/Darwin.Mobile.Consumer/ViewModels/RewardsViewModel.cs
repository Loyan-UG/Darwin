using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Consumer.Services.Caching;
using Darwin.Mobile.Shared.Collections;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// View model for the consumer rewards page.
/// </summary>
/// <remarks>
/// <para>
/// This screen is intentionally business-context aware because loyalty points are scoped to a single business.
/// In other words, points earned at Business A must not be presented as universally redeemable at Business B.
/// </para>
/// <para>
/// The workflow implemented by this view model is:
/// 1) Load all joined loyalty accounts.
/// 2) Let the user choose one business account explicitly.
/// 3) Show points/rewards/history for that selected business only.
/// 4) Provide multi-business overview metrics and quick actions.
/// </para>
/// <para>
/// Threading note:
/// Any update that can affect UI-bound collections/properties is marshaled through <see cref="BaseViewModel.RunOnMain(System.Action)"/>
/// to avoid MAUI cross-thread view access exceptions.
/// </para>
/// </remarks>
public sealed class RewardsViewModel : BaseViewModel
{
    private const int MaxDisplayedAvailableRewards = 30;
    private const int MaxDisplayedRewardHistoryItems = 40;

    private readonly ILoyaltyService _loyaltyService;
    private readonly IConsumerLoyaltySnapshotCache _loyaltySnapshotCache;
    private readonly INavigationService _navigationService;
    private CancellationTokenSource? _operationCancellation;

    private Guid _businessId;
    private bool _loaded;
    private int _pointsBalance;
    private LoyaltyAccountSummary? _selectedAccount;
    private bool _suppressSelectedAccountRefresh;
    private int _availableRewardsCount;
    private int _redeemableRewardsCount;
    private string? _nextRewardTitle;
    private int? _pointsToNextReward;
    private int? _nextRewardRequiredPoints;
    private decimal? _nextRewardProgressPercent;
    private bool _expiryTrackingEnabled;
    private int _pointsExpiringSoon;
    private DateTime? _nextPointsExpiryAtUtc;

    public RewardsViewModel(
        ILoyaltyService loyaltyService,
        IConsumerLoyaltySnapshotCache loyaltySnapshotCache,
        INavigationService navigationService)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _loyaltySnapshotCache = loyaltySnapshotCache ?? throw new ArgumentNullException(nameof(loyaltySnapshotCache));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        AvailableRewards = new RangeObservableCollection<LoyaltyRewardSummary>();
        RewardHistory = new RangeObservableCollection<PointsTransaction>();
        Accounts = new RangeObservableCollection<LoyaltyAccountSummary>();

        RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);
        OpenSelectedBusinessQrCommand = new AsyncCommand(OpenSelectedBusinessQrAsync, () => CanOpenSelectedBusinessQr);
    }

    /// <summary>
    /// Gets the currently active business identifier for account-scoped operations.
    /// </summary>
    public Guid BusinessId
    {
        get => _businessId;
        private set => SetProperty(ref _businessId, value);
    }

    /// <summary>
    /// Gets the current points balance for <see cref="SelectedAccount"/>.
    /// </summary>
    public int PointsBalance
    {
        get => _pointsBalance;
        private set => SetProperty(ref _pointsBalance, value);
    }

    /// <summary>
    /// Gets the total number of configured rewards for the selected business.
    /// </summary>
    public int AvailableRewardsCount
    {
        get => _availableRewardsCount;
        private set => SetProperty(ref _availableRewardsCount, value);
    }

    /// <summary>
    /// Gets the number of rewards currently redeemable for the selected business.
    /// </summary>
    public int RedeemableRewardsCount
    {
        get => _redeemableRewardsCount;
        private set => SetProperty(ref _redeemableRewardsCount, value);
    }

    /// <summary>
    /// Gets the title of the next reward target, when one exists.
    /// </summary>
    public string? NextRewardTitle
    {
        get => _nextRewardTitle;
        private set
        {
            if (SetProperty(ref _nextRewardTitle, value))
            {
                OnPropertyChanged(nameof(HasNextRewardInsight));
                OnPropertyChanged(nameof(HasUnlockedAllRewards));
            }
        }
    }

    /// <summary>
    /// Gets the remaining points needed to unlock the next reward target.
    /// </summary>
    public int? PointsToNextReward
    {
        get => _pointsToNextReward;
        private set
        {
            if (SetProperty(ref _pointsToNextReward, value))
            {
                OnPropertyChanged(nameof(HasNextRewardInsight));
                OnPropertyChanged(nameof(HasUnlockedAllRewards));
            }
        }
    }

    /// <summary>
    /// Gets the next reward threshold in points, when one exists.
    /// </summary>
    public int? NextRewardRequiredPoints
    {
        get => _nextRewardRequiredPoints;
        private set
        {
            if (SetProperty(ref _nextRewardRequiredPoints, value))
            {
                OnPropertyChanged(nameof(HasNextRewardInsight));
            }
        }
    }

    /// <summary>
    /// Gets the percentage progress toward the next reward threshold.
    /// </summary>
    public decimal? NextRewardProgressPercent
    {
        get => _nextRewardProgressPercent;
        private set
        {
            if (SetProperty(ref _nextRewardProgressPercent, value))
            {
                OnPropertyChanged(nameof(HasNextRewardInsight));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the selected business currently tracks point expiry.
    /// </summary>
    public bool ExpiryTrackingEnabled
    {
        get => _expiryTrackingEnabled;
        private set
        {
            if (SetProperty(ref _expiryTrackingEnabled, value))
            {
                OnPropertyChanged(nameof(HasExpiryInsight));
                OnPropertyChanged(nameof(ExpiryStatusMessage));
            }
        }
    }

    /// <summary>
    /// Gets the points known to expire soon for the selected business account.
    /// </summary>
    public int PointsExpiringSoon
    {
        get => _pointsExpiringSoon;
        private set
        {
            if (SetProperty(ref _pointsExpiringSoon, value))
            {
                OnPropertyChanged(nameof(HasExpiryInsight));
            }
        }
    }

    /// <summary>
    /// Gets the next known point-expiry timestamp in UTC, when available.
    /// </summary>
    public DateTime? NextPointsExpiryAtUtc
    {
        get => _nextPointsExpiryAtUtc;
        private set
        {
            if (SetProperty(ref _nextPointsExpiryAtUtc, value))
            {
                OnPropertyChanged(nameof(HasExpiryInsight));
            }
        }
    }

    /// <summary>
    /// Gets all loyalty accounts that belong to the currently logged-in consumer.
    /// Each entry corresponds to one business membership.
    /// </summary>
    public RangeObservableCollection<LoyaltyAccountSummary> Accounts { get; }

    /// <summary>
    /// Gets history entries for the currently selected business account.
    /// </summary>
    public RangeObservableCollection<PointsTransaction> RewardHistory { get; }

    /// <summary>
    /// Gets currently available rewards for the selected business account.
    /// </summary>
    public RangeObservableCollection<LoyaltyRewardSummary> AvailableRewards { get; }

    /// <summary>
    /// Backwards-compatibility alias retained for older bindings.
    /// </summary>
    public RangeObservableCollection<LoyaltyRewardSummary> Rewards => AvailableRewards;

    /// <summary>
    /// Manual refresh command used by pull-to-refresh or button-based refresh.
    /// </summary>
    public AsyncCommand RefreshCommand { get; }

    /// <summary>
    /// Quick action command that opens QR tab for the currently selected business account.
    /// </summary>
    public AsyncCommand OpenSelectedBusinessQrCommand { get; }

    /// <summary>
    /// Gets whether any joined business accounts are currently available.
    /// </summary>
    public bool HasAccounts => Accounts.Count > 0;

    /// <summary>
    /// Gets total joined loyalty businesses count for multi-business overview.
    /// </summary>
    public int TotalJoinedBusinesses => Accounts.Count;

    /// <summary>
    /// Gets aggregated points balance across all joined businesses.
    /// </summary>
    public int TotalPointsAcrossBusinesses => Accounts.Sum(x => Math.Max(0, x.PointsBalance));

    /// <summary>
    /// Gets the top business name by current points balance.
    /// </summary>
    public string TopBusinessByPoints => Accounts
        .OrderByDescending(x => x.PointsBalance)
        .Select(x => x.BusinessName)
        .FirstOrDefault() ?? "—";

    /// <summary>
    /// Gets whether the selected account has any history entries to show.
    /// </summary>
    public bool HasHistory => RewardHistory.Count > 0;

    /// <summary>
    /// Gets whether the selected business has a next-reward progress insight to show.
    /// </summary>
    public bool HasNextRewardInsight =>
        !string.IsNullOrWhiteSpace(NextRewardTitle) ||
        NextRewardRequiredPoints.HasValue ||
        NextRewardProgressPercent.HasValue;

    /// <summary>
    /// Gets whether the selected business has already unlocked all currently configured rewards.
    /// </summary>
    public bool HasUnlockedAllRewards =>
        AvailableRewardsCount > 0 &&
        string.IsNullOrWhiteSpace(NextRewardTitle) &&
        PointsToNextReward == 0;

    /// <summary>
    /// Gets whether point-expiry insight is available for the selected business.
    /// </summary>
    public bool HasExpiryInsight =>
        ExpiryTrackingEnabled &&
        (PointsExpiringSoon > 0 || NextPointsExpiryAtUtc.HasValue);

    /// <summary>
    /// Gets a localized status message describing the current expiry-tracking behavior.
    /// </summary>
    public string ExpiryStatusMessage =>
        ExpiryTrackingEnabled
            ? AppResources.RewardsExpiryTrackingEnabled
            : AppResources.RewardsExpiryTrackingDisabled;

    /// <summary>
    /// Gets whether quick QR navigation can execute for selected account.
    /// </summary>
    public bool CanOpenSelectedBusinessQr => SelectedAccount is not null && SelectedAccount.BusinessId != Guid.Empty && !IsBusy;

    /// <summary>
    /// Gets or sets the currently selected loyalty account.
    /// </summary>
    /// <remarks>
    /// Setting this property updates <see cref="BusinessId"/> and triggers a scoped data refresh.
    /// The guard flag <c>_suppressSelectedAccountRefresh</c> prevents duplicate refresh calls
    /// when selection is set programmatically during initial loading.
    /// </remarks>
    public LoyaltyAccountSummary? SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            if (!SetProperty(ref _selectedAccount, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanOpenSelectedBusinessQr));
            OpenSelectedBusinessQrCommand.RaiseCanExecuteChanged();

            if (value is null || value.BusinessId == Guid.Empty)
            {
                return;
            }

            BusinessId = value.BusinessId;

            if (!_suppressSelectedAccountRefresh)
            {
                _ = SafeRefreshForSelectionChangeAsync();
            }
        }
    }

    /// <summary>
    /// Allows external navigation flows to preselect a business context.
    /// </summary>
    public void SetBusiness(Guid businessId)
    {
        if (businessId == Guid.Empty)
        {
            throw new ArgumentException("Business id must not be empty.", nameof(businessId));
        }

        BusinessId = businessId;
    }

    /// <summary>
    /// Loads data once when the rewards page becomes visible for the first time.
    /// </summary>
    public override async Task OnAppearingAsync()
    {
        if (_loaded)
        {
            return;
        }

        await RefreshAsync();
        _loaded = true;
    }

    /// <summary>
    /// Cancels any in-flight rewards load or navigation operation when the page is no longer visible.
    /// </summary>
    /// <returns>A completed task because cancellation is signaled synchronously.</returns>
    public override Task OnDisappearingAsync()
    {
        CancelCurrentOperation();
        EndBusyState();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Full refresh pipeline:
    /// - load joined accounts
    /// - choose active account
    /// - load account-scoped balance/rewards/history
    /// </summary>
    private async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        BeginBusyState();
        var operationCancellation = BeginCurrentOperation();

        try
        {
            var cancellationToken = operationCancellation.Token;
            var overviewResult = await _loyaltySnapshotCache.GetMyOverviewAsync(cancellationToken);

            if (!overviewResult.Succeeded || overviewResult.Value is null)
            {
                RunOnMain(() => ErrorMessage = Resources.AppResources.RewardsLoadAccountsFailed);
                return;
            }

            var orderedAccounts = overviewResult.Value.Accounts
                .Where(a => a.BusinessId != Guid.Empty)
                .OrderBy(a => a.BusinessName)
                .ToList();

            // The accounts list is UI-bound; always update it on the main thread.
            RunOnMain(() =>
            {
                Accounts.ReplaceRange(orderedAccounts);
                RaiseOverviewChanged();
            });

            if (orderedAccounts.Count == 0)
            {
                // If there is no joined account, clear stale UI and show a focused message.
                RunOnMain(() =>
                {
                    PointsBalance = 0;
                    AvailableRewards.ClearRange();
                    RewardHistory.ClearRange();
                    OnPropertyChanged(nameof(HasHistory));
                });

                RunOnMain(() => ErrorMessage = Resources.AppResources.RewardsNoAccountsFound);
                return;
            }

            if (BusinessId == Guid.Empty)
            {
                BusinessId = orderedAccounts[0].BusinessId;
            }

            var selected = orderedAccounts.FirstOrDefault(a => a.BusinessId == BusinessId) ?? orderedAccounts[0];

            // Programmatic assignment should not trigger an additional redundant refresh.
            RunOnMain(() =>
            {
                _suppressSelectedAccountRefresh = true;
                SelectedAccount = selected;
                _suppressSelectedAccountRefresh = false;
            });

            await LoadSelectedBusinessDataAsync(BusinessId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Navigation away from rewards intentionally cancels stale reward loads.
        }
        finally
        {
            EndBusyState();
            EndCurrentOperation(operationCancellation);
        }
    }

    /// <summary>
    /// Safe wrapper around selection-change refresh.
    /// </summary>
    /// <remarks>
    /// The selection-change path is triggered from a property setter and therefore runs fire-and-forget.
    /// This wrapper ensures any unexpected exception is converted to a user-facing message instead of
    /// bubbling as an unobserved task exception.
    /// </remarks>
    private async Task SafeRefreshForSelectionChangeAsync()
    {
        try
        {
            await RefreshForSelectedBusinessAsync();
        }
        catch
        {
            RunOnMain(() => ErrorMessage = Resources.AppResources.RewardsLoadAccountSummaryFailed);
        }
    }

    /// <summary>
    /// Refreshes only the currently selected business context.
    /// </summary>
    private async Task RefreshForSelectedBusinessAsync()
    {
        if (IsBusy || BusinessId == Guid.Empty)
        {
            return;
        }

        BeginBusyState();
        var operationCancellation = BeginCurrentOperation();

        try
        {
            await LoadSelectedBusinessDataAsync(BusinessId, operationCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            // Selection changes intentionally cancel stale reward loads.
        }
        finally
        {
            EndBusyState();
            EndCurrentOperation(operationCancellation);
        }
    }

    /// <summary>
    /// Loads all data required for a single business-scoped rewards view.
    /// </summary>
    /// <param name="businessId">Business identifier that owns the selected loyalty account.</param>
    private async Task LoadSelectedBusinessDataAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var dashboardResult = await _loyaltyService.GetBusinessDashboardAsync(businessId, cancellationToken);

        if (!dashboardResult.Succeeded || dashboardResult.Value is null)
        {
            RunOnMain(() => ErrorMessage = Resources.AppResources.RewardsLoadAccountSummaryFailed);
            ClearRewardInsights();
            return;
        }

        var dashboard = dashboardResult.Value;

        // Keep points strictly tied to the selected business account.
        RunOnMain(() =>
        {
            PointsBalance = dashboard.Account.PointsBalance;
            AvailableRewardsCount = dashboard.AvailableRewardsCount;
            RedeemableRewardsCount = dashboard.RedeemableRewardsCount;
            NextRewardTitle = dashboard.Account.NextRewardTitle ?? dashboard.NextReward?.Name;
            PointsToNextReward = dashboard.PointsToNextReward ?? dashboard.Account.PointsToNextReward;
            NextRewardRequiredPoints = dashboard.NextRewardRequiredPoints ?? dashboard.Account.NextRewardRequiredPoints;
            NextRewardProgressPercent = dashboard.NextRewardProgressPercent ?? dashboard.Account.NextRewardProgressPercent;
            ExpiryTrackingEnabled = dashboard.ExpiryTrackingEnabled;
            PointsExpiringSoon = dashboard.PointsExpiringSoon;
            NextPointsExpiryAtUtc = dashboard.NextPointsExpiryAtUtc;
        });

        UpdateSelectedAccountFromDashboard(dashboard.Account);

        var rewardsResult = await _loyaltyService.GetAvailableRewardsAsync(businessId, cancellationToken);

        if (rewardsResult?.Value != null)
        {
            var orderedRewards = rewardsResult.Value
                .OrderBy(r => r.RequiredPoints)
                .Take(MaxDisplayedAvailableRewards)
                .ToList();

            RunOnMain(() => AvailableRewards.ReplaceRange(orderedRewards));
        }
        else
        {
            RunOnMain(AvailableRewards.ClearRange);
            RunOnMain(() => ErrorMessage = Resources.AppResources.RewardsLoadRewardsFailed);
        }

        if (dashboard.RecentTransactions is not null)
        {
            var orderedHistory = dashboard.RecentTransactions
                .OrderByDescending(h => h.OccurredAtUtc)
                .Take(MaxDisplayedRewardHistoryItems)
                .ToList();

            RunOnMain(() =>
            {
                RewardHistory.ReplaceRange(orderedHistory);
                OnPropertyChanged(nameof(HasHistory));
            });
        }
        else
        {
            RunOnMain(() =>
            {
                ErrorMessage = Resources.AppResources.RewardsLoadHistoryFailed;
                RewardHistory.ClearRange();
                OnPropertyChanged(nameof(HasHistory));
            });
        }
    }

    private void UpdateSelectedAccountFromDashboard(LoyaltyAccountSummary updatedAccount)
    {
        if (updatedAccount.BusinessId == Guid.Empty)
        {
            return;
        }

        RunOnMain(() =>
        {
            var existingIndex = Accounts
                .Select((account, index) => new { account, index })
                .FirstOrDefault(x => x.account.BusinessId == updatedAccount.BusinessId)?.index;

            if (existingIndex is null)
            {
                return;
            }

            Accounts[existingIndex.Value] = updatedAccount;
            RaiseOverviewChanged();

            if (SelectedAccount?.BusinessId == updatedAccount.BusinessId)
            {
                _suppressSelectedAccountRefresh = true;
                SelectedAccount = updatedAccount;
                _suppressSelectedAccountRefresh = false;
            }
        });
    }

    private void ClearRewardInsights()
    {
        RunOnMain(() =>
        {
            AvailableRewardsCount = 0;
            RedeemableRewardsCount = 0;
            NextRewardTitle = null;
            PointsToNextReward = null;
            NextRewardRequiredPoints = null;
            NextRewardProgressPercent = null;
            ExpiryTrackingEnabled = false;
            PointsExpiringSoon = 0;
            NextPointsExpiryAtUtc = null;
        });
    }

    /// <summary>
    /// Opens QR tab for the currently selected account business context.
    /// </summary>
    private async Task OpenSelectedBusinessQrAsync()
    {
        if (!CanOpenSelectedBusinessQr || SelectedAccount is null || IsBusy)
        {
            return;
        }

        BeginBusyState();
        var operationCancellation = BeginCurrentOperation();

        try
        {
            IDictionary<string, object?> parameters = new Dictionary<string, object?>
            {
                ["businessId"] = SelectedAccount.BusinessId
            };

            await _navigationService.GoToAsync($"//{Routes.Qr}", parameters);
        }
        catch (OperationCanceledException)
        {
            // Navigation away from rewards intentionally cancels stale QR navigation work.
        }
        finally
        {
            EndBusyState();
            EndCurrentOperation(operationCancellation);
        }
    }

    /// <summary>
    /// Starts a cancellable rewards operation and cancels any stale operation still in-flight.
    /// </summary>
    private CancellationTokenSource BeginCurrentOperation()
    {
        var current = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _operationCancellation, current);
        previous?.Cancel();
        return current;
    }

    /// <summary>
    /// Cancels the active rewards operation without disposing a token source still observed by service code.
    /// </summary>
    private void CancelCurrentOperation()
    {
        var current = Interlocked.Exchange(ref _operationCancellation, null);
        current?.Cancel();
    }

    /// <summary>
    /// Releases a completed rewards operation when it still owns the active operation slot.
    /// </summary>
    /// <param name="operationCancellation">Completed operation token source.</param>
    private void EndCurrentOperation(CancellationTokenSource operationCancellation)
    {
        if (ReferenceEquals(_operationCancellation, operationCancellation))
        {
            _operationCancellation = null;
        }

        operationCancellation.Dispose();
    }

    /// <summary>
    /// Applies busy state and disables rewards actions for the current operation.
    /// </summary>
    private void BeginBusyState()
    {
        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            RaiseOverviewChanged();
        });
    }

    /// <summary>
    /// Clears busy state and re-enables rewards actions after the current operation.
    /// </summary>
    private void EndBusyState()
    {
        RunOnMain(() =>
        {
            IsBusy = false;
            RaiseOverviewChanged();
        });
    }

    /// <summary>
    /// Raises change notifications for aggregate overview and quick-action state.
    /// </summary>
    private void RaiseOverviewChanged()
    {
        OnPropertyChanged(nameof(HasAccounts));
        OnPropertyChanged(nameof(TotalJoinedBusinesses));
        OnPropertyChanged(nameof(TotalPointsAcrossBusinesses));
        OnPropertyChanged(nameof(TopBusinessByPoints));
        OnPropertyChanged(nameof(CanOpenSelectedBusinessQr));
        RefreshCommand.RaiseCanExecuteChanged();
        OpenSelectedBusinessQrCommand.RaiseCanExecuteChanged();
    }
}
