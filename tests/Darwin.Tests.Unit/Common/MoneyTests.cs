using System;
using Darwin.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Darwin.Tests.Unit.Common;

/// <summary>
/// Unit tests for the <see cref="Money"/> value object.
/// Covers construction, minor-unit mapping, arithmetic operations,
/// allocation helpers, equality, comparison, and display formatting.
/// </summary>
public sealed class MoneyTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Construction
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_Should_NormalizeCurrencyToUpperCase()
    {
        var m = new Money(100, "eur");
        m.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Constructor_Should_NormalizeCurrencyWithWhitespace()
    {
        var m = new Money(100, " usd ");
        m.Currency.Should().Be("USD");
    }

    [Fact]
    public void Constructor_Should_SetAmountMinor()
    {
        var m = new Money(1099, "EUR");
        m.AmountMinor.Should().Be(1099);
    }

    [Fact]
    public void Constructor_Should_AllowNegativeAmounts()
    {
        var m = new Money(-500, "EUR");
        m.AmountMinor.Should().Be(-500);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Should_ThrowForNullOrWhitespaceCurrency(string currency)
    {
        Action act = () => _ = new Money(0, currency);
        act.Should().Throw<ArgumentNullException>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FromMajor / ToMajor
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void FromMajor_Should_ConvertEurCorrectly()
    {
        var m = Money.FromMajor(10.99m, "EUR");
        m.AmountMinor.Should().Be(1099);
        m.Currency.Should().Be("EUR");
    }

    [Fact]
    public void FromMajor_Should_ConvertJpyWithZeroDecimalPlaces()
    {
        var m = Money.FromMajor(1000m, "JPY");
        m.AmountMinor.Should().Be(1000);
    }

    [Fact]
    public void FromMajor_Should_ConvertBhdWithThreeDecimalPlaces()
    {
        var m = Money.FromMajor(1.505m, "BHD");
        // 1.505 * 1000 = 1505, rounded AwayFromZero
        m.AmountMinor.Should().Be(1505);
    }

    [Fact]
    public void FromMajor_Should_RoundMidpointAwayFromZero()
    {
        // 0.005 EUR * 100 = 0.5 minor -> rounds to 1
        var m = Money.FromMajor(0.005m, "EUR");
        m.AmountMinor.Should().Be(1);
    }

    [Fact]
    public void ToMajor_Should_ReturnMajorUnitForEur()
    {
        var m = new Money(1099, "EUR");
        m.ToMajor().Should().Be(10.99m);
    }

    [Fact]
    public void ToMajor_Should_ReturnMajorUnitForJpy()
    {
        var m = new Money(1000, "JPY");
        m.ToMajor().Should().Be(1000m);
    }

    [Fact]
    public void FromMajorAndToMajor_Should_RoundTrip()
    {
        var original = 49.95m;
        var m = Money.FromMajor(original, "EUR");
        m.ToMajor().Should().Be(original);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Arithmetic
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Add_Should_SumAmountsOfSameCurrency()
    {
        var a = new Money(500, "EUR");
        var b = new Money(300, "EUR");
        var result = a.Add(b);
        result.AmountMinor.Should().Be(800);
        result.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Add_Should_ThrowOnCurrencyMismatch()
    {
        var a = new Money(500, "EUR");
        var b = new Money(300, "USD");
        Action act = () => a.Add(b);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Currency mismatch*");
    }

    [Fact]
    public void Subtract_Should_ReturnDifference()
    {
        var a = new Money(1000, "EUR");
        var b = new Money(400, "EUR");
        var result = a.Subtract(b);
        result.AmountMinor.Should().Be(600);
    }

    [Fact]
    public void Subtract_Should_AllowNegativeResult()
    {
        var a = new Money(100, "EUR");
        var b = new Money(300, "EUR");
        var result = a.Subtract(b);
        result.AmountMinor.Should().Be(-200);
    }

    [Fact]
    public void Subtract_Should_ThrowOnCurrencyMismatch()
    {
        var a = new Money(500, "EUR");
        var b = new Money(300, "GBP");
        Action act = () => a.Subtract(b);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MultiplyByQuantity_Should_MultiplyAmount()
    {
        var price = new Money(999, "EUR");
        var total = price.MultiplyByQuantity(3);
        total.AmountMinor.Should().Be(2997);
        total.Currency.Should().Be("EUR");
    }

    [Fact]
    public void MultiplyByQuantity_Should_AllowZeroQuantity()
    {
        var price = new Money(999, "EUR");
        var result = price.MultiplyByQuantity(0);
        result.AmountMinor.Should().Be(0);
    }

    [Fact]
    public void MultiplyByQuantity_Should_ThrowForNegativeQuantity()
    {
        var price = new Money(999, "EUR");
        Action act = () => price.MultiplyByQuantity(-1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AllocateEven
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AllocateEven_Should_PreserveTotalSum()
    {
        var total = new Money(1000, "EUR");
        var parts = total.AllocateEven(3);
        var sum = 0L;
        foreach (var p in parts) sum += p.AmountMinor;
        sum.Should().Be(1000);
    }

    [Fact]
    public void AllocateEven_Should_ReturnCorrectNumberOfParts()
    {
        var total = new Money(100, "EUR");
        var parts = total.AllocateEven(4);
        parts.Should().HaveCount(4);
    }

    [Fact]
    public void AllocateEven_Should_DistributeRemainderOneByOne()
    {
        // 10 / 3 = 3 r 1 => [4, 3, 3]
        var total = new Money(10, "EUR");
        var parts = total.AllocateEven(3);
        parts[0].AmountMinor.Should().Be(4);
        parts[1].AmountMinor.Should().Be(3);
        parts[2].AmountMinor.Should().Be(3);
    }

    [Fact]
    public void AllocateEven_Should_WorkForNegativeAmount()
    {
        // -10 / 3 = -3 r -1 => [-4, -3, -3]
        var total = new Money(-10, "EUR");
        var parts = total.AllocateEven(3);
        var sum = 0L;
        foreach (var p in parts) sum += p.AmountMinor;
        sum.Should().Be(-10);
    }

    [Fact]
    public void AllocateEven_Should_ThrowForZeroParts()
    {
        var total = new Money(100, "EUR");
        Action act = () => total.AllocateEven(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AllocateEven_Should_ThrowForNegativeParts()
    {
        var total = new Money(100, "EUR");
        Action act = () => total.AllocateEven(-1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AllocateByWeights
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AllocateByWeights_Should_PreserveTotalSum()
    {
        var total = new Money(1000, "EUR");
        var parts = total.AllocateByWeights(1, 2, 3);
        var sum = 0L;
        foreach (var p in parts) sum += p.AmountMinor;
        sum.Should().Be(1000);
    }

    [Fact]
    public void AllocateByWeights_Should_DistributeProportionally()
    {
        // 600 split by [1, 2] => [200, 400]
        var total = new Money(600, "EUR");
        var parts = total.AllocateByWeights(1, 2);
        parts[0].AmountMinor.Should().Be(200);
        parts[1].AmountMinor.Should().Be(400);
    }

    [Fact]
    public void AllocateByWeights_Should_FallBackToEvenWhenAllWeightsZero()
    {
        // All zero weights => even split
        var total = new Money(10, "EUR");
        var parts = total.AllocateByWeights(0, 0);
        var sum = 0L;
        foreach (var p in parts) sum += p.AmountMinor;
        sum.Should().Be(10);
        parts.Should().HaveCount(2);
    }

    [Fact]
    public void AllocateByWeights_Should_ThrowForNullWeights()
    {
        var total = new Money(100, "EUR");
        Action act = () => total.AllocateByWeights(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AllocateByWeights_Should_ThrowForEmptyWeights()
    {
        var total = new Money(100, "EUR");
        Action act = () => total.AllocateByWeights(Array.Empty<int>());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AllocateByWeights_Should_ThrowForNegativeWeight()
    {
        var total = new Money(100, "EUR");
        Action act = () => total.AllocateByWeights(1, -1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Equality and comparison
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Equals_Should_BeTrueForSameAmountAndCurrency()
    {
        var a = new Money(100, "EUR");
        var b = new Money(100, "EUR");
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_Should_BeTrueForCaseInsensitiveCurrency()
    {
        var a = new Money(100, "EUR");
        var b = new Money(100, "eur");
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_Should_BeFalseForDifferentAmount()
    {
        var a = new Money(100, "EUR");
        var b = new Money(200, "EUR");
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_Should_BeFalseForDifferentCurrency()
    {
        var a = new Money(100, "EUR");
        var b = new Money(100, "USD");
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_Should_Work()
    {
        var a = new Money(100, "EUR");
        var b = new Money(100, "EUR");
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_Should_BeEqualForEqualMoney()
    {
        var a = new Money(100, "EUR");
        var b = new Money(100, "eur");
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void CompareTo_Should_OrderByCurrencyThenAmount()
    {
        var eur100 = new Money(100, "EUR");
        var eur200 = new Money(200, "EUR");
        var usd100 = new Money(100, "USD");

        eur100.CompareTo(eur200).Should().BeNegative("EUR 100 < EUR 200");
        eur200.CompareTo(eur100).Should().BePositive("EUR 200 > EUR 100");
        eur100.CompareTo(eur100).Should().Be(0, "same amount, same currency");
        eur100.CompareTo(usd100).Should().BeNegative("EUR sorts before USD alphabetically");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetMinorDigits
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("EUR", 2)]
    [InlineData("USD", 2)]
    [InlineData("GBP", 2)]
    [InlineData("JPY", 0)]
    [InlineData("KRW", 0)]
    [InlineData("BHD", 3)]
    [InlineData("KWD", 3)]
    public void GetMinorDigits_Should_ReturnCorrectDigitsForKnownCurrencies(string currency, int expected)
    {
        Money.GetMinorDigits(currency).Should().Be(expected);
    }

    [Fact]
    public void GetMinorDigits_Should_DefaultToTwoForUnknownCurrency()
    {
        Money.GetMinorDigits("XXX").Should().Be(2, "unknown currencies default to 2 minor digits");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetMinorDigits_Should_ThrowForNullOrWhitespace(string currency)
    {
        Action act = () => Money.GetMinorDigits(currency);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetMinorDigits_Should_BeCaseInsensitive()
    {
        Money.GetMinorDigits("eur").Should().Be(2);
        Money.GetMinorDigits("EUR").Should().Be(2);
        Money.GetMinorDigits("Eur").Should().Be(2);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ToString
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_Should_FormatEurCorrectly()
    {
        var m = new Money(1099, "EUR");
        m.ToString().Should().Be("EUR 10.99");
    }

    [Fact]
    public void ToString_Should_FormatJpyWithNoDecimalPlaces()
    {
        var m = new Money(1000, "JPY");
        m.ToString().Should().Be("JPY 1000");
    }

    [Fact]
    public void ToString_Should_FormatBhdWithThreeDecimalPlaces()
    {
        var m = new Money(1505, "BHD");
        m.ToString().Should().Be("BHD 1.505");
    }

    [Fact]
    public void ToString_Should_FormatNegativeAmounts()
    {
        var m = new Money(-500, "EUR");
        m.ToString().Should().Be("EUR -5.00");
    }
}
