using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CMS.DTOs;
using Darwin.Domain.Entities.CMS;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.CMS.Validators
{
    public sealed class PageCreateUniqueSlugValidator : AbstractValidator<PageCreateDto>
    {
        private readonly IAppDbContext _db;
        public PageCreateUniqueSlugValidator(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;

            RuleForEach(x => x.Translations).MustAsync(BeUniqueSlug).WithMessage(localizer["SlugMustBeUniquePerCulture"]);
        }

        private async Task<bool> BeUniqueSlug(PageTranslationDto t, CancellationToken ct)
        {
            return !await _db.Set<PageTranslation>()
                .AsNoTracking()
                .AnyAsync(x => x.Culture == t.Culture && x.Slug == t.Slug, ct);
        }
    }


    public sealed class PageEditUniqueSlugValidator : AbstractValidator<PageEditDto>
    {
        private readonly IAppDbContext _db;
        public PageEditUniqueSlugValidator(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;

            RuleForEach(x => x.Translations).MustAsync(BeUniqueSlug).WithMessage(localizer["SlugMustBeUniquePerCulture"]);
        }

        private async Task<bool> BeUniqueSlug(PageEditDto dto, PageTranslationDto t, CancellationToken ct)
        {
            return !await _db.Set<PageTranslation>()
                .AsNoTracking()
                .AnyAsync(x => x.Culture == t.Culture && x.Slug == t.Slug && x.PageId != dto.Id, ct);
        }
    }
}
