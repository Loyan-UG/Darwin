using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Application.CMS.DTOs;
using Darwin.Domain.Entities.CMS;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CMS.Queries
{
    /// <summary>
    ///     Paged query for listing CMS pages in Admin with minimal projected fields
    ///     (title, slug, status, modified timestamp) filtered by culture with fallback logic.
    /// </summary>
    public sealed class GetPagesPageHandler
    {
        private readonly IAppDbContext _db;

        public GetPagesPageHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<(IReadOnlyList<PageListItemDto> Items, int Total)> HandleAsync(
            int page = 1,
            int pageSize = 20,
            string? culture = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCultureDefault,
            CancellationToken ct = default)
        {
            return await HandleAsync(page, pageSize, culture, query: null, filter: null, ct).ConfigureAwait(false);
        }

        public async Task<(IReadOnlyList<PageListItemDto> Items, int Total)> HandleAsync(
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
            var nowUtc = DateTime.UtcNow;

            var q = _db.Set<Page>()
                .AsNoTracking()
                .Where(p =>
                    !p.IsDeleted &&
                    (query == null ||
                     p.Translations.Any(t => !t.IsDeleted && EF.Functions.Like(t.Title, query, QueryLikePattern.EscapeCharacter)) ||
                     p.Translations.Any(t => !t.IsDeleted && EF.Functions.Like(t.Slug, query, QueryLikePattern.EscapeCharacter))));

            q = filter switch
            {
                "draft" => q.Where(p => p.Status == PageStatus.Draft),
                "published" => q.Where(p => p.Status == PageStatus.Published),
                "windowed" => q.Where(p => p.PublishStartUtc != null || p.PublishEndUtc != null),
                "live-window" => q.Where(p =>
                    p.Status == PageStatus.Published &&
                    (p.PublishStartUtc != null || p.PublishEndUtc != null) &&
                    (p.PublishStartUtc == null || p.PublishStartUtc <= nowUtc) &&
                    (p.PublishEndUtc == null || p.PublishEndUtc >= nowUtc)),
                _ => q
            };

            var total = await q.CountAsync(ct);

            var items = await q
                .OrderByDescending(p => p.ModifiedAtUtc ?? p.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PageListItemDto
                {
                    Id = p.Id,
                    Title = p.Translations.Where(t => !t.IsDeleted && t.Culture == culture).Select(t => t.Title).FirstOrDefault()
                        ?? p.Translations.Where(t => !t.IsDeleted).Select(t => t.Title).FirstOrDefault(),
                    Status = p.Status,
                    PublishStartUtc = p.PublishStartUtc,
                    PublishEndUtc = p.PublishEndUtc,
                    HasPublishWindow = p.PublishStartUtc != null || p.PublishEndUtc != null,
                    IsCurrentlyLive = p.Status == PageStatus.Published
                        && (p.PublishStartUtc == null || p.PublishStartUtc <= nowUtc)
                        && (p.PublishEndUtc == null || p.PublishEndUtc >= nowUtc),
                    ModifiedAtUtc = p.ModifiedAtUtc ?? p.CreatedAtUtc,
                    RowVersion = p.RowVersion
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }

    public sealed class GetPageOpsSummaryHandler
    {
        private readonly IAppDbContext _db;

        public GetPageOpsSummaryHandler(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<PageOpsSummaryDto> HandleAsync(CancellationToken ct = default)
        {
            var nowUtc = DateTime.UtcNow;
            var pages = _db.Set<Page>().AsNoTracking().Where(p => !p.IsDeleted);

            return new PageOpsSummaryDto
            {
                TotalCount = await pages.CountAsync(ct),
                DraftCount = await pages.CountAsync(p => p.Status == PageStatus.Draft, ct),
                PublishedCount = await pages.CountAsync(p => p.Status == PageStatus.Published, ct),
                WindowedCount = await pages.CountAsync(p => p.PublishStartUtc != null || p.PublishEndUtc != null, ct),
                LiveWindowCount = await pages.CountAsync(p =>
                    p.Status == PageStatus.Published &&
                    (p.PublishStartUtc != null || p.PublishEndUtc != null) &&
                    (p.PublishStartUtc == null || p.PublishStartUtc <= nowUtc) &&
                    (p.PublishEndUtc == null || p.PublishEndUtc >= nowUtc), ct)
            };
        }
    }
}
