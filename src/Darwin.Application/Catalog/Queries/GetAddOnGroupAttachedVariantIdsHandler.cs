using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Queries
{
    /// <summary>
    /// Returns the list of active Variant IDs attached to a given AddOnGroup.
    /// This query respects soft-delete and only returns non-deleted links.
    /// Use this in Admin UI to pre-check items in the selection view.
    /// </summary>
    public sealed class GetAddOnGroupAttachedVariantIdsHandler
    {
        private readonly IAppDbContext _db;

        /// <summary>
        /// Creates a new query handler instance bound to the application DbContext abstraction.
        /// </summary>
        public GetAddOnGroupAttachedVariantIdsHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Gets all variant ids currently attached to the specified add-on group.
        /// </summary>
        /// <param name="groupId">The add-on group identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Distinct, non-deleted variant IDs attached to the group.</returns>
        public async Task<IReadOnlyList<Guid>> HandleAsync(Guid groupId, CancellationToken ct = default)
        {
            // Validate group existence minimally; avoid throwing to keep UI flow simple.
            var exists = await _db.Set<AddOnGroup>()
                .AnyAsync(g => g.Id == groupId && !g.IsDeleted, ct);
            if (!exists) return Array.Empty<Guid>();

            // Join table is soft-deleted via BaseEntity. Only return active links.
            var ids = await _db.Set<AddOnGroupVariant>()
                .AsNoTracking()
                .Where(x => x.AddOnGroupId == groupId && !x.IsDeleted)
                .Select(x => x.VariantId)
                .Distinct()
                .ToListAsync(ct);

            return ids;
        }
    }
}
