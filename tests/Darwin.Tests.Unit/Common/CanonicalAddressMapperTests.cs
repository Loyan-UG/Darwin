using System.Text.Json;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Common.Addresses;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using FluentAssertions;

namespace Darwin.Tests.Unit.Common;

public sealed class CanonicalAddressMapperTests
{
    [Fact]
    public void FromAddress_Should_NormalizeIdentityAddress()
    {
        var address = new Address
        {
            FullName = "  Jane Customer  ",
            Company = "  Example GmbH  ",
            Street1 = "  Main Street 1  ",
            Street2 = "  Floor 2  ",
            PostalCode = "  10115  ",
            City = "  Berlin  ",
            State = "  BE  ",
            CountryCode = " de ",
            PhoneE164 = "  +491234567  "
        };

        var canonical = CanonicalAddressMapper.FromAddress(address);

        canonical.FullName.Should().Be("Jane Customer");
        canonical.Company.Should().Be("Example GmbH");
        canonical.Street1.Should().Be("Main Street 1");
        canonical.Street2.Should().Be("Floor 2");
        canonical.PostalCode.Should().Be("10115");
        canonical.City.Should().Be("Berlin");
        canonical.StateOrRegion.Should().Be("BE");
        canonical.CountryCode.Should().Be("DE");
        canonical.PhoneE164.Should().Be("+491234567");
    }

    [Fact]
    public void ToCustomerAddress_Should_PreserveCrmWireShapeFields()
    {
        var dto = new CustomerAddressDto
        {
            AddressId = Guid.NewGuid(),
            Line1 = "  Bahnhofstrasse 10  ",
            Line2 = "  Suite 4  ",
            City = "  Zurich  ",
            State = "  ZH  ",
            PostalCode = "  8001  ",
            Country = " ch ",
            IsDefaultBilling = true,
            IsDefaultShipping = true
        };

        var address = CanonicalAddressMapper.ToCustomerAddress(dto);

        address.AddressId.Should().Be(dto.AddressId);
        address.Line1.Should().Be("Bahnhofstrasse 10");
        address.Line2.Should().Be("Suite 4");
        address.City.Should().Be("Zurich");
        address.State.Should().Be("ZH");
        address.PostalCode.Should().Be("8001");
        address.Country.Should().Be("CH");
        address.IsDefaultBilling.Should().BeTrue();
        address.IsDefaultShipping.Should().BeTrue();
    }

    [Fact]
    public void ToCustomerAddressDto_Should_PreserveExistingCrmContractNames()
    {
        var address = new CustomerAddress
        {
            Id = Guid.NewGuid(),
            AddressId = Guid.NewGuid(),
            Line1 = "  Main Road 2  ",
            Line2 = "  ",
            City = "  Hamburg  ",
            State = "  HH  ",
            PostalCode = "  20095  ",
            Country = " de ",
            IsDefaultBilling = true
        };

        var dto = CanonicalAddressMapper.ToCustomerAddressDto(address);

        dto.Id.Should().Be(address.Id);
        dto.AddressId.Should().Be(address.AddressId);
        dto.Line1.Should().Be("Main Road 2");
        dto.Line2.Should().BeNull();
        dto.City.Should().Be("Hamburg");
        dto.State.Should().Be("HH");
        dto.PostalCode.Should().Be("20095");
        dto.Country.Should().Be("DE");
        dto.IsDefaultBilling.Should().BeTrue();
        dto.IsDefaultShipping.Should().BeFalse();
    }

    [Fact]
    public void ApplyToBusinessLocation_Should_PreserveLocationSpecificFields()
    {
        var location = new BusinessLocation
        {
            Name = "Primary",
            OpeningHoursJson = "{\"mon\":[]}"
        };
        var dto = new BusinessLocationCreateDto
        {
            AddressLine1 = "  Market 1  ",
            AddressLine2 = "  Hall A  ",
            City = "  Cologne  ",
            Region = "  NRW  ",
            PostalCode = "  50667  ",
            CountryCode = " de "
        };

        CanonicalAddressMapper.ApplyToBusinessLocation(location, CanonicalAddressMapper.FromBusinessLocationDto(dto));

        location.AddressLine1.Should().Be("Market 1");
        location.AddressLine2.Should().Be("Hall A");
        location.City.Should().Be("Cologne");
        location.Region.Should().Be("NRW");
        location.PostalCode.Should().Be("50667");
        location.CountryCode.Should().Be("DE");
        location.OpeningHoursJson.Should().Be("{\"mon\":[]}");
    }

    [Fact]
    public void FromBusinessPublicLocationDto_Should_NotRequireCoordinateOrOpeningHours()
    {
        var dto = new BusinessPublicLocationDto
        {
            AddressLine1 = "  Ring 7  ",
            City = "  Munich  ",
            CountryCode = " de ",
            PostalCode = "  80331  "
        };

        var canonical = CanonicalAddressMapper.FromBusinessPublicLocationDto(dto);

        canonical.Street1.Should().Be("Ring 7");
        canonical.City.Should().Be("Munich");
        canonical.CountryCode.Should().Be("DE");
        canonical.PostalCode.Should().Be("80331");
    }

    [Fact]
    public void ToCheckoutAddressDto_Should_KeepSnapshotWireShape()
    {
        var dto = new CheckoutAddressDto
        {
            FullName = "  Max Buyer  ",
            Company = "  Buyer GmbH  ",
            Street1 = "  Delivery Street 5  ",
            Street2 = "  ",
            PostalCode = "  10117  ",
            City = "  Berlin  ",
            State = "  BE  ",
            CountryCode = " de ",
            PhoneE164 = "  +491111111  "
        };

        var snapshot = CanonicalAddressMapper.ToCheckoutAddressDto(CanonicalAddressMapper.FromCheckoutAddress(dto));
        var json = JsonSerializer.Serialize(snapshot);
        var roundTrip = JsonSerializer.Deserialize<CheckoutAddressDto>(json);

        roundTrip.Should().NotBeNull();
        roundTrip!.FullName.Should().Be("Max Buyer");
        roundTrip.Company.Should().Be("Buyer GmbH");
        roundTrip.Street1.Should().Be("Delivery Street 5");
        roundTrip.Street2.Should().BeNull();
        roundTrip.PostalCode.Should().Be("10117");
        roundTrip.City.Should().Be("Berlin");
        roundTrip.State.Should().Be("BE");
        roundTrip.CountryCode.Should().Be("DE");
        roundTrip.PhoneE164.Should().Be("+491111111");
    }
}
