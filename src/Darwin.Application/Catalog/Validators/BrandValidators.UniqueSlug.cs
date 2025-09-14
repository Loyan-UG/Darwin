using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Catalog.Validators
{
    /// <summary>
    /// Ensures brand slugs are unique per culture for create operations.
    /// </summary>
    public sealed class BrandCreateUniqueSlugValidator : AbstractValidator<BrandCreateDto>
    {
        private readonly IAppDbContext _db;

        public BrandCreateUniqueSlugValidator(IAppDbContext db)
        {
            _db = db;
            RuleForEach(x => x.Translations)
                .MustAsync(BeUniqueSlug).WithMessage("Slug must be unique per culture");
        }

        private async Task<bool> BeUniqueSlug(BrandTranslationDto t, CancellationToken ct)
        {
            return !await _db.Set<BrandTranslation>()
                .AsNoTracking()
                .AnyAsync(x => x.Culture == t.Culture && x.Slug == t.Slug, ct);
        }
    }

    /// <summary>
    /// Ensures brand slugs remain unique per culture when editing a brand.
    /// </summary>
    public sealed class BrandEditUniqueSlugValidator : AbstractValidator<BrandEditDto>
    {
        private readonly IAppDbContext _db;

        public BrandEditUniqueSlugValidator(IAppDbContext db)
        {
            _db = db;
            RuleForEach(x => x.Translations)
                .MustAsync(BeUniqueSlug).WithMessage("Slug must be unique per culture");
        }

        private async Task<bool> BeUniqueSlug(BrandEditDto dto, BrandTranslationDto t, CancellationToken ct)
        {
            return !await _db.Set<BrandTranslation>()
                .AsNoTracking()
                .AnyAsync(x => x.Culture == t.Culture && x.Slug == t.Slug && x.BrandId != dto.Id, ct);
        }
    }
}
