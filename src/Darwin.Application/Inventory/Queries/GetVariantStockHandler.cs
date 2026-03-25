using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Inventory.Queries
{
    /// <summary>
    /// Returns current stock figures for a product variant aggregated from warehouse-aware stock levels.
    /// </summary>
    public sealed class GetVariantStockHandler
    {
        private readonly IAppDbContext _db;
        public GetVariantStockHandler(IAppDbContext db) => _db = db;

        public async Task<(int OnHand, int Reserved, int Available)?> HandleAsync(Guid variantId, Guid? warehouseId = null, CancellationToken ct = default)
        {
            var query = _db.Set<Darwin.Domain.Entities.Inventory.StockLevel>()
                .AsNoTracking()
                .Where(v => v.ProductVariantId == variantId);

            if (warehouseId.HasValue)
            {
                query = query.Where(v => v.WarehouseId == warehouseId.Value);
            }

            var row = await query
                .GroupBy(_ => 1)
                .Select(v => new
                {
                    Available = v.Sum(x => x.AvailableQuantity),
                    Reserved = v.Sum(x => x.ReservedQuantity)
                })
                .FirstOrDefaultAsync(ct);

            if (row == null) return null;

            var onHand = row.Available + row.Reserved;
            return (onHand, row.Reserved, row.Available);
        }
    }
}
