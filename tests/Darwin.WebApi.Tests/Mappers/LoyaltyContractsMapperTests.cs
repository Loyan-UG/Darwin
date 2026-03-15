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
