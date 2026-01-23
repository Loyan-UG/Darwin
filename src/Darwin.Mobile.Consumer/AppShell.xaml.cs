using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Consumer.Views;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Consumer;

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


