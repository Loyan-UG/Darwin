using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Inventory
{
    /// <summary>
    /// Shared inventory helper methods used by command handlers that still expose
    /// product-variant-centric operations while the platform transitions toward
    /// warehouse-aware stock management.
    /// </summary>
    internal static class InventoryStockHelper
    {
        /// <summary>
        /// Resolves the target warehouse for a stock mutation.
        /// When the caller does not specify a warehouse, the helper prefers an existing
        /// stock level's default warehouse and falls back to the first configured warehouse.
        /// </summary>
        public static async Task<Guid> ResolveWarehouseIdAsync(
            IAppDbContext db,
            Guid productVariantId,
            Guid? warehouseId,
            CancellationToken ct)
        {
            if (warehouseId.HasValue)
            {
                var requestedWarehouseExists = await db.Set<Warehouse>()
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == warehouseId.Value, ct)
                    .ConfigureAwait(false);

                if (!requestedWarehouseExists)
                {
                    throw new InvalidOperationException("Warehouse not found.");
                }

                return warehouseId.Value;
            }

            var existingWarehouseId = await (
                    from stockLevel in db.Set<StockLevel>().AsNoTracking()
                    join warehouse in db.Set<Warehouse>().AsNoTracking() on stockLevel.WarehouseId equals warehouse.Id
                    where stockLevel.ProductVariantId == productVariantId
                    orderby warehouse.IsDefault descending, warehouse.Name
                    select stockLevel.WarehouseId)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (existingWarehouseId != Guid.Empty)
            {
                return existingWarehouseId;
            }

            var fallbackWarehouseId = await db.Set<Warehouse>()
                .AsNoTracking()
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.Name)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (fallbackWarehouseId == Guid.Empty)
            {
                throw new InvalidOperationException("No warehouse is configured.");
            }

            return fallbackWarehouseId;
        }

        /// <summary>
        /// Loads an existing stock level or creates a new tracked row for the requested
        /// warehouse and product variant pair.
        /// </summary>
        public static async Task<StockLevel> GetOrCreateStockLevelAsync(
            IAppDbContext db,
            Guid warehouseId,
            Guid productVariantId,
            CancellationToken ct)
        {
            var stockLevel = await db.Set<StockLevel>()
                .FirstOrDefaultAsync(
                    x => x.WarehouseId == warehouseId && x.ProductVariantId == productVariantId,
                    ct)
                .ConfigureAwait(false);

            if (stockLevel is not null)
            {
                return stockLevel;
            }

            stockLevel = new StockLevel
            {
                WarehouseId = warehouseId,
                ProductVariantId = productVariantId
            };

            db.Set<StockLevel>().Add(stockLevel);
            return stockLevel;
        }

        /// <summary>
        /// Refreshes legacy aggregate stock fields on <see cref="ProductVariant"/> from
        /// warehouse-aware stock levels so older consumers continue to observe consistent totals.
        /// </summary>
        public static async Task RefreshLegacyVariantStockAsync(
            IAppDbContext db,
            Guid productVariantId,
            CancellationToken ct)
        {
            var totals = await db.Set<StockLevel>()
                .AsNoTracking()
                .Where(x => x.ProductVariantId == productVariantId)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Available = g.Sum(x => x.AvailableQuantity),
                    Reserved = g.Sum(x => x.ReservedQuantity)
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            var variant = await db.Set<ProductVariant>()
                .FirstOrDefaultAsync(x => x.Id == productVariantId, ct)
                .ConfigureAwait(false);

            if (variant is null)
            {
                throw new InvalidOperationException("Variant not found.");
            }

            var availableQuantity = totals?.Available ?? 0;
            var reservedQuantity = totals?.Reserved ?? 0;

            variant.StockOnHand = checked(availableQuantity + reservedQuantity);
            variant.StockReserved = reservedQuantity;
        }
    }
}
