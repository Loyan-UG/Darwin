using Darwin.Application;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Unit tests for password-reset and email confirmation validators:
/// <see cref="RequestPasswordResetValidator"/>, <see cref="RequestEmailConfirmationValidator"/>,
/// <see cref="ConfirmEmailValidator"/>, and <see cref="ResetPasswordValidator"/>.
/// </summary>
public sealed class PasswordResetValidatorsTests
{
    private static IStringLocalizer<ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        return mock.Object;
    }

    // ─── RequestPasswordResetValidator ───────────────────────────────────────

    [Fact]
    public void RequestPasswordReset_Should_Pass_For_Valid_Email()
    {
        var dto = new RequestPasswordResetDto { Email = "user@example.com" };

        var result = new RequestPasswordResetValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a valid email address should be accepted");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public void RequestPasswordReset_Should_Fail_For_Invalid_Email(string email)
    {
        var dto = new RequestPasswordResetDto { Email = email };

        var result = new RequestPasswordResetValidator().Validate(dto);

        result.IsValid.Should().BeFalse($"email '{email}' is invalid and must be rejected");
    }

    // ─── RequestEmailConfirmationValidator ───────────────────────────────────

    [Fact]
    public void RequestEmailConfirmation_Should_Pass_For_Valid_Email()
    {
        var dto = new RequestEmailConfirmationDto { Email = "confirm@example.com" };

        var result = new RequestEmailConfirmationValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a valid email address should be accepted");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void RequestEmailConfirmation_Should_Fail_For_Invalid_Email(string email)
    {
        var dto = new RequestEmailConfirmationDto { Email = email };

        var result = new RequestEmailConfirmationValidator().Validate(dto);

        result.IsValid.Should().BeFalse($"email '{email}' is invalid and must be rejected");
    }

    // ─── ConfirmEmailValidator ───────────────────────────────────────────────

    [Fact]
    public void ConfirmEmail_Should_Pass_For_Valid_Email_And_Token()
    {
        var dto = new ConfirmEmailDto
        {
            Email = "user@example.com",
            Token = "confirmation-token-abc"
        };

        var result = new ConfirmEmailValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a valid email and non-empty token should pass");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void ConfirmEmail_Should_Fail_When_Email_Invalid(string email)
    {
        var dto = new ConfirmEmailDto
        {
            Email = email,
            Token = "some-token"
        };

        var result = new ConfirmEmailValidator().Validate(dto);

        result.IsValid.Should().BeFalse("an invalid or empty email must be rejected");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ConfirmEmail_Should_Fail_When_Token_Empty(string token)
    {
        var dto = new ConfirmEmailDto
        {
            Email = "user@example.com",
            Token = token
        };

        var result = new ConfirmEmailValidator().Validate(dto);

        result.IsValid.Should().BeFalse("an empty or whitespace token must be rejected");
    }

    // ─── ResetPasswordValidator ──────────────────────────────────────────────

    [Fact]
    public void ResetPassword_Should_Pass_For_Valid_Input()
    {
        var dto = new ResetPasswordDto
        {
            Email = "user@example.com",
            Token = "one-time-reset-token",
            NewPassword = "SecureNew1A"
        };

        var result = new ResetPasswordValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("all fields satisfy the password policy and format requirements");
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    public void ResetPassword_Should_Fail_When_Email_Invalid(string email)
    {
        var dto = new ResetPasswordDto
        {
            Email = email,
            Token = "some-token",
            NewPassword = "SecureNew1A"
        };

        var result = new ResetPasswordValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("an invalid or empty email must be rejected");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ResetPassword_Should_Fail_When_Token_Empty(string token)
    {
        var dto = new ResetPasswordDto
        {
            Email = "user@example.com",
            Token = token,
            NewPassword = "SecureNew1A"
        };

        var result = new ResetPasswordValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("an empty or whitespace token must be rejected");
    }

    [Theory]
    [InlineData("short")]    // too short, missing uppercase, digit
    [InlineData("alllower1")] // no uppercase
    [InlineData("ALLUPPER1")] // no lowercase
    [InlineData("NoDigitsAa")] // no digit
    public void ResetPassword_Should_Fail_When_NewPassword_Violates_Policy(string password)
    {
        var dto = new ResetPasswordDto
        {
            Email = "user@example.com",
            Token = "valid-token",
            NewPassword = password
        };

        var result = new ResetPasswordValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse($"password '{password}' violates the password policy");
    }

    [Fact]
    public void ResetPassword_Should_Fail_When_NewPassword_Empty()
    {
        var dto = new ResetPasswordDto
        {
            Email = "user@example.com",
            Token = "valid-token",
            NewPassword = string.Empty
        };

        var result = new ResetPasswordValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("an empty password must be rejected");
    }
}
