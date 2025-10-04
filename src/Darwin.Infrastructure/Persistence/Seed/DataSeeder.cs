using Darwin.Infrastructure.Persistence.Db;
using Darwin.Infrastructure.Persistence.Seed.Sections;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Infrastructure.Persistence.Seed
{
    /// <summary>
    /// Application-wide data seeder orchestrator. It coordinates multiple seed sections
    /// so that the initial dataset is populated in a clean, idempotent manner.
    /// Keep this class intentionally slim; each domain area owns its own section.
    /// </summary>
    public sealed class DataSeeder
    {
        private readonly DarwinDbContext _db;

        // Sections
        private readonly IdentitySeedSection _identity;
        private readonly PricingSeedSection _pricing;
        private readonly CmsSeedSection _cms;
        private readonly InventorySeedSection _inventory;
        private readonly IntegrationSeedSection _integration;
        private readonly SeoSeedSection _seo;
        private readonly CartSeedSection _cart;

        /// <summary>
        /// Initializes a new <see cref="DataSeeder"/> with all seed sections injected.
        /// </summary>
        public DataSeeder(
            DarwinDbContext db,
            IdentitySeedSection identity,
            PricingSeedSection pricing,
            CmsSeedSection cms,
            InventorySeedSection inventory,
            IntegrationSeedSection integration,
            SeoSeedSection seo,
            CartSeedSection cart)
        {
            _db = db;
            _identity = identity;
            _pricing = pricing;
            _cms = cms;
            _inventory = inventory;
            _integration = integration;
            _seo = seo;
            _cart = cart;
        }

        /// <summary>
        /// Applies pending migrations (if any) and runs all seed sections in a reasonable order.
        /// </summary>
        public async Task SeedAsync(CancellationToken ct = default)
        {
            await _db.Database.MigrateAsync(ct);

            // Order matters: Identity first (so UserId exists for other sections), then pricing, etc.
            await _identity.SeedAsync(_db, ct);
            await _pricing.SeedAsync(_db, ct);
            await _cms.SeedAsync(_db, ct);
            await _inventory.SeedAsync(_db, ct);
            await _integration.SeedAsync(_db, ct);
            await _seo.SeedAsync(_db, ct);
            await _cart.SeedAsync(_db, ct);
        }
    }
}
