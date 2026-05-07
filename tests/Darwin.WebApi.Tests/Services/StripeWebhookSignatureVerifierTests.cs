using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Darwin.Application.Abstractions.Services;
using Moq;
using Darwin.WebApi.Services;
using FluentAssertions;

namespace Darwin.WebApi.Tests.Services;

public sealed class StripeWebhookSignatureVerifierTests
{
    [Fact]
    public void Ctor_Should_Throw_WhenClockIsNull()
    {
        Action act = () => new StripeWebhookSignatureVerifier(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("clock");
    }

    [Fact]
    public void TryVerify_Should_ReturnFalse_WhenSignatureHeaderIsMissing()
    {
        // Arrange
        var verifier = new StripeWebhookSignatureVerifier(CreateClock());

        // Act
        var isValid = verifier.TryVerify("{}", null, "secret", out var errorKey);

        // Assert
        isValid.Should().BeFalse();
        errorKey.Should().Be("StripeWebhookSignatureHeaderRequired");
    }

    [Fact]
    public void TryVerify_Should_ReturnFalse_WhenSecretIsMissing()
    {
        // Arrange
        var verifier = new StripeWebhookSignatureVerifier(CreateClock());
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = $"t={timestamp},v1=abcdef";

        // Act
        var isValid = verifier.TryVerify("{}", header, "", out var errorKey);

        // Assert
        isValid.Should().BeFalse();
        errorKey.Should().Be("StripeWebhookSignatureInvalid");
    }

    [Fact]
    public void TryVerify_Should_ReturnFalse_WhenHeaderCannotBeParsed()
    {
        // Arrange
        var verifier = new StripeWebhookSignatureVerifier(CreateClock());

        // Act
        var isValid = verifier.TryVerify("{}", "v1=abcdef", "secret", out var errorKey);

        // Assert
        isValid.Should().BeFalse();
        errorKey.Should().Be("StripeWebhookSignatureInvalid");
    }

    [Fact]
    public void TryVerify_Should_ReturnFalse_WhenTimestampIsMissing()
    {
        // Arrange
        var verifier = new StripeWebhookSignatureVerifier(CreateClock());
        const string payload = "{\"event\":\"payment_intent.succeeded\"}";
        const string secret = "whsec_123";

        // Act
        var isValid = verifier.TryVerify(payload, "v1=0011", secret, out var errorKey);

        // Assert
        isValid.Should().BeFalse();
        errorKey.Should().Be("StripeWebhookSignatureInvalid");
    }

    [Fact]
    public void TryVerify_Should_ReturnFalse_WhenTimestampIsInvalidFormat()
    {
        // Arrange
        var verifier = new StripeWebhookSignatureVerifier(CreateClock());
        const string payload = "{\"event\":\"payment_intent.succeeded\"}";
        const string secret = "whsec_123";

        // Act
        var isValid = verifier.TryVerify(payload, "t=abc,v1=0011", secret, out var errorKey);

        // Assert
        isValid.Should().BeFalse();
        errorKey.Should().Be("StripeWebhookSignatureInvalid");
    }

    [Fact]
    public void TryVerify_Should_ReturnFalse_WhenTooManySignatures()
    {
        // Arrange
        var verifier = new StripeWebhookSignatureVerifier(CreateClock());
        var payload = "{\"event\":\"payment_intent.succeeded\"}";
        const string secret = "whsec_123";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var validSignature = ComputeStripeV1Signature(payload, secret, timestamp);
        var header =
            $"t={timestamp},v1={validSignature},v1={validSignature},v1={validSignature},v1={validSignature},v1={validSignature},v1={validSignature}";

        // Act
        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        // Assert
        isValid.Should().BeFalse();
        errorKey.Should().Be("StripeWebhookSignatureInvalid");
    }

    [Fact]
    public void TryVerify_Should_ReturnFalse_WhenSignatureLengthIsInvalid()
    {
        // Arrange
        var verifier = new StripeWebhookSignatureVerifier(CreateClock());
        const string payload = "{\"event\":\"payment_intent.succeeded\"}";
        const string secret = "whsec_123";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = $"t={timestamp},v1=zz";

        // Act
        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        // Assert
        isValid.Should().BeFalse();
        errorKey.Should().Be("StripeWebhookSignatureInvalid");
    }

    /// <summary>
    ///     Ensures signature check fails deterministically when payload is missing.
    /// </summary>
    [Fact]
    public void TryVerify_Should_ReturnFalse_WhenPayloadIsMissing()
    {
        // Arrange
        var verifier = new StripeWebhookSignatureVerifier(CreateClock());
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = $"t={timestamp},v1=abcdef";

        // Act
        var isValid = verifier.TryVerify("  ", header, "secret", out var errorKey);

        // Assert
        isValid.Should().BeFalse();
        errorKey.Should().Be("StripeWebhookSignatureInvalid");
    }

    [Fact]
    public void TryVerify_Should_ReturnFalse_WhenTimestampIsOutsideTolerance()
    {
        // Arrange
        var now = DateTime.UtcNow.AddMinutes(-20);
        var verifier = new StripeWebhookSignatureVerifier(CreateClock(now));
        var payload = "{\"orderId\":\"o-1\"}";
        const string secret = "whsec_123";
        var timestamp = new DateTimeOffset(now.AddMinutes(-20), TimeSpan.Zero).ToUnixTimeSeconds();
        var signature = ComputeStripeV1Signature(payload, secret, timestamp);
        var header = $"t={timestamp},v1={signature}";

        // Act
        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        // Assert
        isValid.Should().BeFalse();
        errorKey.Should().Be("StripeWebhookSignatureInvalid");
    }

    [Fact]
    public void TryVerify_Should_ReturnFalse_WhenTimestampIsTooFarInFuture()
    {
        // Arrange
        var verifier = new StripeWebhookSignatureVerifier(CreateClock(DateTime.UtcNow));
        var payload = "{\"orderId\":\"o-1\"}";
        const string secret = "whsec_123";
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(11).ToUnixTimeSeconds();
        var signature = ComputeStripeV1Signature(payload, secret, timestamp);
        var header = $"t={timestamp},v1={signature}";

        // Act
        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        // Assert
        isValid.Should().BeFalse();
        errorKey.Should().Be("StripeWebhookSignatureInvalid");
    }

    [Fact]
    public void TryVerify_Should_ReturnFalse_WhenTimestampIsNegative()
    {
        var verifier = new StripeWebhookSignatureVerifier(CreateClock(DateTime.UtcNow));
        const string payload = "{\"orderId\":\"o-1\"}";
        const string secret = "whsec_123";
        var timestamp = -12L;
        var signature = ComputeStripeV1Signature(payload, secret, timestamp);
        var header = $"t={timestamp},v1={signature}";

        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        isValid.Should().BeFalse();
        errorKey.Should().Be("StripeWebhookSignatureInvalid");
    }

    [Fact]
    public void TryVerify_Should_ReturnTrue_WhenTimestampHasLeadingPlusSign()
    {
        var now = DateTime.UtcNow;
        var verifier = new StripeWebhookSignatureVerifier(CreateClock(now));
        const string payload = "{\"orderId\":\"o-2\"}";
        const string secret = "whsec_123";
        var timestamp = new DateTimeOffset(now.AddMinutes(1), TimeSpan.Zero).ToUnixTimeSeconds();
        var signature = ComputeStripeV1Signature(payload, secret, timestamp);
        var header = $"t=+{timestamp},v1={signature}";

        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        isValid.Should().BeTrue();
        errorKey.Should().BeEmpty();
    }

    [Fact]
    public void TryVerify_Should_ReturnTrue_WhenSignatureEntriesAppearBeforeTimestamp()
    {
        var now = DateTime.UtcNow;
        var verifier = new StripeWebhookSignatureVerifier(CreateClock(now));
        const string payload = "{\"event\":\"charge.updated\"}";
        const string secret = "whsec_123";
        var timestamp = new DateTimeOffset(now, TimeSpan.Zero).ToUnixTimeSeconds();
        var signature = ComputeStripeV1Signature(payload, secret, timestamp);
        var header = $"v1={signature}, t={timestamp}";

        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        isValid.Should().BeTrue();
        errorKey.Should().BeEmpty();
    }

    [Fact]
    public void TryVerify_Should_ReturnFalse_WhenHeaderLengthExceedsMaximum()
    {
        var verifier = new StripeWebhookSignatureVerifier(CreateClock());
        var payload = "{\"orderId\":\"o-1\"}";
        const string secret = "whsec_123";
        var header = new string('a', 1025);

        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        isValid.Should().BeFalse();
        errorKey.Should().Be("StripeWebhookSignatureInvalid");
    }

    [Fact]
    public void TryVerify_Should_ReturnFalse_WhenTimestampIsZero()
    {
        var now = DateTime.UtcNow;
        var verifier = new StripeWebhookSignatureVerifier(CreateClock(now));
        const string payload = "{\"orderId\":\"o-1\"}";
        const string secret = "whsec_123";
        var signature = ComputeStripeV1Signature(payload, secret, 0);
        var header = $"t=0,v1={signature}";

        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        isValid.Should().BeFalse();
        errorKey.Should().Be("StripeWebhookSignatureInvalid");
    }

    [Fact]
    public void TryVerify_Should_ReturnFalse_WhenHeaderHasTrailingCommaWithoutTimestamp()
    {
        var verifier = new StripeWebhookSignatureVerifier(CreateClock());
        const string payload = "{\"event\":\"payment_intent.succeeded\"}";
        const string secret = "whsec_123";
        var header = "t=,v1=0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123,";

        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        isValid.Should().BeFalse();
        errorKey.Should().Be("StripeWebhookSignatureInvalid");
    }

    [Fact]
    public void TryVerify_Should_ReturnFalse_WhenNoValidV1SignaturesAreParsable()
    {
        var now = DateTime.UtcNow;
        var verifier = new StripeWebhookSignatureVerifier(CreateClock(now));
        const string payload = "{\"orderId\":\"o-1\"}";
        const string secret = "whsec_123";
        var timestamp = new DateTimeOffset(now, TimeSpan.Zero).ToUnixTimeSeconds();
        var header = $"t={timestamp},v1=zzzz,v1=,v1=   ";

        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        isValid.Should().BeFalse();
        errorKey.Should().Be("StripeWebhookSignatureInvalid");
    }

    [Fact]
    public void TryVerify_Should_ReturnTrue_WhenTimestampIsAtBoundary()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var verifier = new StripeWebhookSignatureVerifier(CreateClock(now));
        var payload = "{\"event\":\"payment_intent.succeeded\"}";
        const string secret = "whsec_123";
        var timestamp = new DateTimeOffset(now.AddMinutes(-10), TimeSpan.Zero).ToUnixTimeSeconds();
        var signature = ComputeStripeV1Signature(payload, secret, timestamp);
        var header = $"t={timestamp},v1={signature}";

        // Act
        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        // Assert
        isValid.Should().BeTrue();
        errorKey.Should().BeEmpty();
    }

