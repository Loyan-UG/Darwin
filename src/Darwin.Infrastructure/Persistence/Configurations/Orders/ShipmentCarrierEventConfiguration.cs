using Darwin.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Orders;

public sealed class ShipmentCarrierEventConfiguration : IEntityTypeConfiguration<ShipmentCarrierEvent>
{
    public void Configure(EntityTypeBuilder<ShipmentCarrierEvent> builder)
    {
        builder.Property(x => x.Carrier).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ProviderShipmentReference).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CarrierEventKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ProviderStatus).HasMaxLength(128);
        builder.Property(x => x.ExceptionCode).HasMaxLength(128);
        builder.Property(x => x.ExceptionMessage).HasMaxLength(512);
        builder.Property(x => x.TrackingNumber).HasMaxLength(128);
        builder.Property(x => x.LabelUrl).HasMaxLength(2048);
        builder.Property(x => x.Service).HasMaxLength(64);

        builder.HasIndex(x => new { x.ShipmentId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.ProviderShipmentReference, x.Carrier, x.OccurredAtUtc });

        builder.HasOne<Shipment>()
            .WithMany(x => x.CarrierEvents)
            .HasForeignKey(x => x.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
