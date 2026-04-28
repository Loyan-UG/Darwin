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

            var requested = (categoryIds ?? Array.Empty<Guid>())
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();

            var validCategoryIds = await _db.Set<Category>()
                .AsNoTracking()
                .Where(c => requested.Contains(c.Id) && !c.IsDeleted)
                .Select(c => c.Id)
                .ToListAsync(ct);

            if (validCategoryIds.Count != requested.Length)
            {
                throw new InvalidOperationException(_localizer["CategoriesNotFoundOrDeleted"]);
            }

            var existing = await _db.Set<AddOnGroupCategory>()
                .IgnoreQueryFilters()
                .Where(x => x.AddOnGroupId == groupId)
                .ToListAsync(ct);

            _db.Set<AddOnGroupCategory>().RemoveRange(existing);

            var toAdd = validCategoryIds.Select(cid => new AddOnGroupCategory
            {
                AddOnGroupId = groupId,
                CategoryId = cid
            });

            _db.Set<AddOnGroupCategory>().AddRange(toAdd);
            await _db.SaveChangesAsync(ct);
        }
    }
}
