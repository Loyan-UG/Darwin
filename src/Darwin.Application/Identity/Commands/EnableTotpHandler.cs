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
    /// Validates the initial TOTP code for the latest unactivated secret and enables 2FA.
    /// </summary>
    public sealed class EnableTotpHandler
    {
        private readonly IAppDbContext _db;

        public EnableTotpHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Confirms the user's most recent unactivated secret by validating the provided code.
        /// </summary>
        public async Task<Result> HandleAsync(TotpEnableDto dto, CancellationToken ct = default)
        {
            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.UserId && !u.IsDeleted, ct);
            if (user is null) return Result.Fail("User not found.");

            var secret = await _db.Set<UserTwoFactorSecret>()
                .OrderByDescending(s => s.CreatedAtUtc)
                .FirstOrDefaultAsync(s => s.UserId == user.Id && s.ActivatedAtUtc == null && !s.IsDeleted, ct);

            if (secret is null) return Result.Fail("No pending TOTP secret.");

            var ok = TotpUtility.VerifyTotpCode(secret.SecretBase32, DateTime.UtcNow, dto.Code, allowedDriftSteps: 1);
            if (!ok) return Result.Fail("Invalid TOTP code.");

            secret.Activate(DateTime.UtcNow);
            user.TwoFactorEnabled = true;

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
