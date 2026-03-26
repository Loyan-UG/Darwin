using FluentAssertions;

namespace Darwin.Tests.Unit.WebApi;

/// <summary>
/// Guards the audience-specific WebApi route aliases directly in controller source files.
/// This keeps regression checks runnable even when a live WebApi process prevents rebuilding the API project.
/// </summary>
public sealed class WebApiRouteAliasSourceTests
{
    [Fact]
    public void AuthController_Should_ContainMemberAliases()
    {
        var source = ReadControllerSource("AuthController.cs");

        source.Should().Contain("api/v1/member/auth");
        source.Should().Contain("/api/v1/auth/login");
        source.Should().Contain("/api/v1/auth/refresh");
        source.Should().Contain("/api/v1/auth/logout");
        source.Should().Contain("/api/v1/auth/logout-all");
        source.Should().Contain("/api/v1/auth/register");
        source.Should().Contain("/api/v1/auth/password/change");
        source.Should().Contain("/api/v1/auth/password/request-reset");
        source.Should().Contain("/api/v1/auth/password/reset");
    }

    [Fact]
    public void PublicBusinessesController_Should_ContainPublicCanonicalRoutes_AndLegacyAliases()
    {
        var source = ReadControllerSource(Path.Combine("Public", "PublicBusinessesController.cs"));

        source.Should().Contain("api/v1/public/businesses");
        source.Should().Contain("/api/v1/businesses/list");
        source.Should().Contain("/api/v1/businesses/{id:guid}");
    }

    [Fact]
    public void MemberBusinessesController_Should_ContainMemberCanonicalRoutes_AndLegacyAliases()
    {
        var source = ReadControllerSource(Path.Combine("Member", "MemberBusinessesController.cs"));

        source.Should().Contain("api/v1/member/businesses");
        source.Should().Contain("/api/v1/businesses/onboarding");
        source.Should().Contain("/api/v1/businesses/{id:guid}/with-my-account");
        source.Should().Contain("/api/v1/businesses/{id:guid}/engagement/my");
        source.Should().Contain("/api/v1/businesses/{id:guid}/likes/toggle");
        source.Should().Contain("/api/v1/businesses/{id:guid}/favorites/toggle");
        source.Should().Contain("/api/v1/businesses/{id:guid}/my-review");
    }

    [Fact]
    public void BusinessesMetaController_Should_ContainPublicCanonicalRoute_AndLegacyAlias()
    {
        var source = ReadControllerSource(Path.Combine("Businesses", "BusinessesMetaController.cs"));

        source.Should().Contain("api/v1/public/businesses");
        source.Should().Contain("/api/v1/businesses/category-kinds");
    }

    [Fact]
    public void ProfileController_Should_ContainMemberCanonicalRoutes_AndLegacyAliases()
    {
        var source = ReadControllerSource(Path.Combine("Profile", "ProfileController.cs"));

        source.Should().Contain("api/v1/member/profile");
        source.Should().Contain("/api/v1/profile/me");
        source.Should().Contain("/api/v1/profile/me/deletion-request");
    }

    [Fact]
    public void BillingController_Should_ContainBusinessCanonicalRoutes_AndLegacyAliases()
    {
        var source = ReadControllerSource(Path.Combine("Billing", "BillingController.cs"));

        source.Should().Contain("api/v1/business/billing");
        source.Should().Contain("/api/v1/billing/business/subscription/current");
        source.Should().Contain("/api/v1/billing/business/subscription/cancel-at-period-end");
        source.Should().Contain("/api/v1/billing/plans");
        source.Should().Contain("/api/v1/billing/business/subscription/checkout-intent");
    }

    [Fact]
    public void NotificationsController_Should_ContainMemberCanonicalRoute_AndLegacyAlias()
    {
        var source = ReadControllerSource(Path.Combine("Notifications", "NotificationsController.cs"));

        source.Should().Contain("api/v1/member/notifications");
        source.Should().Contain("/api/v1/notifications/devices/register");
    }

    private static string ReadControllerSource(string relativeControllerPath)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Darwin.WebApi", "Controllers", relativeControllerPath));

        File.Exists(path).Should().BeTrue($"controller source should exist at {path}");
        return File.ReadAllText(path);
    }
}
