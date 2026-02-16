using CommunityToolkit.Mvvm.Input;
using Darwin.Contracts.Meta;
using Darwin.Mobile.Business;
using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;
using Microsoft.Maui.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Handles user login via <see cref="IAuthService"/>.
/// After successful authentication, the application root is swapped to <see cref="AppShell"/>.
/// </summary>
public sealed partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginViewModel"/> class.
    /// </summary>
    /// <param name="authService">Service used to authenticate users.</param>
    public LoginViewModel(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
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
    /// switches the app root page to the authenticated shell on success.
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
            // Attempt to log in with email/password.
            // DeviceId remains null until we wire a dedicated device identity flow.
            AppBootstrapResponse _ = await _authService.LoginAsync(
                Email,
                Password,
                deviceId: null,
                CancellationToken.None);

            // Root page replacement must run through App so it can target the active window.
            if (Application.Current is App app)
            {
                await app.NavigateToAuthenticatedShellAsync();
            }
            else
            {
                throw new InvalidOperationException("Application instance is not available.");
            }
        }
        catch (Exception ex)
        {
            // Surface a user-friendly message while preserving the exception text for diagnostics during development.
            ErrorMessage = "Login failed. Please check your credentials and try again. " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}