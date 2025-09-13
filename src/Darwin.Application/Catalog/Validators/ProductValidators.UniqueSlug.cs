using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Validators
{
    /// <summary>
    /// Ensures product slugs are unique per culture when creating a product.
    /// Mirrors the pattern used by Category/Page unique-slug validators.
    /// </summary>
    public sealed class ProductCreateUniqueSlugValidator : AbstractValidator<ProductCreateDto>
    {
        private readonly IAppDbContext _db;

        public ProductCreateUniqueSlugValidator(IAppDbContext db)
        {
            _db = db;
            RuleForEach(x => x.Translations)
                .MustAsync(BeUniqueSlug)
                .WithMessage("Slug must be unique per culture.");
        }

        /// <summary>
        /// Checks that no ProductTranslation exists with the same Culture + Slug.
        /// </summary>
        private async Task<bool> BeUniqueSlug(ProductTranslationDto t, CancellationToken ct)
        {
            return !await _db.Set<ProductTranslation>()
                .AsNoTracking()
                .AnyAsync(x => x.Culture == t.Culture && x.Slug == t.Slug, ct);
        }
    }

    /// <summary>
    /// Ensures product slugs are unique per culture when editing a product.
    /// </summary>
    public sealed class ProductEditUniqueSlugValidator : AbstractValidator<ProductEditDto>
    {
        private readonly IAppDbContext _db;

        public ProductEditUniqueSlugValidator(IAppDbContext db)
        {
            _db = db;
            RuleForEach(x => x.Translations)
                .MustAsync(BeUniqueSlug)
                .WithMessage("Slug must be unique per culture.");
        }

        /// <summary>
        /// Ensures uniqueness excluding the product itself (by ProductId).
        /// </summary>
        private async Task<bool> BeUniqueSlug(ProductEditDto dto, ProductTranslationDto t, CancellationToken ct)
        {
            return !await _db.Set<ProductTranslation>()
                .AsNoTracking()
                .AnyAsync(x => x.Culture == t.Culture && x.Slug == t.Slug && x.ProductId != dto.Id, ct);
        }
    }
}
