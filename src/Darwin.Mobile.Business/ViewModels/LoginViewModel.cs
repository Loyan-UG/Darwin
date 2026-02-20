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
/// Handles login flow for the Business app.
/// 
/// Important UX behavior:
/// - Do not collapse all failures into "Invalid credentials".
/// - Distinguish network/server reachability errors from authentication errors.
/// - Keep security-sensitive failures generic where needed.
/// </summary>
public sealed partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    private string? _email;
    private string? _password;

    public LoginViewModel(IAuthService authService, INavigationService navigationService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        LoginCommand = new AsyncCommand(LoginAsync, CanLogin);
    }

    public string? Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string? Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public AsyncCommand LoginCommand { get; }

    private bool CanLogin() => !IsBusy;

    /// <summary>
    /// Executes login and maps failures to user-friendly localized messages.
    /// </summary>
    private async Task LoginAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = AppResources.EmailRequired;
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = AppResources.PasswordRequired;
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
            var msg = ex.Message ?? string.Empty;

            // 1) Explicit network/connectivity failures (ApiClient returns "Network error: ...")
            if (msg.Contains("Network error", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("No such host is known", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("SSL", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("certificate", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("connection", StringComparison.OrdinalIgnoreCase))
            {
                RunOnMain(() => ErrorMessage = AppResources.ServerUnreachableMessage);
                return;
            }

            // 2) Business app token-shape failure => credentials OK but no business membership
            if (msg.Contains("missing business_id claim", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("not assigned to any business", StringComparison.OrdinalIgnoreCase))
            {
                RunOnMain(() => ErrorMessage = AppResources.NoBusinessMembershipMessage);
                return;
            }

            // 3) Default secure fallback
            RunOnMain(() => ErrorMessage = AppResources.InvalidCredentials);
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
}
