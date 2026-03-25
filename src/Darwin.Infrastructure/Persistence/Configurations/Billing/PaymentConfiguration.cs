using Darwin.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Billing
{
    /// <summary>
    /// Configures billing payments used by invoice, order, and accounting workflows.
    /// </summary>
    public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments", schema: "Billing");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(x => x.Provider)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(x => x.ProviderTransactionRef)
                .HasMaxLength(256);

            builder.Property(x => x.FailureReason)
                .HasMaxLength(1000);

            builder.Property(x => x.AmountMinor)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.OrderId);
            builder.HasIndex(x => x.InvoiceId);
            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.UserId);
        }
    }
}
