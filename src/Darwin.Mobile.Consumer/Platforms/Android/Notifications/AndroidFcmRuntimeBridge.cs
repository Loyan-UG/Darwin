using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Gms.Tasks;
using Firebase.Messaging;

namespace Darwin.Mobile.Consumer.Services.Notifications;

/// <summary>
/// Android runtime bridge for resolving current FCM registration token.
/// </summary>
/// <remarks>
/// This bridge first checks in-memory cache updated by <see cref="ConsumerFirebaseMessagingService"/>,
/// then falls back to Firebase token API so cold-start scenarios can still resolve the token.
/// </remarks>
internal static class AndroidFcmRuntimeBridge
{
    public static async Task<string?> GetTokenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cached = PushTokenRuntimeState.GetPushToken();
        if (!string.IsNullOrWhiteSpace(cached))
        {
            return cached;
        }

        var task = FirebaseMessaging.Instance.GetToken();
        var token = await task.AsTask(cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(token))
        {
            PushTokenRuntimeState.SetPushToken(token);
        }

        return token;
    }

    private static Task<string?> AsTask(this Android.Gms.Tasks.Task firebaseTask, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        firebaseTask.AddOnCompleteListener(new OnCompleteListener(completeTask =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
                return;
            }

            if (!completeTask.IsSuccessful)
            {
                tcs.TrySetException(completeTask.Exception ?? new InvalidOperationException("FCM token retrieval failed."));
                return;
            }

            var token = completeTask.Result?.ToString();
            tcs.TrySetResult(string.IsNullOrWhiteSpace(token) ? null : token);
        }));

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        }

        return tcs.Task;
    }

    private sealed class OnCompleteListener : Java.Lang.Object, IOnCompleteListener
    {
        private readonly Action<Android.Gms.Tasks.Task> _onComplete;

        public OnCompleteListener(Action<Android.Gms.Tasks.Task> onComplete)
        {
            _onComplete = onComplete ?? throw new ArgumentNullException(nameof(onComplete));
        }

        public void OnComplete(Android.Gms.Tasks.Task task)
        {
            _onComplete(task);
        }
    }
}
