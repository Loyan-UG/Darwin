using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// Handles Business app login flow.
///
/// Important behavior:
/// - Keeps Business-specific error mapping in this app layer only.
/// - Distinguishes connectivity issues from credential issues.
/// - Raises an event so the view can reveal the error area when needed.
/// </summary>
public sealed partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    private string? _email;
    private string? _password;

    // This message is intentionally explicit for business onboarding gap.
    private const string NoBusinessMembershipUserMessage =
        "Your username and password are correct, but your account is not assigned to any business yet. Please contact support.";

    // Friendly connectivity message for mobile users.
    private const string ServerUnreachableUserMessage =
        "Unable to connect to server. Please check your internet connection and server URL, then try again.";

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginViewModel"/> class.
    /// </summary>
    public LoginViewModel(IAuthService authService, INavigationService navigationService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        LoginCommand = new AsyncCommand(LoginAsync, CanLogin);
    }

    /// <summary>
    /// Gets or sets the user email.
    /// </summary>
    public string? Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    /// <summary>
    /// Gets or sets the user password.
    /// </summary>
    public string? Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    /// <summary>
    /// Login command.
    /// </summary>
    public AsyncCommand LoginCommand { get; }

    /// <summary>
    /// Raised when the page should reveal the error area (for example: scroll to top).
    /// </summary>
    public event Action? ErrorBecameVisibleRequested;

    private bool CanLogin() => !IsBusy;

    /// <summary>
    /// Executes login.
    /// </summary>
    private async Task LoginAsync()
    {
        ErrorMessage = null;

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

        IsBusy = true;
        LoginCommand.RaiseCanExecuteChanged();

        try
        {
            await _authService.LoginAsync(Email.Trim(), Password, deviceId: null, CancellationToken.None);
            await _navigationService.GoToAsync($"//{Routes.Home}");

            Email = string.Empty;
            Password = string.Empty;
        }
        catch (Exception ex)
        {
            var resolvedMessage = ResolveBusinessLoginErrorMessage(ex);

            RunOnMain(() =>
            {
                ErrorMessage = resolvedMessage;
                ErrorBecameVisibleRequested?.Invoke();
            });
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                LoginCommand.RaiseCanExecuteChanged();
            });
        }
    }

    /// <summary>
    /// Maps technical errors to business-app-specific user messages.
    /// </summary>
    private static string ResolveBusinessLoginErrorMessage(Exception ex)
    {
        var raw = ex.Message ?? string.Empty;

        // Network / DNS / TLS / transport level failures.
        if (raw.Contains("Network error", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("No such host", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("SSL", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("certificate", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("connection", StringComparison.OrdinalIgnoreCase))
        {
            return ServerUnreachableUserMessage;
        }

        // Business app token shape mismatch: valid auth but no business context in token.
        if (raw.Contains("missing business_id claim", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("not assigned to any business", StringComparison.OrdinalIgnoreCase))
        {
            return NoBusinessMembershipUserMessage;
        }

        // Security-safe generic fallback.
        return AppResources.InvalidCredentials;
    }
}
