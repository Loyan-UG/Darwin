using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.CMS;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CMS.Commands
{
    /// <summary>
    /// Soft deletes a <see cref="Page"/> to keep content history intact while removing it
    /// from active listings. The operation is idempotent and succeeds silently if the page
    /// does not exist or is already deleted.
    /// </summary>
    public sealed class SoftDeletePageHandler
    {
        private readonly IAppDbContext _db;

        /// <summary>
        /// Initializes a new instance of the handler with the shared DbContext abstraction.
        /// </summary>
        public SoftDeletePageHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Executes the soft delete for the given page <paramref name="id"/>.
        /// </summary>
        public async Task HandleAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Set<Page>()
                                  .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

            if (entity is null) return;

            entity.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
