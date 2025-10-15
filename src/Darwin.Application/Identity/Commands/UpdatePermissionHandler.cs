using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Updates a permission's editable fields (DisplayName/Description) with concurrency check.
    /// </summary>
    public sealed class UpdatePermissionHandler
    {
        private readonly IAppDbContext _db;
        private readonly PermissionEditValidator _validator = new();

        public UpdatePermissionHandler(IAppDbContext db) => _db = db;

        public async Task<Result> HandleAsync(PermissionEditDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) return Result.Fail(v.ToString());

            var p = await _db.Set<Permission>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct);
            if (p == null) return Result.Fail("Permission not found.");

            if (!StructuralComparisons.StructuralEqualityComparer.Equals(p.RowVersion, dto.RowVersion))
                return Result.Fail("Concurrency conflict. Reload and try again.");

            // Key and IsSystem are not editable
            p.DisplayName = dto.DisplayName.Trim();
            p.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
