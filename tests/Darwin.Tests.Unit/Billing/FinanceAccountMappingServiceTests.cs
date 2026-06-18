using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.Services;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Billing;

public sealed class FinanceAccountMappingServiceTests
{
    [Fact]
    public async Task UpsertMappingAsync_Should_CreateAndUpdateBusinessRoleMapping()
    {
        await using var db = FinanceAccountMappingTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var firstAccountId = SeedAccount(db, businessId, AccountType.Asset, "Accounts receivable", "AR");
        var secondAccountId = SeedAccount(db, businessId, AccountType.Asset, "Trade receivables", "AR-2");
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new FinanceAccountMappingService(db);

        var created = await service.UpsertMappingAsync(new UpsertFinanceAccountMappingCommand(
            businessId,
            FinancePostingAccountRole.Receivables,
            firstAccountId,
            Description: " Primary receivables ",
            MetadataJson: "{\"source\":\"unit\"}"), TestContext.Current.CancellationToken);
        var updated = await service.UpsertMappingAsync(new UpsertFinanceAccountMappingCommand(
            businessId,
            FinancePostingAccountRole.Receivables,
            secondAccountId,
            Description: " Updated receivables "), TestContext.Current.CancellationToken);

        created.Succeeded.Should().BeTrue();
        updated.Succeeded.Should().BeTrue();
        updated.Value.Should().Be(created.Value);
        var mapping = await db.Set<FinancePostingAccountMapping>().SingleAsync(TestContext.Current.CancellationToken);
        mapping.FinancialAccountId.Should().Be(secondAccountId);
        mapping.Description.Should().Be("Updated receivables");
        mapping.MetadataJson.Should().Be("{}");
    }

