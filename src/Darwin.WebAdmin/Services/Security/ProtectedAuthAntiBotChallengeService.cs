using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Abstractions.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Darwin.WebAdmin.Services.Security
{
    public sealed class ProtectedAuthAntiBotChallengeService : IAuthAntiBotChallengeService
    {
        private const string Purpose = "Darwin.WebAdmin.AuthAntiBot.v1";
        private readonly IDataProtector _protector;
        private readonly IClock _clock;
        private readonly IOptionsMonitor<AuthAntiBotOptions> _options;

        public ProtectedAuthAntiBotChallengeService(
            IDataProtectionProvider dataProtection,
            IClock clock,
            IOptionsMonitor<AuthAntiBotOptions> options)
        {
            ArgumentNullException.ThrowIfNull(dataProtection);
            _protector = dataProtection.CreateProtector(Purpose);
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public string CreateChallengeToken()
        {
            var issuedAtTicks = new DateTimeOffset(_clock.UtcNow, TimeSpan.Zero).UtcTicks;
            var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            return _protector.Protect(string.Create(CultureInfo.InvariantCulture, $"{issuedAtTicks}:{nonce}"));
        }

        public Task<AuthAntiBotVerificationResult> VerifyAsync(AuthAntiBotCheck check, CancellationToken ct = default)
        {
            var options = _options.CurrentValue;
            if (!options.Enabled)
            {
                return Task.FromResult(AuthAntiBotVerificationResult.Success());
            }

            if (!string.IsNullOrEmpty(check.HoneypotValue))
            {
                return Task.FromResult(AuthAntiBotVerificationResult.Fail("Honeypot was filled."));
            }

            if (string.IsNullOrWhiteSpace(check.ChallengeToken))
            {
                return Task.FromResult(options.RequireChallengeToken
                    ? AuthAntiBotVerificationResult.Fail("Missing challenge token.")
                    : AuthAntiBotVerificationResult.Success());
            }

            if (!TryReadIssuedAt(check.ChallengeToken, out var issuedAtUtc))
            {
                return Task.FromResult(AuthAntiBotVerificationResult.Fail("Invalid challenge token."));
            }

            var age = new DateTimeOffset(_clock.UtcNow, TimeSpan.Zero) - issuedAtUtc;
            if (age < options.MinimumFormAge)
            {
                return Task.FromResult(AuthAntiBotVerificationResult.Fail("Form submitted too quickly."));
            }

            if (age > options.ChallengeTokenMaxAge)
            {
                return Task.FromResult(AuthAntiBotVerificationResult.Fail("Challenge token expired."));
            }

            return Task.FromResult(AuthAntiBotVerificationResult.Success());
        }

        private bool TryReadIssuedAt(string token, out DateTimeOffset issuedAtUtc)
        {
            issuedAtUtc = default;
            try
            {
                var payload = _protector.Unprotect(token);
                var separator = payload.IndexOf(':', StringComparison.Ordinal);
                if (separator <= 0)
                {
                    return false;
                }

                var ticksText = payload[..separator];
                if (!long.TryParse(ticksText, NumberStyles.None, CultureInfo.InvariantCulture, out var ticks))
                {
                    return false;
                }

                issuedAtUtc = new DateTimeOffset(ticks, TimeSpan.Zero);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
