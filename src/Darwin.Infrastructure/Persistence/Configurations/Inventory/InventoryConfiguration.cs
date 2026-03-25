using Darwin.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Inventory
{
    /// <summary>
    /// EF Core configuration for multi-warehouse inventory entities.
    /// </summary>
    public sealed class InventoryConfiguration :
        IEntityTypeConfiguration<Warehouse>,
        IEntityTypeConfiguration<StockLevel>,
        IEntityTypeConfiguration<StockTransfer>,
        IEntityTypeConfiguration<InventoryTransaction>
    {
        public void Configure(EntityTypeBuilder<Warehouse> builder)
        {
            builder.ToTable("Warehouses", schema: "Inventory");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.AddressJson).HasMaxLength(4000);
            builder.HasIndex(x => x.IsDefault);
        }

        public void Configure(EntityTypeBuilder<StockLevel> builder)
        {
            builder.ToTable("StockLevels", schema: "Inventory");
            builder.HasKey(x => new { x.WarehouseId, x.VariantId });
            builder.Property(x => x.OnHand).IsRequired();
            builder.Property(x => x.Reserved).IsRequired();
            builder.HasIndex(x => x.VariantId);
        }

        public void Configure(EntityTypeBuilder<StockTransfer> builder)
        {
            builder.ToTable("StockTransfers", schema: "Inventory");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Quantity).IsRequired();
            builder.Property(x => x.Reason).IsRequired().HasMaxLength(500);
            builder.HasIndex(x => x.FromWarehouseId);
            builder.HasIndex(x => x.ToWarehouseId);
            builder.HasIndex(x => x.VariantId);
        }

        public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
        {
            builder.ToTable("InventoryTransactions", schema: "Inventory");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Reason).IsRequired().HasMaxLength(500);
            builder.HasIndex(x => x.WarehouseId);
            builder.HasIndex(x => x.VariantId);
        }
    }
}
