using System;
using System.Collections.Generic;

namespace Darwin.Application.Abstractions.Auth
{
    /// <summary>
    /// Issues short-lived access tokens and opaque refresh tokens for a user.
    /// Access tokens are JWTs signed with SiteSetting-configured keys.
    /// Refresh tokens are opaque, stored server-side using UserToken with purpose = "JwtRefresh".
    /// </summary>
    public interface IJwtTokenService
    {
        /// <summary>
        /// Issues a new access token (JWT) and a refresh token for a given user id.
        /// </summary>
        (string accessToken, DateTime expiresAtUtc, string refreshToken, DateTime refreshExpiresAtUtc)
            IssueTokens(Guid userId, string email, IEnumerable<string>? scopes = null);

        /// <summary>
        /// Validates a refresh token (opaque) and returns the associated user id if valid.
        /// Optionally enforces device binding by user agent hash.
        /// </summary>
        Guid? ValidateRefreshToken(string refreshToken, string? deviceId);

        /// <summary>
        /// Revokes a single refresh token (on logout) or all active tokens for a user (on password reset).
        /// </summary>
        void RevokeRefreshToken(string refreshToken, string? deviceId);
        int RevokeAllForUser(Guid userId);
    }
}
