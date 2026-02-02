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
            await _authService.LoginAsync(Email, Password, deviceId: null, CancellationToken.None);
            await _navigationService.GoToAsync($"//{Routes.Home}");
            Email = string.Empty;
            Password = string.Empty;
        }
        catch (Exception ex)
        {
            // user-friendly message, with optional developer details when debugging
            var userMsg = AppResources.InvalidCredentials; // موجود در منابع
    #if DEBUG
            var devDetails = ex.Message;
            RunOnMain(() => ErrorMessage = $"{userMsg}\n({devDetails})");
    #else
            RunOnMain(() => ErrorMessage = userMsg);
    #endif
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
