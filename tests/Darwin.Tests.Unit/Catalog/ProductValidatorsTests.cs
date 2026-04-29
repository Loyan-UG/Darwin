using System;
using System.Collections.Generic;
using Darwin.Application;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Validators;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.Catalog;

/// <summary>
/// Unit tests for <see cref="ProductTranslationDtoValidator"/>,
/// <see cref="ProductVariantCreateDtoValidator"/>, <see cref="ProductCreateDtoValidator"/>,
/// and <see cref="ProductEditDtoValidator"/>.
/// </summary>
public sealed class ProductValidatorsTests
{
    private static IStringLocalizer<ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        return mock.Object;
    }

    private static ProductTranslationDto ValidTranslation() =>
        new() { Culture = "en-US", Name = "Widget Pro", Slug = "widget-pro" };

    private static ProductVariantCreateDto ValidVariant() =>
        new()
        {
            Sku = "WGT-PRO-S",
            Currency = "EUR",
            BasePriceNetMinor = 2500,
            TaxCategoryId = Guid.NewGuid()
        };

    // ─── ProductTranslationDtoValidator ─────────────────────────────────────

    [Fact]
    public void ProductTranslation_Should_Pass_For_Valid_Dto()
    {
        var result = new ProductTranslationDtoValidator().Validate(ValidTranslation());

        result.IsValid.Should().BeTrue("a fully valid translation should pass");
    }

    [Fact]
    public void ProductTranslation_Should_Fail_When_Culture_Empty()
    {
        var dto = ValidTranslation();
        dto.Culture = "";

        var result = new ProductTranslationDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Culture is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Culture));
    }

    [Fact]
    public void ProductTranslation_Should_Fail_When_Culture_Too_Long()
    {
        var dto = ValidTranslation();
        dto.Culture = new string('c', 11);

        var result = new ProductTranslationDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Culture must not exceed 10 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Culture));
    }

    [Fact]
    public void ProductTranslation_Should_Fail_When_Name_Empty()
    {
        var dto = ValidTranslation();
        dto.Name = "";

        var result = new ProductTranslationDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Name is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Name));
    }

    [Fact]
    public void ProductTranslation_Should_Fail_When_Name_Too_Long()
    {
        var dto = ValidTranslation();
        dto.Name = new string('N', 201);

        var result = new ProductTranslationDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Name must not exceed 200 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Name));
    }

    [Fact]
    public void ProductTranslation_Should_Fail_When_Slug_Empty()
    {
        var dto = ValidTranslation();
        dto.Slug = "";

        var result = new ProductTranslationDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Slug is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Slug));
    }

    [Fact]
    public void ProductTranslation_Should_Fail_When_Slug_Too_Long()
    {
        var dto = ValidTranslation();
        dto.Slug = new string('s', 201);

        var result = new ProductTranslationDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Slug must not exceed 200 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Slug));
    }

    // ─── ProductVariantCreateDtoValidator ───────────────────────────────────

    [Fact]
    public void ProductVariantCreate_Should_Pass_For_Valid_Dto()
    {
        var result = new ProductVariantCreateDtoValidator().Validate(ValidVariant());

        result.IsValid.Should().BeTrue("a fully valid variant should pass");
    }

    [Fact]
    public void ProductVariantCreate_Should_Fail_When_Sku_Empty()
    {
        var dto = ValidVariant();
        dto.Sku = "";

        var result = new ProductVariantCreateDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Sku is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Sku));
    }

    [Fact]
    public void ProductVariantCreate_Should_Fail_When_Sku_Too_Long()
    {
        var dto = ValidVariant();
        dto.Sku = new string('S', 101);

        var result = new ProductVariantCreateDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Sku must not exceed 100 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Sku));
    }

    [Fact]
    public void ProductVariantCreate_Should_Fail_When_Currency_Empty()
    {
        var dto = ValidVariant();
        dto.Currency = "";

        var result = new ProductVariantCreateDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Currency is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Currency));
    }

    [Fact]
    public void ProductVariantCreate_Should_Fail_When_Currency_Wrong_Length()
    {
        var dto = ValidVariant();
        dto.Currency = "EURO";

        var result = new ProductVariantCreateDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Currency must be exactly 3 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Currency));
    }

    [Fact]
    public void ProductVariantCreate_Should_Fail_When_BasePriceNetMinor_Negative()
    {
        var dto = ValidVariant();
        dto.BasePriceNetMinor = -1;

        var result = new ProductVariantCreateDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("BasePriceNetMinor must be >= 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.BasePriceNetMinor));
    }

    [Fact]
    public void ProductVariantCreate_Should_Pass_When_BasePriceNetMinor_Is_Zero()
    {
        var dto = ValidVariant();
        dto.BasePriceNetMinor = 0;

        var result = new ProductVariantCreateDtoValidator().Validate(dto);

        result.IsValid.Should().BeTrue("zero price (e.g., free product) is allowed");
    }

    [Fact]
    public void ProductVariantCreate_Should_Fail_When_StockOnHand_Negative()
    {
        var dto = ValidVariant();
        dto.StockOnHand = -5;

        var result = new ProductVariantCreateDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("StockOnHand must be >= 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.StockOnHand));
    }

    [Fact]
    public void ProductVariantCreate_Should_Fail_When_StockReserved_Negative()
    {
        var dto = ValidVariant();
        dto.StockReserved = -1;

        var result = new ProductVariantCreateDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("StockReserved must be >= 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.StockReserved));
    }

    [Fact]
    public void ProductVariantCreate_Should_Fail_When_TaxCategoryId_Empty()
    {
        var dto = ValidVariant();
        dto.TaxCategoryId = Guid.Empty;

        var result = new ProductVariantCreateDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("TaxCategoryId must not be empty");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.TaxCategoryId));
    }

    // ─── ProductCreateDtoValidator ───────────────────────────────────────────

    [Fact]
    public void ProductCreate_Should_Pass_For_Minimal_Valid_Dto()
    {
        var dto = new ProductCreateDto
        {
            Translations = new List<ProductTranslationDto> { ValidTranslation() },
            Variants = new List<ProductVariantCreateDto> { ValidVariant() }
        };

        var result = new ProductCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a product with one translation and one variant should pass");
    }

    [Fact]
    public void ProductCreate_Should_Fail_When_Translations_Empty()
    {
        var dto = new ProductCreateDto
        {
            Translations = new List<ProductTranslationDto>(),
            Variants = new List<ProductVariantCreateDto> { ValidVariant() }
        };

        var result = new ProductCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("at least one translation is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Translations));
    }

    [Fact]
    public void ProductCreate_Should_Fail_When_Variants_Empty()
    {
        var dto = new ProductCreateDto
        {
            Translations = new List<ProductTranslationDto> { ValidTranslation() },
            Variants = new List<ProductVariantCreateDto>()
        };

        var result = new ProductCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("at least one variant is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Variants));
    }

    [Fact]
    public void ProductCreate_Should_Fail_When_Translation_Is_Invalid()
    {
        var badTranslation = ValidTranslation();
        badTranslation.Name = "";

        var dto = new ProductCreateDto
        {
            Translations = new List<ProductTranslationDto> { badTranslation },
            Variants = new List<ProductVariantCreateDto> { ValidVariant() }
        };

        var result = new ProductCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("an invalid translation should cascade to the product validator");
    }

    [Fact]
    public void ProductCreate_Should_Fail_When_Variant_Is_Invalid()
    {
        var badVariant = ValidVariant();
        badVariant.Sku = "";

        var dto = new ProductCreateDto
        {
            Translations = new List<ProductTranslationDto> { ValidTranslation() },
            Variants = new List<ProductVariantCreateDto> { badVariant }
        };

        var result = new ProductCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("an invalid variant should cascade to the product validator");
    }

    // ─── ProductEditDtoValidator ─────────────────────────────────────────────

    [Fact]
    public void ProductEdit_Should_Pass_For_Valid_Dto()
    {
        var dto = new ProductEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Translations = new List<ProductTranslationDto> { ValidTranslation() },
            Variants = new List<ProductVariantCreateDto> { ValidVariant() }
        };

        var result = new ProductEditDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a fully valid edit dto should pass");
    }

    [Fact]
    public void ProductEdit_Should_Fail_When_Id_Empty()
    {
        var dto = new ProductEditDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 },
            Translations = new List<ProductTranslationDto> { ValidTranslation() },
            Variants = new List<ProductVariantCreateDto> { ValidVariant() }
        };

        var result = new ProductEditDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Id must not be empty for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void ProductEdit_Should_Fail_When_RowVersion_Null()
    {
        var dto = new ProductEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = null!,
            Translations = new List<ProductTranslationDto> { ValidTranslation() },
            Variants = new List<ProductVariantCreateDto> { ValidVariant() }
        };

        var result = new ProductEditDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("RowVersion must not be null for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RowVersion));
    }

    [Fact]
    public void ProductEdit_Should_Fail_When_Translation_Culture_Too_Long()
    {
        var badTranslation = ValidTranslation();
        badTranslation.Culture = new string('c', 11);

        var dto = new ProductEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Translations = new List<ProductTranslationDto> { badTranslation },
            Variants = new List<ProductVariantCreateDto> { ValidVariant() }
        };

        var result = new ProductEditDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("invalid translation fields should be caught in an edit");
    }

    [Fact]
    public void ProductEdit_Should_Fail_When_Variant_Has_Invalid_Currency()
    {
        var badVariant = ValidVariant();
        badVariant.Currency = "XX";  // too short

        var dto = new ProductEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Translations = new List<ProductTranslationDto> { ValidTranslation() },
            Variants = new List<ProductVariantCreateDto> { badVariant }
        };

        var result = new ProductEditDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("an invalid variant currency should be caught in an edit");
    }
}
