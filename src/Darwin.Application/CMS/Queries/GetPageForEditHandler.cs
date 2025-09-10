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
    ///     Query handler that loads a single CMS page with all translations for editing,
    ///     returning a complete DTO, including <c>RowVersion</c> for concurrency checks.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use <c>AsNoTracking</c> to avoid leaking tracked entities into the web layer.
    ///         Sanitize only on save; the edit form should display raw HTML previously sanitized on write.
    ///     </para>
    /// </remarks>
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
