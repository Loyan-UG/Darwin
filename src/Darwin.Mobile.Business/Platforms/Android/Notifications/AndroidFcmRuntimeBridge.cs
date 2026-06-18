using System;
using System.Threading.Tasks;
using Android.Gms.Tasks;
using Firebase.Messaging;

namespace Darwin.Mobile.Business.Services.Notifications;

internal static class AndroidFcmRuntimeBridge
{
    public static async Task<string?> GetTokenAsync(System.Threading.CancellationToken cancellationToken)
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

    private static async Task<string?> AsTask(this Android.Gms.Tasks.Task firebaseTask, System.Threading.CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        firebaseTask.AddOnCompleteListener(new OnCompleteListener(completeTask =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(cancellationToken);
                    return;
                }

                if (!completeTask.IsSuccessful)
                {
                    var errorMessage = completeTask.Exception?.Message;
                    tcs.TrySetException(new InvalidOperationException(
                        string.IsNullOrWhiteSpace(errorMessage)
                            ? "FCM token retrieval failed."
                            : $"FCM token retrieval failed. {errorMessage}"));
                    return;
                }

                var token = completeTask.Result?.ToString();
                tcs.TrySetResult(string.IsNullOrWhiteSpace(token) ? null : token);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }));

        using var cancellationRegistration = cancellationToken.CanBeCanceled
            ? cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken))
            : default;

        return await tcs.Task.ConfigureAwait(false);
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
