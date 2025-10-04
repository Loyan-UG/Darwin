using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Integration;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds demo webhook subscription and a pending delivery.
    /// </summary>
    public sealed class IntegrationSeedSection
    {
        /// <summary>
        /// Adds a single active subscription and a corresponding pending delivery.
        /// </summary>
        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            if (!await db.Set<WebhookSubscription>().AnyAsync(ct))
            {
                var sub = new WebhookSubscription
                {
                    EventType = "order.created",
                    CallbackUrl = "https://webhook.site/your-demo-endpoint",
                    Secret = "demo-secret",
                    IsActive = true
                };
                db.Add(sub);

                db.Add(new WebhookDelivery
                {
                    SubscriptionId = sub.Id,
                    EventRefId = Guid.NewGuid(),
                    Status = "Pending",
                    RetryCount = 0,
                    PayloadHash = "sha256:payload-demo",
                    IdempotencyKey = Guid.NewGuid().ToString("N")
                });

                await db.SaveChangesAsync(ct);
            }
        }
    }
}
