using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.ViewModels;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Consumer.ViewModels
{
    /// <summary>
    /// View model for the rewards dashboard in the consumer app.
    /// Responsible for loading the current balance and available rewards
    /// for a specific business.
    /// </summary>
    public sealed class RewardsViewModel : BaseViewModel
    {
        private readonly ILoyaltyService _loyaltyService;

        private Guid _businessId;
        private int _currentPoints;
        private bool _hasLoaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="RewardsViewModel"/> class.
        /// </summary>
        /// <param name="loyaltyService">The loyalty service abstraction.</param>
        public RewardsViewModel(ILoyaltyService loyaltyService)
        {
            _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));

            Rewards = new ObservableCollection<LoyaltyRewardSummary>();
            RefreshCommand = new AsyncCommand(RefreshAsync);
        }

        /// <summary>
        /// Gets the business identifier for which this rewards dashboard applies.
        /// </summary>
        public Guid BusinessId
        {
            get => _businessId;
            private set => SetProperty(ref _businessId, value);
        }

        /// <summary>
        /// Gets the current points balance for the consumer at this business.
        /// </summary>
        public int CurrentPoints
        {
            get => _currentPoints;
            private set => SetProperty(ref _currentPoints, value);
        }

        /// <summary>
        /// Gets the collection of rewards that are available for this business.
        /// </summary>
        public ObservableCollection<LoyaltyRewardSummary> Rewards { get; }

        /// <summary>
        /// Command that triggers a refresh of the account summary and rewards list.
        /// </summary>
        public AsyncCommand RefreshCommand { get; }

        /// <summary>
        /// Sets the business context for this view model.
        /// </summary>
        /// <param name="businessId">The business identifier.</param>
        public void SetBusiness(Guid businessId)
        {
            if (businessId == Guid.Empty)
            {
                throw new ArgumentException("Business id must not be empty.", nameof(businessId));
            }

            BusinessId = businessId;
        }

        /// <inheritdoc />
        public override async Task OnAppearingAsync()
        {
            if (!_hasLoaded && BusinessId != Guid.Empty)
            {
                await RefreshAsync().ConfigureAwait(false);
                _hasLoaded = true;
            }
        }

        private async Task RefreshAsync()
        {
            if (BusinessId == Guid.Empty)
            {
                ErrorMessage = "Business context is not set.";
                return;
            }

            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var accountResult = await _loyaltyService
                    .GetAccountSummaryAsync(BusinessId, CancellationToken.None)
                    .ConfigureAwait(false);

                if (!accountResult.Succeeded || accountResult.Value is null)
                {
                    ErrorMessage = accountResult.Error ?? "Failed to load loyalty account.";
                    return;
                }

                var account = accountResult.Value;
                CurrentPoints = account.PointsBalance;

                var rewardsResult = await _loyaltyService
                    .GetAvailableRewardsAsync(BusinessId, CancellationToken.None)
                    .ConfigureAwait(false);

                if (!rewardsResult.Succeeded || rewardsResult.Value is null)
                {
                    ErrorMessage = rewardsResult.Error ?? "Failed to load rewards.";
                    return;
                }

                Rewards.Clear();
                foreach (var reward in rewardsResult.Value.OrderBy(r => r.RequiredPoints))
                {
                    Rewards.Add(reward);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
