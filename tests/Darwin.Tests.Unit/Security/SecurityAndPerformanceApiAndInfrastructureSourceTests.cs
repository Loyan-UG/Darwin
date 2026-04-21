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
        accountSource.Should().Contain("public IActionResult Register(string? returnUrl = null)");
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
        source.Should().Contain("TempData[\"return\"] = returnUrl ?? string.Empty;");
        source.Should().Contain("return RedirectToAction(nameof(LoginTwoFactor));");
        source.Should().Contain("await IssueCookieAsync(result.UserId.Value, result.SecurityStamp!, rememberMe, ct);");
        source.Should().Contain("var dest = await DeterminePostLoginRedirectAsync(result.UserId.Value, returnUrl, ct);");
        source.Should().Contain("return Redirect(dest);");
        source.Should().Contain("ViewData[\"RememberMe\"] = TempData.TryGetValue(\"remember\", out var r) && (string?)r == \"1\";");
        source.Should().Contain("ViewData[\"TwoFaUserId\"] = idObj.ToString();");
        source.Should().Contain("var verify = await _verifyTotp.HandleAsync(new TotpVerifyDto { UserId = uid, Code = code }, ct);");
        source.Should().Contain("var stampRes = await _getSecurityStamp.HandleAsync(uid, ct);");
        source.Should().Contain("return Json(new { challengeTokenId = res.Value.ChallengeTokenId, options = res.Value.OptionsJson });");
        source.Should().Contain("return Json(new { redirect = dest });");
        source.Should().Contain("var siteSettings = _siteSettingCache.GetAsync().GetAwaiter().GetResult();");
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
        source.Should().Contain("var sign = await _signIn.HandleAsync(new SignInDto { Email = email.Trim(), Password = password, RememberMe = true }, ct);");
        source.Should().Contain("return Redirect(SafeReturnUrl(returnUrl));");
        source.Should().Contain("private async Task IssueCookieAsync(Guid userId, string securityStamp, bool persistent, CancellationToken ct)");
        source.Should().Contain("new Claim(ClaimTypes.NameIdentifier, userId.ToString())");
        source.Should().Contain("new Claim(\"sstamp\", securityStamp)");
        source.Should().Contain("CookieAuthenticationDefaults.AuthenticationScheme");
        source.Should().Contain("ExpiresUtc = persistent ? DateTimeOffset.UtcNow.AddDays(30) : (DateTimeOffset?)null");
        source.Should().Contain("private async Task<string> DeterminePostLoginRedirectAsync(Guid userId, string? returnUrl, CancellationToken ct)");
        source.Should().Contain("Uri.TryCreate(returnUrl, UriKind.Relative, out _)");
        source.Should().Contain("await _permissions.HasAsync(userId, \"AccessAdminPanel\", ct)");
        source.Should().Contain("var adminUrl = Url.Action(\"Index\", \"Home\")");
        source.Should().Contain("private static string SafeReturnUrl(string? returnUrl)");
        source.Should().Contain("if (string.IsNullOrWhiteSpace(returnUrl)) return \"~/\";");
        source.Should().Contain("return Uri.TryCreate(returnUrl, UriKind.Relative, out _) ? returnUrl : \"~/\";");
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
        dependencyInjectionSource.Should().Contain("options.ExpireTimeSpan = TimeSpan.FromDays(30);");
        dependencyInjectionSource.Should().Contain(".AddControllersWithViews(options =>");
        dependencyInjectionSource.Should().Contain("options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;");
        dependencyInjectionSource.Should().Contain(".AddViewLocalization()");
        dependencyInjectionSource.Should().Contain(".AddDataAnnotationsLocalization(options =>");
        dependencyInjectionSource.Should().Contain("factory.Create(typeof(SharedResource));");
        dependencyInjectionSource.Should().Contain("services.AddApplication();");
        dependencyInjectionSource.Should().Contain("services.AddSharedHostingDataProtection(config);");
        dependencyInjectionSource.Should().Contain("services.AddPersistence(config);");
        dependencyInjectionSource.Should().Contain("services.AddAntiforgery();");
        dependencyInjectionSource.Should().Contain("services.AddMemoryCache();");
        dependencyInjectionSource.Should().Contain("services.AddScoped<ISiteSettingCache, SiteSettingCache>();");
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
    public void MemberAndStorefrontPaymentSnapshots_Should_KeepCreatedAtUtcMappedForAttemptOrdering()
    {
        var memberOrdersSource = ReadWebApiFile(Path.Combine("Controllers", "Member", "MemberOrdersController.cs"));
        var publicCheckoutSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicCheckoutController.cs"));
        var memberOrderContractsSource = ReadContractsFile(Path.Combine("Orders", "MemberOrderContracts.cs"));
        var storefrontContractsSource = ReadContractsFile(Path.Combine("Orders", "StorefrontCheckoutContracts.cs"));

        memberOrdersSource.Should().Contain("Payments = dto.Payments.Select(payment => new MemberOrderPayment");
        memberOrdersSource.Should().Contain("CreatedAtUtc = payment.CreatedAtUtc,");
        memberOrdersSource.Should().Contain("PaidAtUtc = payment.PaidAtUtc");

        publicCheckoutSource.Should().Contain("Payments = confirmation.Payments.Select(payment => new StorefrontOrderConfirmationPayment");
        publicCheckoutSource.Should().Contain("CreatedAtUtc = payment.CreatedAtUtc,");
        publicCheckoutSource.Should().Contain("PaidAtUtc = payment.PaidAtUtc");

        memberOrderContractsSource.Should().Contain("public DateTime CreatedAtUtc { get; set; }");
        memberOrderContractsSource.Should().Contain("public DateTime? PaidAtUtc { get; set; }");

        storefrontContractsSource.Should().Contain("public DateTime CreatedAtUtc { get; set; }");
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
    public void BusinessEditorWorkspace_Should_KeepInvitationSummaryTileHelperBacked()
    {
        var editorShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessEditorShell.cshtml"));

        editorShellSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        editorShellSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        editorShellSource.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = Model.Id })");
        editorShellSource.Should().NotContain("@T.T(\"BusinessEditorPendingInvites\")");
        editorShellSource.Should().Contain("hx-push-url=\"true\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        editorShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Invitations\")</a>");
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
}

