using System;
using Darwin.Application;
using Darwin.Application.SEO.DTOs;
using Darwin.Application.SEO.Validators;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.SEO;

/// <summary>
/// Unit tests for <see cref="RedirectRuleCreateValidator"/> and
/// <see cref="RedirectRuleEditValidator"/>.
/// </summary>
public sealed class RedirectRuleValidatorsTests
{
    private static IStringLocalizer<ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        return mock.Object;
    }

    // ─── RedirectRuleCreateValidator ─────────────────────────────────────────

    [Fact]
    public void RedirectRuleCreate_Should_Pass_For_Valid_Permanent_Redirect()
    {
        var dto = new RedirectRuleCreateDto
        {
            FromPath = "/old-page",
            To = "/new-page",
            IsPermanent = true
        };

        var result = new RedirectRuleCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a well-formed permanent redirect should pass validation");
    }

    [Fact]
    public void RedirectRuleCreate_Should_Pass_For_Temporary_Redirect()
    {
        var dto = new RedirectRuleCreateDto
        {
            FromPath = "/promo",
            To = "https://external.example.com/landing",
            IsPermanent = false
        };

        var result = new RedirectRuleCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a temporary redirect to an external URL should pass");
    }

    [Fact]
    public void RedirectRuleCreate_Should_Fail_When_FromPath_Empty()
    {
        var dto = new RedirectRuleCreateDto { FromPath = "", To = "/target" };

        var result = new RedirectRuleCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("FromPath is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.FromPath));
    }

    [Fact]
    public void RedirectRuleCreate_Should_Fail_When_FromPath_Does_Not_Start_With_Slash()
    {
        var dto = new RedirectRuleCreateDto { FromPath = "no-leading-slash", To = "/target" };

        var result = new RedirectRuleCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("FromPath must start with '/'");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.FromPath));
    }

    [Fact]
    public void RedirectRuleCreate_Should_Fail_When_FromPath_Too_Long()
    {
        var dto = new RedirectRuleCreateDto
        {
            FromPath = "/" + new string('x', 2048),
            To = "/target"
        };

        var result = new RedirectRuleCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("FromPath must not exceed 2048 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.FromPath));
    }

    [Fact]
    public void RedirectRuleCreate_Should_Fail_When_To_Empty()
    {
        var dto = new RedirectRuleCreateDto { FromPath = "/source", To = "" };

        var result = new RedirectRuleCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("To is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.To));
    }

    [Fact]
    public void RedirectRuleCreate_Should_Fail_When_To_Too_Long()
    {
        var dto = new RedirectRuleCreateDto
        {
            FromPath = "/source",
            To = new string('y', 2049)
        };

        var result = new RedirectRuleCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("To must not exceed 2048 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.To));
    }

    [Fact]
    public void RedirectRuleCreate_Should_Pass_When_FromPath_Is_Exactly_Max_Length()
    {
        var dto = new RedirectRuleCreateDto
        {
            FromPath = "/" + new string('a', 2047),
            To = "/target"
        };

        var result = new RedirectRuleCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a path of exactly 2048 characters is at the allowed limit");
    }

    // ─── RedirectRuleEditValidator ────────────────────────────────────────────

    [Fact]
    public void RedirectRuleEdit_Should_Pass_For_Valid_Dto()
    {
        var dto = new RedirectRuleEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            FromPath = "/old-path",
            To = "/new-path",
            IsPermanent = true
        };

        var result = new RedirectRuleEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a fully populated edit DTO should pass");
    }

    [Fact]
    public void RedirectRuleEdit_Should_Fail_When_Id_Empty()
    {
        var dto = new RedirectRuleEditDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 },
            FromPath = "/source",
            To = "/target"
        };

        var result = new RedirectRuleEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Id must not be empty for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void RedirectRuleEdit_Should_Fail_When_RowVersion_Null()
    {
        var dto = new RedirectRuleEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = null!,
            FromPath = "/source",
            To = "/target"
        };

        var result = new RedirectRuleEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("RowVersion must not be null for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RowVersion));
    }

    [Fact]
    public void RedirectRuleEdit_Should_Fail_When_FromPath_Does_Not_Start_With_Slash()
    {
        var dto = new RedirectRuleEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            FromPath = "missing-slash",
            To = "/target"
        };

        var result = new RedirectRuleEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("FromPath must begin with '/'");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.FromPath));
    }

    [Fact]
    public void RedirectRuleEdit_Should_Fail_When_To_Empty()
    {
        var dto = new RedirectRuleEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            FromPath = "/source",
            To = ""
        };

        var result = new RedirectRuleEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("To is required for an edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.To));
    }

    [Fact]
    public void RedirectRuleEdit_Should_Fail_When_FromPath_Too_Long()
    {
        var dto = new RedirectRuleEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            FromPath = "/" + new string('z', 2048),
            To = "/target"
        };

        var result = new RedirectRuleEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("FromPath must not exceed 2048 characters");
    }

    [Fact]
    public void RedirectRuleEdit_Should_Fail_When_To_Too_Long()
    {
        var dto = new RedirectRuleEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            FromPath = "/source",
            To = new string('t', 2049)
        };

        var result = new RedirectRuleEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("To must not exceed 2048 characters");
    }
}
