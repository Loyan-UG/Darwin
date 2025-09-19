using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Updates editable fields. Uses RowVersion for optimistic concurrency.
    /// </summary>
    public sealed class UpdateUserHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<UserEditDto> _validator;

        public UpdateUserHandler(IAppDbContext db, IValidator<UserEditDto> validator)
        {
            _db = db; _validator = validator;
        }

        public async Task<Result> HandleAsync(UserEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var u = await _db.Set<User>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct);
            if (u == null) return Result.Fail("User not found.");

            // Concurrency
            if (u.RowVersion is not null && dto.RowVersion is not null && u.RowVersion.Length > 0)
            {
                if (!StructuralComparisons.StructuralEqualityComparer.Equals(u.RowVersion, dto.RowVersion))
                    return Result.Fail("Concurrency conflict.");
            }

            u.FirstName = dto.FirstName;
            u.LastName = dto.LastName;
            u.Locale = dto.Locale;
            u.Timezone = dto.Timezone;
            u.Currency = dto.Currency;
            u.IsActive = dto.IsActive;

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
