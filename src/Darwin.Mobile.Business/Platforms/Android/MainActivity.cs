using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace Darwin.Mobile.Business;

/// <summary>
/// Main Android activity for the Business application.
/// </summary>
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
