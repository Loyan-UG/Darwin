using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    /// Updates a category aggregate and its translations with optimistic concurrency check.
    /// </summary>
    public sealed class UpdateCategoryHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<CategoryEditDto> _validator;

        public UpdateCategoryHandler(IAppDbContext db, IValidator<CategoryEditDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        public async Task HandleAsync(CategoryEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = await _db.Set<Category>()
                .Include(c => c.Translations)
                .FirstOrDefaultAsync(c => c.Id == dto.Id, ct);

            if (entity == null)
                throw new ValidationException("Category not found.");

            // Concurrency
            if (!entity.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException("The category was modified by another user.");

            entity.ParentId = dto.ParentId;
            entity.IsActive = dto.IsActive;
            entity.SortOrder = dto.SortOrder;

            entity.Translations.Clear();
            foreach (var t in dto.Translations)
            {
                entity.Translations.Add(new CategoryTranslation
                {
                    Culture = t.Culture,
                    Name = t.Name,
                    Slug = t.Slug,
                    Description = t.Description,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription
                });
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
