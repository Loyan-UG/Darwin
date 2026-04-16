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
    public void BusinessCommunicationsController_Should_KeepTestSendPostsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        source.Should().Contain("[ValidateAntiForgeryToken]");
        source.Should().Contain("public async Task<IActionResult> SendTestEmail(");
        source.Should().Contain("public async Task<IActionResult> SendTestSms(");
        source.Should().Contain("public async Task<IActionResult> SendTestWhatsApp(");
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
