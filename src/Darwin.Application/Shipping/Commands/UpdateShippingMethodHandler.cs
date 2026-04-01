using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Shipping.DTOs;
using Darwin.Application.Shipping.Validators;
using Darwin.Domain.Entities.Shipping;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Shipping.Commands
{
    /// <summary>
    /// Updates a shipping method and replaces its rate tiers (simple replace-all in phase 1).
    /// Performs optimistic concurrency check via RowVersion.
    /// </summary>
    public sealed class UpdateShippingMethodHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<ShippingMethodEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateShippingMethodHandler(
            IAppDbContext db,
            IValidator<ShippingMethodEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _validator = validator;
            _localizer = localizer;
        }

        public async Task HandleAsync(ShippingMethodEditDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new ValidationException(v.Errors);

            var method = await _db.Set<ShippingMethod>()
                .Include(m => m.Rates)
                .FirstOrDefaultAsync(m => m.Id == dto.Id, ct);

            if (method is null) throw new InvalidOperationException(_localizer["ShippingMethodNotFound"]);

            if (!method.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);

            var exists = await _db.Set<ShippingMethod>().AsNoTracking()
                .AnyAsync(m => m.Id != dto.Id && m.Carrier == dto.Carrier && m.Service == dto.Service, ct);
            if (exists)
                throw new ValidationException(_localizer["ShippingMethodCarrierServiceMustBeUnique"]);

            method.Name = dto.Name.Trim();
            method.Carrier = dto.Carrier.Trim();
            method.Service = dto.Service.Trim();
            method.CountriesCsv = string.IsNullOrWhiteSpace(dto.CountriesCsv) ? null : dto.CountriesCsv.Trim();
            method.IsActive = dto.IsActive;
            method.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? null : dto.Currency.Trim();

            method.Rates.Clear();
            foreach (var r in dto.Rates.OrderBy(r => r.SortOrder))
            {
                method.Rates.Add(new ShippingRate
                {
                    MaxShipmentMass = r.MaxShipmentMass,
                    MaxSubtotalNetMinor = r.MaxSubtotalNetMinor,
                    PriceMinor = r.PriceMinor,
                    SortOrder = r.SortOrder
                });
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
