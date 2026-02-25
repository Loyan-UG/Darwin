using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Darwin.Domain.Entities.Businesses;


namespace Darwin.Infrastructure.Security.Jwt
{
    /// <summary>
    /// Default JWT issuing/validation service backed by the UserToken entity.
    /// 
    /// Responsibilities:
    /// - Read JWT configuration from the single SiteSetting row.
    /// - Issue signed access tokens (JWT) and opaque refresh tokens.
    /// - Persist refresh tokens as UserToken entries with purpose "JwtRefresh" or "JwtRefresh:{deviceId}".
    /// - Enforce SiteSetting-driven policies:
    ///   - JwtRequireDeviceBinding: require a device id and bind the refresh token to it.
    ///   - JwtSingleDeviceOnly: keep at most one active refresh token per user (revoke all previous on login).
    /// </summary>
    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly IAppDbContext _db;

        /// <summary>
        /// Initializes a new instance of <see cref="JwtTokenService"/>.
        /// </summary>
        /// <param name="db">
        /// Application DbContext abstraction used to read SiteSetting and persist UserToken rows.
        /// </param>
        public JwtTokenService(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

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
        public (string accessToken, DateTime expiresAtUtc, string refreshToken, DateTime refreshExpiresAtUtc)
            IssueTokens(Guid userId, string email, string? deviceId, IEnumerable<string>? scopes = null)
        {
            var settings = _db.Set<SiteSetting>()
                .AsNoTracking()
                .First();

            if (!settings.JwtEnabled)
            {
                throw new InvalidOperationException("JWT is disabled by SiteSetting (JwtEnabled = false).");
            }

            var nowUtc = DateTime.UtcNow;
            var accessExp = nowUtc.AddMinutes(Math.Max(5, settings.JwtAccessTokenMinutes));
            var refreshExp = nowUtc.AddDays(Math.Max(1, settings.JwtRefreshTokenDays));

            // Null-safe signing key material to avoid nullable warning at call site.
            var signingKeyMaterial = settings.JwtSigningKey ?? string.Empty;
            var signingKey = new SymmetricSecurityKey(GetKeyBytes(signingKeyMaterial));
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.Email, email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new(
                    JwtRegisteredClaimNames.Iat,
                    ToUnixTimestampSeconds(nowUtc),
                    ClaimValueTypes.Integer64)
            };

            if (settings.JwtEmitScopes && scopes is not null)
            {
                claims.Add(new Claim("scope", string.Join(",", scopes)));
            }

            var businessId = ResolveActiveBusinessId(userId);
            if (businessId.HasValue)
            {
                claims.Add(new Claim("business_id", businessId.Value.ToString("D")));
            }

            var jwt = new JwtSecurityToken(
                issuer: settings.JwtIssuer,
                audience: settings.JwtAudience,
                claims: claims,
                notBefore: nowUtc,
                expires: accessExp,
                signingCredentials: signingCredentials);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

            string? effectiveDeviceId = null;
            if (settings.JwtRequireDeviceBinding)
            {
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    throw new InvalidOperationException(
                        "Device binding is required (JwtRequireDeviceBinding = true) but no device id was supplied.");
                }

                effectiveDeviceId = deviceId;
            }

            var purpose = BuildRefreshPurpose(effectiveDeviceId);

            if (settings.JwtSingleDeviceOnly)
            {
                RevokeAllForUser(userId);
            }

            var refreshToken = CreateOpaqueToken();
            var refreshRow = new UserToken(userId, purpose, refreshToken, refreshExp);

            // Upsert by unique key (UserId + Purpose) to avoid duplicate key conflicts.
            var existingRow = _db.Set<UserToken>()
                .FirstOrDefault(x => x.UserId == userId && x.Purpose == purpose);

            if (existingRow != null)
            {
                existingRow.Value = refreshToken;
                existingRow.ExpiresAtUtc = refreshExp;
                existingRow.UsedAtUtc = null;
            }
            else
            {
                _db.Set<UserToken>().Add(refreshRow);
            }

            _db.SaveChangesAsync().GetAwaiter().GetResult();

