using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Identity.Services;
using Darwin.Application.Settings.DTOs;
using Darwin.Application.Settings.Queries;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.CMS;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Pricing;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.WebAdmin.Services.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Darwin.WebAdmin.Tests.TestInfrastructure;

public sealed class WebAdminTestFactory : WebApplicationFactory<Program>
{
    public const string TestAuthHeader = "X-Test-Auth";
    public static readonly Guid TestBrandId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid TestCategoryId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    public static readonly Guid TestTaxCategoryId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    public static readonly Guid TestProductId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid TestProductVariantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid TestMemberUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public static readonly Guid TestLoyaltyProgramBusinessId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid TestRoleId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    public static readonly Guid TestPermissionId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    public static readonly Guid TestLifecycleUserId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    public static readonly Guid TestOrderId = Guid.Parse("12121212-1212-1212-1212-121212121212");
    public static readonly Guid TestOrderLineId = Guid.Parse("13131313-1313-1313-1313-131313131313");
    public static readonly Guid TestOrderPaymentId = Guid.Parse("14141414-1414-1414-1414-141414141414");
    public static readonly Guid TestClearPushDeviceId = Guid.Parse("15151515-1515-1515-1515-151515151515");
    public static readonly Guid TestDeactivateDeviceId = Guid.Parse("16161616-1616-1616-1616-161616161616");
    public static readonly Guid TestDhlLabelShipmentId = Guid.Parse("17171717-1717-1717-1717-171717171717");
    public static readonly Guid TestReturnedShipmentId = Guid.Parse("18181818-1818-1818-1818-181818181818");
    public static readonly Guid TestReturnedShipmentEventId = Guid.Parse("19191919-1919-1919-1919-191919191919");
    public static readonly Guid TestMediaAssetId = Guid.Parse("20202020-2020-2020-2020-202020202020");
    public static readonly Guid TestBillingPlanId = Guid.Parse("21212121-2121-2121-2121-212121212121");
    public static readonly Guid TestBrandLifecycleId = Guid.Parse("22222222-2222-2222-2222-222222222223");
    public static readonly Guid TestBusinessLocationLifecycleId = Guid.Parse("23232323-2323-2323-2323-232323232323");
    public static readonly Guid TestBusinessInvitationLifecycleId = Guid.Parse("24242424-2424-2424-2424-242424242424");
    private const string TestAuthenticationScheme = "Test";

    public HttpClient CreateNoRedirectClient()
    {
        return CreateNoRedirectClient(new Uri("https://localhost"));
    }

    public HttpClient CreateNoRedirectClient(Uri baseAddress)
    {
        return WithWebHostBuilder(builder =>
        {
            ConfigureSmokeHost(builder);
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = baseAddress
        });
    }

