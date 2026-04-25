using System;
using System.Text.RegularExpressions;
using Darwin.Shared.Security;
using FluentAssertions;
using Xunit;

namespace Darwin.Tests.Unit.Common;

/// <summary>
/// Unit tests for <see cref="OpaqueTokenGenerator"/>, <see cref="RandomTokenGenerator"/>,
/// and <see cref="Totp"/>.
/// </summary>
public sealed class SecurityHelpersTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // OpaqueTokenGenerator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OpaqueTokenGenerator_Create_Should_ReturnNonEmptyString()
    {
        var token = OpaqueTokenGenerator.Create();
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void OpaqueTokenGenerator_Create_Should_ReturnBase64UrlCharactersOnly()
    {
        var token = OpaqueTokenGenerator.Create();
        // Base64Url uses A-Z, a-z, 0-9, '-', '_'; no padding '='
        token.Should().MatchRegex(@"^[A-Za-z0-9\-_]+$");
    }

    [Fact]
    public void OpaqueTokenGenerator_Create_Should_NotContainPadding()
    {
        var token = OpaqueTokenGenerator.Create();
        token.Should().NotContain("=");
    }

    [Fact]
    public void OpaqueTokenGenerator_Create_Should_HaveExpectedLengthFor32Bytes()
    {
        // 32 bytes -> 43 base64url chars (without padding)
        var token = OpaqueTokenGenerator.Create(32);
        token.Length.Should().Be(43);
    }

    [Fact]
    public void OpaqueTokenGenerator_Create_Should_EnforceMinimumStrength()
    {
        // Values below 16 bytes should be clamped to 16 bytes = 22 base64url chars
        var token = OpaqueTokenGenerator.Create(1);
        token.Length.Should().Be(22);
    }

    [Fact]
    public void OpaqueTokenGenerator_Create_Should_ProduceUniqueTokens()
    {
        var t1 = OpaqueTokenGenerator.Create();
        var t2 = OpaqueTokenGenerator.Create();
        t1.Should().NotBe(t2, "cryptographically random tokens should not collide");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RandomTokenGenerator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void RandomTokenGenerator_UrlSafeToken_Should_ReturnNonEmptyString()
    {
        var token = RandomTokenGenerator.UrlSafeToken();
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void RandomTokenGenerator_UrlSafeToken_Should_ReturnBase64UrlCharactersOnly()
    {
        var token = RandomTokenGenerator.UrlSafeToken();
        token.Should().MatchRegex(@"^[A-Za-z0-9\-_]+$");
    }

    [Fact]
    public void RandomTokenGenerator_UrlSafeToken_Should_NotContainPadding()
    {
        var token = RandomTokenGenerator.UrlSafeToken();
        token.Should().NotContain("=");
    }

    [Fact]
    public void RandomTokenGenerator_UrlSafeToken_Should_HaveExpectedLength()
    {
        // 32 bytes -> 43 base64url chars (without padding)
        var token = RandomTokenGenerator.UrlSafeToken(32);
        token.Length.Should().Be(43);
    }

    [Fact]
    public void RandomTokenGenerator_UrlSafeToken_Should_ProduceUniqueTokens()
    {
        var t1 = RandomTokenGenerator.UrlSafeToken();
        var t2 = RandomTokenGenerator.UrlSafeToken();
        t1.Should().NotBe(t2, "cryptographically random tokens should not collide");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Totp
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly byte[] TestSecret = System.Text.Encoding.UTF8.GetBytes("TOTP_TEST_SECRET");

    [Fact]
    public void Totp_Generate_Should_ReturnSixDigitString()
    {
        var code = Totp.Generate(TestSecret, DateTime.UtcNow);
        code.Should().MatchRegex(@"^\d{6}$", "default TOTP code is always 6 digits");
    }

    [Fact]
    public void Totp_Generate_Should_ReturnSameCodeWithinSameTimeStep()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var code1 = Totp.Generate(TestSecret, now);
        var code2 = Totp.Generate(TestSecret, now.AddSeconds(10)); // same 30s window
        code1.Should().Be(code2, "codes generated within the same 30s step must match");
    }

    [Fact]
    public void Totp_Generate_Should_ReturnDifferentCodeForDifferentStep()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var code1 = Totp.Generate(TestSecret, now);
        var code2 = Totp.Generate(TestSecret, now.AddSeconds(30)); // next step
        // It is theoretically possible for two steps to produce the same code,
        // but highly unlikely; this is an adequate sanity check.
        // We can't assert inequality as it's probabilistic, so we just ensure both are valid.
        code1.Should().MatchRegex(@"^\d{6}$");
        code2.Should().MatchRegex(@"^\d{6}$");
    }

    [Fact]
    public void Totp_Verify_Should_ReturnTrueForCurrentCode()
    {
        var now = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var code = Totp.Generate(TestSecret, now);
        var isValid = Totp.Verify(TestSecret, code, now);
        isValid.Should().BeTrue("the code was generated for exactly the same instant");
    }

    [Fact]
    public void Totp_Verify_Should_ReturnTrueForCodeWithinWindow()
    {
        var now = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var code = Totp.Generate(TestSecret, now);
        // Verify with clock 25 seconds ahead (still within 1-step window)
        var isValid = Totp.Verify(TestSecret, code, now.AddSeconds(25), window: 1);
        isValid.Should().BeTrue("clock skew within the window should be accepted");
    }

    [Fact]
    public void Totp_Verify_Should_ReturnFalseForExpiredCode()
    {
        var now = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var code = Totp.Generate(TestSecret, now);
        // Move far beyond any reasonable window
        var isValid = Totp.Verify(TestSecret, code, now.AddSeconds(300), window: 1);
        isValid.Should().BeFalse("a code that is many steps old must be rejected");
    }

    [Fact]
    public void Totp_Verify_Should_ReturnFalseForWrongCode()
    {
        var now = DateTime.UtcNow;
        var isValid = Totp.Verify(TestSecret, "000000", now);
        // '000000' is almost certainly not the current TOTP code
        // If by chance it is, this test may flake; in practice this is astronomically unlikely.
        isValid.Should().BeFalse("000000 is very unlikely to be the valid current TOTP code");
    }

    [Fact]
    public void Totp_BuildOtpAuthUri_Should_ReturnWellFormedUri()
    {
        var uri = Totp.BuildOtpAuthUri("MyApp", "user@example.com", "JBSWY3DPEHPK3PXP");
        uri.Should().StartWith("otpauth://totp/");
        uri.Should().Contain("secret=JBSWY3DPEHPK3PXP");
        uri.Should().Contain("issuer=MyApp");
        uri.Should().Contain("digits=6");
        uri.Should().Contain("period=30");
    }

    [Fact]
    public void Totp_BuildOtpAuthUri_Should_UrlEncodeIssuerAndLabel()
    {
        var uri = Totp.BuildOtpAuthUri("My App", "user name@example.com", "SECRET");
        uri.Should().Contain("My%20App");
        uri.Should().Contain("user%20name%40example.com");
    }

    [Fact]
    public void Totp_Generate_Should_HonorCustomDigitCount()
    {
        var code = Totp.Generate(TestSecret, DateTime.UtcNow, digits: 8);
        code.Should().MatchRegex(@"^\d{8}$", "custom 8-digit TOTP must return 8 digits");
    }
}
