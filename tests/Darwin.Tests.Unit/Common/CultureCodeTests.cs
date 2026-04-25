using System;
using System.Globalization;
using Darwin.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Darwin.Tests.Unit.Common;

/// <summary>
/// Unit tests for the <see cref="CultureCode"/> value object.
/// Covers construction, normalization, TryParse, equality, comparison, and implicit string conversion.
/// </summary>
public sealed class CultureCodeTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Construction and normalization
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_Should_NormalizeCaseToCanonicalBcp47()
    {
        var code = new CultureCode("de-de");
        code.Value.Should().Be("de-DE");
    }

    [Fact]
    public void Constructor_Should_AcceptValidCulture()
    {
        var code = new CultureCode("en-US");
        code.Value.Should().Be("en-US");
    }

    [Fact]
    public void Constructor_Should_TrimWhitespace()
    {
        var code = new CultureCode("  en-US  ");
        code.Value.Should().Be("en-US");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Should_ThrowForNullOrWhitespace(string value)
    {
        Action act = () => _ = new CultureCode(value);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_Should_ThrowForUnknownCulture()
    {
        // Single-letter codes like "a" are rejected by CultureInfo.GetCultureInfo on all platforms.
        Action act = () => _ = new CultureCode("a");
        act.Should().Throw<CultureNotFoundException>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Parse
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Should_ReturnCultureCodeForValidInput()
    {
        var code = CultureCode.Parse("de-DE");
        code.Value.Should().Be("de-DE");
    }

    [Fact]
    public void Parse_Should_NormalizeCase()
    {
        var code = CultureCode.Parse("DE-de");
        code.Value.Should().Be("de-DE");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TryParse
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TryParse_Should_ReturnTrueForValidCulture()
    {
        var success = CultureCode.TryParse("en-US", out var result);
        success.Should().BeTrue();
        result.Value.Should().Be("en-US");
    }

    [Fact]
    public void TryParse_Should_ReturnFalseForNullInput()
    {
        var success = CultureCode.TryParse(null, out _);
        success.Should().BeFalse();
    }

    [Fact]
    public void TryParse_Should_ReturnFalseForWhitespaceInput()
    {
        var success = CultureCode.TryParse("   ", out _);
        success.Should().BeFalse();
    }

    [Fact]
    public void TryParse_Should_ReturnFalseForInvalidCulture()
    {
        // Single-letter codes like "a" are rejected by CultureInfo.GetCultureInfo on all platforms.
        var success = CultureCode.TryParse("a", out _);
        success.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Equality
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Equals_Should_BeTrueForSameCanonicalValue()
    {
        var a = new CultureCode("de-DE");
        var b = new CultureCode("de-DE");
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_Should_BeTrueForDifferentInputCaseResolvingToSameCanonical()
    {
        var a = new CultureCode("de-de");
        var b = new CultureCode("DE-DE");
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_Should_BeFalseForDifferentCultures()
    {
        var a = new CultureCode("de-DE");
        var b = new CultureCode("en-US");
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_Should_Work()
    {
        var a = new CultureCode("de-DE");
        var b = new CultureCode("de-DE");
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void EqualsIgnoreCase_Should_BeTrueForSameCulture()
    {
        var a = new CultureCode("de-DE");
        var b = new CultureCode("de-DE");
        a.EqualsIgnoreCase(b).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_Should_BeEqualForEqualCultureCodes()
    {
        var a = new CultureCode("de-DE");
        var b = new CultureCode("de-de");
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Comparison
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CompareTo_Should_ReturnZeroForEqualValues()
    {
        var a = new CultureCode("de-DE");
        var b = new CultureCode("de-DE");
        a.CompareTo(b).Should().Be(0);
    }

    [Fact]
    public void CompareTo_Should_ReturnNonZeroForDifferentValues()
    {
        var a = new CultureCode("de-DE");
        var b = new CultureCode("en-US");
        a.CompareTo(b).Should().NotBe(0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ToString and implicit string conversion
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_Should_ReturnCanonicalValue()
    {
        var code = new CultureCode("de-DE");
        code.ToString().Should().Be("de-DE");
    }

    [Fact]
    public void ImplicitStringConversion_Should_ReturnValue()
    {
        var code = new CultureCode("en-US");
        string value = code;
        value.Should().Be("en-US");
    }
}
