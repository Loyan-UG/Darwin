using Darwin.Infrastructure.Persistence.Db;
using Darwin.Infrastructure.Persistence.Seed.Sections;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Infrastructure.Persistence.Seed
{
    public sealed class DataSeeder
    {
        private readonly DarwinDbContext _db;

        private readonly IdentitySeedSection _identity;
        private readonly PricingSeedSection _pricing;
        private readonly CmsSeedSection _cms;
        private readonly InventorySeedSection _inventory;
        private readonly IntegrationSeedSection _integration;
        private readonly SeoSeedSection _seo;
        private readonly CartSeedSection _cart;
        private readonly SiteSettingsSeedSection _siteSettings;
        private readonly CatalogSeedSection _catalog;
        private readonly BusinessesSeedSection _businesses;
        private readonly LoyaltySeedSection _loyalty;
        private readonly BillingSeedSection _billing;
        private readonly MarketingSeedSection _marketing;
        private readonly ShippingSeedSection _shipping;
        private readonly OrdersSeedSection _orders;

        public DataSeeder(
            DarwinDbContext db,
            IdentitySeedSection identity,
            PricingSeedSection pricing,
            CmsSeedSection cms,
            InventorySeedSection inventory,
            IntegrationSeedSection integration,
            SeoSeedSection seo,
            CartSeedSection cart,
            SiteSettingsSeedSection siteSettings,
            CatalogSeedSection catalog,
            BusinessesSeedSection businesses,
            LoyaltySeedSection loyalty,
            BillingSeedSection billing,
            MarketingSeedSection marketing,
            ShippingSeedSection shipping,
            OrdersSeedSection orders)
        {
            _db = db;
            _identity = identity;
            _pricing = pricing;
            _cms = cms;
            _inventory = inventory;
            _integration = integration;
            _seo = seo;
            _cart = cart;
            _siteSettings = siteSettings;
            _catalog = catalog;
            _businesses = businesses;
            _loyalty = loyalty;
            _billing = billing;
            _marketing = marketing;
            _shipping = shipping;
            _orders = orders;
        }

        public async Task SeedAsync(CancellationToken ct = default)
        {
            await _db.Database.MigrateAsync(ct);

            await _identity.SeedAsync(_db, ct);
            await _siteSettings.SeedAsync(_db, ct);
            await _pricing.SeedAsync(_db, ct);
            await _cms.SeedAsync(_db, ct);
            await _catalog.SeedAsync(_db, ct);

            await _businesses.SeedAsync(_db, ct);
            await _loyalty.SeedAsync(_db, ct);

            await _billing.SeedAsync(_db, ct);
            await _marketing.SeedAsync(_db, ct);
            await _shipping.SeedAsync(_db, ct);
            await _orders.SeedAsync(_db, ct);

            await _inventory.SeedAsync(_db, ct);
            await _integration.SeedAsync(_db, ct);
            await _seo.SeedAsync(_db, ct);
            await _cart.SeedAsync(_db, ct);
        }
    }
}