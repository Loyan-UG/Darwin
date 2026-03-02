using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// View model for the Discover tab.
/// </summary>
/// <remarks>
/// Discover intentionally has two user journeys:
/// 1) Joined businesses (quick access): businesses where the member already owns a loyalty account.
/// 2) Explore businesses: searchable catalog for joining new businesses.
///
/// This separation prevents mixing "already joined" actions with "explore and join" actions,
/// and keeps reward usage clearly business-scoped.
/// </remarks>
public sealed class DiscoverViewModel : BaseViewModel
{
    private readonly IBusinessService _businessService;
    private readonly ILoyaltyService _loyaltyService;

    private bool _hasLoaded;
    private bool _isJoinedTabSelected = true;
    private string? _searchQuery;

    public DiscoverViewModel(IBusinessService businessService, ILoyaltyService loyaltyService)
    {
        _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));

        JoinedAccounts = new ObservableCollection<LoyaltyAccountSummary>();
        ExploreBusinesses = new ObservableCollection<DiscoverExploreItem>();

        ShowJoinedTabCommand = new AsyncCommand(ShowJoinedTabAsync);
        ShowExploreTabCommand = new AsyncCommand(ShowExploreTabAsync);
        RefreshCommand = new AsyncCommand(RefreshAsync);
        SearchCommand = new AsyncCommand(SearchAsync);
    }

    /// <summary>
    /// Businesses where current user already joined loyalty.
    /// </summary>
    public ObservableCollection<LoyaltyAccountSummary> JoinedAccounts { get; }

    /// <summary>
    /// Search/explore result list with joined-state metadata.
    /// </summary>
    public ObservableCollection<DiscoverExploreItem> ExploreBusinesses { get; }

    /// <summary>
    /// Free text query used for explore endpoint filtering.
    /// </summary>
    public string? SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value);
    }

    /// <summary>
    /// True when "My Businesses" tab is selected.
    /// </summary>
    public bool IsJoinedTabSelected
    {
        get => _isJoinedTabSelected;
        private set
        {
            if (SetProperty(ref _isJoinedTabSelected, value))
            {
                OnPropertyChanged(nameof(IsExploreTabSelected));
            }
        }
    }

    /// <summary>
    /// True when "Explore" tab is selected.
    /// </summary>
    public bool IsExploreTabSelected => !IsJoinedTabSelected;

    public bool HasJoinedAccounts => JoinedAccounts.Count > 0;

    public bool HasExploreResults => ExploreBusinesses.Count > 0;

    /// <summary>
    /// Tab command: switch to joined businesses and refresh that section.
    /// </summary>
    public AsyncCommand ShowJoinedTabCommand { get; }

    /// <summary>
    /// Tab command: switch to explore and refresh explore list.
    /// </summary>
    public AsyncCommand ShowExploreTabCommand { get; }

    /// <summary>
    /// Manual refresh command for currently selected tab.
    /// </summary>
    public AsyncCommand RefreshCommand { get; }

    /// <summary>
    /// Search command for explore tab.
    /// </summary>
    public AsyncCommand SearchCommand { get; }

    public override async Task OnAppearingAsync()
    {
        if (_hasLoaded)
        {
            return;
        }

        await RefreshAsync();
        _hasLoaded = true;
    }

    private async Task ShowJoinedTabAsync()
    {
        if (IsJoinedTabSelected)
        {
            return;
        }

        IsJoinedTabSelected = true;
        await RefreshAsync();
    }

    private async Task ShowExploreTabAsync()
    {
        if (IsExploreTabSelected)
        {
            return;
        }

        IsJoinedTabSelected = false;
        await RefreshAsync();
    }

    private async Task SearchAsync()
    {
        if (!IsExploreTabSelected)
        {
            IsJoinedTabSelected = false;
        }

        await LoadExploreBusinessesAsync();
    }

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
            // Joined accounts are loaded first so Explore can label each business as joined/not-joined.
            await LoadJoinedAccountsAsync();

            if (IsExploreTabSelected)
            {
                await LoadExploreBusinessesAsync();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadJoinedAccountsAsync()
    {
        var accountsResult = await _loyaltyService.GetMyAccountsAsync(CancellationToken.None);

        if (!accountsResult.Succeeded || accountsResult.Value is null)
        {
            ErrorMessage = Resources.AppResources.DiscoverLoadJoinedFailed;

            RunOnMain(() =>
            {
                JoinedAccounts.Clear();
                OnPropertyChanged(nameof(HasJoinedAccounts));
            });

            return;
        }

        var ordered = accountsResult.Value
            .Where(a => a.BusinessId != Guid.Empty)
            .OrderBy(a => a.BusinessName)
            .ToList();

        RunOnMain(() =>
        {
            JoinedAccounts.Clear();
            foreach (var account in ordered)
            {
                JoinedAccounts.Add(account);
            }

            OnPropertyChanged(nameof(HasJoinedAccounts));
        });
    }

    private async Task LoadExploreBusinessesAsync()
    {
        var request = new BusinessListRequest
        {
            Page = 1,
            PageSize = 80,
            Query = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim(),
            AddressQuery = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim()
        };

        var response = await _businessService.ListAsync(request, CancellationToken.None);

        if (response?.Items is null)
        {
            ErrorMessage = Resources.AppResources.DiscoverLoadExploreFailed;

            RunOnMain(() =>
            {
                ExploreBusinesses.Clear();
                OnPropertyChanged(nameof(HasExploreResults));
            });

            return;
        }

        // Build joined id set once to avoid O(n*m) checks while projecting explore list.
        var joinedSet = new HashSet<Guid>(JoinedAccounts.Select(a => a.BusinessId));

        var projected = response.Items
            .OrderBy(b => b.Name)
            .Select(b => new DiscoverExploreItem
            {
                Business = b,
                IsJoined = joinedSet.Contains(b.Id)
            })
            .ToList();

        RunOnMain(() =>
        {
            ExploreBusinesses.Clear();
            foreach (var item in projected)
            {
                ExploreBusinesses.Add(item);
            }

            OnPropertyChanged(nameof(HasExploreResults));
        });
    }
}