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
    public sealed class CreatePageHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<PageCreateDto> _validator;

        public CreatePageHandler(IAppDbContext db, IValidator<PageCreateDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        public async Task<System.Guid> HandleAsync(PageCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);
            var sanitizer = HtmlSanitizerFactory.Create();

            var entity = new Page
            {
                Status = dto.Status,
                PublishStartUtc = dto.PublishStartUtc,
                PublishEndUtc = dto.PublishEndUtc
            };

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

            _db.Set<Page>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }
    }
}
