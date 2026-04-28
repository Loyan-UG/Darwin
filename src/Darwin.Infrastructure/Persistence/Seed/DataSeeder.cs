using Darwin.Infrastructure.Persistence.Db;
using Darwin.Infrastructure.Persistence.Seed.Sections;
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
        private readonly CrmSeedSection _crm;
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
            CrmSeedSection crm,
            LoyaltySeedSection loyalty,
            BillingSeedSection billing,
            MarketingSeedSection marketing,
            ShippingSeedSection shipping,
            OrdersSeedSection orders)
        {
            _db = db ?? throw new System.ArgumentNullException(nameof(db));
            _identity = identity ?? throw new System.ArgumentNullException(nameof(identity));
            _pricing = pricing ?? throw new System.ArgumentNullException(nameof(pricing));
            _cms = cms ?? throw new System.ArgumentNullException(nameof(cms));
            _inventory = inventory ?? throw new System.ArgumentNullException(nameof(inventory));
            _integration = integration ?? throw new System.ArgumentNullException(nameof(integration));
            _seo = seo ?? throw new System.ArgumentNullException(nameof(seo));
            _cart = cart ?? throw new System.ArgumentNullException(nameof(cart));
            _siteSettings = siteSettings ?? throw new System.ArgumentNullException(nameof(siteSettings));
            _catalog = catalog ?? throw new System.ArgumentNullException(nameof(catalog));
            _businesses = businesses ?? throw new System.ArgumentNullException(nameof(businesses));
            _crm = crm ?? throw new System.ArgumentNullException(nameof(crm));
            _loyalty = loyalty ?? throw new System.ArgumentNullException(nameof(loyalty));
            _billing = billing ?? throw new System.ArgumentNullException(nameof(billing));
            _marketing = marketing ?? throw new System.ArgumentNullException(nameof(marketing));
            _shipping = shipping ?? throw new System.ArgumentNullException(nameof(shipping));
            _orders = orders ?? throw new System.ArgumentNullException(nameof(orders));
        }

        public async Task SeedAsync(CancellationToken ct = default)
        {
            await _identity.SeedAsync(_db, ct);
            await _siteSettings.SeedAsync(_db, ct);
            await _pricing.SeedAsync(_db, ct);
            await _cms.SeedAsync(_db, ct);
            await _catalog.SeedAsync(_db, ct);

            await _businesses.SeedAsync(_db, ct);
            await _inventory.SeedAsync(_db, ct);
            await _crm.SeedAsync(_db, ct);
            await _loyalty.SeedAsync(_db, ct);

            await _billing.SeedAsync(_db, ct);
            await _marketing.SeedAsync(_db, ct);
            await _shipping.SeedAsync(_db, ct);
            await _orders.SeedAsync(_db, ct);
            await _integration.SeedAsync(_db, ct);
            await _seo.SeedAsync(_db, ct);
            await _cart.SeedAsync(_db, ct);
        }
    }
}
