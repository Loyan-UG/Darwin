using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Catalog.Commands
{
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
