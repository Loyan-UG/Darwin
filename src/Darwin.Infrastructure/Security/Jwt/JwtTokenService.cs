using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Darwin.Infrastructure.Security.Jwt
{
    /// <summary>
    /// Default JWT issuing/validation service backed by the UserToken entity.
    /// </summary>
    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;

        public JwtTokenService(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task<(string accessToken, DateTime expiresAtUtc, string refreshToken, DateTime refreshExpiresAtUtc)>
            IssueTokensAsync(
                Guid userId,
                string email,
                string? deviceId,
                IEnumerable<string>? scopes = null,
                Guid? preferredBusinessId = null,
                CancellationToken ct = default)
        {
            var settings = await _db.Set<SiteSetting>()
                .AsNoTracking()
                .FirstAsync(x => !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (!settings.JwtEnabled)
            {
                throw new InvalidOperationException("JWT is disabled by SiteSetting (JwtEnabled = false).");
            }

            var nowUtc = _clock.UtcNow;
            var accessExp = nowUtc.AddMinutes(Math.Max(5, settings.JwtAccessTokenMinutes));
            var refreshExp = nowUtc.AddDays(Math.Max(1, settings.JwtRefreshTokenDays));
            var signingKey = new SymmetricSecurityKey(GetKeyBytes(settings.JwtSigningKey ?? string.Empty));
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.Email, email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new(JwtRegisteredClaimNames.Iat, ToUnixTimestampSeconds(nowUtc), ClaimValueTypes.Integer64)
            };

            if (settings.JwtEmitScopes && scopes is not null)
            {
                claims.Add(new Claim("scope", string.Join(",", scopes)));
            }

            var businessId = await ResolveActiveBusinessIdAsync(userId, preferredBusinessId, ct).ConfigureAwait(false);
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

                effectiveDeviceId = deviceId.Trim();
            }

            if (settings.JwtSingleDeviceOnly)
            {
                await RevokeAllForUserAsync(userId, ct).ConfigureAwait(false);
            }

            var purpose = BuildRefreshPurpose(effectiveDeviceId);
            var refreshToken = CreateOpaqueToken();
            var refreshTokenHash = HashRefreshToken(refreshToken);
            var existingRow = await _db.Set<UserToken>()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Purpose == purpose, ct)
                .ConfigureAwait(false);

            if (existingRow is not null)
            {
                existingRow.Value = refreshTokenHash;
                existingRow.ExpiresAtUtc = refreshExp;
                existingRow.UsedAtUtc = null;
            }
            else
            {
                _db.Set<UserToken>().Add(new UserToken(userId, purpose, refreshTokenHash, refreshExp));
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return (accessToken, accessExp, refreshToken, refreshExp);
        }

        public async Task<Guid?> ValidateRefreshTokenAsync(string refreshToken, string? deviceId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return null;
            }

            var normalizedRefreshToken = refreshToken.Trim();
            var settings = await _db.Set<SiteSetting>()
                .AsNoTracking()
                .FirstAsync(x => !x.IsDeleted, ct)
                .ConfigureAwait(false);

            string? effectiveDeviceId = null;
            if (settings.JwtRequireDeviceBinding)
            {
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    return null;
                }

                effectiveDeviceId = deviceId.Trim();
            }

            var purpose = BuildRefreshPurpose(effectiveDeviceId);
            var refreshTokenHash = HashRefreshToken(normalizedRefreshToken);
            var nowUtc = _clock.UtcNow;
            var userId = await _db.Set<UserToken>()
                .AsNoTracking()
                .Where(
                    x => x.Purpose == purpose &&
                         (x.Value == refreshTokenHash || x.Value == normalizedRefreshToken) &&
                         x.UsedAtUtc == null &&
                         (x.ExpiresAtUtc == null || x.ExpiresAtUtc > nowUtc))
                .Select(x => (Guid?)x.UserId)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            return userId;
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken, string? deviceId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return;
            }

            var normalizedRefreshToken = refreshToken.Trim();
            var settings = await _db.Set<SiteSetting>()
                .AsNoTracking()
                .FirstAsync(x => !x.IsDeleted, ct)
                .ConfigureAwait(false);

            var tokens = _db.Set<UserToken>();
            var refreshTokenHash = HashRefreshToken(normalizedRefreshToken);
            UserToken? row;
            if (settings.JwtRequireDeviceBinding && !string.IsNullOrWhiteSpace(deviceId))
            {
                var purpose = BuildRefreshPurpose(deviceId.Trim());
                row = await tokens
                    .FirstOrDefaultAsync(
                        x => x.Purpose == purpose &&
                             x.UsedAtUtc == null &&
                             (x.Value == refreshTokenHash || x.Value == normalizedRefreshToken),
                        ct)
                    .ConfigureAwait(false);
            }
            else
            {
                row = await tokens
                    .FirstOrDefaultAsync(
                        x => x.UsedAtUtc == null &&
                             (x.Value == refreshTokenHash || x.Value == normalizedRefreshToken) &&
                             x.Purpose.StartsWith("JwtRefresh"),
                        ct)
                    .ConfigureAwait(false);
            }

            if (row?.UsedAtUtc is null && row is not null)
            {
                row.UsedAtUtc = _clock.UtcNow;
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
        }

        public async Task<int> RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
        {
            var nowUtc = _clock.UtcNow;
            var rows = await _db.Set<UserToken>()
                .Where(x => x.UserId == userId && x.Purpose.StartsWith("JwtRefresh"))
                .ToListAsync(ct)
                .ConfigureAwait(false);

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
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            return rows.Count;
        }

        private async Task<Guid?> ResolveActiveBusinessIdAsync(Guid userId, Guid? preferredBusinessId, CancellationToken ct)
        {
            if (preferredBusinessId.HasValue)
            {
                var preferredMatch = await (from m in _db.Set<BusinessMember>().AsNoTracking()
                                            join b in _db.Set<Business>().AsNoTracking() on m.BusinessId equals b.Id
                                            where m.UserId == userId
                                                  && m.BusinessId == preferredBusinessId.Value
                                                  && !m.IsDeleted
                                                  && m.IsActive
                                                  && !b.IsDeleted
                                                  && b.IsActive
                                            select (Guid?)m.BusinessId)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                if (preferredMatch.HasValue)
                {
                    return preferredMatch;
                }
            }

            return await (from m in _db.Set<BusinessMember>().AsNoTracking()
                          join b in _db.Set<Business>().AsNoTracking() on m.BusinessId equals b.Id
                          where m.UserId == userId
                                && !m.IsDeleted
                                && m.IsActive
                                && !b.IsDeleted
                                && b.IsActive
                          orderby m.BusinessId
                          select (Guid?)m.BusinessId)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
        }

        private static string ToUnixTimestampSeconds(DateTime utc)
        {
            var seconds = new DateTimeOffset(utc).ToUnixTimeSeconds();
            return seconds.ToString(CultureInfo.InvariantCulture);
        }

        private static byte[] GetKeyBytes(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("JWT signing key (SiteSetting.JwtSigningKey) is not configured.");
            }

            try
            {
                return EnsureMinimumSigningKeyLength(Convert.FromBase64String(key));
            }
            catch (FormatException)
            {
                return EnsureMinimumSigningKeyLength(Encoding.UTF8.GetBytes(key));
            }
        }

        private static byte[] EnsureMinimumSigningKeyLength(byte[] bytes)
        {
            if (bytes.Length < 32)
            {
                throw new InvalidOperationException(
                    "JWT signing key (SiteSetting.JwtSigningKey) must be at least 32 bytes for HS256.");
            }

            return bytes;
        }

        private static string CreateOpaqueToken()
        {
            Span<byte> bytes = stackalloc byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string HashRefreshToken(string refreshToken)
        {
            var bytes = Encoding.UTF8.GetBytes(refreshToken);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static string BuildRefreshPurpose(string? deviceId)
        {
            var normalizedDeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId.Trim();
            return normalizedDeviceId is null ? "JwtRefresh" : $"JwtRefresh:{normalizedDeviceId}";
        }
    }
}
