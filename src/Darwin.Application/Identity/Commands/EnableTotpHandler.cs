using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Application.Identity.Services;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Validates the initial TOTP code for the latest unactivated secret and enables 2FA.
    /// </summary>
    public sealed class EnableTotpHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IValidator<TotpEnableDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public EnableTotpHandler(
            IAppDbContext db,
            IStringLocalizer<ValidationResource>? localizer = null,
            IClock? clock = null)
            : this(db, new TotpEnableValidator(), localizer, clock)
        {
        }

        public EnableTotpHandler(
            IAppDbContext db,
            IValidator<TotpEnableDto> validator,
            IStringLocalizer<ValidationResource>? localizer = null,
            IClock? clock = null)
        {
            _db = db;
            _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
            _validator = validator;
            _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
        }

        /// <summary>
        /// Confirms the user's most recent unactivated secret by validating the provided code.
        /// </summary>
        public async Task<Result> HandleAsync(TotpEnableDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.UserId && !u.IsDeleted, ct);
            if (user is null) return Result.Fail(_localizer["UserNotFound"]);

            var secret = await _db.Set<UserTwoFactorSecret>()
                .OrderByDescending(s => s.CreatedAtUtc)
                .FirstOrDefaultAsync(s => s.UserId == user.Id && s.ActivatedAtUtc == null && !s.IsDeleted, ct);

            if (secret is null) return Result.Fail(_localizer["NoPendingTotpSecret"]);

            var now = _clock.UtcNow;
            var ok = TotpUtility.VerifyTotpCode(secret.SecretBase32, now, dto.Code, allowedDriftSteps: 1);
            if (!ok) return Result.Fail(_localizer["InvalidTotpCode"]);

            secret.Activate(now);
            user.TwoFactorEnabled = true;

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}

