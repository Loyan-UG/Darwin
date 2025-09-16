using System;
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
    /// Updates a tax category (optimistic concurrency via RowVersion).
    /// </summary>
    public sealed class UpdateTaxCategoryHandler
    {
        private readonly IAppDbContext _db;
        private readonly TaxCategoryEditValidator _validator = new();

        public UpdateTaxCategoryHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(TaxCategoryEditDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new ValidationException(v.Errors);

            var entity = await _db.Set<TaxCategory>().FirstOrDefaultAsync(t => t.Id == dto.Id, ct);
            if (entity is null) throw new InvalidOperationException("Tax category not found.");

            if (!entity.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");

            var exists = await _db.Set<TaxCategory>().AsNoTracking()
                .AnyAsync(t => t.Id != dto.Id && t.Name.ToLower() == dto.Name.ToLower(), ct);
            if (exists) throw new ValidationException("Tax category name must be unique.");

            entity.Name = dto.Name.Trim();
            entity.VatRate = dto.VatRate;
            entity.EffectiveFromUtc = dto.EffectiveFromUtc;
            entity.Notes = dto.Notes;

            await _db.SaveChangesAsync(ct);
        }
    }
}
