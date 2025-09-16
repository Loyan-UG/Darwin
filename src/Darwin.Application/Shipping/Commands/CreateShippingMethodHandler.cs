using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Shipping.DTOs;
using Darwin.Application.Shipping.Validators;
using Darwin.Domain.Entities.Shipping;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Shipping.Commands
{
    /// <summary>
    /// Creates a new shipping method along with its tiered rates (replace-all strategy in phase 1).
    /// Ensures (Carrier, Service) uniqueness to avoid duplicates in admin UI.
    /// </summary>
    public sealed class CreateShippingMethodHandler
    {
        private readonly IAppDbContext _db;
        private readonly ShippingMethodCreateValidator _validator = new();

        public CreateShippingMethodHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(ShippingMethodCreateDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new ValidationException(v.Errors);

            var exists = await _db.Set<ShippingMethod>().AsNoTracking()
                .AnyAsync(m => m.Carrier == dto.Carrier && m.Service == dto.Service, ct);
            if (exists)
                throw new ValidationException("Carrier/Service combination must be unique.");

            var method = new ShippingMethod
            {
                Name = dto.Name.Trim(),
                Carrier = dto.Carrier.Trim(),
                Service = dto.Service.Trim(),
                CountriesCsv = string.IsNullOrWhiteSpace(dto.CountriesCsv) ? null : dto.CountriesCsv.Trim(),
                IsActive = dto.IsActive,
                Currency = string.IsNullOrWhiteSpace(dto.Currency) ? null : dto.Currency.Trim()
            };

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

            _db.Set<ShippingMethod>().Add(method);
            await _db.SaveChangesAsync(ct);
        }
    }
}
