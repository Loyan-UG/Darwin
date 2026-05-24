using Darwin.Application;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Validators;
using Darwin.Application.CMS.DTOs;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.CMS.Validators;
using Darwin.Application.Catalog.Validators;
using System.Collections.Generic;
using Darwin.Application.Pricing.DTOs;
using Darwin.Application.Pricing.Validators;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace Darwin.Tests.Unit.Validation;

/// <summary>
/// Unit tests for enum-boundary validation across critical admin/business surfaces.
/// </summary>
public sealed class EnumBoundaryValidatorTests
{
    private static IStringLocalizer<ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        return mock.Object;
    }

    [Fact]
    public void BusinessEdit_Should_Reject_OutOfRange_OperationalStatus()
    {
        var dto = new BusinessEditDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Business",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC",
            OperationalStatus = (BusinessOperationalStatus)999,
            RowVersion = [1, 2, 3]
        };

        var result = new BusinessEditDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("out-of-range enum values should be rejected by IsInEnum");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.OperationalStatus));
    }

    [Fact]
    public void CmsPageCreate_Should_Reject_OutOfRange_Status()
    {
        var dto = new PageCreateDto
        {
            Status = (PageStatus)999,
            Translations = new List<PageTranslationDto>
            {
                new PageTranslationDto
                {
                    Culture = "en-US",
                    Title = "Test Page",
                    Slug = "test-page",
                    ContentHtml = "<p>body</p>"
                }
            }
        };

        var result = new PageCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("out-of-range enum values should be rejected by IsInEnum");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Status));
    }

    [Fact]
    public void AddOnGroupCreate_Should_Reject_OutOfRange_SelectionMode()
    {
        var dto = new AddOnGroupCreateDto
        {
            Name = "Gift Wrapping",
            Currency = "EUR",
            SelectionMode = (AddOnSelectionMode)999
        };

        var result = new AddOnGroupCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("out-of-range enum values should be rejected by IsInEnum");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SelectionMode));
    }

    [Fact]
    public void PromotionCreate_Should_Reject_OutOfRange_Type()
    {
        var dto = new PromotionCreateDto
        {
            Name = "New Promotion",
            Currency = "EUR",
            Type = (PromotionType)999,
            Percent = 10m
        };

        var result = new PromotionCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("out-of-range enum values should be rejected by IsInEnum");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Type));
    }
}
