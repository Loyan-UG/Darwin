using System;
using Darwin.Application;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Unit tests for role and permission FluentValidation validators:
/// <see cref="RoleCreateValidator"/>, <see cref="RoleEditValidator"/>,
/// <see cref="PermissionCreateValidator"/>, <see cref="PermissionEditValidator"/>,
/// and <see cref="PermissionDeleteValidator"/>.
/// </summary>
public sealed class RoleAndPermissionValidatorsTests
{
    private static IStringLocalizer<ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        return mock.Object;
    }

    // ─── RoleCreateValidator ──────────────────────────────────────────────────

    [Fact]
    public void RoleCreate_Should_Pass_ForValidDto()
    {
        var dto = new RoleCreateDto
        {
            Key = "store-manager",
            DisplayName = "Store Manager",
            Description = "Manages store operations"
        };

        var result = new RoleCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RoleCreate_Should_Fail_WhenKeyIsEmpty()
    {
        var dto = new RoleCreateDto { Key = "", DisplayName = "Test" };

        var result = new RoleCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Key));
    }

    [Fact]
    public void RoleCreate_Should_Fail_WhenDisplayNameIsEmpty()
    {
        var dto = new RoleCreateDto { Key = "some-role", DisplayName = "" };

        var result = new RoleCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DisplayName));
    }

    [Fact]
    public void RoleCreate_Should_Fail_WhenKeyExceedsMaxLength()
    {
        var dto = new RoleCreateDto
        {
            Key = new string('k', 129),
            DisplayName = "Valid Name"
        };

        var result = new RoleCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Key));
    }

    [Fact]
    public void RoleCreate_Should_Fail_WhenDisplayNameExceedsMaxLength()
    {
        var dto = new RoleCreateDto
        {
            Key = "some-role",
            DisplayName = new string('d', 257)
        };

        var result = new RoleCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DisplayName));
    }

    // ─── RoleEditValidator ────────────────────────────────────────────────────

    [Fact]
    public void RoleEdit_Should_Pass_ForValidDto()
    {
        var dto = new RoleEditDto
        {
            Id = Guid.NewGuid(),
            DisplayName = "Updated Manager",
            RowVersion = [1, 2, 3]
        };

        var result = new RoleEditValidator().Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RoleEdit_Should_Fail_WhenIdIsEmpty()
    {
        var dto = new RoleEditDto
        {
            Id = Guid.Empty,
            DisplayName = "Valid",
            RowVersion = [1]
        };

        var result = new RoleEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void RoleEdit_Should_Fail_WhenDisplayNameIsEmpty()
    {
        var dto = new RoleEditDto
        {
            Id = Guid.NewGuid(),
            DisplayName = "",
            RowVersion = [1]
        };

        var result = new RoleEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DisplayName));
    }

    // ─── PermissionCreateValidator ────────────────────────────────────────────

    [Fact]
    public void PermissionCreate_Should_Pass_ForValidDto()
    {
        var dto = new PermissionCreateDto
        {
            Key = "catalog.write",
            DisplayName = "Catalog Write"
        };

        var result = new PermissionCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PermissionCreate_Should_Fail_WhenKeyIsEmpty()
    {
        var dto = new PermissionCreateDto { Key = "", DisplayName = "Valid" };

        var result = new PermissionCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Key));
    }

    [Fact]
    public void PermissionCreate_Should_Fail_WhenKeyIsTooShort()
    {
        var dto = new PermissionCreateDto { Key = "ab", DisplayName = "Valid" };

        var result = new PermissionCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Key));
    }

    [Fact]
    public void PermissionCreate_Should_Fail_WhenDisplayNameIsEmpty()
    {
        var dto = new PermissionCreateDto { Key = "valid.key", DisplayName = "" };

        var result = new PermissionCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DisplayName));
    }

    // ─── PermissionEditValidator ──────────────────────────────────────────────

    [Fact]
    public void PermissionEdit_Should_Pass_ForValidDto()
    {
        var dto = new PermissionEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1, 2, 3],
            DisplayName = "New Display"
        };

        var result = new PermissionEditValidator().Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PermissionEdit_Should_Fail_WhenIdIsEmpty()
    {
        var dto = new PermissionEditDto
        {
            Id = Guid.Empty,
            RowVersion = [1],
            DisplayName = "Valid"
        };

        var result = new PermissionEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void PermissionEdit_Should_Fail_WhenDisplayNameIsEmpty()
    {
        var dto = new PermissionEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1],
            DisplayName = ""
        };

        var result = new PermissionEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DisplayName));
    }

    // ─── PermissionDeleteValidator ────────────────────────────────────────────

    [Fact]
    public void PermissionDelete_Should_Pass_ForValidDto()
    {
        var dto = new PermissionDeleteDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1, 2, 3]
        };

        var result = new PermissionDeleteValidator().Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PermissionDelete_Should_Fail_WhenIdIsEmpty()
    {
        var dto = new PermissionDeleteDto
        {
            Id = Guid.Empty,
            RowVersion = [1]
        };

        var result = new PermissionDeleteValidator().Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }
}
