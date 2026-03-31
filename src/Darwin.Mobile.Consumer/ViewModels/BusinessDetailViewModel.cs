using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Consumer.Services.Caching;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// View model for the Business Detail page.
/// Loads business details, allows joining loyalty, and supports engagement actions
/// (like/favorite/review) for the current member.
/// </summary>
public sealed class BusinessDetailViewModel : BaseViewModel
{
    private readonly IBusinessService _businessService;
    private readonly ILoyaltyService _loyaltyService;
    private readonly IConsumerLoyaltySnapshotCache _loyaltySnapshotCache;
    private readonly INavigationService _navigationService;

    private BusinessDetail? _business;
    private bool _isLikedByMe;
    private bool _isFavoritedByMe;
    private int _likeCount;
    private int _favoriteCount;
    private int _ratingCount;
    private decimal? _ratingAverage;
    private int _myRating = 5;
    private string? _myReviewComment;

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessDetailViewModel"/> class.
    /// </summary>
    public BusinessDetailViewModel(
        IBusinessService businessService,
        ILoyaltyService loyaltyService,
        IConsumerLoyaltySnapshotCache loyaltySnapshotCache,
        INavigationService navigationService)
    {
        _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _loyaltySnapshotCache = loyaltySnapshotCache ?? throw new ArgumentNullException(nameof(loyaltySnapshotCache));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        Reviews = new ObservableCollection<BusinessReviewItem>();

        JoinCommand = new AsyncRelayCommand(JoinAsync, () => !IsBusy && BusinessId != Guid.Empty);
        ToggleLikeCommand = new AsyncRelayCommand(ToggleLikeAsync, () => !IsBusy && BusinessId != Guid.Empty);
        ToggleFavoriteCommand = new AsyncRelayCommand(ToggleFavoriteAsync, () => !IsBusy && BusinessId != Guid.Empty);
        SaveReviewCommand = new AsyncRelayCommand(SaveReviewAsync, () => !IsBusy && BusinessId != Guid.Empty);
    }

    /// <summary>
    /// The ID of the business being viewed.
    /// </summary>
    public Guid BusinessId { get; private set; }

    /// <summary>
    /// Loaded public business details.
    /// </summary>
    public BusinessDetail? Business
    {
        get => _business;
        private set => SetProperty(ref _business, value);
    }

    /// <summary>
    /// Recent public reviews (up to 5 records) returned by engagement endpoint.
    /// </summary>
    public ObservableCollection<BusinessReviewItem> Reviews { get; }

    public bool IsLikedByMe
    {
        get => _isLikedByMe;
        private set => SetProperty(ref _isLikedByMe, value);
    }

    public bool IsFavoritedByMe
    {
        get => _isFavoritedByMe;
        private set => SetProperty(ref _isFavoritedByMe, value);
    }

    public int LikeCount
    {
        get => _likeCount;
        private set
        {
            if (SetProperty(ref _likeCount, value))
            {
                OnPropertyChanged(nameof(LikeCountText));
            }
        }
    }

    public int FavoriteCount
    {
        get => _favoriteCount;
        private set
        {
            if (SetProperty(ref _favoriteCount, value))
            {
                OnPropertyChanged(nameof(FavoriteCountText));
            }
        }
    }

    public int RatingCount
    {
        get => _ratingCount;
        private set
        {
            if (SetProperty(ref _ratingCount, value))
            {
                OnPropertyChanged(nameof(RatingCountText));
            }
        }
    }

    public decimal? RatingAverage
    {
        get => _ratingAverage;
        private set
        {
            if (SetProperty(ref _ratingAverage, value))
            {
                OnPropertyChanged(nameof(RatingAverageText));
                OnPropertyChanged(nameof(RatingAverageDisplayText));
            }
        }
    }

    /// <summary>
    /// Preformatted rating value used directly in XAML labels.
    /// </summary>
    public string RatingAverageText => RatingAverage.HasValue ? $"{RatingAverage.Value:0.0}" : "-";

    /// <summary>
    /// Localized label for likes count.
    /// </summary>
    public string LikeCountText => string.Format(Resources.AppResources.BusinessLikesCountFormat, LikeCount);

