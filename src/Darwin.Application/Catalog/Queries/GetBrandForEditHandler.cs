using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Queries
{
    /// <summary>
    /// Loads a single brand with all translations for editing. Includes RowVersion for optimistic concurrency.
    /// </summary>
    public sealed class GetBrandForEditHandler
    {
        private readonly IAppDbContext _db;
        public GetBrandForEditHandler(IAppDbContext db) => _db = db;

        public async Task<BrandEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Set<Brand>().AsNoTracking()
                .Where(b => b.Id == id)
                .Select(b => new BrandEditDto
                {
                    Id = b.Id,
                    RowVersion = b.RowVersion,
                    Slug = b.Slug,
                    LogoMediaId = b.LogoMediaId,
                    Translations = b.Translations
                        .OrderBy(t => t.Culture)
                        .Select(t => new BrandTranslationDto
                        {
                            Culture = t.Culture,
                            Name = t.Name,
                            DescriptionHtml = t.DescriptionHtml
                        }).ToList()
                })
                .FirstOrDefaultAsync(ct);
        }
    }
}
