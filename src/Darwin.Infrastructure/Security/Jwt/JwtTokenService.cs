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

        public JwtTokenService(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
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

            var nowUtc = DateTime.UtcNow;
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
            var existingRow = await _db.Set<UserToken>()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Purpose == purpose, ct)
                .ConfigureAwait(false);

            if (existingRow is not null)
            {
                existingRow.Value = refreshToken;
                existingRow.ExpiresAtUtc = refreshExp;
                existingRow.UsedAtUtc = null;
            }
            else
            {
                _db.Set<UserToken>().Add(new UserToken(userId, purpose, refreshToken, refreshExp));
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
            var row = await _db.Set<UserToken>()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Purpose == purpose && x.Value == refreshToken && x.UsedAtUtc == null,
                    ct)
                .ConfigureAwait(false);

            if (row is null || (row.ExpiresAtUtc.HasValue && row.ExpiresAtUtc.Value < DateTime.UtcNow))
            {
                return null;
            }

            return row.UserId;
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken, string? deviceId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return;
            }

            var settings = await _db.Set<SiteSetting>()
                .AsNoTracking()
                .FirstAsync(x => !x.IsDeleted, ct)
                .ConfigureAwait(false);

            var tokens = _db.Set<UserToken>();
            UserToken? row;
            if (settings.JwtRequireDeviceBinding && !string.IsNullOrWhiteSpace(deviceId))
            {
                var purpose = BuildRefreshPurpose(deviceId.Trim());
                row = await tokens.FirstOrDefaultAsync(x => x.Purpose == purpose && x.Value == refreshToken, ct).ConfigureAwait(false);
            }
            else
            {
                row = await tokens.FirstOrDefaultAsync(x => x.Value == refreshToken && x.Purpose.StartsWith("JwtRefresh"), ct).ConfigureAwait(false);
            }

            if (row?.UsedAtUtc is null && row is not null)
            {
                row.UsedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
        }

        public async Task<int> RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
        {
            var nowUtc = DateTime.UtcNow;
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
                var preferredMatch = await (from m in _db.Set<BusinessMember>()
                                            join b in _db.Set<Business>() on m.BusinessId equals b.Id
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

            return await (from m in _db.Set<BusinessMember>()
                          join b in _db.Set<Business>() on m.BusinessId equals b.Id
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

        private static string BuildRefreshPurpose(string? deviceId)
        {
            var normalizedDeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId.Trim();
            return normalizedDeviceId is null ? "JwtRefresh" : $"JwtRefresh:{normalizedDeviceId}";
        }
    }
}
