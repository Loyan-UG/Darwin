using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CMS.DTOs;
using Darwin.Application.Common.Html;
using Darwin.Domain.Entities.CMS;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.CMS.Commands
{
    public sealed class UpdatePageHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<PageEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdatePageHandler(
            IAppDbContext db,
            IValidator<PageEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _validator = validator;
            _localizer = localizer;
        }

        public async Task HandleAsync(PageEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = await _db.Set<Page>()
                .Include(p => p.Translations)
                .FirstOrDefaultAsync(p => p.Id == dto.Id, ct);

            if (entity == null)
                throw new ValidationException(_localizer["PageNotFound"]);

            if (!entity.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException(_localizer["PageModifiedByAnotherUser"]);

            var sanitizer = HtmlSanitizerFactory.Create();

            entity.Status = dto.Status;
            entity.PublishStartUtc = dto.PublishStartUtc;
            entity.PublishEndUtc = dto.PublishEndUtc;

            foreach (var t in dto.Translations)
            {
                var translation = entity.Translations.FirstOrDefault(x => x.Culture == t.Culture);
                if (translation == null)
                {
                    translation = new PageTranslation { Culture = t.Culture };
                    entity.Translations.Add(translation);
                }

                translation.Culture = t.Culture.Trim();
                translation.Title = t.Title.Trim();
                translation.Slug = t.Slug.Trim();
                translation.MetaTitle = t.MetaTitle?.Trim();
                translation.MetaDescription = t.MetaDescription?.Trim();
                translation.ContentHtml = sanitizer.Sanitize(t.ContentHtml ?? string.Empty);
            }

            PageRootSnapshot.SyncFromPrimaryTranslation(entity);

            await _db.SaveChangesAsync(ct);
        }
    }
}
