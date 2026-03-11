using System;
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

    [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    public void DidRegisterForRemoteNotifications(UIApplication application, NSData deviceToken)
    {
        ApplePushRuntimeBridge.SetDeviceToken(deviceToken);
    }

    [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
    public void DidFailToRegisterForRemoteNotifications(UIApplication application, NSError error)
    {
        // Clear stale token when APNs registration fails to avoid sending invalid token to backend.
        PushTokenRuntimeState.SetPushToken(null);
    }
}
