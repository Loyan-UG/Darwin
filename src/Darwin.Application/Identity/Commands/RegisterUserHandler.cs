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
    /// Self-service registration (or admin-create). This is similar to CreateUser but
    /// exists in Auth namespace to reflect flows from UI. It can attach a default role.
    /// </summary>
    public sealed class RegisterUserHandler
    {
        private readonly IAppDbContext _db;
        private readonly IUserPasswordHasher _hasher;
        private readonly ISecurityStampService _stamps;
        private readonly IValidator<UserCreateDto> _validator;

        public RegisterUserHandler(IAppDbContext db, IUserPasswordHasher hasher, ISecurityStampService stamps, IValidator<UserCreateDto> validator)
        {
            _db = db; _hasher = hasher; _stamps = stamps; _validator = validator;
        }

        public async Task<Result<Guid>> HandleAsync(UserCreateDto dto, Guid? defaultRoleId = null, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var exists = await _db.Set<User>().AnyAsync(u => u.Email == dto.Email && !u.IsDeleted, ct);
            if (exists) return Result<Guid>.Fail("Email already in use.");

            var u = new User
            {
                Email = dto.Email,
                PasswordHash = _hasher.Hash(dto.Password),
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Locale = dto.Locale,
                Timezone = dto.Timezone,
                Currency = dto.Currency,
                IsActive = dto.IsActive,
                IsSystem = dto.IsSystem,
                SecurityStamp = _stamps.NewStamp()
            };
            _db.Set<User>().Add(u);

            if (defaultRoleId.HasValue)
            {
                // TODO: Consider verifying that role exists and is not deleted; idempotent add.
                _db.Set<UserRole>().Add(
                    new UserRole { UserId = u.Id, RoleId = defaultRoleId.Value });
            }

            await _db.SaveChangesAsync(ct);
            return Result<Guid>.Ok(u.Id);
        }
    }
}
