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
    /// <summary>Creates a role with unique Key (Name).</summary>
    public sealed class CreateRoleHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<RoleCreateDto> _validator;

        public CreateRoleHandler(IAppDbContext db, IValidator<RoleCreateDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        public async Task<Result<Guid>> HandleAsync(RoleCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var normalized = dto.Key.Trim().ToUpperInvariant();
            var exists = await _db.Set<Role>().AnyAsync(r => r.NormalizedName == normalized && !r.IsDeleted, ct);
            
            if (exists) 
                return Result<Guid>.Fail("Role key already exists.");

            // NOTE: Domain.Role has: Name, NormalizedName, IsSystem, Description.
            // We also need DisplayName (admin-facing). If Domain already has it, map directly.
            // If Domain does NOT have it yet, please add `public string DisplayName { get; set; } = string.Empty;` to Role.
            var role = new Role(dto.Key, dto.DisplayName, dto.IsSystem, dto.Description);
            // NOTE: Role ctor sets Name/NormalizedName/DisplayName/IsSystem/Description

            _db.Set<Role>().Add(role);
            await _db.SaveChangesAsync(ct);
            return Result<Guid>.Ok(role.Id);
        }
    }
}
