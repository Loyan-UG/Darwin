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
    /// Query handler that retrieves a single shipping method by its identifier
    /// and maps it into a <see cref="ShippingMethodEditDto"/> for use in
    /// edit forms. Includes associated rate tiers. Returns null when the
    /// requested method does not exist.
    /// </summary>
    public sealed class GetShippingMethodForEditHandler
    {
        private readonly IAppDbContext _db;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetShippingMethodForEditHandler"/>.
        /// </summary>
        /// <param name="db">Database context used to query shipping methods.</param>
        public GetShippingMethodForEditHandler(IAppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Loads a shipping method by its primary key and projects it into
        /// an editable DTO. Returns null if the method cannot be found.
        /// Soft‑deleted methods are automatically filtered out by global query
        /// filters on <see cref="ShippingMethod"/>.
        /// </summary>
        /// <param name="id">Primary key of the shipping method.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A populated <see cref="ShippingMethodEditDto"/> or null.</returns>
        public async Task<ShippingMethodEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Set<ShippingMethod>()
                .AsNoTracking()
                .Include(sm => sm.Rates)
                .FirstOrDefaultAsync(sm => sm.Id == id, ct);

            if (entity == null)
                return null;

            var dto = new ShippingMethodEditDto
            {
                Id = entity.Id,
                RowVersion = entity.RowVersion,
                Name = entity.Name,
                Carrier = entity.Carrier,
                Service = entity.Service,
                CountriesCsv = entity.CountriesCsv,
                IsActive = entity.IsActive,
                Rates = entity.Rates
                    .OrderBy(r => r.SortOrder)
                    .Select(r => new ShippingDtos
                    {
                        MaxWeight = r.MaxWeight,
                        MaxSubtotalNetMinor = r.MaxSubtotalNetMinor,
                        PriceMinor = r.PriceMinor,
                        SortOrder = r.SortOrder
                    })
                    .ToList()
            };
            return dto;
        }
    }
}