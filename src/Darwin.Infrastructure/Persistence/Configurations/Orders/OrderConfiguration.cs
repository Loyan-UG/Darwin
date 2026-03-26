using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Shipping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Orders
{
    public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> b)
        {
            b.ToTable("Orders", schema: "Orders");
            b.Property(x => x.OrderNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.ShippingMethodName).HasMaxLength(200);
            b.Property(x => x.ShippingCarrier).HasMaxLength(100);
            b.Property(x => x.ShippingService).HasMaxLength(100);

            b.HasIndex(x => x.OrderNumber).IsUnique().HasFilter("[IsDeleted] = 0");
            b.HasIndex(x => x.ShippingMethodId).HasDatabaseName("IX_Orders_ShippingMethodId");

            b.HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(o => o.Payments).WithOne().HasForeignKey(p => p.OrderId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(o => o.Shipments).WithOne().HasForeignKey(s => s.OrderId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<ShippingMethod>()
                .WithMany()
                .HasForeignKey(x => x.ShippingMethodId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
    {
        public void Configure(EntityTypeBuilder<OrderLine> b)
        {
            b.ToTable("OrderLines", schema: "Orders");
            b.Property(x => x.Sku).IsRequired().HasMaxLength(100);
            b.Property(x => x.VatRate).HasPrecision(9, 4);
            b.HasIndex(x => x.WarehouseId).HasDatabaseName("IX_OrderLines_WarehouseId");
            b.HasOne<Warehouse>()
                .WithMany()
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
