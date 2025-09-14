using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Validators
{
    public sealed class CategoryCreateUniqueSlugValidator : AbstractValidator<CategoryCreateDto>
    {
        private readonly IAppDbContext _db;
        public CategoryCreateUniqueSlugValidator(IAppDbContext db)
        {
            _db = db;
            RuleForEach(x => x.Translations).MustAsync(BeUniqueSlug).WithMessage("Slug must be unique per culture.");
        }

        private async Task<bool> BeUniqueSlug(CategoryTranslationDto t, CancellationToken ct)
        {
            return !await _db.Set<CategoryTranslation>()
                .AsNoTracking()
                .AnyAsync(x => x.Culture == t.Culture && x.Slug == t.Slug, ct);
        }
    }

    public sealed class CategoryEditUniqueSlugValidator : AbstractValidator<CategoryEditDto>
    {
        private readonly IAppDbContext _db;
        public CategoryEditUniqueSlugValidator(IAppDbContext db)
        {
            _db = db;
            RuleForEach(x => x.Translations).MustAsync(BeUniqueSlug).WithMessage("Slug must be unique per culture.");
        }

        private async Task<bool> BeUniqueSlug(CategoryEditDto dto, CategoryTranslationDto t, CancellationToken ct)
        {
            return !await _db.Set<CategoryTranslation>()
                .AsNoTracking()
                .AnyAsync(x => x.Culture == t.Culture && x.Slug == t.Slug && x.CategoryId != dto.Id, ct);
        }
    }
}
