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
}
