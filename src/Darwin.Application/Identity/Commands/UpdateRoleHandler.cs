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
    /// <summary>Updates role display metadata. Key is immutable to avoid cascading changes.</summary>
    public sealed class UpdateRoleHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<RoleEditDto> _validator;
        public UpdateRoleHandler(IAppDbContext db, IValidator<RoleEditDto> validator) { _db = db; _validator = validator; }

        public async Task<Result> HandleAsync(RoleEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var role = await _db.Set<Role>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct);
            if (role == null) return Result.Fail("Role not found.");

            // Concurrency
            if (role.RowVersion is not null && dto.RowVersion is not null && role.RowVersion.Length > 0)
            {
                if (!StructuralComparisons.StructuralEqualityComparer.Equals(role.RowVersion, dto.RowVersion))
                    return Result.Fail("Concurrency conflict.");
            }

            if (role.IsSystem) // Protect system roles
                return Result.Fail("System role cannot be modified.");

            role.DisplayName = dto.DisplayName;
            role.Description = dto.Description;

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
