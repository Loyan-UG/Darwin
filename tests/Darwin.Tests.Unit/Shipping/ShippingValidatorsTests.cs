using System;
using System.Collections.Generic;
using Darwin.Application.Shipping.DTOs;
using Darwin.Application.Shipping.Validators;
using FluentAssertions;

namespace Darwin.Tests.Unit.Shipping;

/// <summary>
/// Unit tests for <see cref="ShippingMethodCreateValidator"/>,
/// <see cref="ShippingMethodEditValidator"/>, <see cref="ShippingRateValidator"/>,
/// and <see cref="RateShipmentInputValidator"/>.
/// </summary>
public sealed class ShippingValidatorsTests
{
    // ─── ShippingMethodCreateValidator ───────────────────────────────────────

    [Fact]
    public void ShippingMethodCreate_Should_Pass_For_Minimal_Valid_Dto()
    {
        var dto = new ShippingMethodCreateDto
        {
            Name = "Standard Shipping",
            Carrier = "DHL",
            Service = "PARCEL",
            Rates = new List<ShippingRateDto>()
        };

        var result = new ShippingMethodCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a well-formed shipping method create request should pass");
    }

    [Fact]
    public void ShippingMethodCreate_Should_Pass_With_Optional_Currency()
    {
        var dto = new ShippingMethodCreateDto
        {
            Name = "Express",
            Carrier = "DHL",
            Service = "EXPRESS",
            Currency = "EUR",
            Rates = new List<ShippingRateDto>()
        };

        var result = new ShippingMethodCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a 3-character currency code is valid");
    }

    [Fact]
    public void ShippingMethodCreate_Should_Pass_With_Valid_Rates()
    {
        var dto = new ShippingMethodCreateDto
        {
            Name = "Standard",
            Carrier = "DHL",
            Service = "PARCEL",
            Rates = new List<ShippingRateDto>
            {
                new() { SortOrder = 0, PriceMinor = 499 },
                new() { SortOrder = 1, PriceMinor = 0, MaxSubtotalNetMinor = 10000 }
            }
        };

        var result = new ShippingMethodCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("valid rates should not cause failure");
    }

