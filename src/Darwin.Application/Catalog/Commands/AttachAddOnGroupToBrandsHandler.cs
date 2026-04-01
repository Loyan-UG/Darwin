using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public AttachAddOnGroupToBrandsHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task HandleAsync(Guid groupId, IEnumerable<Guid> brandIds, CancellationToken ct = default)
        {
            var groupExists = await _db.Set<AddOnGroup>().AnyAsync(g => g.Id == groupId && !g.IsDeleted, ct);
            if (!groupExists) throw new InvalidOperationException(_localizer["AddOnGroupNotFound"]);

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
