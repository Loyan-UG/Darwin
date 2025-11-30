using System.Collections.ObjectModel;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Integration;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Business.ViewModels
{
    /// <summary>
    /// View model responsible for the QR scan flow in the business app.
    /// It drives the scanner, calls the loyalty service to process the
    /// scan session, and exposes commands to confirm accrual and redemption.
    /// </summary>
    public sealed class ScannerViewModel : BaseViewModel
    {
        private readonly ILoyaltyService _loyaltyService;
        private readonly IScanner _scanner;

        private Guid _currentBusinessId;
        private Guid? _currentScanSessionId;
        private LoyaltyScanMode? _currentMode;
        private ObservableCollection<LoyaltyRewardSummary> _selectedRewards;
        private bool _canAccrue;
        private bool _canRedeem;
        private int _pointsToAccrue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScannerViewModel"/> class.
        /// </summary>
        /// <param name="loyaltyService">Service used to process scan sessions and confirm actions.</param>
        /// <param name="scanner">Scanner abstraction used to read QR codes from the device camera.</param>
        public ScannerViewModel(
            ILoyaltyService loyaltyService,
            IScanner scanner)
        {
            _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
            _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));

            _selectedRewards = new ObservableCollection<LoyaltyRewardSummary>();

            ScanCommand = new AsyncCommand(ScanAsync);
            ConfirmAccrualCommand = new AsyncCommand(ConfirmAccrualAsync, () => CanAccrue);
            ConfirmRedemptionCommand = new AsyncCommand(ConfirmRedemptionAsync, () => CanRedeem);
        }

        /// <summary>
        /// Gets or sets the identifier of the current business associated with this device.
        /// This will normally be configured at login/onboarding.
        /// </summary>
        public Guid CurrentBusinessId
        {
            get => _currentBusinessId;
            set => SetProperty(ref _currentBusinessId, value);
        }

        /// <summary>
        /// Gets the current scan session identifier returned by the server after
        /// processing the scanned QR token. This value is required for accrual
        /// and redemption confirmation calls.
        /// </summary>
        public Guid? CurrentScanSessionId
        {
            get => _currentScanSessionId;
            private set => SetProperty(ref _currentScanSessionId, value);
        }

        /// <summary>
        /// Gets the current mode of the scan session (accrual or redemption).
        /// </summary>
        public LoyaltyScanMode? CurrentMode
        {
            get => _currentMode;
            private set => SetProperty(ref _currentMode, value);
        }

        /// <summary>
        /// Gets the list of rewards selected by the consumer for redemption
        /// in the current scan session, as returned by the server.
        /// </summary>
        public ObservableCollection<LoyaltyRewardSummary> SelectedRewards
        {
            get => _selectedRewards;
            private set => SetProperty(ref _selectedRewards, value);
        }

        /// <summary>
        /// Gets or sets a flag indicating whether the accrual action is currently allowed.
        /// </summary>
        public bool CanAccrue
        {
            get => _canAccrue;
            private set
            {
                if (SetProperty(ref _canAccrue, value))
                {
                    ConfirmAccrualCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a flag indicating whether the redemption action is currently allowed.
        /// </summary>
        public bool CanRedeem
        {
            get => _canRedeem;
            private set
            {
                if (SetProperty(ref _canRedeem, value))
                {
                    ConfirmRedemptionCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of points to accrue for the current transaction.
        /// This may be derived from order value or a simple "+1 per visit" rule.
        /// </summary>
        public int PointsToAccrue
        {
            get => _pointsToAccrue;
            set => SetProperty(ref _pointsToAccrue, value);
        }

        /// <summary>
        /// Command that starts the scanner, reads the QR token, and processes
        /// the scan session with the backend.
        /// </summary>
        public AsyncCommand ScanCommand { get; }

        /// <summary>
        /// Command that confirms accrual of points for the current scan session.
        /// </summary>
        public AsyncCommand ConfirmAccrualCommand { get; }

        /// <summary>
        /// Command that confirms redemption of rewards for the current scan session.
        /// </summary>
        public AsyncCommand ConfirmRedemptionCommand { get; }

        private async Task ScanAsync()
        {
            if (CurrentBusinessId == Guid.Empty)
            {
                ErrorMessage = "Business context is not set.";
                return;
            }

            try
            {
                IsBusy = true;
                ErrorMessage = null;
                CanAccrue = false;
                CanRedeem = false;
                SelectedRewards.Clear();
                CurrentScanSessionId = null;
                CurrentMode = null;

                var token = await _scanner.ScanAsync(CancellationToken.None).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(token))
                {
                    ErrorMessage = "No QR code detected.";
                    return;
                }

                if (!Guid.TryParse(token, out var scanSessionId))
                {
                    ErrorMessage = "Invalid QR code.";
                    return;
                }

                var request = new ProcessScanSessionForBusinessRequest
                {
                    ScanSessionId = scanSessionId
                };

                var result = await _loyaltyService.ProcessScanSessionForBusinessAsync(request, CancellationToken.None)
                    .ConfigureAwait(false);

                if (!result.Succeeded)
                {
                    ErrorMessage = result.Error ?? "Failed to process scan session.";
                    return;
                }

                var response = result.Value;

                CurrentMode = response.Mode;
                CurrentScanSessionId = response.ScanSessionId;

                SelectedRewards.Clear();

                if (response.SelectedRewards is { Count: > 0 })
                {
                    foreach (var reward in response.SelectedRewards)
                    {
                        SelectedRewards.Add(reward);
                    }
                }

                // AllowedActions is expected to be a flag enum or structured permissions.
                CanAccrue = response.AllowedActions.HasFlag(LoyaltyAllowedActions.CanConfirmAccrual);
                CanRedeem = response.AllowedActions.HasFlag(LoyaltyAllowedActions.CanConfirmRedemption);
            }
            catch (Exception ex)
            {
                ErrorMessage = "An unexpected error occurred while scanning QR.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ConfirmAccrualAsync()
        {
            if (!CanAccrue || CurrentScanSessionId is null)
            {
                return;
            }

            try
            {
                IsBusy = true;
                ErrorMessage = null;

                var request = new ConfirmAccrualRequest
                {
                    // NOTE:
                    // Property name ScanSessionId is chosen to match the standard
                    // flow (start-scan returns ScanSessionId, accrue/redeem use it).
                    ScanSessionId = CurrentScanSessionId.Value,
                    Points = PointsToAccrue > 0 ? PointsToAccrue : 1
                };

                var result = await _loyaltyService.ConfirmAccrualAsync(request, CancellationToken.None)
                    .ConfigureAwait(false);

                if (!result.Succeeded)
                {
                    ErrorMessage = result.Error ?? "Failed to confirm accrual.";
                    return;
                }

                // Optionally, you may update the UI with result.Value.NewBalance here.
                CanAccrue = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = "An unexpected error occurred while confirming accrual.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ConfirmRedemptionAsync()
        {
            if (!CanRedeem || CurrentScanSessionId is null)
            {
                return;
            }

            try
            {
                IsBusy = true;
                ErrorMessage = null;

                var request = new ConfirmRedemptionRequest
                {
                    ScanSessionId = CurrentScanSessionId.Value
                };

                var result = await _loyaltyService.ConfirmRedemptionAsync(request, CancellationToken.None)
                    .ConfigureAwait(false);

                if (!result.Succeeded)
                {
                    ErrorMessage = result.Error ?? "Failed to confirm redemption.";
                    return;
                }

                CanRedeem = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = "An unexpected error occurred while confirming redemption.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
