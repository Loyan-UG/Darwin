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
using Darwin.Mobile.Shared.Services.Legal;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// Handles Business app login flow.
/// </summary>
/// <remarks>
/// Important behavior:
/// - This ViewModel is Business-app specific and must not affect Consumer/Web flows.
/// - It translates technical auth/network exceptions into user-friendly messages.
/// - It exposes legal entry points before login so the app remains compliant even for unauthenticated users.
/// </remarks>
public sealed partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly ApiOptions _apiOptions;
    private readonly ILegalLinkService _legalLinkService;

    private string? _email;
    private string? _password;
    private string? _infoMessage;

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
        ApiOptions apiOptions,
        ILegalLinkService legalLinkService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _apiOptions = apiOptions ?? throw new ArgumentNullException(nameof(apiOptions));
        _legalLinkService = legalLinkService ?? throw new ArgumentNullException(nameof(legalLinkService));

        LoginCommand = new AsyncCommand(LoginAsync, CanLogin);
        RequestActivationEmailCommand = new AsyncCommand(RequestActivationEmailAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(Email));
        OpenInvitationAcceptanceCommand = new AsyncCommand(OpenInvitationAcceptanceAsync, () => !IsBusy);
        OpenImpressumCommand = new AsyncCommand(() => OpenLegalLinkAsync(LegalLinkKind.Impressum), () => !IsBusy);
        OpenPrivacyPolicyCommand = new AsyncCommand(() => OpenLegalLinkAsync(LegalLinkKind.PrivacyPolicy), () => !IsBusy);
        OpenTermsCommand = new AsyncCommand(() => OpenLegalLinkAsync(LegalLinkKind.BusinessTerms), () => !IsBusy);
        OpenLegalHubCommand = new AsyncCommand(OpenLegalHubAsync, () => !IsBusy);

#if DEBUG
        Email = "biz1@darwin.de";
        Password = "Business123!";
#else
        Email = string.Empty;
        Password = string.Empty;
#endif
    }

    public string? Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
            {
                RaiseReadinessChanged();
                RaiseCommandStates();
            }
        }
    }

    public string? Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                RaiseReadinessChanged();
                RaiseCommandStates();
            }
        }
    }

    /// <summary>
    /// Gets whether the sign-in form is locally ready for submission.
    /// </summary>
    public bool IsLoginReady => !IsBusy && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);

    /// <summary>
    /// Gets contextual guidance for the operator sign-in flow.
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
    /// Gets or sets a non-error informational message shown for auth recovery flows.
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

    public AsyncCommand LoginCommand { get; }

    public AsyncCommand RequestActivationEmailCommand { get; }

    public AsyncCommand OpenInvitationAcceptanceCommand { get; }

    public AsyncCommand OpenImpressumCommand { get; }

    public AsyncCommand OpenPrivacyPolicyCommand { get; }

    public AsyncCommand OpenTermsCommand { get; }

    public AsyncCommand OpenLegalHubCommand { get; }

    private bool CanLogin() => IsLoginReady;

    private async Task LoginAsync()
    {
        ErrorMessage = null;
        InfoMessage = null;

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
        RaiseReadinessChanged();
        RaiseCommandStates();

        try
        {
            await _authService.LoginAsync(Email.Trim(), Password, deviceId: null, CancellationToken.None);
            await _navigationService.GoToAsync($"//{Routes.Home}");
            Email = string.Empty;
            Password = string.Empty;
        }
        catch (Exception ex)
        {
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
                RaiseReadinessChanged();
                RaiseCommandStates();
            });
        }
    }

    private async Task OpenLegalHubAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        RaiseReadinessChanged();
        RaiseCommandStates();

        try
        {
            await _navigationService.GoToAsync(Routes.SettingsLegalHub);
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseReadinessChanged();
                RaiseCommandStates();
            });
        }
    }

    private async Task OpenInvitationAcceptanceAsync()
    {
        if (IsBusy)
        {
            return;
        }

        await _navigationService.GoToAsync(Routes.InvitationAcceptance);
    }

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
            ErrorBecameVisibleRequested?.Invoke();
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        InfoMessage = null;
        RaiseReadinessChanged();
        RaiseCommandStates();

        try
        {
            var sent = await _authService.RequestEmailConfirmationAsync(Email.Trim(), CancellationToken.None);
            if (!sent)
            {
                ErrorMessage = AppResources.ActivationEmailRequestFailed;
                ErrorBecameVisibleRequested?.Invoke();
                return;
            }

            InfoMessage = AppResources.ActivationEmailSent;
            ErrorBecameVisibleRequested?.Invoke();
        }
        catch (Exception ex)
        {
            var resolvedMessage = ResolveBusinessAuthMessage(ex, _apiOptions, AppResources.ActivationEmailRequestFailed);

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
                RaiseReadinessChanged();
                RaiseCommandStates();
            });
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

    private void RaiseReadinessChanged()
    {
        OnPropertyChanged(nameof(IsLoginReady));
        OnPropertyChanged(nameof(LoginReadinessMessage));
    }

    private void RaiseCommandStates()
    {
        LoginCommand?.RaiseCanExecuteChanged();
        RequestActivationEmailCommand?.RaiseCanExecuteChanged();
        OpenInvitationAcceptanceCommand?.RaiseCanExecuteChanged();
        OpenImpressumCommand?.RaiseCanExecuteChanged();
        OpenPrivacyPolicyCommand?.RaiseCanExecuteChanged();
        OpenTermsCommand?.RaiseCanExecuteChanged();
        OpenLegalHubCommand?.RaiseCanExecuteChanged();
    }

    private static string ResolveBusinessLoginErrorMessage(Exception ex, ApiOptions apiOptions)
        => ResolveBusinessAuthMessage(ex, apiOptions, AppResources.InvalidCredentials);

    private static string ResolveBusinessAuthMessage(Exception ex, ApiOptions apiOptions, string fallback)
    {
        var raw = ex.Message ?? string.Empty;

        if (raw.Contains("missing business_id claim", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("not assigned to any business", StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.NoBusinessMembershipMessage;
        }

        if (raw.Contains("Email address is not confirmed", StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.LoginEmailConfirmationRequired;
        }

        if (raw.Contains("Account is locked", StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.LoginAccountLocked;
        }

        if (LooksLikeConnectivityError(raw, ex))
        {
            if (!apiOptions.EnableVerboseNetworkDiagnostics)
            {
                return AppResources.ServerUnreachableMessage;
            }

            var hint = BuildNetworkDiagnosticHint(ex, apiOptions.BaseUrl, raw);
            return $"{AppResources.ServerUnreachableMessage}\n{hint}";
        }

        return fallback;
    }

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

        var typeName = ex.GetType().FullName ?? string.Empty;
        return typeName.Contains("HttpRequestException", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("SocketException", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("AuthenticationException", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("TaskCanceledException", StringComparison.OrdinalIgnoreCase);
    }

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
