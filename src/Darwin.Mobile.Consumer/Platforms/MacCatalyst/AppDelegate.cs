using System.Threading;
using Darwin.Mobile.Consumer.Services.Notifications;
using Foundation;
using UIKit;

namespace Darwin.Mobile.Consumer;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        var launched = base.FinishedLaunching(application, launchOptions);

        // Bootstrap APNs registration as early as possible.
        _ = ApplePushRuntimeBridge.RequestAuthorizationAndRegisterAsync(CancellationToken.None);
        return launched;
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
    {
        base.RegisteredForRemoteNotifications(application, deviceToken);
        ApplePushRuntimeBridge.SetDeviceToken(deviceToken);
    }

    public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
    {
        base.FailedToRegisterForRemoteNotifications(application, error);

        // Clear stale token when APNs registration fails to avoid sending invalid token to backend.
        PushTokenRuntimeState.SetPushToken(null);
    }
}
