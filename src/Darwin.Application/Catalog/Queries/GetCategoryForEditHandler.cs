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
    /// Gets a category with translations for editing.
    /// </summary>
    public sealed class GetCategoryForEditHandler
    {
        private readonly IAppDbContext _db;
        public GetCategoryForEditHandler(IAppDbContext db) => _db = db;

        public async Task<CategoryEditDto?> HandleAsync(System.Guid id, CancellationToken ct = default)
        {
            var c = await _db.Set<Category>()
                .Include(x => x.Translations)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (c == null) return null;

            return new CategoryEditDto
            {
                Id = c.Id,
                ParentId = c.ParentId,
                IsActive = c.IsActive,
                SortOrder = c.SortOrder,
                RowVersion = c.RowVersion,
                Translations = c.Translations.Select(t => new CategoryTranslationDto
                {
                    Culture = t.Culture,
                    Name = t.Name,
                    Slug = t.Slug,
                    Description = t.Description,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription
                }).ToList()
            };
        }
    }
}
