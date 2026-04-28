using Darwin.Application.Common.DTOs;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Enums;
using Darwin.WebApi.Mappers;
using FluentAssertions;

namespace Darwin.WebApi.Tests.Mappers;

/// <summary>
///     Verifies projection behavior of <see cref="LoyaltyContractsMapper"/> to keep
///     mobile-critical loyalty payloads contract-compatible and deterministic.
/// </summary>
public sealed class LoyaltyContractsMapperTests
{
    /// <summary>
    ///     Ensures account summary mapping keeps identifiers, status token conversion,
    ///     numeric balances, and non-null business-name normalization.
    /// </summary>
    [Fact]
    public void ToContract_AccountSummary_Should_NormalizeBusinessNameAndStatus()
    {
        // Arrange
        var dto = new LoyaltyAccountSummaryDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            BusinessName = null,
            Status = LoyaltyAccountStatus.Suspended,
            PointsBalance = 42,
            LifetimePoints = 320,
            LastAccrualAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var contract = LoyaltyContractsMapper.ToContract(dto);

        // Assert
        contract.LoyaltyAccountId.Should().Be(dto.Id);
        contract.BusinessId.Should().Be(dto.BusinessId);
        contract.BusinessName.Should().BeEmpty();
        contract.Status.Should().Be(LoyaltyAccountStatus.Suspended.ToString());
        contract.PointsBalance.Should().Be(42);
        contract.LifetimePoints.Should().Be(320);
        contract.LastAccrualAtUtc.Should().Be(dto.LastAccrualAtUtc);
    }

    /// <summary>
    ///     Ensures timeline entry mapping preserves all value fields and maps
    ///     application enum kinds to the corresponding contract enum kinds.
    /// </summary>
    [Fact]
    public void ToContract_TimelineEntry_Should_MapAllFieldsAndKind()
    {
        // Arrange
        var dto = new LoyaltyTimelineEntryDto
        {
            Id = Guid.NewGuid(),
            Kind = LoyaltyTimelineEntryKind.RewardRedemption,
            LoyaltyAccountId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            OccurredAtUtc = DateTime.UtcNow,
            PointsDelta = -120,
            PointsSpent = 120,
            RewardTierId = Guid.NewGuid(),
            Reference = "rcpt-001",
            Note = "Redeemed at checkout"
        };

        // Act
        var contract = LoyaltyContractsMapper.ToContract(dto);

        // Assert
        contract.Id.Should().Be(dto.Id);
        contract.Kind.Should().Be(Darwin.Contracts.Loyalty.LoyaltyTimelineEntryKind.RewardRedemption);
        contract.LoyaltyAccountId.Should().Be(dto.LoyaltyAccountId);
        contract.BusinessId.Should().Be(dto.BusinessId);
        contract.OccurredAtUtc.Should().Be(dto.OccurredAtUtc);
        contract.PointsDelta.Should().Be(-120);
        contract.PointsSpent.Should().Be(120);
        contract.RewardTierId.Should().Be(dto.RewardTierId);
        contract.Reference.Should().Be("rcpt-001");
        contract.Note.Should().Be("Redeemed at checkout");
    }

    /// <summary>
    ///     Ensures geo-coordinate conversion back to Application DTO preserves all
    ///     spatial fields, including optional altitude, for reverse mapping use-cases.
    /// </summary>
    [Fact]
    public void ToApplication_GeoCoordinate_Should_PreserveLatitudeLongitudeAndAltitude()
    {
        // Arrange
        var model = new Darwin.Contracts.Common.GeoCoordinateModel
        {
            Latitude = 50.1109,
            Longitude = 8.6821,
            AltitudeMeters = 112.3
        };

        // Act
        var dto = LoyaltyContractsMapper.ToApplication(model);

        // Assert
        dto.Should().BeEquivalentTo(new GeoCoordinateDto
        {
            Latitude = 50.1109,
            Longitude = 8.6821,
            AltitudeMeters = 112.3
        });
    }

