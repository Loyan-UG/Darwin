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
/// View model for the Business scanner screen.
///
/// Responsibilities:
/// - Start QR scan.
/// - Resolve scanned token into a server-side scan session.
/// - Expose action availability (accrual/redemption).
/// - Trigger UI feedback visibility when error/warning should be shown.
///
/// Notes:
/// - This class stays Business-app scoped and does not affect Consumer app behavior.
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
    /// Raised when the page should reveal feedback area (typically after validation/server errors).
    /// </summary>
    public event Action? FeedbackVisibilityRequested;

    /// <summary>
    /// Initializes scanner view model.
    /// </summary>
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
    /// Last scanned token, useful for operator confirmation/debug.
    /// </summary>
    public string LastScannedToken
    {
        get => _lastScannedToken;
        private set => SetProperty(ref _lastScannedToken, value);
    }

    /// <summary>
    /// Number of points to accrue.
    /// </summary>
    public int PointsToAccrue
    {
        get => _pointsToAccrue;
        set => SetProperty(ref _pointsToAccrue, value);
    }

    /// <summary>
    /// True when current session allows accrual action.
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
    /// True when current session allows redemption action.
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
    /// Starts QR scan and resolves server session.
    /// </summary>
    public AsyncCommand ScanCommand { get; }

    /// <summary>
    /// Confirms points accrual for active session.
    /// </summary>
    public AsyncCommand ConfirmAccrualCommand { get; }

    /// <summary>
    /// Confirms reward redemption for active session.
    /// </summary>
    public AsyncCommand ConfirmRedemptionCommand { get; }

    /// <summary>
    /// Scans QR token and navigates to Session page when valid.
    /// </summary>
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
            var token = await _scanner.ScanAsync(CancellationToken.None).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(token))
            {
                ErrorMessage = "No QR code detected.";
                LastScannedToken = string.Empty;
                RequestFeedbackVisibility();
                return;
            }

            LastScannedToken = token;

            var result = await _loyaltyService
                .ProcessScanSessionForBusinessAsync(token, CancellationToken.None)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to process scan.";
                RequestFeedbackVisibility();
                return;
            }

            _currentSession = result.Value;
            CanConfirmAccrual = _currentSession.CanConfirmAccrual;
            CanConfirmRedemption = _currentSession.CanConfirmRedemption;

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

    /// <summary>
    /// Confirms accrual action on server.
    /// </summary>
    private async Task ConfirmAccrualAsync()
    {
        if (_currentSession is null)
        {
            ErrorMessage = "No active scan session.";
            RequestFeedbackVisibility();
            return;
        }

        if (!CanConfirmAccrual)
        {
            ErrorMessage = "Accrual is not allowed for this session.";
            RequestFeedbackVisibility();
            return;
        }

        if (PointsToAccrue <= 0)
        {
            ErrorMessage = "Points must be greater than zero.";
            RequestFeedbackVisibility();
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
                RequestFeedbackVisibility();
                return;
            }

            CanConfirmAccrual = false;
            CanConfirmRedemption = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Confirms redemption action on server.
    /// </summary>
    private async Task ConfirmRedemptionAsync()
    {
        if (_currentSession is null)
        {
            ErrorMessage = "No active scan session.";
            RequestFeedbackVisibility();
            return;
        }

        if (!CanConfirmRedemption)
        {
            ErrorMessage = "Redemption is not allowed for this session.";
            RequestFeedbackVisibility();
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
                RequestFeedbackVisibility();
                return;
            }

            CanConfirmRedemption = false;
            CanConfirmAccrual = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Requests UI to make feedback visible on screen.
    /// We marshal to main thread to keep UI updates safe.
    /// </summary>
    private void RequestFeedbackVisibility()
    {
        RunOnMain(() => FeedbackVisibilityRequested?.Invoke());
    }
}
