using Darwin.Domain.Entities.Foundation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Foundation;

public sealed class FeatureAreaConfiguration : IEntityTypeConfiguration<FeatureArea>
{
    public void Configure(EntityTypeBuilder<FeatureArea> builder)
    {
        builder.ToTable("FeatureAreas", schema: "Foundation");

        builder.Property(x => x.Code).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1024);
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.VisibilityScope).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.DefaultEnabled).HasDefaultValue(true);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.RequiredPermissionKey).HasMaxLength(128);
        builder.Property(x => x.MetadataJson).HasMaxLength(4000).IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("UX_FeatureAreas_Code")
            .HasFilter("[Code] IS NOT NULL AND [IsActive] = 1 AND [IsDeleted] = 0");

        builder.HasIndex(x => new { x.Category, x.SortOrder })
            .HasDatabaseName("IX_FeatureAreas_Category_SortOrder");
    }
}

public sealed class BusinessFeatureOverrideConfiguration : IEntityTypeConfiguration<BusinessFeatureOverride>
{
    public void Configure(EntityTypeBuilder<BusinessFeatureOverride> builder)
    {
        builder.ToTable("BusinessFeatureOverrides", schema: "Foundation");

        builder.Property(x => x.Reason).HasMaxLength(1024);
        builder.Property(x => x.MetadataJson).HasMaxLength(4000).IsRequired();

        builder.HasIndex(x => new { x.BusinessId, x.FeatureAreaId })
            .IsUnique()
            .HasDatabaseName("UX_BusinessFeatureOverrides_Business_Feature")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.BusinessId, x.IsEnabled })
            .HasDatabaseName("IX_BusinessFeatureOverrides_Business_IsEnabled");
    }
}