    /// <summary>
    ///     Ensures business scan-session account summary mapping keeps points and
    ///     customer display alias fields required by the Business app scanner flow.
    /// </summary>
    [Fact]
    public void ToContractBusinessAccountSummary_Should_MapPointsAndDisplayName()
    {
        // Arrange
        var dto = new ScanSessionBusinessViewDto
        {
            LoyaltyAccountId = Guid.NewGuid(),
            CurrentPointsBalance = 155,
            CustomerDisplayName = "Customer #A12"
        };

        // Act
        var contract = LoyaltyContractsMapper.ToContractBusinessAccountSummary(dto);

        // Assert
        contract.LoyaltyAccountId.Should().Be(dto.LoyaltyAccountId);
        contract.PointsBalance.Should().Be(155);
        contract.CustomerDisplayName.Should().Be("Customer #A12");
    }

    /// <summary>
    ///     Ensures reward summary mapping preserves identifiers and selection flags
    ///     that drive redemption eligibility in mobile clients.
    /// </summary>
    [Fact]
    public void ToContract_RewardSummary_Should_MapIdentifiersAndFlags()
    {
        // Arrange
        var dto = new LoyaltyRewardSummaryDto
        {
            LoyaltyRewardTierId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            Name = "Free Espresso",
            Description = "Single shot",
            RequiredPoints = 90,
            IsActive = true,
            IsSelectable = false
        };

        // Act
        var contract = LoyaltyContractsMapper.ToContract(dto);

        // Assert
        contract.LoyaltyRewardTierId.Should().Be(dto.LoyaltyRewardTierId);
        contract.BusinessId.Should().Be(dto.BusinessId);
        contract.Name.Should().Be("Free Espresso");
        contract.RequiredPoints.Should().Be(90);
        contract.IsActive.Should().BeTrue();
        contract.IsSelectable.Should().BeFalse();
    }

    /// <summary>
    ///     Ensures optional reward title is normalized to an empty string when absent,
    ///     while optional description values remain as provided.
    /// </summary>
    [Fact]
    public void ToContract_RewardSummary_Should_DefaultNameWhenMissing()
    {
        // Arrange
        var dto = new LoyaltyRewardSummaryDto
        {
            LoyaltyRewardTierId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            Name = null,
            Description = null,
            RequiredPoints = 50,
            IsActive = true,
            IsSelectable = true
        };

        // Act
        var contract = LoyaltyContractsMapper.ToContract(dto);

        // Assert
        contract.Name.Should().BeEmpty();
        contract.Description.Should().BeNull();
    }

    /// <summary>
    ///     Ensures timeline entry mapping handles both supported kinds.
    /// </summary>
    [Fact]
    public void ToContract_TimelineEntry_Should_MapPointsTransactionKind()
    {
        // Arrange
        var dto = new LoyaltyTimelineEntryDto
        {
            Id = Guid.NewGuid(),
            Kind = LoyaltyTimelineEntryKind.PointsTransaction,
            LoyaltyAccountId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            OccurredAtUtc = DateTime.UtcNow,
            PointsDelta = 30,
            PointsSpent = 0
        };

        // Act
        var contract = LoyaltyContractsMapper.ToContract(dto);

        // Assert
        contract.Kind.Should().Be(Darwin.Contracts.Loyalty.LoyaltyTimelineEntryKind.PointsTransaction);
    }

    /// <summary>
    ///     Ensures account summary includes optional progress indicators when present.
    /// </summary>
    [Fact]
    public void ToContract_AccountSummary_Should_MapRewardProgressIndicators()
    {
        // Arrange
        var dto = new LoyaltyAccountSummaryDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            BusinessName = "Rewards Lounge",
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = 250,
            LifetimePoints = 1500,
            LastAccrualAtUtc = DateTime.UtcNow.AddMinutes(-45),
            NextRewardTitle = "Free Pastry",
            NextRewardRequiredPoints = 300,
            PointsToNextReward = 50,
            NextRewardProgressPercent = 83.33m
        };

