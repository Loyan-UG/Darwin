using Darwin.Application.AdminTextOverrides;
using FluentAssertions;

namespace Darwin.Tests.Unit.Common;

/// <summary>
/// Unit tests for <see cref="AdminTextOverrideJsonCatalog"/>.
/// Covers IsValid, Parse, and TryParse for all branches.
/// </summary>
public sealed class AdminTextOverrideJsonCatalogTests
{
    // ─── IsValid ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsValid_Should_Return_True_For_Null_Input()
    {
        AdminTextOverrideJsonCatalog.IsValid(null).Should().BeTrue(
            "null is treated as an empty override (no overrides configured)");
    }

    [Fact]
    public void IsValid_Should_Return_True_For_Empty_String()
    {
        AdminTextOverrideJsonCatalog.IsValid("").Should().BeTrue(
            "empty string is treated as no overrides");
    }

    [Fact]
    public void IsValid_Should_Return_True_For_Whitespace_String()
    {
        AdminTextOverrideJsonCatalog.IsValid("   ").Should().BeTrue(
            "whitespace-only string is treated as no overrides");
    }

    [Fact]
    public void IsValid_Should_Return_True_For_Empty_Object()
    {
        AdminTextOverrideJsonCatalog.IsValid("{}").Should().BeTrue(
            "an empty JSON object is structurally valid");
    }

    [Fact]
    public void IsValid_Should_Return_True_For_Valid_Culture_Map()
    {
        const string json = """{"de-DE":{"WelcomeTitle":"Willkommen"},"en-US":{"WelcomeTitle":"Welcome"}}""";
        AdminTextOverrideJsonCatalog.IsValid(json).Should().BeTrue();
    }

    [Fact]
    public void IsValid_Should_Return_False_For_Invalid_Json()
    {
        AdminTextOverrideJsonCatalog.IsValid("{not-valid-json").Should().BeFalse();
    }

    [Fact]
    public void IsValid_Should_Return_False_For_Json_Array_Root()
    {
        AdminTextOverrideJsonCatalog.IsValid("[1,2,3]").Should().BeFalse(
            "root must be an object, not an array");
    }

    [Fact]
    public void IsValid_Should_Return_False_For_Json_String_Root()
    {
        AdminTextOverrideJsonCatalog.IsValid("\"hello\"").Should().BeFalse(
            "root must be an object, not a string scalar");
    }

    [Fact]
    public void IsValid_Should_Return_False_When_Culture_Value_Is_Not_Object()
    {
        // e.g. { "de-DE": "invalid" }
        AdminTextOverrideJsonCatalog.IsValid("""{"de-DE":"not-an-object"}""").Should().BeFalse();
    }

    [Fact]
    public void IsValid_Should_Return_False_When_Culture_Key_Is_Whitespace()
    {
        // e.g. { " ": { "key": "value" } }
        AdminTextOverrideJsonCatalog.IsValid("""{"  ":{"key":"value"}}""").Should().BeFalse(
            "a whitespace-only culture key is invalid");
    }

    [Fact]
    public void IsValid_Should_Return_False_When_Text_Key_Is_Whitespace()
    {
        // e.g. { "de-DE": { " ": "value" } }
        AdminTextOverrideJsonCatalog.IsValid("""{"de-DE":{"  ":"value"}}""").Should().BeFalse(
            "a whitespace-only text key is invalid");
    }

    [Fact]
    public void IsValid_Should_Return_False_When_Text_Value_Is_Not_String_Or_Null()
    {
        // e.g. { "de-DE": { "key": 123 } }
        AdminTextOverrideJsonCatalog.IsValid("""{"de-DE":{"key":123}}""").Should().BeFalse(
            "integer text values are not permitted");
    }

    [Fact]
    public void IsValid_Should_Return_True_When_Text_Value_Is_Null()
    {
        // null values are explicitly ignored (skipped), not rejected
        AdminTextOverrideJsonCatalog.IsValid("""{"de-DE":{"key":null}}""").Should().BeTrue(
            "null text values are silently ignored");
    }

    // ─── Parse ────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Should_Return_Empty_Catalog_For_Null()
    {
        var catalog = AdminTextOverrideJsonCatalog.Parse(null);
        catalog.Should().BeEmpty("null input yields no overrides");
    }