    /// <summary>
    /// Localized label for favorites count.
    /// </summary>
    public string FavoriteCountText => string.Format(Resources.AppResources.BusinessFavoritesCountFormat, FavoriteCount);

    /// <summary>
    /// Localized label for rating average.
    /// </summary>
    public string RatingAverageDisplayText => string.Format(Resources.AppResources.BusinessRatingFormat, RatingAverageText);

    /// <summary>
    /// Localized label for review count.
    /// </summary>
    public string RatingCountText => string.Format(Resources.AppResources.BusinessReviewsCountFormat, RatingCount);

    /// <summary>
    /// Localized label for current member rating input.
    /// </summary>
    public string MyRatingText => string.Format(Resources.AppResources.BusinessCurrentRatingFormat, MyRating);

    /// <summary>
    /// Bound rating input for the current member review (1..5).
    /// </summary>
    public int MyRating
    {
        get => _myRating;
        set
        {
            var safe = value;
            if (safe < 1) safe = 1;
            if (safe > 5) safe = 5;
            if (SetProperty(ref _myRating, safe))
            {
                OnPropertyChanged(nameof(MyRatingText));
            }
        }
    }

    /// <summary>
    /// Bound free-text comment for current member review.
    /// </summary>
    public string? MyReviewComment
    {
        get => _myReviewComment;
        set => SetProperty(ref _myReviewComment, value);
    }

    public bool HasReviews => Reviews.Count > 0;

    public IAsyncRelayCommand JoinCommand { get; }
    public IAsyncRelayCommand ToggleLikeCommand { get; }
    public IAsyncRelayCommand ToggleFavoriteCommand { get; }
    public IAsyncRelayCommand SaveReviewCommand { get; }

    /// <summary>
    /// Loads business details and engagement snapshot on first appearance.
    /// </summary>
    public override async Task OnAppearingAsync()
    {
        if (BusinessId == Guid.Empty || Business != null)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        RaiseCanExecute();

        try
        {
            Business = await _businessService.GetAsync(BusinessId, CancellationToken.None);

            if (Business == null)
            {
                ErrorMessage = Resources.AppResources.BusinessDetailsNotFound;
                return;
            }

            await LoadEngagementAsync();
        }
        catch (Exception)
        {
            ErrorMessage = Resources.AppResources.BusinessDetailsLoadFailed;
        }
        finally
        {
            IsBusy = false;
            RaiseCanExecute();
        }
    }

    /// <summary>
    /// Sets the business context for the view model.
    /// </summary>
    public void SetBusiness(Guid businessId)
    {
        BusinessId = businessId;
    }

    /// <summary>
    /// Loads business engagement snapshot for the current member:
    /// like/favorite state, counters, my review, and recent reviews.
    /// </summary>
    private async Task LoadEngagementAsync()
    {
        var result = await _businessService.GetMyEngagementAsync(BusinessId, CancellationToken.None);

        if (!result.Succeeded || result.Value is null)
        {
            ErrorMessage = result.Error ?? Resources.AppResources.BusinessEngagementLoadFailed;
            return;
        }

        var dto = result.Value;

        RunOnMain(() =>
        {
            IsLikedByMe = dto.IsLikedByMe;
            IsFavoritedByMe = dto.IsFavoritedByMe;
            LikeCount = dto.LikeCount;
            FavoriteCount = dto.FavoriteCount;
            RatingCount = dto.RatingCount;
            RatingAverage = dto.RatingAverage;

            Reviews.Clear();
            foreach (var review in dto.RecentReviews)
            {
                Reviews.Add(review);
            }

            OnPropertyChanged(nameof(HasReviews));

            if (dto.MyReview is not null)
            {
                MyRating = dto.MyReview.Rating;
                MyReviewComment = dto.MyReview.Comment;
            }
        });
    }

