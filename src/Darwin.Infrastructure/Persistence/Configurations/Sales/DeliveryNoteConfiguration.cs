using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Sales;

public sealed class DeliveryNoteConfiguration : IEntityTypeConfiguration<DeliveryNote>
{
    public void Configure(EntityTypeBuilder<DeliveryNote> builder)
    {
        builder.ToTable("DeliveryNotes", schema: "Sales");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DeliveryNoteNumber).HasMaxLength(50);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        builder.Property(x => x.Carrier).HasMaxLength(100);
        builder.Property(x => x.Service).HasMaxLength(100);
        builder.Property(x => x.TrackingNumber).HasMaxLength(120);
        builder.Property(x => x.ProviderShipmentReference).HasMaxLength(160);
        builder.Property(x => x.ShippingAddressJson).IsRequired().HasMaxLength(16000);
        builder.Property(x => x.InternalNotes).HasMaxLength(2000);
        builder.Property(x => x.MetadataJson).IsRequired().HasMaxLength(16000);

        builder.HasIndex(x => x.DeliveryNoteNumber)
            .IsUnique()
            .HasDatabaseName("IX_DeliveryNotes_DeliveryNoteNumber")
            .HasFilter("[DeliveryNoteNumber] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(x => x.OrderId).HasDatabaseName("IX_DeliveryNotes_OrderId");
        builder.HasIndex(x => x.ShipmentId)
            .IsUnique()
            .HasDatabaseName("IX_DeliveryNotes_ShipmentId")
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.BusinessId).HasDatabaseName("IX_DeliveryNotes_BusinessId");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("IX_DeliveryNotes_CustomerId");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_DeliveryNotes_Status");
        builder.HasIndex(x => x.IssuedAtUtc).HasDatabaseName("IX_DeliveryNotes_IssuedAtUtc");
        builder.HasIndex(x => x.CreatedAtUtc).HasDatabaseName("IX_DeliveryNotes_CreatedAtUtc");

        builder.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Shipment>().WithMany().HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.PreparedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.IssuedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ShippedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.DeliveredByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CancelledByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.DeliveryNoteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class DeliveryNoteLineConfiguration : IEntityTypeConfiguration<DeliveryNoteLine>
{
    public void Configure(EntityTypeBuilder<DeliveryNoteLine> builder)
    {
        builder.ToTable("DeliveryNoteLines", schema: "Sales");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(250);
        builder.Property(x => x.Sku).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.TaxRate).HasPrecision(18, 4);

        builder.HasIndex(x => x.DeliveryNoteId).HasDatabaseName("IX_DeliveryNoteLines_DeliveryNoteId");
        builder.HasIndex(x => x.OrderLineId).HasDatabaseName("IX_DeliveryNoteLines_OrderLineId");
        builder.HasIndex(x => x.ProductVariantId).HasDatabaseName("IX_DeliveryNoteLines_ProductVariantId");
        builder.HasIndex(x => new { x.DeliveryNoteId, x.SortOrder }).HasDatabaseName("IX_DeliveryNoteLines_Note_SortOrder");
    }
}
