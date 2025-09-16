using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Pricing.DTOs;
using Darwin.Application.Pricing.Validators;
using Darwin.Domain.Entities.Pricing;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Pricing.Commands
{
    /// <summary>
    /// Creates a tax category. Enforces name uniqueness (case-insensitive) for admin sanity.
    /// </summary>
    public sealed class CreateTaxCategoryHandler
    {
        private readonly IAppDbContext _db;
        private readonly TaxCategoryCreateValidator _validator = new();

        public CreateTaxCategoryHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(TaxCategoryCreateDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new ValidationException(v.Errors);

            var exists = await _db.Set<TaxCategory>().AsNoTracking()
                .AnyAsync(t => t.Name.ToLower() == dto.Name.ToLower(), ct);
            if (exists) throw new ValidationException("Tax category name must be unique.");

            var entity = new TaxCategory
            {
                Name = dto.Name.Trim(),
                VatRate = dto.VatRate,
                EffectiveFromUtc = dto.EffectiveFromUtc,
                Notes = dto.Notes
            };

            _db.Set<TaxCategory>().Add(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}
