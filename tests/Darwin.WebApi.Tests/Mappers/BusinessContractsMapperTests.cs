using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Common.DTOs;
using Darwin.Application.Loyalty.DTOs;
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
    ///     Ensures mapper guard clauses reject null discovery DTO input.
    /// </summary>
    [Fact]
    public void ToContract_DiscoveryItem_Should_Throw_WhenDtoIsNull()
    {
        // Arrange
        BusinessDiscoveryListItemDto? dto = null;

        // Act
        Action act = () => BusinessContractsMapper.ToContract(dto!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

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
    ///     Ensures kilometer-to-meter conversion uses MidpointRounding.AwayFromZero
    ///     so half-meter boundaries stay deterministic across runtimes.
    /// </summary>
    [Theory]
    [InlineData(1.2344, 1234)]
    [InlineData(1.2345, 1235)]
    [InlineData(1.2346, 1235)]
    public void ToContract_DiscoveryItem_Should_RoundDistanceUsingAwayFromZero(
        double distanceKm,
        int expectedDistanceMeters)
    {
        // Arrange
        var dto = new BusinessDiscoveryListItemDto
        {
            Id = Guid.NewGuid(),
            Name = "Darwin Kiosk",
            Category = BusinessCategoryKind.Cafe,
            DistanceKm = distanceKm
        };

        // Act
        var contract = BusinessContractsMapper.ToContract(dto);

        // Assert
        contract.DistanceMeters.Should().Be(expectedDistanceMeters);
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
    ///     Ensures legacy combined image list trims whitespace and skips empty entries
    ///     while preserving stable primary-first ordering.
    /// </summary>
    [Fact]
    public void ToContract_BusinessDetail_Should_TrimAndFilterImageUrls()
    {
        // Arrange
        var dto = new BusinessPublicDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "Darwin Roastery",
            Category = BusinessCategoryKind.Cafe,
            DefaultCurrency = "EUR",
            DefaultCulture = "de-DE",
            PrimaryImageUrl = " https://cdn.example/primary.jpg ",
            GalleryImageUrls = [" ", " https://cdn.example/gallery-a.jpg ", null!, "https://cdn.example/gallery-b.jpg"],
            Locations =
            [
                new BusinessPublicLocationDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Primary",
                    IsPrimary = true,
                    City = "Berlin"
                }
            ]
        };

        // Act
        var contract = BusinessContractsMapper.ToContract(dto);

        // Assert
        contract.ImageUrls.Should().ContainInOrder(
            "https://cdn.example/primary.jpg",
            "https://cdn.example/gallery-a.jpg",
            "https://cdn.example/gallery-b.jpg");
        contract.ImageUrls.Should().HaveCount(3);
    }

    /// <summary>
    ///     Ensures distance projection remains null for discovery entries without
    ///     explicit distance values.
    /// </summary>
    [Fact]
    public void ToContract_DiscoveryItem_Should_HandleNullDistance()
    {
        // Arrange
        var dto = new BusinessDiscoveryListItemDto
        {
            Id = Guid.NewGuid(),
            Name = "Darwin NoGPS",
            Category = BusinessCategoryKind.Cafe,
            DistanceKm = null
        };

        // Act
        var contract = BusinessContractsMapper.ToContract(dto);

        // Assert
        contract.DistanceMeters.Should().BeNull();
    }

    /// <summary>
    ///     Ensures discovery items without names map to empty contract names.
    /// </summary>
    [Fact]
    public void ToContract_DiscoveryItem_Should_DefaultNameToEmpty_WhenMissing()
    {
        // Arrange
        var dto = new BusinessDiscoveryListItemDto
        {
            Id = Guid.NewGuid(),
            Name = null,
            Category = BusinessCategoryKind.Cafe
        };

        // Act
        var contract = BusinessContractsMapper.ToContract(dto);

        // Assert
        contract.Name.Should().BeEmpty();
    }

    /// <summary>
    ///     Ensures business-detail projection falls back to the first location when
    ///     no location is marked as primary, keeping city/coordinate fields populated.
    /// </summary>
    [Fact]
    public void ToContract_BusinessDetail_Should_FallbackToFirstLocation_WhenPrimaryLocationMissing()
    {
        // Arrange
        var dto = new BusinessPublicDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "Darwin Bakery",
            Category = BusinessCategoryKind.Cafe,
            DefaultCurrency = "EUR",
            DefaultCulture = "de-DE",
            Locations =
            [
                new BusinessPublicLocationDto
                {
                    Id = Guid.NewGuid(),
                    Name = "First Branch",
                    IsPrimary = false,
                    City = "Cologne",
                    Coordinate = new GeoCoordinateDto { Latitude = 50.9375, Longitude = 6.9603, AltitudeMeters = 53 }
                },
                new BusinessPublicLocationDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Second Branch",
                    IsPrimary = false,
                    City = "Stuttgart",
                    Coordinate = new GeoCoordinateDto { Latitude = 48.7758, Longitude = 9.1829, AltitudeMeters = 245 }
                }
            ]
        };

        // Act
        var contract = BusinessContractsMapper.ToContract(dto);

        // Assert
        contract.City.Should().Be("Cologne");
        contract.Coordinate.Should().NotBeNull();
        contract.Coordinate!.Latitude.Should().Be(50.9375);
        contract.Coordinate.Longitude.Should().Be(6.9603);
    }

    /// <summary>
    ///     Ensures business-detail projection uses the explicitly primary location
    ///     when it is present.
    /// </summary>
    [Fact]
    public void ToContract_BusinessDetail_Should_UsePrimaryLocation_WhenAvailable()
    {
        // Arrange
        var dto = new BusinessPublicDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "Darwin Hub",
            Category = BusinessCategoryKind.Cafe,
            DefaultCurrency = "EUR",
            DefaultCulture = "de-DE",
            Locations =
            [
                new BusinessPublicLocationDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Branch A",
                    IsPrimary = false,
                    City = "Stuttgart",
                    Coordinate = new GeoCoordinateDto { Latitude = 48.7758, Longitude = 9.1829 }
                },
                new BusinessPublicLocationDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Primary",
                    IsPrimary = true,
                    City = "Dresden",
                    Coordinate = new GeoCoordinateDto { Latitude = 51.0504, Longitude = 13.7373 }
                }
            ]
        };

        // Act
        var contract = BusinessContractsMapper.ToContract(dto);

        // Assert
        contract.City.Should().Be("Dresden");
        contract.Coordinate.Should().NotBeNull();
        contract.Coordinate!.Latitude.Should().Be(51.0504);
        contract.Coordinate.Longitude.Should().Be(13.7373);
    }

    /// <summary>
    ///     Ensures business-detail projection handles empty location sets with empty
    ///     city/coordinate and a non-null image list.
    /// </summary>
    [Fact]
    public void ToContract_BusinessDetail_Should_KeepCityCoordinateUnset_WhenNoLocations()
    {
        // Arrange
        var dto = new BusinessPublicDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "Darwin Silent Shop",
            Category = BusinessCategoryKind.Cafe,
            DefaultCurrency = "EUR",
            DefaultCulture = "de-DE",
            GalleryImageUrls = []
        };

        // Act
        var contract = BusinessContractsMapper.ToContract(dto);

        // Assert
        contract.City.Should().BeNull();
        contract.Coordinate.Should().BeNull();
        contract.ImageUrls.Should().NotBeNull();
        contract.ImageUrls.Should().BeEmpty();
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
                Category = BusinessCategoryKind.Cafe,
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

    /// <summary>
    ///     Ensures a detail-with-account payload can explicitly indicate no active
    ///     account and keep MyAccount null without throwing.
    /// </summary>
    [Fact]
    public void ToContract_BusinessDetailWithMyAccount_Should_KeepMyAccountNull_WhenHasAccountIsFalse()
    {
        // Arrange
        var dto = new BusinessPublicDetailWithMyAccountDto
        {
            Business = new BusinessPublicDetailDto
            {
                Id = Guid.NewGuid(),
                Name = "Darwin Express",
                Category = BusinessCategoryKind.Cafe,
                DefaultCurrency = "EUR",
                DefaultCulture = "de-DE"
            },
            HasAccount = false,
            MyAccount = null
        };

        // Act
        var contract = BusinessContractsMapper.ToContract(dto);

        // Assert
        contract.HasAccount.Should().BeFalse();
        contract.MyAccount.Should().BeNull();
        contract.Business.Name.Should().Be("Darwin Express");
    }

    /// <summary>
    ///     Ensures combined business-detail/account mapping enforces required nested business payload.
    /// </summary>
    [Fact]
    public void ToContract_BusinessDetailWithMyAccount_Should_Throw_WhenBusinessIsNull()
    {
        // Arrange
        var dto = new BusinessPublicDetailWithMyAccountDto
        {
            Business = null!,
            HasAccount = false,
            MyAccount = null
        };

        // Act
        Action act = () => BusinessContractsMapper.ToContract(dto);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }


    /// <summary>
    ///     Ensures location mapping preserves address fragments, primary flag,
    ///     opening-hours JSON, and coordinate altitude values.
    /// </summary>
    [Fact]
    public void ToContract_BusinessLocation_Should_MapAddressAndCoordinateFields()
    {
        // Arrange
        var dto = new BusinessPublicLocationDto
        {
            Id = Guid.NewGuid(),
            Name = "Downtown Branch",
            AddressLine1 = "Main Street 1",
            AddressLine2 = "Floor 2",
            City = "Frankfurt",
            Region = "HE",
            CountryCode = "DE",
            PostalCode = "60311",
            Coordinate = new GeoCoordinateDto
            {
                Latitude = 50.1109,
                Longitude = 8.6821,
                AltitudeMeters = 112.4
            },
            IsPrimary = true,
            OpeningHoursJson = "{\"mon\":\"08:00-18:00\"}"
        };

        // Act
        var contract = BusinessContractsMapper.ToContract(dto);

        // Assert
        contract.BusinessLocationId.Should().Be(dto.Id);
        contract.Name.Should().Be("Downtown Branch");
        contract.AddressLine1.Should().Be("Main Street 1");
        contract.AddressLine2.Should().Be("Floor 2");
        contract.City.Should().Be("Frankfurt");
        contract.Region.Should().Be("HE");
        contract.CountryCode.Should().Be("DE");
        contract.PostalCode.Should().Be("60311");
        contract.Coordinate.Should().NotBeNull();
        contract.Coordinate!.AltitudeMeters.Should().Be(112.4);
        contract.IsPrimary.Should().BeTrue();
        contract.OpeningHoursJson.Should().Contain("08:00-18:00");
    }

    /// <summary>
    ///     Ensures location mapping keeps coordinate null when source coordinate is absent.
    /// </summary>
    [Fact]
    public void ToContract_BusinessLocation_Should_KeepNullCoordinate_WhenCoordinateMissing()
    {
        // Arrange
        var dto = new BusinessPublicLocationDto
        {
            Id = Guid.NewGuid(),
            Name = "No GPS",
            City = "Leipzig",
            IsPrimary = false,
            Coordinate = null
        };

        // Act
        var contract = BusinessContractsMapper.ToContract(dto);

        // Assert
        contract.Coordinate.Should().BeNull();
        contract.City.Should().Be("Leipzig");
    }

    /// <summary>
    ///     Ensures location name always maps to a non-null contract value.
    /// </summary>
    [Fact]
    public void ToContract_BusinessLocation_Should_DefaultNameToEmpty_WhenNameMissing()
    {
        // Arrange
        var dto = new BusinessPublicLocationDto
        {
            Id = Guid.NewGuid(),
            Name = null,
            City = "Leipzig",
            IsPrimary = false
        };

        // Act
        var contract = BusinessContractsMapper.ToContract(dto);

        // Assert
        contract.Name.Should().BeEmpty();
    }

    /// <summary>
    ///     Ensures location mapper guard clause rejects null DTO input.
    /// </summary>
    [Fact]
    public void ToContract_BusinessLocation_Should_Throw_WhenDtoIsNull()
    {
        // Arrange
        BusinessPublicLocationDto? dto = null;

        // Act
        Action act = () => BusinessContractsMapper.ToContract(dto!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    ///     Ensures loyalty program mapping preserves reward-tier value/type fields,
    ///     including self-redemption capability metadata.
    /// </summary>
    [Fact]
    public void ToContract_LoyaltyProgramPublic_Should_MapRewardTiersAndFlags()
    {
        // Arrange
        var dto = new LoyaltyProgramPublicDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            Name = "City Rewards",
            IsActive = true,
            RewardTiers =
            [
                new LoyaltyRewardTierPublicDto
                {
                    Id = Guid.NewGuid(),
                    PointsRequired = 140,
                    RewardType = Darwin.Domain.Enums.LoyaltyRewardType.PercentDiscount,
                    RewardValue = 10,
                    Description = "10% off",
                    AllowSelfRedemption = true
                }
            ]
        };

        // Act
        var contract = BusinessContractsMapper.ToContract(dto);

        // Assert
        contract.Id.Should().Be(dto.Id);
        contract.BusinessId.Should().Be(dto.BusinessId);
        contract.Name.Should().Be("City Rewards");
        contract.IsActive.Should().BeTrue();
        contract.RewardTiers.Should().HaveCount(1);
        contract.RewardTiers[0].PointsRequired.Should().Be(140);
        contract.RewardTiers[0].RewardType.Should().Be(Darwin.Domain.Enums.LoyaltyRewardType.PercentDiscount.ToString());
        contract.RewardTiers[0].RewardValue.Should().Be(10);
        contract.RewardTiers[0].AllowSelfRedemption.Should().BeTrue();
    }

    /// <summary>
    ///     Ensures loyalty-program projection keeps contract lists non-null and
    ///     applies stable string/name defaults when optional source data is absent.
    /// </summary>
    [Fact]
    public void ToContract_LoyaltyProgramPublic_Should_ApplySafeDefaults_WhenOptionalFieldsAreMissing()
    {
        // Arrange
        var dto = new LoyaltyProgramPublicDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            Name = null!,
            IsActive = false,
            RewardTiers = null!
        };

        // Act
        var contract = BusinessContractsMapper.ToContract(dto);

        // Assert
        contract.Name.Should().BeEmpty();
        contract.IsActive.Should().BeFalse();
        contract.RewardTiers.Should().NotBeNull();
        contract.RewardTiers.Should().BeEmpty();
    }

    /// <summary>
    ///     Ensures loyalty-program mapper guard clause rejects null DTO input.
    /// </summary>
    [Fact]
    public void ToContract_LoyaltyProgramPublic_Should_Throw_WhenDtoIsNull()
    {
        // Arrange
        LoyaltyProgramPublicDto? dto = null;

        // Act
        Action act = () => BusinessContractsMapper.ToContract(dto!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

}
