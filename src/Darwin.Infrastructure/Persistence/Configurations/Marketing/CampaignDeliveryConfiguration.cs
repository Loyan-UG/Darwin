using Darwin.Domain.Entities.Marketing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Marketing
{
    /// <summary>
    /// EF Core mapping for <see cref="CampaignDelivery"/>.
    /// </summary>
    public sealed class CampaignDeliveryConfiguration : IEntityTypeConfiguration<CampaignDelivery>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<CampaignDelivery> builder)
        {
            builder.ToTable("CampaignDeliveries", schema: "Marketing");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CampaignId)
                .IsRequired();

            builder.Property(x => x.RecipientUserId);

            builder.Property(x => x.BusinessId);

            builder.Property(x => x.Channel)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.Destination)
                .HasMaxLength(512);

            builder.Property(x => x.AttemptCount)
                .IsRequired();

            builder.Property(x => x.FirstAttemptAtUtc);
            builder.Property(x => x.LastAttemptAtUtc);

            builder.Property(x => x.LastResponseCode);

            builder.Property(x => x.ProviderMessageId)
                .HasMaxLength(128);

            builder.Property(x => x.LastError)
                .HasMaxLength(2000);

            builder.Property(x => x.IdempotencyKey)
                .HasMaxLength(128);

            builder.Property(x => x.PayloadHash)
                .HasMaxLength(128);

            // Relationships:
            builder.HasOne<Campaign>()
                .WithMany()
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for processing queues and reporting:
            builder.HasIndex(x => x.CampaignId)
                .HasDatabaseName("IX_CampaignDeliveries_CampaignId");

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("IX_CampaignDeliveries_Status");

            builder.HasIndex(x => x.Channel)
                .HasDatabaseName("IX_CampaignDeliveries_Channel");

            builder.HasIndex(x => x.RecipientUserId)
                .HasDatabaseName("IX_CampaignDeliveries_RecipientUserId");

            builder.HasIndex(x => x.BusinessId)
                .HasDatabaseName("IX_CampaignDeliveries_BusinessId");

            builder.HasIndex(x => x.LastAttemptAtUtc)
                .HasDatabaseName("IX_CampaignDeliveries_LastAttemptAtUtc");

            // Optional idempotency key; keep it unique to avoid duplicate deliveries.
            // Unique indexes allow multiple NULLs in SQL Server and PostgreSQL.
            builder.HasIndex(x => x.IdempotencyKey)
                .IsUnique()
                .HasDatabaseName("UX_CampaignDeliveries_IdempotencyKey");
        }
    }
}
