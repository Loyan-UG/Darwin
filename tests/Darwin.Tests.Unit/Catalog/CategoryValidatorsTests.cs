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
/// Unit tests for <see cref="CategoryTranslationDtoValidator"/>,
/// <see cref="CategoryCreateDtoValidator"/>, and <see cref="CategoryEditDtoValidator"/>.
/// </summary>
public sealed class CategoryValidatorsTests
{
    private static IStringLocalizer<ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        return mock.Object;
    }

    // ─── CategoryTranslationDtoValidator ────────────────────────────────────

    [Fact]
    public void CategoryTranslation_Should_Pass_For_Valid_Dto()
    {
        var dto = new CategoryTranslationDto
        {
            Culture = "en-US",
            Name = "Electronics",
            Slug = "electronics"
        };

        var result = new CategoryTranslationDtoValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a fully valid translation should pass");
    }

    [Fact]
    public void CategoryTranslation_Should_Fail_When_Culture_Empty()
    {
        var dto = new CategoryTranslationDto
        {
            Culture = "",
            Name = "Electronics",
            Slug = "electronics"
        };

        var result = new CategoryTranslationDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Culture is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Culture));
    }

    [Fact]
    public void CategoryTranslation_Should_Fail_When_Culture_Too_Long()
    {
        var dto = new CategoryTranslationDto
        {
            Culture = new string('x', 11),
            Name = "Electronics",
            Slug = "electronics"
        };

        var result = new CategoryTranslationDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Culture must not exceed 10 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Culture));
    }

    [Fact]
    public void CategoryTranslation_Should_Fail_When_Name_Empty()
    {
        var dto = new CategoryTranslationDto
        {
            Culture = "en-US",
            Name = "",
            Slug = "electronics"
        };

        var result = new CategoryTranslationDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Name is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Name));
    }

    [Fact]
    public void CategoryTranslation_Should_Fail_When_Name_Too_Long()
    {
        var dto = new CategoryTranslationDto
        {
            Culture = "en-US",
            Name = new string('N', 201),
            Slug = "electronics"
        };

        var result = new CategoryTranslationDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Name must not exceed 200 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Name));
    }

    [Fact]
    public void CategoryTranslation_Should_Fail_When_Slug_Empty()
    {
        var dto = new CategoryTranslationDto
        {
            Culture = "en-US",
            Name = "Electronics",
            Slug = ""
        };

        var result = new CategoryTranslationDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Slug is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Slug));
    }

    [Fact]
    public void CategoryTranslation_Should_Fail_When_Slug_Too_Long()
    {
        var dto = new CategoryTranslationDto
        {
            Culture = "en-US",
            Name = "Electronics",
            Slug = new string('s', 201)
        };

        var result = new CategoryTranslationDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Slug must not exceed 200 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Slug));
    }

    // ─── CategoryCreateDtoValidator ──────────────────────────────────────────

    [Fact]
    public void CategoryCreate_Should_Pass_For_Valid_Dto()
    {
        var dto = new CategoryCreateDto
        {
            Translations = new List<CategoryTranslationDto>
            {
                new() { Culture = "en-US", Name = "Electronics", Slug = "electronics" }
            }
        };

        var result = new CategoryCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a valid category create request should pass");
    }

    [Fact]
    public void CategoryCreate_Should_Fail_When_Translations_Empty()
    {
        var dto = new CategoryCreateDto
        {
            Translations = new List<CategoryTranslationDto>()
        };

        var result = new CategoryCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("at least one translation is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Translations));
    }

    [Fact]
    public void CategoryCreate_Should_Fail_When_Translation_Has_Invalid_Culture()
    {
        var dto = new CategoryCreateDto
        {
            Translations = new List<CategoryTranslationDto>
            {
                new() { Culture = "", Name = "Electronics", Slug = "electronics" }
            }
        };

        var result = new CategoryCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("translation Culture is required");
    }

    [Fact]
    public void CategoryCreate_Should_Pass_With_Multiple_Translations()
    {
        var dto = new CategoryCreateDto
        {
            Translations = new List<CategoryTranslationDto>
            {
                new() { Culture = "en-US", Name = "Electronics", Slug = "electronics" },
                new() { Culture = "de-DE", Name = "Elektronik", Slug = "elektronik" }
            }
        };

        var result = new CategoryCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("multiple valid translations should pass");
    }

    // ─── CategoryEditDtoValidator ────────────────────────────────────────────

    [Fact]
    public void CategoryEdit_Should_Pass_For_Valid_Dto()
    {
        var dto = new CategoryEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Translations = new List<CategoryTranslationDto>
            {
                new() { Culture = "en-US", Name = "Electronics Updated", Slug = "electronics-updated" }
            }
        };

        var result = new CategoryEditDtoValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a valid category edit should pass");
    }

    [Fact]
    public void CategoryEdit_Should_Fail_When_Id_Empty()
    {
        var dto = new CategoryEditDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 },
            Translations = new List<CategoryTranslationDto>
            {
                new() { Culture = "en-US", Name = "Electronics", Slug = "electronics" }
            }
        };

        var result = new CategoryEditDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Id must not be empty for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void CategoryEdit_Should_Fail_When_RowVersion_Null()
    {
        var dto = new CategoryEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = null!,
            Translations = new List<CategoryTranslationDto>
            {
                new() { Culture = "en-US", Name = "Electronics", Slug = "electronics" }
            }
        };

        var result = new CategoryEditDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("RowVersion must not be null for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RowVersion));
    }

    [Fact]
    public void CategoryEdit_Should_Fail_When_Translation_Slug_Too_Long()
    {
        var dto = new CategoryEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Translations = new List<CategoryTranslationDto>
            {
                new() { Culture = "en-US", Name = "Electronics", Slug = new string('s', 201) }
            }
        };

        var result = new CategoryEditDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse("translation Slug must not exceed 200 characters in an edit");
    }

    [Fact]
    public void CategoryEdit_Should_Pass_With_No_Translations()
    {
        // CategoryEditDtoValidator does not require at least one translation,
        // it only validates each translation that IS provided.
        var dto = new CategoryEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Translations = new List<CategoryTranslationDto>()
        };

        var result = new CategoryEditDtoValidator().Validate(dto);

        result.IsValid.Should().BeTrue("the edit validator does not mandate at least one translation");
    }
}
