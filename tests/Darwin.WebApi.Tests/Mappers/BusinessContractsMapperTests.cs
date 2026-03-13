using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Common.DTOs;
using Darwin.Domain.Enums;
using Darwin.WebApi.Mappers;
using FluentAssertions;

namespace Darwin.WebApi.Tests.Mappers;

/// <summary>
///     Verifies projection behavior of <see cref="BusinessContractsMapper"/> so
///     contract-facing response shapes remain stable for mobile clients.
/// </summary>
public sealed class BusinessContractsMapperTests
{
    /// <summary>
    ///     Ensures discovery list mapping converts distance from kilometers to meters,
    ///     preserves optional location payload, and emits string category tokens.
    /// </summary>
    [Fact]
    public void ToContract_DiscoveryItem_Should_MapDistanceAndLocationCorrectly()
    {
        // Arrange
        var dto = new BusinessDiscoveryListItemDto
        {
            Id = Guid.NewGuid(),
            Name = "Darwin Cafe",
            ShortDescription = "Neighborhood coffee",
            Category = BusinessCategoryKind.Cafe,
            IsActive = true,
            City = "Berlin",
            Coordinate = new GeoCoordinateDto
            {
                Latitude = 52.520008,
                Longitude = 13.404954,
                AltitudeMeters = 35.5
            },
            PrimaryImageUrl = "https://cdn.example/logo.png",
            DistanceKm = 1.245,
            RatingAverage = 4.7m,
            RatingCount = 112,
            IsOpenNow = true
        };

        // Act
        var contract = BusinessContractsMapper.ToContract(dto);

        // Assert
        contract.Id.Should().Be(dto.Id);
        contract.Name.Should().Be("Darwin Cafe");
        contract.Category.Should().Be(BusinessCategoryKind.Cafe.ToString());
        contract.DistanceMeters.Should().Be(1245);
        contract.Location.Should().NotBeNull();
        contract.Location!.Latitude.Should().Be(52.520008);
        contract.Location.Longitude.Should().Be(13.404954);
        contract.Location.AltitudeMeters.Should().Be(35.5);
        contract.Rating.Should().Be(4.7d);
        contract.RatingCount.Should().Be(112);
        contract.IsOpenNow.Should().BeTrue();
    }

    /// <summary>
    ///     Ensures business detail mapping uses defensive defaults for currency/culture,
    ///     preserves contact fields, and builds backward-compatible image arrays.
    /// </summary>
    [Fact]
    public void ToContract_BusinessDetail_Should_ApplyDefaultsAndComposeImages()
    {
        // Arrange
        var dto = new BusinessPublicDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "",
            Category = BusinessCategoryKind.Restaurant,
            DefaultCurrency = "  ",
            DefaultCulture = null!,
            ContactPhoneE164 = "+49123456789",
            PrimaryImageUrl = "https://cdn.example/primary.jpg",
            GalleryImageUrls = ["https://cdn.example/gallery-a.jpg", "https://cdn.example/gallery-b.jpg"],
            Locations =
            [
                new BusinessPublicLocationDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Secondary",
                    IsPrimary = false,
                    City = "Munich",
                    Coordinate = new GeoCoordinateDto { Latitude = 48.1351, Longitude = 11.5820 }
                },
                new BusinessPublicLocationDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Primary",
                    IsPrimary = true,
                    City = "Hamburg",
                    Coordinate = new GeoCoordinateDto { Latitude = 53.5511, Longitude = 9.9937 }
                }
            ]
        };

        // Act
        var contract = BusinessContractsMapper.ToContract(dto);

        // Assert
        contract.Name.Should().Be(string.Empty);
        contract.DefaultCurrency.Should().Be("EUR");
        contract.DefaultCulture.Should().Be("de-DE");
        contract.Category.Should().Be(BusinessCategoryKind.Restaurant.ToString());
        contract.PhoneE164.Should().Be("+49123456789");
        contract.ContactPhoneE164.Should().Be("+49123456789");
        contract.City.Should().Be("Hamburg");
        contract.ImageUrls.Should().ContainInOrder(
            "https://cdn.example/primary.jpg",
            "https://cdn.example/gallery-a.jpg",
            "https://cdn.example/gallery-b.jpg");
    }

    /// <summary>
    ///     Ensures combined business-detail/account mapping preserves HasAccount flag
    ///     and maps nested loyalty account snapshot using contract field names.
    /// </summary>
    [Fact]
    public void ToContract_BusinessDetailWithMyAccount_Should_MapNestedAccountSnapshot()
    {
        // Arrange
        var dto = new BusinessPublicDetailWithMyAccountDto
        {
            Business = new BusinessPublicDetailDto
            {
                Id = Guid.NewGuid(),
                Name = "Darwin Market",
                Category = BusinessCategoryKind.Grocery,
                DefaultCurrency = "EUR",
                DefaultCulture = "de-DE",
                Locations =
                [
                    new BusinessPublicLocationDto
                    {
                        Id = Guid.NewGuid(),
                        Name = "Primary",
                        IsPrimary = true,
                        City = "Berlin",
                        Coordinate = new GeoCoordinateDto { Latitude = 52.5, Longitude = 13.4 }
                    }
                ]
            },
            HasAccount = true,
            MyAccount = new LoyaltyAccountSummaryDto
            {
                Id = Guid.NewGuid(),
                BusinessId = Guid.NewGuid(),
                BusinessName = "Darwin Market",
                PointsBalance = 88,
                LifetimePoints = 220,
                Status = Darwin.Domain.Enums.LoyaltyAccountStatus.Active,
                LastAccrualAtUtc = DateTime.UtcNow.AddHours(-6)
            }
        };

        // Act
        var contract = BusinessContractsMapper.ToContract(dto);

        // Assert
        contract.HasAccount.Should().BeTrue();
        contract.Business.Name.Should().Be("Darwin Market");
        contract.MyAccount.Should().NotBeNull();
        contract.MyAccount!.PointsBalance.Should().Be(88);
        contract.MyAccount.BusinessName.Should().Be("Darwin Market");
    }

}