    [Fact]
    public void ShippingMethodCreate_Should_Fail_When_Name_Empty()
    {
        var dto = new ShippingMethodCreateDto
        {
            Name = "",
            Carrier = "DHL",
            Service = "PARCEL",
            Rates = new List<ShippingRateDto>()
        };

        var result = new ShippingMethodCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Name is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Name));
    }

    [Fact]
    public void ShippingMethodCreate_Should_Fail_When_Name_Too_Long()
    {
        var dto = new ShippingMethodCreateDto
        {
            Name = new string('A', 257),
            Carrier = "DHL",
            Service = "PARCEL",
            Rates = new List<ShippingRateDto>()
        };

        var result = new ShippingMethodCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Name must not exceed 256 characters");
    }

    [Fact]
    public void ShippingMethodCreate_Should_Fail_When_Carrier_Empty()
    {
        var dto = new ShippingMethodCreateDto
        {
            Name = "Standard",
            Carrier = "",
            Service = "PARCEL",
            Rates = new List<ShippingRateDto>()
        };

        var result = new ShippingMethodCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Carrier is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Carrier));
    }

    [Fact]
    public void ShippingMethodCreate_Should_Fail_When_Carrier_Too_Long()
    {
        var dto = new ShippingMethodCreateDto
        {
            Name = "Standard",
            Carrier = new string('C', 65),
            Service = "PARCEL",
            Rates = new List<ShippingRateDto>()
        };

        var result = new ShippingMethodCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Carrier must not exceed 64 characters");
    }

    [Fact]
    public void ShippingMethodCreate_Should_Fail_When_Service_Empty()
    {
        var dto = new ShippingMethodCreateDto
        {
            Name = "Standard",
            Carrier = "DHL",
            Service = "",
            Rates = new List<ShippingRateDto>()
        };

        var result = new ShippingMethodCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Service is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Service));
    }

    [Fact]
    public void ShippingMethodCreate_Should_Fail_When_Currency_Wrong_Length()
    {
        var dto = new ShippingMethodCreateDto
        {
            Name = "Standard",
            Carrier = "DHL",
            Service = "PARCEL",
            Currency = "EURO",
            Rates = new List<ShippingRateDto>()
        };

        var result = new ShippingMethodCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Currency must be exactly 3 characters when provided");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Currency));
    }

    [Fact]
    public void ShippingMethodCreate_Should_Pass_When_Currency_Is_Null()
    {
        var dto = new ShippingMethodCreateDto
        {
            Name = "Standard",
            Carrier = "DHL",
            Service = "PARCEL",
            Currency = null,
            Rates = new List<ShippingRateDto>()
        };

        var result = new ShippingMethodCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("null Currency means no currency override is required");
    }

    [Fact]
    public void ShippingMethodCreate_Should_Fail_When_Rate_Has_Invalid_SortOrder()
    {
        var dto = new ShippingMethodCreateDto
        {
            Name = "Standard",
            Carrier = "DHL",
            Service = "PARCEL",
            Rates = new List<ShippingRateDto>
            {
                new() { SortOrder = -1, PriceMinor = 499 }
            }
        };

        var result = new ShippingMethodCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("SortOrder must be >= 0");
    }

    [Fact]
    public void ShippingMethodCreate_Should_Fail_When_Rate_Has_Negative_PriceMinor()
    {
        var dto = new ShippingMethodCreateDto
        {
            Name = "Standard",
            Carrier = "DHL",
            Service = "PARCEL",
            Rates = new List<ShippingRateDto>
            {
                new() { SortOrder = 0, PriceMinor = -1 }
            }
        };

        var result = new ShippingMethodCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("PriceMinor must be >= 0");
    }

    [Fact]
    public void ShippingMethodCreate_Should_Fail_When_Rate_MaxShipmentMass_Is_Zero()
    {
        var dto = new ShippingMethodCreateDto
        {
            Name = "Standard",
            Carrier = "DHL",
            Service = "PARCEL",
            Rates = new List<ShippingRateDto>
            {
                new() { SortOrder = 0, PriceMinor = 499, MaxShipmentMass = 0 }
            }
        };

        var result = new ShippingMethodCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("MaxShipmentMass must be > 0 when provided");
    }

    [Fact]
    public void ShippingMethodCreate_Should_Pass_When_Rate_MaxShipmentMass_Is_Null()
    {
        var dto = new ShippingMethodCreateDto
        {
            Name = "Standard",
            Carrier = "DHL",
            Service = "PARCEL",
            Rates = new List<ShippingRateDto>
            {
                new() { SortOrder = 0, PriceMinor = 499, MaxShipmentMass = null }
            }
        };

        var result = new ShippingMethodCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("null MaxShipmentMass means no mass cap is applied");
    }

    // ─── ShippingMethodEditValidator ──────────────────────────────────────────

    [Fact]
    public void ShippingMethodEdit_Should_Pass_For_Valid_Dto()
    {
        var dto = new ShippingMethodEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Name = "Express Shipping",
            Carrier = "DHL",
            Service = "EXPRESS",
            Rates = new List<ShippingRateDto>()
        };

        var result = new ShippingMethodEditValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a fully populated edit DTO should pass");
    }

    [Fact]
    public void ShippingMethodEdit_Should_Fail_When_Id_Empty()
    {
        var dto = new ShippingMethodEditDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 },
            Name = "Standard",
            Carrier = "DHL",
            Service = "PARCEL",
            Rates = new List<ShippingRateDto>()
        };

        var result = new ShippingMethodEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Id must not be empty for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void ShippingMethodEdit_Should_Fail_When_RowVersion_Null()
    {
        var dto = new ShippingMethodEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = null!,
            Name = "Standard",
            Carrier = "DHL",
            Service = "PARCEL",
            Rates = new List<ShippingRateDto>()
        };

        var result = new ShippingMethodEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("RowVersion must not be null for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RowVersion));
    }

    [Fact]
    public void ShippingMethodEdit_Should_Fail_When_Name_Empty()
    {
        var dto = new ShippingMethodEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Name = "",
            Carrier = "DHL",
            Service = "PARCEL",
            Rates = new List<ShippingRateDto>()
        };

        var result = new ShippingMethodEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Name is required for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Name));
    }

    [Fact]
    public void ShippingMethodEdit_Should_Fail_When_Currency_Wrong_Length()
    {
        var dto = new ShippingMethodEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Name = "Standard",
            Carrier = "DHL",
            Service = "PARCEL",
            Currency = "EU",
            Rates = new List<ShippingRateDto>()
        };

        var result = new ShippingMethodEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Currency must be exactly 3 characters when provided");
    }

    // ─── ShippingRateValidator ────────────────────────────────────────────────

    [Fact]
    public void ShippingRate_Should_Pass_For_Valid_Rate_No_Caps()
    {
        var rate = new ShippingRateDto { SortOrder = 0, PriceMinor = 0 };

        var result = new ShippingRateValidator().Validate(rate);

        result.IsValid.Should().BeTrue("zero price and no caps is a valid free-shipping rate");
    }

    [Fact]
    public void ShippingRate_Should_Pass_For_Valid_Rate_With_Caps()
    {
        var rate = new ShippingRateDto
        {
            SortOrder = 1,
            PriceMinor = 599,
            MaxShipmentMass = 5000,
            MaxSubtotalNetMinor = 50000
        };

        var result = new ShippingRateValidator().Validate(rate);

        result.IsValid.Should().BeTrue("positive caps are valid");
    }

    [Fact]
    public void ShippingRate_Should_Fail_When_SortOrder_Negative()
    {
        var rate = new ShippingRateDto { SortOrder = -1, PriceMinor = 0 };

        var result = new ShippingRateValidator().Validate(rate);

        result.IsValid.Should().BeFalse("SortOrder must be >= 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(rate.SortOrder));
    }

    [Fact]
    public void ShippingRate_Should_Fail_When_PriceMinor_Negative()
    {
        var rate = new ShippingRateDto { SortOrder = 0, PriceMinor = -100 };

        var result = new ShippingRateValidator().Validate(rate);

        result.IsValid.Should().BeFalse("PriceMinor must be >= 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(rate.PriceMinor));
    }

    [Fact]
    public void ShippingRate_Should_Fail_When_MaxShipmentMass_Is_Zero()
    {
        var rate = new ShippingRateDto { SortOrder = 0, PriceMinor = 0, MaxShipmentMass = 0 };

        var result = new ShippingRateValidator().Validate(rate);

        result.IsValid.Should().BeFalse("MaxShipmentMass = 0 is invalid; must be > 0 when set");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(rate.MaxShipmentMass));
    }

    [Fact]
    public void ShippingRate_Should_Fail_When_MaxSubtotalNetMinor_Is_Zero()
    {
        var rate = new ShippingRateDto { SortOrder = 0, PriceMinor = 0, MaxSubtotalNetMinor = 0 };

        var result = new ShippingRateValidator().Validate(rate);

        result.IsValid.Should().BeFalse("MaxSubtotalNetMinor = 0 is invalid; must be > 0 when set");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(rate.MaxSubtotalNetMinor));
    }

    // ─── RateShipmentInputValidator ───────────────────────────────────────────

    [Fact]
    public void RateShipmentInput_Should_Pass_For_Valid_Dto()
    {
        var dto = new RateShipmentInputDto
        {
            Country = "DE",
            SubtotalNetMinor = 5000,
            ShipmentMass = 1000
        };

        var result = new RateShipmentInputValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a well-formed rate request should pass");
    }

    [Fact]
    public void RateShipmentInput_Should_Pass_With_Optional_Currency()
    {
        var dto = new RateShipmentInputDto
        {
            Country = "US",
            SubtotalNetMinor = 0,
            ShipmentMass = 500,
            Currency = "USD"
        };

        var result = new RateShipmentInputValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a valid 3-character currency is acceptable");
    }

    [Fact]
    public void RateShipmentInput_Should_Fail_When_Country_Empty()
    {
        var dto = new RateShipmentInputDto
        {
            Country = "",
            SubtotalNetMinor = 100,
            ShipmentMass = 100
        };

        var result = new RateShipmentInputValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Country is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Country));
    }

    [Fact]
    public void RateShipmentInput_Should_Fail_When_Country_Wrong_Length()
    {
        var dto = new RateShipmentInputDto
        {
            Country = "DEU",
            SubtotalNetMinor = 100,
            ShipmentMass = 100
        };

        var result = new RateShipmentInputValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Country must be exactly 2 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Country));
    }

    [Fact]
    public void RateShipmentInput_Should_Fail_When_SubtotalNetMinor_Negative()
    {
        var dto = new RateShipmentInputDto
        {
            Country = "DE",
            SubtotalNetMinor = -1,
            ShipmentMass = 100
        };

        var result = new RateShipmentInputValidator().Validate(dto);

        result.IsValid.Should().BeFalse("SubtotalNetMinor must be >= 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SubtotalNetMinor));
    }

    [Fact]
    public void RateShipmentInput_Should_Fail_When_ShipmentMass_Negative()
    {
        var dto = new RateShipmentInputDto
        {
            Country = "DE",
            SubtotalNetMinor = 0,
            ShipmentMass = -1
        };

        var result = new RateShipmentInputValidator().Validate(dto);

        result.IsValid.Should().BeFalse("ShipmentMass must be >= 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ShipmentMass));
    }

    [Fact]
    public void RateShipmentInput_Should_Fail_When_Currency_Wrong_Length()
    {
        var dto = new RateShipmentInputDto
        {
            Country = "DE",
            SubtotalNetMinor = 100,
            ShipmentMass = 100,
            Currency = "EU"
        };

        var result = new RateShipmentInputValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Currency must be exactly 3 characters when provided");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Currency));
    }

    [Fact]
    public void RateShipmentInput_Should_Pass_When_Zero_Mass_And_Zero_Subtotal()
    {
        var dto = new RateShipmentInputDto
        {
            Country = "AT",
            SubtotalNetMinor = 0,
            ShipmentMass = 0
        };

        var result = new RateShipmentInputValidator().Validate(dto);

        result.IsValid.Should().BeTrue("zero mass and zero subtotal are at the valid lower boundary");
    }
}
