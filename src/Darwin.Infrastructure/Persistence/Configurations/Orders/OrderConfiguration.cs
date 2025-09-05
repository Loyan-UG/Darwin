using Darwin.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Orders
{
    public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> b)
        {
            b.ToTable("Orders");
            b.Property(x => x.OrderNumber).IsRequired().HasMaxLength(50);

            b.HasIndex(x => x.OrderNumber).IsUnique().HasFilter("[IsDeleted] = 0");

            b.HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(o => o.Payments).WithOne().HasForeignKey(p => p.OrderId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(o => o.Shipments).WithOne().HasForeignKey(s => s.OrderId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
    {
        public void Configure(EntityTypeBuilder<OrderLine> b)
        {
            b.ToTable("OrderLines");
            b.Property(x => x.Sku).IsRequired().HasMaxLength(100);
        }
    }
}
