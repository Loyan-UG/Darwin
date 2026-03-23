using Android.App;
using Android.Content.PM;
using Android.OS;

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
}
