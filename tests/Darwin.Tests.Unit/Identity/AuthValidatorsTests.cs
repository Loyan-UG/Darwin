using System;
using Darwin.Application;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Unit tests for identity authentication validators:
/// <see cref="SignInValidator"/>, <see cref="PasswordResetRequestValidator"/>,
/// <see cref="PasswordResetConfirmValidator"/>, <see cref="PasswordLoginRequestValidator"/>,
/// <see cref="RefreshRequestValidator"/>, and <see cref="RevokeRefreshRequestValidator"/>.
/// </summary>
public sealed class AuthValidatorsTests
{
    private static IStringLocalizer<ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        return mock.Object;
    }

    // ─── SignInValidator ─────────────────────────────────────────────────────

    [Fact]
    public void SignIn_Should_Pass_For_Valid_Email_And_Password()
    {
        var dto = new SignInDto { Email = "user@example.com", Password = "Secret123!" };

        var result = new SignInValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a non-empty valid email and non-empty password should pass");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public void SignIn_Should_Fail_When_Email_Invalid(string email)
    {
        var dto = new SignInDto { Email = email, Password = "AnyPass1" };

        var result = new SignInValidator().Validate(dto);

        result.IsValid.Should().BeFalse("an invalid or empty email address must be rejected");
    }

    [Fact]
    public void SignIn_Should_Fail_When_Password_Empty()
    {
        var dto = new SignInDto { Email = "user@example.com", Password = string.Empty };

        var result = new SignInValidator().Validate(dto);

        result.IsValid.Should().BeFalse("an empty password must be rejected");
    }

    // ─── PasswordResetRequestValidator ──────────────────────────────────────

    [Fact]
    public void PasswordResetRequest_Should_Pass_For_Valid_Email()
    {
        var dto = new PasswordResetRequestDto { Email = "reset@example.com" };

        var result = new PasswordResetRequestValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a valid email address should be accepted");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void PasswordResetRequest_Should_Fail_For_Invalid_Email(string email)
    {
        var dto = new PasswordResetRequestDto { Email = email };

        var result = new PasswordResetRequestValidator().Validate(dto);

        result.IsValid.Should().BeFalse("an empty or malformed email must be rejected");
    }

    // ─── PasswordResetConfirmValidator ───────────────────────────────────────

    [Fact]
    public void PasswordResetConfirm_Should_Pass_For_Valid_Input()
    {
        var dto = new PasswordResetConfirmDto
        {
            Email = "user@example.com",
            Token = "valid-reset-token",
            NewPassword = "NewPass1A"
        };

        var result = new PasswordResetConfirmValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("all required fields satisfy the password policy");
    }

    [Fact]
    public void PasswordResetConfirm_Should_Fail_When_Email_Invalid()
    {
        var dto = new PasswordResetConfirmDto
        {
            Email = "not-an-email",
            Token = "valid-token",
            NewPassword = "NewPass1A"
        };

        var result = new PasswordResetConfirmValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("an invalid email should fail");
    }

    [Fact]
    public void PasswordResetConfirm_Should_Fail_When_Token_Empty()
    {
        var dto = new PasswordResetConfirmDto
        {
            Email = "user@example.com",
            Token = string.Empty,
            NewPassword = "NewPass1A"
        };

        var result = new PasswordResetConfirmValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("an empty reset token must be rejected");
    }

    [Theory]
    [InlineData("short")]   // too short, no uppercase/digit
    [InlineData("alllower1")]  // no uppercase
    [InlineData("ALLUPPER1")]  // no lowercase
    [InlineData("NoDigitsAa")] // no digits
    public void PasswordResetConfirm_Should_Fail_When_Password_Violates_Policy(string password)
    {
        var dto = new PasswordResetConfirmDto
        {
            Email = "user@example.com",
            Token = "valid-token",
            NewPassword = password
        };

        var result = new PasswordResetConfirmValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse($"password '{password}' should fail password policy");
    }

    // ─── PasswordLoginRequestValidator ──────────────────────────────────────

    [Fact]
    public void PasswordLoginRequest_Should_Pass_For_Valid_Credentials()
    {
        var dto = new PasswordLoginRequestDto
        {
            Email = "user@example.com",
            PasswordPlain = "secret123"
        };

        var result = new PasswordLoginRequestValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a valid email and non-empty password of sufficient length should pass");
    }

    [Fact]
    public void PasswordLoginRequest_Should_Pass_With_Optional_DeviceId_And_BusinessId()
    {
        var dto = new PasswordLoginRequestDto
        {
            Email = "user@example.com",
            PasswordPlain = "validpw",
            DeviceId = "device-001",
            BusinessId = Guid.NewGuid()
        };

        var result = new PasswordLoginRequestValidator().Validate(dto);

        result.IsValid.Should().BeTrue("optional DeviceId and BusinessId do not affect base validity");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void PasswordLoginRequest_Should_Fail_When_Email_Invalid(string email)
    {
        var dto = new PasswordLoginRequestDto { Email = email, PasswordPlain = "valid123" };

        var result = new PasswordLoginRequestValidator().Validate(dto);

        result.IsValid.Should().BeFalse("an invalid or empty email must be rejected");
    }

    [Fact]
    public void PasswordLoginRequest_Should_Fail_When_Password_Empty()
    {
        var dto = new PasswordLoginRequestDto { Email = "user@example.com", PasswordPlain = string.Empty };

        var result = new PasswordLoginRequestValidator().Validate(dto);

        result.IsValid.Should().BeFalse("an empty password must be rejected");
    }

    [Fact]
    public void PasswordLoginRequest_Should_Fail_When_Password_Too_Short()
    {
        var dto = new PasswordLoginRequestDto { Email = "user@example.com", PasswordPlain = "abc" };

        var result = new PasswordLoginRequestValidator().Validate(dto);

        result.IsValid.Should().BeFalse("a password shorter than the minimum length must be rejected");
    }

    // ─── RefreshRequestValidator ─────────────────────────────────────────────

    [Fact]
    public void RefreshRequest_Should_Pass_When_RefreshToken_Provided()
    {
        var dto = new RefreshRequestDto { RefreshToken = "some-opaque-refresh-token" };

        var result = new RefreshRequestValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a non-empty refresh token is all that is required");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RefreshRequest_Should_Fail_When_RefreshToken_Empty(string token)
    {
        var dto = new RefreshRequestDto { RefreshToken = token };

        var result = new RefreshRequestValidator().Validate(dto);

        result.IsValid.Should().BeFalse("an empty or whitespace refresh token must be rejected");
    }

    // ─── RevokeRefreshRequestValidator ──────────────────────────────────────

    [Fact]
    public void RevokeRefreshRequest_Should_Pass_When_RefreshToken_Provided()
    {
        var dto = new RevokeRefreshRequestDto { RefreshToken = "opaque-token-abc" };

        var result = new RevokeRefreshRequestValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("providing a RefreshToken satisfies the either-or requirement");
    }

    [Fact]
    public void RevokeRefreshRequest_Should_Pass_When_UserId_Provided()
    {
        var dto = new RevokeRefreshRequestDto { UserId = Guid.NewGuid() };

        var result = new RevokeRefreshRequestValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("providing a UserId satisfies the either-or requirement");
    }

    [Fact]
    public void RevokeRefreshRequest_Should_Pass_When_Both_UserId_And_Token_Provided()
    {
        var dto = new RevokeRefreshRequestDto
        {
            UserId = Guid.NewGuid(),
            RefreshToken = "opaque-token-xyz"
        };

        var result = new RevokeRefreshRequestValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("providing both identifiers should pass");
    }

    [Fact]
    public void RevokeRefreshRequest_Should_Fail_When_Neither_UserId_Nor_Token_Provided()
    {
        var dto = new RevokeRefreshRequestDto { UserId = null, RefreshToken = null };

        var result = new RevokeRefreshRequestValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("at least one of UserId or RefreshToken must be provided");
    }

    [Fact]
    public void RevokeRefreshRequest_Should_Fail_When_RefreshToken_Is_Whitespace_And_No_UserId()
    {
        var dto = new RevokeRefreshRequestDto { UserId = null, RefreshToken = "   " };

        var result = new RevokeRefreshRequestValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("a whitespace-only RefreshToken with no UserId must fail");
    }
}
