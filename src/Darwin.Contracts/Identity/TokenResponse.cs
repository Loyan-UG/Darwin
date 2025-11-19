using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Contracts.Identity
{
    /// <summary>
    /// Standard authentication result for mobile/Web API clients.
    /// Wraps access and refresh tokens along with user identity metadata.
    /// </summary>
    public sealed class TokenResponse
    {
        /// <summary>
        /// Gets or sets the signed JWT access token (short-lived).
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC timestamp when the access token expires.
        /// </summary>
        public DateTime AccessTokenExpiresAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the opaque refresh token (long-lived).
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC timestamp when the refresh token expires.
        /// </summary>
        public DateTime RefreshTokenExpiresAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the authenticated user's identifier.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the authenticated user's email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of scopes granted in this token.
        /// This is optional and controlled by JwtEmitScopes site setting.
        /// </summary>
        public IReadOnlyList<string>? Scopes { get; set; }
    }
}
