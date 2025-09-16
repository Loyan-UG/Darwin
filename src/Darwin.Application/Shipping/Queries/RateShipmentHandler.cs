using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Shipping.DTOs;
using Darwin.Application.Shipping.Validators;
using Darwin.Domain.Entities.Shipping;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Shipping.Queries
{
    /// <summary>
    /// Computes available shipping options by evaluating each method's tiered rates
    /// against destination, subtotal (net, minor), and shipment mass.
    /// </summary>
    public sealed class RateShipmentHandler
    {
        private readonly IAppDbContext _db;
        private readonly RateShipmentInputValidator _validator = new();

        public RateShipmentHandler(IAppDbContext db) => _db = db;

        public async Task<List<ShippingOptionDto>> HandleAsync(RateShipmentInputDto input, string defaultCurrency, CancellationToken ct = default)
        {
            var val = _validator.Validate(input);
            if (!val.IsValid) throw new FluentValidation.ValidationException(val.Errors);

            var options = new List<ShippingOptionDto>();

            var q = _db.Set<ShippingMethod>().AsNoTracking()
                .Include(m => m.Rates)
                .Where(m => m.IsActive);

            // Countries filter
            q = q.Where(m => m.CountriesCsv == null
                             || m.CountriesCsv == string.Empty
                             || m.CountriesCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                                .Contains(input.Country));

            var methods = await q.ToListAsync(ct);

            foreach (var m in methods)
            {
                var currency = m.Currency ?? input.Currency ?? defaultCurrency;

                ShippingRate? match = null;
                foreach (var r in m.Rates.OrderBy(r => r.SortOrder))
                {
                    var massOk = !r.MaxShipmentMass.HasValue || input.ShipmentMass <= r.MaxShipmentMass.Value;
                    var subtotalOk = !r.MaxSubtotalNetMinor.HasValue || input.SubtotalNetMinor <= r.MaxSubtotalNetMinor.Value;

                    if (massOk && subtotalOk)
                    {
                        match = r;
                        break;
                    }
                }

                if (match is not null)
                {
                    options.Add(new ShippingOptionDto
                    {
                        MethodId = m.Id,
                        Name = $"{m.Carrier} – {m.Service}",
                        PriceMinor = match.PriceMinor,
                        Currency = currency,
                        Carrier = m.Carrier,
                        Service = m.Service
                    });
                }
            }

            return options.OrderBy(o => o.PriceMinor).ToList();
        }
    }
}
