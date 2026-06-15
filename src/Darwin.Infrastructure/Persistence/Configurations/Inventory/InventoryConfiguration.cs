using Darwin.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Inventory
{
    /// <summary>
    /// Configures inventory, warehouse, supplier, and procurement entities.
    /// </summary>
    public sealed class InventoryConfiguration :
        IEntityTypeConfiguration<Warehouse>,
        IEntityTypeConfiguration<StockLevel>,
        IEntityTypeConfiguration<StockTransfer>,
        IEntityTypeConfiguration<StockTransferLine>,
        IEntityTypeConfiguration<Supplier>,
        IEntityTypeConfiguration<PurchaseOrder>,
        IEntityTypeConfiguration<PurchaseOrderLine>,
        IEntityTypeConfiguration<GoodsReceipt>,
        IEntityTypeConfiguration<GoodsReceiptLine>,
        IEntityTypeConfiguration<InventoryTransaction>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<Warehouse> builder)
        {
            builder.ToTable("Warehouses", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Description).HasMaxLength(2000);
            builder.Property(x => x.Location).HasMaxLength(500);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => new { x.BusinessId, x.Name })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            builder.HasIndex(x => new { x.BusinessId, x.IsDefault });

            builder.HasMany(x => x.StockLevels)
                .WithOne()
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<StockLevel> builder)
        {
            builder.ToTable("StockLevels", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AvailableQuantity).IsRequired();
            builder.Property(x => x.ReservedQuantity).IsRequired();
            builder.Property(x => x.ReorderPoint).IsRequired();
            builder.Property(x => x.ReorderQuantity).IsRequired();
            builder.Property(x => x.InTransitQuantity).IsRequired();

            builder.HasIndex(x => x.WarehouseId);
            builder.HasIndex(x => x.ProductVariantId);
            builder.HasIndex(x => new { x.WarehouseId, x.ProductVariantId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<StockTransfer> builder)
        {
            builder.ToTable("StockTransfers", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status).IsRequired();

            builder.HasIndex(x => x.FromWarehouseId);
            builder.HasIndex(x => x.ToWarehouseId);
            builder.HasIndex(x => x.Status);

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.StockTransferId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<StockTransferLine> builder)
        {
            builder.ToTable("StockTransferLines", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity).IsRequired();

            builder.HasIndex(x => x.StockTransferId);
            builder.HasIndex(x => x.ProductVariantId);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<Supplier> builder)
        {
            builder.ToTable("Suppliers", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Code).HasMaxLength(64);
            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.Email).IsRequired().HasMaxLength(320);
            builder.Property(x => x.Phone).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Address).HasMaxLength(500);
            builder.Property(x => x.Notes).HasMaxLength(4000);
            builder.Property(x => x.PreferredCurrency).HasMaxLength(3);
            builder.Property(x => x.Website).HasMaxLength(500);
            builder.Property(x => x.TaxRegistrationNumber).HasMaxLength(100);
            builder.Property(x => x.ExternalNotes).HasMaxLength(2000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => new { x.BusinessId, x.Name });
            builder.HasIndex(x => new { x.BusinessId, x.Status });
            builder.HasIndex(x => new { x.BusinessId, x.Code })
                .IsUnique()
                .HasFilter("[Code] IS NOT NULL AND [IsDeleted] = 0");

            builder.HasMany(x => x.PurchaseOrders)
                .WithOne()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
        {
            builder.ToTable("PurchaseOrders", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.OrderNumber).IsRequired().HasMaxLength(100);
            builder.Property(x => x.OrderedAtUtc).IsRequired();
            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("EUR");
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.Status).IsRequired();

            builder.HasIndex(x => x.SupplierId);
            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.ExpectedDeliveryDateUtc);
            builder.HasIndex(x => x.IssuedAtUtc);
            builder.HasIndex(x => x.ReceivedAtUtc);
            builder.HasIndex(x => x.CancelledAtUtc);
            builder.HasIndex(x => new { x.BusinessId, x.OrderNumber })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
        {
            builder.ToTable("PurchaseOrderLines", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity).IsRequired();
            builder.Property(x => x.SupplierSku).HasMaxLength(100);
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.ReceivedQuantity).IsRequired();
            builder.Property(x => x.CancelledQuantity).IsRequired();
            builder.Property(x => x.UnitCostMinor).IsRequired();
            builder.Property(x => x.TotalCostMinor).IsRequired();

            builder.HasIndex(x => x.PurchaseOrderId);
            builder.HasIndex(x => x.ProductVariantId);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
        {
            builder.ToTable("GoodsReceipts", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.GoodsReceiptNumber).HasMaxLength(100);
            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.SupplierId);
            builder.HasIndex(x => x.PurchaseOrderId);
            builder.HasIndex(x => x.WarehouseId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.ReceivedAtUtc);
            builder.HasIndex(x => x.PostedAtUtc);
            builder.HasIndex(x => new { x.BusinessId, x.GoodsReceiptNumber })
                .IsUnique()
                .HasFilter("[GoodsReceiptNumber] IS NOT NULL AND [IsDeleted] = 0");

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<GoodsReceiptLine> builder)
        {
            builder.ToTable("GoodsReceiptLines", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SupplierSku).HasMaxLength(100);
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.OrderedQuantity).IsRequired();
            builder.Property(x => x.PreviouslyReceivedQuantity).IsRequired();
            builder.Property(x => x.ReceivedQuantity).IsRequired();
            builder.Property(x => x.AcceptedQuantity).IsRequired();
            builder.Property(x => x.RejectedQuantity).IsRequired();
            builder.Property(x => x.DamagedQuantity).IsRequired();
            builder.Property(x => x.UnitCostMinor).IsRequired();
            builder.Property(x => x.TotalCostMinor).IsRequired();
            builder.Property(x => x.SortOrder).IsRequired();

            builder.HasIndex(x => x.GoodsReceiptId);
            builder.HasIndex(x => x.PurchaseOrderLineId);
            builder.HasIndex(x => x.ProductVariantId);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
        {
            builder.ToTable("InventoryTransactions", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.QuantityDelta).IsRequired();
            builder.Property(x => x.Reason).IsRequired().HasMaxLength(500);

            builder.HasIndex(x => x.WarehouseId);
            builder.HasIndex(x => x.ProductVariantId);
            builder.HasIndex(x => x.ReferenceId);
        }
    }
}
