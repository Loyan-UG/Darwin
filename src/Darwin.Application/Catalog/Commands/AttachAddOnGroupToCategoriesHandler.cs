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
}
