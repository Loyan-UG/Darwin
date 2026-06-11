using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Integration;

public sealed class ExternalReferenceConfiguration : IEntityTypeConfiguration<ExternalReference>
{
    public void Configure(EntityTypeBuilder<ExternalReference> builder)
    {
        builder.ToTable("ExternalReferences", schema: "Integration");

        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ReferenceKind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ExternalId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ExternalDisplayId).HasMaxLength(256);
        builder.Property(x => x.SourceOfTruth).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.MetadataJson).HasMaxLength(4000).IsRequired();

        builder.HasOne(x => x.ExternalSystem)
            .WithMany(x => x.References)
            .HasForeignKey(x => x.ExternalSystemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.EntityType, x.EntityId })
            .HasDatabaseName("IX_ExternalReferences_EntityType_EntityId");

        builder.HasIndex(x => new { x.ExternalSystemId, x.EntityType, x.ReferenceKind, x.ExternalId })
            .IsUnique()
            .HasDatabaseName("UX_ExternalReferences_System_EntityType_Kind_ExternalId")
            .HasFilter("[IsDeleted] = 0");
    }
}
