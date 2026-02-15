using Darwin.Mobile.Consumer.Constants;
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

        // Register the dynamic route for business details
        Routing.RegisterRoute($"{Routes.BusinessDetail}/{{businessId}}", typeof(Views.BusinessDetailPage));
    }

    /// <summary>
    /// Handles the logout toolbar item click. Logs out the user and navigates to the login page.
    /// </summary>
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        try
        {
            await _authService.LogoutAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            // After logging out, replace the MainPage with a new navigation stack
            Application.Current.MainPage = new NavigationPage(new Views.LoginPage());
        }
    }
}
