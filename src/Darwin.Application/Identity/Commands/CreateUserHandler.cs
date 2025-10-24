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
    /// Creates a new user with a hashed password and security stamp.
    /// Enforces unique Email (case-insensitive).
    /// </summary>
    public sealed class CreateUserHandler
    {
        private readonly IAppDbContext _db;
        private readonly IUserPasswordHasher _hasher;
        private readonly ISecurityStampService _stamps;
        private readonly IValidator<UserCreateDto> _validator;

        public CreateUserHandler(IAppDbContext db, IUserPasswordHasher hasher, ISecurityStampService stamps, IValidator<UserCreateDto> validator)
        {
            _db = db; _hasher = hasher; _stamps = stamps; _validator = validator;
        }

        public async Task<Result<Guid>> HandleAsync
            (UserCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var normalizedEmail = dto.Email.Trim().ToUpperInvariant();
            var exists = await _db.Set<User>().AnyAsync
                (u => u.NormalizedEmail == normalizedEmail && !u.IsDeleted, ct);
            if (exists) 
                return Result<Guid>.Fail("Email already in use.");

            var user = new User(dto.Email, _hasher.Hash(dto.Password), _stamps.NewStamp())
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Locale = dto.Locale,
                Timezone = dto.Timezone,
                Currency = dto.Currency,
                IsActive = dto.IsActive,
                IsSystem = dto.IsSystem,
                PhoneE164 = dto.PhoneE164,
                RowVersion = dto.RowVersion
            };

            _db.Set<User>().Add(user);
            await _db.SaveChangesAsync(ct);
            return Result<Guid>.Ok(user.Id);
        }
    }
}
