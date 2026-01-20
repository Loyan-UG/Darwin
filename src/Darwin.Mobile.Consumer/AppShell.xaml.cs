using Microsoft.Maui.Controls;
using Darwin.Mobile.Consumer.Views;

namespace Darwin.Mobile.Consumer;

/// <summary>
/// Defines the shell and registers application routes.
/// </summary>
public sealed partial class AppShell : Shell
{
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
