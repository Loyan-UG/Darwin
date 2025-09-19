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

namespace Darwin.Application.Identity.Permissions.Commands
{
    /// <summary>Creates a permission with unique Key.</summary>
    public sealed class CreatePermissionHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<PermissionCreateDto> _validator;
        public CreatePermissionHandler(IAppDbContext db, IValidator<PermissionCreateDto> validator) { _db = db; _validator = validator; }

        public async Task<Result<Guid>> HandleAsync(PermissionCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var exists = await _db.Set<Permission>().AnyAsync(p => p.Key == dto.Key && !p.IsDeleted, ct);
            if (exists) return Result<Guid>.Fail("Permission key already exists.");

            var p = new Permission
            {
                Key = dto.Key,
                DisplayName = dto.DisplayName,
                Description = dto.Description,
                IsSystem = dto.IsSystem
            };
            _db.Set<Permission>().Add(p);
            await _db.SaveChangesAsync(ct);
            return Result<Guid>.Ok(p.Id);
        }
    }
}
