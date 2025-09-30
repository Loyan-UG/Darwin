using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Services;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Validates a TOTP code during a login flow for a user who has 2FA enabled.
    /// </summary>
    public sealed class VerifyTotpForLoginHandler
    {
        private readonly IAppDbContext _db;

        public VerifyTotpForLoginHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Verifies the provided TOTP code against the user's active TOTP secret.
        /// </summary>
        public async Task<Result> HandleAsync(TotpVerifyDto dto, CancellationToken ct = default)
        {
            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.UserId && !u.IsDeleted, ct);
            if (user is null) return Result.Fail("User not found.");

            if (!user.TwoFactorEnabled) return Result.Fail("Two-factor authentication is not enabled.");

            var secret = await _db.Set<UserTwoFactorSecret>()
                .OrderByDescending(s => s.ActivatedAtUtc)
                .FirstOrDefaultAsync(s => s.UserId == user.Id && s.ActivatedAtUtc != null && !s.IsDeleted, ct);

            if (secret is null) return Result.Fail("No active TOTP secret.");

            var ok = TotpUtility.VerifyTotpCode(secret.SecretBase32, DateTime.UtcNow, dto.Code, allowedDriftSteps: 1);
            if (!ok) return Result.Fail("Invalid TOTP code.");

            return Result.Ok();
        }
    }
}
