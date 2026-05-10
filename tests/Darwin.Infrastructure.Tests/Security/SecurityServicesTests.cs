using System;
using System.IO;
using System.Text.RegularExpressions;
using Darwin.Application.Abstractions.Services;
using Darwin.Infrastructure.Security;
using Darwin.Infrastructure.Security.LoginRateLimiter;
using Darwin.Infrastructure.Security.Secrets;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Xunit;

namespace Darwin.Infrastructure.Tests.Security;

public sealed class Argon2PasswordHasherTests
{
    [Fact]
    public void Hash_Should_Throw_WhenPasswordIsNull()
    {
        var hasher = new Argon2PasswordHasher();

        Action act = () => hasher.Hash(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("password");
    }

    [Fact]
    public void Verify_Should_Throw_WhenHashedPasswordIsNull()
    {
        var hasher = new Argon2PasswordHasher();

        Action act = () => hasher.Verify(null!, "password");

        act.Should().Throw<ArgumentNullException>().WithParameterName("hashedPassword");
    }

    [Fact]
    public void Verify_Should_Throw_WhenProvidedPasswordIsNull()
    {
        var hasher = new Argon2PasswordHasher();
        var hash = hasher.Hash("correct-password");

        Action act = () => hasher.Verify(hash, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("providedPassword");
    }

    [Fact]
    public void Hash_Should_ProduceArgon2PhcString()
    {
        var hasher = new Argon2PasswordHasher();

        var hash = hasher.Hash("Password123!");

        hash.Should().NotBeNullOrWhiteSpace();
        Regex.IsMatch(hash, @"^\$argon2id\$v=\d+\$m=\d+,t=\d+,p=\d+\$.+\$.+").Should().BeTrue();
    }

    [Fact]
    public void Hash_Should_BeVerifiable_WithSamePassword()
    {
        var hasher = new Argon2PasswordHasher();
        var password = "CorrectHorseBatteryStaple";
        var hash = hasher.Hash(password);

        hasher.Verify(hash, password).Should().BeTrue();
    }

    [Fact]
    public void Hash_Should_GenerateDifferentValue_ForSamePassword()
    {
        var hasher = new Argon2PasswordHasher();

        var first = hasher.Hash("repeat-me");
        var second = hasher.Hash("repeat-me");

        first.Should().NotBe(second);
    }

    [Fact]
    public void Verify_Should_ReturnFalse_ForWrongPassword()
    {
        var hasher = new Argon2PasswordHasher();
        var hash = hasher.Hash("expected-password");

        hasher.Verify(hash, "wrong-password").Should().BeFalse();
    }

    [Fact]
    public void Verify_Should_ReturnFalse_ForMalformedHash()
    {
        var hasher = new Argon2PasswordHasher();

        hasher.Verify("not-a-valid-argon2-hash", "password").Should().BeFalse();
    }
}

public sealed class SecurityStampServiceTests
{
    [Fact]
    public void NewStamp_Should_ReturnLowerHex_64Characters()
    {
        var service = new SecurityStampService();

        var stamp = service.NewStamp();

        stamp.Should().NotBeNullOrWhiteSpace();
        stamp.Length.Should().Be(64);
        Regex.IsMatch(stamp, @"\A[a-f0-9]{64}\z").Should().BeTrue();
    }

    [Fact]
    public void NewStamp_Should_GenerateDifferentStamps()
    {
        var service = new SecurityStampService();

        var first = service.NewStamp();
        var second = service.NewStamp();

        first.Should().NotBe(second);
    }

    [Fact]
    public void AreEqual_Should_CompareByLengthAndContent()
    {
        var service = new SecurityStampService();

        service.AreEqual("abc", "abc").Should().BeTrue();
        service.AreEqual("abc", "abd").Should().BeFalse();
    }

    [Fact]
    public void AreEqual_Should_TreatNullAndEmptyAsSame()
    {
        var service = new SecurityStampService();

        service.AreEqual(null, null).Should().BeTrue();
        service.AreEqual(null, string.Empty).Should().BeTrue();
        service.AreEqual(string.Empty, null).Should().BeTrue();
        service.AreEqual(string.Empty, string.Empty).Should().BeTrue();
    }

    [Fact]
    public void AreEqual_Should_RejectDifferentLengthStrings()
    {
        var service = new SecurityStampService();

        service.AreEqual("aaaa", "aaa").Should().BeFalse();
        service.AreEqual("short", string.Empty).Should().BeFalse();
    }

    [Fact]
    public void Equals_Should_RespectLegacyOverload()
    {
        var service = new SecurityStampService();

        service.Equals("legacy", "legacy").Should().BeTrue();
        service.Equals("legacy", "other").Should().BeFalse();
    }
}

public sealed class TotpServiceTests
{
    private const string SampleSecret = "JBSWY3DPEHPK3PXP";
    private static readonly DateTime FixedUtc = new(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void GenerateCode_Should_ReturnSixDigitCode()
    {
        var service = CreateTotpService();

        var code = service.GenerateCode(SampleSecret);

        code.Should().HaveLength(6);
        code.Should().MatchRegex(@"^\d{6}$");
    }

    [Fact]
    public void VerifyCode_Should_ReturnTrue_ForCurrentCode()
    {
        var service = CreateTotpService();
        var code = service.GenerateCode(SampleSecret);

        service.VerifyCode(SampleSecret, code).Should().BeTrue();
    }

    [Fact]
    public void VerifyCode_Should_ReturnFalse_ForWhitespaceInput()
    {
        var service = CreateTotpService();

        service.VerifyCode("   ", "123456").Should().BeFalse();
        service.VerifyCode(SampleSecret, "   ").Should().BeFalse();
        service.VerifyCode("   ", "   ").Should().BeFalse();
    }

    [Fact]
    public void VerifyCode_Should_ReturnFalse_ForWrongCode()
    {
        var service = CreateTotpService();

        service.VerifyCode(SampleSecret, "000000").Should().BeFalse();
    }

    [Fact]
    public void VerifyCode_Should_ReturnFalse_ForNonNumericCode()
    {
        var service = CreateTotpService();

        service.VerifyCode(SampleSecret, "12a456").Should().BeFalse();
    }

    [Fact]
    public void VerifyCode_Should_PropagateInvalidSecretFormat()
    {
        var service = CreateTotpService();

        Action act = () => service.VerifyCode("invalid-base32!", "123456");

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void VerifyCode_Should_HandleLowercaseSecretAndClampNegativeWindowToCurrentStep()
    {
        var service = CreateTotpService();
        var code = service.GenerateCode(SampleSecret.ToLowerInvariant());

        service.VerifyCode(SampleSecret.ToUpperInvariant(), code).Should().BeTrue();
        service.VerifyCode(SampleSecret.ToUpperInvariant(), code, window: -1).Should().BeTrue();
    }

    [Fact]
    public void GenerateCode_Should_HandlePaddedBase32Secret()
    {
        var service = CreateTotpService();

        var padded = $"{SampleSecret}====";
        var code = service.GenerateCode(padded);

        code.Should().HaveLength(6);
        code.Should().MatchRegex(@"^\d{6}$");
        service.VerifyCode(padded, code).Should().BeTrue();
    }

    [Fact]
    public void GenerateCode_Should_AcceptLowercaseSecret()
    {
        var service = CreateTotpService();

        var code = service.GenerateCode(SampleSecret.ToLowerInvariant());

        code.Should().HaveLength(6);
        code.Should().MatchRegex(@"^\d{6}$");
    }

    private static TotpService CreateTotpService()
    {
        return new TotpService(new TestClock(FixedUtc));
    }
}

public sealed class MemoryLoginRateLimiterTests
{
    [Fact]
    public async System.Threading.Tasks.Task IsAllowedAsync_Should_AllowInitially()
    {
        var clock = new TestClock(new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc));
        var limiter = new MemoryLoginRateLimiter(clock);

        var allowed = await limiter.IsAllowedAsync("ip:127.0.0.1", maxAttempts: 3, windowSeconds: 60, TestContext.Current.CancellationToken);

        allowed.Should().BeTrue();
    }

    [Fact]
    public async System.Threading.Tasks.Task IsAllowedAsync_Should_RespectRecordedAttemptLimit()
    {
        var clock = new TestClock(new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc));
        var limiter = new MemoryLoginRateLimiter(clock);
        var key = "ip:192.168.1.1";

        await limiter.RecordAsync(key, TestContext.Current.CancellationToken);
        (await limiter.IsAllowedAsync(key, maxAttempts: 2, windowSeconds: 60, TestContext.Current.CancellationToken)).Should().BeTrue();

        await limiter.RecordAsync(key, TestContext.Current.CancellationToken);
        (await limiter.IsAllowedAsync(key, maxAttempts: 2, windowSeconds: 60, TestContext.Current.CancellationToken)).Should().BeFalse();

        await limiter.RecordAsync(key, TestContext.Current.CancellationToken);
        (await limiter.IsAllowedAsync(key, maxAttempts: 2, windowSeconds: 60, TestContext.Current.CancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async System.Threading.Tasks.Task IsAllowedAsync_Should_UseMinimumWindowOfOneSecond()
    {
        var clock = new TestClock(new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc));
        var limiter = new MemoryLoginRateLimiter(clock);
        var key = "ip:127.0.0.100";

        await limiter.RecordAsync(key, TestContext.Current.CancellationToken);
        (await limiter.IsAllowedAsync(key, maxAttempts: 1, windowSeconds: 0, TestContext.Current.CancellationToken)).Should().BeFalse();
        clock.Advance(TimeSpan.FromMilliseconds(1100));
        (await limiter.IsAllowedAsync(key, maxAttempts: 1, windowSeconds: 0, TestContext.Current.CancellationToken)).Should().BeTrue();
    }

    [Fact]
    public async System.Threading.Tasks.Task IsAllowedAsync_Should_ResetWindowAfterConfiguredWindowSeconds()
    {
        var clock = new TestClock(new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc));
        var limiter = new MemoryLoginRateLimiter(clock);
        var key = "ip:10.0.0.1";

        await limiter.RecordAsync(key, TestContext.Current.CancellationToken);
        (await limiter.IsAllowedAsync(key, maxAttempts: 0, windowSeconds: 1, TestContext.Current.CancellationToken)).Should().BeFalse();

        clock.Advance(TimeSpan.FromSeconds(1.2));
        (await limiter.IsAllowedAsync(key, maxAttempts: 0, windowSeconds: 1, TestContext.Current.CancellationToken)).Should().BeTrue();
    }

    [Fact]
    public async System.Threading.Tasks.Task IsAllowedAsync_Should_RespectZeroOrNegativeMaxAttempts()
    {
        var clock = new TestClock(new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc));
        var limiter = new MemoryLoginRateLimiter(clock);
        var key = "ip:127.0.0.2";

        (await limiter.IsAllowedAsync(key, maxAttempts: 0, windowSeconds: 60, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await limiter.IsAllowedAsync(key, maxAttempts: -1, windowSeconds: 60, TestContext.Current.CancellationToken)).Should().BeFalse();
    }
}

internal sealed class TestClock : IClock
{
    public TestClock(DateTime utcNow)
    {
        UtcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }

    public DateTime UtcNow { get; private set; }

    public void Advance(TimeSpan value)
    {
        UtcNow = UtcNow.Add(value);
    }
}

public sealed class DataProtectionSecretProtectorTests
{
    [Fact]
    public void Ctor_Should_Throw_WhenProviderIsNull()
    {
        Action act = () => new DataProtectionSecretProtector(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("provider");
    }

    [Fact]
    public void ProtectAndUnprotect_ShouldRoundTrip()
    {
        var root = Path.Combine(Path.GetTempPath(), "darwin-security-protector-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(root);

        try
        {
            var provider = DataProtectionProvider.Create(root);
            var protector = new DataProtectionSecretProtector(provider);
            const string secret = "totp-secret";

            var protectedData = protector.Protect(secret);
            var unprotected = protector.Unprotect(protectedData);

            protectedData.Should().NotBeNullOrWhiteSpace();
            unprotected.Should().Be(secret);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Protect_Should_Throw_WhenPlainIsNull()
    {
        var root = Path.Combine(Path.GetTempPath(), "darwin-security-protector-null-" + Guid.NewGuid());
        Directory.CreateDirectory(root);
        try
        {
            var provider = DataProtectionProvider.Create(root);
            var protector = new DataProtectionSecretProtector(provider);

            Action act = () => protector.Protect(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("plain");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Unprotect_Should_Throw_WhenCiphertextIsMalformed()
    {
        var root = Path.Combine(Path.GetTempPath(), "darwin-security-protector-malformed-" + Guid.NewGuid());
        Directory.CreateDirectory(root);
        try
        {
            var provider = DataProtectionProvider.Create(root);
            var protector = new DataProtectionSecretProtector(provider);

            Action act = () => protector.Unprotect("not-a-valid-payload");

            act.Should().Throw<Exception>();
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ProtectData_Should_UnprotectAcrossProviderInstancesWithSameKeyDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "darwin-security-protector-restart-" + Guid.NewGuid());
        Directory.CreateDirectory(root);

        try
        {
            var provider1 = DataProtectionProvider.Create(root);
            var provider2 = DataProtectionProvider.Create(root);
            var protector1 = new DataProtectionSecretProtector(provider1);
            var protector2 = new DataProtectionSecretProtector(provider2);
            const string plain = "stable-secret";

            var protectedValue = protector1.Protect(plain);
            var restored = protector2.Unprotect(protectedValue);

            restored.Should().Be(plain);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Protect_Should_WorkForEmptySecret()
    {
        var root = Path.Combine(Path.GetTempPath(), "darwin-security-protector-empty-" + Guid.NewGuid());
        Directory.CreateDirectory(root);

        try
        {
            var provider = DataProtectionProvider.Create(root);
            var protector = new DataProtectionSecretProtector(provider);

            var protectedData = protector.Protect(string.Empty);
            var unprotected = protector.Unprotect(protectedData);

            unprotected.Should().Be(string.Empty);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
