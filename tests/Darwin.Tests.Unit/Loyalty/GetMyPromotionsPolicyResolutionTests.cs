using System;
using System.Reflection;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Queries;
using FluentAssertions;

namespace Darwin.Tests.Unit.Loyalty;

/// <summary>
/// Unit tests for policy normalization logic used by <see cref="GetMyPromotionsHandler"/>.
/// These tests intentionally exercise the private static resolver via reflection because
/// the behavior directly affects public API contract values returned to mobile clients.
/// </summary>
public sealed class GetMyPromotionsPolicyResolutionTests
{
    private static readonly MethodInfo ResolvePolicyMethod =
        typeof(GetMyPromotionsHandler).GetMethod("ResolvePolicy", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("GetMyPromotionsHandler.ResolvePolicy was not found.");

    /// <summary>
    /// Ensures that default policy values are stable when the request does not provide overrides.
    /// This protects backward compatibility for mobile consumers that omit the policy object.
    /// </summary>
    [Fact]
    public void ResolvePolicy_Should_ApplyDefaultValues_WhenRequestPolicyIsNull()
    {
        // Arrange
        PromotionFeedPolicyDto? requestPolicy = null;
        const int baseMax = 20;

        // Act
        var resolved = InvokeResolvePolicy(requestPolicy, baseMax);

        // Assert
        resolved.EnableDeduplication.Should().BeTrue();
        resolved.MaxCards.Should().Be(6);
        resolved.FrequencyWindowMinutes.Should().BeNull();
        resolved.SuppressionWindowMinutes.Should().Be(480);
    }

    /// <summary>
    /// Ensures frequency and suppression windows preserve their independent meanings:
    /// - Frequency window should stay explicitly set when provided.
    /// - Suppression window should still round-trip its own configured value.
    /// This avoids ambiguity in API responses consumed by mobile diagnostics.
    /// </summary>
    [Fact]
    public void ResolvePolicy_Should_PreserveDistinctFrequencyAndSuppressionValues_WhenBothProvided()
    {
        // Arrange
        var requestPolicy = new PromotionFeedPolicyDto
        {
            EnableDeduplication = false,
            MaxCards = 50,
            FrequencyWindowMinutes = 30,
            SuppressionWindowMinutes = 240
        };

        const int baseMax = 10;

        // Act
        var resolved = InvokeResolvePolicy(requestPolicy, baseMax);

        // Assert
        resolved.EnableDeduplication.Should().BeFalse();
        resolved.MaxCards.Should().Be(10, "max cards must be capped by baseMax");
        resolved.FrequencyWindowMinutes.Should().Be(30);
        resolved.SuppressionWindowMinutes.Should().Be(240);
    }

    /// <summary>
    /// Ensures invalid or out-of-range numeric values are normalized safely.
    /// This protects API stability and prevents negative windows from leaking into runtime filtering.
    /// </summary>
    [Fact]
    public void ResolvePolicy_Should_ClampInvalidNumericValues()
    {
        // Arrange
        var requestPolicy = new PromotionFeedPolicyDto
        {
            MaxCards = -1,
            FrequencyWindowMinutes = -10,
            SuppressionWindowMinutes = 99999999
        };

        const int baseMax = 5;

        // Act
        var resolved = InvokeResolvePolicy(requestPolicy, baseMax);

        // Assert
        resolved.MaxCards.Should().Be(5, "invalid max should fallback to 6 and then be bounded by baseMax");
        resolved.FrequencyWindowMinutes.Should().Be(0);
        resolved.SuppressionWindowMinutes.Should().Be(60 * 24 * 30);
    }

    private static PromotionFeedPolicyDto InvokeResolvePolicy(PromotionFeedPolicyDto? requestedPolicy, int baseMax)
    {
        var raw = ResolvePolicyMethod.Invoke(null, [requestedPolicy, baseMax]);
        raw.Should().NotBeNull();
        return raw as PromotionFeedPolicyDto
               ?? throw new InvalidOperationException("ResolvePolicy did not return PromotionFeedPolicyDto.");
    }
}
