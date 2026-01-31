using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;
using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Consumer.Resources;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Handles user login via AuthService and navigates to the QR tab on success.
/// </summary>
public sealed class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    private string? _email;
    private string? _password;

    public LoginViewModel(IAuthService authService, INavigationService navigationService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        LoginCommand = new AsyncCommand(LoginAsync, () => !IsBusy);
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
            await _authService.LoginAsync(Email, Password, null, CancellationToken.None).ConfigureAwait(false);
            // Navigate to QR tab
            await _navigationService.GoToAsync($"//{Routes.Qr}").ConfigureAwait(false);
            // Clear credentials
            Email = string.Empty;
            Password = string.Empty;
        }
        catch
        {
            ErrorMessage = AppResources.InvalidCredentials;
        }
        finally
        {
            IsBusy = false;
            LoginCommand.RaiseCanExecuteChanged();
        }
    }
}
