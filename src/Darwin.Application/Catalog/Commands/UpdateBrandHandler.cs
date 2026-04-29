using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Validators;
using Darwin.Application.Common.Html;
using Darwin.Domain.Entities.Catalog;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    /// Updates an existing brand. Performs optimistic concurrency check via RowVersion and upserts
    /// translations so existing translation identities are preserved.
    /// </summary>
    public sealed class UpdateBrandHandler
    {
        private readonly IAppDbContext _db;
        private readonly BrandEditDtoValidator _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateBrandHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
            _validator = new BrandEditDtoValidator(localizer);
        }

        public async Task HandleAsync(BrandEditDto dto, CancellationToken ct = default)
        {
            var validation = _validator.Validate(dto);
            if (!validation.IsValid)
                throw new FluentValidation.ValidationException(validation.Errors);

            var brand = await _db.Set<Brand>()
                .Include(b => b.Translations)
                .FirstOrDefaultAsync(b => b.Id == dto.Id && !b.IsDeleted, ct);

            if (brand is null) throw new InvalidOperationException(_localizer["BrandNotFound"]);

            // Concurrency check
            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = brand.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);

            // Unique slug check if changed
            var normalizedSlug = string.IsNullOrWhiteSpace(dto.Slug) ? null : dto.Slug.Trim();
            if (!string.Equals(brand.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(normalizedSlug))
            {
                var slugExists = await _db.Set<Brand>()
                    .AnyAsync(b => !b.IsDeleted && b.Id != brand.Id && b.Slug == normalizedSlug, ct);
                if (slugExists)
                    throw new FluentValidation.ValidationException(_localizer["BrandSlugMustBeUnique"]);
            }

            brand.Slug = normalizedSlug;
            brand.LogoMediaId = dto.LogoMediaId;

            var sanitizer = HtmlSanitizerFactory.Create();

            foreach (var tr in dto.Translations)
            {
                var culture = tr.Culture.Trim();
                var translation = tr.Id.HasValue && tr.Id.Value != Guid.Empty
                    ? brand.Translations.FirstOrDefault(t => t.Id == tr.Id.Value)
                    : brand.Translations.FirstOrDefault(t => string.Equals(t.Culture, culture, StringComparison.OrdinalIgnoreCase));

                if (translation is null)
                {
                    brand.Translations.Add(new BrandTranslation
                    {
                        Culture = culture,
                        Name = tr.Name.Trim(),
                        DescriptionHtml = tr.DescriptionHtml is null ? null : sanitizer.Sanitize(tr.DescriptionHtml)
                    });
                    continue;
                }

                translation.IsDeleted = false;
                translation.Culture = culture;
                translation.Name = tr.Name.Trim();
                translation.DescriptionHtml = tr.DescriptionHtml is null ? null : sanitizer.Sanitize(tr.DescriptionHtml);
            }

            var duplicateCultures = brand.Translations
                .Where(t => !t.IsDeleted)
                .GroupBy(t => t.Culture, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateCultures.Count > 0)
            {
                throw new FluentValidation.ValidationException(_localizer["DuplicateCulturesNotAllowed"]);
            }

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }
        }
    }
}
