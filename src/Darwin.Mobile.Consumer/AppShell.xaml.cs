using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Consumer.ViewModels;
using Darwin.Mobile.Consumer.Views;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.Services;
using Microsoft.Maui.Controls;
using System;
using System.Threading;

namespace Darwin.Mobile.Consumer;

/// <summary>
/// Code-behind for the main shell. Registers dynamic routes and handles logout.
/// </summary>
public partial class AppShell : Shell
{
    private readonly IAuthService _authService;

    public AppShell(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        Routing.RegisterRoute($"{Routes.BusinessDetail}/{{businessId}}", typeof(BusinessDetailPage));
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        try
        {
            await _authService.LogoutAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            // Create new LoginViewModel with required services
            var navigationService = new ShellNavigationService();
            var loginViewModel = new LoginViewModel(_authService, navigationService);
            var loginPage = new LoginPage(loginViewModel);

            // Reset MainPage to a new navigation stack starting from the login page
            Application.Current.MainPage = new NavigationPage(loginPage);
        }
    }
}
