using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Integration;

public sealed class SyncStateConfiguration : IEntityTypeConfiguration<SyncState>
{
    public void Configure(EntityTypeBuilder<SyncState> builder)
    {
        builder.ToTable("SyncStates", schema: "Integration");

        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.SyncScope).HasMaxLength(64).IsRequired();
        builder.Property(x => x.LastErrorCode).HasMaxLength(128);
        builder.Property(x => x.LastErrorSummary).HasMaxLength(1024);
        builder.Property(x => x.RemoteVersion).HasMaxLength(256);
        builder.Property(x => x.LocalVersion).HasMaxLength(256);
        builder.Property(x => x.MetadataJson).HasMaxLength(4000).IsRequired();

        builder.HasOne(x => x.ExternalSystem)
            .WithMany()
            .HasForeignKey(x => x.ExternalSystemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.EntityType, x.EntityId })
            .HasDatabaseName("IX_SyncStates_EntityType_EntityId");

        builder.HasIndex(x => new { x.ExternalSystemId, x.Status, x.NextRetryAtUtc })
            .HasDatabaseName("IX_SyncStates_System_Status_NextRetry");

        builder.HasIndex(x => new { x.ExternalSystemId, x.EntityType, x.EntityId, x.Direction, x.SyncScope })
            .IsUnique()
            .HasDatabaseName("UX_SyncStates_System_Entity_Direction_Scope")
            .HasFilter("[IsDeleted] = 0");
    }
}
