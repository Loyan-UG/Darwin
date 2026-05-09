using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Security;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Auth.Commands
{
    /// <summary>
    /// Validates credentials and returns a SignInResultDto.
    /// NOTE: Creating the authentication cookie / claims principal is NOT done here.
    /// Web layer should take UserId + SecurityStamp and issue the cookie.
    /// </summary>
    public sealed class SignInHandler
    {
        private readonly IAppDbContext _db;
        private readonly IUserPasswordHasher _hasher;
        private readonly IValidator<SignInDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        private readonly IAuthAntiBotVerifier _antiBot;
        private readonly ILoginRateLimiter _limiter;
        private readonly IClock _clock;

        public SignInHandler(
            IAppDbContext db,
            IUserPasswordHasher hasher,
            IValidator<SignInDto> validator,
            IStringLocalizer<ValidationResource> localizer,
            IAuthAntiBotVerifier antiBot,
            ILoginRateLimiter limiter,
            IClock clock)
        {
            _db = db;
            _hasher = hasher;
            _validator = validator;
            _localizer = localizer;
            _antiBot = antiBot;
            _limiter = limiter;
            _clock = clock;
        }

        public async Task<SignInResultDto> HandleAsync(SignInDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var normalizedEmail = dto.Email.Trim().ToUpperInvariant();
            var rateKey = BuildRateKey(dto.ClientIpAddress, normalizedEmail);
            if (!await _limiter.IsAllowedAsync(rateKey, maxAttempts: 8, windowSeconds: 300, ct).ConfigureAwait(false))
            {
                return new SignInResultDto { Succeeded = false, FailureReason = _localizer["TooManyAttemptsPleaseTryAgain"] };
            }

            var antiBotResult = await _antiBot.VerifyAsync(
                new AuthAntiBotCheck
                {
                    ChallengeToken = dto.AntiBotToken,
                    HoneypotValue = dto.AntiBotHoneypot,
                    ClientIpAddress = dto.ClientIpAddress,
                    UserAgent = dto.UserAgent
                },
                ct).ConfigureAwait(false);

            if (!antiBotResult.Succeeded)
            {
                await _limiter.RecordAsync(rateKey, ct).ConfigureAwait(false);
                return new SignInResultDto { Succeeded = false, FailureReason = _localizer["InvalidCredentials"] };
            }

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail && !u.IsDeleted, ct);
            if (user == null || !user.IsActive)
            {
                await _limiter.RecordAsync(rateKey, ct).ConfigureAwait(false);
                return new SignInResultDto { Succeeded = false, FailureReason = _localizer["InvalidCredentials"] };
            }

            if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > _clock.UtcNow)
            {
                await _limiter.RecordAsync(rateKey, ct).ConfigureAwait(false);
                return new SignInResultDto { Succeeded = false, FailureReason = _localizer["AccountLocked"] };
            }

            if (!_hasher.Verify(user.PasswordHash, dto.Password))
            {
                await _limiter.RecordAsync(rateKey, ct).ConfigureAwait(false);
                return new SignInResultDto { Succeeded = false, FailureReason = _localizer["InvalidCredentials"] };
            }

            if (!user.EmailConfirmed)
            {
                return new SignInResultDto { Succeeded = false, FailureReason = _localizer["EmailAddressNotConfirmed"] };
            }

            var twoFactorEnabled = user.TwoFactorEnabled;
            if (twoFactorEnabled)
            {
                return new SignInResultDto
                {
                    Succeeded = false,
                    RequiresTwoFactor = true,
                    TwoFactorDelivery = "TOTP", // For now we standardize on TOTP app
                    UserId = user.Id
                };
            }

            return new SignInResultDto
            {
                Succeeded = true,
                UserId = user.Id,
                SecurityStamp = user.SecurityStamp
            };
        }

        private static string BuildRateKey(string? ipAddress, string normalizedEmail)
        {
            var ip = string.IsNullOrWhiteSpace(ipAddress) ? "unknown" : ipAddress.Trim();
            var emailHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail)));
            return $"webadmin-login:{ip}:{emailHash}";
        }
    }
}
