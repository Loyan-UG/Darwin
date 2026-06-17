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
        /// Structured warehouse locations and bins.
        /// </summary>
        public DbSet<WarehouseLocation> WarehouseLocations => Set<WarehouseLocation>();

        /// <summary>
        /// Product variant inventory tracking policies.
        /// </summary>
        public DbSet<ProductTrackingPolicy> ProductTrackingPolicies => Set<ProductTrackingPolicy>();

        /// <summary>
        /// Reusable inventory lot identities.
        /// </summary>
        public DbSet<InventoryLot> InventoryLots => Set<InventoryLot>();

        /// <summary>
        /// Unique inventory serial unit identities.
        /// </summary>
        public DbSet<InventorySerialUnit> InventorySerialUnits => Set<InventorySerialUnit>();

        /// <summary>
        /// Grouped-stock handling unit identities.
        /// </summary>
        public DbSet<HandlingUnit> HandlingUnits => Set<HandlingUnit>();

        /// <summary>
        /// Handling unit content records.
        /// </summary>
        public DbSet<HandlingUnitContent> HandlingUnitContents => Set<HandlingUnitContent>();

        /// <summary>
        /// Provider-neutral warehouse label templates.
        /// </summary>
        public DbSet<WarehouseLabelTemplate> WarehouseLabelTemplates => Set<WarehouseLabelTemplate>();

        /// <summary>
        /// Internal warehouse execution tasks.
        /// </summary>
        public DbSet<WarehouseTask> WarehouseTasks => Set<WarehouseTask>();

        /// <summary>
        /// Internal warehouse execution task lines.
        /// </summary>
        public DbSet<WarehouseTaskLine> WarehouseTaskLines => Set<WarehouseTaskLine>();

        /// <summary>
        /// Formal stock count sessions.
        /// </summary>
        public DbSet<StockCountSession> StockCountSessions => Set<StockCountSession>();

        /// <summary>
        /// Formal stock count lines.
        /// </summary>
        public DbSet<StockCountLine> StockCountLines => Set<StockCountLine>();

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
        /// Structured supplier contacts.
        /// </summary>
        public DbSet<SupplierContact> SupplierContacts => Set<SupplierContact>();

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
