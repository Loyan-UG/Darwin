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
using Microsoft.Maui.Storage;

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
    private const string PromotionSeenAtStoragePrefix = "consumer.feed.promotion.seen-at.v1";
    private static readonly TimeSpan PromotionDisplaySuppressionWindow = TimeSpan.FromHours(8);
    private const int PromotionDisplayCap = 6;

    private readonly ILoyaltyService _loyaltyService;
    private readonly INavigationService _navigationService;

    private bool _hasLoaded;
    private bool _isLoadingMore;
    private DateTime? _nextBeforeAtUtc;
    private Guid? _nextBeforeId;
    private LoyaltyAccountSummary? _selectedAccount;
    private bool _suppressSelectionRefresh;
    private bool _isPromotionScopeAllBusinesses;
    private int _promotionSuppressedByFrequencyCount;
    private int _promotionDeduplicatedCount;
    private int _promotionTrimmedByCapCount;
    private int _promotionInitialCandidateCount;
    private int _promotionFinalCount;
    private int _promotionAppliedMaxCards;
    private int _promotionAppliedSuppressionMinutes;

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
        ShowSelectedBusinessPromotionsCommand = new AsyncCommand(ShowSelectedBusinessPromotionsAsync, () => !IsBusy);
        ShowAllBusinessesPromotionsCommand = new AsyncCommand(ShowAllBusinessesPromotionsAsync, () => !IsBusy);
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
    /// Gets the number of initial promotion candidates before server guardrails.
    /// </summary>
    public int PromotionInitialCandidateCount => _promotionInitialCandidateCount;

    /// <summary>
    /// Gets the number of items suppressed by server-side frequency policy.
    /// </summary>
    public int PromotionSuppressedByFrequencyCount => _promotionSuppressedByFrequencyCount;

    /// <summary>
    /// Gets the number of items removed by de-duplication policy.
    /// </summary>
    public int PromotionDeduplicatedCount => _promotionDeduplicatedCount;

    /// <summary>
    /// Gets the number of items removed by max-card cap policy.
    /// </summary>
    public int PromotionTrimmedByCapCount => _promotionTrimmedByCapCount;

    /// <summary>
    /// Gets the final promotion count produced by server for current request.
    /// </summary>
    public int PromotionFinalCount => _promotionFinalCount;

    /// <summary>
    /// Gets effective max-card cap applied by server policy.
    /// </summary>
    public int PromotionAppliedMaxCards => _promotionAppliedMaxCards;

    /// <summary>
    /// Gets effective suppression window minutes resolved from server policy.
    /// </summary>
    public int PromotionAppliedSuppressionMinutes => _promotionAppliedSuppressionMinutes;

    /// <summary>
    /// Gets whether server diagnostics contain at least one non-zero guardrail signal.
    /// </summary>
    public bool HasPromotionDiagnostics
        => PromotionInitialCandidateCount > 0
            || PromotionSuppressedByFrequencyCount > 0
            || PromotionDeduplicatedCount > 0
            || PromotionTrimmedByCapCount > 0;

    /// <summary>
    /// Gets whether promotion cards are loaded across all joined businesses.
    /// </summary>
    public bool IsPromotionScopeAllBusinesses
    {
        get => _isPromotionScopeAllBusinesses;
        private set
        {
            if (SetProperty(ref _isPromotionScopeAllBusinesses, value))
            {
                OnPropertyChanged(nameof(IsPromotionScopeSelectedBusiness));
                OnPropertyChanged(nameof(PromotionScopeText));
            }
        }
    }

    /// <summary>
    /// Gets whether promotion cards are scoped to selected business only.
    /// </summary>
    public bool IsPromotionScopeSelectedBusiness => !IsPromotionScopeAllBusinesses;

    /// <summary>
    /// Gets localized promotion scope descriptor for the current mode.
    /// </summary>
    public string PromotionScopeText
        => IsPromotionScopeAllBusinesses
            ? Resources.AppResources.FeedPromotionScopeAllBusinesses
            : Resources.AppResources.FeedPromotionScopeSelectedBusiness;

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

    public AsyncCommand ShowSelectedBusinessPromotionsCommand { get; }

    public AsyncCommand ShowAllBusinessesPromotionsCommand { get; }

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
    private async Task LoadPromotionsAsync(Guid selectedBusinessId)
    {
        var promotionBusinessId = IsPromotionScopeAllBusinesses ? (Guid?)null : selectedBusinessId;

        var result = await _loyaltyService.GetMyPromotionsAsync(new MyPromotionsRequest
        {
            BusinessId = promotionBusinessId,
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

        var unique = ordered
            .GroupBy(BuildPromotionGuardKey, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

        var nowUtc = DateTime.UtcNow;
        var suppressionWindow = ResolvePromotionSuppressionWindow(result.Value.AppliedPolicy);
        var displayCap = ResolvePromotionDisplayCap(result.Value.AppliedPolicy);

        _promotionInitialCandidateCount = result.Value.Diagnostics.InitialCandidates;
        _promotionSuppressedByFrequencyCount = result.Value.Diagnostics.SuppressedByFrequency;
        _promotionDeduplicatedCount = result.Value.Diagnostics.Deduplicated;
        _promotionTrimmedByCapCount = result.Value.Diagnostics.TrimmedByCap;
        _promotionFinalCount = result.Value.Diagnostics.FinalCount;
        _promotionAppliedMaxCards = displayCap;
        _promotionAppliedSuppressionMinutes = (int)Math.Round(suppressionWindow.TotalMinutes, MidpointRounding.AwayFromZero);

        var eligible = unique
            .Where(item => !IsPromotionSuppressed(item, nowUtc, suppressionWindow))
            .Take(displayCap)
            .ToList();

        if (eligible.Count == 0)
        {
            eligible = unique.Take(displayCap).ToList();
        }

        RunOnMain(() =>
        {
            foreach (var item in eligible)
            {
                PromotionItems.Add(item);
            }

            OnPropertyChanged(nameof(HasPromotions));
            OnPropertyChanged(nameof(PromotionInitialCandidateCount));
            OnPropertyChanged(nameof(PromotionSuppressedByFrequencyCount));
            OnPropertyChanged(nameof(PromotionDeduplicatedCount));
            OnPropertyChanged(nameof(PromotionTrimmedByCapCount));
            OnPropertyChanged(nameof(PromotionFinalCount));
            OnPropertyChanged(nameof(PromotionAppliedMaxCards));
            OnPropertyChanged(nameof(PromotionAppliedSuppressionMinutes));
            OnPropertyChanged(nameof(HasPromotionDiagnostics));
        });

        foreach (var item in eligible)
        {
            MarkPromotionAsSeen(item, nowUtc);
        }

        _ = TrackPromotionImpressionsBestEffortAsync(eligible);
    }

    /// <summary>
    /// Builds a deterministic key used for promotion de-duplication and suppression tracking.
    /// </summary>
    private static string BuildPromotionGuardKey(PromotionFeedItem item)
    {
        return string.Join("|",
            item.BusinessId,
            item.Title?.Trim() ?? string.Empty,
            item.CtaKind?.Trim() ?? string.Empty);
    }

    /// <summary>
    /// Resolves effective client-side suppression window from server-applied policy.
    /// </summary>
    private static TimeSpan ResolvePromotionSuppressionWindow(PromotionFeedPolicy? policy)
    {
        var minutes = policy?.FrequencyWindowMinutes ?? policy?.SuppressionWindowMinutes;
        if (!minutes.HasValue || minutes.Value <= 0)
        {
            return PromotionDisplaySuppressionWindow;
        }

        return TimeSpan.FromMinutes(minutes.Value);
    }

    /// <summary>
    /// Resolves effective display-cap from server-applied policy while keeping safe client fallback.
    /// </summary>
    private static int ResolvePromotionDisplayCap(PromotionFeedPolicy? policy)
    {
        if (policy is null || policy.MaxCards <= 0)
        {
            return PromotionDisplayCap;
        }

        return policy.MaxCards;
    }

    /// <summary>
    /// Checks whether the promotion is still inside the suppression window.
    /// </summary>
    private static bool IsPromotionSuppressed(PromotionFeedItem item, DateTime nowUtc, TimeSpan suppressionWindow)
    {
        var storageKey = BuildPromotionSeenAtStorageKey(item);
        var seenAtTicks = Preferences.Default.Get(storageKey, 0L);
        if (seenAtTicks <= 0)
        {
            return false;
        }

        var seenAtUtc = new DateTime(seenAtTicks, DateTimeKind.Utc);
        var elapsed = nowUtc - seenAtUtc;
        return elapsed >= TimeSpan.Zero && elapsed < suppressionWindow;
    }

    /// <summary>
    /// Persists the latest display timestamp for suppression enforcement.
    /// </summary>
    private static void MarkPromotionAsSeen(PromotionFeedItem item, DateTime seenAtUtc)
    {
        var storageKey = BuildPromotionSeenAtStorageKey(item);
        Preferences.Default.Set(storageKey, seenAtUtc.Ticks);
    }

    /// <summary>
    /// Builds a stable preferences key for a promotion card.
    /// </summary>
    private static string BuildPromotionSeenAtStorageKey(PromotionFeedItem item)
        => $"{PromotionSeenAtStoragePrefix}:{BuildPromotionGuardKey(item)}";

    /// <summary>
    /// Switches promotions scope to selected business and reloads promotion cards.
    /// </summary>
    private async Task ShowSelectedBusinessPromotionsAsync()
    {
        if (IsPromotionScopeSelectedBusiness)
        {
            return;
        }

        IsPromotionScopeAllBusinesses = false;

        if (SelectedAccount is null || SelectedAccount.BusinessId == Guid.Empty)
        {
            return;
        }

        await RefreshPromotionsOnlyAsync(SelectedAccount.BusinessId);
    }

    /// <summary>
    /// Switches promotions scope to all joined businesses and reloads promotion cards.
    /// </summary>
    private async Task ShowAllBusinessesPromotionsAsync()
    {
        if (IsPromotionScopeAllBusinesses)
        {
            return;
        }

        IsPromotionScopeAllBusinesses = true;

        if (SelectedAccount is null || SelectedAccount.BusinessId == Guid.Empty)
        {
            return;
        }

        await RefreshPromotionsOnlyAsync(SelectedAccount.BusinessId);
    }

    /// <summary>
    /// Reloads only promotion cards while preserving timeline state.
    /// </summary>
    private async Task RefreshPromotionsOnlyAsync(Guid selectedBusinessId)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        RaiseCommandsCanExecute();

        try
        {
            await LoadPromotionsAsync(selectedBusinessId);
        }
        finally
        {
            IsBusy = false;
            RaiseCommandsCanExecute();
        }
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

        _ = TrackPromotionInteractionBestEffortAsync(item, PromotionInteractionEventType.Open);

        if (string.Equals(item.CtaKind, "OpenQr", StringComparison.OrdinalIgnoreCase))
        {
            await _navigationService.GoToAsync($"//{Routes.Qr}", parameters);
            return;
        }

        await _navigationService.GoToAsync($"//{Routes.Rewards}", parameters);
    }

    /// <summary>
    /// Sends impression events for visible promotion cards (best effort, non-blocking).
    /// </summary>
    private async Task TrackPromotionImpressionsBestEffortAsync(IReadOnlyCollection<PromotionFeedItem> items)
    {
        foreach (var item in items)
        {
            await TrackPromotionInteractionBestEffortAsync(item, PromotionInteractionEventType.Impression)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Sends one promotion interaction event to backend analytics endpoint.
    /// Failures are intentionally ignored to avoid affecting user flow.
    /// </summary>
    private async Task TrackPromotionInteractionBestEffortAsync(PromotionFeedItem item, PromotionInteractionEventType eventType)
    {
        try
        {
            if (item.BusinessId == Guid.Empty || string.IsNullOrWhiteSpace(item.Title))
            {
                return;
            }

            await _loyaltyService.TrackPromotionInteractionAsync(new TrackPromotionInteractionRequest
            {
                BusinessId = item.BusinessId,
                BusinessName = item.BusinessName,
                Title = item.Title,
                CtaKind = item.CtaKind,
                EventType = eventType,
                OccurredAtUtc = DateTime.UtcNow
            }, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Intentionally swallow tracking errors.
        }
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
        _promotionInitialCandidateCount = 0;
        _promotionSuppressedByFrequencyCount = 0;
        _promotionDeduplicatedCount = 0;
        _promotionTrimmedByCapCount = 0;
        _promotionFinalCount = 0;
        _promotionAppliedMaxCards = 0;
        _promotionAppliedSuppressionMinutes = 0;

        RunOnMain(() =>
        {
            PromotionItems.Clear();
            OnPropertyChanged(nameof(HasPromotions));
            OnPropertyChanged(nameof(PromotionInitialCandidateCount));
            OnPropertyChanged(nameof(PromotionSuppressedByFrequencyCount));
            OnPropertyChanged(nameof(PromotionDeduplicatedCount));
            OnPropertyChanged(nameof(PromotionTrimmedByCapCount));
            OnPropertyChanged(nameof(PromotionFinalCount));
            OnPropertyChanged(nameof(PromotionAppliedMaxCards));
            OnPropertyChanged(nameof(PromotionAppliedSuppressionMinutes));
            OnPropertyChanged(nameof(HasPromotionDiagnostics));
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
        ShowSelectedBusinessPromotionsCommand.RaiseCanExecuteChanged();
        ShowAllBusinessesPromotionsCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(CanNavigateWithSelection));
        OnPropertyChanged(nameof(PromotionScopeText));
    }
}
