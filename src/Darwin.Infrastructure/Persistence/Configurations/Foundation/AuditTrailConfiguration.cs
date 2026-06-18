using Darwin.Domain.Entities.Foundation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Foundation;

public sealed class AuditTrailConfiguration : IEntityTypeConfiguration<AuditTrail>
{
    public void Configure(EntityTypeBuilder<AuditTrail> builder)
    {
        builder.ToTable("AuditTrails", schema: "Foundation");

        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(2000);
        builder.Property(x => x.CorrelationId).HasMaxLength(128);
        builder.Property(x => x.ChangeSetJson).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.MetadataJson).HasMaxLength(4000).IsRequired();

        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAtUtc })
            .HasDatabaseName("IX_AuditTrails_EntityType_EntityId_OccurredAtUtc");

        builder.HasIndex(x => new { x.BusinessId, x.OccurredAtUtc })
            .HasDatabaseName("IX_AuditTrails_BusinessId_OccurredAtUtc");

        builder.HasIndex(x => x.BusinessEventId)
            .HasDatabaseName("IX_AuditTrails_BusinessEventId");

        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_AuditTrails_CorrelationId");
    }
}
