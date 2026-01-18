using Darwin.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Billing
{
    /// <summary>
    /// EF Core mapping for <see cref="BusinessSubscription"/>.
    /// </summary>
    public sealed class BusinessSubscriptionConfiguration : IEntityTypeConfiguration<BusinessSubscription>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<BusinessSubscription> builder)
        {
            builder.ToTable("BusinessSubscriptions", schema: "Billing");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.BillingPlanId)
                .IsRequired();

            builder.Property(x => x.Provider)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(x => x.ProviderCustomerId)
                .HasMaxLength(128);

            builder.Property(x => x.ProviderSubscriptionId)
                .HasMaxLength(128);

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.StartedAtUtc)
                .IsRequired();

            builder.Property(x => x.CurrentPeriodStartUtc);
            builder.Property(x => x.CurrentPeriodEndUtc);

            builder.Property(x => x.CancelAtPeriodEnd)
                .IsRequired();

            builder.Property(x => x.CanceledAtUtc);
            builder.Property(x => x.TrialEndsAtUtc);

            builder.Property(x => x.UnitPriceMinor)
                .IsRequired();

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(x => x.MetadataJson)
                .HasMaxLength(4000);

            // Indexes:
            builder.HasIndex(x => x.BusinessId)
                .IsUnique();

            builder.HasIndex(x => x.BillingPlanId);

            // Provider subscription id should be unique when present.
            builder.HasIndex(x => new { x.Provider, x.ProviderSubscriptionId })
                .IsUnique();
        }
    }
}
