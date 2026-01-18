using Darwin.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Billing
{
    /// <summary>
    /// EF Core mapping for <see cref="BillingPlan"/>.
    /// </summary>
    public sealed class BillingPlanConfiguration : IEntityTypeConfiguration<BillingPlan>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<BillingPlan> builder)
        {
            builder.ToTable("BillingPlans", schema: "Billing");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.Description)
                .HasMaxLength(4000);

            builder.Property(x => x.PriceMinor)
                .IsRequired();

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(x => x.Interval)
                .IsRequired();

            builder.Property(x => x.IntervalCount)
                .IsRequired();

            builder.Property(x => x.TrialDays);

            builder.Property(x => x.IsActive)
                .IsRequired();

            builder.Property(x => x.FeaturesJson)
                .IsRequired()
                .HasMaxLength(4000);

            // Business requirement: stable unique plan code.
            builder.HasIndex(x => x.Code)
                .IsUnique();

            builder.HasIndex(x => x.IsActive);
        }
    }
}
