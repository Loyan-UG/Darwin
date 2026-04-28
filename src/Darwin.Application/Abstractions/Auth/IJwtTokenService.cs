using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        Task<(string accessToken, DateTime expiresAtUtc, string refreshToken, DateTime refreshExpiresAtUtc)>
            IssueTokensAsync(
                Guid userId,
                string email,
                string? deviceId,
                IEnumerable<string>? scopes = null,
                Guid? preferredBusinessId = null,
                CancellationToken ct = default);

        /// <summary>
        /// Validates a refresh token (opaque) and returns the associated user id if valid.
        /// </summary>
        Task<Guid?> ValidateRefreshTokenAsync(string refreshToken, string? deviceId, CancellationToken ct = default);

        /// <summary>
        /// Revokes a single refresh token by marking it as used.
        /// </summary>
        Task RevokeRefreshTokenAsync(string refreshToken, string? deviceId, CancellationToken ct = default);

        /// <summary>
        /// Revokes all active refresh tokens for the specified user by marking them as used.
        /// </summary>
        Task<int> RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);
    }
}
