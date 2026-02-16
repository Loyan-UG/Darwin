using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Consumer.ViewModels;
using Darwin.Mobile.Consumer.Views;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer;

/// <summary>
/// Application entry point for the Consumer app.
///
/// Startup responsibility:
/// - Create the first window.
/// - Resolve authentication state.
/// - Delegate root-page switching to a window-aware navigator.
/// </summary>
public partial class App : Application
{
    private readonly IAuthService _authService;

    public App(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        MainPage = new ContentPage();
        CheckAuthenticationState();
    }

    /// <summary>
    /// Checks whether a valid token exists and navigates accordingly.
    /// </summary>
    private async void CheckAuthenticationState()
    {
        bool tokenValid;
        try
        {
            tokenValid = await _authService.TryRefreshAsync(CancellationToken.None);
        }
        catch
        {
            tokenValid = false;
        }

        if (tokenValid)
        {
            // When authenticated, pass the auth service into the shell constructor.
            MainPage = new AppShell(_authService);
        }
        else
        {
            // When not authenticated, construct the LoginViewModel manually and inject dependencies.
            var navigationService = new ShellNavigationService();
            var loginViewModel = new LoginViewModel(_authService, navigationService);
            var loginPage = new LoginPage(loginViewModel);
            MainPage = new NavigationPage(loginPage);
        }
    }
}