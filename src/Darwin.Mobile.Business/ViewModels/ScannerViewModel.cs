using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Integration;
using Darwin.Mobile.Shared.Models.Loyalty;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// View model for the business scanner screen.
/// Orchestrates scanning a consumer QR code and calling the loyalty service
/// to resolve, accrue points, or redeem rewards. After a successful scan,
/// it navigates to the session page to display the session details.
/// </summary>
public sealed class ScannerViewModel : BaseViewModel
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly IScanner _scanner;
    private readonly INavigationService _navigationService;

    private string _lastScannedToken = string.Empty;
    private BusinessScanSessionClientModel? _currentSession;

    private bool _canConfirmAccrual;
    private bool _canConfirmRedemption;
    private int _pointsToAccrue = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScannerViewModel"/> class.
    /// </summary>
    /// <param name="loyaltyService">The loyalty service abstraction.</param>
    /// <param name="scanner">The scanner implementation used to read QR codes.</param>
    /// <param name="navigationService">Shell navigation service for page transitions.</param>
    public ScannerViewModel(
        ILoyaltyService loyaltyService,
        IScanner scanner,
        INavigationService navigationService)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        ScanCommand = new AsyncCommand(ScanAsync);
        ConfirmAccrualCommand = new AsyncCommand(ConfirmAccrualAsync, () => CanConfirmAccrual);
        ConfirmRedemptionCommand = new AsyncCommand(ConfirmRedemptionAsync, () => CanConfirmRedemption);
    }

    /// <summary>
    /// Gets the last scanned QR token, mainly for debugging or display only.
    /// </summary>
    public string LastScannedToken
    {
        get => _lastScannedToken;
        private set => SetProperty(ref _lastScannedToken, value);
    }

    /// <summary>
    /// Gets or sets the number of points to accrue when confirming.
    /// This may represent a fixed per-visit value or be set by the cashier.
    /// </summary>
    public int PointsToAccrue
    {
        get => _pointsToAccrue;
        set => SetProperty(ref _pointsToAccrue, value);
    }

    /// <summary>
    /// Gets a value indicating whether the current session allows confirming accrual.
    /// </summary>
    public bool CanConfirmAccrual
    {
        get => _canConfirmAccrual;
        private set
        {
            if (SetProperty(ref _canConfirmAccrual, value))
            {
                ConfirmAccrualCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current session allows confirming redemption.
    /// </summary>
    public bool CanConfirmRedemption
    {
        get => _canConfirmRedemption;
        private set
        {
            if (SetProperty(ref _canConfirmRedemption, value))
            {
                ConfirmRedemptionCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Command that triggers scanning a QR code and resolving it via WebApi.
    /// On successful scan and session resolution, navigates to the session page.
    /// </summary>
    public AsyncCommand ScanCommand { get; }

    /// <summary>
    /// Command that confirms point accrual for the current scan session.
    /// </summary>
    public AsyncCommand ConfirmAccrualCommand { get; }

    /// <summary>
    /// Command that confirms reward redemption for the current scan session.
    /// </summary>
    public AsyncCommand ConfirmRedemptionCommand { get; }

    private async Task ScanAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        _currentSession = null;
        CanConfirmAccrual = false;
        CanConfirmRedemption = false;

        try
        {
            // Read a QR code from the scanner. If the user cancels or no QR is found,
            // scanner returns null or empty string.
            var token = await _scanner.ScanAsync(CancellationToken.None).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(token))
            {
                ErrorMessage = "No QR code detected.";
                LastScannedToken = string.Empty;
                return;
            }

            LastScannedToken = token;

            // Resolve the scanned token via WebApi to determine session details
            var result = await _loyaltyService
                .ProcessScanSessionForBusinessAsync(token, CancellationToken.None)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to process scan.";
                return;
            }

            _currentSession = result.Value;

            // Determine which actions are allowed on this session.
            CanConfirmAccrual = _currentSession.CanConfirmAccrual;
            CanConfirmRedemption = _currentSession.CanConfirmRedemption;

            // Navigate to the session page and pass the session token as a query parameter.
            var parameters = new Dictionary<string, object?>
            {
                ["token"] = token
            };

            await _navigationService.GoToAsync(Routes.Session, parameters).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ConfirmAccrualAsync()
    {
        if (_currentSession is null)
        {
            ErrorMessage = "No active scan session.";
            return;
        }

        if (!CanConfirmAccrual)
        {
            ErrorMessage = "Accrual is not allowed for this session.";
            return;
        }

        if (PointsToAccrue <= 0)
        {
            ErrorMessage = "Points must be greater than zero.";
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
            var result = await _loyaltyService
                .ConfirmAccrualAsync(_currentSession.Token, PointsToAccrue, CancellationToken.None)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to confirm accrual.";
                return;
            }

            // After a successful accrual, disable further accrual and redemption on this session.
            CanConfirmAccrual = false;
            CanConfirmRedemption = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ConfirmRedemptionAsync()
    {
        if (_currentSession is null)
        {
            ErrorMessage = "No active scan session.";
            return;
        }

        if (!CanConfirmRedemption)
        {
            ErrorMessage = "Redemption is not allowed for this session.";
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
            var result = await _loyaltyService
                .ConfirmRedemptionAsync(_currentSession.Token, CancellationToken.None)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to confirm redemption.";
                return;
            }

            // After redemption, prevent repeat actions on the same session.
            CanConfirmRedemption = false;
            CanConfirmAccrual = false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
