using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    /// Attaches an add-on group to products.
    /// </summary>
    public sealed class AttachAddOnGroupToProductsHandler
    {
        private readonly IAppDbContext _db;
        public AttachAddOnGroupToProductsHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(Guid groupId, IEnumerable<Guid> productIds, CancellationToken ct = default)
        {
            var groupExists = await _db.Set<AddOnGroup>().AnyAsync(g => g.Id == groupId && !g.IsDeleted, ct);
            if (!groupExists) throw new InvalidOperationException("Add-on group not found.");

            var existing = await _db.Set<AddOnGroupProduct>()
                .Where(x => x.AddOnGroupId == groupId)
                .ToListAsync(ct);

            _db.Set<AddOnGroupProduct>().RemoveRange(existing);

            var toAdd = productIds.Distinct().Select(pid => new AddOnGroupProduct
            {
                AddOnGroupId = groupId,
                ProductId = pid
            });

            _db.Set<AddOnGroupProduct>().AddRange(toAdd);
            await _db.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Attaches an add-on group to categories.
    /// </summary>
    public sealed class AttachAddOnGroupToCategoriesHandler
    {
        private readonly IAppDbContext _db;
        public AttachAddOnGroupToCategoriesHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(Guid groupId, IEnumerable<Guid> categoryIds, CancellationToken ct = default)
        {
            var groupExists = await _db.Set<AddOnGroup>().AnyAsync(g => g.Id == groupId && !g.IsDeleted, ct);
            if (!groupExists) throw new InvalidOperationException("Add-on group not found.");

            var existing = await _db.Set<AddOnGroupCategory>()
                .Where(x => x.AddOnGroupId == groupId)
                .ToListAsync(ct);

            _db.Set<AddOnGroupCategory>().RemoveRange(existing);

            var toAdd = categoryIds.Distinct().Select(cid => new AddOnGroupCategory
            {
                AddOnGroupId = groupId,
                CategoryId = cid
            });

            _db.Set<AddOnGroupCategory>().AddRange(toAdd);
            await _db.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Attaches an add-on group to brands.
    /// </summary>
    public sealed class AttachAddOnGroupToBrandsHandler
    {
        private readonly IAppDbContext _db;
        public AttachAddOnGroupToBrandsHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(Guid groupId, IEnumerable<Guid> brandIds, CancellationToken ct = default)
        {
            var groupExists = await _db.Set<AddOnGroup>().AnyAsync(g => g.Id == groupId && !g.IsDeleted, ct);
            if (!groupExists) throw new InvalidOperationException("Add-on group not found.");

            var existing = await _db.Set<AddOnGroupBrand>()
                .Where(x => x.AddOnGroupId == groupId)
                .ToListAsync(ct);

            _db.Set<AddOnGroupBrand>().RemoveRange(existing);

            var toAdd = brandIds.Distinct().Select(bid => new AddOnGroupBrand
            {
                AddOnGroupId = groupId,
                BrandId = bid
            });

            _db.Set<AddOnGroupBrand>().AddRange(toAdd);
            await _db.SaveChangesAsync(ct);
        }
    }
}
