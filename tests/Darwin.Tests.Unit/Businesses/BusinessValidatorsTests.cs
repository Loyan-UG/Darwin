using System;
using Darwin.Application;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Validators;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.Businesses;

/// <summary>
/// Unit tests for business FluentValidation validators:
/// <see cref="BusinessCreateDtoValidator"/>, <see cref="BusinessEditDtoValidator"/>,
/// <see cref="BusinessDeleteDtoValidator"/>, and <see cref="BusinessLifecycleActionDtoValidator"/>.
/// </summary>
public sealed class BusinessValidatorsTests
{
    private static IStringLocalizer<ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        return mock.Object;
    }

    // ─── BusinessCreateDtoValidator ──────────────────────────────────────────

    [Fact]
    public void BusinessCreate_Should_Pass_ForValidDto()
    {
        var dto = new BusinessCreateDto
        {
            Name = "Aurora Cafe",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "Europe/Berlin"
        };

        var result = new BusinessCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a DTO with all required fields filled should pass validation");
    }

    [Fact]
    public void BusinessCreate_Should_Fail_WhenNameIsEmpty()
    {
        var dto = new BusinessCreateDto
        {
            Name = "",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC"
        };

        var result = new BusinessCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Name));
    }

    [Fact]
    public void BusinessCreate_Should_Fail_WhenNameExceedsMaxLength()
    {
        var dto = new BusinessCreateDto
        {
            Name = new string('A', 201),
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC"
        };

        var result = new BusinessCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Name));
    }

    [Fact]
    public void BusinessCreate_Should_Fail_WhenDefaultCurrencyIsEmpty()
    {
        var dto = new BusinessCreateDto
        {
            Name = "Test Business",
            DefaultCurrency = "",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC"
        };

        var result = new BusinessCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DefaultCurrency));
    }

    [Fact]
    public void BusinessCreate_Should_Fail_WhenDefaultCurrencyExceedsMaxLength()
    {
        var dto = new BusinessCreateDto
        {
            Name = "Test Business",
            DefaultCurrency = "EURO", // 4 chars, max is 3
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC"
        };

        var result = new BusinessCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DefaultCurrency));
    }

    [Fact]
    public void BusinessCreate_Should_Fail_WhenDefaultCultureIsEmpty()
    {
        var dto = new BusinessCreateDto
        {
            Name = "Test Business",
            DefaultCurrency = "EUR",
            DefaultCulture = "",
            DefaultTimeZoneId = "UTC"
        };

        var result = new BusinessCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DefaultCulture));
    }

    [Fact]
    public void BusinessCreate_Should_Fail_WhenDefaultTimeZoneIdIsEmpty()
    {
        var dto = new BusinessCreateDto
        {
            Name = "Test Business",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = ""
        };

        var result = new BusinessCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DefaultTimeZoneId));
    }

    [Fact]
    public void BusinessCreate_Should_Fail_WhenSupportEmailIsInvalidFormat()
    {
        var dto = new BusinessCreateDto
        {
            Name = "Test Business",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC",
            SupportEmail = "not-a-valid-email"
        };

        var result = new BusinessCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SupportEmail));
    }

    [Fact]
    public void BusinessCreate_Should_Pass_WhenSupportEmailIsNull()
    {
        var dto = new BusinessCreateDto
        {
            Name = "Test Business",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC",
            SupportEmail = null
        };

        var result = new BusinessCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("null optional email fields should be skipped");
    }

    [Fact]
    public void BusinessCreate_Should_Fail_WhenContactEmailIsInvalidFormat()
    {
        var dto = new BusinessCreateDto
        {
            Name = "Test Business",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC",
            ContactEmail = "bad@@email"
        };

        var result = new BusinessCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ContactEmail));
    }

    [Fact]
    public void BusinessCreate_Should_Fail_WhenCommunicationReplyToEmailIsInvalidFormat()
    {
        var dto = new BusinessCreateDto
        {
            Name = "Test Business",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC",
            CommunicationReplyToEmail = "invalid-email-format"
        };

        var result = new BusinessCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.CommunicationReplyToEmail));
    }

    [Fact]
    public void BusinessCreate_Should_Fail_WhenAdminTextOverridesJsonIsMalformed()
    {
        var dto = new BusinessCreateDto
        {
            Name = "Test Business",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC",
            AdminTextOverridesJson = "{ not valid json at all {{{"
        };

        var result = new BusinessCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.AdminTextOverridesJson));
    }

    [Fact]
    public void BusinessCreate_Should_Pass_WhenAdminTextOverridesJsonIsValidDictionary()
    {
        var dto = new BusinessCreateDto
        {
            Name = "Test Business",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC",
            AdminTextOverridesJson = """{"section1":{"key1":"value1"}}"""
        };

        var result = new BusinessCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("valid nested JSON dictionary should pass validation");
    }

    [Fact]
    public void BusinessCreate_Should_Pass_WhenAdminTextOverridesJsonIsNull()
    {
        var dto = new BusinessCreateDto
        {
            Name = "Test Business",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC",
            AdminTextOverridesJson = null
        };

        var result = new BusinessCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("null AdminTextOverridesJson is optional and should pass");
    }

    [Fact]
    public void BusinessCreate_Should_Fail_WhenLegalNameExceedsMaxLength()
    {
        var dto = new BusinessCreateDto
        {
            Name = "Test Business",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC",
            LegalName = new string('X', 301) // max is 300
        };

        var result = new BusinessCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.LegalName));
    }

    [Fact]
    public void BusinessCreate_Should_Fail_WhenShortDescriptionExceedsMaxLength()
    {
        var dto = new BusinessCreateDto
        {
            Name = "Test Business",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC",
            ShortDescription = new string('S', 1001) // max is 1000
        };

        var result = new BusinessCreateDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ShortDescription));
    }

    // ─── BusinessEditDtoValidator ─────────────────────────────────────────────

    [Fact]
    public void BusinessEdit_Should_Pass_ForValidDto()
    {
        var dto = new BusinessEditDto
        {
            Id = Guid.NewGuid(),
            Name = "Aurora Cafe",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "Europe/Berlin",
            RowVersion = new byte[] { 1, 2, 3 }
        };

        var result = new BusinessEditDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void BusinessEdit_Should_Fail_WhenIdIsEmpty()
    {
        var dto = new BusinessEditDto
        {
            Id = Guid.Empty,
            Name = "Test Business",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC",
            RowVersion = new byte[] { 1 }
        };

        var result = new BusinessEditDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void BusinessEdit_Should_Fail_WhenRowVersionIsEmpty()
    {
        var dto = new BusinessEditDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Business",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC",
            RowVersion = Array.Empty<byte>()
        };

        var result = new BusinessEditDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RowVersion));
    }

    [Fact]
    public void BusinessEdit_Should_Fail_WhenRowVersionIsNull()
    {
        var dto = new BusinessEditDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Business",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC",
            RowVersion = null!
        };

        var result = new BusinessEditDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RowVersion));
    }

    [Fact]
    public void BusinessEdit_Should_Fail_WhenNameIsEmpty()
    {
        var dto = new BusinessEditDto
        {
            Id = Guid.NewGuid(),
            Name = "",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC",
            RowVersion = new byte[] { 1 }
        };

        var result = new BusinessEditDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Name));
    }

    [Fact]
    public void BusinessEdit_Should_Fail_WhenSupportEmailIsInvalidFormat()
    {
        var dto = new BusinessEditDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Business",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC",
            RowVersion = new byte[] { 1 },
            SupportEmail = "not-an-email"
        };

        var result = new BusinessEditDtoValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SupportEmail));
    }

    // ─── BusinessDeleteDtoValidator ───────────────────────────────────────────

    [Fact]
    public void BusinessDelete_Should_Pass_ForValidDto()
    {
        var dto = new BusinessDeleteDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1, 2 }
        };

        var result = new BusinessDeleteDtoValidator().Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void BusinessDelete_Should_Fail_WhenIdIsEmpty()
    {
        var dto = new BusinessDeleteDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 }
        };

        var result = new BusinessDeleteDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void BusinessDelete_Should_Fail_WhenRowVersionIsEmpty()
    {
        var dto = new BusinessDeleteDto
        {
            Id = Guid.NewGuid(),
            RowVersion = Array.Empty<byte>()
        };

        var result = new BusinessDeleteDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RowVersion));
    }

    // ─── BusinessLifecycleActionDtoValidator ─────────────────────────────────

    [Fact]
    public void BusinessLifecycleAction_Should_Pass_ForValidDto()
    {
        var dto = new BusinessLifecycleActionDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 5, 6, 7 }
        };

        var result = new BusinessLifecycleActionDtoValidator().Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void BusinessLifecycleAction_Should_Pass_WhenNoteIsProvided()
    {
        var dto = new BusinessLifecycleActionDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Note = "Approved after manual review of documentation."
        };

        var result = new BusinessLifecycleActionDtoValidator().Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void BusinessLifecycleAction_Should_Fail_WhenIdIsEmpty()
    {
        var dto = new BusinessLifecycleActionDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 }
        };

        var result = new BusinessLifecycleActionDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void BusinessLifecycleAction_Should_Fail_WhenRowVersionIsEmpty()
    {
        var dto = new BusinessLifecycleActionDto
        {
            Id = Guid.NewGuid(),
            RowVersion = Array.Empty<byte>()
        };

        var result = new BusinessLifecycleActionDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RowVersion));
    }

    [Fact]
    public void BusinessLifecycleAction_Should_Fail_WhenNoteExceedsMaxLength()
    {
        var dto = new BusinessLifecycleActionDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Note = new string('N', 501) // max is 500
        };

        var result = new BusinessLifecycleActionDtoValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Note));
    }
}
