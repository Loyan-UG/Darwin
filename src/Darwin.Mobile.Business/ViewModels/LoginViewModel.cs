using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// Handles Business app login flow.
/// 
/// Important behavior:
/// - This ViewModel is Business-app specific and must not affect Consumer/Web flows.
/// - It translates technical auth/network exceptions into user-friendly messages.
/// - It can emit compact diagnostics in test builds when EnableVerboseNetworkDiagnostics is true.
/// </summary>
public sealed partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly ApiOptions _apiOptions;

    private string? _email;
    private string? _password;

    // I keep this message centralized to make future localization/resource migration easy.
    private const string NoBusinessMembershipUserMessage =
        "Your username and password are correct, but your account is not assigned to any business yet. Please contact support.";

    private const string ServerUnreachableUserMessage =
        "Unable to connect to server. Please check your internet connection and server URL, then try again.";

    /// <summary>
    /// Raised when the page should reveal the error area (e.g., scroll to top).
    /// The view subscribes to this to avoid error text being hidden behind the keyboard.
    /// </summary>
    public event Action? ErrorBecameVisibleRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginViewModel"/> class.
    /// </summary>
    public LoginViewModel(
        IAuthService authService,
        INavigationService navigationService,
        ApiOptions apiOptions)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _apiOptions = apiOptions ?? throw new ArgumentNullException(nameof(apiOptions));

        // TODO [TEST-ONLY][MOBILE-SECURITY]:
        // Remove these default credentials before release builds.
        // They are intentionally set for rapid QA loops in local/dev environments only.
        Email = "biz1@darwin.de";
        Password = "Business123!";

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
            await _authService.LoginAsync(Email.Trim(), Password, deviceId: null, CancellationToken.None);

            await _navigationService.GoToAsync($"//{Routes.Home}");

            // I clear credentials after success for privacy and to avoid stale inputs.
            Email = string.Empty;
            Password = string.Empty;
        }
        catch (Exception ex)
        {
            // Convert technical/internal errors to a user-facing message appropriate for the Business app.
            var resolvedMessage = ResolveBusinessLoginErrorMessage(ex, _apiOptions);

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
    /// </summary>
    private static string ResolveBusinessLoginErrorMessage(Exception ex, ApiOptions apiOptions)
    {
        var raw = ex.Message ?? string.Empty;

        if (raw.Contains("missing business_id claim", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("not assigned to any business", StringComparison.OrdinalIgnoreCase))
        {
            return NoBusinessMembershipUserMessage;
        }

        if (LooksLikeConnectivityError(raw, ex))
        {
            if (!apiOptions.EnableVerboseNetworkDiagnostics)
            {
                return ServerUnreachableUserMessage;
            }

            var hint = BuildNetworkDiagnosticHint(ex, apiOptions.BaseUrl, raw);
            return $"{ServerUnreachableUserMessage}\n{hint}";
        }

        return AppResources.InvalidCredentials;
    }

    /// <summary>
    /// Detects typical transport/network failures from exception message or type.
    /// </summary>
    private static bool LooksLikeConnectivityError(string raw, Exception ex)
    {
        if (raw.Contains("Network error", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("No such host", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("SSL", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("certificate", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("connection", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Defensive fallback based on exception types often used by HttpClient stack.
        var typeName = ex.GetType().FullName ?? string.Empty;
        return typeName.Contains("HttpRequestException", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("SocketException", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("AuthenticationException", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("TaskCanceledException", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Builds a compact diagnostic hint for test builds.
    /// </summary>
    private static string BuildNetworkDiagnosticHint(Exception ex, string baseUrl, string rawMessage)
    {
        var sb = new StringBuilder();

        sb.Append("Diagnostic hint: ");
        sb.Append($"Exception={ex.GetType().Name}; ");
        sb.Append($"BaseUrl={baseUrl}; ");
        sb.Append($"Message={rawMessage}");

        if (ex.InnerException is not null)
        {
            sb.Append($"; Inner={ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        }

        return sb.ToString();
    }
}
