using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Integration
{
    /// <summary>
    /// EF Core mapping for <see cref="AnalyticsExportFile"/>.
    /// </summary>
    public sealed class AnalyticsExportFileConfiguration : IEntityTypeConfiguration<AnalyticsExportFile>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<AnalyticsExportFile> builder)
        {
            builder.ToTable("AnalyticsExportFiles", schema: "Integration");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.AnalyticsExportJobId)
                .IsRequired();

            builder.Property(x => x.StorageKey)
                .IsRequired()
                .HasMaxLength(1024);

            builder.Property(x => x.FileName)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.ContentType)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(x => x.SizeBytes);

            builder.Property(x => x.ContentHash)
                .HasMaxLength(128);

            builder.Property(x => x.ExpiresAtUtc);

            // Relationship: File -> Job
            builder.HasOne<AnalyticsExportJob>()
                .WithMany()
                .HasForeignKey(x => x.AnalyticsExportJobId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes:
            builder.HasIndex(x => x.AnalyticsExportJobId)
                .HasDatabaseName("IX_AnalyticsExportFiles_JobId");

            builder.HasIndex(x => x.ExpiresAtUtc)
                .HasDatabaseName("IX_AnalyticsExportFiles_ExpiresAtUtc");

            // Prevent duplicates within a job.
            builder.HasIndex(x => new { x.AnalyticsExportJobId, x.StorageKey })
                .IsUnique()
                .HasDatabaseName("UX_AnalyticsExportFiles_Job_StorageKey");
        }
    }
}
