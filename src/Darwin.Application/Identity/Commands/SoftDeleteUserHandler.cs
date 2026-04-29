using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Orders;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Performs a soft delete of a user. Records flagged as system cannot be deleted.
    /// </summary>
    public sealed class SoftDeleteUserHandler
    {
        private readonly IAppDbContext _db;
        private readonly IJwtTokenService _jwt;
        private readonly ISecurityStampService _stamps;
        private readonly IValidator<UserDeleteDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public SoftDeleteUserHandler(
            IAppDbContext db,
            IJwtTokenService jwt,
            ISecurityStampService stamps,
            IValidator<UserDeleteDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _jwt = jwt ?? throw new ArgumentNullException(nameof(jwt));
            _stamps = stamps ?? throw new ArgumentNullException(nameof(stamps));
            _validator = validator;
            _localizer = localizer;
        }

        public async Task<Result<UserDeleteOutcomeDto>> HandleAsync(UserDeleteDto dto, CancellationToken ct = default)
        {
            var validation = await _validator.ValidateAsync(dto, ct);
            if (!validation.IsValid)
                return Result<UserDeleteOutcomeDto>.Fail(dto.RowVersion is null || dto.RowVersion.Length == 0
                    ? _localizer["RowVersionRequired"]
                    : _localizer["InvalidDeleteRequest"]);

            var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == dto.Id && !u.IsDeleted, ct);
            if (user is null)
                return Result<UserDeleteOutcomeDto>.Fail(_localizer["UserNotFound"]);

            var currentVersion = user.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(dto.RowVersion))
                return Result<UserDeleteOutcomeDto>.Fail(_localizer["ConcurrencyConflict"]);

            if (user.IsSystem)
                return Result<UserDeleteOutcomeDto>.Fail(_localizer["SystemUsersCannotBeDeleted"]);

            var hasOrderHistory = await _db.Set<Order>()
                .AsNoTracking()
                .AnyAsync(x => x.UserId == user.Id, ct)
                .ConfigureAwait(false);

            user.IsActive = false;
            user.SecurityStamp = _stamps.NewStamp();

            var outcome = new UserDeleteOutcomeDto();
            if (hasOrderHistory)
            {
                outcome.WasDeactivatedDueToReferences = true;
            }
            else
            {
                user.IsDeleted = true;
                outcome.WasSoftDeleted = true;
            }

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<UserDeleteOutcomeDto>.Fail(_localizer["ConcurrencyConflict"]);
            }

            await _jwt.RevokeAllForUserAsync(user.Id, ct);

            return Result<UserDeleteOutcomeDto>.Ok(outcome);
        }
    }
}
