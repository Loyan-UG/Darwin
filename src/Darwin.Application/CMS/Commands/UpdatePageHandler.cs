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

            var rowVersion = dto.RowVersion ?? System.Array.Empty<byte>();
            var currentVersion = entity.RowVersion ?? System.Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
                throw new DbUpdateConcurrencyException(_localizer["PageModifiedByAnotherUser"]);

            var sanitizer = HtmlSanitizerFactory.Create();

            entity.Status = dto.Status;
            entity.PublishStartUtc = NormalizeNullableUtc(dto.PublishStartUtc);
            entity.PublishEndUtc = NormalizeNullableUtc(dto.PublishEndUtc);

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
                translation.Slug = NormalizeSlug(t.Slug);
                translation.MetaTitle = t.MetaTitle?.Trim();
                translation.MetaDescription = t.MetaDescription?.Trim();
                translation.ContentHtml = sanitizer.Sanitize(t.ContentHtml ?? string.Empty);
            }

            PageRootSnapshot.SyncFromPrimaryTranslation(entity);

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException(_localizer["PageModifiedByAnotherUser"]);
            }
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
