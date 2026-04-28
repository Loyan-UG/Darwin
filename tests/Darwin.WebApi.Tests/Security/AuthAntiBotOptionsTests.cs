using Darwin.WebApi.Security;
using FluentAssertions;

namespace Darwin.WebApi.Tests.Security;

public sealed class AuthAntiBotOptionsTests
{
    [Fact]
    public void MinimumFormAge_Should_ClampToZero_WhenMinimumFormSecondsIsNegative()
    {
        var options = new AuthAntiBotOptions
        {
            MinimumFormSeconds = -5
        };

        options.MinimumFormAge.Should().Be(System.TimeSpan.Zero);
    }

    [Fact]
    public void MinimumFormAge_Should_ClampToSixty_WhenMinimumFormSecondsIsAboveSixty()
    {
        var options = new AuthAntiBotOptions
        {
            MinimumFormSeconds = 120
        };

        options.MinimumFormAge.Should().Be(System.TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void ChallengeTokenMaxAge_Should_ClampToOneMinute_WhenBelowRange()
    {
        var options = new AuthAntiBotOptions
        {
            ChallengeTokenMaxAgeSeconds = 30
        };

        options.ChallengeTokenMaxAge.Should().Be(System.TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void ChallengeTokenMaxAge_Should_ClampToOneHour_WhenAboveRange()
    {
        var options = new AuthAntiBotOptions
        {
            ChallengeTokenMaxAgeSeconds = 10_000
        };

        options.ChallengeTokenMaxAge.Should().Be(System.TimeSpan.FromSeconds(3600));
    }
}
