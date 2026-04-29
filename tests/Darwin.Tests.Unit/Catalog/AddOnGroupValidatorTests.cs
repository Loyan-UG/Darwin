using System;
using System.Collections.Generic;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Validators;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace Darwin.Tests.Unit.Catalog;

/// <summary>
/// Unit tests for <see cref="AddOnGroupCreateValidator"/> and
/// <see cref="AddOnGroupEditValidator"/>.
/// These validators protect the add-on group management endpoints against
/// malformed payloads.
/// </summary>
public sealed class AddOnGroupValidatorTests
{
    private static IStringLocalizer<Darwin.Application.ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<Darwin.Application.ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        return mock.Object;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AddOnGroupCreateValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddOnGroupCreate_Should_Pass_For_Minimal_Valid_Dto()
    {
        var dto = new AddOnGroupCreateDto
        {
            Name = "Gift Wrapping",
            Currency = "EUR"
        };

        var result = new AddOnGroupCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a minimal valid create payload should pass");
    }

    [Fact]
    public void AddOnGroupCreate_Should_Pass_For_Dto_With_Options()
    {
        var dto = new AddOnGroupCreateDto
        {
            Name = "Lens Type",
            Currency = "EUR",
            SelectionMode = AddOnSelectionMode.Single,
            MinSelections = 1,
            MaxSelections = 1,
            Options = new List<AddOnOptionDto>
            {
                new()
                {
                    Label = "Lens",
                    Values = new List<AddOnOptionValueDto>
                    {
                        new() { Label = "Clear", PriceDeltaMinor = 0 },
                        new() { Label = "Tinted", PriceDeltaMinor = 500 }
                    }
                }
            }
        };

        var result = new AddOnGroupCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a fully populated create payload should pass");
    }

    [Fact]
    public void AddOnGroupCreate_Should_Fail_When_Name_Empty()
    {
        var dto = new AddOnGroupCreateDto { Name = "", Currency = "EUR" };

        var result = new AddOnGroupCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Name is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Name));
    }

    [Fact]
    public void AddOnGroupCreate_Should_Fail_When_Name_Too_Long()
    {
        var dto = new AddOnGroupCreateDto { Name = new string('A', 257), Currency = "EUR" };

        var result = new AddOnGroupCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Name must not exceed 256 characters");
    }

    [Fact]
    public void AddOnGroupCreate_Should_Fail_When_Currency_Wrong_Length()
    {
        var dto = new AddOnGroupCreateDto { Name = "Wrapping", Currency = "EURO" };

        var result = new AddOnGroupCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Currency must be exactly 3 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Currency));
    }

    [Fact]
    public void AddOnGroupCreate_Should_Fail_When_MinSelections_Negative()
    {
        var dto = new AddOnGroupCreateDto { Name = "Group", Currency = "EUR", MinSelections = -1 };

        var result = new AddOnGroupCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("MinSelections must be >= 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.MinSelections));
    }

    [Fact]
    public void AddOnGroupCreate_Should_Fail_When_MaxSelections_Negative()
    {
        var dto = new AddOnGroupCreateDto { Name = "Group", Currency = "EUR", MaxSelections = -1 };

        var result = new AddOnGroupCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("MaxSelections must be >= 0 when provided");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.MaxSelections));
    }

    [Fact]
    public void AddOnGroupCreate_Should_Pass_When_MaxSelections_Null()
    {
        var dto = new AddOnGroupCreateDto { Name = "Group", Currency = "EUR", MaxSelections = null };

        var result = new AddOnGroupCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("null MaxSelections means unlimited");
    }

    [Fact]
    public void AddOnGroupCreate_Should_Fail_When_Option_Label_Empty()
    {
        var dto = new AddOnGroupCreateDto
        {
            Name = "Group",
            Currency = "EUR",
            Options = new List<AddOnOptionDto>
            {
                new() { Label = "", Values = new List<AddOnOptionValueDto>() }
            }
        };

        var result = new AddOnGroupCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("option labels are required");
    }

    [Fact]
    public void AddOnGroupCreate_Should_Fail_When_OptionValue_Label_Empty()
    {
        var dto = new AddOnGroupCreateDto
        {
            Name = "Group",
            Currency = "EUR",
            Options = new List<AddOnOptionDto>
            {
                new()
                {
                    Label = "Option",
                    Values = new List<AddOnOptionValueDto>
                    {
                        new() { Label = "" }
                    }
                }
            }
        };

        var result = new AddOnGroupCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("option value labels are required");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AddOnGroupEditValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddOnGroupEdit_Should_Pass_For_Valid_Dto()
    {
        var dto = new AddOnGroupEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Name = "Gift Wrap",
            Currency = "EUR"
        };

        var result = new AddOnGroupEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a valid edit DTO should pass");
    }

    [Fact]
    public void AddOnGroupEdit_Should_Fail_When_Id_Empty()
    {
        var dto = new AddOnGroupEditDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 },
            Name = "Gift Wrap",
            Currency = "EUR"
        };

        var result = new AddOnGroupEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Id is required for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void AddOnGroupEdit_Should_Fail_When_RowVersion_Null()
    {
        var dto = new AddOnGroupEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = null!,
            Name = "Gift Wrap",
            Currency = "EUR"
        };

        var result = new AddOnGroupEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("RowVersion must not be null for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RowVersion));
    }

    [Fact]
    public void AddOnGroupEdit_Should_Fail_When_Name_Empty()
    {
        var dto = new AddOnGroupEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Name = "",
            Currency = "EUR"
        };

        var result = new AddOnGroupEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Name is required for edit");
    }

    [Fact]
    public void AddOnGroupEdit_Should_Fail_When_MinSelections_Negative()
    {
        var dto = new AddOnGroupEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Name = "Group",
            Currency = "EUR",
            MinSelections = -1
        };

        var result = new AddOnGroupEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("MinSelections must be >= 0 for edit");
    }
}
