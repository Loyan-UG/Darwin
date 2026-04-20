using CommunityToolkit.Mvvm.Input;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Consumer.Services.Navigation;
using Darwin.Mobile.Consumer.Services.Notifications;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Services.Legal;
using Darwin.Mobile.Shared.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Handles user login via <see cref="IAuthService"/>.
/// After successful authentication, the app root is switched to the authenticated shell.
/// </summary>
public sealed partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IAppRootNavigator _appRootNavigator;
    private readonly IConsumerPushRegistrationCoordinator _pushRegistrationCoordinator;
    private readonly ILegalLinkService _legalLinkService;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string? _infoMessage;
    private bool _showActivationEmailAction;

    /// <summary>
    /// Raised when the page should reveal the error area (for example: scroll to top).
    /// Keeping this signal in the ViewModel helps us keep the UX consistent across screen sizes
    /// where the software keyboard can hide feedback text after a failed login attempt.
    /// </summary>
    public event Action? ErrorBecameVisibleRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginViewModel"/> class.
    /// </summary>
    /// <param name="authService">Service used to authenticate users.</param>
    /// <param name="appRootNavigator">Service that performs window-safe root navigation.</param>
    /// <param name="pushRegistrationCoordinator">Best-effort push registration coordinator.</param>
    /// <param name="legalLinkService">Service used to open configured legal links from the pre-login area.</param>
    public LoginViewModel(
        IAuthService authService,
        IAppRootNavigator appRootNavigator,
        IConsumerPushRegistrationCoordinator pushRegistrationCoordinator,
        ILegalLinkService legalLinkService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _appRootNavigator = appRootNavigator ?? throw new ArgumentNullException(nameof(appRootNavigator));
        _pushRegistrationCoordinator = pushRegistrationCoordinator ?? throw new ArgumentNullException(nameof(pushRegistrationCoordinator));
        _legalLinkService = legalLinkService ?? throw new ArgumentNullException(nameof(legalLinkService));

#if DEBUG
        Email = "cons1@darwin.de";
        Password = "Consumer123!";
#else
        Email = string.Empty;
        Password = string.Empty;
#endif
    }

    /// <summary>
    /// Gets or sets the email entered by the user.
    /// </summary>
    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
            {
                RaiseReadinessChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the password entered by the user.
    /// </summary>
    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                RaiseReadinessChanged();
            }
        }
    }

    /// <summary>
    /// Gets whether the login form contains the minimum data required to continue.
    /// </summary>
    public bool IsLoginReady => !IsBusy && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);

    /// <summary>
    /// Gets contextual guidance that explains the next step in the sign-in flow.
    /// </summary>
    public string LoginReadinessMessage =>
        IsBusy
            ? AppResources.LoginReadinessBusy
            : string.IsNullOrWhiteSpace(Email)
                ? AppResources.LoginReadinessEmail
                : string.IsNullOrWhiteSpace(Password)
                    ? AppResources.LoginReadinessPassword
                    : AppResources.LoginReadinessReady;

    /// <summary>
    /// Gets or sets a non-error informational message shown for self-service authentication flows.
    /// </summary>
    public string? InfoMessage
    {
        get => _infoMessage;
        private set
        {
            if (SetProperty(ref _infoMessage, value))
            {
                OnPropertyChanged(nameof(HasInfo));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether an informational message is available.
    /// </summary>
    public bool HasInfo => !string.IsNullOrWhiteSpace(_infoMessage);

    /// <summary>
    /// Gets whether the self-service activation-email action should be shown.
    /// This action is intentionally hidden for normal sign-in to avoid cluttering the entry screen.
    /// </summary>
    public bool ShowActivationEmailAction
    {
        get => _showActivationEmailAction;
        private set => SetProperty(ref _showActivationEmailAction, value);
    }

    /// <summary>
    /// Applies sign-in entry context from nearby auth surfaces such as registration or recovery handoff.
    /// This intentionally resets password and transient error state because the login attempt has not happened yet.
    /// </summary>
    public void ApplyEntryContext(string? email, string? infoMessage = null, bool showActivationEmailAction = false)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            Email = email.Trim();
        }

        Password = string.Empty;
        ErrorMessage = null;
        InfoMessage = infoMessage;
        ShowActivationEmailAction = showActivationEmailAction;
        RaiseReadinessChanged();
    }

    /// <summary>
    /// Executes the login process. Uses <see cref="IAuthService.LoginAsync"/> and
    /// swaps the root page to the authenticated shell on success.
    /// </summary>
    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        InfoMessage = null;
        ShowActivationEmailAction = false;
        RaiseReadinessChanged();

        try
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = AppResources.EmailRequired;
                ErrorBecameVisibleRequested?.Invoke();
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = AppResources.PasswordRequired;
                ErrorBecameVisibleRequested?.Invoke();
                return;
            }

            _ = await _authService.LoginAsync(
                Email.Trim(),
                Password,
                deviceId: null,
                CancellationToken.None);

            _ = _pushRegistrationCoordinator.TryRegisterCurrentDeviceAsync(CancellationToken.None);

            await _appRootNavigator.NavigateToAuthenticatedShellAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ResolveLoginErrorMessage(ex);
            ShowActivationEmailAction = string.Equals(ErrorMessage, AppResources.LoginEmailConfirmationRequired, StringComparison.Ordinal);
            ErrorBecameVisibleRequested?.Invoke();
        }
        finally
        {
            IsBusy = false;
            RaiseReadinessChanged();
        }
    }

    [RelayCommand]
    private async Task OpenImpressumAsync() => await OpenLegalLinkAsync(LegalLinkKind.Impressum).ConfigureAwait(false);

    [RelayCommand]
    private async Task OpenPrivacyPolicyAsync() => await OpenLegalLinkAsync(LegalLinkKind.PrivacyPolicy).ConfigureAwait(false);

    [RelayCommand]
    private async Task OpenTermsAsync() => await OpenLegalLinkAsync(LegalLinkKind.ConsumerTerms).ConfigureAwait(false);

    [RelayCommand]
    private async Task RequestActivationEmailAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = AppResources.EmailRequired;
            InfoMessage = null;
            ShowActivationEmailAction = false;
            ErrorBecameVisibleRequested?.Invoke();
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        InfoMessage = null;
        ShowActivationEmailAction = true;
        RaiseReadinessChanged();

        try
        {
            var sent = await _authService.RequestEmailConfirmationAsync(Email.Trim(), CancellationToken.None);
            if (!sent)
            {
                ErrorMessage = AppResources.ActivationEmailRequestFailed;
                ShowActivationEmailAction = true;
                ErrorBecameVisibleRequested?.Invoke();
                return;
            }

            InfoMessage = AppResources.ActivationEmailSent;
            ShowActivationEmailAction = true;
            ErrorBecameVisibleRequested?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.ActivationEmailRequestFailed);
            ShowActivationEmailAction = true;
            ErrorBecameVisibleRequested?.Invoke();
        }
        finally
        {
            IsBusy = false;
            RaiseReadinessChanged();
        }
    }

    private async Task OpenLegalLinkAsync(LegalLinkKind linkKind)
    {
        var result = await _legalLinkService.OpenAsync(linkKind, CancellationToken.None).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            RunOnMain(() => ErrorMessage = AppResources.LegalOpenFailed);
        }
    }

    /// <summary>
    /// Raises readiness properties together so the login card and CTA stay aligned.
    /// </summary>
    private void RaiseReadinessChanged()
    {
        OnPropertyChanged(nameof(IsLoginReady));
        OnPropertyChanged(nameof(LoginReadinessMessage));
    }

    /// <summary>
    /// Maps technical exceptions to end-user login messages.
    /// </summary>
    private static string ResolveLoginErrorMessage(Exception ex)
    {
        return ViewModelErrorMapper.ToUserMessage(ex, AppResources.InvalidCredentials);
    }
}
