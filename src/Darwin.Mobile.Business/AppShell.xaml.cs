using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Business.Views;
using Microsoft.Maui.Controls;


namespace Darwin.Mobile.Business;

/// <summary>
/// Defines the shell and registers application routes.
/// </summary>
public sealed partial class AppShell : Shell
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppShell"/> class.
    /// </summary>
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(Routes.Home, typeof(HomePage));
        Routing.RegisterRoute(Routes.Rewards, typeof(ComingSoonPage));
    }
}

/// <summary>
/// Static class containing route names for navigation.
/// </summary>
public static class Routes
{
    public const string Home = "Home";
    public const string Rewards = "Rewards";
}
