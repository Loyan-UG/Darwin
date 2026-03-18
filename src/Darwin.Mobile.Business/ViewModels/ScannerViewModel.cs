using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Business.Services.Identity;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Integration;
using Darwin.Mobile.Shared.Navigation;
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
    private readonly IScanner _scanner;
    private readonly INavigationService _navigationService;
    private readonly IBusinessAuthorizationService _authorizationService;

    private string _lastScannedToken = string.Empty;
    private bool _hasRedemptionPermission = true;
    private bool _hasAccrualPermission = true;
    private string _operatorRole = "—";

    private string? _successMessage;
    private string? _warningMessage;

    /// <summary>
    /// Requests page to reveal feedback area when an error/warning/success is set.
    /// </summary>
    public event Action? FeedbackVisibilityRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScannerViewModel"/> class.
    /// </summary>
    /// <param name="scanner">The scanner implementation used to read QR codes.</param>
    /// <param name="navigationService">Shell navigation service for page transitions.</param>
    /// <param name="authorizationService">Business authorization context used for role/permission labels.</param>
    public ScannerViewModel(
        IScanner scanner,
        INavigationService navigationService,
        IBusinessAuthorizationService authorizationService)
    {
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));

        ScanCommand = new AsyncCommand(ScanAsync);
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
                OnPropertyChanged(nameof(HasAnyProcessingPermission));
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
                OnPropertyChanged(nameof(HasAnyProcessingPermission));
            }
        }
    }

    /// <summary>
    /// Indicates whether the operator has at least one processing permission.
    /// This is shown on the scanner page as a quick readiness indicator.
    /// </summary>
    public bool HasAnyProcessingPermission => HasAccrualPermission || HasRedemptionPermission;

    /// <summary>
    /// Gets the last scanned QR token, mainly for debugging or display only.
    /// </summary>
    public string LastScannedToken
    {
        get => _lastScannedToken;
        private set => SetProperty(ref _lastScannedToken, value);
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

    public override async Task OnAppearingAsync()
    {
        await RefreshAuthorizationAsync().ConfigureAwait(false);
    }

    private async Task ScanAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearFeedback();

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

    private async Task RefreshAuthorizationAsync()
    {
        var snapshot = await _authorizationService.GetSnapshotAsync(CancellationToken.None).ConfigureAwait(false);

        if (!snapshot.Succeeded || snapshot.Value is null)
        {
            OperatorRole = "—";
            HasAccrualPermission = false;
            HasRedemptionPermission = false;
            SetWarning(AppResources.BusinessPermissionsUnavailableWarning);
            return;
        }

        OperatorRole = snapshot.Value.RoleDisplayName;
        HasAccrualPermission = snapshot.Value.CanConfirmAccrual;
        HasRedemptionPermission = snapshot.Value.CanConfirmRedemption;

        if (!HasAnyProcessingPermission)
        {
            SetWarning(AppResources.BusinessNoScannerPermissionWarning);
        }
    }
}
