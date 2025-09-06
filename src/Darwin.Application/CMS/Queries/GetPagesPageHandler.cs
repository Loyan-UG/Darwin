using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CMS.DTOs;
using Darwin.Domain.Entities.CMS;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CMS.Queries
{
    public sealed class GetPagesPageHandler
    {
        private readonly IAppDbContext _db;
        public GetPagesPageHandler(IAppDbContext db) => _db = db;

        public async Task<(IReadOnlyList<PageListItemDto> Items, int Total)>
            HandleAsync(int page = 1, int pageSize = 20, string? culture = "de-DE", CancellationToken ct = default)
        {
            var q = _db.Set<Page>().AsNoTracking();
            var total = await q.CountAsync(ct);

            var items = await q
                .OrderByDescending(p => p.ModifiedAtUtc ?? p.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PageListItemDto
                {
                    Id = p.Id,
                    Title = p.Translations.Where(t => t.Culture == culture).Select(t => t.Title).FirstOrDefault()
                            ?? p.Translations.Select(t => t.Title).FirstOrDefault(),
                    Status = p.Status,
                    PublishStartUtc = p.PublishStartUtc,
                    PublishEndUtc = p.PublishEndUtc,
                    // If ModifiedAtUtc is nullable in BaseEntity, coalesce to CreatedAtUtc
                    ModifiedAtUtc = (p.ModifiedAtUtc ?? p.CreatedAtUtc)
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }
}
