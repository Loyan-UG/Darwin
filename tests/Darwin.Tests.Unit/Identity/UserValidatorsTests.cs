using System;
using Darwin.Application;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Unit tests for user management validators:
/// <see cref="UserCreateValidator"/>, <see cref="UserEditValidator"/>,
/// <see cref="UserAdminSetPasswordValidator"/>, <see cref="UserChangePasswordValidator"/>,
/// and <see cref="UserAdminActionValidator"/>.
/// </summary>
public sealed class UserValidatorsTests
{
    private static IStringLocalizer<ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        return mock.Object;
    }

    // ─── UserCreateValidator ─────────────────────────────────────────────────

    [Fact]
    public void UserCreate_Should_Pass_For_Valid_Dto()
    {
        var dto = new UserCreateDto
        {
            Email = "newuser@example.com",
            Password = "ValidPass1A",
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR"
        };

        var result = new UserCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("all mandatory fields are valid and the password meets the policy");
    }

    [Fact]
    public void UserCreate_Should_Pass_With_Optional_Name_Fields()
    {
        var dto = new UserCreateDto
        {
            Email = "user@example.com",
            Password = "ValidPass1A",
            Locale = "en-US",
            Timezone = "America/New_York",
            Currency = "USD",
            FirstName = "John",
            LastName = "Doe"
        };

        var result = new UserCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("optional name fields do not affect validity");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void UserCreate_Should_Fail_When_Email_Invalid(string email)
    {
        var dto = new UserCreateDto
        {
            Email = email,
            Password = "ValidPass1A",
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR"
        };

        var result = new UserCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("an invalid email address must be rejected");
    }

    [Theory]
    [InlineData("short")]   // too short
    [InlineData("alllower1")] // no uppercase
    [InlineData("ALLUPPER1")] // no lowercase
    [InlineData("NoDigitsAa")] // no digit
    public void UserCreate_Should_Fail_When_Password_Violates_Policy(string password)
    {
        var dto = new UserCreateDto
        {
            Email = "user@example.com",
            Password = password,
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR"
        };

        var result = new UserCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse($"password '{password}' violates the password policy");
    }

    [Fact]
    public void UserCreate_Should_Fail_When_Locale_Empty()
    {
        var dto = new UserCreateDto
        {
            Email = "user@example.com",
            Password = "ValidPass1A",
            Locale = string.Empty,
            Timezone = "Europe/Berlin",
            Currency = "EUR"
        };

        var result = new UserCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("an empty locale must be rejected");
    }

    [Fact]
    public void UserCreate_Should_Fail_When_Currency_Not_Three_Characters()
    {
        var dto = new UserCreateDto
        {
            Email = "user@example.com",
            Password = "ValidPass1A",
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EU"  // only 2 chars, not the required 3
        };

        var result = new UserCreateValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("a currency code shorter than 3 characters must be rejected");
    }

    // ─── UserEditValidator ───────────────────────────────────────────────────

    [Fact]
    public void UserEdit_Should_Pass_For_Valid_Dto()
    {
        var dto = new UserEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1, 2, 3 },
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR"
        };

        var result = new UserEditValidator().Validate(dto);

        result.IsValid.Should().BeTrue("all mandatory fields are provided and valid");
    }

    [Fact]
    public void UserEdit_Should_Fail_When_Id_Empty()
    {
        var dto = new UserEditDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 },
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR"
        };

        var result = new UserEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("an empty (default) Guid must be rejected as an Id");
    }

    [Fact]
    public void UserEdit_Should_Fail_When_RowVersion_Null()
    {
        var dto = new UserEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = null!,
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR"
        };

        var result = new UserEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("a null RowVersion must be rejected to enforce optimistic concurrency");
    }

    [Fact]
    public void UserEdit_Should_Fail_When_Currency_Wrong_Length()
    {
        var dto = new UserEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Locale = "en-US",
            Timezone = "UTC",
            Currency = "USDD"  // 4 chars instead of 3
        };

        var result = new UserEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("a 4-character currency code violates the exact-length-3 rule");
    }

    // ─── UserAdminSetPasswordValidator ───────────────────────────────────────

    [Fact]
    public void UserAdminSetPassword_Should_Pass_For_Valid_Input()
    {
        var dto = new UserAdminSetPasswordDto
        {
            Id = Guid.NewGuid(),
            NewPassword = "AdminSet1A"
        };

        var result = new UserAdminSetPasswordValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a valid user Id and policy-compliant password should pass");
    }

    [Fact]
    public void UserAdminSetPassword_Should_Fail_When_Id_Empty()
    {
        var dto = new UserAdminSetPasswordDto
        {
            Id = Guid.Empty,
            NewPassword = "AdminSet1A"
        };

        var result = new UserAdminSetPasswordValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("an empty Guid for Id must be rejected");
    }

    [Fact]
    public void UserAdminSetPassword_Should_Fail_When_Password_Violates_Policy()
    {
        var dto = new UserAdminSetPasswordDto
        {
            Id = Guid.NewGuid(),
            NewPassword = "weak"
        };

        var result = new UserAdminSetPasswordValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("a weak password should fail the policy check");
    }

    // ─── UserChangePasswordValidator ─────────────────────────────────────────

    [Fact]
    public void UserChangePassword_Should_Pass_For_Valid_Input()
    {
        var dto = new UserChangePasswordDto
        {
            Id = Guid.NewGuid(),
            CurrentPassword = "OldPass1A",
            NewPassword = "NewPass1A"
        };

        var result = new UserChangePasswordValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("all required fields satisfy the constraints");
    }

    [Fact]
    public void UserChangePassword_Should_Fail_When_Id_Empty()
    {
        var dto = new UserChangePasswordDto
        {
            Id = Guid.Empty,
            CurrentPassword = "OldPass1A",
            NewPassword = "NewPass1A"
        };

        var result = new UserChangePasswordValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("empty Guid for Id must be rejected");
    }

    [Fact]
    public void UserChangePassword_Should_Fail_When_CurrentPassword_Empty()
    {
        var dto = new UserChangePasswordDto
        {
            Id = Guid.NewGuid(),
            CurrentPassword = string.Empty,
            NewPassword = "NewPass1A"
        };

        var result = new UserChangePasswordValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("an empty current password must be rejected");
    }

    [Fact]
    public void UserChangePassword_Should_Fail_When_NewPassword_Violates_Policy()
    {
        var dto = new UserChangePasswordDto
        {
            Id = Guid.NewGuid(),
            CurrentPassword = "OldPass1A",
            NewPassword = "weak"
        };

        var result = new UserChangePasswordValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("a new password that violates the policy must be rejected");
    }

    // ─── UserAdminActionValidator ────────────────────────────────────────────

    [Fact]
    public void UserAdminAction_Should_Pass_For_Valid_UserId()
    {
        var dto = new UserAdminActionDto { Id = Guid.NewGuid() };

        var result = new UserAdminActionValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a non-empty Guid satisfies the sole requirement");
    }

    [Fact]
    public void UserAdminAction_Should_Fail_When_Id_Is_Empty()
    {
        var dto = new UserAdminActionDto { Id = Guid.Empty };

        var result = new UserAdminActionValidator().Validate(dto);

        result.IsValid.Should().BeFalse("an empty Guid must be rejected as an invalid identifier");
    }
}
