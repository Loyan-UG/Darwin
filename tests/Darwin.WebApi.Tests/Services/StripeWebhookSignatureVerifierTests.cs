using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Darwin.WebApi.Services;
using FluentAssertions;

namespace Darwin.WebApi.Tests.Services;

public sealed class StripeWebhookSignatureVerifierTests
{
    [Fact]
    public void TryVerify_Should_ReturnFalse_WhenSignatureHeaderIsMissing()
    {
        // Arrange
        var verifier = new StripeWebhookSignatureVerifier();

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
        var verifier = new StripeWebhookSignatureVerifier();
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
        var verifier = new StripeWebhookSignatureVerifier();

        // Act
        var isValid = verifier.TryVerify("{}", "v1=abcdef", "secret", out var errorKey);

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
        var verifier = new StripeWebhookSignatureVerifier();
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
        var verifier = new StripeWebhookSignatureVerifier();
        var payload = "{\"orderId\":\"o-1\"}";
        const string secret = "whsec_123";
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(-20).ToUnixTimeSeconds();
        var signature = ComputeStripeV1Signature(payload, secret, timestamp);
        var header = $"t={timestamp},v1={signature}";

        // Act
        var isValid = verifier.TryVerify(payload, header, secret, out var errorKey);

        // Assert
        isValid.Should().BeFalse();
        errorKey.Should().Be("StripeWebhookSignatureInvalid");
    }

    /// <summary>
    ///     Ensures hmac compare is not attempted when no v1 signatures are present.
    /// </summary>
    [Fact]
    public void TryVerify_Should_ReturnFalse_WhenHeaderHasNoV1Signature()
    {
        // Arrange
        var verifier = new StripeWebhookSignatureVerifier();
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
        var verifier = new StripeWebhookSignatureVerifier();
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
    public void TryVerify_Should_ReturnTrue_WhenAnyProvidedSignatureMatches()
    {
        // Arrange
        var verifier = new StripeWebhookSignatureVerifier();
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

    private static string ComputeStripeV1Signature(string payload, string secret, long timestamp)
    {
        var signedPayload = $"{timestamp.ToString(CultureInfo.InvariantCulture)}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
    }
}
