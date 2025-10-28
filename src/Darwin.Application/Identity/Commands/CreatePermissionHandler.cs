using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>Creates a permission with unique Key (case-insensitive).</summary>
    public sealed class CreatePermissionHandler
    {
        private readonly IAppDbContext _db;
        public CreatePermissionHandler(IAppDbContext db) => _db = db;

        public async Task<Result<Guid>> HandleAsync(string key, string displayName, string? description, bool isSystem = false, CancellationToken ct = default)
        {
            var normalized = key.Trim().ToUpperInvariant();
            var exists = await _db.Set<Permission>().AnyAsync(p => p.Key.ToUpper() == normalized && !p.IsDeleted, ct);
            if (exists) return Result<Guid>.Fail("Permission key already exists.");

            var permission = new Permission(key, displayName, isSystem, description);
            _db.Set<Permission>().Add(permission);
            await _db.SaveChangesAsync(ct);
            return Result<Guid>.Ok(permission.Id);
        }
    }
}