    public HttpClient CreateAuthenticatedNoRedirectClient(bool allowPermissions = true)
    {
        var client = WithWebHostBuilder(builder =>
        {
            ConfigureSmokeHost(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPermissionService>();
                if (allowPermissions)
                {
                    services.AddSingleton<IPermissionService, AllowAllPermissionService>();
                }
                else
                {
                    services.AddSingleton<IPermissionService, DenyAllPermissionService>();
                }

                services
                    .AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthenticationScheme;
                        options.DefaultChallengeScheme = TestAuthenticationScheme;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        TestAuthenticationScheme,
                        _ => { });
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        client.DefaultRequestHeaders.Add(TestAuthHeader, "true");
        return client;
    }

    public HttpClient CreateAuthenticatedDatabaseNoRedirectClient(bool allowPermissions = true)
    {
        var databaseName = $"darwin_webadmin_smoke_{Guid.NewGuid():N}";
        var factory = WithWebHostBuilder(builder =>
        {
            ConfigureSmokeHost(builder);
            ConfigureTestAuthentication(builder, allowPermissions);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<DarwinDbContext>>();
                services.RemoveAll<DarwinDbContext>();
                services.RemoveAll<IAppDbContext>();

                var inMemoryProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                services.AddDbContext<DarwinDbContext>(options =>
                    options
                        .UseInMemoryDatabase(databaseName)
                        .UseInternalServiceProvider(inMemoryProvider));
                services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<DarwinDbContext>());
                services.RemoveAll<ISiteSettingCache>();
                services.AddScoped<ISiteSettingCache, DatabaseSiteSettingCache>();
            });
        });

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            var businessId = Guid.Parse("44444444-4444-4444-4444-444444444444");
            db.Set<SiteSetting>().Add(CreateDefaultSiteSetting());
            db.Set<User>().Add(new User(
                "webadmin-member@example.test",
                "not-used-in-smoke-tests",
                "webadmin-member-smoke-security-stamp")
            {
                Id = TestMemberUserId,
                EmailConfirmed = true,
                FirstName = "WebAdmin",
                LastName = "Member",
                IsActive = true,
                Locale = SiteSettingDto.DefaultCultureDefault,
                Currency = SiteSettingDto.DefaultCurrencyDefault,
                Timezone = SiteSettingDto.TimeZoneDefault,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                RowVersion = [1]
            });
            db.Set<User>().Add(new User(
                "webadmin-lifecycle@example.test",
                "not-used-in-smoke-tests",
                "webadmin-lifecycle-smoke-security-stamp")
            {
                Id = TestLifecycleUserId,
                EmailConfirmed = true,
                FirstName = "WebAdmin",
                LastName = "Lifecycle",
                IsActive = true,
                Locale = SiteSettingDto.DefaultCultureDefault,
                Currency = SiteSettingDto.DefaultCurrencyDefault,
                Timezone = SiteSettingDto.TimeZoneDefault,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                RowVersion = [1]
            });
            db.Set<Role>().Add(new Role(
                "webadmin-smoke-role",
                "WebAdmin Smoke Role",
                false,
                "Seeded role for WebAdmin role-permission smoke tests.")
            {
                Id = TestRoleId,
                RowVersion = [1]
            });
            db.Set<Permission>().Add(new Permission(
                "webadmin.smoke.permission",
                "WebAdmin Smoke Permission",
                false,
                "Seeded permission for WebAdmin role-permission smoke tests.")
            {
                Id = TestPermissionId,
                RowVersion = [1]
            });
            db.Set<MediaAsset>().Add(new MediaAsset
            {
                Id = TestMediaAssetId,
                Url = "/uploads/webadmin-smoke-seeded.png",
                Alt = "Seeded WebAdmin media alt",
                Title = "Seeded WebAdmin Media",
                OriginalFileName = "webadmin-smoke-seeded.png",
                SizeBytes = 68,
                ContentHash = "WEBADMINSMOKESEEDED",
                Width = 1,
                Height = 1,
                Role = "LibraryAsset",
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                RowVersion = [1]
            });
            db.Set<BillingPlan>().Add(new BillingPlan
            {
                Id = TestBillingPlanId,
                Code = "WEBADMIN-SMOKE-SEEDED-PLAN",
                Name = "Seeded WebAdmin Billing Plan",
                Description = "Seeded plan for WebAdmin billing edit smoke tests.",
                PriceMinor = 1990,
                Currency = SiteSettingDto.DefaultCurrencyDefault,
                Interval = BillingInterval.Month,
                IntervalCount = 1,
                TrialDays = 7,
                IsActive = true,
                FeaturesJson = "{\"seeded\":true}",
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                RowVersion = [1]
            });
            db.Set<Brand>().Add(new Brand
            {
                Id = TestBrandLifecycleId,
                Slug = "webadmin-smoke-brand-lifecycle",
                IsPublished = true,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                RowVersion = [1],
                Translations =
                {
                    new BrandTranslation
                    {
                        Culture = SiteSettingDto.DefaultCultureDefault,
                        Name = "Seeded WebAdmin Brand Lifecycle",
                        DescriptionHtml = "<p>Seeded brand lifecycle.</p>"
                    }
                }
            });
            db.Set<UserDevice>().Add(new UserDevice
            {
                Id = TestClearPushDeviceId,
                UserId = TestMemberUserId,
                DeviceId = "webadmin-smoke-clear-push",
                Platform = MobilePlatform.Android,
                PushToken = "push-clear-smoke",
                PushTokenUpdatedAtUtc = DateTime.UtcNow,
                NotificationsEnabled = true,
                LastSeenAtUtc = DateTime.UtcNow,
                AppVersion = "1.2.3",
                DeviceModel = "Smoke Android",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                RowVersion = [1]
            });
            db.Set<UserDevice>().Add(new UserDevice
            {
                Id = TestDeactivateDeviceId,
                UserId = TestLifecycleUserId,
                DeviceId = "webadmin-smoke-deactivate",
                Platform = MobilePlatform.iOS,
                PushToken = "push-deactivate-smoke",
                PushTokenUpdatedAtUtc = DateTime.UtcNow,
                NotificationsEnabled = true,
                LastSeenAtUtc = DateTime.UtcNow,
                AppVersion = "2.3.4",
                DeviceModel = "Smoke iPhone",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                RowVersion = [1]
            });
            db.Businesses.Add(new Business
            {
                Id = businessId,
                Name = "WebAdmin Smoke Business",
                LegalName = "WebAdmin Smoke Business GmbH",
                ContactEmail = "business-smoke@example.test",
                DefaultCurrency = SiteSettingDto.DefaultCurrencyDefault,
                DefaultCulture = SiteSettingDto.DefaultCultureDefault,
                DefaultTimeZoneId = SiteSettingDto.TimeZoneDefault,
                OperationalStatus = BusinessOperationalStatus.Approved,
                ApprovedAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222")
            });
            db.Set<BusinessLocation>().Add(new BusinessLocation
            {
                Id = TestBusinessLocationLifecycleId,
                BusinessId = businessId,
                Name = "Seeded WebAdmin Business Location Lifecycle",
                AddressLine1 = "Seed Street 1",
                AddressLine2 = "Floor 2",
                City = "Berlin",
                Region = "Berlin",
                CountryCode = "DE",
                PostalCode = "10115",
                Coordinate = new Darwin.Domain.Common.GeoCoordinate(52.5200, 13.4050, 35),
                IsPrimary = false,
                OpeningHoursJson = "{\"tue\":\"10:00-16:00\"}",
                InternalNote = "Seeded business location lifecycle.",
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                RowVersion = [1]
            });
            db.Set<BusinessInvitation>().Add(new BusinessInvitation
            {
                Id = TestBusinessInvitationLifecycleId,
                BusinessId = businessId,
                InvitedByUserId = TestLifecycleUserId,
                Email = "webadmin-invitation-lifecycle@example.test",
                NormalizedEmail = "WEBADMIN-INVITATION-LIFECYCLE@EXAMPLE.TEST",
                Role = BusinessMemberRole.Manager,
                Token = "seeded-invitation-lifecycle-token",
                ExpiresAtUtc = DateTime.UtcNow.AddDays(3),
                Status = BusinessInvitationStatus.Pending,
                Note = "Seeded invitation lifecycle.",
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                RowVersion = [1]
            });
            db.Businesses.Add(new Business
            {
                Id = TestLoyaltyProgramBusinessId,
                Name = "WebAdmin Smoke Loyalty Program Business",
                LegalName = "WebAdmin Smoke Loyalty Program Business GmbH",
                ContactEmail = "loyalty-program-smoke@example.test",
                DefaultCurrency = SiteSettingDto.DefaultCurrencyDefault,
                DefaultCulture = SiteSettingDto.DefaultCultureDefault,
                DefaultTimeZoneId = SiteSettingDto.TimeZoneDefault,
                OperationalStatus = BusinessOperationalStatus.Approved,
                ApprovedAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222")
            });
            db.Set<LoyaltyProgram>().Add(new LoyaltyProgram
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                BusinessId = businessId,
                Name = "WebAdmin Smoke Loyalty",
                AccrualMode = LoyaltyAccrualMode.PerVisit,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222")
            });
            db.Set<Brand>().Add(new Brand
            {
                Id = TestBrandId,
                Slug = "webadmin-smoke-brand",
                IsPublished = true,
                Translations =
                {
                    new BrandTranslation
                    {
                        Culture = SiteSettingDto.DefaultCultureDefault,
                        Name = "WebAdmin Smoke Brand"
                    }
                }
            });
            db.Set<Category>().Add(new Category
            {
                Id = TestCategoryId,
                IsActive = true,
                IsPublished = true,
                SortOrder = 1,
                Translations =
                {
                    new CategoryTranslation
                    {
                        Culture = SiteSettingDto.DefaultCultureDefault,
                        Name = "WebAdmin Smoke Category",
                        Slug = "webadmin-smoke-category"
                    }
                }
            });
            db.Set<TaxCategory>().Add(new TaxCategory
            {
                Id = TestTaxCategoryId,
                Name = "WebAdmin Smoke Standard VAT",
                VatRate = 0.19m,
                EffectiveFromUtc = DateTime.UtcNow.Date
            });
            db.Set<Product>().Add(new Product
            {
                Id = TestProductId,
                BrandId = TestBrandId,
                PrimaryCategoryId = TestCategoryId,
                Kind = ProductKind.Simple,
                IsActive = true,
                IsVisible = true,
                Translations =
                {
                    new ProductTranslation
                    {
                        Culture = SiteSettingDto.DefaultCultureDefault,
                        Name = "WebAdmin Smoke Inventory Product",
                        Slug = "webadmin-smoke-inventory-product"
                    }
                },
                Variants =
                {
                    new ProductVariant
                    {
                        Id = TestProductVariantId,
                        Sku = "WEBADMIN-SMOKE-VARIANT",
                        Currency = SiteSettingDto.DefaultCurrencyDefault,
                        TaxCategoryId = TestTaxCategoryId,
                        BasePriceNetMinor = 1299,
                        StockOnHand = 0,
                        StockReserved = 0,
                        ReorderPoint = 1,
                        IsDigital = false
                    }
                }
            });
            db.Set<Order>().Add(new Order
            {
                Id = TestOrderId,
                OrderNumber = "WEBADMIN-SMOKE-ORDER",
                UserId = TestMemberUserId,
                Currency = SiteSettingDto.DefaultCurrencyDefault,
                PricesIncludeTax = false,
                SubtotalNetMinor = 2184,
                TaxTotalMinor = 415,
                ShippingTotalMinor = 0,
                DiscountTotalMinor = 0,
                GrandTotalGrossMinor = 2599,
                ShippingMethodName = "WebAdmin Smoke Shipping",
                ShippingCarrier = "SmokeCarrier",
                ShippingService = "SmokeService",
                Status = OrderStatus.Created,
                BillingAddressJson = "{\"city\":\"Berlin\",\"country\":\"DE\"}",
                ShippingAddressJson = "{\"city\":\"Berlin\",\"country\":\"DE\"}",
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                RowVersion = [1],
                Lines =
                {
                    new OrderLine
                    {
                        Id = TestOrderLineId,
                        VariantId = TestProductVariantId,
                        Name = "WebAdmin Smoke Inventory Product",
                        Sku = "WEBADMIN-SMOKE-VARIANT",
                        Quantity = 1,
                        UnitPriceNetMinor = 2184,
                        VatRate = 0.19m,
                        UnitPriceGrossMinor = 2599,
                        LineTaxMinor = 415,
                        LineGrossMinor = 2599,
                        CreatedAtUtc = DateTime.UtcNow,
                        CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        RowVersion = [1]
                    }
                }
            });
            db.Set<Payment>().Add(new Payment
            {
                Id = TestOrderPaymentId,
                OrderId = TestOrderId,
                UserId = TestMemberUserId,
                AmountMinor = 2599,
                Currency = SiteSettingDto.DefaultCurrencyDefault,
                Status = PaymentStatus.Captured,
                Provider = "WebAdminSeedPay",
                ProviderTransactionRef = "seed-order-payment",
                PaidAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                RowVersion = [1]
            });
            db.Set<Shipment>().Add(new Shipment
            {
                Id = TestDhlLabelShipmentId,
                OrderId = TestOrderId,
                Carrier = "DHL",
                Service = "DHL-SMOKE-LABEL",
                ProviderShipmentReference = "DHL-SMOKE-LABEL-REF",
                TotalWeight = 250,
                Status = ShipmentStatus.Packed,
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                RowVersion = [1],
                Lines =
                {
                    new ShipmentLine
                    {
                        OrderLineId = TestOrderLineId,
                        Quantity = 1,
                        CreatedAtUtc = DateTime.UtcNow,
                        CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        RowVersion = [1]
                    }
                }
            });
            db.Set<Shipment>().Add(new Shipment
            {
                Id = TestReturnedShipmentId,
                OrderId = TestOrderId,
                Carrier = "DHL",
                Service = "DHL-SMOKE-RETURN",
                ProviderShipmentReference = "DHL-SMOKE-RETURN-REF",
                TrackingNumber = "DHLRETURN123",
                TotalWeight = 250,
                Status = ShipmentStatus.Returned,
                ShippedAtUtc = DateTime.UtcNow.AddDays(-2),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
                LastCarrierEventKey = "shipment.returned",
                CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                RowVersion = [1],
                Lines =
                {
                    new ShipmentLine
                    {
                        OrderLineId = TestOrderLineId,
                        Quantity = 1,
                        CreatedAtUtc = DateTime.UtcNow,
                        CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        RowVersion = [1]
                    }
                },
                CarrierEvents =
                {
                    new ShipmentCarrierEvent
                    {
                        Id = TestReturnedShipmentEventId,
                        Carrier = "DHL",
                        ProviderShipmentReference = "DHL-SMOKE-RETURN-REF",
                        CarrierEventKey = "shipment.returned",
                        ProviderStatus = "Returned",
                        ExceptionCode = "RETURNED_TO_SENDER",
                        ExceptionMessage = "Smoke return event",
                        TrackingNumber = "DHLRETURN123",
                        Service = "DHL-SMOKE-RETURN",
                        OccurredAtUtc = DateTime.UtcNow.AddDays(-1),
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                        CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        RowVersion = [1]
                    }
                }
            });
            db.SaveChanges();
        }

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        client.DefaultRequestHeaders.Add(TestAuthHeader, "true");
        return client;
    }

    private static void ConfigureSmokeHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile("appsettings.Testing.json", optional: true, reloadOnChange: false);
            config.AddJsonFile("appsettings.Testing.Development.json", optional: true, reloadOnChange: false);
            config.AddEnvironmentVariables();
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=Darwin_WebAdmin_SmokeTests;Trusted_Connection=True;TrustServerCertificate=True",
                ["DataProtection:RequireKeyEncryption"] = "false"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ISiteSettingCache>();
            services.AddSingleton<ISiteSettingCache, StaticSiteSettingCache>();
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, NoOpEmailSender>();
        });
    }

    private static void ConfigureTestAuthentication(IWebHostBuilder builder, bool allowPermissions)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPermissionService>();
            if (allowPermissions)
            {
                services.AddSingleton<IPermissionService, AllowAllPermissionService>();
            }
            else
            {
                services.AddSingleton<IPermissionService, DenyAllPermissionService>();
            }

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationScheme,
                    _ => { });
        });
    }

    private static SiteSetting CreateDefaultSiteSetting()
    {
        return new SiteSetting
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Title = "Darwin WebAdmin Smoke Tests",
            ContactEmail = "admin-smoke@example.test",
            HomeSlug = "home",
            DefaultCulture = SiteSettingDto.DefaultCultureDefault,
            SupportedCulturesCsv = SiteSettingDto.SupportedCulturesCsvDefault,
            DefaultCountry = SiteSettingDto.DefaultCountryDefault,
            DefaultCurrency = SiteSettingDto.DefaultCurrencyDefault,
            TimeZone = SiteSettingDto.TimeZoneDefault,
            DateFormat = SiteSettingDto.DateFormatDefault,
            TimeFormat = SiteSettingDto.TimeFormatDefault,
            JwtEnabled = true,
            JwtIssuer = "Darwin",
            JwtAudience = "Darwin.PublicApi",
            JwtSigningKey = "01234567890123456789012345678901",
            JwtClockSkewSeconds = 60,
            JwtAccessTokenMinutes = 15,
            JwtRefreshTokenDays = 30,
            JwtRequireDeviceBinding = true,
            MobileQrTokenRefreshSeconds = 30,
            MobileMaxOutboxItems = 200,
            BusinessManagementWebsiteUrl = "https://business.example.test",
            AccountDeletionUrl = "https://business.example.test/account/delete",
            ImpressumUrl = "https://business.example.test/impressum",
            PrivacyPolicyUrl = "https://business.example.test/privacy",
            BusinessTermsUrl = "https://business.example.test/terms",
            VatEnabled = true,
            DefaultVatRatePercent = 19m,
            PricesIncludeVat = true,
            InvoiceIssuerLegalName = "Darwin Smoke GmbH",
            InvoiceIssuerTaxId = "DE123456789",
            InvoiceIssuerAddressLine1 = "Smoke Street 1",
            InvoiceIssuerPostalCode = "10115",
            InvoiceIssuerCity = "Berlin",
            InvoiceIssuerCountry = "DE",
            ShipmentAttentionDelayHours = 24,
            ShipmentTrackingGraceHours = 12,
            SoftDeleteCleanupEnabled = true,
            SoftDeleteRetentionDays = 90,
            SoftDeleteCleanupBatchSize = 500,
            MeasurementSystem = "Metric",
            DisplayWeightUnit = "kg",
            DisplayLengthUnit = "cm",
            MeasurementSettingsJson = "{}",
            NumberFormattingOverridesJson = "{}",
            EnableCanonical = true,
            HreflangEnabled = true,
            SeoTitleTemplate = "{title} | Darwin",
            SeoMetaDescriptionTemplate = "Smoke settings description",
            OpenGraphDefaultsJson = "{}",
            FeatureFlagsJson = "{\"smoke\":true}",
            WhatsAppEnabled = true,
            WhatsAppBusinessPhoneId = "wa-phone-smoke",
            WhatsAppAccessToken = "wa-token-smoke",
            WhatsAppFromPhoneE164 = "+4915700000002",
            WhatsAppAdminRecipientsCsv = "+4915700000003",
            WebAuthnRelyingPartyId = "localhost",
            WebAuthnRelyingPartyName = "Darwin",
            WebAuthnAllowedOriginsCsv = "https://localhost",
            SmtpEnabled = true,
            SmtpHost = "smtp.example.test",
            SmtpPort = 587,
            SmtpEnableSsl = true,
            SmtpFromAddress = "noreply@example.test",
            SmtpFromDisplayName = "Darwin Smoke",
            SmsEnabled = true,
            SmsProvider = "Twilio",
            SmsFromPhoneE164 = "+4915700000000",
            SmsApiKey = "sms-key",
            SmsApiSecret = "sms-secret",
            SmsExtraSettingsJson = "{}",
            AdminAlertEmailsCsv = "alerts@example.test",
            AdminAlertSmsRecipientsCsv = "+4915700000004",
            TransactionalEmailSubjectPrefix = "[Smoke]",
            CommunicationTestInboxEmail = "communication-smoke@example.test",
            CommunicationTestSmsRecipientE164 = "+4915700000001",
            CommunicationTestWhatsAppRecipientE164 = "+4915700000003",
            CommunicationTestEmailSubjectTemplate = "[Smoke] Email transport {test_target}",
            CommunicationTestEmailBodyTemplate = "<p>Smoke email body {test_target}</p>",
            CommunicationTestSmsTemplate = "Smoke SMS {test_target}",
            CommunicationTestWhatsAppTemplate = "Smoke WhatsApp {test_target}",
            BusinessInvitationEmailSubjectTemplate = "Smoke invite {business_name}",
            BusinessInvitationEmailBodyTemplate = "<p>Smoke invitation {business_name}</p>",
            AccountActivationEmailSubjectTemplate = "Smoke activation {email}",
            AccountActivationEmailBodyTemplate = "<p>Smoke activation {email}</p>",
            PasswordResetEmailSubjectTemplate = "Smoke reset {email}",
            PasswordResetEmailBodyTemplate = "<p>Smoke reset {email}</p>",
            PhoneVerificationSmsTemplate = "Smoke phone SMS {token}",
            PhoneVerificationWhatsAppTemplate = "Smoke phone WhatsApp {token}",
            PhoneVerificationPreferredChannel = "Sms",
            PhoneVerificationAllowFallback = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            RowVersion = [1]
        };
    }

    private sealed class StaticSiteSettingCache : ISiteSettingCache
    {
        public static readonly SiteSettingDto Settings = new()
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Title = "Darwin WebAdmin Smoke Tests",
            DefaultCulture = SiteSettingDto.DefaultCultureDefault,
            SupportedCulturesCsv = SiteSettingDto.SupportedCulturesCsvDefault,
            DefaultCurrency = SiteSettingDto.DefaultCurrencyDefault,
            TimeZone = SiteSettingDto.TimeZoneDefault
        };

        public Task<SiteSettingDto> GetAsync(CancellationToken ct = default)
        {
            return Task.FromResult(Settings);
        }

        public void Invalidate()
        {
        }
    }

    private sealed class DatabaseSiteSettingCache : ISiteSettingCache
    {
        private readonly GetSiteSettingHandler _handler;

        public DatabaseSiteSettingCache(GetSiteSettingHandler handler)
        {
            _handler = handler;
        }

        public async Task<SiteSettingDto> GetAsync(CancellationToken ct = default)
        {
            var settings = await _handler.HandleAsync(ct).ConfigureAwait(false);
            return settings ?? StaticSiteSettingCache.Settings;
        }

        public void Invalidate()
        {
        }
    }

    private sealed class NoOpEmailSender : IEmailSender
    {
        public Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken ct = default,
            EmailDispatchContext? context = null)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class AllowAllPermissionService : IPermissionService
    {
        public Task<bool> HasAsync(Guid userId, string permissionKey, CancellationToken ct = default)
        {
            return Task.FromResult(true);
        }

        public Task<HashSet<string>> GetAllAsync(Guid userId, CancellationToken ct = default)
        {
            return Task.FromResult(new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "AccessAdminPanel",
                "FullAdminAccess"
            });
        }
    }

    private sealed class DenyAllPermissionService : IPermissionService
    {
        public Task<bool> HasAsync(Guid userId, string permissionKey, CancellationToken ct = default)
        {
            return Task.FromResult(false);
        }

        public Task<HashSet<string>> GetAllAsync(Guid userId, CancellationToken ct = default)
        {
            return Task.FromResult(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }
    }

    private sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(TestAuthHeader, out var value) ||
                !StringValuesContainsTrue(value))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "22222222-2222-2222-2222-222222222222"),
                new Claim(ClaimTypes.Name, "webadmin-smoke@example.test")
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        private static bool StringValuesContainsTrue(Microsoft.Extensions.Primitives.StringValues values)
        {
            return values.Any(value => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));
        }
    }
}
