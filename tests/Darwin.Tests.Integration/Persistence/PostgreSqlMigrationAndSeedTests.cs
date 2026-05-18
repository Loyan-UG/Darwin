using Darwin.Infrastructure.Persistence.Db;
using Darwin.Infrastructure.Persistence.Seed;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using Darwin.Tests.Common.TestInfrastructure;
using Darwin.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using Xunit.Sdk;

namespace Darwin.Tests.Integration.Persistence;

public sealed class PostgreSqlMigrationAndSeedTests : DeterministicIntegrationTestBase,
    IClassFixture<WebApplicationFactory<Program>>
{
    public PostgreSqlMigrationAndSeedTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task FreshPostgreSqlDatabase_Should_ApplyMigrations_AndSeedExpectedRows()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        if (!string.Equals(db.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            throw SkipException.ForSkip(
                "Skipping PostgreSQL migration smoke. Configure a PostgreSQL Testing provider for this run.");
        }

        await db.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);

        var pendingMigrations = await db.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        pendingMigrations.Should().BeEmpty();

        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);

        var seedExists = await db.SiteSettings.AnyAsync(
            setting => setting.Title == "Darwin" && setting.ContactEmail == "admin@darwin.de",
            TestContext.Current.CancellationToken).ConfigureAwait(false);

        var webhookSubscriptionExists = await db.WebhookSubscriptions.AnyAsync(
            sub => sub.EventType == "order.created" && sub.IsActive,
            TestContext.Current.CancellationToken).ConfigureAwait(false);

        var webhookDeliveryExists = await db.WebhookDeliveries.AnyAsync(
            delivery => delivery.Status == "Pending",
            TestContext.Current.CancellationToken).ConfigureAwait(false);

        seedExists.Should().BeTrue();
        webhookSubscriptionExists.Should().BeTrue();
        webhookDeliveryExists.Should().BeTrue();
    }

    [Fact]
    public async Task FreshSqlServerDatabase_Should_ApplyMigrations_AndSeedExpectedRows_WithoutApplyingApplicationTablesToDbo()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        if (!string.Equals(db.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            throw SkipException.ForSkip(
                "Skipping SQL Server migration smoke. Configure a SQL Server Testing provider for this run.");
        }

        await db.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);

        var pendingMigrations = await db.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        pendingMigrations.Should().BeEmpty();

        using (var command = db.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";

            if (command.Connection is not null && command.Connection.State != ConnectionState.Open)
            {
                await command.Connection.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
            }

            using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
            var actualTables = new List<(string Schema, string Name)>();

            while (await reader.ReadAsync(TestContext.Current.CancellationToken).ConfigureAwait(false))
            {
                actualTables.Add((reader.GetString(0), reader.GetString(1)));
            }

            var expectedModelTables = db.Model.GetEntityTypes()
                .Where(type => type.GetTableName() is not null)
                .Select(type => (Schema: type.GetSchema() ?? string.Empty, Name: type.GetTableName()!))
                .Distinct()
                .Where(mapping => !string.IsNullOrWhiteSpace(mapping.Schema))
                .ToList();

            expectedModelTables.Should().NotBeEmpty();

            foreach (var expected in expectedModelTables)
            {
                actualTables.Should().Contain(
                    expected,
                    "all mapped application tables should be created with their configured schema");

                expected.Schema.Should().NotBe("dbo");
            }
        }

        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);

        var seedExists = await db.SiteSettings.AnyAsync(
            setting => setting.Title == "Darwin" && setting.ContactEmail == "admin@darwin.de",
            TestContext.Current.CancellationToken).ConfigureAwait(false);

        var webhookSubscriptionExists = await db.WebhookSubscriptions.AnyAsync(
            sub => sub.EventType == "order.created" && sub.IsActive,
            TestContext.Current.CancellationToken).ConfigureAwait(false);

        var webhookDeliveryExists = await db.WebhookDeliveries.AnyAsync(
            delivery => delivery.Status == "Pending",
            TestContext.Current.CancellationToken).ConfigureAwait(false);

        seedExists.Should().BeTrue();
        webhookSubscriptionExists.Should().BeTrue();
        webhookDeliveryExists.Should().BeTrue();
    }

    [Fact]
    public async Task FreshPostgreSqlDatabase_Should_RejectInvalidJsonWrites_ForAllJsonConstrainedColumns()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        if (!string.Equals(db.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            throw SkipException.ForSkip(
                "Skipping PostgreSQL JSON validity coverage. Configure a PostgreSQL Testing provider for this run.");
        }

        var ct = TestContext.Current.CancellationToken;
        var invalidJson = "not-a-json";

        await AssertInvalidJsonWriteAsync(
            "CK_PG_BillingPlans_FeaturesJson_ValidJson",
            async context =>
            {
                context.Set<BillingPlan>().Add(new BillingPlan
                {
                    Name = "JSON Validity Plan",
                    Code = $"json-plan-{Guid.NewGuid():N}",
                    PriceMinor = 100,
                    FeaturesJson = invalidJson
                });
            }).ConfigureAwait(false);

        await AssertInvalidJsonWriteAsync(
            "CK_PG_Businesses_AdminTextOverridesJson_ValidJson",
            async context =>
            {
                context.Set<Business>().Add(new Business
                {
                    Name = $"JSON Validity Business {Guid.NewGuid():N}",
                    AdminTextOverridesJson = invalidJson
                });
            }).ConfigureAwait(false);

        await AssertInvalidJsonWriteAsync(
            "CK_PG_Carts_SelectedAddOnValueIdsJson_ValidJson",
            async context =>
            {
                var cart = new Cart { Currency = DomainDefaults.DefaultCurrency };

                cart.Items.Add(new CartItem
                {
                    VariantId = Guid.NewGuid(),
                    Quantity = 1,
                    UnitPriceNetMinor = 100,
                    VatRate = 0.19m,
                    SelectedAddOnValueIdsJson = invalidJson
                });

                context.Set<Cart>().Add(cart);
            }).ConfigureAwait(false);

        await AssertInvalidJsonWriteAsync(
            "CK_PG_EventLogs_PropertiesJson_ValidJson",
            async context =>
            {
                context.Set<EventLog>().Add(new EventLog
                {
                    Type = "Test",
                    OccurredAtUtc = DateTime.UtcNow,
                    PropertiesJson = invalidJson,
                    UtmSnapshotJson = "{}"
                });
            }).ConfigureAwait(false);

        await AssertInvalidJsonWriteAsync(
            "CK_PG_EventLogs_UtmSnapshotJson_ValidJson",
            async context =>
            {
                context.Set<EventLog>().Add(new EventLog
                {
                    Type = "Test",
                    OccurredAtUtc = DateTime.UtcNow,
                    PropertiesJson = "{}",
                    UtmSnapshotJson = invalidJson
                });
            }).ConfigureAwait(false);

        await AssertInvalidJsonWriteAsync(
            "CK_PG_ProviderCallbackInboxMessages_PayloadJson_ValidJson",
            async context =>
            {
                context.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
                {
                    Provider = "Stripe",
                    CallbackType = "Webhook",
                    PayloadJson = invalidJson,
                    Status = "Pending"
                });
            }).ConfigureAwait(false);

        await AssertInvalidJsonWriteAsync(
            "CK_PG_LoyaltyPrograms_RulesJson_ValidJson",
            async context =>
            {
                var businessId = await EnsureBusinessIdAsync(context, ct).ConfigureAwait(false);

                context.Set<LoyaltyProgram>().Add(new LoyaltyProgram
                {
                    BusinessId = businessId,
                    Name = $"JSON Validity Program {Guid.NewGuid():N}",
                    RulesJson = invalidJson
                });
            }).ConfigureAwait(false);

        await AssertInvalidJsonWriteAsync(
            "CK_PG_LoyaltyRewardRedemptions_MetadataJson_ValidJson",
            async context =>
            {
                var businessId = await EnsureBusinessIdAsync(context, ct).ConfigureAwait(false);
                var accountId = await EnsureLoyaltyAccountIdAsync(context, businessId, ct).ConfigureAwait(false);
                var programId = await EnsureLoyaltyProgramIdAsync(context, businessId, ct).ConfigureAwait(false);
                var rewardTierId = await EnsureLoyaltyRewardTierIdAsync(context, programId, ct).ConfigureAwait(false);

                context.Set<LoyaltyRewardRedemption>().Add(new LoyaltyRewardRedemption
                {
                    LoyaltyAccountId = accountId,
                    BusinessId = businessId,
                    LoyaltyRewardTierId = rewardTierId,
                    PointsSpent = 1,
                    MetadataJson = invalidJson
                });
            }).ConfigureAwait(false);

        await AssertInvalidJsonWriteAsync(
            "CK_PG_LoyaltyRewardTiers_MetadataJson_ValidJson",
            async context =>
            {
                var businessId = await EnsureBusinessIdAsync(context, ct).ConfigureAwait(false);
                var programId = await EnsureLoyaltyProgramIdAsync(context, businessId, ct).ConfigureAwait(false);

                context.Set<LoyaltyRewardTier>().Add(new LoyaltyRewardTier
                {
                    LoyaltyProgramId = programId,
                    PointsRequired = 1,
                    RewardType = LoyaltyRewardType.FreeItem,
                    MetadataJson = invalidJson
                });
            }).ConfigureAwait(false);

        await AssertInvalidJsonWriteAsync(
            "CK_PG_ScanSessions_SelectedRewardsJson_ValidJson",
            async context =>
            {
                var businessId = await EnsureBusinessIdAsync(context, ct).ConfigureAwait(false);
                var accountId = await EnsureLoyaltyAccountIdAsync(context, businessId, ct).ConfigureAwait(false);

                var token = new QrCodeToken
                {
                    UserId = Guid.NewGuid(),
                    LoyaltyAccountId = accountId,
                    Token = $"scan-{Guid.NewGuid():N}",
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15)
                };

                context.Set<QrCodeToken>().Add(token);

                context.Set<ScanSession>().Add(new ScanSession
                {
                    QrCodeTokenId = token.Id,
                    LoyaltyAccountId = accountId,
                    BusinessId = businessId,
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
                    SelectedRewardsJson = invalidJson
                });
            }).ConfigureAwait(false);

        await AssertInvalidJsonWriteAsync(
            "CK_PG_SiteSettings_AdminTextOverridesJson_ValidJson",
            async context =>
            {
                var siteSetting = await EnsureSiteSettingAsync(context, ct).ConfigureAwait(false);
                siteSetting.AdminTextOverridesJson = invalidJson;
            }).ConfigureAwait(false);

        async Task AssertInvalidJsonWriteAsync(
            string expectedConstraintName,
            Func<DarwinDbContext, Task> arrangeInvalidWrite)
        {
            using var writeScope = Factory.Services.CreateScope();
            var attemptDb = writeScope.ServiceProvider.GetRequiredService<DarwinDbContext>();

            await arrangeInvalidWrite(attemptDb).ConfigureAwait(false);

            var act = async () => await attemptDb.SaveChangesAsync(ct).ConfigureAwait(false);
            var assertion = await act.Should().ThrowAsync<DbUpdateException>(ct).ConfigureAwait(false);

            assertion.Which.InnerException.Should().NotBeNull();
            assertion.Which.InnerException!.Message.Should().Contain(expectedConstraintName);
        }
    }

    private static async Task<Guid> EnsureBusinessIdAsync(DarwinDbContext db, CancellationToken cancellationToken)
    {
        var businessId = await db.Set<Business>()
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (businessId != Guid.Empty)
        {
            return businessId;
        }

        var business = new Business
        {
            Name = $"JSON Validity {Guid.NewGuid():N}"
        };

        db.Set<Business>().Add(business);
        return business.Id;
    }

    private static async Task<Guid> EnsureLoyaltyProgramIdAsync(
        DarwinDbContext db,
        Guid businessId,
        CancellationToken cancellationToken)
    {
        var programId = await db.Set<LoyaltyProgram>()
            .Where(program => program.BusinessId == businessId)
            .Select(program => program.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (programId != Guid.Empty)
        {
            return programId;
        }

        var program = new LoyaltyProgram
        {
            BusinessId = businessId,
            Name = $"JSON Validity Program {Guid.NewGuid():N}",
            RulesJson = "{}"
        };

        db.Set<LoyaltyProgram>().Add(program);
        return program.Id;
    }

    private static async Task<Guid> EnsureLoyaltyRewardTierIdAsync(
        DarwinDbContext db,
        Guid programId,
        CancellationToken cancellationToken)
    {
        var rewardTierId = await db.Set<LoyaltyRewardTier>()
            .Where(rewardTier => rewardTier.LoyaltyProgramId == programId)
            .Select(rewardTier => rewardTier.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (rewardTierId != Guid.Empty)
        {
            return rewardTierId;
        }

        var rewardTier = new LoyaltyRewardTier
        {
            LoyaltyProgramId = programId,
            PointsRequired = 1,
            RewardType = LoyaltyRewardType.FreeItem,
            MetadataJson = "{}"
        };

        db.Set<LoyaltyRewardTier>().Add(rewardTier);
        return rewardTier.Id;
    }

    private static async Task<Guid> EnsureLoyaltyAccountIdAsync(
        DarwinDbContext db,
        Guid businessId,
        CancellationToken cancellationToken)
    {
        var accountId = await db.Set<LoyaltyAccount>()
            .Where(account => account.BusinessId == businessId)
            .Select(account => account.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (accountId != Guid.Empty)
        {
            return accountId;
        }

        var account = new LoyaltyAccount
        {
            BusinessId = businessId,
            UserId = Guid.NewGuid()
        };

        db.Set<LoyaltyAccount>().Add(account);
        return account.Id;
    }

    private static async Task<SiteSetting> EnsureSiteSettingAsync(DarwinDbContext db, CancellationToken cancellationToken)
    {
        var setting = await db.Set<SiteSetting>()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (setting is not null)
        {
            return setting;
        }

        setting = new SiteSetting
        {
            Title = "Darwin",
            ContactEmail = "admin@darwin.de",
            HomeSlug = "home",
            DefaultCulture = DomainDefaults.DefaultCulture,
            SupportedCulturesCsv = DomainDefaults.SupportedCulturesCsv,
            DefaultCountry = DomainDefaults.DefaultCountryCode,
            DefaultCurrency = DomainDefaults.DefaultCurrency,
            TimeZone = DomainDefaults.DefaultTimezone,
            DateFormat = "dd.MM.yyyy",
            TimeFormat = "HH:mm",
            MeasurementSystem = "Metric",
            DisplayWeightUnit = "kg",
            DisplayLengthUnit = "cm"
        };

        db.Set<SiteSetting>().Add(setting);
        return setting;
    }
}
