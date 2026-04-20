using FluentAssertions;

namespace Darwin.Tests.Unit.Security;

public sealed class SecurityAndPerformanceSourceTests
{
    [Fact]
    public void WebApiStartup_Should_KeepRateLimiterAheadOfAuthentication_AndAuthorization()
    {
        var source = ReadWebApiFile(Path.Combine("Extensions", "Startup.cs"));

        source.Should().Contain("app.UseRateLimiter();");
        source.Should().Contain("app.UseAuthentication();");
        source.Should().Contain("app.UseAuthorization();");
        source.IndexOf("app.UseRateLimiter();", StringComparison.Ordinal)
            .Should().BeLessThan(source.IndexOf("app.UseAuthentication();", StringComparison.Ordinal));
        source.IndexOf("app.UseAuthentication();", StringComparison.Ordinal)
            .Should().BeLessThan(source.IndexOf("app.UseAuthorization();", StringComparison.Ordinal));
    }

    [Fact]
    public void WebApiDependencyInjection_Should_KeepAuthRateLimitPoliciesConfigured()
    {
        var source = ReadWebApiFile(Path.Combine("Extensions", "DependencyInjection.cs"));

        source.Should().Contain("services.AddRateLimiter");
        source.Should().Contain("options.AddPolicy(\"auth-login\"");
        source.Should().Contain("PermitLimit = 5");
        source.Should().Contain("Window = TimeSpan.FromSeconds(30)");
        source.Should().Contain("options.AddPolicy(\"auth-refresh\"");
        source.Should().Contain("PermitLimit = 20");
        source.Should().Contain("Window = TimeSpan.FromSeconds(60)");
        source.Should().Contain("StatusCodes.Status429TooManyRequests");
    }

    [Fact]
    public void AuthController_Should_KeepRateLimitedAnonymousLoginAndRefreshEndpoints()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "AuthController.cs"));

        source.Should().Contain("[AllowAnonymous]");
        source.Should().Contain("[EnableRateLimiting(\"auth-login\")]");
        source.Should().Contain("[EnableRateLimiting(\"auth-refresh\")]");
        source.Should().Contain("public async Task<IActionResult> LoginAsync(");
        source.Should().Contain("public async Task<IActionResult> RefreshAsync(");
    }

    [Fact]
    public void AuthController_Should_KeepAuthenticationAndLogoutAliasesStable()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "AuthController.cs"));

        source.Should().Contain("[Route(\"api/v1/member/auth\")]");
        source.Should().Contain("[HttpPost(\"login\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/auth/login\")]");
        source.Should().Contain("[HttpPost(\"refresh\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/auth/refresh\")]");
        source.Should().Contain("public async Task<IActionResult> LogoutAsync(");
        source.Should().Contain("[Authorize]");
        source.Should().Contain("[HttpPost(\"logout\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/auth/logout\")]");
        source.Should().Contain("public async Task<IActionResult> LogoutAllAsync(");
        source.Should().Contain("[HttpPost(\"logout-all\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/auth/logout-all\")]");
    }

    [Fact]
    public void PublicAndBootstrapControllers_Should_RemainAnonymous()
    {
        ReadWebApiFile(Path.Combine("Controllers", "MetaController.cs"))
            .Should().Contain("[AllowAnonymous]");
        ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicBusinessesController.cs"))
            .Should().Contain("[AllowAnonymous]");
        ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicCartController.cs"))
            .Should().Contain("[AllowAnonymous]");
        ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicCheckoutController.cs"))
            .Should().Contain("[AllowAnonymous]");
        ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicShippingController.cs"))
            .Should().Contain("[AllowAnonymous]");
        ReadWebApiFile(Path.Combine("Controllers", "Business", "BusinessAuthController.cs"))
            .Should().Contain("[AllowAnonymous]");
    }

    [Fact]
    public void BusinessAuthController_Should_KeepAnonymousInvitationPreviewAndAcceptance()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Business", "BusinessAuthController.cs"));

        source.Should().Contain("[AllowAnonymous]");
        source.Should().Contain("public async Task<IActionResult> PreviewInvitationAsync(");
        source.Should().Contain("public async Task<IActionResult> AcceptInvitationAsync(");
        source.Should().Contain("/api/v1/auth/business-invitations/preview");
        source.Should().Contain("/api/v1/auth/business-invitations/accept");
    }

    [Fact]
    public void NotificationsAndProfileControllers_Should_RemainAuthenticated()
    {
        ReadWebApiFile(Path.Combine("Controllers", "Notifications", "NotificationsController.cs"))
            .Should().Contain("[Authorize]");
        ReadWebApiFile(Path.Combine("Controllers", "Profile", "ProfileController.cs"))
            .Should().Contain("[Authorize]");
    }

    [Fact]
    public void NotificationsController_Should_KeepAuthenticatedDeviceRegistrationAliases()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Notifications", "NotificationsController.cs"));

        source.Should().Contain("[Authorize]");
        source.Should().Contain("[Route(\"api/v1/member/notifications\")]");
        source.Should().Contain("public async Task<IActionResult> RegisterDeviceAsync(");
        source.Should().Contain("[HttpPost(\"devices/register\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/notifications/devices/register\")]");
        source.Should().Contain("MobileDevicePlatform.Android");
        source.Should().Contain("MobileDevicePlatform.iOS");
    }

    [Fact]
    public void ProfileController_Should_KeepAuthenticatedLifecycleAndPreferencesAliases()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Profile", "ProfileController.cs"));

        source.Should().Contain("[Authorize]");
        source.Should().Contain("[Route(\"api/v1/member/profile\")]");
        source.Should().Contain("public async Task<IActionResult> UpdatePreferencesAsync(");
        source.Should().Contain("[HttpPut(\"preferences\")]");
        source.Should().Contain("[HttpPut(\"/api/v1/profile/me/preferences\")]");
        source.Should().Contain("public async Task<IActionResult> RequestPhoneVerificationAsync(");
        source.Should().Contain("[HttpPost(\"me/phone/request-verification\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/profile/me/phone/request-verification\")]");
        source.Should().Contain("public async Task<IActionResult> ConfirmPhoneVerificationAsync(");
        source.Should().Contain("[HttpPost(\"me/phone/confirm\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/profile/me/phone/confirm\")]");
        source.Should().Contain("public async Task<IActionResult> RequestAccountDeletionAsync(");
        source.Should().Contain("[HttpPost(\"me/deletion-request\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/profile/me/deletion-request\")]");
    }

    [Fact]
    public void MemberAndLoyaltyControllers_Should_RemainAuthenticated()
    {
        ReadWebApiFile(Path.Combine("Controllers", "Member", "MemberBusinessesController.cs"))
            .Should().Contain("[Authorize]");
        ReadWebApiFile(Path.Combine("Controllers", "Loyalty", "LoyaltyController.cs"))
            .Should().Contain("[Authorize]");
        ReadWebApiFile(Path.Combine("Controllers", "Business", "BusinessLoyaltyController.cs"))
            .Should().Contain("[Authorize]");
        ReadWebApiFile(Path.Combine("Controllers", "Business", "BusinessAccountController.cs"))
            .Should().Contain("[Authorize]");
    }

    [Fact]
    public void MemberBusinessesController_Should_KeepAuthenticatedOnboardingAndEngagementAliases()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Member", "MemberBusinessesController.cs"));

        source.Should().Contain("[Authorize]");
        source.Should().Contain("public async Task<IActionResult> OnboardAsync(");
        source.Should().Contain("[HttpPost(\"onboarding\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/businesses/onboarding\")]");
        source.Should().Contain("public async Task<IActionResult> GetWithMyAccountAsync(");
        source.Should().Contain("[Authorize(Policy = \"perm:AccessMemberArea\")]");
        source.Should().Contain("[HttpGet(\"{id:guid}/with-my-account\")]");
        source.Should().Contain("[HttpGet(\"/api/v1/businesses/{id:guid}/with-my-account\")]");
        source.Should().Contain("public async Task<IActionResult> GetMyEngagementAsync(");
        source.Should().Contain("[HttpGet(\"{id:guid}/engagement/my\")]");
        source.Should().Contain("[HttpGet(\"/api/v1/businesses/{id:guid}/engagement/my\")]");
    }

    [Fact]
    public void LoyaltyController_Should_KeepAuthenticatedMemberRoutesAndPolicies()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Loyalty", "LoyaltyController.cs"));

        source.Should().Contain("[Authorize]");
        source.Should().Contain("[Route(\"api/v1/member/loyalty\")]");
        source.Should().Contain("[Route(\"api/v1/loyalty\")]");
        source.Should().Contain("[Authorize(Policy = \"perm:AccessMemberArea\")]");
        source.Should().Contain("public async Task<IActionResult> PrepareScanSessionAsync(");
        source.Should().Contain("[HttpPost(\"scan/prepare\")]");
        source.Should().Contain("public async Task<IActionResult> GetMyAccountsAsync(");
        source.Should().Contain("[HttpGet(\"my/accounts\")]");
        source.Should().Contain("public async Task<IActionResult> GetMyOverviewAsync(");
        source.Should().Contain("[HttpGet(\"my/overview\")]");
        source.Should().Contain("public async Task<IActionResult> JoinLoyaltyAsync(");
    }

    [Fact]
    public void BusinessLoyaltyController_Should_KeepAuthenticatedBusinessRoutesAndPolicies()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Business", "BusinessLoyaltyController.cs"));

        source.Should().Contain("[Authorize]");
        source.Should().Contain("[Route(\"api/v1/business/loyalty\")]");
        source.Should().Contain("[Authorize(Policy = \"perm:AccessLoyaltyBusiness\")]");
        source.Should().Contain("public async Task<IActionResult> GetBusinessRewardConfigurationAsync(");
        source.Should().Contain("[HttpGet(\"reward-config\")]");
        source.Should().Contain("[HttpGet(\"/api/v1/loyalty/business/reward-config\")]");
        source.Should().Contain("public async Task<IActionResult> CreateBusinessRewardTierAsync(");
        source.Should().Contain("[HttpPost(\"reward-config/tiers\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/loyalty/business/reward-config/tiers\")]");
        source.Should().Contain("public async Task<IActionResult> ProcessScanSessionForBusinessAsync(");
    }

    [Fact]
    public void PublicCheckoutController_Should_KeepAnonymousStorefrontPaymentEndpoints()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicCheckoutController.cs"));

        source.Should().Contain("[AllowAnonymous]");
        source.Should().Contain("[Route(\"api/v1/public/checkout\")]");
        source.Should().Contain("public async Task<IActionResult> CreatePaymentIntentAsync(");
        source.Should().Contain("[HttpPost(\"orders/{orderId:guid}/payment-intent\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/checkout/orders/{orderId:guid}/payment-intent\")]");
        source.Should().Contain("public async Task<IActionResult> CompletePaymentAsync(");
        source.Should().Contain("[HttpPost(\"orders/{orderId:guid}/payments/{paymentId:guid}/complete\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/checkout/orders/{orderId:guid}/payments/{paymentId:guid}/complete\")]");
        source.Should().Contain("public async Task<IActionResult> GetConfirmationAsync(");
    }

    [Fact]
    public void PublicCheckoutController_Should_KeepAnonymousIntentAndOrderPlacementEndpoints()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicCheckoutController.cs"));

        source.Should().Contain("[AllowAnonymous]");
        source.Should().Contain("[Route(\"api/v1/public/checkout\")]");
        source.Should().Contain("public async Task<IActionResult> CreateIntentAsync(");
        source.Should().Contain("[HttpPost(\"intent\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/checkout/intent\")]");
        source.Should().Contain("CreateStorefrontCheckoutIntentHandler");
        source.Should().Contain("public async Task<IActionResult> PlaceOrderAsync(");
        source.Should().Contain("[HttpPost(\"orders\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/checkout/orders\")]");
        source.Should().Contain("PlaceOrderFromCartHandler");
    }

    [Fact]
    public void MemberOrderAndInvoiceControllers_Should_KeepAuthenticatedPaymentIntentAliases()
    {
        var ordersSource = ReadWebApiFile(Path.Combine("Controllers", "Member", "MemberOrdersController.cs"));
        var invoicesSource = ReadWebApiFile(Path.Combine("Controllers", "Member", "MemberInvoicesController.cs"));

        ordersSource.Should().Contain("[Authorize]");
        ordersSource.Should().Contain("[Route(\"api/v1/member/orders\")]");
        ordersSource.Should().Contain("public async Task<IActionResult> CreatePaymentIntentAsync(");
        ordersSource.Should().Contain("[HttpPost(\"{id:guid}/payment-intent\")]");
        ordersSource.Should().Contain("[HttpPost(\"/api/v1/orders/{id:guid}/payment-intent\")]");

        invoicesSource.Should().Contain("[Authorize]");
        invoicesSource.Should().Contain("[Route(\"api/v1/member/invoices\")]");
        invoicesSource.Should().Contain("public async Task<IActionResult> CreatePaymentIntentAsync(");
        invoicesSource.Should().Contain("[HttpPost(\"{id:guid}/payment-intent\")]");
        invoicesSource.Should().Contain("[HttpPost(\"/api/v1/invoices/{id:guid}/payment-intent\")]");
    }

    [Fact]
    public void MemberOrderAndInvoiceControllers_Should_KeepAuthenticatedReadAndDocumentAliases()
    {
        var ordersSource = ReadWebApiFile(Path.Combine("Controllers", "Member", "MemberOrdersController.cs"));
        var invoicesSource = ReadWebApiFile(Path.Combine("Controllers", "Member", "MemberInvoicesController.cs"));

        ordersSource.Should().Contain("[Authorize]");
        ordersSource.Should().Contain("[Route(\"api/v1/member/orders\")]");
        ordersSource.Should().Contain("public async Task<IActionResult> GetMyOrdersAsync(");
        ordersSource.Should().Contain("[HttpGet]");
        ordersSource.Should().Contain("[HttpGet(\"/api/v1/orders\")]");
        ordersSource.Should().Contain("public async Task<IActionResult> GetMyOrderAsync(");
        ordersSource.Should().Contain("[HttpGet(\"{id:guid}\")]");
        ordersSource.Should().Contain("[HttpGet(\"/api/v1/orders/{id:guid}\")]");
        ordersSource.Should().Contain("public async Task<IActionResult> DownloadDocumentAsync(");
        ordersSource.Should().Contain("[HttpGet(\"{id:guid}/document\")]");
        ordersSource.Should().Contain("[HttpGet(\"/api/v1/orders/{id:guid}/document\")]");

        invoicesSource.Should().Contain("[Authorize]");
        invoicesSource.Should().Contain("[Route(\"api/v1/member/invoices\")]");
        invoicesSource.Should().Contain("public async Task<IActionResult> GetMyInvoicesAsync(");
        invoicesSource.Should().Contain("[HttpGet]");
        invoicesSource.Should().Contain("[HttpGet(\"/api/v1/invoices\")]");
        invoicesSource.Should().Contain("public async Task<IActionResult> GetMyInvoiceAsync(");
        invoicesSource.Should().Contain("[HttpGet(\"{id:guid}\")]");
        invoicesSource.Should().Contain("[HttpGet(\"/api/v1/invoices/{id:guid}\")]");
        invoicesSource.Should().Contain("public async Task<IActionResult> DownloadDocumentAsync(");
        invoicesSource.Should().Contain("[HttpGet(\"{id:guid}/document\")]");
        invoicesSource.Should().Contain("[HttpGet(\"/api/v1/invoices/{id:guid}/document\")]");
    }

    [Fact]
    public void BusinessBillingController_Should_KeepAuthenticatedPolicyGatedBillingEndpoints()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Billing", "BillingController.cs"));

        source.Should().Contain("[Authorize]");
        source.Should().Contain("[Route(\"api/v1/business/billing\")]");
        source.Should().Contain("[Authorize(Policy = \"perm:AccessLoyaltyBusiness\")]");
        source.Should().Contain("public async Task<IActionResult> GetCurrentBusinessSubscriptionAsync(");
        source.Should().Contain("[HttpGet(\"subscription/current\")]");
        source.Should().Contain("[HttpGet(\"/api/v1/billing/business/subscription/current\")]");
        source.Should().Contain("public async Task<IActionResult> SetCancelAtPeriodEndAsync(");
        source.Should().Contain("[HttpPost(\"subscription/cancel-at-period-end\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/billing/business/subscription/cancel-at-period-end\")]");
        source.Should().Contain("public async Task<IActionResult> CreateSubscriptionCheckoutIntentAsync(");
        source.Should().Contain("[HttpPost(\"subscription/checkout-intent\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/billing/business/subscription/checkout-intent\")]");
    }

    [Fact]
    public void PublicCatalogAndCmsControllers_Should_KeepPublicStorefrontReadRoutes()
    {
        var catalogSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicCatalogController.cs"));
        var cmsSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicCmsController.cs"));

        catalogSource.Should().NotContain("[Authorize]");
        catalogSource.Should().Contain("[Route(\"api/v1/public/catalog\")]");
        catalogSource.Should().Contain("public async Task<IActionResult> GetCategoriesAsync(");
        catalogSource.Should().Contain("[HttpGet(\"categories\")]");
        catalogSource.Should().Contain("[HttpGet(\"/api/v1/catalog/categories\")]");
        catalogSource.Should().Contain("public async Task<IActionResult> GetProductsAsync(");
        catalogSource.Should().Contain("[HttpGet(\"products\")]");
        catalogSource.Should().Contain("[HttpGet(\"/api/v1/catalog/products\")]");

        cmsSource.Should().NotContain("[Authorize]");
        cmsSource.Should().Contain("[Route(\"api/v1/public/cms\")]");
        cmsSource.Should().Contain("public async Task<IActionResult> GetPagesAsync(");
        cmsSource.Should().Contain("[HttpGet(\"pages\")]");
        cmsSource.Should().Contain("[HttpGet(\"/api/v1/cms/pages\")]");
        cmsSource.Should().Contain("public async Task<IActionResult> GetPageBySlugAsync(");
        cmsSource.Should().Contain("[HttpGet(\"pages/{slug}\")]");
        cmsSource.Should().Contain("[HttpGet(\"/api/v1/cms/pages/{slug}\")]");
    }

    [Fact]
    public void BusinessesMetaAndProfileAddressesControllers_Should_KeepBoundaryContracts()
    {
        var businessesMetaSource = ReadWebApiFile(Path.Combine("Controllers", "Businesses", "BusinessesMetaController.cs"));
        var profileAddressesSource = ReadWebApiFile(Path.Combine("Controllers", "Profile", "ProfileAddressesController.cs"));

        businessesMetaSource.Should().Contain("[AllowAnonymous]");
        businessesMetaSource.Should().Contain("[Route(\"api/v1/public/businesses\")]");
        businessesMetaSource.Should().Contain("public async Task<IActionResult> GetCategoryKinds(");
        businessesMetaSource.Should().Contain("[HttpGet(\"category-kinds\")]");
        businessesMetaSource.Should().Contain("[HttpGet(\"/api/v1/businesses/category-kinds\")]");

        profileAddressesSource.Should().Contain("[Authorize]");
        profileAddressesSource.Should().Contain("[Route(\"api/v1/member/profile\")]");
        profileAddressesSource.Should().Contain("public async Task<IActionResult> GetAddressesAsync(");
        profileAddressesSource.Should().Contain("[HttpGet(\"addresses\")]");
        profileAddressesSource.Should().Contain("[HttpGet(\"/api/v1/profile/me/addresses\")]");
        profileAddressesSource.Should().Contain("public async Task<IActionResult> GetLinkedCustomerAsync(");
        profileAddressesSource.Should().Contain("[HttpGet(\"customer\")]");
        profileAddressesSource.Should().Contain("[HttpGet(\"/api/v1/profile/me/customer\")]");
        profileAddressesSource.Should().Contain("public async Task<IActionResult> GetLinkedCustomerContextAsync(");
        profileAddressesSource.Should().Contain("[HttpGet(\"customer/context\")]");
        profileAddressesSource.Should().Contain("[HttpGet(\"/api/v1/profile/me/customer/context\")]");
    }

    [Fact]
    public void PublicCartAndShippingControllers_Should_KeepAnonymousStorefrontBoundaries()
    {
        var cartSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicCartController.cs"));
        var shippingSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicShippingController.cs"));

        cartSource.Should().Contain("[AllowAnonymous]");
        cartSource.Should().Contain("[Route(\"api/v1/public/cart\")]");
        cartSource.Should().Contain("public async Task<IActionResult> GetAsync(");
        cartSource.Should().Contain("[HttpGet(\"/api/v1/cart\")]");
        cartSource.Should().Contain("public async Task<IActionResult> AddItemAsync(");
        cartSource.Should().Contain("[HttpPost(\"items\")]");
        cartSource.Should().Contain("[HttpPost(\"/api/v1/cart/items\")]");
        cartSource.Should().Contain("public async Task<IActionResult> ApplyCouponAsync(");
        cartSource.Should().Contain("[HttpPost(\"coupon\")]");
        cartSource.Should().Contain("[HttpPost(\"/api/v1/cart/coupon\")]");

        shippingSource.Should().Contain("[AllowAnonymous]");
        shippingSource.Should().Contain("[Route(\"api/v1/public/shipping\")]");
        shippingSource.Should().Contain("public async Task<IActionResult> GetRatesAsync(");
        shippingSource.Should().Contain("[HttpPost(\"rates\")]");
        shippingSource.Should().Contain("[HttpPost(\"/api/v1/shipping/rates\")]");
    }

    [Fact]
    public void PublicCartController_Should_KeepAnonymousMutationAliases()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicCartController.cs"));

        source.Should().Contain("[AllowAnonymous]");
        source.Should().Contain("[Route(\"api/v1/public/cart\")]");
        source.Should().Contain("public async Task<IActionResult> AddItemAsync(");
        source.Should().Contain("[HttpPost(\"items\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/cart/items\")]");
        source.Should().Contain("public async Task<IActionResult> UpdateItemAsync(");
        source.Should().Contain("[HttpPut(\"items\")]");
        source.Should().Contain("[HttpPut(\"/api/v1/cart/items\")]");
        source.Should().Contain("public async Task<IActionResult> RemoveItemAsync(");
        source.Should().Contain("[HttpDelete(\"items\")]");
        source.Should().Contain("[HttpDelete(\"/api/v1/cart/items\")]");
        source.Should().Contain("public async Task<IActionResult> ApplyCouponAsync(");
        source.Should().Contain("[HttpPost(\"coupon\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/cart/coupon\")]");
    }

    [Fact]
    public void BusinessAccountController_Should_RemainAuthenticatedAndBusinessScoped()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Business", "BusinessAccountController.cs"));

        source.Should().Contain("[Authorize]");
        source.Should().Contain("[Route(\"api/v1/business/account\")]");
        source.Should().Contain("public async Task<IActionResult> GetAccessStateAsync(");
        source.Should().Contain("[HttpGet(\"access-state\")]");
        source.Should().Contain("BusinessControllerConventions.TryGetCurrentBusinessId(User, out var businessId)");
    }

    [Fact]
    public void PublicBusinessesController_Should_KeepAnonymousDiscoveryAndDetailAliases()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicBusinessesController.cs"));

        source.Should().Contain("[AllowAnonymous]");
        source.Should().Contain("[Route(\"api/v1/public/businesses\")]");
        source.Should().Contain("public async Task<IActionResult> ListAsync(");
        source.Should().Contain("[HttpPost(\"list\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/businesses/list\")]");
        source.Should().Contain("public async Task<IActionResult> MapAsync(");
        source.Should().Contain("[HttpPost(\"map\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/businesses/map\")]");
        source.Should().Contain("public async Task<IActionResult> GetAsync(");
        source.Should().Contain("[HttpGet(\"{id:guid}\")]");
        source.Should().Contain("[HttpGet(\"/api/v1/businesses/{id:guid}\")]");
    }

    [Fact]
    public void BusinessAuthController_Should_KeepAnonymousInvitationOnboardingEndpoints()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Business", "BusinessAuthController.cs"));

        source.Should().Contain("[AllowAnonymous]");
        source.Should().Contain("[Route(\"api/v1/business/auth\")]");
        source.Should().Contain("public async Task<IActionResult> PreviewInvitationAsync(");
        source.Should().Contain("[HttpGet(\"invitations/preview\")]");
        source.Should().Contain("[HttpGet(\"/api/v1/auth/business-invitations/preview\")]");
        source.Should().Contain("public async Task<IActionResult> AcceptInvitationAsync(");
        source.Should().Contain("[HttpPost(\"invitations/accept\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/auth/business-invitations/accept\")]");
    }

    [Fact]
    public void AuthController_Should_KeepAnonymousRecoveryAndConfirmationEndpoints()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "AuthController.cs"));

        source.Should().Contain("[Route(\"api/v1/member/auth\")]");
        source.Should().Contain("[AllowAnonymous]");
        source.Should().Contain("public async Task<IActionResult> RequestEmailConfirmationAsync(");
        source.Should().Contain("[HttpPost(\"email/request-confirmation\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/auth/email/request-confirmation\")]");
        source.Should().Contain("public async Task<IActionResult> ConfirmEmailAsync(");
        source.Should().Contain("[HttpPost(\"email/confirm\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/auth/email/confirm\")]");
        source.Should().Contain("public async Task<IActionResult> RequestPasswordResetAsync(");
        source.Should().Contain("[HttpPost(\"password/request-reset\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/auth/password/request-reset\")]");
        source.Should().Contain("public async Task<IActionResult> ResetPasswordAsync(");
        source.Should().Contain("[HttpPost(\"password/reset\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/auth/password/reset\")]");
    }

    [Fact]
    public void AdminBaseController_Should_RequireAdminPanelPermissionByDefault()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));

        source.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");
    }

    [Fact]
    public void BusinessesController_Should_KeepSensitiveLifecycleActionsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        source.Should().Contain("[HttpPost, ValidateAntiForgeryToken]");
        source.Should().Contain("[PermissionAuthorize(PermissionKeys.FullAdminAccess)]");
        source.Should().Contain("public async Task<IActionResult> Delete(");
        source.Should().Contain("public async Task<IActionResult> Approve(");
        source.Should().Contain("public async Task<IActionResult> Suspend(");
        source.Should().Contain("public async Task<IActionResult> Reactivate(");
        source.Should().Contain("public async Task<IActionResult> CreateMember(");
    }

    [Fact]
    public void BillingController_Should_KeepFinancialMutationPostsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Billing", "BillingController.cs"));

        source.Should().Contain("[ValidateAntiForgeryToken]");
        source.Should().Contain("public async Task<IActionResult> CreatePlan(BillingPlanEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditPlan(BillingPlanEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CreatePayment(PaymentEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditPayment(PaymentEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateFinancialAccount(FinancialAccountEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditFinancialAccount(FinancialAccountEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateExpense(ExpenseEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditExpense(ExpenseEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateJournalEntry(JournalEntryEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditJournalEntry(JournalEntryEditVm vm");
    }

    [Fact]
    public void BillingController_Should_KeepAdminFinanceWorkspacesAndEditorsReachable()
    {
        var billingSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Billing", "BillingController.cs"));
        var adminBaseSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));

        adminBaseSource.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");
        billingSource.Should().Contain("public sealed class BillingController : AdminBaseController");
        billingSource.Should().Contain("public IActionResult Index() => RedirectOrHtmx(nameof(Payments), new { });");
        billingSource.Should().Contain("public async Task<IActionResult> Plans(");
        billingSource.Should().Contain("public async Task<IActionResult> Payments(");
        billingSource.Should().Contain("public async Task<IActionResult> TaxCompliance(");
        billingSource.Should().Contain("public async Task<IActionResult> Webhooks(");
        billingSource.Should().Contain("public async Task<IActionResult> Refunds(");
        billingSource.Should().Contain("public async Task<IActionResult> FinancialAccounts(");
        billingSource.Should().Contain("public async Task<IActionResult> Expenses(");
        billingSource.Should().Contain("public async Task<IActionResult> JournalEntries(");
        billingSource.Should().Contain("public IActionResult CreatePlan()");
        billingSource.Should().Contain("public async Task<IActionResult> EditPlan(Guid id");
        billingSource.Should().Contain("public async Task<IActionResult> CreatePayment(Guid? businessId = null");
        billingSource.Should().Contain("public async Task<IActionResult> EditPayment(Guid id");
        billingSource.Should().Contain("public async Task<IActionResult> CreateFinancialAccount(Guid? businessId = null");
        billingSource.Should().Contain("public async Task<IActionResult> EditFinancialAccount(Guid id");
        billingSource.Should().Contain("public async Task<IActionResult> CreateExpense(Guid? businessId = null");
        billingSource.Should().Contain("public async Task<IActionResult> EditExpense(Guid id");
        billingSource.Should().Contain("public async Task<IActionResult> CreateJournalEntry(Guid? businessId = null");
        billingSource.Should().Contain("public async Task<IActionResult> EditJournalEntry(Guid id");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepTestSendPostsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        source.Should().Contain("public sealed class BusinessCommunicationsController : AdminBaseController");
        source.Should().Contain("[PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]");
        source.Should().Contain("[ValidateAntiForgeryToken]");
        source.Should().Contain("public async Task<IActionResult> RetryEmailAudit(");
        source.Should().Contain("public async Task<IActionResult> SendTestEmail(");
        source.Should().Contain("public async Task<IActionResult> SendTestSms(");
        source.Should().Contain("public async Task<IActionResult> SendTestWhatsApp(");
        source.Should().Contain("public async Task<IActionResult> Index(");
        source.Should().Contain("public async Task<IActionResult> Details(");
        source.Should().Contain("public async Task<IActionResult> EmailAudits(");
        source.Should().Contain("public async Task<IActionResult> ChannelAudits(");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedResendPolicyContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));

        controllerSource.Should().Contain("private List<CommunicationResendPolicyVm> BuildResendPolicies()");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationResendPolicyInvitationFlow\")");
        controllerSource.Should().Contain("CurrentSafeAction = T(\"CommunicationResendPolicyInvitationSafeAction\")");
        controllerSource.Should().Contain("GenericRetryStatus = T(\"CommunicationResendPolicyInvitationRetryStatus\")");
        controllerSource.Should().Contain("OperatorEntryPoint = T(\"CommunicationResendPolicyInvitationEntryPoint\")");
        controllerSource.Should().Contain("EscalationRule = T(\"CommunicationResendPolicyInvitationEscalation\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationResendPolicyActivationFlow\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationResendPolicyPasswordResetFlow\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationResendPolicyPhoneVerificationFlow\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationResendPolicyAdminTestFlow\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationResendPolicyAdminAlertsFlow\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"OpenInvitations\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"OpenUsers\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"BusinessCommunicationOpenPolicyAction\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"CommunicationResendPolicyOpenAuditLog\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"CommunicationResendPolicyOpenAlertSettings\")");

        indexViewSource.Should().Contain("@T.T(\"CommunicationResendPolicyTitle\")");
        indexViewSource.Should().Contain("@T.T(\"CommunicationCurrentSafeActionColumn\")");
        indexViewSource.Should().Contain("@T.T(\"CommunicationGenericRetryStatusColumn\")");
        indexViewSource.Should().Contain("@T.T(\"CommunicationOperatorEntryPointColumn\")");
        indexViewSource.Should().Contain("@T.T(\"CommunicationEscalationRuleColumn\")");
        indexViewSource.Should().Contain("@item.CurrentSafeAction");
        indexViewSource.Should().Contain("@item.OperatorEntryPoint");
        indexViewSource.Should().Contain("@item.EscalationRule");

        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationResendPolicySnapshotTitle\")");
        detailsViewSource.Should().Contain("@item.CurrentSafeAction");
        detailsViewSource.Should().Contain("@item.GenericRetryStatus");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedBuiltInFlowContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));

        controllerSource.Should().Contain("private List<BuiltInCommunicationFlowVm> BuildBuiltInFlows()");
        controllerSource.Should().Contain("Name = T(\"CommunicationBuiltInFlowInvitationName\")");
        controllerSource.Should().Contain("Trigger = T(\"CommunicationBuiltInFlowInvitationTrigger\")");
        controllerSource.Should().Contain("DeliveryPath = T(\"CommunicationBuiltInFlowInvitationDeliveryPath\")");
        controllerSource.Should().Contain("CurrentImplementationStatus = T(\"CommunicationBuiltInFlowInvitationStatus\")");
        controllerSource.Should().Contain("NextStep = T(\"CommunicationBuiltInFlowInvitationNextStep\")");
        controllerSource.Should().Contain("Name = T(\"CommunicationBuiltInFlowActivationName\")");
        controllerSource.Should().Contain("Name = T(\"CommunicationBuiltInFlowPasswordResetName\")");
        controllerSource.Should().Contain("Name = T(\"CommunicationBuiltInFlowPhoneVerificationName\")");
        controllerSource.Should().Contain("Name = T(\"CommunicationBuiltInFlowAdminTestName\")");
        controllerSource.Should().Contain("Name = T(\"CommunicationBuiltInFlowAdminAlertsName\")");
        controllerSource.Should().Contain("Name = T(\"CommunicationBuiltInFlowTestTargetsName\")");
        controllerSource.Should().Contain("private string DescribeBuiltInFlowChannel(string? channelGroup)");
        controllerSource.Should().Contain("\"Email\" => DescribeCommunicationChannel(\"Email\")");
        controllerSource.Should().Contain("\"SmsWhatsApp\" => T(\"CommunicationBuiltInFlowSmsWhatsAppChannel\")");
        controllerSource.Should().Contain("\"EmailSmsWhatsApp\" => T(\"CommunicationBuiltInFlowEmailSmsWhatsAppChannel\")");
        controllerSource.Should().Contain("\"EmailSmsWhatsAppCompact\" => T(\"CommunicationBuiltInFlowEmailSmsWhatsAppCompactChannel\")");
        controllerSource.Should().Contain("Channel = DescribeBuiltInFlowChannel(\"Email\")");
        controllerSource.Should().Contain("Channel = DescribeBuiltInFlowChannel(\"SmsWhatsApp\")");
        controllerSource.Should().Contain("Channel = DescribeBuiltInFlowChannel(\"EmailSmsWhatsApp\")");
        controllerSource.Should().Contain("Channel = DescribeBuiltInFlowChannel(\"EmailSmsWhatsAppCompact\")");

        indexViewSource.Should().Contain("@T.T(\"Trigger\")");
        indexViewSource.Should().Contain("@T.T(\"CommunicationDeliveryPathColumn\")");
        indexViewSource.Should().Contain("@T.T(\"CurrentStatus\")");
        indexViewSource.Should().Contain("@T.T(\"NextStep\")");
        indexViewSource.Should().Contain("@flow.Trigger");
        indexViewSource.Should().Contain("@flow.DeliveryPath");
        indexViewSource.Should().Contain("@flow.CurrentImplementationStatus");
        indexViewSource.Should().Contain("@flow.NextStep");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedCapabilityAndChannelOpsContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));

        controllerSource.Should().Contain("private List<CommunicationCapabilityCoverageVm> BuildCapabilityCoverage()");
        controllerSource.Should().Contain("Capability = T(\"CommunicationCapabilityTemplateEngine\")");
        controllerSource.Should().Contain("CurrentState = T(\"CommunicationCapabilityTemplateEngineState\")");
        controllerSource.Should().Contain("OperatorVisibility = T(\"CommunicationCapabilityTemplateEngineVisibility\")");
        controllerSource.Should().Contain("NextStep = T(\"CommunicationCapabilityTemplateEngineNextStep\")");
        controllerSource.Should().Contain("Capability = T(\"CommunicationCapabilityDeliveryLogging\")");
        controllerSource.Should().Contain("Capability = T(\"CommunicationCapabilityRetryWorkflow\")");
        controllerSource.Should().Contain("Capability = T(\"CommunicationCapabilityBusinessPolicyVisibility\")");
        controllerSource.Should().Contain("Capability = T(\"CommunicationCapabilityChannelTestTargets\")");

        controllerSource.Should().Contain("private List<CommunicationChannelOpsVm> BuildChannelOperations(SiteSettingDto settings)");
        controllerSource.Should().Contain("Channel = DescribeCommunicationChannel(\"Email\")");
        controllerSource.Should().Contain("CurrentState = emailReady ? T(\"CommunicationChannelOpsEmailReadyState\") : T(\"CommunicationChannelOpsNotReadyState\")");
        controllerSource.Should().Contain("LiveFlows = T(\"CommunicationChannelOpsEmailLiveFlows\")");
        controllerSource.Should().Contain("? T(\"CommunicationChannelOpsEmailReadyActions\")");
        controllerSource.Should().Contain(": T(\"CommunicationChannelOpsEmailNotReadyActions\")");
        controllerSource.Should().Contain("RiskBoundary = T(\"CommunicationChannelOpsEmailRiskBoundary\")");
        controllerSource.Should().Contain("NextStep = T(\"CommunicationChannelOpsEmailNextStep\")");
        controllerSource.Should().Contain("Channel = \"SMS\"");
        controllerSource.Should().Contain("CurrentState = smsReady ? T(\"CommunicationChannelOpsProviderReadyState\") : T(\"CommunicationChannelOpsNotReadyState\")");
        controllerSource.Should().Contain("LiveFlows = T(\"CommunicationChannelOpsSmsLiveFlows\")");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationChannelOpsSmsFallbackRiskBoundary\"), DescribePreferredPhoneVerificationChannel(settings.PhoneVerificationPreferredChannel))");
        controllerSource.Should().Contain("T(\"CommunicationChannelOpsSmsStrictRiskBoundary\")");
        controllerSource.Should().Contain("Channel = \"WhatsApp\"");
        controllerSource.Should().Contain("LiveFlows = T(\"CommunicationChannelOpsWhatsAppLiveFlows\")");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationChannelOpsWhatsAppFallbackRiskBoundary\"), DescribePreferredPhoneVerificationChannel(settings.PhoneVerificationPreferredChannel))");
        controllerSource.Should().Contain("T(\"CommunicationChannelOpsWhatsAppStrictRiskBoundary\")");

        indexViewSource.Should().Contain("@item.Capability");
        indexViewSource.Should().Contain("@item.CurrentState");
        indexViewSource.Should().Contain("@item.OperatorVisibility");
        indexViewSource.Should().Contain("@ChannelLabel(item.Channel)");
        indexViewSource.Should().Contain("@item.LiveFlows");
        indexViewSource.Should().Contain("@item.SafeOperatorActions");
        indexViewSource.Should().Contain("@item.RiskBoundary");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedTemplateInventoryContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));

        controllerSource.Should().Contain("private List<CommunicationTemplateInventoryVm> BuildTemplateInventory(SiteSettingDto settings)");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationTemplateInventoryInvitationFlow\")");
        controllerSource.Should().Contain("TemplateSurface = T(\"CommunicationTemplateInventoryInvitationSurface\")");
        controllerSource.Should().Contain("SubjectSource = T(\"CommunicationTemplateInventoryInvitationSubjectSource\")");
        controllerSource.Should().Contain("BodySource = T(\"CommunicationTemplateInventoryInvitationBodySource\")");
        controllerSource.Should().Contain("CurrentSubjectTemplate = SummarizeTemplate(settings.BusinessInvitationEmailSubjectTemplate, T(\"CommunicationTemplateInventoryInvitationSubjectFallback\"))");
        controllerSource.Should().Contain("CurrentBodyTemplate = SummarizeTemplate(settings.BusinessInvitationEmailBodyTemplate, T(\"CommunicationTemplateInventoryInvitationBodyFallback\"))");
        controllerSource.Should().Contain("OperatorControl = T(\"CommunicationTemplateInventoryInvitationOperatorControl\")");
        controllerSource.Should().Contain("NextStep = T(\"CommunicationTemplateInventoryInvitationNextStep\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationTemplateInventoryActivationFlow\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationTemplateInventoryPasswordResetFlow\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationTemplateInventoryPhoneVerificationFlow\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationTemplateInventoryAdminTestFlow\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationTemplateInventoryAdminAlertsFlow\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"OpenInvitations\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"OpenUsers\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"BusinessCommunicationOpenPolicyAction\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"CommunicationResendPolicyOpenAuditLog\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"CommunicationResendPolicyOpenAlertSettings\")");

        indexViewSource.Should().Contain("@T.T(\"CommunicationTemplateInventoryTitle\")");
        indexViewSource.Should().Contain("@T.T(\"CommunicationTemplateSurfaceColumn\")");
        indexViewSource.Should().Contain("@T.T(\"CommunicationSubjectSourceColumn\")");
        indexViewSource.Should().Contain("@T.T(\"CommunicationBodySourceColumn\")");
        indexViewSource.Should().Contain("@T.T(\"CommunicationOperatorControlColumn\")");
        indexViewSource.Should().Contain("@item.FlowName");
        indexViewSource.Should().Contain("@item.TemplateSurface");
        indexViewSource.Should().Contain("@item.SubjectSource");
        indexViewSource.Should().Contain("@item.BodySource");
        indexViewSource.Should().Contain("@item.OperatorControl");
        indexViewSource.Should().Contain("@item.NextStep");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedChannelFamilyContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));

        controllerSource.Should().Contain("private List<ChannelMessageFamilyVm> BuildChannelTemplateFamilies(SiteSettingDto settings, string? flowKey)");
        controllerSource.Should().Contain("FamilyKey = \"PhoneVerification\"");
        controllerSource.Should().Contain("FamilyKey = \"AdminCommunicationTest\"");
        controllerSource.Should().Contain("ChannelValue = \"SMS\"");
        controllerSource.Should().Contain("ChannelValue = \"WhatsApp\"");
        controllerSource.Should().Contain("FamilyName = T(\"CommunicationChannelFamilyPhoneVerificationName\")");
        controllerSource.Should().Contain("CurrentTemplate = SummarizeTemplate(settings.PhoneVerificationSmsTemplate, T(\"CommunicationTemplateInventoryPhoneVerificationSmsFallback\"))");
        controllerSource.Should().Contain("PolicyNote = string.Format(CultureInfo.InvariantCulture, T(\"CommunicationChannelFamilyPhoneVerificationPolicyNote\")");
        controllerSource.Should().Contain("SafeUsageNote = T(\"CommunicationChannelFamilyPhoneVerificationSafeUsage\")");
        controllerSource.Should().Contain("RolloutBoundary = T(\"CommunicationChannelFamilyPhoneVerificationRolloutBoundary\")");
        controllerSource.Should().Contain("FamilyName = T(\"CommunicationChannelFamilyAdminTestName\")");
        controllerSource.Should().Contain("TargetSurface = settings.CommunicationTestSmsRecipientE164 ?? T(\"CommunicationChannelFamilyReservedTestTargetMissing\")");
        controllerSource.Should().Contain("PolicyNote = T(\"CommunicationChannelFamilyAdminTestPolicyNote\")");
        controllerSource.Should().Contain("SafeUsageNote = T(\"CommunicationChannelFamilyAdminTestSmsSafeUsage\")");
        controllerSource.Should().Contain("SafeUsageNote = T(\"CommunicationChannelFamilyAdminTestWhatsAppSafeUsage\")");
        controllerSource.Should().Contain("ActionLabel = T(\"CommunicationChannelFamilyOpenTestTargetsAction\")");
        controllerSource.Should().Contain("return string.Equals(preferredChannel, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");

        indexViewSource.Should().Contain("@family.FamilyName");
        indexViewSource.Should().Contain("@family.PolicyNote");
        indexViewSource.Should().Contain("@family.SafeUsageNote");
        indexViewSource.Should().Contain("@family.RolloutBoundary");
        indexViewSource.Should().Contain("asp-route-channel=\"@family.ChannelValue\"");
        indexViewSource.Should().Contain("channel = family.ChannelValue");
        indexViewSource.Should().Contain("string.Equals(family.FamilyKey, \"PhoneVerification\", StringComparison.OrdinalIgnoreCase)");
        indexViewSource.Should().Contain("string.Equals(family.FamilyKey, \"AdminCommunicationTest\", StringComparison.OrdinalIgnoreCase)");
    }

    [Fact]
    public void BusinessCommunicationsChannelFamilyRoutes_Should_KeepCanonicalChannelValueContractsWired()
    {
        var viewModelSource = ReadWebAdminFile(Path.Combine("ViewModels", "Businesses", "BusinessCommunicationOpsVms.cs"));
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));

        viewModelSource.Should().Contain("public string ChannelValue { get; set; } = string.Empty;");
        viewModelSource.Should().Contain("public string FamilyKey { get; set; } = string.Empty;");

        controllerSource.Should().Contain("FamilyKey = \"PhoneVerification\"");
        controllerSource.Should().Contain("FamilyKey = \"AdminCommunicationTest\"");
        controllerSource.Should().Contain("ChannelValue = \"SMS\"");
        controllerSource.Should().Contain("ChannelValue = \"WhatsApp\"");
        controllerSource.Should().Contain("Channel = \"SMS\"");
        controllerSource.Should().Contain("Channel = \"WhatsApp\"");

        indexViewSource.Should().Contain("string.Equals(family.FamilyKey, \"PhoneVerification\", StringComparison.OrdinalIgnoreCase)");
        indexViewSource.Should().Contain("string.Equals(family.FamilyKey, \"AdminCommunicationTest\", StringComparison.OrdinalIgnoreCase)");
        indexViewSource.Should().Contain("string.Equals(family.ChannelValue, \"SMS\", StringComparison.OrdinalIgnoreCase)");
        indexViewSource.Should().Contain("string.Equals(family.ChannelValue, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");
        indexViewSource.Should().NotContain("string.Equals(family.FamilyName, T.T(\"CommunicationChannelFamilyPhoneVerificationName\"), StringComparison.OrdinalIgnoreCase)");
        indexViewSource.Should().NotContain("string.Equals(family.FamilyName, T.T(\"CommunicationChannelFamilyAdminTestName\"), StringComparison.OrdinalIgnoreCase)");
        indexViewSource.Should().NotContain("string.Equals(family.Channel, T.T(\"BusinessCommunicationSmsShort\"), StringComparison.OrdinalIgnoreCase)");
        indexViewSource.Should().NotContain("string.Equals(family.Channel, T.T(\"BusinessCommunicationWhatsAppShort\"), StringComparison.OrdinalIgnoreCase)");
        indexViewSource.Should().Contain("asp-route-channel=\"@family.ChannelValue\"");
        indexViewSource.Should().Contain("channel = family.ChannelValue");
        indexViewSource.Should().NotContain("asp-route-channel=\"@family.Channel\"");
        indexViewSource.Should().NotContain("channel = family.Channel })");

        detailsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"PhoneVerification\", StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"AdminCommunicationTest\", StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().Contain("string.Equals(family.ChannelValue, \"SMS\", StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().Contain("string.Equals(family.ChannelValue, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().NotContain("string.Equals(family.FamilyName, T.T(\"CommunicationChannelFamilyPhoneVerificationName\"), StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().NotContain("string.Equals(family.FamilyName, T.T(\"CommunicationChannelFamilyAdminTestName\"), StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().NotContain("string.Equals(family.Channel, T.T(\"BusinessCommunicationSmsShort\"), StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().NotContain("string.Equals(family.Channel, T.T(\"BusinessCommunicationWhatsAppShort\"), StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().Contain("asp-route-channel=\"@family.ChannelValue\"");
        detailsViewSource.Should().Contain("channel = family.ChannelValue");
        detailsViewSource.Should().NotContain("asp-route-channel=\"@family.Channel\"");
        detailsViewSource.Should().NotContain("channel = family.Channel })");

        channelAuditsViewSource.Should().Contain("asp-route-channel=\"@family.ChannelValue\"");
        channelAuditsViewSource.Should().Contain("channel = family.ChannelValue");
        channelAuditsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"PhoneVerification\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"AdminCommunicationTest\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(family.ChannelValue, \"SMS\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(family.ChannelValue, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("asp-route-channel=\"@family.Channel\"");
        channelAuditsViewSource.Should().NotContain("channel = family.Channel, businessId = Model.BusinessId");
        channelAuditsViewSource.Should().NotContain("string.Equals(family.FamilyName, T.T(\"CommunicationChannelFamilyPhoneVerificationName\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(family.FamilyName, T.T(\"CommunicationChannelFamilyAdminTestName\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(family.Channel, T.T(\"BusinessCommunicationSmsShort\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(family.Channel, T.T(\"BusinessCommunicationWhatsAppShort\"), StringComparison.OrdinalIgnoreCase)");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedDetailsReadinessContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));

        controllerSource.Should().Contain("private List<string> BuildActiveFlowNames(BusinessCommunicationProfileDto profile)");
        controllerSource.Should().Contain("flows.Add(T(\"CommunicationDetailsActiveFlowInvitation\"))");
        controllerSource.Should().Contain("flows.Add(T(\"CommunicationDetailsActiveFlowActivation\"))");
        controllerSource.Should().Contain("flows.Add(T(\"CommunicationDetailsActiveFlowPasswordReset\"))");
        controllerSource.Should().Contain("flows.Add(T(\"CommunicationDetailsActiveFlowAdminAlerts\"))");

        controllerSource.Should().Contain("private List<string> BuildReadinessIssues(");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueMissingSupportEmail\"))");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueMissingSenderIdentity\"))");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueMissingSmtp\"))");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueMissingAdminRouting\"))");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssuePendingApproval\"))");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueInactive\"))");

        controllerSource.Should().Contain("private List<string> BuildRecommendedActions(");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionCompleteBusinessDefaults\"))");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionOpenSmtp\"))");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionConfigureAdminRouting\"))");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionReviewMembers\"))");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionReviewInvitations\"))");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionReviewLockedMembers\"))");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionCompleteBeforeApproval\"))");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionNoImmediateAction\"))");

        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationReadinessIssuesTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationRecommendedNextActionsTitle\")");
        detailsViewSource.Should().Contain("@foreach (var issue in Model.ReadinessIssues)");
        detailsViewSource.Should().Contain("@foreach (var action in Model.RecommendedActions)");
        detailsViewSource.Should().Contain("@if (Model.ReadinessIssues.Count == 0)");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationNoReadinessIssues\")");
    }

    [Fact]
    public void BusinessCommunicationsFamilyChannelPayloads_Should_RemainCanonical()
    {
        var viewModelSource = ReadWebAdminFile(Path.Combine("ViewModels", "Businesses", "BusinessCommunicationOpsVms.cs"));
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));
        var familyBuilderStart = controllerSource.IndexOf("private List<ChannelMessageFamilyVm> BuildChannelTemplateFamilies", StringComparison.Ordinal);

        familyBuilderStart.Should().BeGreaterThanOrEqualTo(0);
        var familyBuilderSource = controllerSource[familyBuilderStart..];

        viewModelSource.Should().Contain("public string Channel { get; set; } = string.Empty;");
        viewModelSource.Should().Contain("public string ChannelValue { get; set; } = string.Empty;");

        familyBuilderSource.Should().Contain("Channel = \"SMS\"");
        familyBuilderSource.Should().Contain("Channel = \"WhatsApp\"");
        familyBuilderSource.Should().Contain("ChannelValue = \"SMS\"");
        familyBuilderSource.Should().Contain("ChannelValue = \"WhatsApp\"");
        familyBuilderSource.Should().NotContain("Channel = T(\"BusinessCommunicationSmsShort\")");
        familyBuilderSource.Should().NotContain("Channel = T(\"BusinessCommunicationWhatsAppShort\")");

        indexViewSource.Should().Contain("@ChannelLabel(family.Channel)");
        detailsViewSource.Should().Contain("@ChannelLabel(family.Channel)");
        channelAuditsViewSource.Should().Contain("@ChannelLabel(family.Channel)");
    }

    [Fact]
    public void BusinessCommunicationsPreferredVerificationChannelLabels_Should_RemainHelperBacked()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("private string DescribePreferredPhoneVerificationChannel(string? preferredChannel)");
        controllerSource.Should().Contain("string.Equals(preferredChannel, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");
        controllerSource.Should().Contain("? DescribeCommunicationChannel(\"WhatsApp\")");
        controllerSource.Should().Contain(": DescribeCommunicationChannel(\"SMS\")");

        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationChannelOpsSmsFallbackRiskBoundary\"), DescribePreferredPhoneVerificationChannel(settings.PhoneVerificationPreferredChannel))");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationChannelOpsWhatsAppFallbackRiskBoundary\"), DescribePreferredPhoneVerificationChannel(settings.PhoneVerificationPreferredChannel))");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationChannelFamilyPhoneVerificationPolicyNote\"), DescribePreferredPhoneVerificationChannel(settings.PhoneVerificationPreferredChannel), settings.PhoneVerificationAllowFallback ? T(\"Enabled\") : T(\"Disabled\"))");

        controllerSource.Should().NotContain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationChannelOpsSmsFallbackRiskBoundary\"), settings.PhoneVerificationPreferredChannel ?? T(\"BusinessCommunicationSmsShort\"))");
        controllerSource.Should().NotContain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationChannelOpsWhatsAppFallbackRiskBoundary\"), settings.PhoneVerificationPreferredChannel ?? T(\"BusinessCommunicationSmsShort\"))");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedAuditGuidanceContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var emailAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "EmailAudits.cshtml"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));

        controllerSource.Should().Contain("private string BuildAuditRecommendedAction(EmailDispatchAuditListItemDto item)");
        controllerSource.Should().Contain("return T(\"CommunicationAuditRecommendedActionNoImmediateAction\")");
        controllerSource.Should().Contain("? T(\"CommunicationAuditRecommendedActionInvitationBusiness\")");
        controllerSource.Should().Contain(": T(\"CommunicationAuditRecommendedActionInvitationGeneric\")");
        controllerSource.Should().Contain("? T(\"CommunicationAuditRecommendedActionActivationBusiness\")");
        controllerSource.Should().Contain(": T(\"CommunicationAuditRecommendedActionActivationGeneric\")");
        controllerSource.Should().Contain("? T(\"CommunicationAuditRecommendedActionPasswordResetBusiness\")");
        controllerSource.Should().Contain(": T(\"CommunicationAuditRecommendedActionPasswordResetGeneric\")");
        controllerSource.Should().Contain("return T(\"CommunicationAuditRecommendedActionGeneric\")");
        controllerSource.Should().Contain("private string BuildEmailAuditChainStatusMix(string? statusMix)");
        controllerSource.Should().Contain("\"Mixed success/failure\" => T(\"CommunicationChainStatusMixed\")");
        controllerSource.Should().Contain("private string? BuildEmailAuditRetryBlockedReason(EmailDispatchAuditListItemDto item)");
        controllerSource.Should().Contain("return T(\"CommunicationEmailRetryBlockedUnsupported\")");
        controllerSource.Should().Contain("return T(\"CommunicationEmailRetryBlockedClosed\")");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationEmailRetryBlockedRateLimited\"), item.RecentAttemptCount24h)");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationEmailRetryBlockedCooldownUntil\"), item.RetryAvailableAtUtc.Value)");

        controllerSource.Should().Contain("private List<CommunicationFlowPlaybookVm> BuildAuditPlaybooks()");
        controllerSource.Should().Contain("Title = T(\"CommunicationAuditPlaybookInvitationTitle\")");
        controllerSource.Should().Contain("ScopeNote = T(\"CommunicationAuditPlaybookInvitationScope\")");
        controllerSource.Should().Contain("AllowedAction = T(\"CommunicationAuditPlaybookInvitationAllowedAction\")");
        controllerSource.Should().Contain("EscalationRule = T(\"CommunicationAuditPlaybookInvitationEscalation\")");
        controllerSource.Should().Contain("Title = T(\"CommunicationAuditPlaybookActivationTitle\")");
        controllerSource.Should().Contain("Title = T(\"CommunicationAuditPlaybookPasswordResetTitle\")");
        controllerSource.Should().Contain("Title = T(\"CommunicationAuditPlaybookAdminTestTitle\")");

        emailAuditsViewSource.Should().Contain("@playbook.ScopeNote");
        emailAuditsViewSource.Should().Contain("@playbook.AllowedAction");
        emailAuditsViewSource.Should().Contain("@playbook.EscalationRule");
        detailsViewSource.Should().Contain("@item.RecommendedAction");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedFilterContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var emailAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "EmailAudits.cshtml"));
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));

        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildFilterItems(BusinessCommunicationSetupFilter selectedFilter)");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationSetupFilterNeedsSetup\")");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationSetupFilterMissingSupportEmail\")");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationSetupFilterMissingSenderIdentity\")");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationSetupFilterTransactionalEnabled\")");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationSetupFilterMarketingEnabled\")");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationSetupFilterOperationalAlertsEnabled\")");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationSetupFilterAllBusinesses\")");
        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildAuditStatusItems(string? selectedStatus)");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationAuditStatusAll\")");
        controllerSource.Should().Contain("private string DescribeDeliveryStatus(string? status)");
        controllerSource.Should().Contain("\"Sent\" => T(\"Sent\")");
        controllerSource.Should().Contain("\"Failed\" => T(\"Failed\")");
        controllerSource.Should().Contain("\"Pending\" => T(\"Pending\")");
        controllerSource.Should().Contain("new SelectListItem(DescribeDeliveryStatus(\"Sent\"), \"Sent\"");
        controllerSource.Should().Contain("new SelectListItem(DescribeDeliveryStatus(\"Failed\"), \"Failed\"");
        controllerSource.Should().Contain("new SelectListItem(DescribeDeliveryStatus(\"Pending\"), \"Pending\"");
        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildAuditFlowItems(string? selectedFlowKey)");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationAuditFlowAll\")");
        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildChannelItems(string? selectedChannel)");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationChannelAll\")");
        controllerSource.Should().Contain("new SelectListItem(DescribeCommunicationChannel(\"SMS\"), \"SMS\"");
        controllerSource.Should().Contain("new SelectListItem(DescribeCommunicationChannel(\"WhatsApp\"), \"WhatsApp\"");
        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildChannelProviderItems(IEnumerable<string> providers, string? selectedProvider)");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationProviderAll\")");
        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildChannelFlowItems(string? selectedFlowKey)");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationChannelFlowAll\")");

        emailAuditsViewSource.Should().Contain("asp-items=\"Model.StatusItems\"");
        emailAuditsViewSource.Should().Contain("asp-items=\"Model.FlowItems\"");
        channelAuditsViewSource.Should().Contain("asp-items=\"Model.ProviderItems\"");
        channelAuditsViewSource.Should().Contain("asp-items=\"Model.ChannelItems\"");
        channelAuditsViewSource.Should().Contain("asp-items=\"Model.FlowItems\"");
        channelAuditsViewSource.Should().Contain("asp-items=\"Model.StatusItems\"");
    }

    [Fact]
    public void BusinessCommunicationsStatusAndChannelShortcutHelpers_Should_RemainSourceBacked()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));

        controllerSource.Should().Contain("private string DescribeDeliveryStatus(string? status)");
        controllerSource.Should().Contain("\"Sent\" => T(\"Sent\")");
        controllerSource.Should().Contain("\"Failed\" => T(\"Failed\")");
        controllerSource.Should().Contain("\"Pending\" => T(\"Pending\")");
        controllerSource.Should().Contain("new SelectListItem(DescribeDeliveryStatus(\"Sent\"), \"Sent\"");
        controllerSource.Should().Contain("new SelectListItem(DescribeDeliveryStatus(\"Failed\"), \"Failed\"");
        controllerSource.Should().Contain("new SelectListItem(DescribeDeliveryStatus(\"Pending\"), \"Pending\"");
        controllerSource.Should().NotContain("new SelectListItem(T(\"Sent\"), \"Sent\"");
        controllerSource.Should().NotContain("new SelectListItem(T(\"Failed\"), \"Failed\"");
        controllerSource.Should().NotContain("new SelectListItem(T(\"Pending\"), \"Pending\"");

        indexViewSource.Should().Contain("@ChannelLabel(\"SMS\")");
        indexViewSource.Should().Contain("@ChannelLabel(\"WhatsApp\")");
        indexViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"SMS\")</a>");
        indexViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"WhatsApp\")</a>");

        detailsViewSource.Should().Contain("@ChannelLabel(\"SMS\")");
        detailsViewSource.Should().Contain("@ChannelLabel(\"WhatsApp\")");
        detailsViewSource.Should().NotContain("@T.T(\"SMS\") @(Model.SmsTransportConfigured ? T.T(\"CommonReadyBadge\") : T.T(\"CommonMissingBadge\"))");
        detailsViewSource.Should().NotContain("@T.T(\"WhatsApp\") @(Model.WhatsAppTransportConfigured ? T.T(\"CommonReadyBadge\") : T.T(\"CommonMissingBadge\"))");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepIndexWorkspaceCompositionAndRenderContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Index(");
        controllerSource.Should().Contain("var summary = await _getSummary.HandleAsync(ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (items, total) = await _getSetupPage.HandleAsync(page, pageSize, query, setupOnly, filter, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (emailAudits, _, _) = await _getEmailDispatchAuditsPage");
        controllerSource.Should().Contain("pageSize: 10,");
        controllerSource.Should().Contain("var (channelAudits, channelAuditSummary) = await _getChannelDispatchActivity");
        controllerSource.Should().Contain("var vm = new BusinessCommunicationOpsVm");
        controllerSource.Should().Contain("Transport = new BusinessCommunicationOpsTransportVm");
        controllerSource.Should().Contain("Summary = new BusinessCommunicationOpsSummaryPanelVm");
        controllerSource.Should().Contain("BuiltInFlows = BuildBuiltInFlows(),");
        controllerSource.Should().Contain("TemplateInventory = BuildTemplateInventory(settings),");
        controllerSource.Should().Contain("CapabilityCoverage = BuildCapabilityCoverage(),");
        controllerSource.Should().Contain("ChannelOperations = BuildChannelOperations(settings),");
        controllerSource.Should().Contain("ChannelTemplateFamilies = BuildChannelTemplateFamilies(settings, null),");
        controllerSource.Should().Contain("ResendPolicies = BuildResendPolicies(),");
        controllerSource.Should().Contain("ChannelAuditSummary = new ChannelDispatchAuditSummaryVm");
        controllerSource.Should().Contain("RecentEmailAudits = emailAudits.Select(x => new EmailDispatchAuditListItemVm");
        controllerSource.Should().Contain("RecentChannelAudits = channelAudits.Select(x => new ChannelDispatchAuditListItemVm");
        controllerSource.Should().Contain("Items = items.Select(x => new BusinessCommunicationSetupListItemVm");
        controllerSource.Should().Contain("return RenderCommunicationsWorkspace(vm);");
        controllerSource.Should().Contain("private IActionResult RenderCommunicationsWorkspace(BusinessCommunicationOpsVm vm)");
        controllerSource.Should().Contain("return PartialView(\"~/Views/BusinessCommunications/Index.cshtml\", vm);");
        controllerSource.Should().Contain("return View(\"Index\", vm);");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepDetailsWorkspaceCompositionAndRenderContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Details(Guid businessId, CancellationToken ct = default)");
        controllerSource.Should().Contain("var profile = await _getProfile.HandleAsync(businessId, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessCommunicationProfileNotFound\");");
        controllerSource.Should().Contain("var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (recentAudits, _, _) = await _getEmailDispatchAuditsPage");
        controllerSource.Should().Contain("pageSize: 5,");
        controllerSource.Should().Contain("var vm = new BusinessCommunicationProfileVm");
        controllerSource.Should().Contain("ActiveFlowNames = BuildActiveFlowNames(profile),");
        controllerSource.Should().Contain("TemplateInventory = BuildTemplateInventory(settings),");
        controllerSource.Should().Contain("ChannelOperations = BuildChannelOperations(settings),");
        controllerSource.Should().Contain("ChannelTemplateFamilies = BuildChannelTemplateFamilies(settings, null),");
        controllerSource.Should().Contain("ResendPolicies = BuildResendPolicies(),");
        controllerSource.Should().Contain("ReadinessIssues = BuildReadinessIssues(profile, emailTransportConfigured, adminAlertRoutingConfigured),");
        controllerSource.Should().Contain("RecommendedActions = BuildRecommendedActions(profile, emailTransportConfigured, adminAlertRoutingConfigured),");
        controllerSource.Should().Contain("RecentEmailAudits = recentAudits.Select(x => new EmailDispatchAuditListItemVm");
        controllerSource.Should().Contain("var (channelAudits, channelAuditSummary) = await _getChannelDispatchActivity");
        controllerSource.Should().Contain("vm.ChannelAuditSummary = new ChannelDispatchAuditSummaryVm");
        controllerSource.Should().Contain("vm.RecentChannelAudits = channelAudits.Select(x => new ChannelDispatchAuditListItemVm");
        controllerSource.Should().Contain("return RenderCommunicationProfileWorkspace(vm);");
        controllerSource.Should().Contain("private IActionResult RenderCommunicationProfileWorkspace(BusinessCommunicationProfileVm vm)");
        controllerSource.Should().Contain("return PartialView(\"~/Views/BusinessCommunications/Details.cshtml\", vm);");
        controllerSource.Should().Contain("return View(\"Details\", vm);");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepEmailAuditsWorkspaceCompositionAndRenderContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> EmailAudits(");
        controllerSource.Should().Contain("var (items, total, chainSummary) = await _getEmailDispatchAuditsPage");
        controllerSource.Should().Contain("var summary = await _getEmailDispatchAuditsPage.GetSummaryAsync(businessId, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var vm = new EmailDispatchAuditsListVm");
        controllerSource.Should().Contain("CanSendTestEmail = await CanSendTestEmailAsync(ct).ConfigureAwait(false),");
        controllerSource.Should().Contain("Summary = new EmailDispatchAuditSummaryVm");
        controllerSource.Should().Contain("ChainSummary = chainSummary == null ? null : new EmailDispatchAuditChainSummaryVm");
        controllerSource.Should().Contain("StatusMix = BuildEmailAuditChainStatusMix(chainSummary.StatusMix),");
        controllerSource.Should().Contain("PageSizeItems = BuildPageSizeItems(pageSize),");
        controllerSource.Should().Contain("StatusItems = BuildAuditStatusItems(status),");
        controllerSource.Should().Contain("FlowItems = BuildAuditFlowItems(flowKey),");
        controllerSource.Should().Contain("Playbooks = BuildAuditPlaybooks(),");
        controllerSource.Should().Contain("Items = items.Select(x => new EmailDispatchAuditListItemVm");
        controllerSource.Should().Contain("RetryBlockedReason = BuildEmailAuditRetryBlockedReason(x),");
        controllerSource.Should().Contain("ChainStatusMix = BuildEmailAuditChainStatusMix(x.ChainStatusMix),");
        controllerSource.Should().Contain("RecommendedAction = BuildAuditRecommendedAction(x)");
        controllerSource.Should().Contain("return RenderEmailAuditsWorkspace(vm);");
        controllerSource.Should().Contain("private IActionResult RenderEmailAuditsWorkspace(EmailDispatchAuditsListVm vm)");
        controllerSource.Should().Contain("return PartialView(\"~/Views/BusinessCommunications/EmailAudits.cshtml\", vm);");
        controllerSource.Should().Contain("return View(\"EmailAudits\", vm);");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepChannelAuditsWorkspaceCompositionAndRenderContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> ChannelAudits(");
        controllerSource.Should().Contain("var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var filter = new ChannelDispatchAuditFilterDto");
        controllerSource.Should().Contain("var (items, total, summary, chainSummary, providerSummary) = await _getChannelDispatchActivity");
        controllerSource.Should().Contain("HandlePageAsync(page, pageSize, filter, ct)");
        controllerSource.Should().Contain("var vm = new ChannelDispatchAuditsListVm");
        controllerSource.Should().Contain("Summary = new ChannelDispatchAuditSummaryVm");
        controllerSource.Should().Contain("ChainSummary = chainSummary == null ? null : new ChannelDispatchAuditChainSummaryVm");
        controllerSource.Should().Contain("RecommendedAction = BuildChannelChainRecommendedAction(chainSummary.RecommendedAction),");
        controllerSource.Should().Contain("EscalationHint = BuildChannelChainEscalationHint(chainSummary.EscalationHint),");
        controllerSource.Should().Contain("ProviderSummary = providerSummary == null ? null : new ChannelDispatchProviderSummaryVm");
        controllerSource.Should().Contain("RecommendedAction = BuildChannelProviderRecommendedAction(providerSummary.RecommendedAction),");
        controllerSource.Should().Contain("EscalationHint = BuildChannelProviderEscalationHint(providerSummary.EscalationHint)");
        controllerSource.Should().Contain("TemplateFamilies = BuildChannelTemplateFamilies(settings, filter.FlowKey),");
        controllerSource.Should().Contain("Items = items.Select(x => new ChannelDispatchAuditListItemVm");
        controllerSource.Should().Contain("ActionPolicyState = BuildChannelAuditActionPolicyState(x.ActionPolicyState),");
        controllerSource.Should().Contain("ActionBlockedReason = BuildChannelAuditActionBlockedReason(x),");
        controllerSource.Should().Contain("EscalationReason = BuildChannelAuditEscalationReason(x),");
        controllerSource.Should().Contain("ProviderItems = BuildChannelProviderItems(items.Select(x => x.Provider), provider),");
        controllerSource.Should().Contain("ChannelItems = BuildChannelItems(channel),");
        controllerSource.Should().Contain("FlowItems = BuildChannelFlowItems(flowKey),");
        controllerSource.Should().Contain("StatusItems = BuildAuditStatusItems(status)");
        controllerSource.Should().Contain("return RenderChannelAuditsWorkspace(vm);");
        controllerSource.Should().Contain("private IActionResult RenderChannelAuditsWorkspace(ChannelDispatchAuditsListVm vm)");
        controllerSource.Should().Contain("return PartialView(\"~/Views/BusinessCommunications/ChannelAudits.cshtml\", vm);");
        controllerSource.Should().Contain("return View(\"ChannelAudits\", vm);");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepIndexWorkspaceMappingContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("Summary = new BusinessCommunicationOpsSummaryPanelVm");
        controllerSource.Should().Contain("TransactionalEmailBusinessesCount = summary.BusinessesWithCustomerEmailNotificationsEnabledCount,");
        controllerSource.Should().Contain("MarketingEmailBusinessesCount = summary.BusinessesWithMarketingEmailsEnabledCount,");
        controllerSource.Should().Contain("OperationalAlertBusinessesCount = summary.BusinessesWithOperationalAlertEmailsEnabledCount,");
        controllerSource.Should().Contain("MissingSupportEmailCount = summary.BusinessesMissingSupportEmailCount,");
        controllerSource.Should().Contain("MissingSenderIdentityCount = summary.BusinessesMissingSenderIdentityCount,");
        controllerSource.Should().Contain("BusinessesRequiringEmailSetupCount = summary.BusinessesRequiringEmailSetupCount");
        controllerSource.Should().Contain("ChannelAuditSummary = new ChannelDispatchAuditSummaryVm");
        controllerSource.Should().Contain("RecentEmailAudits = emailAudits.Select(x => new EmailDispatchAuditListItemVm");
        controllerSource.Should().Contain("RetryBlockedReason = BuildEmailAuditRetryBlockedReason(x),");
        controllerSource.Should().Contain("ChainStatusMix = BuildEmailAuditChainStatusMix(x.ChainStatusMix),");
        controllerSource.Should().Contain("RecommendedAction = BuildAuditRecommendedAction(x)");
        controllerSource.Should().Contain("RecentChannelAudits = channelAudits.Select(x => new ChannelDispatchAuditListItemVm");
        controllerSource.Should().Contain("Items = items.Select(x => new BusinessCommunicationSetupListItemVm");
        controllerSource.Should().Contain("CommunicationReplyToEmail = x.CommunicationReplyToEmail,");
        controllerSource.Should().Contain("OperationalAlertEmailsEnabled = x.OperationalAlertEmailsEnabled,");
        controllerSource.Should().Contain("MissingSenderIdentity = x.MissingSenderIdentity");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepDetailsWorkspaceMappingContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("var vm = new BusinessCommunicationProfileVm");
        controllerSource.Should().Contain("Id = profile.Id,");
        controllerSource.Should().Contain("Name = profile.Name,");
        controllerSource.Should().Contain("LegalName = profile.LegalName,");
        controllerSource.Should().Contain("OperationalStatus = profile.OperationalStatus,");
        controllerSource.Should().Contain("OpenInvitationCount = profile.OpenInvitationCount,");
        controllerSource.Should().Contain("PendingActivationMemberCount = profile.PendingActivationMemberCount,");
        controllerSource.Should().Contain("LockedMemberCount = profile.LockedMemberCount,");
        controllerSource.Should().Contain("ActiveFlowNames = BuildActiveFlowNames(profile),");
        controllerSource.Should().Contain("ReadinessIssues = BuildReadinessIssues(profile, emailTransportConfigured, adminAlertRoutingConfigured),");
        controllerSource.Should().Contain("RecommendedActions = BuildRecommendedActions(profile, emailTransportConfigured, adminAlertRoutingConfigured),");
        controllerSource.Should().Contain("RecentEmailAudits = recentAudits.Select(x => new EmailDispatchAuditListItemVm");
        controllerSource.Should().Contain("vm.ChannelAuditSummary = new ChannelDispatchAuditSummaryVm");
        controllerSource.Should().Contain("vm.RecentChannelAudits = channelAudits.Select(x => new ChannelDispatchAuditListItemVm");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepEmailAuditsWorkspaceMappingContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("Summary = new EmailDispatchAuditSummaryVm");
        controllerSource.Should().Contain("TotalCount = summary.TotalCount,");
        controllerSource.Should().Contain("FailedCount = summary.FailedCount,");
        controllerSource.Should().Contain("SentCount = summary.SentCount,");
        controllerSource.Should().Contain("PendingCount = summary.PendingCount,");
        controllerSource.Should().Contain("StalePendingCount = summary.StalePendingCount,");
        controllerSource.Should().Contain("BusinessLinkedFailureCount = summary.BusinessLinkedFailureCount,");
        controllerSource.Should().Contain("FailedInvitationCount = summary.FailedInvitationCount,");
        controllerSource.Should().Contain("FailedActivationCount = summary.FailedActivationCount,");
        controllerSource.Should().Contain("FailedPasswordResetCount = summary.FailedPasswordResetCount,");
        controllerSource.Should().Contain("FailedAdminTestCount = summary.FailedAdminTestCount,");
        controllerSource.Should().Contain("RetryReadyCount = summary.RetryReadyCount,");
        controllerSource.Should().Contain("RetryBlockedCount = summary.RetryBlockedCount,");
        controllerSource.Should().Contain("ChainSummary = chainSummary == null ? null : new EmailDispatchAuditChainSummaryVm");
        controllerSource.Should().Contain("StatusMix = BuildEmailAuditChainStatusMix(chainSummary.StatusMix),");
        controllerSource.Should().Contain("RecentHistory = chainSummary.RecentHistory.Select(x => new EmailDispatchAuditChainHistoryItemVm");
        controllerSource.Should().Contain("Items = items.Select(x => new EmailDispatchAuditListItemVm");
        controllerSource.Should().Contain("RetryBlockedReason = BuildEmailAuditRetryBlockedReason(x),");
        controllerSource.Should().Contain("ChainStatusMix = BuildEmailAuditChainStatusMix(x.ChainStatusMix),");
        controllerSource.Should().Contain("RecommendedAction = BuildAuditRecommendedAction(x)");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepChannelAuditsWorkspaceMappingContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("var filter = new ChannelDispatchAuditFilterDto");
        controllerSource.Should().Contain("Query = query ?? string.Empty,");
        controllerSource.Should().Contain("RecipientAddress = recipientAddress ?? string.Empty,");
        controllerSource.Should().Contain("Provider = provider ?? string.Empty,");
        controllerSource.Should().Contain("Channel = channel ?? string.Empty,");
        controllerSource.Should().Contain("FlowKey = flowKey ?? string.Empty,");
        controllerSource.Should().Contain("Status = status ?? string.Empty,");
        controllerSource.Should().Contain("FailedOnly = failedOnly,");
        controllerSource.Should().Contain("ActionBlockedOnly = actionBlockedOnly,");
        controllerSource.Should().Contain("ChainResolvedOnly = chainResolvedOnly");
        controllerSource.Should().Contain("Summary = new ChannelDispatchAuditSummaryVm");
        controllerSource.Should().Contain("TotalCount = summary.TotalCount,");
        controllerSource.Should().Contain("FailedCount = summary.FailedCount,");
        controllerSource.Should().Contain("PendingCount = summary.PendingCount,");
        controllerSource.Should().Contain("SmsCount = summary.SmsCount,");
        controllerSource.Should().Contain("WhatsAppCount = summary.WhatsAppCount,");
        controllerSource.Should().Contain("PhoneVerificationCount = summary.PhoneVerificationCount,");
        controllerSource.Should().Contain("AdminTestCount = summary.AdminTestCount,");
        controllerSource.Should().Contain("ProviderReviewCount = summary.ProviderReviewCount,");
        controllerSource.Should().Contain("ProviderRecoveredCount = summary.ProviderRecoveredCount");
        controllerSource.Should().Contain("ChainSummary = chainSummary == null ? null : new ChannelDispatchAuditChainSummaryVm");
        controllerSource.Should().Contain("RecommendedAction = BuildChannelChainRecommendedAction(chainSummary.RecommendedAction),");
        controllerSource.Should().Contain("EscalationHint = BuildChannelChainEscalationHint(chainSummary.EscalationHint),");
        controllerSource.Should().Contain("ProviderSummary = providerSummary == null ? null : new ChannelDispatchProviderSummaryVm");
        controllerSource.Should().Contain("RecommendedAction = BuildChannelProviderRecommendedAction(providerSummary.RecommendedAction),");
        controllerSource.Should().Contain("EscalationHint = BuildChannelProviderEscalationHint(providerSummary.EscalationHint)");
        controllerSource.Should().Contain("Items = items.Select(x => new ChannelDispatchAuditListItemVm");
        controllerSource.Should().Contain("ActionPolicyState = BuildChannelAuditActionPolicyState(x.ActionPolicyState),");
        controllerSource.Should().Contain("ActionBlockedReason = BuildChannelAuditActionBlockedReason(x),");
        controllerSource.Should().Contain("EscalationReason = BuildChannelAuditEscalationReason(x),");
        controllerSource.Should().Contain("ProviderRecentAttemptCount24h = x.ProviderRecentAttemptCount24h,");
        controllerSource.Should().Contain("ProviderFailureCount24h = x.ProviderFailureCount24h,");
        controllerSource.Should().Contain("ProviderPressureState = x.ProviderPressureState,");
        controllerSource.Should().Contain("ProviderRecoveryState = x.ProviderRecoveryState,");
        controllerSource.Should().Contain("ProviderLastSuccessfulAttemptAtUtc = x.ProviderLastSuccessfulAttemptAtUtc");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepRetryEmailAuditPostContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> RetryEmailAudit(");
        controllerSource.Should().Contain("var result = await _retryEmailDispatchAudit");
        controllerSource.Should().Contain(".HandleAsync(new RetryEmailDispatchAuditDto { AuditId = id }, ct)");
        controllerSource.Should().Contain("SetSuccessMessage(\"EmailFlowRetriedSuccessfully\")");
        controllerSource.Should().Contain("TempData[\"Error\"] = result.Error ?? T(\"CommunicationEmailRetryFailedFallback\")");
        controllerSource.Should().Contain("return RedirectOrHtmx(");
        controllerSource.Should().Contain("nameof(EmailAudits),");
        controllerSource.Should().Contain("chainFollowUpOnly,");
        controllerSource.Should().Contain("chainResolvedOnly,");
        controllerSource.Should().Contain("businessId");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepSendTestEmailPostContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SendTestEmail(CancellationToken ct = default)");
        controllerSource.Should().Contain("var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var emailTransportConfigured = settings.SmtpEnabled &&");
        controllerSource.Should().Contain("SetErrorMessage(\"EmailTransportNotReadyForCommunicationTest\")");
        controllerSource.Should().Contain("SetErrorMessage(\"CommunicationTestInboxNotConfigured\")");
        controllerSource.Should().Contain("BuildCommunicationTestPlaceholders(");
        controllerSource.Should().Contain("channel: DescribeCommunicationChannel(\"Email\"),");
        controllerSource.Should().Contain("transportState: DescribeCommunicationTransportState(emailTransportConfigured)");
        controllerSource.Should().Contain("await _emailSender.SendAsync(");
        controllerSource.Should().Contain("FlowKey = \"AdminCommunicationTest\"");
        controllerSource.Should().Contain("TempData[\"Success\"] = string.Format(T(\"CommunicationTestEmailSentMessage\"), settings.CommunicationTestInboxEmail);");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepSendTestSmsPostContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SendTestSms(");
        controllerSource.Should().Contain("var smsTransportConfigured = settings.SmsEnabled &&");
        controllerSource.Should().Contain("SetErrorMessage(\"SmsTransportNotReadyForCommunicationTest\")");
        controllerSource.Should().Contain("SetErrorMessage(\"CommunicationTestSmsRecipientNotConfigured\")");
        controllerSource.Should().Contain("var smsCooldownUntilUtc = await GetChannelTestCooldownUntilUtcAsync(");
        controllerSource.Should().Contain("\"SMS\",");
        controllerSource.Should().Contain("TempData[\"Error\"] = string.Format(CultureInfo.InvariantCulture, T(\"CommunicationTestSmsCooldownMessage\"), smsCooldownUntilUtc.Value);");
        controllerSource.Should().Contain("channel: DescribeCommunicationChannel(\"SMS\"),");
        controllerSource.Should().Contain("await _smsSender.SendAsync(");
        controllerSource.Should().Contain("FlowKey = \"AdminCommunicationTest\"");
        controllerSource.Should().Contain("TempData[\"Success\"] = string.Format(T(\"CommunicationTestSmsSentMessage\"), settings.CommunicationTestSmsRecipientE164);");
        controllerSource.Should().Contain("return RedirectToChannelAuditsOrIndex(");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepSendTestWhatsAppPostContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SendTestWhatsApp(");
        controllerSource.Should().Contain("var whatsAppTransportConfigured = settings.WhatsAppEnabled &&");
        controllerSource.Should().Contain("SetErrorMessage(\"WhatsAppTransportNotReadyForCommunicationTest\")");
        controllerSource.Should().Contain("SetErrorMessage(\"CommunicationTestWhatsAppRecipientNotConfigured\")");
        controllerSource.Should().Contain("var whatsAppCooldownUntilUtc = await GetChannelTestCooldownUntilUtcAsync(");
        controllerSource.Should().Contain("\"WhatsApp\",");
        controllerSource.Should().Contain("TempData[\"Error\"] = string.Format(CultureInfo.InvariantCulture, T(\"CommunicationTestWhatsAppCooldownMessage\"), whatsAppCooldownUntilUtc.Value);");
        controllerSource.Should().Contain("channel: DescribeCommunicationChannel(\"WhatsApp\"),");
        controllerSource.Should().Contain("await _whatsAppSender.SendTextAsync(");
        controllerSource.Should().Contain("FlowKey = \"AdminCommunicationTest\"");
        controllerSource.Should().Contain("TempData[\"Success\"] = string.Format(T(\"CommunicationTestWhatsAppSentMessage\"), settings.CommunicationTestWhatsAppRecipientE164);");
        controllerSource.Should().Contain("return RedirectToChannelAuditsOrIndex(");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepChannelTestRedirectHelperWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("private IActionResult RedirectToChannelAuditsOrIndex(");
        controllerSource.Should().Contain("if (!returnToChannelAudits)");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        controllerSource.Should().Contain("return RedirectOrHtmx(");
        controllerSource.Should().Contain("nameof(ChannelAudits)");
        controllerSource.Should().Contain("recipientAddress,");
        controllerSource.Should().Contain("provider,");
        controllerSource.Should().Contain("channel,");
        controllerSource.Should().Contain("flowKey,");
        controllerSource.Should().Contain("status,");
        controllerSource.Should().Contain("chainFollowUpOnly,");
        controllerSource.Should().Contain("chainResolvedOnly,");
        controllerSource.Should().Contain("businessId");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepChannelTestCooldownHelperWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("private async Task<DateTime?> GetChannelTestCooldownUntilUtcAsync(");
        controllerSource.Should().Contain("var latestAttemptAtUtc = await _db.Set<ChannelDispatchAudit>()");
        controllerSource.Should().Contain(".AsNoTracking()");
        controllerSource.Should().Contain("x.FlowKey == \"AdminCommunicationTest\" &&");
        controllerSource.Should().Contain("x.Channel == channel &&");
        controllerSource.Should().Contain("x.RecipientAddress == recipientAddress)");
        controllerSource.Should().Contain(".OrderByDescending(x => x.AttemptedAtUtc)");
        controllerSource.Should().Contain(".Select(x => (DateTime?)x.AttemptedAtUtc)");
        controllerSource.Should().Contain(".FirstOrDefaultAsync(ct)");
        controllerSource.Should().Contain("if (!latestAttemptAtUtc.HasValue)");
        controllerSource.Should().Contain("var cooldownUntilUtc = latestAttemptAtUtc.Value.AddMinutes(5);");
        controllerSource.Should().Contain("return cooldownUntilUtc > DateTime.UtcNow ? cooldownUntilUtc : null;");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepTestEmailReadinessHelperWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("private async Task<bool> CanSendTestEmailAsync(CancellationToken ct)");
        controllerSource.Should().Contain("var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("return settings.SmtpEnabled &&");
        controllerSource.Should().Contain("!string.IsNullOrWhiteSpace(settings.SmtpHost) &&");
        controllerSource.Should().Contain("settings.SmtpPort.HasValue &&");
        controllerSource.Should().Contain("!string.IsNullOrWhiteSpace(settings.SmtpFromAddress) &&");
        controllerSource.Should().Contain("!string.IsNullOrWhiteSpace(settings.CommunicationTestInboxEmail);");
        controllerSource.Should().Contain("CanSendTestEmail = await CanSendTestEmailAsync(ct).ConfigureAwait(false),");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepTemplatePlaceholderHelpersWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("private static Dictionary<string, string?> BuildCommunicationTestPlaceholders(");
        controllerSource.Should().Contain("[\"channel\"] = channel,");
        controllerSource.Should().Contain("[\"requested_by\"] = requestedBy,");
        controllerSource.Should().Contain("[\"attempted_at_utc\"] = attemptedAtUtc.ToString(\"yyyy-MM-dd HH:mm:ss\"),");
        controllerSource.Should().Contain("[\"test_target\"] = testTarget,");
        controllerSource.Should().Contain("[\"transport_state\"] = transportState");
        controllerSource.Should().Contain("private string DescribeCommunicationTransportState(bool isReady)");
        controllerSource.Should().Contain("return isReady ? T(\"Ready\") : T(\"CommunicationTransportStateNotReady\");");
        controllerSource.Should().Contain("private static string RenderTemplate(string? template, string fallback, IReadOnlyDictionary<string, string?> placeholders)");
        controllerSource.Should().Contain("var output = string.IsNullOrWhiteSpace(template) ? fallback : template;");
        controllerSource.Should().Contain("foreach (var pair in placeholders)");
        controllerSource.Should().Contain("output = output.Replace(\"{\" + pair.Key + \"}\", pair.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);");
        controllerSource.Should().Contain("return output;");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedTestTransportContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("User?.Identity?.Name ?? T(\"CommunicationChannelFamilyOperatorPlaceholder\")");
        controllerSource.Should().Contain("private string DescribeCommunicationChannel(string? channel)");
        controllerSource.Should().Contain("\"Email\" => T(\"CommunicationBuiltInFlowEmailChannel\")");
        controllerSource.Should().Contain("\"WhatsApp\" => T(\"BusinessCommunicationWhatsAppShort\")");
        controllerSource.Should().Contain("_ => T(\"BusinessCommunicationSmsShort\")");
        controllerSource.Should().Contain("channel: DescribeCommunicationChannel(\"Email\")");
        controllerSource.Should().Contain("channel: DescribeCommunicationChannel(\"SMS\")");
        controllerSource.Should().Contain("channel: DescribeCommunicationChannel(\"WhatsApp\")");
        controllerSource.Should().Contain("BuildCommunicationTestPlaceholders(DescribeCommunicationChannel(\"SMS\"), T(\"CommunicationChannelFamilyOperatorPlaceholder\")");
        controllerSource.Should().Contain("BuildCommunicationTestPlaceholders(DescribeCommunicationChannel(\"WhatsApp\"), T(\"CommunicationChannelFamilyOperatorPlaceholder\")");
        controllerSource.Should().NotContain("channel: T(\"BusinessCommunicationSmsShort\")");
        controllerSource.Should().NotContain("channel: T(\"BusinessCommunicationWhatsAppShort\")");
        controllerSource.Should().Contain("transportState: DescribeCommunicationTransportState(emailTransportConfigured)");
        controllerSource.Should().Contain("transportState: DescribeCommunicationTransportState(smsTransportConfigured)");
        controllerSource.Should().Contain("transportState: DescribeCommunicationTransportState(whatsAppTransportConfigured)");
        controllerSource.Should().Contain("T(\"CommunicationTemplateInventoryAdminTestSubjectFallback\")");
        controllerSource.Should().Contain("T(\"CommunicationTestEmailBodyRuntimeFallback\")");
        controllerSource.Should().Contain("T(\"CommunicationTemplateInventoryAdminTestSmsBodyFallback\")");
        controllerSource.Should().Contain("T(\"CommunicationTemplateInventoryAdminTestWhatsAppBodyFallback\")");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationTestSmsCooldownMessage\"), smsCooldownUntilUtc.Value)");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationTestWhatsAppCooldownMessage\"), whatsAppCooldownUntilUtc.Value)");
        controllerSource.Should().Contain("private string DescribeCommunicationTransportState(bool isReady)");
        controllerSource.Should().Contain("return isReady ? T(\"Ready\") : T(\"CommunicationTransportStateNotReady\")");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedRetryFallbackContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("SetSuccessMessage(\"EmailFlowRetriedSuccessfully\")");
        controllerSource.Should().Contain("TempData[\"Error\"] = result.Error ?? T(\"CommunicationEmailRetryFailedFallback\")");
    }

    [Fact]
    public void ChannelAuditsView_Should_KeepLocalizedInlineTimelineContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));

        controllerSource.Should().Contain("private string BuildChannelAuditActionPolicyState(string? state)");
        controllerSource.Should().Contain("\"Canonical flow\" => T(\"CommunicationChannelActionPolicyCanonicalFlow\")");
        controllerSource.Should().Contain("\"Retry ready\" => T(\"CommunicationChannelActionPolicyRetryReady\")");
        controllerSource.Should().Contain("private string? BuildChannelAuditActionBlockedReason(ChannelDispatchAuditListItemDto item)");
        controllerSource.Should().Contain("return T(\"CommunicationChannelActionBlockedCanonicalFlow\")");
        controllerSource.Should().Contain("return T(\"CommunicationChannelActionBlockedCooldown\")");
        controllerSource.Should().Contain("return T(\"CommunicationChannelActionBlockedUnsupported\")");
        controllerSource.Should().Contain("private string? BuildChannelAuditEscalationReason(ChannelDispatchAuditListItemDto item)");
        controllerSource.Should().Contain("return T(\"CommunicationChannelEscalationPhoneVerification\")");
        controllerSource.Should().Contain("return T(\"CommunicationChannelEscalationAdminTest\")");
        controllerSource.Should().Contain("private string BuildChannelProviderRecommendedAction(string recommendedAction)");
        controllerSource.Should().Contain("\"Review SMS/WhatsApp readiness, fallback policy, and current verification channel choice before issuing another canonical verification code.\" => T(\"CommunicationChannelProviderRecommendedVerificationElevated\")");
        controllerSource.Should().Contain("private string BuildChannelProviderEscalationHint(string escalationHint)");
        controllerSource.Should().Contain("\"Escalate as provider or channel-policy instability if verification traffic continues to fail without any successful recovery in this provider lane.\" => T(\"CommunicationChannelProviderEscalationVerificationElevated\")");
        controllerSource.Should().Contain("private string BuildChannelChainRecommendedAction(string recommendedAction)");
        controllerSource.Should().Contain("\"Do not replay historical verification messages. If the user is still blocked, confirm the current phone number and request a fresh code through the canonical verification flow.\" => T(\"CommunicationChannelChainRecommendedVerificationRecovered\")");
        controllerSource.Should().Contain("private string BuildChannelChainEscalationHint(string escalationHint)");
        controllerSource.Should().Contain("\"Repeated verification failures without a successful send indicate a likely transport or channel-policy issue. Escalate after confirming SMS/WhatsApp readiness and fallback policy.\" => T(\"CommunicationChannelChainEscalationVerificationBlocked\")");

        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditFirstAttemptLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditLatestAttemptLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditChainContextLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditProviderLaneLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditTotalAttemptsInlineLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditProfileInlineLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditPriorAttemptsInlineLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditPriorFailuresInlineLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditLastSuccessInlineLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditRecoveryInlineLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditAttempts24hInlineLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditFailures24hInlineLabel\")");
        channelAuditsViewSource.Should().Contain("string ProviderPressureLabel(string? pressureState) => pressureState switch");
        channelAuditsViewSource.Should().Contain("\"Elevated\" => T.T(\"CommunicationProviderPressureElevated\")");
        channelAuditsViewSource.Should().Contain("\"Recovered\" => T.T(\"CommunicationProviderRecoveryRecovered\")");
        channelAuditsViewSource.Should().Contain("\"Mixed success/failure\" => T.T(\"CommunicationChainStatusMixed\")");
        channelAuditsViewSource.Should().Contain("@DeliveryStatusLabel(\"Pending\")");
        channelAuditsViewSource.Should().Contain("@ProviderPressureLabel(Model.ProviderSummary.PressureState)");
        channelAuditsViewSource.Should().Contain("@ProviderRecoveryLabel(Model.ProviderSummary.RecoveryState)");
        channelAuditsViewSource.Should().Contain("@ChainStatusMixLabel(Model.ChainSummary.StatusMix)");
        channelAuditsViewSource.Should().Contain("@ChainStatusMixLabel(item.ChainStatusMix)");
        channelAuditsViewSource.Should().Contain("@ProviderPressureLabel(item.ProviderPressureState)");
        channelAuditsViewSource.Should().Contain("@ProviderRecoveryLabel(item.ProviderRecoveryState)");
        channelAuditsViewSource.Should().Contain("@item.ActionPolicyState");
        channelAuditsViewSource.Should().Contain("@item.ActionBlockedReason");
        channelAuditsViewSource.Should().Contain("@item.EscalationReason");
        channelAuditsViewSource.Should().Contain("@Model.ProviderSummary.RecommendedAction");
        channelAuditsViewSource.Should().Contain("@Model.ProviderSummary.EscalationHint");
        channelAuditsViewSource.Should().Contain("@Model.ChainSummary.RecommendedAction");
        channelAuditsViewSource.Should().Contain("@Model.ChainSummary.EscalationHint");
        channelAuditsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"PhoneVerification\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"AdminCommunicationTest\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("T.T(\"BusinessCommunicationSmsShort\")");
        channelAuditsViewSource.Should().Contain("T.T(\"BusinessCommunicationWhatsAppShort\")");
        channelAuditsViewSource.Should().Contain("asp-route-channel=\"@family.ChannelValue\"");
        channelAuditsViewSource.Should().Contain("channel = family.ChannelValue");
    }

    [Fact]
    public void ChannelAuditsView_Should_KeepLocalizedActionPolicyAndGuidanceContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));

        controllerSource.Should().Contain("private string BuildChannelAuditActionPolicyState(string? state)");
        controllerSource.Should().Contain("\"Canonical flow\" => T(\"CommunicationChannelActionPolicyCanonicalFlow\")");
        controllerSource.Should().Contain("\"Cooldown\" => T(\"CommunicationChannelActionPolicyCooldown\")");
        controllerSource.Should().Contain("\"Retry ready\" => T(\"CommunicationChannelActionPolicyRetryReady\")");
        controllerSource.Should().Contain("\"Ready\" => T(\"CommunicationChannelActionPolicyReady\")");
        controllerSource.Should().Contain("\"Unsupported\" => T(\"CommunicationChannelActionPolicyUnsupported\")");
        controllerSource.Should().Contain("private string? BuildChannelAuditActionBlockedReason(ChannelDispatchAuditListItemDto item)");
        controllerSource.Should().Contain("private string? BuildChannelAuditEscalationReason(ChannelDispatchAuditListItemDto item)");
        controllerSource.Should().Contain("private string BuildChannelProviderRecommendedAction(string recommendedAction)");
        controllerSource.Should().Contain("private string BuildChannelProviderEscalationHint(string escalationHint)");
        controllerSource.Should().Contain("private string BuildChannelChainRecommendedAction(string recommendedAction)");
        controllerSource.Should().Contain("private string BuildChannelChainEscalationHint(string escalationHint)");

        channelAuditsViewSource.Should().Contain("@item.ActionPolicyState");
        channelAuditsViewSource.Should().Contain("@item.ActionBlockedReason");
        channelAuditsViewSource.Should().Contain("@item.EscalationReason");
        channelAuditsViewSource.Should().Contain("@Model.ProviderSummary.RecommendedAction");
        channelAuditsViewSource.Should().Contain("@Model.ProviderSummary.EscalationHint");
        channelAuditsViewSource.Should().Contain("@Model.ChainSummary.RecommendedAction");
        channelAuditsViewSource.Should().Contain("@Model.ChainSummary.EscalationHint");
    }

    [Fact]
    public void ChannelAuditsView_Should_KeepLocalizedProviderReviewChannelContractsWired()
    {
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));

        channelAuditsViewSource.Should().Contain("string ChannelLabel(string? channel) => channel switch");
        channelAuditsViewSource.Should().Contain("\"SMS\" => T.T(\"BusinessCommunicationSmsShort\")");
        channelAuditsViewSource.Should().Contain("\"WhatsApp\" => T.T(\"BusinessCommunicationWhatsAppShort\")");
        channelAuditsViewSource.Should().Contain("@ChannelLabel(Model.Channel)");
        channelAuditsViewSource.Should().Contain("string.Equals(Model.Channel, \"SMS\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(Model.Channel, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("<code>@Model.Channel</code>");
        channelAuditsViewSource.Should().NotContain("string.Equals(ChannelLabel(Model.Channel), T.T(\"BusinessCommunicationSmsShort\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(ChannelLabel(Model.Channel), T.T(\"BusinessCommunicationWhatsAppShort\"), StringComparison.OrdinalIgnoreCase)");
    }

    [Fact]
    public void ChannelAuditsView_Should_KeepCanonicalFamilyBranchContractsWired()
    {
        var viewModelSource = ReadWebAdminFile(Path.Combine("ViewModels", "Businesses", "BusinessCommunicationOpsVms.cs"));
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));

        viewModelSource.Should().Contain("public string FamilyKey { get; set; } = string.Empty;");
        viewModelSource.Should().Contain("public string ChannelValue { get; set; } = string.Empty;");

        controllerSource.Should().Contain("FamilyKey = \"PhoneVerification\"");
        controllerSource.Should().Contain("FamilyKey = \"AdminCommunicationTest\"");
        controllerSource.Should().Contain("ChannelValue = \"SMS\"");
        controllerSource.Should().Contain("ChannelValue = \"WhatsApp\"");

        channelAuditsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"PhoneVerification\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"AdminCommunicationTest\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(family.ChannelValue, \"SMS\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(family.ChannelValue, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("asp-route-channel=\"@family.ChannelValue\"");
        channelAuditsViewSource.Should().Contain("channel = family.ChannelValue");
        channelAuditsViewSource.Should().NotContain("string.Equals(family.FamilyName, T.T(\"CommunicationChannelFamilyPhoneVerificationName\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(family.FamilyName, T.T(\"CommunicationChannelFamilyAdminTestName\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(family.Channel, T.T(\"BusinessCommunicationSmsShort\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(family.Channel, T.T(\"BusinessCommunicationWhatsAppShort\"), StringComparison.OrdinalIgnoreCase)");
    }

    [Fact]
    public void BusinessCommunicationsViews_Should_KeepLocalizedFlowLabelContractsWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));
        var emailAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "EmailAudits.cshtml"));

        indexViewSource.Should().Contain("string FlowLabel(string? flowKey) => flowKey switch");
        indexViewSource.Should().Contain("\"BusinessInvitation\" => T.T(\"CommunicationDetailsActiveFlowInvitation\")");
        indexViewSource.Should().Contain("\"AccountActivation\" => T.T(\"CommunicationDetailsActiveFlowActivation\")");
        indexViewSource.Should().Contain("\"PasswordReset\" => T.T(\"CommunicationTemplateInventoryPasswordResetFlow\")");
        indexViewSource.Should().Contain("\"AdminCommunicationTest\" => T.T(\"CommunicationTemplateInventoryAdminTestFlow\")");
        indexViewSource.Should().Contain("\"PhoneVerification\" => T.T(\"CommunicationTemplateInventoryPhoneVerificationFlow\")");
        indexViewSource.Should().Contain("string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : flowKey");
        indexViewSource.Should().Contain("<td>@FlowLabel(item.FlowKey)</td>");
        indexViewSource.Should().NotContain("@(string.IsNullOrWhiteSpace(item.FlowKey) ? T.T(\"Unclassified\") : item.FlowKey)");
        indexViewSource.Should().Contain("string DeliveryStatusLabel(string? status) => status switch");
        indexViewSource.Should().Contain("\"Pending\" => T.T(\"Pending\")");
        indexViewSource.Should().Contain("string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : status");
        indexViewSource.Should().Contain("@DeliveryStatusLabel(\"Sent\")");
        indexViewSource.Should().Contain("@DeliveryStatusLabel(\"Failed\")");
        indexViewSource.Should().Contain("@DeliveryStatusLabel(item.Status)");
        indexViewSource.Should().Contain("string SeverityLabel(string? severity) => severity switch");
        indexViewSource.Should().Contain("\"High\" => T.T(\"High\")");
        indexViewSource.Should().Contain("\"Medium\" => T.T(\"Medium\")");
        indexViewSource.Should().Contain("\"Watch\" => T.T(\"Watch\")");
        indexViewSource.Should().Contain("\"Slow\" => T.T(\"Slow\")");
        indexViewSource.Should().Contain("\"Normal\" => T.T(\"Normal\")");
        indexViewSource.Should().Contain("string.IsNullOrWhiteSpace(severity) ? T.T(\"CommonUnclassified\") : severity");
        indexViewSource.Should().Contain("string ChannelLabel(string? channel) => channel switch");
        indexViewSource.Should().Contain("\"SMS\" => T.T(\"BusinessCommunicationSmsShort\")");
        indexViewSource.Should().Contain("\"WhatsApp\" => T.T(\"BusinessCommunicationWhatsAppShort\")");
        indexViewSource.Should().Contain("string.IsNullOrWhiteSpace(channel) ? T.T(\"CommonUnclassified\") : channel");
        indexViewSource.Should().Contain("@ChannelLabel(\"SMS\")");
        indexViewSource.Should().Contain("@ChannelLabel(\"WhatsApp\")");
        indexViewSource.Should().Contain("@ChannelLabel(item.Channel)");
        indexViewSource.Should().Contain("@ChannelLabel(family.Channel)");
        indexViewSource.Should().Contain("asp-route-channel=\"@family.ChannelValue\"");
        indexViewSource.Should().Contain("channel = family.ChannelValue");
        indexViewSource.Should().Contain("@SeverityLabel(item.Severity)");
        indexViewSource.Should().Contain("T.T(\"BusinessCommunicationSmsShort\")");
        indexViewSource.Should().Contain("T.T(\"BusinessCommunicationWhatsAppShort\")");

        detailsViewSource.Should().Contain("string FlowLabel(string? flowKey) => flowKey switch");
        detailsViewSource.Should().Contain("\"BusinessInvitation\" => T.T(\"CommunicationDetailsActiveFlowInvitation\")");
        detailsViewSource.Should().Contain("\"AccountActivation\" => T.T(\"CommunicationDetailsActiveFlowActivation\")");
        detailsViewSource.Should().Contain("\"PasswordReset\" => T.T(\"CommunicationTemplateInventoryPasswordResetFlow\")");
        detailsViewSource.Should().Contain("\"AdminCommunicationTest\" => T.T(\"CommunicationTemplateInventoryAdminTestFlow\")");
        detailsViewSource.Should().Contain("\"PhoneVerification\" => T.T(\"CommunicationTemplateInventoryPhoneVerificationFlow\")");
        detailsViewSource.Should().Contain("string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : flowKey");
        detailsViewSource.Should().Contain("string OperationalStatusLabel(string? operationalStatus) => operationalStatus switch");
        detailsViewSource.Should().Contain("\"Approved\" => T.T(\"Approved\")");
        detailsViewSource.Should().Contain("string.IsNullOrWhiteSpace(operationalStatus) ? T.T(\"CommonUnclassified\") : operationalStatus");
        detailsViewSource.Should().Contain("string DeliveryStatusLabel(string? status) => status switch");
        detailsViewSource.Should().Contain("\"Pending\" => T.T(\"Pending\")");
        detailsViewSource.Should().Contain("string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : status");
        detailsViewSource.Should().Contain("@DeliveryStatusLabel(\"Sent\")");
        detailsViewSource.Should().Contain("@DeliveryStatusLabel(\"Failed\")");
        detailsViewSource.Should().Contain("@DeliveryStatusLabel(item.Status)");
        detailsViewSource.Should().Contain("@OperationalStatusLabel(Model.OperationalStatus)");
        detailsViewSource.Should().Contain("@FlowLabel(item.FlowKey)");
        detailsViewSource.Should().Contain("string.IsNullOrWhiteSpace(channel) ? T.T(\"CommonUnclassified\") : channel");
        detailsViewSource.Should().Contain("@ChannelLabel(\"SMS\")");
        detailsViewSource.Should().Contain("@ChannelLabel(\"WhatsApp\")");

        channelAuditsViewSource.Should().Contain("string FlowLabel(string? flowKey) => flowKey switch");
        channelAuditsViewSource.Should().Contain("\"PhoneVerification\" => T.T(\"CommunicationTemplateInventoryPhoneVerificationFlow\")");
        channelAuditsViewSource.Should().Contain("\"AdminCommunicationTest\" => T.T(\"CommunicationTemplateInventoryAdminTestFlow\")");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : flowKey");
        channelAuditsViewSource.Should().Contain("string DeliveryStatusLabel(string? status) => status switch");
        channelAuditsViewSource.Should().Contain("\"Pending\" => T.T(\"Pending\")");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : status");
        channelAuditsViewSource.Should().Contain("@DeliveryStatusLabel(\"Sent\")");
        channelAuditsViewSource.Should().Contain("@DeliveryStatusLabel(\"Failed\")");
        channelAuditsViewSource.Should().Contain("string ChannelLabel(string? channel) => channel switch");
        channelAuditsViewSource.Should().Contain("\"SMS\" => T.T(\"BusinessCommunicationSmsShort\")");
        channelAuditsViewSource.Should().Contain("\"WhatsApp\" => T.T(\"BusinessCommunicationWhatsAppShort\")");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(channel) ? T.T(\"CommonUnclassified\") : channel");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(pressureState) ? T.T(\"CommonUnclassified\") : pressureState");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(recoveryState) ? T.T(\"CommonUnclassified\") : recoveryState");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(statusMix) ? T.T(\"CommonUnclassified\") : statusMix");
        channelAuditsViewSource.Should().Contain("@ChannelLabel(item.Channel)");
        channelAuditsViewSource.Should().Contain("@ChannelLabel(family.Channel)");
        channelAuditsViewSource.Should().Contain("@ChannelLabel(Model.Channel)");
        channelAuditsViewSource.Should().Contain("asp-route-channel=\"@family.ChannelValue\"");
        channelAuditsViewSource.Should().Contain("channel = family.ChannelValue");
        channelAuditsViewSource.Should().Contain("string.Equals(item.Channel, \"SMS\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(item.Channel, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(Model.Channel, \"SMS\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(Model.Channel, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(ChannelLabel(item.Channel), T.T(\"BusinessCommunicationSmsShort\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(ChannelLabel(item.Channel), T.T(\"BusinessCommunicationWhatsAppShort\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(ChannelLabel(Model.Channel), T.T(\"BusinessCommunicationSmsShort\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(ChannelLabel(Model.Channel), T.T(\"BusinessCommunicationWhatsAppShort\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("@DeliveryStatusLabel(item.Status)");
        channelAuditsViewSource.Should().Contain("<code>@FlowLabel(Model.FlowKey)</code>");
        channelAuditsViewSource.Should().Contain("@FlowLabel(item.FlowKey)");

        emailAuditsViewSource.Should().Contain("string FlowLabel(string? flowKey) => flowKey switch");
        emailAuditsViewSource.Should().Contain("\"BusinessInvitation\" => T.T(\"CommunicationDetailsActiveFlowInvitation\")");
        emailAuditsViewSource.Should().Contain("\"AccountActivation\" => T.T(\"CommunicationDetailsActiveFlowActivation\")");
        emailAuditsViewSource.Should().Contain("\"PasswordReset\" => T.T(\"CommunicationTemplateInventoryPasswordResetFlow\")");
        emailAuditsViewSource.Should().Contain("\"AdminCommunicationTest\" => T.T(\"CommunicationTemplateInventoryAdminTestFlow\")");
        emailAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : flowKey");
        emailAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(retryPolicyState) ? T.T(\"CommonUnclassified\") : retryPolicyState");
        emailAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : status");
        emailAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(statusMix) ? T.T(\"CommonUnclassified\") : statusMix");
        emailAuditsViewSource.Should().Contain("<span> @T.T(\"InFlow\") <code>@FlowLabel(Model.FlowKey)</code></span>");
        emailAuditsViewSource.Should().Contain("<td>@FlowLabel(item.FlowKey)</td>");
    }

    [Fact]
    public void BusinessCommunicationsIndexView_Should_KeepLocalizedActionLabelsWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));

        indexViewSource.Should().Contain("@T.T(\"CommunicationInspectProfileAction\")");
        indexViewSource.Should().Contain("@T.T(\"CommonOpenSetupAction\")");
        indexViewSource.Should().Contain("@T.T(\"CommonEditBusiness\")");
        indexViewSource.Should().NotContain("> Inspect");
        indexViewSource.Should().NotContain("> Open Setup");
        indexViewSource.Should().NotContain("> Edit");
    }

    [Fact]
    public void BusinessCommunicationsIndexView_Should_KeepLocalizedRecentEmailActivityContractsWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));

        indexViewSource.Should().Contain("string FlowLabel(string? flowKey) => flowKey switch");
        indexViewSource.Should().Contain("\"BusinessInvitation\" => T.T(\"CommunicationDetailsActiveFlowInvitation\")");
        indexViewSource.Should().Contain("\"AccountActivation\" => T.T(\"CommunicationDetailsActiveFlowActivation\")");
        indexViewSource.Should().Contain("\"PasswordReset\" => T.T(\"CommunicationTemplateInventoryPasswordResetFlow\")");
        indexViewSource.Should().Contain("\"AdminCommunicationTest\" => T.T(\"CommunicationTemplateInventoryAdminTestFlow\")");
        indexViewSource.Should().Contain("\"PhoneVerification\" => T.T(\"CommunicationTemplateInventoryPhoneVerificationFlow\")");
        indexViewSource.Should().Contain("string DeliveryStatusLabel(string? status) => status switch");
        indexViewSource.Should().Contain("\"Pending\" => T.T(\"Pending\")");
        indexViewSource.Should().Contain("@DeliveryStatusLabel(\"Sent\")");
        indexViewSource.Should().Contain("@DeliveryStatusLabel(\"Failed\")");
        indexViewSource.Should().Contain("string SeverityLabel(string? severity) => severity switch");
        indexViewSource.Should().Contain("\"High\" => T.T(\"High\")");
        indexViewSource.Should().Contain("\"Medium\" => T.T(\"Medium\")");
        indexViewSource.Should().Contain("\"Watch\" => T.T(\"Watch\")");
        indexViewSource.Should().Contain("\"Slow\" => T.T(\"Slow\")");
        indexViewSource.Should().Contain("\"Normal\" => T.T(\"Normal\")");
        indexViewSource.Should().Contain("<td>@FlowLabel(item.FlowKey)</td>");
        indexViewSource.Should().NotContain("@(string.IsNullOrWhiteSpace(item.FlowKey) ? T.T(\"Unclassified\") : item.FlowKey)");
        indexViewSource.Should().Contain("@DeliveryStatusLabel(item.Status)");
        indexViewSource.Should().Contain("@SeverityLabel(item.Severity)");
        indexViewSource.Should().Contain("@T.T(\"CommunicationInspectProfileAction\")");
        indexViewSource.Should().Contain("@T.T(\"CommonOpenSetupAction\")");
        indexViewSource.Should().Contain("@T.T(\"CommonEditBusiness\")");
    }

    [Fact]
    public void BusinessCommunicationsDetailsView_Should_KeepLocalizedOperationalMatrixContractsWired()
    {
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));

        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationChannelTruthSnapshotTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationChannelTruthSnapshotNote\")");
        detailsViewSource.Should().Contain("@ChannelLabel(item.Channel)");
        detailsViewSource.Should().Contain("@item.CurrentState");
        detailsViewSource.Should().Contain("@item.LiveFlows");
        detailsViewSource.Should().Contain("@item.SafeOperatorActions");
        detailsViewSource.Should().Contain("@item.RiskBoundary");

        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationTemplateInventorySnapshotTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationTemplateInventorySnapshotNote\")");
        detailsViewSource.Should().Contain("@item.FlowName");
        detailsViewSource.Should().Contain("@item.TemplateSurface");
        detailsViewSource.Should().Contain("@item.CurrentSubjectTemplate");
        detailsViewSource.Should().Contain("@item.CurrentBodyTemplate");
        detailsViewSource.Should().Contain("@item.OperatorControl");
        detailsViewSource.Should().Contain("@item.OperatorActionLabel");
        detailsViewSource.Should().Contain("@ChannelLabel(family.Channel)");
        detailsViewSource.Should().Contain("@T.T(\"OpenPolicyAction\")");

        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationResendPolicySnapshotTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationResendPolicySnapshotNote\")");
        detailsViewSource.Should().Contain("@item.CurrentSafeAction");
        detailsViewSource.Should().Contain("@item.GenericRetryStatus");
        detailsViewSource.Should().Contain("@item.OperatorActionLabel");
        detailsViewSource.Should().Contain("@T.T(\"FailedAuditsAction\")");
        detailsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"PhoneVerification\", StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"AdminCommunicationTest\", StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().Contain("T.T(\"BusinessCommunicationSmsShort\")");
        detailsViewSource.Should().Contain("T.T(\"BusinessCommunicationWhatsAppShort\")");
        detailsViewSource.Should().Contain("string ChannelLabel(string? channel) => channel switch");
        detailsViewSource.Should().Contain("\"SMS\" => T.T(\"BusinessCommunicationSmsShort\")");
        detailsViewSource.Should().Contain("\"WhatsApp\" => T.T(\"BusinessCommunicationWhatsAppShort\")");
        detailsViewSource.Should().Contain("@ChannelLabel(item.Channel)");
        detailsViewSource.Should().Contain("@ChannelLabel(\"SMS\")");
        detailsViewSource.Should().Contain("@ChannelLabel(\"WhatsApp\")");
        detailsViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"BusinessCommunicationSmsShort\")</a>");
        detailsViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"BusinessCommunicationWhatsAppShort\")</a>");
        detailsViewSource.Should().Contain("asp-route-channel=\"@family.ChannelValue\"");
        detailsViewSource.Should().Contain("channel = family.ChannelValue");
    }

    [Fact]
    public void BusinessCommunicationsDetails_Should_KeepLocalizedBusinessProfileGuidanceContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));

        controllerSource.Should().Contain("private List<string> BuildActiveFlowNames(BusinessCommunicationProfileDto profile)");
        controllerSource.Should().Contain("flows.Add(T(\"CommunicationDetailsActiveFlowInvitation\"));");
        controllerSource.Should().Contain("flows.Add(T(\"CommunicationDetailsActiveFlowActivation\"));");
        controllerSource.Should().Contain("flows.Add(T(\"CommunicationDetailsActiveFlowPasswordReset\"));");
        controllerSource.Should().Contain("flows.Add(T(\"CommunicationDetailsActiveFlowAdminAlerts\"));");

        controllerSource.Should().Contain("private List<string> BuildReadinessIssues(");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueMissingSupportEmail\"));");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueMissingSenderIdentity\"));");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueMissingSmtp\"));");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueMissingAdminRouting\"));");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssuePendingApproval\"));");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueInactive\"));");

        controllerSource.Should().Contain("private List<string> BuildRecommendedActions(");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionCompleteBusinessDefaults\"));");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionOpenSmtp\"));");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionConfigureAdminRouting\"));");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionReviewMembers\"));");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionReviewInvitations\"));");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionReviewLockedMembers\"));");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionCompleteBeforeApproval\"));");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionNoImmediateAction\"));");

        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationReadinessIssuesTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationNoReadinessIssues\")");
        detailsViewSource.Should().Contain("@foreach (var issue in Model.ReadinessIssues)");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationCurrentFlowsTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationNoCurrentFlows\")");
        detailsViewSource.Should().Contain("@foreach (var flow in Model.ActiveFlowNames)");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationRecommendedNextActionsTitle\")");
        detailsViewSource.Should().Contain("@foreach (var action in Model.RecommendedActions)");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationViewInvitationAuditsAction\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationOpenMemberSupportAction\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationOpenInvitationsAction\")");
    }

    [Fact]
    public void BusinessCommunicationsViews_Should_KeepCommonUnclassifiedFallbackContractsAligned()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));
        var emailAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "EmailAudits.cshtml"));

        indexViewSource.Should().Contain("string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : flowKey");
        indexViewSource.Should().Contain("string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : status");
        indexViewSource.Should().Contain("string.IsNullOrWhiteSpace(severity) ? T.T(\"CommonUnclassified\") : severity");
        indexViewSource.Should().Contain("string.IsNullOrWhiteSpace(channel) ? T.T(\"CommonUnclassified\") : channel");
        indexViewSource.Should().NotContain("T.T(\"Unclassified\")");

        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : flowKey");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(pressureState) ? T.T(\"CommonUnclassified\") : pressureState");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(recoveryState) ? T.T(\"CommonUnclassified\") : recoveryState");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(statusMix) ? T.T(\"CommonUnclassified\") : statusMix");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : status");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(channel) ? T.T(\"CommonUnclassified\") : channel");
        channelAuditsViewSource.Should().NotContain("T.T(\"Unclassified\")");

        emailAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : flowKey");
        emailAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(retryPolicyState) ? T.T(\"CommonUnclassified\") : retryPolicyState");
        emailAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : status");
        emailAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(statusMix) ? T.T(\"CommonUnclassified\") : statusMix");
        emailAuditsViewSource.Should().NotContain("T.T(\"Unclassified\")");

        detailsViewSource.Should().Contain("string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : flowKey");
        detailsViewSource.Should().Contain("string.IsNullOrWhiteSpace(operationalStatus) ? T.T(\"CommonUnclassified\") : operationalStatus");
        detailsViewSource.Should().Contain("string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : status");
        detailsViewSource.Should().Contain("string.IsNullOrWhiteSpace(channel) ? T.T(\"CommonUnclassified\") : channel");
        detailsViewSource.Should().NotContain("T.T(\"Unclassified\")");
    }

    [Fact]
    public void EmailAuditsView_Should_KeepLocalizedPlaybookIntroContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var emailAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "EmailAudits.cshtml"));

        controllerSource.Should().Contain("private string BuildEmailAuditChainStatusMix(string? statusMix)");
        controllerSource.Should().Contain("\"Mixed success/failure\" => T(\"CommunicationChainStatusMixed\")");
        controllerSource.Should().Contain("private string? BuildEmailAuditRetryBlockedReason(EmailDispatchAuditListItemDto item)");
        controllerSource.Should().Contain("return T(\"CommunicationEmailRetryBlockedUnsupported\")");
        controllerSource.Should().Contain("return T(\"CommunicationEmailRetryBlockedClosed\")");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationEmailRetryBlockedRateLimited\"), item.RecentAttemptCount24h)");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationEmailRetryBlockedCooldownUntil\"), item.RetryAvailableAtUtc.Value)");

        emailAuditsViewSource.Should().Contain("@T.T(\"CommunicationEmailAuditsPlaybookIntro\")");
        emailAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditFirstAttemptLabel\")");
        emailAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditLatestAttemptLabel\")");
        emailAuditsViewSource.Should().Contain("string ChainStatusMixLabel(string? statusMix) => statusMix switch");
        emailAuditsViewSource.Should().Contain("\"Mixed success/failure\" => T.T(\"CommunicationChainStatusMixed\")");
        emailAuditsViewSource.Should().Contain("string DeliveryStatusLabel(string? status) => status switch");
        emailAuditsViewSource.Should().Contain("\"Pending\" => T.T(\"Pending\")");
        emailAuditsViewSource.Should().Contain("@DeliveryStatusLabel(\"Sent\")");
        emailAuditsViewSource.Should().Contain("@DeliveryStatusLabel(\"Failed\")");
        emailAuditsViewSource.Should().Contain("@DeliveryStatusLabel(\"Pending\")");
        emailAuditsViewSource.Should().Contain("@DeliveryStatusLabel(history.Status)");
        emailAuditsViewSource.Should().Contain("@ChainStatusMixLabel(Model.ChainSummary.StatusMix)");
        emailAuditsViewSource.Should().Contain("@string.Format(T.T(\"EmailAuditsChainProfile\"), ChainStatusMixLabel(item.ChainStatusMix))");
        emailAuditsViewSource.Should().Contain("string RetryPolicyStateLabel(string? retryPolicyState) => retryPolicyState switch");
        emailAuditsViewSource.Should().Contain("\"Unsupported\" => T.T(\"CommunicationEmailRetryPolicyUnsupported\")");
        emailAuditsViewSource.Should().Contain("\"Closed\" => T.T(\"CommunicationEmailRetryPolicyClosed\")");
        emailAuditsViewSource.Should().Contain("@RetryPolicyStateLabel(item.RetryPolicyState)");
        emailAuditsViewSource.Should().Contain("@DeliveryStatusLabel(item.Status)");
        emailAuditsViewSource.Should().Contain("@item.RetryBlockedReason");
    }

    [Fact]
    public void EmailAuditsView_Should_KeepLocalizedChainStatusMixContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var emailAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "EmailAudits.cshtml"));

        controllerSource.Should().Contain("private string BuildEmailAuditChainStatusMix(string? statusMix)");
        controllerSource.Should().Contain("\"Mixed success/failure\" => T(\"CommunicationChainStatusMixed\")");
        controllerSource.Should().Contain("\"Open failure chain\" => T(\"CommunicationChainStatusOpenFailure\")");
        controllerSource.Should().Contain("\"Failure-only chain\" => T(\"CommunicationChainStatusFailureOnly\")");
        controllerSource.Should().Contain("\"Pending-only chain\" => T(\"CommunicationChainStatusPendingOnly\")");
        controllerSource.Should().Contain("\"Success-only chain\" => T(\"CommunicationChainStatusSuccessOnly\")");
        controllerSource.Should().Contain("\"Single attempt\" => T(\"CommunicationChainStatusSingleAttempt\")");

        emailAuditsViewSource.Should().Contain("string ChainStatusMixLabel(string? statusMix) => statusMix switch");
        emailAuditsViewSource.Should().Contain("\"Mixed success/failure\" => T.T(\"CommunicationChainStatusMixed\")");
        emailAuditsViewSource.Should().Contain("\"Open failure chain\" => T.T(\"CommunicationChainStatusOpenFailure\")");
        emailAuditsViewSource.Should().Contain("\"Failure-only chain\" => T.T(\"CommunicationChainStatusFailureOnly\")");
        emailAuditsViewSource.Should().Contain("\"Pending-only chain\" => T.T(\"CommunicationChainStatusPendingOnly\")");
        emailAuditsViewSource.Should().Contain("\"Success-only chain\" => T.T(\"CommunicationChainStatusSuccessOnly\")");
        emailAuditsViewSource.Should().Contain("\"Single attempt\" => T.T(\"CommunicationChainStatusSingleAttempt\")");
        emailAuditsViewSource.Should().Contain("@ChainStatusMixLabel(Model.ChainSummary.StatusMix)");
        emailAuditsViewSource.Should().Contain("@string.Format(T.T(\"EmailAuditsChainProfile\"), ChainStatusMixLabel(item.ChainStatusMix))");
        emailAuditsViewSource.Should().NotContain("@Model.ChainSummary.StatusMix");
        emailAuditsViewSource.Should().NotContain("@string.Format(T.T(\"EmailAuditsChainProfile\"), item.ChainStatusMix)");
        emailAuditsViewSource.Should().Contain("@T.T(\"EmailAuditLastSuccessLabel\")");
        emailAuditsViewSource.Should().NotContain("@T.T(\"EmailAuditLastSuccessLabel\").ToLowerInvariant()");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessSupportAndMembershipMutationsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        source.Should().Contain("[PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]");
        source.Should().Contain("[PermissionAuthorize(PermissionKeys.FullAdminAccess)]");
        source.Should().Contain("[HttpPost, ValidateAntiForgeryToken]");
        source.Should().Contain("public async Task<IActionResult> CreateInvitation(BusinessInvitationCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> ResendInvitation(");
        source.Should().Contain("public async Task<IActionResult> RevokeInvitation(");
        source.Should().Contain("public async Task<IActionResult> CreateLocation(BusinessLocationEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditLocation(BusinessLocationEditVm vm");
        source.Should().Contain("public async Task<IActionResult> DeleteLocation(");
        source.Should().Contain("public async Task<IActionResult> CreateMember(BusinessMemberEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditMember(BusinessMemberEditVm vm");
        source.Should().Contain("public async Task<IActionResult> DeleteMember(");
        source.Should().Contain("public async Task<IActionResult> SendMemberActivationEmail(");
        source.Should().Contain("public async Task<IActionResult> ConfirmMemberEmail(");
        source.Should().Contain("public async Task<IActionResult> SendMemberPasswordReset(");
        source.Should().Contain("public async Task<IActionResult> LockMemberUser(");
        source.Should().Contain("public async Task<IActionResult> UnlockMemberUser(");
    }

    [Fact]
    public void BusinessesController_Should_KeepSupportQueueAndMerchantReadinessEndpointsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        source.Should().Contain("public sealed class BusinessesController : AdminBaseController");
        source.Should().Contain("[PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]");
        source.Should().Contain("public async Task<IActionResult> SupportQueue(");
        source.Should().Contain("public async Task<IActionResult> MerchantReadiness(");
        source.Should().Contain("public async Task<IActionResult> SupportQueueSummaryFragment(");
        source.Should().Contain("public async Task<IActionResult> SupportQueueAttentionFragment(");
        source.Should().Contain("public async Task<IActionResult> SupportQueueFailedEmailsFragment(");
    }

    [Fact]
    public void BusinessesController_Should_KeepSupportQueueWorkspaceCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("var summary = await _getBusinessSupportSummary.HandleAsync(null, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (attentionBusinesses, _) = await _getBusinessesPage.HandleAsync(1, 10, null, null, true, readinessFilter: null, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (failedEmails, _, _) = await _getEmailDispatchAuditsPage");
        controllerSource.Should().Contain("page: 1,");
        controllerSource.Should().Contain("pageSize: 8,");
        controllerSource.Should().Contain("status: \"Failed\",");
        controllerSource.Should().Contain("var vm = new BusinessSupportQueueVm");
        controllerSource.Should().Contain("Summary = new BusinessSupportSummaryVm");
        controllerSource.Should().Contain("AttentionBusinesses = attentionBusinesses.Select(x => new BusinessListItemVm");
        controllerSource.Should().Contain("FailedEmails = failedEmails.Select(x => new BusinessSupportFailedEmailVm");
        controllerSource.Should().Contain("Playbooks = BuildMerchantReadinessPlaybooks()");
        controllerSource.Should().Contain("return RenderSupportQueueWorkspace(vm);");
        controllerSource.Should().Contain("return PartialView(\"SupportQueue\", vm);");
        controllerSource.Should().Contain("return View(\"SupportQueue\", vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepSupportQueueFragmentQueriesWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SupportQueueSummaryFragment(CancellationToken ct = default)");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_SupportQueueSummary.cshtml\", MapSupportSummaryVm(summary));");
        controllerSource.Should().Contain("public async Task<IActionResult> SupportQueueAttentionFragment(CancellationToken ct = default)");
        controllerSource.Should().Contain("var (attentionBusinesses, _) = await _getBusinessesPage.HandleAsync(1, 10, null, null, true, readinessFilter: null, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_SupportQueueAttentionBusinesses.cshtml\", vm);");
        controllerSource.Should().Contain("public async Task<IActionResult> SupportQueueFailedEmailsFragment(CancellationToken ct = default)");
        controllerSource.Should().Contain("pageSize: 8,");
        controllerSource.Should().Contain("status: \"Failed\",");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_SupportQueueFailedEmails.cshtml\", vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepSupportQueueAttentionFragmentMappingContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SupportQueueAttentionFragment(CancellationToken ct = default)");
        controllerSource.Should().Contain("var (attentionBusinesses, _) = await _getBusinessesPage.HandleAsync(1, 10, null, null, true, readinessFilter: null, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var vm = attentionBusinesses.Select(x => new BusinessListItemVm");
        controllerSource.Should().Contain("Id = x.Id,");
        controllerSource.Should().Contain("Name = x.Name,");
        controllerSource.Should().Contain("LegalName = x.LegalName,");
        controllerSource.Should().Contain("Category = x.Category,");
        controllerSource.Should().Contain("IsActive = x.IsActive,");
        controllerSource.Should().Contain("OperationalStatus = x.OperationalStatus,");
        controllerSource.Should().Contain("MemberCount = x.MemberCount,");
        controllerSource.Should().Contain("ActiveOwnerCount = x.ActiveOwnerCount,");
        controllerSource.Should().Contain("LocationCount = x.LocationCount,");
        controllerSource.Should().Contain("PrimaryLocationCount = x.PrimaryLocationCount,");
        controllerSource.Should().Contain("InvitationCount = x.InvitationCount,");
        controllerSource.Should().Contain("HasContactEmailConfigured = x.HasContactEmailConfigured,");
        controllerSource.Should().Contain("HasLegalNameConfigured = x.HasLegalNameConfigured,");
        controllerSource.Should().Contain("CreatedAtUtc = x.CreatedAtUtc,");
        controllerSource.Should().Contain("ModifiedAtUtc = x.ModifiedAtUtc,");
        controllerSource.Should().Contain("RowVersion = x.RowVersion");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_SupportQueueAttentionBusinesses.cshtml\", vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepSupportQueueFailedEmailsFragmentMappingContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SupportQueueFailedEmailsFragment(CancellationToken ct = default)");
        controllerSource.Should().Contain("_getEmailDispatchAuditsPage");
        controllerSource.Should().Contain("page: 1,");
        controllerSource.Should().Contain("pageSize: 8,");
        controllerSource.Should().Contain("status: \"Failed\",");
        controllerSource.Should().Contain("stalePendingOnly: false,");
        controllerSource.Should().Contain("businessLinkedFailuresOnly: false,");
        controllerSource.Should().Contain("repeatedFailuresOnly: false,");
        controllerSource.Should().Contain("priorSuccessOnly: false,");
        controllerSource.Should().Contain("retryReadyOnly: false,");
        controllerSource.Should().Contain("retryBlockedOnly: false,");
        controllerSource.Should().Contain("var vm = failedEmails.Select(x => new BusinessSupportFailedEmailVm");
        controllerSource.Should().Contain("Id = x.Id,");
        controllerSource.Should().Contain("FlowKey = x.FlowKey ?? string.Empty,");
        controllerSource.Should().Contain("BusinessId = x.BusinessId,");
        controllerSource.Should().Contain("BusinessName = x.BusinessName,");
        controllerSource.Should().Contain("RecipientEmail = x.RecipientEmail,");
        controllerSource.Should().Contain("Subject = x.Subject,");
        controllerSource.Should().Contain("AttemptedAtUtc = x.AttemptedAtUtc,");
        controllerSource.Should().Contain("FailureMessage = x.FailureMessage,");
        controllerSource.Should().Contain("RecommendedAction = BuildSupportAuditRecommendedAction(x)");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_SupportQueueFailedEmails.cshtml\", vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessesIndexWorkspaceCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Index(");
        controllerSource.Should().Contain("var summary = await _getBusinessSupportSummary.HandleAsync(null, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (items, total) = await _getBusinessesPage.HandleAsync(");
        controllerSource.Should().Contain("var vm = new BusinessesListVm");
        controllerSource.Should().Contain("Page = page,");
        controllerSource.Should().Contain("PageSize = pageSize,");
        controllerSource.Should().Contain("Total = total,");
        controllerSource.Should().Contain("Query = query ?? string.Empty,");
        controllerSource.Should().Contain("OperationalStatus = operationalStatus,");
        controllerSource.Should().Contain("AttentionOnly = attentionOnly,");
        controllerSource.Should().Contain("ReadinessFilter = readinessFilter,");
        controllerSource.Should().Contain("Summary = MapSupportSummaryVm(summary),");
        controllerSource.Should().Contain("Playbooks = BuildMerchantReadinessPlaybooks(),");
        controllerSource.Should().Contain("PageSizeItems = BuildPageSizeItems(pageSize),");
        controllerSource.Should().Contain("OperationalStatusItems = BuildBusinessStatusItems(operationalStatus),");
        controllerSource.Should().Contain("Items = items.Select(x => new BusinessListItemVm");
        controllerSource.Should().Contain("return RenderBusinessesWorkspace(vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessesIndexItemMappingContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("Items = items.Select(x => new BusinessListItemVm");
        controllerSource.Should().Contain("Id = x.Id,");
        controllerSource.Should().Contain("Name = x.Name,");
        controllerSource.Should().Contain("LegalName = x.LegalName,");
        controllerSource.Should().Contain("Category = x.Category,");
        controllerSource.Should().Contain("IsActive = x.IsActive,");
        controllerSource.Should().Contain("OperationalStatus = x.OperationalStatus,");
        controllerSource.Should().Contain("MemberCount = x.MemberCount,");
        controllerSource.Should().Contain("ActiveOwnerCount = x.ActiveOwnerCount,");
        controllerSource.Should().Contain("LocationCount = x.LocationCount,");
        controllerSource.Should().Contain("PrimaryLocationCount = x.PrimaryLocationCount,");
        controllerSource.Should().Contain("InvitationCount = x.InvitationCount,");
        controllerSource.Should().Contain("HasContactEmailConfigured = x.HasContactEmailConfigured,");
        controllerSource.Should().Contain("HasLegalNameConfigured = x.HasLegalNameConfigured,");
        controllerSource.Should().Contain("CreatedAtUtc = x.CreatedAtUtc,");
        controllerSource.Should().Contain("ModifiedAtUtc = x.ModifiedAtUtc,");
        controllerSource.Should().Contain("RowVersion = x.RowVersion");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessesIndexRenderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderBusinessesWorkspace(BusinessesListVm vm)");
        controllerSource.Should().Contain("if (IsHtmxRequest())");
        controllerSource.Should().Contain("return PartialView(\"Index\", vm);");
        controllerSource.Should().Contain("return View(\"Index\", vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepMembersWorkspaceCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Members(");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        controllerSource.Should().Contain("var (items, total) = await _getBusinessMembersPage.HandleAsync(businessId, page, pageSize, query, filter, ct);");
        controllerSource.Should().Contain("var vm = new BusinessMembersListVm");
        controllerSource.Should().Contain("Business = business,");
        controllerSource.Should().Contain("Page = page,");
        controllerSource.Should().Contain("PageSize = pageSize,");
        controllerSource.Should().Contain("Total = total,");
        controllerSource.Should().Contain("Query = query ?? string.Empty,");
        controllerSource.Should().Contain("Filter = filter,");
        controllerSource.Should().Contain("FilterItems = BuildBusinessMemberFilterItems(filter),");
        controllerSource.Should().Contain("Summary = await BuildBusinessMemberOpsSummaryAsync(businessId, ct).ConfigureAwait(false),");
        controllerSource.Should().Contain("Playbooks = BuildBusinessMemberPlaybooks(businessId),");
        controllerSource.Should().Contain("Items = items.Select(x => new BusinessMemberListItemVm");
        controllerSource.Should().Contain("return RenderMembersWorkspace(vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepMembersItemMappingContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("Items = items.Select(x => new BusinessMemberListItemVm");
        controllerSource.Should().Contain("Id = x.Id,");
        controllerSource.Should().Contain("BusinessId = x.BusinessId,");
        controllerSource.Should().Contain("UserId = x.UserId,");
        controllerSource.Should().Contain("UserDisplayName = x.UserDisplayName,");
        controllerSource.Should().Contain("UserEmail = x.UserEmail,");
        controllerSource.Should().Contain("EmailConfirmed = x.EmailConfirmed,");
        controllerSource.Should().Contain("LockoutEndUtc = x.LockoutEndUtc,");
        controllerSource.Should().Contain("Role = x.Role,");
        controllerSource.Should().Contain("IsActive = x.IsActive,");
        controllerSource.Should().Contain("ModifiedAtUtc = x.ModifiedAtUtc,");
        controllerSource.Should().Contain("RowVersion = x.RowVersion");
    }

    [Fact]
    public void BusinessesController_Should_KeepMembersRenderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderMembersWorkspace(BusinessMembersListVm vm)");
        controllerSource.Should().Contain("if (IsHtmxRequest())");
        controllerSource.Should().Contain("return PartialView(\"Members\", vm);");
        controllerSource.Should().Contain("return View(\"Members\", vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepInvitationsWorkspaceCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Invitations(");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        controllerSource.Should().Contain("var (items, total) = await _getBusinessInvitationsPage.HandleAsync(businessId, page, pageSize, query, filter, ct);");
        controllerSource.Should().Contain("var vm = new BusinessInvitationsListVm");
        controllerSource.Should().Contain("Business = business,");
        controllerSource.Should().Contain("Page = page,");
        controllerSource.Should().Contain("PageSize = pageSize,");
        controllerSource.Should().Contain("Total = total,");
        controllerSource.Should().Contain("Query = query ?? string.Empty,");
        controllerSource.Should().Contain("Filter = filter,");
        controllerSource.Should().Contain("FilterItems = BuildBusinessInvitationFilterItems(filter),");
        controllerSource.Should().Contain("Summary = await BuildBusinessInvitationOpsSummaryAsync(businessId, ct).ConfigureAwait(false),");
        controllerSource.Should().Contain("Playbooks = BuildBusinessInvitationPlaybooks(businessId),");
        controllerSource.Should().Contain("Items = items.Select(x => new BusinessInvitationListItemVm");
        controllerSource.Should().Contain("return RenderInvitationsWorkspace(vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepInvitationsItemMappingContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("Items = items.Select(x => new BusinessInvitationListItemVm");
        controllerSource.Should().Contain("Id = x.Id,");
        controllerSource.Should().Contain("BusinessId = x.BusinessId,");
        controllerSource.Should().Contain("Email = x.Email,");
        controllerSource.Should().Contain("Role = x.Role,");
        controllerSource.Should().Contain("Status = x.Status,");
        controllerSource.Should().Contain("InvitedByDisplayName = x.InvitedByDisplayName,");
        controllerSource.Should().Contain("ExpiresAtUtc = x.ExpiresAtUtc,");
        controllerSource.Should().Contain("AcceptedAtUtc = x.AcceptedAtUtc,");
        controllerSource.Should().Contain("RevokedAtUtc = x.RevokedAtUtc,");
        controllerSource.Should().Contain("CreatedAtUtc = x.CreatedAtUtc,");
        controllerSource.Should().Contain("Note = x.Note");
    }

    [Fact]
    public void BusinessesController_Should_KeepInvitationsRenderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderInvitationsWorkspace(BusinessInvitationsListVm vm)");
        controllerSource.Should().Contain("if (IsHtmxRequest())");
        controllerSource.Should().Contain("return PartialView(\"Invitations\", vm);");
        controllerSource.Should().Contain("return View(\"Invitations\", vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepLocationEditorCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> EditLocation(Guid id, CancellationToken ct = default)");
        controllerSource.Should().Contain("var dto = await _getBusinessLocationForEdit.HandleAsync(id, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessLocationNotFound\");");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(dto.BusinessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("var vm = new BusinessLocationEditVm");
        controllerSource.Should().Contain("Id = dto.Id,");
        controllerSource.Should().Contain("BusinessId = dto.BusinessId,");
        controllerSource.Should().Contain("RowVersion = dto.RowVersion,");
        controllerSource.Should().Contain("Name = dto.Name,");
        controllerSource.Should().Contain("AddressLine1 = dto.AddressLine1,");
        controllerSource.Should().Contain("AddressLine2 = dto.AddressLine2,");
        controllerSource.Should().Contain("City = dto.City,");
        controllerSource.Should().Contain("Region = dto.Region,");
        controllerSource.Should().Contain("CountryCode = dto.CountryCode,");
        controllerSource.Should().Contain("PostalCode = dto.PostalCode,");
        controllerSource.Should().Contain("Latitude = dto.Coordinate?.Latitude,");
        controllerSource.Should().Contain("Longitude = dto.Coordinate?.Longitude,");
        controllerSource.Should().Contain("AltitudeMeters = dto.Coordinate?.AltitudeMeters,");
        controllerSource.Should().Contain("IsPrimary = dto.IsPrimary,");
        controllerSource.Should().Contain("OpeningHoursJson = dto.OpeningHoursJson,");
        controllerSource.Should().Contain("InternalNote = dto.InternalNote,");
        controllerSource.Should().Contain("Business = business");
        controllerSource.Should().Contain("return RenderLocationEditor(vm, isCreate: false);");
    }

    [Fact]
    public void BusinessesController_Should_KeepCreateLocationEditorCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> CreateLocation(Guid businessId, CancellationToken ct = default)");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        controllerSource.Should().Contain("return RenderLocationEditor(new BusinessLocationEditVm");
        controllerSource.Should().Contain("BusinessId = businessId,");
        controllerSource.Should().Contain("CountryCode = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCountryDefault,");
        controllerSource.Should().Contain("Business = business");
        controllerSource.Should().Contain("}, isCreate: true);");
    }

    [Fact]
    public void BusinessesController_Should_KeepLocationEditorRenderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderLocationEditor(BusinessLocationEditVm vm, bool isCreate)");
        controllerSource.Should().Contain("ViewData[\"IsCreate\"] = isCreate;");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_BusinessLocationEditorShell.cshtml\", vm);");
        controllerSource.Should().Contain("return isCreate ? View(\"CreateLocation\", vm) : View(\"EditLocation\", vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepMemberEditorCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> EditMember(Guid id, CancellationToken ct = default)");
        controllerSource.Should().Contain("var dto = await _getBusinessMemberForEdit.HandleAsync(id, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessMemberNotFound\");");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(dto.BusinessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("var vm = new BusinessMemberEditVm");
        controllerSource.Should().Contain("Id = dto.Id,");
        controllerSource.Should().Contain("BusinessId = dto.BusinessId,");
        controllerSource.Should().Contain("UserId = dto.UserId,");
        controllerSource.Should().Contain("RowVersion = dto.RowVersion,");
        controllerSource.Should().Contain("UserDisplayName = dto.UserDisplayName,");
        controllerSource.Should().Contain("UserEmail = dto.UserEmail,");
        controllerSource.Should().Contain("EmailConfirmed = dto.EmailConfirmed,");
        controllerSource.Should().Contain("LockoutEndUtc = dto.LockoutEndUtc,");
        controllerSource.Should().Contain("Role = dto.Role,");
        controllerSource.Should().Contain("IsActive = dto.IsActive,");
        controllerSource.Should().Contain("IsLastActiveOwner = dto.IsLastActiveOwner,");
        controllerSource.Should().Contain("OverrideReason = null,");
        controllerSource.Should().Contain("Business = business");
        controllerSource.Should().Contain("await PopulateMemberFormOptionsAsync(vm, includeUserSelection: false, ct);");
        controllerSource.Should().Contain("return RenderMemberEditor(vm, isCreate: false);");
    }

    [Fact]
    public void BusinessesController_Should_KeepCreateMemberEditorCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> CreateMember(Guid businessId, CancellationToken ct = default)");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        controllerSource.Should().Contain("var vm = new BusinessMemberEditVm");
        controllerSource.Should().Contain("BusinessId = businessId,");
        controllerSource.Should().Contain("Role = BusinessMemberRole.Owner,");
        controllerSource.Should().Contain("IsActive = true,");
        controllerSource.Should().Contain("Business = business");
        controllerSource.Should().Contain("await PopulateMemberFormOptionsAsync(vm, includeUserSelection: true, ct);");
        controllerSource.Should().Contain("return RenderMemberEditor(vm, isCreate: true);");
    }

    [Fact]
    public void BusinessesController_Should_KeepMemberEditorRenderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderMemberEditor(BusinessMemberEditVm vm, bool isCreate)");
        controllerSource.Should().Contain("ViewData[\"IsCreate\"] = isCreate;");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_BusinessMemberEditorShell.cshtml\", vm);");
        controllerSource.Should().Contain("return isCreate ? View(\"CreateMember\", vm) : View(\"EditMember\", vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepInvitationEditorCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> CreateInvitation(Guid businessId, CancellationToken ct = default)");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("var vm = new BusinessInvitationCreateVm");
        controllerSource.Should().Contain("BusinessId = businessId,");
        controllerSource.Should().Contain("Business = business,");
        controllerSource.Should().Contain("Role = BusinessMemberRole.Owner,");
        controllerSource.Should().Contain("ExpiresInDays = 7");
        controllerSource.Should().Contain("PopulateInvitationFormOptions(vm);");
        controllerSource.Should().Contain("return RenderInvitationEditor(vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepCreateLocationSubmitContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> CreateLocation(BusinessLocationEditVm vm, CancellationToken ct = default)");
        controllerSource.Should().Contain("if (!ModelState.IsValid)");
        controllerSource.Should().Contain("await PopulateBusinessContextAsync(vm, ct);");
        controllerSource.Should().Contain("return RenderLocationEditor(vm, isCreate: true);");
        controllerSource.Should().Contain("await _createBusinessLocation.HandleAsync(new BusinessLocationCreateDto");
        controllerSource.Should().Contain("BusinessId = vm.BusinessId,");
        controllerSource.Should().Contain("Coordinate = BuildCoordinate(vm),");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessLocationCreated\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Locations), new { businessId = vm.BusinessId });");
    }

    [Fact]
    public void BusinessesController_Should_KeepCreateMemberSubmitContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> CreateMember(BusinessMemberEditVm vm, CancellationToken ct = default)");
        controllerSource.Should().Contain("await PopulateMemberFormOptionsAsync(vm, includeUserSelection: true, ct);");
        controllerSource.Should().Contain("return RenderMemberEditor(vm, isCreate: true);");
        controllerSource.Should().Contain("await _createBusinessMember.HandleAsync(new BusinessMemberCreateDto");
        controllerSource.Should().Contain("BusinessId = vm.BusinessId,");
        controllerSource.Should().Contain("UserId = vm.UserId,");
        controllerSource.Should().Contain("Role = vm.Role,");
        controllerSource.Should().Contain("IsActive = vm.IsActive");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessMemberAssigned\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Members), new { businessId = vm.BusinessId });");
    }

    [Fact]
    public void BusinessesController_Should_KeepCreateInvitationSubmitContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> CreateInvitation(BusinessInvitationCreateVm vm, CancellationToken ct = default)");
        controllerSource.Should().Contain("PopulateInvitationFormOptions(vm);");
        controllerSource.Should().Contain("return RenderInvitationEditor(vm);");
        controllerSource.Should().Contain("await _createBusinessInvitation.HandleAsync(new BusinessInvitationCreateDto");
        controllerSource.Should().Contain("BusinessId = vm.BusinessId,");
        controllerSource.Should().Contain("Email = vm.Email,");
        controllerSource.Should().Contain("Role = vm.Role,");
        controllerSource.Should().Contain("ExpiresInDays = vm.ExpiresInDays,");
        controllerSource.Should().Contain("Note = vm.Note");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessInvitationSent\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Invitations), new { businessId = vm.BusinessId });");
    }

    [Fact]
    public void BusinessesController_Should_KeepInvitationEditorRenderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderInvitationEditor(BusinessInvitationCreateVm vm)");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_BusinessInvitationEditorShell.cshtml\", vm);");
        controllerSource.Should().Contain("return View(\"CreateInvitation\", vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessEditorCreateCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Create(CancellationToken ct = default)");
        controllerSource.Should().Contain("var vm = new BusinessEditVm");
        controllerSource.Should().Contain("IsActive = false");
        controllerSource.Should().Contain("await PopulateBusinessFormOptionsAsync(vm, ct);");
        controllerSource.Should().Contain("return RenderBusinessEditor(vm, isCreate: true);");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessEditorCreateSubmitContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Create(BusinessEditVm vm, CancellationToken ct = default)");
        controllerSource.Should().Contain("if (!ModelState.IsValid)");
        controllerSource.Should().Contain("await PopulateBusinessFormOptionsAsync(vm, ct);");
        controllerSource.Should().Contain("return RenderBusinessEditor(vm, isCreate: true);");
        controllerSource.Should().Contain("var dto = new BusinessCreateDto");
        controllerSource.Should().Contain("Name = vm.Name,");
        controllerSource.Should().Contain("LegalName = vm.LegalName,");
        controllerSource.Should().Contain("DefaultCurrency = vm.DefaultCurrency,");
        controllerSource.Should().Contain("CommunicationReplyToEmail = vm.CommunicationReplyToEmail,");
        controllerSource.Should().Contain("OperationalAlertEmailsEnabled = vm.OperationalAlertEmailsEnabled,");
        controllerSource.Should().Contain("IsActive = vm.IsActive");
        controllerSource.Should().Contain("var businessId = await _createBusiness.HandleAsync(dto, ct);");
        controllerSource.Should().Contain("if (vm.OwnerUserId.HasValue)");
        controllerSource.Should().Contain("await _createBusinessMember.HandleAsync(new BusinessMemberCreateDto");
        controllerSource.Should().Contain("Role = BusinessMemberRole.Owner,");
        controllerSource.Should().Contain("TempData[\"Success\"] = vm.OwnerUserId.HasValue");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Edit), new { id = businessId });");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessEditorEditCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)");
        controllerSource.Should().Contain("var dto = await _getBusinessForEdit.HandleAsync(id, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        controllerSource.Should().Contain("var vm = MapBusinessEditVm(dto);");
        controllerSource.Should().Contain("await PopulateBusinessFormOptionsAsync(vm, ct);");
        controllerSource.Should().Contain("return RenderBusinessEditor(vm, isCreate: false);");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessEditorEditSubmitContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Edit(BusinessEditVm vm, CancellationToken ct = default)");
        controllerSource.Should().Contain("await PopulateBusinessFormOptionsAsync(vm, ct);");
        controllerSource.Should().Contain("return RenderBusinessEditor(vm, isCreate: false);");
        controllerSource.Should().Contain("var dto = new BusinessEditDto");
        controllerSource.Should().Contain("Id = vm.Id,");
        controllerSource.Should().Contain("RowVersion = vm.RowVersion ?? Array.Empty<byte>(),");
        controllerSource.Should().Contain("SupportEmail = vm.SupportEmail,");
        controllerSource.Should().Contain("CustomerMarketingEmailsEnabled = vm.CustomerMarketingEmailsEnabled,");
        controllerSource.Should().Contain("await _updateBusiness.HandleAsync(dto, ct);");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessUpdated\");");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessConcurrencyConflict\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessEditorRenderContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderBusinessEditor(BusinessEditVm vm, bool isCreate)");
        controllerSource.Should().Contain("ViewData[\"IsCreate\"] = isCreate;");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_BusinessEditorShell.cshtml\", vm);");
        controllerSource.Should().Contain("return isCreate ? View(\"Create\", vm) : View(\"Edit\", vm);");
        controllerSource.Should().Contain("private IActionResult RenderBusinessSetupEditor(BusinessEditVm vm)");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_BusinessSetupShell.cshtml\", vm);");
        controllerSource.Should().Contain("return View(\"Setup\", vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessSetupContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Setup(Guid id, CancellationToken ct = default)");
        controllerSource.Should().Contain("var dto = await _getBusinessForEdit.HandleAsync(id, ct);");
        controllerSource.Should().Contain("var vm = MapBusinessEditVm(dto);");
        controllerSource.Should().Contain("await PopulateBusinessFormOptionsAsync(vm, ct);");
        controllerSource.Should().Contain("return RenderBusinessSetupEditor(vm);");
        controllerSource.Should().Contain("public async Task<IActionResult> Setup(BusinessEditVm vm, CancellationToken ct = default)");
        controllerSource.Should().Contain("if (!ModelState.IsValid)");
        controllerSource.Should().Contain("return RenderBusinessSetupEditor(vm);");
        controllerSource.Should().Contain("await _updateBusiness.HandleAsync(dto, ct);");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessUpdated\");");
    }

    [Fact]
    public void BusinessesController_Should_KeepSubscriptionWorkspaceCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Subscription(Guid businessId, CancellationToken ct = default)");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        controllerSource.Should().Contain("var vm = await BuildBusinessSubscriptionWorkspaceAsync(business, ct);");
        controllerSource.Should().Contain("return RenderSubscriptionWorkspace(vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepSubscriptionWorkspaceBuilderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private async Task<BusinessSubscriptionWorkspaceVm> BuildBusinessSubscriptionWorkspaceAsync(BusinessContextVm business, CancellationToken ct)");
        controllerSource.Should().Contain("var subscription = await BuildBusinessSubscriptionSnapshotAsync(business.Id, ct);");
        controllerSource.Should().Contain("var settings = await _siteSettingCache.GetAsync(ct);");
        controllerSource.Should().Contain("var workspaceManagementWebsiteUrl = BuildSubscriptionManagementWebsiteUrl(managementWebsiteUrl, business.Id, planCode: null);");
        controllerSource.Should().Contain("var plans = await _getBillingPlans.HandleAsync(activeOnly: true, ct);");
        controllerSource.Should().Contain("var recentInvoices = await _getBusinessSubscriptionInvoicesPage.HandleAsync(");
        controllerSource.Should().Contain("pageSize: 5,");
        controllerSource.Should().Contain("var invoiceSummary = await _getBusinessSubscriptionInvoiceOpsSummary.HandleAsync(business.Id, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var planVms = new List<BusinessBillingPlanVm>();");
        controllerSource.Should().Contain("var validation = await _createSubscriptionCheckoutIntent.ValidateAsync(business.Id, x.Id, ct);");
        controllerSource.Should().Contain("planVms.Add(new BusinessBillingPlanVm");
        controllerSource.Should().Contain("InvoiceSummary = MapBusinessSubscriptionInvoiceOpsSummaryVm(invoiceSummary),");
        controllerSource.Should().Contain("RecentInvoices = recentInvoices.Items.Select(MapBusinessSubscriptionInvoiceListItemVm).ToList(),");
        controllerSource.Should().Contain("Playbooks = BuildSubscriptionPlaybooks(business.Id, subscription, managementWebsiteConfigured)");
    }

    [Fact]
    public void BusinessesController_Should_KeepSubscriptionRenderContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderSubscriptionWorkspace(BusinessSubscriptionWorkspaceVm vm)");
        controllerSource.Should().Contain("return PartialView(\"Subscription\", vm);");
        controllerSource.Should().Contain("return View(\"Subscription\", vm);");
        controllerSource.Should().Contain("private IActionResult RenderSubscriptionInvoicesWorkspace(BusinessSubscriptionInvoicesListVm vm)");
        controllerSource.Should().Contain("return PartialView(\"SubscriptionInvoices\", vm);");
        controllerSource.Should().Contain("return View(\"SubscriptionInvoices\", vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepSubscriptionInvoicesWorkspaceCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SubscriptionInvoices(");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("var result = await _getBusinessSubscriptionInvoicesPage.HandleAsync(");
        controllerSource.Should().Contain("var summary = await _getBusinessSubscriptionInvoiceOpsSummary.HandleAsync(businessId, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var vm = new BusinessSubscriptionInvoicesListVm");
        controllerSource.Should().Contain("Business = business,");
        controllerSource.Should().Contain("Page = page,");
        controllerSource.Should().Contain("PageSize = pageSize,");
        controllerSource.Should().Contain("Total = result.Total,");
        controllerSource.Should().Contain("Query = query ?? string.Empty,");
        controllerSource.Should().Contain("Filter = filter,");
        controllerSource.Should().Contain("FilterItems = BuildBusinessSubscriptionInvoiceFilterItems(filter),");
        controllerSource.Should().Contain("Summary = MapBusinessSubscriptionInvoiceOpsSummaryVm(summary),");
        controllerSource.Should().Contain("Items = result.Items.Select(MapBusinessSubscriptionInvoiceListItemVm).ToList()");
        controllerSource.Should().Contain("return RenderSubscriptionInvoicesWorkspace(vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepSubscriptionInvoiceMappingContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private static BusinessSubscriptionInvoiceOpsSummaryVm MapBusinessSubscriptionInvoiceOpsSummaryVm(BusinessSubscriptionInvoiceOpsSummaryDto dto)");
        controllerSource.Should().Contain("private static BusinessSubscriptionInvoiceListItemVm MapBusinessSubscriptionInvoiceListItemVm(BusinessSubscriptionInvoiceListItemDto dto)");
        controllerSource.Should().Contain("TotalCount = dto.TotalCount,");
        controllerSource.Should().Contain("OpenCount = dto.OpenCount,");
        controllerSource.Should().Contain("PaidCount = dto.PaidCount,");
        controllerSource.Should().Contain("DraftCount = dto.DraftCount,");
        controllerSource.Should().Contain("UncollectibleCount = dto.UncollectibleCount,");
        controllerSource.Should().Contain("HostedLinkMissingCount = dto.HostedLinkMissingCount,");
        controllerSource.Should().Contain("StripeCount = dto.StripeCount,");
        controllerSource.Should().Contain("OverdueCount = dto.OverdueCount,");
        controllerSource.Should().Contain("PdfMissingCount = dto.PdfMissingCount");
        controllerSource.Should().Contain("Id = dto.Id,");
        controllerSource.Should().Contain("BusinessId = dto.BusinessId,");
        controllerSource.Should().Contain("BusinessSubscriptionId = dto.BusinessSubscriptionId,");
        controllerSource.Should().Contain("Provider = dto.Provider,");
        controllerSource.Should().Contain("ProviderInvoiceId = dto.ProviderInvoiceId,");
        controllerSource.Should().Contain("Status = dto.Status,");
        controllerSource.Should().Contain("TotalMinor = dto.TotalMinor,");
        controllerSource.Should().Contain("Currency = dto.Currency,");
        controllerSource.Should().Contain("IssuedAtUtc = dto.IssuedAtUtc,");
        controllerSource.Should().Contain("DueAtUtc = dto.DueAtUtc,");
        controllerSource.Should().Contain("PaidAtUtc = dto.PaidAtUtc,");
        controllerSource.Should().Contain("HostedInvoiceUrl = dto.HostedInvoiceUrl,");
        controllerSource.Should().Contain("PdfUrl = dto.PdfUrl,");
        controllerSource.Should().Contain("FailureReason = dto.FailureReason,");
        controllerSource.Should().Contain("PlanName = dto.PlanName,");
        controllerSource.Should().Contain("PlanCode = dto.PlanCode,");
        controllerSource.Should().Contain("HasHostedInvoiceUrl = dto.HasHostedInvoiceUrl,");
        controllerSource.Should().Contain("HasPdfUrl = dto.HasPdfUrl,");
        controllerSource.Should().Contain("IsStripe = dto.IsStripe,");
        controllerSource.Should().Contain("IsOverdue = dto.IsOverdue");
    }

    [Fact]
    public void BusinessesController_Should_KeepLocationsWorkspaceCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Locations(Guid businessId, int page = 1, int pageSize = 20, string? query = null, BusinessLocationQueueFilter filter = BusinessLocationQueueFilter.All, CancellationToken ct = default)");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("var (items, total) = await _getBusinessLocationsPage.HandleAsync(businessId, page, pageSize, query, filter, ct);");
        controllerSource.Should().Contain("var summary = await _getBusinessLocationsPage.GetSummaryAsync(businessId, ct);");
        controllerSource.Should().Contain("var vm = new BusinessLocationsListVm");
        controllerSource.Should().Contain("Business = business,");
        controllerSource.Should().Contain("Page = page,");
        controllerSource.Should().Contain("PageSize = pageSize,");
        controllerSource.Should().Contain("Total = total,");
        controllerSource.Should().Contain("Query = query ?? string.Empty,");
        controllerSource.Should().Contain("Filter = filter,");
        controllerSource.Should().Contain("FilterItems = BuildBusinessLocationFilterItems(filter),");
        controllerSource.Should().Contain("Summary = new BusinessLocationOpsSummaryVm");
        controllerSource.Should().Contain("Playbooks = BuildBusinessLocationPlaybooks(businessId),");
        controllerSource.Should().Contain("Items = items.Select(x => new BusinessLocationListItemVm");
        controllerSource.Should().Contain("return RenderLocationsWorkspace(vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepLocationsSummaryAndItemMappingContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("TotalCount = summary.TotalCount,");
        controllerSource.Should().Contain("PrimaryCount = summary.PrimaryCount,");
        controllerSource.Should().Contain("MissingAddressCount = summary.MissingAddressCount,");
        controllerSource.Should().Contain("MissingCoordinatesCount = summary.MissingCoordinatesCount");
        controllerSource.Should().Contain("Items = items.Select(x => new BusinessLocationListItemVm");
        controllerSource.Should().Contain("Id = x.Id,");
        controllerSource.Should().Contain("BusinessId = x.BusinessId,");
        controllerSource.Should().Contain("Name = x.Name,");
        controllerSource.Should().Contain("City = x.City,");
        controllerSource.Should().Contain("Region = x.Region,");
        controllerSource.Should().Contain("CountryCode = x.CountryCode,");
        controllerSource.Should().Contain("IsPrimary = x.IsPrimary,");
        controllerSource.Should().Contain("HasAddress = x.HasAddress,");
        controllerSource.Should().Contain("HasCoordinates = x.HasCoordinates,");
        controllerSource.Should().Contain("ModifiedAtUtc = x.ModifiedAtUtc,");
        controllerSource.Should().Contain("RowVersion = x.RowVersion");
    }

    [Fact]
    public void BusinessesController_Should_KeepLocationsRenderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderLocationsWorkspace(BusinessLocationsListVm vm)");
        controllerSource.Should().Contain("return PartialView(\"Locations\", vm);");
        controllerSource.Should().Contain("return View(\"Locations\", vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepOwnerOverrideAuditsWorkspaceCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> OwnerOverrideAudits(Guid businessId, int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("var (items, total) = await _getBusinessOwnerOverrideAuditsPage.HandleAsync(businessId, page, pageSize, query, ct);");
        controllerSource.Should().Contain("var vm = new BusinessOwnerOverrideAuditsListVm");
        controllerSource.Should().Contain("Business = business,");
        controllerSource.Should().Contain("Page = page,");
        controllerSource.Should().Contain("PageSize = pageSize,");
        controllerSource.Should().Contain("Total = total,");
        controllerSource.Should().Contain("Query = query ?? string.Empty,");
        controllerSource.Should().Contain("Playbooks = BuildBusinessOwnerOverrideAuditPlaybooks(businessId),");
        controllerSource.Should().Contain("Items = items.Select(x => new BusinessOwnerOverrideAuditListItemVm");
        controllerSource.Should().Contain("return RenderOwnerOverrideAuditsWorkspace(vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepOwnerOverrideAuditItemMappingContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("Items = items.Select(x => new BusinessOwnerOverrideAuditListItemVm");
        controllerSource.Should().Contain("Id = x.Id,");
        controllerSource.Should().Contain("BusinessId = x.BusinessId,");
        controllerSource.Should().Contain("BusinessMemberId = x.BusinessMemberId,");
        controllerSource.Should().Contain("AffectedUserId = x.AffectedUserId,");
        controllerSource.Should().Contain("AffectedUserDisplayName = x.AffectedUserDisplayName,");
        controllerSource.Should().Contain("AffectedUserEmail = x.AffectedUserEmail,");
        controllerSource.Should().Contain("ActionKind = x.ActionKind,");
        controllerSource.Should().Contain("Reason = x.Reason,");
        controllerSource.Should().Contain("ActorDisplayName = x.ActorDisplayName,");
        controllerSource.Should().Contain("CreatedAtUtc = x.CreatedAtUtc");
    }

    [Fact]
    public void BusinessesController_Should_KeepOwnerOverrideAuditsRenderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderOwnerOverrideAuditsWorkspace(BusinessOwnerOverrideAuditsListVm vm)");
        controllerSource.Should().Contain("return PartialView(\"OwnerOverrideAudits\", vm);");
        controllerSource.Should().Contain("return View(\"OwnerOverrideAudits\", vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepSubscriptionCancelAtPeriodEndPostContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SetSubscriptionCancelAtPeriodEnd(");
        controllerSource.Should().Contain("var parsedRowVersion = string.IsNullOrWhiteSpace(rowVersion)");
        controllerSource.Should().Contain("Array.Empty<byte>()");
        controllerSource.Should().Contain("Convert.FromBase64String(rowVersion);");
        controllerSource.Should().Contain("var result = await _setCancelAtPeriodEnd.HandleAsync(");
        controllerSource.Should().Contain("businessId,");
        controllerSource.Should().Contain("subscriptionId,");
        controllerSource.Should().Contain("cancelAtPeriodEnd,");
        controllerSource.Should().Contain("parsedRowVersion,");
        controllerSource.Should().Contain("TempData[result.Succeeded ? \"Success\" : \"Error\"] = result.Succeeded");
        controllerSource.Should().Contain("T(\"BusinessSubscriptionCancelAtPeriodEndUpdated\")");
        controllerSource.Should().Contain("T(\"BusinessSubscriptionRenewalRestored\")");
        controllerSource.Should().Contain("T(\"BusinessSubscriptionCancelAtPeriodEndUpdateFailed\")");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Subscription), new { businessId });");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessLifecyclePostContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Approve([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)");
        controllerSource.Should().Contain("await _approveBusiness.HandleAsync(new BusinessLifecycleActionDto");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessApproved\")");
        controllerSource.Should().Contain("public async Task<IActionResult> Suspend([FromForm] Guid id, [FromForm] byte[]? rowVersion, [FromForm] string? note, CancellationToken ct = default)");
        controllerSource.Should().Contain("await _suspendBusiness.HandleAsync(new BusinessLifecycleActionDto");
        controllerSource.Should().Contain("Note = note");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessSuspended\")");
        controllerSource.Should().Contain("public async Task<IActionResult> Reactivate([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)");
        controllerSource.Should().Contain("await _reactivateBusiness.HandleAsync(new BusinessLifecycleActionDto");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessReactivated\")");
        controllerSource.Should().Contain("TempData[\"Error\"] = ex.Message;");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Edit), new { id });");
    }

    [Fact]
    public void BusinessesController_Should_KeepDeleteActionPostContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)");
        controllerSource.Should().Contain("Result result = await _deleteBusiness.HandleAsync(new BusinessDeleteDto");
        controllerSource.Should().Contain("T(\"BusinessArchived\")");
        controllerSource.Should().Contain("T(\"BusinessArchiveFailed\")");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        controllerSource.Should().Contain("public async Task<IActionResult> DeleteLocation([FromForm] Guid id, [FromForm(Name = \"userId\")] Guid businessId, [FromForm] byte[]? rowVersion, CancellationToken ct = default)");
        controllerSource.Should().Contain("var result = await _deleteBusinessLocation.HandleAsync(new BusinessLocationDeleteDto");
        controllerSource.Should().Contain("T(\"BusinessLocationArchived\")");
        controllerSource.Should().Contain("T(\"BusinessLocationArchiveFailed\")");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Locations), new { businessId });");
        controllerSource.Should().Contain("public async Task<IActionResult> DeleteMember([FromForm] Guid id, [FromForm(Name = \"userId\")] Guid businessId, [FromForm] byte[]? rowVersion, CancellationToken ct = default)");
        controllerSource.Should().Contain("await _deleteBusinessMember.HandleAsync(new BusinessMemberDeleteDto");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessMemberRemoved\")");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Members), new { businessId });");
    }

    [Fact]
    public void BusinessesController_Should_KeepInvitationActionPostContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> ResendInvitation([FromForm] Guid id, [FromForm] Guid businessId, CancellationToken ct = default)");
        controllerSource.Should().Contain("await _resendBusinessInvitation.HandleAsync(new BusinessInvitationResendDto");
        controllerSource.Should().Contain("ExpiresInDays = 7");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessInvitationReissued\")");
        controllerSource.Should().Contain("public async Task<IActionResult> RevokeInvitation([FromForm] Guid id, [FromForm] Guid businessId, CancellationToken ct = default)");
        controllerSource.Should().Contain("await _revokeBusinessInvitation.HandleAsync(new BusinessInvitationRevokeDto");
        controllerSource.Should().Contain("Note = T(\"BusinessInvitationRevokedFromWebAdminNote\")");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessInvitationRevoked\")");
        controllerSource.Should().Contain("TempData[\"Error\"] = ex.Message;");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Invitations), new { businessId });");
    }

    [Fact]
    public void BusinessesController_Should_KeepSetupMembersPreviewFragmentContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SetupMembersPreview(Guid businessId, CancellationToken ct = default)");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_SetupMembersPreview.cshtml\", new BusinessSetupMembersPreviewVm");
        controllerSource.Should().Contain("BusinessId = businessId");
        controllerSource.Should().Contain("var (items, _) = await _getBusinessMembersPage.HandleAsync(");
        controllerSource.Should().Contain("filter: BusinessMemberSupportFilter.All,");
        controllerSource.Should().Contain("var attentionMembers = items");
        controllerSource.Should().Contain(".Where(x => !x.EmailConfirmed || (x.LockoutEndUtc.HasValue && x.LockoutEndUtc.Value > DateTime.UtcNow))");
        controllerSource.Should().Contain(".Take(5)");
        controllerSource.Should().Contain(".Select(x => new BusinessMemberListItemVm");
        controllerSource.Should().Contain("UserDisplayName = x.UserDisplayName,");
        controllerSource.Should().Contain("UserEmail = x.UserEmail,");
        controllerSource.Should().Contain("EmailConfirmed = x.EmailConfirmed,");
        controllerSource.Should().Contain("LockoutEndUtc = x.LockoutEndUtc,");
        controllerSource.Should().Contain("AttentionCount = items.Count(x => !x.EmailConfirmed || (x.LockoutEndUtc.HasValue && x.LockoutEndUtc.Value > DateTime.UtcNow)),");
        controllerSource.Should().Contain("Items = attentionMembers");
    }

    [Fact]
    public void BusinessesController_Should_KeepSetupInvitationsPreviewFragmentContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SetupInvitationsPreview(Guid businessId, CancellationToken ct = default)");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_SetupInvitationsPreview.cshtml\", new BusinessSetupInvitationsPreviewVm");
        controllerSource.Should().Contain("BusinessId = businessId");
        controllerSource.Should().Contain("var (items, _) = await _getBusinessInvitationsPage.HandleAsync(");
        controllerSource.Should().Contain("filter: BusinessInvitationQueueFilter.All,");
        controllerSource.Should().Contain("var openInvitations = items");
        controllerSource.Should().Contain(".Where(x => x.Status == BusinessInvitationStatus.Pending || x.Status == BusinessInvitationStatus.Expired)");
        controllerSource.Should().Contain(".Take(5)");
        controllerSource.Should().Contain(".Select(x => new BusinessInvitationListItemVm");
        controllerSource.Should().Contain("Email = x.Email,");
        controllerSource.Should().Contain("Role = x.Role,");
        controllerSource.Should().Contain("Status = x.Status,");
        controllerSource.Should().Contain("InvitedByDisplayName = x.InvitedByDisplayName,");
        controllerSource.Should().Contain("OpenCount = items.Count(x => x.Status == BusinessInvitationStatus.Pending || x.Status == BusinessInvitationStatus.Expired),");
        controllerSource.Should().Contain("Items = openInvitations");
    }

    [Fact]
    public void BusinessesController_Should_KeepSupportSummaryMappingContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private static BusinessSupportSummaryVm MapSupportSummaryVm(BusinessSupportSummaryDto summary)");
        controllerSource.Should().Contain("AttentionBusinessCount = summary.AttentionBusinessCount,");
        controllerSource.Should().Contain("PendingApprovalBusinessCount = summary.PendingApprovalBusinessCount,");
        controllerSource.Should().Contain("SuspendedBusinessCount = summary.SuspendedBusinessCount,");
        controllerSource.Should().Contain("ApprovedInactiveBusinessCount = summary.ApprovedInactiveBusinessCount,");
        controllerSource.Should().Contain("MissingOwnerBusinessCount = summary.MissingOwnerBusinessCount,");
        controllerSource.Should().Contain("MissingPrimaryLocationBusinessCount = summary.MissingPrimaryLocationBusinessCount,");
        controllerSource.Should().Contain("MissingContactEmailBusinessCount = summary.MissingContactEmailBusinessCount,");
        controllerSource.Should().Contain("MissingLegalNameBusinessCount = summary.MissingLegalNameBusinessCount,");
        controllerSource.Should().Contain("OpenInvitationCount = summary.OpenInvitationCount,");
        controllerSource.Should().Contain("PendingActivationMemberCount = summary.PendingActivationMemberCount,");
        controllerSource.Should().Contain("LockedMemberCount = summary.LockedMemberCount");
    }

    [Fact]
    public void BusinessesController_Should_KeepMerchantReadinessWorkspaceCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> MerchantReadiness(CancellationToken ct = default)");
        controllerSource.Should().Contain("var summary = await _getBusinessSupportSummary.HandleAsync(null, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (attentionBusinesses, _) = await _getBusinessesPage.HandleAsync(1, 12, null, null, true, readinessFilter: null, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var items = new List<MerchantReadinessItemVm>();");
        controllerSource.Should().Contain("foreach (var business in attentionBusinesses)");
        controllerSource.Should().Contain("var vm = new MerchantReadinessWorkspaceVm");
        controllerSource.Should().Contain("Summary = MapSupportSummaryVm(summary),");
        controllerSource.Should().Contain("Items = items,");
        controllerSource.Should().Contain("Playbooks = BuildMerchantReadinessPlaybooks()");
        controllerSource.Should().Contain("return RenderMerchantReadinessWorkspace(vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepMerchantReadinessSubscriptionSnapshotPipelineWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("var subscription = await BuildBusinessSubscriptionSnapshotAsync(business.Id, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("items.Add(new MerchantReadinessItemVm");
        controllerSource.Should().Contain("HasSubscription = subscription.HasSubscription,");
        controllerSource.Should().Contain("SubscriptionStatus = subscription.Status,");
        controllerSource.Should().Contain("SubscriptionPlanName = subscription.PlanName,");
        controllerSource.Should().Contain("CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,");
        controllerSource.Should().Contain("CurrentPeriodEndUtc = subscription.CurrentPeriodEndUtc");
        controllerSource.Should().Contain("private async Task<BusinessSubscriptionSnapshotVm> BuildBusinessSubscriptionSnapshotAsync(Guid businessId, CancellationToken ct)");
        controllerSource.Should().Contain("var result = await _getBusinessSubscriptionStatus.HandleAsync(businessId, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("Status = T(\"Unavailable\")");
    }

    [Fact]
    public void BusinessesController_Should_KeepMerchantReadinessRenderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderMerchantReadinessWorkspace(MerchantReadinessWorkspaceVm vm)");
        controllerSource.Should().Contain("return PartialView(\"MerchantReadiness\", vm);");
        controllerSource.Should().Contain("return View(\"MerchantReadiness\", vm);");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepMemberAndInvitationShortcutLabelsHelperBacked()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        summaryViewSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        summaryViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        summaryViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        summaryViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        summaryViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingActivation\")</div>");
        summaryViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingInvites\")</div>");
        summaryViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingInvites\")</a>");
        summaryViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterUnconfirmed\")</a>");
        summaryViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepMemberAndInvitationCardSubtitlesHelperBacked()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        summaryViewSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</div>");
        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</div>");
        summaryViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingActivation\")</div>");
        summaryViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingInvites\")</div>");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepMemberAndInvitationSummaryLabelsHelperBacked()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        indexViewSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        indexViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        indexViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        indexViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        indexViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingInvites\")</div>");
        indexViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingActivation\")</div>");
        indexViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"LockedMembers\")</div>");
        indexViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingInvites\")</a>");
        indexViewSource.Should().NotContain("<i class=\"fa-solid fa-envelope-open-text\"></i> @T.T(\"PendingInvites\")");
        indexViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterUnconfirmed\")</a>");
        indexViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
        indexViewSource.Should().NotContain("@item.InvitationCount @T.T(\"PendingInvites\")");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepMemberAndInvitationCardSubtitlesHelperBacked()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        indexViewSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        indexViewSource.Should().Contain("<div class=\"text-muted small\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</div>");
        indexViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</div>");
        indexViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingInvites\")</div>");
        indexViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingActivation\")</div>");
    }

    [Fact]
    public void SupportQueueAttentionFragment_Should_KeepInvitationSignalLabelsHelperBacked()
    {
        var attentionFragmentSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueAttentionBusinesses.cshtml"));

        attentionFragmentSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        attentionFragmentSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        attentionFragmentSource.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = item.Id, filter = \"Open\" })");
        attentionFragmentSource.Should().NotContain("@item.InvitationCount @T.T(\"PendingInvites\")");
    }

    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepMemberAndInvitationSummaryLabelsHelperBacked()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        merchantReadinessViewSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        merchantReadinessViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        merchantReadinessViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        merchantReadinessViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        merchantReadinessViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingInvites\")</div>");
        merchantReadinessViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingActivation\")</div>");
        merchantReadinessViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"LockedMembers\")</div>");
        merchantReadinessViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingInvites\")</a>");
        merchantReadinessViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterUnconfirmed\")</a>");
        merchantReadinessViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
        merchantReadinessViewSource.Should().NotContain("@item.InvitationCount @T.T(\"PendingInvites\")");
    }

    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepMemberAndInvitationCardSubtitlesHelperBacked()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        merchantReadinessViewSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</div>");
        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</div>");
        merchantReadinessViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingInvites\")</div>");
        merchantReadinessViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingActivation\")</div>");
    }

    [Fact]
    public void SupportQueueWorkspace_Should_KeepMemberAndInvitationShortcutLabelsHelperBacked()
    {
        var supportQueueViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        supportQueueViewSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        supportQueueViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        supportQueueViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        supportQueueViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        supportQueueViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingInvites\")</a>");
        supportQueueViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
        supportQueueViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"LockedMembers\")</a>");
    }

    [Fact]
    public void SupportQueueFailedEmailsFragment_Should_KeepLocalizedActionShortcutsHelperBacked()
    {
        var failedEmailsViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueFailedEmails.cshtml"));

        failedEmailsViewSource.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        failedEmailsViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        failedEmailsViewSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Open)");
        failedEmailsViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        failedEmailsViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        failedEmailsViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"OpenInvitations\")</a>");
        failedEmailsViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterUnconfirmed\")</a>");
        failedEmailsViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
    }

    [Fact]
    public void BusinessesController_Should_KeepLocalizedMutationFeedbackContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        source.Should().Contain("? T(\"BusinessCreateOwnerAssigned\")");
        source.Should().Contain(": T(\"BusinessCreateNextSteps\")");
        source.Should().Contain("? (cancelAtPeriodEnd ? T(\"BusinessSubscriptionCancelAtPeriodEndUpdated\") : T(\"BusinessSubscriptionRenewalRestored\"))");
        source.Should().Contain(": (result.Error ?? T(\"BusinessSubscriptionCancelAtPeriodEndUpdateFailed\"))");
        source.Should().Contain("result.Succeeded ? T(\"BusinessArchived\") : (result.Error ?? T(\"BusinessArchiveFailed\"))");
        source.Should().Contain("result.Succeeded ? T(\"BusinessLocationArchived\") : (result.Error ?? T(\"BusinessLocationArchiveFailed\"))");
        source.Should().Contain("Note = T(\"BusinessInvitationRevokedFromWebAdminNote\")");
    }

    [Fact]
    public void BusinessesWorkspaces_Should_KeepMerchantPlaybookContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));
        var supportQueueViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        controllerSource.Should().Contain("Playbooks = BuildMerchantReadinessPlaybooks()");
        controllerSource.Should().Contain("private List<MerchantReadinessPlaybookVm> BuildMerchantReadinessPlaybooks()");
        controllerSource.Should().Contain("QueueActionLabel = T(\"PendingApproval\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"BusinessSupportQueueTitle\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"NeedsAttention\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"CommonSetup\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"ApprovedInactive\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"Payments\")");

        indexViewSource.Should().Contain("BusinessesOperationsPlaybooksTitle");
        indexViewSource.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        indexViewSource.Should().Contain("hx-target=\"#businesses-workspace-shell\"");
        indexViewSource.Should().Contain(">@playbook.Title</a>");
        indexViewSource.Should().Contain(">@playbook.ScopeNote</a>");
        indexViewSource.Should().Contain(">@playbook.OperatorAction</a>");
        indexViewSource.Should().Contain(">@playbook.QueueActionLabel</a>");
        indexViewSource.Should().Contain(">@playbook.FollowUpLabel</a>");
        supportQueueViewSource.Should().Contain("BusinessesOperationsPlaybooksTitle");
        supportQueueViewSource.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        supportQueueViewSource.Should().Contain("hx-target=\"#business-support-queue-workspace-shell\"");
        supportQueueViewSource.Should().Contain(">@playbook.Title</a>");
        supportQueueViewSource.Should().Contain(">@playbook.ScopeNote</a>");
        supportQueueViewSource.Should().Contain(">@playbook.OperatorAction</a>");
        supportQueueViewSource.Should().Contain(">@playbook.QueueActionLabel</a>");
        supportQueueViewSource.Should().Contain(">@playbook.FollowUpLabel</a>");
        indexViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { operationalStatus = \"Suspended\" })");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</a>");
    }

    [Fact]
    public void SupportQueueFailedEmailsFragment_Should_KeepLocalizedFlowLabelContractsWired()
    {
        var failedEmailsViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueFailedEmails.cshtml"));

        failedEmailsViewSource.Should().Contain("string FlowLabel(string? flowKey) => flowKey switch");
        failedEmailsViewSource.Should().Contain("\"BusinessInvitation\" => T.T(\"CommunicationDetailsActiveFlowInvitation\")");
        failedEmailsViewSource.Should().Contain("\"AccountActivation\" => T.T(\"CommunicationDetailsActiveFlowActivation\")");
        failedEmailsViewSource.Should().Contain("\"PasswordReset\" => T.T(\"CommunicationTemplateInventoryPasswordResetFlow\")");
        failedEmailsViewSource.Should().Contain("\"AdminCommunicationTest\" => T.T(\"CommunicationTemplateInventoryAdminTestFlow\")");
        failedEmailsViewSource.Should().Contain("\"PhoneVerification\" => T.T(\"CommunicationTemplateInventoryPhoneVerificationFlow\")");
        failedEmailsViewSource.Should().Contain("string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : flowKey");
        failedEmailsViewSource.Should().Contain("<div class=\"fw-semibold\">@FlowLabel(item.FlowKey)</div>");
        failedEmailsViewSource.Should().NotContain("T.T(\"Unclassified\")");
        failedEmailsViewSource.Should().NotContain("@(string.IsNullOrWhiteSpace(item.FlowKey) ? T.T(\"Unclassified\") : item.FlowKey)");
    }

    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepDeepRowActionContractsWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("hx-target=\"#merchant-readiness-workspace-shell\"");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Edit\", \"Businesses\", new { id = item.Id })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Setup\", \"Businesses\", new { id = item.Id })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Subscription\", \"Businesses\", new { businessId = item.Id })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"SubscriptionInvoices\", \"Businesses\", new { businessId = item.Id })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = item.Id })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = item.Id })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"OwnerOverrideAudits\", \"Businesses\", new { businessId = item.Id })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Locations\", \"Businesses\", new { businessId = item.Id })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Details\", \"BusinessCommunications\", new { businessId = item.Id })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { businessId = item.Id })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"ChannelAudits\", \"BusinessCommunications\", new { businessId = item.Id })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Payments\", \"Billing\", new { businessId = item.Id })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Refunds\", \"Billing\", new { businessId = item.Id })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"FinancialAccounts\", \"Billing\", new { businessId = item.Id })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Expenses\", \"Billing\", new { businessId = item.Id })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"JournalEntries\", \"Billing\", new { businessId = item.Id })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"TaxCompliance\", \"Billing\")");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        merchantReadinessViewSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
    }

    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepSummaryAndQueueEntryContractsWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
        merchantReadinessViewSource.Should().Contain("@T.T(\"MerchantReadinessIntro\")");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { attentionOnly = true })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Payments\", \"Billing\")");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"TaxCompliance\", \"Billing\")");
        merchantReadinessViewSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
        merchantReadinessViewSource.Should().Contain("@T.T(\"Payments\")");
        merchantReadinessViewSource.Should().Contain("@T.T(\"TaxComplianceTitle\")");
        merchantReadinessViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)");
        merchantReadinessViewSource.Should().Contain("@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)");
        merchantReadinessViewSource.Should().Contain("@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)");
        merchantReadinessViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)");
        merchantReadinessViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        merchantReadinessViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        merchantReadinessViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)");
        merchantReadinessViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)");
        merchantReadinessViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)");
        merchantReadinessViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)");
        merchantReadinessViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        merchantReadinessViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        merchantReadinessViewSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        merchantReadinessViewSource.Should().Contain("class=\"btn btn-outline-secondary\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</a>");
        merchantReadinessViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { operationalStatus = \"PendingApproval\" })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { operationalStatus = \"Suspended\" })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Index\", \"Users\", new { filter = \"Unconfirmed\" })");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Index\", \"Users\", new { filter = \"Locked\" })");
        merchantReadinessViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        merchantReadinessViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        merchantReadinessViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterUnconfirmed\")</a>");
        merchantReadinessViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
        merchantReadinessViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingInvites\")</a>");
        merchantReadinessViewSource.Should().Contain("BusinessReadinessQueueFilter.MissingOwner");
        merchantReadinessViewSource.Should().Contain("BusinessReadinessQueueFilter.ApprovedInactive");
        merchantReadinessViewSource.Should().Contain("BusinessReadinessQueueFilter.MissingPrimaryLocation");
        merchantReadinessViewSource.Should().Contain("BusinessReadinessQueueFilter.MissingContactEmail");
        merchantReadinessViewSource.Should().Contain("BusinessReadinessQueueFilter.MissingLegalName");
        merchantReadinessViewSource.Should().Contain("BusinessReadinessQueueFilter.PendingInvites");
        merchantReadinessViewSource.Should().Contain("@T.T(\"MerchantReadinessQueueTitle\")");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Index\", \"BusinessCommunications\")");
        merchantReadinessViewSource.Should().Contain("@T.T(\"CommunicationOps\")");
        merchantReadinessViewSource.Should().Contain("@T.T(\"MerchantReadinessEmptyState\")");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\")");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessesTitle\")</a>");
        merchantReadinessViewSource.Should().Contain("@T.T(\"MerchantReadinessPlaybooksTitle\")");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Payments\", \"Billing\")");
        merchantReadinessViewSource.Should().Contain("@playbook.QueueActionUrl");
        merchantReadinessViewSource.Should().Contain("@playbook.FollowUpUrl");
        merchantReadinessViewSource.Should().Contain("@playbook.Title");
        merchantReadinessViewSource.Should().Contain("@playbook.ScopeNote");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
    }

    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepPlaybookRemediationContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        controllerSource.Should().Contain("private List<MerchantReadinessPlaybookVm> BuildMerchantReadinessPlaybooks()");
        controllerSource.Should().Contain("Title = T(\"MerchantReadinessPlaybookApprovalTitle\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"PendingApproval\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"BusinessSupportQueueTitle\")");
        controllerSource.Should().Contain("Title = T(\"MerchantReadinessPlaybookSetupTitle\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"NeedsAttention\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"CommonSetup\")");
        controllerSource.Should().Contain("Title = T(\"MerchantReadinessPlaybookBillingTitle\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"ApprovedInactive\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"Payments\")");

        merchantReadinessViewSource.Should().Contain("@T.T(\"MerchantReadinessPlaybooksTitle\")");
        merchantReadinessViewSource.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        merchantReadinessViewSource.Should().Contain("playbook.QueueActionUrl");
        merchantReadinessViewSource.Should().Contain("playbook.FollowUpUrl");
        merchantReadinessViewSource.Should().Contain(">@playbook.Title</a>");
        merchantReadinessViewSource.Should().Contain(">@playbook.ScopeNote</a>");
        merchantReadinessViewSource.Should().Contain(">@playbook.OperatorAction</a>");
        merchantReadinessViewSource.Should().Contain(">@playbook.QueueActionLabel</a>");
        merchantReadinessViewSource.Should().Contain(">@playbook.FollowUpLabel</a>");
        merchantReadinessViewSource.Should().Contain("@playbook.Title\n                            }");
        merchantReadinessViewSource.Should().Contain("@playbook.ScopeNote\n                            }");
        merchantReadinessViewSource.Should().Contain("<div class=\"mb-2\">@playbook.OperatorAction</div>");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Payments\", \"Billing\")");
        merchantReadinessViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { attentionOnly = true })");
        merchantReadinessViewSource.Should().Contain("hx-target=\"#merchant-readiness-workspace-shell\"");
    }

    [Fact]
    public void BusinessLocationsAndSubscriptionInvoiceWorkspaces_Should_KeepFilteredOpsContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var locationsViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Locations.cshtml"));
        var subscriptionInvoicesViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SubscriptionInvoices.cshtml"));

        controllerSource.Should().Contain("Playbooks = BuildBusinessLocationPlaybooks(businessId)");
        controllerSource.Should().Contain("private List<BusinessLocationPlaybookVm> BuildBusinessLocationPlaybooks(Guid businessId)");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommonAll\"), BusinessLocationQueueFilter.All.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"BusinessLocationsPrimaryLocationLabel\"), BusinessLocationQueueFilter.Primary.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"MissingAddress\"), BusinessLocationQueueFilter.MissingAddress.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"BusinessLocationsMissingCoordinatesLabel\"), BusinessLocationQueueFilter.MissingCoordinates.ToString()");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessLocationsPrimaryLocationLabel\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessLocationsPlaybookPrimaryWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessLocationsPlaybookPrimaryAction\")");
        controllerSource.Should().Contain("QueueLabel = T(\"MissingAddress\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessLocationsPlaybookMissingAddressWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessLocationsPlaybookMissingAddressAction\")");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessLocationsMissingCoordinatesLabel\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessLocationsPlaybookMissingCoordinatesWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessLocationsPlaybookMissingCoordinatesAction\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Locations\", \"Businesses\", new { businessId, filter = BusinessLocationQueueFilter.Primary }) ?? string.Empty");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Locations\", \"Businesses\", new { businessId, filter = BusinessLocationQueueFilter.MissingAddress }) ?? string.Empty");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Locations\", \"Businesses\", new { businessId, filter = BusinessLocationQueueFilter.MissingCoordinates }) ?? string.Empty");

        locationsViewSource.Should().Contain("BusinessLocationsPlaybooksTitle");
        locationsViewSource.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        locationsViewSource.Should().Contain("hx-target=\"#business-locations-workspace-shell\"");
        locationsViewSource.Should().Contain(">@playbook.QueueLabel</a>");
        locationsViewSource.Should().Contain(">@playbook.WhyItMatters</a>");
        locationsViewSource.Should().Contain(">@playbook.OperatorAction</a>");
        locationsViewSource.Should().Contain("href=\"@playbook.QueueActionUrl\"");
        locationsViewSource.Should().Contain("BusinessLocationQueueFilter.Primary");
        locationsViewSource.Should().Contain("BusinessLocationQueueFilter.MissingAddress");
        locationsViewSource.Should().Contain("BusinessLocationQueueFilter.MissingCoordinates");
          locationsViewSource.Should().Contain("@T.T(\"BusinessLocationsEmptyState\")");
          locationsViewSource.Should().Contain("@Url.Action(\"CreateLocation\", \"Businesses\", new { businessId = Model.Business.Id })");
          locationsViewSource.Should().Contain("@Url.Action(\"Setup\", \"Businesses\", new { id = Model.Business.Id })");
          locationsViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
          locationsViewSource.Should().Contain("@Url.Action(\"EditLocation\", \"Businesses\", new { id = item.Id })");
          locationsViewSource.Should().Contain("@T.T(\"CommonEdit\")");
          locationsViewSource.Should().Contain("@T.T(\"Setup\")");
          locationsViewSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");

        subscriptionInvoicesViewSource.Should().Contain("BusinessSubscriptionBillingPlaybooksTitle");
        subscriptionInvoicesViewSource.Should().Contain("BusinessSubscriptionInvoiceQueueFilter.Open");
        subscriptionInvoicesViewSource.Should().Contain("BusinessSubscriptionInvoiceQueueFilter.Paid");
        subscriptionInvoicesViewSource.Should().Contain("BusinessSubscriptionInvoiceQueueFilter.Draft");
        subscriptionInvoicesViewSource.Should().Contain("BusinessSubscriptionInvoiceQueueFilter.Uncollectible");
          subscriptionInvoicesViewSource.Should().Contain("BusinessSubscriptionInvoiceQueueFilter.Stripe");
          subscriptionInvoicesViewSource.Should().Contain("BusinessSubscriptionInvoiceQueueFilter.Overdue");
          subscriptionInvoicesViewSource.Should().Contain("BusinessSubscriptionInvoiceQueueFilter.PdfMissing");
          subscriptionInvoicesViewSource.Should().Contain("BusinessSubscriptionInvoiceQueueFilter.HostedLinkMissing");
          subscriptionInvoicesViewSource.Should().Contain("hx-target=\"#business-subscription-invoices-workspace-shell\"");
          subscriptionInvoicesViewSource.Should().Contain("@T.T(\"BusinessSubscriptionInvoicesEmptyState\")");
          subscriptionInvoicesViewSource.Should().Contain("@Url.Action(\"Subscription\", \"Businesses\", new { businessId = Model.Business.Id })");
          subscriptionInvoicesViewSource.Should().Contain("@Url.Action(\"Payments\", \"Billing\", new { businessId = Model.Business.Id, q = item.ProviderInvoiceId })");
          subscriptionInvoicesViewSource.Should().Contain("@Url.Action(\"Payments\", \"Billing\", new { businessId = Model.Business.Id })");
          subscriptionInvoicesViewSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
          subscriptionInvoicesViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
          subscriptionInvoicesViewSource.Should().Contain("@T.T(\"BusinessSubscriptionShort\")");
          subscriptionInvoicesViewSource.Should().Contain("@T.T(\"CommonPayments\")");
          subscriptionInvoicesViewSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
          subscriptionInvoicesViewSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
      }

    [Fact]
    public void BusinessSubscriptionWorkspace_Should_KeepPlaybookOpsContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var subscriptionViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Subscription.cshtml"));

        controllerSource.Should().Contain("Playbooks = BuildSubscriptionPlaybooks(business.Id, subscription, managementWebsiteConfigured)");
        controllerSource.Should().Contain("private List<BusinessSubscriptionPlaybookVm> BuildSubscriptionPlaybooks(Guid businessId, BusinessSubscriptionSnapshotVm subscription, bool managementWebsiteConfigured)");
        controllerSource.Should().Contain("new SelectListItem(T(\"BusinessSubscriptionAllInvoicesLabel\"), BusinessSubscriptionInvoiceQueueFilter.All.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommonOpen\"), BusinessSubscriptionInvoiceQueueFilter.Open.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommonPaid\"), BusinessSubscriptionInvoiceQueueFilter.Paid.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommonDraft\"), BusinessSubscriptionInvoiceQueueFilter.Draft.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommonUncollectible\"), BusinessSubscriptionInvoiceQueueFilter.Uncollectible.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"BusinessSubscriptionHostedLinkMissing\"), BusinessSubscriptionInvoiceQueueFilter.HostedLinkMissing.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommonStripe\"), BusinessSubscriptionInvoiceQueueFilter.Stripe.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommonOverdue\"), BusinessSubscriptionInvoiceQueueFilter.Overdue.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"BusinessSubscriptionReviewPdfMissing\"), BusinessSubscriptionInvoiceQueueFilter.PdfMissing.ToString()");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessManagementWebsite\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessSubscriptionPlaybookManagementWebsiteWhyItMatters\")");
        controllerSource.Should().Contain("? T(\"BusinessSubscriptionPlaybookManagementWebsiteActionConfigured\")");
        controllerSource.Should().Contain(": T(\"BusinessSubscriptionPlaybookManagementWebsiteActionMissing\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"CommonSetup\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Edit\", \"SiteSettings\", new { fragment = \"site-settings-business-app\" }) ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessSubscriptionCancellationPolicy\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessSubscriptionPlaybookCancellationWhyItMatters\")");
        controllerSource.Should().Contain("? T(\"BusinessSubscriptionPlaybookCancellationActionActive\")");
        controllerSource.Should().Contain(": T(\"BusinessSubscriptionPlaybookCancellationActionInactive\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"SubscriptionInvoices\", \"Businesses\", new { businessId, filter = BusinessSubscriptionInvoiceQueueFilter.Open }) ?? string.Empty");
        controllerSource.Should().Contain("FollowUpLabel = T(\"CommonPayments\")");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"Payments\", \"Billing\", new { businessId }) ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessSubscriptionNoActivePlan\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessSubscriptionPlaybookNoActivePlanWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessSubscriptionPlaybookNoActivePlanAction\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"BusinessSupportQueueTitle\")");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"SupportQueue\", \"Businesses\") ?? string.Empty");
        controllerSource.Should().Contain("Status = T(\"Unavailable\")");
        controllerSource.Should().Contain("CheckoutReadinessLabel = validation.Succeeded ? T(\"BusinessSubscriptionCheckoutReady\") : (validation.Error ?? T(\"NotReady\"))");
        controllerSource.Should().Contain("? T(\"BusinessSubscriptionManageCurrentPlan\")");
        controllerSource.Should().Contain("subscription.HasSubscription ? T(\"BusinessSubscriptionUpgradeToPlan\") : T(\"BusinessSubscriptionStartWithPlan\")");
        controllerSource.Should().Contain("? T(\"BusinessSubscriptionCurrentPlanBadge\")");
        controllerSource.Should().Contain("? T(\"BusinessSubscriptionOpenBillingWebsite\")");
        controllerSource.Should().Contain("? T(\"BusinessSubscriptionResolvePrerequisites\")");
        controllerSource.Should().Contain(": T(\"BusinessSubscriptionConfigureWebsite\")");

        subscriptionViewSource.Should().Contain("BusinessSubscriptionBillingPlaybooksTitle");
        subscriptionViewSource.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        subscriptionViewSource.Should().Contain("playbook.QueueActionUrl");
        subscriptionViewSource.Should().Contain("playbook.FollowUpUrl");
        subscriptionViewSource.Should().Contain("hx-target=\"#business-subscription-workspace-shell\"");
        subscriptionViewSource.Should().Contain("href=\"@playbook.QueueActionUrl\"");
        subscriptionViewSource.Should().Contain(">@playbook.WhyItMatters</a>");
        subscriptionViewSource.Should().Contain(">@playbook.QueueLabel</a>");
        subscriptionViewSource.Should().Contain(">@playbook.OperatorAction</a>");
        subscriptionViewSource.Should().Contain(">@playbook.QueueActionLabel</a>");
        subscriptionViewSource.Should().Contain(">@playbook.FollowUpLabel</a>");
        subscriptionViewSource.Should().Contain("@T.T(\"BusinessSubscriptionNoActiveSnapshot\")");
        subscriptionViewSource.Should().Contain("@Url.Action(\"Setup\", \"Businesses\", new { id = Model.Business.Id })");
        subscriptionViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        subscriptionViewSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        subscriptionViewSource.Should().Contain("@T.T(\"BusinessSubscriptionNoRecentInvoices\")");
        subscriptionViewSource.Should().Contain("@T.T(\"BusinessSubscriptionOpenInvoiceQueue\")");
        subscriptionViewSource.Should().Contain("@Url.Action(\"Payments\", \"Billing\", new { businessId = Model.Business.Id })");
        subscriptionViewSource.Should().Contain("@T.T(\"BusinessSubscriptionNoActivePlans\")");
        subscriptionViewSource.Should().Contain("@T.T(\"BusinessSubscriptionResolvePrerequisites\")");
        subscriptionViewSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
    }

    [Fact]
    public void BusinessMembersWorkspace_Should_KeepSummaryAndPlaybookOpsContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var membersViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Members.cshtml"));

        controllerSource.Should().Contain("Summary = await BuildBusinessMemberOpsSummaryAsync(businessId, ct).ConfigureAwait(false)");
        controllerSource.Should().Contain("Playbooks = BuildBusinessMemberPlaybooks(businessId)");
        controllerSource.Should().Contain("private async Task<BusinessMemberOpsSummaryVm> BuildBusinessMemberOpsSummaryAsync(Guid businessId, CancellationToken ct)");
        controllerSource.Should().Contain("new SelectListItem(T(\"BusinessMembersAllLabel\"), BusinessMemberSupportFilter.All.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"NeedsAttention\"), BusinessMemberSupportFilter.Attention.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"PendingActivation\"), BusinessMemberSupportFilter.PendingActivation.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"BusinessMembersLockedLabel\"), BusinessMemberSupportFilter.Locked.ToString()");
        controllerSource.Should().Contain("BusinessMemberSupportFilter.PendingActivation");
        controllerSource.Should().Contain("BusinessMemberSupportFilter.Locked");
        controllerSource.Should().Contain("BusinessMemberSupportFilter.Attention");
        controllerSource.Should().Contain("private List<BusinessMemberPlaybookVm> BuildBusinessMemberPlaybooks(Guid businessId)");
        controllerSource.Should().Contain("QueueActionLabel = T(\"PendingActivation\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"MobileOperationsTitle\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"UsersFilterLocked\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"UsersFilterLocked\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"NeedsAttention\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"OwnerOverrideAuditTitle\")");

        membersViewSource.Should().Contain("Model.Summary.TotalCount");
        membersViewSource.Should().Contain("Model.Summary.PendingActivationCount");
        membersViewSource.Should().Contain("Model.Summary.LockedCount");
        membersViewSource.Should().Contain("Model.Summary.AttentionCount");
        membersViewSource.Should().Contain("BusinessesOperationsPlaybooksTitle");
        membersViewSource.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        membersViewSource.Should().Contain("hx-target=\"#business-members-workspace-shell\"");
        membersViewSource.Should().Contain("href=\"@playbook.QueueActionUrl\"");
        membersViewSource.Should().Contain(">@playbook.WhyItMatters</a>");
        membersViewSource.Should().Contain(">@playbook.QueueLabel</a>");
        membersViewSource.Should().Contain(">@playbook.OperatorAction</a>");
        membersViewSource.Should().Contain(">@playbook.QueueActionLabel</a>");
        membersViewSource.Should().Contain(">@playbook.FollowUpLabel</a>");
        membersViewSource.Should().Contain("BusinessMemberSupportFilter.PendingActivation");
        membersViewSource.Should().Contain("BusinessMemberSupportFilter.Locked");
        membersViewSource.Should().Contain("BusinessMemberSupportFilter.Attention");
        membersViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        membersViewSource.Should().Contain("string MemberConfirmationStatusLabel(bool emailConfirmed) => emailConfirmed");
        membersViewSource.Should().Contain("string MemberActiveStatusLabel(bool isActive) => isActive ? T.T(\"Yes\") : T.T(\"No\")");
        membersViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        membersViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        membersViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)");
        membersViewSource.Should().Contain("@T.T(\"BusinessMembersNoActiveOwnerWarning\")");
        membersViewSource.Should().Contain("@Url.Action(\"OwnerOverrideAudits\", \"Businesses\", new { businessId = Model.Business.Id })");
          membersViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
          membersViewSource.Should().Contain("@Url.Action(\"Setup\", \"Businesses\", new { id = Model.Business.Id })");
          membersViewSource.Should().Contain("@T.T(\"BusinessMembersPendingActivationNote\")");
          membersViewSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Business.Id, filter = Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation })");
          membersViewSource.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\", flowKey = \"AccountActivation\" })");
          membersViewSource.Should().Contain("@T.T(\"OpenFailedActivationEmails\")");
          membersViewSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
          membersViewSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
          membersViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
          membersViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
          membersViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterUnconfirmed\")</a>");
          membersViewSource.Should().NotContain("<div class=\"text-muted small text-uppercase\">@T.T(\"NeedsAttention\")</div>");
          membersViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"NeedsAttention\")</a>");
          membersViewSource.Should().NotContain("<span class=\"badge text-bg-warning\">@T.T(\"PendingActivation\")</span>");
          membersViewSource.Should().NotContain("<span class=\"badge text-bg-danger ms-1\">@T.T(\"Locked\")</span>");
          membersViewSource.Should().NotContain("<span class=\"badge text-bg-success\">@T.T(\"Confirmed\")</span>");
          membersViewSource.Should().NotContain("<span class=\"badge text-bg-success\">@T.T(\"Yes\")</span>");
          membersViewSource.Should().NotContain("<span class=\"badge text-bg-secondary\">@T.T(\"No\")</span>");
      }

    [Fact]
    public void BusinessInvitationsWorkspace_Should_KeepSummaryAndPlaybookOpsContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var invitationsViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Invitations.cshtml"));

        controllerSource.Should().Contain("Summary = await BuildBusinessInvitationOpsSummaryAsync(businessId, ct).ConfigureAwait(false)");
        controllerSource.Should().Contain("Playbooks = BuildBusinessInvitationPlaybooks(businessId)");
        controllerSource.Should().Contain("private async Task<BusinessInvitationOpsSummaryVm> BuildBusinessInvitationOpsSummaryAsync(Guid businessId, CancellationToken ct)");
        controllerSource.Should().Contain("private string DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter filter)");
        controllerSource.Should().Contain("BusinessInvitationQueueFilter.All => T(\"BusinessInvitationsAllLabel\")");
        controllerSource.Should().Contain("BusinessInvitationQueueFilter.Open => T(\"OpenInvitations\")");
        controllerSource.Should().Contain("BusinessInvitationQueueFilter.Pending => T(\"Pending\")");
        controllerSource.Should().Contain("BusinessInvitationQueueFilter.Expired => T(\"Expired\")");
        controllerSource.Should().Contain("BusinessInvitationQueueFilter.Accepted => T(\"Accepted\")");
        controllerSource.Should().Contain("BusinessInvitationQueueFilter.Revoked => T(\"Revoked\")");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.All), BusinessInvitationQueueFilter.All.ToString()");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Open), BusinessInvitationQueueFilter.Open.ToString()");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Pending), BusinessInvitationQueueFilter.Pending.ToString()");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Expired), BusinessInvitationQueueFilter.Expired.ToString()");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Accepted), BusinessInvitationQueueFilter.Accepted.ToString()");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Revoked), BusinessInvitationQueueFilter.Revoked.ToString()");
        controllerSource.Should().NotContain("new SelectListItem(T(\"BusinessInvitationsAllLabel\"), BusinessInvitationQueueFilter.All.ToString()");
        controllerSource.Should().NotContain("new SelectListItem(T(\"OpenInvitations\"), BusinessInvitationQueueFilter.Open.ToString()");
        controllerSource.Should().NotContain("new SelectListItem(T(\"Pending\"), BusinessInvitationQueueFilter.Pending.ToString()");
        controllerSource.Should().NotContain("new SelectListItem(T(\"Expired\"), BusinessInvitationQueueFilter.Expired.ToString()");
        controllerSource.Should().NotContain("new SelectListItem(T(\"Accepted\"), BusinessInvitationQueueFilter.Accepted.ToString()");
        controllerSource.Should().NotContain("new SelectListItem(T(\"Revoked\"), BusinessInvitationQueueFilter.Revoked.ToString()");
        controllerSource.Should().Contain("BusinessInvitationQueueFilter.Open");
        controllerSource.Should().Contain("BusinessInvitationQueueFilter.Pending");
        controllerSource.Should().Contain("BusinessInvitationQueueFilter.Expired");
        controllerSource.Should().Contain("private List<BusinessInvitationPlaybookVm> BuildBusinessInvitationPlaybooks(Guid businessId)");
        controllerSource.Should().Contain("QueueActionLabel = T(\"OpenInvitations\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessInvitationsPlaybookOpenWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessInvitationsPlaybookOpenAction\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"FailedInvitations\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"Pending\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessInvitationsPlaybookPendingWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessInvitationsPlaybookPendingAction\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"BusinessSupportQueueTitle\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"Expired\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessInvitationsPlaybookExpiredWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessInvitationsPlaybookExpiredAction\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"BusinessInvitationsInviteUserAction\")");

        invitationsViewSource.Should().Contain("Model.Summary.TotalCount");
        invitationsViewSource.Should().Contain("Model.Summary.OpenCount");
        invitationsViewSource.Should().Contain("Model.Summary.PendingCount");
        invitationsViewSource.Should().Contain("Model.Summary.ExpiredCount");
        invitationsViewSource.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        invitationsViewSource.Should().Contain("BusinessInvitationQueueFilter.Open => T.T(\"OpenInvitations\")");
        invitationsViewSource.Should().Contain("BusinessInvitationQueueFilter.Pending => T.T(\"Pending\")");
        invitationsViewSource.Should().Contain("BusinessInvitationQueueFilter.Expired => T.T(\"Expired\")");
        invitationsViewSource.Should().Contain("string InvitationStatusLabel(Darwin.Domain.Enums.BusinessInvitationStatus status) => status switch");
        invitationsViewSource.Should().Contain("BusinessInvitationStatus.Accepted => T.T(\"Accepted\")");
        invitationsViewSource.Should().Contain("BusinessInvitationStatus.Revoked => T.T(\"Revoked\")");
        invitationsViewSource.Should().Contain("BusinessesOperationsPlaybooksTitle");
        invitationsViewSource.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        invitationsViewSource.Should().Contain("hx-target=\"#business-invitations-workspace-shell\"");
        invitationsViewSource.Should().Contain("href=\"@playbook.QueueActionUrl\"");
        invitationsViewSource.Should().Contain(">@playbook.WhyItMatters</a>");
        invitationsViewSource.Should().Contain(">@playbook.QueueLabel</a>");
        invitationsViewSource.Should().Contain(">@playbook.OperatorAction</a>");
        invitationsViewSource.Should().Contain(">@playbook.QueueActionLabel</a>");
        invitationsViewSource.Should().Contain(">@playbook.FollowUpLabel</a>");
        invitationsViewSource.Should().Contain("BusinessInvitationQueueFilter.Open");
        invitationsViewSource.Should().Contain("BusinessInvitationQueueFilter.Pending");
        invitationsViewSource.Should().Contain("BusinessInvitationQueueFilter.Expired");
        invitationsViewSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Open)");
        invitationsViewSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        invitationsViewSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Expired)");
        invitationsViewSource.Should().Contain("@InvitationStatusLabel(item.Status)");
        invitationsViewSource.Should().NotContain("@T.T(\"Pending\")</a>");
        invitationsViewSource.Should().NotContain("@T.T(\"Expired\")</a>");
        invitationsViewSource.Should().NotContain("@T.T(\"Pending\")</span>");
        invitationsViewSource.Should().NotContain("@T.T(\"Accepted\")</span>");
        invitationsViewSource.Should().NotContain("@T.T(\"Revoked\")</span>");
        invitationsViewSource.Should().NotContain("@T.T(\"Expired\")</span>");
          invitationsViewSource.Should().Contain("@T.T(\"BusinessInvitationsEmptyState\")");
          invitationsViewSource.Should().Contain("@Url.Action(\"CreateInvitation\", \"Businesses\", new { businessId = Model.Business.Id })");
          invitationsViewSource.Should().Contain("@Url.Action(\"Setup\", \"Businesses\", new { id = Model.Business.Id })");
          invitationsViewSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
          invitationsViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
          invitationsViewSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
          invitationsViewSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
      }

    [Fact]
    public void BusinessOwnerOverrideAuditsWorkspace_Should_KeepPlaybookAndDrillInContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var auditsViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "OwnerOverrideAudits.cshtml"));

        controllerSource.Should().Contain("Playbooks = BuildBusinessOwnerOverrideAuditPlaybooks(businessId)");
        controllerSource.Should().Contain("private List<BusinessOwnerOverrideAuditPlaybookVm> BuildBusinessOwnerOverrideAuditPlaybooks(Guid businessId)");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessOwnerOverrideForceRemove\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessOwnerOverridePlaybookForceRemoveWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessOwnerOverridePlaybookForceRemoveAction\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Members\", \"Businesses\", new { businessId, filter = BusinessMemberSupportFilter.Attention }) ?? string.Empty");
        controllerSource.Should().Contain("FollowUpLabel = T(\"BusinessSupportQueueTitle\")");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessOwnerOverrideDemoteDeactivate\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessOwnerOverridePlaybookDemoteWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessOwnerOverridePlaybookDemoteAction\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"MerchantReadinessTitle\")");
        controllerSource.Should().Contain("QueueLabel = T(\"MissingActiveOwner\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessOwnerOverridePlaybookMissingOwnerWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessOwnerOverridePlaybookMissingOwnerAction\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"CommonSetup\")");

        auditsViewSource.Should().Contain("BusinessesOperationsPlaybooksTitle");
        auditsViewSource.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        auditsViewSource.Should().Contain("hx-target=\"#business-owner-override-audits-workspace-shell\"");
        auditsViewSource.Should().Contain("href=\"@playbook.QueueActionUrl\"");
        auditsViewSource.Should().Contain(">@playbook.WhyItMatters</a>");
        auditsViewSource.Should().Contain(">@playbook.QueueLabel</a>");
        auditsViewSource.Should().Contain(">@playbook.OperatorAction</a>");
        auditsViewSource.Should().Contain(">@playbook.QueueActionLabel</a>");
        auditsViewSource.Should().Contain(">@playbook.FollowUpLabel</a>");
        auditsViewSource.Should().Contain("OpenUserAction");
        auditsViewSource.Should().Contain("CommonMembers");
        auditsViewSource.Should().Contain("@T.T(\"BusinessOwnerOverrideAuditsIntro\")");
        auditsViewSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        auditsViewSource.Should().Contain("@T.T(\"BusinessOwnerOverrideAuditsEmptyState\")");
        auditsViewSource.Should().Contain("@Url.Action(\"Setup\", \"Businesses\", new { id = Model.Business.Id })");
        auditsViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        auditsViewSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
        auditsViewSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
    }

    [Fact]
    public void UsersController_Should_KeepIdentityAndAddressMutationsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "UsersController.cs"));

        source.Should().Contain("[ValidateAntiForgeryToken]");
        source.Should().Contain("public async Task<IActionResult> Create(UserCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> Edit(UserEditVm vm");
        source.Should().Contain("public async Task<IActionResult> ChangeEmail(UserChangeEmailVm vm");
        source.Should().Contain("public async Task<IActionResult> ConfirmEmail(");
        source.Should().Contain("public async Task<IActionResult> SendActivationEmail(");
        source.Should().Contain("public async Task<IActionResult> SendPasswordReset(");
        source.Should().Contain("public async Task<IActionResult> ChangePassword(UserChangePasswordVm vm");
        source.Should().Contain("public async Task<IActionResult> Lock(");
        source.Should().Contain("public async Task<IActionResult> Unlock(");
        source.Should().Contain("public async Task<IActionResult> Delete(");
        source.Should().Contain("public async Task<IActionResult> Roles(UserRolesEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateAddress(");
        source.Should().Contain("public async Task<IActionResult> EditAddress(");
        source.Should().Contain("public async Task<IActionResult> DeleteAddress(");
        source.Should().Contain("public async Task<IActionResult> SetDefaultAddress(");
    }

    [Fact]
    public void UsersController_Should_KeepAdminWorkspacesAndEditorGetsReachable()
    {
        var usersSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "UsersController.cs"));
        var adminBaseSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));

        adminBaseSource.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");
        usersSource.Should().Contain("public sealed class UsersController : AdminBaseController");
        usersSource.Should().Contain("public async Task<IActionResult> Index(");
        usersSource.Should().Contain("public IActionResult Create() => RenderCreateEditor(new UserCreateVm())");
        usersSource.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)");
        usersSource.Should().Contain("public IActionResult ChangeEmail([FromRoute] Guid id, [FromQuery] string? currentEmail = null)");
        usersSource.Should().Contain("public IActionResult ChangePassword(Guid id, string? email = null)");
        usersSource.Should().Contain("public async Task<IActionResult> AddressesSection(Guid userId, CancellationToken ct = default)");
        usersSource.Should().Contain("public async Task<IActionResult> Roles(Guid id, bool returnToIndex = false, string? q = null, UserQueueFilter filter = UserQueueFilter.All, int page = 1, int pageSize = 20, CancellationToken ct = default)");
    }

    [Fact]
    public void UsersController_Should_KeepIdentitySupportAuditAndPlaybookContracts()
    {
        var usersSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "UsersController.cs"));

        usersSource.Should().Contain("private readonly GetEmailDispatchAuditsPageHandler _getEmailDispatchAuditsPage;");
        usersSource.Should().Contain("GetEmailDispatchAuditsPageHandler getEmailDispatchAuditsPage");
        usersSource.Should().Contain("_getEmailDispatchAuditsPage = getEmailDispatchAuditsPage;");
        usersSource.Should().Contain("var auditSummary = await _getEmailDispatchAuditsPage.GetSummaryAsync(null, ct);");
        usersSource.Should().Contain("FailedActivationEmailCount = auditSummary.FailedActivationCount");
        usersSource.Should().Contain("FailedPasswordResetEmailCount = auditSummary.FailedPasswordResetCount");
        usersSource.Should().Contain("QueueFilter = UserQueueFilter.Unconfirmed");
        usersSource.Should().Contain("AuditFlowKey = \"AccountActivation\"");
        usersSource.Should().Contain("QueueFilter = UserQueueFilter.Locked");
        usersSource.Should().Contain("AuditFlowKey = \"PasswordReset\"");
        usersSource.Should().Contain("QueueFilter = UserQueueFilter.MobileLinked");
        usersSource.Should().Contain("OpensMobileOperations = true");
    }

    [Fact]
    public void AccountController_Should_KeepAnonymousAuthEntryPointsAndProtectedLogout()
    {
        var accountSource = ReadWebAdminFile(Path.Combine("Controllers", "AccountController.cs"));

        accountSource.Should().Contain("public sealed class AccountController : Controller");
        accountSource.Should().Contain("[AllowAnonymous]");
        accountSource.Should().Contain("[HttpGet(\"/account/login\")]");
        accountSource.Should().Contain("public IActionResult Login(string? returnUrl = null)");
        accountSource.Should().Contain("[HttpPost(\"/account/login\")]");
        accountSource.Should().Contain("public async Task<IActionResult> LoginPost(");
        accountSource.Should().Contain("[HttpGet(\"/account/login-2fa\")]");
        accountSource.Should().Contain("public IActionResult LoginTwoFactor()");
        accountSource.Should().Contain("[HttpPost(\"/account/login-2fa\")]");
        accountSource.Should().Contain("public async Task<IActionResult> LoginTwoFactorPost(");
        accountSource.Should().Contain("[HttpPost(\"/account/webauthn/begin-login\")]");
        accountSource.Should().Contain("public async Task<IActionResult> WebAuthnBeginLogin(");
        accountSource.Should().Contain("[HttpPost(\"/account/webauthn/finish-login\")]");
        accountSource.Should().Contain("public async Task<IActionResult> WebAuthnFinishLogin(");
        accountSource.Should().Contain("[HttpGet(\"/account/register\")]");
        accountSource.Should().Contain("public IActionResult Register(string? returnUrl = null)");
        accountSource.Should().Contain("[HttpPost(\"/account/register\")]");
        accountSource.Should().Contain("public async Task<IActionResult> RegisterPost(");
        accountSource.Should().Contain("[Authorize]");
        accountSource.Should().Contain("public async Task<IActionResult> Logout(CancellationToken ct = default)");
        accountSource.Should().Contain("[ValidateAntiForgeryToken]");
    }

    [Fact]
    public void HomeDashboardAndCultureControllers_Should_KeepAdminAndLocalizationBoundaries()
    {
        var adminBaseSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));
        var homeSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Home", "HomeController.cs"));
        var dashboardSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Home", "DashboardController.cs"));
        var cultureSource = ReadWebAdminFile(Path.Combine("Controllers", "CultureController.cs"));

        adminBaseSource.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");

        homeSource.Should().Contain("public sealed class HomeController : AdminBaseController");
        homeSource.Should().Contain("public async Task<IActionResult> Index(Guid? businessId = null, CancellationToken ct = default)");
        homeSource.Should().Contain("public async Task<IActionResult> CommunicationOpsFragment(Guid? businessId = null, CancellationToken ct = default)");
        homeSource.Should().Contain("public async Task<IActionResult> BusinessSupportQueueFragment(Guid? businessId = null, CancellationToken ct = default)");
        homeSource.Should().Contain("public IActionResult AlertsFragment()");

        dashboardSource.Should().Contain("public sealed class DashboardController : AdminBaseController");
        dashboardSource.Should().Contain("[HttpGet(\"/admin\")]");
        dashboardSource.Should().Contain("[HttpGet(\"/dashboard\")]");
        dashboardSource.Should().Contain("public IActionResult Index()");

        cultureSource.Should().Contain("public sealed class CultureController : Controller");
        cultureSource.Should().Contain("[HttpPost]");
        cultureSource.Should().Contain("[ValidateAntiForgeryToken]");
        cultureSource.Should().Contain("public IActionResult SetCulture(string culture, string? returnUrl = null)");
        cultureSource.Should().Contain("AdminCultureCatalog.NormalizeUiCulture(culture)");
        cultureSource.Should().Contain("CookieRequestCultureProvider.DefaultCookieName");
        cultureSource.Should().Contain("Url.IsLocalUrl(returnUrl)");
        cultureSource.Should().Contain("return LocalRedirect(returnUrl);");
        cultureSource.Should().Contain("return RedirectToAction(\"Index\", \"Home\");");
    }

    [Fact]
    public void LocalizationDefaults_Should_StayCentralizedAcross_Settings_And_AdminCultureCatalog()
    {
        var siteSettingDtoSource = ReadApplicationFile(Path.Combine("Settings", "DTOs", "SiteSettingDto.cs"));
        var domainDefaultsSource = ReadDomainFile(Path.Combine("Common", "DomainDefaults.cs"));
        var adminCultureCatalogSource = ReadWebAdminFile(Path.Combine("Localization", "AdminCultureCatalog.cs"));

        domainDefaultsSource.Should().Contain("public const string DefaultCulture = \"de-DE\";");
        domainDefaultsSource.Should().Contain("public const string DefaultTimezone = \"Europe/Berlin\";");
        domainDefaultsSource.Should().Contain("public const string DefaultCurrency = \"EUR\";");
        domainDefaultsSource.Should().Contain("public const string DefaultCountryCode = \"DE\";");
        domainDefaultsSource.Should().Contain("public const string SupportedCulturesCsv = \"de-DE,en-US\";");

        siteSettingDtoSource.Should().Contain("using Darwin.Domain.Common;");
        siteSettingDtoSource.Should().Contain("public const string DefaultCultureDefault = DomainDefaults.DefaultCulture;");
        siteSettingDtoSource.Should().Contain("public const string SupportedCulturesCsvDefault = DomainDefaults.SupportedCulturesCsv;");
        siteSettingDtoSource.Should().Contain("public const string DefaultCountryDefault = DomainDefaults.DefaultCountryCode;");
        siteSettingDtoSource.Should().Contain("public const string DefaultCurrencyDefault = DomainDefaults.DefaultCurrency;");
        siteSettingDtoSource.Should().Contain("public const string TimeZoneDefault = DomainDefaults.DefaultTimezone;");

        adminCultureCatalogSource.Should().Contain("using Darwin.Application.Settings.DTOs;");
        adminCultureCatalogSource.Should().Contain("public const string German = SiteSettingDto.DefaultCultureDefault;");
        adminCultureCatalogSource.Should().Contain("public const string SupportedCulturesCsvDefault = SiteSettingDto.SupportedCulturesCsvDefault;");
        adminCultureCatalogSource.Should().Contain("return DefaultCulture;");
    }

    [Fact]
    public void InfrastructureSeeds_Should_KeepUsing_SharedDomainDefaults_For_LocalizationAndCurrency()
    {
        var identitySeedSource = ReadInfrastructureFile(Path.Combine("Persistence", "Seed", "Sections", "IdentitySeedSection.cs"));
        var businessesSeedSource = ReadInfrastructureFile(Path.Combine("Persistence", "Seed", "Sections", "BusinessesSeedSection.cs"));
        var shippingSeedSource = ReadInfrastructureFile(Path.Combine("Persistence", "Seed", "Sections", "ShippingSeedSection.cs"));
        var siteSettingsSeedSource = ReadInfrastructureFile(Path.Combine("Persistence", "Seed", "Sections", "SiteSettingsSeedSection.cs"));
        var billingSeedSource = ReadInfrastructureFile(Path.Combine("Persistence", "Seed", "Sections", "BillingSeedSection.cs"));
        var cartSeedSource = ReadInfrastructureFile(Path.Combine("Persistence", "Seed", "Sections", "CartSeedSection.cs"));
        var catalogSeedSource = ReadInfrastructureFile(Path.Combine("Persistence", "Seed", "Sections", "CatalogSeedSection.cs"));
        var crmSeedSource = ReadInfrastructureFile(Path.Combine("Persistence", "Seed", "Sections", "CrmSeedSection.cs"));
        var ordersSeedSource = ReadInfrastructureFile(Path.Combine("Persistence", "Seed", "Sections", "OrdersSeedSection.cs"));
        var pricingSeedSource = ReadInfrastructureFile(Path.Combine("Persistence", "Seed", "Sections", "PricingSeedSection.cs"));
        var cmsSeedSource = ReadInfrastructureFile(Path.Combine("Persistence", "Seed", "Sections", "CmsSeedSection.cs"));

        identitySeedSource.Should().Contain("using Darwin.Domain.Common;");
        identitySeedSource.Should().Contain("Locale = DomainDefaults.DefaultCulture");
        identitySeedSource.Should().Contain("Timezone = DomainDefaults.DefaultTimezone");
        identitySeedSource.Should().Contain("Currency = DomainDefaults.DefaultCurrency");
        identitySeedSource.Should().Contain("CountryCode = DomainDefaults.DefaultCountryCode");

        businessesSeedSource.Should().Contain("DefaultCurrency = DomainDefaults.DefaultCurrency");
        businessesSeedSource.Should().Contain("DefaultCulture = DomainDefaults.DefaultCulture");
        businessesSeedSource.Should().Contain("CountryCode = DomainDefaults.DefaultCountryCode");

        shippingSeedSource.Should().Contain("using Darwin.Domain.Common;");
        shippingSeedSource.Should().Contain("CountriesCsv = DomainDefaults.DefaultCountryCode");
        shippingSeedSource.Should().Contain("Currency = DomainDefaults.DefaultCurrency");

        siteSettingsSeedSource.Should().Contain("using Darwin.Domain.Common;");
        siteSettingsSeedSource.Should().Contain("DefaultCulture = DomainDefaults.DefaultCulture");
        siteSettingsSeedSource.Should().Contain("SupportedCulturesCsv = DomainDefaults.SupportedCulturesCsv");
        siteSettingsSeedSource.Should().Contain("DefaultCountry = DomainDefaults.DefaultCountryCode");
        siteSettingsSeedSource.Should().Contain("DefaultCurrency = DomainDefaults.DefaultCurrency");
        siteSettingsSeedSource.Should().Contain("TimeZone = DomainDefaults.DefaultTimezone");
        siteSettingsSeedSource.Should().Contain("InvoiceIssuerCountry = DomainDefaults.DefaultCountryCode");
        siteSettingsSeedSource.Should().Contain("DhlShipperCountry = DomainDefaults.DefaultCountryCode");

        billingSeedSource.Should().Contain("using Darwin.Domain.Common;");
        billingSeedSource.Should().Contain("Currency = DomainDefaults.DefaultCurrency");

        cartSeedSource.Should().Contain("using Darwin.Domain.Common;");
        cartSeedSource.Should().Contain("Currency = DomainDefaults.DefaultCurrency");

        catalogSeedSource.Should().Contain("using Darwin.Domain.Common;");
        catalogSeedSource.Should().Contain("Culture = DomainDefaults.DefaultCulture");
        catalogSeedSource.Should().Contain("Money.FromMajor(it.Price, DomainDefaults.DefaultCurrency)");
        catalogSeedSource.Should().Contain("Currency = DomainDefaults.DefaultCurrency");
        catalogSeedSource.Should().Contain("PriceDeltaMinor = Money.FromMajor(0m, DomainDefaults.DefaultCurrency).AmountMinor");

        cmsSeedSource.Should().Contain("using Darwin.Domain.Common;");
        cmsSeedSource.Should().Contain("Culture = DomainDefaults.DefaultCulture");
        cmsSeedSource.Should().Contain("t.Culture == DomainDefaults.DefaultCulture");
        cmsSeedSource.Should().Contain("new PageTranslation { Culture = DomainDefaults.DefaultCulture }");

        crmSeedSource.Should().Contain("using Darwin.Domain.Common;");
        crmSeedSource.Should().Contain("Country = DomainDefaults.DefaultCountryCode");
        crmSeedSource.Should().Contain("Currency = DomainDefaults.DefaultCurrency");

        ordersSeedSource.Should().Contain("using Darwin.Domain.Common;");
        ordersSeedSource.Should().Contain("Currency = DomainDefaults.DefaultCurrency");

        pricingSeedSource.Should().Contain("using Darwin.Domain.Common;");
        pricingSeedSource.Should().Contain("Currency = DomainDefaults.DefaultCurrency");
    }

    [Fact]
    public void OrdersController_Should_KeepOperationalMutationsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Orders", "OrdersController.cs"));

        source.Should().Contain("[ValidateAntiForgeryToken]");
        source.Should().Contain("public async Task<IActionResult> AddPayment(PaymentCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> AddShipment(ShipmentCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> AddRefund(RefundCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateInvoice(OrderInvoiceCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> ChangeStatus(OrderStatusChangeVm vm");
    }

    [Fact]
    public void OrdersController_Should_KeepAdminOrderWorkspacesAndReviewEndpointsReachable()
    {
        var ordersSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Orders", "OrdersController.cs"));
        var adminBaseSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));

        adminBaseSource.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");
        ordersSource.Should().Contain("public sealed class OrdersController : AdminBaseController");
        ordersSource.Should().Contain("public async Task<IActionResult> Index(");
        ordersSource.Should().Contain("public async Task<IActionResult> ShipmentsQueue(");
        ordersSource.Should().Contain("public async Task<IActionResult> ReturnsQueue(");
        ordersSource.Should().Contain("public async Task<IActionResult> Details(Guid id");
        ordersSource.Should().Contain("public async Task<IActionResult> Payments(Guid orderId");
        ordersSource.Should().Contain("public async Task<IActionResult> Shipments(Guid orderId");
        ordersSource.Should().Contain("public async Task<IActionResult> Refunds(Guid orderId");
        ordersSource.Should().Contain("public async Task<IActionResult> Invoices(Guid orderId");
        ordersSource.Should().Contain("public async Task<IActionResult> AddPayment(Guid orderId");
        ordersSource.Should().Contain("public async Task<IActionResult> AddShipment(Guid orderId");
        ordersSource.Should().Contain("public async Task<IActionResult> AddRefund(Guid orderId");
        ordersSource.Should().Contain("public async Task<IActionResult> CreateInvoice(Guid orderId");
    }

    [Fact]
    public void InventoryController_Should_KeepOperationalMutationsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Inventory", "InventoryController.cs"));

        source.Should().Contain("[ValidateAntiForgeryToken]");
        source.Should().Contain("public async Task<IActionResult> CreateWarehouse(WarehouseEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditWarehouse(WarehouseEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateSupplier(SupplierEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditSupplier(SupplierEditVm vm");
        source.Should().Contain("public async Task<IActionResult> AdjustStock(InventoryAdjustActionVm vm");
        source.Should().Contain("public async Task<IActionResult> ReserveStock(InventoryReserveActionVm vm");
        source.Should().Contain("public async Task<IActionResult> ReleaseReservation(InventoryReleaseReservationActionVm vm");
        source.Should().Contain("public async Task<IActionResult> ReturnReceipt(InventoryReturnReceiptActionVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateStockLevel(StockLevelEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditStockLevel(StockLevelEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateStockTransfer(StockTransferEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditStockTransfer(StockTransferEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CreatePurchaseOrder(PurchaseOrderEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditPurchaseOrder(PurchaseOrderEditVm vm");
    }

    [Fact]
    public void InventoryController_Should_KeepAdminWorkspacesAndEditorGetsReachable()
    {
        var inventorySource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Inventory", "InventoryController.cs"));
        var adminBaseSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));

        adminBaseSource.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");
        inventorySource.Should().Contain("public sealed class InventoryController : AdminBaseController");
        inventorySource.Should().Contain("public IActionResult Index() => RedirectOrHtmx(nameof(Warehouses), new { });");
        inventorySource.Should().Contain("public async Task<IActionResult> Warehouses(");
        inventorySource.Should().Contain("public async Task<IActionResult> Suppliers(");
        inventorySource.Should().Contain("public async Task<IActionResult> StockLevels(");
        inventorySource.Should().Contain("public async Task<IActionResult> StockTransfers(");
        inventorySource.Should().Contain("public async Task<IActionResult> PurchaseOrders(");
        inventorySource.Should().Contain("public async Task<IActionResult> VariantLedger(");
        inventorySource.Should().Contain("public async Task<IActionResult> CreateWarehouse(Guid? businessId = null");
        inventorySource.Should().Contain("public async Task<IActionResult> EditWarehouse(Guid id");
        inventorySource.Should().Contain("public async Task<IActionResult> CreateSupplier(Guid? businessId = null");
        inventorySource.Should().Contain("public async Task<IActionResult> EditSupplier(Guid id");
        inventorySource.Should().Contain("public async Task<IActionResult> AdjustStock(Guid stockLevelId");
        inventorySource.Should().Contain("public async Task<IActionResult> ReserveStock(Guid stockLevelId");
        inventorySource.Should().Contain("public async Task<IActionResult> ReleaseReservation(Guid stockLevelId");
        inventorySource.Should().Contain("public async Task<IActionResult> ReturnReceipt(Guid stockLevelId");
        inventorySource.Should().Contain("public async Task<IActionResult> CreateStockLevel(Guid? businessId = null, Guid? warehouseId = null");
        inventorySource.Should().Contain("public async Task<IActionResult> EditStockLevel(Guid id");
        inventorySource.Should().Contain("public async Task<IActionResult> CreateStockTransfer(Guid? businessId = null, Guid? warehouseId = null");
        inventorySource.Should().Contain("public async Task<IActionResult> EditStockTransfer(Guid id");
        inventorySource.Should().Contain("public async Task<IActionResult> CreatePurchaseOrder(Guid? businessId = null");
        inventorySource.Should().Contain("public async Task<IActionResult> EditPurchaseOrder(Guid id");
    }

    [Fact]
    public void CrmController_Should_KeepOperationalMutationsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Crm", "CrmController.cs"));

        source.Should().Contain("[ValidateAntiForgeryToken]");
        source.Should().Contain("public async Task<IActionResult> CreateCustomer(CustomerEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditCustomer(CustomerEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditInvoice(InvoiceEditVm vm");
        source.Should().Contain("public async Task<IActionResult> TransitionInvoiceStatus(InvoiceStatusTransitionVm vm");
        source.Should().Contain("public async Task<IActionResult> RefundInvoice(InvoiceRefundCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateLead(LeadEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditLead(LeadEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateOpportunity(OpportunityEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditOpportunity(OpportunityEditVm vm");
        source.Should().Contain("public async Task<IActionResult> ConvertLead(ConvertLeadVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateSegment(CustomerSegmentEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditSegment(CustomerSegmentEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CustomerInteractions(InteractionCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> LeadInteractions(InteractionCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> OpportunityInteractions(InteractionCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> CustomerConsents(ConsentCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> CustomerSegmentMemberships(AssignCustomerSegmentVm vm");
        source.Should().Contain("public async Task<IActionResult> RemoveCustomerSegmentMembership(");
    }

    [Fact]
    public void CrmController_Should_KeepAdminWorkspacesAndReviewGetsReachable()
    {
        var crmSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Crm", "CrmController.cs"));
        var adminBaseSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));

        adminBaseSource.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");
        crmSource.Should().Contain("public sealed class CrmController : AdminBaseController");
        crmSource.Should().Contain("public async Task<IActionResult> Index(");
        crmSource.Should().Contain("public async Task<IActionResult> Customers(");
        crmSource.Should().Contain("public async Task<IActionResult> Invoices(");
        crmSource.Should().Contain("public async Task<IActionResult> Leads(");
        crmSource.Should().Contain("public async Task<IActionResult> Opportunities(");
        crmSource.Should().Contain("public async Task<IActionResult> Segments(");
        crmSource.Should().Contain("public async Task<IActionResult> CreateCustomer(CancellationToken ct = default)");
        crmSource.Should().Contain("public async Task<IActionResult> EditCustomer(Guid id");
        crmSource.Should().Contain("public async Task<IActionResult> EditInvoice(Guid id");
        crmSource.Should().Contain("public async Task<IActionResult> CreateLead(CancellationToken ct = default)");
        crmSource.Should().Contain("public async Task<IActionResult> EditLead(Guid id");
        crmSource.Should().Contain("public async Task<IActionResult> CreateOpportunity(Guid? customerId = null");
        crmSource.Should().Contain("public async Task<IActionResult> EditOpportunity(Guid id");
        crmSource.Should().Contain("public IActionResult CreateSegment() => RenderSegmentEditor(new CustomerSegmentEditVm(), nameof(CreateSegment));");
        crmSource.Should().Contain("public async Task<IActionResult> EditSegment(Guid id");
        crmSource.Should().Contain("public async Task<IActionResult> CustomerInteractions(Guid customerId");
        crmSource.Should().Contain("public async Task<IActionResult> LeadInteractions(Guid leadId");
        crmSource.Should().Contain("public async Task<IActionResult> OpportunityInteractions(Guid opportunityId");
        crmSource.Should().Contain("public async Task<IActionResult> CustomerConsents(Guid customerId");
        crmSource.Should().Contain("public async Task<IActionResult> CustomerSegmentMemberships(Guid customerId");
    }

    [Fact]
    public void LoyaltyController_Should_KeepOperationalMutationsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Loyalty", "LoyaltyController.cs"));

        source.Should().Contain("[ValidateAntiForgeryToken]");
        source.Should().Contain("public async Task<IActionResult> CreateProgram(LoyaltyProgramEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditProgram(LoyaltyProgramEditVm vm");
        source.Should().Contain("public async Task<IActionResult> DeleteProgram(");
        source.Should().Contain("public async Task<IActionResult> CreateRewardTier(LoyaltyRewardTierEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditRewardTier(LoyaltyRewardTierEditVm vm");
        source.Should().Contain("public async Task<IActionResult> DeleteRewardTier(");
        source.Should().Contain("public async Task<IActionResult> CreateAccount(CreateLoyaltyAccountVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateCampaign(LoyaltyCampaignEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditCampaign(LoyaltyCampaignEditVm vm");
        source.Should().Contain("public async Task<IActionResult> SetCampaignActivation(");
        source.Should().Contain("public async Task<IActionResult> AdjustPoints(AdjustLoyaltyPointsVm vm");
        source.Should().Contain("public async Task<IActionResult> SuspendAccount(");
        source.Should().Contain("public async Task<IActionResult> ActivateAccount(");
        source.Should().Contain("public async Task<IActionResult> ConfirmRedemption(");
    }

    [Fact]
    public void LoyaltyController_Should_KeepAdminWorkspacesAndEditorGetsReachable()
    {
        var loyaltySource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Loyalty", "LoyaltyController.cs"));
        var adminBaseSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));

        adminBaseSource.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");
        loyaltySource.Should().Contain("public sealed class LoyaltyController : AdminBaseController");
        loyaltySource.Should().Contain("public IActionResult Index() => RedirectOrHtmx(nameof(Programs), new { });");
        loyaltySource.Should().Contain("public async Task<IActionResult> Programs(");
        loyaltySource.Should().Contain("public async Task<IActionResult> RewardTiers(Guid loyaltyProgramId");
        loyaltySource.Should().Contain("public async Task<IActionResult> Accounts(");
        loyaltySource.Should().Contain("public async Task<IActionResult> Campaigns(");
        loyaltySource.Should().Contain("public async Task<IActionResult> ScanSessions(");
        loyaltySource.Should().Contain("public async Task<IActionResult> Redemptions(");
        loyaltySource.Should().Contain("public async Task<IActionResult> AccountDetails(Guid id");
        loyaltySource.Should().Contain("public async Task<IActionResult> CreateProgram(Guid? businessId = null");
        loyaltySource.Should().Contain("public async Task<IActionResult> EditProgram(Guid id");
        loyaltySource.Should().Contain("public async Task<IActionResult> CreateRewardTier(Guid loyaltyProgramId");
        loyaltySource.Should().Contain("public async Task<IActionResult> EditRewardTier(Guid id, Guid loyaltyProgramId");
        loyaltySource.Should().Contain("public async Task<IActionResult> CreateAccount(Guid? businessId = null");
        loyaltySource.Should().Contain("public async Task<IActionResult> CreateCampaign(Guid? businessId = null");
        loyaltySource.Should().Contain("public async Task<IActionResult> EditCampaign(Guid id, Guid businessId");
        loyaltySource.Should().Contain("public async Task<IActionResult> AdjustPoints(Guid loyaltyAccountId");
    }

    [Fact]
    public void CatalogAndContentControllers_Should_KeepMutationsProtected()
    {
        var productsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "ProductsController.cs"));
        var categoriesSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "CategoriesController.cs"));
        var pagesSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "CMS", "PagesController.cs"));
        var mediaSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Media", "MediaController.cs"));
        var shippingMethodsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Shipping", "ShippingMethodsController.cs"));

        productsSource.Should().Contain("[ValidateAntiForgeryToken]");
        productsSource.Should().Contain("public async Task<IActionResult> Create(ProductCreateVm vm");
        productsSource.Should().Contain("public async Task<IActionResult> Edit(ProductEditVm vm");
        productsSource.Should().Contain("public async Task<IActionResult> Delete(");

        categoriesSource.Should().Contain("[ValidateAntiForgeryToken]");
        categoriesSource.Should().Contain("public async Task<IActionResult> Create(CategoryCreateVm vm");
        categoriesSource.Should().Contain("public async Task<IActionResult> Edit(CategoryEditVm vm");
        categoriesSource.Should().Contain("public async Task<IActionResult> Delete(");

        pagesSource.Should().Contain("[ValidateAntiForgeryToken]");
        pagesSource.Should().Contain("public async Task<IActionResult> Create(PageCreateVm vm");
        pagesSource.Should().Contain("public async Task<IActionResult> Edit(PageEditVm vm");
        pagesSource.Should().Contain("public async Task<IActionResult> Delete(");

        mediaSource.Should().Contain("[ValidateAntiForgeryToken]");
        mediaSource.Should().Contain("public async Task<IActionResult> Create(MediaAssetCreateVm vm");
        mediaSource.Should().Contain("public async Task<IActionResult> Edit(MediaAssetEditVm vm");
        mediaSource.Should().Contain("public async Task<IActionResult> Delete(");

        shippingMethodsSource.Should().Contain("ValidateAntiForgeryToken");
        shippingMethodsSource.Should().Contain("public async Task<IActionResult> Create(ShippingMethodEditVm vm");
        shippingMethodsSource.Should().Contain("public async Task<IActionResult> Edit(ShippingMethodEditVm vm");
    }

    [Fact]
    public void CatalogAndContentControllers_Should_KeepAdminWorkspacesAndEditorGetsReachable()
    {
        var adminBaseSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));
        var productsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "ProductsController.cs"));
        var categoriesSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "CategoriesController.cs"));
        var pagesSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "CMS", "PagesController.cs"));
        var mediaSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Media", "MediaController.cs"));
        var shippingMethodsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Shipping", "ShippingMethodsController.cs"));

        adminBaseSource.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");
        productsSource.Should().Contain("public sealed class ProductsController : AdminBaseController");
        categoriesSource.Should().Contain("public sealed class CategoriesController : AdminBaseController");
        pagesSource.Should().Contain("public sealed class PagesController : AdminBaseController");
        mediaSource.Should().Contain("public sealed class MediaController : AdminBaseController");
        shippingMethodsSource.Should().Contain("public sealed class ShippingMethodsController : AdminBaseController");

        productsSource.Should().Contain("public async Task<IActionResult> Index(");
        productsSource.Should().Contain("public async Task<IActionResult> Create(CancellationToken ct)");
        productsSource.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct)");

        categoriesSource.Should().Contain("public async Task<IActionResult> Index(");
        categoriesSource.Should().Contain("public async Task<IActionResult> Create(CancellationToken ct)");
        categoriesSource.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct)");

        pagesSource.Should().Contain("public async Task<IActionResult> Index(");
        pagesSource.Should().Contain("public async Task<IActionResult> Create(CancellationToken ct)");
        pagesSource.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct)");

        mediaSource.Should().Contain("public async Task<IActionResult> Index(");
        mediaSource.Should().Contain("public IActionResult Create()");
        mediaSource.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)");
        mediaSource.Should().Contain("public async Task<IActionResult> UploadQuill(IFormFile? file, CancellationToken ct)");

        shippingMethodsSource.Should().Contain("public async Task<IActionResult> Index(");
        shippingMethodsSource.Should().Contain("public IActionResult Create()");
        shippingMethodsSource.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)");
    }

    [Fact]
    public void RemainingAdminControllers_Should_KeepOperationalMutationsProtected()
    {
        var rolesSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "RolesController.cs"));
        var permissionsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "PermissionsController.cs"));
        var siteSettingsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Settings", "SiteSettingsController.cs"));
        var mobileOpsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Mobile", "MobileOperationsController.cs"));
        var addOnGroupsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "AddOnGroupsController.cs"));
        var brandsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "BrandsController.cs"));

        rolesSource.Should().Contain("[ValidateAntiForgeryToken]");
        rolesSource.Should().Contain("public async Task<IActionResult> Create(RoleCreateVm model");
        rolesSource.Should().Contain("public async Task<IActionResult> Edit(RoleEditVm model");
        rolesSource.Should().Contain("public async Task<IActionResult> Delete(");
        rolesSource.Should().Contain("public async Task<IActionResult> Permissions(RolePermissionsEditVm vm");

        permissionsSource.Should().Contain("[ValidateAntiForgeryToken]");
        permissionsSource.Should().Contain("public async Task<IActionResult> Create(PermissionCreateVm vm");
        permissionsSource.Should().Contain("public async Task<IActionResult> Edit(PermissionEditVm vm");
        permissionsSource.Should().Contain("public async Task<IActionResult> Delete(");

        siteSettingsSource.Should().Contain("[ValidateAntiForgeryToken]");
        siteSettingsSource.Should().Contain("public async Task<IActionResult> Edit(SiteSettingVm vm");
        siteSettingsSource.Should().Contain("await _update.HandleAsync(dto, ct);");
        siteSettingsSource.Should().Contain("_cache.Invalidate();");

        mobileOpsSource.Should().Contain("public async Task<IActionResult> Index(");
        mobileOpsSource.Should().Contain("[ValidateAntiForgeryToken]");
        mobileOpsSource.Should().Contain("public async Task<IActionResult> ClearPushToken(");
        mobileOpsSource.Should().Contain("public async Task<IActionResult> DeactivateDevice(");

        addOnGroupsSource.Should().Contain("ValidateAntiForgeryToken");
        addOnGroupsSource.Should().Contain("public async Task<IActionResult> Create(AddOnGroupCreateVm vm");
        addOnGroupsSource.Should().Contain("public async Task<IActionResult> Edit(AddOnGroupEditVm vm");
        addOnGroupsSource.Should().Contain("public async Task<IActionResult> Delete(");
        addOnGroupsSource.Should().Contain("public async Task<IActionResult> AttachToProducts(AddOnGroupAttachToProductsVm vm");
        addOnGroupsSource.Should().Contain("public async Task<IActionResult> AttachToCategories(AddOnGroupAttachToCategoriesVm vm");
        addOnGroupsSource.Should().Contain("public async Task<IActionResult> AttachToBrands(AddOnGroupAttachToBrandsVm vm");
        addOnGroupsSource.Should().Contain("public async Task<IActionResult> AttachToVariants(AddOnGroupAttachToVariantsVm vm");

        brandsSource.Should().Contain("ValidateAntiForgeryToken");
        brandsSource.Should().Contain("public async Task<IActionResult> Create(BrandEditVm vm");
        brandsSource.Should().Contain("public async Task<IActionResult> Edit(BrandEditVm vm");
        brandsSource.Should().Contain("public async Task<IActionResult> Delete(");
    }

    [Fact]
    public void RemainingAdminControllers_Should_KeepWorkspacesAndEditorGetsReachable()
    {
        var adminBaseSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));
        var rolesSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "RolesController.cs"));
        var permissionsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "PermissionsController.cs"));
        var siteSettingsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Settings", "SiteSettingsController.cs"));
        var mobileOpsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Mobile", "MobileOperationsController.cs"));
        var addOnGroupsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "AddOnGroupsController.cs"));
        var brandsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "BrandsController.cs"));

        adminBaseSource.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");
        rolesSource.Should().Contain("public sealed class RolesController : AdminBaseController");
        permissionsSource.Should().Contain("public sealed class PermissionsController : AdminBaseController");
        siteSettingsSource.Should().Contain("public sealed class SiteSettingsController : AdminBaseController");
        mobileOpsSource.Should().Contain("public sealed class MobileOperationsController : AdminBaseController");
        addOnGroupsSource.Should().Contain("public sealed class AddOnGroupsController : AdminBaseController");
        brandsSource.Should().Contain("public sealed class BrandsController : AdminBaseController");

        rolesSource.Should().Contain("public async Task<IActionResult> Index(");
        rolesSource.Should().Contain("public IActionResult Create()");
        rolesSource.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)");
        rolesSource.Should().Contain("public async Task<IActionResult> Permissions(Guid id, CancellationToken ct = default)");

        permissionsSource.Should().Contain("public async Task<IActionResult> Index(");
        permissionsSource.Should().Contain("public IActionResult Create()");
        permissionsSource.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)");

        siteSettingsSource.Should().Contain("public async Task<IActionResult> Edit(CancellationToken ct)");
        siteSettingsSource.Should().Contain("var dto = await _cache.GetAsync(ct);");
        siteSettingsSource.Should().Contain("return PartialView(\"~/Views/SiteSettings/_SiteSettingsEditorShell.cshtml\", vm);");
        siteSettingsSource.Should().Contain("Response.Headers[\"HX-Redirect\"] = Url.Action(actionName) ?? string.Empty;");

        mobileOpsSource.Should().Contain("public async Task<IActionResult> Index(");

        addOnGroupsSource.Should().Contain("public async Task<IActionResult> Index(");
        addOnGroupsSource.Should().Contain("public IActionResult Create()");
        addOnGroupsSource.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)");
        addOnGroupsSource.Should().Contain("public async Task<IActionResult> AttachToProducts(Guid id, int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)");
        addOnGroupsSource.Should().Contain("public async Task<IActionResult> AttachToCategories(Guid id, int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)");
        addOnGroupsSource.Should().Contain("public async Task<IActionResult> AttachToBrands(Guid id, int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)");
        addOnGroupsSource.Should().Contain("public async Task<IActionResult> AttachToVariants(");

        brandsSource.Should().Contain("public async Task<IActionResult> Index(int page = 1, int pageSize = 20,");
        brandsSource.Should().Contain("public IActionResult Create() => RenderBrandEditor(new BrandEditVm");
        brandsSource.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)");
    }

    [Fact]
    public void BusinessStaffAccessBadgeWorkspace_Should_KeepConstraintRemediationContractsWired()
    {
        var businessesSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var badgeViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "StaffAccessBadge.cshtml"));

        businessesSource.Should().Contain("public async Task<IActionResult> StaffAccessBadge(Guid id, CancellationToken ct = default)");
        businessesSource.Should().Contain("return RenderStaffAccessBadgeWorkspace(vm);");

        badgeViewSource.Should().Contain("@Model.Business.Name | @Model.UserDisplayName");
        badgeViewSource.Should().Contain("@T.T(\"BusinessStaffAccessBadgeIntro\")");
        badgeViewSource.Should().Contain("@Url.Action(\"EditMember\", \"Businesses\", new { id = Model.MembershipId })");
        badgeViewSource.Should().Contain("@Url.Action(\"Edit\", \"Users\", new { id = Model.UserId })");
        badgeViewSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Business.Id })");
        badgeViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        badgeViewSource.Should().Contain("@if (!Model.EmailConfirmed || !Model.IsActive || isLocked)");
        badgeViewSource.Should().Contain("@T.T(\"BusinessStaffAccessBadgeConstraintWarning\")");
        badgeViewSource.Should().Contain("@T.T(\"OpenUserAction\")");
        badgeViewSource.Should().Contain("@T.T(\"EditMemberAction\")");
        badgeViewSource.Should().Contain("@T.T(\"CommonMembers\")");
        badgeViewSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
        badgeViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        badgeViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        badgeViewSource.Should().NotContain("<i class=\"fa-solid fa-user-lock\"></i> @T.T(\"UsersFilterLocked\")");
        badgeViewSource.Should().NotContain("<span class=\"text-danger\"> | @T.T(\"UsersLifecycleLockedBadge\")</span>");
        badgeViewSource.Should().Contain("@T.T(\"MobileOperationsTitle\")");
        badgeViewSource.Should().Contain("@T.T(\"BusinessStaffAccessBadgePayloadNote\")");
        badgeViewSource.Should().Contain("@Url.Action(\"EditMember\", \"Businesses\", new { id = Model.MembershipId })");
        badgeViewSource.Should().Contain("@Url.Action(\"Edit\", \"Users\", new { id = Model.UserId })");
        badgeViewSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Business.Id })");
        badgeViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
    }

    [Fact]
    public void BusinessSetupPreviewWorkspaces_Should_KeepMemberAndInvitationRemediationContractsWired()
    {
        var businessesSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));
        var membersPreviewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SetupMembersPreview.cshtml"));
        var invitationsPreviewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SetupInvitationsPreview.cshtml"));

        businessesSource.Should().Contain("@T.T(\"BusinessSetupMembersRequiringAttentionTitle\")");
        businessesSource.Should().Contain("@Url.Action(\"SetupMembersPreview\", \"Businesses\", new { businessId = Model.Id })");
        businessesSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Id })");
        businessesSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        businessesSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Open)");
        businessesSource.Should().Contain("@Url.Action(\"SetupInvitationsPreview\", \"Businesses\", new { businessId = Model.Id })");
        businessesSource.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = Model.Id })");
        businessesSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        businessesSource.Should().Contain("@T.T(\"Members\")");
        businessesSource.Should().Contain("@T.T(\"Invitations\")");
        businessesSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
        businessesSource.Should().Contain("string MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        businessesSource.Should().Contain("@MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        businessesSource.Should().Contain("@MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        businessesSource.Should().NotContain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Id, filter = Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation })\"\r\n                       hx-target=\"#business-setup-shell\"\r\n                       hx-swap=\"outerHTML\"\r\n                       hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
        businessesSource.Should().NotContain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Id, filter = Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked })\"\r\n                       hx-target=\"#business-setup-shell\"\r\n                       hx-swap=\"outerHTML\"\r\n                       hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
        businessesSource.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        businessesSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        businessesSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Expired)");
        businessesSource.Should().NotContain("<span>@T.T(\"OpenInvitations\")</span>");
        businessesSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Pending\")</a>");
        businessesSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Expired\")</a>");

        membersPreviewSource.Should().Contain("@Url.Action(\"Edit\", \"Users\", new { id = item.UserId })");
        membersPreviewSource.Should().Contain("@Url.Action(\"EditMember\", \"Businesses\", new { id = item.Id })");
        membersPreviewSource.Should().Contain("flowKey = \"AccountActivation\"");
        membersPreviewSource.Should().Contain("string MemberAttentionStatusLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        membersPreviewSource.Should().Contain("@MemberAttentionStatusLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        membersPreviewSource.Should().Contain("@MemberAttentionStatusLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        membersPreviewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
        membersPreviewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Locked\")</a>");

        invitationsPreviewSource.Should().Contain("flowKey = \"BusinessInvitation\"");
        invitationsPreviewSource.Should().Contain("@T.T(\"OpenFailedInvitationEmails\")");
        invitationsPreviewSource.Should().Contain("filter = \"Pending\"");
        invitationsPreviewSource.Should().Contain("filter = \"Expired\"");
        invitationsPreviewSource.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        invitationsPreviewSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        invitationsPreviewSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Expired)");
        invitationsPreviewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Pending\")</a>");
        invitationsPreviewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Expired\")</a>");
    }

    [Fact]
    public void BusinessSetupWorkspace_Should_KeepSummaryRemediationContractsWired()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@T.T(\"OperationalStatus\")");
        setupShellSource.Should().Contain("operationalStatus = Model.OperationalStatus");
        setupShellSource.Should().Contain("@T.T(\"EditBusiness\")");
        setupShellSource.Should().Contain("@T.T(\"Members\")");
        setupShellSource.Should().Contain("@T.T(\"Locations\")");
        setupShellSource.Should().Contain("@T.T(\"OpenGlobalLocalization\")");
        setupShellSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
    }

    [Fact]
    public void BusinessSetupWorkspace_Should_KeepCommunicationReadinessRemediationContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        controllerSource.Should().Contain("? T(\"BusinessCommunicationReadinessEmailConfiguredSummary\")");
        controllerSource.Should().Contain(": T(\"BusinessCommunicationReadinessEmailMissingSummary\")");
        controllerSource.Should().Contain("? T(\"BusinessCommunicationReadinessSmsConfiguredSummary\")");
        controllerSource.Should().Contain(": T(\"BusinessCommunicationReadinessSmsMissingSummary\")");
        controllerSource.Should().Contain("? T(\"BusinessCommunicationReadinessWhatsAppConfiguredSummary\")");
        controllerSource.Should().Contain(": T(\"BusinessCommunicationReadinessWhatsAppMissingSummary\")");
        controllerSource.Should().Contain("? T(\"BusinessCommunicationReadinessAdminRoutingConfiguredSummary\")");
        controllerSource.Should().Contain(": T(\"BusinessCommunicationReadinessAdminRoutingMissingSummary\")");
        setupShellSource.Should().Contain("@T.T(\"CommunicationReadiness\")");
        setupShellSource.Should().Contain("@Url.Action(\"Details\", \"BusinessCommunications\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"ChannelAudits\", \"BusinessCommunications\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("fragment = \"site-settings-admin-routing\"");
        setupShellSource.Should().Contain("@T.T(\"BusinessCommunicationProfileTitle\")");
        setupShellSource.Should().Contain("@T.T(\"EmailAudits\")");
        setupShellSource.Should().Contain("@T.T(\"SmsWhatsAppAuditsTitle\")");
        setupShellSource.Should().Contain("@T.T(\"AdminAlerts\")");
    }

    [Fact]
    public void BusinessSupportAuditRecommendations_Should_KeepLocalizedGuidanceContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("RecommendedAction = BuildSupportAuditRecommendedAction(x)");
        controllerSource.Should().Contain("private string BuildSupportAuditRecommendedAction(EmailDispatchAuditListItemDto item)");
        controllerSource.Should().Contain("? T(\"BusinessSupportAuditInvitationBusinessAction\")");
        controllerSource.Should().Contain(": T(\"BusinessSupportAuditInvitationGenericAction\")");
        controllerSource.Should().Contain("? T(\"BusinessSupportAuditActivationBusinessAction\")");
        controllerSource.Should().Contain(": T(\"BusinessSupportAuditActivationGenericAction\")");
        controllerSource.Should().Contain("return T(\"BusinessSupportAuditPasswordResetAction\")");
        controllerSource.Should().Contain("return T(\"BusinessSupportAuditGenericAction\")");
    }

    [Fact]
    public void BusinessSetupWorkspace_Should_KeepSubscriptionSnapshotRemediationContractsWired()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@T.T(\"BusinessSubscriptionSnapshot\")");
        setupShellSource.Should().Contain("@T.T(\"NoActiveSubscriptionSnapshot\")");
        setupShellSource.Should().Contain("@Url.Action(\"Subscription\", \"Businesses\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"SubscriptionInvoices\", \"Businesses\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"Payments\", \"Billing\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessSubscriptionInvoicesTitle\")");
        setupShellSource.Should().Contain("@T.T(\"OpenPayments\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
    }

    [Fact]
    public void BusinessSetupWorkspace_Should_KeepIncompleteChecklistRemediationContractsWired()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@T.T(\"SetupIncompleteWarning\")");
        setupShellSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)");
        setupShellSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)");
        setupShellSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)");
        setupShellSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)");
        setupShellSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"Locations\", \"Businesses\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"Edit\", \"Businesses\", new { id = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
    }

    [Fact]
    public void BusinessSetupWorkspace_Should_KeepOwnershipRemediationContractsWired()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@T.T(\"BusinessOwnedHere\")");
        setupShellSource.Should().Contain("@T.T(\"GlobalDependencies\")");
        setupShellSource.Should().Contain("@Url.Action(\"Details\", \"BusinessCommunications\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"Setup\", \"Businesses\", new { id = Model.Id })");
        setupShellSource.Should().Contain("fragment = \"site-settings-communications-policy\"");
        setupShellSource.Should().Contain("fragment = \"site-settings-payments\"");
        setupShellSource.Should().Contain("fragment = \"site-settings-tax\"");
        setupShellSource.Should().Contain("@T.T(\"BusinessCommunicationProfileTitle\")");
        setupShellSource.Should().Contain("@T.T(\"CommunicationPolicy\")");
        setupShellSource.Should().Contain("@T.T(\"Payments\")");
        setupShellSource.Should().Contain("@T.T(\"Tax\")");
    }

    [Fact]
    public void BusinessSetupWorkspace_Should_KeepProfileAndLocalizationDefaultContractsWired()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@T.T(\"BusinessProfile\")");
        setupShellSource.Should().Contain("@Url.Action(\"Details\", \"BusinessCommunications\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"TaxCompliance\", \"Billing\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        setupShellSource.Should().Contain("@T.T(\"LocalizationOperationalDefaults\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessSetupLocalizationOwnershipIntro\")");
        setupShellSource.Should().Contain("fragment = \"site-settings-localization\"");
        setupShellSource.Should().Contain("@Url.Action(\"Customers\", \"Crm\")");
        setupShellSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        setupShellSource.Should().Contain("@T.T(\"CustomerLocaleReview\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
    }

    [Fact]
    public void BusinessSetupWorkspace_Should_KeepBrandingFollowUpContractsWired()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@T.T(\"Branding\")");
        setupShellSource.Should().Contain("@T.T(\"BrandDisplayName\")");
        setupShellSource.Should().Contain("@T.T(\"BrandLogoUrl\")");
        setupShellSource.Should().Contain("@Url.Action(\"Edit\", \"SiteSettings\", new { fragment = \"site-settings-business-app\" })");
        setupShellSource.Should().Contain("@Url.Action(\"Details\", \"BusinessCommunications\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessApp\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessCommunicationProfileTitle\")");
        setupShellSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
    }

    [Fact]
    public void BusinessSetupWorkspace_Should_KeepOperationalSetupActionPlaybookContractsWired()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@T.T(\"OperationalSetupActions\")");
        setupShellSource.Should().Contain("@Url.Action(\"CreateMember\", \"Businesses\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"CreateLocation\", \"Businesses\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"CreateInvitation\", \"Businesses\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"OwnerOverrideAudits\", \"Businesses\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Id, filter = Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation })");
        setupShellSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Id, filter = Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked })");
        setupShellSource.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = Model.Id, filter = Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending })");
        setupShellSource.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = Model.Id, filter = Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Expired })");
        setupShellSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        setupShellSource.Should().Contain("string MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        setupShellSource.Should().Contain("@MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        setupShellSource.Should().Contain("@MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        setupShellSource.Should().NotContain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Id, filter = Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation })\"\r\n                       hx-target=\"#business-setup-shell\"\r\n                       hx-swap=\"outerHTML\"\r\n                       hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
        setupShellSource.Should().NotContain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Id, filter = Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked })\"\r\n                       hx-target=\"#business-setup-shell\"\r\n                       hx-swap=\"outerHTML\"\r\n                       hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
        setupShellSource.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        setupShellSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        setupShellSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Expired)");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Pending\")</a>");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Expired\")</a>");
    }

    [Fact]
    public void BusinessSetupWorkspace_Should_KeepMemberAndInvitationHelperLabelsSourceBacked()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("string MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        setupShellSource.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        setupShellSource.Should().Contain("@MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        setupShellSource.Should().Contain("@MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        setupShellSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Open)");
        setupShellSource.Should().NotContain("<span>@T.T(\"OpenInvitations\")</span>");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
    }

    [Fact]
    public void BusinessSetupMembersPreview_Should_KeepAttentionStatusesHelperBacked()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SetupMembersPreview.cshtml"));

        source.Should().Contain("string MemberAttentionStatusLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        source.Should().Contain("@MemberAttentionStatusLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        source.Should().Contain("@MemberAttentionStatusLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        source.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
        source.Should().NotContain("hx-push-url=\"true\">@T.T(\"Locked\")</a>");
    }

    [Fact]
    public void BusinessSetupInvitationsPreview_Should_KeepQueueStatusesHelperBacked()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SetupInvitationsPreview.cshtml"));

        source.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        source.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        source.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Expired)");
        source.Should().NotContain("hx-push-url=\"true\">@T.T(\"Pending\")</a>");
        source.Should().NotContain("hx-push-url=\"true\">@T.T(\"Expired\")</a>");
    }

    [Fact]
    public void BusinessSetupMembersPreview_Should_KeepPendingActivationBadgeHelperBacked()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SetupMembersPreview.cshtml"));

        source.Should().Contain("@MemberAttentionStatusLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        source.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
    }

    [Fact]
    public void BusinessSetupMembersPreview_Should_KeepLockedBadgeHelperBacked()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SetupMembersPreview.cshtml"));

        source.Should().Contain("@MemberAttentionStatusLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        source.Should().NotContain("hx-push-url=\"true\">@T.T(\"Locked\")</a>");
    }

    [Fact]
    public void BusinessSetupInvitationsPreview_Should_KeepPendingBadgeHelperBacked()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SetupInvitationsPreview.cshtml"));

        source.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        source.Should().NotContain("hx-push-url=\"true\">@T.T(\"Pending\")</a>");
    }

    [Fact]
    public void BusinessSetupInvitationsPreview_Should_KeepExpiredBadgeHelperBacked()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SetupInvitationsPreview.cshtml"));

        source.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Expired)");
        source.Should().NotContain("hx-push-url=\"true\">@T.T(\"Expired\")</a>");
    }

    [Fact]
    public void BusinessSetupMembersPreview_Should_KeepEditMemberActionWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SetupMembersPreview.cshtml"));

        source.Should().Contain("@Url.Action(\"EditMember\", \"Businesses\", new { id = item.Id })");
        source.Should().Contain("@T.T(\"EditMemberAction\")");
    }

    [Fact]
    public void BusinessSetupMembersPreview_Should_KeepOpenMembersActionWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SetupMembersPreview.cshtml"));

        source.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.BusinessId })");
        source.Should().Contain("@T.T(\"BusinessSetupOpenMembersAction\")");
    }

    [Fact]
    public void BusinessSetupInvitationsPreview_Should_KeepOpenInvitationsActionWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SetupInvitationsPreview.cshtml"));

        source.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = Model.BusinessId })");
        source.Should().Contain("@T.T(\"BusinessSetupOpenInvitationsAction\")");
    }

    [Fact]
    public void BusinessSetupInvitationsPreview_Should_KeepFailedInvitationsActionWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SetupInvitationsPreview.cshtml"));

        source.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\", flowKey = \"BusinessInvitation\", recipientEmail = item.Email })");
        source.Should().Contain("@T.T(\"OpenFailedInvitationEmails\")");
    }

    [Fact]
    public void BusinessSetupWorkspace_Should_KeepCommunicationDefaultsAndPlatformDependencyContractsWired()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@T.T(\"BusinessSetupPlatformDependenciesIntro\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessSetupPlatformDependenciesRule\")");
        setupShellSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        setupShellSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        setupShellSource.Should().Contain("fragment = \"site-settings-communications-policy\"");
        setupShellSource.Should().Contain("@Url.Action(\"Edit\", \"Businesses\", new { id = Model.Id })");
        setupShellSource.Should().Contain("@T.T(\"OpenGlobalSettings\")");
        setupShellSource.Should().Contain("@T.T(\"EditBusiness\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessCommunicationDefaults\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessCommunicationDefaultsHelp\")");
        setupShellSource.Should().Contain("@Url.Action(\"Details\", \"BusinessCommunications\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"ChannelAudits\", \"BusinessCommunications\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@T.T(\"BusinessSetupPaymentsShippingTitle\")");
        setupShellSource.Should().Contain("fragment = \"site-settings-payments\"");
        setupShellSource.Should().Contain("fragment = \"site-settings-shipping\"");
        setupShellSource.Should().Contain("@Url.Action(\"Payments\", \"Billing\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@T.T(\"OpenPayments\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessSetupSecurityMobileAccessTitle\")");
        setupShellSource.Should().Contain("fragment = \"site-settings-security\"");
        setupShellSource.Should().Contain("fragment = \"site-settings-mobile\"");
        setupShellSource.Should().Contain("@Url.Action(\"Index\", \"MobileOperations\")");
        setupShellSource.Should().Contain("@Url.Action(\"Index\", \"Users\", new { filter = \"Locked\" })");
        setupShellSource.Should().Contain("@T.T(\"MobileOperationsTitle\")");
        setupShellSource.Should().Contain("@MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
        setupShellSource.Should().Contain("@T.T(\"Communications\")");
        setupShellSource.Should().Contain("fragment = \"site-settings-smtp\"");
        setupShellSource.Should().Contain("fragment = \"site-settings-sms\"");
        setupShellSource.Should().Contain("fragment = \"site-settings-whatsapp\"");
        setupShellSource.Should().Contain("string CommunicationChannelLabel(string channel) => string.Equals(channel, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");
        setupShellSource.Should().Contain("? T.T(\"BusinessCommunicationWhatsAppShort\")");
        setupShellSource.Should().Contain(": T.T(\"SMS\");");
        setupShellSource.Should().Contain("@CommunicationChannelLabel(\"SMS\")");
        setupShellSource.Should().Contain("@CommunicationChannelLabel(\"WhatsApp\")");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"SMS\")</a>");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"WhatsApp\")</a>");
        setupShellSource.Should().Contain("@Url.Action(\"Details\", \"BusinessCommunications\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"ChannelAudits\", \"BusinessCommunications\", new { businessId = Model.Id })");
    }

    [Fact]
    public void BusinessSetupWorkspace_Should_KeepFooterFollowUpContractsWired()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@T.T(\"BusinessSetupSaveAction\")");
        setupShellSource.Should().Contain("@Url.Action(\"Edit\", \"Businesses\", new { id = Model.Id })");
        setupShellSource.Should().Contain("@T.T(\"BusinessMembersBackToBusinessAction\")");
        setupShellSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        setupShellSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        setupShellSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
    }

    [Fact]
    public void BusinessEditorWorkspace_Should_KeepSummaryAndOnboardingRemediationContractsWired()
    {
        var editorShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessEditorShell.cshtml"));

        editorShellSource.Should().Contain("@T.T(\"BusinessEditorOwners\")");
        editorShellSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        editorShellSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        editorShellSource.Should().NotContain("@T.T(\"BusinessEditorPendingInvites\")");
        editorShellSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Id })");
        editorShellSource.Should().Contain("@Url.Action(\"Locations\", \"Businesses\", new { businessId = Model.Id })");
        editorShellSource.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = Model.Id })");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorOperationalStatus\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorOnboardingChecklist\")");
        editorShellSource.Should().Contain("@Url.Action(\"Setup\", \"Businesses\", new { id = Model.Id })");
        editorShellSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        editorShellSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorNextActions\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorNotOnboardingCompleteYet\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
    }

    [Fact]
    public void BusinessEditorWorkspace_Should_KeepInvitationSummaryTileHelperBacked()
    {
        var editorShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessEditorShell.cshtml"));

        editorShellSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        editorShellSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        editorShellSource.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = Model.Id })");
        editorShellSource.Should().NotContain("@T.T(\"BusinessEditorPendingInvites\")");
    }

    [Fact]
    public void BusinessEditorWorkspace_Should_KeepInvitationActionLanesWired()
    {
        var editorShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessEditorShell.cshtml"));

        editorShellSource.Should().Contain("@Url.Action(\"CreateInvitation\", \"Businesses\", new { businessId = Model.Id })");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorInviteUser\")");
        editorShellSource.Should().Contain("@if (Model.InvitationCount > 0)");
        editorShellSource.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = Model.Id })");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorReviewInvitations\")");
    }

    [Fact]
    public void BusinessEditorWorkspace_Should_KeepOwnerLocationAndSetupActionLanesWired()
    {
        var editorShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessEditorShell.cshtml"));

        editorShellSource.Should().Contain("@Url.Action(\"CreateMember\", \"Businesses\", new { businessId = Model.Id })");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorAssignOwner\")");
        editorShellSource.Should().Contain("@Url.Action(\"CreateLocation\", \"Businesses\", new { businessId = Model.Id })");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorAddPrimaryLocation\")");
        editorShellSource.Should().Contain("@Url.Action(\"Subscription\", \"Businesses\", new { businessId = Model.Id })");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorReviewSubscription\")");
        editorShellSource.Should().Contain("@Url.Action(\"Setup\", \"Businesses\", new { id = Model.Id })");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorOpenSetupWorkspace\")");
    }

    [Fact]
    public void BusinessEditorWorkspace_Should_KeepChecklistSummaryAndIncompleteRemediationWired()
    {
        var editorShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessEditorShell.cshtml"));

        editorShellSource.Should().Contain("@(checklistComplete ? T.T(\"BusinessEditorReadyForGoLiveReview\") : T.T(\"BusinessEditorSetupStillIncomplete\"))");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorChecklistComplete\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorChecklistIncomplete\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorNotOnboardingCompleteYet\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorAddAtLeastOneActiveOwner\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorAddPrimaryLocationSentence\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorConfigureContactEmail\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorCompleteLegalBusinessName\")");
    }

    [Fact]
    public void BusinessEditorWorkspace_Should_KeepChecklistStatusRowsWired()
    {
        var editorShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessEditorShell.cshtml"));

        editorShellSource.Should().Contain("@((Model.ActiveOwnerCount > 0) ? T.T(\"Yes\") : T.T(\"No\")) - @T.T(\"BusinessEditorActiveOwnerAssigned\")");
        editorShellSource.Should().Contain("@((Model.PrimaryLocationCount > 0) ? T.T(\"Yes\") : T.T(\"No\")) - @T.T(\"BusinessEditorPrimaryLocationConfigured\")");
        editorShellSource.Should().Contain("@(Model.HasContactEmailConfigured ? T.T(\"Yes\") : T.T(\"No\")) - @T.T(\"BusinessEditorContactEmailConfigured\")");
        editorShellSource.Should().Contain("@(Model.HasLegalNameConfigured ? T.T(\"Yes\") : T.T(\"No\")) - @T.T(\"BusinessEditorLegalBusinessNameConfigured\")");
    }

    [Fact]
    public void BusinessEditorWorkspace_Should_KeepOperationalStatusSummaryWired()
    {
        var editorShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessEditorShell.cshtml"));

        editorShellSource.Should().Contain("@T.T(\"BusinessEditorOperationalStatus\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorPendingApproval\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorApproved\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorSuspended\")");
        editorShellSource.Should().Contain("@string.Format(T.T(\"BusinessEditorApprovedAt\"), Model.ApprovedAtUtc.Value.ToString(\"yyyy-MM-dd HH:mm\"))");
        editorShellSource.Should().Contain("@string.Format(T.T(\"BusinessEditorSuspendedAt\"), Model.SuspendedAtUtc.Value.ToString(\"yyyy-MM-dd HH:mm\"))");
        editorShellSource.Should().Contain("@string.Format(T.T(\"BusinessEditorReasonValue\"), Model.SuspensionReason)");
    }

    [Fact]
    public void BusinessEditorWorkspace_Should_KeepOperationalStatusActionsWired()
    {
        var editorShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessEditorShell.cshtml"));

        editorShellSource.Should().Contain("asp-action=\"Approve\"");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorApprove\")");
        editorShellSource.Should().Contain("asp-action=\"Reactivate\"");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorReactivate\")");
        editorShellSource.Should().Contain("asp-action=\"Suspend\"");
        editorShellSource.Should().Contain("placeholder=\"@T.T(\"BusinessEditorOptionalSuspensionReason\")\"");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorSuspend\")");
    }

    [Fact]
    public void BusinessLocationAndInvitationEditorShells_Should_KeepWorkspacePivotContractsWired()
    {
        var locationShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessLocationEditorShell.cshtml"));
        var invitationShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessInvitationEditorShell.cshtml"));

        locationShellSource.Should().Contain("@Url.Action(\"Locations\", \"Businesses\", new { businessId = Model.BusinessId })");
        locationShellSource.Should().Contain("@Url.Action(\"Setup\", \"Businesses\", new { id = Model.BusinessId })");
        locationShellSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        locationShellSource.Should().Contain("@T.T(\"BusinessLocationBackToLocations\")");
        locationShellSource.Should().Contain("@T.T(\"Setup\")");
        locationShellSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
        locationShellSource.Should().Contain("mt-4");

        invitationShellSource.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = Model.BusinessId })");
        invitationShellSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        invitationShellSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        invitationShellSource.Should().Contain("@T.T(\"BusinessInvitationBackToInvitations\")");
        invitationShellSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
        invitationShellSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
        invitationShellSource.Should().Contain("mt-4");
    }

    [Fact]
    public void BusinessLocationEditorShell_Should_KeepTopWorkspacePivotsWired()
    {
        var locationShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessLocationEditorShell.cshtml"));

        locationShellSource.Should().Contain("hx-target=\"#business-location-editor-shell\"");
        locationShellSource.Should().Contain("@Url.Action(\"Locations\", \"Businesses\", new { businessId = Model.BusinessId })");
        locationShellSource.Should().Contain("@T.T(\"BusinessLocationBackToLocations\")");
        locationShellSource.Should().Contain("@Url.Action(\"Setup\", \"Businesses\", new { id = Model.BusinessId })");
        locationShellSource.Should().Contain("@T.T(\"Setup\")");
        locationShellSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        locationShellSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
    }

    [Fact]
    public void BusinessInvitationEditorShell_Should_KeepTopWorkspacePivotsWired()
    {
        var invitationShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessInvitationEditorShell.cshtml"));

        invitationShellSource.Should().Contain("hx-target=\"#business-invitation-editor-shell\"");
        invitationShellSource.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = Model.BusinessId })");
        invitationShellSource.Should().Contain("@T.T(\"BusinessInvitationBackToInvitations\")");
        invitationShellSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        invitationShellSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
        invitationShellSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        invitationShellSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
    }

    [Fact]
    public void BusinessLocationEditorShell_Should_KeepFooterWorkspacePivotsWired()
    {
        var locationShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessLocationEditorShell.cshtml"));

        locationShellSource.Should().Contain("<div class=\"d-flex gap-2 flex-wrap mt-4\">");
        locationShellSource.Should().Contain("hx-target=\"#business-location-editor-shell\"");
        locationShellSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessLocationBackToLocations\")</a>");
        locationShellSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Setup\")</a>");
        locationShellSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void BusinessInvitationEditorShell_Should_KeepFooterWorkspacePivotsWired()
    {
        var invitationShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessInvitationEditorShell.cshtml"));

        invitationShellSource.Should().Contain("<div class=\"d-flex gap-2 flex-wrap mt-4\">");
        invitationShellSource.Should().Contain("hx-target=\"#business-invitation-editor-shell\"");
        invitationShellSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessInvitationBackToInvitations\")</a>");
        invitationShellSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessSupportQueueTitle\")</a>");
        invitationShellSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void BusinessLocationEditorShell_Should_KeepFormPostContractWired()
    {
        var locationShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessLocationEditorShell.cshtml"));

        locationShellSource.Should().Contain("<form asp-action=\"@(isCreate ? \"CreateLocation\" : \"EditLocation\")\"");
        locationShellSource.Should().Contain("hx-post=\"@Url.Action(isCreate ? \"CreateLocation\" : \"EditLocation\", \"Businesses\")\"");
        locationShellSource.Should().Contain("@Html.AntiForgeryToken()");
        locationShellSource.Should().Contain("<input type=\"hidden\" asp-for=\"BusinessId\" />");
        locationShellSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        locationShellSource.Should().Contain("<partial name=\"_BusinessLocationForm\" model=\"Model\" />");
    }

    [Fact]
    public void BusinessInvitationEditorShell_Should_KeepFormPostContractWired()
    {
        var invitationShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessInvitationEditorShell.cshtml"));

        invitationShellSource.Should().Contain("<form asp-action=\"CreateInvitation\"");
        invitationShellSource.Should().Contain("hx-post=\"@Url.Action(\"CreateInvitation\", \"Businesses\")\"");
        invitationShellSource.Should().Contain("@Html.AntiForgeryToken()");
        invitationShellSource.Should().Contain("<input type=\"hidden\" asp-for=\"BusinessId\" />");
        invitationShellSource.Should().Contain("<partial name=\"_BusinessInvitationForm\" model=\"Model\" />");
    }

    [Fact]
    public void BusinessInvitationForm_Should_KeepCreationGuidanceRemediationContractsWired()
    {
        var invitationFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessInvitationForm.cshtml"));

        invitationFormSource.Should().Contain("@T.T(\"BusinessInvitationCreateHelp\")");
        invitationFormSource.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = Model.BusinessId })");
        invitationFormSource.Should().Contain("@Url.Action(\"Setup\", \"Businesses\", new { id = Model.BusinessId })");
        invitationFormSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        invitationFormSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        invitationFormSource.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        invitationFormSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Open)");
        invitationFormSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"OpenInvitations\")</a>");
        invitationFormSource.Should().Contain("@T.T(\"Setup\")");
        invitationFormSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
        invitationFormSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
    }

    [Fact]
    public void BusinessMemberForm_Should_KeepAssignmentGuidanceRemediationContractsWired()
    {
        var memberFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberForm.cshtml"));

        memberFormSource.Should().Contain("@T.T(\"BusinessMemberAssignmentHelp\")");
        memberFormSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.BusinessId })");
        memberFormSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.BusinessId, filter = Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation })");
        memberFormSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        memberFormSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        memberFormSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        memberFormSource.Should().Contain("@T.T(\"CommonMembers\")");
        memberFormSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        memberFormSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
        memberFormSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
        memberFormSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
    }

    [Fact]
    public void BusinessMemberEditorShell_Should_KeepSupportBadgesHelperBacked()
    {
        var memberEditorShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberEditorShell.cshtml"));

        memberEditorShellSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        memberEditorShellSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        memberEditorShellSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        memberEditorShellSource.Should().Contain("@T.T(\"BusinessMemberEmailConfirmedBadge\")");
        memberEditorShellSource.Should().NotContain("<span class=\"badge text-bg-warning\">@T.T(\"PendingActivation\")</span>");
        memberEditorShellSource.Should().NotContain("<span class=\"badge text-bg-danger\">@T.T(\"UsersLifecycleLockedBadge\")</span>");
    }

    [Fact]
    public void BusinessMemberEditorShell_Should_KeepLockedBadgeHelperBacked()
    {
        var memberEditorShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberEditorShell.cshtml"));

        memberEditorShellSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        memberEditorShellSource.Should().NotContain("<span class=\"badge text-bg-danger\">@T.T(\"UsersLifecycleLockedBadge\")</span>");
    }

    [Fact]
    public void BusinessStaffAccessBadgeWorkspace_Should_KeepLockedStateSummaryHelperBacked()
    {
        var badgeViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "StaffAccessBadge.cshtml"));

        badgeViewSource.Should().Contain("<span class=\"text-danger\"> | @MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</span>");
        badgeViewSource.Should().NotContain("<span class=\"text-danger\"> | @T.T(\"UsersLifecycleLockedBadge\")</span>");
    }

    [Fact]
    public void BusinessStaffAccessBadgeWorkspace_Should_KeepLockedRemediationShortcutHelperBacked()
    {
        var badgeViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "StaffAccessBadge.cshtml"));

        badgeViewSource.Should().Contain("<i class=\"fa-solid fa-user-lock\"></i> @MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        badgeViewSource.Should().NotContain("<i class=\"fa-solid fa-user-lock\"></i> @T.T(\"UsersFilterLocked\")");
    }

    [Fact]
    public void BusinessMembersWorkspace_Should_KeepRowStatusBadgesHelperBacked()
    {
        var membersViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Members.cshtml"));

        membersViewSource.Should().Contain("<span class=\"badge text-bg-warning\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</span>");
        membersViewSource.Should().Contain("<span class=\"badge text-bg-danger ms-1\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</span>");
        membersViewSource.Should().Contain("<span class=\"badge text-bg-success\">@MemberConfirmationStatusLabel(item.EmailConfirmed)</span>");
        membersViewSource.Should().Contain("<span class=\"badge text-bg-success\">@MemberActiveStatusLabel(item.IsActive)</span>");
        membersViewSource.Should().Contain("<span class=\"badge text-bg-secondary\">@MemberActiveStatusLabel(item.IsActive)</span>");
        membersViewSource.Should().NotContain("<span class=\"badge text-bg-warning\">@T.T(\"PendingActivation\")</span>");
        membersViewSource.Should().NotContain("<span class=\"badge text-bg-danger ms-1\">@T.T(\"Locked\")</span>");
        membersViewSource.Should().NotContain("<span class=\"badge text-bg-success\">@T.T(\"Confirmed\")</span>");
        membersViewSource.Should().NotContain("<span class=\"badge text-bg-success\">@T.T(\"Yes\")</span>");
        membersViewSource.Should().NotContain("<span class=\"badge text-bg-secondary\">@T.T(\"No\")</span>");
    }

    [Fact]
    public void BusinessMembersWorkspace_Should_KeepAttentionSummaryCardHelperBacked()
    {
        var membersViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Members.cshtml"));

        membersViewSource.Should().Contain("<div class=\"text-muted small text-uppercase\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</div>");
        membersViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        membersViewSource.Should().NotContain("<div class=\"text-muted small text-uppercase\">@T.T(\"NeedsAttention\")</div>");
        membersViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"NeedsAttention\")</a>");
    }

    [Fact]
    public void BusinessMembersWorkspace_Should_KeepConfirmedAndActiveBadgesHelperBacked()
    {
        var membersViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Members.cshtml"));

        membersViewSource.Should().Contain("string MemberConfirmationStatusLabel(bool emailConfirmed) => emailConfirmed");
        membersViewSource.Should().Contain("string MemberActiveStatusLabel(bool isActive) => isActive ? T.T(\"Yes\") : T.T(\"No\")");
        membersViewSource.Should().Contain("<span class=\"badge text-bg-success\">@MemberConfirmationStatusLabel(item.EmailConfirmed)</span>");
        membersViewSource.Should().Contain("<span class=\"badge text-bg-success\">@MemberActiveStatusLabel(item.IsActive)</span>");
        membersViewSource.Should().Contain("<span class=\"badge text-bg-secondary\">@MemberActiveStatusLabel(item.IsActive)</span>");
        membersViewSource.Should().NotContain("<span class=\"badge text-bg-success\">@T.T(\"Confirmed\")</span>");
        membersViewSource.Should().NotContain("<span class=\"badge text-bg-success\">@T.T(\"Yes\")</span>");
        membersViewSource.Should().NotContain("<span class=\"badge text-bg-secondary\">@T.T(\"No\")</span>");
    }

    [Fact]
    public void BusinessInvitationsWorkspace_Should_KeepRowStatusBadgesHelperBacked()
    {
        var invitationsViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Invitations.cshtml"));

        invitationsViewSource.Should().Contain("<span class=\"badge text-bg-primary\">@InvitationStatusLabel(item.Status)</span>");
        invitationsViewSource.Should().Contain("<span class=\"badge text-bg-warning\">@InvitationStatusLabel(item.Status)</span>");
        invitationsViewSource.Should().NotContain("<span class=\"badge text-bg-primary\">@T.T(\"Pending\")</span>");
        invitationsViewSource.Should().NotContain("<span class=\"badge text-bg-warning\">@T.T(\"Expired\")</span>");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepRowInvitationSignalsHelperBacked()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("@item.InvitationCount @InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        indexViewSource.Should().NotContain("@item.InvitationCount @T.T(\"PendingInvites\")");
    }

    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepRowInvitationSignalsHelperBacked()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("@item.InvitationCount @InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        merchantReadinessViewSource.Should().NotContain("@item.InvitationCount @T.T(\"PendingInvites\")");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepLockedMemberCardContractsWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</div>");
        summaryViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        summaryViewSource.Should().Contain("@T.T(\"OpenFailedPasswordResets\")");
        summaryViewSource.Should().Contain("@T.T(\"MobileOperationsTitle\")");
        summaryViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"LockedMembers\")</div>");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepLockedMemberCardWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</div>");
        summaryViewSource.Should().Contain("@Model.LockedMemberCount");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"Users\", new { filter = \"Locked\" })");
        summaryViewSource.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\", flowKey = \"PasswordReset\" })");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"MobileOperations\")");
        summaryViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedPasswordResets\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MobileOperationsTitle\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepSuspendedBusinessesCardWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</div>");
        summaryViewSource.Should().Contain("@Model.SuspendedBusinessCount");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { operationalStatus = \"Suspended\" })");
        summaryViewSource.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\" })");
        summaryViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"FailedEmails\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepMissingOwnerCardWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)</div>");
        summaryViewSource.Should().Contain("@Model.MissingOwnerBusinessCount");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner })");
        summaryViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepApprovedInactiveAndMissingLocationCardsWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)</div>");
        summaryViewSource.Should().Contain("@Model.ApprovedInactiveBusinessCount");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive })");
        summaryViewSource.Should().Contain("@Url.Action(\"Payments\", \"Billing\")");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Payments\")</a>");
        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</div>");
        summaryViewSource.Should().Contain("@Model.MissingPrimaryLocationBusinessCount");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation })");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Locations\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepMissingContactAndLegalNameCardsWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</div>");
        summaryViewSource.Should().Contain("@Model.MissingContactEmailBusinessCount");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail })");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Edit\")</a>");
        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</div>");
        summaryViewSource.Should().Contain("@Model.MissingLegalNameBusinessCount");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName })");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepNeedsAttentionCardWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</div>");
        summaryViewSource.Should().Contain("@Model.AttentionBusinessCount");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { attentionOnly = true })");
        summaryViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepPendingApprovalCardWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</div>");
        summaryViewSource.Should().Contain("@Model.PendingApprovalBusinessCount");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { operationalStatus = Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval })");
        summaryViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepLockedMemberCardSubtitleHelperBacked()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</div>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</a>");
        indexViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"LockedMembers\")</div>");
    }

    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepLockedMemberCardSubtitleHelperBacked()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</div>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</a>");
        merchantReadinessViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"LockedMembers\")</div>");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepPendingActivationCardSubtitleHelperBacked()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</div>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</a>");
        indexViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingActivation\")</div>");
    }

    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepPendingActivationCardSubtitleHelperBacked()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</div>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</a>");
        merchantReadinessViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingActivation\")</div>");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepPendingInvitesCardSubtitleHelperBacked()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("<div class=\"text-muted small\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</div>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        indexViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingInvites\")</div>");
    }

    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepPendingInvitesCardSubtitleHelperBacked()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</div>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        merchantReadinessViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingInvites\")</div>");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepPendingActivationCardSubtitleHelperBacked()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</div>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</a>");
        summaryViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingActivation\")</div>");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepPendingActivationCardActionRailsWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("@T.T(\"OpenFailedActivationEmails\")");
        summaryViewSource.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\", flowKey = \"AccountActivation\" })");
        summaryViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        summaryViewSource.Should().Contain("@T.T(\"MobileOperationsTitle\")");
        summaryViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepPendingActivationCardWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</div>");
        summaryViewSource.Should().Contain("@Model.PendingActivationMemberCount");
        summaryViewSource.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\", flowKey = \"AccountActivation\" })");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"Users\", new { filter = \"Unconfirmed\" })");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"MobileOperations\")");
        summaryViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedActivationEmails\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MobileOperationsTitle\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepPendingInvitesCardSubtitleHelperBacked()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</div>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        summaryViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingInvites\")</div>");
        summaryViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"Invitations\")</div>");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepPendingInvitesCardActionRailsWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        summaryViewSource.Should().Contain("@T.T(\"OpenFailedInvitationEmails\")");
        summaryViewSource.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\", flowKey = \"BusinessInvitation\" })");
        summaryViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepPendingInvitesCardWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</div>");
        summaryViewSource.Should().Contain("@Model.OpenInvitationCount");
        summaryViewSource.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\", flowKey = \"BusinessInvitation\" })");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites })");
        summaryViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedInvitationEmails\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void SupportQueueWorkspace_Should_KeepPendingInvitesShortcutHelperBacked()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("hx-push-url=\"true\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        supportQueueSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingInvites\")</a>");
    }

    [Fact]
    public void SupportQueueWorkspace_Should_KeepPendingInvitesActionRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        supportQueueSource.Should().Contain("@T.T(\"OpenFailedInvitationEmails\")");
        supportQueueSource.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\", flowKey = \"BusinessInvitation\" })");
    }

    [Fact]
    public void SupportQueueWorkspace_Should_KeepPendingActivationShortcutHelperBacked()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</a>");
        supportQueueSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
    }

    [Fact]
    public void SupportQueueWorkspace_Should_KeepPendingActivationActionRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        supportQueueSource.Should().Contain("@T.T(\"OpenFailedActivationEmails\")");
        supportQueueSource.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\", flowKey = \"AccountActivation\" })");
        supportQueueSource.Should().Contain("@T.T(\"MobileOperationsTitle\")");
    }

    [Fact]
    public void SupportQueueWorkspace_Should_KeepLockedMembersShortcutHelperBacked()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</a>");
        supportQueueSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"LockedMembers\")</a>");
    }

    [Fact]
    public void SupportQueueWorkspace_Should_KeepLockedMembersActionRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        supportQueueSource.Should().Contain("@T.T(\"OpenFailedPasswordResets\")");
        supportQueueSource.Should().Contain("@T.T(\"MobileOperationsTitle\")");
    }

    [Fact]
    public void SupportQueueFailedEmailsFragment_Should_KeepPendingActivationShortcutHelperBacked()
    {
        var failedEmailsSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueFailedEmails.cshtml"));

        failedEmailsSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</a>");
        failedEmailsSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterUnconfirmed\")</a>");
    }

    [Fact]
    public void SupportQueueFailedEmailsFragment_Should_KeepLockedShortcutHelperBacked()
    {
        var failedEmailsSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueFailedEmails.cshtml"));

        failedEmailsSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</a>");
        failedEmailsSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
    }

    [Fact]
    public void SupportQueueFailedEmailsFragment_Should_KeepOpenInvitationsShortcutHelperBacked()
    {
        var failedEmailsSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueFailedEmails.cshtml"));

        failedEmailsSource.Should().Contain("hx-push-url=\"true\">@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Open)</a>");
        failedEmailsSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"OpenInvitations\")</a>");
    }

    [Fact]
    public void SupportQueueWorkspace_Should_KeepTopLevelMemberAndInvitationShortcutsHelperBacked()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        supportQueueSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        supportQueueSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        supportQueueSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        supportQueueSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        supportQueueSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingInvites\")</a>");
        supportQueueSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
        supportQueueSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"LockedMembers\")</a>");
    }

    [Fact]
    public void SupportQueueFailedEmailsFragment_Should_KeepInvitationAndMemberShortcutsHelperBacked()
    {
        var failedEmailsSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueFailedEmails.cshtml"));

        failedEmailsSource.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        failedEmailsSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        failedEmailsSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Open)");
        failedEmailsSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        failedEmailsSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        failedEmailsSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"OpenInvitations\")</a>");
        failedEmailsSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterUnconfirmed\")</a>");
        failedEmailsSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
    }

    [Fact]
    public void BusinessInvitationForm_Should_KeepOpenInvitationsShortcutHelperBacked()
    {
        var invitationFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessInvitationForm.cshtml"));

        invitationFormSource.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        invitationFormSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Open)");
        invitationFormSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"OpenInvitations\")</a>");
    }

    [Fact]
    public void BusinessMemberForm_Should_KeepPendingActivationShortcutHelperBacked()
    {
        var memberFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberForm.cshtml"));

        memberFormSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        memberFormSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        memberFormSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
    }

    [Fact]
    public void BusinessInvitationForm_Should_KeepHelpAndCancelWorkspacePivotsWired()
    {
        var invitationFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessInvitationForm.cshtml"));

        invitationFormSource.Should().Contain("@T.T(\"BusinessInvitationCreateHelp\")");
        invitationFormSource.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = Model.BusinessId })");
        invitationFormSource.Should().Contain("@Url.Action(\"Setup\", \"Businesses\", new { id = Model.BusinessId })");
        invitationFormSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        invitationFormSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        invitationFormSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Cancel\")</a>");
    }

    [Fact]
    public void BusinessMemberForm_Should_KeepCreateHelpAndCancelWorkspacePivotsWired()
    {
        var memberFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberForm.cshtml"));

        memberFormSource.Should().Contain("@T.T(\"BusinessMemberAssignmentHelp\")");
        memberFormSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.BusinessId })");
        memberFormSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        memberFormSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        memberFormSource.Should().Contain("hx-swap=\"outerHTML\">@T.T(\"Cancel\")</a>");
    }

    [Fact]
    public void BusinessSetupWorkspace_Should_KeepPendingActivationQuickLinkHelperBacked()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
    }

    [Fact]
    public void BusinessSetupWorkspace_Should_KeepLockedQuickLinkHelperBacked()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
    }

    [Fact]
    public void BusinessSetupWorkspace_Should_KeepPendingInvitationQuickLinkHelperBacked()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Pending\")</a>");
    }

    [Fact]
    public void BusinessSetupWorkspace_Should_KeepExpiredInvitationQuickLinkHelperBacked()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Expired)");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Expired\")</a>");
    }

    [Fact]
    public void BusinessSetupWorkspace_Should_KeepPendingLockedAndInvitationQueueQuickLinksHelperBacked()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        setupShellSource.Should().Contain("@MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        setupShellSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        setupShellSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Expired)");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Pending\")</a>");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Expired\")</a>");
    }

    [Fact]
    public void BusinessLocationForm_Should_KeepFooterRemediationContractsWired()
    {
        var locationFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessLocationForm.cshtml"));

        locationFormSource.Should().Contain("@Url.Action(\"Locations\", \"Businesses\", new { businessId = Model.BusinessId })");
        locationFormSource.Should().Contain("@Url.Action(\"Setup\", \"Businesses\", new { id = Model.BusinessId })");
        locationFormSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        locationFormSource.Should().Contain("@T.T(\"Cancel\")");
        locationFormSource.Should().Contain("@T.T(\"Setup\")");
        locationFormSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
    }

    [Fact]
    public void BusinessLocationForm_Should_KeepFieldAndSubmitContractsWired()
    {
        var locationFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessLocationForm.cshtml"));

        locationFormSource.Should().Contain("@T.T(\"BusinessLocationName\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationPostalCode\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationAddressLine1\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationAddressLine2\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationCity\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationRegion\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationCountryCode\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationLatitude\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationLongitude\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationAltitudeMeters\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationOpeningHoursJson\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationInternalNote\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationPrimary\")");
        locationFormSource.Should().Contain("@T.T(\"Save\")");
    }

    [Fact]
    public void BusinessInvitationForm_Should_KeepFieldAndSubmitContractsWired()
    {
        var invitationFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessInvitationForm.cshtml"));

        invitationFormSource.Should().Contain("@T.T(\"BusinessInvitationEmail\")");
        invitationFormSource.Should().Contain("@T.T(\"BusinessInvitationRole\")");
        invitationFormSource.Should().Contain("@T.T(\"BusinessInvitationExpiresInDays\")");
        invitationFormSource.Should().Contain("@T.T(\"BusinessInvitationInternalNote\")");
        invitationFormSource.Should().Contain("@T.T(\"BusinessInvitationSend\")");
        invitationFormSource.Should().Contain("@T.T(\"Cancel\")");
    }

    [Fact]
    public void BusinessForm_Should_KeepInitialOwnerGuidanceRemediationContractsWired()
    {
        var businessFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessForm.cshtml"));

        businessFormSource.Should().Contain("@T.T(\"BusinessFormInitialOwnerHelp\")");
        businessFormSource.Should().Contain("@T.T(\"BusinessFormActiveHelp\")");
        businessFormSource.Should().Contain("@Url.Action(\"Index\", \"Users\")");
        businessFormSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Id })");
        businessFormSource.Should().Contain("@Url.Action(\"Locations\", \"Businesses\", new { businessId = Model.Id })");
        businessFormSource.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = Model.Id })");
        businessFormSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        businessFormSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        businessFormSource.Should().Contain("@T.T(\"Users\")");
        businessFormSource.Should().Contain("@T.T(\"BusinessFormManageMembers\")");
        businessFormSource.Should().Contain("@T.T(\"BusinessFormManageLocations\")");
        businessFormSource.Should().Contain("@T.T(\"BusinessFormManageInvitations\")");
        businessFormSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
        businessFormSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
    }

    [Fact]
    public void BusinessForm_Should_KeepInitialOwnerGuidanceRailWired()
    {
        var businessFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessForm.cshtml"));

        businessFormSource.Should().Contain("@T.T(\"BusinessFormInitialOwnerHelp\")");
        businessFormSource.Should().Contain("@Url.Action(\"Index\", \"Users\")");
        businessFormSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Users\")</a>");
        businessFormSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessSupportQueueTitle\")</a>");
        businessFormSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void BusinessForm_Should_KeepActiveStateGuidanceRailWired()
    {
        var businessFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessForm.cshtml"));

        businessFormSource.Should().Contain("@T.T(\"BusinessFormActiveHelp\")");
        businessFormSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        businessFormSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        businessFormSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessSupportQueueTitle\")</a>");
        businessFormSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void BusinessForm_Should_KeepEditFooterManagementRailWired()
    {
        var businessFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessForm.cshtml"));

        businessFormSource.Should().Contain("@T.T(\"BusinessFormBackToList\")");
        businessFormSource.Should().Contain("@T.T(\"BusinessFormManageMembers\")");
        businessFormSource.Should().Contain("@T.T(\"BusinessFormManageLocations\")");
        businessFormSource.Should().Contain("@T.T(\"BusinessFormManageInvitations\")");
        businessFormSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Id })");
        businessFormSource.Should().Contain("@Url.Action(\"Locations\", \"Businesses\", new { businessId = Model.Id })");
        businessFormSource.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = Model.Id })");
        businessFormSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessSupportQueueTitle\")</a>");
        businessFormSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepAttentionAndApprovalSummaryRailsWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</div>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        indexViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</div>");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { operationalStatus = \"PendingApproval\" })\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</a>");
        indexViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</div>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</a>");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepRowOperationalStatusBadgesWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("if (item.OperationalStatus == Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { operationalStatus = \"PendingApproval\" })\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</a>");
        indexViewSource.Should().Contain("else if (item.OperationalStatus == Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { operationalStatus = \"Suspended\" })\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</a>");
        indexViewSource.Should().NotContain("else if (!item.IsActive)");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepRowReadinessIssueBadgesWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("@BusinessOperationalStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive })\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)</a>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)</a>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</a>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</a>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</a>");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepPendingInvitesSummaryCardActionRailWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites })\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\", flowKey = \"BusinessInvitation\" })\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedInvitationEmails\")</a>");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepMemberSummaryCardActionRailsWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Users\", new { filter = \"Unconfirmed\" })\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</a>");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\", flowKey = \"AccountActivation\" })\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedActivationEmails\")</a>");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"MobileOperations\")\"");
        indexViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Users\", new { filter = \"Locked\" })\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</a>");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\", flowKey = \"PasswordReset\" })\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedPasswordResets\")</a>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MobileOperationsTitle\")</a>");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepMissingOwnerAndLocationSummaryCardActionRailsWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)</div>");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner })\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)</a>");
        indexViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</div>");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation })\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</a>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepApprovedInactiveAndMetadataSummaryCardActionRailsWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)</div>");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive })\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)</a>");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Payments\", \"Billing\")\"");
        indexViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</div>");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail })\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</a>");
        indexViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</div>");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName })\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</a>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Payments\")</a>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepTopOperationalFilterRailWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("class=\"btn btn-sm @(Model.AttentionOnly ? \"btn-warning\" : \"btn-outline-warning\")\"");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { attentionOnly = true })\"");
        indexViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)");
        indexViewSource.Should().Contain("class=\"btn btn-sm @(Model.OperationalStatus == Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval ? \"btn-warning\" : \"btn-outline-warning\")\"");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { operationalStatus = Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval })\"");
        indexViewSource.Should().Contain("@BusinessOperationalStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)");
        indexViewSource.Should().Contain("class=\"btn btn-sm @(Model.OperationalStatus == Darwin.Domain.Enums.BusinessOperationalStatus.Suspended ? \"btn-danger\" : \"btn-outline-danger\")\"");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { operationalStatus = Darwin.Domain.Enums.BusinessOperationalStatus.Suspended })\"");
        indexViewSource.Should().Contain("@BusinessOperationalStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepTopReadinessFilterRailWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("class=\"btn btn-sm @(Model.ReadinessFilter == Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner ? \"btn-warning\" : \"btn-outline-warning\")\"");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner })\"");
        indexViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)");
        indexViewSource.Should().Contain("class=\"btn btn-sm @(Model.ReadinessFilter == Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation ? \"btn-warning\" : \"btn-outline-warning\")\"");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation })\"");
        indexViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)");
        indexViewSource.Should().Contain("class=\"btn btn-sm @(Model.ReadinessFilter == Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail ? \"btn-warning\" : \"btn-outline-warning\")\"");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail })\"");
        indexViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)");
        indexViewSource.Should().Contain("class=\"btn btn-sm @(Model.ReadinessFilter == Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName ? \"btn-warning\" : \"btn-outline-warning\")\"");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName })\"");
        indexViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)");
        indexViewSource.Should().Contain("class=\"btn btn-sm @(Model.ReadinessFilter == Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites ? \"btn-warning\" : \"btn-outline-warning\")\"");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites })\"");
        indexViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        indexViewSource.Should().Contain("class=\"btn btn-sm @(Model.ReadinessFilter == Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive ? \"btn-secondary\" : \"btn-outline-secondary\")\"");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive })\"");
        indexViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\")\"");
        indexViewSource.Should().Contain("@T.T(\"ClearQueueFilters\")");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepSearchAndFilterFormContractWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("<form method=\"get\"");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\")\"");
        indexViewSource.Should().Contain("<input type=\"hidden\" name=\"readinessFilter\" value=\"@Model.ReadinessFilter\" />");
        indexViewSource.Should().Contain("<input type=\"text\" name=\"query\" value=\"@Model.Query\" class=\"form-control\" placeholder=\"@T.T(\"SearchBusinessesPlaceholder\")\" />");
        indexViewSource.Should().Contain("<select name=\"operationalStatus\" asp-items=\"Model.OperationalStatusItems\" class=\"form-select\"></select>");
        indexViewSource.Should().Contain("<select name=\"pageSize\" class=\"form-select\">");
        indexViewSource.Should().Contain("@foreach (var item in Model.PageSizeItems)");
        indexViewSource.Should().Contain("<input type=\"checkbox\" name=\"attentionOnly\" value=\"true\" class=\"form-check-input\" id=\"attentionOnly\" checked=\"@(Model.AttentionOnly ? \"checked\" : null)\" />");
        indexViewSource.Should().Contain("<label class=\"form-check-label\" for=\"attentionOnly\">@T.T(\"NeedsAttentionOnly\")</label>");
        indexViewSource.Should().Contain("<button type=\"submit\" class=\"btn btn-outline-secondary\"><i class=\"fa-solid fa-magnifying-glass\"></i> @T.T(\"Search\")</button>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Reset\")</a>");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepPagerStateContractWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("<pager page=\"Model.Page\"");
        indexViewSource.Should().Contain("page-size=\"Model.PageSize\"");
        indexViewSource.Should().Contain("total=\"Model.Total\"");
        indexViewSource.Should().Contain("asp-controller=\"Businesses\"");
        indexViewSource.Should().Contain("asp-action=\"Index\"");
        indexViewSource.Should().Contain("asp-route-query=\"@Model.Query\"");
        indexViewSource.Should().Contain("asp-route-operationalStatus=\"@Model.OperationalStatus\"");
        indexViewSource.Should().Contain("asp-route-attentionOnly=\"@Model.AttentionOnly\"");
        indexViewSource.Should().Contain("asp-route-readinessFilter=\"@Model.ReadinessFilter\"");
        indexViewSource.Should().Contain("hx-target=\"#businesses-workspace-shell\"");
        indexViewSource.Should().Contain("hx-swap=\"outerHTML\"");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepRowPrimaryActionRailWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Members\", \"Businesses\", new { businessId = item.Id })\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">");
        indexViewSource.Should().Contain("@T.T(\"Members\")");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Subscription\", \"Businesses\", new { businessId = item.Id })\"");
        indexViewSource.Should().Contain("@T.T(\"Subscription\")");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"SubscriptionInvoices\", \"Businesses\", new { businessId = item.Id })\"");
        indexViewSource.Should().Contain("@T.T(\"SubscriptionInvoicesTitle\")");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Invitations\", \"Businesses\", new { businessId = item.Id })\"");
        indexViewSource.Should().Contain("@T.T(\"Invitations\")");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepRowFullAdminActionRailWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"Businesses\", new { id = item.Id })\"");
        indexViewSource.Should().Contain("@T.T(\"Edit\")");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Setup\", \"Businesses\", new { id = item.Id })\"");
        indexViewSource.Should().Contain("@T.T(\"Setup\")");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Details\", \"BusinessCommunications\", new { businessId = item.Id })\"");
        indexViewSource.Should().Contain("@T.T(\"CommunicationOps\")");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { businessId = item.Id })\"");
        indexViewSource.Should().Contain("@T.T(\"EmailDeliveryAudits\")");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"ChannelAudits\", \"BusinessCommunications\", new { businessId = item.Id })\"");
        indexViewSource.Should().Contain("@T.T(\"SmsWhatsAppAuditsTitle\")");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Payments\", \"Billing\", new { businessId = item.Id })\"");
        indexViewSource.Should().Contain("@T.T(\"Payments\")");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Refunds\", \"Billing\", new { businessId = item.Id })\"");
        indexViewSource.Should().Contain("@T.T(\"Refunds\")");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"FinancialAccounts\", \"Billing\", new { businessId = item.Id })\"");
        indexViewSource.Should().Contain("@T.T(\"FinancialAccountsTitle\")");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Expenses\", \"Billing\", new { businessId = item.Id })\"");
        indexViewSource.Should().Contain("@T.T(\"ExpensesTitle\")");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"JournalEntries\", \"Billing\", new { businessId = item.Id })\"");
        indexViewSource.Should().Contain("@T.T(\"JournalEntriesTitle\")");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"TaxCompliance\", \"Billing\")\"");
        indexViewSource.Should().Contain("@T.T(\"TaxComplianceTitle\")");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"OwnerOverrideAudits\", \"Businesses\", new { businessId = item.Id })\"");
        indexViewSource.Should().Contain("@T.T(\"OwnerOverrideAuditTitle\")");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Locations\", \"Businesses\", new { businessId = item.Id })\"");
        indexViewSource.Should().Contain("@T.T(\"Locations\")");
        indexViewSource.Should().Contain("data-action=\"@Url.Action(\"Delete\", \"Businesses\")\"");
        indexViewSource.Should().Contain("data-rowversion=\"@Convert.ToBase64String(item.RowVersion)\"");
        indexViewSource.Should().Contain("@T.T(\"Archive\")");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepRowIdentityDrillInWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("<div class=\"fw-semibold\">");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"Businesses\", new { id = item.Id })\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@item.Name</a>");
        indexViewSource.Should().Contain("@if (!string.IsNullOrWhiteSpace(item.LegalName))");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@item.LegalName</a>");
    }

    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepRowOwnerMemberAndLocationDrillInsWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("@if (item.ActiveOwnerCount > 0)");
        indexViewSource.Should().Contain("class=\"badge text-bg-success text-decoration-none\"");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Members\", \"Businesses\", new { businessId = item.Id })\"");
        indexViewSource.Should().Contain("@item.ActiveOwnerCount</a>");
        indexViewSource.Should().Contain("class=\"badge text-bg-warning text-decoration-none\"");
        indexViewSource.Should().Contain("@T.T(\"MissingText\")</a>");
        indexViewSource.Should().Contain("@item.MemberCount</a>");
        indexViewSource.Should().Contain("@if (item.LocationCount > 0)");
        indexViewSource.Should().Contain("hx-get=\"@Url.Action(\"Locations\", \"Businesses\", new { businessId = item.Id })\"");
        indexViewSource.Should().Contain("@item.LocationCount</a>");
    }

    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepOperationalAndReadinessSummaryRailsWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</div>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</div>");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { operationalStatus = \"PendingApproval\" })\"");
        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</div>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</a>");
        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</div>");
        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</div>");
        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</div>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</a>");
    }

    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepRowOperationalStatusBadgesWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("if (item.OperationalStatus == Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { operationalStatus = \"PendingApproval\" })\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</a>");
        merchantReadinessViewSource.Should().Contain("else if (item.OperationalStatus == Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { operationalStatus = \"Suspended\" })\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</a>");
        merchantReadinessViewSource.Should().Contain("else if (!item.IsActive)");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive })\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)</a>");
    }

    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepRowReadinessIssueBadgesWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessSupportQueueTitle\")</a>");
    }

    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepRowIdentitySetupAndSubscriptionDrillInsWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"Businesses\", new { id = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@item.Name</a>");
        merchantReadinessViewSource.Should().Contain("@if (!string.IsNullOrWhiteSpace(item.LegalName))");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@item.LegalName</a>");
        merchantReadinessViewSource.Should().Contain("@item.InvitationCount @InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        merchantReadinessViewSource.Should().Contain("@if (setupMissing)");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Setup\", \"Businesses\", new { id = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"MerchantReadinessSetupMissing\")</a>");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Members\", \"Businesses\", new { businessId = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)</a>");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Locations\", \"Businesses\", new { businessId = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</a>");
        merchantReadinessViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</a>");
        merchantReadinessViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</a>");
        merchantReadinessViewSource.Should().Contain("@if (item.HasSubscription)");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Subscription\", \"Businesses\", new { businessId = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@item.SubscriptionStatus</a>");
        merchantReadinessViewSource.Should().Contain("@T.T(\"BusinessSubscriptionCancelAtPeriodEnd\")</a>");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"SubscriptionInvoices\", \"Businesses\", new { businessId = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"BusinessSubscriptionNoActivePlan\")</a>");
    }

    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepRowActionRailWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"Businesses\", new { id = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"Edit\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Setup\", \"Businesses\", new { id = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"CommonSetup\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Subscription\", \"Businesses\", new { businessId = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"Subscription\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"SubscriptionInvoices\", \"Businesses\", new { businessId = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"SubscriptionInvoicesTitle\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Members\", \"Businesses\", new { businessId = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"Members\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Invitations\", \"Businesses\", new { businessId = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"Invitations\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"OwnerOverrideAudits\", \"Businesses\", new { businessId = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"OwnerOverrideAuditTitle\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Locations\", \"Businesses\", new { businessId = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"Locations\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Details\", \"BusinessCommunications\", new { businessId = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"CommunicationOps\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { businessId = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"EmailDeliveryAudits\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"ChannelAudits\", \"BusinessCommunications\", new { businessId = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"SmsWhatsAppAuditsTitle\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Payments\", \"Billing\", new { businessId = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"Payments\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Refunds\", \"Billing\", new { businessId = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"Refunds\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"FinancialAccounts\", \"Billing\", new { businessId = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"FinancialAccountsTitle\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Expenses\", \"Billing\", new { businessId = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"ExpensesTitle\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"JournalEntries\", \"Billing\", new { businessId = item.Id })\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"JournalEntriesTitle\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"TaxCompliance\", \"Billing\")\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"TaxComplianceTitle\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"SupportQueue\", \"Businesses\")\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
    }

    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepHeaderActionRailWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("<h1 class=\"mb-1\"><i class=\"fa-solid fa-store me-2\"></i>@T.T(\"MerchantReadinessTitle\")</h1>");
        merchantReadinessViewSource.Should().Contain("@T.T(\"MerchantReadinessIntro\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"SupportQueue\", \"Businesses\")\"");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { attentionOnly = true })\"");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Payments\", \"Billing\")\"");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"TaxCompliance\", \"Billing\")\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessSupportQueueTitle\")</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Payments\")</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"TaxComplianceTitle\")</a>");
    }

    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepEmptyStateFallbackRailWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("@if (Model.Items.Count == 0)");
        merchantReadinessViewSource.Should().Contain("@T.T(\"MerchantReadinessEmptyState\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\")\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessesTitle\")</a>");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"SupportQueue\", \"Businesses\")\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessSupportQueueTitle\")</a>");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Payments\", \"Billing\")\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Payments\")</a>");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"BusinessCommunications\")\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"CommunicationOps\")</a>");
    }

    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepPlaybookShellRailsWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("<span>@T.T(\"MerchantReadinessPlaybooksTitle\")</span>");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"SupportQueue\", \"Businesses\")\"");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Payments\", \"Billing\")\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessSupportQueueTitle\")</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Payments\")</a>");
        merchantReadinessViewSource.Should().Contain("<div class=\"d-flex gap-2 flex-wrap mt-3\">");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { attentionOnly = true })\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
    }

    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepQueueHeaderCtaWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("<span>@T.T(\"MerchantReadinessQueueTitle\")</span>");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"BusinessCommunications\")\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"CommunicationOps\")</a>");
    }

    [Fact]
    public void SupportQueueWorkspace_Should_KeepHeaderActionRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("<h1 class=\"mb-1\"><i class=\"fa-solid fa-life-ring me-2\"></i>@T.T(\"BusinessSupportQueueTitle\")</h1>");
        supportQueueSource.Should().Contain("@T.T(\"BusinessSupportQueueIntro\")");
        supportQueueSource.Should().Contain("hx-get=\"@Url.Action(\"MerchantReadiness\", \"Businesses\")\"");
        supportQueueSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { attentionOnly = true })\"");
        supportQueueSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"BusinessCommunications\")\"");
        supportQueueSource.Should().Contain("hx-get=\"@Url.Action(\"Payments\", \"Billing\")\"");
        supportQueueSource.Should().Contain("hx-get=\"@Url.Action(\"TaxCompliance\", \"Billing\")\"");
        supportQueueSource.Should().Contain("hx-get=\"@Url.Action(\"Refunds\", \"Billing\")\"");
        supportQueueSource.Should().Contain("hx-get=\"@Url.Action(\"FinancialAccounts\", \"Billing\")\"");
        supportQueueSource.Should().Contain("hx-get=\"@Url.Action(\"Expenses\", \"Billing\")\"");
        supportQueueSource.Should().Contain("hx-get=\"@Url.Action(\"JournalEntries\", \"Billing\")\"");
        supportQueueSource.Should().Contain("hx-get=\"@Url.Action(\"OwnerOverrideAudits\", \"Businesses\")\"");
        supportQueueSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"MobileOperations\")\"");
        supportQueueSource.Should().Contain("hx-get=\"@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\" })\"");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Communications\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"FailedEmails\")</a>");
    }

    [Fact]
    public void SupportQueueAttentionFragment_Should_KeepHeaderRailWired()
    {
        var attentionFragmentSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueAttentionBusinesses.cshtml"));

        attentionFragmentSource.Should().Contain("<span>@T.T(\"AttentionBusinesses\")</span>");
        attentionFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"SupportQueueAttentionFragment\", \"Businesses\")\"");
        attentionFragmentSource.Should().Contain("hx-target=\"#support-queue-attention\"");
        attentionFragmentSource.Should().Contain("@T.T(\"Refresh\")");
        attentionFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { attentionOnly = true })\"");
        attentionFragmentSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
    }

    [Fact]
    public void SupportQueueAttentionFragment_Should_KeepEmptyStateAndTableShellWired()
    {
        var attentionFragmentSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueAttentionBusinesses.cshtml"));

        attentionFragmentSource.Should().Contain("<div class=\"table-responsive\">");
        attentionFragmentSource.Should().Contain("<table class=\"table table-sm align-middle mb-0\">");
        attentionFragmentSource.Should().Contain("<th>@T.T(\"BusinessLabel\")</th>");
        attentionFragmentSource.Should().Contain("<th>@T.T(\"Status\")</th>");
        attentionFragmentSource.Should().Contain("<th>@T.T(\"Signals\")</th>");
        attentionFragmentSource.Should().Contain("<th class=\"text-end\">@T.T(\"Actions\")</th>");
        attentionFragmentSource.Should().Contain("@if (Model.Count == 0)");
        attentionFragmentSource.Should().Contain("<tr><td colspan=\"4\" class=\"text-center text-muted py-4\">@T.T(\"NoAttentionBusinessesQueued\")</td></tr>");
        attentionFragmentSource.Should().Contain("else");
        attentionFragmentSource.Should().Contain("foreach (var item in Model)");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepHeaderRefreshRailWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div id=\"support-queue-summary\" class=\"position-relative\">");
        summaryViewSource.Should().Contain("<div class=\"d-flex justify-content-end mb-2\">");
        summaryViewSource.Should().Contain("hx-get=\"@Url.Action(\"SupportQueueSummaryFragment\", \"Businesses\")\"");
        summaryViewSource.Should().Contain("hx-target=\"#support-queue-summary\"");
        summaryViewSource.Should().Contain("hx-swap=\"outerHTML\"");
        summaryViewSource.Should().Contain("@T.T(\"RefreshSummary\")");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepSummaryGridShellWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"row g-3 mb-4\">");
        summaryViewSource.Should().Contain("@Model.AttentionBusinessCount");
        summaryViewSource.Should().Contain("@Model.PendingApprovalBusinessCount");
        summaryViewSource.Should().Contain("@Model.OpenInvitationCount");
        summaryViewSource.Should().Contain("@Model.PendingActivationMemberCount");
        summaryViewSource.Should().Contain("@Model.ApprovedInactiveBusinessCount");
        summaryViewSource.Should().Contain("@Model.MissingPrimaryLocationBusinessCount");
        summaryViewSource.Should().Contain("@Model.MissingContactEmailBusinessCount");
        summaryViewSource.Should().Contain("@Model.MissingLegalNameBusinessCount");
        summaryViewSource.Should().Contain("@Model.SuspendedBusinessCount");
        summaryViewSource.Should().Contain("@Model.MissingOwnerBusinessCount");
        summaryViewSource.Should().Contain("@Model.LockedMemberCount");
        summaryViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)");
        summaryViewSource.Should().Contain("@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)");
        summaryViewSource.Should().Contain("@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)");
        summaryViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        summaryViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        summaryViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepAttentionAndApprovalCardRailsWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("@Model.AttentionBusinessCount");
        summaryViewSource.Should().Contain("@Model.PendingApprovalBusinessCount");
        summaryViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)");
        summaryViewSource.Should().Contain("@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { attentionOnly = true })");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { operationalStatus = Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval })");
        summaryViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepReadinessAndGovernanceCardRailsWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("@Model.ApprovedInactiveBusinessCount");
        summaryViewSource.Should().Contain("@Model.MissingPrimaryLocationBusinessCount");
        summaryViewSource.Should().Contain("@Model.MissingContactEmailBusinessCount");
        summaryViewSource.Should().Contain("@Model.MissingLegalNameBusinessCount");
        summaryViewSource.Should().Contain("@Model.SuspendedBusinessCount");
        summaryViewSource.Should().Contain("@Model.MissingOwnerBusinessCount");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)");
        summaryViewSource.Should().Contain("@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive })");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation })");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail })");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName })");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner })");
        summaryViewSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { operationalStatus = \"Suspended\" })");
        summaryViewSource.Should().Contain("@Url.Action(\"Payments\", \"Billing\")");
        summaryViewSource.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\" })");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Payments\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"FailedEmails\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Locations\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Edit\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void SupportQueueWorkspace_Should_KeepFailedEmailDrillInRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\" })");
        supportQueueSource.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\", flowKey = \"BusinessInvitation\" })");
        supportQueueSource.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\", flowKey = \"AccountActivation\" })");
        supportQueueSource.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\", flowKey = \"PasswordReset\" })");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"FailedEmails\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedInvitationEmails\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedActivationEmails\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedPasswordResets\")</a>");
    }

    [Fact]
    public void SupportQueueWorkspace_Should_KeepGovernanceAndReadinessChipRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive })");
        supportQueueSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { operationalStatus = Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval })");
        supportQueueSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { operationalStatus = Darwin.Domain.Enums.BusinessOperationalStatus.Suspended })");
        supportQueueSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner })");
        supportQueueSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites })");
        supportQueueSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation })");
        supportQueueSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail })");
        supportQueueSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName })");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</a>");
    }

    [Fact]
    public void SupportQueueWorkspace_Should_KeepWorkspacePivotRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        supportQueueSource.Should().Contain("@Url.Action(\"Index\", \"Businesses\", new { attentionOnly = true })");
        supportQueueSource.Should().Contain("@Url.Action(\"Index\", \"BusinessCommunications\")");
        supportQueueSource.Should().Contain("@Url.Action(\"OwnerOverrideAudits\", \"Businesses\")");
        supportQueueSource.Should().Contain("@Url.Action(\"Index\", \"MobileOperations\")");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Communications\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OwnerOverrideAuditTitle\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MobileOperationsTitle\")</a>");
    }

    [Fact]
    public void SupportQueueWorkspace_Should_KeepBillingOperationsRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("@Url.Action(\"Payments\", \"Billing\")");
        supportQueueSource.Should().Contain("@Url.Action(\"TaxCompliance\", \"Billing\")");
        supportQueueSource.Should().Contain("@Url.Action(\"Refunds\", \"Billing\")");
        supportQueueSource.Should().Contain("@Url.Action(\"FinancialAccounts\", \"Billing\")");
        supportQueueSource.Should().Contain("@Url.Action(\"Expenses\", \"Billing\")");
        supportQueueSource.Should().Contain("@Url.Action(\"JournalEntries\", \"Billing\")");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Payments\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"TaxComplianceTitle\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Refunds\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"FinancialAccountsTitle\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"ExpensesTitle\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"JournalEntriesTitle\")</a>");
    }

    [Fact]
    public void SupportQueueAttentionFragment_Should_KeepRowDrillInsAndActionRailWired()
    {
        var attentionFragmentSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueAttentionBusinesses.cshtml"));

        attentionFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"SupportQueueAttentionFragment\", \"Businesses\")\"");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        attentionFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"Businesses\", new { id = item.Id })\"");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@item.Name</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@item.LegalName</a>");
        attentionFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { operationalStatus = \"PendingApproval\" })\"");
        attentionFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { operationalStatus = \"Suspended\" })\"");
        attentionFragmentSource.Should().Contain("@BusinessOperationalStatusLabel(item.OperationalStatus)</span>");
        attentionFragmentSource.Should().Contain("string BusinessOperationalStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus status) => status switch");
        attentionFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Businesses\", new { readinessFilter = Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive })\"");
        attentionFragmentSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)");
        attentionFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"Members\", \"Businesses\", new { businessId = item.Id })\"");
        attentionFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"Locations\", \"Businesses\", new { businessId = item.Id })\"");
        attentionFragmentSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)");
        attentionFragmentSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)");
        attentionFragmentSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)");
        attentionFragmentSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)");
        attentionFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"Invitations\", \"Businesses\", new { businessId = item.Id, filter = \"Open\" })\"");
        attentionFragmentSource.Should().Contain("@item.InvitationCount @InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        attentionFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"Setup\", \"Businesses\", new { id = item.Id })\"");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Setup\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Members\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Invites\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Locations\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"CommunicationOps\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Payments\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Subscription\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"SubscriptionInvoicesTitle\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"EmailDeliveryAudits\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"SmsWhatsAppAuditsTitle\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"TaxComplianceTitle\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Refunds\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"FinancialAccountsTitle\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"ExpensesTitle\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"JournalEntriesTitle\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OwnerOverrideAuditTitle\")</a>");
    }

    [Fact]
    public void SupportQueueWorkspace_Should_KeepPlaybookShellRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("<div class=\"card mb-3\">");
        supportQueueSource.Should().Contain("<div class=\"card-header\">@T.T(\"BusinessesOperationsPlaybooksTitle\")</div>");
        supportQueueSource.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        supportQueueSource.Should().Contain("<th>@T.T(\"Playbook\")</th>");
        supportQueueSource.Should().Contain("<th>@T.T(\"TaxComplianceScopeColumn\")</th>");
        supportQueueSource.Should().Contain("<th>@T.T(\"OperatorAction\")</th>");
        supportQueueSource.Should().Contain("<th>@T.T(\"UsersPlaybooksFollowUpColumn\")</th>");
        supportQueueSource.Should().Contain("href=\"@playbook.QueueActionUrl\"");
        supportQueueSource.Should().Contain("href=\"@playbook.FollowUpUrl\"");
        supportQueueSource.Should().Contain("hx-target=\"#business-support-queue-workspace-shell\"");
        supportQueueSource.Should().Contain(">@playbook.Title</a>");
        supportQueueSource.Should().Contain(">@playbook.ScopeNote</a>");
        supportQueueSource.Should().Contain(">@playbook.OperatorAction</a>");
        supportQueueSource.Should().Contain(">@playbook.QueueActionLabel</a>");
        supportQueueSource.Should().Contain(">@playbook.FollowUpLabel</a>");
        supportQueueSource.Should().Contain("@if (!string.IsNullOrWhiteSpace(playbook.QueueActionUrl))");
        supportQueueSource.Should().Contain("@if (!string.IsNullOrWhiteSpace(playbook.FollowUpUrl))");
    }

    [Fact]
    public void SupportQueueWorkspace_Should_KeepFragmentCompositionRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("<div id=\"business-support-queue-workspace-shell\">");
        supportQueueSource.Should().Contain("<partial name=\"~/Views/Businesses/_SupportQueueSummary.cshtml\" model=\"Model.Summary\" />");
        supportQueueSource.Should().Contain("<div class=\"row g-3\">");
        supportQueueSource.Should().Contain("<div class=\"col-xl-7\">");
        supportQueueSource.Should().Contain("<partial name=\"~/Views/Businesses/_SupportQueueAttentionBusinesses.cshtml\" model=\"Model.AttentionBusinesses\" />");
        supportQueueSource.Should().Contain("<div class=\"col-xl-5\">");
        supportQueueSource.Should().Contain("<partial name=\"~/Views/Businesses/_SupportQueueFailedEmails.cshtml\" model=\"Model.FailedEmails\" />");
    }

    [Fact]
    public void SupportQueueFailedEmailsFragment_Should_KeepHeaderAndEmptyStateRailsWired()
    {
        var failedEmailsFragmentSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueFailedEmails.cshtml"));

        failedEmailsFragmentSource.Should().Contain("<span>@T.T(\"RecentFailedEmailEvents\")</span>");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"SupportQueueFailedEmailsFragment\", \"Businesses\")\"");
        failedEmailsFragmentSource.Should().Contain("hx-target=\"#support-queue-failed-emails\"");
        failedEmailsFragmentSource.Should().Contain("@T.T(\"Refresh\")");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\" })\"");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"FailedEmails\")</a>");
        failedEmailsFragmentSource.Should().Contain("@if (Model.Count == 0)");
        failedEmailsFragmentSource.Should().Contain("@T.T(\"NoFailedEmailEventsQueued\")");
    }

    [Fact]
    public void SupportQueueFailedEmailsFragment_Should_KeepRowDrillInsAndRemediationRailsWired()
    {
        var failedEmailsFragmentSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueFailedEmails.cshtml"));

        failedEmailsFragmentSource.Should().Contain("@FlowLabel(item.FlowKey)");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"Businesses\", new { id = item.BusinessId })\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"Details\", \"BusinessCommunications\", new { businessId = item.BusinessId })\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Users\", new { q = item.RecipientEmail })\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\", flowKey = item.FlowKey, businessId = item.BusinessId })\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"Setup\", \"Businesses\", new { id = item.BusinessId })\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"Locations\", \"Businesses\", new { businessId = item.BusinessId })\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"Members\", \"Businesses\", new { businessId = item.BusinessId, filter = \"Attention\" })\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { businessId = item.BusinessId })\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@Url.Action(\"ChannelAudits\", \"BusinessCommunications\", new { businessId = item.BusinessId })\"");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Edit\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Setup\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Locations\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Members\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"CommunicationOps\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"EmailDeliveryAudits\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"SmsWhatsAppAuditsTitle\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Payments\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Subscription\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"SubscriptionInvoicesTitle\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"TaxComplianceTitle\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Refunds\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"FinancialAccountsTitle\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"ExpensesTitle\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"JournalEntriesTitle\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OwnerOverrideAuditTitle\")</a>");
        failedEmailsFragmentSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Open)");
        failedEmailsFragmentSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        failedEmailsFragmentSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenMembers\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenUsers\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MobileOperationsTitle\")</a>");
    }

    [Fact]
    public void BusinessMemberEditorShell_Should_KeepOwnerOverrideRemediationContractsWired()
    {
        var memberShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberEditorShell.cshtml"));

        memberShellSource.Should().Contain("@T.T(\"BusinessMemberLastActiveOwnerWarning\")");
        memberShellSource.Should().Contain("@T.T(\"BusinessMemberControlledOwnerOverrideTitle\")");
        memberShellSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.BusinessId })");
        memberShellSource.Should().Contain("@Url.Action(\"OwnerOverrideAudits\", \"Businesses\", new { businessId = Model.BusinessId })");
        memberShellSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        memberShellSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        memberShellSource.Should().Contain("@T.T(\"CommonMembers\")");
        memberShellSource.Should().Contain("@T.T(\"BusinessMembersOwnerOverrideAuditAction\")");
        memberShellSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
        memberShellSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
    }

    [Fact]
    public void BusinessMemberEditorShell_Should_KeepSupportActionsCardWired()
    {
        var memberShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberEditorShell.cshtml"));

        memberShellSource.Should().Contain("@T.T(\"BusinessMemberSupportActionsTitle\")");
        memberShellSource.Should().Contain("@T.T(\"BusinessMemberEmailConfirmedBadge\")");
        memberShellSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        memberShellSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        memberShellSource.Should().Contain("@T.T(\"BusinessMemberUnlockedBadge\")");
        memberShellSource.Should().Contain("@Url.Action(\"Edit\", \"Users\", new { id = Model.UserId })");
        memberShellSource.Should().Contain("@Url.Action(\"Index\", \"MobileOperations\", new { q = Model.UserEmail })");
        memberShellSource.Should().Contain("@Url.Action(\"Accounts\", \"Loyalty\", new { q = Model.UserEmail })");
        memberShellSource.Should().Contain("@Url.Action(\"StaffAccessBadge\", \"Businesses\", new { id = Model.Id })");
        memberShellSource.Should().Contain("@Url.Action(\"SendMemberActivationEmail\", \"Businesses\")");
        memberShellSource.Should().Contain("@Url.Action(\"ConfirmMemberEmail\", \"Businesses\")");
        memberShellSource.Should().Contain("@Url.Action(\"SendMemberPasswordReset\", \"Businesses\")");
        memberShellSource.Should().Contain("@Url.Action(\"UnlockMemberUser\", \"Businesses\")");
        memberShellSource.Should().Contain("@Url.Action(\"LockMemberUser\", \"Businesses\")");
    }

    [Fact]
    public void BusinessMemberEditorShell_Should_KeepFormPostContractWired()
    {
        var memberShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberEditorShell.cshtml"));

        memberShellSource.Should().Contain("<form asp-action=\"@(isCreate ? \"CreateMember\" : \"EditMember\")\"");
        memberShellSource.Should().Contain("hx-post=\"@Url.Action(isCreate ? \"CreateMember\" : \"EditMember\", \"Businesses\")\"");
        memberShellSource.Should().Contain("@Html.AntiForgeryToken()");
        memberShellSource.Should().Contain("<input type=\"hidden\" asp-for=\"BusinessId\" />");
        memberShellSource.Should().Contain("<input type=\"hidden\" asp-for=\"UserId\" />");
        memberShellSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        memberShellSource.Should().Contain("<partial name=\"_BusinessMemberForm\" model=\"Model\" />");
    }

    [Fact]
    public void BusinessMemberEditorShell_Should_KeepTopWorkspacePivotWired()
    {
        var memberShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberEditorShell.cshtml"));

        memberShellSource.Should().Contain("hx-target=\"#business-member-editor-shell\"");
        memberShellSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.BusinessId })");
        memberShellSource.Should().Contain("@T.T(\"BackToMembersAction\")");
    }

    [Fact]
    public void BusinessMemberEditorShell_Should_KeepFooterWorkspacePivotsWired()
    {
        var memberShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberEditorShell.cshtml"));

        memberShellSource.Should().Contain("<div class=\"d-flex gap-2 flex-wrap mt-4\">");
        memberShellSource.Should().Contain("hx-target=\"#business-member-editor-shell\"");
        memberShellSource.Should().Contain("hx-push-url=\"true\">@T.T(\"CommonMembers\")</a>");
        memberShellSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessSupportQueueTitle\")</a>");
        memberShellSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void BusinessMemberEditorShell_Should_KeepOwnerOverrideRemediationRailsWired()
    {
        var memberShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberEditorShell.cshtml"));

        memberShellSource.Should().Contain("@T.T(\"BusinessMemberLastActiveOwnerWarning\")");
        memberShellSource.Should().Contain("@T.T(\"BusinessMemberControlledOwnerOverrideTitle\")");
        memberShellSource.Should().Contain("@T.T(\"BusinessMemberControlledOwnerOverrideNote\")");
        memberShellSource.Should().Contain("@Url.Action(\"OwnerOverrideAudits\", \"Businesses\", new { businessId = Model.BusinessId })");
        memberShellSource.Should().Contain("@T.T(\"BusinessMembersOwnerOverrideAuditAction\")");
        memberShellSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessSupportQueueTitle\")</a>");
        memberShellSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }

    [Fact]
    public void BusinessMemberEditorShell_Should_KeepOwnerOverrideForceDeleteContractWired()
    {
        var memberShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberEditorShell.cshtml"));

        memberShellSource.Should().Contain("<form asp-action=\"ForceDeleteMember\"");
        memberShellSource.Should().Contain("hx-post=\"@Url.Action(\"ForceDeleteMember\", \"Businesses\")\"");
        memberShellSource.Should().Contain("<input type=\"hidden\" name=\"rowVersion\" value=\"@Convert.ToBase64String(Model.RowVersion)\" />");
        memberShellSource.Should().Contain("@T.T(\"BusinessMemberOverrideReasonLabel\")");
        memberShellSource.Should().Contain("placeholder=\"@T.T(\"BusinessMemberOverrideReasonPlaceholder\")\"");
        memberShellSource.Should().Contain("@T.T(\"BusinessMemberForceRemoveLastOwnerAction\")");
    }

    [Fact]
    public void BusinessesController_Should_KeepStaffAccessBadgeWorkspaceContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> StaffAccessBadge(Guid id, CancellationToken ct = default)");
        controllerSource.Should().Contain("var dto = await _getBusinessMemberForEdit.HandleAsync(id, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessMemberNotFound\");");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(dto.BusinessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("var issuedAtUtc = DateTime.UtcNow;");
        controllerSource.Should().Contain("var expiresAtUtc = issuedAtUtc.AddMinutes(2);");
        controllerSource.Should().Contain("var payload = BuildStaffAccessBadgePayload(dto, business, issuedAtUtc, expiresAtUtc);");
        controllerSource.Should().Contain("var vm = new BusinessStaffAccessBadgeVm");
        controllerSource.Should().Contain("BadgePayload = payload,");
        controllerSource.Should().Contain("BadgeImageDataUrl = BuildQrCodeDataUrl(payload)");
        controllerSource.Should().Contain("return RenderStaffAccessBadgeWorkspace(vm);");
        controllerSource.Should().Contain("private IActionResult RenderStaffAccessBadgeWorkspace(BusinessStaffAccessBadgeVm vm)");
        controllerSource.Should().Contain("return PartialView(\"StaffAccessBadge\", vm);");
        controllerSource.Should().Contain("return View(\"StaffAccessBadge\", vm);");
    }

    [Fact]
    public void BusinessesController_Should_KeepMemberSupportActionLaneWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SendMemberActivationEmail(");
        controllerSource.Should().Contain("var result = await _requestEmailConfirmation.HandleAsync(");
        controllerSource.Should().Contain("? T(\"BusinessMemberActivationEmailSent\")");
        controllerSource.Should().Contain(": (result.Error ?? T(\"BusinessMemberActivationEmailFailed\"));");

        controllerSource.Should().Contain("public async Task<IActionResult> ConfirmMemberEmail(");
        controllerSource.Should().Contain("var result = await _confirmUserEmail.HandleAsync(new UserAdminActionDto");
        controllerSource.Should().Contain("? T(\"BusinessMemberEmailConfirmed\")");
        controllerSource.Should().Contain(": (result.Error ?? T(\"BusinessMemberEmailConfirmFailed\"));");

        controllerSource.Should().Contain("public async Task<IActionResult> SendMemberPasswordReset(");
        controllerSource.Should().Contain("var result = await _requestPasswordReset.HandleAsync(");
        controllerSource.Should().Contain("? T(\"BusinessMemberPasswordResetSent\")");
        controllerSource.Should().Contain(": (result.Error ?? T(\"BusinessMemberPasswordResetFailed\"));");

        controllerSource.Should().Contain("public async Task<IActionResult> LockMemberUser(");
        controllerSource.Should().Contain("var result = await _lockUser.HandleAsync(new UserAdminActionDto");
        controllerSource.Should().Contain("? T(\"BusinessMemberAccountLocked\")");
        controllerSource.Should().Contain(": (result.Error ?? T(\"BusinessMemberAccountLockFailed\"));");

        controllerSource.Should().Contain("public async Task<IActionResult> UnlockMemberUser(");
        controllerSource.Should().Contain("var result = await _unlockUser.HandleAsync(new UserAdminActionDto");
        controllerSource.Should().Contain("? T(\"BusinessMemberAccountUnlocked\")");
        controllerSource.Should().Contain(": (result.Error ?? T(\"BusinessMemberAccountUnlockFailed\"));");

        controllerSource.Should().Contain("var member = await _getBusinessMemberForEdit.HandleAsync(id, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessMemberNotFound\");");
        controllerSource.Should().Contain("TempData[result.Succeeded ? \"Success\" : \"Error\"] = result.Succeeded");
        controllerSource.Should().Contain("return RedirectMemberSupport(returnToEdit, id, businessId);");
        controllerSource.Should().Contain("private IActionResult RedirectMemberSupport(bool returnToEdit, Guid membershipId, Guid businessId)");
        controllerSource.Should().Contain("? RedirectOrHtmx(nameof(EditMember), new { id = membershipId })");
        controllerSource.Should().Contain(": RedirectOrHtmx(nameof(Members), new { businessId });");
    }

    [Fact]
    public void BusinessesController_Should_KeepForceDeleteMemberOverrideActionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> ForceDeleteMember(");
        controllerSource.Should().Contain("await _deleteBusinessMember.HandleAsync(new BusinessMemberDeleteDto");
        controllerSource.Should().Contain("AllowLastOwnerOverride = true,");
        controllerSource.Should().Contain("OverrideReason = overrideReason,");
        controllerSource.Should().Contain("OverrideActorDisplayName = GetCurrentActorDisplayName()");
        controllerSource.Should().Contain("RowVersion = rowVersion ?? Array.Empty<byte>(),");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessMemberRemovedOverride\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Members), new { businessId });");
        controllerSource.Should().Contain("ModelState.AddModelError(string.Empty, ex.Message);");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(EditMember), new { id });");
    }

    [Fact]
    public void BusinessesController_Should_KeepStaffAccessBadgePayloadAndQrContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private static string BuildStaffAccessBadgePayload(BusinessMemberDetailDto member, BusinessContextVm business, DateTime issuedAtUtc, DateTime expiresAtUtc)");
        controllerSource.Should().Contain("Type = \"staff-access-badge\",");
        controllerSource.Should().Contain("Version = 1,");
        controllerSource.Should().Contain("BusinessId = business.Id,");
        controllerSource.Should().Contain("BusinessName = business.Name,");
        controllerSource.Should().Contain("OperatorEmail = member.UserEmail,");
        controllerSource.Should().Contain("Role = member.Role.ToString(),");
        controllerSource.Should().Contain("IssuedAtUtc = issuedAtUtc,");
        controllerSource.Should().Contain("ExpiresAtUtc = expiresAtUtc,");
        controllerSource.Should().Contain("Nonce = Guid.NewGuid().ToString(\"N\")");
        controllerSource.Should().Contain("return JsonSerializer.Serialize(payload);");

        controllerSource.Should().Contain("private static string BuildQrCodeDataUrl(string payload)");
        controllerSource.Should().Contain("using var generator = new QRCodeGenerator();");
        controllerSource.Should().Contain("using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);");
        controllerSource.Should().Contain("var png = new PngByteQRCode(data);");
        controllerSource.Should().Contain("var bytes = png.GetGraphic(20);");
        controllerSource.Should().Contain("return $\"data:image/png;base64,{Convert.ToBase64String(bytes)}\";");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessMemberSummaryAndPlaybookHelperContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private async Task<BusinessMemberOpsSummaryVm> BuildBusinessMemberOpsSummaryAsync(Guid businessId, CancellationToken ct)");
        controllerSource.Should().Contain("var (_, totalCount) = await _getBusinessMembersPage.HandleAsync(businessId, 1, 1, null, BusinessMemberSupportFilter.All, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (_, pendingActivationCount) = await _getBusinessMembersPage.HandleAsync(businessId, 1, 1, null, BusinessMemberSupportFilter.PendingActivation, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (_, lockedCount) = await _getBusinessMembersPage.HandleAsync(businessId, 1, 1, null, BusinessMemberSupportFilter.Locked, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (_, attentionCount) = await _getBusinessMembersPage.HandleAsync(businessId, 1, 1, null, BusinessMemberSupportFilter.Attention, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("TotalCount = totalCount,");
        controllerSource.Should().Contain("PendingActivationCount = pendingActivationCount,");
        controllerSource.Should().Contain("LockedCount = lockedCount,");
        controllerSource.Should().Contain("AttentionCount = attentionCount");

        controllerSource.Should().Contain("private List<BusinessMemberPlaybookVm> BuildBusinessMemberPlaybooks(Guid businessId)");
        controllerSource.Should().Contain("QueueLabel = T(\"PendingActivation\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Members\", \"Businesses\", new { businessId, filter = BusinessMemberSupportFilter.PendingActivation }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"Index\", \"MobileOperations\") ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"Locked\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Members\", \"Businesses\", new { businessId, filter = BusinessMemberSupportFilter.Locked }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"Index\", \"Users\", new { filter = \"Locked\" }) ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"MissingActiveOwner\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Members\", \"Businesses\", new { businessId, filter = BusinessMemberSupportFilter.Attention }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"OwnerOverrideAudits\", \"Businesses\", new { businessId }) ?? string.Empty");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessInvitationSummaryAndPlaybookHelperContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private async Task<BusinessInvitationOpsSummaryVm> BuildBusinessInvitationOpsSummaryAsync(Guid businessId, CancellationToken ct)");
        controllerSource.Should().Contain("var (_, totalCount) = await _getBusinessInvitationsPage.HandleAsync(businessId, 1, 1, null, BusinessInvitationQueueFilter.All, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (_, openCount) = await _getBusinessInvitationsPage.HandleAsync(businessId, 1, 1, null, BusinessInvitationQueueFilter.Open, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (_, pendingCount) = await _getBusinessInvitationsPage.HandleAsync(businessId, 1, 1, null, BusinessInvitationQueueFilter.Pending, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (_, expiredCount) = await _getBusinessInvitationsPage.HandleAsync(businessId, 1, 1, null, BusinessInvitationQueueFilter.Expired, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("TotalCount = totalCount,");
        controllerSource.Should().Contain("OpenCount = openCount,");
        controllerSource.Should().Contain("PendingCount = pendingCount,");
        controllerSource.Should().Contain("ExpiredCount = expiredCount");

        controllerSource.Should().Contain("private List<BusinessInvitationPlaybookVm> BuildBusinessInvitationPlaybooks(Guid businessId)");
        controllerSource.Should().Contain("QueueLabel = T(\"OpenInvitations\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Invitations\", \"Businesses\", new { businessId, filter = BusinessInvitationQueueFilter.Open }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { flowKey = \"BusinessInvitation\", status = \"Failed\" }) ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"Pending\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Invitations\", \"Businesses\", new { businessId, filter = BusinessInvitationQueueFilter.Pending }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"SupportQueue\", \"Businesses\") ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"Expired\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Invitations\", \"Businesses\", new { businessId, filter = BusinessInvitationQueueFilter.Expired }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"CreateInvitation\", \"Businesses\", new { businessId }) ?? string.Empty");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessLocationAndOwnerOverridePlaybookHelperContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private List<BusinessLocationPlaybookVm> BuildBusinessLocationPlaybooks(Guid businessId)");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessLocationsPrimaryLocationLabel\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Locations\", \"Businesses\", new { businessId, filter = BusinessLocationQueueFilter.Primary }) ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"MissingAddress\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Locations\", \"Businesses\", new { businessId, filter = BusinessLocationQueueFilter.MissingAddress }) ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessLocationsMissingCoordinatesLabel\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Locations\", \"Businesses\", new { businessId, filter = BusinessLocationQueueFilter.MissingCoordinates }) ?? string.Empty");

        controllerSource.Should().Contain("private List<BusinessOwnerOverrideAuditPlaybookVm> BuildBusinessOwnerOverrideAuditPlaybooks(Guid businessId)");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessOwnerOverrideForceRemove\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Members\", \"Businesses\", new { businessId, filter = BusinessMemberSupportFilter.Attention }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"SupportQueue\", \"Businesses\") ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessOwnerOverrideDemoteDeactivate\")");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"MerchantReadiness\", \"Businesses\") ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"MissingActiveOwner\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Members\", \"Businesses\", new { businessId }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"Setup\", \"Businesses\", new { id = businessId }) ?? string.Empty");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessFormOptionsPopulationContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private async Task PopulateBusinessFormOptionsAsync(BusinessEditVm vm, CancellationToken ct)");
        controllerSource.Should().Contain("var settings = await _siteSettingCache.GetAsync(ct);");
        controllerSource.Should().Contain("vm.DefaultCurrency = string.IsNullOrWhiteSpace(vm.DefaultCurrency) ? settings.DefaultCurrency : vm.DefaultCurrency;");
        controllerSource.Should().Contain("vm.DefaultCulture = string.IsNullOrWhiteSpace(vm.DefaultCulture) ? settings.DefaultCulture : vm.DefaultCulture;");
        controllerSource.Should().Contain("vm.DefaultTimeZoneId = string.IsNullOrWhiteSpace(vm.DefaultTimeZoneId) ? (settings.TimeZone ?? string.Empty) : vm.DefaultTimeZoneId;");
        controllerSource.Should().Contain("vm.CategoryOptions = Enum.GetValues<BusinessCategoryKind>()");
        controllerSource.Should().Contain(".Select(x => new SelectListItem(x.ToString(), x.ToString(), vm.Category == x))");
        controllerSource.Should().Contain("vm.OwnerUserOptions = await _referenceData.GetUserOptionsAsync(vm.OwnerUserId, includeEmpty: true, ct);");
        controllerSource.Should().Contain("vm.CommunicationReadiness = await BuildBusinessCommunicationReadinessAsync(ct);");
        controllerSource.Should().Contain("vm.Subscription = await BuildBusinessSubscriptionSnapshotAsync(vm.Id, ct);");
    }

    [Fact]
    public void BusinessesController_Should_KeepMemberFormOptionsPopulationContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private async Task PopulateMemberFormOptionsAsync(BusinessMemberEditVm vm, bool includeUserSelection, CancellationToken ct)");
        controllerSource.Should().Contain("vm.RoleOptions = Enum.GetValues<BusinessMemberRole>()");
        controllerSource.Should().Contain(".Select(x => new SelectListItem(x.ToString(), x.ToString(), vm.Role == x))");
        controllerSource.Should().Contain("if (includeUserSelection)");
        controllerSource.Should().Contain("vm.UserOptions = await _referenceData.GetUserOptionsAsync(vm.UserId == Guid.Empty ? null : vm.UserId, includeEmpty: false, ct);");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessContextPopulationHelpersWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private async Task PopulateBusinessContextAsync(BusinessLocationEditVm vm, CancellationToken ct)");
        controllerSource.Should().Contain("private async Task PopulateBusinessContextAsync(BusinessMemberEditVm vm, CancellationToken ct)");
        controllerSource.Should().Contain("private async Task PopulateBusinessContextAsync(BusinessInvitationCreateVm vm, CancellationToken ct)");
        controllerSource.Should().Contain("vm.Business = await LoadBusinessContextAsync(vm.BusinessId, ct) ?? new BusinessContextVm { Id = vm.BusinessId };");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessCommunicationReadinessHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private async Task<BusinessCommunicationReadinessVm> BuildBusinessCommunicationReadinessAsync(CancellationToken ct)");
        controllerSource.Should().Contain("var settings = await _siteSettingCache.GetAsync(ct);");
        controllerSource.Should().Contain("var emailConfigured = settings.SmtpEnabled &&");
        controllerSource.Should().Contain("!string.IsNullOrWhiteSpace(settings.SmtpHost) &&");
        controllerSource.Should().Contain("!string.IsNullOrWhiteSpace(settings.SmtpFromAddress);");
        controllerSource.Should().Contain("var smsConfigured = settings.SmsEnabled &&");
        controllerSource.Should().Contain("!string.IsNullOrWhiteSpace(settings.SmsProvider);");
        controllerSource.Should().Contain("var whatsAppConfigured = settings.WhatsAppEnabled &&");
        controllerSource.Should().Contain("!string.IsNullOrWhiteSpace(settings.WhatsAppBusinessPhoneId) &&");
        controllerSource.Should().Contain("!string.IsNullOrWhiteSpace(settings.WhatsAppAccessToken) &&");
        controllerSource.Should().Contain("!string.IsNullOrWhiteSpace(settings.WhatsAppFromPhoneE164);");
        controllerSource.Should().Contain("var adminEmailRoutingConfigured = !string.IsNullOrWhiteSpace(settings.AdminAlertEmailsCsv);");
        controllerSource.Should().Contain("var adminSmsRoutingConfigured = !string.IsNullOrWhiteSpace(settings.AdminAlertSmsRecipientsCsv);");
        controllerSource.Should().Contain("EmailTransportEnabled = settings.SmtpEnabled,");
        controllerSource.Should().Contain("SmsTransportEnabled = settings.SmsEnabled,");
        controllerSource.Should().Contain("WhatsAppTransportEnabled = settings.WhatsAppEnabled,");
        controllerSource.Should().Contain("AdminAlertEmailsConfigured = adminEmailRoutingConfigured,");
        controllerSource.Should().Contain("AdminAlertSmsConfigured = adminSmsRoutingConfigured,");
        controllerSource.Should().Contain("EmailTransportSummary = emailConfigured");
        controllerSource.Should().Contain("SmsTransportSummary = smsConfigured");
        controllerSource.Should().Contain("WhatsAppTransportSummary = whatsAppConfigured");
        controllerSource.Should().Contain("AdminRoutingSummary = adminEmailRoutingConfigured || adminSmsRoutingConfigured");
    }

    [Fact]
    public void BusinessesController_Should_KeepInvitationFormOptionsPopulationContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private static void PopulateInvitationFormOptions(BusinessInvitationCreateVm vm)");
        controllerSource.Should().Contain("vm.RoleOptions = Enum.GetValues<BusinessMemberRole>()");
        controllerSource.Should().Contain(".Select(x => new SelectListItem(x.ToString(), x.ToString(), vm.Role == x))");
        controllerSource.Should().Contain(".ToList();");
    }

    [Fact]
    public void BusinessesController_Should_KeepCurrentActorDisplayNameHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private string? GetCurrentActorDisplayName()");
        controllerSource.Should().Contain("var explicitName = User.FindFirstValue(ClaimTypes.Name)");
        controllerSource.Should().Contain("?? User.FindFirstValue(ClaimTypes.Email)");
        controllerSource.Should().Contain("?? User.Identity?.Name;");
        controllerSource.Should().Contain("return string.IsNullOrWhiteSpace(explicitName) ? null : explicitName.Trim();");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessContextLoaderHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private async Task<BusinessContextVm?> LoadBusinessContextAsync(Guid id, CancellationToken ct)");
        controllerSource.Should().Contain("var dto = await _getBusinessForEdit.HandleAsync(id, ct);");
        controllerSource.Should().Contain("if (dto is null)");
        controllerSource.Should().Contain("return null;");
        controllerSource.Should().Contain("return new BusinessContextVm");
        controllerSource.Should().Contain("Id = dto.Id,");
        controllerSource.Should().Contain("Name = dto.Name,");
        controllerSource.Should().Contain("LegalName = dto.LegalName,");
        controllerSource.Should().Contain("Category = dto.Category,");
        controllerSource.Should().Contain("IsActive = dto.IsActive,");
        controllerSource.Should().Contain("OperationalStatus = dto.OperationalStatus,");
        controllerSource.Should().Contain("ApprovedAtUtc = dto.ApprovedAtUtc,");
        controllerSource.Should().Contain("SuspendedAtUtc = dto.SuspendedAtUtc,");
        controllerSource.Should().Contain("SuspensionReason = dto.SuspensionReason,");
        controllerSource.Should().Contain("MemberCount = dto.MemberCount,");
        controllerSource.Should().Contain("ActiveOwnerCount = dto.ActiveOwnerCount,");
        controllerSource.Should().Contain("LocationCount = dto.LocationCount,");
        controllerSource.Should().Contain("PrimaryLocationCount = dto.PrimaryLocationCount,");
        controllerSource.Should().Contain("InvitationCount = dto.InvitationCount,");
        controllerSource.Should().Contain("HasContactEmailConfigured = dto.HasContactEmailConfigured,");
        controllerSource.Should().Contain("HasLegalNameConfigured = dto.HasLegalNameConfigured");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessEditMappingHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private static BusinessEditVm MapBusinessEditVm(BusinessEditDto dto)");
        controllerSource.Should().Contain("return new BusinessEditVm");
        controllerSource.Should().Contain("RowVersion = dto.RowVersion,");
        controllerSource.Should().Contain("Name = dto.Name,");
        controllerSource.Should().Contain("LegalName = dto.LegalName,");
        controllerSource.Should().Contain("TaxId = dto.TaxId,");
        controllerSource.Should().Contain("ShortDescription = dto.ShortDescription,");
        controllerSource.Should().Contain("WebsiteUrl = dto.WebsiteUrl,");
        controllerSource.Should().Contain("ContactEmail = dto.ContactEmail,");
        controllerSource.Should().Contain("ContactPhoneE164 = dto.ContactPhoneE164,");
        controllerSource.Should().Contain("DefaultCurrency = dto.DefaultCurrency,");
        controllerSource.Should().Contain("DefaultCulture = dto.DefaultCulture,");
        controllerSource.Should().Contain("DefaultTimeZoneId = dto.DefaultTimeZoneId,");
        controllerSource.Should().Contain("AdminTextOverridesJson = dto.AdminTextOverridesJson,");
        controllerSource.Should().Contain("BrandDisplayName = dto.BrandDisplayName,");
        controllerSource.Should().Contain("BrandLogoUrl = dto.BrandLogoUrl,");
        controllerSource.Should().Contain("BrandPrimaryColorHex = dto.BrandPrimaryColorHex,");
        controllerSource.Should().Contain("BrandSecondaryColorHex = dto.BrandSecondaryColorHex,");
        controllerSource.Should().Contain("SupportEmail = dto.SupportEmail,");
        controllerSource.Should().Contain("CommunicationSenderName = dto.CommunicationSenderName,");
        controllerSource.Should().Contain("CommunicationReplyToEmail = dto.CommunicationReplyToEmail,");
        controllerSource.Should().Contain("CustomerEmailNotificationsEnabled = dto.CustomerEmailNotificationsEnabled,");
        controllerSource.Should().Contain("CustomerMarketingEmailsEnabled = dto.CustomerMarketingEmailsEnabled,");
        controllerSource.Should().Contain("OperationalAlertEmailsEnabled = dto.OperationalAlertEmailsEnabled,");
        controllerSource.Should().Contain("OperationalStatus = dto.OperationalStatus,");
        controllerSource.Should().Contain("MemberCount = dto.MemberCount,");
        controllerSource.Should().Contain("InvitationCount = dto.InvitationCount,");
        controllerSource.Should().Contain("HasContactEmailConfigured = dto.HasContactEmailConfigured,");
        controllerSource.Should().Contain("HasLegalNameConfigured = dto.HasLegalNameConfigured");
    }

    [Fact]
    public void BusinessesController_Should_KeepRedirectAndHtmxHelpersWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RedirectOrHtmx(string actionName, object routeValues)");
        controllerSource.Should().Contain("if (IsHtmxRequest())");
        controllerSource.Should().Contain("Response.Headers[\"HX-Redirect\"] = Url.Action(actionName, routeValues) ?? string.Empty;");
        controllerSource.Should().Contain("return new EmptyResult();");
        controllerSource.Should().Contain("return RedirectToAction(actionName, routeValues);");
        controllerSource.Should().Contain("private bool IsHtmxRequest()");
        controllerSource.Should().Contain("return string.Equals(Request.Headers[\"HX-Request\"], \"true\", StringComparison.OrdinalIgnoreCase);");
    }

    [Fact]
    public void BusinessesController_Should_KeepLocationCoordinateBuilderHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private static GeoCoordinateDto? BuildCoordinate(BusinessLocationEditVm vm)");
        controllerSource.Should().Contain("if (!vm.Latitude.HasValue || !vm.Longitude.HasValue)");
        controllerSource.Should().Contain("return null;");
        controllerSource.Should().Contain("return new GeoCoordinateDto");
        controllerSource.Should().Contain("Latitude = vm.Latitude.Value,");
        controllerSource.Should().Contain("Longitude = vm.Longitude.Value,");
        controllerSource.Should().Contain("AltitudeMeters = vm.AltitudeMeters");
    }

    [Fact]
    public void BusinessesController_Should_KeepSubscriptionManagementWebsiteUrlHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private static string? BuildSubscriptionManagementWebsiteUrl(string? baseUrl, Guid businessId, string? planCode)");
        controllerSource.Should().Contain("if (string.IsNullOrWhiteSpace(baseUrl))");
        controllerSource.Should().Contain("return null;");
        controllerSource.Should().Contain("var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? \"&\" : \"?\";");
        controllerSource.Should().Contain("var url = $\"{baseUrl}{separator}businessId={WebUtility.UrlEncode(businessId.ToString())}\";");
        controllerSource.Should().Contain("if (!string.IsNullOrWhiteSpace(planCode))");
        controllerSource.Should().Contain("url = $\"{url}&planCode={WebUtility.UrlEncode(planCode)}\";");
        controllerSource.Should().Contain("return url;");
    }

    [Fact]
    public void BusinessesController_Should_KeepPageSizeAndBusinessStatusItemBuilderContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private static IEnumerable<SelectListItem> BuildPageSizeItems(int selectedPageSize)");
        controllerSource.Should().Contain("var sizes = new[] { 10, 20, 50, 100 };");
        controllerSource.Should().Contain("return sizes.Select(x => new SelectListItem(x.ToString(), x.ToString(), x == selectedPageSize)).ToList();");

        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildBusinessStatusItems(BusinessOperationalStatus? selectedStatus)");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"AllStatuses\"), string.Empty, !selectedStatus.HasValue);");
        controllerSource.Should().Contain("foreach (var status in Enum.GetValues<BusinessOperationalStatus>())");
        controllerSource.Should().Contain("yield return new SelectListItem(status.ToString(), status.ToString(), selectedStatus == status);");
    }

    [Fact]
    public void BusinessesController_Should_KeepWorkspaceFilterItemBuilderContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildBusinessMemberFilterItems(BusinessMemberSupportFilter selectedFilter)");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"BusinessMembersAllLabel\"), BusinessMemberSupportFilter.All.ToString(), selectedFilter == BusinessMemberSupportFilter.All);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"NeedsAttention\"), BusinessMemberSupportFilter.Attention.ToString(), selectedFilter == BusinessMemberSupportFilter.Attention);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"PendingActivation\"), BusinessMemberSupportFilter.PendingActivation.ToString(), selectedFilter == BusinessMemberSupportFilter.PendingActivation);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"BusinessMembersLockedLabel\"), BusinessMemberSupportFilter.Locked.ToString(), selectedFilter == BusinessMemberSupportFilter.Locked);");

        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildBusinessInvitationFilterItems(BusinessInvitationQueueFilter selectedFilter)");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.All), BusinessInvitationQueueFilter.All.ToString(), selectedFilter == BusinessInvitationQueueFilter.All);");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Open), BusinessInvitationQueueFilter.Open.ToString(), selectedFilter == BusinessInvitationQueueFilter.Open);");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Pending), BusinessInvitationQueueFilter.Pending.ToString(), selectedFilter == BusinessInvitationQueueFilter.Pending);");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Expired), BusinessInvitationQueueFilter.Expired.ToString(), selectedFilter == BusinessInvitationQueueFilter.Expired);");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Accepted), BusinessInvitationQueueFilter.Accepted.ToString(), selectedFilter == BusinessInvitationQueueFilter.Accepted);");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Revoked), BusinessInvitationQueueFilter.Revoked.ToString(), selectedFilter == BusinessInvitationQueueFilter.Revoked);");

        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildBusinessLocationFilterItems(BusinessLocationQueueFilter selectedFilter)");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"CommonAll\"), BusinessLocationQueueFilter.All.ToString(), selectedFilter == BusinessLocationQueueFilter.All);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"BusinessLocationsPrimaryLocationLabel\"), BusinessLocationQueueFilter.Primary.ToString(), selectedFilter == BusinessLocationQueueFilter.Primary);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"MissingAddress\"), BusinessLocationQueueFilter.MissingAddress.ToString(), selectedFilter == BusinessLocationQueueFilter.MissingAddress);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"BusinessLocationsMissingCoordinatesLabel\"), BusinessLocationQueueFilter.MissingCoordinates.ToString(), selectedFilter == BusinessLocationQueueFilter.MissingCoordinates);");
    }

    [Fact]
    public void BusinessesController_Should_KeepSupportAuditRecommendedActionHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private string BuildSupportAuditRecommendedAction(EmailDispatchAuditListItemDto item)");
        controllerSource.Should().Contain("if (string.Equals(item.FlowKey, \"BusinessInvitation\", StringComparison.OrdinalIgnoreCase))");
        controllerSource.Should().Contain("? T(\"BusinessSupportAuditInvitationBusinessAction\")");
        controllerSource.Should().Contain(": T(\"BusinessSupportAuditInvitationGenericAction\")");
        controllerSource.Should().Contain("if (string.Equals(item.FlowKey, \"AccountActivation\", StringComparison.OrdinalIgnoreCase))");
        controllerSource.Should().Contain("? T(\"BusinessSupportAuditActivationBusinessAction\")");
        controllerSource.Should().Contain(": T(\"BusinessSupportAuditActivationGenericAction\")");
        controllerSource.Should().Contain("if (string.Equals(item.FlowKey, \"PasswordReset\", StringComparison.OrdinalIgnoreCase))");
        controllerSource.Should().Contain("return T(\"BusinessSupportAuditPasswordResetAction\")");
        controllerSource.Should().Contain("return T(\"BusinessSupportAuditGenericAction\")");
    }

    [Fact]
    public void BusinessesController_Should_KeepMerchantReadinessPlaybookHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private List<MerchantReadinessPlaybookVm> BuildMerchantReadinessPlaybooks()");
        controllerSource.Should().Contain("Title = T(\"MerchantReadinessPlaybookApprovalTitle\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Index\", \"Businesses\", new { operationalStatus = BusinessOperationalStatus.PendingApproval }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"SupportQueue\", \"Businesses\") ?? string.Empty");
        controllerSource.Should().Contain("Title = T(\"MerchantReadinessPlaybookSetupTitle\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Index\", \"Businesses\", new { attentionOnly = true }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"MerchantReadiness\", \"Businesses\") ?? string.Empty");
        controllerSource.Should().Contain("Title = T(\"MerchantReadinessPlaybookBillingTitle\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Index\", \"Businesses\", new { readinessFilter = BusinessReadinessQueueFilter.ApprovedInactive }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"Payments\", \"Billing\") ?? string.Empty");
    }

    [Fact]
    public void BusinessesController_Should_KeepSubscriptionPlaybookHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private List<BusinessSubscriptionPlaybookVm> BuildSubscriptionPlaybooks(Guid businessId, BusinessSubscriptionSnapshotVm subscription, bool managementWebsiteConfigured)");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessManagementWebsite\")");
        controllerSource.Should().Contain("OperatorAction = managementWebsiteConfigured");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Edit\", \"SiteSettings\", new { fragment = \"site-settings-business-app\" }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"Payments\", \"Billing\", new { businessId }) ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessSubscriptionCancellationPolicy\")");
        controllerSource.Should().Contain("OperatorAction = subscription.HasSubscription");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"SubscriptionInvoices\", \"Businesses\", new { businessId, filter = BusinessSubscriptionInvoiceQueueFilter.Open }) ?? string.Empty,");
        controllerSource.Should().Contain("items.Add(new BusinessSubscriptionPlaybookVm");
        controllerSource.Should().Contain("if (!subscription.HasSubscription)");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessSubscriptionNoActivePlan\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"SubscriptionInvoices\", \"Businesses\", new { businessId, filter = BusinessSubscriptionInvoiceQueueFilter.All }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"SupportQueue\", \"Businesses\") ?? string.Empty");
        controllerSource.Should().Contain("return items;");
    }

    [Fact]
    public void BusinessesController_Should_KeepBusinessSubscriptionSnapshotHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private async Task<BusinessSubscriptionSnapshotVm> BuildBusinessSubscriptionSnapshotAsync(Guid businessId, CancellationToken ct)");
        controllerSource.Should().Contain("if (businessId == Guid.Empty)");
        controllerSource.Should().Contain("return new BusinessSubscriptionSnapshotVm();");
        controllerSource.Should().Contain("var result = await _getBusinessSubscriptionStatus.HandleAsync(businessId, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("if (!result.Succeeded || result.Value is null)");
        controllerSource.Should().Contain("HasSubscription = false,");
        controllerSource.Should().Contain("Status = T(\"Unavailable\")");
        controllerSource.Should().Contain("return new BusinessSubscriptionSnapshotVm");
        controllerSource.Should().Contain("HasSubscription = result.Value.HasSubscription,");
        controllerSource.Should().Contain("SubscriptionId = result.Value.SubscriptionId,");
        controllerSource.Should().Contain("RowVersion = result.Value.RowVersion,");
        controllerSource.Should().Contain("Status = result.Value.Status,");
        controllerSource.Should().Contain("Provider = result.Value.Provider,");
        controllerSource.Should().Contain("PlanCode = result.Value.PlanCode,");
        controllerSource.Should().Contain("PlanName = result.Value.PlanName,");
        controllerSource.Should().Contain("Currency = result.Value.Currency,");
        controllerSource.Should().Contain("UnitPriceMinor = result.Value.UnitPriceMinor,");
        controllerSource.Should().Contain("StartedAtUtc = result.Value.StartedAtUtc,");
        controllerSource.Should().Contain("CurrentPeriodEndUtc = result.Value.CurrentPeriodEndUtc,");
        controllerSource.Should().Contain("TrialEndsAtUtc = result.Value.TrialEndsAtUtc,");
        controllerSource.Should().Contain("CancelAtPeriodEnd = result.Value.CancelAtPeriodEnd");
    }

    [Fact]
    public void BusinessesController_Should_KeepSubscriptionInvoiceFilterItemBuilderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildBusinessSubscriptionInvoiceFilterItems(BusinessSubscriptionInvoiceQueueFilter selectedFilter)");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"BusinessSubscriptionAllInvoicesLabel\"), BusinessSubscriptionInvoiceQueueFilter.All.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.All);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"CommonOpen\"), BusinessSubscriptionInvoiceQueueFilter.Open.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Open);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"CommonPaid\"), BusinessSubscriptionInvoiceQueueFilter.Paid.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Paid);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"CommonDraft\"), BusinessSubscriptionInvoiceQueueFilter.Draft.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Draft);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"CommonUncollectible\"), BusinessSubscriptionInvoiceQueueFilter.Uncollectible.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Uncollectible);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"BusinessSubscriptionHostedLinkMissing\"), BusinessSubscriptionInvoiceQueueFilter.HostedLinkMissing.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.HostedLinkMissing);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"CommonStripe\"), BusinessSubscriptionInvoiceQueueFilter.Stripe.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Stripe);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"CommonOverdue\"), BusinessSubscriptionInvoiceQueueFilter.Overdue.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Overdue);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"BusinessSubscriptionReviewPdfMissing\"), BusinessSubscriptionInvoiceQueueFilter.PdfMissing.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.PdfMissing);");
    }

    [Fact]
    public void BusinessCommunicationsController_Should_KeepPhoneVerificationPlaceholderHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("private static Dictionary<string, string?> BuildPhoneVerificationPlaceholders(");
        controllerSource.Should().Contain("string phoneE164,");
        controllerSource.Should().Contain("string token,");
        controllerSource.Should().Contain("DateTime expiresAtUtc)");
        controllerSource.Should().Contain("return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)");
        controllerSource.Should().Contain("[\"phone_e164\"] = phoneE164,");
        controllerSource.Should().Contain("[\"token\"] = token,");
        controllerSource.Should().Contain("[\"expires_at_utc\"] = expiresAtUtc.ToString(\"yyyy-MM-dd HH:mm:ss\")");
        controllerSource.Should().Contain("BuildPhoneVerificationPlaceholders(\"+4915112345678\", \"731904\", DateTime.UtcNow.AddMinutes(10))");
        controllerSource.Should().Contain("SupportedTokens = \"{phone_e164}, {token}, {expires_at_utc}\"");
    }

    private static string ReadWebApiFile(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Darwin.WebApi", relativePath));

        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }

    private static string ReadWebAdminFile(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Darwin.WebAdmin", relativePath));

        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }

    private static string ReadApplicationFile(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Darwin.Application", relativePath));

        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }

    private static string ReadDomainFile(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Darwin.Domain", relativePath));

        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }

    private static string ReadInfrastructureFile(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Darwin.Infrastructure", relativePath));

        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }
}

