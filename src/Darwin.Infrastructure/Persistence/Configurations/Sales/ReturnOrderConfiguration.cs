using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Sales;

public sealed class ReturnOrderConfiguration : IEntityTypeConfiguration<ReturnOrder>
{
    public void Configure(EntityTypeBuilder<ReturnOrder> builder)
    {
        builder.ToTable("ReturnOrders", schema: "Sales");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReturnOrderNumber).HasMaxLength(50);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        builder.Property(x => x.CustomerSnapshotJson).IsRequired().HasMaxLength(16000);
        builder.Property(x => x.ShippingAddressJson).IsRequired().HasMaxLength(16000);
        builder.Property(x => x.InternalNotes).HasMaxLength(2000);
        builder.Property(x => x.MetadataJson).IsRequired().HasMaxLength(16000);

        builder.HasIndex(x => x.ReturnOrderNumber)
            .IsUnique()
            .HasDatabaseName("IX_ReturnOrders_ReturnOrderNumber")
            .HasFilter("[ReturnOrderNumber] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(x => x.OrderId).HasDatabaseName("IX_ReturnOrders_OrderId");
        builder.HasIndex(x => x.ShipmentId).HasDatabaseName("IX_ReturnOrders_ShipmentId");
        builder.HasIndex(x => x.InvoiceId).HasDatabaseName("IX_ReturnOrders_InvoiceId");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_ReturnOrders_Status");
        builder.HasIndex(x => x.BusinessId).HasDatabaseName("IX_ReturnOrders_BusinessId");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("IX_ReturnOrders_CustomerId");
        builder.HasIndex(x => x.ApprovedAtUtc).HasDatabaseName("IX_ReturnOrders_ApprovedAtUtc");
        builder.HasIndex(x => x.ReceivedAtUtc).HasDatabaseName("IX_ReturnOrders_ReceivedAtUtc");
        builder.HasIndex(x => x.InspectedAtUtc).HasDatabaseName("IX_ReturnOrders_InspectedAtUtc");
        builder.HasIndex(x => x.RefundedAtUtc).HasDatabaseName("IX_ReturnOrders_RefundedAtUtc");
        builder.HasIndex(x => x.CreatedAtUtc).HasDatabaseName("IX_ReturnOrders_CreatedAtUtc");

        builder.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Shipment>().WithMany().HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Invoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.RejectedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ReturnShipmentQueuedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ReceivedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.InspectedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.RefundReadyByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.RefundedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ClosedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CancelledByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.ReturnOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.RefundLinks)
            .WithOne()
            .HasForeignKey(x => x.ReturnOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ReturnOrderLineConfiguration : IEntityTypeConfiguration<ReturnOrderLine>
{
    public void Configure(EntityTypeBuilder<ReturnOrderLine> builder)
    {
        builder.ToTable("ReturnOrderLines", schema: "Sales");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(250);
        builder.Property(x => x.Sku).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.TaxRate).HasPrecision(18, 4);
        builder.Property(x => x.Disposition).IsRequired().HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(x => x.ReturnOrderId).HasDatabaseName("IX_ReturnOrderLines_ReturnOrderId");
        builder.HasIndex(x => x.OrderLineId).HasDatabaseName("IX_ReturnOrderLines_OrderLineId");
        builder.HasIndex(x => x.ShipmentLineId).HasDatabaseName("IX_ReturnOrderLines_ShipmentLineId");
        builder.HasIndex(x => x.ProductVariantId).HasDatabaseName("IX_ReturnOrderLines_ProductVariantId");
        builder.HasIndex(x => x.RestockWarehouseId).HasDatabaseName("IX_ReturnOrderLines_RestockWarehouseId");
        builder.HasIndex(x => new { x.ReturnOrderId, x.SortOrder }).HasDatabaseName("IX_ReturnOrderLines_ReturnOrder_SortOrder");

        builder.HasOne<OrderLine>().WithMany().HasForeignKey(x => x.OrderLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ShipmentLine>().WithMany().HasForeignKey(x => x.ShipmentLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.RestockWarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ReturnOrderRefundLinkConfiguration : IEntityTypeConfiguration<ReturnOrderRefundLink>
{
    public void Configure(EntityTypeBuilder<ReturnOrderRefundLink> builder)
    {
        builder.ToTable("ReturnOrderRefundLinks", schema: "Sales");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => x.ReturnOrderId).HasDatabaseName("IX_ReturnOrderRefundLinks_ReturnOrderId");
        builder.HasIndex(x => x.RefundId).HasDatabaseName("IX_ReturnOrderRefundLinks_RefundId");
        builder.HasIndex(x => new { x.ReturnOrderId, x.RefundId })
            .IsUnique()
            .HasDatabaseName("IX_ReturnOrderRefundLinks_ReturnOrder_Refund")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne<Refund>().WithMany().HasForeignKey(x => x.RefundId).OnDelete(DeleteBehavior.Restrict);
    }
}
