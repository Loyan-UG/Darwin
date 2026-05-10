using FluentAssertions;

namespace Darwin.Tests.Unit.Security;

public sealed class SecurityAndPerformanceApiAndInfrastructureSourceTests : SecurityAndPerformanceSourceTestBase
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
    public void WebApiJwtAndStorefrontInfrastructure_Should_KeepLocalizedConfigurationFailureContractsWired()
    {
        var dependencyInjectionSource = ReadWebApiFile(Path.Combine("Extensions", "DependencyInjection.cs"));
        var jwtProviderSource = ReadWebApiFile(Path.Combine("Security", "JwtSigningParametersProvider.cs"));
        var storefrontUrlBuilderSource = ReadWebApiFile(Path.Combine("Services", "StorefrontCheckoutUrlBuilder.cs"));
        var validationResourceSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.resx"));
        var germanValidationResourceSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.de-DE.resx"));

        dependencyInjectionSource.Should().Contain("using Darwin.Application;");
        dependencyInjectionSource.Should().Contain("using System.Globalization;");
        dependencyInjectionSource.Should().Contain("using System.Resources;");
        dependencyInjectionSource.Should().Contain("throw new InvalidOperationException(GetValidationResourceText(\"JwtIssuerConfigurationMissing\"));");
        dependencyInjectionSource.Should().Contain("throw new InvalidOperationException(GetValidationResourceText(\"JwtAudienceConfigurationMissing\"));");
        dependencyInjectionSource.Should().Contain("throw new InvalidOperationException(GetValidationResourceText(\"JwtSigningKeyConfigurationMissing\"));");
        dependencyInjectionSource.Should().Contain("new ResourceManager(\"Darwin.Application.Resources.ValidationResource\", typeof(ValidationResource).Assembly);");

        jwtProviderSource.Should().Contain("private readonly IStringLocalizer<ValidationResource> _validationLocalizer;");
        jwtProviderSource.Should().Contain("IStringLocalizer<ValidationResource> validationLocalizer)");
        jwtProviderSource.Should().Contain("_validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));");
        jwtProviderSource.Should().Contain("throw new InvalidOperationException(_validationLocalizer[\"JwtValidationSiteSettingsMissing\"]);");
        jwtProviderSource.Should().Contain("throw new InvalidOperationException(_validationLocalizer[\"JwtValidationDisabled\"]);");
        jwtProviderSource.Should().Contain("throw new InvalidOperationException(_validationLocalizer[\"JwtSigningKeyMissingInSiteSettings\"]);");

        storefrontUrlBuilderSource.Should().Contain("private readonly IStringLocalizer<ValidationResource> _validationLocalizer;");
        storefrontUrlBuilderSource.Should().Contain("IStringLocalizer<ValidationResource> validationLocalizer)");
        storefrontUrlBuilderSource.Should().Contain("_validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));");
        storefrontUrlBuilderSource.Should().Contain("throw new InvalidOperationException(_validationLocalizer[\"StorefrontFrontOfficeBaseUrlNotConfigured\"]);");
        storefrontUrlBuilderSource.Should().Contain("throw new InvalidOperationException(_validationLocalizer[\"StorefrontPaymentProviderNotSupported\"]);");
        storefrontUrlBuilderSource.Should().Contain("throw new InvalidOperationException(_validationLocalizer[\"StorefrontStripeCheckoutBaseUrlNotConfigured\"]);");

        validationResourceSource.Should().Contain("<data name=\"JwtIssuerConfigurationMissing\"");
        validationResourceSource.Should().Contain("<data name=\"JwtAudienceConfigurationMissing\"");
        validationResourceSource.Should().Contain("<data name=\"JwtSigningKeyConfigurationMissing\"");
        validationResourceSource.Should().Contain("<data name=\"JwtValidationSiteSettingsMissing\"");
        validationResourceSource.Should().Contain("<data name=\"JwtValidationDisabled\"");
        validationResourceSource.Should().Contain("<data name=\"JwtSigningKeyMissingInSiteSettings\"");
        validationResourceSource.Should().Contain("<data name=\"StorefrontFrontOfficeBaseUrlNotConfigured\"");
        validationResourceSource.Should().Contain("<data name=\"StorefrontStripeCheckoutBaseUrlNotConfigured\"");
        validationResourceSource.Should().Contain("<data name=\"StorefrontPaymentProviderNotSupported\"");

        germanValidationResourceSource.Should().Contain("<data name=\"JwtIssuerConfigurationMissing\"");
        germanValidationResourceSource.Should().Contain("<data name=\"JwtAudienceConfigurationMissing\"");
        germanValidationResourceSource.Should().Contain("<data name=\"JwtSigningKeyConfigurationMissing\"");
        germanValidationResourceSource.Should().Contain("<data name=\"JwtValidationSiteSettingsMissing\"");
        germanValidationResourceSource.Should().Contain("<data name=\"JwtValidationDisabled\"");
        germanValidationResourceSource.Should().Contain("<data name=\"JwtSigningKeyMissingInSiteSettings\"");
        germanValidationResourceSource.Should().Contain("<data name=\"StorefrontFrontOfficeBaseUrlNotConfigured\"");
        germanValidationResourceSource.Should().Contain("<data name=\"StorefrontStripeCheckoutBaseUrlNotConfigured\"");
        germanValidationResourceSource.Should().Contain("<data name=\"StorefrontPaymentProviderNotSupported\"");
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
    public void ProfileControllers_Should_KeepLocalizedProfileAddressAndDeletionGuardsWired()
    {
        var profileSource = ReadWebApiFile(Path.Combine("Controllers", "Profile", "ProfileController.cs"));
        var addressesSource = ReadWebApiFile(Path.Combine("Controllers", "Profile", "ProfileAddressesController.cs"));
        var validationResourceSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.resx"));
        var validationResourceGermanSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.de-DE.resx"));

        profileSource.Should().Contain("private readonly IStringLocalizer<ValidationResource> _validationLocalizer;");
        profileSource.Should().Contain("validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer))");
        profileSource.Should().Contain("return NotFoundProblem(_validationLocalizer[\"ProfileNotFound\"])");
        profileSource.Should().Contain("return NotFoundProblem(_validationLocalizer[\"PreferencesNotFound\"])");
        profileSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"RequestPayloadRequired\"])");
        profileSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"IdentifierMustNotBeEmpty\"])");
        profileSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"RowVersionRequiredForOptimisticConcurrency\"])");
        profileSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"LocaleRequired\"])");
        profileSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"TimezoneRequired\"])");
        profileSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"CurrencyRequired\"])");
        profileSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"VerificationCodeRequired\"])");
        profileSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"ExplicitDeletionConfirmationRequired\"])");
        profileSource.Should().NotContain("Profile not found.");
        profileSource.Should().NotContain("Preferences not found.");
        profileSource.Should().NotContain("Verification code is required.");

        addressesSource.Should().Contain("private readonly IStringLocalizer<ValidationResource> _validationLocalizer;");
        addressesSource.Should().Contain("validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer))");
        addressesSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"RequestPayloadRequired\"])");
        addressesSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"AddressIdRequired\"])");
        addressesSource.Should().Contain("return dto is null ? NotFoundProblem(_validationLocalizer[\"LinkedCustomerNotFound\"]) : Ok(MapCustomer(dto));");
        addressesSource.Should().Contain("return dto is null ? NotFoundProblem(_validationLocalizer[\"LinkedCustomerContextNotFound\"]) : Ok(MapCustomerContext(dto));");
        addressesSource.Should().Contain("return address is null ? NotFoundProblem(_validationLocalizer[\"AddressNotFound\"]) : Ok(MapAddress(address));");
        addressesSource.Should().NotContain("Linked CRM customer not found.");
        addressesSource.Should().NotContain("Linked CRM customer context not found.");
        addressesSource.Should().NotContain("Address not found.");

        validationResourceSource.Should().Contain("<data name=\"ProfileNotFound\"");
        validationResourceSource.Should().Contain("<data name=\"PreferencesNotFound\"");
        validationResourceSource.Should().Contain("<data name=\"RowVersionRequiredForOptimisticConcurrency\"");
        validationResourceSource.Should().Contain("<data name=\"LocaleRequired\"");
        validationResourceSource.Should().Contain("<data name=\"TimezoneRequired\"");
        validationResourceSource.Should().Contain("<data name=\"CurrencyRequired\"");
        validationResourceSource.Should().Contain("<data name=\"VerificationCodeRequired\"");
        validationResourceSource.Should().Contain("<data name=\"LinkedCustomerContextNotFound\"");

        validationResourceGermanSource.Should().Contain("<data name=\"ProfileNotFound\"");
        validationResourceGermanSource.Should().Contain("<data name=\"PreferencesNotFound\"");
        validationResourceGermanSource.Should().Contain("<data name=\"RowVersionRequiredForOptimisticConcurrency\"");
        validationResourceGermanSource.Should().Contain("<data name=\"LocaleRequired\"");
        validationResourceGermanSource.Should().Contain("<data name=\"TimezoneRequired\"");
        validationResourceGermanSource.Should().Contain("<data name=\"CurrencyRequired\"");
        validationResourceGermanSource.Should().Contain("<data name=\"VerificationCodeRequired\"");
        validationResourceGermanSource.Should().Contain("<data name=\"LinkedCustomerContextNotFound\"");
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
        source.Should().Contain("IStringLocalizer<ValidationResource> validationLocalizer");
        source.Should().Contain("BusinessAccessStateMessageLocalizer.LocalizeBlockingReason(dto, _validationLocalizer)");
    }


    [Fact]
    public void BusinessAccessStateAndApiBusinessGates_Should_KeepLocalizedForbiddenContractsWired()
    {
        var accessStateSource = ReadApplicationFile(Path.Combine("Businesses", "DTOs", "BusinessAccessDtos.cs"));
        var localizerSource = ReadWebApiFile(Path.Combine("Controllers", "Businesses", "BusinessAccessStateMessageLocalizer.cs"));
        var apiControllerBaseSource = ReadWebApiFile(Path.Combine("Controllers", "ApiControllerBase.cs"));
        var billingSource = ReadWebApiFile(Path.Combine("Controllers", "Billing", "BillingController.cs"));
        var loyaltySource = ReadWebApiFile(Path.Combine("Controllers", "Business", "BusinessLoyaltyController.cs"));
        var validationResourceSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.resx"));
        var validationResourceGermanSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.de-DE.resx"));

        accessStateSource.Should().Contain("_ when !HasActiveMembership => \"membership_inactive\"");
        accessStateSource.Should().Contain("_ when !IsUserActive => \"user_inactive\"");
        accessStateSource.Should().Contain("_ when !IsUserEmailConfirmed => \"email_confirmation_required\"");
        accessStateSource.Should().Contain("_ when IsUserLockedOut => \"user_locked\"");
        accessStateSource.Should().Contain("BusinessOperationalStatus.PendingApproval => \"business_pending_approval\"");
        accessStateSource.Should().Contain("BusinessOperationalStatus.Suspended => \"business_suspended\"");
        accessStateSource.Should().Contain("_ when !IsActive => \"business_inactive\"");
        accessStateSource.Should().Contain("_ when !IsSetupComplete => \"setup_incomplete\"");
        accessStateSource.Should().NotContain("Business approval is still pending.");

        localizerSource.Should().Contain("\"membership_inactive\" => localizer[\"BusinessMembershipInactiveAccess\"]");
        localizerSource.Should().Contain("\"user_inactive\" => localizer[\"BusinessUserInactiveAccess\"]");
        localizerSource.Should().Contain("\"email_confirmation_required\" => localizer[\"BusinessEmailConfirmationRequiredAccess\"]");
        localizerSource.Should().Contain("\"user_locked\" => localizer[\"BusinessUserLockedAccess\"]");
        localizerSource.Should().Contain("\"business_pending_approval\" => localizer[\"BusinessPendingApprovalAccess\"]");
        localizerSource.Should().Contain("\"business_suspended\" when !string.IsNullOrWhiteSpace(state.SuspensionReason) => state.SuspensionReason");
        localizerSource.Should().Contain("\"business_suspended\" => localizer[\"BusinessSuspendedAccess\"]");
        localizerSource.Should().Contain("\"business_inactive\" => localizer[\"BusinessInactiveAccess\"]");
        localizerSource.Should().Contain("\"setup_incomplete\" => localizer[\"BusinessSetupIncompleteAccess\"]");

        apiControllerBaseSource.Should().Contain("protected IActionResult ForbiddenProblem(string title = \"Forbidden\", string? detail = null)");
        billingSource.Should().Contain("ForbiddenProblem(detail: BusinessAccessStateMessageLocalizer.LocalizeBlockingReason(accessState, _validationLocalizer))");
        loyaltySource.Should().Contain("ForbiddenProblem(detail: BusinessAccessStateMessageLocalizer.LocalizeBlockingReason(accessState, _validationLocalizer))");
        billingSource.Should().NotContain("Forbid(accessState.BlockingReason)");
        loyaltySource.Should().NotContain("Forbid(accessState.BlockingReason)");

        validationResourceSource.Should().Contain("<data name=\"BusinessMembershipInactiveAccess\"");
        validationResourceSource.Should().Contain("<data name=\"BusinessUserInactiveAccess\"");
        validationResourceSource.Should().Contain("<data name=\"BusinessEmailConfirmationRequiredAccess\"");
        validationResourceSource.Should().Contain("<data name=\"BusinessUserLockedAccess\"");
        validationResourceSource.Should().Contain("<data name=\"BusinessPendingApprovalAccess\"");
        validationResourceSource.Should().Contain("<data name=\"BusinessSuspendedAccess\"");
        validationResourceSource.Should().Contain("<data name=\"BusinessInactiveAccess\"");
        validationResourceSource.Should().Contain("<data name=\"BusinessSetupIncompleteAccess\"");
        validationResourceGermanSource.Should().Contain("<data name=\"BusinessMembershipInactiveAccess\"");
        validationResourceGermanSource.Should().Contain("<data name=\"BusinessUserInactiveAccess\"");
        validationResourceGermanSource.Should().Contain("<data name=\"BusinessEmailConfirmationRequiredAccess\"");
        validationResourceGermanSource.Should().Contain("<data name=\"BusinessUserLockedAccess\"");
        validationResourceGermanSource.Should().Contain("<data name=\"BusinessPendingApprovalAccess\"");
        validationResourceGermanSource.Should().Contain("<data name=\"BusinessSuspendedAccess\"");
        validationResourceGermanSource.Should().Contain("<data name=\"BusinessInactiveAccess\"");
        validationResourceGermanSource.Should().Contain("<data name=\"BusinessSetupIncompleteAccess\"");
    }


    [Fact]
    public void BusinessFacingBillingAndLoyaltyControllers_Should_KeepLocalizedRequestGuardsAndCanonicalScanFailureMappingWired()
    {
        var accountSource = ReadWebApiFile(Path.Combine("Controllers", "Business", "BusinessAccountController.cs"));
        var billingSource = ReadWebApiFile(Path.Combine("Controllers", "Billing", "BillingController.cs"));
        var loyaltySource = ReadWebApiFile(Path.Combine("Controllers", "Business", "BusinessLoyaltyController.cs"));
        var validationResourceSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.resx"));
        var validationResourceGermanSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.de-DE.resx"));

        accountSource.Should().Contain("BadRequestProblem(_validationLocalizer[\"BusinessRequired\"])");
        accountSource.Should().Contain("BadRequestProblem(_validationLocalizer[\"UserRequired\"])");
        accountSource.Should().Contain("NotFoundProblem(_validationLocalizer[\"BusinessNotFound\"])");

        billingSource.Should().Contain("ProblemFromResult(result, _validationLocalizer[\"BusinessSubscriptionStatusRetrievalFailed\"])");
        billingSource.Should().Contain("BadRequestProblem(_validationLocalizer[\"RequestPayloadRequired\"])");
        billingSource.Should().Contain("ProblemFromResult(result, _validationLocalizer[\"SubscriptionCancellationUpdateFailed\"])");
        billingSource.Should().Contain("ProblemFromResult(validation, _validationLocalizer[\"CheckoutIntentCreationFailed\"])");
        billingSource.Should().Contain("BadRequestProblem(_validationLocalizer[\"BillingCheckoutEndpointNotConfigured\"])");
        billingSource.Should().Contain("NotFoundProblem(_validationLocalizer[\"BusinessNotFound\"])");
        billingSource.Should().NotContain("Failed to retrieve business subscription status.");
        billingSource.Should().NotContain("Failed to update subscription cancellation preference.");
        billingSource.Should().NotContain("Unable to create checkout intent.");
        billingSource.Should().NotContain("Billing checkout endpoint is not configured.");

        loyaltySource.Should().Contain("BadRequestProblem(_validationLocalizer[\"RequestPayloadRequired\"])");
        loyaltySource.Should().Contain("BadRequestProblem(_validationLocalizer[\"RewardTypeInvalidAllowedValues\"])");
        loyaltySource.Should().Contain("BadRequestProblem(_validationLocalizer[\"RewardTierIdCannotBeEmpty\"])");
        loyaltySource.Should().Contain("BadRequestProblem(_validationLocalizer[\"LoyaltyProgramNotFound\"])");
        loyaltySource.Should().Contain("BadRequestProblem(_validationLocalizer[\"LoyaltyRewardTierCreateFailed\"])");
        loyaltySource.Should().Contain("BadRequestProblem(_validationLocalizer[\"LoyaltyRewardTierUpdateFailed\"])");
        loyaltySource.Should().Contain("BadRequestProblem(_validationLocalizer[\"ScanSessionTokenRequired\"])");
        loyaltySource.Should().Contain("BadRequestProblem(_validationLocalizer[\"ScanSessionTokenTooLong\"])");
        loyaltySource.Should().Contain("BadRequestProblem(_validationLocalizer[\"PointsPositiveInteger\"])");
        loyaltySource.Should().Contain("BadRequestProblem(_validationLocalizer[\"RequestBodyRouteIdMismatch\"])");
        loyaltySource.Should().Contain("NotFoundProblem(_validationLocalizer[\"BusinessNotFound\"])");
        loyaltySource.Should().Contain("? _validationLocalizer[\"OperationFailed\"].Value.Trim()");
        loyaltySource.Should().Contain("return ProblemFromResult(result, _validationLocalizer[\"OperationFailed\"]);");
        loyaltySource.Should().Contain("\"expired\" or \"scansessiontokenexpired\" => _validationLocalizer[\"ScanSessionTokenExpired\"]");
        loyaltySource.Should().Contain("\"tokenalreadyconsumed\" or \"scansessiontokenalreadyconsumed\" => _validationLocalizer[\"ScanSessionTokenAlreadyConsumed\"]");
        loyaltySource.Should().Contain("\"accountnotfound\" or \"scansessiontokennotfound\" or \"loyaltyaccountnotfoundforscansession\" => _validationLocalizer[\"LoyaltyAccountNotFoundForScanSession\"]");
        loyaltySource.Should().Contain("\"accountnotactive\" => _validationLocalizer[\"LoyaltyAccountInactive\"]");
        loyaltySource.Should().Contain("\"noselections\" => _validationLocalizer[\"SelectedRewardsMissing\"]");
        loyaltySource.Should().Contain("\"invalidselections\" => _validationLocalizer[\"SelectedRewardsInvalid\"]");
        loyaltySource.Should().Contain("\"insufficientpoints\" => _validationLocalizer[\"InsufficientPointsForSelectedRewards\"]");
        loyaltySource.Should().Contain("private static string NormalizeToken(string value)");
        loyaltySource.Should().NotContain("Request body is required.");
        loyaltySource.Should().NotContain("RewardType is invalid. Allowed values: FreeItem, PercentDiscount, AmountDiscount.");
        loyaltySource.Should().NotContain("RewardTierId is required.");
        loyaltySource.Should().NotContain("No loyalty program was found for the current business.");
        loyaltySource.Should().NotContain("ScanSessionToken is required.");
        loyaltySource.Should().NotContain("ScanSessionToken is too long.");
        loyaltySource.Should().NotContain("Points must be greater than zero.");
        loyaltySource.Should().NotContain("Request body is required and route id must match body id.");

        validationResourceSource.Should().Contain("<data name=\"ScanSessionTokenTooLong\"");
        validationResourceSource.Should().Contain("<data name=\"SelectedRewardsInvalid\"");
        validationResourceSource.Should().Contain("<data name=\"OperationFailed\"");
        validationResourceSource.Should().Contain("<data name=\"RewardTypeInvalidAllowedValues\"");
        validationResourceSource.Should().Contain("<data name=\"LoyaltyRewardTierCreateFailed\"");
        validationResourceSource.Should().Contain("<data name=\"LoyaltyRewardTierUpdateFailed\"");
        validationResourceSource.Should().Contain("<data name=\"RequestBodyRouteIdMismatch\"");
        validationResourceSource.Should().Contain("<data name=\"BusinessSubscriptionStatusRetrievalFailed\"");
        validationResourceSource.Should().Contain("<data name=\"SubscriptionCancellationUpdateFailed\"");
        validationResourceSource.Should().Contain("<data name=\"CheckoutIntentCreationFailed\"");
        validationResourceSource.Should().Contain("<data name=\"BillingCheckoutEndpointNotConfigured\"");

        validationResourceGermanSource.Should().Contain("<data name=\"ScanSessionTokenTooLong\"");
        validationResourceGermanSource.Should().Contain("<data name=\"SelectedRewardsInvalid\"");
        validationResourceGermanSource.Should().Contain("<data name=\"OperationFailed\"");
        validationResourceGermanSource.Should().Contain("<data name=\"RewardTypeInvalidAllowedValues\"");
        validationResourceGermanSource.Should().Contain("<data name=\"LoyaltyRewardTierCreateFailed\"");
        validationResourceGermanSource.Should().Contain("<data name=\"LoyaltyRewardTierUpdateFailed\"");
        validationResourceGermanSource.Should().Contain("<data name=\"RequestBodyRouteIdMismatch\"");
        validationResourceGermanSource.Should().Contain("<data name=\"BusinessSubscriptionStatusRetrievalFailed\"");
        validationResourceGermanSource.Should().Contain("<data name=\"SubscriptionCancellationUpdateFailed\"");
        validationResourceGermanSource.Should().Contain("<data name=\"CheckoutIntentCreationFailed\"");
        validationResourceGermanSource.Should().Contain("<data name=\"BillingCheckoutEndpointNotConfigured\"");
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
        accountSource.Should().Contain("public async Task<IActionResult> Register(string? returnUrl = null, CancellationToken ct = default)");
        accountSource.Should().Contain("[HttpPost(\"/account/register\")]");
        accountSource.Should().Contain("public async Task<IActionResult> RegisterPost(");
        accountSource.Should().Contain("[Authorize]");
        accountSource.Should().Contain("public async Task<IActionResult> Logout(CancellationToken ct = default)");
        accountSource.Should().Contain("[ValidateAntiForgeryToken]");
    }


    [Fact]
    public void AccountController_Should_KeepCookieRedirectAndRegistrationHelpersWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "AccountController.cs"));

        source.Should().Contain("ViewData[\"ReturnUrl\"] = returnUrl;");
        source.Should().Contain("return View();");
        source.Should().Contain("ModelState.AddModelError(string.Empty, _text.T(\"EmailPasswordRequiredMessage\"));");
        source.Should().Contain("TempData[\"2fa_user\"] = result.UserId.Value.ToString();");
        source.Should().Contain("TempData[\"remember\"] = rememberMe ? \"1\" : \"0\";");
        source.Should().Contain("ViewData[\"ReturnUrl\"] = SafeReturnUrlForForm(returnUrl);");
        source.Should().Contain("TempData[\"return\"] = SafeReturnUrlForForm(returnUrl);");
        source.Should().Contain("return RedirectToAction(nameof(LoginTwoFactor));");
        source.Should().Contain("PrepareTwoFactorViewState(");
        source.Should().Contain("PrepareTwoFactorViewState(userId, rememberMe, returnUrl);");
        source.Should().Contain("idObj.ToString(),");
        source.Should().Contain("AddLocalizedModelError(\"InvalidCredentialsMessage\");");
        source.Should().Contain("await IssueCookieAsync(result.UserId.Value, result.SecurityStamp!, rememberMe, ct);");
        source.Should().Contain("var dest = await DeterminePostLoginRedirectAsync(result.UserId.Value, returnUrl, ct);");
        source.Should().Contain("return Redirect(dest);");
        source.Should().Contain("var verify = await _verifyTotp.HandleAsync(new TotpVerifyDto { UserId = uid, Code = code }, ct);");
        source.Should().Contain("AddLocalizedModelError(\"InvalidCodeMessage\");");
        source.Should().Contain("var stampRes = await _getSecurityStamp.HandleAsync(uid, ct);");
        source.Should().Contain("return BadRequestLocalizedError(\"FailedToBeginPasskeyLoginMessage\");");
        source.Should().Contain("return BadRequestLocalizedError(\"PasskeyLoginFailedMessage\");");
        source.Should().Contain("AddLocalizedModelError(\"RegistrationFailedMessage\");");
        source.Should().Contain("private void AddLocalizedModelError(string fallbackKey)");
        source.Should().Contain("private BadRequestObjectResult BadRequestLocalizedError(string fallbackKey)");
        source.Should().Contain("private void PrepareTwoFactorViewState(string? userId, bool rememberMe, string? returnUrl)");
        source.Should().Contain("ViewData[\"RememberMe\"] = rememberMe;");
        source.Should().Contain("ViewData[\"ReturnUrl\"] = returnUrl ?? string.Empty;");
        source.Should().Contain("ViewData[\"TwoFaUserId\"] = userId ?? string.Empty;");
        source.Should().Contain("return Json(new { challengeTokenId = res.Value.ChallengeTokenId, options = res.Value.OptionsJson });");
        source.Should().NotContain("result.FailureReason!");
        source.Should().NotContain("verify.Error!");
        source.Should().NotContain("res.Error");
        source.Should().NotContain("result.Error!");
        source.Should().Contain("return Json(new { redirect = dest });");
        source.Should().Contain("var siteSettings = await _siteSettingCache.GetAsync(ct);");
        source.Should().Contain("ViewData[\"DefaultCurrency\"] = siteSettings.DefaultCurrency;");
        source.Should().Contain("ViewData[\"DefaultLocale\"] = siteSettings.DefaultCulture;");
        source.Should().Contain("ViewData[\"DefaultTimeZone\"] = siteSettings.TimeZone ?? string.Empty;");
        source.Should().Contain("ViewData[\"SupportedCulturesCsv\"] = siteSettings.SupportedCulturesCsv;");
        source.Should().Contain("var defaultCurrency = siteSettings.DefaultCurrency;");
        source.Should().Contain("var defaultLocale = string.IsNullOrWhiteSpace(siteSettings.DefaultCulture)");
        source.Should().Contain("AdminCultureCatalog.NormalizeUiCulture(siteSettings.DefaultCulture)");
        source.Should().Contain("var defaultTimeZone = siteSettings.TimeZone ?? string.Empty;");
        source.Should().Contain("var roleRes = await _getRoleIdByKey.HandleAsync(\"Members\", ct);");
        source.Should().Contain("if (roleRes.Succeeded) defaultRoleId = roleRes.Value;");
        source.Should().Contain("var result = await _register.HandleAsync(create, defaultRoleId, ct);");
        source.Should().Contain("return Redirect(SafeReturnUrl(returnUrl));");
        source.Should().Contain("private async Task IssueCookieAsync(Guid userId, string securityStamp, bool persistent, CancellationToken ct)");
        source.Should().Contain("new Claim(ClaimTypes.NameIdentifier, userId.ToString())");
        source.Should().Contain("new Claim(\"sstamp\", securityStamp)");
        source.Should().Contain("CookieAuthenticationDefaults.AuthenticationScheme");
        source.Should().Contain("ExpiresUtc = persistent ? new DateTimeOffset(_clock.UtcNow, TimeSpan.Zero).AddDays(30) : (DateTimeOffset?)null");
        source.Should().Contain("private async Task<string> DeterminePostLoginRedirectAsync(Guid userId, string? returnUrl, CancellationToken ct)");
        source.Should().Contain("if (IsSafeLocalReturnUrl(returnUrl))");
        source.Should().Contain("await _permissions.HasAsync(userId, \"AccessAdminPanel\", ct)");
        source.Should().Contain("var adminUrl = Url.Action(\"Index\", \"Home\")");
        source.Should().Contain("private string SafeReturnUrl(string? returnUrl)");
        source.Should().Contain("if (string.IsNullOrWhiteSpace(returnUrl)) return \"~/\";");
        source.Should().Contain("return IsSafeLocalReturnUrl(returnUrl) ? returnUrl : \"~/\";");
        source.Should().Contain("private string SafeReturnUrlForForm(string? returnUrl)");
        source.Should().Contain("return IsSafeLocalReturnUrl(returnUrl) ? returnUrl! : string.Empty;");
        source.Should().Contain("private bool IsSafeLocalReturnUrl(string? returnUrl)");
        source.Should().Contain("return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl);");
    }


    [Fact]
    public void WebAdminProgramAndDependencyInjection_Should_KeepCompositionAndAuthLocalizationRegistrationWired()
    {
        var programSource = ReadWebAdminFile("Program.cs");
        var dependencyInjectionSource = ReadWebAdminFile(Path.Combine("Extensions", "DependencyInjection.cs"));

        programSource.Should().Contain("builder.Services.AddWebComposition(builder.Configuration);");
        programSource.Should().Contain("var app = builder.Build();");
        programSource.Should().Contain("app.UseSerilogRequestLogging();");
        programSource.Should().Contain("await app.UseWebStartupAsync();");
        programSource.Should().Contain("app.Run();");

        dependencyInjectionSource.Should().Contain("public static IServiceCollection AddWebComposition(this IServiceCollection services, IConfiguration config)");
        dependencyInjectionSource.Should().Contain("services.AddHttpContextAccessor();");
        dependencyInjectionSource.Should().Contain("services.AddScoped<PermissionRazorHelper>();");
        dependencyInjectionSource.Should().Contain("services.AddLocalization(options => options.ResourcesPath = \"Resources\");");
        dependencyInjectionSource.Should().Contain("services.AddScoped<IAdminTextLocalizer, AdminTextLocalizer>();");
        dependencyInjectionSource.Should().Contain("services.AddSingleton<IDisplayMetadataProvider, SharedDisplayMetadataProvider>();");
        dependencyInjectionSource.Should().Contain("services.AddSingleton<IConfigureOptions<MvcOptions>, ConfigureDisplayMetadataLocalization>();");
        dependencyInjectionSource.Should().Contain(".AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)");
        dependencyInjectionSource.Should().Contain("options.LoginPath = \"/account/login\";");
        dependencyInjectionSource.Should().Contain("options.LogoutPath = \"/account/logout\";");
        dependencyInjectionSource.Should().Contain("options.AccessDeniedPath = \"/account/login\";");
        dependencyInjectionSource.Should().Contain("options.Cookie.Name = \"Darwin.Auth\";");
        dependencyInjectionSource.Should().Contain("options.Cookie.HttpOnly = true;");
        dependencyInjectionSource.Should().Contain("options.Cookie.SameSite = SameSiteMode.Lax;");
        dependencyInjectionSource.Should().Contain("options.Cookie.SecurePolicy = CookieSecurePolicy.Always;");
        dependencyInjectionSource.Should().Contain("options.ExpireTimeSpan = TimeSpan.FromDays(30);");
        dependencyInjectionSource.Should().Contain(".AddControllersWithViews(options =>");
        dependencyInjectionSource.Should().Contain("options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;");
        dependencyInjectionSource.Should().Contain(".AddViewLocalization()");
        dependencyInjectionSource.Should().Contain(".AddDataAnnotationsLocalization(options =>");
        dependencyInjectionSource.Should().Contain("factory.Create(typeof(SharedResource));");
        dependencyInjectionSource.Should().Contain("services.AddApplication(config);");
        dependencyInjectionSource.Should().Contain("services.AddSharedHostingDataProtection(config);");
        dependencyInjectionSource.Should().Contain("services.AddConfiguredPersistence(config);");
        dependencyInjectionSource.Should().Contain("services.AddAntiforgery(options =>");
        dependencyInjectionSource.Should().Contain("options.HeaderName = \"RequestVerificationToken\";");
        dependencyInjectionSource.Should().Contain("services.AddMemoryCache();");
        dependencyInjectionSource.Should().Contain("services.AddScoped<ISiteSettingCache, SiteSettingCache>();");
    }


    [Fact]
    public void WebAdminDependencyInjection_Should_KeepCookiePolicyStrict()
    {
        var dependencyInjectionSource = ReadWebAdminFile(Path.Combine("Extensions", "DependencyInjection.cs"));

        dependencyInjectionSource.Should().Contain("options.Cookie.SecurePolicy = CookieSecurePolicy.Always;");
        dependencyInjectionSource.Should().Contain("options.Cookie.HttpOnly = true;");
        dependencyInjectionSource.Should().Contain("options.Cookie.SameSite = SameSiteMode.Lax;");
        dependencyInjectionSource.Should().Contain("options.Cookie.Name = \"Darwin.Auth\";");
        dependencyInjectionSource.Should().Contain("options.Cookie.Name = \"Darwin.AntiForgery\";");
    }


    [Fact]
    public void InfrastructureSeeds_Should_KeepUsing_DomainDefaults_For_LocalizationAndCurrency()
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
        catalogSeedSource.Should().Contain("PriceDeltaMinor = Money.FromMajor(priceDeltaMajor, DomainDefaults.DefaultCurrency).AmountMinor");

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
        shippingMethodsSource.Should().Contain("public async Task<IActionResult> Create(CancellationToken ct = default)");
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

        siteSettingsSource.Should().Contain("public async Task<IActionResult> Edit(string? fragment, CancellationToken ct)");
        siteSettingsSource.Should().Contain("var dto = await _cache.GetAsync(ct);");
        siteSettingsSource.Should().Contain("return PartialView(\"~/Views/SiteSettings/_SiteSettingsEditorShell.cshtml\", vm);");
        siteSettingsSource.Should().Contain("ViewData[\"ActiveFragment\"] = string.IsNullOrWhiteSpace(fragment) ? null : fragment.Trim();");
        siteSettingsSource.Should().Contain("Response.Headers[\"HX-Redirect\"] = targetUrl;");

        mobileOpsSource.Should().Contain("public async Task<IActionResult> Index(");

        addOnGroupsSource.Should().Contain("public async Task<IActionResult> Index(");
        addOnGroupsSource.Should().Contain("public async Task<IActionResult> Create(CancellationToken ct = default)");
        addOnGroupsSource.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)");
        addOnGroupsSource.Should().Contain("public async Task<IActionResult> AttachToProducts(Guid id, int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)");
        addOnGroupsSource.Should().Contain("public async Task<IActionResult> AttachToCategories(Guid id, int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)");
        addOnGroupsSource.Should().Contain("public async Task<IActionResult> AttachToBrands(Guid id, int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)");
        addOnGroupsSource.Should().Contain("public async Task<IActionResult> AttachToVariants(");

        brandsSource.Should().Contain("public async Task<IActionResult> Index(int page = 1, int pageSize = 20,");
        brandsSource.Should().Contain("public async Task<IActionResult> Create(CancellationToken ct = default)");
        brandsSource.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)");
    }

    [Fact]
    public void MemberAndStorefrontPaymentSnapshots_Should_KeepCreatedAtUtcMappedForAttemptOrdering()
    {
        var memberOrdersSource = ReadWebApiFile(Path.Combine("Controllers", "Member", "MemberOrdersController.cs"));
        var publicCheckoutSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicCheckoutController.cs"));
        var memberOrderContractsSource = ReadContractsFile(Path.Combine("Orders", "MemberOrderContracts.cs"));
        var storefrontContractsSource = ReadContractsFile(Path.Combine("Orders", "StorefrontCheckoutContracts.cs"));

        memberOrdersSource.Should().Contain("Payments = dto.Payments.Select(payment => new MemberOrderPayment");
        memberOrdersSource.Should().Contain("CreatedAtUtc = payment.CreatedAtUtc,");
        memberOrdersSource.Should().Contain("ProviderPaymentIntentReference = payment.ProviderPaymentIntentReference,");
        memberOrdersSource.Should().Contain("ProviderCheckoutSessionReference = payment.ProviderCheckoutSessionReference,");
        memberOrdersSource.Should().Contain("PaidAtUtc = payment.PaidAtUtc");

        publicCheckoutSource.Should().Contain("Payments = confirmation.Payments.Select(payment => new StorefrontOrderConfirmationPayment");
        publicCheckoutSource.Should().Contain("CreatedAtUtc = payment.CreatedAtUtc,");
        publicCheckoutSource.Should().Contain("ProviderPaymentIntentReference = payment.ProviderPaymentIntentReference,");
        publicCheckoutSource.Should().Contain("ProviderCheckoutSessionReference = payment.ProviderCheckoutSessionReference,");
        publicCheckoutSource.Should().Contain("PaidAtUtc = payment.PaidAtUtc");

        memberOrderContractsSource.Should().Contain("public DateTime CreatedAtUtc { get; set; }");
        memberOrderContractsSource.Should().Contain("public string? ProviderPaymentIntentReference { get; set; }");
        memberOrderContractsSource.Should().Contain("public string? ProviderCheckoutSessionReference { get; set; }");
        memberOrderContractsSource.Should().Contain("public DateTime? PaidAtUtc { get; set; }");

        storefrontContractsSource.Should().Contain("public DateTime CreatedAtUtc { get; set; }");
        storefrontContractsSource.Should().Contain("public string? ProviderPaymentIntentReference { get; set; }");
        storefrontContractsSource.Should().Contain("public string? ProviderCheckoutSessionReference { get; set; }");
        storefrontContractsSource.Should().Contain("public DateTime? PaidAtUtc { get; set; }");
    }


    [Fact]
    public void BusinessSetupWorkspace_Should_KeepMemberAndInvitationHelperLabelsSourceBacked()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("string MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        setupShellSource.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        setupShellSource.Should().Contain("@MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        setupShellSource.Should().Contain("@MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        setupShellSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
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

        source.Should().Contain("asp-route-filter=\"@Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending\"");
        source.Should().Contain("@BusinessInvitationsUrl(Model.BusinessId, Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending, item.Email)");
        source.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        source.Should().NotContain("hx-push-url=\"true\">@T.T(\"Pending\")</a>");
    }


    [Fact]
    public void BusinessSetupInvitationsPreview_Should_KeepExpiredBadgeHelperBacked()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SetupInvitationsPreview.cshtml"));

        source.Should().Contain("asp-route-filter=\"@Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Expired\"");
        source.Should().Contain("@BusinessInvitationsUrl(Model.BusinessId, Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Expired, item.Email)");
        source.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Expired)");
        source.Should().NotContain("hx-push-url=\"true\">@T.T(\"Expired\")</a>");
    }


    [Fact]
    public void BusinessSetupMembersPreview_Should_KeepEditMemberActionWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SetupMembersPreview.cshtml"));

        source.Should().Contain("@EditMemberUrl(item.Id)");
        source.Should().Contain("@T.T(\"EditMemberAction\")");
    }


    [Fact]
    public void BusinessSetupMembersPreview_Should_KeepOpenMembersActionWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SetupMembersPreview.cshtml"));

        source.Should().Contain("@BusinessMembersUrl(Model.BusinessId, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)");
        source.Should().Contain("@T.T(\"BusinessSetupOpenMembersAction\")");
    }


    [Fact]
    public void BusinessSetupInvitationsPreview_Should_KeepOpenInvitationsActionWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SetupInvitationsPreview.cshtml"));

        source.Should().Contain("var invitationWorkspaceFilter = Model.PendingCount > 0");
        source.Should().Contain("var invitationWorkspaceCountLabel = Model.PendingCount > 0");
        source.Should().Contain("BusinessSetupInvitationsPreviewPendingCount");
        source.Should().Contain("? Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending");
        source.Should().Contain(": Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Open;");
        source.Should().Contain("@BusinessInvitationsUrl(Model.BusinessId, invitationWorkspaceFilter)");
        source.Should().Contain("@InvitationQueueLabel(invitationWorkspaceFilter)");
    }


    [Fact]
    public void BusinessSetupInvitationsPreview_Should_KeepFailedInvitationsActionWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SetupInvitationsPreview.cshtml"));

        source.Should().Contain("@EmailAuditsUrl(Model.BusinessId, status: \"Failed\", flowKey: \"BusinessInvitation\", recipientEmail: item.Email)");
        source.Should().Contain("@T.T(\"OpenFailedInvitationEmails\")");
    }


    [Fact]
    public void BusinessEditorWorkspace_Should_KeepInvitationSummaryTileHelperBacked()
    {
        var editorShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessEditorShell.cshtml"));

        editorShellSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        editorShellSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        editorShellSource.Should().Contain("asp-route-filter=\"@(Model.InvitationCount > 0 ? Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending : null)\"");
        editorShellSource.Should().Contain("hx-get=\"@BusinessInvitationsUrl(Model.Id, Model.InvitationCount > 0 ? Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending : null)\"");
        editorShellSource.Should().NotContain("@T.T(\"BusinessEditorPendingInvites\")");
        editorShellSource.Should().Contain("hx-push-url=\"true\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        editorShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Invitations\")</a>");
    }


    [Fact]
    public void BusinessEditorWorkspace_Should_KeepInvitationActionLanesWired()
    {
        var editorShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessEditorShell.cshtml"));

        editorShellSource.Should().Contain("@CreateInvitationUrl(Model.Id)");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorInviteUser\")");
        editorShellSource.Should().Contain("@if (Model.InvitationCount > 0)");
        editorShellSource.Should().Contain("asp-route-filter=\"@(Model.InvitationCount > 0 ? Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending : null)\"");
        editorShellSource.Should().Contain("hx-get=\"@BusinessInvitationsUrl(Model.Id, Model.InvitationCount > 0 ? Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending : null)\"");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorReviewInvitations\")");
    }


    [Fact]
    public void BusinessEditorWorkspace_Should_KeepOwnerLocationAndSetupActionLanesWired()
    {
        var editorShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessEditorShell.cshtml"));

        editorShellSource.Should().Contain("@CreateMemberUrl(Model.Id)");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorAssignOwner\")");
        editorShellSource.Should().Contain("@CreateLocationUrl(Model.Id)");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorAddPrimaryLocation\")");
        editorShellSource.Should().Contain("@BusinessSubscriptionUrl(Model.Id)");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorReviewSubscription\")");
        editorShellSource.Should().Contain("@BusinessSetupUrl(Model.Id)");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorOpenSetupWorkspace\")");
    }


    [Fact]
    public void BusinessEditorWorkspace_Should_KeepChecklistStatusRowsWired()
    {
        var editorShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessEditorShell.cshtml"));

        editorShellSource.Should().Contain("string ChecklistBooleanLabel(bool isComplete) => isComplete ? T.T(\"Yes\") : T.T(\"No\")");
        editorShellSource.Should().Contain("@ChecklistBooleanLabel(Model.ActiveOwnerCount > 0) - @T.T(\"BusinessEditorActiveOwnerAssigned\")");
        editorShellSource.Should().Contain("@ChecklistBooleanLabel(Model.PrimaryLocationCount > 0) - @T.T(\"BusinessEditorPrimaryLocationConfigured\")");
        editorShellSource.Should().Contain("@ChecklistBooleanLabel(Model.HasContactEmailConfigured) - @T.T(\"BusinessEditorContactEmailConfigured\")");
        editorShellSource.Should().Contain("@ChecklistBooleanLabel(Model.HasLegalNameConfigured) - @T.T(\"BusinessEditorLegalBusinessNameConfigured\")");
    }


    [Fact]
    public void BusinessEditorWorkspace_Should_KeepOperationalStatusSummaryWired()
    {
        var editorShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessEditorShell.cshtml"));

        editorShellSource.Should().Contain("@T.T(\"BusinessEditorOperationalStatus\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorPendingApproval\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorApproved\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorSuspended\")");
        editorShellSource.Should().Contain("@string.Format(T.T(\"BusinessEditorApprovedAt\"), Model.ApprovedAtUtc.Value.ToLocalTime().ToString(CultureInfo.CurrentCulture))");
        editorShellSource.Should().Contain("@string.Format(T.T(\"BusinessEditorSuspendedAt\"), Model.SuspendedAtUtc.Value.ToLocalTime().ToString(CultureInfo.CurrentCulture))");
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
    public void BusinessLocationEditorShell_Should_KeepTopWorkspacePivotsWired()
    {
        var locationShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessLocationEditorShell.cshtml"));

        locationShellSource.Should().Contain("hx-target=\"#business-location-editor-shell\"");
        locationShellSource.Should().Contain("@BusinessLocationsUrl(Model.BusinessId, Model.Page, Model.PageSize, Model.Query, Model.Filter)");
        locationShellSource.Should().Contain("@T.T(\"BusinessLocationBackToLocations\")");
        locationShellSource.Should().Contain("@BusinessSetupUrl(Model.BusinessId)");
        locationShellSource.Should().Contain("@T.T(\"Setup\")");
        locationShellSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.BusinessId)");
        locationShellSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
    }


    [Fact]
    public void BusinessInvitationEditorShell_Should_KeepTopWorkspacePivotsWired()
    {
        var invitationShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessInvitationEditorShell.cshtml"));

        invitationShellSource.Should().Contain("hx-target=\"#business-invitation-editor-shell\"");
        invitationShellSource.Should().Contain("@BusinessInvitationsUrl(Model.BusinessId, Model.Page, Model.PageSize, Model.Query, Model.Filter)");
        invitationShellSource.Should().Contain("@T.T(\"BusinessInvitationBackToInvitations\")");
        invitationShellSource.Should().Contain("@BusinessSupportQueueUrl(Model.BusinessId)");
        invitationShellSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
        invitationShellSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.BusinessId)");
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
    public void BusinessInvitationForm_Should_KeepOpenInvitationsShortcutHelperBacked()
    {
        var invitationFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessInvitationForm.cshtml"));

        invitationFormSource.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        invitationFormSource.Should().Contain("@InvitationQueueLabel(Model.Filter)");
        invitationFormSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"OpenInvitations\")</a>");
    }


    [Fact]
    public void BusinessMemberForm_Should_KeepPendingActivationShortcutHelperBacked()
    {
        var memberFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberForm.cshtml"));

        memberFormSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        memberFormSource.Should().Contain("asp-route-filter=\"@Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation\"");
        memberFormSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        memberFormSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
    }


    [Fact]
    public void BusinessInvitationForm_Should_KeepHelpAndCancelWorkspacePivotsWired()
    {
        var invitationFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessInvitationForm.cshtml"));

        invitationFormSource.Should().Contain("@T.T(\"BusinessInvitationCreateHelp\")");
        invitationFormSource.Should().Contain("@BusinessInvitationsUrl(Model.BusinessId, Model.Page, Model.PageSize, Model.Query, Model.Filter)");
        invitationFormSource.Should().Contain("@BusinessSetupUrl(Model.BusinessId)");
        invitationFormSource.Should().Contain("@BusinessSupportQueueUrl(Model.BusinessId)");
        invitationFormSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.BusinessId)");
        invitationFormSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Cancel\")</a>");
    }


    [Fact]
    public void BusinessMemberForm_Should_KeepCreateHelpAndCancelWorkspacePivotsWired()
    {
        var memberFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberForm.cshtml"));

        memberFormSource.Should().Contain("@T.T(\"BusinessMemberAssignmentHelp\")");
        memberFormSource.Should().Contain("asp-route-filter=\"@(Model.Business.ActiveOwnerCount > 0 ? null : Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)\"");
        memberFormSource.Should().Contain("@BusinessMembersUrl(Model.BusinessId, filter: Model.Business.ActiveOwnerCount > 0 ? null : Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)");
        memberFormSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.BusinessId)");
        memberFormSource.Should().Contain("@BusinessSupportQueueUrl(Model.BusinessId)");
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
    public void BusinessForm_Should_KeepInitialOwnerGuidanceRailWired()
    {
        var businessFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessForm.cshtml"));

        businessFormSource.Should().Contain("@T.T(\"BusinessFormInitialOwnerHelp\")");
        businessFormSource.Should().Contain("string GlobalUsersUrl() => Url.Action(\"Index\", \"Users\") ?? string.Empty;");
        businessFormSource.Should().Contain("hx-get=\"@GlobalUsersUrl()\"");
        businessFormSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Users\")</a>");
        businessFormSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessSupportQueueTitle\")</a>");
        businessFormSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }


    [Fact]
    public void BusinessForm_Should_KeepActiveStateGuidanceRailWired()
    {
        var businessFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessForm.cshtml"));

        businessFormSource.Should().Contain("@T.T(\"BusinessFormActiveHelp\")");
        businessFormSource.Should().Contain("@BusinessSupportQueueUrl(Model.Id)");
        businessFormSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.Id)");
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
        businessFormSource.Should().Contain("asp-route-filter=\"@(Model.ActiveOwnerCount > 0 ? null : Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)\"");
        businessFormSource.Should().Contain("hx-get=\"@BusinessMembersUrl(Model.Id, Model.ActiveOwnerCount > 0 ? null : Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)\"");
        businessFormSource.Should().Contain("@BusinessLocationsUrl(Model.Id)");
        businessFormSource.Should().Contain("asp-route-filter=\"@(Model.InvitationCount > 0 ? Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending : null)\"");
        businessFormSource.Should().Contain("hx-get=\"@BusinessInvitationsUrl(Model.Id, Model.InvitationCount > 0 ? Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending : null)\"");
        businessFormSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessSupportQueueTitle\")</a>");
        businessFormSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
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
        memberShellSource.Should().Contain("@UserEditUrl(Model.UserId)");
        memberShellSource.Should().Contain("@MobileOperationsUrl(Model.BusinessId, Model.UserEmail)");
        memberShellSource.Should().Contain("@LoyaltyAccountsUrl(Model.UserEmail)");
        memberShellSource.Should().Contain("@StaffAccessBadgeUrl(Model.Id)");
        memberShellSource.Should().Contain("@Url.Action(\"SendMemberActivationEmail\", \"Businesses\")");
        memberShellSource.Should().Contain("@Url.Action(\"ConfirmMemberEmail\", \"Businesses\")");
        memberShellSource.Should().Contain("@Url.Action(\"SendMemberPasswordReset\", \"Businesses\")");
        memberShellSource.Should().Contain("@Url.Action(\"UnlockMemberUser\", \"Businesses\")");
        memberShellSource.Should().Contain("@Url.Action(\"LockMemberUser\", \"Businesses\")");
    }


    [Fact]
    public void BusinessMemberEditorShell_Should_KeepTopWorkspacePivotWired()
    {
        var memberShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberEditorShell.cshtml"));

        memberShellSource.Should().Contain("hx-target=\"#business-member-editor-shell\"");
        memberShellSource.Should().Contain("@BusinessMembersUrl(Model.BusinessId, Model.Page, Model.PageSize, Model.Query, Model.Filter)");
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
    public void StripeWebhookInfrastructure_Should_KeepVerifiedPublicCallbackAndLifecycleHandlingWired()
    {
        var controllerSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "StripeWebhooksController.cs"));
        var verifierSource = ReadWebApiFile(Path.Combine("Services", "StripeWebhookSignatureVerifier.cs"));
        var dependencyInjectionSource = ReadWebApiFile(Path.Combine("Extensions", "DependencyInjection.cs"));
        var handlerSource = ReadApplicationFile(Path.Combine("Billing", "ProcessStripeWebhookHandler.cs"));
        var dbContextSource = ReadInfrastructureFile(Path.Combine("Persistence", "Db", "DarwinDbContext.cs"));

        controllerSource.Should().Contain("[AllowAnonymous]");
        controllerSource.Should().Contain("[Route(\"api/v1/public/billing/stripe/webhooks\")]");
        controllerSource.Should().Contain("[HttpPost(\"/api/v1/billing/stripe/webhooks\")]");
        controllerSource.Should().Contain("private readonly ProviderCallbackInboxWriter _inboxWriter;");
        controllerSource.Should().Contain("private readonly GetSiteSettingHandler _getSiteSettingHandler;");
        controllerSource.Should().Contain("private readonly StripeWebhookSignatureVerifier _signatureVerifier;");
        controllerSource.Should().Contain("var signatureHeader = Request.Headers[\"Stripe-Signature\"].ToString();");
        controllerSource.Should().Contain("siteSetting.StripeWebhookSecret");
        controllerSource.Should().Contain("_signatureVerifier.TryVerify(rawPayload, signatureHeader, siteSetting.StripeWebhookSecret, out var errorKey)");
        controllerSource.Should().Contain("var existing = await _inboxWriter.AddIfNewAsync(");
        controllerSource.Should().Contain("callbackType: eventType,");
        controllerSource.Should().Contain("idempotencyKey: eventId,");
        controllerSource.Should().Contain("rawPayload,");
        controllerSource.Should().Contain("duplicate = existing");

        verifierSource.Should().Contain("public sealed class StripeWebhookSignatureVerifier");
        verifierSource.Should().Contain("private static readonly TimeSpan DefaultTolerance = TimeSpan.FromMinutes(10);");
        verifierSource.Should().Contain("new HMACSHA256(Encoding.UTF8.GetBytes(secret))");
        verifierSource.Should().Contain("CryptographicOperations.FixedTimeEquals(computedHash, providedHash)");
        verifierSource.Should().Contain("keyValue[0], \"v1\"");

        dependencyInjectionSource.Should().Contain("services.TryAddSingleton<StripeWebhookSignatureVerifier>();");

        handlerSource.Should().Contain("public sealed class ProcessStripeWebhookHandler");
        handlerSource.Should().Contain("_db.Set<EventLog>()");
        handlerSource.Should().Contain("AnyAsync(x => !x.IsDeleted && x.IdempotencyKey == eventId, ct)");
        handlerSource.Should().Contain("case \"payment_intent.succeeded\":");
        handlerSource.Should().Contain("case \"charge.refunded\":");
        handlerSource.Should().Contain("case \"invoice.paid\":");
        handlerSource.Should().Contain("case \"customer.subscription.updated\":");

        dbContextSource.Should().Contain("public DbSet<ProviderCallbackInboxMessage> ProviderCallbackInboxMessages => Set<ProviderCallbackInboxMessage>();");
    }

    [Fact]
    public void DhlWebhookInfrastructure_Should_KeepVerifiedPublicCallbackAndShipmentLifecycleHandlingWired()
    {
        var controllerSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "DhlWebhooksController.cs"));
        var handlerSource = ReadApplicationFile(Path.Combine("Orders", "Commands", "ApplyShipmentCarrierEventHandler.cs"));
        var validatorSource = ReadApplicationFile(Path.Combine("Orders", "Validators", "OrderValidators.cs"));
        var contractsSource = ReadContractsFile(Path.Combine("Shipping", "DhlShippingContracts.cs"));
        var recorderSource = ReadApplicationFile(Path.Combine("Orders", "Commands", "ShipmentCarrierEventRecorder.cs"));
        var configSource = ReadInfrastructureFile(Path.Combine("Persistence", "Configurations", "Orders", "ShipmentCarrierEventConfiguration.cs"));
        var dbContextSource = ReadInfrastructureFile(Path.Combine("Persistence", "Db", "DarwinDbContext.cs"));
        var inboxConfigSource = ReadInfrastructureFile(Path.Combine("Persistence", "Configurations", "Integration", "ProviderCallbackInboxMessageConfiguration.cs"));
        var shipmentProviderOperationConfigSource = ReadInfrastructureFile(Path.Combine("Persistence", "Configurations", "Integration", "ShipmentProviderOperationConfiguration.cs"));

        controllerSource.Should().Contain("[AllowAnonymous]");
        controllerSource.Should().Contain("[Route(\"api/v1/public/shipping/dhl/webhooks\")]");
        controllerSource.Should().Contain("[HttpPost(\"/api/v1/shipping/dhl/webhooks\")]");
        controllerSource.Should().Contain("private readonly ProviderCallbackInboxWriter _inboxWriter;");
        controllerSource.Should().Contain("private readonly GetSiteSettingHandler _getSiteSettingHandler;");
        controllerSource.Should().Contain("var apiKeyHeader = Request.Headers[\"X-DHL-Key\"].ToString();");
        controllerSource.Should().Contain("var signatureHeader = Request.Headers[\"X-DHL-Signature\"].ToString();");
        controllerSource.Should().Contain("TryVerifySignature(rawPayload, signatureHeader, siteSetting.DhlApiSecret)");
        controllerSource.Should().Contain("var idempotencyKey = BuildIdempotencyKey(request);");
        controllerSource.Should().Contain("var existing = await _inboxWriter.AddIfNewAsync(");
        controllerSource.Should().Contain("callbackType: carrierEventKey,");
        controllerSource.Should().Contain("idempotencyKey,");
        controllerSource.Should().Contain("rawPayload,");
        controllerSource.Should().Contain("duplicate = existing");
        controllerSource.Should().Contain("private static string BuildIdempotencyKey(DhlShipmentCallbackRequest request)");
        controllerSource.Should().Contain("request.OccurredAtUtc.ToUniversalTime().ToString(\"O\")");

        handlerSource.Should().Contain("public sealed class ApplyShipmentCarrierEventHandler");
        handlerSource.Should().Contain("FirstOrDefaultAsync(");
        handlerSource.Should().Contain("var normalizedCarrier = dto.Carrier.Trim();");
        handlerSource.Should().Contain("var normalizedReference = dto.ProviderShipmentReference.Trim();");
        handlerSource.Should().Contain("x.ProviderShipmentReference == normalizedReference");
        handlerSource.Should().Contain("x.Carrier == normalizedCarrier");
        handlerSource.Should().Contain("var normalizedEventKey = dto.CarrierEventKey.Trim();");
        handlerSource.Should().Contain("shipment.LastCarrierEventKey = normalizedEventKey;");
        handlerSource.Should().Contain("ResolveStatus(dto.ProviderStatus, dto.CarrierEventKey)");
        handlerSource.Should().Contain("await ShipmentCarrierEventRecorder.AddIfMissingAsync(");
        handlerSource.Should().Contain("var normalizedExceptionCode = string.IsNullOrWhiteSpace(dto.ExceptionCode) ? null : dto.ExceptionCode.Trim();");
        handlerSource.Should().Contain("var normalizedExceptionMessage = string.IsNullOrWhiteSpace(dto.ExceptionMessage) ? null : dto.ExceptionMessage.Trim();");
        handlerSource.Should().Contain("return ShipmentStatus.Delivered;");
        handlerSource.Should().Contain("return ShipmentStatus.Returned;");
        handlerSource.Should().Contain("shipment.ShippedAtUtc ??= dto.OccurredAtUtc;");
        handlerSource.Should().Contain("shipment.DeliveredAtUtc ??= dto.OccurredAtUtc;");

        recorderSource.Should().Contain("internal static class ShipmentCarrierEventRecorder");
        recorderSource.Should().Contain("db.Set<ShipmentCarrierEvent>()");
        recorderSource.Should().Contain("x.ShipmentId == shipment.Id");
        recorderSource.Should().Contain("x.CarrierEventKey == normalizedEventKey");
        recorderSource.Should().Contain("string? exceptionCode = null,");
        recorderSource.Should().Contain("string? exceptionMessage = null,");
        recorderSource.Should().Contain("existing.ExceptionCode = normalizedExceptionCode;");
        recorderSource.Should().Contain("existing.ExceptionMessage = normalizedExceptionMessage;");
        recorderSource.Should().Contain("ExceptionCode = normalizedExceptionCode,");
        recorderSource.Should().Contain("ExceptionMessage = normalizedExceptionMessage,");

        configSource.Should().Contain("public sealed class ShipmentCarrierEventConfiguration : IEntityTypeConfiguration<ShipmentCarrierEvent>");
        configSource.Should().Contain("builder.HasIndex(x => new { x.ShipmentId, x.OccurredAtUtc });");
        configSource.Should().Contain("builder.HasOne<Shipment>()");
        configSource.Should().Contain("builder.Property(x => x.ExceptionCode).HasMaxLength(128);");
        configSource.Should().Contain("builder.Property(x => x.ExceptionMessage).HasMaxLength(512);");

        dbContextSource.Should().Contain("public DbSet<ShipmentCarrierEvent> ShipmentCarrierEvents => Set<ShipmentCarrierEvent>();");
        dbContextSource.Should().Contain("public DbSet<ProviderCallbackInboxMessage> ProviderCallbackInboxMessages => Set<ProviderCallbackInboxMessage>();");
        dbContextSource.Should().Contain("public DbSet<ShipmentProviderOperation> ShipmentProviderOperations => Set<ShipmentProviderOperation>();");

        inboxConfigSource.Should().Contain("public sealed class ProviderCallbackInboxMessageConfiguration : IEntityTypeConfiguration<ProviderCallbackInboxMessage>");
        inboxConfigSource.Should().Contain("builder.ToTable(\"ProviderCallbackInboxMessages\", schema: \"Integration\");");
        inboxConfigSource.Should().Contain("builder.HasIndex(x => new { x.Provider, x.Status, x.CreatedAtUtc });");
        inboxConfigSource.Should().Contain("builder.HasIndex(x => new { x.Provider, x.IdempotencyKey })");
        shipmentProviderOperationConfigSource.Should().Contain("public sealed class ShipmentProviderOperationConfiguration : IEntityTypeConfiguration<ShipmentProviderOperation>");
        shipmentProviderOperationConfigSource.Should().Contain("builder.ToTable(\"ShipmentProviderOperations\", schema: \"Integration\");");

        validatorSource.Should().Contain("public sealed class ApplyShipmentCarrierEventValidator : AbstractValidator<ApplyShipmentCarrierEventDto>");
        validatorSource.Should().Contain("RuleFor(x => x.ProviderShipmentReference).NotEmpty().MaximumLength(128);");
        validatorSource.Should().Contain("RuleFor(x => x.CarrierEventKey).NotEmpty().MaximumLength(128);");
        validatorSource.Should().Contain("WithMessage(localizer[\"CarrierEventOccurredAtUtcRequired\"])");
        validatorSource.Should().Contain("RuleFor(x => x.ExceptionCode).MaximumLength(128)");
        validatorSource.Should().Contain("RuleFor(x => x.ExceptionMessage).MaximumLength(512)");

        contractsSource.Should().Contain("public sealed class DhlShipmentCallbackRequest");
        contractsSource.Should().Contain("public string ProviderShipmentReference { get; set; } = string.Empty;");
        contractsSource.Should().Contain("public string CarrierEventKey { get; set; } = string.Empty;");
        contractsSource.Should().Contain("public DateTime OccurredAtUtc { get; set; }");
        contractsSource.Should().Contain("public string? ExceptionCode { get; set; }");
        contractsSource.Should().Contain("public string? ExceptionMessage { get; set; }");
    }

    [Fact]
    public void ProviderWebhookPayloadReader_Should_KeepMaxPayloadLimitStable()
    {
        var readerSource = ReadWebApiFile(Path.Combine("Services", "ProviderWebhookPayloadReader.cs"));

        readerSource.Should().Contain("public const int MaxPayloadBytes = 256 * 1024;");
        readerSource.Should().Contain("if (memory.Length + read > MaxPayloadBytes)");
    }

    [Fact]
    public void StripeWebhookController_Should_KeepPayloadSizeGuardBeforeSignatureVerification()
    {
        var controllerSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "StripeWebhooksController.cs"));

        controllerSource.Should().Contain("var payloadRead = await ProviderWebhookPayloadReader.ReadAsync(Request, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("if (payloadRead.PayloadTooLarge)");
        controllerSource.Should().Contain("return PayloadTooLargeProblem(_validationLocalizer[\"ProviderWebhookPayloadTooLarge\"]);");
        controllerSource.IndexOf("var payloadRead = await ProviderWebhookPayloadReader.ReadAsync(Request, ct).ConfigureAwait(false);", StringComparison.Ordinal)
            .Should().BeLessThan(controllerSource.IndexOf("var signatureHeader = Request.Headers[\"Stripe-Signature\"].ToString();", StringComparison.Ordinal));
    }

    [Fact]
    public void DhlWebhookController_Should_KeepPayloadSizeGuardBeforeSignatureAuthentication()
    {
        var controllerSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "DhlWebhooksController.cs"));

        controllerSource.Should().Contain("var payloadRead = await ProviderWebhookPayloadReader.ReadAsync(Request, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("if (payloadRead.PayloadTooLarge)");
        controllerSource.Should().Contain("return PayloadTooLargeProblem(_validationLocalizer[\"ProviderWebhookPayloadTooLarge\"]);");
        controllerSource.Should().Contain("var apiKeyHeader = Request.Headers[\"X-DHL-Key\"].ToString();");
        controllerSource.IndexOf("var payloadRead = await ProviderWebhookPayloadReader.ReadAsync(Request, ct).ConfigureAwait(false);", StringComparison.Ordinal)
            .Should().BeLessThan(controllerSource.IndexOf("var apiKeyHeader = Request.Headers[\"X-DHL-Key\"].ToString();", StringComparison.Ordinal));
    }

    [Fact]
    public void DependencyInjection_Should_RegisterProviderWebhookRateLimitPolicy()
    {
        var source = ReadWebApiFile(Path.Combine("Extensions", "DependencyInjection.cs"));

        source.Should().Contain("options.AddPolicy(\"provider-webhook\"");
        source.Should().Contain("PermitLimit = 60");
        source.Should().Contain("Window = TimeSpan.FromMinutes(1)");
    }

    [Fact]
    public void DependencyInjection_Should_RegisterProviderCallbackInboxWriterAsScoped()
    {
        var source = ReadWebApiFile(Path.Combine("Extensions", "DependencyInjection.cs"));

        source.Should().Contain("services.AddScoped<ProviderCallbackInboxWriter>();");
    }

    [Fact]
    public void ProviderCallbackInboxWriter_Should_KeepDuplicateSafePersistenceContractWired()
    {
        var writerSource = ReadWebApiFile(Path.Combine("Services", "ProviderCallbackInboxWriter.cs"));

        writerSource.Should().Contain("private readonly IAppDbContext _db;");
        writerSource.Should().Contain("public ProviderCallbackInboxWriter(IAppDbContext db)");
        writerSource.Should().Contain("_db = db ?? throw new ArgumentNullException(nameof(db));");
        writerSource.Should().Contain("public async Task<bool> AddIfNewAsync(");
        writerSource.Should().Contain("if (await InboxMessageExistsAsync(provider, idempotencyKey, ct).ConfigureAwait(false))");
        writerSource.Should().Contain("_db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage");
        writerSource.Should().Contain("Status = \"Pending\"");
        writerSource.Should().Contain("try");
        writerSource.Should().Contain("await _db.SaveChangesAsync(ct).ConfigureAwait(false);");
        writerSource.Should().Contain("catch (DbUpdateException)");
        writerSource.Should().Contain("if (await InboxMessageExistsAsync(provider, idempotencyKey, ct).ConfigureAwait(false))");
        writerSource.Should().Contain("return true;");
        writerSource.Should().Contain("throw;");
        writerSource.Should().Contain("private Task<bool> InboxMessageExistsAsync(string provider, string idempotencyKey, CancellationToken ct)");
        writerSource.Should().Contain(".AnyAsync(x => !x.IsDeleted && x.Provider == provider && x.IdempotencyKey == idempotencyKey, ct)");
    }

    [Fact]
    public void ValidationResources_Should_KeepProviderWebhookPayloadTooLargeMessageLocalized()
    {
        var resourceSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.resx"));
        var resourceGermanSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.de-DE.resx"));

        resourceSource.Should().Contain("<data name=\"ProviderWebhookPayloadTooLarge\"");
        resourceGermanSource.Should().Contain("<data name=\"ProviderWebhookPayloadTooLarge\"");
        resourceSource.Should().Contain("<value>Provider webhook payload is too large.</value>");
        resourceGermanSource.Should().Contain("<data name=\"ProviderWebhookPayloadTooLarge\"");
    }

    [Fact]
    public void ProviderWebhookPayloadReader_Should_ResetRequestBodyForReuseAcrossPipeline()
    {
        var readerSource = ReadWebApiFile(Path.Combine("Services", "ProviderWebhookPayloadReader.cs"));

        readerSource.Should().Contain("request.EnableBuffering();");
        readerSource.IndexOf("request.EnableBuffering();", StringComparison.Ordinal)
            .Should().BeLessThan(readerSource.IndexOf("request.Body.Position = 0;", StringComparison.Ordinal));
        var lastResetIndex = readerSource.LastIndexOf("request.Body.Position = 0;", StringComparison.Ordinal);
        lastResetIndex.Should().BeGreaterThan(readerSource.IndexOf("memory.Write(buffer, 0, read);", StringComparison.Ordinal));
    }

    [Fact]
    public void StripeWebhookController_Should_VerifySignatureAfterPayloadValidation()
    {
        var controllerSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "StripeWebhooksController.cs"));

        var signatureIndex = controllerSource.IndexOf("var signatureHeader = Request.Headers[\"Stripe-Signature\"].ToString();", StringComparison.Ordinal);
        signatureIndex.Should().BeGreaterThan(0);
        signatureIndex.Should().BeGreaterThan(controllerSource.IndexOf("if (payloadRead.PayloadTooLarge)", StringComparison.Ordinal));
        signatureIndex.Should().BeGreaterThan(controllerSource.IndexOf("if (string.IsNullOrWhiteSpace(rawPayload))", StringComparison.Ordinal));
    }

    [Fact]
    public void DhlWebhookController_Should_VerifyApiKeyAndSignatureAfterPayloadValidation()
    {
        var controllerSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "DhlWebhooksController.cs"));

        var apiKeyIndex = controllerSource.IndexOf("var apiKeyHeader = Request.Headers[\"X-DHL-Key\"].ToString();", StringComparison.Ordinal);
        apiKeyIndex.Should().BeGreaterThan(0);
        apiKeyIndex.Should().BeGreaterThan(controllerSource.IndexOf("if (payloadRead.PayloadTooLarge)", StringComparison.Ordinal));
        apiKeyIndex.Should().BeGreaterThan(controllerSource.IndexOf("if (string.IsNullOrWhiteSpace(rawPayload))", StringComparison.Ordinal));
        controllerSource.IndexOf("var signatureHeader = Request.Headers[\"X-DHL-Signature\"].ToString();", StringComparison.Ordinal)
            .Should().BeGreaterThan(apiKeyIndex);
    }

    [Fact]
    public void BrevoWebhookController_Should_KeepWebhookCredentialAndAuthenticationGuards()
    {
        var controllerSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "BrevoWebhooksController.cs"));

        controllerSource.Should().Contain("[Route(\"api/v1/public/notifications/brevo/webhooks\")]");
        controllerSource.Should().Contain("[AllowAnonymous]");
        controllerSource.Should().Contain("private readonly IStringLocalizer<ValidationResource> _validationLocalizer;");
        controllerSource.Should().Contain("private bool HasWebhookCredentials()");
        controllerSource.Should().Contain("if (!HasWebhookCredentials())");
        controllerSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"BrevoWebhookAuthenticationNotConfigured\"]);");
        controllerSource.Should().Contain("private bool TryVerifyBasicAuth(string authorizationHeader)");
        controllerSource.Should().Contain("if (!TryVerifyBasicAuth(Request.Headers.Authorization.ToString()))");
        controllerSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"BrevoWebhookAuthenticationInvalid\"]);");
    }

    [Fact]
    public void BrevoWebhookController_Should_KeepPayloadSizeGuardBeforePayloadParsing()
    {
        var controllerSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "BrevoWebhooksController.cs"));

        var payloadIndex = controllerSource.IndexOf("var payloadRead = await ProviderWebhookPayloadReader.ReadAsync(Request, ct).ConfigureAwait(false);", StringComparison.Ordinal);
        var parseIndex = controllerSource.IndexOf("if (!TryParseEnvelope(rawPayload, out var eventName, out var messageId, out var eventTimestamp))", StringComparison.Ordinal);

        payloadIndex.Should().BeGreaterThan(controllerSource.IndexOf("if (!HasWebhookCredentials())", StringComparison.Ordinal));
        payloadIndex.Should().BeGreaterThan(controllerSource.IndexOf("if (!TryVerifyBasicAuth(Request.Headers.Authorization.ToString())", StringComparison.Ordinal));
        parseIndex.Should().BeGreaterThan(payloadIndex);
        parseIndex.Should().BeGreaterThan(controllerSource.IndexOf("if (payloadRead.PayloadTooLarge)", StringComparison.Ordinal));
        controllerSource.Should().Contain("if (payloadRead.PayloadTooLarge)");
        controllerSource.Should().Contain("return PayloadTooLargeProblem(_validationLocalizer[\"ProviderWebhookPayloadTooLarge\"]);");
    }

    [Fact]
    public void BrevoWebhookController_Should_TruncateCallbackTypeBeforeIdempotency()
    {
        var controllerSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "BrevoWebhooksController.cs"));

        controllerSource.Should().Contain("private static string NormalizeCallbackType(string? value)");
        controllerSource.Should().Contain("var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;");
        controllerSource.Should().Contain("return normalized.Length <= 64 ? normalized : normalized[..64];");
        controllerSource.Should().Contain("eventName = NormalizeCallbackType(ReadStringAny(root, \"event\", \"Event\"));");
        controllerSource.Should().Contain("callbackType: eventName,");
    }

    [Fact]
    public void BrevoEmailSender_Should_KeepProviderManagedTemplateMappingOptionalAndParameterized()
    {
        var senderSource = ReadInfrastructureFile(Path.Combine("Notifications", "Brevo", "BrevoEmailSender.cs"));
        var optionsSource = ReadInfrastructureFile(Path.Combine("Notifications", "Brevo", "BrevoEmailOptions.cs"));
        var validatorSource = ReadInfrastructureFile(Path.Combine("Notifications", "Brevo", "BrevoEmailOptionsValidator.cs"));
        var deliveryValidatorSource = ReadInfrastructureFile(Path.Combine("Notifications", "EmailDeliveryOptionsValidator.cs"));
        var smtpValidatorSource = ReadInfrastructureFile(Path.Combine("Notifications", "Smtp", "SmtpEmailOptionsValidator.cs"));
        var notificationsCompositionSource = ReadInfrastructureFile(Path.Combine("Extensions", "ServiceCollectionExtensions.Notifications.cs"));
        var dispatchContextSource = ReadApplicationFile(Path.Combine("Abstractions", "Notifications", "EmailDispatchContext.cs"));
        var invitationCreateSource = ReadApplicationFile(Path.Combine("Businesses", "Commands", "CreateBusinessInvitationHandler.cs"));
        var invitationResendSource = ReadApplicationFile(Path.Combine("Businesses", "Commands", "ResendBusinessInvitationHandler.cs"));
        var emailConfirmationSource = ReadApplicationFile(Path.Combine("Identity", "Commands", "EmailConfirmationHandlers.cs"));
        var passwordResetSource = ReadApplicationFile(Path.Combine("Identity", "Commands", "RequestPasswordResetHandler.cs"));

        optionsSource.Should().Contain("public Dictionary<string, int> TemplateIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);");
        deliveryValidatorSource.Should().Contain("public sealed class EmailDeliveryOptionsValidator : IValidateOptions<EmailDeliveryOptions>");
        deliveryValidatorSource.Should().Contain("Email:Provider '{provider}' is not supported.");
        deliveryValidatorSource.Should().Contain("EmailProviderNames.Smtp or EmailProviderNames.Brevo");
        notificationsCompositionSource.Should().Contain("services.AddSingleton<IValidateOptions<EmailDeliveryOptions>, EmailDeliveryOptionsValidator>();");
        notificationsCompositionSource.Should().Contain("services.AddOptions<EmailDeliveryOptions>()");
        notificationsCompositionSource.Should().Contain("services.AddSingleton<IValidateOptions<SmtpEmailOptions>, SmtpEmailOptionsValidator>();");
        notificationsCompositionSource.Should().Contain("services.AddOptions<SmtpEmailOptions>()");
        notificationsCompositionSource.Should().Contain(".ValidateOnStart();");
        smtpValidatorSource.Should().Contain("public sealed class SmtpEmailOptionsValidator : IValidateOptions<SmtpEmailOptions>");
        smtpValidatorSource.Should().Contain("Email:Smtp:Host is required when Email:Provider is SMTP.");
        smtpValidatorSource.Should().Contain("Email:Smtp:Port must be between 1 and 65535.");
        smtpValidatorSource.Should().Contain("Email:Smtp:FromAddress must be a valid email address.");
        smtpValidatorSource.Should().Contain("Email:Smtp:Password is required when Email:Smtp:Username is configured.");
        validatorSource.Should().Contain("Email:Brevo:BaseUrl must be an absolute HTTPS URL when Email:Provider is Brevo.");
        validatorSource.Should().Contain("Email:Brevo:SenderEmail must be a valid email address.");
        validatorSource.Should().Contain("Email:Brevo:ReplyToEmail must be a valid email address.");
        validatorSource.Should().Contain("Email:Brevo:WebhookUsername is required when Email:Provider is Brevo.");
        validatorSource.Should().Contain("Email:Brevo:WebhookPassword is required when Email:Provider is Brevo.");
        validatorSource.Should().Contain("Email:Brevo:TemplateIds keys must not be empty.");
        validatorSource.Should().Contain("Email:Brevo:TemplateIds:{pair.Key} must be a positive Brevo template id.");
        validatorSource.Should().Contain("private static bool IsHttpsAbsoluteUrl");
        validatorSource.Should().Contain("private static bool IsEmailAddress");
        dispatchContextSource.Should().Contain("public IReadOnlyDictionary<string, string?> TemplateParameters { get; set; }");

        senderSource.Should().Contain("var templateId = ResolveTemplateId(context);");
        senderSource.Should().Contain("payload[\"templateId\"] = templateId.Value;");
        senderSource.Should().Contain("payload[\"params\"] = BuildTemplateParameters(context);");
        senderSource.Should().Contain("payload[\"htmlContent\"] = htmlBody ?? string.Empty;");
        senderSource.Should().Contain("if (!string.IsNullOrWhiteSpace(context.TemplateKey)");
        senderSource.Should().Contain("if (!string.IsNullOrWhiteSpace(context.FlowKey)");

        invitationCreateSource.Should().Contain("TemplateKey = \"BusinessInvitationEmail\"");
        invitationCreateSource.Should().Contain("TemplateParameters = bodyParameters");
        invitationResendSource.Should().Contain("TemplateKey = \"BusinessInvitationEmail\"");
        invitationResendSource.Should().Contain("TemplateParameters = bodyParameters");
        emailConfirmationSource.Should().Contain("TemplateKey = \"AccountActivationEmail\"");
        emailConfirmationSource.Should().Contain("TemplateParameters = bodyParameters");
        passwordResetSource.Should().Contain("TemplateKey = \"PasswordResetEmail\"");
        passwordResetSource.Should().Contain("TemplateParameters = bodyParameters");
    }

    [Fact]
    public void ValidationResources_Should_KeepBrevoWebhookMessagesLocalized()
    {
        var resourceSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.resx"));
        var resourceGermanSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.de-DE.resx"));

        resourceSource.Should().Contain("<data name=\"BrevoWebhookAuthenticationNotConfigured\" ");
        resourceSource.Should().Contain("<data name=\"BrevoWebhookAuthenticationInvalid\" ");
        resourceSource.Should().Contain("<data name=\"BrevoWebhookPayloadInvalid\" ");
        resourceGermanSource.Should().Contain("<data name=\"BrevoWebhookAuthenticationNotConfigured\" ");
        resourceGermanSource.Should().Contain("<data name=\"BrevoWebhookAuthenticationInvalid\" ");
        resourceGermanSource.Should().Contain("<data name=\"BrevoWebhookPayloadInvalid\" ");
        resourceSource.Should().Contain("<value>Brevo webhook authentication is not configured.</value>");
    }

    [Fact]
    public void SiteSettingValidator_Should_KeepProviderBackedSmsAndWhatsAppReadinessRules()
    {
        var validatorSource = ReadApplicationFile(Path.Combine("Settings", "Validators", "SiteSettingEditValidator.cs"));
        var resourceSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.resx"));
        var resourceGermanSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.de-DE.resx"));

        validatorSource.Should().Contain("RuleFor(x => x.SmsProvider)");
        validatorSource.Should().Contain(".When(x => x.SmsEnabled)");
        validatorSource.Should().Contain("string.Equals(provider, \"Twilio\", StringComparison.OrdinalIgnoreCase)");
        validatorSource.Should().Contain("SiteSettingSmsProviderUnsupported");
        validatorSource.Should().Contain("RuleFor(x => x.SmsFromPhoneE164)");
        validatorSource.Should().Contain("RuleFor(x => x.SmsApiKey)");
        validatorSource.Should().Contain("RuleFor(x => x.SmsApiSecret)");
        validatorSource.Should().Contain("RuleFor(x => x.WhatsAppBusinessPhoneId)");
        validatorSource.Should().Contain("RuleFor(x => x.WhatsAppAccessToken)");
        validatorSource.Should().Contain("RuleFor(x => x.WhatsAppFromPhoneE164)");
        validatorSource.Should().Contain("RuleFor(x => x.SmtpHost)");
        validatorSource.Should().Contain("RuleFor(x => x.SmtpPort)");
        validatorSource.Should().Contain("RuleFor(x => x.SmtpPassword)");
        validatorSource.Should().Contain("RuleFor(x => x.SmtpFromAddress)");
        validatorSource.Should().Contain("RuleFor(x => x.SmtpFromDisplayName)");
        validatorSource.Should().Contain("SiteSettingSmtpHostRequiredWhenEnabled");
        validatorSource.Should().Contain("SiteSettingSmtpPasswordRequiredWhenUsernameConfigured");

        resourceSource.Should().Contain("<data name=\"SiteSettingSmsProviderUnsupported\"");
        resourceSource.Should().Contain("<data name=\"SiteSettingWhatsAppAccessTokenRequiredWhenEnabled\"");
        resourceSource.Should().Contain("<data name=\"SiteSettingSmtpHostRequiredWhenEnabled\"");
        resourceSource.Should().Contain("<data name=\"SiteSettingSmtpPasswordRequiredWhenUsernameConfigured\"");
        resourceGermanSource.Should().Contain("<data name=\"SiteSettingSmsProviderUnsupported\"");
        resourceGermanSource.Should().Contain("<data name=\"SiteSettingWhatsAppAccessTokenRequiredWhenEnabled\"");
        resourceGermanSource.Should().Contain("<data name=\"SiteSettingSmtpHostRequiredWhenEnabled\"");
        resourceGermanSource.Should().Contain("<data name=\"SiteSettingSmtpPasswordRequiredWhenUsernameConfigured\"");
    }
}

