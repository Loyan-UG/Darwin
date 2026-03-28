using System.Collections.Generic;
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
    /// Returns a paged list of brands for Admin grid, selecting localized name for the requested culture (with fallback).
    /// </summary>
    public sealed class GetBrandsPageHandler
    {
        private readonly IAppDbContext _db;

        public GetBrandsPageHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<(List<BrandListItemDto> Items, int Total)> HandleAsync(
            int page,
            int pageSize,
            string culture = "de-DE",
            CancellationToken ct = default)
        {
            return await HandleAsync(page, pageSize, culture, query: null, filter: null, ct);
        }

        public async Task<(List<BrandListItemDto> Items, int Total)> HandleAsync(
            int page,
            int pageSize,
            string culture,
            string? query,
            string? filter,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
            filter = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim().ToLowerInvariant();

            var baseQuery = _db.Set<Brand>()
                .AsNoTracking()
                .Where(b =>
                    query == null ||
                    (b.Slug != null && b.Slug.Contains(query)) ||
                    b.Translations.Any(t => t.Name.Contains(query)));

            baseQuery = filter switch
            {
                "unpublished" => baseQuery.Where(b => !b.IsPublished),
                "missing-slug" => baseQuery.Where(b => b.Slug == null || b.Slug == string.Empty),
                "missing-logo" => baseQuery.Where(b => b.LogoMediaId == null),
                _ => baseQuery
            };

            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderBy(b => b.Translations
                    .Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault()
                    ?? b.Translations.Select(t => t.Name).FirstOrDefault())
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BrandListItemDto
                {
                    Id = b.Id,
                    Name = b.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault()
                           ?? b.Translations.Select(t => t.Name).FirstOrDefault()
                           ?? "?",
                    Slug = b.Slug,
                    LogoMediaId = b.LogoMediaId,
                    IsPublished = b.IsPublished,
                    ModifiedAtUtc = b.ModifiedAtUtc,
                    RowVersion = b.RowVersion
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }

    public sealed class GetBrandOpsSummaryHandler
    {
        private readonly IAppDbContext _db;

        public GetBrandOpsSummaryHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<BrandOpsSummaryDto> HandleAsync(CancellationToken ct = default)
        {
            var brands = _db.Set<Brand>().AsNoTracking();

            return new BrandOpsSummaryDto
            {
                TotalCount = await brands.CountAsync(ct),
                UnpublishedCount = await brands.CountAsync(b => !b.IsPublished, ct),
                MissingSlugCount = await brands.CountAsync(b => b.Slug == null || b.Slug == string.Empty, ct),
                MissingLogoCount = await brands.CountAsync(b => b.LogoMediaId == null, ct)
            };
        }
    }
}
