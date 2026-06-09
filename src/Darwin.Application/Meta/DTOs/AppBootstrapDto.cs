using System;

namespace Darwin.Application.Meta.DTOs
{
    /// <summary>
    /// Application-internal DTO returned by the bootstrap use case.
    /// This type is intentionally not part of Contracts to keep Application independent.
    /// </summary>
    /// <param name="JwtAudience">Audience used by mobile clients for JWT authentication.</param>
    /// <param name="QrTokenRefreshSeconds">Client refresh cadence for QR token refresh behavior.</param>
    /// <param name="MaxOutboxItems">Client-side maximum outbox size before forcing a flush.</param>
    /// <param name="GoogleExternalLoginEnabled">Whether Google external login is enabled.</param>
    /// <param name="GoogleExternalLoginAndroidClientId">Google OAuth client id for Android clients.</param>
    /// <param name="GoogleExternalLoginIosClientId">Google OAuth client id for iOS/MacCatalyst clients.</param>
    /// <param name="GoogleExternalLoginWebClientId">Google OAuth client id for Web/front-office clients.</param>
    public sealed record AppBootstrapDto(
        string JwtAudience,
        int QrTokenRefreshSeconds,
        int MaxOutboxItems,
        bool GoogleExternalLoginEnabled,
        string? GoogleExternalLoginAndroidClientId,
        string? GoogleExternalLoginIosClientId,
        string? GoogleExternalLoginWebClientId);
}
