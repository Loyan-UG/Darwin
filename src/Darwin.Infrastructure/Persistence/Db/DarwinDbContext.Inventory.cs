using Darwin.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Persistence.Db
{
    /// <summary>
    /// Inventory and procurement DbSets.
    /// </summary>
    public sealed partial class DarwinDbContext
    {
        /// <summary>
        /// Warehouses.
        /// </summary>
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();

        /// <summary>
        /// Stock levels by warehouse and variant.
        /// </summary>
        public DbSet<StockLevel> StockLevels => Set<StockLevel>();

        /// <summary>
        /// Warehouse-to-warehouse stock transfers.
        /// </summary>
        public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();

        /// <summary>
        /// Stock transfer lines.
        /// </summary>
        public DbSet<StockTransferLine> StockTransferLines => Set<StockTransferLine>();

        /// <summary>
        /// Suppliers.
        /// </summary>
        public DbSet<Supplier> Suppliers => Set<Supplier>();

        /// <summary>
        /// Purchase orders.
        /// </summary>
        public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

        /// <summary>
        /// Purchase order lines.
        /// </summary>
        public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();

        /// <summary>
        /// Inventory movement ledger.
        /// </summary>
        public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    }
}