        // Act
        var contract = LoyaltyContractsMapper.ToContract(dto);

        // Assert
        contract.NextRewardTitle.Should().Be("Free Pastry");
        contract.NextRewardRequiredPoints.Should().Be(300);
        contract.PointsToNextReward.Should().Be(50);
        contract.NextRewardProgressPercent.Should().Be(83.33m);
    }

    /// <summary>
    ///     Ensures unknown timeline kinds safely fall back to points-transaction kind.
    /// </summary>
    [Fact]
    public void ToContract_TimelineEntry_Should_FallbackToPointsTransaction_ForUnknownKind()
    {
        // Arrange
        var dto = new LoyaltyTimelineEntryDto
        {
            Id = Guid.NewGuid(),
            Kind = (LoyaltyTimelineEntryKind)99,
            LoyaltyAccountId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            OccurredAtUtc = DateTime.UtcNow,
            PointsDelta = -5,
            PointsSpent = 5
        };

        // Act
        var contract = LoyaltyContractsMapper.ToContract(dto);

        // Assert
        contract.Kind.Should().Be(Darwin.Contracts.Loyalty.LoyaltyTimelineEntryKind.PointsTransaction);
    }


    /// <summary>
    ///     Ensures scan-mode conversion is symmetric for known values and uses
    ///     deterministic accrual fallback for unknown enum inputs.
    /// </summary>
    [Fact]
    public void ScanModeMapping_Should_BeSymmetric_AndFallbackToAccrualForUnknownValues()
    {
        // Arrange
        const Darwin.Contracts.Loyalty.LoyaltyScanMode contractAccrual = Darwin.Contracts.Loyalty.LoyaltyScanMode.Accrual;
        const Darwin.Contracts.Loyalty.LoyaltyScanMode contractRedemption = Darwin.Contracts.Loyalty.LoyaltyScanMode.Redemption;

        // Act
        var domainAccrual = LoyaltyContractsMapper.ToDomain(contractAccrual);
        var domainRedemption = LoyaltyContractsMapper.ToDomain(contractRedemption);
        var contractFromDomainAccrual = LoyaltyContractsMapper.ToContract(domainAccrual);
        var contractFromDomainRedemption = LoyaltyContractsMapper.ToContract(domainRedemption);
        var fallbackFromUnknownContract = LoyaltyContractsMapper.ToDomain((Darwin.Contracts.Loyalty.LoyaltyScanMode)99);
        var fallbackFromUnknownDomain = LoyaltyContractsMapper.ToContract((Darwin.Domain.Enums.LoyaltyScanMode)99);

        // Assert
        domainAccrual.Should().Be(Darwin.Domain.Enums.LoyaltyScanMode.Accrual);
        domainRedemption.Should().Be(Darwin.Domain.Enums.LoyaltyScanMode.Redemption);
        contractFromDomainAccrual.Should().Be(Darwin.Contracts.Loyalty.LoyaltyScanMode.Accrual);
        contractFromDomainRedemption.Should().Be(Darwin.Contracts.Loyalty.LoyaltyScanMode.Redemption);
        fallbackFromUnknownContract.Should().Be(Darwin.Domain.Enums.LoyaltyScanMode.Accrual);
        fallbackFromUnknownDomain.Should().Be(Darwin.Contracts.Loyalty.LoyaltyScanMode.Accrual);
    }

    /// <summary>
    ///     Ensures loyalty points transaction mapping keeps date/type/notes values
    ///     stable for mobile history and ledger rendering.
    /// </summary>
    [Fact]
    public void ToContract_PointsTransaction_Should_MapLedgerFields()
    {
        // Arrange
        var occurredAt = DateTime.UtcNow.AddHours(-2);
        var dto = new LoyaltyPointsTransactionDto
        {
            CreatedAtUtc = occurredAt,
            Type = Darwin.Domain.Enums.LoyaltyPointsTransactionType.Accrual,
            PointsDelta = 15,
            Reference = "txn-501",
            Notes = "In-store purchase"
        };

        // Act
        var contract = LoyaltyContractsMapper.ToContract(dto);

        // Assert
        contract.OccurredAtUtc.Should().Be(occurredAt);
        contract.Type.Should().Be(Darwin.Domain.Enums.LoyaltyPointsTransactionType.Accrual.ToString());
        contract.Delta.Should().Be(15);
        contract.Reference.Should().Be("txn-501");
        contract.Notes.Should().Be("In-store purchase");
    }

    /// <summary>
    ///     Ensures loyalty overview projection maps aggregate counters and propagates
    ///     nested account summaries with stable contract naming.
    /// </summary>
    [Fact]
    public void ToContract_MyLoyaltyOverview_Should_MapAggregateCountersAndNestedAccounts()
    {
        // Arrange
        var dto = new MyLoyaltyOverviewDto
        {
            TotalAccounts = 3,
            ActiveAccounts = 2,
            TotalPointsBalance = 410,
            TotalLifetimePoints = 1200,
            LastAccrualAtUtc = DateTime.UtcNow.AddMinutes(-5),
            Accounts =
            [
                new LoyaltyAccountSummaryDto
                {
                    Id = Guid.NewGuid(),
                    BusinessId = Guid.NewGuid(),
                    BusinessName = "Star Bakery",
                    Status = LoyaltyAccountStatus.Active,
                    PointsBalance = 150,
                    LifetimePoints = 500,
                    LastAccrualAtUtc = DateTime.UtcNow.AddDays(-1)
                },
                new LoyaltyAccountSummaryDto
                {
                    Id = Guid.NewGuid(),
                    BusinessId = Guid.NewGuid(),
                    BusinessName = null!,
                    Status = LoyaltyAccountStatus.Inactive,
                    PointsBalance = 0,
                    LifetimePoints = 700,
                    LastAccrualAtUtc = null
                }
            ]
        };

        // Act
        var contract = LoyaltyContractsMapper.ToContract(dto);

        // Assert
        contract.TotalAccounts.Should().Be(3);
        contract.ActiveAccounts.Should().Be(2);
        contract.TotalPointsBalance.Should().Be(410);
        contract.TotalLifetimePoints.Should().Be(1200);
        contract.LastAccrualAtUtc.Should().Be(dto.LastAccrualAtUtc);
        contract.Accounts.Should().HaveCount(2);
        contract.Accounts[0].BusinessName.Should().Be("Star Bakery");
        contract.Accounts[0].LoyaltyAccountId.Should().Be(dto.Accounts[0].Id);
        contract.Accounts[0].Status.Should().Be(LoyaltyAccountStatus.Active.ToString());
        contract.Accounts[1].BusinessName.Should().BeEmpty();
        contract.Accounts[1].Status.Should().Be(LoyaltyAccountStatus.Inactive.ToString());
    }

    /// <summary>
    ///     Ensures dashboard projection supports nullable next-reward paths while
    ///     still mapping account/transactions/expiry metadata.
    /// </summary>
    [Fact]
    public void ToContract_MyLoyaltyBusinessDashboard_Should_MapDashboardFields_WhenNextRewardIsNull()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var dto = new MyLoyaltyBusinessDashboardDto
        {
            Account = new LoyaltyAccountSummaryDto
            {
                Id = accountId,
                BusinessId = Guid.NewGuid(),
                BusinessName = "Coffee Club",
                Status = LoyaltyAccountStatus.Active,
                PointsBalance = 320,
                LifetimePoints = 950,
                LastAccrualAtUtc = DateTime.UtcNow.AddHours(-3)
            },
            AvailableRewardsCount = 4,
            RedeemableRewardsCount = 2,
            NextReward = null,
            RecentTransactions =
            [
                new LoyaltyPointsTransactionDto
                {
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
                    Type = LoyaltyPointsTransactionType.Redemption,
                    PointsDelta = -40,
                    Reference = "txn-null-reward",
                    Notes = "Redemption flow"
                },
                new LoyaltyPointsTransactionDto
                {
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                    Type = LoyaltyPointsTransactionType.Accrual,
                    PointsDelta = 60,
                    Reference = "txn-accrual",
                    Notes = "Purchase bonus"
                }
            ],
            PointsToNextReward = null,
            NextRewardRequiredPoints = null,
            NextRewardProgressPercent = null,
            ExpiryTrackingEnabled = false,
            PointsExpiringSoon = 15,
            NextPointsExpiryAtUtc = DateTime.UtcNow.AddDays(20)
        };

        // Act
        var contract = LoyaltyContractsMapper.ToContract(dto);

        // Assert
        contract.Account.LoyaltyAccountId.Should().Be(accountId);
        contract.Account.BusinessName.Should().Be("Coffee Club");
        contract.AvailableRewardsCount.Should().Be(4);
        contract.RedeemableRewardsCount.Should().Be(2);
        contract.NextReward.Should().BeNull();
        contract.RecentTransactions.Should().HaveCount(2);
        contract.RecentTransactions[0].Type.Should().Be(LoyaltyPointsTransactionType.Redemption.ToString());
        contract.PointsToNextReward.Should().BeNull();
        contract.NextRewardRequiredPoints.Should().BeNull();
        contract.NextRewardProgressPercent.Should().BeNull();
        contract.ExpiryTrackingEnabled.Should().BeFalse();
        contract.PointsExpiringSoon.Should().Be(15);
        contract.NextPointsExpiryAtUtc.Should().NotBeNull();
    }

    /// <summary>
    ///     Ensures dashboard projection keeps nested next-reward and reward summary details
    ///     when next reward is present.
    /// </summary>
    [Fact]
    public void ToContract_MyLoyaltyBusinessDashboard_Should_MapNestedNextRewardWhenProvided()
    {
        // Arrange
        var dto = new MyLoyaltyBusinessDashboardDto
        {
            Account = new LoyaltyAccountSummaryDto
            {
                Id = Guid.NewGuid(),
                BusinessId = Guid.NewGuid(),
                BusinessName = "Tea House",
                Status = LoyaltyAccountStatus.Active,
                PointsBalance = 420,
                LifetimePoints = 1200,
                LastAccrualAtUtc = null
            },
            AvailableRewardsCount = 6,
            RedeemableRewardsCount = 3,
            NextReward = new LoyaltyRewardSummaryDto
            {
                LoyaltyRewardTierId = Guid.NewGuid(),
                BusinessId = Guid.NewGuid(),
                Name = "Free Muffin",
                Description = "Redeem for one free muffin",
                RequiredPoints = 400,
                IsActive = true,
                IsSelectable = true
            },
            RecentTransactions = [],
            PointsToNextReward = 45,
            NextRewardRequiredPoints = 400,
            NextRewardProgressPercent = 80.5m,
            ExpiryTrackingEnabled = true,
            PointsExpiringSoon = 10,
            NextPointsExpiryAtUtc = DateTime.UtcNow.AddDays(12)
        };

        // Act
        var contract = LoyaltyContractsMapper.ToContract(dto);

        // Assert
        contract.NextReward.Should().NotBeNull();
        contract.NextReward!.Name.Should().Be("Free Muffin");
        contract.NextReward.RequiredPoints.Should().Be(400);
        contract.NextReward.IsActive.Should().BeTrue();
        contract.PointsToNextReward.Should().Be(45);
        contract.NextRewardRequiredPoints.Should().Be(400);
        contract.NextRewardProgressPercent.Should().Be(80.5m);
        contract.ExpiryTrackingEnabled.Should().BeTrue();
    }

    /// <summary>
    ///     Ensures dashboard mapping enforces required nested account payloads.
    /// </summary>
    [Fact]
    public void ToContract_MyLoyaltyBusinessDashboard_Should_Throw_WhenAccountIsNull()
    {
        // Arrange
        var dto = new MyLoyaltyBusinessDashboardDto
        {
            Account = null!,
            AvailableRewardsCount = 0,
            RedeemableRewardsCount = 0,
            NextReward = null,
            RecentTransactions = [],
            ExpiryTrackingEnabled = false
        };

        // Act
        Action act = () => LoyaltyContractsMapper.ToContract(dto);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    ///     Ensures null DTOs fail fast in all new and existing mapping entry points.
    /// </summary>
    [Fact]
    public void ToContract_Should_ThrowArgumentNullException_ForNullDtos()
    {
        // Act
        Action accountSummary = () => LoyaltyContractsMapper.ToContract((LoyaltyAccountSummaryDto)null!);
        Action account = () => LoyaltyContractsMapper.ToContract((MyLoyaltyOverviewDto)null!);
        Action dashboard = () => LoyaltyContractsMapper.ToContract((MyLoyaltyBusinessDashboardDto)null!);
        Action rewardSummary = () => LoyaltyContractsMapper.ToContract((LoyaltyRewardSummaryDto)null!);
        Action businessSummary = () => LoyaltyContractsMapper.ToContract((MyLoyaltyBusinessListItemDto)null!);
        Action businessAccount = () => LoyaltyContractsMapper.ToContractBusinessAccountSummary(null!);
        Action pointsTransaction = () => LoyaltyContractsMapper.ToContract((LoyaltyPointsTransactionDto)null!);
        Action timelineEntry = () => LoyaltyContractsMapper.ToContract((LoyaltyTimelineEntryDto)null!);
        Action location = () => LoyaltyContractsMapper.ToApplication((Darwin.Contracts.Common.GeoCoordinateModel)null!);

        // Assert
        accountSummary.Should().Throw<ArgumentNullException>();
        account.Should().Throw<ArgumentNullException>();
        dashboard.Should().Throw<ArgumentNullException>();
        rewardSummary.Should().Throw<ArgumentNullException>();
        businessSummary.Should().Throw<ArgumentNullException>();
        businessAccount.Should().Throw<ArgumentNullException>();
        pointsTransaction.Should().Throw<ArgumentNullException>();
        timelineEntry.Should().Throw<ArgumentNullException>();
        location.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    ///     Ensures "My businesses" list mapping keeps geo/location and account
    ///     snapshot values stable for consumer discovery shortcuts.
    /// </summary>
    [Fact]
    public void ToContract_MyLoyaltyBusinessSummary_Should_MapGeoAndAccountFields()
    {
        // Arrange
        var dto = new MyLoyaltyBusinessListItemDto
        {
            BusinessId = Guid.NewGuid(),
            BusinessName = "Darwin Bakery",
            Category = Darwin.Domain.Enums.BusinessCategoryKind.Bakery,
            City = "Cologne",
            Coordinate = new GeoCoordinateDto { Latitude = 50.9375, Longitude = 6.9603, AltitudeMeters = 53.2 },
            PrimaryImageUrl = "https://cdn.example/bakery.png",
            PointsBalance = 41,
            LifetimePoints = 320,
            AccountStatus = Darwin.Domain.Enums.LoyaltyAccountStatus.Active,
            LastAccrualAtUtc = DateTime.UtcNow.AddDays(-3)
        };

        // Act
        var contract = LoyaltyContractsMapper.ToContract(dto);

        // Assert
        contract.BusinessName.Should().Be("Darwin Bakery");
        contract.Category.Should().Be(Darwin.Domain.Enums.BusinessCategoryKind.Bakery.ToString());
        contract.Location.Should().NotBeNull();
        contract.Location!.Latitude.Should().Be(50.9375);
        contract.PointsBalance.Should().Be(41);
        contract.Status.Should().Be(Darwin.Domain.Enums.LoyaltyAccountStatus.Active.ToString());
    }

}
