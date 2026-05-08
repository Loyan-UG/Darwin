using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Security;
using Darwin.Application.Abstractions.Services;
using Darwin.WebAdmin.Services.Security;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Darwin.WebAdmin.Tests.Security;

public sealed class ProtectedAuthAntiBotChallengeServiceTests
{
    private const string Purpose = "Darwin.WebAdmin.AuthAntiBot.v1";

    [Fact]
    public void Ctor_Should_CreateProtector_AndIssueToken()
    {
        string tempPath;
        var service = CreateService(out tempPath);

        try
        {
            var token = service.CreateChallengeToken();
            token.Should().NotBeNullOrWhiteSpace();
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task VerifyAsync_Should_ReturnSuccess_WhenDisabled()
    {
        string tempPath;
        var service = CreateService(out tempPath, new AuthAntiBotOptions { Enabled = false });

        try
        {
            var result = await service.VerifyAsync(new AuthAntiBotCheck());
            result.Succeeded.Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task VerifyAsync_Should_Fail_WhenHoneypotFilled()
    {
        string tempPath;
        var service = CreateService(out tempPath);

        try
        {
            var result = await service.VerifyAsync(new AuthAntiBotCheck { HoneypotValue = "bot" });
            result.Succeeded.Should().BeFalse();
            result.FailureReason.Should().Be("Honeypot was filled.");
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task VerifyAsync_Should_Fail_WhenTokenIsMissing_AndTokenRequired()
    {
        string tempPath;
        var service = CreateService(out tempPath);

        try
        {
            var result = await service.VerifyAsync(new AuthAntiBotCheck());
            result.Succeeded.Should().BeFalse();
            result.FailureReason.Should().Be("Missing challenge token.");
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task VerifyAsync_Should_Succeed_WhenTokenIsMissing_AndTokenNotRequired()
    {
        string tempPath;
        var service = CreateService(out tempPath, new AuthAntiBotOptions { RequireChallengeToken = false });

        try
        {
            var result = await service.VerifyAsync(new AuthAntiBotCheck());
            result.Succeeded.Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task VerifyAsync_Should_Fail_WhenChallengeTokenIsMalformed()
    {
        string tempPath;
        var service = CreateService(out tempPath);

        try
        {
            var result = await service.VerifyAsync(new AuthAntiBotCheck { ChallengeToken = "not-a-valid-payload" });
            result.Succeeded.Should().BeFalse();
            result.FailureReason.Should().Be("Invalid challenge token.");
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task VerifyAsync_Should_Fail_WhenFormSubmittedTooQuickly()
    {
        string tempPath;
        var service = CreateService(
            out tempPath,
            new AuthAntiBotOptions { MinimumFormSeconds = 10_000, ChallengeTokenMaxAgeSeconds = 120 });

        try
        {
            var token = service.CreateChallengeToken();
            var result = await service.VerifyAsync(new AuthAntiBotCheck { ChallengeToken = token });

            result.Succeeded.Should().BeFalse();
            result.FailureReason.Should().Be("Form submitted too quickly.");
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task VerifyAsync_Should_Fail_WhenHoneypotContainsOnlyWhitespace()
    {
        string tempPath;
        var service = CreateService(out tempPath);

        try
        {
            var result = await service.VerifyAsync(new AuthAntiBotCheck { HoneypotValue = "   " });

            result.Succeeded.Should().BeFalse();
            result.FailureReason.Should().Be("Honeypot was filled.");
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task VerifyAsync_Should_Fail_WhenChallengeTokenExpired()
    {
        string tempPath;
        var service = CreateService(out tempPath, new AuthAntiBotOptions { ChallengeTokenMaxAgeSeconds = 60 });

        try
        {
            var expiredAt = DateTimeOffset.UtcNow.AddSeconds(-61);
            var token = BuildTokenWithIssuedAt(expiredAt, tempPath);

            var result = await service.VerifyAsync(new AuthAntiBotCheck { ChallengeToken = token });
            result.Succeeded.Should().BeFalse();
            result.FailureReason.Should().Be("Challenge token expired.");
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task VerifyAsync_Should_Fail_WhenTokenIssuedInFuture()
    {
        string tempPath;
        var service = CreateService(out tempPath, new AuthAntiBotOptions { MinimumFormSeconds = 2, ChallengeTokenMaxAgeSeconds = 60 });

        try
        {
            var issuedInFuture = DateTimeOffset.UtcNow.AddSeconds(90);
            var token = BuildTokenWithIssuedAt(issuedInFuture, tempPath);

            var result = await service.VerifyAsync(new AuthAntiBotCheck { ChallengeToken = token });

            result.Succeeded.Should().BeFalse();
            result.FailureReason.Should().Be("Form submitted too quickly.");
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task VerifyAsync_Should_Succeed_WithFreshToken()
    {
        string tempPath;
        var service = CreateService(out tempPath, new AuthAntiBotOptions { MinimumFormSeconds = 1 });

        try
        {
            var token = BuildTokenWithIssuedAt(DateTimeOffset.UtcNow.AddSeconds(-2), tempPath);

            var result = await service.VerifyAsync(new AuthAntiBotCheck { ChallengeToken = token });
            result.Succeeded.Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task VerifyAsync_Should_Succeed_WhenMinimumFormSecondsIsZero()
    {
        string tempPath;
        var service = CreateService(out tempPath, new AuthAntiBotOptions { MinimumFormSeconds = 0 });

        try
        {
            var token = BuildTokenWithIssuedAt(DateTimeOffset.UtcNow, tempPath);
            var result = await service.VerifyAsync(new AuthAntiBotCheck { ChallengeToken = token });

            result.Succeeded.Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task VerifyAsync_Should_FailWhenTokenIsRequiredButEmptyStringProvided()
    {
        string tempPath;
        var service = CreateService(out tempPath);

        try
        {
            var result = await service.VerifyAsync(new AuthAntiBotCheck { ChallengeToken = string.Empty });

            result.Succeeded.Should().BeFalse();
            result.FailureReason.Should().Be("Missing challenge token.");
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task VerifyAsync_Should_IgnoreHoneypotWhenDisabled()
    {
        string tempPath;
        var service = CreateService(out tempPath, new AuthAntiBotOptions { Enabled = false });

        try
        {
            var result = await service.VerifyAsync(new AuthAntiBotCheck
            {
                HoneypotValue = "bot",
                ChallengeToken = string.Empty
            });

            result.Succeeded.Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task VerifyAsync_Should_Succeed_WhenTokenAgeEqualsMaxAge()
    {
        string tempPath;
        var service = CreateService(out tempPath, new AuthAntiBotOptions { ChallengeTokenMaxAgeSeconds = 30 });

        try
        {
            var issuedAt = DateTimeOffset.UtcNow.AddSeconds(-30);
            var token = BuildTokenWithIssuedAt(issuedAt, tempPath);

            var result = await service.VerifyAsync(new AuthAntiBotCheck { ChallengeToken = token });

            result.Succeeded.Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task VerifyAsync_Should_Succeed_WhenTokenAgeIsJustOverMinimum()
    {
        string tempPath;
        var service = CreateService(out tempPath, new AuthAntiBotOptions { MinimumFormSeconds = 2 });

        try
        {
            var token = BuildTokenWithIssuedAt(DateTimeOffset.UtcNow.AddSeconds(-3), tempPath);

            var result = await service.VerifyAsync(new AuthAntiBotCheck { ChallengeToken = token });

            result.Succeeded.Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public async Task VerifyAsync_Should_Fail_WhenChallengeTokenCannotBeParsed()
    {
        string tempPath;
        var service = CreateService(out tempPath);

        try
        {
            var token = BuildTokenWithPayload("not-a-timestamp", tempPath);
            var result = await service.VerifyAsync(new AuthAntiBotCheck { ChallengeToken = token });

            result.Succeeded.Should().BeFalse();
            result.FailureReason.Should().Be("Invalid challenge token.");
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public void MinimumFormAge_Should_BeClampedToRange()
    {
        var options = new AuthAntiBotOptions
        {
            MinimumFormSeconds = -1,
            ChallengeTokenMaxAgeSeconds = 10_000,
        };

        options.MinimumFormAge.Should().Be(TimeSpan.Zero);
        options.ChallengeTokenMaxAge.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void ChallengeTokenMaxAge_Should_BeClampedToRange()
    {
        var options = new AuthAntiBotOptions
        {
            MinimumFormSeconds = 5,
            ChallengeTokenMaxAgeSeconds = 30
        };

        options.ChallengeTokenMaxAge.Should().Be(TimeSpan.FromMinutes(1));
    }

    private static ProtectedAuthAntiBotChallengeService CreateService(
        out string tempPath,
        AuthAntiBotOptions? options = null)
    {
        tempPath = Path.Combine(Path.GetTempPath(), "darwin-abot-service-" + Guid.NewGuid());
        Directory.CreateDirectory(tempPath);
        var provider = DataProtectionProvider.Create(tempPath);
        return new ProtectedAuthAntiBotChallengeService(
            provider,
            new TestClock(),
            new TestOptionsMonitor(options ?? new AuthAntiBotOptions()));
    }

    private static string BuildTokenWithIssuedAt(DateTimeOffset issuedAtUtc, string tempPath)
    {
        return BuildTokenWithPayload($"{issuedAtUtc.UtcTicks}:{RandomHexNonce()}", tempPath);
    }

    private static string BuildTokenWithPayload(string payload, string tempPath)
    {
        var provider = DataProtectionProvider.Create(tempPath);
        var protector = provider.CreateProtector(Purpose);
        return protector.Protect(payload);
    }

    private static string RandomHexNonce()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
    }
}

sealed class TestOptionsMonitor : IOptionsMonitor<AuthAntiBotOptions>
{
    private readonly AuthAntiBotOptions _current;

    public TestOptionsMonitor(AuthAntiBotOptions current)
    {
        _current = current;
    }

    public AuthAntiBotOptions CurrentValue => _current;
    public AuthAntiBotOptions Get(string? name) => _current;
    public IDisposable OnChange(Action<AuthAntiBotOptions, string?> listener) => new NullDisposable();
}

sealed class TestClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

sealed class NullDisposable : IDisposable
{
    public void Dispose()
    {
    }
}
