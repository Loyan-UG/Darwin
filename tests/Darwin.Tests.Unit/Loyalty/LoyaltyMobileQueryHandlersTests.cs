using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Loyalty;

/// <summary>
/// Covers consumer/mobile Loyalty query handlers that were not previously unit-tested:
/// <see cref="GetMyLoyaltyBusinessesHandler"/>,
/// <see cref="GetMyLoyaltyAccountForBusinessHandler"/>,
/// <see cref="GetAvailableLoyaltyRewardsForBusinessHandler"/>,
/// <see cref="GetMyLoyaltyTimelinePageHandler"/>.
/// </summary>
public sealed class LoyaltyMobileQueryHandlersTests
{
    // ─── GetMyLoyaltyBusinessesHandler ───────────────────────────────────────

    [Fact]
    public async Task GetMyLoyaltyBusinesses_Should_ReturnBusinessesWithAccounts_ForCurrentUser()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId1 = Guid.NewGuid();
        var businessId2 = Guid.NewGuid();

        db.Set<Business>().AddRange(
            new Business { Id = businessId1, Name = "Cafe Aurora", IsActive = true },
            new Business { Id = businessId2, Name = "Backwerk Mitte", IsActive = true });
        db.Set<LoyaltyAccount>().AddRange(
            new LoyaltyAccount { Id = Guid.NewGuid(), BusinessId = businessId1, UserId = userId, Status = LoyaltyAccountStatus.Active, PointsBalance = 250, LifetimePoints = 500, RowVersion = [1] },
            new LoyaltyAccount { Id = Guid.NewGuid(), BusinessId = businessId2, UserId = userId, Status = LoyaltyAccountStatus.Suspended, PointsBalance = 50, LifetimePoints = 200, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyBusinessesHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        var (items, total) = await handler.HandleAsync(new MyLoyaltyBusinessListRequestDto(), TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().HaveCount(2);
        items.Select(x => x.BusinessName).Should().BeEquivalentTo(["Cafe Aurora", "Backwerk Mitte"]);
    }

    [Fact]
    public async Task GetMyLoyaltyBusinesses_Should_ReturnEmpty_WhenUserHasNoAccounts()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();

        var handler = new GetMyLoyaltyBusinessesHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        var (items, total) = await handler.HandleAsync(new MyLoyaltyBusinessListRequestDto(), TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyLoyaltyBusinesses_Should_NotReturnAccountsForOtherUsers()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Other Business", IsActive = true });
        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount { Id = Guid.NewGuid(), BusinessId = businessId, UserId = otherUserId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyBusinessesHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        var (items, total) = await handler.HandleAsync(new MyLoyaltyBusinessListRequestDto(), TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyLoyaltyBusinesses_Should_ExcludeDeletedBusinesses()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        var activeBusinessId = Guid.NewGuid();
        var deletedBusinessId = Guid.NewGuid();

        db.Set<Business>().AddRange(
            new Business { Id = activeBusinessId, Name = "Active Business", IsActive = true },
            new Business { Id = deletedBusinessId, Name = "Deleted Business", IsActive = true, IsDeleted = true });
        db.Set<LoyaltyAccount>().AddRange(
            new LoyaltyAccount { Id = Guid.NewGuid(), BusinessId = activeBusinessId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] },
            new LoyaltyAccount { Id = Guid.NewGuid(), BusinessId = deletedBusinessId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyBusinessesHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        var (items, total) = await handler.HandleAsync(new MyLoyaltyBusinessListRequestDto(), TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items[0].BusinessName.Should().Be("Active Business");
    }

    [Fact]
    public async Task GetMyLoyaltyBusinesses_Should_ExcludeInactiveBusinesses_WhenNotIncluded()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        var activeBusinessId = Guid.NewGuid();
        var inactiveBusinessId = Guid.NewGuid();

        db.Set<Business>().AddRange(
            new Business { Id = activeBusinessId, Name = "Active", IsActive = true },
            new Business { Id = inactiveBusinessId, Name = "Inactive", IsActive = false });
        db.Set<LoyaltyAccount>().AddRange(
            new LoyaltyAccount { Id = Guid.NewGuid(), BusinessId = activeBusinessId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] },
            new LoyaltyAccount { Id = Guid.NewGuid(), BusinessId = inactiveBusinessId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyBusinessesHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        var (items, total) = await handler.HandleAsync(new MyLoyaltyBusinessListRequestDto { IncludeInactiveBusinesses = false }, TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items[0].BusinessName.Should().Be("Active");
    }

    [Fact]
    public async Task GetMyLoyaltyBusinesses_Should_IncludeLocationCityWhenPrimaryLocationExists()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Cafe Berlin", IsActive = true });
        db.Set<BusinessLocation>().Add(new BusinessLocation { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Main Branch", City = "Berlin", IsPrimary = true, RowVersion = [1] });
        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount { Id = Guid.NewGuid(), BusinessId = businessId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyBusinessesHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        var (items, _) = await handler.HandleAsync(new MyLoyaltyBusinessListRequestDto(), TestContext.Current.CancellationToken);

        items[0].City.Should().Be("Berlin");
    }

    [Fact]
    public async Task GetMyLoyaltyBusinesses_Should_SupportPagination()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        for (var i = 0; i < 5; i++)
        {
            var bizId = Guid.NewGuid();
            db.Set<Business>().Add(new Business { Id = bizId, Name = $"Business {i}", IsActive = true });
            db.Set<LoyaltyAccount>().Add(new LoyaltyAccount { Id = Guid.NewGuid(), BusinessId = bizId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyBusinessesHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        var (items, total) = await handler.HandleAsync(new MyLoyaltyBusinessListRequestDto { Page = 1, PageSize = 3 }, TestContext.Current.CancellationToken);

        total.Should().Be(5);
        items.Should().HaveCount(3);
    }

    // ─── GetMyLoyaltyAccountForBusinessHandler ───────────────────────────────

    [Fact]
    public async Task GetMyLoyaltyAccountForBusiness_Should_ReturnAccount_WhenExists()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Test Biz", IsActive = true });
        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            UserId = userId,
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = 400,
            LifetimePoints = 800,
            RowVersion = [1]
        });
        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram { Id = programId, BusinessId = businessId, Name = "Rewards", IsActive = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyAccountForBusinessHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.BusinessId.Should().Be(businessId);
        result.Value.PointsBalance.Should().Be(400);
        result.Value.LifetimePoints.Should().Be(800);
        result.Value.Status.Should().Be(LoyaltyAccountStatus.Active);
    }

    [Fact]
    public async Task GetMyLoyaltyAccountForBusiness_Should_ReturnNullOk_WhenNoAccount()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Test Biz", IsActive = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyAccountForBusinessHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetMyLoyaltyAccountForBusiness_Should_ReturnFail_WhenBusinessIdIsEmpty()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var handler = new GetMyLoyaltyAccountForBusinessHandler(db, new StubCurrentUserService(Guid.NewGuid()), new TestLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(Guid.Empty, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task GetMyLoyaltyAccountForBusiness_Should_ReturnFail_WhenUserIdIsEmpty()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var handler = new GetMyLoyaltyAccountForBusinessHandler(db, new StubCurrentUserService(Guid.Empty), new TestLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task GetMyLoyaltyAccountForBusiness_Should_ExcludeDeletedAccount()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Test Biz", IsActive = true });
        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            UserId = userId,
            Status = LoyaltyAccountStatus.Active,
            IsDeleted = true,
            RowVersion = [1]
        });
        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Rewards", IsActive = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyAccountForBusinessHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    // ─── GetAvailableLoyaltyRewardsForBusinessHandler ─────────────────────────

    [Fact]
    public async Task GetAvailableLoyaltyRewards_Should_ReturnRewards_ForBusinessWithActiveProgramAndAccount()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram { Id = programId, BusinessId = businessId, Name = "Rewards", IsActive = true, RowVersion = [1] });
        db.Set<LoyaltyRewardTier>().AddRange(
            new LoyaltyRewardTier { Id = Guid.NewGuid(), LoyaltyProgramId = programId, PointsRequired = 100, Description = "Free Coffee", AllowSelfRedemption = true, RowVersion = [1] },
            new LoyaltyRewardTier { Id = Guid.NewGuid(), LoyaltyProgramId = programId, PointsRequired = 500, Description = "Free Cake", AllowSelfRedemption = false, RowVersion = [1] });
        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount { Id = Guid.NewGuid(), BusinessId = businessId, UserId = userId, Status = LoyaltyAccountStatus.Active, PointsBalance = 150, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAvailableLoyaltyRewardsForBusinessHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        var freeCoffee = result.Value!.Single(x => x.Name == "Free Coffee");
        freeCoffee.IsSelectable.Should().BeTrue();
        freeCoffee.RequiresConfirmation.Should().BeFalse();
        var freeCake = result.Value.Single(x => x.Name == "Free Cake");
        freeCake.IsSelectable.Should().BeFalse(); // 150 < 500
        freeCake.RequiresConfirmation.Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailableLoyaltyRewards_Should_ReturnEmptyList_WhenNoActiveProgram()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        var handler = new GetAvailableLoyaltyRewardsForBusinessHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableLoyaltyRewards_Should_ReturnFail_WhenBusinessIdIsEmpty()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var handler = new GetAvailableLoyaltyRewardsForBusinessHandler(db, new StubCurrentUserService(Guid.NewGuid()), new TestLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(Guid.Empty, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableLoyaltyRewards_Should_ReturnFail_WhenUserIdIsEmpty()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Rewards", IsActive = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAvailableLoyaltyRewardsForBusinessHandler(db, new StubCurrentUserService(Guid.Empty), new TestLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableLoyaltyRewards_Should_MarkRewardsNotSelectable_WhenAccountInactive()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram { Id = programId, BusinessId = businessId, Name = "Rewards", IsActive = true, RowVersion = [1] });
        db.Set<LoyaltyRewardTier>().Add(new LoyaltyRewardTier { Id = Guid.NewGuid(), LoyaltyProgramId = programId, PointsRequired = 100, Description = "Free Coffee", AllowSelfRedemption = true, RowVersion = [1] });
        // Account is Suspended
        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount { Id = Guid.NewGuid(), BusinessId = businessId, UserId = userId, Status = LoyaltyAccountStatus.Suspended, PointsBalance = 500, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAvailableLoyaltyRewardsForBusinessHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.Single().IsSelectable.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableLoyaltyRewards_Should_MarkRewardsNotSelectable_WhenNoAccount()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram { Id = programId, BusinessId = businessId, Name = "Rewards", IsActive = true, RowVersion = [1] });
        db.Set<LoyaltyRewardTier>().Add(new LoyaltyRewardTier { Id = Guid.NewGuid(), LoyaltyProgramId = programId, PointsRequired = 100, Description = "Free Cookie", AllowSelfRedemption = true, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAvailableLoyaltyRewardsForBusinessHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.Single().IsSelectable.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableLoyaltyRewards_Should_ReturnEmptyList_WhenProgramHasNoTiers()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var programId = Guid.NewGuid();

        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram { Id = programId, BusinessId = businessId, Name = "Rewards", IsActive = true, RowVersion = [1] });
        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount { Id = Guid.NewGuid(), BusinessId = businessId, UserId = userId, Status = LoyaltyAccountStatus.Active, PointsBalance = 500, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAvailableLoyaltyRewardsForBusinessHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // ─── GetMyLoyaltyTimelinePageHandler ─────────────────────────────────────

    [Fact]
    public async Task GetMyLoyaltyTimelinePage_Should_ReturnMergedTransactionsAndRedemptions()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var tierId = Guid.NewGuid();

        db.Set<Business>().Add(new Business { Id = businessId, Name = "Test Biz" });
        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount { Id = accountId, BusinessId = businessId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] });
        db.Set<LoyaltyPointsTransaction>().Add(new LoyaltyPointsTransaction
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = accountId,
            BusinessId = businessId,
            Type = LoyaltyPointsTransactionType.Accrual,
            PointsDelta = 100,
            CreatedAtUtc = new DateTime(2030, 6, 1, 10, 0, 0, DateTimeKind.Utc),
            RowVersion = [1]
        });
        db.Set<LoyaltyRewardRedemption>().Add(new LoyaltyRewardRedemption
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = accountId,
            BusinessId = businessId,
            LoyaltyRewardTierId = tierId,
            PointsSpent = 200,
            CreatedAtUtc = new DateTime(2030, 5, 15, 8, 0, 0, DateTimeKind.Utc),
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyTimelinePageHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        var result = await handler.HandleAsync(new GetMyLoyaltyTimelinePageDto { BusinessId = businessId, PageSize = 50 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        // Newest first: June > May
        result.Value.Items[0].Kind.Should().Be(LoyaltyTimelineEntryKind.PointsTransaction);
        result.Value.Items[0].PointsDelta.Should().Be(100);
        result.Value.Items[1].Kind.Should().Be(LoyaltyTimelineEntryKind.RewardRedemption);
        result.Value.Items[1].PointsSpent.Should().Be(200);
    }

    [Fact]
    public async Task GetMyLoyaltyTimelinePage_Should_ReturnFail_WhenBusinessIdIsEmpty()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var handler = new GetMyLoyaltyTimelinePageHandler(db, new StubCurrentUserService(Guid.NewGuid()), new TestLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(new GetMyLoyaltyTimelinePageDto { BusinessId = Guid.Empty }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task GetMyLoyaltyTimelinePage_Should_ReturnFail_WhenAccountNotFound()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        var handler = new GetMyLoyaltyTimelinePageHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        var result = await handler.HandleAsync(new GetMyLoyaltyTimelinePageDto { BusinessId = businessId }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task GetMyLoyaltyTimelinePage_Should_ReturnFail_WhenUserIdIsEmpty()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var businessId = Guid.NewGuid();

        var handler = new GetMyLoyaltyTimelinePageHandler(db, new StubCurrentUserService(Guid.Empty), new TestLocalizer<ValidationResource>());
        var result = await handler.HandleAsync(new GetMyLoyaltyTimelinePageDto { BusinessId = businessId }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task GetMyLoyaltyTimelinePage_Should_ReturnFail_WhenCursorIsPartial()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount { Id = Guid.NewGuid(), BusinessId = businessId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyTimelinePageHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        // Only BeforeAtUtc provided, BeforeId is null → invalid cursor
        var result = await handler.HandleAsync(new GetMyLoyaltyTimelinePageDto
        {
            BusinessId = businessId,
            BeforeAtUtc = DateTime.UtcNow,
            BeforeId = null
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task GetMyLoyaltyTimelinePage_Should_ProvideNextCursor_WhenMoreItemsExist()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount { Id = accountId, BusinessId = businessId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] });
        for (var i = 0; i < 3; i++)
        {
            db.Set<LoyaltyPointsTransaction>().Add(new LoyaltyPointsTransaction
            {
                Id = Guid.NewGuid(),
                LoyaltyAccountId = accountId,
                BusinessId = businessId,
                Type = LoyaltyPointsTransactionType.Accrual,
                PointsDelta = 10,
                CreatedAtUtc = new DateTime(2030, 1, i + 1, 0, 0, 0, DateTimeKind.Utc),
                RowVersion = [1]
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyTimelinePageHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        var result = await handler.HandleAsync(new GetMyLoyaltyTimelinePageDto { BusinessId = businessId, PageSize = 2 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.NextBeforeAtUtc.Should().HaveValue();
        result.Value.NextBeforeId.Should().HaveValue();
    }

    [Fact]
    public async Task GetMyLoyaltyTimelinePage_Should_UseKeysetCursor_WhenProvided()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount { Id = accountId, BusinessId = businessId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] });
        var txIds = new List<Guid>();
        for (var i = 0; i < 4; i++)
        {
            var txId = Guid.NewGuid();
            txIds.Add(txId);
            db.Set<LoyaltyPointsTransaction>().Add(new LoyaltyPointsTransaction
            {
                Id = txId,
                LoyaltyAccountId = accountId,
                BusinessId = businessId,
                Type = LoyaltyPointsTransactionType.Accrual,
                PointsDelta = 10 * (i + 1),
                CreatedAtUtc = new DateTime(2030, 1, i + 1, 0, 0, 0, DateTimeKind.Utc),
                RowVersion = [1]
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyTimelinePageHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());

        // First page: get page 1 (2 newest items)
        var firstPage = await handler.HandleAsync(new GetMyLoyaltyTimelinePageDto { BusinessId = businessId, PageSize = 2 }, TestContext.Current.CancellationToken);
        firstPage.Succeeded.Should().BeTrue();
        firstPage.Value!.Items.Should().HaveCount(2);

        // Second page: use cursor from first page
        var secondPage = await handler.HandleAsync(new GetMyLoyaltyTimelinePageDto
        {
            BusinessId = businessId,
            PageSize = 2,
            BeforeAtUtc = firstPage.Value.NextBeforeAtUtc,
            BeforeId = firstPage.Value.NextBeforeId
        }, TestContext.Current.CancellationToken);

        secondPage.Succeeded.Should().BeTrue();
        secondPage.Value!.Items.Should().HaveCount(2);
        // Items on second page should be older (lower PointsDelta)
        secondPage.Value.Items.All(x => x.PointsDelta < firstPage.Value.Items.Min(y => y.PointsDelta)).Should().BeTrue();
    }

    [Fact]
    public async Task GetMyLoyaltyTimelinePage_Should_ExcludeDeletedTransactions()
    {
        await using var db = LoyaltyMobileTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount { Id = accountId, BusinessId = businessId, UserId = userId, Status = LoyaltyAccountStatus.Active, RowVersion = [1] });
        db.Set<LoyaltyPointsTransaction>().AddRange(
            new LoyaltyPointsTransaction { Id = Guid.NewGuid(), LoyaltyAccountId = accountId, BusinessId = businessId, Type = LoyaltyPointsTransactionType.Accrual, PointsDelta = 50, CreatedAtUtc = DateTime.UtcNow, RowVersion = [1] },
            new LoyaltyPointsTransaction { Id = Guid.NewGuid(), LoyaltyAccountId = accountId, BusinessId = businessId, Type = LoyaltyPointsTransactionType.Accrual, PointsDelta = 30, IsDeleted = true, CreatedAtUtc = DateTime.UtcNow, RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMyLoyaltyTimelinePageHandler(db, new StubCurrentUserService(userId), new TestLocalizer<ValidationResource>());
        var result = await handler.HandleAsync(new GetMyLoyaltyTimelinePageDto { BusinessId = businessId }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].PointsDelta.Should().Be(50);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        private readonly Guid _userId;
        public StubCurrentUserService(Guid userId) => _userId = userId;
        public Guid GetCurrentUserId() => _userId;
    }

    private sealed class TestLocalizer<TResource> : IStringLocalizer<TResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class LoyaltyMobileTestDbContext : DbContext, IAppDbContext
    {
        private LoyaltyMobileTestDbContext(DbContextOptions<LoyaltyMobileTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static LoyaltyMobileTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<LoyaltyMobileTestDbContext>()
                .UseInMemoryDatabase($"darwin_loyalty_mobile_tests_{Guid.NewGuid()}")
                .Options;
            return new LoyaltyMobileTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<Business>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.DefaultCurrency).IsRequired();
                builder.Property(x => x.DefaultCulture).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Members);
                builder.Ignore(x => x.Locations);
                builder.Ignore(x => x.Favorites);
                builder.Ignore(x => x.Likes);
                builder.Ignore(x => x.Reviews);
                builder.Ignore(x => x.EngagementStats);
                builder.Ignore(x => x.Invitations);
                builder.Ignore(x => x.StaffQrCodes);
                builder.Ignore(x => x.Subscriptions);
                builder.Ignore(x => x.AnalyticsExportJobs);
            });

            modelBuilder.Entity<BusinessLocation>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Coordinate);
            });

            modelBuilder.Entity<BusinessMedia>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Url).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<LoyaltyAccount>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.HasMany(x => x.Transactions).WithOne().HasForeignKey(x => x.LoyaltyAccountId);
                builder.Ignore(x => x.Redemptions);
            });

            modelBuilder.Entity<LoyaltyProgram>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.HasMany(x => x.RewardTiers).WithOne().HasForeignKey(x => x.LoyaltyProgramId);
            });

            modelBuilder.Entity<LoyaltyRewardTier>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<LoyaltyPointsTransaction>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<LoyaltyRewardRedemption>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
