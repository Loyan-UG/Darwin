using Darwin.Domain.Entities.Foundation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Foundation;

public sealed class NumberSequenceConfiguration : IEntityTypeConfiguration<NumberSequence>
{
    public void Configure(EntityTypeBuilder<NumberSequence> builder)
    {
        builder.ToTable("NumberSequences", schema: "Foundation");

        builder.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ScopeKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.PrefixPattern).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NextValue).IsRequired();
        builder.Property(x => x.PaddingLength).IsRequired();
        builder.Property(x => x.ResetPolicy).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CurrentPeriodKey).HasMaxLength(32);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.Description).HasMaxLength(1024);
        builder.Property(x => x.MetadataJson).HasMaxLength(4000).IsRequired();

        builder.HasIndex(x => new { x.BusinessId, x.DocumentType, x.ScopeKey })
            .IsUnique()
            .HasDatabaseName("UX_NumberSequences_Business_DocumentType_Scope")
            .HasFilter("[BusinessId] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasIndex(x => new { x.DocumentType, x.ScopeKey })
            .IsUnique()
            .HasDatabaseName("UX_NumberSequences_Global_DocumentType_Scope")
            .HasFilter("[BusinessId] IS NULL AND [IsDeleted] = 0");
    }
}
