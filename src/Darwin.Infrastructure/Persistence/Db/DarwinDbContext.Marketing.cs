using Darwin.Domain.Entities.Marketing;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Persistence.Db
{
    public sealed partial class DarwinDbContext
    {
        /// <summary>
        /// Marketing campaigns used for engagement automation and in-app feed.
        /// </summary>
        public DbSet<Campaign> Campaigns => Set<Campaign>();

        /// <summary>
        /// Delivery attempts / planned deliveries for campaigns.
        /// </summary>
        public DbSet<CampaignDelivery> CampaignDeliveries => Set<CampaignDelivery>();
    }
}
