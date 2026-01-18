using Darwin.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Billing
{
    /// <summary>
    /// EF Core mapping for <see cref="SubscriptionInvoice"/>.
    /// The domain model represents invoices synchronized from a billing provider
    /// (e.g., Stripe) for a <see cref="BusinessSubscription"/>.
    /// </summary>
    public sealed class SubscriptionInvoiceConfiguration : IEntityTypeConfiguration<SubscriptionInvoice>
    {
        /// <summary>
        /// Configures the database mapping for <see cref="SubscriptionInvoice"/>.
        /// </summary>
        /// <param name="builder">The EF Core entity builder.</param>
        public void Configure(EntityTypeBuilder<SubscriptionInvoice> builder)
        {
            builder.ToTable("SubscriptionInvoices", schema: "Billing");

            builder.HasKey(x => x.Id);

            // Required FK/scalars
            builder.Property(x => x.BusinessSubscriptionId)
                .IsRequired();

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.Provider)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(x => x.IssuedAtUtc)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.TotalMinor)
                .IsRequired();

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(3);

            // Optional provider reconciliation fields
            builder.Property(x => x.ProviderInvoiceId)
                .HasMaxLength(128);

            builder.Property(x => x.DueAtUtc);
            builder.Property(x => x.HostedInvoiceUrl).HasMaxLength(1024);
            builder.Property(x => x.PdfUrl).HasMaxLength(1024);
            builder.Property(x => x.PaidAtUtc);
            builder.Property(x => x.FailureReason).HasMaxLength(2000);

            // JSON snapshots
            builder.Property(x => x.LinesJson)
                .IsRequired()
                .HasMaxLength(4000);

            builder.Property(x => x.MetadataJson)
                .HasMaxLength(4000);

            // Indexes: operational lookups and provider reconciliation
            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.BusinessSubscriptionId);
            builder.HasIndex(x => x.IssuedAtUtc);

            // Provider invoice id should be unique per provider when present.
            builder.HasIndex(x => new { x.Provider, x.ProviderInvoiceId })
                .IsUnique();
        }
    }
}
