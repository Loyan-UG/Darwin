using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Microsoft.Maui.Storage;

namespace Darwin.Mobile.Consumer;

/// <summary>
/// Main Android activity for the Consumer application.
/// </summary>
/// <remarks>
/// Runtime responsibilities:
/// - Keeps default MAUI single-top launch behavior.
/// - Requests Android 13+ notification permission once at startup to align FCM delivery
///   capability with server-side registration metadata.
/// - Avoids showing permission prompt repeatedly after a hard deny ("Don't ask again") to
///   prevent noisy UX and unnecessary system dialogs.
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
    private const string NotificationPermissionPromptedPreferenceKey = "consumer.android.notifications.prompted.v1";

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

        var hasPromptedBefore = Preferences.Default.Get(NotificationPermissionPromptedPreferenceKey, false);
        var shouldShowRationale = ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.PostNotifications);

        // Request when:
        // 1) We have never asked before, or
        // 2) Android indicates rationale should be shown (user denied before but can be asked again).
        // Skip when user hard-denied with "Don't ask again" to avoid repeated prompts.
        if (!hasPromptedBefore || shouldShowRationale)
        {
            Preferences.Default.Set(NotificationPermissionPromptedPreferenceKey, true);

            ActivityCompat.RequestPermissions(
                this,
                [Manifest.Permission.PostNotifications],
                NotificationPermissionRequestCode);
        }
    }
}