    /// <summary>
    ///     Ensures hmac compare is not attempted when no v1 signatures are present.
    /// </summary>
    [Fact]
    public void TryVerify_Should_ReturnFalse_WhenHeaderHasNoV1Signature()
    {
        // Arrange
        var verifier = new StripeWebhookSignatureVerifier(CreateClock());
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = $"t={timestamp}";

        // Act
        var isValid = verifier.TryVerify("{}", header, "secret", out var errorKey);

        // Assert
        isValid.Should().BeFalse();
        errorKey.Should().Be("StripeWebhookSignatureInvalid");
    }

    [Fact]
    public void TryVerify_Should_ReturnFalse_WhenNoProvidedSignatureMatches()
    {
        // Arrange
        var verifier = new StripeWebhookSignatureVerifier(CreateClock());
        var payload = "{\"event\":\"payment_intent.succeeded\"}";
        const string secret = "whsec_123";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = $"t={timestamp},v1=0011,v1=not-hex";

        // Act
        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        // Assert
        isValid.Should().BeFalse();
        errorKey.Should().Be("StripeWebhookSignatureInvalid");
    }

    [Fact]
    public void TryVerify_Should_ReturnTrue_WhenSignatureHeaderContainsWhitespaceAroundParts()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var verifier = new StripeWebhookSignatureVerifier(CreateClock(now));
        const string payload = "{\"event\":\"payment_intent.succeeded\"}";
        const string secret = "whsec_123";
        var timestamp = new DateTimeOffset(now, TimeSpan.Zero).ToUnixTimeSeconds();
        var signature = ComputeStripeV1Signature(payload, secret, timestamp);
        var header = $" t={timestamp} , v1={signature} ";

