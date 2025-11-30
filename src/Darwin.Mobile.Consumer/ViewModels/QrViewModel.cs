using System.Collections.ObjectModel;
using System.Windows.Input;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.ViewModels;
using Darwin.Mobile.Shared.Services;

namespace Darwin.Mobile.Consumer.ViewModels
{
    /// <summary>
    /// View model responsible for rendering the consumer QR code for loyalty scans
    /// and managing the current scan session mode (accrual vs. redemption) together
    /// with the list of selected rewards for redemption.
    /// </summary>
    public sealed class QrViewModel : BaseViewModel
    {
        private readonly ILoyaltyService _loyaltyService;
        private readonly INavigationService _navigationService;

        private Guid _businessId;
        private string? _currentToken;
        private DateTimeOffset? _expiresAtUtc;
        private bool _isRedemptionMode;
        private ObservableCollection<LoyaltyRewardSummary> _selectedRewards;

        /// <summary>
        /// Initializes a new instance of the <see cref="QrViewModel"/> class.
        /// </summary>
        /// <param name="loyaltyService">Service used to prepare loyalty scan sessions.</param>
        /// <param name="navigationService">Navigation abstraction for routing from the QR screen.</param>
        public QrViewModel(
            ILoyaltyService loyaltyService,
            INavigationService navigationService)
        {
            _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            _selectedRewards = new ObservableCollection<LoyaltyRewardSummary>();

            RefreshQrCommand = new AsyncCommand(RefreshQrAsync);
            SwitchToAccrualModeCommand = new AsyncCommand(SwitchToAccrualModeAsync);
            SwitchToRedemptionModeCommand = new AsyncCommand(SwitchToRedemptionModeAsync);
        }

        /// <summary>
        /// Gets or sets the current business identifier for which the QR is being generated.
        /// The consumer app will typically bind this to the currently selected business context.
        /// </summary>
        public Guid BusinessId
        {
            get => _businessId;
            set => SetProperty(ref _businessId, value);
        }

        /// <summary>
        /// Gets the opaque token string that should be rendered as a QR code.
        /// </summary>
        public string? CurrentToken
        {
            get => _currentToken;
            private set => SetProperty(ref _currentToken, value);
        }

        /// <summary>
        /// Gets the UTC expiry timestamp of the current scan session token, if available.
        /// UI can use this to display a countdown or trigger a refresh.
        /// </summary>
        public DateTimeOffset? ExpiresAtUtc
        {
            get => _expiresAtUtc;
            private set => SetProperty(ref _expiresAtUtc, value);
        }

        /// <summary>
        /// Gets a value indicating whether the current scan session represents
        /// a redemption of rewards instead of a simple accrual of points.
        /// </summary>
        public bool IsRedemptionMode
        {
            get => _isRedemptionMode;
            private set => SetProperty(ref _isRedemptionMode, value);
        }

        /// <summary>
        /// Gets the collection of rewards that the consumer has selected
        /// for redemption in the current scan session.
        /// </summary>
        public ObservableCollection<LoyaltyRewardSummary> SelectedRewards
        {
            get => _selectedRewards;
            private set => SetProperty(ref _selectedRewards, value);
        }

        /// <summary>
        /// Command to force-refresh the QR token by preparing a new scan session
        /// in the current mode (accrual or redemption).
        /// </summary>
        public AsyncCommand RefreshQrCommand { get; }

        /// <summary>
        /// Command to switch the current QR into accrual mode (no reward selection).
        /// </summary>
        public AsyncCommand SwitchToAccrualModeCommand { get; }

        /// <summary>
        /// Command to switch the current QR into redemption mode using the
        /// currently selected rewards from the rewards screen.
        /// </summary>
        public AsyncCommand SwitchToRedemptionModeCommand { get; }

        /// <summary>
        /// Initializes the QR state for a given business. This method should be
        /// called by the hosting page when the business context becomes available.
        /// </summary>
        /// <param name="businessId">Identifier of the business for which to prepare scan sessions.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InitializeAsync(Guid businessId)
        {
            BusinessId = businessId;

            // Default to accrual mode on first load.
            IsRedemptionMode = false;
            SelectedRewards.Clear();

            await RefreshQrAsync().ConfigureAwait(false);
        }

        private async Task RefreshQrAsync()
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

                var mode = IsRedemptionMode ? LoyaltyScanMode.Redemption : LoyaltyScanMode.Accrual;

                var request = new PrepareScanSessionRequest
                {
                    BusinessId = BusinessId,
                    Mode = mode,
                    // IMPORTANT:
                    // In Darwin.Contracts the property name may be RewardTierIds or similar.
                    // If PrepareScanSessionRequest uses a different name, adjust this assignment.
                    SelectedRewardTierIds = IsRedemptionMode
                        ? SelectedRewards.Select(r => r.LoyaltyRewardTierId).ToArray()
                        : Array.Empty<Guid>()
                };

                var response = await _loyaltyService.PrepareScanSessionAsync(request, CancellationToken.None)
                    .ConfigureAwait(false);

                if (!response.Succeeded)
                {
                    ErrorMessage = response.Error ?? "Failed to prepare scan session.";
                    return;
                }

                var payload = response.Value;

                // NOTE: Property names Token / ExpiresAtUtc are chosen to align with
                // the existing QrCodePayload pattern in DarwinMobile.md.
                CurrentToken = payload.Token;
                ExpiresAtUtc = payload.ExpiresAtUtc;
            }
            catch (Exception ex)
            {
                // Map to a simple user-facing message; diagnostics should be logged in the app host.
                ErrorMessage = "An unexpected error occurred while preparing the QR.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SwitchToAccrualModeAsync()
        {
            if (IsRedemptionMode)
            {
                IsRedemptionMode = false;
                SelectedRewards.Clear();
                await RefreshQrAsync().ConfigureAwait(false);
            }
        }

        private async Task SwitchToRedemptionModeAsync()
        {
            if (!IsRedemptionMode)
            {
                IsRedemptionMode = true;
                // SelectedRewards should already be filled by RewardsViewModel via navigation/context.
                await RefreshQrAsync().ConfigureAwait(false);
            }
        }
    }
}