    [Fact]
    public async Task UpsertMappingAsync_Should_RejectCrossBusinessOrIncompatibleAccounts()
    {
        await using var db = FinanceAccountMappingTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var otherBusinessAccountId = SeedAccount(db, Guid.NewGuid(), AccountType.Asset, "Other AR");
        var revenueAccountId = SeedAccount(db, businessId, AccountType.Revenue, "Revenue");
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new FinanceAccountMappingService(db);

        var crossBusiness = await service.UpsertMappingAsync(new UpsertFinanceAccountMappingCommand(
            businessId,
            FinancePostingAccountRole.Receivables,
            otherBusinessAccountId), TestContext.Current.CancellationToken);
        var incompatible = await service.UpsertMappingAsync(new UpsertFinanceAccountMappingCommand(
            businessId,
            FinancePostingAccountRole.Receivables,
            revenueAccountId), TestContext.Current.CancellationToken);

        crossBusiness.Succeeded.Should().BeFalse();
        crossBusiness.Error.Should().Contain("business");
        incompatible.Succeeded.Should().BeFalse();
        incompatible.Error.Should().Contain("compatible");
        db.Set<FinancePostingAccountMapping>().Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveRequiredAccountsAsync_Should_ReturnActiveMappings_AndFailMissingInactiveOrDeleted()
    {
        await using var db = FinanceAccountMappingTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var receivablesAccountId = SeedAccount(db, businessId, AccountType.Asset, "Receivables");
        var revenueAccountId = SeedAccount(db, businessId, AccountType.Revenue, "Revenue");
        var inactiveAccountId = SeedAccount(db, businessId, AccountType.Liability, "Tax payable");
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new FinanceAccountMappingService(db);
        await service.UpsertMappingAsync(new UpsertFinanceAccountMappingCommand(
            businessId,
            FinancePostingAccountRole.Receivables,
            receivablesAccountId), TestContext.Current.CancellationToken);
        await service.UpsertMappingAsync(new UpsertFinanceAccountMappingCommand(
            businessId,
            FinancePostingAccountRole.SalesRevenue,
            revenueAccountId), TestContext.Current.CancellationToken);
        await service.UpsertMappingAsync(new UpsertFinanceAccountMappingCommand(
            businessId,
            FinancePostingAccountRole.TaxPayable,
            inactiveAccountId,
            IsActive: false), TestContext.Current.CancellationToken);

        var resolved = await service.ResolveRequiredAccountsAsync(
            businessId,
            [FinancePostingAccountRole.Receivables, FinancePostingAccountRole.SalesRevenue],
            TestContext.Current.CancellationToken);
        var missingInactive = await service.ResolveRequiredAccountsAsync(
            businessId,
            [FinancePostingAccountRole.TaxPayable],
            TestContext.Current.CancellationToken);

        resolved.Succeeded.Should().BeTrue();
        resolved.Value![FinancePostingAccountRole.Receivables].Should().Be(receivablesAccountId);
        resolved.Value[FinancePostingAccountRole.SalesRevenue].Should().Be(revenueAccountId);
        missingInactive.Succeeded.Should().BeFalse();
        missingInactive.Error.Should().Contain(nameof(FinancePostingAccountRole.TaxPayable));
    }

    [Fact]
    public async Task ResolveRequiredAccountsAsync_Should_Fail_WhenMappedAccountWasDeleted()
    {
        await using var db = FinanceAccountMappingTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var accountId = SeedAccount(db, businessId, AccountType.Asset, "Cash clearing");
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new FinanceAccountMappingService(db);
        await service.UpsertMappingAsync(new UpsertFinanceAccountMappingCommand(
            businessId,
            FinancePostingAccountRole.CashClearing,
            accountId), TestContext.Current.CancellationToken);
        var account = await db.Set<FinancialAccount>().SingleAsync(TestContext.Current.CancellationToken);
        account.IsDeleted = true;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await service.ResolveRequiredAccountsAsync(
            businessId,
            [FinancePostingAccountRole.CashClearing],
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain(nameof(FinancePostingAccountRole.CashClearing));
    }

    [Fact]
    public async Task UpsertMappingAsync_Should_RejectMissingFieldsAndSensitiveMetadata()
    {
        await using var db = FinanceAccountMappingTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var accountId = SeedAccount(db, businessId, AccountType.Asset, "Receivables");
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new FinanceAccountMappingService(db);

        var missingBusiness = await service.UpsertMappingAsync(new UpsertFinanceAccountMappingCommand(
            Guid.Empty,
            FinancePostingAccountRole.Receivables,
            accountId), TestContext.Current.CancellationToken);
        var sensitive = await service.UpsertMappingAsync(new UpsertFinanceAccountMappingCommand(
            businessId,
            FinancePostingAccountRole.Receivables,
            accountId,
            MetadataJson: "{\"apiToken\":\"secret\"}"), TestContext.Current.CancellationToken);

        missingBusiness.Succeeded.Should().BeFalse();
        sensitive.Succeeded.Should().BeFalse();
        sensitive.Error.Should().Contain("Sensitive");
        db.Set<FinancePostingAccountMapping>().Should().BeEmpty();
    }

    [Theory]
    [InlineData(FinancePostingAccountRole.Receivables, AccountType.Asset, true)]
    [InlineData(FinancePostingAccountRole.SalesRevenue, AccountType.Revenue, true)]
    [InlineData(FinancePostingAccountRole.TaxPayable, AccountType.Liability, true)]
    [InlineData(FinancePostingAccountRole.CashClearing, AccountType.Asset, true)]
    [InlineData(FinancePostingAccountRole.RefundClearing, AccountType.Liability, true)]
    [InlineData(FinancePostingAccountRole.Rounding, AccountType.Expense, true)]
    [InlineData(FinancePostingAccountRole.Receivables, AccountType.Revenue, false)]
    [InlineData(FinancePostingAccountRole.SalesRevenue, AccountType.Asset, false)]
    public void IsAccountTypeAllowed_Should_EnforceRoleCompatibility(
        FinancePostingAccountRole role,
        AccountType accountType,
        bool expected)
    {
        FinanceAccountMappingService.IsAccountTypeAllowed(role, accountType).Should().Be(expected);
    }

    private static Guid SeedAccount(
        FinanceAccountMappingTestDbContext db,
        Guid businessId,
        AccountType accountType,
        string name,
        string? code = null)
    {
        var account = new FinancialAccount
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = name,
            Type = accountType,
            Code = code
        };
        db.Set<FinancialAccount>().Add(account);
        return account.Id;
    }

    private sealed class FinanceAccountMappingTestDbContext : DbContext, IAppDbContext
    {
        private FinanceAccountMappingTestDbContext(DbContextOptions<FinanceAccountMappingTestDbContext> options)
            : base(options)
        {
        }

        public static FinanceAccountMappingTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<FinanceAccountMappingTestDbContext>()
                .UseInMemoryDatabase($"darwin_finance_account_mapping_tests_{Guid.NewGuid()}")
                .Options;
            return new FinanceAccountMappingTestDbContext(options);
        }

        public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();
        public DbSet<FinancePostingAccountMapping> FinancePostingAccountMappings => Set<FinancePostingAccountMapping>();
    }
}
