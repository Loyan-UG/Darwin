using Darwin.Application.Common;
using FluentAssertions;

namespace Darwin.Tests.Unit.Common;

/// <summary>
/// Unit tests for <see cref="OperatorDisplayTextSanitizer.SanitizeFailureText"/>.
/// Verifies null/whitespace pass-through, sensitive-marker redaction,
/// email and phone masking, and length truncation behaviour.
/// </summary>
public sealed class OperatorDisplayTextSanitizerTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Null / empty / whitespace
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SanitizeFailureText_Should_Return_Null_For_Null_Input()
    {
        var result = OperatorDisplayTextSanitizer.SanitizeFailureText(null);

        result.Should().BeNull();
    }

    [Fact]
    public void SanitizeFailureText_Should_Return_Null_For_Empty_String()
    {
        var result = OperatorDisplayTextSanitizer.SanitizeFailureText(string.Empty);

        result.Should().BeNull();
    }

    [Fact]
    public void SanitizeFailureText_Should_Return_Null_For_Whitespace_Only()
    {
        var result = OperatorDisplayTextSanitizer.SanitizeFailureText("   ");

        result.Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Sensitive-marker redaction
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("contains secret key")]
    [InlineData("Bearer token expired")]
    [InlineData("wrong password supplied")]
    [InlineData("Authorization header missing")]
    [InlineData("invalid signature detected")]
    [InlineData("api_key is invalid")]
    [InlineData("apikey not recognised")]
    public void SanitizeFailureText_Should_Redact_When_Sensitive_Marker_Present(string sensitiveText)
    {
        var result = OperatorDisplayTextSanitizer.SanitizeFailureText(sensitiveText);

        result.Should().Be(
            "Delivery failure details were captured but are redacted for operator display.",
            $"the text contains a sensitive keyword and must be redacted (input: '{sensitiveText}')");
    }

    [Theory]
    [InlineData("SECRET")]
    [InlineData("TOKEN")]
    [InlineData("PASSWORD")]
    [InlineData("AUTHORIZATION")]
    [InlineData("SIGNATURE")]
    [InlineData("API_KEY")]
    [InlineData("APIKEY")]
    public void SanitizeFailureText_Should_Redact_Case_Insensitively(string upperCaseSensitiveWord)
    {
        var result = OperatorDisplayTextSanitizer.SanitizeFailureText(upperCaseSensitiveWord);

        result.Should().NotBeNullOrEmpty();
        result.Should().Be(
            "Delivery failure details were captured but are redacted for operator display.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Email masking
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SanitizeFailureText_Should_Mask_Email_Addresses()
    {
        var result = OperatorDisplayTextSanitizer.SanitizeFailureText("Sending to john.doe@example.com failed");

        result.Should().NotContain("john.doe@example.com", "original email must be masked");
        result.Should().Contain("@example.com", "domain should remain visible");
    }

    [Fact]
    public void SanitizeFailureText_Should_Keep_Two_Char_Prefix_In_Masked_Email()
    {
        var result = OperatorDisplayTextSanitizer.SanitizeFailureText("user alice@example.org not found");

        // 'alice' → 'al***@example.org'
        result.Should().Contain("al***@example.org");
    }

    [Fact]
    public void SanitizeFailureText_Should_Mask_Short_Local_Part_Email()
    {
        // Single-char local part: 'a@b.com' → 'a***@b.com'
        var result = OperatorDisplayTextSanitizer.SanitizeFailureText("bounce from a@b.com");

        result.Should().Contain("@b.com");
        result.Should().NotContain("a@b.com", "short local should be partially masked");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Phone masking
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SanitizeFailureText_Should_Mask_Phone_Numbers_Keeping_Last_Four_Digits()
    {
        var result = OperatorDisplayTextSanitizer.SanitizeFailureText("Call from +49 123 456789 unreachable");

        result.Should().NotContain("+49 123 456789", "phone number should be masked");
        result.Should().Contain("***6789", "last four digits should remain visible");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Plain text pass-through
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SanitizeFailureText_Should_Return_Plain_Text_Unchanged_When_No_Sensitive_Content()
    {
        const string safe = "SMTP server connection refused";

        var result = OperatorDisplayTextSanitizer.SanitizeFailureText(safe);

        result.Should().Be(safe);
    }

    [Fact]
    public void SanitizeFailureText_Should_Trim_Leading_And_Trailing_Whitespace()
    {
        var result = OperatorDisplayTextSanitizer.SanitizeFailureText("  connection timed out  ");

        result.Should().Be("connection timed out");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Truncation
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SanitizeFailureText_Should_Truncate_Long_Text_At_MaxLength()
    {
        var longText = new string('X', 300);

        var result = OperatorDisplayTextSanitizer.SanitizeFailureText(longText);

        result.Should().NotBeNull();
        result!.Length.Should().BeLessThanOrEqualTo(220, "default maxLength is 220");
        result.Should().EndWith("...", "truncated text must end with ellipsis");
    }

    [Fact]
    public void SanitizeFailureText_Should_Not_Truncate_Text_At_Or_Below_MaxLength()
    {
        var text = new string('Y', 220);

        var result = OperatorDisplayTextSanitizer.SanitizeFailureText(text);

        result.Should().Be(text, "text at exactly maxLength must not be truncated");
    }

    [Fact]
    public void SanitizeFailureText_Should_Respect_Custom_MaxLength()
    {
        var text = new string('Z', 60);

        var result = OperatorDisplayTextSanitizer.SanitizeFailureText(text, maxLength: 50);

        result.Should().NotBeNull();
        result!.Length.Should().BeLessThanOrEqualTo(50);
        result.Should().EndWith("...");
    }
}
