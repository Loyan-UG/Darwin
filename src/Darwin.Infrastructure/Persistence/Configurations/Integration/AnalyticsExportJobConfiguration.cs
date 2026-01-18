using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Integration
{
    /// <summary>
    /// EF Core mapping for <see cref="AnalyticsExportJob"/>.
    /// </summary>
    public sealed class AnalyticsExportJobConfiguration : IEntityTypeConfiguration<AnalyticsExportJob>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<AnalyticsExportJob> builder)
        {
            builder.ToTable("AnalyticsExportJobs", schema: "Integration");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.RequestedByUserId)
                .IsRequired();

            builder.Property(x => x.ReportType)
                .IsRequired();

            builder.Property(x => x.Format)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.ParametersJson)
                .IsRequired()
                .HasMaxLength(8000);

            builder.Property(x => x.StartedAtUtc);
            builder.Property(x => x.FinishedAtUtc);

            builder.Property(x => x.Error)
                .HasMaxLength(2000);

            builder.Property(x => x.IdempotencyKey)
                .HasMaxLength(128);

            builder.Property(x => x.RetainUntilUtc);

            // Indexes:
            builder.HasIndex(x => x.BusinessId)
                .HasDatabaseName("IX_AnalyticsExportJobs_BusinessId");

            builder.HasIndex(x => x.RequestedByUserId)
                .HasDatabaseName("IX_AnalyticsExportJobs_RequestedByUserId");

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("IX_AnalyticsExportJobs_Status");

            builder.HasIndex(x => x.RetainUntilUtc)
                .HasDatabaseName("IX_AnalyticsExportJobs_RetainUntilUtc");

            // Optional idempotency key; uniqueness per business prevents duplicates.
            builder.HasIndex(x => new { x.BusinessId, x.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("UX_AnalyticsExportJobs_Business_IdempotencyKey");
        }
    }
}