    [Fact]
    public void Parse_Should_Return_Empty_Catalog_For_Invalid_Json()
    {
        var catalog = AdminTextOverrideJsonCatalog.Parse("{bad json");
        catalog.Should().BeEmpty("invalid JSON must fall back to empty catalog");
    }

    [Fact]
    public void Parse_Should_Return_Catalog_With_Correct_Entries()
    {
        const string json = """{"de-DE":{"WelcomeTitle":"Willkommen"},"en-US":{"WelcomeTitle":"Welcome"}}""";

        var catalog = AdminTextOverrideJsonCatalog.Parse(json);

        catalog.Should().ContainKey("de-DE");
        catalog["de-DE"].Should().ContainKey("WelcomeTitle");
        catalog["de-DE"]["WelcomeTitle"].Should().Be("Willkommen");

        catalog.Should().ContainKey("en-US");
        catalog["en-US"]["WelcomeTitle"].Should().Be("Welcome");
    }

    [Fact]
    public void Parse_Should_Be_Case_Insensitive_On_Culture_Key()
    {
        const string json = """{"DE-de":{"Title":"Hallo"}}""";
        var catalog = AdminTextOverrideJsonCatalog.Parse(json);

        // The dictionary is OrdinalIgnoreCase so "de-DE" should match "DE-de"
        catalog.Should().ContainKey("DE-de");
    }

    [Fact]
    public void Parse_Should_Trim_Text_Values()
    {
        const string json = """{"en-US":{"Key":"  trimmed  "}}""";
        var catalog = AdminTextOverrideJsonCatalog.Parse(json);

        catalog["en-US"]["Key"].Should().Be("trimmed", "values must be stored trimmed");
    }

    [Fact]
    public void Parse_Should_Skip_Null_Text_Values()
    {
        const string json = """{"en-US":{"Present":"hello","Absent":null}}""";
        var catalog = AdminTextOverrideJsonCatalog.Parse(json);

        catalog["en-US"].Should().ContainKey("Present");
        catalog["en-US"].Should().NotContainKey("Absent", "null values are skipped");
    }

    [Fact]
    public void Parse_Should_Skip_Whitespace_Only_Text_Values()
    {
        const string json = """{"en-US":{"SpacesOnly":"   "}}""";
        var catalog = AdminTextOverrideJsonCatalog.Parse(json);

        catalog["en-US"].Should().NotContainKey("SpacesOnly",
            "whitespace-only values are treated as absent");
    }

    // ─── TryParse ────────────────────────────────────────────────────────────

    [Fact]
    public void TryParse_Should_Return_True_And_Empty_Catalog_For_Empty_Input()
    {
        var result = AdminTextOverrideJsonCatalog.TryParse("", out var catalog);

        result.Should().BeTrue();
        catalog.Should().BeEmpty();
    }

    [Fact]
    public void TryParse_Should_Return_False_For_Invalid_Json()
    {
        var result = AdminTextOverrideJsonCatalog.TryParse("{oops", out var catalog);

        result.Should().BeFalse();
        catalog.Should().BeEmpty();
    }

    [Fact]
    public void TryParse_Should_Return_True_With_Populated_Catalog()
    {
        const string json = """{"fr-FR":{"Greeting":"Bonjour"}}""";

        var result = AdminTextOverrideJsonCatalog.TryParse(json, out var catalog);

        result.Should().BeTrue();
        catalog.Should().ContainKey("fr-FR");
        catalog["fr-FR"]["Greeting"].Should().Be("Bonjour");
    }

    [Fact]
    public void TryParse_Should_Return_False_When_Root_Is_Array()
    {
        var result = AdminTextOverrideJsonCatalog.TryParse("[]", out var catalog);

        result.Should().BeFalse();
        catalog.Should().BeEmpty();
    }

    [Fact]
    public void TryParse_Should_Return_False_When_Culture_Value_Is_Array()
    {
        var result = AdminTextOverrideJsonCatalog.TryParse("""{"de-DE":[]}""", out _);
        result.Should().BeFalse();
    }

    [Fact]
    public void TryParse_Should_Return_False_When_Text_Value_Is_Number()
    {
        var result = AdminTextOverrideJsonCatalog.TryParse("""{"de-DE":{"key":42}}""", out _);
        result.Should().BeFalse();
    }
}
