using CommunityToolkit.Mvvm.Input;
using Darwin.Contracts.Meta;
using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;
using Darwin.Shared.Results;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Handles user login via <see cref="IAuthService"/>. After a successful login,
/// the user is navigated to the Discover tab.
/// </summary>
public sealed partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginViewModel"/> class.
    /// </summary>
    /// <param name="authService">Service used to authenticate users.</param>
    /// <param name="navigationService">Service used to perform navigation after login.</param>
    public LoginViewModel(IAuthService authService, INavigationService navigationService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
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
    /// navigates to the Discover tab on success.
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
            // Attempt to log in with email/password. The device ID is null for now.
            AppBootstrapResponse bootstrap =
                await _authService.LoginAsync(Email, Password, deviceId: null, CancellationToken.None)
                .ConfigureAwait(false);

            // If login succeeds, navigate to the Discover tab and reset navigation stack.
            await _navigationService.GoToAsync($"//{Routes.Discover}").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Show a friendly error message; avoid leaking internal details.
            ErrorMessage = "Login failed. Please check your credentials and try again. " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
