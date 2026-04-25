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
/// Unit tests for <see cref="BrandCreateDtoValidator"/>, <see cref="BrandEditDtoValidator"/>,
/// and <see cref="BrandDeleteValidator"/>.
/// </summary>
public sealed class BrandValidatorsTests
{
    private static IStringLocalizer<ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        return mock.Object;
    }

    // ─── BrandCreateDtoValidator ─────────────────────────────────────────────

    [Fact]
    public void BrandCreate_Should_Pass_For_Minimal_Valid_Dto()
    {
        var dto = new BrandCreateDto
        {
            Translations = new List<BrandTranslationDto>
            {
                new() { Culture = "en-US", Name = "Acme" }
            }
        };

        var result = new BrandCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a brand with one valid translation should pass");
    }

    [Fact]
    public void BrandCreate_Should_Pass_With_Optional_Slug()
    {
        var dto = new BrandCreateDto
        {
            Slug = "acme-brand",
            Translations = new List<BrandTranslationDto>
            {
                new() { Culture = "en-US", Name = "Acme" }
            }
        };

        var result = new BrandCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a slug within the 256-character limit is valid");
    }

    [Fact]
    public void BrandCreate_Should_Fail_When_Translations_Empty()
    {
        var dto = new BrandCreateDto
        {
            Translations = new List<BrandTranslationDto>()
        };

        var result = new BrandCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("at least one translation is required");
    }

    [Fact]
    public void BrandCreate_Should_Fail_When_Translation_Culture_Empty()
    {
        var dto = new BrandCreateDto
        {
            Translations = new List<BrandTranslationDto>
            {
                new() { Culture = "", Name = "Acme" }
            }
        };

        var result = new BrandCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("translation Culture is required");
    }

    [Fact]
    public void BrandCreate_Should_Fail_When_Translation_Culture_Too_Long()
    {
        var dto = new BrandCreateDto
        {
            Translations = new List<BrandTranslationDto>
            {
                new() { Culture = new string('x', 17), Name = "Acme" }
            }
        };

        var result = new BrandCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("translation Culture must not exceed 16 characters");
    }

    [Fact]
    public void BrandCreate_Should_Fail_When_Translation_Name_Empty()
    {
        var dto = new BrandCreateDto
        {
            Translations = new List<BrandTranslationDto>
            {
                new() { Culture = "en-US", Name = "" }
            }
        };

        var result = new BrandCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("translation Name is required");
    }

    [Fact]
    public void BrandCreate_Should_Fail_When_Translation_Name_Too_Long()
    {
        var dto = new BrandCreateDto
        {
            Translations = new List<BrandTranslationDto>
            {
                new() { Culture = "en-US", Name = new string('N', 257) }
            }
        };

        var result = new BrandCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("translation Name must not exceed 256 characters");
    }

    [Fact]
    public void BrandCreate_Should_Fail_When_Slug_Too_Long()
    {
        var dto = new BrandCreateDto
        {
            Slug = new string('s', 257),
            Translations = new List<BrandTranslationDto>
            {
                new() { Culture = "en-US", Name = "Acme" }
            }
        };

        var result = new BrandCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("slug must not exceed 256 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Slug));
    }

    // ─── BrandEditDtoValidator ───────────────────────────────────────────────

    [Fact]
    public void BrandEdit_Should_Pass_For_Valid_Dto()
    {
        var dto = new BrandEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Translations = new List<BrandTranslationDto>
            {
                new() { Culture = "en-US", Name = "Acme Updated" }
            }
        };

        var result = new BrandEditDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a fully valid edit dto should pass");
    }

    [Fact]
    public void BrandEdit_Should_Fail_When_Id_Empty()
    {
        var dto = new BrandEditDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 },
            Translations = new List<BrandTranslationDto>
            {
                new() { Culture = "en-US", Name = "Acme" }
            }
        };

        var result = new BrandEditDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Id must not be empty for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void BrandEdit_Should_Fail_When_RowVersion_Null()
    {
        var dto = new BrandEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = null!,
            Translations = new List<BrandTranslationDto>
            {
                new() { Culture = "en-US", Name = "Acme" }
            }
        };

        var result = new BrandEditDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("RowVersion must not be null for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RowVersion));
    }

    [Fact]
    public void BrandEdit_Should_Fail_When_Translations_Empty()
    {
        var dto = new BrandEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Translations = new List<BrandTranslationDto>()
        };

        var result = new BrandEditDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("at least one translation is required for an edit");
    }

    [Fact]
    public void BrandEdit_Should_Fail_When_Translation_Name_Empty()
    {
        var dto = new BrandEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Translations = new List<BrandTranslationDto>
            {
                new() { Culture = "en-US", Name = "" }
            }
        };

        var result = new BrandEditDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("translation Name is required for an edit");
    }

    // ─── BrandDeleteValidator ────────────────────────────────────────────────

    [Fact]
    public void BrandDelete_Should_Pass_For_Valid_Dto()
    {
        var dto = new BrandDeleteDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1, 2, 3 }
        };

        var result = new BrandDeleteValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a valid delete request should pass");
    }

    [Fact]
    public void BrandDelete_Should_Fail_When_Id_Empty()
    {
        var dto = new BrandDeleteDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 }
        };

        var result = new BrandDeleteValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Id must not be empty for a delete");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void BrandDelete_Should_Fail_When_RowVersion_Null()
    {
        var dto = new BrandDeleteDto
        {
            Id = Guid.NewGuid(),
            RowVersion = null!
        };

        var result = new BrandDeleteValidator().Validate(dto);

        result.IsValid.Should().BeFalse("RowVersion must not be null for a delete");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RowVersion));
    }
}
