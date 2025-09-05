using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using FluentValidation;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    /// Creates a category aggregate with translations.
    /// </summary>
    public sealed class CreateCategoryHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<CategoryCreateDto> _validator;

        public CreateCategoryHandler(IAppDbContext db, IValidator<CategoryCreateDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        public async Task<System.Guid> HandleAsync(CategoryCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var entity = new Category
            {
                ParentId = dto.ParentId,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder
            };

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

            _db.Set<Category>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.Id;
        }
    }
}
