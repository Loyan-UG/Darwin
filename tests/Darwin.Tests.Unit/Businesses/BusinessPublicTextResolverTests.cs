using System.Collections.Generic;
using Darwin.Application.Businesses;
using FluentAssertions;

namespace Darwin.Tests.Unit.Businesses;

/// <summary>
/// Unit tests for <see cref="BusinessPublicTextResolver"/>.
/// Covers ResolveName and ResolveShortDescription for all culture resolution
/// branches: exact match, default-culture fallback, de-DE fallback, en-US fallback,
/// missing overrides, whitespace override, and null inputs.
/// </summary>
public sealed class BusinessPublicTextResolverTests
{
    // ─── ResolveName ──────────────────────────────────────────────────────────

    [Fact]
    public void ResolveName_Should_ReturnOriginalName_WhenOverridesJsonIsNull()
    {
        var result = BusinessPublicTextResolver.ResolveName(
            name: "Original Business",
            adminTextOverridesJson: null,
            culture: "de-DE",
            defaultCulture: "de-DE");

        result.Should().Be("Original Business");
    }

    [Fact]
    public void ResolveName_Should_ReturnOriginalName_WhenOverridesJsonIsEmpty()
    {
        var result = BusinessPublicTextResolver.ResolveName(
            name: "Original Business",
            adminTextOverridesJson: "{}",
            culture: "de-DE",
            defaultCulture: "de-DE");

        result.Should().Be("Original Business");
    }

    [Fact]
    public void ResolveName_Should_ReturnOverride_WhenRequestedCultureMatches()
    {
        const string overrides = """{"de-DE":{"PublicBusinessName":"Café Aurora"}}""";

        var result = BusinessPublicTextResolver.ResolveName(
            name: "Original Name",
            adminTextOverridesJson: overrides,
            culture: "de-DE",
            defaultCulture: "en-US");

        result.Should().Be("Café Aurora");
    }

    [Fact]
    public void ResolveName_Should_FallbackToDefaultCulture_WhenRequestedCultureHasNoOverride()
    {
        const string overrides = """{"en-US":{"PublicBusinessName":"Aurora Cafe"}}""";

        var result = BusinessPublicTextResolver.ResolveName(
            name: "Original Name",
            adminTextOverridesJson: overrides,
            culture: "fr-FR",
            defaultCulture: "en-US");

        result.Should().Be("Aurora Cafe",
            "the default culture en-US has an override so it should be used when fr-FR does not");
    }

    [Fact]
    public void ResolveName_Should_FallbackToDeDe_WhenBothCultureAndDefaultCultureAreNull()
    {
        const string overrides = """{"de-DE":{"PublicBusinessName":"Fallback DE"}}""";

        var result = BusinessPublicTextResolver.ResolveName(
            name: "Original Name",
            adminTextOverridesJson: overrides,
            culture: null,
            defaultCulture: null);

        result.Should().Be("Fallback DE");
    }

    [Fact]
    public void ResolveName_Should_FallbackToEnUs_WhenDeDeFallbackAlsoMissing()
    {
        const string overrides = """{"en-US":{"PublicBusinessName":"Fallback EN"}}""";

        var result = BusinessPublicTextResolver.ResolveName(
            name: "Original Name",
            adminTextOverridesJson: overrides,
            culture: null,
            defaultCulture: null);

        result.Should().Be("Fallback EN",
            "en-US is the final hardcoded fallback after de-DE");
    }

    [Fact]
    public void ResolveName_Should_ReturnOriginalName_WhenNoFallbackCultureHasOverride()
    {
        const string overrides = """{"fr-FR":{"PublicBusinessName":"Fallback FR"}}""";

        var result = BusinessPublicTextResolver.ResolveName(
            name: "Original Name",
            adminTextOverridesJson: overrides,
            culture: null,
            defaultCulture: null);

        result.Should().Be("Original Name",
            "fr-FR is not in the fallback chain so none of the cascade steps match");
    }

    [Fact]
    public void ResolveName_Should_ReturnOriginalName_WhenOverrideValueIsWhitespaceOnly()
    {
        const string overrides = """{"de-DE":{"PublicBusinessName":"   "}}""";

        var result = BusinessPublicTextResolver.ResolveName(
            name: "Original Name",
            adminTextOverridesJson: overrides,
            culture: "de-DE",
            defaultCulture: "de-DE");

        result.Should().Be("Original Name",
            "whitespace-only override values are treated as absent");
    }

    [Fact]
    public void ResolveName_Should_TrimOverrideValue_WhenOverrideHasLeadingOrTrailingWhitespace()
    {
        const string overrides = """{"de-DE":{"PublicBusinessName":"  Trimmed Name  "}}""";

        var result = BusinessPublicTextResolver.ResolveName(
            name: "Original Name",
            adminTextOverridesJson: overrides,
            culture: "de-DE",
            defaultCulture: "de-DE");

        result.Should().Be("Trimmed Name");
    }

    [Fact]
    public void ResolveName_Should_UseCulturePriorityOrder_WhenMultipleCulturesPresent()
    {
        // All cultures have an override — the requested culture (it-IT matches nothing in cascade,
        // so defaultCulture fr-FR also not present; ultimately falls to de-DE)
        const string overrides = """{"de-DE":{"PublicBusinessName":"Deutsch"},"en-US":{"PublicBusinessName":"English"}}""";

        var result = BusinessPublicTextResolver.ResolveName(
            name: "Original",
            adminTextOverridesJson: overrides,
            culture: "it-IT",       // no match
            defaultCulture: "fr-FR"); // no match → falls to de-DE

        result.Should().Be("Deutsch");
    }

    // ─── ResolveShortDescription ──────────────────────────────────────────────

    [Fact]
    public void ResolveShortDescription_Should_ReturnNull_WhenOverridesJsonIsNull()
    {
        var result = BusinessPublicTextResolver.ResolveShortDescription(
            shortDescription: null,
            adminTextOverridesJson: null,
            culture: "de-DE",
            defaultCulture: "de-DE");

        result.Should().BeNull();
    }

    [Fact]
    public void ResolveShortDescription_Should_ReturnOriginalDescription_WhenNoMatchInOverrides()
    {
        const string overrides = """{"fr-FR":{"PublicBusinessShortDescription":"French desc"}}""";

        var result = BusinessPublicTextResolver.ResolveShortDescription(
            shortDescription: "Original description",
            adminTextOverridesJson: overrides,
            culture: null,
            defaultCulture: null);

        result.Should().Be("Original description");
    }

    [Fact]
    public void ResolveShortDescription_Should_ReturnOverride_WhenCultureMatches()
    {
        const string overrides = """{"en-US":{"PublicBusinessShortDescription":"Our best café downtown."}}""";

        var result = BusinessPublicTextResolver.ResolveShortDescription(
            shortDescription: "Original desc",
            adminTextOverridesJson: overrides,
            culture: "en-US",
            defaultCulture: "de-DE");

        result.Should().Be("Our best café downtown.");
    }

    [Fact]
    public void ResolveShortDescription_Should_ReturnNull_WhenOriginalIsNullAndNoOverride()
    {
        const string overrides = """{}""";

        var result = BusinessPublicTextResolver.ResolveShortDescription(
            shortDescription: null,
            adminTextOverridesJson: overrides,
            culture: "de-DE",
            defaultCulture: "de-DE");

        result.Should().BeNull("no override exists and original is null");
    }
}