        // Act
        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        // Assert
        isValid.Should().BeTrue();
        errorKey.Should().BeEmpty();
    }

    [Fact]
    public void TryVerify_Should_ReturnTrue_WhenHeaderHasUnknownParameters()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var verifier = new StripeWebhookSignatureVerifier(CreateClock(now));
        const string payload = "{\"event\":\"payment_intent.succeeded\"}";
        const string secret = "whsec_123";
        var timestamp = new DateTimeOffset(now, TimeSpan.Zero).ToUnixTimeSeconds();
        var signature = ComputeStripeV1Signature(payload, secret, timestamp);
        var header = $"t={timestamp},foo=ignored,v1=zz{signature.Substring(2)},v1={signature},bar=baz";

        // Act
        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        // Assert
        isValid.Should().BeTrue();
        errorKey.Should().BeEmpty();
    }

    [Fact]
    public void TryVerify_Should_ReturnTrue_WhenAnyProvidedSignatureMatches()
    {
        // Arrange
        var verifier = new StripeWebhookSignatureVerifier(CreateClock());
        var payload = "{\"event\":\"checkout.session.completed\"}";
        const string secret = "whsec_123";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var validSignature = ComputeStripeV1Signature(payload, secret, timestamp);
        var header = $"t={timestamp},v1=00,v1={validSignature}";

        // Act
        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        // Assert
        isValid.Should().BeTrue();
        errorKey.Should().BeEmpty();
    }

    [Fact]
    public void TryVerify_Should_ReturnTrue_WhenMaximumAllowedSignatureEntriesContainMatch()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var verifier = new StripeWebhookSignatureVerifier(CreateClock(now));
        const string payload = "{\"event\":\"checkout.session.completed\"}";
        const string secret = "whsec_123";
        var timestamp = new DateTimeOffset(now, TimeSpan.Zero).ToUnixTimeSeconds();
        var validSignature = ComputeStripeV1Signature(payload, secret, timestamp);
        var header =
            $"t={timestamp},v1={new string('0', 64)},v1={new string('1', 64)},v1={validSignature},v1={new string('2', 64)},v1={new string('3', 64)}";

        // Act
        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        // Assert
        isValid.Should().BeTrue();
        errorKey.Should().BeEmpty();
    }

    [Fact]
    public void TryVerify_Should_IgnoreEmptySignatureEntries()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var verifier = new StripeWebhookSignatureVerifier(CreateClock(now));
        const string payload = "{\"event\":\"payment_intent.succeeded\"}";
        const string secret = "whsec_123";
        var timestamp = new DateTimeOffset(now, TimeSpan.Zero).ToUnixTimeSeconds();
        var signature = ComputeStripeV1Signature(payload, secret, timestamp);
        var header = $"t={timestamp},v1=,v1={signature},v1=   ";

        // Act
        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        // Assert
        isValid.Should().BeTrue();
        errorKey.Should().BeEmpty();
    }

    [Fact]
    public void TryVerify_Should_Accept_UppercaseSignatureParameterNames()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var verifier = new StripeWebhookSignatureVerifier(CreateClock(now));
        const string payload = "{\"event\":\"payment_intent.succeeded\"}";
        const string secret = "whsec_123";
        var timestamp = new DateTimeOffset(now, TimeSpan.Zero).ToUnixTimeSeconds();
        var signature = ComputeStripeV1Signature(payload, secret, timestamp);
        var header = $"T={timestamp},V1={signature}";

        // Act
        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        // Assert
        isValid.Should().BeTrue();
        errorKey.Should().BeEmpty();
    }

    private static string ComputeStripeV1Signature(string payload, string secret, long timestamp)
    {
        var signedPayload = $"{timestamp.ToString(CultureInfo.InvariantCulture)}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
    }

    private static IClock CreateClock(DateTime? now = null)
    {
        var resolvedNow = now ?? DateTime.UtcNow;
        var clock = new Mock<IClock>();
        clock.Setup(x => x.UtcNow).Returns(resolvedNow);
        return clock.Object;
    }
}
