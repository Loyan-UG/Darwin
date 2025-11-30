using System.Collections.ObjectModel;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.ViewModels;
using Darwin.Mobile.Shared.Services;

namespace Darwin.Mobile.Consumer.ViewModels
{
    /// <summary>
    /// View model responsible for showing the consumer's loyalty status for
    /// a specific business, including current points and the list of
    /// available rewards that can be selected for redemption.
    /// </summary>
    public sealed class RewardsViewModel : BaseViewModel
    {
        private readonly ILoyaltyService _loyaltyService;

        private Guid _businessId;
        private LoyaltyAccountSummary? _accountSummary;
        private ObservableCollection<LoyaltyRewardSummary> _availableRewards;
        private ObservableCollection<LoyaltyRewardSummary> _selectedRewards;

        /// <summary>
        /// Initializes a new instance of the <see cref="RewardsViewModel"/> class.
        /// </summary>
        /// <param name="loyaltyService">Service used to load loyalty account and rewards.</param>
        public RewardsViewModel(ILoyaltyService loyaltyService)
        {
            _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));

            _availableRewards = new ObservableCollection<LoyaltyRewardSummary>();
            _selectedRewards = new ObservableCollection<LoyaltyRewardSummary>();

            LoadAsyncCommand = new AsyncCommand(LoadAsync);
            ToggleSelectionCommand = new AsyncCommand<LoyaltyRewardSummary>(ToggleSelectionAsync);
        }

        /// <summary>
        /// Gets or sets the business identifier for which rewards are displayed.
        /// </summary>
        public Guid BusinessId
        {
            get => _businessId;
            set => SetProperty(ref _businessId, value);
        }

        /// <summary>
        /// Gets the summary of the consumer's loyalty account at the current business,
        /// including balance and next reward thresholds.
        /// </summary>
        public LoyaltyAccountSummary? AccountSummary
        {
            get => _accountSummary;
            private set => SetProperty(ref _accountSummary, value);
        }

        /// <summary>
        /// Gets the list of rewards available for this consumer at the current business.
        /// </summary>
        public ObservableCollection<LoyaltyRewardSummary> AvailableRewards
        {
            get => _availableRewards;
            private set => SetProperty(ref _availableRewards, value);
        }

        /// <summary>
        /// Gets the list of rewards currently selected by the consumer for redemption.
        /// </summary>
        public ObservableCollection<LoyaltyRewardSummary> SelectedRewards
        {
            get => _selectedRewards;
            private set => SetProperty(ref _selectedRewards, value);
        }

        /// <summary>
        /// Command that loads the account summary and available rewards from the server.
        /// </summary>
        public AsyncCommand LoadAsyncCommand { get; }

        /// <summary>
        /// Command that toggles selection of a reward for redemption.
        /// </summary>
        public AsyncCommand<LoyaltyRewardSummary> ToggleSelectionCommand { get; }

        /// <summary>
        /// Asynchronously loads the account summary and available rewards for the
        /// current business and updates bound collections.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LoadAsync()
        {
            if (BusinessId == Guid.Empty)
            {
                ErrorMessage = "Business context is not set.";
                return;
            }

            try
            {
                IsBusy = true;
                ErrorMessage = null;

                var accountResult = await _loyaltyService.GetAccountSummaryAsync(BusinessId, CancellationToken.None)
                    .ConfigureAwait(false);

                if (!accountResult.Succeeded)
                {
                    ErrorMessage = accountResult.Error ?? "Failed to load loyalty account.";
                    return;
                }

                AccountSummary = accountResult.Value;

                var rewardsResult = await _loyaltyService.GetAvailableRewardsAsync(BusinessId, CancellationToken.None)
                    .ConfigureAwait(false);

                if (!rewardsResult.Succeeded)
                {
                    ErrorMessage = rewardsResult.Error ?? "Failed to load rewards.";
                    return;
                }

                AvailableRewards.Clear();
                SelectedRewards.Clear();

                foreach (var reward in rewardsResult.Value)
                {
                    AvailableRewards.Add(reward);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An unexpected error occurred while loading rewards.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Task ToggleSelectionAsync(LoyaltyRewardSummary? reward)
        {
            if (reward is null)
            {
                return Task.CompletedTask;
            }

            // NOTE:
            // The property name RewardTierId is used here because the Contracts
            // model is expected to identify reward tiers by an explicit identifier.
            // If your LoyaltyRewardSummary type uses a different name, adjust this code.
            var existing = SelectedRewards.FirstOrDefault(r => r.LoyaltyRewardTierId == reward.LoyaltyRewardTierId);

            if (existing is not null)
            {
                SelectedRewards.Remove(existing);
            }
            else
            {
                // Ensure the consumer has enough points before selecting.
                if (AccountSummary is not null && AccountSummary.PointsBalance>= reward.RequiredPoints)
                {
                    SelectedRewards.Add(reward);
                }
                else
                {
                    // In a real app, you might surface a UI toast instead of a static message.
                    ErrorMessage = "Not enough points for this reward.";
                }
            }

            return Task.CompletedTask;
        }
    }
}
