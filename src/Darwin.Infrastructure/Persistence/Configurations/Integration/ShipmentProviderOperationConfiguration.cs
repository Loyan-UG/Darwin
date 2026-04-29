using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Integration;

public sealed class ShipmentProviderOperationConfiguration : IEntityTypeConfiguration<ShipmentProviderOperation>
{
    public void Configure(EntityTypeBuilder<ShipmentProviderOperation> builder)
    {
        builder.ToTable("ShipmentProviderOperations", schema: "Integration");

        builder.Property(x => x.Provider).HasMaxLength(32).IsRequired();
        builder.Property(x => x.OperationType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(1024);

        builder.HasIndex(x => new { x.ShipmentId, x.Provider, x.OperationType, x.Status, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.ShipmentId, x.Provider, x.OperationType })
            .IsUnique()
            .HasDatabaseName("UX_ShipmentProviderOperations_ActivePending")
            .HasFilter("[IsDeleted] = 0 AND [Status] = N'Pending'");
    }
}
