using Darwin.Domain.Entities.Inventory;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds inventory baseline for the multi-warehouse model:
    /// - ensures a default warehouse exists,
    /// - creates initial stock levels for known seed variants,
    /// - creates a few inventory transactions.
    /// </summary>
    public sealed class InventorySeedSection
    {
        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            var warehouse = await EnsureDefaultWarehouseAsync(db, ct);
            await EnsureSeedStockLevelsAsync(db, warehouse.Id, ct);
            await EnsureSeedTransactionsAsync(db, warehouse.Id, ct);
        }

        private static async Task<Warehouse> EnsureDefaultWarehouseAsync(DarwinDbContext db, CancellationToken ct)
        {
            var existingDefault = await db.Warehouses.FirstOrDefaultAsync(x => x.IsDefault, ct);
            if (existingDefault is not null)
                return existingDefault;

            var warehouse = new Warehouse
            {
                Name = "Main warehouse",
                AddressJson = "{}",
                IsDefault = true
            };

            db.Warehouses.Add(warehouse);
            await db.SaveChangesAsync(ct);
            return warehouse;
        }

        private static async Task EnsureSeedStockLevelsAsync(DarwinDbContext db, Guid warehouseId, CancellationToken ct)
        {
            var seededVariantIds = new[]
            {
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("22222222-2222-2222-2222-222222222222")
            };

            var existing = await db.StockLevels
                .Where(x => x.WarehouseId == warehouseId)
                .Select(x => x.VariantId)
                .ToListAsync(ct);

            var missing = seededVariantIds.Except(existing).ToList();
            if (missing.Count == 0)
                return;

            var variants = await db.ProductVariants
                .Where(v => missing.Contains(v.Id))
                .Select(v => v.Id)
                .ToListAsync(ct);

            var levels = new List<StockLevel>();
            foreach (var variantId in variants)
            {
                levels.Add(new StockLevel
                {
                    WarehouseId = warehouseId,
                    VariantId = variantId,
                    OnHand = variantId == seededVariantIds[0] ? 100 : 80,
                    Reserved = 0,
                    ReorderPoint = 10
                });
            }

            if (levels.Count > 0)
            {
                db.StockLevels.AddRange(levels);
                await db.SaveChangesAsync(ct);
            }
        }

        private static async Task EnsureSeedTransactionsAsync(DarwinDbContext db, Guid warehouseId, CancellationToken ct)
        {
            if (await db.InventoryTransactions.AnyAsync(ct))
                return;

            var v1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var v2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

            db.InventoryTransactions.AddRange(
                new InventoryTransaction
                {
                    WarehouseId = warehouseId,
                    VariantId = v1,
                    QuantityDelta = +100,
                    Reason = "Seed.GoodsReceipt",
                    ReferenceId = null
                },
                new InventoryTransaction
                {
                    WarehouseId = warehouseId,
                    VariantId = v1,
                    QuantityDelta = -2,
                    Reason = "Seed.ManualAdjustment",
                    ReferenceId = null
                },
                new InventoryTransaction
                {
                    WarehouseId = warehouseId,
                    VariantId = v2,
                    QuantityDelta = +80,
                    Reason = "Seed.GoodsReceipt",
                    ReferenceId = null
                }
            );

            await db.SaveChangesAsync(ct);
        }
    }
}
