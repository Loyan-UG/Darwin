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
}
