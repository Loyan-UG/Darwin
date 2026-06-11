using Darwin.Mobile.Consumer.Resources;

namespace Darwin.Mobile.Consumer.Services.Authentication;

/// <summary>
/// Safe fallback for platforms where the native external identity provider is not configured yet.
/// </summary>
public sealed class UnavailableConsumerExternalAuthService : IConsumerExternalAuthService
{
    /// <inheritdoc />
    public Task<ExternalIdentityCredential> SignInWithGoogleAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        throw new InvalidOperationException(AppResources.ExternalLoginGoogleUnavailable);
    }
}
