using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;

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
    /// <summary>
    /// Applies brand-consistent Android system bar colors so no default purple chrome remains.
    /// </summary>
    /// <param name="savedInstanceState">Saved Android activity state.</param>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var brandStatusColor = Android.Graphics.Color.ParseColor("#F4B223");
        var brandNavigationColor = Android.Graphics.Color.ParseColor("#FFF8E7");

        Window?.SetStatusBarColor(brandStatusColor);
        Window?.SetNavigationBarColor(brandNavigationColor);

        if (Window is not null)
        {
            var insetsController = WindowCompat.GetInsetsController(Window, Window.DecorView);
            if (insetsController is not null)
            {
                insetsController.AppearanceLightStatusBars = true;
                insetsController.AppearanceLightNavigationBars = true;
            }
        }
    }
}
