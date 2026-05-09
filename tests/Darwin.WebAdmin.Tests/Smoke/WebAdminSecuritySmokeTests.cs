using Darwin.WebAdmin.Tests.TestInfrastructure;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;

namespace Darwin.WebAdmin.Tests.Smoke;

public sealed class WebAdminSecuritySmokeTests : IClassFixture<WebAdminTestFactory>
{
    private readonly WebAdminTestFactory _factory;

    public WebAdminSecuritySmokeTests(WebAdminTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnauthenticatedAdminRoot_ShouldRedirectToLoginAndEmitSecurityHeaders()
    {
        using var client = _factory.CreateNoRedirectClient();

        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().StartWith("https://localhost/account/login");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        var csp = cspValues?.Single() ?? string.Empty;
        csp.Should().Contain("default-src 'self'");
        csp.Should().Contain("script-src 'self'");
        csp.Should().Contain("style-src 'self'");
        csp.Should().Contain("frame-ancestors 'none'");
        csp.Should().NotContain("unsafe-inline");
        csp.Should().NotContain("unsafe-eval");
        response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions).Should().BeTrue();
        contentTypeOptions!.Single().Should().Be("nosniff");
        response.Headers.TryGetValues("Referrer-Policy", out var referrerPolicy).Should().BeTrue();
        referrerPolicy!.Single().Should().Be("strict-origin-when-cross-origin");
        response.Headers.TryGetValues("Permissions-Policy", out var permissionsPolicy).Should().BeTrue();
        permissionsPolicy!.Single().Should().Contain("camera=()");
    }

    [Fact]
    public async Task ForwardedHttpsRequest_ShouldNotBeRedirectedAgainByHttpsRedirection()
    {
        using var client = _factory.CreateNoRedirectClient(new Uri("http://localhost"));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", "127.0.0.1");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().StartWith("https://localhost/account/login");
        response.Headers.Location?.OriginalString.Should().Contain("ReturnUrl=%2F");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("frame-ancestors 'none'");
    }

