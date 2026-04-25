using System;
using Darwin.Application.Pricing.DTOs;
using Darwin.Application.Pricing.Validators;
using Darwin.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Darwin.Tests.Unit.Pricing;

/// <summary>
/// Unit tests for <see cref="PromotionCreateValidator"/>, <see cref="PromotionEditValidator"/>,
/// and <see cref="ValidateCouponInputValidator"/>.
/// These validators guard the pricing mutation endpoints against invalid payloads.
/// </summary>
public sealed class PromotionValidatorTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // PromotionCreateValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void PromotionCreate_Should_Pass_For_Valid_Percentage_Promotion()
    {
        var dto = new PromotionCreateDto
        {
            Name = "Summer Sale",
            Currency = "EUR",
            Type = PromotionType.Percentage,
            Percent = 20m
        };

        var result = new PromotionCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a well-formed percentage promotion should pass validation");
    }

    [Fact]
    public void PromotionCreate_Should_Pass_For_Valid_Amount_Promotion()
    {
        var dto = new PromotionCreateDto
        {
            Name = "€5 Off",
            Currency = "EUR",
            Type = PromotionType.Amount,
            AmountMinor = 500
        };

        var result = new PromotionCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a well-formed fixed-amount promotion should pass validation");
    }

    [Fact]
    public void PromotionCreate_Should_Fail_When_Name_Empty()
    {
        var dto = new PromotionCreateDto { Name = "", Currency = "EUR" };

        var result = new PromotionCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("promotion name is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Name));
    }

    [Fact]
    public void PromotionCreate_Should_Fail_When_Name_Too_Long()
    {
        var dto = new PromotionCreateDto { Name = new string('A', 257), Currency = "EUR" };

        var result = new PromotionCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("promotion name must not exceed 256 characters");
    }

    [Fact]
    public void PromotionCreate_Should_Fail_When_Currency_Wrong_Length()
    {
        var dto = new PromotionCreateDto { Name = "Test", Currency = "EURO" };

        var result = new PromotionCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("currency must be exactly 3 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Currency));
    }

    [Fact]
    public void PromotionCreate_Should_Fail_When_Currency_Empty()
    {
        var dto = new PromotionCreateDto { Name = "Test", Currency = "" };

        var result = new PromotionCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("currency is required");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    [InlineData(200)]
    public void PromotionCreate_Should_Fail_When_Percent_Out_Of_Range(double percent)
    {
        var dto = new PromotionCreateDto
        {
            Name = "Bad Pct",
            Currency = "EUR",
            Type = PromotionType.Percentage,
            Percent = (decimal)percent
        };

        var result = new PromotionCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse($"percent {percent} is outside [0, 100]");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(50.5)]
    public void PromotionCreate_Should_Pass_When_Percent_In_Range(double percent)
    {
        var dto = new PromotionCreateDto
        {
            Name = "Good Pct",
            Currency = "EUR",
            Type = PromotionType.Percentage,
            Percent = (decimal)percent
        };

        var result = new PromotionCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue($"percent {percent} is within [0, 100]");
    }

    [Fact]
    public void PromotionCreate_Should_Fail_When_AmountMinor_Zero_For_Amount_Type()
    {
        var dto = new PromotionCreateDto
        {
            Name = "Bad Amount",
            Currency = "EUR",
            Type = PromotionType.Amount,
            AmountMinor = 0
        };

        var result = new PromotionCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("amount promotion requires a positive AmountMinor");
    }

    [Fact]
    public void PromotionCreate_Should_Fail_When_Code_Has_Invalid_Characters()
    {
        var dto = new PromotionCreateDto
        {
            Name = "Bad Code",
            Currency = "EUR",
            Code = "INVAL!D CODE"
        };

        var result = new PromotionCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("coupon code must match [A-Za-z0-9_-]+");
    }

    [Fact]
    public void PromotionCreate_Should_Pass_When_Code_Valid()
    {
        var dto = new PromotionCreateDto
        {
            Name = "Good Code",
            Currency = "EUR",
            Code = "SUMMER-2026_A"
        };

        var result = new PromotionCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("the code contains only alphanumeric, hyphen, and underscore characters");
    }

    [Fact]
    public void PromotionCreate_Should_Fail_When_EndsAt_Before_StartsAt()
    {
        var start = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var dto = new PromotionCreateDto
        {
            Name = "Bad Dates",
            Currency = "EUR",
            StartsAtUtc = start,
            EndsAtUtc = start.AddDays(-1)
        };

        var result = new PromotionCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("EndsAtUtc must be after StartsAtUtc");
    }

    [Fact]
    public void PromotionCreate_Should_Pass_When_EndsAt_After_StartsAt()
    {
        var start = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var dto = new PromotionCreateDto
        {
            Name = "Good Dates",
            Currency = "EUR",
            StartsAtUtc = start,
            EndsAtUtc = start.AddDays(30)
        };

        var result = new PromotionCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("EndsAtUtc is after StartsAtUtc");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PromotionEditValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void PromotionEdit_Should_Pass_For_Valid_Dto()
    {
        var dto = new PromotionEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Name = "Autumn Sale",
            Currency = "EUR",
            Type = PromotionType.Percentage,
            Percent = 15m
        };

        var result = new PromotionEditValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a fully populated edit DTO should pass");
    }

    [Fact]
    public void PromotionEdit_Should_Fail_When_Id_Empty()
    {
        var dto = new PromotionEditDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 },
            Name = "Sale",
            Currency = "EUR"
        };

        var result = new PromotionEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Id must not be empty for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void PromotionEdit_Should_Fail_When_RowVersion_Null()
    {
        var dto = new PromotionEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = null!,
            Name = "Sale",
            Currency = "EUR"
        };

        var result = new PromotionEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("RowVersion must not be null for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RowVersion));
    }

    [Fact]
    public void PromotionEdit_Should_Fail_When_Name_Empty()
    {
        var dto = new PromotionEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Name = "",
            Currency = "EUR"
        };

        var result = new PromotionEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("name is required for edit");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ValidateCouponInputValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateCouponInput_Should_Pass_For_Valid_Dto()
    {
        var dto = new ValidateCouponInputDto
        {
            Code = "SUMMER10",
            SubtotalNetMinor = 5000,
            Currency = "EUR"
        };

        var result = new ValidateCouponInputValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a well-formed coupon validation request should pass");
    }

    [Fact]
    public void ValidateCouponInput_Should_Fail_When_Code_Empty()
    {
        var dto = new ValidateCouponInputDto { Code = "", SubtotalNetMinor = 100, Currency = "EUR" };

        var result = new ValidateCouponInputValidator().Validate(dto);

        result.IsValid.Should().BeFalse("coupon code is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Code));
    }

    [Fact]
    public void ValidateCouponInput_Should_Fail_When_Code_Too_Long()
    {
        var dto = new ValidateCouponInputDto
        {
            Code = new string('X', 65),
            SubtotalNetMinor = 100,
            Currency = "EUR"
        };

        var result = new ValidateCouponInputValidator().Validate(dto);

        result.IsValid.Should().BeFalse("code must not exceed 64 characters");
    }

    [Fact]
    public void ValidateCouponInput_Should_Fail_When_SubtotalNegative()
    {
        var dto = new ValidateCouponInputDto { Code = "CODE", SubtotalNetMinor = -1, Currency = "EUR" };

        var result = new ValidateCouponInputValidator().Validate(dto);

        result.IsValid.Should().BeFalse("SubtotalNetMinor must be >= 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SubtotalNetMinor));
    }

    [Fact]
    public void ValidateCouponInput_Should_Pass_When_SubtotalZero()
    {
        var dto = new ValidateCouponInputDto { Code = "FREE", SubtotalNetMinor = 0, Currency = "EUR" };

        var result = new ValidateCouponInputValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a zero subtotal is valid");
    }

    [Fact]
    public void ValidateCouponInput_Should_Fail_When_Currency_Invalid()
    {
        var dto = new ValidateCouponInputDto { Code = "CODE", SubtotalNetMinor = 100, Currency = "US" };

        var result = new ValidateCouponInputValidator().Validate(dto);

        result.IsValid.Should().BeFalse("currency must be exactly 3 characters");
    }
}
