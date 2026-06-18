using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Firebase.Messaging;
using System;
using System.Collections.Generic;

namespace Darwin.Mobile.Consumer.Services.Notifications;

/// <summary>
/// Receives Firebase Messaging callbacks and keeps the latest token synchronized.
/// </summary>
[Service(Exported = false, Name = "com.loyan.darwin.mobile.consumer.ConsumerFirebaseMessagingService")]
[IntentFilter(["com.google.firebase.MESSAGING_EVENT"])]
public sealed class ConsumerFirebaseMessagingService : FirebaseMessagingService
{
    private const string NotificationChannelId = "loyan_consumer_notifications";
    private const string NotificationChannelName = "Loyan notifications";

    public override void OnNewToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            PushTokenRuntimeState.SetPushToken(null);
            return;
        }

        base.OnNewToken(token);

        // Persist token updates immediately so registration coordinator can pick up changes.
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
            string.IsNullOrWhiteSpace(title) ? "Loyan" : title!,
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
            intent.SetAction("com.loyan.darwin.mobile.consumer.NOTIFICATION_TAP");
            intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
            if (!string.IsNullOrWhiteSpace(deepLink))
            {
                intent.PutExtra("deepLink", deepLink);
            }

            if (!string.IsNullOrWhiteSpace(notificationId))
            {
                intent.PutExtra("notificationId", notificationId);
            }

            var pendingIntent = PendingIntent.GetActivity(
                this,
                BuildNotificationRequestCode(notificationId, deepLink),
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

            NotificationManagerCompat.From(this)?.Notify(
                BuildNotificationRequestCode(notificationId, deepLink),
                builder.Build());
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
            Description = "Loyan campaign and account notifications"
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

        return Math.Abs((deepLink ?? "loyan-consumer").GetHashCode(StringComparison.Ordinal));
    }
}
