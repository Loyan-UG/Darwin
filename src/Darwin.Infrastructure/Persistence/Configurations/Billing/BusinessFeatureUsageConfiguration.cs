using Darwin.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Billing
{
    /// <summary>
    /// EF Core mapping for <see cref="BusinessFeatureUsage"/>.
    /// </summary>
    public sealed class BusinessFeatureUsageConfiguration : IEntityTypeConfiguration<BusinessFeatureUsage>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<BusinessFeatureUsage> builder)
        {
            builder.ToTable("BusinessFeatureUsages", schema: "Billing");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.FeatureKey)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(x => x.PeriodStartUtc)
                .IsRequired();

            builder.Property(x => x.PeriodEndUtc)
                .IsRequired();

            builder.Property(x => x.SourceId)
                .IsRequired();

            builder.Property(x => x.UsedAtUtc)
                .IsRequired();

            builder.HasIndex(x => new { x.BusinessId, x.FeatureKey, x.PeriodStartUtc });

            builder.HasIndex(x => new { x.BusinessId, x.FeatureKey, x.PeriodStartUtc, x.SourceId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        }
    }
}
