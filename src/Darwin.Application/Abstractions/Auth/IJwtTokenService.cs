using System;
using System.Collections.Generic;

namespace Darwin.Application.Abstractions.Auth
{
    /// <summary>
    /// Issues short-lived access tokens and opaque refresh tokens for a user.
    /// Access tokens are JWTs signed with SiteSetting-configured keys.
    /// Refresh tokens are opaque, stored server-side using UserToken with purpose
    /// "JwtRefresh" or "JwtRefresh:{deviceId}" depending on SiteSetting configuration.
    /// </summary>
    public interface IJwtTokenService
    {
        /// <summary>
        /// Issues a new access token (JWT) and a refresh token for a given user id.
        /// The refresh token may be device-bound depending on SiteSetting.JwtRequireDeviceBinding.
        /// </summary>
        /// <param name="userId">The authenticated user identifier.</param>
        /// <param name="email">
        /// Email address of the user. It is embedded as the "email" claim in the JWT.
        /// </param>
        /// <param name="deviceId">
        /// Optional logical device identifier (for example, a mobile installation or browser fingerprint).
        /// When SiteSetting.JwtRequireDeviceBinding is true, callers MUST supply a non-empty value;
        /// the refresh token will then be persisted with the purpose "JwtRefresh:{deviceId}" and is only
        /// considered valid when the same device id is presented on validation.
        /// When SiteSetting.JwtRequireDeviceBinding is false, any provided value is ignored and the
        /// refresh token is stored with the generic purpose "JwtRefresh".
        /// </param>
        /// <param name="scopes">
        /// Optional collection of logical scopes to embed in the "scope" claim when
        /// SiteSetting.JwtEmitScopes is enabled. This keeps the JWT compact while still
        /// allowing coarse-grained authorization decisions.
        /// </param>
        /// <returns>
        /// A tuple describing the issued access token and refresh token along with their
        /// respective expiration timestamps in UTC.
        /// </returns>
        (string accessToken, DateTime expiresAtUtc, string refreshToken, DateTime refreshExpiresAtUtc)
            IssueTokens(Guid userId, string email, string? deviceId, IEnumerable<string>? scopes = null);

        /// <summary>
        /// Validates a refresh token (opaque) and returns the associated user id if valid.
        /// Device binding is enforced when SiteSetting.JwtRequireDeviceBinding is enabled:
        /// callers must provide the same device id that was used at issuance time.
        /// </summary>
        /// <param name="refreshToken">Opaque refresh token value supplied by the client.</param>
        /// <param name="deviceId">
        /// Optional device identifier supplied by the client. Required when
        /// SiteSetting.JwtRequireDeviceBinding is true; ignored otherwise.
        /// </param>
        /// <returns>
        /// The user id associated with the refresh token when valid; otherwise null.
        /// </returns>
        Guid? ValidateRefreshToken(string refreshToken, string? deviceId);

        /// <summary>
        /// Revokes a single refresh token (typically on logout) by marking it as used.
        /// Device binding is taken into account in the same way as in <see cref="ValidateRefreshToken"/>.
        /// </summary>
        /// <param name="refreshToken">Opaque refresh token value to revoke.</param>
        /// <param name="deviceId">
        /// Optional device identifier used to compute the token purpose. Required when
        /// SiteSetting.JwtRequireDeviceBinding is true; ignored otherwise.
        /// </param>
        void RevokeRefreshToken(string refreshToken, string? deviceId);

        /// <summary>
        /// Revokes all active refresh tokens for the specified user by marking them as used.
        /// This is primarily used to implement "single device only" sessions and security-critical
        /// operations such as password reset, account compromise recovery, and forced logout.
        /// </summary>
        /// <param name="userId">User whose refresh tokens should be invalidated.</param>
        /// <returns>The number of tokens that were marked as revoked.</returns>
        int RevokeAllForUser(Guid userId);
    }
}
