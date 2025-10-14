using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    /// Performs a soft delete of a <see cref="Product"/> by setting <c>IsDeleted = true</c>.
    /// Soft delete preserves audit trail and avoids hard data removal, which is important
    /// for historical orders and references. No-op when the entity is already deleted or missing.
    /// </summary>
    public sealed class SoftDeleteProductHandler
    {
        private readonly IAppDbContext _db;

        /// <summary>
        /// Constructs the handler with the application DbContext abstraction.
        /// </summary>
        public SoftDeleteProductHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Marks the specified product as deleted and commits the change.
        /// </summary>
        public async Task HandleAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Set<Product>()
                                  .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

            if (entity is null) return;

            entity.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
