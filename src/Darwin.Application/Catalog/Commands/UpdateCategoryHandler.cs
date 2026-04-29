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
                .FirstOrDefaultAsync(c => c.Id == dto.Id && !c.IsDeleted, ct);

            if (entity == null)
                throw new ValidationException(_localizer["CategoryNotFound"]);

            // Concurrency
            var rowVersion = dto.RowVersion ?? System.Array.Empty<byte>();
            var currentVersion = entity.RowVersion ?? System.Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
                throw new DbUpdateConcurrencyException(_localizer["CategoryModifiedByAnotherUser"]);

            entity.ParentId = dto.ParentId;
            entity.IsActive = dto.IsActive;
            entity.SortOrder = dto.SortOrder;

            var requestedCultures = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var t in dto.Translations)
            {
                var culture = t.Culture.Trim();
                if (!requestedCultures.Add(culture))
                {
                    throw new ValidationException(_localizer["DuplicateCulturesNotAllowed"]);
                }

                var translation = entity.Translations.FirstOrDefault(x =>
                    string.Equals(x.Culture, culture, System.StringComparison.OrdinalIgnoreCase));
                if (translation == null)
                {
                    translation = new CategoryTranslation { Culture = culture };
                    entity.Translations.Add(translation);
                }

                translation.IsDeleted = false;
                translation.Culture = culture;
                translation.Name = t.Name.Trim();
                translation.Slug = t.Slug.Trim();
                translation.Description = t.Description?.Trim();
                translation.MetaTitle = t.MetaTitle?.Trim();
                translation.MetaDescription = t.MetaDescription?.Trim();
            }

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException(_localizer["CategoryModifiedByAnotherUser"]);
            }
        }
    }
}
