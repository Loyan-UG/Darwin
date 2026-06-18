using Darwin.Domain.Entities.Foundation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Foundation;

public sealed class BusinessEventConfiguration : IEntityTypeConfiguration<BusinessEvent>
{
    public void Configure(EntityTypeBuilder<BusinessEvent> builder)
    {
        builder.ToTable("BusinessEvents", schema: "Foundation");

        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EventKey).HasMaxLength(256);
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(2000);
        builder.Property(x => x.CorrelationId).HasMaxLength(128);
        builder.Property(x => x.CausationId).HasMaxLength(128);
        builder.Property(x => x.PayloadJson).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.MetadataJson).HasMaxLength(4000).IsRequired();

        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAtUtc })
            .HasDatabaseName("IX_BusinessEvents_EntityType_EntityId_OccurredAtUtc");

        builder.HasIndex(x => new { x.BusinessId, x.OccurredAtUtc })
            .HasDatabaseName("IX_BusinessEvents_BusinessId_OccurredAtUtc");

        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_BusinessEvents_CorrelationId");

        builder.HasIndex(x => x.EventKey)
            .IsUnique()
            .HasDatabaseName("UX_BusinessEvents_EventKey")
            .HasFilter("[EventKey] IS NOT NULL AND [IsDeleted] = 0");
    }
}
