using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Firebase.Messaging;
using System;
using System.Collections.Generic;

namespace Darwin.Mobile.Business.Services.Notifications;

[Service(Exported = false, Name = "com.loyan.darwin.mobile.business.BusinessFirebaseMessagingService")]
[IntentFilter(["com.google.firebase.MESSAGING_EVENT"])]
public sealed class BusinessFirebaseMessagingService : FirebaseMessagingService
{
    private const string NotificationChannelId = "loyan_business_notifications";
    private const string NotificationChannelName = "Loyan business notifications";

    public override void OnNewToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            PushTokenRuntimeState.SetPushToken(null);
            return;
        }

        base.OnNewToken(token);
        PushTokenRuntimeState.SetPushToken(token);
    }

    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);

        var data = message.Data;
        var notification = message.GetNotification();
        var title = ReadData(data, "title") ?? notification?.Title;
        var body = ReadData(data, "body") ?? notification?.Body;
        var deepLink = ReadData(data, "deepLink");
        var notificationId = ReadData(data, "notificationId");

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
        {
            return;
        }

        ShowLocalNotification(
            string.IsNullOrWhiteSpace(title) ? "Loyan Business" : title!,
            body ?? string.Empty,
            deepLink,
            notificationId);
    }

    private void ShowLocalNotification(string title, string body, string? deepLink, string? notificationId)
    {
        try
        {
            EnsureNotificationChannel();

            var intent = new Intent(this, typeof(MainActivity));
            intent.SetAction("com.loyan.darwin.mobile.business.NOTIFICATION_TAP");
            intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
            if (!string.IsNullOrWhiteSpace(deepLink))
            {
                intent.PutExtra("deepLink", deepLink);
            }

            if (!string.IsNullOrWhiteSpace(notificationId))
            {
                intent.PutExtra("notificationId", notificationId);
            }

            var requestCode = BuildNotificationRequestCode(notificationId, deepLink);
            var pendingIntent = PendingIntent.GetActivity(
                this,
                requestCode,
                intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var builder = new NotificationCompat.Builder(this, NotificationChannelId);
            builder.SetSmallIcon(Resource.Mipmap.appicon);
            builder.SetContentTitle(title);
            builder.SetContentText(body);
            builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(body));
            builder.SetAutoCancel(true);
            if (pendingIntent is not null)
            {
                builder.SetContentIntent(pendingIntent);
            }

            builder.SetPriority((int)NotificationPriority.Default);

            NotificationManagerCompat.From(this)?.Notify(requestCode, builder.Build());
        }
        catch
        {
            // Notification display is best effort. Permission or platform setup must not crash message receipt.
        }
    }

    private void EnsureNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return;
        }

#pragma warning disable CA1416
        var manager = (NotificationManager?)GetSystemService(NotificationService);
        if (manager?.GetNotificationChannel(NotificationChannelId) is not null)
        {
            return;
        }

        var channel = new NotificationChannel(NotificationChannelId, NotificationChannelName, NotificationImportance.Default)
        {
            Description = "Loyan business campaign and system notifications"
        };
        manager?.CreateNotificationChannel(channel);
#pragma warning restore CA1416
    }

    private static string? ReadData(IDictionary<string, string>? data, string key)
        => data is not null && data.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

    private static int BuildNotificationRequestCode(string? notificationId, string? deepLink)
    {
        if (Guid.TryParse(notificationId, out var id))
        {
            return id.GetHashCode();
        }

        return Math.Abs((deepLink ?? "loyan-business").GetHashCode(StringComparison.Ordinal));
    }
}
