using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Shipping;
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
            await _validator.ValidateAndThrowAsync(dto, ct);

            var method = await _db.Set<ShippingMethod>()
                .Include(m => m.Rates)
                .FirstOrDefaultAsync(m => m.Id == dto.Id, ct);

            if (method is null) throw new InvalidOperationException(_localizer["ShippingMethodNotFound"]);

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0)
                throw new ValidationException(_localizer["RowVersionRequired"]);

            var currentRowVersion = method.RowVersion ?? Array.Empty<byte>();
            if (!currentRowVersion.SequenceEqual(rowVersion))
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);

            var carrier = ShippingMethodConventions.NormalizeCarrier(dto.Carrier);
            var service = dto.Service.Trim();
            var exists = await _db.Set<ShippingMethod>().AsNoTracking()
                .AnyAsync(m => m.Id != dto.Id && m.Carrier == carrier && m.Service == service, ct);
            if (exists)
                throw new ValidationException(_localizer["ShippingMethodCarrierServiceMustBeUnique"]);

            method.Name = dto.Name.Trim();
            method.Carrier = carrier;
            method.Service = service;
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
