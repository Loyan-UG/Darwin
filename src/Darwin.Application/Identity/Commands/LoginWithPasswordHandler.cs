using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Security;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Authenticates a user with email/password and issues access/refresh tokens.
    /// Applies simple rate limiting per email/IP key to reduce brute-force attempts.
    /// </summary>
    public sealed class LoginWithPasswordHandler
    {
        private readonly IAppDbContext _db;
        private readonly IJwtTokenService _jwt;
        private readonly ILoginRateLimiter _limiter;
        private readonly IUserPasswordHasher _hasher;

        public LoginWithPasswordHandler(IAppDbContext db, IJwtTokenService jwt, ILoginRateLimiter limiter, IUserPasswordHasher hasher)
        {
            _db = db;
            _jwt = jwt;
            _limiter = limiter;
            _hasher = hasher;
        }

        public async Task<Result<AuthResultDto>> HandleAsync(PasswordLoginRequestDto dto, string rateKey, CancellationToken ct = default)
        {
            // 1) Rate limit (window: 60s, max 5 attempts)
            if (!await _limiter.IsAllowedAsync(rateKey, maxAttempts: 5, windowSeconds: 60, ct))
                return Result<AuthResultDto>.Fail("Too many attempts. Please try again.");

            var normalizedEmail = dto.Email.Trim().ToUpperInvariant();

            // 2) Load user
            var user = await _db.Set<User>()
                .Where(u => u.NormalizedEmail == normalizedEmail && !u.IsDeleted && u.IsActive)
                .FirstOrDefaultAsync(ct);

            if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                await _limiter.RecordAsync(rateKey, ct);
                return Result<AuthResultDto>.Fail("Invalid credentials.");
            }

            // 3) Verify password (Argon2)
            var ok = _hasher.Verify(user.PasswordHash, dto.PasswordPlain);
            if (!ok)
            {
                await _limiter.RecordAsync(rateKey, ct);
                return Result<AuthResultDto>.Fail("Invalid credentials.");
            }

            // 4) Issue access and refresh tokens. DeviceId is forwarded so that the JwtTokenService
            // can enforce SiteSetting.JwtRequireDeviceBinding and SiteSetting.JwtSingleDeviceOnly.
            var (access, accessExp, refresh, refreshExp) =
                _jwt.IssueTokens(user.Id, user.Email, dto.DeviceId, scopes: null);


            var result = new AuthResultDto
            {
                AccessToken = access,
                AccessTokenExpiresAtUtc = accessExp,
                RefreshToken = refresh,
                RefreshTokenExpiresAtUtc = refreshExp,
                UserId = user.Id,
                Email = user.Email
            };

            return Result<AuthResultDto>.Ok(result);
        }
    }
}
