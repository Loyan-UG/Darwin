using Darwin.Domain.Entities.Foundation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Foundation;

public sealed class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("Notes", schema: "Foundation");

        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.MetadataJson).HasMaxLength(4000).IsRequired();

        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAtUtc })
            .HasDatabaseName("IX_Notes_EntityType_EntityId_CreatedAtUtc");
    }
}
