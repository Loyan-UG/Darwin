using System;
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
    /// Query handler that retrieves a lightweight list of shipping methods for
    /// administrative index pages. Each item includes identifying information,
    /// carrier/service codes, activation flag and the number of associated
    /// rate tiers. Soft‑deleted records are excluded via the global query
    /// filter configured on <see cref="ShippingMethod"/>.
    /// </summary>
    public sealed class GetShippingMethodsPageHandler
    {
        private readonly IAppDbContext _db;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetShippingMethodsPageHandler"/>.
        /// </summary>
        /// <param name="db">Database context used for reading shipping methods.</param>
        public GetShippingMethodsPageHandler(IAppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Retrieves all non‑deleted shipping methods and projects them into
        /// <see cref="ShippingMethodListItemDto"/> objects. No pagination is
        /// applied; callers may paginate or sort the results as needed.
        /// </summary>
        /// <param name="ct">Cancellation token for async operation.</param>
        /// <returns>A list of shipping method list items.</returns>
        public async Task<List<ShippingMethodListItemDto>> HandleAsync(CancellationToken ct = default)
        {
            return await _db.Set<ShippingMethod>()
                .AsNoTracking()
                .Select(sm => new ShippingMethodListItemDto
                {
                    Id = sm.Id,
                    Name = sm.Name,
                    Carrier = sm.Carrier,
                    Service = sm.Service,
                    IsActive = sm.IsActive,
                    RateCount = sm.Rates.Count
                })
                .ToListAsync(ct);
        }
    }
}