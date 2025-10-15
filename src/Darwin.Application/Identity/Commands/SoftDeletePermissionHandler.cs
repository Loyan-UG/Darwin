using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Soft-delete a permission with optional concurrency token (RowVersion).
    /// Does nothing for IsSystem = true.
    /// </summary>
    public sealed class SoftDeletePermissionHandler
    {
        private readonly IAppDbContext _db;
        public SoftDeletePermissionHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(Guid id, byte[]? rowVersion, CancellationToken ct = default)
        {
            var p = await _db.Set<Permission>().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
            if (p == null) return;
            if (p.IsSystem) throw new InvalidOperationException("System permission cannot be deleted.");

            if (rowVersion != null && rowVersion.Length > 0 &&
                !StructuralComparisons.StructuralEqualityComparer.Equals(p.RowVersion, rowVersion))
                throw new DbUpdateConcurrencyException("Concurrency conflict. The item was modified by another user.");

            p.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
