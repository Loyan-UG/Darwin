using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Shipping.DTOs;
using Darwin.Domain.Entities.Shipping;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Shipping.Queries
{
    /// <summary>
    /// Loads a shipping method with all of its tiers for the admin edit form.
    /// </summary>
    public sealed class GetShippingMethodForEditHandler
    {
        private readonly IAppDbContext _db;
        public GetShippingMethodForEditHandler(IAppDbContext db) => _db = db;

        public async Task<ShippingMethodEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Set<ShippingMethod>().AsNoTracking()
                .Where(m => m.Id == id)
                .Select(m => new ShippingMethodEditDto
                {
                    Id = m.Id,
                    RowVersion = m.RowVersion,
                    Name = m.Name,
                    Carrier = m.Carrier,
                    Service = m.Service,
                    CountriesCsv = m.CountriesCsv,
                    IsActive = m.IsActive,
                    Currency = m.Currency,
                    Rates = m.Rates.OrderBy(r => r.SortOrder).Select(r => new ShippingRateDto
                    {
                        Id = r.Id,
                        MaxShipmentMass = r.MaxShipmentMass,
                        MaxSubtotalNetMinor = r.MaxSubtotalNetMinor,
                        PriceMinor = r.PriceMinor,
                        SortOrder = r.SortOrder
                    }).ToList()
                })
                .FirstOrDefaultAsync(ct);
        }
    }
}
