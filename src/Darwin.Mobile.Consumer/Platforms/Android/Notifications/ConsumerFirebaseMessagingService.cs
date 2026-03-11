using Android.App;
using Android.Content;
using Firebase.Messaging;

namespace Darwin.Mobile.Consumer.Services.Notifications;

/// <summary>
/// Receives Firebase Messaging callbacks and keeps the latest token synchronized.
/// </summary>
[Service(Exported = false, Name = "com.loyan.darwin.mobile.consumer.ConsumerFirebaseMessagingService")]
[IntentFilter(["com.google.firebase.MESSAGING_EVENT"])]
public sealed class ConsumerFirebaseMessagingService : FirebaseMessagingService
{
    public override void OnNewToken(string? token)
    {
        base.OnNewToken(token);

        // Persist token updates immediately so registration coordinator can pick up changes.
        PushTokenRuntimeState.SetPushToken(token);
    }
}
