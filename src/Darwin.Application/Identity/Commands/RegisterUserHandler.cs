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
    /// Self-service registration (or admin-create). Attaches default role if provided.
    /// </summary>
    public sealed class RegisterUserHandler
    {
        private readonly IAppDbContext _db;
        private readonly IUserPasswordHasher _hasher;
        private readonly ISecurityStampService _stamps;
        private readonly IValidator<UserCreateDto> _validator;

        public RegisterUserHandler(
            IAppDbContext db,
            IUserPasswordHasher hasher,
            ISecurityStampService stamps,
            IValidator<UserCreateDto> validator)
        {
            _db = db; _hasher = hasher; _stamps = stamps; _validator = validator;
        }

        public async Task<Result<Guid>> HandleAsync(UserCreateDto dto, Guid? defaultRoleId = null, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var exists = await _db.Set<User>().AnyAsync(u => u.Email == dto.Email && !u.IsDeleted, ct);
            if (exists) return Result<Guid>.Fail("Email already in use.");

            var user = new User(
                email: dto.Email,
                passwordHash: _hasher.Hash(dto.Password),
                securityStamp: _stamps.NewStamp()
            )
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Locale = dto.Locale,
                Timezone = dto.Timezone,
                Currency = dto.Currency,
                IsActive = dto.IsActive,
                IsSystem = dto.IsSystem
            };

            _db.Set<User>().Add(user);

            if (defaultRoleId.HasValue)
            {
                // Ensure role exists & not deleted (idempotent add)
                var roleExists = await _db.Set<Role>().AnyAsync(r => r.Id == defaultRoleId.Value && !r.IsDeleted, ct);
                if (roleExists) user.AddRole(defaultRoleId.Value); // uses domain helper (no direct setters)
            }

            await _db.SaveChangesAsync(ct);
            return Result<Guid>.Ok(user.Id);
        }
    }
}
