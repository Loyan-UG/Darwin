using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Darwin.Infrastructure.Security.Jwt
{
    /// <summary>
    /// Default JWT implementation. Reads signing parameters from SiteSetting.
    /// Persists refresh tokens as UserToken rows with purpose "JwtRefresh[:deviceId]".
    /// </summary>
    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly IAppDbContext _db;

        public JwtTokenService(IAppDbContext db) => _db = db;

        public (string accessToken, DateTime expiresAtUtc, string refreshToken, DateTime refreshExpiresAtUtc)
            IssueTokens(Guid userId, string email, IEnumerable<string>? scopes = null)
        {
            // Read settings (single row model)
            var s = _db.Set<SiteSetting>().AsNoTracking().First();
            if (s.JwtEnabled == false) throw new InvalidOperationException("JWT is disabled by SiteSetting.");

            var nowUtc = DateTime.UtcNow;
            var accessExp = nowUtc.AddMinutes(Math.Max(5, s.JwtAccessTokenMinutes));
            var refreshExp = nowUtc.AddDays(Math.Max(1, s.JwtRefreshTokenDays));

            // Build JWT
            var key = new SymmetricSecurityKey(GetKeyBytes(s.JwtSigningKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.Email, email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new(JwtRegisteredClaimNames.Iat, ToUnix(nowUtc), ClaimValueTypes.Integer64),
            };

            if (s.JwtEmitScopes && scopes is not null)
                claims.Add(new Claim("scope", string.Join(",", scopes)));

            var jwt = new JwtSecurityToken(
                issuer: s.JwtIssuer,
                audience: s.JwtAudience,
                claims: claims,
                notBefore: nowUtc,
                expires: accessExp,
                signingCredentials: creds);

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);

            // Create opaque refresh token and persist
            var refresh = CreateOpaqueToken();
            var ut = new UserToken
            (
                userId,
                BuildRefreshPurpose(null), // device-less by default; attach deviceId in API layer if supplied
                refresh,
                refreshExp
            );

            _db.Set<UserToken>().Add(ut);
            _db.SaveChangesAsync().GetAwaiter().GetResult();

            return (token, accessExp, refresh, refreshExp);
        }

        public Guid? ValidateRefreshToken(string refreshToken, string? deviceId)
        {
            var purpose = BuildRefreshPurpose(deviceId);
            var row = _db.Set<UserToken>()
                         .AsNoTracking()
                         .Where(x => x.Purpose == purpose && x.Value == refreshToken && x.UsedAtUtc == null)
                         .FirstOrDefault();

            if (row is null) return null;
            if (row.ExpiresAtUtc.HasValue && row.ExpiresAtUtc.Value < DateTime.UtcNow) return null;

            return row.UserId;
        }

        public void RevokeRefreshToken(string refreshToken, string? deviceId)
        {
            var purpose = BuildRefreshPurpose(deviceId);
            var row = _db.Set<UserToken>().Where(x => x.Purpose == purpose && x.Value == refreshToken).FirstOrDefault();
            if (row is null) return;
            row.UsedAtUtc = DateTime.UtcNow;
            _db.SaveChangesAsync().GetAwaiter().GetResult();
        }

        public int RevokeAllForUser(Guid userId)
        {
            var rows = _db.Set<UserToken>().Where(x => x.UserId == userId && x.Purpose.StartsWith("JwtRefresh")).ToList();
            if (rows.Count == 0) return 0;
            var now = DateTime.UtcNow;
            foreach (var r in rows) r.UsedAtUtc = now;
            _db.SaveChangesAsync().GetAwaiter().GetResult();
            return rows.Count;
        }

        // Helpers
        private static string ToUnix(DateTime dt) => ((long)(dt - DateTime.UnixEpoch).TotalSeconds).ToString();

        private static byte[] GetKeyBytes(string? key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("JwtSigningKey is not configured.");
            // Allow plain or Base64
            try { return Convert.FromBase64String(key); } catch { /* ignore */ }
            return System.Text.Encoding.UTF8.GetBytes(key);
        }

        private static string CreateOpaqueToken()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static string BuildRefreshPurpose(string? deviceId) =>
            string.IsNullOrWhiteSpace(deviceId) ? "JwtRefresh" : $"JwtRefresh:{deviceId}";
    }
}
