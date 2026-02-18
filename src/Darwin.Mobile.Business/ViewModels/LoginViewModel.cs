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
/// - This ViewModel is Business-app specific and must not affect Consumer/Web flows.
/// - It translates technical auth exceptions into user-friendly messages.
/// - It keeps generic messaging for security-sensitive cases (invalid credentials or missing permission),
///   while showing a specific support message only for the known "no business membership" case.
/// </summary>
public sealed partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    private string? _email;
    private string? _password;

    // I keep this message centralized to make future localization/resource migration easy.
    private const string NoBusinessMembershipUserMessage =
        "Your username and password are correct, but your account is not assigned to any business yet. Please contact support.";

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
    /// Raised when the page should reveal the error area (e.g., scroll to top).
    /// The view subscribes to this to avoid error text being hidden behind the keyboard.
    /// </summary>
    public event Action? ErrorBecameVisibleRequested;

    private bool CanLogin() => !IsBusy;

    /// <summary>
    /// Executes login.
    /// </summary>
    private async Task LoginAsync()
    {
        // I always clear previous errors before a new submit.
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
            // Shared endpoint is used by all clients.
            // Business-specific handling is intentionally done only in this app layer.
            await _authService.LoginAsync(Email.Trim(), Password, deviceId: null, CancellationToken.None);

            await _navigationService.GoToAsync($"//{Routes.Home}");

            // I clear credentials after success for privacy and to avoid stale inputs.
            Email = string.Empty;
            Password = string.Empty;
        }
        catch (Exception ex)
        {
            // Convert technical/internal errors to a user-facing message appropriate for the Business app.
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
    /// Maps exceptions to Business-app-specific user messages.
    ///
    /// Security and UX rules:
    /// - Keep generic message for invalid credentials / insufficient permission.
    /// - Show a dedicated support message only when we can confidently detect
    ///   the "valid credentials but no business membership" path.
    /// </summary>
    private static string ResolveBusinessLoginErrorMessage(Exception ex)
    {
        var raw = ex.Message ?? string.Empty;

        // This is thrown by AuthService token validation when Business app receives an access token
        // without business_id claim. In our architecture this means the account is not bound
        // to an active business membership.
        if (raw.Contains("missing business_id claim", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("not assigned to any business", StringComparison.OrdinalIgnoreCase))
        {
            return NoBusinessMembershipUserMessage;
        }

        // Any other auth failure remains generic by design.
        return AppResources.InvalidCredentials;
    }
}
