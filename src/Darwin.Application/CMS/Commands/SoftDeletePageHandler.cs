using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.CMS;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

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
        /// Soft-deletes the specified page by Id. If the page is system-protected or not found,
        /// a failure result is returned. When a row version is supplied, an optimistic
        /// concurrency check is performed.
        /// </summary>
        /// <param name="id">Page identifier.</param>
        /// <param name="rowVersion">
        /// Optional concurrency token; when provided, the current entity version must match.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<Result> HandleAsync(Guid id, byte[]? rowVersion, CancellationToken ct = default)
        {
            var page = await _db.Set<Page>().FirstOrDefaultAsync(p => p.Id == id, ct);
            if (page is null)
                return Result.Fail("Page not found.");

            if (rowVersion is not null)
            {
                // Optimistic concurrency: ensure the incoming token matches the current one.
                if (page.RowVersion is null || !page.RowVersion.SequenceEqual(rowVersion))
                    return Result.Fail("The page was modified by another user. Please reload and try again.");
            }

            page.IsDeleted = true;
            page.ModifiedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
