using System.Collections.Generic;
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
    /// Returns a paged list of shipping methods for the Admin grid.
    /// </summary>
    public sealed class GetShippingMethodsPageHandler
    {
        private readonly IAppDbContext _db;
        public GetShippingMethodsPageHandler(IAppDbContext db) => _db = db;

        public async Task<(List<ShippingMethodListItemDto> Items, int Total)> HandleAsync(
            int page,
            int pageSize,
            string? query = null,
            ShippingMethodQueueFilter filter = ShippingMethodQueueFilter.All,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var baseQuery = _db.Set<ShippingMethod>().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                baseQuery = baseQuery.Where(m =>
                    m.Name.Contains(term) ||
                    m.Carrier.Contains(term) ||
                    m.Service.Contains(term) ||
                    (m.CountriesCsv != null && m.CountriesCsv.Contains(term)));
            }

            baseQuery = filter switch
            {
                ShippingMethodQueueFilter.Active => baseQuery.Where(m => m.IsActive),
                ShippingMethodQueueFilter.Inactive => baseQuery.Where(m => !m.IsActive),
                ShippingMethodQueueFilter.MissingRates => baseQuery.Where(m => !m.Rates.Any()),
                ShippingMethodQueueFilter.Dhl => baseQuery.Where(m =>
                    m.Carrier.Contains("DHL") ||
                    m.Name.Contains("DHL") ||
                    m.Service.Contains("DHL")),
                _ => baseQuery
            };

            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .Include(m => m.Rates)
                .OrderBy(m => m.Carrier).ThenBy(m => m.Service)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(m => new ShippingMethodListItemDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Carrier = m.Carrier,
                    Service = m.Service,
                    DisplayName = m.Carrier + " – " + m.Service,
                    CountriesCsv = m.CountriesCsv,
                    Currency = m.Currency,
                    IsActive = m.IsActive,
                    RatesCount = m.Rates.Count,
                    ModifiedAtUtc = m.ModifiedAtUtc
                }).ToListAsync(ct);

            return (items, total);
        }
    }
}
