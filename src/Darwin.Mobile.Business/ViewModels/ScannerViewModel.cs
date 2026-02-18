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
/// ViewModel for the Business scanner screen.
///
/// UX goals:
/// - Show clear feedback for success/warning/error states.
/// - Keep scan flow predictable and avoid silent failures.
/// - Request the view to reveal feedback area when needed (so messages are not hidden).
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
    /// Raised when the page should scroll feedback into view.
    /// </summary>
    public event Action? FeedbackVisibilityRequested;

    public ScannerViewModel(
        ILoyaltyService loyaltyService,
        IScanner scanner,
        INavigationService navigationService)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        ScanCommand = new AsyncCommand(ScanAsync);
        ConfirmAccrualCommand = new AsyncCommand(ConfirmAccrualAsync, () => CanConfirmAccrual && !IsBusy);
        ConfirmRedemptionCommand = new AsyncCommand(ConfirmRedemptionAsync, () => CanConfirmRedemption && !IsBusy);
    }

    /// <summary>
    /// Last scanned token (for operator visibility/debugging).
    /// </summary>
    public string LastScannedToken
    {
        get => _lastScannedToken;
        private set => SetProperty(ref _lastScannedToken, value);
    }

    /// <summary>
    /// Points input for optional accrual action.
    /// </summary>
    public int PointsToAccrue
    {
        get => _pointsToAccrue;
        set => SetProperty(ref _pointsToAccrue, value);
    }

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

    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);
    public bool HasWarning => !string.IsNullOrWhiteSpace(WarningMessage);

    public AsyncCommand ScanCommand { get; }
    public AsyncCommand ConfirmAccrualCommand { get; }
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
            // Scanner service handles camera permission and fallback flow.
            var token = await _scanner.ScanAsync(CancellationToken.None);

            if (string.IsNullOrWhiteSpace(token))
            {
                LastScannedToken = string.Empty;
                SetWarning("No QR code was scanned. Please try again.");
                return;
            }

            LastScannedToken = token;

            var result = await _loyaltyService.ProcessScanSessionForBusinessAsync(token, CancellationToken.None);
            if (!result.Succeeded || result.Value is null)
            {
                SetError(result.Error ?? "Failed to process the scanned QR token.");
                return;
            }

            _currentSession = result.Value;
            CanConfirmAccrual = _currentSession.CanConfirmAccrual;
            CanConfirmRedemption = _currentSession.CanConfirmRedemption;

            SetSuccess("QR code scanned successfully. Loading session details...");

            // Keep navigation explicit and deterministic.
            var parameters = new Dictionary<string, object?> { ["token"] = token };
            await _navigationService.GoToAsync(Routes.Session, parameters);
        }
        catch (OperationCanceledException)
        {
            SetWarning("Scan operation was cancelled.");
        }
        catch (Exception)
        {
            SetError("Unexpected error while scanning. Please try again.");
        }
        finally
        {
            IsBusy = false;
            ConfirmAccrualCommand.RaiseCanExecuteChanged();
            ConfirmRedemptionCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task ConfirmAccrualAsync()
    {
        if (_currentSession is null)
        {
            SetWarning("There is no active scan session.");
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
            var result = await _loyaltyService.ConfirmAccrualAsync(_currentSession.Token, PointsToAccrue, CancellationToken.None);
            if (!result.Succeeded || result.Value is null)
            {
                SetError(result.Error ?? "Failed to confirm accrual.");
                return;
            }

            CanConfirmAccrual = false;
            CanConfirmRedemption = false;
            SetSuccess("Points accrual confirmed successfully.");
        }
        catch (Exception)
        {
            SetError("Unexpected error while confirming accrual.");
        }
        finally
        {
            IsBusy = false;
            ConfirmAccrualCommand.RaiseCanExecuteChanged();
            ConfirmRedemptionCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task ConfirmRedemptionAsync()
    {
        if (_currentSession is null)
        {
            SetWarning("There is no active scan session.");
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
            var result = await _loyaltyService.ConfirmRedemptionAsync(_currentSession.Token, CancellationToken.None);
            if (!result.Succeeded || result.Value is null)
            {
                SetError(result.Error ?? "Failed to confirm redemption.");
                return;
            }

            CanConfirmAccrual = false;
            CanConfirmRedemption = false;
            SetSuccess("Reward redemption confirmed successfully.");
        }
        catch (Exception)
        {
            SetError("Unexpected error while confirming redemption.");
        }
        finally
        {
            IsBusy = false;
            ConfirmAccrualCommand.RaiseCanExecuteChanged();
            ConfirmRedemptionCommand.RaiseCanExecuteChanged();
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
