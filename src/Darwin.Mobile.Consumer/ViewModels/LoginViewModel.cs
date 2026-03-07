using CommunityToolkit.Mvvm.Input;
using Darwin.Contracts.Meta;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Consumer.Services.Navigation;
using Darwin.Mobile.Consumer.Services.Notifications;
using Darwin.Mobile.Shared.Services;
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
    public LoginViewModel(IAuthService authService, IAppRootNavigator appRootNavigator, IConsumerPushRegistrationCoordinator pushRegistrationCoordinator)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _appRootNavigator = appRootNavigator ?? throw new ArgumentNullException(nameof(appRootNavigator));
        _pushRegistrationCoordinator = pushRegistrationCoordinator ?? throw new ArgumentNullException(nameof(pushRegistrationCoordinator));

        // TODO [TEST-ONLY][MOBILE-SECURITY]:
        // Remove these default credentials before release builds.
        // They are intentionally set for rapid QA loops in local/dev environments only.
        Email = "cons1@darwin.de";
        Password = "Consumer123!";
    }

    /// <summary>
    /// Gets or sets the email entered by the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password entered by the user.
    /// </summary>
    public string Password { get; set; } = string.Empty;

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

        try
        {
            // Keep client-side validation explicit so users get fast feedback without roundtrip.
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

            // Attempt login with email/password credentials.
            // DeviceId remains null until a dedicated device identity workflow is added.
            _ = await _authService.LoginAsync(
                Email.Trim(),
                Password,
                deviceId: null,
                CancellationToken.None);

            // Register current installation in backend device registry (best-effort, non-blocking for login).
            _ = _pushRegistrationCoordinator.TryRegisterCurrentDeviceAsync(CancellationToken.None);

            // Enter authenticated mode by switching the root page via the window-aware navigator.
            await _appRootNavigator.NavigateToAuthenticatedShellAsync();
        }
        catch (Exception ex)
        {
            // We intentionally map transport failures to a connectivity message.
            // This prevents misleading "invalid credentials" errors when server is unreachable.
            ErrorMessage = ResolveLoginErrorMessage(ex);
            ErrorBecameVisibleRequested?.Invoke();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Maps technical exceptions to end-user login messages.
    /// </summary>
    private static string ResolveLoginErrorMessage(Exception ex)
    {
        // Keep credential failures generic for security for non-connectivity failures.
        var mapped = ViewModelErrorMapper.ToUserMessage(ex, AppResources.InvalidCredentials);
        return mapped;
    }
}
