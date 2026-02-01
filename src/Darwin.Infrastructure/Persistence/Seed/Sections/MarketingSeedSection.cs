using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Marketing;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds marketing data for feed and engagement:
    /// - Campaigns (10+)
    /// - CampaignDeliveries (10+)
    /// </summary>
    public sealed class MarketingSeedSection
    {
        private readonly ILogger<MarketingSeedSection> _logger;

        public MarketingSeedSection(ILogger<MarketingSeedSection> logger)
        {
            _logger = logger;
        }

        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            _logger.LogInformation("Seeding Marketing (campaigns/deliveries) ...");

            if (!await db.Campaigns.AnyAsync(ct))
                await SeedCampaignsAsync(db, ct);

            if (!await db.CampaignDeliveries.AnyAsync(ct))
                await SeedDeliveriesAsync(db, ct);

            _logger.LogInformation("Marketing seeding done.");
        }

        private static async Task SeedCampaignsAsync(DarwinDbContext db, CancellationToken ct)
        {
            var businesses = await db.Set<Business>().OrderBy(b => b.Name).ToListAsync(ct);

            var campaigns = new List<Campaign>
            {
                new() { BusinessId = null, Name = "Berlin Frühling", Title = "Frühlingsangebote in Berlin", Subtitle = "Nur diese Woche", Body = "Entdecke lokale Angebote in Berlin.", MediaUrl = "https://cdn.darwin.dev/campaigns/berlin.jpg", LandingUrl = "https://darwin.de/berlin", Channels = CampaignChannels.InApp | CampaignChannels.Push, StartsAtUtc = DateTime.UtcNow.AddDays(-2), EndsAtUtc = DateTime.UtcNow.AddDays(10), IsActive = true, TargetingJson = "{\"city\":\"Berlin\"}", PayloadJson = "{\"deeplink\":\"darwin://discover?city=Berlin\"}" },
                new() { BusinessId = businesses.ElementAtOrDefault(1)?.Id, Name = "München Kaffee", Title = "Kostenloser Kaffee", Subtitle = "Nur bei Bäckerei König", Body = "Beim nächsten Besuch gibt es einen kostenlosen Kaffee.", MediaUrl = "https://cdn.darwin.dev/campaigns/coffee.jpg", LandingUrl = "https://baeckerei-koenig.de", Channels = CampaignChannels.InApp, StartsAtUtc = DateTime.UtcNow.AddDays(-1), EndsAtUtc = DateTime.UtcNow.AddDays(14), IsActive = true, TargetingJson = "{\"favoritesOnly\":true}", PayloadJson = "{\"deeplink\":\"darwin://business/koenig\"}" },
                new() { BusinessId = businesses.ElementAtOrDefault(2)?.Id, Name = "Köln Abendessen", Title = "Abendmenü -20%", Subtitle = "RheinEssen", Body = "20% Rabatt auf das Abendmenü.", MediaUrl = "https://cdn.darwin.dev/campaigns/koeln.jpg", LandingUrl = "https://rheinessen.de", Channels = CampaignChannels.InApp | CampaignChannels.Email, StartsAtUtc = DateTime.UtcNow, EndsAtUtc = DateTime.UtcNow.AddDays(20), IsActive = true, TargetingJson = "{\"minVisits\":2}", PayloadJson = "{\"deeplink\":\"darwin://business/rheinessen\"}" },
                new() { BusinessId = businesses.ElementAtOrDefault(3)?.Id, Name = "Hamburg Fitness", Title = "Probetraining kostenlos", Subtitle = "NordFit Club", Body = "Jetzt kostenloses Probetraining sichern.", MediaUrl = "https://cdn.darwin.dev/campaigns/fitness.jpg", LandingUrl = "https://nordfit.de", Channels = CampaignChannels.InApp | CampaignChannels.Push, StartsAtUtc = DateTime.UtcNow.AddDays(-3), EndsAtUtc = DateTime.UtcNow.AddDays(30), IsActive = true, TargetingJson = "{\"city\":\"Hamburg\"}", PayloadJson = "{\"deeplink\":\"darwin://business/nordfit\"}" },
                new() { BusinessId = businesses.ElementAtOrDefault(4)?.Id, Name = "Frankfurt Supermarkt", Title = "Frische Woche", Subtitle = "MainMarkt", Body = "Frische Produkte mit 10% Rabatt.", MediaUrl = "https://cdn.darwin.dev/campaigns/groceries.jpg", LandingUrl = "https://mainmarkt.de", Channels = CampaignChannels.InApp, StartsAtUtc = DateTime.UtcNow.AddDays(-5), EndsAtUtc = DateTime.UtcNow.AddDays(7), IsActive = true, TargetingJson = "{\"city\":\"Frankfurt\"}", PayloadJson = "{\"deeplink\":\"darwin://business/mainmarkt\"}" },
                new() { BusinessId = businesses.ElementAtOrDefault(5)?.Id, Name = "Stuttgart Wellness", Title = "Wellness-Tag", Subtitle = "SchönZeit Spa", Body = "Massage + Sauna Paket im Angebot.", MediaUrl = "https://cdn.darwin.dev/campaigns/spa.jpg", LandingUrl = "https://schoenzeit.de", Channels = CampaignChannels.InApp | CampaignChannels.Email, StartsAtUtc = DateTime.UtcNow.AddDays(-1), EndsAtUtc = DateTime.UtcNow.AddDays(12), IsActive = true, TargetingJson = "{\"city\":\"Stuttgart\"}", PayloadJson = "{\"deeplink\":\"darwin://business/schoenzeit\"}" },
                new() { BusinessId = businesses.ElementAtOrDefault(6)?.Id, Name = "Düsseldorf Feinkost", Title = "Feinkost der Woche", Subtitle = "DorfLaden", Body = "Exklusive regionale Produkte.", MediaUrl = "https://cdn.darwin.dev/campaigns/delikat.jpg", LandingUrl = "https://dorfladen.de", Channels = CampaignChannels.InApp, StartsAtUtc = DateTime.UtcNow, EndsAtUtc = DateTime.UtcNow.AddDays(15), IsActive = true, TargetingJson = "{\"city\":\"Düsseldorf\"}", PayloadJson = "{\"deeplink\":\"darwin://business/dorfladen\"}" },
                new() { BusinessId = businesses.ElementAtOrDefault(7)?.Id, Name = "Leipzig Service", Title = "Express-Abholung", Subtitle = "Service Hub", Body = "Reparaturen jetzt schneller verfügbar.", MediaUrl = "https://cdn.darwin.dev/campaigns/service.jpg", LandingUrl = "https://lshub.de", Channels = CampaignChannels.InApp, StartsAtUtc = DateTime.UtcNow, EndsAtUtc = DateTime.UtcNow.AddDays(25), IsActive = true, TargetingJson = "{\"city\":\"Leipzig\"}", PayloadJson = "{\"deeplink\":\"darwin://business/lshub\"}" },
                new() { BusinessId = businesses.ElementAtOrDefault(8)?.Id, Name = "Dresden Bistro", Title = "Mittagsmenü 9,90€", Subtitle = "ElbeBistro", Body = "Tägliches Mittagsangebot.", MediaUrl = "https://cdn.darwin.dev/campaigns/bistro.jpg", LandingUrl = "https://elbebistro.de", Channels = CampaignChannels.InApp | CampaignChannels.Push, StartsAtUtc = DateTime.UtcNow.AddDays(-2), EndsAtUtc = DateTime.UtcNow.AddDays(9), IsActive = true, TargetingJson = "{\"city\":\"Dresden\"}", PayloadJson = "{\"deeplink\":\"darwin://business/elbebistro\"}" },
                new() { BusinessId = businesses.ElementAtOrDefault(9)?.Id, Name = "Nürnberg Kaffeehaus", Title = "Hausmischung -15%", Subtitle = "FrankenKaffee", Body = "15% auf unsere Hausmischung.", MediaUrl = "https://cdn.darwin.dev/campaigns/nuernberg.jpg", LandingUrl = "https://frankenkaffee.de", Channels = CampaignChannels.InApp, StartsAtUtc = DateTime.UtcNow, EndsAtUtc = DateTime.UtcNow.AddDays(20), IsActive = true, TargetingJson = "{\"city\":\"Nürnberg\"}", PayloadJson = "{\"deeplink\":\"darwin://business/frankenkaffee\"}" }
            };

            db.AddRange(campaigns);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedDeliveriesAsync(DarwinDbContext db, CancellationToken ct)
        {
            var campaigns = await db.Campaigns.OrderBy(c => c.Name).ToListAsync(ct);
            var users = await db.Set<User>().OrderBy(u => u.Email).ToListAsync(ct);

            var deliveries = new List<CampaignDelivery>();

            for (var i = 0; i < campaigns.Count && i < 10; i++)
            {
                var user = users[i % users.Count];

                deliveries.Add(new CampaignDelivery
                {
                    CampaignId = campaigns[i].Id,
                    RecipientUserId = user.Id,
                    BusinessId = campaigns[i].BusinessId,
                    Channel = CampaignDeliveryChannel.InApp,
                    Status = i % 2 == 0 ? CampaignDeliveryStatus.Succeeded : CampaignDeliveryStatus.Pending,
                    Destination = user.Email,
                    AttemptCount = i % 2 == 0 ? 1 : 0,
                    FirstAttemptAtUtc = DateTime.UtcNow.AddMinutes(-10),
                    LastAttemptAtUtc = DateTime.UtcNow.AddMinutes(-5),
                    IdempotencyKey = $"camp-{campaigns[i].Id:N}-{user.Id:N}",
                    PayloadHash = $"hash-{i:D2}"
                });
            }

            db.AddRange(deliveries);
            await db.SaveChangesAsync(ct);
        }
    }
}