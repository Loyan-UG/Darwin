using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Catalog.Validators
{
    /// <summary>
    /// Ensures the (culture-invariant) Brand.Slug is globally unique for create operations.
    /// </summary>
    public sealed class BrandCreateUniqueSlugValidator : AbstractValidator<BrandCreateDto>
    {
        private readonly IAppDbContext _db;

        public BrandCreateUniqueSlugValidator(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;

            RuleFor(x => x.Slug)
                .MaximumLength(256)
                .MustAsync(BeUniqueSlug).WithMessage(localizer["SlugMustBeUnique"])
                .When(x => !string.IsNullOrWhiteSpace(x.Slug));
        }

        private async Task<bool> BeUniqueSlug(string? slug, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(slug)) return true;
            return !await _db.Set<Brand>().AsNoTracking().AnyAsync(b => b.Slug == slug, ct);
        }
    }

    /// <summary>
    /// Ensures the (culture-invariant) Brand.Slug remains globally unique on edit.
    /// </summary>
    public sealed class BrandEditUniqueSlugValidator : AbstractValidator<BrandEditDto>
    {
        private readonly IAppDbContext _db;

        public BrandEditUniqueSlugValidator(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;

            RuleFor(x => x.Slug)
                .MaximumLength(256)
                .MustAsync(BeUniqueSlug).WithMessage(localizer["SlugMustBeUnique"])
                .When(x => !string.IsNullOrWhiteSpace(x.Slug));
        }

        private async Task<bool> BeUniqueSlug(BrandEditDto dto, string? slug, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(slug)) return true;
            return !await _db.Set<Brand>().AsNoTracking()
                .AnyAsync(b => b.Id != dto.Id && b.Slug == slug, ct);
        }
    }
}
