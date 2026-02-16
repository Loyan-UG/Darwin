using CommunityToolkit.Mvvm.Input;
using Darwin.Contracts.Meta;
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
            // Attempt login with email/password credentials.
            // DeviceId remains null until a dedicated device identity workflow is added.
            AppBootstrapResponse _ = await _authService.LoginAsync(
                Email,
                Password,
                deviceId: null,
                CancellationToken.None);

            // Enter authenticated mode by switching the root page via the window-aware navigator.
            await _appRootNavigator.NavigateToAuthenticatedShellAsync();
        }
        catch (Exception ex)
        {
            // Keep the message user-friendly while still surfacing basic diagnostic context during development.
            ErrorMessage = "Login failed. Please check your credentials and try again. " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
