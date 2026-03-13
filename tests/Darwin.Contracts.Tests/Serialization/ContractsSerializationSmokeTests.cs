using Darwin.Contracts.Businesses;
using Darwin.Contracts.Common;
using Darwin.Contracts.Identity;
using Darwin.Contracts.Loyalty;
using FluentAssertions;
using System.Text.Json;

namespace Darwin.Contracts.Tests.Serialization;

/// <summary>
///     Provides contract-level serialization smoke tests for DTOs that are
///     directly consumed by mobile applications.
/// </summary>
public sealed class ContractsSerializationSmokeTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     Verifies that <see cref="TokenResponse"/> serializes and deserializes
    ///     without data loss for authentication-critical fields.
    /// </summary>
    [Fact]
    public void TokenResponse_Should_RoundTripSerialization()
    {
        // Arrange
        var model = new TokenResponse
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            AccessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
            RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            UserId = Guid.NewGuid(),
            Email = "user@example.test",
            SecurityStamp = "stamp-1"
        };

        // Act
        var json = JsonSerializer.Serialize(model, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<TokenResponse>(json, JsonOptions);

        // Assert
        roundTrip.Should().NotBeNull();
        roundTrip!.AccessToken.Should().Be("access-token");
        roundTrip.RefreshToken.Should().Be("refresh-token");
        roundTrip.UserId.Should().Be(model.UserId);
        roundTrip.Email.Should().Be("user@example.test");
        roundTrip.SecurityStamp.Should().Be("stamp-1");
    }

    /// <summary>
    ///     Verifies map discovery request and business summary contracts keep
    ///     coordinate and optional numeric fields stable across JSON boundaries.
    /// </summary>
    [Fact]
    public void BusinessDiscoveryContracts_Should_RoundTripCoordinatesAndRatingFields()
    {
        // Arrange
        var request = new BusinessMapDiscoveryRequest
        {
            Center = new GeoCoordinateModel { Latitude = 52.52, Longitude = 13.40, AltitudeMeters = 34.2 },
            RadiusKm = 3.5,
            Query = "coffee",
            Category = "Cafe",
            MinRating = 4.2,
            HasActiveLoyaltyProgram = true,
            MaxResults = 100
        };

        var summary = new BusinessSummary
        {
            Id = Guid.NewGuid(),
            Name = "Darwin Cafe",
            Category = "Cafe",
            Location = new GeoCoordinateModel { Latitude = 52.52, Longitude = 13.40, AltitudeMeters = 34.2 },
            Rating = 4.8,
            RatingCount = 25,
            DistanceMeters = 740
        };

        // Act
        var requestJson = JsonSerializer.Serialize(request, JsonOptions);
        var summaryJson = JsonSerializer.Serialize(summary, JsonOptions);

        var requestRoundTrip = JsonSerializer.Deserialize<BusinessMapDiscoveryRequest>(requestJson, JsonOptions);
        var summaryRoundTrip = JsonSerializer.Deserialize<BusinessSummary>(summaryJson, JsonOptions);

        // Assert
        requestRoundTrip.Should().NotBeNull();
        requestRoundTrip!.Center.Should().NotBeNull();
        requestRoundTrip.Center!.Latitude.Should().Be(52.52);
        requestRoundTrip.MinRating.Should().Be(4.2);

        summaryRoundTrip.Should().NotBeNull();
        summaryRoundTrip!.Location.Should().NotBeNull();
        summaryRoundTrip.Location!.Longitude.Should().Be(13.40);
        summaryRoundTrip.Rating.Should().Be(4.8);
        summaryRoundTrip.DistanceMeters.Should().Be(740);
    }

    /// <summary>
    ///     Verifies loyalty timeline page keeps paging cursors and entry payloads
    ///     stable across serialization.
    /// </summary>
    [Fact]
    public void LoyaltyTimelinePageResponse_Should_RoundTripWithItemsAndCursor()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var model = new GetMyLoyaltyTimelinePageResponse
        {
            Items =
            [
                new LoyaltyTimelineEntry
                {
                    Id = entryId,
                    Kind = LoyaltyTimelineEntryKind.PointsTransaction,
                    LoyaltyAccountId = Guid.NewGuid(),
                    BusinessId = Guid.NewGuid(),
                    OccurredAtUtc = DateTime.UtcNow,
                    PointsDelta = 15,
                    PointsSpent = null,
                    RewardTierId = null,
                    Reference = "txn-101",
                    Note = "Accrual"
                }
            ],
            NextBeforeAtUtc = DateTime.UtcNow.AddMinutes(-2),
            NextBeforeId = Guid.NewGuid()
        };

        // Act
        var json = JsonSerializer.Serialize(model, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<GetMyLoyaltyTimelinePageResponse>(json, JsonOptions);

        // Assert
        roundTrip.Should().NotBeNull();
        roundTrip!.Items.Should().HaveCount(1);
        roundTrip.Items[0].Id.Should().Be(entryId);
        roundTrip.NextBeforeAtUtc.Should().NotBeNull();
        roundTrip.NextBeforeId.Should().NotBeNull();
    }
}
