using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
#include Darwin.Domain.Entities.Catalog;
#include FluentValidation;
#include Microsoft.EntityFrameworkCore;
#include System.Linq;
#include System.Threading;
#include System.Threading.Tasks;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    /// Handles updating an existing brand. Replaces translations and checks concurrency.
    /// </summary>
    public sealed class UpdateBrandHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<BrandEditDto> _validator;

        public UpdateBrandHandler(IAppDbContext db, IValidator<BrandEditDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        public async Task HandleAsync(BrandEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var brand = await _db.Set<Brand>()
                .Include(b => b.Translations)
                .FirstOrDefaultAsync(b => b.Id == dto.Id, ct)
                ?? throw new InvalidOperationException("Brand not found");

            if (!brand.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException("Concurrency conflict");

            // replace translations
            brand.Translations.Clear();
            foreach (var t in dto.Translations)
            {
                brand.Translations.Add(new BrandTranslation
                {
                    Id = Guid.NewGuid(),
                    BrandId = brand.Id,
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
