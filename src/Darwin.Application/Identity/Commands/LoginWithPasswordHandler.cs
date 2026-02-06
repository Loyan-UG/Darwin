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

        /// <summary>
        /// Handles password login:
        /// - Applies simple rate limiting via ILoginRateLimiter.
        /// - Loads user by normalized email.
        /// - Verifies password using IUserPasswordHasher.
        /// - Issues access + refresh tokens via IJwtTokenService.
        /// The method catches OperationCanceledException so cancelled requests are surfaced
        /// as controlled failures instead of bubbling unhandled exceptions.
        /// </summary>
        public async Task<Result<AuthResultDto>> HandleAsync(
            PasswordLoginRequestDto dto,
            string rateKey,
            CancellationToken ct = default)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(rateKey)) rateKey = string.Empty;

            try
            {
                // 1) Rate limit (window: 60s, max 5 attempts)
                if (!await _limiter.IsAllowedAsync(rateKey, maxAttempts: 5, windowSeconds: 60, ct).ConfigureAwait(false))
                    return Result<AuthResultDto>.Fail("Too many attempts. Please try again.");

                var normalizedEmail = (dto.Email ?? string.Empty).Trim().ToUpperInvariant();

                // 2) Load user
                var user = await _db.Set<User>()
                    .Where(u => u.NormalizedEmail == normalizedEmail && !u.IsDeleted && u.IsActive)
                    .FirstOrDefaultAsync(ct).ConfigureAwait(false);

                if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
                {
                    // record attempt and return generic error
                    await _limiter.RecordAsync(rateKey, ct).ConfigureAwait(false);
                    return Result<AuthResultDto>.Fail("Invalid credentials.");
                }

                // 3) Verify password (Argon2)
                var ok = _hasher.Verify(user.PasswordHash, dto.PasswordPlain);
                if (!ok)
                {
                    await _limiter.RecordAsync(rateKey, ct).ConfigureAwait(false);
                    return Result<AuthResultDto>.Fail("Invalid credentials.");
                }

                // 4) Issue access and refresh tokens.
                // DeviceId forwarded so JwtTokenService can enforce device-binding and single-device policies.
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
            catch (OperationCanceledException)
            {
                // The client disconnected or cancelled the request (e.g., network/timeout).
                // Return a controlled failure so middleware/logging is clearer.
                return Result<AuthResultDto>.Fail("Request cancelled by client or timed out during login.");
            }
        }
    }
}
