using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Shared.Results;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    /// Performs a soft delete on a <see cref="Category"/> by setting <c>IsDeleted = true</c>.
    /// This command does not cascade to child entities; higher-level business rules
    /// should prevent deleting categories that are still referenced if necessary.
    /// The handler is designed to be invoked from the Admin UI. It returns a
    /// <see cref="Result"/> indicating success or a friendly error.
    /// </summary>
    public sealed class SoftDeleteCategoryHandler
    {
        private readonly IAppDbContext _db;

        /// <summary>
        /// Creates a new instance tied to the application's DbContext abstraction.
        /// </summary>
        public SoftDeleteCategoryHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Marks the category as soft-deleted when found and not already deleted.
        /// The operation is idempotent; calling it for an already-deleted row succeeds.
        /// </summary>
        /// <param name="id">Category identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="Result"/> with <c>Succeeded = true</c> on success; otherwise a failure with a short message.
        /// </returns>
        public async Task<Result> HandleAsync(Guid id, CancellationToken ct = default)
        {
            // Read tracking is needed to update IsDeleted.
            var category = await _db.Set<Category>()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (category is null)
                return Result.Fail("Category not found.");

            if (!category.IsDeleted)
                category.IsDeleted = true;

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
