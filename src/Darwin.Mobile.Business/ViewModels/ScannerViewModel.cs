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
/// Orchestrates scanning a consumer QR code and navigation to the session page.
///
/// Important architecture note:
/// - This view model should NOT process scan sessions via API anymore.
/// - Session resolution is owned by <see cref="SessionViewModel"/> so each scan token
///   is processed exactly once and API logs stay deterministic.
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

    private string? _successMessage;
    private string? _warningMessage;

    /// <summary>
    /// Requests page to reveal feedback area when an error/warning/success is set.
    /// </summary>
    public event Action? FeedbackVisibilityRequested;

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
    /// Success banner text shown at top of page.
    /// </summary>
    public string? SuccessMessage
    {
        get => _successMessage;
        private set
        {
            if (SetProperty(ref _successMessage, value))
            {
                OnPropertyChanged(nameof(HasSuccess));
            }
        }
    }

    /// <summary>
    /// Warning banner text shown at top of page.
    /// </summary>
    public string? WarningMessage
    {
        get => _warningMessage;
        private set
        {
            if (SetProperty(ref _warningMessage, value))
            {
                OnPropertyChanged(nameof(HasWarning));
            }
        }
    }

    /// <summary>
    /// Gets whether success feedback is currently available.
    /// </summary>
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    /// <summary>
    /// Gets whether warning feedback is currently available.
    /// </summary>
    public bool HasWarning => !string.IsNullOrWhiteSpace(WarningMessage);

    /// <summary>
    /// Command that triggers scanning a QR code and navigation to session page.
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
        ClearFeedback();
        _currentSession = null;
        CanConfirmAccrual = false;
        CanConfirmRedemption = false;

        try
        {
            // Read a QR code from the scanner. If the user cancels or no QR is found,
            // scanner returns null or empty string.
            // IMPORTANT: keep default await context in view-model methods because we update
            // UI-bound properties right after awaits (Android requires main-thread UI access).
            var token = await _scanner.ScanAsync(CancellationToken.None);

            if (string.IsNullOrWhiteSpace(token))
            {
                LastScannedToken = string.Empty;
                SetWarning("No QR code detected.");
                return;
            }

            LastScannedToken = token;

            // IMPORTANT:
            // Do not call ProcessScanSessionForBusinessAsync here.
            // The session page is the single owner of scan/session resolution and will call
            // the API exactly once after navigation. This avoids duplicate process requests
            // and prevents inconsistent UI state when the same token is processed twice.
            var parameters = new Dictionary<string, object?>
            {
                ["token"] = token
            };

            SetSuccess("QR code scanned successfully.");
            await _navigationService.GoToAsync(Routes.Session, parameters);
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
            SetWarning("No active scan session.");
            return;
        }

        if (!CanConfirmAccrual)
        {
            SetWarning("Accrual is not allowed for this session.");
            return;
        }

        if (PointsToAccrue <= 0)
        {
            SetWarning("Points must be greater than zero.");
            return;
        }

        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearFeedback();

        try
        {
            var result = await _loyaltyService
                .ConfirmAccrualAsync(_currentSession.Token, PointsToAccrue, CancellationToken.None);

            if (!result.Succeeded || result.Value is null)
            {
                SetError(result.Error ?? "Failed to confirm accrual.");
                return;
            }

            CanConfirmAccrual = false;
            CanConfirmRedemption = false;
            SetSuccess("Points accrual confirmed successfully.");
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
            SetWarning("No active scan session.");
            return;
        }

        if (!CanConfirmRedemption)
        {
            SetWarning("Redemption is not allowed for this session.");
            return;
        }

        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearFeedback();

        try
        {
            var result = await _loyaltyService
                .ConfirmRedemptionAsync(_currentSession.Token, CancellationToken.None);

            if (!result.Succeeded || result.Value is null)
            {
                SetError(result.Error ?? "Failed to confirm redemption.");
                return;
            }

            CanConfirmRedemption = false;
            CanConfirmAccrual = false;
            SetSuccess("Reward redemption confirmed successfully.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Clears all feedback messages before a new operation starts.
    /// </summary>
    private void ClearFeedback()
    {
        SuccessMessage = null;
        WarningMessage = null;
        ErrorMessage = null;
    }

    private void SetSuccess(string message)
    {
        SuccessMessage = message;
        WarningMessage = null;
        ErrorMessage = null;
        FeedbackVisibilityRequested?.Invoke();
    }

    private void SetWarning(string message)
    {
        SuccessMessage = null;
        WarningMessage = message;
        ErrorMessage = null;
        FeedbackVisibilityRequested?.Invoke();
    }

    private void SetError(string message)
    {
        SuccessMessage = null;
        WarningMessage = null;
        ErrorMessage = message;
        FeedbackVisibilityRequested?.Invoke();
    }
}
