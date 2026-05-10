using Darwin.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Orders;

public sealed class ShipmentLineConfiguration : IEntityTypeConfiguration<ShipmentLine>
{
    public void Configure(EntityTypeBuilder<ShipmentLine> builder)
    {
        builder.ToTable("ShipmentLines", schema: "Orders");
    }
}

public sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("Refunds", schema: "Orders");
        builder.Property(x => x.Provider).HasMaxLength(64);
        builder.Property(x => x.ProviderRefundReference).HasMaxLength(128);
        builder.Property(x => x.ProviderPaymentReference).HasMaxLength(128);
        builder.Property(x => x.ProviderStatus).HasMaxLength(64);
        builder.Property(x => x.FailureReason).HasMaxLength(512);
    }
}
