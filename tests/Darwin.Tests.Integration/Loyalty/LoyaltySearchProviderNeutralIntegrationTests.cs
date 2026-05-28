using Darwin.Application.Loyalty.Queries;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Tests.Integration.Loyalty;

public sealed class LoyaltySearchProviderNeutralIntegrationTests
    : DeterministicIntegrationTestBase,
      IClassFixture<WebApplicationFactory<Program>>
{
    public LoyaltySearchProviderNeutralIntegrationTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetLoyaltyAccountsPage_Should_HandleEscapedSubstringAndCaseVariants_OnEmailSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchEmail = $"loyalty_%_probe[{marker}]@example.test";
        var unrelatedEmail = $"loyaltyXprobe[{marker.Substring(0, 6)}]@example.test";
        var businessId = Guid.NewGuid();

        var exactMatchUser = new User(exactMatchEmail, "hash", Guid.NewGuid().ToString("N"));
        var unrelatedUser = new User(unrelatedEmail, "hash", Guid.NewGuid().ToString("N"));

        db.Set<User>().AddRange(exactMatchUser, unrelatedUser);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<LoyaltyAccount>().AddRange(
            new LoyaltyAccount
            {
                BusinessId = businessId,
                UserId = exactMatchUser.Id,
                Status = LoyaltyAccountStatus.Active,
                PointsBalance = 50
            },
            new LoyaltyAccount
            {
                BusinessId = businessId,
                UserId = unrelatedUser.Id,
                Status = LoyaltyAccountStatus.Active,
                PointsBalance = 20
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetLoyaltyAccountsPageHandler>();
        var lowerCaseResult = await handler.HandleAsync(
            businessId,
            page: 1,
            pageSize: 20,
            query: $"loyalty_%_probe[{marker}]@example.test",
            status: null,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            businessId,
            page: 1,
            pageSize: 20,
            query: $"LOYALTY_%_PROBE[{marker}]@EXAMPLE.TEST",
            status: null,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.UserEmail == exactMatchEmail);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.UserEmail == exactMatchEmail);
    }

    [Fact]
    public async Task GetLoyaltyRedemptionsPage_Should_HandleEscapedSubstringAndCaseVariants_OnConsumerEmailSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchEmail = $"redeem_%_probe[{marker}]@example.test";
        var unrelatedEmail = $"redeemXprobe[{marker.Substring(0, 6)}]@example.test";
        var businessId = Guid.NewGuid();

        var exactMatchUser = new User(exactMatchEmail, "hash", Guid.NewGuid().ToString("N"));
        var unrelatedUser = new User(unrelatedEmail, "hash", Guid.NewGuid().ToString("N"));
        db.Set<User>().AddRange(exactMatchUser, unrelatedUser);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var program = new LoyaltyProgram
        {
            BusinessId = businessId,
            Name = $"Program {marker}"
        };
        db.Set<LoyaltyProgram>().Add(program);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var rewardTier = new LoyaltyRewardTier
        {
            LoyaltyProgramId = program.Id,
            PointsRequired = 10,
            RewardType = LoyaltyRewardType.FreeItem,
            Description = "Free cookie"
        };
        var exactMatchAccount = new LoyaltyAccount
        {
            BusinessId = businessId,
            UserId = exactMatchUser.Id,
            Status = LoyaltyAccountStatus.Active
        };
        var unrelatedAccount = new LoyaltyAccount
        {
            BusinessId = businessId,
            UserId = unrelatedUser.Id,
            Status = LoyaltyAccountStatus.Active
        };

        db.Set<LoyaltyRewardTier>().Add(rewardTier);
        db.Set<LoyaltyAccount>().AddRange(exactMatchAccount, unrelatedAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var exactMatchRedemption = new LoyaltyRewardRedemption
        {
            LoyaltyAccountId = exactMatchAccount.Id,
            BusinessId = businessId,
            LoyaltyRewardTierId = rewardTier.Id,
            PointsSpent = 5,
            Status = LoyaltyRedemptionStatus.Pending
        };
        var unrelatedRedemption = new LoyaltyRewardRedemption
        {
            LoyaltyAccountId = unrelatedAccount.Id,
            BusinessId = businessId,
            LoyaltyRewardTierId = rewardTier.Id,
            PointsSpent = 6,
            Status = LoyaltyRedemptionStatus.Pending
        };

        db.Set<LoyaltyRewardRedemption>().AddRange(exactMatchRedemption, unrelatedRedemption);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var exactMatchRedemptionId = exactMatchRedemption.Id;

        var handler = scope.ServiceProvider.GetRequiredService<GetLoyaltyRedemptionsPageHandler>();
        var lowerCaseResult = await handler.HandleAsync(
            businessId,
            page: 1,
            pageSize: 20,
            query: $"redeem_%_probe[{marker}]@example.test",
            status: null,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            businessId,
            page: 1,
            pageSize: 20,
            query: $"REDEEM_%_PROBE[{marker}]@EXAMPLE.TEST",
            status: null,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.Id == exactMatchRedemptionId);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.Id == exactMatchRedemptionId);
    }
}
