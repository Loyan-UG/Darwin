using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Allows an administrator to mark a user's primary email as confirmed when support operations require it.
    /// </summary>
    public sealed class ConfirmUserEmailByAdminHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<UserAdminActionDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        /// <summary>
        /// Initializes a new instance of <see cref="ConfirmUserEmailByAdminHandler"/>.
        /// </summary>
        public ConfirmUserEmailByAdminHandler(IAppDbContext db, IValidator<UserAdminActionDto> validator, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Marks the target user's email as confirmed. The operation is idempotent.
        /// </summary>
        public async Task<Result> HandleAsync(UserAdminActionDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct);

            if (user is null)
            {
                return Result.Fail(_localizer["UserNotFound"]);
            }

            if (user.EmailConfirmed)
            {
                return Result.Ok();
            }

            user.EmailConfirmed = true;
            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }

    /// <summary>
    /// Allows an administrator to lock a user account for operational support or abuse prevention.
    /// </summary>
    public sealed class LockUserByAdminHandler
    {
        private readonly IAppDbContext _db;
        private readonly IJwtTokenService _jwt;
        private readonly IClock _clock;
        private readonly ISecurityStampService _stamps;
        private readonly IValidator<UserAdminActionDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        /// <summary>
        /// Initializes a new instance of <see cref="LockUserByAdminHandler"/>.
        /// </summary>
        public LockUserByAdminHandler(
            IAppDbContext db,
            IJwtTokenService jwt,
            IClock clock,
            ISecurityStampService stamps,
            IValidator<UserAdminActionDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _jwt = jwt ?? throw new ArgumentNullException(nameof(jwt));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _stamps = stamps ?? throw new ArgumentNullException(nameof(stamps));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Locks the target user until explicitly unlocked. System users are protected from this operation.
        /// </summary>
        public async Task<Result> HandleAsync(UserAdminActionDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct);

            if (user is null)
            {
                return Result.Fail(_localizer["UserNotFound"]);
            }

            if (user.IsSystem)
            {
                return Result.Fail(_localizer["SystemUsersCannotBeLocked"]);
            }

            var lockoutEndUtc = _clock.UtcNow.AddYears(10);
            if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value >= lockoutEndUtc.AddDays(-1))
            {
                return Result.Ok();
            }

            user.LockoutEndUtc = lockoutEndUtc;
            user.AccessFailedCount = 0;
            user.SecurityStamp = _stamps.NewStamp();
            await _db.SaveChangesAsync(ct);
            await _jwt.RevokeAllForUserAsync(user.Id, ct);
            return Result.Ok();
        }
    }

    /// <summary>
    /// Allows an administrator to remove a prior operational lockout from a user account.
    /// </summary>
    public sealed class UnlockUserByAdminHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<UserAdminActionDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        /// <summary>
        /// Initializes a new instance of <see cref="UnlockUserByAdminHandler"/>.
        /// </summary>
        public UnlockUserByAdminHandler(IAppDbContext db, IValidator<UserAdminActionDto> validator, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Clears the current lockout and resets failed access counters. The operation is idempotent.
        /// </summary>
        public async Task<Result> HandleAsync(UserAdminActionDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var user = await _db.Set<User>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct);

            if (user is null)
            {
                return Result.Fail(_localizer["UserNotFound"]);
            }

            if (user.LockoutEndUtc is null && user.AccessFailedCount == 0)
            {
                return Result.Ok();
            }

            user.LockoutEndUtc = null;
            user.AccessFailedCount = 0;
            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
