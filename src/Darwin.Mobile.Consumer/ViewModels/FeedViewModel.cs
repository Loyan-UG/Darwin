using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// View model for the consumer Feed tab.
/// </summary>
/// <remarks>
/// <para>
/// Feed is intentionally business-scoped because loyalty timeline entries are attached
/// to a single loyalty account and each account belongs to one business.
/// </para>
/// <para>
/// Current implementation uses the loyalty timeline endpoint as the feed source and provides
/// account selection, paging, refresh, promotion cards, and quick context actions
/// (Open QR / Open Rewards).
/// </para>
/// </remarks>
public sealed class FeedViewModel : BaseViewModel
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly INavigationService _navigationService;

    private bool _hasLoaded;
    private bool _isLoadingMore;
    private DateTime? _nextBeforeAtUtc;
    private Guid? _nextBeforeId;
    private LoyaltyAccountSummary? _selectedAccount;
    private bool _suppressSelectionRefresh;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedViewModel"/> class.
    /// </summary>
    public FeedViewModel(ILoyaltyService loyaltyService, INavigationService navigationService)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        Items = new ObservableCollection<LoyaltyTimelineEntry>();
        PromotionItems = new ObservableCollection<PromotionFeedItem>();
        Accounts = new ObservableCollection<LoyaltyAccountSummary>();

        RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);
        LoadMoreCommand = new AsyncCommand(LoadMoreAsync, () => HasMore && !_isLoadingMore && !IsBusy);
        OpenQrCommand = new AsyncCommand(OpenQrAsync, () => CanNavigateWithSelection);
        OpenRewardsCommand = new AsyncCommand(OpenRewardsAsync, () => CanNavigateWithSelection);
        OpenPromotionCommand = new AsyncCommand<PromotionFeedItem>(OpenPromotionAsync, item => item is not null && !IsBusy);
    }

    /// <summary>
    /// Timeline-backed feed items shown in the UI.
    /// </summary>
    public ObservableCollection<LoyaltyTimelineEntry> Items { get; }

    /// <summary>
    /// Promotion cards shown at the top of feed for quick actions.
    /// </summary>
    public ObservableCollection<PromotionFeedItem> PromotionItems { get; }

    /// <summary>
    /// Gets a value indicating whether at least one promotion card exists.
    /// </summary>
    public bool HasPromotions => PromotionItems.Count > 0;

    /// <summary>
    /// Joined loyalty accounts available for business-context switching.
    /// </summary>
    public ObservableCollection<LoyaltyAccountSummary> Accounts { get; }

    /// <summary>
    /// Gets a value indicating whether the user has at least one joined account.
    /// </summary>
    public bool HasAccounts => Accounts.Count > 0;

    /// <summary>
    /// Gets a value indicating whether more timeline pages are available.
    /// </summary>
    public bool HasMore => _nextBeforeAtUtc.HasValue && _nextBeforeId.HasValue;

    /// <summary>
    /// Gets a value indicating whether at least one timeline record exists.
    /// </summary>
    public bool HasItems => Items.Count > 0;

    /// <summary>
    /// Gets a value indicating whether context-aware navigation can run.
    /// </summary>
    public bool CanNavigateWithSelection => SelectedAccount is not null && SelectedAccount.BusinessId != Guid.Empty && !IsBusy;

    /// <summary>
    /// Gets the localized points summary for the selected business context.
    /// </summary>
    public string SelectedPointsText
        => SelectedAccount is null
            ? string.Empty
            : string.Format(Resources.AppResources.FeedSelectedBusinessPointsFormat, SelectedAccount.PointsBalance);

    /// <summary>
    /// Gets or sets the currently selected account that defines the active business context of the feed.
    /// </summary>
    public LoyaltyAccountSummary? SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            if (!SetProperty(ref _selectedAccount, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanNavigateWithSelection));
            OnPropertyChanged(nameof(SelectedPointsText));
            OpenQrCommand.RaiseCanExecuteChanged();
            OpenRewardsCommand.RaiseCanExecuteChanged();
            OpenPromotionCommand.RaiseCanExecuteChanged();

            if (value is null || value.BusinessId == Guid.Empty)
            {
                return;
            }

            if (!_suppressSelectionRefresh)
            {
                _ = SafeRefreshForSelectionChangeAsync();
            }
        }
    }

    public AsyncCommand RefreshCommand { get; }

    public AsyncCommand LoadMoreCommand { get; }

    public AsyncCommand OpenQrCommand { get; }

    public AsyncCommand OpenRewardsCommand { get; }

    public AsyncCommand<PromotionFeedItem> OpenPromotionCommand { get; }

    public override async Task OnAppearingAsync()
    {
        if (_hasLoaded)
        {
            return;
        }

        await RefreshAsync();
        _hasLoaded = true;
    }

    private async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        RaiseCommandsCanExecute();

        try
        {
            var selectedBusinessId = await LoadAccountsAndResolveSelectionAsync();
            if (selectedBusinessId == Guid.Empty)
            {
                return;
            }

            await LoadPromotionsAsync(selectedBusinessId);
            await LoadFirstPageAsync(selectedBusinessId);
        }
        finally
        {
            IsBusy = false;
            RaiseCommandsCanExecute();
        }
    }

    private async Task LoadMoreAsync()
    {
        if (_isLoadingMore || IsBusy || !HasMore || SelectedAccount is null)
        {
            return;
        }

        _isLoadingMore = true;
        ErrorMessage = null;
        LoadMoreCommand.RaiseCanExecuteChanged();

        try
        {
            var request = new GetMyLoyaltyTimelinePageRequest
            {
                BusinessId = SelectedAccount.BusinessId,
                PageSize = 20,
                BeforeAtUtc = _nextBeforeAtUtc,
                BeforeId = _nextBeforeId
            };

            var result = await _loyaltyService.GetMyLoyaltyTimelinePageAsync(request, CancellationToken.None);

            if (!result.Succeeded || result.Value is null)
            {
                ErrorMessage = Resources.AppResources.FeedLoadFailed;
                return;
            }

            var ordered = result.Value.Items
                .OrderByDescending(i => i.OccurredAtUtc)
                .ToList();

            RunOnMain(() =>
            {
                foreach (var item in ordered)
                {
                    Items.Add(item);
                }

                OnPropertyChanged(nameof(HasItems));
            });

            _nextBeforeAtUtc = result.Value.NextBeforeAtUtc;
            _nextBeforeId = result.Value.NextBeforeId;
            OnPropertyChanged(nameof(HasMore));
        }
        finally
        {
            _isLoadingMore = false;
            LoadMoreCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Loads all joined accounts and resolves a deterministic selection.
    /// Priority is the previously selected business (if still present), otherwise the first account alphabetically.
    /// </summary>
    private async Task<Guid> LoadAccountsAndResolveSelectionAsync()
    {
        var previousBusinessId = SelectedAccount?.BusinessId ?? Guid.Empty;

        var accountsResult = await _loyaltyService.GetMyAccountsAsync(CancellationToken.None);
        if (!accountsResult.Succeeded || accountsResult.Value is null)
        {
            ErrorMessage = Resources.AppResources.FeedLoadFailed;
            return Guid.Empty;
        }

        var orderedAccounts = accountsResult.Value
            .Where(a => a.BusinessId != Guid.Empty)
            .OrderBy(a => a.BusinessName)
            .ToList();

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
            ErrorMessage = Resources.AppResources.FeedNoAccountsMessage;
            ClearFeedCollections();
            ClearPromotions();
            return Guid.Empty;
        }

        var selected = orderedAccounts.FirstOrDefault(a => a.BusinessId == previousBusinessId) ?? orderedAccounts[0];

        RunOnMain(() =>
        {
            _suppressSelectionRefresh = true;
            SelectedAccount = selected;
            _suppressSelectionRefresh = false;
        });

        return selected.BusinessId;
    }

    /// <summary>
    /// Loads the first page for the selected business and resets pagination state.
    /// </summary>
    private async Task LoadFirstPageAsync(Guid businessId)
    {
        var request = new GetMyLoyaltyTimelinePageRequest
        {
            BusinessId = businessId,
            PageSize = 20,
            BeforeAtUtc = null,
            BeforeId = null
        };

        var result = await _loyaltyService.GetMyLoyaltyTimelinePageAsync(request, CancellationToken.None);

        if (!result.Succeeded || result.Value is null)
        {
            ErrorMessage = Resources.AppResources.FeedLoadFailed;
            ClearFeedCollections();
            return;
        }

        var ordered = result.Value.Items
            .OrderByDescending(i => i.OccurredAtUtc)
            .ToList();

        RunOnMain(() =>
        {
            Items.Clear();
            foreach (var item in ordered)
            {
                Items.Add(item);
            }

            OnPropertyChanged(nameof(HasItems));
        });

        _nextBeforeAtUtc = result.Value.NextBeforeAtUtc;
        _nextBeforeId = result.Value.NextBeforeId;
        OnPropertyChanged(nameof(HasMore));
    }

    /// <summary>
    /// Safe wrapper for selection-triggered refresh to avoid unobserved task exceptions.
    /// </summary>
    private async Task SafeRefreshForSelectionChangeAsync()
    {
        try
        {
            if (SelectedAccount is null || SelectedAccount.BusinessId == Guid.Empty)
            {
                return;
            }

            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            ErrorMessage = null;
            RaiseCommandsCanExecute();

            await LoadPromotionsAsync(SelectedAccount.BusinessId);
            await LoadFirstPageAsync(SelectedAccount.BusinessId);
        }
        catch
        {
            ErrorMessage = Resources.AppResources.FeedLoadFailed;
        }
        finally
        {
            IsBusy = false;
            RaiseCommandsCanExecute();
        }
    }

    /// <summary>
    /// Loads promotion cards for the selected business context.
    /// </summary>
    private async Task LoadPromotionsAsync(Guid businessId)
    {
        var result = await _loyaltyService.GetMyPromotionsAsync(new MyPromotionsRequest
        {
            BusinessId = businessId,
            MaxItems = 8
        }, CancellationToken.None);

        ClearPromotions();

        if (!result.Succeeded || result.Value is null)
        {
            return;
        }

        var ordered = result.Value.Items
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.BusinessName)
            .ToList();

        RunOnMain(() =>
        {
            foreach (var item in ordered)
            {
                PromotionItems.Add(item);
            }

            OnPropertyChanged(nameof(HasPromotions));
        });
    }

    /// <summary>
    /// Navigates to QR tab using selected business as context.
    /// </summary>
    private async Task OpenQrAsync()
    {
        if (SelectedAccount is null || SelectedAccount.BusinessId == Guid.Empty)
        {
            return;
        }

        var parameters = BuildContextParameters();
        await _navigationService.GoToAsync($"//{Routes.Qr}", parameters);
    }

    /// <summary>
    /// Navigates to Rewards tab using selected business as context.
    /// </summary>
    private async Task OpenRewardsAsync()
    {
        if (SelectedAccount is null || SelectedAccount.BusinessId == Guid.Empty)
        {
            return;
        }

        var parameters = BuildContextParameters();
        await _navigationService.GoToAsync($"//{Routes.Rewards}", parameters);
    }

    /// <summary>
    /// Creates a consistent Shell parameter payload for business-context navigation.
    /// </summary>
    private Dictionary<string, object?> BuildContextParameters()
    {
        return new Dictionary<string, object?>
        {
            ["businessId"] = SelectedAccount?.BusinessId,
            ["businessName"] = SelectedAccount?.BusinessName,
            ["pointsBalance"] = SelectedAccount?.PointsBalance
        };
    }

    /// <summary>
    /// Opens promotion target using CTA kind semantics while preserving selected business context.
    /// </summary>
    private async Task OpenPromotionAsync(PromotionFeedItem? item)
    {
        if (item is null || item.BusinessId == Guid.Empty || IsBusy)
        {
            return;
        }

        var parameters = new Dictionary<string, object?>
        {
            ["businessId"] = item.BusinessId,
            ["businessName"] = item.BusinessName
        };

        if (string.Equals(item.CtaKind, "OpenQr", StringComparison.OrdinalIgnoreCase))
        {
            await _navigationService.GoToAsync($"//{Routes.Qr}", parameters);
            return;
        }

        await _navigationService.GoToAsync($"//{Routes.Rewards}", parameters);
    }

    /// <summary>
    /// Clears feed records and pagination cursor while keeping account list intact.
    /// </summary>
    private void ClearFeedCollections()
    {
        RunOnMain(() =>
        {
            Items.Clear();
            OnPropertyChanged(nameof(HasItems));
        });

        _nextBeforeAtUtc = null;
        _nextBeforeId = null;
        OnPropertyChanged(nameof(HasMore));
    }

    /// <summary>
    /// Clears promotion cards and notifies the view.
    /// </summary>
    private void ClearPromotions()
    {
        RunOnMain(() =>
        {
            PromotionItems.Clear();
            OnPropertyChanged(nameof(HasPromotions));
        });
    }

    /// <summary>
    /// Re-evaluates command state whenever busy/selection/paging state changes.
    /// </summary>
    private void RaiseCommandsCanExecute()
    {
        RefreshCommand.RaiseCanExecuteChanged();
        LoadMoreCommand.RaiseCanExecuteChanged();
        OpenQrCommand.RaiseCanExecuteChanged();
        OpenRewardsCommand.RaiseCanExecuteChanged();
        OpenPromotionCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(CanNavigateWithSelection));
    }
}