using Darwin.Domain.Entities.Foundation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Foundation;

public sealed class CustomFieldDefinitionConfiguration : IEntityTypeConfiguration<CustomFieldDefinition>
{
    public void Configure(EntityTypeBuilder<CustomFieldDefinition> builder)
    {
        builder.ToTable("CustomFieldDefinitions", schema: "Foundation");

        builder.Property(x => x.TargetEntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Key).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DataType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ValidationJson).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.MetadataJson).HasMaxLength(4000).IsRequired();

        builder.HasIndex(x => new { x.BusinessId, x.TargetEntityType, x.Key })
            .IsUnique()
            .HasDatabaseName("UX_CustomFieldDefinitions_Business_Target_Key")
            .HasFilter("[BusinessId] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasIndex(x => new { x.TargetEntityType, x.Key })
            .IsUnique()
            .HasDatabaseName("UX_CustomFieldDefinitions_Global_Target_Key")
            .HasFilter("[BusinessId] IS NULL AND [IsDeleted] = 0");
    }
}

public sealed class CustomFieldValueConfiguration : IEntityTypeConfiguration<CustomFieldValue>
{
    public void Configure(EntityTypeBuilder<CustomFieldValue> builder)
    {
        builder.ToTable("CustomFieldValues", schema: "Foundation");

        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.StringValue).HasMaxLength(4000);
        builder.Property(x => x.NumberValue).HasPrecision(18, 4);
        builder.Property(x => x.JsonValue).HasMaxLength(4000);
        builder.Property(x => x.MetadataJson).HasMaxLength(4000).IsRequired();

        builder.HasOne(x => x.Definition)
            .WithMany(x => x.Values)
            .HasForeignKey(x => x.DefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DefinitionId, x.EntityType, x.EntityId })
            .IsUnique()
            .HasDatabaseName("UX_CustomFieldValues_Definition_Entity")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.EntityType, x.EntityId })
            .HasDatabaseName("IX_CustomFieldValues_EntityType_EntityId");
    }
}
