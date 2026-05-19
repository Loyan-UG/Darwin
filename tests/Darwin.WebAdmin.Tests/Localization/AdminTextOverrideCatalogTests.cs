using Darwin.WebAdmin.Localization;
using FluentAssertions;

namespace Darwin.WebAdmin.Tests.Localization;

public sealed class AdminTextOverrideCatalogTests
{
    [Fact]
    public void Parse_Should_Resolve_CaseVariant_Cultures()
    {
        var overrides = AdminTextOverrideCatalog.Parse("""{"DE-de":{"Key":"Hallo"}, "en-US":{"Key":"Hello"}}""");

        overrides.Count.Should().Be(2);
        overrides.Should().ContainKey("de-DE");
        overrides.Should().ContainKey("en-US");
        overrides["de-DE"]["Key"].Should().Be("Hallo");
    }

    [Fact]
    public void Parse_Should_Keep_LastValueFor_CaseVariant_Duplicate_Culture_Keys()
    {
        var overrides = AdminTextOverrideCatalog.Parse("""{"de-DE":{"key":"Primary"}, "DE-de":{"key":"Fallback"}}""");

        overrides["de-DE"]["key"].Should().Be("Fallback");
    }

    [Fact]
    public void TryResolve_Should_Handle_CaseVariant_KeyAndCulture_WithoutThrowing()
    {
        var overrides = AdminTextOverrideCatalog.Parse("""{"de-DE":{"Logout":"Ausloggen","logout":"Abmelden"}}""");

        overrides.TryGetValue("de-DE", out var _).Should().BeTrue();
        AdminTextOverrideCatalog.TryResolve(overrides, "DE-de", "LOGOUT", out var value).Should().BeTrue();
        value.Should().Be("Abmelden");
    }
}
