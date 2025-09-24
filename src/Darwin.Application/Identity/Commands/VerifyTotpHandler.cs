using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Security;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Verifies a TOTP code against the latest unactivated secret; on success marks it activated and enables 2FA.
    /// </summary>
    public sealed class VerifyTotpHandler
    {
        private readonly IAppDbContext _db;
        private readonly ITotpService _totp;
        private readonly IClock _clock;
        private readonly IValidator<VerifyTotpDto> _validator;

        public VerifyTotpHandler(IAppDbContext db, ITotpService totp, IClock clock, IValidator<VerifyTotpDto> validator)
        {
            _db = db; _totp = totp; _clock = clock; _validator = validator;
        }

        public async Task<Result> HandleAsync(VerifyTotpDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.UserId && !u.IsDeleted, ct);
            if (user == null) return Result.Fail("User not found.");

            var pending = await _db.Set<UserTwoFactorSecret>()
                .Where(s => s.UserId == dto.UserId && s.ActivatedAtUtc == null)
                .OrderByDescending(s => s.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);

            if (pending == null) return Result.Fail("No pending TOTP secret.");

            if (!_totp.VerifyCode(pending.SecretBase32, dto.Code))
                return Result.Fail("Invalid code.");

            pending.Activate(_clock.UtcNow);
            user.TwoFactorEnabled = true;
            await _db.SaveChangesAsync(ct);

            return Result.Ok();
        }
    }
}
