using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Commands;
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
/// </para>
/// <para>
/// Threading note:
/// Any update that can affect UI-bound collections/properties is marshaled through <see cref="RunOnMain"/>
/// to avoid MAUI cross-thread view access exceptions.
/// </para>
/// </remarks>
public sealed class RewardsViewModel : BaseViewModel
{
    private readonly ILoyaltyService _loyaltyService;

    private Guid _businessId;
    private bool _loaded;
    private int _pointsBalance;
    private LoyaltyAccountSummary? _selectedAccount;
    private bool _suppressSelectedAccountRefresh;

    public RewardsViewModel(ILoyaltyService loyaltyService)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));

        AvailableRewards = new ObservableCollection<LoyaltyRewardSummary>();
        RewardHistory = new ObservableCollection<PointsTransaction>();
        Accounts = new ObservableCollection<LoyaltyAccountSummary>();

        RefreshCommand = new AsyncCommand(RefreshAsync);
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
    /// Gets all loyalty accounts that belong to the currently logged-in consumer.
    /// Each entry corresponds to one business membership.
    /// </summary>
    public ObservableCollection<LoyaltyAccountSummary> Accounts { get; }

    /// <summary>
    /// Gets history entries for the currently selected business account.
    /// </summary>
    public ObservableCollection<PointsTransaction> RewardHistory { get; }

    /// <summary>
    /// Gets currently available rewards for the selected business account.
    /// </summary>
    public ObservableCollection<LoyaltyRewardSummary> AvailableRewards { get; }

    /// <summary>
    /// Backwards-compatibility alias retained for older bindings.
    /// </summary>
    public ObservableCollection<LoyaltyRewardSummary> Rewards => AvailableRewards;

    /// <summary>
    /// Manual refresh command used by pull-to-refresh or button-based refresh.
    /// </summary>
    public AsyncCommand RefreshCommand { get; }

    /// <summary>
    /// Gets whether any joined business accounts are currently available.
    /// </summary>
    public bool HasAccounts => Accounts.Count > 0;

    /// <summary>
    /// Gets whether the selected account has any history entries to show.
    /// </summary>
    public bool HasHistory => RewardHistory.Count > 0;

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
            throw new ArgumentException("Business id must not be empty.", nameof(businessId));

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

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var accountsResult = await _loyaltyService.GetMyAccountsAsync(CancellationToken.None);

            if (!accountsResult.Succeeded || accountsResult.Value is null)
            {
                ErrorMessage = Resources.AppResources.RewardsLoadAccountsFailed;
                return;
            }

            var orderedAccounts = accountsResult.Value
                .Where(a => a.BusinessId != Guid.Empty)
                .OrderBy(a => a.BusinessName)
                .ToList();

            // The accounts list is UI-bound; always update it on the main thread.
            RunOnMain(() =>
            {
                Accounts.Clear();
                foreach (var account in orderedAccounts)
                {
                    Accounts.Add(account);
                }

                OnPropertyChanged(nameof(HasAccounts));
            });

            if (orderedAccounts.Count == 0)
            {
                // If there is no joined account, clear stale UI and show a focused message.
                RunOnMain(() =>
                {
                    PointsBalance = 0;
                    AvailableRewards.Clear();
                    RewardHistory.Clear();
                    OnPropertyChanged(nameof(HasHistory));
                });

                ErrorMessage = Resources.AppResources.RewardsNoAccountsFound;
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

            await LoadSelectedBusinessDataAsync(BusinessId);
        }
        finally
        {
            IsBusy = false;
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
            ErrorMessage = Resources.AppResources.RewardsLoadAccountSummaryFailed;
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

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            await LoadSelectedBusinessDataAsync(BusinessId);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Loads all data required for a single business-scoped rewards view.
    /// </summary>
    /// <param name="businessId">Business identifier that owns the selected loyalty account.</param>
    private async Task LoadSelectedBusinessDataAsync(Guid businessId)
    {
        var accountResult = await _loyaltyService.GetAccountSummaryAsync(businessId, CancellationToken.None);

        if (accountResult?.Value == null)
        {
            ErrorMessage = Resources.AppResources.RewardsLoadAccountSummaryFailed;
            return;
        }

        // Keep points strictly tied to the selected business account.
        RunOnMain(() => PointsBalance = accountResult.Value.PointsBalance);

        var rewardsResult = await _loyaltyService.GetAvailableRewardsAsync(businessId, CancellationToken.None);

        RunOnMain(AvailableRewards.Clear);

        if (rewardsResult?.Value != null)
        {
            var orderedRewards = rewardsResult.Value.OrderBy(r => r.RequiredPoints).ToList();

            RunOnMain(() =>
            {
                foreach (var reward in orderedRewards)
                {
                    AvailableRewards.Add(reward);
                }
            });
        }
        else
        {
            ErrorMessage = Resources.AppResources.RewardsLoadRewardsFailed;
        }

        var historyResult = await _loyaltyService.GetMyHistoryAsync(businessId, CancellationToken.None);

        RunOnMain(RewardHistory.Clear);

        if (historyResult.Succeeded && historyResult.Value is not null)
        {
            var orderedHistory = historyResult.Value
                .OrderByDescending(h => h.OccurredAtUtc)
                .ToList();

            RunOnMain(() =>
            {
                foreach (var transaction in orderedHistory)
                {
                    RewardHistory.Add(transaction);
                }

                OnPropertyChanged(nameof(HasHistory));
            });
        }
        else
        {
            ErrorMessage = Resources.AppResources.RewardsLoadHistoryFailed;
            RunOnMain(() => OnPropertyChanged(nameof(HasHistory)));
        }
    }
}