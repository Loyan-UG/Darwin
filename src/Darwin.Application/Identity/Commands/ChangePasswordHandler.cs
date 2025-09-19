using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Validates the current password, sets a new password hash and rotates security stamp.
    /// </summary>
    public sealed class ChangePasswordHandler
    {
        private readonly IAppDbContext _db;
        private readonly IUserPasswordHasher _hasher;
        private readonly ISecurityStampService _stamps;
        private readonly IValidator<UserChangePasswordDto> _validator;

        public ChangePasswordHandler(IAppDbContext db, IUserPasswordHasher hasher, ISecurityStampService stamps, IValidator<UserChangePasswordDto> validator)
        {
            _db = db; _hasher = hasher; _stamps = stamps; _validator = validator;
        }

        public async Task<Result> HandleAsync(UserChangePasswordDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var u = await _db.Set<User>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct);
            if (u == null) return Result.Fail("User not found.");

            if (!_hasher.Verify(u.PasswordHash, dto.CurrentPassword))
                return Result.Fail("Current password is incorrect.");

            u.PasswordHash = _hasher.Hash(dto.NewPassword);
            u.SecurityStamp = _stamps.NewStamp();

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
