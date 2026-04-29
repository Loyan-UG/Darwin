using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Pricing.DTOs;
using Darwin.Application.Pricing.Validators;
using Darwin.Domain.Entities.Pricing;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Pricing.Commands
{
    /// <summary>
    /// Updates a tax category (optimistic concurrency via RowVersion).
    /// </summary>
    public sealed class UpdateTaxCategoryHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<TaxCategoryEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateTaxCategoryHandler(
            IAppDbContext db,
            IValidator<TaxCategoryEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _validator = validator;
            _localizer = localizer;
        }

        public async Task HandleAsync(TaxCategoryEditDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new ValidationException(v.Errors);

            var entity = await _db.Set<TaxCategory>().FirstOrDefaultAsync(t => t.Id == dto.Id, ct);
            if (entity is null) throw new InvalidOperationException(_localizer["TaxCategoryNotFound"]);

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0)
                throw new ValidationException(_localizer["RowVersionRequired"]);

            var currentRowVersion = entity.RowVersion ?? Array.Empty<byte>();
            if (!currentRowVersion.SequenceEqual(rowVersion))
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);

            var normalizedName = dto.Name.Trim();
            var exists = await _db.Set<TaxCategory>().AsNoTracking()
                .AnyAsync(t => t.Id != dto.Id && t.Name == normalizedName, ct);
            if (exists) throw new ValidationException(_localizer["TaxCategoryNameMustBeUnique"]);

            entity.Name = normalizedName;
            entity.VatRate = dto.VatRate;
            entity.EffectiveFromUtc = dto.EffectiveFromUtc;
            entity.Notes = dto.Notes;

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }
        }
    }
}
