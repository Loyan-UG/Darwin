using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.ViewModels;
using Darwin.Mobile.Business.Services.Reporting;
using Darwin.Mobile.Business.Services.Identity;
using Darwin.Mobile.Business.Resources;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// View model for the scan session page in the business app.
/// It loads details about a scanned QR session and exposes commands to confirm accrual or redemption.
/// </summary>
public sealed class SessionViewModel : BaseViewModel
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly INavigationService _navigationService;
    private readonly IBusinessActivityTracker _activityTracker;
    private readonly IBusinessAuthorizationService _businessAuthorizationService;
    private readonly IBusinessAccessService _businessAccessService;

    private string _sessionToken = string.Empty;
    private string? _customerName;
    private int _pointsBalance;
    private bool _canConfirmAccrual;
    private bool _canConfirmRedemption;
    private bool _hasAccrualPermission = true;
    private bool _hasRedemptionPermission = true;
    private bool _isOperationsAllowed = true;
    private int _pointsToAccrue = 1;
    private string _operatorRole = "—";

    private string? _successMessage;
    private string? _warningMessage;

    /// <summary>
    /// Requests page to reveal feedback area when an error/warning/success is set.
    /// </summary>
    public event Action? FeedbackVisibilityRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionViewModel"/> class.
    /// </summary>
    public SessionViewModel(
        ILoyaltyService loyaltyService,
        INavigationService navigationService,
        IBusinessActivityTracker activityTracker,
        IBusinessAuthorizationService businessAuthorizationService,
        IBusinessAccessService businessAccessService)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _activityTracker = activityTracker ?? throw new ArgumentNullException(nameof(activityTracker));
        _businessAuthorizationService = businessAuthorizationService ?? throw new ArgumentNullException(nameof(businessAuthorizationService));
        _businessAccessService = businessAccessService ?? throw new ArgumentNullException(nameof(businessAccessService));

        LoadSessionCommand = new AsyncCommand(LoadSessionAsync);
        ConfirmAccrualCommand = new AsyncCommand(ConfirmAccrualAsync, () => CanExecuteAccrual && !IsBusy);
        ConfirmRedemptionCommand = new AsyncCommand(ConfirmRedemptionAsync, () => CanExecuteRedemption && !IsBusy);
    }

    /// <summary>
    /// Gets or sets the session token (passed from the scanner).
    /// </summary>
    public string SessionToken
    {
        get => _sessionToken;
        set => SetProperty(ref _sessionToken, value);
    }

    /// <summary>
    /// Gets or sets the customer display name.
    /// </summary>
    public string? CustomerName
    {
        get => _customerName;
        private set => SetProperty(ref _customerName, value);
    }

    /// <summary>
    /// Gets or sets the current points balance of the account associated with this session.
    /// </summary>
    public int PointsBalance
    {
        get => _pointsBalance;
        private set => SetProperty(ref _pointsBalance, value);
    }

    /// <summary>
    /// Gets a value indicating whether accrual is allowed.
    /// </summary>
    public bool CanConfirmAccrual
    {
        get => _canConfirmAccrual;
        private set
        {
            if (SetProperty(ref _canConfirmAccrual, value))
            {
                ConfirmAccrualCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(CanExecuteAccrual));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether redemption is allowed.
    /// </summary>
    public bool CanConfirmRedemption
    {
        get => _canConfirmRedemption;
        private set
        {
            if (SetProperty(ref _canConfirmRedemption, value))
            {
                ConfirmRedemptionCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(CanExecuteRedemption));
            }
        }
    }

    /// <summary>
    /// Gets or sets the number of points that will be added when confirming accrual.
    /// </summary>
    public int PointsToAccrue
    {
        get => _pointsToAccrue;
        set => SetProperty(ref _pointsToAccrue, value);
    }

    /// <summary>
    /// Current operator role resolved from token claims.
    /// </summary>
    public string OperatorRole
    {
        get => _operatorRole;
        private set => SetProperty(ref _operatorRole, value);
    }

    /// <summary>
    /// Gets a value indicating whether current operator can confirm accruals.
    /// </summary>
    public bool HasAccrualPermission
    {
        get => _hasAccrualPermission;
        private set
        {
            if (SetProperty(ref _hasAccrualPermission, value))
            {
                ConfirmAccrualCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(CanExecuteAccrual));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether current operator can confirm redemptions.
    /// </summary>
    public bool HasRedemptionPermission
    {
        get => _hasRedemptionPermission;
        private set
        {
            if (SetProperty(ref _hasRedemptionPermission, value))
            {
                ConfirmRedemptionCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(CanExecuteRedemption));
            }
        }
    }

    /// <summary>
    /// UI helper: session and role both allow accrual confirmation.
    /// </summary>
    public bool CanExecuteAccrual => _isOperationsAllowed && CanConfirmAccrual && HasAccrualPermission;

    /// <summary>
    /// UI helper: session and role both allow redemption confirmation.
    /// </summary>
    public bool CanExecuteRedemption => _isOperationsAllowed && CanConfirmRedemption && HasRedemptionPermission;

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
    /// Command that loads session details from the server.
    /// </summary>
    public AsyncCommand LoadSessionCommand { get; }

    /// <summary>
    /// Command that confirms a points accrual for the current session.
    /// </summary>
    public AsyncCommand ConfirmAccrualCommand { get; }

    /// <summary>
    /// Command that confirms a redemption for the current session.
    /// </summary>
    public AsyncCommand ConfirmRedemptionCommand { get; }

    public override async Task OnAppearingAsync()
    {
        await RefreshAuthorizationAsync().ConfigureAwait(false);
        await EnsureOperationsAllowedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Loads session details using the current <see cref="SessionToken"/>.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoadSessionAsync()
    {
        ClearFeedback();

        if (!await EnsureOperationsAllowedAsync().ConfigureAwait(false))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(SessionToken))
        {
            SetWarning("Invalid session token.");
            return;
        }

        IsBusy = true;
        ConfirmAccrualCommand.RaiseCanExecuteChanged();
        ConfirmRedemptionCommand.RaiseCanExecuteChanged();

        try
        {
            // IMPORTANT: keep default await context in view-model methods because we update
            // UI-bound properties right after awaits (Android requires main-thread UI access).
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
            await _activityTracker.RecordSessionLoadedAsync(CustomerName, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
            ConfirmAccrualCommand.RaiseCanExecuteChanged();
            ConfirmRedemptionCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Confirms points accrual for the current session.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ConfirmAccrualAsync()
    {
        ClearFeedback();

        if (!CanConfirmAccrual)
        {
            SetWarning("Accrual is not allowed for this session.");
            return;
        }

        if (!await EnsureOperationsAllowedAsync().ConfigureAwait(false))
        {
            return;
        }

        if (!HasAccrualPermission)
        {
            SetWarning(AppResources.BusinessPermissionDeniedAccrual);
            return;
        }

        if (PointsToAccrue <= 0)
        {
            SetWarning("Points must be greater than zero.");
            return;
        }

        IsBusy = true;
        ConfirmAccrualCommand.RaiseCanExecuteChanged();
        ConfirmRedemptionCommand.RaiseCanExecuteChanged();

        try
        {
            var result = await _loyaltyService.ConfirmAccrualAsync(SessionToken, PointsToAccrue, CancellationToken.None);
            if (!result.Succeeded || result.Value is null)
            {
                SetError(result.Error ?? "Failed to confirm accrual.");
                return;
            }

            // Update points balance and disable further accrual/redemption on this session.
            PointsBalance = result.Value.PointsBalance;
            CanConfirmAccrual = false;
            CanConfirmRedemption = false;

            SetSuccess("Points accrual confirmed successfully.");
            await _activityTracker.RecordAccrualConfirmedAsync(CustomerName, PointsToAccrue, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
            ConfirmAccrualCommand.RaiseCanExecuteChanged();
            ConfirmRedemptionCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Confirms reward redemption for the current session.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ConfirmRedemptionAsync()
    {
        ClearFeedback();

        if (!CanConfirmRedemption)
        {
            SetWarning("Redemption is not allowed for this session.");
            return;
        }

        if (!await EnsureOperationsAllowedAsync().ConfigureAwait(false))
        {
            return;
        }

        if (!HasRedemptionPermission)
        {
            SetWarning(AppResources.BusinessPermissionDeniedRedemption);
            return;
        }

        IsBusy = true;
        ConfirmAccrualCommand.RaiseCanExecuteChanged();
        ConfirmRedemptionCommand.RaiseCanExecuteChanged();

        try
        {
            var result = await _loyaltyService.ConfirmRedemptionAsync(SessionToken, CancellationToken.None);
            if (!result.Succeeded || result.Value is null)
            {
                SetError(result.Error ?? "Failed to confirm redemption.");
                return;
            }

            var previousBalance = PointsBalance;
            PointsBalance = result.Value.PointsBalance;
            CanConfirmAccrual = false;
            CanConfirmRedemption = false;

            SetSuccess("Reward redemption confirmed successfully.");
            var redeemedPoints = Math.Max(0, previousBalance - PointsBalance);
            await _activityTracker.RecordRedemptionConfirmedAsync(CustomerName, redeemedPoints, CancellationToken.None).ConfigureAwait(false);
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

    private async Task RefreshAuthorizationAsync()
    {
        var authSnapshot = await _businessAuthorizationService.GetSnapshotAsync(CancellationToken.None).ConfigureAwait(false);

        if (!authSnapshot.Succeeded || authSnapshot.Value is null)
        {
            OperatorRole = "—";
            HasAccrualPermission = false;
            HasRedemptionPermission = false;
            return;
        }

        OperatorRole = authSnapshot.Value.RoleDisplayName;
        HasAccrualPermission = authSnapshot.Value.CanConfirmAccrual;
        HasRedemptionPermission = authSnapshot.Value.CanConfirmRedemption;
    }

    private async Task<bool> EnsureOperationsAllowedAsync()
    {
        var result = await _businessAccessService.GetCurrentAccessStateAsync(CancellationToken.None).ConfigureAwait(false);
        if (!result.Succeeded || result.Value is null)
        {
            _isOperationsAllowed = false;
            RunOnMain(() =>
            {
                SetWarning(AppResources.BusinessAccessStateLoadFailed);
                ConfirmAccrualCommand.RaiseCanExecuteChanged();
                ConfirmRedemptionCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(CanExecuteAccrual));
                OnPropertyChanged(nameof(CanExecuteRedemption));
            });

            return false;
        }

        _isOperationsAllowed = result.Value.IsOperationsAllowed;
        RunOnMain(() =>
        {
            ConfirmAccrualCommand.RaiseCanExecuteChanged();
            ConfirmRedemptionCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(CanExecuteAccrual));
            OnPropertyChanged(nameof(CanExecuteRedemption));
        });

        if (_isOperationsAllowed)
        {
            return true;
        }

        RunOnMain(() => SetWarning(BusinessAccessStateUiMapper.GetOperationalStatusMessage(result.Value)));
        return false;
    }
}
