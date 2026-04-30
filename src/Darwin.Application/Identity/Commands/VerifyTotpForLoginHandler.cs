using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Services;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Validates a TOTP code during a login flow for a user who has 2FA enabled.
    /// </summary>
    public sealed class VerifyTotpForLoginHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IValidator<TotpVerifyDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public VerifyTotpForLoginHandler(
            IAppDbContext db,
            IClock clock,
            IValidator<TotpVerifyDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _clock = clock;
            _validator = validator;
            _localizer = localizer;
        }

        /// <summary>
        /// Verifies the provided TOTP code against the user's active TOTP secret.
        /// </summary>
        public async Task<Result> HandleAsync(TotpVerifyDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.UserId && !u.IsDeleted, ct);
            if (user is null) return Result.Fail(_localizer["UserNotFound"]);

            if (!user.TwoFactorEnabled) return Result.Fail(_localizer["TwoFactorAuthenticationNotEnabled"]);

            var secret = await _db.Set<UserTwoFactorSecret>()
                .OrderByDescending(s => s.ActivatedAtUtc)
                .FirstOrDefaultAsync(s => s.UserId == user.Id && s.ActivatedAtUtc != null && !s.IsDeleted, ct);

            if (secret is null) return Result.Fail(_localizer["NoActiveTotpSecret"]);

            var ok = TotpUtility.VerifyTotpCode(secret.SecretBase32, _clock.UtcNow, dto.Code, allowedDriftSteps: 1);
            if (!ok) return Result.Fail(_localizer["InvalidTotpCode"]);

            return Result.Ok();
        }
    }
}
