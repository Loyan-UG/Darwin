using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    /// Updates a category aggregate and its translations with optimistic concurrency check.
    /// </summary>
    public sealed class UpdateCategoryHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<CategoryEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateCategoryHandler(
            IAppDbContext db,
            IValidator<CategoryEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _validator = validator;
            _localizer = localizer;
        }

        public async Task HandleAsync(CategoryEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = await _db.Set<Category>()
                .Include(c => c.Translations)
                .FirstOrDefaultAsync(c => c.Id == dto.Id, ct);

            if (entity == null)
                throw new ValidationException(_localizer["CategoryNotFound"]);

            // Concurrency
            if (!entity.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException(_localizer["CategoryModifiedByAnotherUser"]);

            entity.ParentId = dto.ParentId;
            entity.IsActive = dto.IsActive;
            entity.SortOrder = dto.SortOrder;

            foreach (var t in dto.Translations)
            {
                var translation = entity.Translations.FirstOrDefault(x => x.Culture == t.Culture);
                if (translation == null)
                {
                    translation = new CategoryTranslation { Culture = t.Culture };
                    entity.Translations.Add(translation);
                }

                translation.Name = t.Name;
                translation.Slug = t.Slug;
                translation.Description = t.Description;
                translation.MetaTitle = t.MetaTitle;
                translation.MetaDescription = t.MetaDescription;
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
