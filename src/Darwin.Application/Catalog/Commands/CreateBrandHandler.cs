using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using FluentValidation;
using System;
using System.Linq;
using System.Threading;
#include System.Threading.Tasks;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    /// Handles creation of a new brand including its translations.
    /// </summary>
    public sealed class CreateBrandHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<BrandCreateDto> _validator;

        public CreateBrandHandler(IAppDbContext db, IValidator<BrandCreateDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        public async Task<Guid> HandleAsync(BrandCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var brand = new Brand
            {
                Id = Guid.NewGuid(),
                Translations = dto.Translations.Select(t => new BrandTranslation
                {
                    Id = Guid.NewGuid(),
                    BrandId = Guid.Empty, // will be set after adding brand
                    Culture = t.Culture,
                    Name = t.Name,
                    Slug = t.Slug,
                    Description = t.Description,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription
                }).ToList()
            };
            // set BrandId for translations
            foreach (var tr in brand.Translations)
                tr.BrandId = brand.Id;

            _db.Set<Brand>().Add(brand);
            await _db.SaveChangesAsync(ct);
            return brand.Id;
        }
    }
}
,