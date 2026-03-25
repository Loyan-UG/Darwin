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

        source.Should().Contain("/api/v1/member/auth/login");
        source.Should().Contain("/api/v1/member/auth/refresh");
        source.Should().Contain("/api/v1/member/auth/logout");
        source.Should().Contain("/api/v1/member/auth/logout-all");
        source.Should().Contain("/api/v1/member/auth/register");
        source.Should().Contain("/api/v1/member/auth/password/change");
        source.Should().Contain("/api/v1/member/auth/password/request-reset");
        source.Should().Contain("/api/v1/member/auth/password/reset");
    }

    [Fact]
    public void BusinessesController_Should_ContainPublicAndMemberAliases()
    {
        var source = ReadControllerSource(Path.Combine("Businesses", "BusinessesController.cs"));

        source.Should().Contain("/api/v1/public/businesses/list");
        source.Should().Contain("/api/v1/public/businesses/{id:guid}");
        source.Should().Contain("/api/v1/member/businesses/onboarding");
        source.Should().Contain("/api/v1/member/businesses/{id:guid}/with-my-account");
        source.Should().Contain("/api/v1/member/businesses/{id:guid}/engagement/my");
        source.Should().Contain("/api/v1/member/businesses/{id:guid}/likes/toggle");
        source.Should().Contain("/api/v1/member/businesses/{id:guid}/favorites/toggle");
        source.Should().Contain("/api/v1/member/businesses/{id:guid}/my-review");
    }

    [Fact]
    public void ProfileController_Should_ContainMemberAliases()
    {
        var source = ReadControllerSource(Path.Combine("Profile", "ProfileController.cs"));

        source.Should().Contain("/api/v1/member/profile/me");
        source.Should().Contain("/api/v1/member/profile/me/deletion-request");
    }

    [Fact]
    public void BillingController_Should_ContainBusinessAliases()
    {
        var source = ReadControllerSource(Path.Combine("Billing", "BillingController.cs"));

        source.Should().Contain("/api/v1/business/billing/subscription/current");
        source.Should().Contain("/api/v1/business/billing/subscription/cancel-at-period-end");
        source.Should().Contain("/api/v1/business/billing/plans");
        source.Should().Contain("/api/v1/business/billing/subscription/checkout-intent");
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
