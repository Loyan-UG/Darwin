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
    public sealed record AppBootstrapDto(
        string JwtAudience,
        int QrTokenRefreshSeconds,
        int MaxOutboxItems);
}
