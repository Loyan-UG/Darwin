using Android.OS;
using AndroidX.Core.Content;
using AndroidX.Credentials;
using Darwin.Contracts.Meta;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Consumer.Services.Authentication;
using Darwin.Mobile.Shared.Api;
using Microsoft.Maui.ApplicationModel;
using Xamarin.GoogleAndroid.Libraries.Identity.GoogleId;

namespace Darwin.Mobile.Consumer;

/// <summary>
/// Android implementation of Google external login through Android Credential Manager.
/// The resulting Google ID token is exchanged by WebApi; provider tokens are never persisted locally.
/// </summary>
public sealed class AndroidGoogleExternalAuthService : IConsumerExternalAuthService
{
    private readonly IApiClient _apiClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AndroidGoogleExternalAuthService"/> class.
    /// </summary>
    /// <param name="apiClient">Mobile API client used to load public bootstrap configuration.</param>
    public AndroidGoogleExternalAuthService(IApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    /// <inheritdoc />
    public async Task<ExternalIdentityCredential> SignInWithGoogleAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var bootstrap = await _apiClient.GetAsync<AppBootstrapResponse>(ApiRoutes.Meta.Bootstrap, ct)
            .ConfigureAwait(false);

        if (bootstrap is null ||
            !bootstrap.GoogleExternalLoginEnabled ||
            string.IsNullOrWhiteSpace(bootstrap.GoogleExternalLoginWebClientId))
        {
            throw new InvalidOperationException(AppResources.ExternalLoginGoogleUnavailable);
        }

        var activity = Platform.CurrentActivity
            ?? throw new InvalidOperationException(AppResources.ExternalLoginGoogleUnavailable);

        var googleIdOption = new GetGoogleIdOption.Builder()
            .SetServerClientId(bootstrap.GoogleExternalLoginWebClientId.Trim())
            .SetFilterByAuthorizedAccounts(false)
            .SetAutoSelectEnabled(false)
            .Build();

        var request = new GetCredentialRequest.Builder()
            .AddCredentialOption(googleIdOption)
            .Build();

        var credentialManager = CredentialManager.Create(activity);
        var pending = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellationSignal = new CancellationSignal();
        using var callback = new GoogleCredentialManagerCallback(pending);
        var executor = ContextCompat.GetMainExecutor(activity)
            ?? throw new InvalidOperationException(AppResources.ExternalLoginGoogleConfigurationFailed);

        using var registration = ct.Register(() =>
        {
            cancellationSignal.Cancel();
            pending.TrySetCanceled(ct);
        });

        credentialManager.GetCredentialAsync(activity, request, cancellationSignal, executor, callback);

        var idToken = await pending.Task.ConfigureAwait(false);
        return new ExternalIdentityCredential("Google", idToken);
    }

    private sealed class GoogleCredentialManagerCallback : Java.Lang.Object, ICredentialManagerCallback
    {
        private readonly TaskCompletionSource<string> _pending;

        public GoogleCredentialManagerCallback(TaskCompletionSource<string> pending)
        {
            _pending = pending;
        }

        public void OnResult(Java.Lang.Object? result)
        {
            try
            {
                if (result is not GetCredentialResponse response)
                {
                    _pending.TrySetException(new InvalidOperationException(BuildGoogleSignInMessage(
                        AppResources.ExternalLoginGoogleFailed,
                        $"unexpected-result={result?.GetType().Name ?? "missing"}")));
                    return;
                }

                var credential = response.Credential;
                if (credential is null ||
                    !string.Equals(credential.Type, GoogleIdTokenCredential.TypeGoogleIdTokenCredential, StringComparison.Ordinal))
                {
                    _pending.TrySetException(new InvalidOperationException(BuildGoogleSignInMessage(
                        AppResources.ExternalLoginGoogleFailed,
                        $"credential-type={credential?.Type ?? "missing"}")));
                    return;
                }

                var googleCredential = GoogleIdTokenCredential.CreateFrom(credential.Data);
                if (string.IsNullOrWhiteSpace(googleCredential.IdToken))
                {
                    _pending.TrySetException(new InvalidOperationException(BuildGoogleSignInMessage(
                        AppResources.ExternalLoginGoogleTokenMissing,
                        "credential-manager-token-missing")));
                    return;
                }

                _pending.TrySetResult(googleCredential.IdToken);
            }
            catch (Exception ex)
            {
                _pending.TrySetException(new InvalidOperationException(BuildGoogleSignInMessage(
                    AppResources.ExternalLoginGoogleFailed,
                    $"{ex.GetType().Name}: {ex.Message}")));
            }
        }

        public void OnError(Java.Lang.Object? error)
        {
            _pending.TrySetException(new InvalidOperationException(BuildGoogleSignInMessage(
                AppResources.ExternalLoginGoogleFailed,
                error?.ToString() ?? "credential-manager-error")));
        }
    }

    private static string BuildGoogleSignInMessage(string userMessage, string diagnostic)
    {
#if DEBUG
        return $"{userMessage} Debug details: {diagnostic}";
#else
        return userMessage;
#endif
    }
}
