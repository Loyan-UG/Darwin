using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Consumer.Views;

namespace Darwin.Mobile.Consumer;

/// <summary>
/// Defines the shell and registers routes for the Consumer app.
/// Navigates to the login page on startup.
/// </summary>
public sealed partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes
        //Routing.RegisterRoute(Routes.Qr, typeof(QrPage));
        //Routing.RegisterRoute(Routes.Discover, typeof(DiscoverPage));
        //Routing.RegisterRoute(Routes.Rewards, typeof(RewardsPage));
        //Routing.RegisterRoute(Routes.Profile, typeof(ProfilePage));
        //Routing.RegisterRoute(Routes.Login, typeof(LoginPage));

        // Navigate to login at startup
        //Dispatcher.Dispatch(async () =>
        //{
        //    await GoToAsync($"//{Routes.Login}");
        //});

        // Register dynamic route for business details.
        // Route pattern includes a parameter for businessId.
        Routing.RegisterRoute($"{Routes.BusinessDetail}/{{businessId}}", typeof(BusinessDetailPage));
    }
}