    /// <summary>
    /// Toggles current member's like state for this business and updates local counter/state.
    /// </summary>
    private async Task ToggleLikeAsync()
    {
        if (BusinessId == Guid.Empty || IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        RaiseCanExecute();

        try
        {
            var result = await _businessService.ToggleLikeAsync(BusinessId, CancellationToken.None);
            if (!result.Succeeded || result.Value is null)
            {
                ErrorMessage = result.Error ?? Resources.AppResources.BusinessLikeToggleFailed;
                return;
            }

            RunOnMain(() =>
            {
                IsLikedByMe = result.Value.IsActive;
                LikeCount = result.Value.TotalCount;
            });
        }
        finally
        {
            IsBusy = false;
            RaiseCanExecute();
        }
    }

    /// <summary>
    /// Toggles current member's favorite state for this business and updates local counter/state.
    /// </summary>
    private async Task ToggleFavoriteAsync()
    {
        if (BusinessId == Guid.Empty || IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        RaiseCanExecute();

        try
        {
            var result = await _businessService.ToggleFavoriteAsync(BusinessId, CancellationToken.None);
            if (!result.Succeeded || result.Value is null)
            {
                ErrorMessage = result.Error ?? Resources.AppResources.BusinessFavoriteToggleFailed;
                return;
            }

            RunOnMain(() =>
            {
                IsFavoritedByMe = result.Value.IsActive;
                FavoriteCount = result.Value.TotalCount;
            });
        }
        finally
        {
            IsBusy = false;
            RaiseCanExecute();
        }
    }

    /// <summary>
    /// Creates or updates current member review and then reloads engagement snapshot
    /// to keep counters, my review data, and recent reviews consistent.
    /// </summary>
    private async Task SaveReviewAsync()
    {
        if (BusinessId == Guid.Empty || IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        RaiseCanExecute();

        try
        {
            var request = new UpsertBusinessReviewRequest
            {
                Rating = (byte)MyRating,
                Comment = string.IsNullOrWhiteSpace(MyReviewComment) ? null : MyReviewComment.Trim()
            };

            var result = await _businessService.UpsertMyReviewAsync(BusinessId, request, CancellationToken.None);
            if (!result.Succeeded)
            {
                ErrorMessage = result.Error ?? Resources.AppResources.BusinessReviewSaveFailed;
                return;
            }

            await LoadEngagementAsync();
        }
        finally
        {
            IsBusy = false;
            RaiseCanExecute();
        }
    }

    /// <summary>
    /// Joins loyalty for the selected business, prepares an accrual scan session,
    /// and navigates to QR tab with explicit business context.
    /// </summary>
    private async Task JoinAsync()
    {
        if (BusinessId == Guid.Empty || IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        RaiseCanExecute();

        try
        {
            var joinResult = await _loyaltyService.JoinLoyaltyAsync(BusinessId, null, CancellationToken.None);

            if (!joinResult.Succeeded || joinResult.Value == null)
            {
                ErrorMessage = joinResult.Error ?? Resources.AppResources.BusinessJoinFailed;
                return;
            }

            await _loyaltySnapshotCache.InvalidateAsync(CancellationToken.None);

            var sessionResult = await _loyaltyService.PrepareScanSessionAsync(
                BusinessId,
                LoyaltyScanMode.Accrual,
                selectedRewardIds: null,
                CancellationToken.None);

            if (!sessionResult.Succeeded || sessionResult.Value == null)
            {
                ErrorMessage = sessionResult.Error ?? Resources.AppResources.BusinessScanSessionPrepareFailed;
                return;
            }

            var parameters = new Dictionary<string, object?>
            {
                ["businessId"] = BusinessId,
                ["businessName"] = Business?.Name,
                ["joined"] = true
            };

            await _navigationService.GoToAsync($"//{Routes.Qr}", parameters);
        }
        finally
        {
            IsBusy = false;
            RaiseCanExecute();
        }
    }

    /// <summary>
    /// Raises CanExecuteChanged for all commands to keep UI buttons in sync with busy/business state.
    /// </summary>
    private void RaiseCanExecute()
    {
        JoinCommand.NotifyCanExecuteChanged();
        ToggleLikeCommand.NotifyCanExecuteChanged();
        ToggleFavoriteCommand.NotifyCanExecuteChanged();
        SaveReviewCommand.NotifyCanExecuteChanged();
    }
}
