using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CMS.DTOs;
using Darwin.Application.Common.Html;
using Darwin.Domain.Entities.CMS;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CMS.Commands
{
    public sealed class UpdatePageHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<PageEditDto> _validator;

        public UpdatePageHandler(IAppDbContext db, IValidator<PageEditDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        public async Task HandleAsync(PageEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = await _db.Set<Page>()
                .Include(p => p.Translations)
                .FirstOrDefaultAsync(p => p.Id == dto.Id, ct);

            if (entity == null)
                throw new ValidationException("Page not found.");

            if (!entity.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException("The page was modified by another user.");

            var sanitizer = HtmlSanitizerFactory.Create();

            entity.Status = dto.Status;
            entity.PublishStartUtc = dto.PublishStartUtc;
            entity.PublishEndUtc = dto.PublishEndUtc;

            entity.Translations.Clear();
            foreach (var t in dto.Translations)
            {
                entity.Translations.Add(new PageTranslation
                {
                    Culture = t.Culture,
                    Title = t.Title,
                    Slug = t.Slug,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription,
                    ContentHtml = sanitizer.Sanitize(t.ContentHtml ?? string.Empty)
                });
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
