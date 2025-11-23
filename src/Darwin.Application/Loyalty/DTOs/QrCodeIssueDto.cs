using System;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// Represents an issued QR token payload for the business app.
    /// The raw token is returned only to the caller and is never stored in plaintext.
    /// </summary>
    public sealed record QrCodeIssueDto
    {
        /// <summary>
        /// Opaque short-lived token to be rendered as a QR code.
        /// </summary>
        public required string Token { get; init; }

        /// <summary>
        /// UTC timestamp after which the token is no longer valid.
        /// </summary>
        public required DateTime ExpiresAtUtc { get; init; }
    }
}
