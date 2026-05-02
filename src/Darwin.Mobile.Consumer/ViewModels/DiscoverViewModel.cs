using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Common;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Consumer.Services.Caching;
using Darwin.Mobile.Shared.Collections;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Integration;
using Darwin.Mobile.Shared.Services;
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
/// All UI-bound collections are updated through <see cref="BaseViewModel.RunOnMain(System.Action)"/> to keep MAUI thread-safe.
/// </remarks>
public sealed class DiscoverViewModel : BaseViewModel
{
    public sealed class NearbyRadiusOption
    {
        public required int Meters { get; init; }

        public required string DisplayName { get; init; }
    }

    private readonly IBusinessService _businessService;
    private readonly IConsumerLoyaltySnapshotCache _loyaltySnapshotCache;
    private readonly ILocation _location;
    private CancellationTokenSource? _loadCancellation;

    private bool _hasLoaded;
    private bool _isJoinedTabSelected = true;
    private string? _searchQuery;
    private bool _isNearbyOnly;
    private BusinessCategoryKindItem? _selectedCategory;
    private int _selectedNearbyRadiusMeters = DefaultNearbyRadiusMeters;
    private NearbyRadiusOption? _selectedNearbyRadiusOption;

    private const int DefaultNearbyRadiusMeters = 5000;

