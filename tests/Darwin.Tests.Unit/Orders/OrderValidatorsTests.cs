using System;
using System.Collections.Generic;
using Darwin.Application;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Validators;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.Orders;

/// <summary>
/// Unit tests for all order-domain FluentValidation validators:
/// <see cref="OrderCreateValidator"/>, <see cref="OrderLineCreateValidator"/>,
/// <see cref="PaymentCreateValidator"/>, <see cref="ShipmentCreateValidator"/>,
/// <see cref="ShipmentLineCreateValidator"/>, <see cref="ApplyShipmentCarrierEventValidator"/>,
/// <see cref="UpdateOrderStatusValidator"/>, <see cref="RefundCreateValidator"/>,
/// and <see cref="OrderInvoiceCreateValidator"/>.
/// </summary>
public sealed class OrderValidatorsTests
{
    private static IStringLocalizer<ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        return mock.Object;
    }

    // ─── OrderCreateValidator ────────────────────────────────────────────────

    [Fact]
    public void OrderCreate_Should_Pass_For_Valid_Dto()
    {
        var dto = new OrderCreateDto
        {
            Currency = "EUR",
            BillingAddressJson = "{\"city\":\"Berlin\"}",
            ShippingAddressJson = "{\"city\":\"Berlin\"}",
            Lines = new List<OrderLineCreateDto>
            {
                new()
                {
                    VariantId = Guid.NewGuid(),
                    Name = "Widget",
                    Sku = "WGT-1",
                    Quantity = 1,
                    UnitPriceNetMinor = 1000,
                    VatRate = 0.19m
                }
            }
        };

        var result = new OrderCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a well-formed order create request should pass");
    }

    [Fact]
    public void OrderCreate_Should_Fail_When_Currency_Empty()
    {
        var dto = new OrderCreateDto
        {
            Currency = "",
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Lines = new List<OrderLineCreateDto>
            {
                new() { VariantId = Guid.NewGuid(), Name = "Item", Sku = "S", Quantity = 1, VatRate = 0m }
            }
        };

        var result = new OrderCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("currency is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Currency));
    }

    [Fact]
    public void OrderCreate_Should_Fail_When_Currency_Wrong_Length()
    {
        var dto = new OrderCreateDto
        {
            Currency = "EURO",
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Lines = new List<OrderLineCreateDto>
            {
                new() { VariantId = Guid.NewGuid(), Name = "Item", Sku = "S", Quantity = 1, VatRate = 0m }
            }
        };

        var result = new OrderCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("currency must be exactly 3 characters");
    }

    [Fact]
    public void OrderCreate_Should_Fail_When_BillingAddress_Empty()
    {
        var dto = new OrderCreateDto
        {
            Currency = "EUR",
            BillingAddressJson = "",
            ShippingAddressJson = "{}",
            Lines = new List<OrderLineCreateDto>
            {
                new() { VariantId = Guid.NewGuid(), Name = "Item", Sku = "S", Quantity = 1, VatRate = 0m }
            }
        };

        var result = new OrderCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("BillingAddressJson is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.BillingAddressJson));
    }

    [Fact]
    public void OrderCreate_Should_Fail_When_Lines_Empty()
    {
        var dto = new OrderCreateDto
        {
            Currency = "EUR",
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Lines = new List<OrderLineCreateDto>()
        };

        var result = new OrderCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("an order must have at least one line");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Lines));
    }

    [Fact]
    public void OrderCreate_Should_Fail_When_ShippingTotal_Negative()
    {
        var dto = new OrderCreateDto
        {
            Currency = "EUR",
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            ShippingTotalMinor = -1,
            Lines = new List<OrderLineCreateDto>
            {
                new() { VariantId = Guid.NewGuid(), Name = "Item", Sku = "S", Quantity = 1, VatRate = 0m }
            }
        };

        var result = new OrderCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("ShippingTotalMinor must be >= 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ShippingTotalMinor));
    }

    [Fact]
    public void OrderCreate_Should_Fail_When_DiscountTotal_Negative()
    {
        var dto = new OrderCreateDto
        {
            Currency = "EUR",
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            DiscountTotalMinor = -100,
            Lines = new List<OrderLineCreateDto>
            {
                new() { VariantId = Guid.NewGuid(), Name = "Item", Sku = "S", Quantity = 1, VatRate = 0m }
            }
        };

        var result = new OrderCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("DiscountTotalMinor must be >= 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DiscountTotalMinor));
    }

    // ─── OrderLineCreateValidator ────────────────────────────────────────────

    [Fact]
    public void OrderLineCreate_Should_Pass_For_Valid_Line()
    {
        var dto = new OrderLineCreateDto
        {
            VariantId = Guid.NewGuid(),
            Name = "Widget",
            Sku = "WGT-1",
            Quantity = 2,
            UnitPriceNetMinor = 500,
            VatRate = 0.19m
        };

        var result = new OrderLineCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a fully valid order line should pass");
    }

    [Fact]
    public void OrderLineCreate_Should_Fail_When_VariantId_Empty()
    {
        var dto = new OrderLineCreateDto
        {
            VariantId = Guid.Empty,
            Name = "Widget",
            Sku = "WGT-1",
            Quantity = 1,
            VatRate = 0m
        };

        var result = new OrderLineCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("VariantId must not be empty");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.VariantId));
    }

    [Fact]
    public void OrderLineCreate_Should_Fail_When_Name_Empty()
    {
        var dto = new OrderLineCreateDto
        {
            VariantId = Guid.NewGuid(),
            Name = "",
            Sku = "WGT-1",
            Quantity = 1,
            VatRate = 0m
        };

        var result = new OrderLineCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Name is required for an order line");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Name));
    }

    [Fact]
    public void OrderLineCreate_Should_Fail_When_Name_Too_Long()
    {
        var dto = new OrderLineCreateDto
        {
            VariantId = Guid.NewGuid(),
            Name = new string('X', 513),
            Sku = "WGT-1",
            Quantity = 1,
            VatRate = 0m
        };

        var result = new OrderLineCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Name must not exceed 512 characters");
    }

    [Fact]
    public void OrderLineCreate_Should_Fail_When_Sku_Empty()
    {
        var dto = new OrderLineCreateDto
        {
            VariantId = Guid.NewGuid(),
            Name = "Widget",
            Sku = "",
            Quantity = 1,
            VatRate = 0m
        };

        var result = new OrderLineCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Sku is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Sku));
    }

    [Fact]
    public void OrderLineCreate_Should_Fail_When_Quantity_Zero()
    {
        var dto = new OrderLineCreateDto
        {
            VariantId = Guid.NewGuid(),
            Name = "Widget",
            Sku = "WGT-1",
            Quantity = 0,
            VatRate = 0m
        };

        var result = new OrderLineCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Quantity must be greater than 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Quantity));
    }

    [Fact]
    public void OrderLineCreate_Should_Fail_When_UnitPriceNetMinor_Negative()
    {
        var dto = new OrderLineCreateDto
        {
            VariantId = Guid.NewGuid(),
            Name = "Widget",
            Sku = "WGT-1",
            Quantity = 1,
            UnitPriceNetMinor = -1,
            VatRate = 0m
        };

        var result = new OrderLineCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("UnitPriceNetMinor must be >= 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.UnitPriceNetMinor));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(2.0)]
    public void OrderLineCreate_Should_Fail_When_VatRate_Out_Of_Range(double rate)
    {
        var dto = new OrderLineCreateDto
        {
            VariantId = Guid.NewGuid(),
            Name = "Widget",
            Sku = "WGT-1",
            Quantity = 1,
            VatRate = (decimal)rate
        };

        var result = new OrderLineCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse($"VatRate {rate} is outside [0, 1]");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.VatRate));
    }

    [Fact]
    public void OrderLineCreate_Should_Fail_When_WarehouseId_Is_Empty_Guid()
    {
        var dto = new OrderLineCreateDto
        {
            VariantId = Guid.NewGuid(),
            Name = "Widget",
            Sku = "WGT-1",
            Quantity = 1,
            VatRate = 0m,
            WarehouseId = Guid.Empty
        };

        var result = new OrderLineCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("an empty Guid for WarehouseId is invalid; use null to omit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.WarehouseId));
    }

    [Fact]
    public void OrderLineCreate_Should_Pass_When_WarehouseId_Is_Null()
    {
        var dto = new OrderLineCreateDto
        {
            VariantId = Guid.NewGuid(),
            Name = "Widget",
            Sku = "WGT-1",
            Quantity = 1,
            VatRate = 0m,
            WarehouseId = null
        };

        var result = new OrderLineCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("null WarehouseId is acceptable");
    }

    // ─── PaymentCreateValidator ──────────────────────────────────────────────

    [Fact]
    public void PaymentCreate_Should_Pass_For_Successful_Payment()
    {
        var dto = new PaymentCreateDto
        {
            OrderId = Guid.NewGuid(),
            Provider = "Stripe",
            ProviderReference = "pi_test_123",
            AmountMinor = 9900,
            Currency = "EUR",
            Status = PaymentStatus.Captured
        };

        var result = new PaymentCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a fully populated successful payment should pass");
    }

    [Fact]
    public void PaymentCreate_Should_Pass_For_Failed_Payment_With_Reason()
    {
        var dto = new PaymentCreateDto
        {
            OrderId = Guid.NewGuid(),
            Provider = "Stripe",
            ProviderReference = "pi_fail_456",
            AmountMinor = 5000,
            Currency = "EUR",
            Status = PaymentStatus.Failed,
            FailureReason = "insufficient_funds"
        };

        var result = new PaymentCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a failed payment with a failure reason should pass");
    }

    [Fact]
    public void PaymentCreate_Should_Fail_When_Status_Is_Failed_But_No_Reason()
    {
        var dto = new PaymentCreateDto
        {
            OrderId = Guid.NewGuid(),
            Provider = "Stripe",
            ProviderReference = "pi_fail_789",
            AmountMinor = 5000,
            Currency = "EUR",
            Status = PaymentStatus.Failed,
            FailureReason = null
        };

        var result = new PaymentCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("FailureReason is required when Status == Failed");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.FailureReason));
    }

    [Fact]
    public void PaymentCreate_Should_Fail_When_OrderId_Empty()
    {
        var dto = new PaymentCreateDto
        {
            OrderId = Guid.Empty,
            Provider = "Stripe",
            ProviderReference = "ref",
            AmountMinor = 100,
            Currency = "EUR",
            Status = PaymentStatus.Captured
        };

        var result = new PaymentCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("OrderId must not be empty");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.OrderId));
    }

    [Fact]
    public void PaymentCreate_Should_Fail_When_AmountMinor_Zero()
    {
        var dto = new PaymentCreateDto
        {
            OrderId = Guid.NewGuid(),
            Provider = "Stripe",
            ProviderReference = "ref",
            AmountMinor = 0,
            Currency = "EUR",
            Status = PaymentStatus.Captured
        };

        var result = new PaymentCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("AmountMinor must be greater than 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.AmountMinor));
    }

    [Fact]
    public void PaymentCreate_Should_Fail_When_Currency_Wrong_Length()
    {
        var dto = new PaymentCreateDto
        {
            OrderId = Guid.NewGuid(),
            Provider = "Stripe",
            ProviderReference = "ref",
            AmountMinor = 100,
            Currency = "EU",
            Status = PaymentStatus.Captured
        };

        var result = new PaymentCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Currency must be exactly 3 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Currency));
    }

    [Fact]
    public void PaymentCreate_Should_Fail_When_Provider_Empty()
    {
        var dto = new PaymentCreateDto
        {
            OrderId = Guid.NewGuid(),
            Provider = "",
            ProviderReference = "ref",
            AmountMinor = 100,
            Currency = "EUR",
            Status = PaymentStatus.Captured
        };

        var result = new PaymentCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Provider is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Provider));
    }

    [Fact]
    public void PaymentCreate_Should_Fail_When_ProviderReference_Empty()
    {
        var dto = new PaymentCreateDto
        {
            OrderId = Guid.NewGuid(),
            Provider = "Stripe",
            ProviderReference = "",
            AmountMinor = 100,
            Currency = "EUR",
            Status = PaymentStatus.Captured
        };

        var result = new PaymentCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("ProviderReference is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ProviderReference));
    }

    // ─── ShipmentCreateValidator ─────────────────────────────────────────────

    [Fact]
    public void ShipmentCreate_Should_Pass_For_Valid_Dto()
    {
        var dto = new ShipmentCreateDto
        {
            OrderId = Guid.NewGuid(),
            Carrier = "DHL",
            Service = "Parcel",
            Lines = new List<ShipmentLineCreateDto>
            {
                new() { OrderLineId = Guid.NewGuid(), Quantity = 1 }
            }
        };

        var result = new ShipmentCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a valid shipment create request should pass");
    }

    [Fact]
    public void ShipmentCreate_Should_Pass_When_Lines_Empty()
    {
        // Lines is not required by ShipmentCreateValidator
        var dto = new ShipmentCreateDto
        {
            OrderId = Guid.NewGuid(),
            Carrier = "DHL",
            Service = "Parcel",
            Lines = new List<ShipmentLineCreateDto>()
        };

        var result = new ShipmentCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("no Lines constraint is enforced at this validator level");
    }

    [Fact]
    public void ShipmentCreate_Should_Fail_When_OrderId_Empty()
    {
        var dto = new ShipmentCreateDto
        {
            OrderId = Guid.Empty,
            Carrier = "DHL",
            Service = "Parcel"
        };

        var result = new ShipmentCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("OrderId must not be empty");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.OrderId));
    }

    [Fact]
    public void ShipmentCreate_Should_Fail_When_Carrier_Empty()
    {
        var dto = new ShipmentCreateDto
        {
            OrderId = Guid.NewGuid(),
            Carrier = "",
            Service = "Parcel"
        };

        var result = new ShipmentCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Carrier is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Carrier));
    }

    [Fact]
    public void ShipmentCreate_Should_Fail_When_Service_Empty()
    {
        var dto = new ShipmentCreateDto
        {
            OrderId = Guid.NewGuid(),
            Carrier = "DHL",
            Service = ""
        };

        var result = new ShipmentCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Service is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Service));
    }

    // ─── ShipmentLineCreateValidator ─────────────────────────────────────────

    [Fact]
    public void ShipmentLineCreate_Should_Pass_For_Valid_Line()
    {
        var dto = new ShipmentLineCreateDto
        {
            OrderLineId = Guid.NewGuid(),
            Quantity = 2
        };

        var result = new ShipmentLineCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a valid shipment line should pass");
    }

    [Fact]
    public void ShipmentLineCreate_Should_Fail_When_OrderLineId_Empty()
    {
        var dto = new ShipmentLineCreateDto
        {
            OrderLineId = Guid.Empty,
            Quantity = 1
        };

        var result = new ShipmentLineCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("OrderLineId must not be empty");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.OrderLineId));
    }

    [Fact]
    public void ShipmentLineCreate_Should_Fail_When_Quantity_Zero()
    {
        var dto = new ShipmentLineCreateDto
        {
            OrderLineId = Guid.NewGuid(),
            Quantity = 0
        };

        var result = new ShipmentLineCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Quantity must be greater than 0 for a shipment line");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Quantity));
    }

    // ─── ApplyShipmentCarrierEventValidator ──────────────────────────────────

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Pass_For_Valid_Dto()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a valid carrier event should pass");
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_Carrier_Empty()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "",
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Carrier is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Carrier));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_Carrier_Is_Whitespace()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "   ",
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Carrier with whitespace only should be rejected");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Carrier));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_ProviderShipmentReference_Is_Whitespace()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "   ",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("ProviderShipmentReference with whitespace only should be rejected");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ProviderShipmentReference));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_CarrierEventKey_Is_Whitespace()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = "   ",
            OccurredAtUtc = DateTime.UtcNow
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("CarrierEventKey with whitespace only should be rejected");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.CarrierEventKey));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_Carrier_Is_Null()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = null!,
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Carrier is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Carrier));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_ProviderShipmentReference_Is_Null()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = null!,
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("ProviderShipmentReference is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ProviderShipmentReference));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_CarrierEventKey_Is_Null_Or_Whitespace(string? carrierEventKey)
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = carrierEventKey!,
            OccurredAtUtc = DateTime.UtcNow
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("CarrierEventKey is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.CarrierEventKey));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Pass_When_Optional_Fields_Are_Empty()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow,
            TrackingNumber = string.Empty,
            LabelUrl = string.Empty,
            Service = string.Empty,
            ProviderStatus = string.Empty,
            ExceptionCode = string.Empty,
            ExceptionMessage = string.Empty
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("empty optional fields should be treated as absent");
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Pass_When_Optional_Fields_Are_Null()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow,
            TrackingNumber = null!,
            LabelUrl = null!,
            Service = null!,
            ProviderStatus = null!,
            ExceptionCode = null!,
            ExceptionMessage = null!
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("null optional fields should be treated as absent");
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_ProviderShipmentReference_Empty()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("ProviderShipmentReference is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ProviderShipmentReference));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_CarrierEventKey_Empty()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "REF-001",
            CarrierEventKey = "",
            OccurredAtUtc = DateTime.UtcNow
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("CarrierEventKey is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.CarrierEventKey));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_OccurredAtUtc_Is_Default()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "REF-001",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = default
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("OccurredAtUtc must be set (not default DateTime)");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.OccurredAtUtc));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_With_Multiple_Missing_Required_Fields()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "   ",
            ProviderShipmentReference = "",
            CarrierEventKey = null!,
            OccurredAtUtc = default
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("multiple required fields are missing");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Carrier));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ProviderShipmentReference));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.CarrierEventKey));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.OccurredAtUtc));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_Multiple_Optional_Fields_Too_Long()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow,
            TrackingNumber = new string('T', 129),
            LabelUrl = new string('L', 2049),
            Service = new string('S', 65),
            ProviderStatus = new string('P', 65),
            ExceptionCode = new string('E', 129),
            ExceptionMessage = new string('M', 513)
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("multiple optional fields exceeding limits should fail");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.TrackingNumber));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.LabelUrl));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Service));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ProviderStatus));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ExceptionCode));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ExceptionMessage));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_Optional_Fields_Exceed_Max_Length_Because_Of_Whitespace_Padding()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "REF-001",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow,
            TrackingNumber = $" {new string('T', 128)} ",
            LabelUrl = $" {new string('L', 2048)} ",
            Service = $" {new string('S', 64)} ",
            ProviderStatus = $" {new string('P', 64)} ",
            ExceptionCode = $" {new string('E', 128)} ",
            ExceptionMessage = $" {new string('M', 512)} "
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("whitespace-padded fields should fail validator length checks");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.TrackingNumber));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.LabelUrl));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Service));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ProviderStatus));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ExceptionCode));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ExceptionMessage));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Pass_With_Optional_Fields_Set()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "REF-001",
            CarrierEventKey = "shipment.exception",
            OccurredAtUtc = DateTime.UtcNow,
            TrackingNumber = "1Z999AA10123456784",
            LabelUrl = "https://labels.example.com/label.pdf",
            Service = "Express",
            ProviderStatus = "TRANSIT",
            ExceptionCode = "EX01",
            ExceptionMessage = "Delivery attempted"
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("all optional fields within limits should pass");
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_Carrier_TooLong()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = new string('D', 65),
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Carrier is limited to 64 chars");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Carrier));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_ProviderShipmentReference_TooLong()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = new string('R', 129),
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("ProviderShipmentReference is limited to 128 chars");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ProviderShipmentReference));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_CarrierEventKey_TooLong()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = new string('K', 129),
            OccurredAtUtc = DateTime.UtcNow
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("CarrierEventKey is limited to 128 chars");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.CarrierEventKey));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_TrackingNumber_TooLong()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow,
            TrackingNumber = new string('T', 129)
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("TrackingNumber is limited to 128 chars when set");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.TrackingNumber));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_LabelUrl_TooLong()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow,
            LabelUrl = new string('L', 2049)
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("LabelUrl is limited to 2048 chars when set");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.LabelUrl));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_Service_TooLong()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow,
            Service = new string('S', 65)
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Service is limited to 64 chars when set");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Service));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_ProviderStatus_TooLong()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow,
            ProviderStatus = new string('P', 65)
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("ProviderStatus is limited to 64 chars when set");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ProviderStatus));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_ExceptionCode_TooLong()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow,
            ExceptionCode = new string('E', 129)
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("ExceptionCode is limited to 128 chars when set");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ExceptionCode));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_ExceptionMessage_TooLong()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow,
            ExceptionMessage = new string('X', 513)
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("ExceptionMessage is limited to 512 chars when set");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ExceptionMessage));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Pass_When_Optional_Fields_Are_Whitespace()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = "shipment.delivered",
            OccurredAtUtc = DateTime.UtcNow,
            TrackingNumber = "   ",
            LabelUrl = "\t",
            Service = "\r\n",
            ProviderStatus = "   ",
            ExceptionCode = "   ",
            ExceptionMessage = "   "
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("whitespace-only optional fields should be treated as absent");
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Pass_When_Optional_Fields_Are_At_MaxLength()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = new string('D', 64),
            ProviderShipmentReference = new string('R', 128),
            CarrierEventKey = new string('K', 128),
            OccurredAtUtc = DateTime.UtcNow,
            TrackingNumber = new string('T', 128),
            LabelUrl = new string('L', 2048),
            Service = new string('S', 64),
            ProviderStatus = new string('P', 64),
            ExceptionCode = new string('E', 128),
            ExceptionMessage = new string('M', 512)
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("all fields at exact max length should pass");
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Pass_When_CarrierEventKey_Is_Max_Length()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = new string('K', 128),
            OccurredAtUtc = DateTime.UtcNow
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("CarrierEventKey at exact max length is valid");
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Pass_When_Required_Fields_Are_At_Max_Length()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = new string('C', 64),
            ProviderShipmentReference = new string('R', 128),
            CarrierEventKey = new string('K', 128),
            OccurredAtUtc = DateTime.UtcNow
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("all required fields at exact max length should pass");
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_Required_Fields_With_Whitespace_Padding_Exceed_Max_Length()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = $" {new string('C', 64)} ",
            ProviderShipmentReference = $" {new string('R', 128)} ",
            CarrierEventKey = "shipment.created",
            OccurredAtUtc = DateTime.UtcNow
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("whitespace padding must be included in validator length checks");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Carrier));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ProviderShipmentReference));
    }

    [Fact]
    public void ApplyShipmentCarrierEvent_Should_Fail_When_CarrierEventKey_With_Whitespace_Padding_Exceed_Max_Length()
    {
        var dto = new ApplyShipmentCarrierEventDto
        {
            Carrier = "DHL",
            ProviderShipmentReference = "DHLREF-001",
            CarrierEventKey = $" {new string('K', 128)} ",
            OccurredAtUtc = DateTime.UtcNow
        };

        var result = new ApplyShipmentCarrierEventValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("CarrierEventKey must enforce maximum length before trimming");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.CarrierEventKey));
    }

    // ─── UpdateOrderStatusValidator ──────────────────────────────────────────

    [Fact]
    public void UpdateOrderStatus_Should_Pass_For_Valid_Dto()
    {
        var dto = new UpdateOrderStatusDto
        {
            OrderId = Guid.NewGuid(),
            RowVersion = new byte[] { 1, 2, 3 },
            NewStatus = OrderStatus.Paid
        };

        var result = new UpdateOrderStatusValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a valid status update should pass");
    }

    [Fact]
    public void UpdateOrderStatus_Should_Fail_When_OrderId_Empty()
    {
        var dto = new UpdateOrderStatusDto
        {
            OrderId = Guid.Empty,
            RowVersion = new byte[] { 1 },
            NewStatus = OrderStatus.Paid
        };

        var result = new UpdateOrderStatusValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("OrderId must not be empty");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.OrderId));
    }

    [Fact]
    public void UpdateOrderStatus_Should_Fail_When_RowVersion_Null()
    {
        var dto = new UpdateOrderStatusDto
        {
            OrderId = Guid.NewGuid(),
            RowVersion = null!,
            NewStatus = OrderStatus.Paid
        };

        var result = new UpdateOrderStatusValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("RowVersion must not be null");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RowVersion));
    }

    [Fact]
    public void UpdateOrderStatus_Should_Fail_When_WarehouseId_Is_Empty_Guid()
    {
        var dto = new UpdateOrderStatusDto
        {
            OrderId = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            NewStatus = OrderStatus.Paid,
            WarehouseId = Guid.Empty
        };

        var result = new UpdateOrderStatusValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("an empty Guid for WarehouseId is invalid; use null to omit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.WarehouseId));
    }

    [Fact]
    public void UpdateOrderStatus_Should_Pass_When_WarehouseId_Is_Null()
    {
        var dto = new UpdateOrderStatusDto
        {
            OrderId = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            NewStatus = OrderStatus.Paid,
            WarehouseId = null
        };

        var result = new UpdateOrderStatusValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("null WarehouseId is acceptable");
    }

    // ─── RefundCreateValidator ───────────────────────────────────────────────

    [Fact]
    public void RefundCreate_Should_Pass_For_Valid_Dto()
    {
        var dto = new RefundCreateDto
        {
            OrderId = Guid.NewGuid(),
            PaymentId = Guid.NewGuid(),
            AmountMinor = 1000,
            Currency = "EUR",
            Reason = "Customer request"
        };

        var result = new RefundCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a fully valid refund should pass");
    }

    [Fact]
    public void RefundCreate_Should_Fail_When_OrderId_Empty()
    {
        var dto = new RefundCreateDto
        {
            OrderId = Guid.Empty,
            PaymentId = Guid.NewGuid(),
            AmountMinor = 100,
            Currency = "EUR",
            Reason = "Test"
        };

        var result = new RefundCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("OrderId is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.OrderId));
    }

    [Fact]
    public void RefundCreate_Should_Fail_When_PaymentId_Empty()
    {
        var dto = new RefundCreateDto
        {
            OrderId = Guid.NewGuid(),
            PaymentId = Guid.Empty,
            AmountMinor = 100,
            Currency = "EUR",
            Reason = "Test"
        };

        var result = new RefundCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("PaymentId is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.PaymentId));
    }

    [Fact]
    public void RefundCreate_Should_Fail_When_AmountMinor_Zero()
    {
        var dto = new RefundCreateDto
        {
            OrderId = Guid.NewGuid(),
            PaymentId = Guid.NewGuid(),
            AmountMinor = 0,
            Currency = "EUR",
            Reason = "Test"
        };

        var result = new RefundCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("AmountMinor must be > 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.AmountMinor));
    }

    [Fact]
    public void RefundCreate_Should_Fail_When_Currency_Wrong_Length()
    {
        var dto = new RefundCreateDto
        {
            OrderId = Guid.NewGuid(),
            PaymentId = Guid.NewGuid(),
            AmountMinor = 100,
            Currency = "EURO",
            Reason = "Test"
        };

        var result = new RefundCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Currency must be exactly 3 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Currency));
    }

    [Fact]
    public void RefundCreate_Should_Fail_When_Reason_Empty()
    {
        var dto = new RefundCreateDto
        {
            OrderId = Guid.NewGuid(),
            PaymentId = Guid.NewGuid(),
            AmountMinor = 100,
            Currency = "EUR",
            Reason = ""
        };

        var result = new RefundCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Reason is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Reason));
    }

    [Fact]
    public void RefundCreate_Should_Fail_When_Reason_Too_Long()
    {
        var dto = new RefundCreateDto
        {
            OrderId = Guid.NewGuid(),
            PaymentId = Guid.NewGuid(),
            AmountMinor = 100,
            Currency = "EUR",
            Reason = new string('R', 257)
        };

        var result = new RefundCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Reason must not exceed 256 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Reason));
    }

    // ─── OrderInvoiceCreateValidator ─────────────────────────────────────────

    [Fact]
    public void OrderInvoiceCreate_Should_Pass_For_Minimal_Valid_Dto()
    {
        var dto = new OrderInvoiceCreateDto
        {
            OrderId = Guid.NewGuid()
        };

        var result = new OrderInvoiceCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("only OrderId is required; all optional ids may be null");
    }

    [Fact]
    public void OrderInvoiceCreate_Should_Pass_With_All_Optional_Ids()
    {
        var dto = new OrderInvoiceCreateDto
        {
            OrderId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            PaymentId = Guid.NewGuid()
        };

        var result = new OrderInvoiceCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("all optional ids populated with valid Guids should pass");
    }

    [Fact]
    public void OrderInvoiceCreate_Should_Fail_When_OrderId_Empty()
    {
        var dto = new OrderInvoiceCreateDto
        {
            OrderId = Guid.Empty
        };

        var result = new OrderInvoiceCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("OrderId must not be empty");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.OrderId));
    }

    [Fact]
    public void OrderInvoiceCreate_Should_Fail_When_BusinessId_Is_Empty_Guid()
    {
        var dto = new OrderInvoiceCreateDto
        {
            OrderId = Guid.NewGuid(),
            BusinessId = Guid.Empty
        };

        var result = new OrderInvoiceCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("an empty Guid for BusinessId is invalid; use null to omit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.BusinessId));
    }

    [Fact]
    public void OrderInvoiceCreate_Should_Fail_When_CustomerId_Is_Empty_Guid()
    {
        var dto = new OrderInvoiceCreateDto
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.Empty
        };

        var result = new OrderInvoiceCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("an empty Guid for CustomerId is invalid; use null to omit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.CustomerId));
    }

    [Fact]
    public void OrderInvoiceCreate_Should_Fail_When_PaymentId_Is_Empty_Guid()
    {
        var dto = new OrderInvoiceCreateDto
        {
            OrderId = Guid.NewGuid(),
            PaymentId = Guid.Empty
        };

        var result = new OrderInvoiceCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("an empty Guid for PaymentId is invalid; use null to omit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.PaymentId));
    }
}
