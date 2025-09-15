using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CMS.DTOs;
using Darwin.Domain.Entities.CMS;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CMS.Queries
{
    /// <summary>
    /// Loads a menu with all items and their translations for editing. Includes RowVersion.
    /// </summary>
    public sealed class GetMenuForEditHandler
    {
        private readonly IAppDbContext _db;
        public GetMenuForEditHandler(IAppDbContext db) => _db = db;

        public async Task<MenuEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            // Load menu + items + translations
            var menu = await _db.Set<Menu>()
                .AsNoTracking()
                .Where(m => m.Id == id)
                .Select(m => new MenuEditDto
                {
                    Id = m.Id,
                    RowVersion = m.RowVersion,
                    Name = m.Name,
                    Items = m.Items
                        .OrderBy(i => i.ParentId).ThenBy(i => i.SortOrder).ThenBy(i => i.Url)
                        .Select(i => new MenuItemDto
                        {
                            Id = i.Id,
                            ParentId = i.ParentId,
                            Url = i.Url,
                            SortOrder = i.SortOrder,
                            IsActive = i.IsActive,
                            Translations = i.Translations
                                .OrderBy(t => t.Culture)
                                .Select(t => new MenuItemTranslationDto
                                {
                                    Culture = t.Culture,
                                    Label = t.Label
                                }).ToList()
                        }).ToList()
                })
                .FirstOrDefaultAsync(ct);

            return menu;
        }
    }
}
