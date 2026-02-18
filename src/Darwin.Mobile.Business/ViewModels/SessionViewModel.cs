using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// View model for the scan session page in the Business app.
/// Handles session loading, accrual confirmation and redemption confirmation,
/// with explicit success/warning/error feedback for operator UX.
/// </summary>
public sealed class SessionViewModel : BaseViewModel
{
    private readonly ILoyaltyService _loyaltyService;

    private string _sessionToken = string.Empty;
    private string? _customerName;
    private int _pointsBalance;
    private bool _canConfirmAccrual;
    private bool _canConfirmRedemption;
    private int _pointsToAccrue = 1;

    private string? _successMessage;
    private string? _warningMessage;

    /// <summary>
    /// Raised when page should reveal feedback cards (scroll to top).
    /// </summary>
    public event Action? FeedbackVisibilityRequested;

    public SessionViewModel(ILoyaltyService loyaltyService)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));

        LoadSessionCommand = new AsyncCommand(LoadSessionAsync);
        ConfirmAccrualCommand = new AsyncCommand(ConfirmAccrualAsync, () => CanConfirmAccrual && !IsBusy);
        ConfirmRedemptionCommand = new AsyncCommand(ConfirmRedemptionAsync, () => CanConfirmRedemption && !IsBusy);
    }

    public string SessionToken
    {
        get => _sessionToken;
        set => SetProperty(ref _sessionToken, value);
    }

    public string? CustomerName
    {
        get => _customerName;
        private set => SetProperty(ref _customerName, value);
    }

    public int PointsBalance
    {
        get => _pointsBalance;
        private set => SetProperty(ref _pointsBalance, value);
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

    public int PointsToAccrue
    {
        get => _pointsToAccrue;
        set => SetProperty(ref _pointsToAccrue, value);
    }

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

    public AsyncCommand LoadSessionCommand { get; }
    public AsyncCommand ConfirmAccrualCommand { get; }
    public AsyncCommand ConfirmRedemptionCommand { get; }

    public async Task LoadSessionAsync()
    {
        ClearFeedback();

        if (string.IsNullOrWhiteSpace(SessionToken))
        {
            SetWarning("Invalid session token.");
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _loyaltyService.ProcessScanSessionForBusinessAsync(SessionToken, CancellationToken.None);
            if (!result.Succeeded || result.Value is null)
            {
                SetError(result.Error ?? "Failed to load session details.");
                return;
            }

            var model = result.Value;
            CustomerName = model.CustomerDisplayName;
            PointsBalance = model.AccountSummary.PointsBalance;
            CanConfirmAccrual = model.CanConfirmAccrual;
            CanConfirmRedemption = model.CanConfirmRedemption;

            SetSuccess("Session loaded successfully.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ConfirmAccrualAsync()
    {
        ClearFeedback();

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

        IsBusy = true;
        try
        {
            var result = await _loyaltyService.ConfirmAccrualAsync(SessionToken, PointsToAccrue, CancellationToken.None);
            if (!result.Succeeded || result.Value is null)
            {
                SetError(result.Error ?? "Failed to confirm accrual.");
                return;
            }

            PointsBalance = result.Value.PointsBalance;
            CanConfirmAccrual = false;
            CanConfirmRedemption = false;

            SetSuccess("Points were added successfully.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ConfirmRedemptionAsync()
    {
        ClearFeedback();

        if (!CanConfirmRedemption)
        {
            SetWarning("Redemption is not allowed for this session.");
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _loyaltyService.ConfirmRedemptionAsync(SessionToken, CancellationToken.None);
            if (!result.Succeeded || result.Value is null)
            {
                SetError(result.Error ?? "Failed to confirm redemption.");
                return;
            }

            PointsBalance = result.Value.PointsBalance;
            CanConfirmAccrual = false;
            CanConfirmRedemption = false;

            SetSuccess("Redemption confirmed successfully.");
        }
        finally
        {
            IsBusy = false;
        }
    }

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
