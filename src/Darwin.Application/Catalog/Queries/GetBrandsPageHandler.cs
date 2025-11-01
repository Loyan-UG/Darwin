using System;
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
        public GetBrandsPageHandler(IAppDbContext db) => _db = db;

        public async Task<(List<BrandListItemDto> Items, int Total)> HandleAsync(
            int page, int pageSize, string culture = "de-DE", CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var baseQuery = _db.Set<Brand>().AsNoTracking();

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
                    ModifiedAtUtc = b.ModifiedAtUtc,
                    RowVersion = b.RowVersion
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }
}
