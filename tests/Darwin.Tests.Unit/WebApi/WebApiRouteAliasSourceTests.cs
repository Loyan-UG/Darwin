using FluentAssertions;

namespace Darwin.Tests.Unit.WebApi;

/// <summary>
/// Guards the audience-specific WebApi route aliases directly in controller source files.
/// This keeps regression checks runnable even when a live WebApi process prevents rebuilding the API project.
/// </summary>
public sealed class WebApiRouteAliasSourceTests
{
    private static readonly Lazy<string> RepositoryRoot = new(FindRepositoryRoot);

    [Fact]
    public void MetaController_Should_ContainCanonicalRoute_AndExpectedMetaEndpoints()
    {
        var source = ReadControllerSource("MetaController.cs");

        source.Should().Contain("api/v1/meta");
        source.Should().Contain("health");
        source.Should().Contain("info");
        source.Should().Contain("bootstrap");
    }

    [Fact]
    public void MetaController_Should_KeepMobileBootstrapContractShape()
    {
        var source = ReadControllerSource("MetaController.cs");

        source.Should().Contain("GetBootstrapAsync");
        source.Should().Contain("AppBootstrapResponse");
        source.Should().Contain("JwtAudience");
        source.Should().Contain("QrTokenRefreshSeconds");
        source.Should().Contain("MaxOutboxItems");
    }

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
        source.Should().Contain("/api/v1/auth/email/request-confirmation");
        source.Should().Contain("/api/v1/auth/email/confirm");
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
    public void PublicCheckoutController_Should_ContainPublicCanonicalRoute_AndLegacyAlias()
    {
        var source = ReadControllerSource(Path.Combine("Public", "PublicCheckoutController.cs"));

        source.Should().Contain("api/v1/public/checkout");
        source.Should().Contain("/api/v1/checkout/intent");
        source.Should().Contain("/api/v1/checkout/orders");
        source.Should().Contain("/api/v1/checkout/orders/{orderId:guid}/payment-intent");
        source.Should().Contain("/api/v1/checkout/orders/{orderId:guid}/payments/{paymentId:guid}/complete");
        source.Should().Contain("/api/v1/checkout/orders/{orderId:guid}/confirmation");
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
    public void MemberBusinessesController_Should_KeepOnboardingContractShape()
    {
        var source = ReadControllerSource(Path.Combine("Member", "MemberBusinessesController.cs"));

        source.Should().Contain("OnboardAsync");
        source.Should().Contain("BusinessOnboardingRequest");
        source.Should().Contain("BusinessOnboardingResponse");
        source.Should().Contain("BusinessMemberRole.Owner");
        source.Should().Contain("/api/v1/businesses/onboarding");
    }

    [Fact]
    public void MemberOrdersController_Should_ContainMemberCanonicalRoutes_AndLegacyAliases()
    {
        var source = ReadControllerSource(Path.Combine("Member", "MemberOrdersController.cs"));

        source.Should().Contain("api/v1/member/orders");
        source.Should().Contain("/api/v1/orders");
        source.Should().Contain("/api/v1/orders/{id:guid}");
        source.Should().Contain("/api/v1/orders/{id:guid}/payment-intent");
        source.Should().Contain("/api/v1/orders/{id:guid}/document");
    }

    [Fact]
    public void MemberInvoicesController_Should_ContainMemberCanonicalRoutes_AndLegacyAliases()
    {
        var source = ReadControllerSource(Path.Combine("Member", "MemberInvoicesController.cs"));

        source.Should().Contain("api/v1/member/invoices");
        source.Should().Contain("/api/v1/invoices");
        source.Should().Contain("/api/v1/invoices/{id:guid}");
        source.Should().Contain("/api/v1/invoices/{id:guid}/payment-intent");
        source.Should().Contain("/api/v1/invoices/{id:guid}/document");
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
        source.Should().Contain("/api/v1/profile/me/preferences");
        source.Should().Contain("/api/v1/profile/me/deletion-request");
    }

    [Fact]
    public void ProfileController_Should_ContainPhoneVerificationRoutes_AndAliases()
    {
        var source = ReadControllerSource(Path.Combine("Profile", "ProfileController.cs"));

        source.Should().Contain("me/phone/request-verification");
        source.Should().Contain("/api/v1/profile/me/phone/request-verification");
        source.Should().Contain("me/phone/confirm");
        source.Should().Contain("/api/v1/profile/me/phone/confirm");
        source.Should().Contain("RequestPhoneVerificationAsync");
        source.Should().Contain("ConfirmPhoneVerificationAsync");
    }

    [Fact]
    public void ProfileAddressesController_Should_ContainMemberCanonicalRoutes_AndLegacyAliases()
    {
        var source = ReadControllerSource(Path.Combine("Profile", "ProfileAddressesController.cs"));

        source.Should().Contain("api/v1/member/profile");
        source.Should().Contain("/api/v1/profile/me/addresses");
        source.Should().Contain("/api/v1/profile/me/addresses/{id:guid}");
        source.Should().Contain("/api/v1/profile/me/customer");
        source.Should().Contain("/api/v1/profile/me/customer/context");
    }

    [Fact]
    public void ProfileAddressesController_Should_KeepCustomerContextContractShape()
    {
        var source = ReadControllerSource(Path.Combine("Profile", "ProfileAddressesController.cs"));

        source.Should().Contain("GetLinkedCustomerAsync");
        source.Should().Contain("GetLinkedCustomerContextAsync");
        source.Should().Contain("LinkedCustomerProfile");
        source.Should().Contain("MemberCustomerContext");
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
    public void BusinessAuthController_Should_ContainBusinessCanonicalRoutes_AndLegacyAliases()
    {
        var source = ReadControllerSource(Path.Combine("Business", "BusinessAuthController.cs"));

        source.Should().Contain("api/v1/business/auth");
        source.Should().Contain("/api/v1/auth/business-invitations/preview");
        source.Should().Contain("/api/v1/auth/business-invitations/accept");
    }

    [Fact]
    public void BusinessAccountController_Should_ContainBusinessCanonicalRoute()
    {
        var source = ReadControllerSource(Path.Combine("Business", "BusinessAccountController.cs"));

        source.Should().Contain("api/v1/business/account");
        source.Should().Contain("access-state");
    }

    [Fact]
    public void NotificationsController_Should_ContainMemberCanonicalRoute_AndLegacyAlias()
    {
        var source = ReadControllerSource(Path.Combine("Notifications", "NotificationsController.cs"));

        source.Should().Contain("api/v1/member/notifications");
        source.Should().Contain("/api/v1/notifications/devices/register");
    }

    [Fact]
    public void NotificationsController_Should_KeepDeviceRegistrationContractShape()
    {
        var source = ReadControllerSource(Path.Combine("Notifications", "NotificationsController.cs"));

        source.Should().Contain("RegisterDeviceAsync");
        source.Should().Contain("RegisterPushDeviceRequest");
        source.Should().Contain("RegisterPushDeviceResponse");
        source.Should().Contain("MobileDevicePlatform.Android");
        source.Should().Contain("MobileDevicePlatform.iOS");
    }

    [Fact]
    public void LoyaltyController_Should_ContainMemberCanonicalRoutes_AndLegacyAliases()
    {
        var source = ReadControllerSource(Path.Combine("Loyalty", "LoyaltyController.cs"));

        source.Should().Contain("api/v1/member/loyalty");
        source.Should().Contain("api/v1/loyalty");
        source.Should().Contain("my/overview");
        source.Should().Contain("scan/prepare");
        source.Should().Contain("my/accounts");
        source.Should().Contain("my/history/{businessId:guid}");
        source.Should().Contain("business/{businessId:guid}/dashboard");
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
            RepositoryRoot.Value,
            "src", "Darwin.WebApi", "Controllers", relativeControllerPath));

        File.Exists(path).Should().BeTrue($"controller source should exist at {path}");
        return File.ReadAllText(path);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Darwin.sln"))
                && Directory.Exists(Path.Combine(directory.FullName, "src", "Darwin.WebApi")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate the Darwin repository root from '{AppContext.BaseDirectory}'.");
    }
}
