using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Common;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Queries
{
    /// <summary>
    /// Returns a paged list of categories with a culture-specific display name.
    /// </summary>
    public sealed class GetCategoriesPageHandler
    {
        private readonly IAppDbContext _db;

        public GetCategoriesPageHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<(IReadOnlyList<CategoryListItemDto> Items, int Total)> HandleAsync(
            int page = 1,
            int pageSize = 20,
            string? culture = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCultureDefault,
            CancellationToken ct = default)
        {
            return await HandleAsync(page, pageSize, culture, query: null, filter: null, ct);
        }

        public async Task<(IReadOnlyList<CategoryListItemDto> Items, int Total)> HandleAsync(
            int page,
            int pageSize,
            string? culture,
            string? query,
            string? filter,
            CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;
            query = string.IsNullOrWhiteSpace(query) ? null : QueryLikePattern.Contains(query);
            filter = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim().ToLowerInvariant();

            var q = _db.Set<Category>()
                .AsNoTracking()
                .Where(c =>
                    !c.IsDeleted &&
                    (query == null ||
                     c.Translations.Any(t => !t.IsDeleted && EF.Functions.Like(t.Name, query, QueryLikePattern.EscapeCharacter)) ||
                     c.Translations.Any(t => !t.IsDeleted && t.Slug != null && EF.Functions.Like(t.Slug, query, QueryLikePattern.EscapeCharacter))));

            q = filter switch
            {
                "inactive" => q.Where(c => !c.IsActive),
                "unpublished" => q.Where(c => !c.IsPublished),
                "root" => q.Where(c => c.ParentId == null),
                "child" => q.Where(c => c.ParentId != null),
                _ => q
            };

            var projected = q.Select(c => new CategoryListItemDto
            {
                Id = c.Id,
                Name = c.Translations
                    .Where(t => !t.IsDeleted && t.Culture == culture).Select(t => t.Name).FirstOrDefault()
                    ?? c.Translations.Where(t => !t.IsDeleted).Select(t => t.Name).FirstOrDefault(),
                IsActive = c.IsActive,
                IsPublished = c.IsPublished,
                SortOrder = c.SortOrder,
                ParentId = c.ParentId,
                ModifiedAtUtc = c.ModifiedAtUtc ?? c.CreatedAtUtc,
                RowVersion = c.RowVersion
            });

            var total = await projected.CountAsync(ct);
            var items = await projected
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }
    }

    public sealed class GetCategoryOpsSummaryHandler
    {
        private readonly IAppDbContext _db;

        public GetCategoryOpsSummaryHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<CategoryOpsSummaryDto> HandleAsync(CancellationToken ct = default)
        {
            var categories = _db.Set<Category>().AsNoTracking().Where(c => !c.IsDeleted);

            return new CategoryOpsSummaryDto
            {
                TotalCount = await categories.CountAsync(ct),
                InactiveCount = await categories.CountAsync(c => !c.IsActive, ct),
                UnpublishedCount = await categories.CountAsync(c => !c.IsPublished, ct),
                RootCount = await categories.CountAsync(c => c.ParentId == null, ct),
                ChildCount = await categories.CountAsync(c => c.ParentId != null, ct)
            };
        }
    }
}
