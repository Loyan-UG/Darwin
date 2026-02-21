using CommunityToolkit.Mvvm.Input;
using Darwin.Contracts.Meta;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Consumer.Services.Navigation;
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
    public LoginViewModel(IAuthService authService, IAppRootNavigator appRootNavigator)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _appRootNavigator = appRootNavigator ?? throw new ArgumentNullException(nameof(appRootNavigator));
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
            AppBootstrapResponse _ = await _authService.LoginAsync(
                Email.Trim(),
                Password,
                deviceId: null,
                CancellationToken.None);

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
        var raw = ex.Message ?? string.Empty;

        if (raw.Contains("Network error", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("No such host", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("SSL", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("certificate", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("invalid_requesturi", StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.ServerUnreachableMessage;
        }

        // Keep credential failures generic for security.
        return AppResources.InvalidCredentials;
    }
}
