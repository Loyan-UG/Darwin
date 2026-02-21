using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Integration;
using Darwin.Mobile.Shared.Models.Loyalty;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// Scanner workflow view model for Business app.
///
/// Flow:
/// 1) Acquire QR token from platform scanner.
/// 2) Resolve token via WebApi business endpoint.
/// 3) Expose allowed actions and navigate to session details.
/// 4) Provide user-friendly localized feedback messages.
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
    /// Requests page to reveal feedback area when an error/warning is set.
    /// </summary>
    public event Action? FeedbackVisibilityRequested;

    /// <summary>
    /// Constructor.
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
    /// Last scanned token for operator feedback.
    /// </summary>
    public string LastScannedToken
    {
        get => _lastScannedToken;
        private set => SetProperty(ref _lastScannedToken, value);
    }

    /// <summary>
    /// Points input for accrual.
    /// </summary>
    public int PointsToAccrue
    {
        get => _pointsToAccrue;
        set => SetProperty(ref _pointsToAccrue, value);
    }

    /// <summary>
    /// Whether current session allows accrual.
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
    /// Whether current session allows redemption.
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
    /// Command that initiates scan and session resolution.
    /// </summary>
    public AsyncCommand ScanCommand { get; }

    /// <summary>
    /// Command that confirms accrual.
    /// </summary>
    public AsyncCommand ConfirmAccrualCommand { get; }

    /// <summary>
    /// Command that confirms redemption.
    /// </summary>
    public AsyncCommand ConfirmRedemptionCommand { get; }

    /// <summary>
    /// Scans token and resolves business session.
    /// </summary>
    private async Task ScanAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        ErrorMessage = null;
        _currentSession = null;
        CanConfirmAccrual = false;
        CanConfirmRedemption = false;

        try
        {
            var token = await _scanner.ScanAsync(CancellationToken.None);

            if (string.IsNullOrWhiteSpace(token))
            {
                ErrorMessage = AppResources.NoQrDetected;
                LastScannedToken = string.Empty;
                NotifyFeedbackVisibility();
                return;
            }

            LastScannedToken = token;

            var result = await _loyaltyService
                .ProcessScanSessionForBusinessAsync(token, CancellationToken.None);

            if (!result.Succeeded || result.Value is null)
            {
                ErrorMessage = result.Error ?? AppResources.FailedToProcessScan;
                NotifyFeedbackVisibility();
                return;
            }

            _currentSession = result.Value;
            CanConfirmAccrual = _currentSession.CanConfirmAccrual;
            CanConfirmRedemption = _currentSession.CanConfirmRedemption;

            var parameters = new Dictionary<string, object?> { ["token"] = token };
            await _navigationService.GoToAsync(Routes.Session, parameters);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Confirms accrual for the current session.
    /// </summary>
    private async Task ConfirmAccrualAsync()
    {
        if (_currentSession is null)
        {
            ErrorMessage = AppResources.NoActiveSession;
            NotifyFeedbackVisibility();
            return;
        }

        if (!CanConfirmAccrual)
        {
            ErrorMessage = AppResources.AccrualNotAllowed;
            NotifyFeedbackVisibility();
            return;
        }

        if (PointsToAccrue <= 0)
        {
            ErrorMessage = AppResources.PointsMustBeGreaterThanZero;
            NotifyFeedbackVisibility();
            return;
        }

        if (IsBusy) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _loyaltyService
                .ConfirmAccrualAsync(_currentSession.Token, PointsToAccrue, CancellationToken.None);

            if (!result.Succeeded || result.Value is null)
            {
                ErrorMessage = result.Error ?? AppResources.FailedToConfirmAccrual;
                NotifyFeedbackVisibility();
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
    /// Confirms redemption for the current session.
    /// </summary>
    private async Task ConfirmRedemptionAsync()
    {
        if (_currentSession is null)
        {
            ErrorMessage = AppResources.NoActiveSession;
            NotifyFeedbackVisibility();
            return;
        }

        if (!CanConfirmRedemption)
        {
            ErrorMessage = AppResources.RedemptionNotAllowed;
            NotifyFeedbackVisibility();
            return;
        }

        if (IsBusy) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _loyaltyService
                .ConfirmRedemptionAsync(_currentSession.Token, CancellationToken.None);

            if (!result.Succeeded || result.Value is null)
            {
                ErrorMessage = result.Error ?? AppResources.FailedToConfirmRedemption;
                NotifyFeedbackVisibility();
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
    /// Raises feedback-visibility request on the UI thread.
    /// </summary>
    private void NotifyFeedbackVisibility()
    {
        RunOnMain(() => FeedbackVisibilityRequested?.Invoke());
    }
}
