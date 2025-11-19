using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// QR payload displayed by consumer app. NEVER includes internal user id.
    /// It contains a short-lived, opaque token issued by the server.
    /// </summary>
    public sealed class QrCodePayload
    {
        /// <summary>Format version for forward compatibility (e.g., "LOY1").</summary>
        public string Version { get; init; } = "LOY1";

        /// <summary>Opaque, short-lived token bound to the user.</summary>
        public string Token { get; init; } = default!;
    }
}