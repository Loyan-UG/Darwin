using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Integration;

public sealed class SyncConflictConfiguration : IEntityTypeConfiguration<SyncConflict>
{
    public void Configure(EntityTypeBuilder<SyncConflict> builder)
    {
        builder.ToTable("SyncConflicts", schema: "Integration");

        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Resolution).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ConflictKey).HasMaxLength(256).IsRequired();
        builder.Property(x => x.FieldPath).HasMaxLength(256);
        builder.Property(x => x.DarwinValueSummary).HasMaxLength(1024);
        builder.Property(x => x.ExternalValueSummary).HasMaxLength(1024);
        builder.Property(x => x.ResolutionSummary).HasMaxLength(1024);
        builder.Property(x => x.MetadataJson).HasMaxLength(4000).IsRequired();

        builder.HasOne(x => x.SyncState)
            .WithMany(x => x.Conflicts)
            .HasForeignKey(x => x.SyncStateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ExternalSystem)
            .WithMany()
            .HasForeignKey(x => x.ExternalSystemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.EntityType, x.EntityId })
            .HasDatabaseName("IX_SyncConflicts_EntityType_EntityId");

        builder.HasIndex(x => new { x.ExternalSystemId, x.Status, x.DetectedAtUtc })
            .HasDatabaseName("IX_SyncConflicts_System_Status_Detected");

        builder.HasIndex(x => new { x.SyncStateId, x.Status })
            .HasDatabaseName("IX_SyncConflicts_State_Status");

        builder.HasIndex(x => new { x.ExternalSystemId, x.EntityType, x.EntityId, x.ConflictKey })
            .IsUnique()
            .HasDatabaseName("UX_SyncConflicts_System_Entity_Key")
            .HasFilter("[IsDeleted] = 0");
    }
}
