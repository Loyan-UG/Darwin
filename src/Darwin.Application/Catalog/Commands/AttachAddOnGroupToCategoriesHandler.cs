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
    /// Attaches an add-on group to categories.
    /// </summary>
    public sealed class AttachAddOnGroupToCategoriesHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public AttachAddOnGroupToCategoriesHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task HandleAsync(Guid groupId, IEnumerable<Guid> categoryIds, CancellationToken ct = default)
        {
            var groupExists = await _db.Set<AddOnGroup>().AnyAsync(g => g.Id == groupId && !g.IsDeleted, ct);
            if (!groupExists) throw new InvalidOperationException(_localizer["AddOnGroupNotFound"]);

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
}
