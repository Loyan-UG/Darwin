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
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Self-service registration (or admin-create) with optional default role assignment.
    /// Uses domain constructors to respect invariants (no object initializer on sealed entities).
    /// </summary>
    public sealed class RegisterUserHandler
    {
        private readonly IAppDbContext _db;
        private readonly IUserPasswordHasher _hasher;
        private readonly ISecurityStampService _stamps;
        private readonly IValidator<UserCreateDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public RegisterUserHandler(IAppDbContext db, IUserPasswordHasher hasher, ISecurityStampService stamps, IValidator<UserCreateDto> validator, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db; _hasher = hasher; _stamps = stamps; _validator = validator; _localizer = localizer;
        }

        /// <summary>
        /// Registers a new user and (optionally) assigns a default role.
        /// </summary>
        /// <param name="dto">User creation payload (email, password, profile prefs).</param>
        /// <param name="defaultRoleId">Optional role id to attach after creation.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<Result<Guid>> HandleAsync(UserCreateDto dto, Guid? defaultRoleId = null, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var normalizedEmail = dto.Email.Trim().ToUpperInvariant();
            var exists = await _db.Set<User>().AnyAsync(
                u => (u.NormalizedEmail == normalizedEmail || u.NormalizedUserName == normalizedEmail) && !u.IsDeleted,
                ct);
            if (exists) return Result<Guid>.Fail(_localizer["EmailAlreadyInUse"]);

            var passwordHash = _hasher.Hash(dto.Password);
            var user = new User(dto.Email, passwordHash, _stamps.NewStamp())
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneE164 = dto.PhoneE164,
                Locale = dto.Locale,
                Timezone = dto.Timezone,
                Currency = dto.Currency,
                IsActive = dto.IsActive,
                IsSystem = dto.IsSystem
            };

            _db.Set<User>().Add(user);

            if (defaultRoleId.HasValue)
            {
                // idempotent check (optional): confirm role exists & not deleted
                var roleExists = await _db.Set<Role>().AnyAsync(r => r.Id == defaultRoleId.Value && !r.IsDeleted, ct);
                if (!roleExists) return Result<Guid>.Fail(_localizer["DefaultRoleNotFound"]);

                _db.Set<UserRole>().Add(new UserRole(user.Id, defaultRoleId.Value));
            }

            await _db.SaveChangesAsync(ct);
            return Result<Guid>.Ok(user.Id);
        }
    }
}