            return (accessToken, accessExp, refreshToken, refreshExp);
        }




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
        public Guid? ValidateRefreshToken(string refreshToken, string? deviceId)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return null;
            }

            // Read configuration to determine whether we must enforce device binding.
            var settings = _db.Set<SiteSetting>()
                .AsNoTracking()
                .First();

            string? effectiveDeviceId = null;
            if (settings.JwtRequireDeviceBinding)
            {
                // With device binding required, a missing device id makes the token unusable.
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    return null;
                }

                effectiveDeviceId = deviceId;
            }

            var purpose = BuildRefreshPurpose(effectiveDeviceId);

            var row = _db.Set<UserToken>()
                .AsNoTracking()
                .FirstOrDefault(x =>
                    x.Purpose == purpose &&
                    x.Value == refreshToken &&
                    x.UsedAtUtc == null);

            if (row is null)
            {
                return null;
            }

            if (row.ExpiresAtUtc.HasValue && row.ExpiresAtUtc.Value < DateTime.UtcNow)
            {
                return null;
            }

            return row.UserId;
        }



        /// <summary>
        /// Revokes a single refresh token (typically on logout) by marking it as used.
        /// Device binding is taken into account in the same way as in <see cref="ValidateRefreshToken"/>.
        /// </summary>
        /// <param name="refreshToken">Opaque refresh token value to revoke.</param>
        /// <param name="deviceId">
        /// Optional device identifier used to compute the token purpose. Required when
        /// SiteSetting.JwtRequireDeviceBinding is true; ignored otherwise.
        /// </param>
        public void RevokeRefreshToken(string refreshToken, string? deviceId)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return;
            }

            var settings = _db.Set<SiteSetting>()
                .AsNoTracking()
                .First();

            string? effectiveDeviceId = null;
            if (settings.JwtRequireDeviceBinding)
            {
                // Without a device id we cannot reliably find the correct device-bound token.
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    return;
                }

                effectiveDeviceId = deviceId;
            }

            var purpose = BuildRefreshPurpose(effectiveDeviceId);

            var row = _db.Set<UserToken>()
                .FirstOrDefault(x => x.Purpose == purpose && x.Value == refreshToken);

            if (row is null)
            {
                return;
            }

            if (row.UsedAtUtc is null)
            {
                row.UsedAtUtc = DateTime.UtcNow;
                _db.SaveChangesAsync().GetAwaiter().GetResult();
            }
        }



        /// <summary>
        /// Revokes all active refresh tokens for the specified user.
        /// This is used when SiteSetting.JwtSingleDeviceOnly is enabled and a new login occurs.
        /// </summary>
        /// <param name="userId">The user identifier whose refresh tokens should be revoked.</param>
        public int RevokeAllForUser(Guid userId)
        {
            var nowUtc = DateTime.UtcNow;

            var rows = _db.Set<UserToken>()
                .Where(x => x.UserId == userId && x.Purpose.StartsWith("JwtRefresh"))
                .ToList();

            var changed = false;
            foreach (var row in rows)
            {
                if (row.UsedAtUtc is null)
                {
                    row.UsedAtUtc = nowUtc;
                    changed = true;
                }
            }

            if (changed)
            {
                _db.SaveChangesAsync().GetAwaiter().GetResult();
            }
            return rows.Count;
        }



        /// <summary>
        /// Converts DateTime to JWT "iat" Unix timestamp seconds.
        /// </summary>
        private static string ToUnixTimestampSeconds(DateTime utc)
        {
            var seconds = new DateTimeOffset(utc).ToUnixTimeSeconds();
            return seconds.ToString(CultureInfo.InvariantCulture);
        }


        /// <summary>
        /// Builds signing key bytes from a configured secret string.
        /// </summary>
        /// <param name="key">Raw key string from SiteSetting.JwtSigningKey.</param>
        /// <returns>UTF8 bytes of the key.</returns>
        private static byte[] GetKeyBytes(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("JWT signing key (SiteSetting.JwtSigningKey) is not configured.");
            }

            try
            {
                return Convert.FromBase64String(key);
            }
            catch (FormatException)
            {
                return Encoding.UTF8.GetBytes(key);
            }
        }



        /// <summary>
        /// Returns cryptographically strong random opaque refresh token.
        /// </summary>
        private static string CreateOpaqueToken()
        {
            Span<byte> bytes = stackalloc byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        /// <summary>
        /// Builds the refresh-token purpose string.
        /// Device-bound tokens use "JwtRefresh:{deviceId}", while device-less tokens use "JwtRefresh".
        /// </summary>
        private static string BuildRefreshPurpose(string? deviceId) =>
            string.IsNullOrWhiteSpace(deviceId) ? "JwtRefresh" : $"JwtRefresh:{deviceId}";




        /// <summary>
        /// Resolves a deterministic active business id for the authenticated user, when available.
        ///
        /// Selection strategy:
        /// - Active, non-deleted memberships only.
        /// - Membership must point to an active, non-deleted business.
        /// - Lowest GUID ordering is used for deterministic claim value when multiple memberships exist.
        ///
        /// Returning null means the user is not a business member and should receive a consumer-style token.
        /// </summary>
        private Guid? ResolveActiveBusinessId(Guid userId)
        {
            return (from m in _db.Set<BusinessMember>()
                    join b in _db.Set<Business>() on m.BusinessId equals b.Id
                    where m.UserId == userId
                          && !m.IsDeleted
                          && m.IsActive
                          && !b.IsDeleted
                          && b.IsActive
                    orderby m.BusinessId
                    select (Guid?)m.BusinessId)
                .FirstOrDefault();
        }

    }
}
