using System;
using Darwin.Application.Identity.Services;
using FluentAssertions;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Unit tests for <see cref="TotpUtility"/>.
/// Covers secret generation, OTP auth URI building, TOTP code computation, and verification.
/// </summary>
public sealed class TotpUtilityTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // GenerateSecretBase32
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateSecretBase32_Should_ReturnNonEmptyString()
    {
        var secret = TotpUtility.GenerateSecretBase32();
        secret.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateSecretBase32_Should_ReturnOnlyBase32Characters()
    {
        var secret = TotpUtility.GenerateSecretBase32();
        // Base32 alphabet: A-Z and 2-7 (no padding for this implementation)
        secret.Should().MatchRegex(@"^[A-Z2-7]+$", "generated secret must consist only of Base32 characters");
    }

    [Fact]
    public void GenerateSecretBase32_Should_ProduceUniqueValues()
    {
        var s1 = TotpUtility.GenerateSecretBase32();
        var s2 = TotpUtility.GenerateSecretBase32();
        s1.Should().NotBe(s2, "each call should produce a cryptographically random unique secret");
    }

    [Theory]
    [InlineData(20)]
    [InlineData(32)]
    [InlineData(10)]
    public void GenerateSecretBase32_Should_ProduceLongerSecretForMoreBytes(int byteLength)
    {
        var secret = TotpUtility.GenerateSecretBase32(byteLength);
        // Base32 encodes 5 bits per character; ceil(byteLength * 8 / 5) characters expected (without padding)
        var expectedMinLength = (byteLength * 8 + 4) / 5;
        secret.Length.Should().BeGreaterThanOrEqualTo(expectedMinLength - 1,
            "a longer byte length must produce a proportionally longer Base32 string");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BuildOtpAuthUri
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BuildOtpAuthUri_Should_StartWithOtpauthScheme()
    {
        var uri = TotpUtility.BuildOtpAuthUri("MyApp", "user@example.com", "JBSWY3DPEHPK3PXP");
        uri.Should().StartWith("otpauth://totp/");
    }

    [Fact]
    public void BuildOtpAuthUri_Should_ContainSecret()
    {
        var uri = TotpUtility.BuildOtpAuthUri("MyApp", "user@example.com", "MYSECRET");
        uri.Should().Contain("secret=MYSECRET");
    }

    [Fact]
    public void BuildOtpAuthUri_Should_ContainIssuer()
    {
        var uri = TotpUtility.BuildOtpAuthUri("MyApp", "user@example.com", "SECRET");
        uri.Should().Contain("issuer=MyApp");
    }

    [Fact]
    public void BuildOtpAuthUri_Should_ContainDefaultDigitsAndPeriod()
    {
        var uri = TotpUtility.BuildOtpAuthUri("MyApp", "user@example.com", "SECRET");
        uri.Should().Contain("digits=6");
        uri.Should().Contain("period=30");
    }

    [Fact]
    public void BuildOtpAuthUri_Should_ContainAlgorithm()
    {
        var uri = TotpUtility.BuildOtpAuthUri("MyApp", "user@example.com", "SECRET");
        uri.Should().Contain("algorithm=SHA1");
    }

    [Fact]
    public void BuildOtpAuthUri_Should_UrlEncodeLabel()
    {
        var uri = TotpUtility.BuildOtpAuthUri("My App", "user name@example.com", "SECRET");
        // The label is "My App:user name@example.com", URL-encoded in the path
        uri.Should().Contain("My%20App");
    }

    [Fact]
    public void BuildOtpAuthUri_Should_RespectCustomDigitsAndPeriod()
    {
        var uri = TotpUtility.BuildOtpAuthUri("App", "acct", "SEC", digits: 8, period: 60);
        uri.Should().Contain("digits=8");
        uri.Should().Contain("period=60");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ComputeTotpCode
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ComputeTotpCode_Should_ReturnValueInSixDigitRange()
    {
        var secret = TotpUtility.GenerateSecretBase32();
        var code = TotpUtility.ComputeTotpCode(secret, DateTime.UtcNow);
        code.Should().BeInRange(0, 999999, "a 6-digit TOTP code must be in [0, 999999]");
    }

    [Fact]
    public void ComputeTotpCode_Should_ReturnSameCodeWithinSameTimeStep()
    {
        var secret = TotpUtility.GenerateSecretBase32();
        var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var code1 = TotpUtility.ComputeTotpCode(secret, baseTime);
        var code2 = TotpUtility.ComputeTotpCode(secret, baseTime.AddSeconds(15)); // same 30s window

        code1.Should().Be(code2, "codes within the same 30-second window must match");
    }

    [Fact]
    public void ComputeTotpCode_Should_ReturnDifferentCodeAcrossTimeStepBoundary()
    {
        var secret = TotpUtility.GenerateSecretBase32();
        // Use a time exactly on a step boundary so adding 30s definitely crosses into the next step
        var epoch = DateTime.UnixEpoch;
        var step0Start = epoch.AddSeconds(30 * 1000);  // start of step 1000
        var step1Start = step0Start.AddSeconds(30);    // start of step 1001

        var code0 = TotpUtility.ComputeTotpCode(secret, step0Start);
        var code1 = TotpUtility.ComputeTotpCode(secret, step1Start);

        // Very unlikely to collide; confirms the counter advances correctly
        code0.Should().NotBe(code1,
            "adjacent time steps almost certainly produce different HOTP values");
    }

    [Fact]
    public void ComputeTotpCode_Should_RespectCustomPeriodAndDigits()
    {
        var secret = TotpUtility.GenerateSecretBase32();
        var code = TotpUtility.ComputeTotpCode(secret, DateTime.UtcNow, digits: 8, periodSeconds: 60);
        code.Should().BeInRange(0, 99_999_999, "an 8-digit TOTP code must be in [0, 99999999]");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // VerifyTotpCode
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void VerifyTotpCode_Should_ReturnTrue_ForCurrentCode()
    {
        var secret = TotpUtility.GenerateSecretBase32();
        var now = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var code = TotpUtility.ComputeTotpCode(secret, now);

        TotpUtility.VerifyTotpCode(secret, now, code).Should().BeTrue(
            "the code computed for the exact same instant must be accepted");
    }

    [Fact]
    public void VerifyTotpCode_Should_ReturnTrue_WhenClockSkewWithinAllowedWindow()
    {
        var secret = TotpUtility.GenerateSecretBase32();
        var now = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var code = TotpUtility.ComputeTotpCode(secret, now);

        // Verify 25 seconds later — still within 1 drift step of 30s
        TotpUtility.VerifyTotpCode(secret, now.AddSeconds(25), code, allowedDriftSteps: 1)
            .Should().BeTrue("clock skew within one time step should be accepted");
    }

    [Fact]
    public void VerifyTotpCode_Should_ReturnFalse_WhenCodeIsExpired()
    {
        var secret = TotpUtility.GenerateSecretBase32();
        var now = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var code = TotpUtility.ComputeTotpCode(secret, now);

        // Verify 5 minutes (10 steps) later with allowedDriftSteps=1
        TotpUtility.VerifyTotpCode(secret, now.AddSeconds(300), code, allowedDriftSteps: 1)
            .Should().BeFalse("a code that is many steps old must be rejected");
    }

    [Fact]
    public void VerifyTotpCode_Should_ReturnFalse_ForWrongCode()
    {
        var secret = TotpUtility.GenerateSecretBase32();
        var now = DateTime.UtcNow;

        // Use a code that is astronomically unlikely to be valid
        TotpUtility.VerifyTotpCode(secret, now, -1).Should().BeFalse(
            "a clearly invalid code must not be accepted");
    }

    [Fact]
    public void VerifyTotpCode_Should_ReturnTrue_ForCodeInPreviousStep_WithDriftWindow()
    {
        var secret = TotpUtility.GenerateSecretBase32();
        var stepStart = DateTime.UnixEpoch.AddSeconds(30 * 500); // start of step 500
        var previousCode = TotpUtility.ComputeTotpCode(secret, stepStart);

        // Verify from the start of step 501 (one step ahead) with drift=1
        TotpUtility.VerifyTotpCode(secret, stepStart.AddSeconds(30), previousCode, allowedDriftSteps: 1)
            .Should().BeTrue("a code from the previous step must be accepted within a drift window of 1");
    }
}
