using System;
using System.Text;
using Darwin.Shared.Security;
using FluentAssertions;

namespace Darwin.Mobile.Shared.Tests.Security;

public sealed class TokenGeneratorTests
{
    [Fact]
    public void OpaqueTokenGenerator_Should_GenerateUrlSafeToken_WithMinimumLengthAndNoPadding()
    {
        var token = OpaqueTokenGenerator.Create(1);

        token.Should().NotBeNullOrWhiteSpace();
        token.Length.Should().Be(22);
        token.Should().NotContain("=");
        token.Should().MatchRegex("^[A-Za-z0-9_-]+$");
    }

    [Fact]
    public void OpaqueTokenGenerator_Should_EnforceMinimumBytes_AtLeast16()
    {
        var small = OpaqueTokenGenerator.Create(2);
        var normal = OpaqueTokenGenerator.Create(16);

        small.Length.Should().Be(normal.Length);
        small.Should().NotBeNullOrWhiteSpace();
        small.Length.Should().Be(22);
        normal.Length.Should().Be(22);
        small.Should().MatchRegex("^[A-Za-z0-9_-]+$");
        normal.Should().MatchRegex("^[A-Za-z0-9_-]+$");
    }

    [Fact]
    public void OpaqueTokenGenerator_Should_GenerateDistinctTokens()
    {
        var first = OpaqueTokenGenerator.Create(16);
        var second = OpaqueTokenGenerator.Create(16);

        first.Should().NotBe(second);
    }

    [Fact]
    public void RandomTokenGenerator_Should_GenerateUrlSafeToken_WithoutEqualsPadding()
    {
        var token = RandomTokenGenerator.UrlSafeToken();

        token.Should().NotBeNullOrWhiteSpace();
        token.Should().NotContain("+");
        token.Should().NotContain("/");
        token.Should().NotContain("=");
        token.Should().MatchRegex("^[A-Za-z0-9_-]+$");
    }

    [Fact]
    public void RandomTokenGenerator_Should_ReturnDifferentValues()
    {
        var first = RandomTokenGenerator.UrlSafeToken(16);
        var second = RandomTokenGenerator.UrlSafeToken(16);

        first.Should().NotBe(second);
    }

    [Fact]
    public void RandomTokenGenerator_WithZeroBytes_Should_ReturnEmptyString()
    {
        var token = RandomTokenGenerator.UrlSafeToken(0);

        token.Should().Be(string.Empty);
    }

    [Fact]
    public void Totp_Generate_Should_ReturnDigitsWithConfiguredLength()
    {
        var secret = Encoding.UTF8.GetBytes("12345678901234567890");
        var utcNow = new DateTime(2026, 4, 27, 10, 0, 0, DateTimeKind.Utc);

        var code = Totp.Generate(secret, utcNow, stepSeconds: 30, digits: 8);

        code.Should().HaveLength(8);
        code.Should().MatchRegex("^\\d{8}$");
    }

    [Fact]
    public void Totp_Verify_Should_RespectWindow_WhenCheckingSkewedSubmission()
    {
        var secret = Encoding.UTF8.GetBytes("12345678901234567890");
        var baseTime = new DateTime(2026, 4, 27, 10, 0, 0, DateTimeKind.Utc);
        var issuedCode = Totp.Generate(secret, baseTime, stepSeconds: 30, digits: 6);

        Totp.Verify(secret, issuedCode, baseTime.AddSeconds(31), stepSeconds: 30, digits: 6, window: 0)
            .Should().BeFalse();
        Totp.Verify(secret, issuedCode, baseTime.AddSeconds(31), stepSeconds: 30, digits: 6, window: 1)
            .Should().BeTrue();
    }

    [Fact]
    public void Totp_BuildOtpAuthUri_Should_EncodeIssuerAndAccount()
    {
        const string issuer = "Darwin App";
        const string accountLabel = "user@example.com";
        const string secret = "JBSWY3DPEHPK3PXP";

        var uri = Totp.BuildOtpAuthUri(issuer, accountLabel, secret);

        uri.Should().Be(
            "otpauth://totp/Darwin%20App:user%40example.com?secret=JBSWY3DPEHPK3PXP&issuer=Darwin%20App&digits=6&period=30");
    }
}
