using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>Creates a role with unique Key.</summary>
    public sealed class CreateRoleHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<RoleCreateDto> _validator;
        public CreateRoleHandler(IAppDbContext db, IValidator<RoleCreateDto> validator) { _db = db; _validator = validator; }

        public async Task<Result<Guid>> HandleAsync(RoleCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var exists = await _db.Set<Role>().AnyAsync(r => r.Name == dto.Name && !r.IsDeleted, ct);
            if (exists) return Result<Guid>.Fail("Role key already exists.");

            var normalized = dto.Name.Trim().ToUpperInvariant();

            var r = new Role
            {
                IsSystem = dto.IsSystem,
                Name = dto.Name.Trim(),
                NormalizedName = normalized,
                DisplayName = dto.DisplayName,
                Description = dto.Description
            };
            _db.Set<Role>().Add(r);
            await _db.SaveChangesAsync(ct);
            return Result<Guid>.Ok(r.Id);
        }
    }
}
