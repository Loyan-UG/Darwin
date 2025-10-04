using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Inventory;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds a few inventory transactions to support stock-related UI checks.
    /// </summary>
    public sealed class InventorySeedSection
    {
        /// <summary>
        /// Creates several stock movement records for arbitrary (test) variants.
        /// </summary>
        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            if (await db.Set<InventoryTransaction>().AnyAsync(ct)) return;

            var v1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var v2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

            db.AddRange(
                new InventoryTransaction
                {
                    VariantId = v1,
                    QuantityDelta = +50,
                    Reason = "GoodsReceipt",
                    ReferenceId = null
                },
                new InventoryTransaction
                {
                    VariantId = v1,
                    QuantityDelta = -2,
                    Reason = "ManualAdjustment",
                    ReferenceId = null
                },
                new InventoryTransaction
                {
                    VariantId = v2,
                    QuantityDelta = +20,
                    Reason = "GoodsReceipt",
                    ReferenceId = null
                }
            );

            await db.SaveChangesAsync(ct);
        }
    }
}
