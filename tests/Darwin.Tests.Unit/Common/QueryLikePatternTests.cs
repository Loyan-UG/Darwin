using Darwin.Application.Common;
using FluentAssertions;

namespace Darwin.Tests.Unit.Common;

/// <summary>
/// Tests for <see cref="QueryLikePattern"/> covering the <c>Contains</c> builder
/// and its internal SQL-LIKE escape logic.
/// </summary>
public sealed class QueryLikePatternTests
{
    // ─── Contains: output shape ───────────────────────────────────────────────

    [Fact]
    public void Contains_Should_Wrap_Value_In_Percent_Wildcards()
    {
        var result = QueryLikePattern.Contains("hello");
        result.Should().Be("%hello%");
    }

    [Fact]
    public void Contains_Should_Trim_Leading_And_Trailing_Whitespace()
    {
        var result = QueryLikePattern.Contains("  hello  ");
        result.Should().Be("%hello%");
    }

    [Fact]
    public void Contains_Should_Return_DoublePercent_ForEmptyInput()
    {
        var result = QueryLikePattern.Contains("");
        result.Should().Be("%%", "an empty value produces the wildcard-only pattern that matches everything");
    }

    [Fact]
    public void Contains_Should_Return_DoublePercent_ForWhitespaceOnlyInput()
    {
        var result = QueryLikePattern.Contains("   ");
        result.Should().Be("%%", "whitespace collapses to empty after trimming");
    }

    // ─── Contains: SQL LIKE escape characters ─────────────────────────────────

    [Fact]
    public void Contains_Should_Escape_Percent_Signs_In_Input()
    {
        var result = QueryLikePattern.Contains("100%");
        result.Should().Be("%100\\%%", "a literal % in the search term must be escaped so it is not treated as a wildcard");
    }

    [Fact]
    public void Contains_Should_Escape_Underscore_In_Input()
    {
        var result = QueryLikePattern.Contains("a_b");
        result.Should().Be("%a\\_b%", "a literal _ in the search term must be escaped so it is not treated as a single-char wildcard");
    }

    [Fact]
    public void Contains_Should_Escape_OpenSquareBracket_In_Input()
    {
        var result = QueryLikePattern.Contains("[admin]");
        result.Should().Be("%\\[admin]%", "a literal [ must be escaped; ] does not need escaping");
    }

    [Fact]
    public void Contains_Should_Escape_Backslash_In_Input()
    {
        var result = QueryLikePattern.Contains("C:\\path");
        result.Should().Be("%C:\\\\path%", "a literal backslash must be doubled so it is not misinterpreted as the escape prefix");
    }

    [Fact]
    public void Contains_Should_Escape_Multiple_Special_Characters_Together()
    {
        var result = QueryLikePattern.Contains("100%_off[deal]");
        result.Should().Be("%100\\%\\_off\\[deal]%");
    }

    // ─── Contains: length truncation ──────────────────────────────────────────

    [Fact]
    public void Contains_Should_Truncate_SearchTerm_At_256_Characters()
    {
        var longInput = new string('a', 300);
        var result = QueryLikePattern.Contains(longInput);
        // The result must be %{256-a's}%
        result.Should().StartWith("%").And.EndWith("%");
        var inner = result[1..^1]; // strip surrounding %
        inner.Should().HaveLength(256, "the search term is capped at 256 characters before wrapping");
    }

    [Fact]
    public void Contains_Should_Accept_Exactly_256_Characters_Without_Truncation()
    {
        var exactly256 = new string('b', 256);
        var result = QueryLikePattern.Contains(exactly256);
        var inner = result[1..^1];
        inner.Should().HaveLength(256);
    }

    // ─── EscapeCharacter constant ─────────────────────────────────────────────

    [Fact]
    public void EscapeCharacter_Should_Be_Backslash()
    {
        QueryLikePattern.EscapeCharacter.Should().Be("\\",
            "the escape character used in EF.Functions.Like calls must match the one used by Contains");
    }
}
