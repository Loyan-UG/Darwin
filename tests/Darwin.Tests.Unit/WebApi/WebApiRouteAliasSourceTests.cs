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
        source.Should().Contain("/api/v1/businesses/map");
        source.Should().Contain("/api/v1/businesses/{id:guid}");
    }

    [Fact]
    public void PublicCartController_Should_ContainPublicCanonicalRoutes_AndLegacyAliases()
    {
        var source = ReadControllerSource(Path.Combine("Public", "PublicCartController.cs"));

        source.Should().Contain("api/v1/public/cart");
        source.Should().Contain("/api/v1/cart");
        source.Should().Contain("/api/v1/cart/items");
        source.Should().Contain("/api/v1/cart/coupon");
    }

    [Fact]
    public void PublicShippingController_Should_ContainPublicCanonicalRoute_AndLegacyAlias()
    {
        var source = ReadControllerSource(Path.Combine("Public", "PublicShippingController.cs"));

        source.Should().Contain("api/v1/public/shipping");
        source.Should().Contain("/api/v1/shipping/rates");
    }

    [Fact]
    public void PublicCmsController_Should_ContainPublicCanonicalRoutes_AndLegacyAliases()
    {
        var source = ReadControllerSource(Path.Combine("Public", "PublicCmsController.cs"));

        source.Should().Contain("api/v1/public/cms");
        source.Should().Contain("/api/v1/cms/pages");
        source.Should().Contain("/api/v1/cms/pages/{slug}");
        source.Should().Contain("/api/v1/cms/menus/{name}");
    }

    [Fact]
    public void PublicCatalogController_Should_ContainPublicCanonicalRoutes_AndLegacyAliases()
    {
        var source = ReadControllerSource(Path.Combine("Public", "PublicCatalogController.cs"));

        source.Should().Contain("api/v1/public/catalog");
        source.Should().Contain("/api/v1/catalog/categories");
        source.Should().Contain("/api/v1/catalog/products");
        source.Should().Contain("/api/v1/catalog/products/{slug}");
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
    public void MemberOrdersController_Should_ContainMemberCanonicalRoutes_AndLegacyAliases()
    {
        var source = ReadControllerSource(Path.Combine("Member", "MemberOrdersController.cs"));

        source.Should().Contain("api/v1/member/orders");
        source.Should().Contain("/api/v1/orders");
        source.Should().Contain("/api/v1/orders/{id:guid}");
    }

    [Fact]
    public void MemberInvoicesController_Should_ContainMemberCanonicalRoutes_AndLegacyAliases()
    {
        var source = ReadControllerSource(Path.Combine("Member", "MemberInvoicesController.cs"));

        source.Should().Contain("api/v1/member/invoices");
        source.Should().Contain("/api/v1/invoices");
        source.Should().Contain("/api/v1/invoices/{id:guid}");
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
    public void ProfileAddressesController_Should_ContainMemberCanonicalRoutes_AndLegacyAliases()
    {
        var source = ReadControllerSource(Path.Combine("Profile", "ProfileAddressesController.cs"));

        source.Should().Contain("api/v1/member/profile");
        source.Should().Contain("/api/v1/profile/me/addresses");
        source.Should().Contain("/api/v1/profile/me/addresses/{id:guid}");
        source.Should().Contain("/api/v1/profile/me/customer");
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

    [Fact]
    public void LoyaltyController_Should_ContainMemberCanonicalRoutes_AndLegacyAliases()
    {
        var source = ReadControllerSource(Path.Combine("Loyalty", "LoyaltyController.cs"));

        source.Should().Contain("api/v1/member/loyalty");
        source.Should().Contain("api/v1/loyalty");
        source.Should().Contain("scan/prepare");
        source.Should().Contain("my/accounts");
        source.Should().Contain("my/history/{businessId:guid}");
        source.Should().Contain("account/{businessId:guid}/join");
        source.Should().Contain("account/{businessId:guid}/next-reward");
    }

    [Fact]
    public void BusinessLoyaltyController_Should_ContainBusinessCanonicalRoutes_AndLegacyAliases()
    {
        var source = ReadControllerSource(Path.Combine("Business", "BusinessLoyaltyController.cs"));

        source.Should().Contain("api/v1/business/loyalty");
        source.Should().Contain("/api/v1/loyalty/business/reward-config");
        source.Should().Contain("/api/v1/loyalty/business/reward-config/tiers");
        source.Should().Contain("/api/v1/loyalty/scan/process");
        source.Should().Contain("/api/v1/loyalty/scan/confirm-accrual");
        source.Should().Contain("/api/v1/loyalty/scan/confirm-redemption");
        source.Should().Contain("/api/v1/loyalty/business/campaigns");
        source.Should().Contain("/api/v1/loyalty/business/campaigns/{id:guid}/activation");
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
