using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Common;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Integration;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// View model for the Discover tab.
/// </summary>
/// <remarks>
/// Discover contains two finalized journeys:
/// 1) Joined businesses: quick operational access for already joined memberships.
/// 2) Explore: searchable and filterable discovery surface for new businesses.
///
/// The Explore section supports:
/// - Text search (name/address).
/// - Category filtering.
/// - Optional nearby mode (location-based query) with graceful fallback.
///
/// All UI-bound collections are updated through <see cref="RunOnMain"/> to keep MAUI thread-safe.
/// </remarks>
public sealed class DiscoverViewModel : BaseViewModel
{
    private readonly IBusinessService _businessService;
    private readonly ILoyaltyService _loyaltyService;
    private readonly ILocation _location;

    private bool _hasLoaded;
    private bool _isJoinedTabSelected = true;
    private string? _searchQuery;
    private bool _isNearbyOnly;
    private BusinessCategoryKindItem? _selectedCategory;

    public DiscoverViewModel(
        IBusinessService businessService,
        ILoyaltyService loyaltyService,
        ILocation location)
    {
        _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _location = location ?? throw new ArgumentNullException(nameof(location));

        JoinedAccounts = new ObservableCollection<LoyaltyAccountSummary>();
        ExploreBusinesses = new ObservableCollection<DiscoverExploreItem>();
        CategoryKinds = new ObservableCollection<BusinessCategoryKindItem>();

        ShowJoinedTabCommand = new AsyncCommand(ShowJoinedTabAsync);
        ShowExploreTabCommand = new AsyncCommand(ShowExploreTabAsync);
        RefreshCommand = new AsyncCommand(RefreshAsync);
        SearchCommand = new AsyncCommand(SearchAsync);
        ClearFiltersCommand = new AsyncCommand(ClearFiltersAsync);
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
    /// Available category filter options resolved from API metadata.
    /// </summary>
    public ObservableCollection<BusinessCategoryKindItem> CategoryKinds { get; }

    /// <summary>
    /// Free text query used for explore endpoint filtering.
    /// </summary>
    public string? SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value);
    }

    /// <summary>
    /// Enables location-biased filtering when true.
    /// </summary>
    public bool IsNearbyOnly
    {
        get => _isNearbyOnly;
        set => SetProperty(ref _isNearbyOnly, value);
    }

    /// <summary>
    /// Selected category filter item used in Explore requests.
    /// </summary>
    public BusinessCategoryKindItem? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
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

    /// <summary>
    /// Clears explore filters and reloads explore data.
    /// </summary>
    public AsyncCommand ClearFiltersCommand { get; }

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

    private async Task ClearFiltersAsync()
    {
        SearchQuery = null;
        SelectedCategory = null;
        IsNearbyOnly = false;
        await SearchAsync();
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
                await LoadCategoryKindsAsync();
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

    private async Task LoadCategoryKindsAsync()
    {
        var response = await _businessService.GetCategoryKindsAsync(CancellationToken.None);
        var items = response?.Items?.OrderBy(i => i.DisplayName).ToList() ?? new List<BusinessCategoryKindItem>();

        RunOnMain(() =>
        {
            CategoryKinds.Clear();
            foreach (var item in items)
            {
                CategoryKinds.Add(item);
            }
        });
    }

    private async Task LoadExploreBusinessesAsync()
    {
        var trimmedQuery = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim();

        GeoCoordinateModel? near = null;
        var radiusMeters = (int?)null;

        if (IsNearbyOnly)
        {
            var current = await _location.GetCurrentAsync(CancellationToken.None);
            if (current is not null)
            {
                near = new GeoCoordinateModel
                {
                    Latitude = current.Value.lat,
                    Longitude = current.Value.lng
                };

                radiusMeters = 5000;
            }
            else
            {
                ErrorMessage = Resources.AppResources.DiscoverLocationUnavailable;
            }
        }

        var request = new BusinessListRequest
        {
            Page = 1,
            PageSize = 80,
            Query = trimmedQuery,
            AddressQuery = trimmedQuery,
            CategoryKindKey = string.IsNullOrWhiteSpace(SelectedCategory?.Key) ? null : SelectedCategory.Key,
            Near = near,
            RadiusMeters = radiusMeters
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