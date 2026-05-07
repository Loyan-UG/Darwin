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
                PublishStartUtc = NormalizeNullableUtc(dto.PublishStartUtc),
                PublishEndUtc = NormalizeNullableUtc(dto.PublishEndUtc)
            };

            foreach (var t in dto.Translations)
            {
                entity.Translations.Add(new PageTranslation
                {
                    Culture = t.Culture.Trim(),
                    Title = t.Title.Trim(),
                    Slug = NormalizeSlug(t.Slug),
                    MetaTitle = t.MetaTitle?.Trim(),
                    MetaDescription = t.MetaDescription?.Trim(),
                    ContentHtml = sanitizer.Sanitize(t.ContentHtml ?? string.Empty)
                });
            }

            PageRootSnapshot.SyncFromPrimaryTranslation(entity);

            _db.Set<Page>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }

        private static System.DateTime? NormalizeNullableUtc(System.DateTime? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            return value.Value.Kind switch
            {
                System.DateTimeKind.Utc => value.Value,
                System.DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => System.DateTime.SpecifyKind(value.Value, System.DateTimeKind.Utc)
            };
        }

        private static string NormalizeSlug(string slug)
            => slug.Trim().ToLowerInvariant();
    }
}
