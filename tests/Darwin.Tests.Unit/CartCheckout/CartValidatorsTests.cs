using System;
using Darwin.Application;
using Darwin.Application.CartCheckout.DTOs;
using Darwin.Application.CartCheckout.Validators;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.CartCheckout;

/// <summary>
/// Unit tests for cart input validators: <see cref="CartKeyValidator"/>,
/// <see cref="CartAddItemValidator"/>, <see cref="CartUpdateQtyValidator"/>,
/// <see cref="CartRemoveItemValidator"/>, and <see cref="CartApplyCouponValidator"/>.
/// </summary>
public sealed class CartValidatorsTests
{
    private static IStringLocalizer<ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        return mock.Object;
    }

    // ─── CartKeyValidator ────────────────────────────────────────────────────

    [Fact]
    public void CartKey_Should_Pass_When_UserId_Provided()
    {
        var dto = new CartKeyDto { UserId = Guid.NewGuid() };

        var result = new CartKeyValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("UserId alone satisfies the identity requirement");
    }

    [Fact]
    public void CartKey_Should_Pass_When_AnonymousId_Provided()
    {
        var dto = new CartKeyDto { AnonymousId = "anon-abc" };

        var result = new CartKeyValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("AnonymousId alone satisfies the identity requirement");
    }

    [Fact]
    public void CartKey_Should_Fail_When_Neither_UserId_Nor_AnonymousId_Provided()
    {
        var dto = new CartKeyDto { UserId = null, AnonymousId = null };

        var result = new CartKeyValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("at least one of UserId or AnonymousId must be set");
    }

    [Fact]
    public void CartKey_Should_Fail_When_AnonymousId_Is_Whitespace()
    {
        var dto = new CartKeyDto { UserId = null, AnonymousId = "   " };

        var result = new CartKeyValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("a whitespace-only AnonymousId should not satisfy the rule");
    }

    // ─── CartAddItemValidator ────────────────────────────────────────────────

    [Fact]
    public void CartAddItem_Should_Pass_For_Valid_Dto_With_UserId()
    {
        var dto = new CartAddItemDto
        {
            UserId = Guid.NewGuid(),
            VariantId = Guid.NewGuid(),
            Quantity = 2
        };

        var result = new CartAddItemValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a fully valid add-item request should pass");
    }

    [Fact]
    public void CartAddItem_Should_Pass_For_Valid_Dto_With_AnonymousId()
    {
        var dto = new CartAddItemDto
        {
            AnonymousId = "anon-xyz",
            VariantId = Guid.NewGuid(),
            Quantity = 1
        };

        var result = new CartAddItemValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("AnonymousId satisfies the identity requirement");
    }

    [Fact]
    public void CartAddItem_Should_Fail_When_VariantId_Is_Empty()
    {
        var dto = new CartAddItemDto
        {
            UserId = Guid.NewGuid(),
            VariantId = Guid.Empty,
            Quantity = 1
        };

        var result = new CartAddItemValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("VariantId must not be empty");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.VariantId));
    }

    [Fact]
    public void CartAddItem_Should_Fail_When_Quantity_Is_Zero()
    {
        var dto = new CartAddItemDto
        {
            UserId = Guid.NewGuid(),
            VariantId = Guid.NewGuid(),
            Quantity = 0
        };

        var result = new CartAddItemValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Quantity must be greater than 0 when adding an item");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Quantity));
    }

    [Fact]
    public void CartAddItem_Should_Fail_When_Quantity_Is_Negative()
    {
        var dto = new CartAddItemDto
        {
            UserId = Guid.NewGuid(),
            VariantId = Guid.NewGuid(),
            Quantity = -1
        };

        var result = new CartAddItemValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("a negative quantity is invalid");
    }

    [Fact]
    public void CartAddItem_Should_Fail_When_No_Identity_Provided()
    {
        var dto = new CartAddItemDto
        {
            UserId = null,
            AnonymousId = null,
            VariantId = Guid.NewGuid(),
            Quantity = 1
        };

        var result = new CartAddItemValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("at least one identity (UserId or AnonymousId) must be provided");
    }

    // ─── CartUpdateQtyValidator ──────────────────────────────────────────────

    [Fact]
    public void CartUpdateQty_Should_Pass_For_Valid_Positive_Quantity()
    {
        var dto = new CartUpdateQtyDto
        {
            CartId = Guid.NewGuid(),
            VariantId = Guid.NewGuid(),
            Quantity = 3
        };

        var result = new CartUpdateQtyValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a positive quantity update is valid");
    }

    [Fact]
    public void CartUpdateQty_Should_Pass_When_Quantity_Is_Zero()
    {
        var dto = new CartUpdateQtyDto
        {
            CartId = Guid.NewGuid(),
            VariantId = Guid.NewGuid(),
            Quantity = 0  // 0 means remove the line
        };

        var result = new CartUpdateQtyValidator().Validate(dto);

        result.IsValid.Should().BeTrue("zero quantity is allowed (line removal signal)");
    }

    [Fact]
    public void CartUpdateQty_Should_Fail_When_CartId_Is_Empty()
    {
        var dto = new CartUpdateQtyDto
        {
            CartId = Guid.Empty,
            VariantId = Guid.NewGuid(),
            Quantity = 1
        };

        var result = new CartUpdateQtyValidator().Validate(dto);

        result.IsValid.Should().BeFalse("CartId must not be empty");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.CartId));
    }

    [Fact]
    public void CartUpdateQty_Should_Fail_When_VariantId_Is_Empty()
    {
        var dto = new CartUpdateQtyDto
        {
            CartId = Guid.NewGuid(),
            VariantId = Guid.Empty,
            Quantity = 1
        };

        var result = new CartUpdateQtyValidator().Validate(dto);

        result.IsValid.Should().BeFalse("VariantId must not be empty");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.VariantId));
    }

    [Fact]
    public void CartUpdateQty_Should_Fail_When_Quantity_Is_Negative()
    {
        var dto = new CartUpdateQtyDto
        {
            CartId = Guid.NewGuid(),
            VariantId = Guid.NewGuid(),
            Quantity = -5
        };

        var result = new CartUpdateQtyValidator().Validate(dto);

        result.IsValid.Should().BeFalse("quantity must be >= 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Quantity));
    }

    // ─── CartRemoveItemValidator ─────────────────────────────────────────────

    [Fact]
    public void CartRemoveItem_Should_Pass_For_Valid_Dto()
    {
        var dto = new CartRemoveItemDto
        {
            CartId = Guid.NewGuid(),
            VariantId = Guid.NewGuid()
        };

        var result = new CartRemoveItemValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a fully valid remove-item request should pass");
    }

    [Fact]
    public void CartRemoveItem_Should_Fail_When_CartId_Is_Empty()
    {
        var dto = new CartRemoveItemDto
        {
            CartId = Guid.Empty,
            VariantId = Guid.NewGuid()
        };

        var result = new CartRemoveItemValidator().Validate(dto);

        result.IsValid.Should().BeFalse("CartId is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.CartId));
    }

    [Fact]
    public void CartRemoveItem_Should_Fail_When_VariantId_Is_Empty()
    {
        var dto = new CartRemoveItemDto
        {
            CartId = Guid.NewGuid(),
            VariantId = Guid.Empty
        };

        var result = new CartRemoveItemValidator().Validate(dto);

        result.IsValid.Should().BeFalse("VariantId is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.VariantId));
    }

    // ─── CartApplyCouponValidator ────────────────────────────────────────────

    [Fact]
    public void CartApplyCoupon_Should_Pass_With_Valid_CartId()
    {
        var dto = new CartApplyCouponDto
        {
            CartId = Guid.NewGuid(),
            CouponCode = "SUMMER10"
        };

        var result = new CartApplyCouponValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a request with a valid CartId should pass");
    }

    [Fact]
    public void CartApplyCoupon_Should_Pass_When_CouponCode_Is_Null()
    {
        var dto = new CartApplyCouponDto
        {
            CartId = Guid.NewGuid(),
            CouponCode = null
        };

        var result = new CartApplyCouponValidator().Validate(dto);

        result.IsValid.Should().BeTrue("null CouponCode (clear intent) is allowed by the validator");
    }

    [Fact]
    public void CartApplyCoupon_Should_Fail_When_CartId_Is_Empty()
    {
        var dto = new CartApplyCouponDto
        {
            CartId = Guid.Empty,
            CouponCode = "SOME_CODE"
        };

        var result = new CartApplyCouponValidator().Validate(dto);

        result.IsValid.Should().BeFalse("CartId must not be empty");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.CartId));
    }
}
