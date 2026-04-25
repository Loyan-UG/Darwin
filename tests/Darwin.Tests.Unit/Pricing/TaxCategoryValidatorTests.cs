using System;
using Darwin.Application.Pricing.DTOs;
using Darwin.Application.Pricing.Validators;
using FluentAssertions;
using Xunit;

namespace Darwin.Tests.Unit.Pricing;

/// <summary>
/// Unit tests for <see cref="TaxCategoryCreateValidator"/> and
/// <see cref="TaxCategoryEditValidator"/>.
/// </summary>
public sealed class TaxCategoryValidatorTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // TaxCategoryCreateValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TaxCategoryCreate_Should_Pass_For_Valid_Dto()
    {
        var dto = new TaxCategoryCreateDto { Name = "Standard VAT", VatRate = 0.19m };

        var result = new TaxCategoryCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a well-formed tax category create request should pass");
    }

    [Fact]
    public void TaxCategoryCreate_Should_Pass_For_Zero_Rate()
    {
        var dto = new TaxCategoryCreateDto { Name = "Zero Rate", VatRate = 0m };

        var result = new TaxCategoryCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a VAT rate of 0 is valid (e.g., exempt goods)");
    }

    [Fact]
    public void TaxCategoryCreate_Should_Pass_For_Max_Rate()
    {
        var dto = new TaxCategoryCreateDto { Name = "Max Rate", VatRate = 1m };

        var result = new TaxCategoryCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a VAT rate of 1.0 (100%) is the allowed upper bound");
    }

    [Fact]
    public void TaxCategoryCreate_Should_Fail_When_Name_Empty()
    {
        var dto = new TaxCategoryCreateDto { Name = "", VatRate = 0.19m };

        var result = new TaxCategoryCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("name is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Name));
    }

    [Fact]
    public void TaxCategoryCreate_Should_Fail_When_Name_Too_Long()
    {
        var dto = new TaxCategoryCreateDto { Name = new string('X', 129), VatRate = 0.07m };

        var result = new TaxCategoryCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("name must not exceed 128 characters");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(2.5)]
    public void TaxCategoryCreate_Should_Fail_When_VatRate_Out_Of_Range(double rate)
    {
        var dto = new TaxCategoryCreateDto { Name = "Bad Rate", VatRate = (decimal)rate };

        var result = new TaxCategoryCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse($"VatRate {rate} is outside [0, 1]");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.VatRate));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TaxCategoryEditValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TaxCategoryEdit_Should_Pass_For_Valid_Dto()
    {
        var dto = new TaxCategoryEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Name = "Reduced VAT",
            VatRate = 0.07m
        };

        var result = new TaxCategoryEditValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a fully populated edit DTO should pass");
    }

    [Fact]
    public void TaxCategoryEdit_Should_Fail_When_Id_Empty()
    {
        var dto = new TaxCategoryEditDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 },
            Name = "Reduced VAT",
            VatRate = 0.07m
        };

        var result = new TaxCategoryEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Id must not be empty for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void TaxCategoryEdit_Should_Fail_When_RowVersion_Null()
    {
        var dto = new TaxCategoryEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = null!,
            Name = "Reduced VAT",
            VatRate = 0.07m
        };

        var result = new TaxCategoryEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("RowVersion must not be null for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RowVersion));
    }

    [Fact]
    public void TaxCategoryEdit_Should_Fail_When_VatRate_Exceeds_One()
    {
        var dto = new TaxCategoryEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Name = "Bad Rate",
            VatRate = 1.5m
        };

        var result = new TaxCategoryEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("VatRate must be at most 1.0 for edit");
    }
}
