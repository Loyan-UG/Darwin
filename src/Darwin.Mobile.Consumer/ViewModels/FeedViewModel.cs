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
            var request = new GetMyLoyaltyTimelinePageRequest
            {
                BusinessId = null,
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
            var request = new GetMyLoyaltyTimelinePageRequest
            {
                BusinessId = null,
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
}