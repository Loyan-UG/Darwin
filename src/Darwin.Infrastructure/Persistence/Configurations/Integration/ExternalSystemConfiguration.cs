using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Integration;

public sealed class ExternalSystemConfiguration : IEntityTypeConfiguration<ExternalSystem>
{
    public void Configure(EntityTypeBuilder<ExternalSystem> builder)
    {
        builder.ToTable("ExternalSystems", schema: "Integration");

        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.BaseUrl).HasMaxLength(1024);
        builder.Property(x => x.Description).HasMaxLength(1024);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.MetadataJson).HasMaxLength(4000).IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("UX_ExternalSystems_Code")
            .HasFilter("[IsDeleted] = 0");
    }
}
