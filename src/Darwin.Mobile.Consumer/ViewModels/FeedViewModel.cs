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
/// View model for the Consumer feed/promotions tab.
/// </summary>
/// <remarks>
/// The initial Feed implementation is backed by the loyalty timeline endpoint.
/// It provides a stable, paged stream of customer-facing loyalty events and can
/// later be extended with dedicated promotions payloads without breaking the tab UX.
/// </remarks>
public sealed class FeedViewModel : BaseViewModel
{
    private readonly ILoyaltyService _loyaltyService;

    private bool _hasLoaded;
    private bool _isLoadingMore;
    private DateTime? _nextBeforeAtUtc;
    private Guid? _nextBeforeId;
    private Guid _activeBusinessId;

    public FeedViewModel(ILoyaltyService loyaltyService)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));

        Items = new ObservableCollection<LoyaltyTimelineEntry>();
        RefreshCommand = new AsyncCommand(RefreshAsync);
        LoadMoreCommand = new AsyncCommand(LoadMoreAsync, () => HasMore && !_isLoadingMore && !IsBusy);
    }

    /// <summary>
    /// Timeline-backed feed items shown in the UI.
    /// </summary>
    public ObservableCollection<LoyaltyTimelineEntry> Items { get; }

    /// <summary>
    /// True when more server pages are available.
    /// </summary>
    public bool HasMore => _nextBeforeAtUtc.HasValue && _nextBeforeId.HasValue;

    /// <summary>
    /// True when no feed records exist.
    /// </summary>
    public bool HasItems => Items.Count > 0;

    public AsyncCommand RefreshCommand { get; }

    public AsyncCommand LoadMoreCommand { get; }

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

        try
        {
            if (!await EnsureActiveBusinessAsync())
            {
                return;
            }

            var request = new GetMyLoyaltyTimelinePageRequest
            {
                BusinessId = _activeBusinessId,
                PageSize = 20,
                BeforeAtUtc = null,
                BeforeId = null
            };

            var result = await _loyaltyService.GetMyLoyaltyTimelinePageAsync(request, CancellationToken.None);

            if (!result.Succeeded || result.Value is null)
            {
                ErrorMessage = Resources.AppResources.FeedLoadFailed;
                RunOnMain(() =>
                {
                    Items.Clear();
                    OnPropertyChanged(nameof(HasItems));
                });

                _nextBeforeAtUtc = null;
                _nextBeforeId = null;
                LoadMoreCommand.RaiseCanExecuteChanged();
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
            LoadMoreCommand.RaiseCanExecuteChanged();
        }
        finally
        {
            IsBusy = false;
            LoadMoreCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task LoadMoreAsync()
    {
        if (_isLoadingMore || IsBusy || !HasMore)
        {
            return;
        }

        _isLoadingMore = true;
        ErrorMessage = null;
        LoadMoreCommand.RaiseCanExecuteChanged();

        try
        {
            if (_activeBusinessId == Guid.Empty && !await EnsureActiveBusinessAsync())
            {
                return;
            }

            var request = new GetMyLoyaltyTimelinePageRequest
            {
                BusinessId = _activeBusinessId,
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
    /// Resolves a deterministic business context for the feed by selecting the first joined account.
    /// The timeline endpoint is business-scoped, therefore sending a null/empty business id always fails.
    /// </summary>
    /// <returns>
    /// True when a valid business id was resolved; otherwise false and the view model shows a user-safe message.
    /// </returns>
    private async Task<bool> EnsureActiveBusinessAsync()
    {
        if (_activeBusinessId != Guid.Empty)
        {
            return true;
        }

        var accountsResult = await _loyaltyService.GetMyAccountsAsync(CancellationToken.None);
        if (!accountsResult.Succeeded || accountsResult.Value is null)
        {
            ErrorMessage = Resources.AppResources.FeedLoadFailed;
            return false;
        }

        var account = accountsResult.Value.FirstOrDefault();
        if (account is null)
        {
            ErrorMessage = Resources.AppResources.FeedNoAccountsMessage;
            RunOnMain(() =>
            {
                Items.Clear();
                OnPropertyChanged(nameof(HasItems));
            });

            _nextBeforeAtUtc = null;
            _nextBeforeId = null;
            OnPropertyChanged(nameof(HasMore));
            return false;
        }

        _activeBusinessId = account.BusinessId;
        return true;
    }
}