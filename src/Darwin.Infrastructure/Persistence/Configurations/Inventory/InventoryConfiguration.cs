using Darwin.Domain.Common;
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
        IEntityTypeConfiguration<WarehouseLocation>,
        IEntityTypeConfiguration<ProductTrackingPolicy>,
        IEntityTypeConfiguration<InventoryLot>,
        IEntityTypeConfiguration<InventorySerialUnit>,
        IEntityTypeConfiguration<HandlingUnit>,
        IEntityTypeConfiguration<HandlingUnitContent>,
        IEntityTypeConfiguration<WarehouseLabelTemplate>,
        IEntityTypeConfiguration<WarehouseTask>,
        IEntityTypeConfiguration<WarehouseTaskLine>,
        IEntityTypeConfiguration<WarehouseTaskLineIdentity>,
        IEntityTypeConfiguration<StockCountSession>,
        IEntityTypeConfiguration<StockCountLine>,
        IEntityTypeConfiguration<StockCountLineIdentity>,
        IEntityTypeConfiguration<StockLevel>,
        IEntityTypeConfiguration<StockTransfer>,
        IEntityTypeConfiguration<StockTransferLine>,
        IEntityTypeConfiguration<StockTransferLineIdentity>,
        IEntityTypeConfiguration<Supplier>,
        IEntityTypeConfiguration<SupplierContact>,
        IEntityTypeConfiguration<PurchaseOrder>,
        IEntityTypeConfiguration<PurchaseOrderLine>,
        IEntityTypeConfiguration<GoodsReceipt>,
        IEntityTypeConfiguration<GoodsReceiptLine>,
        IEntityTypeConfiguration<GoodsReceiptLineIdentity>,
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

            builder.HasMany(x => x.Locations)
                .WithOne()
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<WarehouseLocation> builder)
        {
            builder.ToTable("WarehouseLocations", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
            builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
            builder.Property(x => x.LocationType).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Barcode).HasMaxLength(128);
            builder.Property(x => x.SortOrder).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.WarehouseId);
            builder.HasIndex(x => x.ParentLocationId);
            builder.HasIndex(x => x.LocationType);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.Barcode);
            builder.HasIndex(x => x.SortOrder);
            builder.HasIndex(x => new { x.BusinessId, x.WarehouseId, x.Code })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasMany(x => x.Children)
                .WithOne()
                .HasForeignKey(x => x.ParentLocationId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<ProductTrackingPolicy> builder)
        {
            builder.ToTable("ProductTrackingPolicies", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TrackingMode).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.ProductVariantId);
            builder.HasIndex(x => x.TrackingMode);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => new { x.BusinessId, x.ProductVariantId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<InventoryLot> builder)
        {
            builder.ToTable("InventoryLots", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.LotCode).IsRequired().HasMaxLength(100);
            builder.Property(x => x.SupplierLotCode).HasMaxLength(100);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.ProductVariantId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.ExpiryDateUtc);
            builder.HasIndex(x => x.SupplierLotCode);
            builder.HasIndex(x => new { x.BusinessId, x.ProductVariantId, x.LotCode })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<InventorySerialUnit> builder)
        {
            builder.ToTable("InventorySerialUnits", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SerialNumber).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.ProductVariantId);
            builder.HasIndex(x => x.InventoryLotId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.ExpiryDateUtc);
            builder.HasIndex(x => new { x.BusinessId, x.ProductVariantId, x.SerialNumber })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<HandlingUnit> builder)
        {
            builder.ToTable("HandlingUnits", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code).IsRequired().HasMaxLength(100);
            builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Barcode).HasMaxLength(128);
            builder.Property(x => x.HandlingUnitType).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.WarehouseId);
            builder.HasIndex(x => x.LocationId);
            builder.HasIndex(x => x.ParentHandlingUnitId);
            builder.HasIndex(x => x.HandlingUnitType);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.Barcode);
            builder.HasIndex(x => new { x.BusinessId, x.Code })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasMany(x => x.Children)
                .WithOne()
                .HasForeignKey(x => x.ParentHandlingUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Contents)
                .WithOne()
                .HasForeignKey(x => x.HandlingUnitId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<HandlingUnitContent> builder)
        {
            builder.ToTable("HandlingUnitContents", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity).IsRequired();
            builder.Property(x => x.SkuSnapshot).HasMaxLength(100);
            builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
            builder.Property(x => x.SortOrder).IsRequired();
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.HandlingUnitId);
            builder.HasIndex(x => x.ProductVariantId);
            builder.HasIndex(x => x.InventoryLotId);
            builder.HasIndex(x => x.InventorySerialUnitId);
            builder.HasIndex(x => x.SortOrder);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<WarehouseLabelTemplate> builder)
        {
            builder.ToTable("WarehouseLabelTemplates", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.TemplateKey).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Format).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.WidthMm).IsRequired();
            builder.Property(x => x.HeightMm).IsRequired();
            builder.Property(x => x.ContentTemplate).IsRequired().HasMaxLength(8000);
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.Format);
            builder.HasIndex(x => new { x.BusinessId, x.TemplateKey })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            builder.HasIndex(x => new { x.BusinessId, x.IsDefault });
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<WarehouseTask> builder)
        {
            builder.ToTable("WarehouseTasks", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TaskNumber).HasMaxLength(100);
            builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
            builder.Property(x => x.TaskType).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.WarehouseId);
            builder.HasIndex(x => x.FromLocationId);
            builder.HasIndex(x => x.ToLocationId);
            builder.HasIndex(x => x.AssignedToUserId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.Priority);
            builder.HasIndex(x => x.TaskType);
            builder.HasIndex(x => x.SourceType);
            builder.HasIndex(x => x.SourceEntityId);
            builder.HasIndex(x => x.DueAtUtc);
            builder.HasIndex(x => x.ReadyAtUtc);
            builder.HasIndex(x => x.CompletedAtUtc);
            builder.HasIndex(x => new { x.BusinessId, x.TaskNumber })
                .IsUnique()
                .HasFilter("[TaskNumber] IS NOT NULL AND [IsDeleted] = 0");

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.WarehouseTaskId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<WarehouseTaskLine> builder)
        {
            builder.ToTable("WarehouseTaskLines", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SkuSnapshot).HasMaxLength(100);
            builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
            builder.Property(x => x.RequestedQuantity).IsRequired();
            builder.Property(x => x.CompletedQuantity).IsRequired();
            builder.Property(x => x.ShortQuantity).IsRequired();
            builder.Property(x => x.ShortReason).HasMaxLength(1000);
            builder.Property(x => x.SortOrder).IsRequired();
            builder.Property(x => x.SourceLineType).HasMaxLength(100);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.WarehouseTaskId);
            builder.HasIndex(x => x.ProductVariantId);
            builder.HasIndex(x => x.FromLocationId);
            builder.HasIndex(x => x.ToLocationId);
            builder.HasIndex(x => x.SourceLineId);
            builder.HasIndex(x => x.SortOrder);
            builder.HasIndex(x => x.ShortQuantity);

            builder.HasMany(x => x.Identities)
                .WithOne()
                .HasForeignKey(x => x.WarehouseTaskLineId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<WarehouseTaskLineIdentity> builder)
        {
            ConfigureInventoryIdentityEvidence(builder, "WarehouseTaskLineIdentities", "WarehouseTaskLineId");
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<StockCountSession> builder)
        {
            builder.ToTable("StockCountSessions", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CountNumber).HasMaxLength(100);
            builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
            builder.Property(x => x.CountType).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.ReviewNotes).HasMaxLength(2000);
            builder.Property(x => x.InternalNotes).HasMaxLength(4000);
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.WarehouseId);
            builder.HasIndex(x => x.LocationId);
            builder.HasIndex(x => x.AssignedToUserId);
            builder.HasIndex(x => x.CountType);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CountWindowStartUtc);
            builder.HasIndex(x => x.PreparedAtUtc);
            builder.HasIndex(x => x.PostedAtUtc);
            builder.HasIndex(x => new { x.BusinessId, x.CountNumber })
                .IsUnique()
                .HasFilter("[CountNumber] IS NOT NULL AND [IsDeleted] = 0");

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.StockCountSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<StockCountLine> builder)
        {
            builder.ToTable("StockCountLines", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SkuSnapshot).HasMaxLength(100);
            builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
            builder.Property(x => x.ExpectedQuantity).IsRequired();
            builder.Property(x => x.CountedQuantity).IsRequired();
            builder.Property(x => x.VarianceQuantity).IsRequired();
            builder.Property(x => x.ReviewStatus).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.ReviewNotes).HasMaxLength(2000);
            builder.Property(x => x.SortOrder).IsRequired();
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.StockCountSessionId);
            builder.HasIndex(x => x.ProductVariantId);
            builder.HasIndex(x => x.LocationId);
            builder.HasIndex(x => x.ReviewStatus);
            builder.HasIndex(x => x.AdjustmentPosted);
            builder.HasIndex(x => x.SortOrder);
            builder.HasIndex(x => new { x.StockCountSessionId, x.ProductVariantId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasMany(x => x.Identities)
                .WithOne()
                .HasForeignKey(x => x.StockCountLineId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<StockCountLineIdentity> builder)
        {
            ConfigureInventoryIdentityEvidence(builder, "StockCountLineIdentities", "StockCountLineId");
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

            builder.HasMany(x => x.Identities)
                .WithOne()
                .HasForeignKey(x => x.StockTransferLineId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<StockTransferLineIdentity> builder)
        {
            ConfigureInventoryIdentityEvidence(builder, "StockTransferLineIdentities", "StockTransferLineId");
        }

        private static void ConfigureInventoryIdentityEvidence<TEntity>(EntityTypeBuilder<TEntity> builder, string tableName, string lineIdProperty)
            where TEntity : BaseEntity
        {
            builder.ToTable(tableName, schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property<int>("Quantity").IsRequired();
            builder.Property<string?>("LotCodeSnapshot").HasMaxLength(100);
            builder.Property<string?>("SupplierLotCodeSnapshot").HasMaxLength(100);
            builder.Property<string?>("SerialNumberSnapshot").HasMaxLength(128);
            builder.Property<string?>("HandlingUnitCodeSnapshot").HasMaxLength(128);
            builder.Property<int>("SortOrder").IsRequired();
            builder.Property<string?>("MetadataJson").HasMaxLength(8000);

            builder.HasIndex(lineIdProperty);
            builder.HasIndex("InventoryLotId");
            builder.HasIndex("InventorySerialUnitId");
            builder.HasIndex("HandlingUnitId");
            builder.HasIndex("SortOrder");
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

            builder.HasMany(x => x.Contacts)
                .WithOne()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<SupplierContact> builder)
        {
            builder.ToTable("SupplierContacts", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(64).IsRequired();
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.JobTitle).HasMaxLength(200);
            builder.Property(x => x.Email).HasMaxLength(320);
            builder.Property(x => x.Phone).HasMaxLength(50);
            builder.Property(x => x.LanguageCode).HasMaxLength(16);
            builder.Property(x => x.Notes).HasMaxLength(1000);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.SupplierId);
            builder.HasIndex(x => x.Role);
            builder.HasIndex(x => new { x.SupplierId, x.IsPrimary });
            builder.HasIndex(x => new { x.BusinessId, x.SupplierId, x.Role, x.Email })
                .IsUnique()
                .HasFilter("[Email] IS NOT NULL AND [IsDeleted] = 0");
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

            builder.HasMany(x => x.Identities)
                .WithOne()
                .HasForeignKey(x => x.GoodsReceiptLineId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<GoodsReceiptLineIdentity> builder)
        {
            builder.ToTable("GoodsReceiptLineIdentities", schema: "Inventory");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.GoodsReceiptLineId).IsRequired();
            builder.Property(x => x.ProductVariantId).IsRequired();
            builder.Property(x => x.Quantity).IsRequired();
            builder.Property(x => x.LotCodeSnapshot).HasMaxLength(100);
            builder.Property(x => x.SupplierLotCodeSnapshot).HasMaxLength(100);
            builder.Property(x => x.SerialNumberSnapshot).HasMaxLength(128);
            builder.Property(x => x.HandlingUnitCodeSnapshot).HasMaxLength(100);
            builder.Property(x => x.SortOrder).IsRequired();
            builder.Property(x => x.MetadataJson).HasMaxLength(8000);

            builder.HasIndex(x => x.GoodsReceiptLineId);
            builder.HasIndex(x => x.ProductVariantId);
            builder.HasIndex(x => x.InventoryLotId);
            builder.HasIndex(x => x.InventorySerialUnitId);
            builder.HasIndex(x => x.HandlingUnitId);
            builder.HasIndex(x => x.ExpiryDateUtc);
            builder.HasIndex(x => new { x.GoodsReceiptLineId, x.InventorySerialUnitId })
                .IsUnique()
                .HasFilter("[InventorySerialUnitId] IS NOT NULL AND [IsDeleted] = 0");
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
