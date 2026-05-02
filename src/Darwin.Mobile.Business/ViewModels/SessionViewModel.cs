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
    private string _operatorRole = "-";
    private string _loadedSessionToken = string.Empty;

    private string? _successMessage;
    private string? _warningMessage;
    private CancellationTokenSource? _operationCts;
    private CancellationTokenSource? _readinessCts;
    private static readonly TimeSpan ActivityTrackingTimeout = TimeSpan.FromSeconds(5);

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

        LoadSessionCommand = new AsyncCommand(LoadSessionAsync, () => !IsBusy);
        ConfirmAccrualCommand = new AsyncCommand(ConfirmAccrualAsync, () => CanExecuteAccrual && !IsBusy);
        ConfirmRedemptionCommand = new AsyncCommand(ConfirmRedemptionAsync, () => CanExecuteRedemption && !IsBusy);
    }

    /// <summary>
    /// Gets or sets the session token (passed from the scanner).
    /// </summary>
    public string SessionToken
    {
        get => _sessionToken;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (SetProperty(ref _sessionToken, normalized) && !string.Equals(_loadedSessionToken, normalized, StringComparison.Ordinal))
            {
                ResetLoadedSessionState();
            }
        }
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
        var readinessCts = StartReadinessScope();

        try
        {
            await RefreshAuthorizationAsync(readinessCts.Token).ConfigureAwait(false);
            await EnsureOperationsAllowedAsync(readinessCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Session readiness refresh is cancelled when the page leaves the foreground.
        }
        finally
        {
            CompleteReadinessScope(readinessCts);
        }
    }

    public override Task OnDisappearingAsync()
    {
        CancelActiveReadiness();
        CancelActiveOperation();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Loads session details using the current <see cref="SessionToken"/>.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoadSessionAsync()
    {
        ClearFeedback();

        if (IsBusy)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_loadedSessionToken) &&
            string.Equals(_loadedSessionToken, SessionToken, StringComparison.Ordinal))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(SessionToken))
        {
            SetWarning(AppResources.InvalidSessionToken);
            return;
        }

        var operationCts = StartOperationScope();

        try
        {
            if (!await EnsureOperationsAllowedAsync(operationCts.Token).ConfigureAwait(false))
            {
                return;
            }

            RunOnMain(() =>
            {
                IsBusy = true;
                RaiseCommandStates();
            });

            // IMPORTANT: keep default await context in view-model methods because we update
            // UI-bound properties right after awaits (Android requires main-thread UI access).
            var result = await _loyaltyService.ProcessScanSessionForBusinessAsync(SessionToken, operationCts.Token);
            if (!result.Succeeded || result.Value is null)
            {
                SetError(result.Error ?? AppResources.SessionLoadFailed);
                return;
            }

            var model = result.Value;
            var customerName = model.CustomerDisplayName;
            RunOnMain(() =>
            {
                CustomerName = customerName;
                PointsBalance = model.AccountSummary.PointsBalance;
                CanConfirmAccrual = model.CanConfirmAccrual;
                CanConfirmRedemption = model.CanConfirmRedemption;
                _loadedSessionToken = SessionToken;
                SetSuccess(AppResources.SessionLoadSuccess);
            });
            await RecordSessionLoadedBestEffortAsync(customerName).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Session loading is cancelled when the page disappears or a newer operation supersedes it.
        }
        finally
        {
            CompleteOperationScope(operationCts);
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCommandStates();
            });
        }
    }

    /// <summary>
    /// Confirms points accrual for the current session.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ConfirmAccrualAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ClearFeedback();

        if (!CanConfirmAccrual)
        {
            SetWarning(AppResources.AccrualNotAllowed);
            return;
        }

        if (!HasAccrualPermission)
        {
            SetWarning(AppResources.BusinessPermissionDeniedAccrual);
            return;
        }

        if (PointsToAccrue <= 0)
        {
            SetWarning(AppResources.PointsMustBeGreaterThanZero);
            return;
        }

        var operationCts = StartOperationScope();

        try
        {
            if (!await EnsureOperationsAllowedAsync(operationCts.Token).ConfigureAwait(false))
            {
                return;
            }

            RunOnMain(() =>
            {
                IsBusy = true;
                RaiseCommandStates();
            });

            var result = await _loyaltyService.ConfirmAccrualAsync(SessionToken, PointsToAccrue, operationCts.Token);
            if (!result.Succeeded || result.Value is null)
            {
                SetError(result.Error ?? AppResources.FailedToConfirmAccrual);
                return;
            }

            // Update points balance and disable further accrual/redemption on this session.
            RunOnMain(() =>
            {
                PointsBalance = result.Value.PointsBalance;
                CanConfirmAccrual = false;
                CanConfirmRedemption = false;
                SetSuccess(AppResources.AccrualConfirmedSuccess);
            });
            await RecordAccrualConfirmedBestEffortAsync(CustomerName, PointsToAccrue).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Confirmation was cancelled because the page left the foreground.
        }
        finally
        {
            CompleteOperationScope(operationCts);
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCommandStates();
            });
        }
    }

    /// <summary>
    /// Confirms reward redemption for the current session.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ConfirmRedemptionAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ClearFeedback();

        if (!CanConfirmRedemption)
        {
            SetWarning(AppResources.RedemptionNotAllowed);
            return;
        }

        if (!HasRedemptionPermission)
        {
            SetWarning(AppResources.BusinessPermissionDeniedRedemption);
            return;
        }

        var operationCts = StartOperationScope();

        try
        {
            if (!await EnsureOperationsAllowedAsync(operationCts.Token).ConfigureAwait(false))
            {
                return;
            }

            RunOnMain(() =>
            {
                IsBusy = true;
                RaiseCommandStates();
            });

            var result = await _loyaltyService.ConfirmRedemptionAsync(SessionToken, operationCts.Token);
            if (!result.Succeeded || result.Value is null)
            {
                SetError(result.Error ?? AppResources.FailedToConfirmRedemption);
                return;
            }

            var previousBalance = PointsBalance;
            var newPointsBalance = result.Value.PointsBalance;
            RunOnMain(() =>
            {
                PointsBalance = newPointsBalance;
                CanConfirmAccrual = false;
                CanConfirmRedemption = false;
                SetSuccess(AppResources.RedemptionConfirmedSuccess);
            });
            var redeemedPoints = Math.Max(0, previousBalance - newPointsBalance);
            await RecordRedemptionConfirmedBestEffortAsync(CustomerName, redeemedPoints).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Confirmation was cancelled because the page left the foreground.
        }
        finally
        {
            CompleteOperationScope(operationCts);
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCommandStates();
            });
        }
    }

    /// <summary>
    /// Refreshes session command availability after busy-state and permission changes.
    /// </summary>
    private void RaiseCommandStates()
    {
        LoadSessionCommand.RaiseCanExecuteChanged();
        ConfirmAccrualCommand.RaiseCanExecuteChanged();
        ConfirmRedemptionCommand.RaiseCanExecuteChanged();
    }

    /// <summary>
    /// Clears all feedback messages before a new operation starts.
    /// </summary>
    private void ClearFeedback()
    {
        RunOnMain(() =>
        {
            SuccessMessage = null;
            WarningMessage = null;
            ErrorMessage = null;
        });
    }

    private void SetSuccess(string message)
    {
        RunOnMain(() =>
        {
            SuccessMessage = message;
            WarningMessage = null;
            ErrorMessage = null;
            FeedbackVisibilityRequested?.Invoke();
        });
    }

    private void SetWarning(string message)
    {
        RunOnMain(() =>
        {
            SuccessMessage = null;
            WarningMessage = message;
            ErrorMessage = null;
            FeedbackVisibilityRequested?.Invoke();
        });
    }

    private void SetError(string message)
    {
        RunOnMain(() =>
        {
            SuccessMessage = null;
            WarningMessage = null;
            ErrorMessage = message;
            FeedbackVisibilityRequested?.Invoke();
        });
    }

    private async Task RefreshAuthorizationAsync(CancellationToken ct)
    {
        var authSnapshot = await _businessAuthorizationService.GetSnapshotAsync(ct).ConfigureAwait(false);

        if (!authSnapshot.Succeeded || authSnapshot.Value is null)
        {
            RunOnMain(() =>
            {
                OperatorRole = "-";
                HasAccrualPermission = false;
                HasRedemptionPermission = false;
            });
            return;
        }

        RunOnMain(() =>
        {
            OperatorRole = authSnapshot.Value.RoleDisplayName;
            HasAccrualPermission = authSnapshot.Value.CanConfirmAccrual;
            HasRedemptionPermission = authSnapshot.Value.CanConfirmRedemption;
        });
    }

    private async Task<bool> EnsureOperationsAllowedAsync(CancellationToken ct)
    {
        var result = await _businessAccessService.GetCurrentAccessStateAsync(ct).ConfigureAwait(false);
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

    /// <summary>
    /// Records local dashboard activity without allowing SQLite or storage latency to block the scan session UX.
    /// </summary>
    /// <param name="customerName">Customer display name resolved from the processed scan session.</param>
    private async Task RecordSessionLoadedBestEffortAsync(string? customerName)
    {
        try
        {
            using var timeout = new CancellationTokenSource(ActivityTrackingTimeout);
            await _activityTracker.RecordSessionLoadedAsync(customerName, timeout.Token).ConfigureAwait(false);
        }
        catch
        {
            // Local reporting is best-effort and must not interrupt the operator after the session was loaded.
        }
    }

    /// <summary>
    /// Records accrual confirmation activity without making local reporting part of the confirmation critical path.
    /// </summary>
    /// <param name="customerName">Customer display name shown in the current session.</param>
    /// <param name="pointsAccrued">Confirmed points delta.</param>
    private async Task RecordAccrualConfirmedBestEffortAsync(string? customerName, int pointsAccrued)
    {
        try
        {
            using var timeout = new CancellationTokenSource(ActivityTrackingTimeout);
            await _activityTracker.RecordAccrualConfirmedAsync(customerName, pointsAccrued, timeout.Token).ConfigureAwait(false);
        }
        catch
        {
            // Local reporting is best-effort and must not interrupt the operator after accrual was confirmed.
        }
    }

    /// <summary>
    /// Records redemption confirmation activity without making local reporting part of the confirmation critical path.
    /// </summary>
    /// <param name="customerName">Customer display name shown in the current session.</param>
    /// <param name="pointsRedeemed">Confirmed redemption delta.</param>
    private async Task RecordRedemptionConfirmedBestEffortAsync(string? customerName, int pointsRedeemed)
    {
        try
        {
            using var timeout = new CancellationTokenSource(ActivityTrackingTimeout);
            await _activityTracker.RecordRedemptionConfirmedAsync(customerName, pointsRedeemed, timeout.Token).ConfigureAwait(false);
        }
        catch
        {
            // Local reporting is best-effort and must not interrupt the operator after redemption was confirmed.
        }
    }

    /// <summary>
    /// Resets session-specific UI state when navigation assigns a different scan token.
    /// </summary>
    private void ResetLoadedSessionState()
    {
        _loadedSessionToken = string.Empty;
        CustomerName = null;
        PointsBalance = 0;
        CanConfirmAccrual = false;
        CanConfirmRedemption = false;
        ClearFeedback();
    }

    /// <summary>
    /// Starts a cancellable session readiness refresh and cancels any stale refresh still in-flight.
    /// </summary>
    /// <returns>The cancellation source owned by the current readiness refresh.</returns>
    private CancellationTokenSource StartReadinessScope()
    {
        var readinessCts = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _readinessCts, readinessCts);
        previous?.Cancel();
        return readinessCts;
    }

    /// <summary>
    /// Releases a completed readiness refresh when it still owns the active refresh slot.
    /// </summary>
    /// <param name="readinessCts">Readiness cancellation source to complete.</param>
    private void CompleteReadinessScope(CancellationTokenSource readinessCts)
    {
        if (ReferenceEquals(_readinessCts, readinessCts))
        {
            _readinessCts = null;
        }

        readinessCts.Dispose();
    }

    /// <summary>
    /// Cancels the active readiness refresh without disposing a token source still observed by service code.
    /// </summary>
    private void CancelActiveReadiness()
    {
        var readinessCts = Interlocked.Exchange(ref _readinessCts, null);
        readinessCts?.Cancel();
    }

    /// <summary>
    /// Starts a cancellable session operation and cancels any older operation defensively.
    /// </summary>
    /// <returns>The cancellation source owned by the current operation.</returns>
    private CancellationTokenSource StartOperationScope()
    {
        var operationCts = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _operationCts, operationCts);
        previous?.Cancel();
        return operationCts;
    }

    /// <summary>
    /// Disposes the active operation scope once the owning operation finishes.
    /// </summary>
    /// <param name="operationCts">Operation cancellation source to complete.</param>
    private void CompleteOperationScope(CancellationTokenSource operationCts)
    {
        if (ReferenceEquals(_operationCts, operationCts))
        {
            _operationCts = null;
        }

        operationCts.Dispose();
    }

    /// <summary>
    /// Cancels any in-flight session operation that should no longer update the view model.
    /// </summary>
    private void CancelActiveOperation()
    {
        var operationCts = Interlocked.Exchange(ref _operationCts, null);
        operationCts?.Cancel();
    }
}
