using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Darwin.WebApi.Security
{
    /// <summary>
    /// Provides JWT signing/validation parameters by reading SiteSetting from the database.
    /// Uses a small in-memory cache to avoid hitting the database on every token validation.
    /// </summary>
    public sealed class JwtSigningParametersProvider
    {
        private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<JwtSigningParametersProvider> _logger;

        private readonly object _sync = new();
        private DateTime _lastReadUtc;
        private CachedSigningParameters? _cache;

        /// <summary>
        /// Initializes a new instance of <see cref="JwtSigningParametersProvider"/>.
        /// </summary>
        /// <param name="scopeFactory">Scope factory used to resolve a scoped IAppDbContext.</param>
        /// <param name="logger">Logger for diagnostics.</param>
        public JwtSigningParametersProvider(IServiceScopeFactory scopeFactory, ILogger<JwtSigningParametersProvider> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets current JWT issuer/audience/clock skew and signing keys (current + previous if any).
        /// Values are read from SiteSetting and cached for a short duration.
        /// </summary>
        /// <returns>Signing parameters to be used in TokenValidationParameters.</returns>
        public CachedSigningParameters GetParameters()
        {
            lock (_sync)
            {
                var nowUtc = DateTime.UtcNow;

                if (_cache is not null && (nowUtc - _lastReadUtc) <= DefaultCacheDuration)
                {
                    return _cache;
                }

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

                var s = db.Set<SiteSetting>().AsNoTracking().FirstOrDefault();
                if (s is null)
                {
                    throw new InvalidOperationException("SiteSetting row not found. JWT validation cannot be configured.");
                }

                if (s.JwtEnabled == false)
                {
                    throw new InvalidOperationException("JWT is disabled by SiteSetting.");
                }

                var keys = new List<SecurityKey>
                {
                    new SymmetricSecurityKey(GetKeyBytes(s.JwtSigningKey))
                };

                if (!string.IsNullOrWhiteSpace(s.JwtPreviousSigningKey))
                {
                    keys.Add(new SymmetricSecurityKey(GetKeyBytes(s.JwtPreviousSigningKey)));
                }

                var skewSeconds = s.JwtClockSkewSeconds;
                if (skewSeconds < 0) skewSeconds = 0;

                _cache = new CachedSigningParameters(
                    issuer: s.JwtIssuer ?? "Darwin",
                    audience: s.JwtAudience ?? "Darwin.PublicApi",
                    clockSkew: TimeSpan.FromSeconds(skewSeconds),
                    signingKeys: keys);

                _lastReadUtc = nowUtc;

                _logger.LogDebug(
                    "JWT signing parameters refreshed from SiteSetting. Issuer={Issuer}, Audience={Audience}, Keys={KeyCount}, SkewSeconds={SkewSeconds}",
                    _cache.Issuer, _cache.Audience, _cache.SigningKeys.Count, skewSeconds);

                return _cache;
            }
        }

        /// <summary>
        /// Converts a signing key string into bytes.
        /// Supports base64 or raw UTF8 strings.
        /// </summary>
        private static byte[] GetKeyBytes(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("JwtSigningKey is missing in SiteSetting.");
            }

            try
            {
                return Convert.FromBase64String(key);
            }
            catch
            {
                return System.Text.Encoding.UTF8.GetBytes(key);
            }
        }

        /// <summary>
        /// Represents a snapshot of JWT signing parameters used for validation.
        /// </summary>
        public sealed class CachedSigningParameters
        {
            public CachedSigningParameters(string issuer, string audience, TimeSpan clockSkew, IReadOnlyList<SecurityKey> signingKeys)
            {
                Issuer = issuer;
                Audience = audience;
                ClockSkew = clockSkew;
                SigningKeys = signingKeys;
            }

            /// <summary>The expected token issuer.</summary>
            public string Issuer { get; }

            /// <summary>The expected token audience.</summary>
            public string Audience { get; }

            /// <summary>Allowed clock skew for lifetime validation.</summary>
            public TimeSpan ClockSkew { get; }

            /// <summary>Current + previous signing keys.</summary>
            public IReadOnlyList<SecurityKey> SigningKeys { get; }
        }
    }
}
