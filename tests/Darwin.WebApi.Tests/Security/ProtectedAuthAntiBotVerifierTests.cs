using System;
using Darwin.Application.Abstractions.Security;
using Darwin.WebApi.Security;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Moq;

namespace Darwin.WebApi.Tests.Security;

public sealed class ProtectedAuthAntiBotVerifierTests
{
    [Fact]
    public async Task VerifyAsync_Should_Succeed_WhenDisabled()
    {
        var (verifier, _) = CreateVerifier(new AuthAntiBotOptions { Enabled = false });
        var check = new AuthAntiBotCheck
        {
            HoneypotValue = "filled",
            ChallengeToken = null
        };

        var result = await verifier.VerifyAsync(check);

        result.Succeeded.Should().BeTrue();
        result.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task VerifyAsync_Should_Fail_WhenHoneypotValueIsFilled()
    {
        var (verifier, _) = CreateVerifier(new AuthAntiBotOptions { Enabled = true });

        var result = await verifier.VerifyAsync(new AuthAntiBotCheck
        {
            HoneypotValue = "I am a bot",
            ChallengeToken = "ignore"
        });

        result.Succeeded.Should().BeFalse();
        result.FailureReason.Should().Be("Honeypot was filled.");
    }

    [Fact]
    public async Task VerifyAsync_Should_Succeed_WhenChallengeMissing_AndChallengeNotRequired()
    {
        var (verifier, _) = CreateVerifier(
            new AuthAntiBotOptions
            {
                Enabled = true,
                RequireChallengeToken = false
            });

        var result = await verifier.VerifyAsync(new AuthAntiBotCheck
        {
            ChallengeToken = null
        });

        result.Succeeded.Should().BeTrue();
        result.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task VerifyAsync_Should_Fail_WhenChallengeMissing_AndChallengeRequired()
    {
        var (verifier, _) = CreateVerifier(
            new AuthAntiBotOptions
            {
                Enabled = true,
                RequireChallengeToken = true
            });

        var result = await verifier.VerifyAsync(new AuthAntiBotCheck
        {
            ChallengeToken = null
        });

        result.Succeeded.Should().BeFalse();
        result.FailureReason.Should().Be("Missing challenge token.");
    }

    [Fact]
    public async Task VerifyAsync_Should_Fail_WhenChallengeTokenCannotBeUnprotected()
    {
        var (verifier, _) = CreateVerifier(new AuthAntiBotOptions { Enabled = true });

        var result = await verifier.VerifyAsync(new AuthAntiBotCheck
        {
            ChallengeToken = "not-a-valid-token"
        });

        result.Succeeded.Should().BeFalse();
        result.FailureReason.Should().Be("Invalid challenge token.");
    }

    [Fact]
    public async Task VerifyAsync_Should_Fail_WhenSubmissionIsTooQuick()
    {
        var issuedAt = DateTimeOffset.UtcNow;
        var (verifier, protector) = CreateVerifier(
            new AuthAntiBotOptions
            {
                Enabled = true,
                MinimumFormSeconds = 120
            });

        var result = await verifier.VerifyAsync(new AuthAntiBotCheck
        {
            ChallengeToken = CreateChallengeToken(issuedAt, protector)
        });

        result.Succeeded.Should().BeFalse();
        result.FailureReason.Should().Be("Form submitted too quickly.");
    }

    [Fact]
    public async Task VerifyAsync_Should_Fail_WhenChallengeTokenHasExpired()
    {
        var (verifier, protector) = CreateVerifier(new AuthAntiBotOptions
        {
            Enabled = true,
            MinimumFormSeconds = 0,
            ChallengeTokenMaxAgeSeconds = 60
        });

        var result = await verifier.VerifyAsync(new AuthAntiBotCheck
        {
            ChallengeToken = CreateChallengeToken(DateTimeOffset.UtcNow.AddMinutes(-2), protector)
        });

        result.Succeeded.Should().BeFalse();
        result.FailureReason.Should().Be("Challenge token expired.");
    }

    [Fact]
    public async Task VerifyAsync_Should_Succeed_WhenSubmissionAgeIsInAcceptedRange()
    {
        var (verifier, protector) = CreateVerifier(new AuthAntiBotOptions
        {
            Enabled = true,
            MinimumFormSeconds = 2,
            ChallengeTokenMaxAgeSeconds = 120
        });

        var result = await verifier.VerifyAsync(new AuthAntiBotCheck
        {
            ChallengeToken = CreateChallengeToken(DateTimeOffset.UtcNow.AddSeconds(-30), protector)
        });

        result.Succeeded.Should().BeTrue();
        result.FailureReason.Should().BeNull();
    }

    private static (ProtectedAuthAntiBotVerifier Verifier, IDataProtector Protector) CreateVerifier(AuthAntiBotOptions options)
    {
        var provider = DataProtectionProvider.Create("Darwin.WebApi.Tests");
        var protector = provider.CreateProtector("Darwin.WebApi.AuthAntiBot.v1");

        var monitor = new Mock<IOptionsMonitor<AuthAntiBotOptions>>();
        monitor.SetupGet(x => x.CurrentValue).Returns(options);

        return (new ProtectedAuthAntiBotVerifier(provider, monitor.Object), protector);
    }

    private static string CreateChallengeToken(DateTimeOffset issuedAtUtc, IDataProtector protector)
    {
        return protector.Protect($"{issuedAtUtc.UtcTicks}:nonce");
    }
}
