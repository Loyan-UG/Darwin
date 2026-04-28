using Darwin.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Orders;

public sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipments", schema: "Orders");

        builder.Property(x => x.Carrier).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Service).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ProviderShipmentReference).HasMaxLength(128);
        builder.Property(x => x.TrackingNumber).HasMaxLength(128);
        builder.Property(x => x.LabelUrl).HasMaxLength(2048);
        builder.Property(x => x.LastCarrierEventKey).HasMaxLength(128);

        builder.HasIndex(x => x.ProviderShipmentReference);
    }
}
