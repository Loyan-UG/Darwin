using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CMS.DTOs;
using Darwin.Domain.Entities.CMS;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CMS.Queries
{
    public sealed class GetPageForEditHandler
    {
        private readonly IAppDbContext _db;
        public GetPageForEditHandler(IAppDbContext db) => _db = db;

        public async Task<PageEditDto?> HandleAsync(System.Guid id, CancellationToken ct = default)
        {
            var p = await _db.Set<Page>()
                .Include(x => x.Translations)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (p == null) return null;

            return new PageEditDto
            {
                Id = p.Id,
                RowVersion = p.RowVersion,
                Status = p.Status,
                PublishStartUtc = p.PublishStartUtc,
                PublishEndUtc = p.PublishEndUtc,
                Translations = p.Translations.Select(t => new PageTranslationDto
                {
                    Culture = t.Culture,
                    Title = t.Title,
                    Slug = t.Slug,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription,
                    ContentHtml = t.ContentHtml
                }).ToList()
            };
        }
    }
}
