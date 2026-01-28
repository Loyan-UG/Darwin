using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;
using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Shared.Navigation;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// View model responsible for handling user login.
/// </summary>
public sealed partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    private string? _email;
    private string? _password;

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
    /// Gets or sets the user's email address.
    /// </summary>
    public string? Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    /// <summary>
    /// Gets or sets the user's password.
    /// </summary>
    public string? Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    /// <summary>
    /// Command used to initiate the login process.
    /// </summary>
    public AsyncCommand LoginCommand { get; }

    private bool CanLogin() => !IsBusy;

    /// <summary>
    /// Attempts to log the user in and navigate to the home page on success.
    /// </summary>
    private async Task LoginAsync()
    {
        ErrorMessage = null;

        // Validate fields
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
            // Perform login via AuthService; device ID is null in this phase
            await _authService.LoginAsync(Email, Password, deviceId: null, CancellationToken.None).ConfigureAwait(false);

            // Navigate to the home page on success
            await _navigationService.GoToAsync($"//{Routes.Home}").ConfigureAwait(false);

            // Clear credentials for security
            Email = string.Empty;
            Password = string.Empty;
        }
        catch
        {
            // Show a generic error to the user; avoid exposing details
            ErrorMessage = AppResources.InvalidCredentials;
        }
        finally
        {
            IsBusy = false;
            LoginCommand.RaiseCanExecuteChanged();
        }
    }
}
