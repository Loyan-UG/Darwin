using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace Darwin.Mobile.Consumer;

/// <summary>
/// Main Android activity for the Consumer application.
/// </summary>
/// <remarks>
/// Runtime responsibilities:
/// - Keeps default MAUI single-top launch behavior.
/// - Requests Android 13+ notification permission at startup so FCM-delivered notifications
///   can be displayed when the user grants consent.
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
    private const int NotificationPermissionRequestCode = 13007;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        RequestNotificationPermissionIfNeeded();
    }

    private void RequestNotificationPermissionIfNeeded()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
        {
            return;
        }

        var currentStatus = ContextCompat.CheckSelfPermission(this, Manifest.Permission.PostNotifications);
        if (currentStatus == Permission.Granted)
        {
            return;
        }

        ActivityCompat.RequestPermissions(
            this,
            [Manifest.Permission.PostNotifications],
            NotificationPermissionRequestCode);
    }
}