    public DiscoverViewModel(
        IBusinessService businessService,
        IConsumerLoyaltySnapshotCache loyaltySnapshotCache,
        ILocation location)
    {
        _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        _loyaltySnapshotCache = loyaltySnapshotCache ?? throw new ArgumentNullException(nameof(loyaltySnapshotCache));
        _location = location ?? throw new ArgumentNullException(nameof(location));

        JoinedAccounts = new RangeObservableCollection<LoyaltyAccountSummary>();
        ExploreBusinesses = new RangeObservableCollection<DiscoverExploreItem>();
        DisplayItems = new RangeObservableCollection<DiscoverDisplayItem>();
        CategoryKinds = new ObservableCollection<BusinessCategoryKindItem>();
        NearbyRadiusOptions = new ObservableCollection<int> { 1000, 3000, 5000, 10000 };
        NearbyRadiusChoices = new ObservableCollection<NearbyRadiusOption>(
            NearbyRadiusOptions.Select(static meters => new NearbyRadiusOption
            {
                Meters = meters,
                DisplayName = string.Format(CultureInfo.CurrentCulture, AppResources.DiscoverNearbyRadiusMetersFormat, meters)
            }));
        _selectedNearbyRadiusOption = NearbyRadiusChoices.FirstOrDefault(x => x.Meters == DefaultNearbyRadiusMeters);

        ShowJoinedTabCommand = new AsyncCommand(ShowJoinedTabAsync, () => !IsBusy);
        ShowExploreTabCommand = new AsyncCommand(ShowExploreTabAsync, () => !IsBusy);
        RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);
        SearchCommand = new AsyncCommand(SearchAsync, () => !IsBusy);
        ClearFiltersCommand = new AsyncCommand(ClearFiltersAsync, () => !IsBusy);
    }

    /// <summary>
    /// Businesses where current user already joined loyalty.
    /// </summary>
    public RangeObservableCollection<LoyaltyAccountSummary> JoinedAccounts { get; }

    /// <summary>
    /// Search/explore result list with joined-state metadata.
    /// </summary>
    public RangeObservableCollection<DiscoverExploreItem> ExploreBusinesses { get; }

    /// <summary>
    /// Rows rendered by the single virtualized Discover list for the currently selected tab.
    /// </summary>
    public RangeObservableCollection<DiscoverDisplayItem> DisplayItems { get; }

    /// <summary>
    /// Available category filter options resolved from API metadata.
    /// </summary>
    public ObservableCollection<BusinessCategoryKindItem> CategoryKinds { get; }

    /// <summary>
    /// Predefined nearby radius options (in meters) for Explore nearby mode.
    /// </summary>
    public ObservableCollection<int> NearbyRadiusOptions { get; }

    /// <summary>
    /// Preformatted nearby-radius choices used by the chip selector.
    /// </summary>
    public ObservableCollection<NearbyRadiusOption> NearbyRadiusChoices { get; }

    /// <summary>
    /// Selected nearby radius in meters used when <see cref="IsNearbyOnly"/> is enabled.
    /// </summary>
    public int SelectedNearbyRadiusMeters
    {
        get => _selectedNearbyRadiusMeters;
        set
        {
            if (!SetProperty(ref _selectedNearbyRadiusMeters, value))
            {
                return;
            }

            SyncSelectedNearbyRadiusOption(value);
        }
    }

    /// <summary>
    /// Selected nearby-radius option for chip group binding.
    /// </summary>
    public NearbyRadiusOption? SelectedNearbyRadiusOption
    {
        get => _selectedNearbyRadiusOption;
        set
        {
            if (!SetProperty(ref _selectedNearbyRadiusOption, value) || value is null)
            {
                return;
            }

            SelectedNearbyRadiusMeters = value.Meters;
        }
    }

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

    /// <summary>
    /// Total number of joined loyalty businesses for current member.
    /// </summary>
    public int JoinedBusinessCount => JoinedAccounts.Count;

    /// <summary>
    /// Sum of points across all joined loyalty businesses.
    /// </summary>
    public int TotalJoinedPointsBalance => JoinedAccounts.Sum(a => Math.Max(0, a.PointsBalance));

    /// <summary>
    /// Business with currently highest points balance (if any).
    /// </summary>
    public string TopJoinedBusinessName => JoinedAccounts
        .OrderByDescending(a => a.PointsBalance)
        .Select(a => a.BusinessName)
        .FirstOrDefault() ?? "—";

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

    /// <summary>
    /// Cancels any in-flight Discover load when the page is no longer visible.
    /// </summary>
    /// <returns>A completed task because cancellation is signaled synchronously.</returns>
    public override Task OnDisappearingAsync()
    {
        CancelCurrentLoad();
        EndBusyState();
        return Task.CompletedTask;
    }

    private async Task ShowJoinedTabAsync()
    {
        if (IsJoinedTabSelected)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsJoinedTabSelected = true;
            RebuildDisplayItemsForCurrentTab();
        });
        await RefreshAsync();
    }

    private async Task ShowExploreTabAsync()
    {
        if (IsExploreTabSelected)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsJoinedTabSelected = false;
            RebuildDisplayItemsForCurrentTab();
        });
        await RefreshAsync();
    }

    private async Task SearchAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (!IsExploreTabSelected)
        {
            RunOnMain(() => IsJoinedTabSelected = false);
        }

        BeginBusyState();
        var loadCancellation = BeginCurrentLoad();

        try
        {
            await LoadExploreBusinessesAsync(loadCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            // Navigation away from Discover intentionally cancels stale search work.
        }
        finally
        {
            EndBusyState();
            EndCurrentLoad(loadCancellation);
        }
    }

    private async Task ClearFiltersAsync()
    {
        if (IsBusy)
        {
            return;
        }

        RunOnMain(() =>
        {
            SearchQuery = null;
            SelectedCategory = null;
            IsNearbyOnly = false;
            SelectedNearbyRadiusMeters = DefaultNearbyRadiusMeters;
        });
        await SearchAsync();
    }

    /// <summary>
    /// Keeps the chip selector synchronized when nearby radius is changed programmatically.
    /// </summary>
    /// <param name="meters">The selected radius in meters.</param>
    private void SyncSelectedNearbyRadiusOption(int meters)
    {
        var option = NearbyRadiusChoices.FirstOrDefault(x => x.Meters == meters);
        if (option is not null && !ReferenceEquals(_selectedNearbyRadiusOption, option))
        {
            _selectedNearbyRadiusOption = option;
            OnPropertyChanged(nameof(SelectedNearbyRadiusOption));
        }
    }

    private async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        BeginBusyState();
        var loadCancellation = BeginCurrentLoad();

        try
        {
            var cancellationToken = loadCancellation.Token;
            // Joined accounts are loaded first so Explore can label each business as joined/not-joined.
            await LoadJoinedAccountsAsync(cancellationToken);

            if (IsExploreTabSelected)
            {
                await LoadCategoryKindsAsync(cancellationToken);
                await LoadExploreBusinessesAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Navigation away from Discover intentionally cancels stale list loads.
        }
        finally
        {
            EndBusyState();
            EndCurrentLoad(loadCancellation);
        }
    }

    /// <summary>
    /// Refreshes command availability after busy-state transitions so buttons and pull gestures cannot start overlapping requests.
    /// </summary>
    private void RaiseCommandStates()
    {
        ShowJoinedTabCommand.RaiseCanExecuteChanged();
        ShowExploreTabCommand.RaiseCanExecuteChanged();
        RefreshCommand.RaiseCanExecuteChanged();
        SearchCommand.RaiseCanExecuteChanged();
        ClearFiltersCommand.RaiseCanExecuteChanged();
    }

    private async Task LoadJoinedAccountsAsync(CancellationToken cancellationToken)
    {
        var accountsResult = await _loyaltySnapshotCache.GetMyAccountsAsync(cancellationToken);

        if (!accountsResult.Succeeded || accountsResult.Value is null)
        {
            RunOnMain(() =>
            {
                ErrorMessage = Resources.AppResources.DiscoverLoadJoinedFailed;
                JoinedAccounts.ClearRange();
                RebuildDisplayItemsForCurrentTab();
                OnPropertyChanged(nameof(HasJoinedAccounts));
                OnPropertyChanged(nameof(JoinedBusinessCount));
                OnPropertyChanged(nameof(TotalJoinedPointsBalance));
                OnPropertyChanged(nameof(TopJoinedBusinessName));
            });

            return;
        }

        var ordered = accountsResult.Value
            .Where(a => a.BusinessId != Guid.Empty)
            .OrderBy(a => a.BusinessName)
            .ToList();

        RunOnMain(() =>
        {
            JoinedAccounts.ReplaceRange(ordered);
            RebuildDisplayItemsForCurrentTab();
            OnPropertyChanged(nameof(HasJoinedAccounts));
            OnPropertyChanged(nameof(JoinedBusinessCount));
            OnPropertyChanged(nameof(TotalJoinedPointsBalance));
            OnPropertyChanged(nameof(TopJoinedBusinessName));
        });
    }

    private async Task LoadCategoryKindsAsync(CancellationToken cancellationToken)
    {
        var response = await _businessService.GetCategoryKindsAsync(cancellationToken);
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

    private async Task LoadExploreBusinessesAsync(CancellationToken cancellationToken)
    {
        var trimmedQuery = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim();

        GeoCoordinateModel? near = null;
        var radiusMeters = (int?)null;

        if (IsNearbyOnly)
        {
            var current = await _location.GetCurrentAsync(cancellationToken);
            if (current is not null)
            {
                near = new GeoCoordinateModel
                {
                    Latitude = current.Value.lat,
                    Longitude = current.Value.lng
                };

                radiusMeters = SelectedNearbyRadiusMeters <= 0 ? DefaultNearbyRadiusMeters : SelectedNearbyRadiusMeters;
            }
            else
            {
                RunOnMain(() => ErrorMessage = Resources.AppResources.DiscoverLocationUnavailable);
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

        var response = await _businessService.ListAsync(request, cancellationToken);

        if (response?.Items is null)
        {
            RunOnMain(() =>
            {
                ErrorMessage = Resources.AppResources.DiscoverLoadExploreFailed;
                ExploreBusinesses.ClearRange();
                RebuildDisplayItemsForCurrentTab();
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
            ExploreBusinesses.ReplaceRange(projected);
            RebuildDisplayItemsForCurrentTab();
            OnPropertyChanged(nameof(HasExploreResults));
        });
    }

    /// <summary>
    /// Rebuilds the single virtualized list from the currently selected tab's backing collection.
    /// </summary>
    private void RebuildDisplayItemsForCurrentTab()
    {
        var items = IsJoinedTabSelected
            ? JoinedAccounts.Select(DiscoverDisplayItem.FromJoinedAccount)
            : ExploreBusinesses.Select(DiscoverDisplayItem.FromExploreItem);

        DisplayItems.ReplaceRange(items);
    }

    /// <summary>
    /// Starts a cancellable Discover load and cancels any stale load still in-flight.
    /// </summary>
    private CancellationTokenSource BeginCurrentLoad()
    {
        var current = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _loadCancellation, current);
        previous?.Cancel();
        return current;
    }

    /// <summary>
    /// Cancels the active Discover load without disposing a token source still observed by service code.
    /// </summary>
    private void CancelCurrentLoad()
    {
        var current = Interlocked.Exchange(ref _loadCancellation, null);
        current?.Cancel();
    }

    /// <summary>
    /// Releases a completed Discover load when it still owns the active load slot.
    /// </summary>
    /// <param name="loadCancellation">Completed load token source.</param>
    private void EndCurrentLoad(CancellationTokenSource loadCancellation)
    {
        if (ReferenceEquals(_loadCancellation, loadCancellation))
        {
            _loadCancellation = null;
        }

        loadCancellation.Dispose();
    }

    /// <summary>
    /// Applies busy state and disables Discover actions for the current load.
    /// </summary>
    private void BeginBusyState()
    {
        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            RaiseCommandStates();
        });
    }

    /// <summary>
    /// Clears busy state and re-enables Discover actions after the current load.
    /// </summary>
    private void EndBusyState()
    {
        RunOnMain(() =>
        {
            IsBusy = false;
            RaiseCommandStates();
        });
    }
}
