using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Validators;
using Darwin.Application.Common.Html;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    /// Updates an existing brand. Performs optimistic concurrency check via RowVersion and replaces
    /// translations with the provided set (simple replace strategy for phase 1).
    /// </summary>
    public sealed class UpdateBrandHandler
    {
        private readonly IAppDbContext _db;
        private readonly BrandEditDtoValidator _validator = new();

        public UpdateBrandHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(BrandEditDto dto, CancellationToken ct = default)
        {
            var validation = _validator.Validate(dto);
            if (!validation.IsValid)
                throw new FluentValidation.ValidationException(validation.Errors);

            var brand = await _db.Set<Brand>()
                .Include(b => b.Translations)
                .FirstOrDefaultAsync(b => b.Id == dto.Id, ct);

            if (brand is null) throw new InvalidOperationException("Brand not found.");

            // Concurrency check
            if (!brand.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");

            // Unique slug check if changed
            if (!string.Equals(brand.Slug, dto.Slug, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(dto.Slug))
            {
                var slugExists = await _db.Set<Brand>()
                    .AnyAsync(b => b.Id != brand.Id && b.Slug == dto.Slug, ct);
                if (slugExists)
                    throw new FluentValidation.ValidationException("Slug must be unique.");
            }

            brand.Slug = string.IsNullOrWhiteSpace(dto.Slug) ? null : dto.Slug.Trim();
            brand.LogoMediaId = dto.LogoMediaId;

            var sanitizer = HtmlSanitizerFactory.Create();

            // Replace translations (phase 1 strategy)
            brand.Translations.Clear();
            foreach (var tr in dto.Translations)
            {
                brand.Translations.Add(new BrandTranslation
                {
                    Culture = tr.Culture.Trim(),
                    Name = tr.Name.Trim(),
                    DescriptionHtml = tr.DescriptionHtml is null ? null : sanitizer.Sanitize(tr.DescriptionHtml)
                });
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
