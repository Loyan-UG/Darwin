using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Business.Views;
using Darwin.Mobile.Shared.Services;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace Darwin.Mobile.Business;

/// <summary>
/// Defines routes and initial navigation for the Business mobile shell.
/// </summary>
public sealed partial class AppShell : Shell
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppShell"/> class.
    /// Registers routes and navigates to the login page on startup.
    /// </summary>
    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation
        Routing.RegisterRoute(Routes.Home, typeof(HomePage));
        Routing.RegisterRoute(Routes.Scanner, typeof(ScannerPage));
        Routing.RegisterRoute(Routes.Login, typeof(LoginPage));
        Routing.RegisterRoute(Routes.Session, typeof(SessionPage));


        // Navigate to login on startup
        Dispatcher.Dispatch(async () =>
        {
            await GoToAsync($"//{Routes.Login}");
        });
    }
}
