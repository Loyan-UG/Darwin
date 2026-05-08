using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.Campaigns;
using Darwin.Domain.Entities.Marketing;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Loyalty;

/// <summary>
/// Unit tests for business campaign handlers:
/// <see cref="CreateBusinessCampaignHandler"/>, <see cref="UpdateBusinessCampaignHandler"/>,
/// <see cref="SetCampaignActivationHandler"/>, and <see cref="UpdateCampaignDeliveryStatusHandler"/>.
/// Covers backlog items from DarwinTesting.md §7 Persistence provider validation backlog:
/// - Business campaign channels validation (BusinessCampaignChannelsInvalid)
/// - Business campaign JSON validation (BusinessCampaignJsonInvalid)
/// - Business campaign schedule/eligibility validation
/// - Business campaign activation with expired/invalid campaigns
/// - Campaign delivery-status boundary coverage (null/empty row versions, invalid statuses)
/// </summary>
public sealed class BusinessCampaignHandlerTests
{
    // ─── CreateBusinessCampaignHandler ────────────────────────────────────────

    [Fact]
    public async Task CreateCampaign_Should_Fail_WhenBusinessIdIsEmpty()
    {
        await using var db = CampaignTestDbContext.Create();
        var handler = new CreateBusinessCampaignHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new CreateBusinessCampaignDto
        {
            BusinessId = Guid.Empty,
            Name = "Test",
            Title = "Test",
            Channels = 1
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessIdRequired");
    }

    [Fact]
    public async Task CreateCampaign_Should_Fail_WhenNameIsEmpty()
    {
        await using var db = CampaignTestDbContext.Create();
        var handler = new CreateBusinessCampaignHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new CreateBusinessCampaignDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "",
            Title = "Test",
            Channels = 1
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignNameAndTitleRequired");
    }

    [Fact]
    public async Task CreateCampaign_Should_Fail_WhenTitleIsEmpty()
    {
        await using var db = CampaignTestDbContext.Create();
        var handler = new CreateBusinessCampaignHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new CreateBusinessCampaignDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "My Campaign",
            Title = "   ",
            Channels = 1
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignNameAndTitleRequired");
    }

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)-1)]
    public async Task CreateCampaign_Should_Fail_WhenChannelsIsZeroOrNegative(short channels)
    {
        await using var db = CampaignTestDbContext.Create();
        var handler = new CreateBusinessCampaignHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new CreateBusinessCampaignDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Campaign",
            Title = "Campaign Title",
            Channels = channels
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignChannelsInvalid");
    }

    [Fact]
    public async Task CreateCampaign_Should_Fail_WhenChannelsHasUnknownBits()
    {
        await using var db = CampaignTestDbContext.Create();
        var handler = new CreateBusinessCampaignHandler(db, new TestLocalizer());

        // Valid mask is InApp|Push|Email|Sms|WhatsApp = 1|2|4|8|16 = 31. Use 32 (unknown bit).
        var result = await handler.HandleAsync(new CreateBusinessCampaignDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Campaign",
            Title = "Campaign Title",
            Channels = 32
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignChannelsInvalid");
    }

    [Fact]
    public async Task CreateCampaign_Should_Fail_WhenTargetingJsonIsMalformed()
    {
        await using var db = CampaignTestDbContext.Create();
        var handler = new CreateBusinessCampaignHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new CreateBusinessCampaignDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Campaign",
            Title = "Campaign Title",
            Channels = 1,
            TargetingJson = "not-valid-json"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignJsonInvalid");
    }

    [Fact]
    public async Task CreateCampaign_Should_Fail_WhenPayloadJsonIsMalformed()
    {
        await using var db = CampaignTestDbContext.Create();
        var handler = new CreateBusinessCampaignHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new CreateBusinessCampaignDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Campaign",
            Title = "Campaign Title",
            Channels = 1,
            TargetingJson = "{}",
            PayloadJson = "[1,2,3]"    // array, not object
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignJsonInvalid");
    }

    [Fact]
    public async Task CreateCampaign_Should_Fail_WhenTargetingJsonIsArray()
    {
        await using var db = CampaignTestDbContext.Create();
        var handler = new CreateBusinessCampaignHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new CreateBusinessCampaignDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Campaign",
            Title = "Campaign Title",
            Channels = 1,
            TargetingJson = "[{\"kind\":\"all\"}]"  // JSON array is not valid object JSON
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignJsonInvalid");
    }

    [Fact]
    public async Task CreateCampaign_Should_Fail_WhenStartsAtUtcIsAfterEndsAtUtc()
    {
        await using var db = CampaignTestDbContext.Create();
        var handler = new CreateBusinessCampaignHandler(db, new TestLocalizer());

        var starts = new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var ends = new DateTime(2030, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = await handler.HandleAsync(new CreateBusinessCampaignDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Campaign",
            Title = "Campaign Title",
            Channels = 1,
            StartsAtUtc = starts,
            EndsAtUtc = ends
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignScheduleInvalid");
    }

    [Fact]
    public async Task CreateCampaign_Should_Fail_WhenEligibilityRuleHasNegativeMinPoints()
    {
        await using var db = CampaignTestDbContext.Create();
        var handler = new CreateBusinessCampaignHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new CreateBusinessCampaignDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Campaign",
            Title = "Campaign Title",
            Channels = 1,
            EligibilityRules = new List<PromotionEligibilityRuleDto>
            {
                new PromotionEligibilityRuleDto { MinPoints = -10, MaxPoints = 100 }
            }
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignEligibilityRulesInvalid");
    }

    [Fact]
    public async Task CreateCampaign_Should_Fail_WhenEligibilityRuleHasMinPointsGreaterThanMaxPoints()
    {
        await using var db = CampaignTestDbContext.Create();
        var handler = new CreateBusinessCampaignHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new CreateBusinessCampaignDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Campaign",
            Title = "Campaign Title",
            Channels = 1,
            EligibilityRules = new List<PromotionEligibilityRuleDto>
            {
                new PromotionEligibilityRuleDto { MinPoints = 500, MaxPoints = 100 }
            }
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignEligibilityRulesInvalid");
    }

    [Fact]
    public async Task CreateCampaign_Should_Succeed_WithValidMinimalDto()
    {
        await using var db = CampaignTestDbContext.Create();
        var handler = new CreateBusinessCampaignHandler(db, new TestLocalizer());
        var businessId = Guid.NewGuid();

        var result = await handler.HandleAsync(new CreateBusinessCampaignDto
        {
            BusinessId = businessId,
            Name = "Summer Promo",
            Title = "Summer Promotion",
            Channels = (short)(CampaignChannels.InApp | CampaignChannels.Push)
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var persisted = await db.Set<Campaign>().SingleAsync(x => x.Id == result.Value, TestContext.Current.CancellationToken);
        persisted.BusinessId.Should().Be(businessId);
        persisted.Name.Should().Be("Summer Promo");
        persisted.Title.Should().Be("Summer Promotion");
        persisted.IsActive.Should().BeFalse("new campaigns start as inactive/draft");
    }

    [Fact]
    public async Task CreateCampaign_Should_Succeed_WithValidScheduleAndEligibilityRules()
    {
        await using var db = CampaignTestDbContext.Create();
        var handler = new CreateBusinessCampaignHandler(db, new TestLocalizer());
        var businessId = Guid.NewGuid();

        var starts = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var ends = new DateTime(2030, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        var result = await handler.HandleAsync(new CreateBusinessCampaignDto
        {
            BusinessId = businessId,
            Name = "Year Campaign",
            Title = "Year Campaign Title",
            Channels = 1,
            StartsAtUtc = starts,
            EndsAtUtc = ends,
            EligibilityRules = new List<PromotionEligibilityRuleDto>
            {
                new PromotionEligibilityRuleDto { MinPoints = 100, MaxPoints = 500 }
            }
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
    }

    // ─── UpdateBusinessCampaignHandler ────────────────────────────────────────

    [Fact]
    public async Task UpdateCampaign_Should_Fail_WhenBusinessIdIsEmpty()
    {
        await using var db = CampaignTestDbContext.Create();
        var handler = new UpdateBusinessCampaignHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new UpdateBusinessCampaignDto
        {
            BusinessId = Guid.Empty,
            Id = Guid.NewGuid(),
            Name = "Test",
            Title = "Test",
            Channels = 1,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignBusinessAndCampaignRequired");
    }

    [Fact]
    public async Task UpdateCampaign_Should_Fail_WhenCampaignIdIsEmpty()
    {
        await using var db = CampaignTestDbContext.Create();
        var handler = new UpdateBusinessCampaignHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new UpdateBusinessCampaignDto
        {
            BusinessId = Guid.NewGuid(),
            Id = Guid.Empty,
            Name = "Test",
            Title = "Test",
            Channels = 1,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignBusinessAndCampaignRequired");
    }

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)64)]
    public async Task UpdateCampaign_Should_Fail_WhenChannelsIsInvalid(short channels)
    {
        await using var db = CampaignTestDbContext.Create();
        var handler = new UpdateBusinessCampaignHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new UpdateBusinessCampaignDto
        {
            BusinessId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Name = "Test",
            Title = "Test",
            Channels = channels,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignChannelsInvalid");
    }

    [Fact]
    public async Task UpdateCampaign_Should_Fail_WhenPayloadJsonIsMalformed()
    {
        await using var db = CampaignTestDbContext.Create();
        var handler = new UpdateBusinessCampaignHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new UpdateBusinessCampaignDto
        {
            BusinessId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Name = "Test",
            Title = "Test",
            Channels = 1,
            PayloadJson = "{broken",
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignJsonInvalid");
    }

    [Fact]
    public async Task UpdateCampaign_Should_Fail_WhenScheduleIsInvalid()
    {
        await using var db = CampaignTestDbContext.Create();
        var handler = new UpdateBusinessCampaignHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new UpdateBusinessCampaignDto
        {
            BusinessId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Name = "Test",
            Title = "Test",
            Channels = 1,
            StartsAtUtc = new DateTime(2030, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            EndsAtUtc = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignScheduleInvalid");
    }

    [Fact]
    public async Task UpdateCampaign_Should_Fail_WhenNotFound()
    {
        await using var db = CampaignTestDbContext.Create();
        var handler = new UpdateBusinessCampaignHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new UpdateBusinessCampaignDto
        {
            BusinessId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            Name = "Test",
            Title = "Test",
            Channels = 1,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignNotFound");
    }

    [Fact]
    public async Task UpdateCampaign_Should_Fail_WhenRowVersionIsEmpty()
    {
        await using var db = CampaignTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var campaign = CreateCampaign(businessId);
        db.Set<Campaign>().Add(campaign);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessCampaignHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new UpdateBusinessCampaignDto
        {
            BusinessId = businessId,
            Id = campaign.Id,
            Name = "Updated",
            Title = "Updated Title",
            Channels = 1,
            RowVersion = Array.Empty<byte>()
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignConcurrencyConflict");
    }

    [Fact]
    public async Task UpdateCampaign_Should_Succeed_WithMatchingRowVersion()
    {
        await using var db = CampaignTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var campaign = CreateCampaign(businessId, rowVersion: [1, 2, 3]);
        db.Set<Campaign>().Add(campaign);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessCampaignHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new UpdateBusinessCampaignDto
        {
            BusinessId = businessId,
            Id = campaign.Id,
            Name = "Updated Name",
            Title = "Updated Title",
            Channels = (short)CampaignChannels.Email,
            RowVersion = [1, 2, 3]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
    }

    // ─── SetCampaignActivationHandler ─────────────────────────────────────────

    [Fact]
    public async Task ActivateCampaign_Should_Fail_WhenBusinessIdIsEmpty()
    {
        await using var db = CampaignTestDbContext.Create();
        var clock = new FakeClock(new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new SetCampaignActivationHandler(db, clock, new TestLocalizer());

        var result = await handler.HandleAsync(new SetCampaignActivationDto
        {
            BusinessId = Guid.Empty,
            Id = Guid.NewGuid(),
            IsActive = true,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignBusinessAndCampaignRequired");
    }

    [Fact]
    public async Task ActivateCampaign_Should_Fail_WhenNotFound()
    {
        await using var db = CampaignTestDbContext.Create();
        var clock = new FakeClock(new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new SetCampaignActivationHandler(db, clock, new TestLocalizer());

        var result = await handler.HandleAsync(new SetCampaignActivationDto
        {
            BusinessId = Guid.NewGuid(),
            Id = Guid.NewGuid(),
            IsActive = true,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignNotFound");
    }

    [Fact]
    public async Task ActivateCampaign_Should_Fail_WhenRowVersionIsEmpty()
    {
        await using var db = CampaignTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var campaign = CreateCampaign(businessId);
        db.Set<Campaign>().Add(campaign);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var clock = new FakeClock(new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new SetCampaignActivationHandler(db, clock, new TestLocalizer());

        var result = await handler.HandleAsync(new SetCampaignActivationDto
        {
            BusinessId = businessId,
            Id = campaign.Id,
            IsActive = true,
            RowVersion = Array.Empty<byte>()
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignConcurrencyConflict");
    }

    [Fact]
    public async Task ActivateCampaign_Should_Fail_WhenChannelsIsInvalid_OnActivation()
    {
        await using var db = CampaignTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var campaign = CreateCampaign(businessId, rowVersion: [1], channels: CampaignChannels.None);
        db.Set<Campaign>().Add(campaign);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var clock = new FakeClock(new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new SetCampaignActivationHandler(db, clock, new TestLocalizer());

        var result = await handler.HandleAsync(new SetCampaignActivationDto
        {
            BusinessId = businessId,
            Id = campaign.Id,
            IsActive = true,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignChannelsInvalid");
    }

    [Fact]
    public async Task ActivateCampaign_Should_Fail_WhenPayloadJsonIsMalformed_OnActivation()
    {
        await using var db = CampaignTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var campaign = CreateCampaign(businessId, rowVersion: [1], payloadJson: "[not-object]");
        db.Set<Campaign>().Add(campaign);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var clock = new FakeClock(new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new SetCampaignActivationHandler(db, clock, new TestLocalizer());

        var result = await handler.HandleAsync(new SetCampaignActivationDto
        {
            BusinessId = businessId,
            Id = campaign.Id,
            IsActive = true,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignJsonInvalid");
    }

    [Fact]
    public async Task ActivateCampaign_Should_Fail_WhenScheduleIsInvalid_OnActivation()
    {
        await using var db = CampaignTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var campaign = CreateCampaign(
            businessId,
            rowVersion: [1],
            startsAtUtc: new DateTime(2030, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            endsAtUtc: new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        db.Set<Campaign>().Add(campaign);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var clock = new FakeClock(new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new SetCampaignActivationHandler(db, clock, new TestLocalizer());

        var result = await handler.HandleAsync(new SetCampaignActivationDto
        {
            BusinessId = businessId,
            Id = campaign.Id,
            IsActive = true,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignScheduleInvalid");
    }

    [Fact]
    public async Task ActivateCampaign_Should_Fail_WhenCampaignIsExpired_OnActivation()
    {
        await using var db = CampaignTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var pastEnd = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var campaign = CreateCampaign(businessId, rowVersion: [1], endsAtUtc: pastEnd);
        db.Set<Campaign>().Add(campaign);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Clock is set to after the campaign ends (future)
        var clock = new FakeClock(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new SetCampaignActivationHandler(db, clock, new TestLocalizer());

        var result = await handler.HandleAsync(new SetCampaignActivationDto
        {
            BusinessId = businessId,
            Id = campaign.Id,
            IsActive = true,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessCampaignCannotActivateExpired");
    }

    [Fact]
    public async Task ActivateCampaign_Should_Succeed_WithValidActiveCampaign()
    {
        await using var db = CampaignTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var campaign = CreateCampaign(businessId, rowVersion: [1]);
        db.Set<Campaign>().Add(campaign);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var clock = new FakeClock(new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new SetCampaignActivationHandler(db, clock, new TestLocalizer());

        var result = await handler.HandleAsync(new SetCampaignActivationDto
        {
            BusinessId = businessId,
            Id = campaign.Id,
            IsActive = true,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateCampaign_Should_Succeed_WithoutValidationChecks()
    {
        // Deactivation (IsActive=false) should skip channel/json/schedule/expiry checks
        await using var db = CampaignTestDbContext.Create();
        var businessId = Guid.NewGuid();
        // Use an invalid channel mask - deactivation should still succeed
        var campaign = CreateCampaign(businessId, rowVersion: [1], channels: CampaignChannels.None);
        campaign.IsActive = true;
        db.Set<Campaign>().Add(campaign);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var clock = new FakeClock(new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new SetCampaignActivationHandler(db, clock, new TestLocalizer());

        var result = await handler.HandleAsync(new SetCampaignActivationDto
        {
            BusinessId = businessId,
            Id = campaign.Id,
            IsActive = false,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
    }

    // ─── UpdateCampaignDeliveryStatusHandler ──────────────────────────────────

    [Fact]
    public async Task UpdateDeliveryStatus_Should_Fail_WhenIdIsEmpty()
    {
        await using var db = CampaignTestDbContext.Create();
        var clock = new FakeClock(new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new UpdateCampaignDeliveryStatusHandler(db, clock, new TestLocalizer());

        var result = await handler.HandleAsync(new UpdateCampaignDeliveryStatusDto
        {
            Id = Guid.Empty,
            Status = (short)CampaignDeliveryStatus.Succeeded,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvalidDeleteRequest");
    }

    [Fact]
    public async Task UpdateDeliveryStatus_Should_Fail_WhenRowVersionIsEmpty()
    {
        await using var db = CampaignTestDbContext.Create();
        var clock = new FakeClock(new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new UpdateCampaignDeliveryStatusHandler(db, clock, new TestLocalizer());

        var result = await handler.HandleAsync(new UpdateCampaignDeliveryStatusDto
        {
            Id = Guid.NewGuid(),
            Status = (short)CampaignDeliveryStatus.Succeeded,
            RowVersion = Array.Empty<byte>()
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RowVersionRequired");
    }

    [Theory]
    [InlineData((short)99)]
    [InlineData((short)-1)]
    [InlineData((short)100)]
    public async Task UpdateDeliveryStatus_Should_Fail_WhenStatusIsInvalidEnumValue(short status)
    {
        await using var db = CampaignTestDbContext.Create();
        var clock = new FakeClock(new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new UpdateCampaignDeliveryStatusHandler(db, clock, new TestLocalizer());

        var result = await handler.HandleAsync(new UpdateCampaignDeliveryStatusDto
        {
            Id = Guid.NewGuid(),
            Status = status,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("CampaignDeliveryStatusInvalid");
    }

    [Fact]
    public async Task UpdateDeliveryStatus_Should_Fail_WhenDeliveryNotFound()
    {
        await using var db = CampaignTestDbContext.Create();
        var clock = new FakeClock(new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new UpdateCampaignDeliveryStatusHandler(db, clock, new TestLocalizer());

        var result = await handler.HandleAsync(new UpdateCampaignDeliveryStatusDto
        {
            Id = Guid.NewGuid(),
            Status = (short)CampaignDeliveryStatus.Succeeded,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("CampaignDeliveryNotFound");
    }

    [Theory]
    [InlineData((short)CampaignDeliveryStatus.Pending)]
    [InlineData((short)CampaignDeliveryStatus.Succeeded)]
    [InlineData((short)CampaignDeliveryStatus.Failed)]
    [InlineData((short)CampaignDeliveryStatus.Cancelled)]
    public async Task UpdateDeliveryStatus_Should_Succeed_WithValidStatus(short status)
    {
        await using var db = CampaignTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var delivery = CreateDelivery(businessId, rowVersion: [5, 6, 7]);
        db.Set<CampaignDelivery>().Add(delivery);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var clock = new FakeClock(new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new UpdateCampaignDeliveryStatusHandler(db, clock, new TestLocalizer());

        var result = await handler.HandleAsync(new UpdateCampaignDeliveryStatusDto
        {
            Id = delivery.Id,
            BusinessId = businessId,
            Status = status,
            RowVersion = [5, 6, 7]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateDeliveryStatus_Should_ClearErrorFields_WhenRequeueingToPending()
    {
        await using var db = CampaignTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var delivery = CreateDelivery(businessId, rowVersion: [1]);
        delivery.LastError = "previous error";
        delivery.LastResponseCode = 500;
        db.Set<CampaignDelivery>().Add(delivery);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var clock = new FakeClock(new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new UpdateCampaignDeliveryStatusHandler(db, clock, new TestLocalizer());

        var result = await handler.HandleAsync(new UpdateCampaignDeliveryStatusDto
        {
            Id = delivery.Id,
            BusinessId = businessId,
            Status = (short)CampaignDeliveryStatus.Pending,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var updated = await db.Set<CampaignDelivery>().SingleAsync(x => x.Id == delivery.Id, TestContext.Current.CancellationToken);
        updated.LastError.Should().BeNull("pending requeue clears error fields");
        updated.LastResponseCode.Should().BeNull("pending requeue clears response code");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static Campaign CreateCampaign(
        Guid businessId,
        byte[]? rowVersion = null,
        CampaignChannels channels = CampaignChannels.InApp,
        string? payloadJson = null,
        DateTime? startsAtUtc = null,
        DateTime? endsAtUtc = null)
    {
        return new Campaign
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = "Test Campaign",
            Title = "Test Campaign Title",
            Channels = channels,
            IsActive = false,
            TargetingJson = "{}",
            PayloadJson = payloadJson ?? "{}",
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc,
            RowVersion = rowVersion ?? [1, 2, 3]
        };
    }

    private static CampaignDelivery CreateDelivery(Guid businessId, byte[]? rowVersion = null)
    {
        return new CampaignDelivery
        {
            Id = Guid.NewGuid(),
            CampaignId = Guid.NewGuid(),
            BusinessId = businessId,
            Channel = CampaignDeliveryChannel.Push,
            Status = CampaignDeliveryStatus.Pending,
            RowVersion = rowVersion ?? [1]
        };
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class CampaignTestDbContext : DbContext, IAppDbContext
    {
        private CampaignTestDbContext(DbContextOptions<CampaignTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static CampaignTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<CampaignTestDbContext>()
                .UseInMemoryDatabase($"darwin_campaign_tests_{Guid.NewGuid()}")
                .Options;
            return new CampaignTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Campaign>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.Title).IsRequired();
                builder.Property(x => x.TargetingJson).IsRequired();
                builder.Property(x => x.PayloadJson).IsRequired();
                builder.Property(x => x.IsDeleted);
                builder.Property(x => x.RowVersion).IsRowVersion();
            });

            modelBuilder.Entity<CampaignDelivery>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.CampaignId).IsRequired();
                builder.Property(x => x.IsDeleted);
                builder.Property(x => x.RowVersion).IsRowVersion();
            });
        }
    }
}
