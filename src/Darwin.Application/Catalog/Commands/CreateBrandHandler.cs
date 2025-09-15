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
    /// Creates a new brand with translations after validating input and sanitizing HTML fields.
    /// Enforces unique slug (when provided) with a pre-insert check for a friendly error.
    /// </summary>
    public sealed class CreateBrandHandler
    {
        private readonly IAppDbContext _db;
        private readonly BrandCreateDtoValidator _validator = new();

        public CreateBrandHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(BrandCreateDto dto, CancellationToken ct = default)
        {
            var validation = _validator.Validate(dto);
            if (!validation.IsValid)
                throw new FluentValidation.ValidationException(validation.Errors);

            if (!string.IsNullOrWhiteSpace(dto.Slug))
            {
                var slugExists = await _db.Set<Brand>().AnyAsync(b => b.Slug == dto.Slug, ct);
                if (slugExists)
                    throw new FluentValidation.ValidationException("Slug must be unique.");
            }

            var sanitizer = HtmlSanitizerFactory.Create();

            var brand = new Brand
            {
                Slug = dto.Slug?.Trim(),
                LogoMediaId = dto.LogoMediaId
            };

            foreach (var tr in dto.Translations)
            {
                brand.Translations.Add(new BrandTranslation
                {
                    Culture = tr.Culture.Trim(),
                    Name = tr.Name.Trim(),
                    DescriptionHtml = tr.DescriptionHtml is null ? null : sanitizer.Sanitize(tr.DescriptionHtml)
                });
            }

            _db.Set<Brand>().Add(brand);
            await _db.SaveChangesAsync(ct);
        }
    }
}
