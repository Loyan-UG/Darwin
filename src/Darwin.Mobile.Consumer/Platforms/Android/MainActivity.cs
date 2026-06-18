using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using Darwin.Mobile.Consumer.Services.Notifications;

namespace Darwin.Mobile.Consumer;

/// <summary>
/// Main Android activity for the Consumer application.
/// </summary>
/// <remarks>
/// Runtime responsibilities:
/// - Keeps default MAUI single-top launch behavior.
/// - Does not request notification permission at startup.
/// - Leaves sensitive permission prompts to dedicated just-in-time flows inside the app experience.
/// </remarks>
[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize |
                           ConfigChanges.Orientation |
                           ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout |
                           ConfigChanges.SmallestScreenSize |
                           ConfigChanges.Density)]
public sealed class MainActivity : MauiAppCompatActivity
{
    private const string AndroidStatusBarColor = "#FEDB42";
    private const string AndroidNavigationBarColor = "#FFFFFF";

    /// <summary>
    /// Applies brand-consistent Android system bar colors so no legacy template chrome remains.
    /// </summary>
    /// <param name="savedInstanceState">Saved Android activity state.</param>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        ApplySystemBarColors();
        HandleNotificationIntent(Intent);
    }

    protected override void OnResume()
    {
        base.OnResume();
        ApplySystemBarColors();
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        if (intent is not null)
        {
            Intent = intent;
        }

        HandleNotificationIntent(intent);
    }

    private void ApplySystemBarColors()
    {
        var brandStatusColor = Android.Graphics.Color.ParseColor(AndroidStatusBarColor);
        var brandNavigationColor = Android.Graphics.Color.ParseColor(AndroidNavigationBarColor);

        if (Window is null)
        {
            return;
        }

#pragma warning disable CA1422
        WindowCompat.SetDecorFitsSystemWindows(Window, true);
        Window.SetStatusBarColor(brandStatusColor);
        Window.SetNavigationBarColor(brandNavigationColor);
#pragma warning restore CA1422

        Window.DecorView.SetBackgroundColor(brandStatusColor);

        var insetsController = WindowCompat.GetInsetsController(Window, Window.DecorView);
        if (insetsController is not null)
        {
            insetsController.AppearanceLightStatusBars = true;
            insetsController.AppearanceLightNavigationBars = true;
        }

        ConsumerShellPlatformStyler.Apply(Window.DecorView);

        Window.DecorView.PostDelayed(() =>
        {
#pragma warning disable CA1422
            Window.SetStatusBarColor(brandStatusColor);
#pragma warning restore CA1422
        }, 250);
    }

    private static void HandleNotificationIntent(Intent? intent)
    {
        var deepLink = intent?.GetStringExtra("deepLink");
        if (!string.IsNullOrWhiteSpace(deepLink))
        {
            NotificationDeepLinkNavigator.HandleIncomingDeepLink(deepLink);
        }
    }
}