    [Fact]
    public async Task LoginPage_ShouldRenderAntiForgeryTokenAndSelfHostedAssets()
    {
        using var client = _factory.CreateNoRedirectClient();

        using var response = await client.GetAsync("/account/login", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("name=\"__RequestVerificationToken\"");
        html.Should().Contain("/lib/bootstrap/css/bootstrap.min.css");
        html.Should().Contain("/lib/fontawesome/css/all.min.css");
        html.Should().Contain("/lib/jquery/jquery.min.js");
        html.Should().Contain("/lib/htmx/htmx.min.js");
        html.Should().Contain("/js/admin-core.js");
        html.Should().NotContain("https://cdn.jsdelivr.net");
        html.Should().NotContain("https://kit.fontawesome.com");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
        response.Headers.TryGetValues("Set-Cookie", out var setCookieValues).Should().BeTrue();
        var antiForgeryCookie = setCookieValues!
            .SingleOrDefault(value => value.StartsWith("Darwin.AntiForgery=", StringComparison.Ordinal));
        antiForgeryCookie.Should().NotBeNull();
        antiForgeryCookie.Should().Contain("secure", Exactly.Once(), because: "anti-forgery cookies must only travel over HTTPS");
        antiForgeryCookie.Should().Contain("httponly", Exactly.Once(), because: "client scripts should not read anti-forgery cookies");
        antiForgeryCookie.Should().Contain("samesite=lax", Exactly.Once(), because: "admin forms should keep same-site CSRF defaults");
    }

    [Fact]
    public async Task LoginPageWithExternalReturnUrl_ShouldNotReflectExternalReturnUrlInForms()
    {
        using var client = _factory.CreateNoRedirectClient();

        using var response = await client.GetAsync("/account/login?returnUrl=https://evil.example/phish", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().NotContain("evil.example");
        html.Should().Contain("name=\"returnUrl\" value=\"\"");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task RegisterPage_ShouldRenderAntiForgeryTokenDefaultsAndSelfHostedValidationAssets()
    {
        using var client = _factory.CreateNoRedirectClient();

        using var response = await client.GetAsync("/account/register", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("name=\"__RequestVerificationToken\"");
        html.Should().Contain("name=\"email\" type=\"email\"");
        html.Should().Contain("name=\"password\" type=\"password\"");
        html.Should().Contain("name=\"supportedCulturesCsv\"");
        html.Should().Contain("value=\"de-DE,en-US\"");
        html.Should().Contain("/lib/jquery-validation/jquery.validate.min.js");
        html.Should().Contain("/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js");
        html.Should().NotContain("https://cdn.jsdelivr.net");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthPageWithQueryStringCulture_ShouldRenderMatchingHtmlLang()
    {
        using var client = _factory.CreateNoRedirectClient();

        using var response = await client.GetAsync("/account/login?culture=en-US", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("<html lang=\"en-US\">");
        html.Should().Contain("<option value=\"en-US\" selected");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("default-src 'self'");
    }

    [Fact]
    public async Task CultureCookie_ShouldDriveAuthPageHtmlLang()
    {
        using var client = _factory.CreateNoRedirectClient();
        using var loginResponse = await client.GetAsync("/account/login", TestContext.Current.CancellationToken);
        var loginHtml = await loginResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var token = ExtractAntiForgeryToken(loginHtml);
        using var cultureContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["culture"] = "en-US",
            ["returnUrl"] = "/account/login",
            ["__RequestVerificationToken"] = token
        });

        using var cultureResponse = await client.PostAsync("/Culture/SetCulture", cultureContent, TestContext.Current.CancellationToken);
        cultureResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        cultureResponse.Headers.Location?.OriginalString.Should().Be("/account/login");
        cultureResponse.Headers.TryGetValues("Set-Cookie", out var setCookieValues).Should().BeTrue();
        var cultureCookie = setCookieValues!.Single(value => value.StartsWith(".AspNetCore.Culture=", StringComparison.Ordinal));
        cultureCookie.Should().Contain("samesite=lax");
        cultureCookie.Should().Contain("secure");

        using var localizedResponse = await client.GetAsync("/account/login", TestContext.Current.CancellationToken);
        var localizedHtml = await localizedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        localizedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        localizedHtml.Should().Contain("<html lang=\"en-US\">");
    }

    [Fact]
    public async Task CulturePostWithoutAntiForgeryToken_ShouldBeRejected()
    {
        using var client = _factory.CreateNoRedirectClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["culture"] = "en-US",
            ["returnUrl"] = "/account/login"
        });

        using var response = await client.PostAsync("/Culture/SetCulture", content, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task CulturePostWithExternalReturnUrl_ShouldNotOpenRedirect()
    {
        using var client = _factory.CreateNoRedirectClient();
        using var loginResponse = await client.GetAsync("/account/login", TestContext.Current.CancellationToken);
        var loginHtml = await loginResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var token = ExtractAntiForgeryToken(loginHtml);
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["culture"] = "en-US",
            ["returnUrl"] = "https://evil.example/phish",
            ["__RequestVerificationToken"] = token
        });

        using var response = await client.PostAsync("/Culture/SetCulture", content, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Be("/");
        response.Headers.Location?.OriginalString.Should().NotContain("evil.example");
        response.Headers.TryGetValues("Set-Cookie", out var setCookieValues).Should().BeTrue();
        setCookieValues!.Should().Contain(value => value.StartsWith(".AspNetCore.Culture=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task LoginTwoFactorWithoutTempData_ShouldRedirectBackToLogin()
    {
        using var client = _factory.CreateNoRedirectClient();

        using var response = await client.GetAsync("/account/login-2fa", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Be("/account/login");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("frame-ancestors 'none'");
    }

    [Theory]
    [InlineData("/account/login")]
    [InlineData("/account/login-2fa")]
    [InlineData("/account/register")]
    [InlineData("/account/webauthn/begin-login")]
    [InlineData("/account/webauthn/finish-login")]
    [InlineData("/account/logout")]
    public async Task AccountPostEndpointsWithoutAntiForgeryToken_ShouldBeRejectedBeforeHandlers(string path)
    {
        using var client = path.EndsWith("/logout", StringComparison.Ordinal)
            ? _factory.CreateAuthenticatedNoRedirectClient()
            : _factory.CreateNoRedirectClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "webadmin-smoke@example.test",
            ["password"] = "not-used",
            ["userId"] = "22222222-2222-2222-2222-222222222222",
            ["code"] = "123456",
            ["challengeTokenId"] = "33333333-3333-3333-3333-333333333333",
            ["clientResponseJson"] = "{}"
        });

        using var response = await client.PostAsync(path, content, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
        response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions).Should().BeTrue();
        contentTypeOptions!.Single().Should().Be("nosniff");
    }

    [Fact]
    public async Task AuthenticatedLogoutPostWithAntiForgeryHeader_ShouldPassTokenValidation()
    {
        using var client = _factory.CreateAuthenticatedNoRedirectClient();
        using var loginResponse = await client.GetAsync("/account/login", TestContext.Current.CancellationToken);
        var loginHtml = await loginResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var token = ExtractAntiForgeryToken(loginHtml);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/account/logout")
        {
            Content = new FormUrlEncodedContent([])
        };
        request.Headers.TryAddWithoutValidation("RequestVerificationToken", token);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Be("/");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedLogoutPostWithInvalidAntiForgeryHeader_ShouldBeRejected()
    {
        using var client = _factory.CreateAuthenticatedNoRedirectClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/account/logout")
        {
            Content = new FormUrlEncodedContent([])
        };
        request.Headers.TryAddWithoutValidation("RequestVerificationToken", "invalid-token");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task UnauthenticatedAdminFragment_ShouldRedirectToLogin()
    {
        using var client = _factory.CreateNoRedirectClient();

        using var response = await client.GetAsync("/Home/AlertsFragment", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().StartWith("https://localhost/account/login");
        response.Headers.Location?.OriginalString.Should().Contain("ReturnUrl=%2FHome%2FAlertsFragment");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("frame-ancestors 'none'");
    }

    [Fact]
    public async Task AuthenticatedAlertsFragment_ShouldRenderWithoutDatabaseBackedDashboardQueries()
    {
        using var client = _factory.CreateAuthenticatedNoRedirectClient();

        using var response = await client.GetAsync("/Home/AlertsFragment", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().BeNullOrWhiteSpace();
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("frame-ancestors 'none'");
        response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions).Should().BeTrue();
        contentTypeOptions!.Single().Should().Be("nosniff");
    }

    [Fact]
    public async Task AuthenticatedDashboard_ShouldRenderAgainstTestDatabaseContext()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Darwin Admin");
        html.Should().Contain("businessId");
        html.Should().Contain("/lib/htmx/htmx.min.js");
        html.Should().Contain("/js/admin-core.js");
        html.Should().NotContain("https://cdn.jsdelivr.net");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("default-src 'self'");
        response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions).Should().BeTrue();
        contentTypeOptions!.Single().Should().Be("nosniff");
    }

    [Fact]
    public async Task AuthenticatedDashboardWithoutRequiredPermission_ShouldBeForbiddenBeforeDatabaseQueries()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(allowPermissions: false);

        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("frame-ancestors 'none'");
    }

    [Theory]
    [InlineData("/Users")]
    [InlineData("/Roles")]
    [InlineData("/Permissions")]
    [InlineData("/Brands")]
    [InlineData("/Categories")]
    [InlineData("/Products")]
    [InlineData("/Pages")]
    [InlineData("/Media")]
    [InlineData("/Orders")]
    [InlineData("/ShippingMethods")]
    [InlineData("/Businesses")]
    [InlineData("/BusinessCommunications")]
    [InlineData("/Billing/Payments")]
    [InlineData("/Billing/Refunds")]
    [InlineData("/Billing/Webhooks")]
    [InlineData("/Billing/TaxCompliance")]
    [InlineData("/Billing/FinancialAccounts")]
    [InlineData("/Billing/Expenses")]
    [InlineData("/Billing/JournalEntries")]
    [InlineData("/Billing/Plans")]
    [InlineData("/Inventory/Warehouses")]
    [InlineData("/Inventory/Suppliers")]
    [InlineData("/Inventory/StockLevels")]
    [InlineData("/Inventory/StockTransfers")]
    [InlineData("/Inventory/PurchaseOrders")]
    [InlineData("/Loyalty/Programs")]
    [InlineData("/Loyalty/Accounts")]
    [InlineData("/Loyalty/Campaigns")]
    [InlineData("/Loyalty/RewardTiers?loyaltyProgramId=55555555-5555-5555-5555-555555555555")]
    [InlineData("/Loyalty/Redemptions")]
    [InlineData("/Loyalty/ScanSessions")]
    public async Task AuthenticatedAdminListPages_ShouldRenderAgainstTestDatabaseContext(string path)
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Darwin Admin");
        html.Should().Contain("/js/admin-core.js");
        html.Should().NotContain("https://cdn.jsdelivr.net");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("default-src 'self'");
        response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions).Should().BeTrue();
        contentTypeOptions!.Single().Should().Be("nosniff");
    }

    [Theory]
    [InlineData("/Media/Create")]
    [InlineData("/ShippingMethods/Create")]
    [InlineData("/Billing/CreatePlan")]
    [InlineData("/Billing/CreatePayment")]
    [InlineData("/Billing/CreateFinancialAccount")]
    [InlineData("/Billing/CreateExpense")]
    [InlineData("/Billing/CreateJournalEntry")]
    [InlineData("/Businesses/Create")]
    [InlineData("/Businesses/CreateLocation?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Businesses/CreateInvitation?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Businesses/CreateMember?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Loyalty/CreateProgram")]
    [InlineData("/Loyalty/CreateCampaign")]
    [InlineData("/Loyalty/CreateRewardTier?loyaltyProgramId=55555555-5555-5555-5555-555555555555")]
    public async Task AuthenticatedAdminCreateEditors_ShouldRenderAgainstTestDatabaseContext(string path)
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Darwin Admin");
        html.Should().Contain("name=\"__RequestVerificationToken\"");
        html.Should().Contain("/js/admin-core.js");
        html.Should().NotContain("https://cdn.jsdelivr.net");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
        response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions).Should().BeTrue();
        contentTypeOptions!.Single().Should().Be("nosniff");
    }

    [Theory]
    [InlineData("/Businesses/Edit?id=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Businesses/Setup?id=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Businesses/SupportQueue?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Businesses/MerchantReadiness?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Businesses/Locations?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Businesses/Members?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Businesses/Invitations?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Businesses/OwnerOverrideAudits?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Businesses/Subscription?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Businesses/SubscriptionInvoices?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/BusinessCommunications/Details?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/BusinessCommunications/EmailAudits?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/BusinessCommunications/ChannelAudits?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Loyalty/EditProgram?id=55555555-5555-5555-5555-555555555555")]
    public async Task AuthenticatedSeededEntityPages_ShouldRenderAgainstTestDatabaseContext(string path)
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Darwin Admin");
        html.Should().Contain("/js/admin-core.js");
        html.Should().NotContain("https://cdn.jsdelivr.net");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("default-src 'self'");
        response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions).Should().BeTrue();
        contentTypeOptions!.Single().Should().Be("nosniff");
    }

    [Theory]
    [InlineData("/Media")]
    [InlineData("/ShippingMethods")]
    [InlineData("/Billing/Payments")]
    [InlineData("/Billing/Refunds")]
    [InlineData("/Billing/Webhooks")]
    [InlineData("/Billing/FinancialAccounts")]
    [InlineData("/Billing/Expenses")]
    [InlineData("/Billing/JournalEntries")]
    [InlineData("/Billing/Plans")]
    [InlineData("/Businesses")]
    [InlineData("/Businesses/SupportQueue?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/BusinessCommunications")]
    [InlineData("/BusinessCommunications/Details?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/BusinessCommunications/EmailAudits?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/BusinessCommunications/ChannelAudits?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Loyalty/Programs")]
    [InlineData("/Loyalty/RewardTiers?loyaltyProgramId=55555555-5555-5555-5555-555555555555")]
    [InlineData("/Orders")]
    [InlineData("/Orders/ShipmentsQueue")]
    [InlineData("/Orders/ReturnsQueue")]
    [InlineData("/Home/CommunicationOpsFragment?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Home/BusinessSupportQueueFragment?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Businesses/SupportQueueSummaryFragment?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Businesses/SupportQueueAttentionFragment")]
    [InlineData("/Businesses/SupportQueueFailedEmailsFragment")]
    [InlineData("/Businesses/SetupMembersPreview?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Businesses/SetupInvitationsPreview?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/MobileOperations")]
    public async Task AuthenticatedHtmxListAndDetailPartials_ShouldRenderWithoutLayoutAgainstTestDatabaseContext(string path)
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        using var response = await SendHtmxGetAsync(client, path);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().NotContain("<!DOCTYPE html>");
        html.Should().NotContain("/js/admin-core.js");
        html.Should().NotContain("https://cdn.jsdelivr.net");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("default-src 'self'");
        response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions).Should().BeTrue();
        contentTypeOptions!.Single().Should().Be("nosniff");
    }

    [Theory]
    [InlineData("/Media/Create")]
    [InlineData("/ShippingMethods/Create")]
    [InlineData("/Billing/CreatePlan")]
    [InlineData("/Billing/CreatePayment")]
    [InlineData("/Billing/CreateFinancialAccount")]
    [InlineData("/Billing/CreateExpense")]
    [InlineData("/Billing/CreateJournalEntry")]
    [InlineData("/Businesses/Create")]
    [InlineData("/Businesses/CreateLocation?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Businesses/CreateInvitation?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Businesses/CreateMember?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/SiteSettings/Edit")]
    [InlineData("/Loyalty/CreateProgram")]
    [InlineData("/Loyalty/CreateCampaign")]
    [InlineData("/Loyalty/CreateRewardTier?loyaltyProgramId=55555555-5555-5555-5555-555555555555")]
    public async Task AuthenticatedHtmxEditorPartials_ShouldRenderAntiForgeryTokenWithoutLayoutAgainstTestDatabaseContext(string path)
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        using var response = await SendHtmxGetAsync(client, path);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().NotContain("<!DOCTYPE html>");
        html.Should().Contain("name=\"__RequestVerificationToken\"");
        html.Should().NotContain("/js/admin-core.js");
        html.Should().NotContain("https://cdn.jsdelivr.net");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
        response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions).Should().BeTrue();
        contentTypeOptions!.Single().Should().Be("nosniff");
    }

    [Theory]
    [InlineData("/Media/Create")]
    [InlineData("/ShippingMethods/Create")]
    [InlineData("/Billing/CreatePlan")]
    [InlineData("/Billing/CreatePayment")]
    [InlineData("/Billing/CreateFinancialAccount")]
    [InlineData("/Billing/CreateExpense")]
    [InlineData("/Billing/CreateJournalEntry")]
    [InlineData("/Businesses/Create")]
    [InlineData("/Businesses/Edit")]
    [InlineData("/Businesses/Setup")]
    [InlineData("/Businesses/SetSubscriptionCancelAtPeriodEnd")]
    [InlineData("/Businesses/ProvisionSupportCustomer")]
    [InlineData("/Businesses/Delete")]
    [InlineData("/Businesses/Approve")]
    [InlineData("/Businesses/Suspend")]
    [InlineData("/Businesses/Reactivate")]
    [InlineData("/Businesses/CreateLocation")]
    [InlineData("/Businesses/EditLocation")]
    [InlineData("/Businesses/DeleteLocation")]
    [InlineData("/Businesses/CreateInvitation")]
    [InlineData("/Businesses/ResendInvitation")]
    [InlineData("/Businesses/RevokeInvitation")]
    [InlineData("/Businesses/CreateMember")]
    [InlineData("/Businesses/EditMember")]
    [InlineData("/Businesses/DeleteMember")]
    [InlineData("/Businesses/ForceDeleteMember")]
    [InlineData("/Businesses/SendMemberActivationEmail")]
    [InlineData("/Businesses/ConfirmMemberEmail")]
    [InlineData("/Businesses/SendMemberPasswordReset")]
    [InlineData("/Businesses/LockMemberUser")]
    [InlineData("/Businesses/UnlockMemberUser")]
    [InlineData("/BusinessCommunications/RetryEmailAudit")]
    [InlineData("/BusinessCommunications/SendTestEmail")]
    [InlineData("/BusinessCommunications/SendTestSms")]
    [InlineData("/BusinessCommunications/SendTestWhatsApp")]
    [InlineData("/SiteSettings/Edit")]
    [InlineData("/Loyalty/CreateProgram")]
    [InlineData("/Loyalty/CreateCampaign")]
    [InlineData("/Loyalty/CreateRewardTier")]
    [InlineData("/MobileOperations/ClearPushToken")]
    [InlineData("/MobileOperations/DeactivateDevice")]
    [InlineData("/Orders/AddPayment")]
    [InlineData("/Orders/AddShipment")]
    [InlineData("/Orders/GenerateDhlLabel")]
    [InlineData("/Orders/AddRefund")]
    [InlineData("/Orders/CreateInvoice")]
    [InlineData("/Orders/ChangeStatus")]
    [InlineData("/Inventory/CreateWarehouse")]
    [InlineData("/Inventory/EditWarehouse")]
    [InlineData("/Inventory/CreateSupplier")]
    [InlineData("/Inventory/EditSupplier")]
    [InlineData("/Inventory/AdjustStock")]
    [InlineData("/Inventory/ReserveStock")]
    [InlineData("/Inventory/ReleaseReservation")]
    [InlineData("/Inventory/ReturnReceipt")]
    [InlineData("/Inventory/CreateStockLevel")]
    [InlineData("/Inventory/EditStockLevel")]
    [InlineData("/Inventory/CreateStockTransfer")]
    [InlineData("/Inventory/EditStockTransfer")]
    [InlineData("/Inventory/CreatePurchaseOrder")]
    [InlineData("/Inventory/EditPurchaseOrder")]
    [InlineData("/Users/Create")]
    [InlineData("/Users/Edit")]
    [InlineData("/Users/ChangeEmail")]
    [InlineData("/Users/ConfirmEmail")]
    [InlineData("/Users/SendActivationEmail")]
    [InlineData("/Users/SendPasswordReset")]
    [InlineData("/Users/Activate")]
    [InlineData("/Users/Deactivate")]
    [InlineData("/Users/Lock")]
    [InlineData("/Users/Unlock")]
    [InlineData("/Users/ChangePassword")]
    [InlineData("/Users/Delete")]
    [InlineData("/Users/CreateAddress")]
    [InlineData("/Users/EditAddress")]
    [InlineData("/Users/DeleteAddress")]
    [InlineData("/Users/SetDefaultAddress")]
    [InlineData("/Users/Roles")]
    [InlineData("/AddOnGroups/Create")]
    [InlineData("/AddOnGroups/Edit")]
    [InlineData("/AddOnGroups/Delete")]
    [InlineData("/AddOnGroups/AttachToProducts")]
    [InlineData("/AddOnGroups/AttachToCategories")]
    [InlineData("/AddOnGroups/AttachToBrands")]
    [InlineData("/AddOnGroups/AttachToVariants")]
    [InlineData("/Brands/Create")]
    [InlineData("/Brands/Edit")]
    [InlineData("/Brands/Delete")]
    [InlineData("/Categories/Create")]
    [InlineData("/Categories/Edit")]
    [InlineData("/Categories/Delete")]
    [InlineData("/Products/Create")]
    [InlineData("/Products/Edit")]
    [InlineData("/Products/Delete")]
    [InlineData("/Pages/Create")]
    [InlineData("/Pages/Edit")]
    [InlineData("/Pages/Delete")]
    [InlineData("/Crm/CreateCustomer")]
    [InlineData("/Crm/EditCustomer")]
    [InlineData("/Crm/EditInvoice")]
    [InlineData("/Crm/TransitionInvoiceStatus")]
    [InlineData("/Crm/RefundInvoice")]
    [InlineData("/Crm/CreateLead")]
    [InlineData("/Crm/EditLead")]
    [InlineData("/Crm/CreateOpportunity")]
    [InlineData("/Crm/EditOpportunity")]
    [InlineData("/Crm/ConvertLead")]
    [InlineData("/Crm/CreateSegment")]
    [InlineData("/Crm/EditSegment")]
    [InlineData("/Crm/CustomerInteractions")]
    [InlineData("/Crm/LeadInteractions")]
    [InlineData("/Crm/OpportunityInteractions")]
    [InlineData("/Crm/CustomerConsents")]
    [InlineData("/Crm/CustomerSegmentMemberships")]
    [InlineData("/Crm/RemoveCustomerSegmentMembership")]
    public async Task AuthenticatedAdminPostEndpointsWithoutAntiForgeryToken_ShouldBeRejectedBeforeHandlers(string path)
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        using var response = await SendHtmxPostAsync(client, path, new Dictionary<string, string>
        {
            ["id"] = "66666666-6666-6666-6666-666666666666",
            ["orderId"] = "77777777-7777-7777-7777-777777777777",
            ["shipmentId"] = "88888888-8888-8888-8888-888888888888",
            ["paymentId"] = "99999999-9999-9999-9999-999999999999",
            ["subscriptionId"] = "12121212-1212-1212-1212-121212121212",
            ["stockLevelId"] = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            ["warehouseId"] = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            ["userId"] = "22222222-2222-2222-2222-222222222222",
            ["customerId"] = "cccccccc-cccc-cccc-cccc-cccccccccccc",
            ["leadId"] = "dddddddd-dddd-dddd-dddd-dddddddddddd",
            ["opportunityId"] = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
            ["membershipId"] = "ffffffff-ffff-ffff-ffff-ffffffffffff",
            ["businessId"] = "44444444-4444-4444-4444-444444444444",
            ["loyaltyProgramId"] = "55555555-5555-5555-5555-555555555555",
            ["name"] = "Tokenless mutation smoke",
            ["displayName"] = "Tokenless mutation smoke",
            ["email"] = "tokenless-mutation@example.test",
            ["newEmail"] = "tokenless-new@example.test",
            ["password"] = "P@ssw0rd-not-used",
            ["confirmPassword"] = "P@ssw0rd-not-used",
            ["kind"] = "Billing",
            ["currency"] = "EUR",
            ["amountMinor"] = "100",
            ["quantity"] = "1",
            ["status"] = "Draft",
            ["title"] = "Tokenless mutation smoke",
            ["slug"] = "tokenless-mutation-smoke",
            ["description"] = "Should be rejected before any handler runs.",
            ["isActive"] = "true"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
        response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions).Should().BeTrue();
        contentTypeOptions!.Single().Should().Be("nosniff");
    }

    [Theory]
    [InlineData("/Media/Create", "/Media/Create")]
    [InlineData("/ShippingMethods/Create", "/ShippingMethods/Create")]
    [InlineData("/Billing/CreatePlan", "/Billing/CreatePlan")]
    [InlineData("/Businesses/Create", "/Businesses/Create")]
    [InlineData("/Businesses/CreateLocation?businessId=44444444-4444-4444-4444-444444444444", "/Businesses/CreateLocation")]
    [InlineData("/Businesses/CreateInvitation?businessId=44444444-4444-4444-4444-444444444444", "/Businesses/CreateInvitation")]
    [InlineData("/Loyalty/CreateProgram", "/Loyalty/CreateProgram")]
    [InlineData("/Loyalty/CreateRewardTier?loyaltyProgramId=55555555-5555-5555-5555-555555555555", "/Loyalty/CreateRewardTier")]
    public async Task AuthenticatedAdminPostEndpointsWithValidAntiForgeryToken_ShouldReachHandlerValidation(
        string tokenSourcePath,
        string postPath)
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        using var tokenResponse = await SendHtmxGetAsync(client, tokenSourcePath);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var token = ExtractAntiForgeryToken(tokenHtml);

        using var response = await SendHtmxPostAsync(client, postPath, new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["businessId"] = "44444444-4444-4444-4444-444444444444",
            ["loyaltyProgramId"] = "55555555-5555-5555-5555-555555555555"
        });

        response.StatusCode.Should().NotBe(HttpStatusCode.BadRequest, because: "a real token from the editor should pass CSRF validation and reach handler/model validation");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
        response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions).Should().BeTrue();
        contentTypeOptions!.Single().Should().Be("nosniff");
    }

    [Fact]
    public async Task AuthenticatedAdminCreatesWithValidAntiForgeryToken_ShouldPersistAndReturnHtmxRedirect()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var billingCode = $"SMOKE-{suffix}".ToUpperInvariant();
        var billingName = $"Smoke Billing {suffix}";
        var addOnGroupName = $"Smoke AddOn {suffix}";
        var brandName = $"Smoke Brand {suffix}";
        var brandSlug = $"smoke-brand-{suffix}";
        var pageTitle = $"Smoke Page {suffix}";
        var pageSlug = $"smoke-page-{suffix}";
        var categoryName = $"Smoke Category {suffix}";
        var categorySlug = $"smoke-category-{suffix}";
        var productName = $"Smoke Product {suffix}";
        var productSlug = $"smoke-product-{suffix}";
        var productSku = $"SKU-{suffix}".ToUpperInvariant();
        var shippingName = $"Smoke Shipping {suffix}";
        var shippingCarrier = $"SmokeCarrier{suffix}";
        var shippingService = $"SmokeService{suffix}";
        var businessName = $"Smoke Business {suffix}";
        var businessEmail = $"business-{suffix}@example.test";
        var locationName = $"Smoke Location {suffix}";
        var invitationEmail = $"invite-{suffix}@example.test";
        var memberEmail = "webadmin-member@example.test";
        var loyaltyProgramName = $"Smoke Loyalty Program {suffix}";
        var loyaltyCampaignName = $"Smoke Campaign {suffix}";
        var loyaltyCampaignTitle = $"Smoke Campaign Title {suffix}";
        var rewardTierDescription = $"Smoke reward tier {suffix}";
        var paymentProvider = $"SmokePay-{suffix}";
        var paymentReference = $"pay-{suffix}";
        var assetAccountName = $"Smoke Cash {suffix}";
        var assetAccountCode = $"100-{suffix}".ToUpperInvariant();
        var revenueAccountName = $"Smoke Revenue {suffix}";
        var revenueAccountCode = $"400-{suffix}".ToUpperInvariant();
        var expenseDescription = $"Smoke expense {suffix}";
        var journalDescription = $"Smoke journal entry {suffix}";
        var warehouseFromName = $"Smoke Warehouse From {suffix}";
        var warehouseToName = $"Smoke Warehouse To {suffix}";
        var supplierName = $"Smoke Supplier {suffix}";
        var supplierEmail = $"supplier-{suffix}@example.test";
        var purchaseOrderNumber = $"PO-SMOKE-{suffix}".ToUpperInvariant();
        var crmCustomerFirstName = $"Smoke Customer {suffix}";
        var crmCustomerLastName = "Contact";
        var crmCustomerEmail = $"customer-{suffix}@example.test";
        var crmLeadFirstName = $"Smoke Lead {suffix}";
        var crmLeadLastName = "Prospect";
        var crmLeadEmail = $"lead-{suffix}@example.test";
        var crmSegmentName = $"Smoke Segment {suffix}";
        var crmOpportunityTitle = $"Smoke Opportunity {suffix}";
        var roleKey = $"smoke-role-{suffix}";
        var roleDisplayName = $"Smoke Role {suffix}";
        var permissionKey = $"smoke.permission.{suffix}";
        var permissionDisplayName = $"Smoke Permission {suffix}";
        var userEmail = $"user-{suffix}@example.test";
        var lifecycleCurrentEmail = "webadmin-lifecycle@example.test";
        var lifecycleNewEmail = $"lifecycle-{suffix}@example.test";
        var orderPaymentProvider = $"OrderPay-{suffix}";
        var orderPaymentReference = $"order-pay-{suffix}";
        var shipmentTrackingNumber = $"TRACK{suffix}".ToUpperInvariant();
        var shipmentProviderReference = $"ship-{suffix}";
        var refundReason = $"Smoke refund {suffix}";
        var siteSettingsTitle = $"Darwin WebAdmin Smoke {suffix}";
        var emailSubjectTemplate = $"Smoke email transport {suffix} {{test_target}}";
        var mediaFileName = $"smoke-media-{suffix}.png";
        var mediaTitle = $"Smoke Media {suffix}";

        await PostValidSiteSettingsMutationAndAssertUpdatedAsync(
            client,
            siteSettingsTitle,
            emailSubjectTemplate);

        await PostValidMediaUploadMutationAndAssertListedAsync(
            client,
            mediaFileName,
            mediaTitle);
        await PostValidMediaEditAndDeleteLifecycleMutationAsync(client, $"Updated Media {suffix}");

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Billing/CreatePlan",
            "/Billing/CreatePlan",
            $"/Billing/Plans?q={Uri.EscapeDataString(billingCode)}",
            billingName,
            new Dictionary<string, string>
            {
                ["Code"] = billingCode,
                ["Name"] = billingName,
                ["Description"] = "Smoke-created billing plan.",
                ["PriceMinor"] = "990",
                ["Currency"] = "EUR",
                ["Interval"] = "Month",
                ["IntervalCount"] = "1",
                ["TrialDays"] = "0",
                ["IsActive"] = "true",
                ["FeaturesJson"] = "{\"smoke\":true}"
            });
        await PostValidBillingPlanEditMutationAndAssertInactiveAsync(
            client,
            WebAdminTestFactory.TestBillingPlanId,
            "WEBADMIN-SMOKE-SEEDED-PLAN",
            $"Updated {billingName}");

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/AddOnGroups/Create",
            "/AddOnGroups/Create",
            $"/AddOnGroups?query={Uri.EscapeDataString(addOnGroupName)}",
            addOnGroupName,
            new Dictionary<string, string>
            {
                ["Name"] = addOnGroupName,
                ["Currency"] = "EUR",
                ["SelectionMode"] = "Single",
                ["MinSelections"] = "0",
                ["MaxSelections"] = "1",
                ["IsGlobal"] = "true",
                ["IsActive"] = "true",
                ["Options[0].Label"] = "Sauce",
                ["Options[0].SortOrder"] = "0",
                ["Options[0].Values[0].Label"] = "Garlic",
                ["Options[0].Values[0].PriceDeltaMinor"] = "50",
                ["Options[0].Values[0].SortOrder"] = "0",
                ["Options[0].Values[0].IsActive"] = "true"
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Brands/Create",
            "/Brands/Create",
            $"/Brands?query={Uri.EscapeDataString(brandSlug)}",
            brandName,
            new Dictionary<string, string>
            {
                ["Slug"] = brandSlug,
                ["Translations[0].Culture"] = "de-DE",
                ["Translations[0].Name"] = brandName,
                ["Translations[0].DescriptionHtml"] = "<p>Smoke-created brand.</p>"
            });
        await PostValidBrandEditAndDeleteLifecycleMutationAsync(
            client,
            "webadmin-smoke-brand-lifecycle",
            $"Updated Brand {suffix}");

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Pages/Create",
            "/Pages/Create",
            $"/Pages?query={Uri.EscapeDataString(pageSlug)}",
            pageTitle,
            new Dictionary<string, string>
            {
                ["Status"] = "Draft",
                ["Translations.Index"] = "0",
                ["Translations[0].Culture"] = "de-DE",
                ["Translations[0].Title"] = pageTitle,
                ["Translations[0].Slug"] = pageSlug,
                ["Translations[0].MetaTitle"] = pageTitle,
                ["Translations[0].MetaDescription"] = "Smoke-created CMS page.",
                ["Translations[0].ContentHtml"] = "<p>Smoke-created CMS page.</p>"
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Categories/Create",
            "/Categories/Create",
            $"/Categories?query={Uri.EscapeDataString(categorySlug)}",
            categoryName,
            new Dictionary<string, string>
            {
                ["SortOrder"] = "10",
                ["IsActive"] = "true",
                ["Translations.Index"] = "0",
                ["Translations[0].Culture"] = "de-DE",
                ["Translations[0].Name"] = categoryName,
                ["Translations[0].Slug"] = categorySlug,
                ["Translations[0].Description"] = "Smoke-created category."
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Products/Create",
            "/Products/Create",
            $"/Products?query={Uri.EscapeDataString(productSku)}",
            productName,
            new Dictionary<string, string>
            {
                ["BrandId"] = WebAdminTestFactory.TestBrandId.ToString(),
                ["PrimaryCategoryId"] = WebAdminTestFactory.TestCategoryId.ToString(),
                ["Kind"] = "Simple",
                ["Translations.Index"] = "0",
                ["Translations[0].Culture"] = "de-DE",
                ["Translations[0].Name"] = productName,
                ["Translations[0].Slug"] = productSlug,
                ["Translations[0].MetaTitle"] = productName,
                ["Translations[0].MetaDescription"] = "Smoke-created product.",
                ["Translations[0].FullDescriptionHtml"] = "<p>Smoke-created product.</p>",
                ["Variants.Index"] = "0",
                ["Variants[0].Sku"] = productSku,
                ["Variants[0].Currency"] = "EUR",
                ["Variants[0].TaxCategoryId"] = WebAdminTestFactory.TestTaxCategoryId.ToString(),
                ["Variants[0].BasePriceNetMinor"] = "1299",
                ["Variants[0].StockOnHand"] = "5",
                ["Variants[0].StockReserved"] = "0",
                ["Variants[0].ReorderPoint"] = "1",
                ["Variants[0].BackorderAllowed"] = "false",
                ["Variants[0].IsDigital"] = "false"
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/ShippingMethods/Create",
            "/ShippingMethods/Create",
            $"/ShippingMethods?query={Uri.EscapeDataString(shippingCarrier)}",
            shippingName,
            new Dictionary<string, string>
            {
                ["Name"] = shippingName,
                ["Carrier"] = shippingCarrier,
                ["Service"] = shippingService,
                ["CountriesCsv"] = "DE,AT",
                ["Currency"] = "EUR",
                ["IsActive"] = "true",
                ["Rates[0].MaxShipmentMass"] = "5000",
                ["Rates[0].MaxSubtotalNetMinor"] = "10000",
                ["Rates[0].PriceMinor"] = "499",
                ["Rates[0].SortOrder"] = "0"
            });

        await PostValidOrderStatusMutationAndAssertDetailsAsync(
            client,
            WebAdminTestFactory.TestOrderId,
            "Confirmed");

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            $"/Orders/AddPayment?orderId={WebAdminTestFactory.TestOrderId}",
            "/Orders/AddPayment",
            $"/Orders/Payments?orderId={WebAdminTestFactory.TestOrderId}",
            orderPaymentReference,
            new Dictionary<string, string>
            {
                ["OrderId"] = WebAdminTestFactory.TestOrderId.ToString(),
                ["Provider"] = orderPaymentProvider,
                ["ProviderReference"] = orderPaymentReference,
                ["AmountMinor"] = "2599",
                ["Currency"] = "EUR",
                ["Status"] = "Captured",
                ["FailureReason"] = string.Empty
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            $"/Orders/AddShipment?orderId={WebAdminTestFactory.TestOrderId}",
            "/Orders/AddShipment",
            $"/Orders/Shipments?orderId={WebAdminTestFactory.TestOrderId}",
            shipmentTrackingNumber,
            new Dictionary<string, string>
            {
                ["OrderId"] = WebAdminTestFactory.TestOrderId.ToString(),
                ["Carrier"] = "SmokeCarrier",
                ["Service"] = "SmokeService",
                ["ProviderShipmentReference"] = shipmentProviderReference,
                ["TrackingNumber"] = shipmentTrackingNumber,
                ["LabelUrl"] = string.Empty,
                ["LastCarrierEventKey"] = string.Empty,
                ["TotalWeight"] = "1200",
                ["Lines[0].OrderLineId"] = WebAdminTestFactory.TestOrderLineId.ToString(),
                ["Lines[0].Label"] = "WEBADMIN-SMOKE-VARIANT - WebAdmin Smoke Inventory Product",
                ["Lines[0].Quantity"] = "1"
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            $"/Orders/CreateInvoice?orderId={WebAdminTestFactory.TestOrderId}",
            "/Orders/CreateInvoice",
            $"/Orders/Invoices?orderId={WebAdminTestFactory.TestOrderId}",
            "seed-order-payment",
            new Dictionary<string, string>
            {
                ["OrderId"] = WebAdminTestFactory.TestOrderId.ToString(),
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["CustomerId"] = string.Empty,
                ["PaymentId"] = WebAdminTestFactory.TestOrderPaymentId.ToString(),
                ["DueAtUtc"] = "2026-05-24T12:00:00"
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            $"/Orders/AddRefund?orderId={WebAdminTestFactory.TestOrderId}&paymentId={WebAdminTestFactory.TestOrderPaymentId}",
            "/Orders/AddRefund",
            $"/Orders/Refunds?orderId={WebAdminTestFactory.TestOrderId}",
            refundReason,
            new Dictionary<string, string>
            {
                ["OrderId"] = WebAdminTestFactory.TestOrderId.ToString(),
                ["PaymentId"] = WebAdminTestFactory.TestOrderPaymentId.ToString(),
                ["AmountMinor"] = "500",
                ["Currency"] = "EUR",
                ["Reason"] = refundReason
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Billing/CreatePayment?businessId=44444444-4444-4444-4444-444444444444",
            "/Billing/CreatePayment",
            $"/Billing/Payments?businessId=44444444-4444-4444-4444-444444444444&q={Uri.EscapeDataString(paymentReference)}",
            paymentProvider,
            new Dictionary<string, string>
            {
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["OrderId"] = string.Empty,
                ["InvoiceId"] = string.Empty,
                ["CustomerId"] = string.Empty,
                ["UserId"] = string.Empty,
                ["AmountMinor"] = "2599",
                ["Currency"] = "EUR",
                ["Status"] = "Pending",
                ["Provider"] = paymentProvider,
                ["ProviderTransactionRef"] = paymentReference,
                ["ProviderPaymentIntentRef"] = string.Empty,
                ["ProviderCheckoutSessionRef"] = string.Empty,
                ["PaidAtUtc"] = string.Empty
            });

        var assetAccountRedirect = await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Billing/CreateFinancialAccount?businessId=44444444-4444-4444-4444-444444444444",
            "/Billing/CreateFinancialAccount",
            $"/Billing/FinancialAccounts?businessId=44444444-4444-4444-4444-444444444444&q={Uri.EscapeDataString(assetAccountCode)}",
            assetAccountName,
            new Dictionary<string, string>
            {
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["Code"] = assetAccountCode,
                ["Type"] = "Asset",
                ["Name"] = assetAccountName
            });
        var assetAccountId = ExtractQueryGuid(assetAccountRedirect, "id");

        var revenueAccountRedirect = await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Billing/CreateFinancialAccount?businessId=44444444-4444-4444-4444-444444444444",
            "/Billing/CreateFinancialAccount",
            $"/Billing/FinancialAccounts?businessId=44444444-4444-4444-4444-444444444444&q={Uri.EscapeDataString(revenueAccountCode)}",
            revenueAccountName,
            new Dictionary<string, string>
            {
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["Code"] = revenueAccountCode,
                ["Type"] = "Revenue",
                ["Name"] = revenueAccountName
            });
        var revenueAccountId = ExtractQueryGuid(revenueAccountRedirect, "id");

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Billing/CreateExpense?businessId=44444444-4444-4444-4444-444444444444",
            "/Billing/CreateExpense",
            $"/Billing/Expenses?businessId=44444444-4444-4444-4444-444444444444&q={Uri.EscapeDataString(expenseDescription)}",
            expenseDescription,
            new Dictionary<string, string>
            {
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["SupplierId"] = string.Empty,
                ["ExpenseDateUtc"] = "2026-04-24",
                ["Category"] = "Smoke",
                ["AmountMinor"] = "3499",
                ["Description"] = expenseDescription
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Billing/CreateJournalEntry?businessId=44444444-4444-4444-4444-444444444444",
            "/Billing/CreateJournalEntry",
            $"/Billing/JournalEntries?businessId=44444444-4444-4444-4444-444444444444&q={Uri.EscapeDataString(journalDescription)}",
            journalDescription,
            new Dictionary<string, string>
            {
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["EntryDateUtc"] = "2026-04-24",
                ["Description"] = journalDescription,
                ["Lines[0].AccountId"] = assetAccountId.ToString(),
                ["Lines[0].DebitMinor"] = "1000",
                ["Lines[0].CreditMinor"] = "0",
                ["Lines[0].Memo"] = "Smoke debit",
                ["Lines[1].AccountId"] = revenueAccountId.ToString(),
                ["Lines[1].DebitMinor"] = "0",
                ["Lines[1].CreditMinor"] = "1000",
                ["Lines[1].Memo"] = "Smoke credit"
            });

        var warehouseFromRedirect = await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Inventory/CreateWarehouse?businessId=44444444-4444-4444-4444-444444444444",
            "/Inventory/CreateWarehouse",
            $"/Inventory/Warehouses?businessId=44444444-4444-4444-4444-444444444444&q={Uri.EscapeDataString(warehouseFromName)}",
            warehouseFromName,
            new Dictionary<string, string>
            {
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["Name"] = warehouseFromName,
                ["Location"] = "Berlin North",
                ["Description"] = "Smoke-created source warehouse.",
                ["IsDefault"] = "true"
            });
        var warehouseFromId = ExtractQueryGuid(warehouseFromRedirect, "id");

        var warehouseToRedirect = await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Inventory/CreateWarehouse?businessId=44444444-4444-4444-4444-444444444444",
            "/Inventory/CreateWarehouse",
            $"/Inventory/Warehouses?businessId=44444444-4444-4444-4444-444444444444&q={Uri.EscapeDataString(warehouseToName)}",
            warehouseToName,
            new Dictionary<string, string>
            {
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["Name"] = warehouseToName,
                ["Location"] = "Berlin South",
                ["Description"] = "Smoke-created destination warehouse.",
                ["IsDefault"] = "false"
            });
        var warehouseToId = ExtractQueryGuid(warehouseToRedirect, "id");

        var supplierRedirect = await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Inventory/CreateSupplier?businessId=44444444-4444-4444-4444-444444444444",
            "/Inventory/CreateSupplier",
            $"/Inventory/Suppliers?businessId=44444444-4444-4444-4444-444444444444&q={Uri.EscapeDataString(supplierEmail)}",
            supplierName,
            new Dictionary<string, string>
            {
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["Name"] = supplierName,
                ["Email"] = supplierEmail,
                ["Phone"] = "+49301234567",
                ["Address"] = "Supplier Street 1, Berlin",
                ["Notes"] = "Smoke-created supplier."
            });
        var supplierId = ExtractQueryGuid(supplierRedirect, "id");

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            $"/Inventory/CreateStockLevel?businessId=44444444-4444-4444-4444-444444444444&warehouseId={warehouseFromId}",
            "/Inventory/CreateStockLevel",
            $"/Inventory/StockLevels?businessId=44444444-4444-4444-4444-444444444444&warehouseId={warehouseFromId}&q=WEBADMIN-SMOKE-VARIANT",
            "WEBADMIN-SMOKE-VARIANT",
            new Dictionary<string, string>
            {
                ["WarehouseId"] = warehouseFromId.ToString(),
                ["ProductVariantId"] = WebAdminTestFactory.TestProductVariantId.ToString(),
                ["AvailableQuantity"] = "25",
                ["ReservedQuantity"] = "2",
                ["ReorderPoint"] = "5",
                ["ReorderQuantity"] = "20",
                ["InTransitQuantity"] = "3"
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            $"/Inventory/CreateStockTransfer?businessId=44444444-4444-4444-4444-444444444444&warehouseId={warehouseFromId}",
            "/Inventory/CreateStockTransfer",
            $"/Inventory/StockTransfers?businessId=44444444-4444-4444-4444-444444444444&warehouseId={warehouseFromId}&q={Uri.EscapeDataString(warehouseToName)}",
            warehouseToName,
            new Dictionary<string, string>
            {
                ["FromWarehouseId"] = warehouseFromId.ToString(),
                ["ToWarehouseId"] = warehouseToId.ToString(),
                ["Status"] = "Draft",
                ["Lines[0].ProductVariantId"] = WebAdminTestFactory.TestProductVariantId.ToString(),
                ["Lines[0].Quantity"] = "4"
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Inventory/CreatePurchaseOrder?businessId=44444444-4444-4444-4444-444444444444",
            "/Inventory/CreatePurchaseOrder",
            $"/Inventory/PurchaseOrders?businessId=44444444-4444-4444-4444-444444444444&q={Uri.EscapeDataString(purchaseOrderNumber)}",
            purchaseOrderNumber,
            new Dictionary<string, string>
            {
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["SupplierId"] = supplierId.ToString(),
                ["Status"] = "Draft",
                ["OrderNumber"] = purchaseOrderNumber,
                ["OrderedAtUtc"] = "2026-04-24T12:00:00",
                ["Lines[0].ProductVariantId"] = WebAdminTestFactory.TestProductVariantId.ToString(),
                ["Lines[0].Quantity"] = "6",
                ["Lines[0].UnitCostMinor"] = "700",
                ["Lines[0].TotalCostMinor"] = "4200"
            });

        var crmCustomerRedirect = await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Crm/CreateCustomer",
            "/Crm/CreateCustomer",
            $"/Crm/Customers?q={Uri.EscapeDataString(crmCustomerEmail)}",
            crmCustomerEmail,
            new Dictionary<string, string>
            {
                ["UserId"] = string.Empty,
                ["CompanyName"] = string.Empty,
                ["TaxProfileType"] = "Consumer",
                ["VatId"] = string.Empty,
                ["FirstName"] = crmCustomerFirstName,
                ["LastName"] = crmCustomerLastName,
                ["Email"] = crmCustomerEmail,
                ["Phone"] = "+493012345678",
                ["Notes"] = "Smoke-created CRM customer.",
                ["Addresses[0].Id"] = string.Empty,
                ["Addresses[0].AddressId"] = string.Empty,
                ["Addresses[0].Line1"] = "CRM Street 1",
                ["Addresses[0].Line2"] = string.Empty,
                ["Addresses[0].PostalCode"] = "10115",
                ["Addresses[0].City"] = "Berlin",
                ["Addresses[0].State"] = "Berlin",
                ["Addresses[0].Country"] = "DE",
                ["Addresses[0].IsDefaultBilling"] = "true",
                ["Addresses[0].IsDefaultShipping"] = "true"
            });
        var crmCustomerId = ExtractQueryGuid(crmCustomerRedirect, "id");

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Crm/CreateLead",
            "/Crm/CreateLead",
            $"/Crm/Leads?q={Uri.EscapeDataString(crmLeadEmail)}",
            crmLeadEmail,
            new Dictionary<string, string>
            {
                ["FirstName"] = crmLeadFirstName,
                ["LastName"] = crmLeadLastName,
                ["CompanyName"] = "Smoke Lead Company",
                ["Status"] = "New",
                ["Email"] = crmLeadEmail,
                ["Phone"] = "+493087654321",
                ["AssignedToUserId"] = string.Empty,
                ["CustomerId"] = string.Empty,
                ["Source"] = "Smoke",
                ["Notes"] = "Smoke-created CRM lead."
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Crm/CreateSegment",
            "/Crm/CreateSegment",
            $"/Crm/Segments?q={Uri.EscapeDataString(crmSegmentName)}",
            crmSegmentName,
            new Dictionary<string, string>
            {
                ["Name"] = crmSegmentName,
                ["Description"] = "Smoke-created CRM segment."
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            $"/Crm/CreateOpportunity?customerId={crmCustomerId}",
            "/Crm/CreateOpportunity",
            $"/Crm/Opportunities?q={Uri.EscapeDataString(crmOpportunityTitle)}",
            crmOpportunityTitle,
            new Dictionary<string, string>
            {
                ["CustomerId"] = crmCustomerId.ToString(),
                ["AssignedToUserId"] = string.Empty,
                ["Title"] = crmOpportunityTitle,
                ["Currency"] = "EUR",
                ["Stage"] = "Qualification",
                ["ExpectedCloseDateUtc"] = "2026-05-24",
                ["EstimatedValueMinor"] = "5000",
                ["Items[0].Id"] = string.Empty,
                ["Items[0].ProductVariantId"] = WebAdminTestFactory.TestProductVariantId.ToString(),
                ["Items[0].Quantity"] = "2",
                ["Items[0].UnitPriceMinor"] = "2500"
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Roles/Create",
            "/Roles/Create",
            $"/Roles?query={Uri.EscapeDataString(roleDisplayName)}",
            roleDisplayName,
            new Dictionary<string, string>
            {
                ["Key"] = roleKey,
                ["DisplayName"] = roleDisplayName,
                ["Description"] = "Smoke-created identity role."
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Permissions/Create",
            "/Permissions/Create",
            $"/Permissions?q={Uri.EscapeDataString(permissionKey)}",
            permissionDisplayName,
            new Dictionary<string, string>
            {
                ["Key"] = permissionKey,
                ["DisplayName"] = permissionDisplayName,
                ["Description"] = "Smoke-created identity permission."
            });

        await PostValidRolePermissionMutationAndAssertSelectedAsync(
            client,
            WebAdminTestFactory.TestRoleId,
            WebAdminTestFactory.TestPermissionId);

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Users/Create",
            "/Users/Create",
            $"/Users?q={Uri.EscapeDataString(userEmail)}",
            userEmail,
            new Dictionary<string, string>
            {
                ["Email"] = userEmail,
                ["Password"] = "SmokePass1",
                ["FirstName"] = "Smoke",
                ["LastName"] = $"User {suffix}",
                ["Locale"] = "de-DE",
                ["Currency"] = "EUR",
                ["Timezone"] = "Europe/Berlin",
                ["PhoneE164"] = "+493055501234"
            });

        await PostValidUserRolesMutationAndAssertSelectedAsync(
            client,
            WebAdminTestFactory.TestLifecycleUserId,
            WebAdminTestFactory.TestRoleId);

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            $"/Users/ChangeEmail/{WebAdminTestFactory.TestLifecycleUserId}?currentEmail={Uri.EscapeDataString(lifecycleCurrentEmail)}",
            "/Users/ChangeEmail",
            $"/Users?q={Uri.EscapeDataString(lifecycleNewEmail)}",
            lifecycleNewEmail,
            new Dictionary<string, string>
            {
                ["Id"] = WebAdminTestFactory.TestLifecycleUserId.ToString(),
                ["CurrentEmail"] = lifecycleCurrentEmail,
                ["NewEmail"] = lifecycleNewEmail,
                ["ReturnToIndex"] = "true",
                ["Query"] = lifecycleNewEmail,
                ["Filter"] = "All",
                ["Page"] = "1",
                ["PageSize"] = "20"
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            $"/Users/ChangePassword?id={WebAdminTestFactory.TestLifecycleUserId}&email={Uri.EscapeDataString(lifecycleNewEmail)}",
            "/Users/ChangePassword",
            $"/Users?q={Uri.EscapeDataString(lifecycleNewEmail)}",
            lifecycleNewEmail,
            new Dictionary<string, string>
            {
                ["Id"] = WebAdminTestFactory.TestLifecycleUserId.ToString(),
                ["Email"] = lifecycleNewEmail,
                ["NewPassword"] = "SmokePass2",
                ["ConfirmNewPassword"] = "SmokePass2",
                ["ReturnToIndex"] = "true",
                ["Query"] = lifecycleNewEmail,
                ["Filter"] = "All",
                ["Page"] = "1",
                ["PageSize"] = "20"
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Businesses/Create",
            "/Businesses/Create",
            $"/Businesses?query={Uri.EscapeDataString(businessName)}",
            businessName,
            new Dictionary<string, string>
            {
                ["Name"] = businessName,
                ["LegalName"] = $"{businessName} GmbH",
                ["Category"] = "Cafe",
                ["DefaultCurrency"] = "EUR",
                ["DefaultCulture"] = "de-DE",
                ["DefaultTimeZoneId"] = "Europe/Berlin",
                ["TaxId"] = $"DE{suffix}",
                ["WebsiteUrl"] = $"https://{businessName.Replace(" ", "-", StringComparison.Ordinal).ToLowerInvariant()}.example.test",
                ["ContactEmail"] = businessEmail,
                ["ContactPhoneE164"] = "+4915112345678",
                ["ShortDescription"] = "Smoke-created business.",
                ["BrandDisplayName"] = businessName,
                ["SupportEmail"] = businessEmail,
                ["CommunicationSenderName"] = businessName,
                ["CommunicationReplyToEmail"] = businessEmail,
                ["CustomerEmailNotificationsEnabled"] = "true",
                ["CustomerMarketingEmailsEnabled"] = "false",
                ["OperationalAlertEmailsEnabled"] = "true",
                ["IsActive"] = "false"
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Businesses/CreateLocation?businessId=44444444-4444-4444-4444-444444444444",
            "/Businesses/CreateLocation",
            $"/Businesses/Locations?businessId=44444444-4444-4444-4444-444444444444&query={Uri.EscapeDataString(locationName)}",
            locationName,
            new Dictionary<string, string>
            {
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["Page"] = "1",
                ["PageSize"] = "20",
                ["Query"] = string.Empty,
                ["Filter"] = "All",
                ["Name"] = locationName,
                ["AddressLine1"] = "Smoke Street 1",
                ["City"] = "Berlin",
                ["Region"] = "Berlin",
                ["CountryCode"] = "DE",
                ["PostalCode"] = "10115",
                ["OpeningHoursJson"] = "{\"mon\":\"09:00-17:00\"}",
                ["InternalNote"] = "Smoke-created location.",
                ["IsPrimary"] = "true"
            });
        await PostValidBusinessLocationEditAndDeleteLifecycleMutationAsync(client, $"Updated Location {suffix}");

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Businesses/CreateInvitation?businessId=44444444-4444-4444-4444-444444444444",
            "/Businesses/CreateInvitation",
            $"/Businesses/Invitations?businessId=44444444-4444-4444-4444-444444444444&query={Uri.EscapeDataString(invitationEmail)}",
            invitationEmail,
            new Dictionary<string, string>
            {
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["Page"] = "1",
                ["PageSize"] = "20",
                ["Query"] = string.Empty,
                ["Filter"] = "All",
                ["Email"] = invitationEmail,
                ["Role"] = "Owner",
                ["ExpiresInDays"] = "7",
                ["Note"] = "Smoke-created invitation."
            });
        await PostValidBusinessInvitationResendAndRevokeLifecycleMutationAsync(client);

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Businesses/CreateMember?businessId=44444444-4444-4444-4444-444444444444",
            "/Businesses/CreateMember",
            $"/Businesses/Members?businessId=44444444-4444-4444-4444-444444444444&query={Uri.EscapeDataString(memberEmail)}",
            memberEmail,
            new Dictionary<string, string>
            {
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["Page"] = "1",
                ["PageSize"] = "20",
                ["Query"] = string.Empty,
                ["Filter"] = "All",
                ["UserId"] = WebAdminTestFactory.TestMemberUserId.ToString(),
                ["Role"] = "Manager",
                ["IsActive"] = "true"
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            $"/Loyalty/CreateProgram?businessId={WebAdminTestFactory.TestLoyaltyProgramBusinessId}",
            "/Loyalty/CreateProgram",
            $"/Loyalty/Programs?businessId={WebAdminTestFactory.TestLoyaltyProgramBusinessId}",
            loyaltyProgramName,
            new Dictionary<string, string>
            {
                ["BusinessId"] = WebAdminTestFactory.TestLoyaltyProgramBusinessId.ToString(),
                ["Name"] = loyaltyProgramName,
                ["AccrualMode"] = "PerVisit",
                ["PointsPerCurrencyUnit"] = string.Empty,
                ["RulesJson"] = "{\"visits\":1}",
                ["IsActive"] = "true"
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Loyalty/CreateCampaign?businessId=44444444-4444-4444-4444-444444444444",
            "/Loyalty/CreateCampaign",
            "/Loyalty/Campaigns?businessId=44444444-4444-4444-4444-444444444444",
            loyaltyCampaignName,
            new Dictionary<string, string>
            {
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["Name"] = loyaltyCampaignName,
                ["Title"] = loyaltyCampaignTitle,
                ["Subtitle"] = "Smoke campaign subtitle",
                ["Body"] = "Smoke-created campaign.",
                ["MediaUrl"] = string.Empty,
                ["LandingUrl"] = "/loyalty",
                ["Channels"] = "1",
                ["StartsAtUtc"] = string.Empty,
                ["EndsAtUtc"] = string.Empty,
                ["TargetingJson"] = "{}",
                ["PayloadJson"] = "{}"
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Loyalty/CreateRewardTier?loyaltyProgramId=55555555-5555-5555-5555-555555555555",
            "/Loyalty/CreateRewardTier",
            "/Loyalty/RewardTiers?loyaltyProgramId=55555555-5555-5555-5555-555555555555",
            rewardTierDescription,
            new Dictionary<string, string>
            {
                ["LoyaltyProgramId"] = "55555555-5555-5555-5555-555555555555",
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["ProgramName"] = "WebAdmin Smoke Loyalty",
                ["PointsRequired"] = "100",
                ["RewardType"] = "FreeItem",
                ["RewardValue"] = string.Empty,
                ["Description"] = rewardTierDescription,
                ["MetadataJson"] = "{}",
                ["AllowSelfRedemption"] = "true"
            });

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Loyalty/CreateAccount?businessId=44444444-4444-4444-4444-444444444444",
            "/Loyalty/CreateAccount",
            $"/Loyalty/Accounts?businessId=44444444-4444-4444-4444-444444444444&q={Uri.EscapeDataString(memberEmail)}",
            memberEmail,
            new Dictionary<string, string>
            {
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["UserId"] = WebAdminTestFactory.TestMemberUserId.ToString()
            });

        await PostValidBusinessCommunicationChannelTestMutationAndAssertQueuedAsync(
            client,
            "/BusinessCommunications/SendTestEmail",
            "/BusinessCommunications/EmailAudits?flowKey=AdminCommunicationTest",
            "communication-smoke@example.test",
            "Smoke email transport");

        await PostValidBusinessCommunicationChannelTestMutationAndAssertQueuedAsync(
            client,
            "/BusinessCommunications/SendTestSms",
            "/BusinessCommunications/ChannelAudits?adminTestOnly=true&channel=SMS",
            "+4915700000001",
            "Smoke SMS");

        await PostValidBusinessCommunicationChannelTestMutationAndAssertQueuedAsync(
            client,
            "/BusinessCommunications/SendTestWhatsApp",
            "/BusinessCommunications/ChannelAudits?adminTestOnly=true&channel=WhatsApp",
            "+4915700000003",
            "Smoke WhatsApp");

        await PostValidDhlLabelGenerationMutationAndAssertQueuedAsync(client);
        await AssertReturnedShipmentQueuesRenderCarrierEventAsync(client);

        await PostValidMobileDeviceMutationAndAssertFilteredAsync(
            client,
            WebAdminTestFactory.TestClearPushDeviceId,
            "webadmin-smoke-clear-push",
            "/MobileOperations/ClearPushToken",
            "missing-push");

        await PostValidMobileDeviceMutationAndAssertFilteredAsync(
            client,
            WebAdminTestFactory.TestDeactivateDeviceId,
            "webadmin-smoke-deactivate",
            "/MobileOperations/DeactivateDevice",
            "notifications-disabled");
    }

    [Fact]
    public async Task AuthenticatedAdminFragmentWithoutRequiredPermission_ShouldBeForbidden()
    {
        using var client = _factory.CreateAuthenticatedNoRedirectClient(allowPermissions: false);

        using var response = await client.GetAsync("/Home/AlertsFragment", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("frame-ancestors 'none'");
        response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions).Should().BeTrue();
        contentTypeOptions!.Single().Should().Be("nosniff");
    }

    [Theory]
    [InlineData("/lib/htmx/htmx.min.js", "text/javascript", "version:\"2.0.4\"")]
    [InlineData("/lib/vendor-manifest.json", "application/json", "\"bootstrap\"")]
    [InlineData("/js/admin-core.js", "text/javascript", "window.darwinAdmin")]
    public async Task StaticAdminAssets_ShouldBeServedLocallyWithSecurityHeaders(string path, string contentType, string expectedContent)
    {
        using var client = _factory.CreateNoRedirectClient();

        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be(contentType);
        body.Should().Contain(expectedContent);
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("default-src 'self'");
        response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions).Should().BeTrue();
        contentTypeOptions!.Single().Should().Be("nosniff");
    }

    [Fact]
    public async Task NonLocalHttpsRequest_ShouldEmitHstsHeader()
    {
        using var client = _factory.CreateNoRedirectClient(new Uri("https://admin.example.test"));

        using var response = await client.GetAsync("/account/login", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Strict-Transport-Security", out var hstsValues).Should().BeTrue();
        hstsValues!.Single().Should().Contain("max-age=");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("frame-ancestors 'none'");
    }

    [Theory]
    [InlineData("/does-not-exist")]
    [InlineData("/lib/does-not-exist.js")]
    [InlineData("/js/does-not-exist.js")]
    public async Task NotFoundResponses_ShouldStillEmitSecurityHeaders(string path)
    {
        using var client = _factory.CreateNoRedirectClient();

        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("default-src 'self'");
        cspValues!.Single().Should().Contain("frame-ancestors 'none'");
        response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions).Should().BeTrue();
        contentTypeOptions!.Single().Should().Be("nosniff");
        response.Headers.TryGetValues("Referrer-Policy", out var referrerPolicy).Should().BeTrue();
        referrerPolicy!.Single().Should().Be("strict-origin-when-cross-origin");
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        const string marker = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
        var markerIndex = html.IndexOf(marker, StringComparison.Ordinal);
        markerIndex.Should().BeGreaterThanOrEqualTo(0, "the page should render a hidden anti-forgery token input");
        var tokenStart = markerIndex + marker.Length;
        var tokenEnd = html.IndexOf('"', tokenStart);
        tokenEnd.Should().BeGreaterThan(tokenStart);
        return html[tokenStart..tokenEnd];
    }

    private static string ExtractHiddenInputValue(string html, string name)
    {
        var nameMarker = $"name=\"{name}\"";
        var nameIndex = html.IndexOf(nameMarker, StringComparison.Ordinal);
        nameIndex.Should().BeGreaterThanOrEqualTo(0, "the page should render a hidden input named {0}", name);

        var inputStart = html.LastIndexOf("<input", nameIndex, StringComparison.OrdinalIgnoreCase);
        inputStart.Should().BeGreaterThanOrEqualTo(0);
        var inputEnd = html.IndexOf('>', nameIndex);
        inputEnd.Should().BeGreaterThan(nameIndex);
        var input = html[inputStart..inputEnd];

        const string valueMarker = "value=\"";
        var valueIndex = input.IndexOf(valueMarker, StringComparison.Ordinal);
        valueIndex.Should().BeGreaterThanOrEqualTo(0, "the hidden input named {0} should have a value", name);
        var valueStart = valueIndex + valueMarker.Length;
        var valueEnd = input.IndexOf('"', valueStart);
        valueEnd.Should().BeGreaterThan(valueStart);
        return input[valueStart..valueEnd];
    }

    private static async Task<HttpResponseMessage> SendHtmxGetAsync(HttpClient client, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("HX-Request", "true");
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<HttpResponseMessage> SendHtmxPostAsync(
        HttpClient client,
        string path,
        Dictionary<string, string> form)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.TryAddWithoutValidation("HX-Request", "true");
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task PostValidSiteSettingsMutationAndAssertUpdatedAsync(
        HttpClient client,
        string title,
        string emailSubjectTemplate)
    {
        const string editorPath = "/SiteSettings/Edit?fragment=site-settings-communications-policy";
        using var tokenResponse = await SendHtmxGetAsync(client, editorPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        var form = BuildValidSiteSettingsForm(
            ExtractHiddenInputValue(tokenHtml, "Id"),
            ExtractHiddenInputValue(tokenHtml, "RowVersion"),
            title,
            emailSubjectTemplate);
        form["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml);

        using var postResponse = await SendHtmxPostAsync(client, editorPath, form);
        var postHtml = await postResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var postPreview = postHtml.Length > 800 ? postHtml[..800] : postHtml;

        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        postResponse.Headers.TryGetValues("HX-Redirect", out var redirectValues)
            .Should().BeTrue("valid SiteSettings persistence should redirect; response preview: {0}", postPreview);
        redirectValues!.Single().Should().Contain("/SiteSettings/Edit");
        postResponse.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");

        using var updatedResponse = await SendHtmxGetAsync(client, editorPath);
        var updatedHtml = await updatedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        updatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedHtml.Should().Contain(title);
        updatedHtml.Should().Contain(emailSubjectTemplate);
        updatedHtml.Should().Contain("communication-smoke@example.test");
        updatedHtml.Should().Contain("4915700000001");
        updatedHtml.Should().Contain("4915700000003");
    }

    private static Dictionary<string, string> BuildValidSiteSettingsForm(
        string id,
        string rowVersion,
        string title,
        string emailSubjectTemplate)
    {
        return new Dictionary<string, string>
        {
            ["Id"] = id,
            ["RowVersion"] = rowVersion,
            ["fragment"] = "site-settings-communications-policy",
            ["Title"] = title,
            ["LogoUrl"] = string.Empty,
            ["ContactEmail"] = "admin-smoke@example.test",
            ["DefaultCulture"] = "de-DE",
            ["SupportedCulturesCsv"] = "de-DE,en-US",
            ["DefaultCountry"] = "DE",
            ["DefaultCurrency"] = "EUR",
            ["TimeZone"] = "Europe/Berlin",
            ["HomeSlug"] = "home",
            ["AdminTextOverridesJson"] = "{\"de-DE\":{\"Smoke\":\"Rauch\"},\"en-US\":{\"Smoke\":\"Smoke\"}}",
            ["DateFormat"] = "yyyy-MM-dd",
            ["TimeFormat"] = "HH:mm",
            ["JwtEnabled"] = "true",
            ["JwtIssuer"] = "Darwin",
            ["JwtAudience"] = "Darwin.PublicApi",
            ["JwtClockSkewSeconds"] = "60",
            ["JwtAccessTokenMinutes"] = "15",
            ["JwtRefreshTokenDays"] = "30",
            ["JwtEmitScopes"] = "true",
            ["JwtSingleDeviceOnly"] = "false",
            ["JwtRequireDeviceBinding"] = "true",
            ["JwtSigningKey"] = "01234567890123456789012345678901",
            ["JwtPreviousSigningKey"] = string.Empty,
            ["MobileQrTokenRefreshSeconds"] = "30",
            ["MobileMaxOutboxItems"] = "200",
            ["BusinessManagementWebsiteUrl"] = "https://business.example.test",
            ["AccountDeletionUrl"] = "https://business.example.test/account/delete",
            ["ImpressumUrl"] = "https://business.example.test/impressum",
            ["PrivacyPolicyUrl"] = "https://business.example.test/privacy",
            ["BusinessTermsUrl"] = "https://business.example.test/terms",
            ["StripeEnabled"] = "false",
            ["StripeMerchantDisplayName"] = string.Empty,
            ["StripePublishableKey"] = string.Empty,
            ["StripeSecretKey"] = string.Empty,
            ["StripeWebhookSecret"] = string.Empty,
            ["VatEnabled"] = "true",
            ["PricesIncludeVat"] = "true",
            ["AllowReverseCharge"] = "false",
            ["DefaultVatRatePercent"] = "19",
            ["InvoiceIssuerLegalName"] = "Darwin Smoke GmbH",
            ["InvoiceIssuerTaxId"] = "DE123456789",
            ["InvoiceIssuerAddressLine1"] = "Smoke Street 1",
            ["InvoiceIssuerPostalCode"] = "10115",
            ["InvoiceIssuerCity"] = "Berlin",
            ["InvoiceIssuerCountry"] = "DE",
            ["DhlEnabled"] = "true",
            ["DhlEnvironment"] = "Sandbox",
            ["DhlApiBaseUrl"] = "https://dhl.example.test",
            ["DhlAccountNumber"] = "DHL-SMOKE-ACCOUNT",
            ["DhlApiKey"] = "dhl-api-key",
            ["DhlApiSecret"] = "dhl-api-secret",
            ["DhlShipperName"] = "Darwin Smoke Shipping",
            ["DhlShipperEmail"] = "shipper@example.test",
            ["DhlShipperPhoneE164"] = "+4915700000005",
            ["DhlShipperStreet"] = "Smoke Street 1",
            ["DhlShipperPostalCode"] = "10115",
            ["DhlShipperCity"] = "Berlin",
            ["DhlShipperCountry"] = "DE",
            ["ShipmentAttentionDelayHours"] = "24",
            ["ShipmentTrackingGraceHours"] = "12",
            ["SoftDeleteCleanupEnabled"] = "true",
            ["SoftDeleteRetentionDays"] = "90",
            ["SoftDeleteCleanupBatchSize"] = "500",
            ["MeasurementSystem"] = "Metric",
            ["DisplayWeightUnit"] = "kg",
            ["DisplayLengthUnit"] = "cm",
            ["MeasurementSettingsJson"] = "{}",
            ["NumberFormattingOverridesJson"] = "{\"decimalSeparator\":\",\",\"thousandsSeparator\":\".\"}",
            ["EnableCanonical"] = "true",
            ["HreflangEnabled"] = "true",
            ["SeoTitleTemplate"] = "{title} | Darwin",
            ["SeoMetaDescriptionTemplate"] = "Smoke settings description",
            ["OpenGraphDefaultsJson"] = "{}",
            ["GoogleAnalyticsId"] = string.Empty,
            ["GoogleTagManagerId"] = string.Empty,
            ["GoogleSearchConsoleVerification"] = string.Empty,
            ["FeatureFlagsJson"] = "{\"smoke\":true}",
            ["WhatsAppEnabled"] = "true",
            ["WhatsAppBusinessPhoneId"] = "wa-phone-smoke",
            ["WhatsAppAccessToken"] = "wa-token-smoke",
            ["WhatsAppFromPhoneE164"] = "+4915700000002",
            ["WhatsAppAdminRecipientsCsv"] = "+4915700000003",
            ["WebAuthnRelyingPartyId"] = "localhost",
            ["WebAuthnRelyingPartyName"] = "Darwin",
            ["WebAuthnAllowedOriginsCsv"] = "https://localhost",
            ["WebAuthnRequireUserVerification"] = "false",
            ["SmtpEnabled"] = "true",
            ["SmtpHost"] = "smtp.example.test",
            ["SmtpPort"] = "587",
            ["SmtpEnableSsl"] = "true",
            ["SmtpUsername"] = string.Empty,
            ["SmtpPassword"] = string.Empty,
            ["SmtpFromAddress"] = "noreply@example.test",
            ["SmtpFromDisplayName"] = "Darwin Smoke",
            ["SmsEnabled"] = "true",
            ["SmsProvider"] = "SmokeSms",
            ["SmsFromPhoneE164"] = "+4915700000000",
            ["SmsApiKey"] = "sms-key",
            ["SmsApiSecret"] = "sms-secret",
            ["SmsExtraSettingsJson"] = "{}",
            ["AdminAlertEmailsCsv"] = "alerts@example.test",
            ["AdminAlertSmsRecipientsCsv"] = "+4915700000004",
            ["TransactionalEmailSubjectPrefix"] = "[Smoke]",
            ["CommunicationTestInboxEmail"] = "communication-smoke@example.test",
            ["CommunicationTestSmsRecipientE164"] = "+4915700000001",
            ["CommunicationTestWhatsAppRecipientE164"] = "+4915700000003",
            ["PhoneVerificationPreferredChannel"] = "Sms",
            ["PhoneVerificationAllowFallback"] = "true",
            ["CommunicationTestEmailSubjectTemplate"] = emailSubjectTemplate,
            ["CommunicationTestEmailBodyTemplate"] = "<p>Smoke email body {test_target}</p>",
            ["CommunicationTestSmsTemplate"] = "Smoke SMS {test_target}",
            ["CommunicationTestWhatsAppTemplate"] = "Smoke WhatsApp {test_target}",
            ["BusinessInvitationEmailSubjectTemplate"] = "Smoke invite {business_name}",
            ["AccountActivationEmailSubjectTemplate"] = "Smoke activation {email}",
            ["BusinessInvitationEmailBodyTemplate"] = "<p>Smoke invitation {business_name}</p>",
            ["AccountActivationEmailBodyTemplate"] = "<p>Smoke activation {email}</p>",
            ["PasswordResetEmailSubjectTemplate"] = "Smoke reset {email}",
            ["PasswordResetEmailBodyTemplate"] = "<p>Smoke reset {email}</p>",
            ["PhoneVerificationSmsTemplate"] = "Smoke phone SMS {token}",
            ["PhoneVerificationWhatsAppTemplate"] = "Smoke phone WhatsApp {token}"
        };
    }

    private static async Task PostValidBusinessCommunicationChannelTestMutationAndAssertQueuedAsync(
        HttpClient client,
        string postPath,
        string listPath,
        string expectedRecipient,
        string expectedMessagePreview)
    {
        using var tokenResponse = await SendHtmxGetAsync(client, "/BusinessCommunications");
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var postResponse = await SendHtmxPostAsync(client, postPath, new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml)
        });
        var postHtml = await postResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var postPreview = postHtml.Length > 600 ? postHtml[..600] : postHtml;

        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        postResponse.Headers.TryGetValues("HX-Redirect", out var redirectValues)
            .Should().BeTrue("a successful communication test mutation should redirect; response preview: {0}", postPreview);
        redirectValues!.Single().Should().Contain("/BusinessCommunications");
        postResponse.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");

        using var listResponse = await SendHtmxGetAsync(client, listPath);
        var listHtml = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        listHtml.Should().Contain(expectedRecipient.TrimStart('+'));
        listHtml.Should().Contain(expectedMessagePreview);
        listHtml.Should().Contain("AdminCommunicationTest");
        listHtml.Should().Contain("Wartet auf Worker");
    }

    private static async Task PostValidDhlLabelGenerationMutationAndAssertQueuedAsync(HttpClient client)
    {
        const string shipmentQuery = "DHL-SMOKE-LABEL";
        const string queuePath = "/Orders/ShipmentsQueue?filter=Dhl&query=DHL-SMOKE-LABEL";
        using var tokenResponse = await SendHtmxGetAsync(client, queuePath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        tokenHtml.Should().Contain(shipmentQuery);
        tokenHtml.Should().Contain("DHL-SMOKE-LABEL-REF");
        tokenHtml.Should().Contain($"name=\"shipmentId\" value=\"{WebAdminTestFactory.TestDhlLabelShipmentId}\"");

        using var postResponse = await SendHtmxPostAsync(client, "/Orders/GenerateDhlLabel", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
            ["shipmentId"] = WebAdminTestFactory.TestDhlLabelShipmentId.ToString(),
            ["orderId"] = WebAdminTestFactory.TestOrderId.ToString(),
            ["rowVersion"] = ExtractHiddenInputValue(tokenHtml, "rowVersion"),
            ["returnToQueue"] = "true",
            ["filter"] = "Dhl",
            ["query"] = shipmentQuery,
            ["page"] = "1",
            ["pageSize"] = "20"
        });
        var postHtml = await postResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var postPreview = postHtml.Length > 600 ? postHtml[..600] : postHtml;

        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        postResponse.Headers.TryGetValues("HX-Redirect", out var redirectValues)
            .Should().BeTrue("valid DHL label generation should redirect; response preview: {0}", postPreview);
        redirectValues!.Single().Should().Contain("/Orders/ShipmentsQueue");
        postResponse.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");

        using var queuedResponse = await SendHtmxGetAsync(client, queuePath);
        var queuedHtml = await queuedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        queuedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        queuedHtml.Should().Contain(shipmentQuery);
        queuedHtml.Should().Contain("DHL-SMOKE-LABEL-REF");
        queuedHtml.Should().NotContain($"name=\"shipmentId\" value=\"{WebAdminTestFactory.TestDhlLabelShipmentId}\"");
    }

    private static async Task AssertReturnedShipmentQueuesRenderCarrierEventAsync(HttpClient client)
    {
        const string returnedQuery = "DHL-SMOKE-RETURN";
        foreach (var filter in new[] { "All", "FollowUp", "CarrierReview" })
        {
            using var response = await SendHtmxGetAsync(
                client,
                $"/Orders/ReturnsQueue?filter={filter}&query={returnedQuery}");
            var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            html.Should().Contain(returnedQuery);
            html.Should().Contain("DHLRETURN123");
            html.Should().Contain("RETURNED_TO_SENDER");
            html.Should().Contain("/Orders/AddRefund");
        }
    }

    private static async Task PostValidMediaUploadMutationAndAssertListedAsync(
        HttpClient client,
        string fileName,
        string title)
    {
        using var tokenResponse = await SendHtmxGetAsync(client, "/Media/Create");
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(ExtractAntiForgeryToken(tokenHtml)), "__RequestVerificationToken");
        content.Add(new StringContent($"Alt text for {title}"), "Alt");
        content.Add(new StringContent(title), "Title");
        content.Add(new StringContent("LibraryAsset"), "Role");

        using var imageContent = new ByteArrayContent(CreateTinyPngBytes());
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "File", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/Media/Create")
        {
            Content = content
        };
        request.Headers.TryAddWithoutValidation("HX-Request", "true");

        using var postResponse = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var postHtml = await postResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var postPreview = postHtml.Length > 600 ? postHtml[..600] : postHtml;

        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        postResponse.Headers.TryGetValues("HX-Redirect", out var redirectValues)
            .Should().BeTrue("valid media upload should redirect; response preview: {0}", postPreview);
        redirectValues!.Single().Should().Contain("/Media");
        postResponse.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");

        using var listResponse = await SendHtmxGetAsync(client, $"/Media?query={Uri.EscapeDataString(fileName)}");
        var listHtml = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        listHtml.Should().Contain(fileName);
        listHtml.Should().Contain(title);

        DeleteUploadedSmokeFileIfPresent(listHtml);
    }

    private static async Task PostValidMediaEditAndDeleteLifecycleMutationAsync(
        HttpClient client,
        string updatedTitle)
    {
        var editPath = $"/Media/Edit?id={WebAdminTestFactory.TestMediaAssetId}";
        using var tokenResponse = await SendHtmxGetAsync(client, editPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        tokenHtml.Should().Contain("webadmin-smoke-seeded.png");
        using var editResponse = await SendHtmxPostAsync(client, "/Media/Edit", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
            ["Id"] = WebAdminTestFactory.TestMediaAssetId.ToString(),
            ["RowVersion"] = ExtractHiddenInputValue(tokenHtml, "RowVersion"),
            ["Alt"] = $"Alt for {updatedTitle}",
            ["Title"] = updatedTitle,
            ["Role"] = "LibraryAssetReviewed"
        });
        var editHtml = await editResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var editPreview = editHtml.Length > 600 ? editHtml[..600] : editHtml;

        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        editResponse.Headers.TryGetValues("HX-Redirect", out var editRedirectValues)
            .Should().BeTrue("valid media edit should redirect; response preview: {0}", editPreview);
        editRedirectValues!.Single().Should().Contain("/Media/Edit");
        editResponse.Headers.TryGetValues("Content-Security-Policy", out var editCspValues).Should().BeTrue();
        editCspValues!.Single().Should().Contain("form-action 'self'");

        using var updatedListResponse = await SendHtmxGetAsync(client, $"/Media?query={Uri.EscapeDataString(updatedTitle)}");
        var updatedListHtml = await updatedListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        updatedListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedListHtml.Should().Contain(updatedTitle);
        updatedListHtml.Should().Contain("LibraryAssetReviewed");

        using var deleteResponse = await SendHtmxPostAsync(client, "/Media/Delete", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
            ["id"] = WebAdminTestFactory.TestMediaAssetId.ToString(),
            ["rowVersion"] = ExtractHiddenInputValue(tokenHtml, "RowVersion")
        });
        var deleteHtml = await deleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var deletePreview = deleteHtml.Length > 600 ? deleteHtml[..600] : deleteHtml;

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        deleteResponse.Headers.TryGetValues("HX-Redirect", out var deleteRedirectValues)
            .Should().BeTrue("valid media delete should redirect; response preview: {0}", deletePreview);
        deleteRedirectValues!.Single().Should().Contain("/Media");
        deleteResponse.Headers.TryGetValues("Content-Security-Policy", out var deleteCspValues).Should().BeTrue();
        deleteCspValues!.Single().Should().Contain("form-action 'self'");

        using var deletedListResponse = await SendHtmxGetAsync(client, "/Media?query=webadmin-smoke-seeded.png");
        var deletedListHtml = await deletedListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        deletedListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        deletedListHtml.Should().NotContain(WebAdminTestFactory.TestMediaAssetId.ToString());
        deletedListHtml.Should().NotContain(updatedTitle);
    }

    private static async Task PostValidBillingPlanEditMutationAndAssertInactiveAsync(
        HttpClient client,
        Guid planId,
        string planCode,
        string updatedName)
    {
        var editPath = $"/Billing/EditPlan?id={planId}";
        using var tokenResponse = await SendHtmxGetAsync(client, editPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        tokenHtml.Should().Contain(planCode);
        using var editResponse = await SendHtmxPostAsync(client, "/Billing/EditPlan", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
            ["Id"] = planId.ToString(),
            ["RowVersion"] = ExtractHiddenInputValue(tokenHtml, "RowVersion"),
            ["Code"] = planCode,
            ["Name"] = updatedName,
            ["Description"] = "Smoke-updated inactive billing plan.",
            ["PriceMinor"] = "1290",
            ["Currency"] = "EUR",
            ["Interval"] = "Month",
            ["IntervalCount"] = "1",
            ["TrialDays"] = "14",
            ["IsActive"] = "false",
            ["FeaturesJson"] = "{\"smoke\":true,\"updated\":true}"
        });
        var editHtml = await editResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var editPreview = editHtml.Length > 600 ? editHtml[..600] : editHtml;

        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        editResponse.Headers.TryGetValues("HX-Redirect", out var redirectValues)
            .Should().BeTrue("valid BillingPlan edit should redirect; response preview: {0}", editPreview);
        redirectValues!.Single().Should().Contain("/Billing/EditPlan");
        editResponse.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");

        using var listResponse = await SendHtmxGetAsync(
            client,
            $"/Billing/Plans?queue=Inactive&q={Uri.EscapeDataString(planCode)}");
        var listHtml = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        listHtml.Should().Contain(planCode);
        listHtml.Should().Contain(updatedName);
        listHtml.Should().Contain("Smoke-updated inactive billing plan.");
        listHtml.Should().Contain("14");
    }

    private static async Task PostValidBusinessLocationEditAndDeleteLifecycleMutationAsync(
        HttpClient client,
        string updatedName)
    {
        var editPath = $"/Businesses/EditLocation?id={WebAdminTestFactory.TestBusinessLocationLifecycleId}";
        using var tokenResponse = await SendHtmxGetAsync(client, editPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        tokenHtml.Should().Contain("Seeded WebAdmin Business Location Lifecycle");
        var rowVersion = ExtractHiddenInputValue(tokenHtml, "RowVersion");

        using var editResponse = await SendHtmxPostAsync(client, "/Businesses/EditLocation", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
            ["Id"] = WebAdminTestFactory.TestBusinessLocationLifecycleId.ToString(),
            ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
            ["Page"] = "1",
            ["PageSize"] = "20",
            ["Query"] = string.Empty,
            ["Filter"] = "All",
            ["RowVersion"] = rowVersion,
            ["Name"] = updatedName,
            ["AddressLine1"] = "Updated Smoke Street 2",
            ["AddressLine2"] = "Suite 4",
            ["City"] = "Hamburg",
            ["Region"] = "Hamburg",
            ["CountryCode"] = "DE",
            ["PostalCode"] = "20095",
            ["Latitude"] = "53,5511",
            ["Longitude"] = "9,9937",
            ["AltitudeMeters"] = "8",
            ["OpeningHoursJson"] = "{\"wed\":\"08:00-18:00\"}",
            ["InternalNote"] = "Smoke-updated business location.",
            ["IsPrimary"] = "false"
        });
        var editHtml = await editResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var editPreview = editHtml.Length > 600 ? editHtml[..600] : editHtml;

        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        editResponse.Headers.TryGetValues("HX-Redirect", out var editRedirectValues)
            .Should().BeTrue("valid BusinessLocation edit should redirect; response preview: {0}", editPreview);
        editRedirectValues!.Single().Should().Contain("/Businesses/Locations");
        editResponse.Headers.TryGetValues("Content-Security-Policy", out var editCspValues).Should().BeTrue();
        editCspValues!.Single().Should().Contain("form-action 'self'");

        using var listResponse = await SendHtmxGetAsync(
            client,
            $"/Businesses/Locations?businessId=44444444-4444-4444-4444-444444444444&query={Uri.EscapeDataString(updatedName)}");
        var listHtml = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        listHtml.Should().Contain(updatedName);
        listHtml.Should().Contain("Hamburg");
        listHtml.Should().Contain(WebAdminTestFactory.TestBusinessLocationLifecycleId.ToString());

        using var deleteResponse = await SendHtmxPostAsync(client, "/Businesses/DeleteLocation", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(listHtml),
            ["id"] = WebAdminTestFactory.TestBusinessLocationLifecycleId.ToString(),
            ["userId"] = "44444444-4444-4444-4444-444444444444",
            ["rowVersion"] = rowVersion
        });
        var deleteHtml = await deleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var deletePreview = deleteHtml.Length > 600 ? deleteHtml[..600] : deleteHtml;

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        deleteResponse.Headers.TryGetValues("HX-Redirect", out var deleteRedirectValues)
            .Should().BeTrue("valid BusinessLocation delete should redirect; response preview: {0}", deletePreview);
        deleteRedirectValues!.Single().Should().Contain("/Businesses/Locations");
        deleteResponse.Headers.TryGetValues("Content-Security-Policy", out var deleteCspValues).Should().BeTrue();
        deleteCspValues!.Single().Should().Contain("form-action 'self'");

        using var deletedListResponse = await SendHtmxGetAsync(
            client,
            $"/Businesses/Locations?businessId=44444444-4444-4444-4444-444444444444&query={Uri.EscapeDataString(updatedName)}");
        var deletedListHtml = await deletedListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        deletedListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        deletedListHtml.Should().NotContain(WebAdminTestFactory.TestBusinessLocationLifecycleId.ToString());
    }

    private static async Task PostValidBusinessInvitationResendAndRevokeLifecycleMutationAsync(HttpClient client)
    {
        const string invitationEmail = "webadmin-invitation-lifecycle@example.test";
        var listPath = $"/Businesses/Invitations?businessId=44444444-4444-4444-4444-444444444444&query={Uri.EscapeDataString(invitationEmail)}";
        using var tokenResponse = await SendHtmxGetAsync(client, listPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        tokenHtml.Should().Contain(invitationEmail);
        tokenHtml.Should().Contain(WebAdminTestFactory.TestBusinessInvitationLifecycleId.ToString());

        using var resendResponse = await SendHtmxPostAsync(client, "/Businesses/ResendInvitation", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
            ["id"] = WebAdminTestFactory.TestBusinessInvitationLifecycleId.ToString(),
            ["businessId"] = "44444444-4444-4444-4444-444444444444",
            ["rowVersion"] = ExtractHiddenInputValue(tokenHtml, "rowVersion"),
            ["page"] = "1",
            ["pageSize"] = "20",
            ["query"] = invitationEmail,
            ["filter"] = "All"
        });
        var resendHtml = await resendResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        resendResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        resendResponse.Headers.TryGetValues("HX-Redirect", out var resendRedirectValues)
            .Should().BeTrue("valid BusinessInvitation resend should redirect; response preview: {0}", resendHtml);
        resendRedirectValues!.Single().Should().Contain("/Businesses/Invitations");
        resendResponse.Headers.TryGetValues("Content-Security-Policy", out var resendCspValues).Should().BeTrue();
        resendCspValues!.Single().Should().Contain("form-action 'self'");

        using var resentListResponse = await SendHtmxGetAsync(client, listPath);
        resentListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var resentListHtml = await resentListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        resentListHtml.Should().Contain(invitationEmail);
        resentListHtml.Should().Contain(WebAdminTestFactory.TestBusinessInvitationLifecycleId.ToString());

        using var revokeResponse = await SendHtmxPostAsync(client, "/Businesses/RevokeInvitation", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(resentListHtml),
            ["id"] = WebAdminTestFactory.TestBusinessInvitationLifecycleId.ToString(),
            ["businessId"] = "44444444-4444-4444-4444-444444444444",
            ["rowVersion"] = ExtractHiddenInputValue(resentListHtml, "rowVersion"),
            ["page"] = "1",
            ["pageSize"] = "20",
            ["query"] = invitationEmail,
            ["filter"] = "All"
        });
        var revokeHtml = await revokeResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        revokeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        revokeResponse.Headers.TryGetValues("HX-Redirect", out var revokeRedirectValues)
            .Should().BeTrue("valid BusinessInvitation revoke should redirect; response preview: {0}", revokeHtml);
        revokeRedirectValues!.Single().Should().Contain("/Businesses/Invitations");
        revokeResponse.Headers.TryGetValues("Content-Security-Policy", out var revokeCspValues).Should().BeTrue();
        revokeCspValues!.Single().Should().Contain("form-action 'self'");

        using var revokedListResponse = await SendHtmxGetAsync(
            client,
            $"/Businesses/Invitations?businessId=44444444-4444-4444-4444-444444444444&filter=Revoked&query={Uri.EscapeDataString(invitationEmail)}");
        var revokedListHtml = await revokedListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        revokedListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        revokedListHtml.Should().Contain(invitationEmail);
        revokedListHtml.Should().Contain("filter=Revoked");
        revokedListHtml.Should().Contain("In WebAdmin widerrufen.");
    }

    private static async Task PostValidBrandEditAndDeleteLifecycleMutationAsync(
        HttpClient client,
        string originalSlug,
        string updatedName)
    {
        const string updatedSlug = "webadmin-smoke-brand-lifecycle-updated";
        var editPath = $"/Brands/Edit?id={WebAdminTestFactory.TestBrandLifecycleId}";
        using var tokenResponse = await SendHtmxGetAsync(client, editPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        tokenHtml.Should().Contain(originalSlug);
        tokenHtml.Should().Contain("Seeded WebAdmin Brand Lifecycle");
        var rowVersion = ExtractHiddenInputValue(tokenHtml, "RowVersion");

        using var editResponse = await SendHtmxPostAsync(client, "/Brands/Edit", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
            ["Id"] = WebAdminTestFactory.TestBrandLifecycleId.ToString(),
            ["RowVersion"] = rowVersion,
            ["Slug"] = updatedSlug,
            ["LogoMediaId"] = string.Empty,
            ["Translations[0].Culture"] = "de-DE",
            ["Translations[0].Name"] = updatedName,
            ["Translations[0].DescriptionHtml"] = "<p>Updated brand lifecycle.</p>"
        });
        var editHtml = await editResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var editPreview = editHtml.Length > 600 ? editHtml[..600] : editHtml;

        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        editResponse.Headers.TryGetValues("HX-Redirect", out var editRedirectValues)
            .Should().BeTrue("valid Brand edit should redirect; response preview: {0}", editPreview);
        editRedirectValues!.Single().Should().Contain("/Brands/Edit");
        editResponse.Headers.TryGetValues("Content-Security-Policy", out var editCspValues).Should().BeTrue();
        editCspValues!.Single().Should().Contain("form-action 'self'");

        using var listResponse = await SendHtmxGetAsync(client, $"/Brands?query={Uri.EscapeDataString(updatedSlug)}");
        var listHtml = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        listHtml.Should().Contain(updatedSlug);
        listHtml.Should().Contain(updatedName);
        listHtml.Should().Contain(WebAdminTestFactory.TestBrandLifecycleId.ToString());

        using var deleteResponse = await SendHtmxPostAsync(client, "/Brands/Delete", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(listHtml),
            ["id"] = WebAdminTestFactory.TestBrandLifecycleId.ToString(),
            ["rowVersion"] = rowVersion
        });
        var deleteHtml = await deleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var deletePreview = deleteHtml.Length > 600 ? deleteHtml[..600] : deleteHtml;

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        deleteResponse.Headers.TryGetValues("HX-Redirect", out var deleteRedirectValues)
            .Should().BeTrue("valid Brand delete should redirect; response preview: {0}", deletePreview);
        deleteRedirectValues!.Single().Should().Contain("/Brands");
        deleteResponse.Headers.TryGetValues("Content-Security-Policy", out var deleteCspValues).Should().BeTrue();
        deleteCspValues!.Single().Should().Contain("form-action 'self'");

        using var deletedListResponse = await SendHtmxGetAsync(client, $"/Brands?query={Uri.EscapeDataString(updatedSlug)}");
        var deletedListHtml = await deletedListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        deletedListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        deletedListHtml.Should().NotContain(WebAdminTestFactory.TestBrandLifecycleId.ToString());
        deletedListHtml.Should().NotContain(updatedName);
    }

    private static byte[] CreateTinyPngBytes()
    {
        return Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");
    }

    private static void DeleteUploadedSmokeFileIfPresent(string html)
    {
        const string uploadsMarker = "/uploads/";
        var uploadIndex = html.IndexOf(uploadsMarker, StringComparison.Ordinal);
        if (uploadIndex < 0)
        {
            return;
        }

        var endIndex = html.IndexOf(".png", uploadIndex, StringComparison.OrdinalIgnoreCase);
        if (endIndex <= uploadIndex)
        {
            return;
        }

        var relativeUrl = html[uploadIndex..(endIndex + ".png".Length)];
        var fileName = Path.GetFileName(relativeUrl);
        var webRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "Darwin.WebAdmin",
            "wwwroot"));
        var fullPath = Path.Combine(webRoot, "uploads", fileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private static async Task PostValidMobileDeviceMutationAndAssertFilteredAsync(
        HttpClient client,
        Guid deviceId,
        string deviceKey,
        string postPath,
        string verificationState)
    {
        var tokenSourcePath = $"/MobileOperations?q={Uri.EscapeDataString(deviceKey)}";
        using var tokenResponse = await SendHtmxGetAsync(client, tokenSourcePath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        tokenHtml.Should().Contain(deviceKey);
        using var postResponse = await SendHtmxPostAsync(client, postPath, new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
            ["id"] = deviceId.ToString(),
            ["rowVersion"] = ExtractHiddenInputValue(tokenHtml, "rowVersion"),
            ["q"] = deviceKey,
            ["page"] = "1"
        });
        var postHtml = await postResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var postPreview = postHtml.Length > 600 ? postHtml[..600] : postHtml;

        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        postResponse.Headers.TryGetValues("HX-Redirect", out var redirectValues)
            .Should().BeTrue("valid mobile device mutation should redirect; response preview: {0}", postPreview);
        redirectValues!.Single().Should().Contain("/MobileOperations");
        postResponse.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");

        using var listResponse = await SendHtmxGetAsync(
            client,
            $"/MobileOperations?state={Uri.EscapeDataString(verificationState)}&q={Uri.EscapeDataString(deviceKey)}");
        var listHtml = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        listHtml.Should().Contain(deviceKey);
    }

    private static async Task<string> PostValidEditorMutationAndAssertListedAsync(
        HttpClient client,
        string tokenSourcePath,
        string postPath,
        string listPath,
        string expectedListText,
        Dictionary<string, string> form)
    {
        using var tokenResponse = await SendHtmxGetAsync(client, tokenSourcePath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        form["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml);

        using var postResponse = await SendHtmxPostAsync(client, postPath, form);

        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var postHtml = await postResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var postPreview = postHtml.Length > 600 ? postHtml[..600] : postHtml;
        postResponse.Headers.TryGetValues("HX-Redirect", out var redirectValues)
            .Should().BeTrue("a successful HTMX mutation should redirect; response preview: {0}", postPreview);
        var redirectPath = redirectValues!.Single();
        redirectPath.Should().NotBeNullOrWhiteSpace();
        postResponse.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");

        using var listResponse = await SendHtmxGetAsync(client, listPath);
        var listHtml = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        listHtml.Should().Contain(expectedListText);
        return redirectPath;
    }

    private static async Task PostValidRolePermissionMutationAndAssertSelectedAsync(
        HttpClient client,
        Guid roleId,
        Guid permissionId)
    {
        var editorPath = $"/Roles/Permissions?id={roleId}";
        using var tokenResponse = await SendHtmxGetAsync(client, editorPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var token = ExtractAntiForgeryToken(tokenHtml);
        var rowVersion = ExtractHiddenInputValue(tokenHtml, "RowVersion");

        using var postResponse = await SendHtmxPostAsync(client, "/Roles/Permissions", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["RoleId"] = roleId.ToString(),
            ["RowVersion"] = rowVersion,
            ["SelectedPermissionIds"] = permissionId.ToString()
        });

        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        postResponse.Headers.TryGetValues("HX-Redirect", out var redirectValues)
            .Should().BeTrue("a successful role-permission mutation should redirect");
        redirectValues!.Single().Should().NotBeNullOrWhiteSpace();
        postResponse.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");

        using var updatedResponse = await SendHtmxGetAsync(client, editorPath);
        updatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedHtml = await updatedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        updatedHtml.Should().Contain($"value=\"{permissionId}\"");
        updatedHtml.Should().Contain("checked");
    }

    private static async Task PostValidOrderStatusMutationAndAssertDetailsAsync(
        HttpClient client,
        Guid orderId,
        string newStatus)
    {
        var detailsPath = $"/Orders/Details?id={orderId}";
        using var tokenResponse = await SendHtmxGetAsync(client, detailsPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var token = ExtractAntiForgeryToken(tokenHtml);
        var rowVersion = ExtractHiddenInputValue(tokenHtml, "RowVersion");

        using var postResponse = await SendHtmxPostAsync(client, "/Orders/ChangeStatus", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["OrderId"] = orderId.ToString(),
            ["RowVersion"] = rowVersion,
            ["NewStatus"] = newStatus,
            ["WarehouseId"] = string.Empty
        });

        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        postResponse.Headers.TryGetValues("HX-Redirect", out var redirectValues)
            .Should().BeTrue("a successful order status mutation should redirect");
        redirectValues!.Single().Should().NotBeNullOrWhiteSpace();
        postResponse.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");

        using var updatedResponse = await SendHtmxGetAsync(client, detailsPath);
        updatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedHtml = await updatedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        updatedHtml.Should().Contain(newStatus);
    }

    private static async Task PostValidUserRolesMutationAndAssertSelectedAsync(
        HttpClient client,
        Guid userId,
        Guid roleId)
    {
        var editorPath = $"/Users/Roles?id={userId}";
        using var tokenResponse = await SendHtmxGetAsync(client, editorPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var token = ExtractAntiForgeryToken(tokenHtml);
        var rowVersion = ExtractHiddenInputValue(tokenHtml, "RowVersion");
        var userEmail = ExtractHiddenInputValue(tokenHtml, "UserEmail");

        using var postResponse = await SendHtmxPostAsync(client, "/Users/Roles", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["UserId"] = userId.ToString(),
            ["RowVersion"] = rowVersion,
            ["UserEmail"] = userEmail,
            ["UserDisplay"] = userEmail,
            ["SelectedRoleIds"] = roleId.ToString(),
            ["ReturnToIndex"] = "false",
            ["Query"] = string.Empty,
            ["Filter"] = "All",
            ["Page"] = "1",
            ["PageSize"] = "20"
        });

        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        postResponse.Headers.TryGetValues("HX-Redirect", out var redirectValues)
            .Should().BeTrue("a successful user-role mutation should redirect");
        redirectValues!.Single().Should().NotBeNullOrWhiteSpace();
        postResponse.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");

        using var updatedResponse = await SendHtmxGetAsync(client, editorPath);
        updatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedHtml = await updatedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        updatedHtml.Should().Contain($"value=\"{roleId}\"");
        updatedHtml.Should().Contain("checked");
    }

    private static Guid ExtractQueryGuid(string path, string parameterName)
    {
        var queryStart = path.IndexOf('?', StringComparison.Ordinal);
        if (queryStart >= 0)
        {
            foreach (var pair in path[(queryStart + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2 && string.Equals(parts[0], parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    return Guid.Parse(Uri.UnescapeDataString(parts[1]));
                }
            }
        }

        var lastSegment = path.Split('?', 2)[0].Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (Guid.TryParse(lastSegment, out var id))
        {
            return id;
        }

        throw new InvalidOperationException($"Redirect path '{path}' did not contain query parameter '{parameterName}' or a trailing GUID segment.");
    }

}
