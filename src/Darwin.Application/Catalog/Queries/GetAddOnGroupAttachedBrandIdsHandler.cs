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
    /// Returns the list of active Brand IDs attached to a given AddOnGroup.
    /// </summary>
    public sealed class GetAddOnGroupAttachedBrandIdsHandler
    {
        private readonly IAppDbContext _db;
        public GetAddOnGroupAttachedBrandIdsHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Retrieves all brand ids currently attached to the specified group.
        /// </summary>
        public async Task<IReadOnlyList<Guid>> HandleAsync(Guid groupId, CancellationToken ct = default)
        {
            var exists = await _db.Set<AddOnGroup>()
                .AnyAsync(g => g.Id == groupId && !g.IsDeleted, ct);
            if (!exists) return Array.Empty<Guid>();

            var ids = await _db.Set<AddOnGroupBrand>()
                .AsNoTracking()
                .Where(x => x.AddOnGroupId == groupId && !x.IsDeleted)
                .Select(x => x.BrandId)
                .Distinct()
                .ToListAsync(ct);

            return ids;
        }
    }
}
