namespace Darwin.Mobile.Consumer.Services.Authentication;

/// <summary>
/// Starts platform-specific external identity flows for the Consumer app.
/// </summary>
public interface IConsumerExternalAuthService
{
    /// <summary>
    /// Starts Google sign-in and returns a short-lived ID token for the WebApi exchange.
    /// </summary>
    /// <param name="ct">Cancellation token for the user-facing operation.</param>
    /// <returns>A provider credential that must be exchanged immediately.</returns>
    Task<ExternalIdentityCredential> SignInWithGoogleAsync(CancellationToken ct);
}
