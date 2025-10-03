using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Integration
{
    public sealed class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
    {
        public void Configure(EntityTypeBuilder<WebhookSubscription> b)
        {
            b.ToTable("WebhookSubscriptions", schema: "Integration");
            b.Property(x => x.EventType).IsRequired().HasMaxLength(100);
            b.Property(x => x.CallbackUrl).IsRequired().HasMaxLength(400);

            b.HasIndex(x => new { x.EventType, x.CallbackUrl })
             .IsUnique()
             .HasFilter("[IsDeleted] = 0");
        }
    }

    public sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
    {
        public void Configure(EntityTypeBuilder<WebhookDelivery> b)
        {
            b.ToTable("WebhookDeliveries", schema: "Integration");
            b.Property(x => x.Status).IsRequired().HasMaxLength(50);
            b.Property(x => x.IdempotencyKey).HasMaxLength(100);
        }
    }

    public sealed class EventLogConfiguration : IEntityTypeConfiguration<EventLog>
    {
        public void Configure(EntityTypeBuilder<EventLog> b)
        {
            b.ToTable("EventLogs", schema: "Integration");
            b.Property(x => x.Type).IsRequired().HasMaxLength(100);
            b.Property(x => x.SessionId).HasMaxLength(100);
            b.Property(x => x.IdempotencyKey).HasMaxLength(100);

            // Useful index for analytics
            b.HasIndex(x => new { x.Type, x.OccurredAtUtc });
        }
    }
}
