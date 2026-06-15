using Darwin.WebAdmin.Tests.TestInfrastructure;
using Darwin.Application.Abstractions.Invoicing;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Inventory.Commands;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Marketing;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

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
    [InlineData("/Sales")]
    [InlineData("/Sales/Orders")]
    [InlineData("/Sales/Invoices")]
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
    [InlineData("/Billing/Subscriptions")]
    [InlineData("/Inventory/Warehouses")]
    [InlineData("/Inventory/Suppliers")]
    [InlineData("/Inventory/StockLevels")]
    [InlineData("/Inventory/StockTransfers")]
    [InlineData("/Inventory/PurchaseOrders")]
    [InlineData("/Inventory/GoodsReceipts")]
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
    [InlineData("/Businesses/OnboardingWizard?id=44444444-4444-4444-4444-444444444444")]
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
    [InlineData("/Billing/Subscriptions")]
    [InlineData("/Businesses")]
    [InlineData("/Businesses/OnboardingWizard?id=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Businesses/SupportQueue?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/BusinessCommunications")]
    [InlineData("/BusinessCommunications/Details?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/BusinessCommunications/EmailAudits?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/BusinessCommunications/ChannelAudits?businessId=44444444-4444-4444-4444-444444444444")]
    [InlineData("/Loyalty/Programs")]
    [InlineData("/Loyalty/RewardTiers?loyaltyProgramId=55555555-5555-5555-5555-555555555555")]
    [InlineData("/Orders")]
    [InlineData("/Sales")]
    [InlineData("/Sales/Orders")]
    [InlineData("/Sales/Invoices")]
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

    [Fact]
    public async Task AuthenticatedBusinessOnboardingWizard_ShouldRenderCompactChecklistAgainstTestDatabaseContext()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        using var response = await SendHtmxGetAsync(
            client,
            "/Businesses/OnboardingWizard?id=44444444-4444-4444-4444-444444444444");
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().NotContain("<!DOCTYPE html>");
        html.Should().Contain("id=\"business-onboarding-wizard-shell\"");
        html.Should().MatchRegex("Onboarding[- ](Wizard|Assistent)");
        html.Should().Contain("WebAdmin Smoke Business");
        html.Should().Contain("data-onboarding-resume-step=\"users\"");
        html.Should().Contain("data-onboarding-next=\"true\"");
        html.Should().Contain("data-onboarding-step=\"profile\"");
        html.Should().Contain("data-onboarding-step=\"plan\"");
        html.Should().Contain("data-onboarding-step=\"users\"");
        html.Should().Contain("data-onboarding-step=\"locations\"");
        html.Should().Contain("data-onboarding-step=\"loyalty\"");
        html.Should().Contain("data-onboarding-step=\"communications\"");
        html.Should().Contain("data-onboarding-step=\"visibility\"");
        html.Should().Contain("data-onboarding-step=\"review\"");
        html.Should().Contain("step=locations");
        html.Should().Contain("hx-target=\"#business-onboarding-wizard-shell\"");
        html.Should().Contain("/Businesses/Edit");
        html.Should().Contain("/Businesses/Members");
        html.Should().Contain("/Businesses/Locations");
        html.Should().Contain("/BusinessCommunications/Details");
        html.Should().Contain("/Businesses/MerchantReadiness");
        html.Should().Contain("/Businesses/Setup");
        html.Should().Contain("/Businesses/SupportQueue");
        html.Should().NotContain("/js/admin-core.js");
        html.Should().NotContain("https://cdn.jsdelivr.net");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("default-src 'self'");
        response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions).Should().BeTrue();
        contentTypeOptions!.Single().Should().Be("nosniff");
    }

    [Fact]
    public async Task AuthenticatedBusinessOnboardingWizard_ShouldResumeRequestedStepWithoutChangingDerivedNextAction()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        using var response = await SendHtmxGetAsync(
            client,
            "/Businesses/OnboardingWizard?id=44444444-4444-4444-4444-444444444444&step=locations");
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("id=\"onboarding-step-locations\"");
        html.Should().Contain("aria-current=\"step\"");
        html.Should().Contain("data-onboarding-resume-step=\"users\"");
        html.Should().Contain("data-onboarding-next=\"true\"");
        html.Should().Contain("step=locations");
        html.Should().Contain("step=review");
        html.Should().NotContain("https://cdn.jsdelivr.net");
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
    [InlineData("/Inventory/CreateGoodsReceipt")]
    [InlineData("/Inventory/UpdateGoodsReceiptLifecycle")]
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

        var stockLevelRedirect = await PostValidEditorMutationAndAssertListedAsync(
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
        var stockLevelId = ExtractQueryGuid(stockLevelRedirect, "id");

        await PostValidInventoryStockActionAndAssertStockLevelAsync(
            client,
            stockLevelId,
            warehouseFromId,
            "ReserveStock",
            "Inventory reserved for smoke order.",
            Guid.NewGuid(),
            "Reserved");
        await AssertStockQuantitiesAsync(client, warehouseFromId, 24, 3);
        await AssertInventoryLedgerContainsAsync(client, warehouseFromId, "Inventory reserved for smoke order.");

        await PostValidInventoryStockActionAndAssertStockLevelAsync(
            client,
            stockLevelId,
            warehouseFromId,
            "ReleaseReservation",
            "Released smoke reservation.",
            Guid.NewGuid(),
            "Reserved");
        await AssertStockQuantitiesAsync(client, warehouseFromId, 25, 2);
        await AssertInventoryLedgerContainsAsync(client, warehouseFromId, "Released smoke reservation.");

        var returnReferenceId = Guid.NewGuid();
        var returnReceiptListHtml = await PostValidInventoryStockActionAndAssertStockLevelAsync(
            client,
            stockLevelId,
            warehouseFromId,
            "ReturnReceipt",
            "Received smoke return.",
            returnReferenceId,
            "WEBADMIN-SMOKE-VARIANT");

        await PostDuplicateReturnReceiptAndAssertIdempotentAsync(
            client,
            stockLevelId,
            warehouseFromId,
            returnReferenceId,
            returnReceiptListHtml);
        await AssertStockQuantitiesAsync(client, warehouseFromId, 26, 2);
        await AssertInventoryLedgerContainsAsync(client, warehouseFromId, "Received smoke return.");

        var stockTransferRedirect = await PostValidEditorMutationAndAssertListedAsync(
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
        var stockTransferId = ExtractQueryGuid(stockTransferRedirect, "id");

        await PostValidStockTransferLifecycleActionAndAssertStatusAsync(
            client,
            stockTransferId,
            warehouseFromId,
            warehouseToName,
            "MarkInTransit",
            "InTransit");
        await AssertStockQuantitiesAsync(client, warehouseFromId, 22, 2);
        await AssertInventoryLedgerContainsAsync(client, warehouseFromId, "StockTransferDispatched");

        await PostValidStockTransferLifecycleActionAndAssertStatusAsync(
            client,
            stockTransferId,
            warehouseFromId,
            warehouseToName,
            "Complete",
            "Completed");
        await AssertStockQuantitiesAsync(client, warehouseFromId, 22, 2);
        await AssertStockQuantitiesAsync(client, warehouseToId, 4, 0);
        await AssertInventoryLedgerContainsAsync(client, warehouseToId, "StockTransferReceived");

        var purchaseOrderRedirect = await PostValidEditorMutationAndAssertListedAsync(
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
        var purchaseOrderId = ExtractQueryGuid(purchaseOrderRedirect, "id");

        await PostValidPurchaseOrderLifecycleActionAndAssertStatusAsync(
            client,
            purchaseOrderId,
            purchaseOrderNumber,
            "Issue",
            "Issued");

        await PostValidPurchaseOrderLifecycleActionAndAssertStatusAsync(
            client,
            purchaseOrderId,
            purchaseOrderNumber,
            "Receive",
            "Received");
        await AssertStockQuantitiesAsync(client, warehouseFromId, 28, 2);
        await AssertInventoryLedgerContainsAsync(client, warehouseFromId, UpdateGoodsReceiptLifecycleHandler.PostedReason);

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
    public async Task AuthenticatedMobileOperationsClearPushToken_WithMissingRowVersion_ShouldSurfaceFailureMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var deviceKey = "webadmin-smoke-clear-push";

        using var tokenResponse = await SendHtmxGetAsync(
            client,
            $"/MobileOperations?q={Uri.EscapeDataString(deviceKey)}");
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/MobileOperations/ClearPushToken",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["id"] = WebAdminTestFactory.TestClearPushDeviceId.ToString(),
                ["rowVersion"] = string.Empty,
                ["q"] = deviceKey,
                ["page"] = "1"
            });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var redirectValues).Should().BeTrue();
        var redirectUrl = redirectValues!.Single();
        redirectUrl.Should().Contain("/MobileOperations");
        using var redirectedResponse = await SendHtmxGetAsync(client, redirectUrl);
        var redirectedHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        redirectedHtml.Should().Contain("Push token could not be cleared.");
        responseHtml.Should().NotBeNullOrWhiteSpace();
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedMobileOperationsClearPushToken_WithStaleRowVersion_ShouldSurfaceFailureMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var deviceKey = "webadmin-smoke-clear-push";

        using var baselineTokenResponse = await SendHtmxGetAsync(
            client,
            $"/MobileOperations?q={Uri.EscapeDataString(deviceKey)}");
        baselineTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineHtml = await baselineTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var baselineRowVersion = ExtractHiddenInputValue(baselineHtml, "rowVersion");

        using var firstResponse = await SendHtmxPostAsync(
            client,
            "/MobileOperations/ClearPushToken",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(baselineHtml),
                ["id"] = WebAdminTestFactory.TestClearPushDeviceId.ToString(),
                ["rowVersion"] = baselineRowVersion,
                ["q"] = deviceKey,
                ["page"] = "1"
            });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstResponse.Headers.TryGetValues("HX-Redirect", out var firstRedirectValues).Should().BeTrue();
        firstRedirectValues!.Single().Should().Contain("/MobileOperations");

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/MobileOperations/ClearPushToken",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(baselineHtml),
                ["id"] = WebAdminTestFactory.TestClearPushDeviceId.ToString(),
                ["rowVersion"] = baselineRowVersion,
                ["q"] = deviceKey,
                ["page"] = "1"
            });
        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues).Should().BeTrue();
        using var staleRedirected = await SendHtmxGetAsync(client, staleRedirectValues!.Single());
        var staleRedirectedHtml = await staleRedirected.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        staleRedirectedHtml.Should().Contain("Push token could not be cleared.");
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleCspValues).Should().BeTrue();
        staleCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedMobileOperationsDeactivateDevice_WithMissingRowVersion_ShouldSurfaceFailureMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var deviceKey = "webadmin-smoke-deactivate";

        using var tokenResponse = await SendHtmxGetAsync(
            client,
            $"/MobileOperations?q={Uri.EscapeDataString(deviceKey)}");
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/MobileOperations/DeactivateDevice",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["id"] = WebAdminTestFactory.TestDeactivateDeviceId.ToString(),
                ["rowVersion"] = string.Empty,
                ["q"] = deviceKey,
                ["page"] = "1"
            });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var redirectValues).Should().BeTrue();
        var redirectUrl = redirectValues!.Single();
        redirectUrl.Should().Contain("/MobileOperations");
        using var redirectedResponse = await SendHtmxGetAsync(client, redirectUrl);
        var redirectedHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        redirectedHtml.Should().Contain("Device could not be deactivated.");
        responseHtml.Should().NotBeNullOrWhiteSpace();
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedMobileOperationsDeactivateDevice_WithStaleRowVersion_ShouldSurfaceFailureMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var deviceKey = "webadmin-smoke-deactivate";

        using var baselineTokenResponse = await SendHtmxGetAsync(
            client,
            $"/MobileOperations?q={Uri.EscapeDataString(deviceKey)}");
        baselineTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineHtml = await baselineTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var baselineRowVersion = ExtractHiddenInputValue(baselineHtml, "rowVersion");

        using var firstResponse = await SendHtmxPostAsync(
            client,
            "/MobileOperations/DeactivateDevice",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(baselineHtml),
                ["id"] = WebAdminTestFactory.TestDeactivateDeviceId.ToString(),
                ["rowVersion"] = baselineRowVersion,
                ["q"] = deviceKey,
                ["page"] = "1"
            });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstResponse.Headers.TryGetValues("HX-Redirect", out var firstRedirectValues).Should().BeTrue();
        firstRedirectValues!.Single().Should().Contain("/MobileOperations");

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/MobileOperations/DeactivateDevice",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(baselineHtml),
                ["id"] = WebAdminTestFactory.TestDeactivateDeviceId.ToString(),
                ["rowVersion"] = baselineRowVersion,
                ["q"] = deviceKey,
                ["page"] = "1"
            });
        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues).Should().BeTrue();
        using var staleRedirected = await SendHtmxGetAsync(client, staleRedirectValues!.Single());
        var staleRedirectedHtml = await staleRedirected.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        staleRedirectedHtml.Should().Contain("Device could not be deactivated.");
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleCspValues).Should().BeTrue();
        staleCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedOrderCancellation_ShouldReleaseReservedStockThroughHostedWebAdminFlow()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var warehouseName = $"Cancel Release Warehouse {suffix}";

        var warehouseRedirect = await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Inventory/CreateWarehouse?businessId=44444444-4444-4444-4444-444444444444",
            "/Inventory/CreateWarehouse",
            $"/Inventory/Warehouses?businessId=44444444-4444-4444-4444-444444444444&q={Uri.EscapeDataString(warehouseName)}",
            warehouseName,
            new Dictionary<string, string>
            {
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["Name"] = warehouseName,
                ["Location"] = "Berlin Cancel Release",
                ["Description"] = "Smoke-created order cancellation release warehouse.",
                ["IsDefault"] = "true"
            });
        var warehouseId = ExtractQueryGuid(warehouseRedirect, "id");

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            $"/Inventory/CreateStockLevel?businessId=44444444-4444-4444-4444-444444444444&warehouseId={warehouseId}",
            "/Inventory/CreateStockLevel",
            $"/Inventory/StockLevels?businessId=44444444-4444-4444-4444-444444444444&warehouseId={warehouseId}&q=WEBADMIN-SMOKE-VARIANT",
            "WEBADMIN-SMOKE-VARIANT",
            new Dictionary<string, string>
            {
                ["WarehouseId"] = warehouseId.ToString(),
                ["ProductVariantId"] = WebAdminTestFactory.TestProductVariantId.ToString(),
                ["AvailableQuantity"] = "10",
                ["ReservedQuantity"] = "1",
                ["ReorderPoint"] = "2",
                ["ReorderQuantity"] = "5",
                ["InTransitQuantity"] = "0"
            });

        await PostValidOrderStatusMutationAndAssertDetailsAsync(
            client,
            WebAdminTestFactory.TestOrderId,
            "Cancelled",
            warehouseId);

        using var stockResponse = await SendHtmxGetAsync(
            client,
            $"/Inventory/StockLevels?businessId=44444444-4444-4444-4444-444444444444&warehouseId={warehouseId}&q=WEBADMIN-SMOKE-VARIANT");
        var stockHtml = await stockResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        stockResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ExtractStockQuantities(stockHtml, "WEBADMIN-SMOKE-VARIANT")
            .Should()
            .Be((11, 0), "cancelling an order with reserved stock should release the reservation exactly once");
    }

    [Fact]
    public async Task AuthenticatedRefundCoordination_ShouldRecordRefundWithoutMovingStock()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var warehouseName = $"Refund Coordination Warehouse {suffix}";
        var refundReason = $"Refund coordination smoke {suffix}";

        var warehouseRedirect = await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Inventory/CreateWarehouse?businessId=44444444-4444-4444-4444-444444444444",
            "/Inventory/CreateWarehouse",
            $"/Inventory/Warehouses?businessId=44444444-4444-4444-4444-444444444444&q={Uri.EscapeDataString(warehouseName)}",
            warehouseName,
            new Dictionary<string, string>
            {
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["Name"] = warehouseName,
                ["Location"] = "Berlin Refund Coordination",
                ["Description"] = "Smoke-created refund coordination warehouse.",
                ["IsDefault"] = "false"
            });
        var warehouseId = ExtractQueryGuid(warehouseRedirect, "id");

        await PostValidEditorMutationAndAssertListedAsync(
            client,
            $"/Inventory/CreateStockLevel?businessId=44444444-4444-4444-4444-444444444444&warehouseId={warehouseId}",
            "/Inventory/CreateStockLevel",
            $"/Inventory/StockLevels?businessId=44444444-4444-4444-4444-444444444444&warehouseId={warehouseId}&q=WEBADMIN-SMOKE-VARIANT",
            "WEBADMIN-SMOKE-VARIANT",
            new Dictionary<string, string>
            {
                ["WarehouseId"] = warehouseId.ToString(),
                ["ProductVariantId"] = WebAdminTestFactory.TestProductVariantId.ToString(),
                ["AvailableQuantity"] = "12",
                ["ReservedQuantity"] = "3",
                ["ReorderPoint"] = "2",
                ["ReorderQuantity"] = "5",
                ["InTransitQuantity"] = "0"
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

        using var paymentsResponse = await SendHtmxGetAsync(
            client,
            $"/Orders/Payments?orderId={WebAdminTestFactory.TestOrderId}");
        var paymentsHtml = await paymentsResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        paymentsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        paymentsHtml.Should().Contain("WebAdminSeedPay");
        paymentsHtml.Should().Contain("seed-order-payment");
        paymentsHtml.Should().ContainAny("EUR 5.00", "EUR 5,00");
        paymentsHtml.Should().ContainAny("EUR 20.99", "EUR 20,99");

        using var stockResponse = await SendHtmxGetAsync(
            client,
            $"/Inventory/StockLevels?businessId=44444444-4444-4444-4444-444444444444&warehouseId={warehouseId}&q=WEBADMIN-SMOKE-VARIANT");
        var stockHtml = await stockResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        stockResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ExtractStockQuantities(stockHtml, "WEBADMIN-SMOKE-VARIANT")
            .Should()
            .Be((12, 3), "creating a payment refund should not move inventory; stock movement belongs to return receipt and cancellation flows");
    }

    [Fact]
    public async Task AuthenticatedBusinessCreation_ShouldStartInactiveAndPendingApproval()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var businessName = $"Hosted Pending Business {suffix}";
        var businessEmail = $"pending-{suffix}@example.test";

        var businessId = await CreateHostedBusinessAsync(
            client,
            businessName,
            businessEmail,
            ownerUserId: null,
            legalName: string.Empty,
            isActive: true);

        using var setupResponse = await SendHtmxGetAsync(client, $"/Businesses/Setup?id={businessId}");
        var setupHtml = await setupResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        setupResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        setupHtml.Should().Contain("name=\"OperationalStatus\" value=\"PendingApproval\"");
        setupHtml.Should().Contain("0 Owner, 0 Primary Locations");
        setupHtml.Should().Contain("disabled=\"disabled\"");
    }

    [Fact]
    public async Task AuthenticatedBusinessLifecycle_ShouldApproveSuspendAndReactivateWithHostedForms()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var businessName = $"Hosted Lifecycle Business {suffix}";
        var businessEmail = $"lifecycle-{suffix}@example.test";
        var locationName = $"Lifecycle Primary Location {suffix}";

        var businessId = await CreateHostedBusinessAsync(
            client,
            businessName,
            businessEmail,
            WebAdminTestFactory.TestMemberUserId,
            legalName: $"{businessName} GmbH",
            isActive: false);

        await CreateHostedBusinessLocationAsync(client, businessId, locationName, isPrimary: true);

        var approvedHtml = await PostHostedBusinessLifecycleActionAsync(client, businessId, "Approve");
        approvedHtml.Should().Contain("name=\"OperationalStatus\" value=\"Approved\"");
        approvedHtml.Should().Contain("checked=\"checked\"");

        var suspendedHtml = await PostHostedBusinessLifecycleActionAsync(
            client,
            businessId,
            "Suspend",
            new Dictionary<string, string> { ["note"] = "Hosted suspension smoke" });
        suspendedHtml.Should().Contain("name=\"OperationalStatus\" value=\"Suspended\"");
        suspendedHtml.Should().Contain("Hosted suspension smoke");

        var reactivatedHtml = await PostHostedBusinessLifecycleActionAsync(client, businessId, "Reactivate");
        reactivatedHtml.Should().Contain("name=\"OperationalStatus\" value=\"Approved\"");
        reactivatedHtml.Should().Contain("checked=\"checked\"");
    }

    [Fact]
    public async Task AuthenticatedBusinessLifecycle_InvalidTransitions_ShouldNotChangeCurrentOperationalStatus()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var businessName = $"Hosted Invalid Lifecycle {suffix}";
        var businessEmail = $"invalid-lifecycle-{suffix}@example.test";
        var locationName = $"Invalid Lifecycle Primary Location {suffix}";

        var businessId = await CreateHostedBusinessAsync(
            client,
            businessName,
            businessEmail,
            WebAdminTestFactory.TestMemberUserId,
            legalName: $"{businessName} GmbH",
            isActive: false);
        await CreateHostedBusinessLocationAsync(client, businessId, locationName, isPrimary: true);

        var setupPath = $"/Businesses/Setup?id={businessId}";
        using var setupResponse = await SendHtmxGetAsync(client, setupPath);
        setupResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var setupHtml = await setupResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        setupHtml.Should().Contain("name=\"OperationalStatus\" value=\"PendingApproval\"");

        using var invalidSuspendResponse = await SendHtmxPostAsync(
            client,
            "/Businesses/Suspend",
            BuildHostedBusinessLifecycleForm(setupHtml, businessId));
        var invalidSuspendHtml = await invalidSuspendResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        invalidSuspendResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        invalidSuspendResponse.Headers.TryGetValues("HX-Redirect", out var invalidSuspendRedirect).Should().BeTrue();
        invalidSuspendRedirect!.Single().Should().Contain("/Businesses/Setup");
        invalidSuspendHtml.Should().BeEmpty();
        invalidSuspendResponse.Headers.TryGetValues("Content-Security-Policy", out var invalidSuspendCsp).Should().BeTrue();
        invalidSuspendCsp!.Single().Should().Contain("form-action 'self'");

        using var stillPendingResponse = await SendHtmxGetAsync(client, setupPath);
        stillPendingResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var stillPendingHtml = await stillPendingResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        stillPendingHtml.Should().Contain("name=\"OperationalStatus\" value=\"PendingApproval\"");
        stillPendingHtml.Should().MatchRegex("Unable to suspend|nicht gesperrt");

        var approvedHtml = await PostHostedBusinessLifecycleActionAsync(client, businessId, "Approve");
        approvedHtml.Should().Contain("name=\"OperationalStatus\" value=\"Approved\"");
        approvedHtml.Should().Contain("checked=\"checked\"");

        using var invalidReactivateResponse = await SendHtmxPostAsync(
            client,
            "/Businesses/Reactivate",
            BuildHostedBusinessLifecycleForm(approvedHtml, businessId));
        invalidReactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        invalidReactivateResponse.Headers.TryGetValues("HX-Redirect", out var invalidReactivateRedirect).Should().BeTrue();
        invalidReactivateRedirect!.Single().Should().Contain("/Businesses/Setup");
        invalidReactivateResponse.Headers.TryGetValues("Content-Security-Policy", out var invalidReactivateCsp).Should().BeTrue();
        invalidReactivateCsp!.Single().Should().Contain("form-action 'self'");

        using var stillApprovedResponse = await SendHtmxGetAsync(client, setupPath);
        stillApprovedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var stillApprovedHtml = await stillApprovedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        stillApprovedHtml.Should().Contain("name=\"OperationalStatus\" value=\"Approved\"");
        stillApprovedHtml.Should().MatchRegex("Unable to reactivate|nicht reaktiviert");

        var suspendedHtml = await PostHostedBusinessLifecycleActionAsync(
            client,
            businessId,
            "Suspend",
            new Dictionary<string, string> { ["note"] = "Hosted lifecycle invalid transition smoke" });
        suspendedHtml.Should().Contain("name=\"OperationalStatus\" value=\"Suspended\"");
        suspendedHtml.Should().Contain("Hosted lifecycle invalid transition smoke");

        using var invalidApproveResponse = await SendHtmxPostAsync(
            client,
            "/Businesses/Approve",
            BuildHostedBusinessLifecycleForm(suspendedHtml, businessId));
        invalidApproveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        invalidApproveResponse.Headers.TryGetValues("HX-Redirect", out var invalidApproveRedirect).Should().BeTrue();
        invalidApproveRedirect!.Single().Should().Contain("/Businesses/Setup");
        invalidApproveResponse.Headers.TryGetValues("Content-Security-Policy", out var invalidApproveCsp).Should().BeTrue();
        invalidApproveCsp!.Single().Should().Contain("form-action 'self'");

        using var stillSuspendedResponse = await SendHtmxGetAsync(client, setupPath);
        stillSuspendedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var stillSuspendedHtml = await stillSuspendedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        stillSuspendedHtml.Should().Contain("name=\"OperationalStatus\" value=\"Suspended\"");
        stillSuspendedHtml.Should().MatchRegex("Unable to approve|nicht freigegeben");
    }

    [Fact]
    public async Task AuthenticatedBusinessApproval_ShouldRemainPendingWhenPrerequisitesAreMissing()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var businessName = $"Hosted Blocked Business {suffix}";
        var businessEmail = $"blocked-{suffix}@example.test";

        var businessId = await CreateHostedBusinessAsync(
            client,
            businessName,
            businessEmail,
            ownerUserId: null,
            legalName: string.Empty,
            isActive: false);

        var blockedHtml = await PostHostedBusinessLifecycleActionAsync(client, businessId, "Approve");

        blockedHtml.Should().Contain("name=\"OperationalStatus\" value=\"PendingApproval\"");
        blockedHtml.Should().Contain("0 Owner, 0 Primary Locations");
    }

    [Theory]
    [InlineData("Accepted", "webadmin-invitation-accepted@example.test")]
    [InlineData("Revoked", "webadmin-invitation-revoked@example.test")]
    public async Task AuthenticatedClosedBusinessInvitations_ShouldNotExposeResendOrRevokeOperatorForms(
        string filter,
        string invitationEmail)
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        using var response = await SendHtmxGetAsync(
            client,
            $"/Businesses/Invitations?businessId=44444444-4444-4444-4444-444444444444&filter={filter}&query={Uri.EscapeDataString(invitationEmail)}");
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain(invitationEmail);
        html.Should().Contain(filter);
        html.Should().NotContain("hx-post=\"/Businesses/ResendInvitation\"");
        html.Should().NotContain("hx-post=\"/Businesses/RevokeInvitation\"");
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

    [Theory]
    [MemberData(nameof(AuthenticatedEditFormsRowVersionRenderCases))]
    public async Task AuthenticatedEditForm_ShouldRenderRowVersion_AsValidBase64ForHtmxRequests(string editPath)
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        await AssertEditFormRendersValidBase64RowVersionAsync(client, editPath);
    }

    [Fact]
    public async Task AuthenticatedBusinessEditForm_ShouldRenderRowVersion_AsValidBase64ForHtmxRequests()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        await AssertEditFormRendersValidBase64RowVersionAsync(
            client,
            "/Businesses/Edit?id=44444444-4444-4444-4444-444444444444");
    }

    [Fact]
    public async Task AuthenticatedRolePermissionsEditForm_ShouldRenderRowVersion_AsValidBase64ForHtmxRequests()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        await AssertEditFormRendersValidBase64RowVersionAsync(
            client,
            $"/Roles/Permissions?id={WebAdminTestFactory.TestRoleId}");
    }

    [Fact]
    public async Task AuthenticatedUserRolesEditForm_ShouldRenderRowVersion_AsValidBase64ForHtmxRequests()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        await AssertEditFormRendersValidBase64RowVersionAsync(
            client,
            $"/Users/Roles?id={WebAdminTestFactory.TestLifecycleUserId}");
    }

    [Fact]
    public async Task AuthenticatedSiteSettingsEditForm_ShouldRenderRowVersion_AsValidBase64ForHtmxRequests()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        await AssertEditFormRendersValidBase64RowVersionAsync(
            client,
            "/SiteSettings/Edit?fragment=site-settings-communications-policy");
    }

    [Fact]
    public async Task AuthenticatedShippingMethodEditForm_ShouldRenderRowVersion_AsValidBase64ForHtmxRequests()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var name = $"FormRender Smoke {suffix}";
        var carrier = $"Carrier-{suffix}";
        var service = $"Service-{suffix}";

        var shippingMethodId = await CreateShippingMethodAndGetIdAsync(client, name, carrier, service);

        await AssertEditFormRendersValidBase64RowVersionAsync(
            client,
            $"/ShippingMethods/Edit?id={shippingMethodId}");
    }

    [Fact]
    public async Task AuthenticatedWarehouseEditForm_ShouldRenderRowVersion_AsValidBase64ForHtmxRequests()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var warehouseId = await CreateTestWarehouseAndReturnIdAsync(
            client,
            $"RowVersion Render {Guid.NewGuid():N}",
            "Berlin",
            "Smoke warehouse for row-version render test",
            false);

        await AssertEditFormRendersValidBase64RowVersionAsync(
            client,
            $"/Inventory/EditWarehouse?id={warehouseId}");
    }

    [Fact]
    public async Task AuthenticatedSupplierEditForm_ShouldRenderRowVersion_AsValidBase64ForHtmxRequests()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var supplierId = await CreateTestSupplierAndReturnIdAsync(
            client,
            $"RowVersion Render Supplier {Guid.NewGuid():N}",
            $"supplier-{Guid.NewGuid():N}@example.test");

        await AssertEditFormRendersValidBase64RowVersionAsync(
            client,
            $"/Inventory/EditSupplier?id={supplierId}");
    }

    [Fact]
    public async Task AuthenticatedStockLevelEditForm_ShouldRenderRowVersion_AsValidBase64ForHtmxRequests()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var (_, stockLevelId) = await CreateTestStockLevelAndReturnIdAsync(client);

        await AssertEditFormRendersValidBase64RowVersionAsync(
            client,
            $"/Inventory/EditStockLevel?id={stockLevelId}");
    }

    [Fact]
    public async Task AuthenticatedCrmCustomerEditForm_ShouldRenderRowVersion_AsValidBase64ForHtmxRequests()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var customerId = await CreateTestCustomerAndReturnIdAsync(
            client,
            $"RowVersion{Guid.NewGuid():N}",
            "Smoke",
            $"row-version-customer-{Guid.NewGuid():N}@example.test");

        await AssertEditFormRendersValidBase64RowVersionAsync(
            client,
            $"/Crm/EditCustomer?id={customerId}");
    }

    private static async Task AssertEditFormRendersValidBase64RowVersionAsync(HttpClient client, string editPath)
    {
        using var response = await SendHtmxGetAsync(client, editPath);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");

        var rowVersion = html.Contains("name=\"RowVersion\"", StringComparison.Ordinal)
            ? ExtractHiddenInputValue(html, "RowVersion")
            : ExtractHiddenInputValue(html, "rowVersion");
        rowVersion.Should().NotBeNullOrWhiteSpace();
        var decode = () => Convert.FromBase64String(rowVersion);
        decode.Should().NotThrow();
        decode().Should().NotBeEmpty();

        html.Should().NotContain("System.Byte[]");
    }

    public static IEnumerable<object[]> AuthenticatedEditFormsRowVersionRenderCases => new[]
    {
        new object[] { $"/Billing/EditPlan?id={WebAdminTestFactory.TestBillingPlanId}" },
        new object[] { $"/Brands/Edit?id={WebAdminTestFactory.TestBrandId}" },
        new object[] { $"/Categories/Edit?id={WebAdminTestFactory.TestCategoryId}" },
        new object[] { $"/Products/Edit?id={WebAdminTestFactory.TestProductId}" },
        new object[] { $"/Media/Edit?id={WebAdminTestFactory.TestMediaAssetId}" }
    };

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
        return WebUtility.HtmlDecode(input[valueStart..valueEnd]);
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

    private static async Task<string> PostValidInventoryStockActionAndAssertStockLevelAsync(
        HttpClient client,
        Guid stockLevelId,
        Guid warehouseId,
        string actionName,
        string reason,
        Guid referenceId,
        string expectedListText)
    {
        var editorPath = $"/Inventory/{actionName}?stockLevelId={stockLevelId}&businessId=44444444-4444-4444-4444-444444444444";
        using var tokenResponse = await SendHtmxGetAsync(client, editorPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var token = ExtractAntiForgeryToken(tokenHtml);

        using var postResponse = await SendHtmxPostAsync(client, $"/Inventory/{actionName}", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["StockLevelId"] = stockLevelId.ToString(),
            ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
            ["WarehouseId"] = warehouseId.ToString(),
            ["ProductVariantId"] = WebAdminTestFactory.TestProductVariantId.ToString(),
            ["AvailableQuantity"] = ExtractHiddenInputValue(tokenHtml, "AvailableQuantity"),
            ["ReservedQuantity"] = ExtractHiddenInputValue(tokenHtml, "ReservedQuantity"),
            ["Quantity"] = "1",
            ["Reason"] = reason,
            ["ReferenceId"] = referenceId.ToString()
        });
        var postHtml = await postResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var postPreview = postHtml.Length > 600 ? postHtml[..600] : postHtml;

        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        postResponse.Headers.TryGetValues("HX-Redirect", out var redirectValues)
            .Should().BeTrue("a valid inventory stock action should redirect; response preview: {0}", postPreview);
        redirectValues!.Single().Should().Contain("/Inventory/StockLevels");
        postResponse.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");

        using var listResponse = await SendHtmxGetAsync(
            client,
            $"/Inventory/StockLevels?businessId=44444444-4444-4444-4444-444444444444&warehouseId={warehouseId}&q=WEBADMIN-SMOKE-VARIANT");
        var listHtml = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        listHtml.Should().Contain(expectedListText);
        listHtml.Should().Contain(WebAdminTestFactory.TestProductVariantId.ToString());
        return listHtml;
    }

    private static async Task PostDuplicateReturnReceiptAndAssertIdempotentAsync(
        HttpClient client,
        Guid stockLevelId,
        Guid warehouseId,
        Guid referenceId,
        string listHtmlAfterFirstReceipt)
    {
        var quantitiesAfterFirstReceipt = ExtractStockQuantities(listHtmlAfterFirstReceipt, "WEBADMIN-SMOKE-VARIANT");

        var duplicateListHtml = await PostValidInventoryStockActionAndAssertStockLevelAsync(
            client,
            stockLevelId,
            warehouseId,
            "ReturnReceipt",
            "Received smoke return.",
            referenceId,
            "WEBADMIN-SMOKE-VARIANT");

        ExtractStockQuantities(duplicateListHtml, "WEBADMIN-SMOKE-VARIANT")
            .Should()
            .Be(quantitiesAfterFirstReceipt, "duplicate return receipts with the same reference should be idempotent");
    }

    private static (int Available, int Reserved) ExtractStockQuantities(string html, string sku)
    {
        var pattern = $@"<tr>\s*<td>{Regex.Escape(sku)}</td>\s*<td>(?<available>-?\d+)</td>\s*<td>(?<reserved>-?\d+)</td>";
        var match = Regex.Match(html, pattern, RegexOptions.Singleline | RegexOptions.CultureInvariant);

        match.Success.Should().BeTrue("stock-level row for {0} should be present in the list HTML", sku);

        return (
            int.Parse(match.Groups["available"].Value, System.Globalization.CultureInfo.InvariantCulture),
            int.Parse(match.Groups["reserved"].Value, System.Globalization.CultureInfo.InvariantCulture));
    }

    private static async Task AssertStockQuantitiesAsync(
        HttpClient client,
        Guid warehouseId,
        int expectedAvailable,
        int expectedReserved)
    {
        using var response = await SendHtmxGetAsync(
            client,
            $"/Inventory/StockLevels?businessId=44444444-4444-4444-4444-444444444444&warehouseId={warehouseId}&q=WEBADMIN-SMOKE-VARIANT");
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ExtractStockQuantities(html, "WEBADMIN-SMOKE-VARIANT")
            .Should()
            .Be((expectedAvailable, expectedReserved));
    }

    private static async Task AssertInventoryLedgerContainsAsync(
        HttpClient client,
        Guid warehouseId,
        string expectedReason)
    {
        using var response = await SendHtmxGetAsync(
            client,
            $"/Inventory/VariantLedger?variantId={WebAdminTestFactory.TestProductVariantId}&warehouseId={warehouseId}");
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain(expectedReason);
        html.Should().Contain(WebAdminTestFactory.TestProductVariantId.ToString());
    }

    private static async Task PostValidStockTransferLifecycleActionAndAssertStatusAsync(
        HttpClient client,
        Guid transferId,
        Guid warehouseId,
        string query,
        string action,
        string expectedStatus)
    {
        var listPath = $"/Inventory/StockTransfers?businessId=44444444-4444-4444-4444-444444444444&warehouseId={warehouseId}&q={Uri.EscapeDataString(query)}";
        using var tokenResponse = await SendHtmxGetAsync(client, listPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        tokenHtml.Should().Contain(transferId.ToString());

        using var postResponse = await SendHtmxPostAsync(client, $"/Inventory/UpdateStockTransferLifecycle?id={transferId}", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
            ["id"] = transferId.ToString(),
            ["rowVersion"] = ExtractHiddenInputValue(tokenHtml, "rowVersion"),
            ["action"] = action,
            ["businessId"] = "44444444-4444-4444-4444-444444444444",
            ["warehouseId"] = warehouseId.ToString(),
            ["page"] = "1",
            ["pageSize"] = "20",
            ["q"] = query,
            ["filter"] = "All"
        });
        var postHtml = await postResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var postPreview = postHtml.Length > 600 ? postHtml[..600] : postHtml;

        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        postResponse.Headers.TryGetValues("HX-Redirect", out var redirectValues)
            .Should().BeTrue("a valid stock-transfer lifecycle action should redirect; response preview: {0}", postPreview);
        redirectValues!.Single().Should().Contain("/Inventory/StockTransfers");
        postResponse.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");

        using var updatedResponse = await SendHtmxGetAsync(client, $"{listPath}&filter={expectedStatus}");
        var updatedHtml = await updatedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        updatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedHtml.Should().Contain(transferId.ToString());
        updatedHtml.Should().Contain(expectedStatus);
    }

    private static async Task PostValidPurchaseOrderLifecycleActionAndAssertStatusAsync(
        HttpClient client,
        Guid purchaseOrderId,
        string orderNumber,
        string action,
        string expectedStatus)
    {
        var listPath = $"/Inventory/PurchaseOrders?businessId=44444444-4444-4444-4444-444444444444&q={Uri.EscapeDataString(orderNumber)}";
        using var tokenResponse = await SendHtmxGetAsync(client, listPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        tokenHtml.Should().Contain(purchaseOrderId.ToString());

        using var postResponse = await SendHtmxPostAsync(client, $"/Inventory/UpdatePurchaseOrderLifecycle?id={purchaseOrderId}", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
            ["id"] = purchaseOrderId.ToString(),
            ["rowVersion"] = ExtractHiddenInputValue(tokenHtml, "rowVersion"),
            ["action"] = action,
            ["businessId"] = "44444444-4444-4444-4444-444444444444",
            ["page"] = "1",
            ["pageSize"] = "20",
            ["q"] = orderNumber,
            ["filter"] = "All"
        });
        var postHtml = await postResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var postPreview = postHtml.Length > 600 ? postHtml[..600] : postHtml;

        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        postResponse.Headers.TryGetValues("HX-Redirect", out var redirectValues)
            .Should().BeTrue("a valid purchase-order lifecycle action should redirect; response preview: {0}", postPreview);
        redirectValues!.Single().Should().Contain("/Inventory/PurchaseOrders");
        postResponse.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");

        using var updatedResponse = await SendHtmxGetAsync(client, $"{listPath}&filter={expectedStatus}");
        var updatedHtml = await updatedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        updatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedHtml.Should().Contain(purchaseOrderId.ToString());
        updatedHtml.Should().Contain(expectedStatus);
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

    [Fact]
    public async Task AuthenticatedSiteSettingsEdit_WithMissingRowVersion_ShouldShowValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        const string editorPath = "/SiteSettings/Edit?fragment=site-settings-communications-policy";

        using var tokenResponse = await SendHtmxGetAsync(client, editorPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        var form = BuildValidSiteSettingsForm(
            ExtractHiddenInputValue(tokenHtml, "Id"),
            string.Empty,
            $"Missing RowVersion {Guid.NewGuid():N}",
            $"Smoke email transport {Guid.NewGuid():N} {{test_target}}");
        form["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml);

        using var response = await SendHtmxPostAsync(client, editorPath, form);
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var redirectValues).Should().BeFalse();
        responseHtml.Should().Contain("name=\"RowVersion\" value=\"\"");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedSiteSettingsEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        const string editorPath = "/SiteSettings/Edit?fragment=site-settings-communications-policy";

        using var baselineTokenResponse = await SendHtmxGetAsync(client, editorPath);
        baselineTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineTokenHtml = await baselineTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var baselineRowVersion = ExtractHiddenInputValue(baselineTokenHtml, "RowVersion");
        var titleSeed = $"Stale Concurrency {Guid.NewGuid():N}";
        var emailTemplateSeed = $"Smoke email transport {Guid.NewGuid():N} {{test_target}}";

        using var baselineResponse = await SendHtmxPostAsync(
            client,
            editorPath,
            AddRequestVerificationToken(
                BuildValidSiteSettingsForm(
                    ExtractHiddenInputValue(baselineTokenHtml, "Id"),
                    baselineRowVersion,
                    $"{titleSeed} phase 1",
                    emailTemplateSeed),
                baselineTokenHtml));
        baselineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        baselineResponse.Headers.TryGetValues("HX-Redirect", out var baselineRedirectValues)
            .Should().BeTrue();
        baselineRedirectValues!.Single().Should().Contain("/SiteSettings/Edit");

        using var staleTokenResponse = await SendHtmxGetAsync(client, editorPath);
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleTokenHtml = await staleTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(
            client,
            editorPath,
            AddRequestVerificationToken(
                BuildValidSiteSettingsForm(
                    ExtractHiddenInputValue(staleTokenHtml, "Id"),
                    baselineRowVersion,
                    $"{titleSeed} phase 2",
                    emailTemplateSeed),
                staleTokenHtml));

        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponseHtml.Should().MatchRegex("Concurrency conflict|Parallelitaetskonflikt");
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleCspValues).Should().BeTrue();
        staleCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedShippingMethodEdit_WithMissingRowVersion_ShouldShowValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var name = $"Smoke Shipping {suffix}";
        var carrier = $"SmokeCarrier{suffix}";
        var service = $"SmokeService{suffix}";

        var methodId = await CreateShippingMethodAndGetIdAsync(client, name, carrier, service);
        var editorPath = $"/ShippingMethods/Edit?id={methodId}";

        using var tokenResponse = await SendHtmxGetAsync(client, editorPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(client, "/ShippingMethods/Edit", AddRequestVerificationToken(
            BuildShippingMethodEditPayload(
                methodId.ToString(),
                string.Empty,
                $"{name} - missing row-version",
                carrier,
                $"{service} 2"),
            tokenHtml));
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedShippingMethodEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var name = $"Smoke Shipping {suffix}";
        var carrier = $"SmokeCarrier{suffix}";
        var service = $"SmokeService{suffix}";

        var methodId = await CreateShippingMethodAndGetIdAsync(client, name, carrier, service);
        var editorPath = $"/ShippingMethods/Edit?id={methodId}";

        using var baselineEditResponse = await SendHtmxGetAsync(client, editorPath);
        baselineEditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineEditHtml = await baselineEditResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var baselineRowVersion = ExtractHiddenInputValue(baselineEditHtml, "RowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/ShippingMethods/Edit",
            AddRequestVerificationToken(
                BuildShippingMethodEditPayload(
                    methodId.ToString(),
                    baselineRowVersion,
                    $"{name} - first",
                    carrier,
                    $"{service} 1"),
                baselineEditHtml));

        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var firstRedirectValues)
            .Should().BeTrue();
        firstRedirectValues!.Single().Should().Contain("/ShippingMethods/Edit");

        using var staleTokenResponse = await SendHtmxGetAsync(client, editorPath);
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleTokenHtml = await staleTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/ShippingMethods/Edit",
            AddRequestVerificationToken(
                BuildShippingMethodEditPayload(
                    methodId.ToString(),
                    baselineRowVersion,
                    $"{name} - stale",
                    carrier,
                    $"{service} 2"),
                staleTokenHtml));

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues)
            .Should().BeTrue();
        var staleRedirectUrl = staleRedirectValues!.Single();
        staleRedirectUrl.Should().Contain("/ShippingMethods/Edit");

        using var staleEditResponse = await SendHtmxGetAsync(client, staleRedirectUrl);
        var staleEditHtml = await staleEditResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        staleEditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleEditHtml.Should().Contain("Concurrency conflict. Reload and try again.");
    }

    [Fact]
    public async Task AuthenticatedLoyaltyAccountSuspend_WithMissingRowVersion_ShouldSurfaceFailureMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        var accountId = await CreateLoyaltyAccountAndGetIdAsync(
            client,
            WebAdminTestFactory.TestLoyaltyProgramBusinessId,
            WebAdminTestFactory.TestMemberUserId);

        using var tokenResponse = await SendHtmxGetAsync(client, $"/Loyalty/AccountDetails?id={accountId}");
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(client, "/Loyalty/SuspendAccount", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
            ["id"] = accountId.ToString(),
            ["rowVersion"] = string.Empty
        });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("Failed to suspend loyalty account.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedLoyaltyAccountActivate_WithMissingRowVersion_ShouldSurfaceFailureMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var accountId = await CreateLoyaltyAccountAndGetIdAsync(
            client,
            WebAdminTestFactory.TestLoyaltyProgramBusinessId,
            WebAdminTestFactory.TestLifecycleUserId);
        var detailsPath = $"/Loyalty/AccountDetails?id={accountId}";

        using var detailsResponse = await SendHtmxGetAsync(client, detailsPath);
        detailsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detailsHtml = await detailsResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var suspendResponse = await SendHtmxPostAsync(
            client,
            "/Loyalty/SuspendAccount",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(detailsHtml),
                ["id"] = accountId.ToString(),
                ["rowVersion"] = ExtractHiddenInputValue(detailsHtml, "rowVersion")
            });
        suspendResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        suspendResponse.Headers.TryGetValues("HX-Redirect", out var suspendRedirectValues).Should().BeTrue();
        suspendRedirectValues!.Single().Should().Contain("/Loyalty/AccountDetails");

        using var refreshedDetailsResponse = await SendHtmxGetAsync(client, detailsPath);
        var refreshedDetailsHtml = await refreshedDetailsResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var activateResponse = await SendHtmxPostAsync(
            client,
            "/Loyalty/ActivateAccount",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(refreshedDetailsHtml),
                ["id"] = accountId.ToString(),
                ["rowVersion"] = string.Empty
            });
        var activateResponseHtml = await activateResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        activateResponseHtml.Should().Contain("Failed to activate loyalty account.");
        activateResponse.Headers.TryGetValues("Content-Security-Policy", out var activateCspValues).Should().BeTrue();
        activateCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedLoyaltyAccountAdjustPoints_WithMissingRowVersion_ShouldSurfaceValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var accountId = await CreateLoyaltyAccountAndGetIdAsync(
            client,
            WebAdminTestFactory.TestLoyaltyProgramBusinessId,
            WebAdminTestFactory.TestMemberUserId);
        var adjustPath = $"/Loyalty/AdjustPoints?loyaltyAccountId={accountId}";

        using var tokenResponse = await SendHtmxGetAsync(client, adjustPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(client, "/Loyalty/AdjustPoints", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
            ["LoyaltyAccountId"] = accountId.ToString(),
            ["BusinessId"] = ExtractHiddenInputValue(tokenHtml, "BusinessId"),
            ["UserId"] = ExtractHiddenInputValue(tokenHtml, "UserId"),
            ["AccountLabel"] = ExtractHiddenInputValue(tokenHtml, "AccountLabel"),
            ["RowVersion"] = string.Empty,
            ["PointsDelta"] = "1",
            ["Reason"] = "Smoke loyalty adjustment",
            ["Reference"] = "Smoke reference"
        });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("Failed to adjust loyalty points.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedLoyaltyCampaignActivation_WithMissingRowVersion_ShouldSurfaceFailure()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var businessId = WebAdminTestFactory.TestLoyaltyProgramBusinessId;
        var campaignName = $"Smoke Loyalty Campaign {Guid.NewGuid():N}";
        var campaignTitle = $"Smoke Campaign Title {Guid.NewGuid():N}";

        var campaignId = await CreateLoyaltyCampaignAndGetIdAsync(
            client,
            businessId,
            campaignName,
            campaignTitle,
            "/loyalty");

        using var listResponse = await SendHtmxGetAsync(client, $"/Loyalty/Campaigns?businessId={businessId}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listHtml = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(client, "/Loyalty/SetCampaignActivation", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(listHtml),
            ["id"] = campaignId.ToString(),
            ["businessId"] = businessId.ToString(),
            ["isActive"] = "true",
            ["rowVersion"] = string.Empty
        });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var redirectValues).Should().BeTrue();
        var redirectPath = redirectValues!.Single();
        using var redirectedResponse = await SendHtmxGetAsync(client, redirectPath);
        redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var redirectedHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        redirectedHtml.Should().Contain("Failed to update loyalty campaign activation.");
        responseHtml.Should().NotBeNullOrWhiteSpace();
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedLoyaltyCampaignActivation_WithStaleRowVersion_ShouldSurfaceFailureMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var businessId = WebAdminTestFactory.TestLoyaltyProgramBusinessId;
        var campaignName = $"Smoke Loyalty Campaign {Guid.NewGuid():N}";
        var campaignTitle = $"Smoke Campaign Title {Guid.NewGuid():N}";
        var campaignId = await CreateLoyaltyCampaignAndGetIdAsync(
            client,
            businessId,
            campaignName,
            campaignTitle,
            "/loyalty");

        var editPath = $"/Loyalty/EditCampaign?id={campaignId}&businessId={businessId}";
        using var editResponse = await SendHtmxGetAsync(client, editPath);
        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var editHtml = await editResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var baselineRowVersion = ExtractHiddenInputValue(editHtml, "RowVersion");
        var campaignIsActive = editHtml.Contains("name=\"IsActive\" checked", StringComparison.Ordinal);
        var staleTargetActive = (!campaignIsActive).ToString().ToLowerInvariant();

        using var firstResponse = await SendHtmxPostAsync(
            client,
            "/Loyalty/SetCampaignActivation",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(editHtml),
                ["id"] = campaignId.ToString(),
                ["businessId"] = businessId.ToString(),
                ["isActive"] = staleTargetActive,
                ["rowVersion"] = baselineRowVersion
            });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstResponse.Headers.TryGetValues("HX-Redirect", out var firstRedirectValues).Should().BeTrue();
        firstRedirectValues!.Single().Should().Contain("/Loyalty/Campaigns");

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Loyalty/SetCampaignActivation",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(editHtml),
                ["id"] = campaignId.ToString(),
                ["businessId"] = businessId.ToString(),
                ["isActive"] = staleTargetActive,
                ["rowVersion"] = baselineRowVersion
            });
        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues).Should().BeTrue();
        using var staleRedirectResponse = await SendHtmxGetAsync(client, staleRedirectValues!.Single());
        staleRedirectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleRedirectHtml = await staleRedirectResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        (
            staleRedirectHtml.Contains("Failed to update loyalty campaign activation.", StringComparison.Ordinal) ||
            staleRedirectHtml.Contains("Concurrency", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleCspValues).Should().BeTrue();
        staleCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedLoyaltyCampaignDeliveryStatus_WithMissingRowVersion_ShouldSurfaceFailureMessage()
    {
        var businessId = WebAdminTestFactory.TestLoyaltyProgramBusinessId;
        var campaignId = Guid.Parse("33333333-3333-3333-3333-333333333330");
        var deliveryId = Guid.Parse("33333333-3333-3333-3333-333333333331");

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                SeedCampaignAndDeliveryForStatusActionTests(db, businessId, campaignId, deliveryId);
            });

        using var listResponse = await SendHtmxGetAsync(
            client,
            $"/Loyalty/CampaignDeliveries?businessId={businessId}&campaignId={campaignId}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listHtml = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/Loyalty/UpdateCampaignDeliveryStatus",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(listHtml),
                ["id"] = deliveryId.ToString(),
                ["businessId"] = businessId.ToString(),
                ["campaignId"] = campaignId.ToString(),
                ["status"] = ((int)global::Darwin.Domain.Enums.CampaignDeliveryStatus.Succeeded).ToString(),
                ["rowVersion"] = string.Empty,
                ["page"] = "1",
                ["pageSize"] = "20",
                ["filter"] = "All"
            });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var redirectValues).Should().BeTrue();
        var redirectUrl = redirectValues!.Single();
        using var redirectedResponse = await SendHtmxGetAsync(client, redirectUrl);
        var redirectedHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        redirectedHtml.Should().Contain("Campaign delivery status could not be updated.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedLoyaltyCampaignDeliveryStatus_WithStaleRowVersion_ShouldSurfaceFailureMessage()
    {
        var businessId = WebAdminTestFactory.TestLoyaltyProgramBusinessId;
        var campaignId = Guid.Parse("33333333-3333-3333-3333-333333333332");
        var deliveryId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                SeedCampaignAndDeliveryForStatusActionTests(db, businessId, campaignId, deliveryId);
            });

        using var listResponse = await SendHtmxGetAsync(
            client,
            $"/Loyalty/CampaignDeliveries?businessId={businessId}&campaignId={campaignId}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listHtml = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(listHtml, "rowVersion");

        using var firstResponse = await SendHtmxPostAsync(
            client,
            "/Loyalty/UpdateCampaignDeliveryStatus",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(listHtml),
                ["id"] = deliveryId.ToString(),
                ["businessId"] = businessId.ToString(),
                ["campaignId"] = campaignId.ToString(),
                ["status"] = ((int)global::Darwin.Domain.Enums.CampaignDeliveryStatus.Succeeded).ToString(),
                ["rowVersion"] = staleRowVersion,
                ["page"] = "1",
                ["pageSize"] = "20",
                ["filter"] = "All"
            });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstResponse.Headers.TryGetValues("HX-Redirect", out var firstRedirectValues).Should().BeTrue();
        using var firstRedirectResponse = await SendHtmxGetAsync(client, firstRedirectValues!.Single());
        firstRedirectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstRedirectHtml = await firstRedirectResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        firstRedirectHtml.Should().Contain("Campaign delivery status was updated.");

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Loyalty/UpdateCampaignDeliveryStatus",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(firstRedirectHtml),
                ["id"] = deliveryId.ToString(),
                ["businessId"] = businessId.ToString(),
                ["campaignId"] = campaignId.ToString(),
                ["status"] = ((int)global::Darwin.Domain.Enums.CampaignDeliveryStatus.Failed).ToString(),
                ["rowVersion"] = staleRowVersion,
                ["page"] = "1",
                ["pageSize"] = "20",
                ["filter"] = "All"
            });
        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues).Should().BeTrue();
        using var staleRedirectResponse = await SendHtmxGetAsync(client, staleRedirectValues!.Single());
        staleRedirectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleRedirectHtml = await staleRedirectResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        staleRedirectHtml.Should().Contain("Campaign delivery status could not be updated.");
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleCspValues).Should().BeTrue();
        staleCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedLoyaltyProgramEdit_WithMissingRowVersion_ShouldSurfaceFailureMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var businessId = WebAdminTestFactory.TestLoyaltyProgramBusinessId;
        var loyaltyProgramName = $"Smoke Loyalty Program {Guid.NewGuid():N}";
        var programId = await CreateLoyaltyProgramAndGetIdAsync(client, businessId, loyaltyProgramName);
        var editPath = $"/Loyalty/EditProgram?id={programId}";

        using var tokenResponse = await SendHtmxGetAsync(client, editPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/Loyalty/EditProgram",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["Id"] = programId.ToString(),
                ["BusinessId"] = businessId.ToString(),
                ["Name"] = $"Edited {loyaltyProgramName}",
                ["IsActive"] = "true",
                ["AccrualMode"] = "PerVisit",
                ["PointsPerCurrencyUnit"] = "1",
                ["RulesJson"] = "{}",
                ["RowVersion"] = string.Empty
            });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("Unable to update the loyalty program right now.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedLoyaltyProgramEdit_WithStaleRowVersion_ShouldSurfaceFailureMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var businessId = WebAdminTestFactory.TestLoyaltyProgramBusinessId;
        var loyaltyProgramName = $"Smoke Loyalty Program {Guid.NewGuid():N}";
        var programId = await CreateLoyaltyProgramAndGetIdAsync(client, businessId, loyaltyProgramName);
        var editPath = $"/Loyalty/EditProgram?id={programId}";

        using var baselineResponse = await SendHtmxGetAsync(client, editPath);
        baselineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineHtml = await baselineResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineHtml, "RowVersion");

        using var firstResponse = await SendHtmxPostAsync(
            client,
            "/Loyalty/EditProgram",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(baselineHtml),
                ["Id"] = programId.ToString(),
                ["BusinessId"] = businessId.ToString(),
                ["Name"] = $"Edited {loyaltyProgramName} v1",
                ["IsActive"] = "true",
                ["AccrualMode"] = "PerVisit",
                ["PointsPerCurrencyUnit"] = "1",
                ["RulesJson"] = "{}",
                ["RowVersion"] = staleRowVersion
            });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstResponse.Headers.TryGetValues("HX-Redirect", out var firstRedirectValues).Should().BeTrue();
        firstRedirectValues!.Single().Should().Contain("/Loyalty/EditProgram");

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Loyalty/EditProgram",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(baselineHtml),
                ["Id"] = programId.ToString(),
                ["BusinessId"] = businessId.ToString(),
                ["Name"] = $"Edited {loyaltyProgramName} v2",
                ["IsActive"] = "true",
                ["AccrualMode"] = "PerVisit",
                ["PointsPerCurrencyUnit"] = "1",
                ["RulesJson"] = "{}",
                ["RowVersion"] = staleRowVersion
            });
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        if (staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues))
        {
            using var staleRedirectResponse = await SendHtmxGetAsync(client, staleRedirectValues.Single());
            staleResponseHtml = await staleRedirectResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        (
            staleResponseHtml.Contains("Unable to update the loyalty program right now.", StringComparison.OrdinalIgnoreCase) ||
            staleResponseHtml.Contains("Concurrency", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleCspValues).Should().BeTrue();
        staleCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedLoyaltyRewardTierEdit_WithMissingRowVersion_ShouldSurfaceFailureMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var businessId = WebAdminTestFactory.TestLoyaltyProgramBusinessId;
        var loyaltyProgramName = $"Smoke Loyalty Program {Guid.NewGuid():N}";
        var rewardDescription = $"Smoke reward tier {Guid.NewGuid():N}";
        var programId = await CreateLoyaltyProgramAndGetIdAsync(client, businessId, loyaltyProgramName);
        var rewardTierId = await CreateLoyaltyRewardTierAndGetIdAsync(
            client,
            programId,
            businessId,
            loyaltyProgramName,
            rewardDescription);
        var editPath = $"/Loyalty/EditRewardTier?id={rewardTierId}&loyaltyProgramId={programId}";

        using var tokenResponse = await SendHtmxGetAsync(client, editPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/Loyalty/EditRewardTier",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["Id"] = rewardTierId.ToString(),
                ["LoyaltyProgramId"] = programId.ToString(),
                ["BusinessId"] = businessId.ToString(),
                ["ProgramName"] = loyaltyProgramName,
                ["PointsRequired"] = "100",
                ["RewardType"] = "FreeItem",
                ["RewardValue"] = string.Empty,
                ["Description"] = rewardDescription,
                ["AllowSelfRedemption"] = "true",
                ["MetadataJson"] = "{}",
                ["RowVersion"] = string.Empty
            });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("Unable to update the reward tier right now.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedLoyaltyRewardTierEdit_WithStaleRowVersion_ShouldSurfaceFailureMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var businessId = WebAdminTestFactory.TestLoyaltyProgramBusinessId;
        var loyaltyProgramName = $"Smoke Loyalty Program {Guid.NewGuid():N}";
        var rewardDescription = $"Smoke reward tier {Guid.NewGuid():N}";
        var programId = await CreateLoyaltyProgramAndGetIdAsync(client, businessId, loyaltyProgramName);
        var rewardTierId = await CreateLoyaltyRewardTierAndGetIdAsync(
            client,
            programId,
            businessId,
            loyaltyProgramName,
            rewardDescription);
        var editPath = $"/Loyalty/EditRewardTier?id={rewardTierId}&loyaltyProgramId={programId}";

        using var baselineResponse = await SendHtmxGetAsync(client, editPath);
        baselineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineHtml = await baselineResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineHtml, "RowVersion");

        using var firstResponse = await SendHtmxPostAsync(
            client,
            "/Loyalty/EditRewardTier",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(baselineHtml),
                ["Id"] = rewardTierId.ToString(),
                ["LoyaltyProgramId"] = programId.ToString(),
                ["BusinessId"] = businessId.ToString(),
                ["ProgramName"] = loyaltyProgramName,
                ["PointsRequired"] = "100",
                ["RewardType"] = "FreeItem",
                ["RewardValue"] = string.Empty,
                ["Description"] = $"{rewardDescription} updated",
                ["AllowSelfRedemption"] = "true",
                ["MetadataJson"] = "{}",
                ["RowVersion"] = staleRowVersion
            });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstResponse.Headers.TryGetValues("HX-Redirect", out var firstRedirectValues).Should().BeTrue();
        firstRedirectValues!.Single().Should().Contain("/Loyalty/EditRewardTier");

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Loyalty/EditRewardTier",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(baselineHtml),
                ["Id"] = rewardTierId.ToString(),
                ["LoyaltyProgramId"] = programId.ToString(),
                ["BusinessId"] = businessId.ToString(),
                ["ProgramName"] = loyaltyProgramName,
                ["PointsRequired"] = "100",
                ["RewardType"] = "FreeItem",
                ["RewardValue"] = string.Empty,
                ["Description"] = $"{rewardDescription} stale",
                ["AllowSelfRedemption"] = "false",
                ["MetadataJson"] = "{}",
                ["RowVersion"] = staleRowVersion
            });
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        if (staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues))
        {
            using var staleRedirectResponse = await SendHtmxGetAsync(client, staleRedirectValues.Single());
            staleResponseHtml = await staleRedirectResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        (
            staleResponseHtml.Contains("Unable to update the reward tier right now.", StringComparison.OrdinalIgnoreCase) ||
            staleResponseHtml.Contains("Concurrency", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleCspValues).Should().BeTrue();
        staleCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedMediaAssetEdit_WithMissingRowVersion_ShouldShowValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var editPath = $"/Media/Edit?id={WebAdminTestFactory.TestMediaAssetId}";

        using var tokenResponse = await SendHtmxGetAsync(client, editPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(client, "/Media/Edit", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
            ["Id"] = WebAdminTestFactory.TestMediaAssetId.ToString(),
            ["RowVersion"] = string.Empty,
            ["Url"] = ExtractHiddenInputValue(tokenHtml, "Url"),
            ["OriginalFileName"] = ExtractHiddenInputValue(tokenHtml, "OriginalFileName"),
            ["SizeBytes"] = ExtractHiddenInputValue(tokenHtml, "SizeBytes"),
            ["Width"] = ExtractHiddenInputValue(tokenHtml, "Width"),
            ["Height"] = ExtractHiddenInputValue(tokenHtml, "Height"),
            ["Alt"] = "Seeded alt",
            ["Title"] = "Seeded title",
            ["Role"] = "LibraryAsset"
        });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedMediaAssetEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var editPath = $"/Media/Edit?id={WebAdminTestFactory.TestMediaAssetId}";

        using var baselineResponse = await SendHtmxGetAsync(client, editPath);
        baselineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineHtml = await baselineResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineHtml, "RowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Media/Edit",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(baselineHtml),
                ["Id"] = WebAdminTestFactory.TestMediaAssetId.ToString(),
                ["RowVersion"] = staleRowVersion,
                ["Url"] = ExtractHiddenInputValue(baselineHtml, "Url"),
                ["OriginalFileName"] = ExtractHiddenInputValue(baselineHtml, "OriginalFileName"),
                ["SizeBytes"] = ExtractHiddenInputValue(baselineHtml, "SizeBytes"),
                ["Width"] = ExtractHiddenInputValue(baselineHtml, "Width"),
                ["Height"] = ExtractHiddenInputValue(baselineHtml, "Height"),
                ["Alt"] = "Seeded alt (version 1)",
                ["Title"] = "Seeded title v1",
                ["Role"] = "LibraryAsset"
            });

        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var firstRedirectValues)
            .Should().BeTrue();
        firstRedirectValues!.Single().Should().Contain("/Media/Edit");

        using var staleTokenResponse = await SendHtmxGetAsync(client, editPath);
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleTokenHtml = await staleTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(client, "/Media/Edit", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(staleTokenHtml),
            ["Id"] = WebAdminTestFactory.TestMediaAssetId.ToString(),
            ["RowVersion"] = staleRowVersion,
            ["Url"] = ExtractHiddenInputValue(staleTokenHtml, "Url"),
            ["OriginalFileName"] = ExtractHiddenInputValue(staleTokenHtml, "OriginalFileName"),
            ["SizeBytes"] = ExtractHiddenInputValue(staleTokenHtml, "SizeBytes"),
            ["Width"] = ExtractHiddenInputValue(staleTokenHtml, "Width"),
            ["Height"] = ExtractHiddenInputValue(staleTokenHtml, "Height"),
            ["Alt"] = "Seeded alt (stale)",
            ["Title"] = "Seeded title stale",
            ["Role"] = "LibraryAssetReviewed"
        });

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues)
            .Should().BeTrue();
        var staleRedirectUrl = staleRedirectValues!.Single();
        staleRedirectUrl.Should().Contain("/Media/Edit");
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleCspValues).Should().BeTrue();
        staleCspValues!.Single().Should().Contain("form-action 'self'");

        using var staleEditResponse = await SendHtmxGetAsync(client, staleRedirectUrl);
        var staleEditHtml = await staleEditResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        staleEditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleEditHtml.Should().Contain("Concurrency conflict. Reload the media asset and try again.");
    }

    [Fact]
    public async Task AuthenticatedMediaAssetDelete_WithStaleRowVersion_ShouldSurfaceValidationMessageAndPreserveAsset()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var editPath = $"/Media/Edit?id={WebAdminTestFactory.TestMediaAssetId}";
        var staleMarker = $"media stale delete {Guid.NewGuid():N}";

        using var baselineTokenResponse = await SendHtmxGetAsync(client, editPath);
        baselineTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineTokenHtml = await baselineTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var baselineRowVersion = ExtractHiddenInputValue(baselineTokenHtml, "RowVersion");

        using var refreshResponse = await SendHtmxPostAsync(
            client,
            "/Media/Edit",
            AddRequestVerificationToken(
                BuildMediaAssetEditPayload(
                    WebAdminTestFactory.TestMediaAssetId.ToString(),
                    baselineRowVersion,
                    $"Seeded media before stale delete {staleMarker}",
                    "Alt before stale delete",
                    ExtractHiddenInputValue(baselineTokenHtml, "Url"),
                    ExtractHiddenInputValue(baselineTokenHtml, "OriginalFileName"),
                    ExtractHiddenInputValue(baselineTokenHtml, "SizeBytes"),
                    ExtractHiddenInputValue(baselineTokenHtml, "Width"),
                    ExtractHiddenInputValue(baselineTokenHtml, "Height"),
                    "LibraryAsset"),
                baselineTokenHtml));
        refreshResponse.Headers.TryGetValues("HX-Redirect", out var refreshRedirectValues)
            .Should().BeTrue();
        refreshRedirectValues!.Single().Should().Contain("/Media/Edit");

        using var staleDeleteSourceResponse = await SendHtmxGetAsync(client, editPath);
        staleDeleteSourceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleDeleteTokenHtml = await staleDeleteSourceResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleDeleteResponse = await SendHtmxPostAsync(client, "/Media/Delete", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(staleDeleteTokenHtml),
            ["id"] = WebAdminTestFactory.TestMediaAssetId.ToString(),
            ["rowVersion"] = baselineRowVersion
        });
        staleDeleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleDeleteResponse.Headers.TryGetValues("HX-Redirect", out var staleDeleteRedirectValues)
            .Should().BeTrue();
        staleDeleteRedirectValues!.Single().Should().Contain("/Media");
        staleDeleteResponse.Headers.TryGetValues("Content-Security-Policy", out var staleDeleteCspValues).Should().BeTrue();
        staleDeleteCspValues!.Single().Should().Contain("form-action 'self'");

        using var staleDeleteEditResponse = await SendHtmxGetAsync(client, editPath);
        staleDeleteEditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleDeleteEditHtml = await staleDeleteEditResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        staleDeleteEditHtml.Should().Contain($"Seeded media before stale delete {staleMarker}");
    }

    [Fact]
    public async Task AuthenticatedMediaAssetPurgeUnused_WithStaleRowVersion_ShouldRejectAndPreserveAsset()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var editPath = $"/Media/Edit?id={WebAdminTestFactory.TestMediaAssetId}";
        var staleMarker = $"media stale purge {Guid.NewGuid():N}";

        using var baselineTokenResponse = await SendHtmxGetAsync(client, editPath);
        baselineTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineTokenHtml = await baselineTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var baselineRowVersion = ExtractHiddenInputValue(baselineTokenHtml, "RowVersion");

        using var refreshResponse = await SendHtmxPostAsync(
            client,
            "/Media/Edit",
            AddRequestVerificationToken(
                BuildMediaAssetEditPayload(
                    WebAdminTestFactory.TestMediaAssetId.ToString(),
                    baselineRowVersion,
                    $"Seeded media before stale purge {staleMarker}",
                    "Alt before stale purge",
                    ExtractHiddenInputValue(baselineTokenHtml, "Url"),
                    ExtractHiddenInputValue(baselineTokenHtml, "OriginalFileName"),
                    ExtractHiddenInputValue(baselineTokenHtml, "SizeBytes"),
                    ExtractHiddenInputValue(baselineTokenHtml, "Width"),
                    ExtractHiddenInputValue(baselineTokenHtml, "Height"),
                    "LibraryAssetReviewed"),
                baselineTokenHtml));
        refreshResponse.Headers.TryGetValues("HX-Redirect", out var refreshRedirectValues)
            .Should().BeTrue();
        refreshRedirectValues!.Single().Should().Contain("/Media/Edit");

        using var stalePurgeSourceResponse = await SendHtmxGetAsync(client, editPath);
        stalePurgeSourceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var stalePurgeTokenHtml = await stalePurgeSourceResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var stalePurgeResponse = await SendHtmxPostAsync(client, "/Media/PurgeUnused", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(stalePurgeTokenHtml),
            ["id"] = WebAdminTestFactory.TestMediaAssetId.ToString(),
            ["rowVersion"] = baselineRowVersion
        });
        stalePurgeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        stalePurgeResponse.Headers.TryGetValues("HX-Redirect", out var stalePurgeRedirectValues)
            .Should().BeTrue();
        stalePurgeRedirectValues!.Single().Should().Contain("/Media?filter=Unused");
        stalePurgeResponse.Headers.TryGetValues("Content-Security-Policy", out var stalePurgeCspValues).Should().BeTrue();
        stalePurgeCspValues!.Single().Should().Contain("form-action 'self'");

        using var stalePurgeEditResponse = await SendHtmxGetAsync(client, editPath);
        stalePurgeEditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var stalePurgeEditHtml = await stalePurgeEditResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        stalePurgeEditHtml.Should().Contain($"Seeded media before stale purge {staleMarker}");
    }

    [Fact]
    public async Task AuthenticatedMediaAssetDelete_WithMissingRowVersion_ShouldShowValidationErrorAndPreserveAsset()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var editPath = $"/Media/Edit?id={WebAdminTestFactory.TestMediaAssetId}";
        var missingMarker = $"media missing delete {Guid.NewGuid():N}";

        using var baselineTokenResponse = await SendHtmxGetAsync(client, editPath);
        baselineTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineTokenHtml = await baselineTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var baselineRowVersion = ExtractHiddenInputValue(baselineTokenHtml, "RowVersion");

        using var refreshResponse = await SendHtmxPostAsync(
            client,
            "/Media/Edit",
            AddRequestVerificationToken(
                BuildMediaAssetEditPayload(
                    WebAdminTestFactory.TestMediaAssetId.ToString(),
                    baselineRowVersion,
                    $"Seeded media before missing delete {missingMarker}",
                    "Alt before missing delete",
                    ExtractHiddenInputValue(baselineTokenHtml, "Url"),
                    ExtractHiddenInputValue(baselineTokenHtml, "OriginalFileName"),
                    ExtractHiddenInputValue(baselineTokenHtml, "SizeBytes"),
                    ExtractHiddenInputValue(baselineTokenHtml, "Width"),
                    ExtractHiddenInputValue(baselineTokenHtml, "Height"),
                    "LibraryAsset"),
                baselineTokenHtml));
        refreshResponse.Headers.TryGetValues("HX-Redirect", out var refreshRedirectValues)
            .Should().BeTrue();
        refreshRedirectValues!.Single().Should().Contain("/Media/Edit");

        using var missingDeleteSourceResponse = await SendHtmxGetAsync(client, editPath);
        missingDeleteSourceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var missingDeleteTokenHtml = await missingDeleteSourceResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var missingDeleteResponse = await SendHtmxPostAsync(client, "/Media/Delete", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(missingDeleteTokenHtml),
            ["id"] = WebAdminTestFactory.TestMediaAssetId.ToString(),
            ["rowVersion"] = string.Empty
        });
        var missingDeleteResponseHtml = await missingDeleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        missingDeleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        missingDeleteResponseHtml.Should().Contain("RowVersion is required.");
        missingDeleteResponse.Headers.TryGetValues("Content-Security-Policy", out var missingDeleteCspValues).Should().BeTrue();
        missingDeleteCspValues!.Single().Should().Contain("form-action 'self'");

        using var preservedAfterDeleteResponse = await SendHtmxGetAsync(client, editPath);
        preservedAfterDeleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var preservedAfterDeleteHtml = await preservedAfterDeleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        preservedAfterDeleteHtml.Should().Contain($"Seeded media before missing delete {missingMarker}");
    }

    [Fact]
    public async Task AuthenticatedMediaAssetPurgeUnused_WithMissingRowVersion_ShouldShowValidationErrorAndPreserveAsset()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var editPath = $"/Media/Edit?id={WebAdminTestFactory.TestMediaAssetId}";
        var missingMarker = $"media missing purge {Guid.NewGuid():N}";

        using var baselineTokenResponse = await SendHtmxGetAsync(client, editPath);
        baselineTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineTokenHtml = await baselineTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var baselineRowVersion = ExtractHiddenInputValue(baselineTokenHtml, "RowVersion");

        using var refreshResponse = await SendHtmxPostAsync(
            client,
            "/Media/Edit",
            AddRequestVerificationToken(
                BuildMediaAssetEditPayload(
                    WebAdminTestFactory.TestMediaAssetId.ToString(),
                    baselineRowVersion,
                    $"Seeded media before missing purge {missingMarker}",
                    "Alt before missing purge",
                    ExtractHiddenInputValue(baselineTokenHtml, "Url"),
                    ExtractHiddenInputValue(baselineTokenHtml, "OriginalFileName"),
                    ExtractHiddenInputValue(baselineTokenHtml, "SizeBytes"),
                    ExtractHiddenInputValue(baselineTokenHtml, "Width"),
                    ExtractHiddenInputValue(baselineTokenHtml, "Height"),
                    "LibraryAssetReviewed"),
                baselineTokenHtml));
        refreshResponse.Headers.TryGetValues("HX-Redirect", out var refreshRedirectValues)
            .Should().BeTrue();
        refreshRedirectValues!.Single().Should().Contain("/Media/Edit");

        using var missingPurgeSourceResponse = await SendHtmxGetAsync(client, editPath);
        missingPurgeSourceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var missingPurgeTokenHtml = await missingPurgeSourceResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var missingPurgeResponse = await SendHtmxPostAsync(client, "/Media/PurgeUnused", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(missingPurgeTokenHtml),
            ["id"] = WebAdminTestFactory.TestMediaAssetId.ToString(),
            ["rowVersion"] = string.Empty
        });
        var missingPurgeResponseHtml = await missingPurgeResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        missingPurgeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        missingPurgeResponseHtml.Should().Contain("RowVersion is required.");
        missingPurgeResponse.Headers.TryGetValues("Content-Security-Policy", out var missingPurgeCspValues).Should().BeTrue();
        missingPurgeCspValues!.Single().Should().Contain("form-action 'self'");

        using var preservedAfterPurgeResponse = await SendHtmxGetAsync(client, editPath);
        preservedAfterPurgeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var preservedAfterPurgeHtml = await preservedAfterPurgeResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        preservedAfterPurgeHtml.Should().Contain($"Seeded media before missing purge {missingMarker}");
    }

    [Fact]
    public async Task AuthenticatedBrandEdit_WithMissingRowVersion_ShouldShowValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var editPath = $"/Brands/Edit?id={WebAdminTestFactory.TestBrandId}";

        using var tokenResponse = await SendHtmxGetAsync(client, editPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/Brands/Edit",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["Id"] = WebAdminTestFactory.TestBrandId.ToString(),
                ["RowVersion"] = string.Empty,
                ["Slug"] = $"smoke-brand-{Guid.NewGuid():N}",
                ["LogoMediaId"] = ExtractHiddenInputValue(tokenHtml, "LogoMediaId"),
                ["Translations[0].Culture"] = ExtractHiddenInputValue(tokenHtml, "Translations[0].Culture"),
                ["Translations[0].Name"] = $"Smoke Brand {Guid.NewGuid():N}",
                ["Translations[0].DescriptionHtml"] = "<p>Smoke brand row-version coverage.</p>"
            });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedBrandEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var editPath = $"/Brands/Edit?id={WebAdminTestFactory.TestBrandId}";

        using var baselineTokenResponse = await SendHtmxGetAsync(client, editPath);
        baselineTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineTokenHtml = await baselineTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineTokenHtml, "RowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Brands/Edit",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(baselineTokenHtml),
                ["Id"] = WebAdminTestFactory.TestBrandId.ToString(),
                ["RowVersion"] = staleRowVersion,
                ["Slug"] = $"smoke-brand-{Guid.NewGuid():N}-v1",
                ["LogoMediaId"] = ExtractHiddenInputValue(baselineTokenHtml, "LogoMediaId"),
                ["Translations[0].Culture"] = ExtractHiddenInputValue(baselineTokenHtml, "Translations[0].Culture"),
                ["Translations[0].Name"] = $"Smoke Brand {Guid.NewGuid():N} v1",
                ["Translations[0].DescriptionHtml"] = "<p>Smoke brand stale step one.</p>"
            });
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var firstRedirectValues).Should().BeTrue();
        firstRedirectValues!.Single().Should().Contain("/Brands/Edit");

        using var staleTokenResponse = await SendHtmxGetAsync(client, editPath);
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleTokenHtml = await staleTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Brands/Edit",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(staleTokenHtml),
                ["Id"] = WebAdminTestFactory.TestBrandId.ToString(),
                ["RowVersion"] = staleRowVersion,
                ["Slug"] = $"smoke-brand-{Guid.NewGuid():N}-v2",
                ["LogoMediaId"] = ExtractHiddenInputValue(staleTokenHtml, "LogoMediaId"),
                ["Translations[0].Culture"] = ExtractHiddenInputValue(staleTokenHtml, "Translations[0].Culture"),
                ["Translations[0].Name"] = $"Smoke Brand {Guid.NewGuid():N} v2",
                ["Translations[0].DescriptionHtml"] = "<p>Smoke brand stale step two.</p>"
            });
        _ = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues).Should().BeTrue();
        staleRedirectValues!.Single().Should().Contain("/Brands/Edit");
        using var staleEditorResponse = await SendHtmxGetAsync(client, staleRedirectValues!.Single());
        var staleEditorHtml = await staleEditorResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        staleEditorHtml.Should().Contain("Concurrency conflict. The brand has been modified by another process.");
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleResponseCspValues).Should().BeTrue();
        staleResponseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedCategoryEdit_WithMissingRowVersion_ShouldShowValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var editPath = $"/Categories/Edit?id={WebAdminTestFactory.TestCategoryId}";

        using var tokenResponse = await SendHtmxGetAsync(client, editPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/Categories/Edit",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["Id"] = WebAdminTestFactory.TestCategoryId.ToString(),
                ["RowVersion"] = string.Empty,
                ["ParentId"] = ExtractHiddenInputValue(tokenHtml, "ParentId"),
                ["SortOrder"] = ExtractHiddenInputValue(tokenHtml, "SortOrder"),
                ["IsActive"] = ExtractHiddenInputValue(tokenHtml, "IsActive"),
                ["Translations[0].Culture"] = ExtractHiddenInputValue(tokenHtml, "Translations[0].Culture"),
                ["Translations[0].Name"] = $"Smoke Category {Guid.NewGuid():N}",
                ["Translations[0].Slug"] = $"smoke-category-{Guid.NewGuid():N}",
                ["Translations[0].Description"] = "Smoke coverage description.",
                ["Translations[0].MetaTitle"] = "Smoke category title",
                ["Translations[0].MetaDescription"] = "Smoke category meta"
            });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedCategoryEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var editPath = $"/Categories/Edit?id={WebAdminTestFactory.TestCategoryId}";

        using var baselineTokenResponse = await SendHtmxGetAsync(client, editPath);
        baselineTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineTokenHtml = await baselineTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineTokenHtml, "RowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Categories/Edit",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(baselineTokenHtml),
                ["Id"] = WebAdminTestFactory.TestCategoryId.ToString(),
                ["RowVersion"] = staleRowVersion,
                ["ParentId"] = ExtractHiddenInputValue(baselineTokenHtml, "ParentId"),
                ["SortOrder"] = ExtractHiddenInputValue(baselineTokenHtml, "SortOrder"),
                ["IsActive"] = ExtractHiddenInputValue(baselineTokenHtml, "IsActive"),
                ["Translations[0].Culture"] = ExtractHiddenInputValue(baselineTokenHtml, "Translations[0].Culture"),
                ["Translations[0].Name"] = $"Smoke Category {Guid.NewGuid():N} v1",
                ["Translations[0].Slug"] = $"smoke-category-{Guid.NewGuid():N}-v1",
                ["Translations[0].Description"] = "Smoke coverage description.",
                ["Translations[0].MetaTitle"] = "Smoke category title",
                ["Translations[0].MetaDescription"] = "Smoke category meta"
            });
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var firstRedirectValues).Should().BeTrue();
        firstRedirectValues!.Single().Should().Contain("/Categories/Edit");

        using var staleTokenResponse = await SendHtmxGetAsync(client, editPath);
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleTokenHtml = await staleTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Categories/Edit",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(staleTokenHtml),
                ["Id"] = WebAdminTestFactory.TestCategoryId.ToString(),
                ["RowVersion"] = staleRowVersion,
                ["ParentId"] = ExtractHiddenInputValue(staleTokenHtml, "ParentId"),
                ["SortOrder"] = ExtractHiddenInputValue(staleTokenHtml, "SortOrder"),
                ["IsActive"] = ExtractHiddenInputValue(staleTokenHtml, "IsActive"),
                ["Translations[0].Culture"] = ExtractHiddenInputValue(staleTokenHtml, "Translations[0].Culture"),
                ["Translations[0].Name"] = $"Smoke Category {Guid.NewGuid():N} v2",
                ["Translations[0].Slug"] = $"smoke-category-{Guid.NewGuid():N}-v2",
                ["Translations[0].Description"] = "Smoke coverage description updated.",
                ["Translations[0].MetaTitle"] = "Smoke category title updated",
                ["Translations[0].MetaDescription"] = "Smoke category meta updated"
            });
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponseHtml.Should().Contain("The category was modified by another user.");
        staleResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeFalse();
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleResponseCspValues).Should().BeTrue();
        staleResponseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedProductEdit_WithMissingRowVersion_ShouldShowValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var editPath = $"/Products/Edit?id={WebAdminTestFactory.TestProductId}";

        using var tokenResponse = await SendHtmxGetAsync(client, editPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/Products/Edit",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["Id"] = WebAdminTestFactory.TestProductId.ToString(),
                ["RowVersion"] = string.Empty,
                ["BrandId"] = WebAdminTestFactory.TestBrandId.ToString(),
                ["PrimaryCategoryId"] = WebAdminTestFactory.TestCategoryId.ToString(),
                ["Kind"] = "Simple",
                ["Translations.Index"] = "0",
                ["Translations[0].Culture"] = "de-DE",
                ["Translations[0].Name"] = $"Smoke Product {Guid.NewGuid():N}",
                ["Translations[0].Slug"] = $"smoke-product-{Guid.NewGuid():N}",
                ["Translations[0].ShortDescription"] = "Smoke product row-version coverage.",
                ["Translations[0].FullDescriptionHtml"] = "<p>Smoke product row-version coverage.</p>",
                ["Translations[0].MetaTitle"] = "Smoke product title",
                ["Translations[0].MetaDescription"] = "Smoke product meta",
                ["Translations[0].SearchKeywords"] = "smoke,product,row-version",
                ["Variants.Index"] = "0",
                ["Variants[0].Id"] = WebAdminTestFactory.TestProductVariantId.ToString(),
                ["Variants[0].Sku"] = $"SMOKE-{Guid.NewGuid():N}",
                ["Variants[0].Currency"] = "EUR",
                ["Variants[0].TaxCategoryId"] = WebAdminTestFactory.TestTaxCategoryId.ToString(),
                ["Variants[0].BasePriceNetMinor"] = "1999",
                ["Variants[0].StockOnHand"] = "5",
                ["Variants[0].StockReserved"] = "0",
                ["Variants[0].BackorderAllowed"] = "false",
                ["Variants[0].IsDigital"] = "false"
            });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedProductEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var editPath = $"/Products/Edit?id={WebAdminTestFactory.TestProductId}";

        using var baselineTokenResponse = await SendHtmxGetAsync(client, editPath);
        baselineTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineTokenHtml = await baselineTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineTokenHtml, "RowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Products/Edit",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(baselineTokenHtml),
                ["Id"] = WebAdminTestFactory.TestProductId.ToString(),
                ["RowVersion"] = staleRowVersion,
                ["BrandId"] = WebAdminTestFactory.TestBrandId.ToString(),
                ["PrimaryCategoryId"] = WebAdminTestFactory.TestCategoryId.ToString(),
                ["Kind"] = "Simple",
                ["Translations.Index"] = "0",
                ["Translations[0].Culture"] = "de-DE",
                ["Translations[0].Name"] = $"Smoke Product {Guid.NewGuid():N} v1",
                ["Translations[0].Slug"] = $"smoke-product-{Guid.NewGuid():N}-v1",
                ["Translations[0].ShortDescription"] = "Smoke product stale coverage.",
                ["Translations[0].FullDescriptionHtml"] = "<p>Smoke product stale coverage v1.</p>",
                ["Translations[0].MetaTitle"] = "Smoke product title v1",
                ["Translations[0].MetaDescription"] = "Smoke product meta v1",
                ["Translations[0].SearchKeywords"] = "smoke,product,stale,v1",
                ["Variants.Index"] = "0",
                ["Variants[0].Id"] = WebAdminTestFactory.TestProductVariantId.ToString(),
                ["Variants[0].Sku"] = $"SMOKE-{Guid.NewGuid():N}v1",
                ["Variants[0].Currency"] = "EUR",
                ["Variants[0].TaxCategoryId"] = WebAdminTestFactory.TestTaxCategoryId.ToString(),
                ["Variants[0].BasePriceNetMinor"] = "1999",
                ["Variants[0].StockOnHand"] = "5",
                ["Variants[0].StockReserved"] = "0",
                ["Variants[0].BackorderAllowed"] = "false",
                ["Variants[0].IsDigital"] = "false"
            });
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();

        using var staleTokenResponse = await SendHtmxGetAsync(client, editPath);
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleTokenHtml = await staleTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Products/Edit",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(staleTokenHtml),
                ["Id"] = WebAdminTestFactory.TestProductId.ToString(),
                ["RowVersion"] = staleRowVersion,
                ["BrandId"] = WebAdminTestFactory.TestBrandId.ToString(),
                ["PrimaryCategoryId"] = WebAdminTestFactory.TestCategoryId.ToString(),
                ["Kind"] = "Simple",
                ["Translations.Index"] = "0",
                ["Translations[0].Culture"] = "de-DE",
                ["Translations[0].Name"] = $"Smoke Product {Guid.NewGuid():N} v2",
                ["Translations[0].Slug"] = $"smoke-product-{Guid.NewGuid():N}-v2",
                ["Translations[0].ShortDescription"] = "Smoke product stale coverage v2.",
                ["Translations[0].FullDescriptionHtml"] = "<p>Smoke product stale coverage v2.</p>",
                ["Translations[0].MetaTitle"] = "Smoke product title v2",
                ["Translations[0].MetaDescription"] = "Smoke product meta v2",
                ["Translations[0].SearchKeywords"] = "smoke,product,stale,v2",
                ["Variants.Index"] = "0",
                ["Variants[0].Id"] = WebAdminTestFactory.TestProductVariantId.ToString(),
                ["Variants[0].Sku"] = $"SMOKE-{Guid.NewGuid():N}v2",
                ["Variants[0].Currency"] = "EUR",
                ["Variants[0].TaxCategoryId"] = WebAdminTestFactory.TestTaxCategoryId.ToString(),
                ["Variants[0].BasePriceNetMinor"] = "1999",
                ["Variants[0].StockOnHand"] = "6",
                ["Variants[0].StockReserved"] = "0",
                ["Variants[0].BackorderAllowed"] = "false",
                ["Variants[0].IsDigital"] = "false"
            });
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponseHtml.Should().Contain("The product was modified by another user. Please reload and try again.");
        staleResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeFalse();
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleResponseCspValues).Should().BeTrue();
        staleResponseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedPageEdit_WithMissingRowVersion_ShouldShowValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var title = $"Smoke Page {Guid.NewGuid():N}";
        var slug = $"smoke-page-{Guid.NewGuid():N}";

        using var createResponse = await SendHtmxGetAsync(client, "/Pages/Create");
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createHtml = await createResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var createToken = ExtractAntiForgeryToken(createHtml);

        using var createPostResponse = await SendHtmxPostAsync(client, "/Pages/Create", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = createToken,
            ["Status"] = "Draft",
            ["PublishStartUtc"] = string.Empty,
            ["PublishEndUtc"] = string.Empty,
            ["Translations.Index"] = "0",
            ["Translations[0].Culture"] = "de-DE",
            ["Translations[0].Title"] = title,
            ["Translations[0].Slug"] = slug,
            ["Translations[0].MetaTitle"] = $"{title} Meta",
            ["Translations[0].MetaDescription"] = "Smoke page meta description.",
            ["Translations[0].ContentHtml"] = "<p>Smoke page content for testing.</p>"
        });

        createPostResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createPostResponseHtml = await createPostResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        createPostResponse.Headers.TryGetValues("HX-Redirect", out var createRedirectValues)
            .Should().BeTrue(
                "a successful CMS page create should return an HTMX redirect; response preview: {0}",
                createPostResponseHtml.Length > 600 ? createPostResponseHtml[..600] : createPostResponseHtml);
        var createRedirect = createRedirectValues!.Single();
        createRedirect.Should().NotBeNullOrWhiteSpace();

        using var createdPageListResponse = await SendHtmxGetAsync(client, $"/Pages?query={Uri.EscapeDataString(title)}");
        var createdListHtml = await createdPageListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        createdPageListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var pageId = ExtractHrefQueryGuid(createdListHtml, "/Pages/Edit", "id");

        var editPath = $"/Pages/Edit?id={pageId}";
        using var editResponse = await SendHtmxGetAsync(client, editPath);
        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var editHtml = await editResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var postResponse = await SendHtmxPostAsync(client, "/Pages/Edit", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(editHtml),
            ["Id"] = pageId.ToString(),
            ["Status"] = "Draft",
            ["PublishStartUtc"] = string.Empty,
            ["PublishEndUtc"] = string.Empty,
            ["Translations.Index"] = "0",
            ["Translations[0].Culture"] = "de-DE",
            ["Translations[0].Title"] = title,
            ["Translations[0].Slug"] = slug,
            ["Translations[0].MetaTitle"] = $"{title} Meta",
            ["Translations[0].MetaDescription"] = "Smoke page meta description.",
            ["Translations[0].ContentHtml"] = "<p>Smoke page content for testing.</p>"
        });
        var postHtml = await postResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        postHtml.Should().Contain("RowVersion is required.");
        postResponse.Headers.TryGetValues("Content-Security-Policy", out var postCspValues).Should().BeTrue();
        postCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedPageEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var title = $"Smoke Page {Guid.NewGuid():N}";
        var staleTitle = $"{title} v1";
        var slug = $"smoke-page-{Guid.NewGuid():N}";
        var updatedTitle = $"{title} v2";

        using var createResponse = await SendHtmxGetAsync(client, "/Pages/Create");
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createHtml = await createResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var createToken = ExtractAntiForgeryToken(createHtml);

        using var createPostResponse = await SendHtmxPostAsync(client, "/Pages/Create", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = createToken,
            ["Status"] = "Draft",
            ["PublishStartUtc"] = string.Empty,
            ["PublishEndUtc"] = string.Empty,
            ["Translations.Index"] = "0",
            ["Translations[0].Culture"] = "de-DE",
            ["Translations[0].Title"] = title,
            ["Translations[0].Slug"] = slug,
            ["Translations[0].MetaTitle"] = $"{title} Meta",
            ["Translations[0].MetaDescription"] = "Smoke page meta description.",
            ["Translations[0].ContentHtml"] = "<p>Smoke page content for testing.</p>"
        });
        createPostResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createPostResponseHtml = await createPostResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        createPostResponse.Headers.TryGetValues("HX-Redirect", out var _)
            .Should().BeTrue("a successful CMS page create should return an HTMX redirect; response preview: {0}",
                createPostResponseHtml.Length > 600 ? createPostResponseHtml[..600] : createPostResponseHtml);

        using var createdPageListResponse = await SendHtmxGetAsync(client, $"/Pages?query={Uri.EscapeDataString(title)}");
        var createdListHtml = await createdPageListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        createdPageListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var pageId = ExtractHrefQueryGuid(createdListHtml, "/Pages/Edit", "id");

        var editPath = $"/Pages/Edit?id={pageId}";
        using var baselineEditResponse = await SendHtmxGetAsync(client, editPath);
        baselineEditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineEditHtml = await baselineEditResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineEditHtml, "RowVersion");
        var validPostToken = ExtractAntiForgeryToken(baselineEditHtml);

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Pages/Edit",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = validPostToken,
                ["Id"] = pageId.ToString(),
                ["RowVersion"] = staleRowVersion,
                ["Status"] = "Published",
                ["PublishStartUtc"] = string.Empty,
                ["PublishEndUtc"] = string.Empty,
                ["Translations.Index"] = "0",
                ["Translations[0].Culture"] = "de-DE",
                ["Translations[0].Title"] = staleTitle,
                ["Translations[0].Slug"] = $"{slug}-v1",
                ["Translations[0].MetaTitle"] = $"{staleTitle} Meta",
                ["Translations[0].MetaDescription"] = "Smoke page meta description.",
                ["Translations[0].ContentHtml"] = "<p>Smoke page content first update.</p>"
            });
        var firstUpdateHtml = await firstUpdateResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var firstUpdateRedirectValues)
            .Should().BeTrue(
                "initial CMS page edit should succeed and return an HTMX redirect; response preview: {0}",
                firstUpdateHtml.Length > 600 ? firstUpdateHtml[..600] : firstUpdateHtml);
        firstUpdateRedirectValues!.Single().Should().Contain("/Pages");

        using var staleEditResponse = await SendHtmxGetAsync(client, editPath);
        staleEditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleEditHtml = await staleEditResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Pages/Edit",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(staleEditHtml),
                ["Id"] = pageId.ToString(),
                ["RowVersion"] = staleRowVersion,
                ["Status"] = "Published",
                ["PublishStartUtc"] = string.Empty,
                ["PublishEndUtc"] = string.Empty,
                ["Translations.Index"] = "0",
                ["Translations[0].Culture"] = "de-DE",
                ["Translations[0].Title"] = updatedTitle,
                ["Translations[0].Slug"] = $"{slug}-stale",
                ["Translations[0].MetaTitle"] = $"{updatedTitle} Meta",
                ["Translations[0].MetaDescription"] = "Smoke page meta description.",
                ["Translations[0].ContentHtml"] = "<p>Smoke page content stale update.</p>"
            });

        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeFalse();
        staleResponseHtml.Should().Contain("The page was modified by another user. Please reload and try again.");
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleResponseCspValues).Should().BeTrue();
        staleResponseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedCustomerEdit_WithMissingRowVersion_ShouldShowValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var crmCustomerFirstName = $"Smoke{Guid.NewGuid():N}";
        var crmCustomerLastName = $"Customer{Guid.NewGuid():N}";
        var crmCustomerEmail = $"smoke-customer-missing-rv-{Guid.NewGuid():N}@example.test";
        var crmCustomerId = await CreateTestCustomerAndReturnIdAsync(client, crmCustomerFirstName, crmCustomerLastName, crmCustomerEmail);

        var editPath = $"/Crm/EditCustomer?id={crmCustomerId}";
        using var tokenResponse = await SendHtmxGetAsync(client, editPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/Crm/EditCustomer",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["Id"] = crmCustomerId.ToString(),
                ["RowVersion"] = string.Empty,
                ["UserId"] = string.Empty,
                ["CompanyName"] = string.Empty,
                ["TaxProfileType"] = "Consumer",
                ["VatId"] = string.Empty,
                ["FirstName"] = crmCustomerFirstName,
                ["LastName"] = crmCustomerLastName,
                ["Email"] = crmCustomerEmail,
                ["Phone"] = "+493012345678",
                ["Notes"] = "Smoke customer missing row-version coverage.",
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
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedCustomerEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var crmCustomerFirstName = $"Smoke{Guid.NewGuid():N}";
        var crmCustomerLastName = $"Customer{Guid.NewGuid():N}";
        var crmCustomerEmail = $"smoke-customer-stale-rv-{Guid.NewGuid():N}@example.test";
        var crmCustomerId = await CreateTestCustomerAndReturnIdAsync(client, crmCustomerFirstName, crmCustomerLastName, crmCustomerEmail);

        var editPath = $"/Crm/EditCustomer?id={crmCustomerId}";
        using var baselineTokenResponse = await SendHtmxGetAsync(client, editPath);
        baselineTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineTokenHtml = await baselineTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineTokenHtml, "RowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Crm/EditCustomer",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = crmCustomerId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["UserId"] = string.Empty,
                    ["CompanyName"] = string.Empty,
                    ["TaxProfileType"] = "Consumer",
                    ["VatId"] = string.Empty,
                    ["FirstName"] = $"{crmCustomerFirstName} v1",
                    ["LastName"] = crmCustomerLastName,
                    ["Email"] = crmCustomerEmail,
                    ["Phone"] = "+493012345678",
                    ["Notes"] = "Smoke customer stale coverage v1.",
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
                },
                baselineTokenHtml));
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();

        using var staleTokenResponse = await SendHtmxGetAsync(client, editPath);
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleTokenHtml = await staleTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Crm/EditCustomer",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = crmCustomerId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["UserId"] = string.Empty,
                    ["CompanyName"] = string.Empty,
                    ["TaxProfileType"] = "Consumer",
                    ["VatId"] = string.Empty,
                    ["FirstName"] = $"{crmCustomerFirstName} v2",
                    ["LastName"] = crmCustomerLastName,
                    ["Email"] = crmCustomerEmail,
                    ["Phone"] = "+493012345678",
                    ["Notes"] = "Smoke customer stale coverage v2.",
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
                },
                staleTokenHtml));
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponseHtml.Contains("Concurrency", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        staleResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeFalse();
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleResponseCspValues).Should().BeTrue();
        staleResponseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedLeadEdit_WithMissingRowVersion_ShouldShowValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var crmLeadFirstName = $"Smoke{Guid.NewGuid():N}";
        var crmLeadLastName = $"Lead{Guid.NewGuid():N}";
        var crmLeadEmail = $"smoke-lead-missing-rv-{Guid.NewGuid():N}@example.test";

        using var createLeadTokenResponse = await SendHtmxGetAsync(client, "/Crm/CreateLead");
        createLeadTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createLeadHtml = await createLeadTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var createLeadResponse = await SendHtmxPostAsync(
            client,
            "/Crm/CreateLead",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(createLeadHtml),
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
        createLeadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        createLeadResponse.Headers.TryGetValues("HX-Redirect", out var createLeadRedirectValues).Should().BeTrue();
        createLeadRedirectValues!.Single().Should().Contain("/Crm/Leads");

        using var leadListResponse = await SendHtmxGetAsync(
            client,
            $"/Crm/Leads?query={Uri.EscapeDataString(crmLeadEmail)}");
        var leadListHtml = await leadListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        leadListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var leadId = ExtractHrefQueryGuid(leadListHtml, "/Crm/EditLead", "id");

        var editPath = $"/Crm/EditLead?id={leadId}";
        using var tokenResponse = await SendHtmxGetAsync(client, editPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/Crm/EditLead",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["Id"] = leadId.ToString(),
                ["RowVersion"] = string.Empty,
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
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedLeadEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var crmLeadFirstName = $"Smoke{Guid.NewGuid():N}";
        var crmLeadLastName = $"Lead{Guid.NewGuid():N}";
        var crmLeadEmail = $"smoke-lead-stale-rv-{Guid.NewGuid():N}@example.test";

        using var createLeadTokenResponse = await SendHtmxGetAsync(client, "/Crm/CreateLead");
        createLeadTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createLeadHtml = await createLeadTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var createLeadResponse = await SendHtmxPostAsync(
            client,
            "/Crm/CreateLead",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(createLeadHtml),
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
        createLeadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        createLeadResponse.Headers.TryGetValues("HX-Redirect", out var createLeadRedirectValues).Should().BeTrue();
        createLeadRedirectValues!.Single().Should().Contain("/Crm/Leads");

        using var leadListResponse = await SendHtmxGetAsync(
            client,
            $"/Crm/Leads?query={Uri.EscapeDataString(crmLeadEmail)}");
        var leadListHtml = await leadListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        leadListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var leadId = ExtractHrefQueryGuid(leadListHtml, "/Crm/EditLead", "id");

        var editPath = $"/Crm/EditLead?id={leadId}";
        using var baselineTokenResponse = await SendHtmxGetAsync(client, editPath);
        baselineTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineTokenHtml = await baselineTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineTokenHtml, "RowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Crm/EditLead",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = leadId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["FirstName"] = $"{crmLeadFirstName} v1",
                    ["LastName"] = crmLeadLastName,
                    ["CompanyName"] = "Smoke Lead Company",
                    ["Status"] = "New",
                    ["Email"] = crmLeadEmail,
                    ["Phone"] = "+493087654321",
                    ["AssignedToUserId"] = string.Empty,
                    ["CustomerId"] = string.Empty,
                    ["Source"] = "Smoke",
                    ["Notes"] = "Smoke-created CRM lead."
                },
                baselineTokenHtml));
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();

        using var staleTokenResponse = await SendHtmxGetAsync(client, editPath);
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleTokenHtml = await staleTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Crm/EditLead",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = leadId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["FirstName"] = $"{crmLeadFirstName} v2",
                    ["LastName"] = crmLeadLastName,
                    ["CompanyName"] = "Smoke Lead Company",
                    ["Status"] = "New",
                    ["Email"] = crmLeadEmail,
                    ["Phone"] = "+493087654321",
                    ["AssignedToUserId"] = string.Empty,
                    ["CustomerId"] = string.Empty,
                    ["Source"] = "Smoke",
                    ["Notes"] = "Smoke-created CRM lead."
                },
                staleTokenHtml));
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponseHtml.Contains("Concurrency", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        staleResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeFalse();
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleResponseCspValues).Should().BeTrue();
        staleResponseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedOpportunityEdit_WithMissingRowVersion_ShouldShowValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var crmCustomerFirstName = $"Smoke{Guid.NewGuid():N}";
        var crmCustomerLastName = $"Customer{Guid.NewGuid():N}";
        var crmCustomerEmail = $"smoke-opportunity-owner-missing-rv-{Guid.NewGuid():N}@example.test";
        var crmCustomerId = await CreateTestCustomerAndReturnIdAsync(client, crmCustomerFirstName, crmCustomerLastName, crmCustomerEmail);

        var crmOpportunityTitle = $"Smoke Opportunity {Guid.NewGuid():N}";
        using var createOpportunityTokenResponse = await SendHtmxGetAsync(
            client,
            $"/Crm/CreateOpportunity?customerId={crmCustomerId}");
        createOpportunityTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createOpportunityHtml = await createOpportunityTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var createOpportunityResponse = await SendHtmxPostAsync(
            client,
            "/Crm/CreateOpportunity",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(createOpportunityHtml),
                ["CustomerId"] = crmCustomerId.ToString(),
                ["AssignedToUserId"] = string.Empty,
                ["Title"] = crmOpportunityTitle,
                ["Currency"] = "EUR",
                ["Stage"] = "Qualification",
                ["ExpectedCloseDateUtc"] = $"{DateTime.UtcNow.AddDays(15):yyyy-MM-dd}",
                ["EstimatedValueMinor"] = "5000",
                ["Items[0].Id"] = string.Empty,
                ["Items[0].ProductVariantId"] = WebAdminTestFactory.TestProductVariantId.ToString(),
                ["Items[0].Quantity"] = "2",
                ["Items[0].UnitPriceMinor"] = "2500"
            });
        createOpportunityResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        createOpportunityResponse.Headers.TryGetValues("HX-Redirect", out var createOpportunityRedirectValues).Should().BeTrue();
        createOpportunityRedirectValues!.Single().Should().Contain("/Crm/Opportunities");

        using var opportunityListResponse = await SendHtmxGetAsync(
            client,
            $"/Crm/Opportunities?query={Uri.EscapeDataString(crmOpportunityTitle)}");
        var opportunityListHtml = await opportunityListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        opportunityListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var opportunityId = ExtractHrefQueryGuid(opportunityListHtml, "/Crm/EditOpportunity", "id");

        var editPath = $"/Crm/EditOpportunity?id={opportunityId}";
        using var tokenResponse = await SendHtmxGetAsync(client, editPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/Crm/EditOpportunity",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["Id"] = opportunityId.ToString(),
                ["RowVersion"] = string.Empty,
                ["CustomerId"] = crmCustomerId.ToString(),
                ["AssignedToUserId"] = string.Empty,
                ["Title"] = crmOpportunityTitle,
                ["Currency"] = "EUR",
                ["Stage"] = "Qualification",
                ["ExpectedCloseDateUtc"] = $"{DateTime.UtcNow.AddDays(15):yyyy-MM-dd}",
                ["EstimatedValueMinor"] = "5000",
                ["Items[0].Id"] = string.Empty,
                ["Items[0].ProductVariantId"] = WebAdminTestFactory.TestProductVariantId.ToString(),
                ["Items[0].Quantity"] = "2",
                ["Items[0].UnitPriceMinor"] = "2500"
            });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedOpportunityEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var crmCustomerFirstName = $"Smoke{Guid.NewGuid():N}";
        var crmCustomerLastName = $"Customer{Guid.NewGuid():N}";
        var crmCustomerEmail = $"smoke-opportunity-owner-stale-rv-{Guid.NewGuid():N}@example.test";
        var crmCustomerId = await CreateTestCustomerAndReturnIdAsync(client, crmCustomerFirstName, crmCustomerLastName, crmCustomerEmail);

        var crmOpportunityTitle = $"Smoke Opportunity {Guid.NewGuid():N}";
        using var createOpportunityTokenResponse = await SendHtmxGetAsync(
            client,
            $"/Crm/CreateOpportunity?customerId={crmCustomerId}");
        createOpportunityTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createOpportunityHtml = await createOpportunityTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var createOpportunityResponse = await SendHtmxPostAsync(
            client,
            "/Crm/CreateOpportunity",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(createOpportunityHtml),
                ["CustomerId"] = crmCustomerId.ToString(),
                ["AssignedToUserId"] = string.Empty,
                ["Title"] = crmOpportunityTitle,
                ["Currency"] = "EUR",
                ["Stage"] = "Qualification",
                ["ExpectedCloseDateUtc"] = $"{DateTime.UtcNow.AddDays(15):yyyy-MM-dd}",
                ["EstimatedValueMinor"] = "5000",
                ["Items[0].Id"] = string.Empty,
                ["Items[0].ProductVariantId"] = WebAdminTestFactory.TestProductVariantId.ToString(),
                ["Items[0].Quantity"] = "2",
                ["Items[0].UnitPriceMinor"] = "2500"
            });
        createOpportunityResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        createOpportunityResponse.Headers.TryGetValues("HX-Redirect", out var createOpportunityRedirectValues).Should().BeTrue();
        createOpportunityRedirectValues!.Single().Should().Contain("/Crm/Opportunities");

        using var opportunityListResponse = await SendHtmxGetAsync(
            client,
            $"/Crm/Opportunities?query={Uri.EscapeDataString(crmOpportunityTitle)}");
        var opportunityListHtml = await opportunityListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        opportunityListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var opportunityId = ExtractHrefQueryGuid(opportunityListHtml, "/Crm/EditOpportunity", "id");

        var editPath = $"/Crm/EditOpportunity?id={opportunityId}";
        using var baselineTokenResponse = await SendHtmxGetAsync(client, editPath);
        baselineTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineTokenHtml = await baselineTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineTokenHtml, "RowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Crm/EditOpportunity",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = opportunityId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["CustomerId"] = crmCustomerId.ToString(),
                    ["AssignedToUserId"] = string.Empty,
                    ["Title"] = $"{crmOpportunityTitle} v1",
                    ["Currency"] = "EUR",
                    ["Stage"] = "Qualification",
                    ["ExpectedCloseDateUtc"] = $"{DateTime.UtcNow.AddDays(16):yyyy-MM-dd}",
                    ["EstimatedValueMinor"] = "5000",
                    ["Items[0].Id"] = string.Empty,
                    ["Items[0].ProductVariantId"] = WebAdminTestFactory.TestProductVariantId.ToString(),
                    ["Items[0].Quantity"] = "2",
                    ["Items[0].UnitPriceMinor"] = "2500"
                },
                baselineTokenHtml));
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();

        using var staleTokenResponse = await SendHtmxGetAsync(client, editPath);
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleTokenHtml = await staleTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Crm/EditOpportunity",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = opportunityId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["CustomerId"] = crmCustomerId.ToString(),
                    ["AssignedToUserId"] = string.Empty,
                    ["Title"] = $"{crmOpportunityTitle} v2",
                    ["Currency"] = "EUR",
                    ["Stage"] = "Qualification",
                    ["ExpectedCloseDateUtc"] = $"{DateTime.UtcNow.AddDays(17):yyyy-MM-dd}",
                    ["EstimatedValueMinor"] = "5000",
                    ["Items[0].Id"] = string.Empty,
                    ["Items[0].ProductVariantId"] = WebAdminTestFactory.TestProductVariantId.ToString(),
                    ["Items[0].Quantity"] = "2",
                    ["Items[0].UnitPriceMinor"] = "2500"
                },
                staleTokenHtml));
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponseHtml.Contains("Concurrency", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        staleResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeFalse();
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleResponseCspValues).Should().BeTrue();
        staleResponseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedInvoiceEdit_WithMissingRowVersion_ShouldShowValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var invoiceId = await CreateTestInvoiceAndReturnIdAsync(client, WebAdminTestFactory.TestOrderId, Guid.Empty);

        using var invoiceTokenResponse = await SendHtmxGetAsync(
            client,
            $"/Crm/EditInvoice?id={invoiceId}");
        invoiceTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var invoiceTokenHtml = await invoiceTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var invoiceRowVersion = ExtractHiddenInputValue(invoiceTokenHtml, "RowVersion");

        using var draftTransitionResponse = await SendHtmxPostAsync(
            client,
            "/Crm/TransitionInvoiceStatus",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = invoiceId.ToString(),
                    ["RowVersion"] = invoiceRowVersion,
                    ["TargetStatus"] = "0"
                },
                invoiceTokenHtml));
        draftTransitionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        draftTransitionResponse.Headers.TryGetValues("HX-Redirect", out var draftRedirectValues).Should().BeTrue();
        draftRedirectValues!.Single().Should().Contain($"/Crm/EditInvoice");

        using var draftTokenResponse = await SendHtmxGetAsync(client, $"/Crm/EditInvoice?id={invoiceId}");
        draftTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var draftTokenHtml = await draftTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/Crm/EditInvoice",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(draftTokenHtml),
                ["Id"] = invoiceId.ToString(),
                ["RowVersion"] = string.Empty,
                ["Status"] = "Draft",
                ["Currency"] = "EUR",
                ["TotalNetMinor"] = "1000",
                ["TotalTaxMinor"] = "190",
                ["TotalGrossMinor"] = "1190",
                ["DueDateUtc"] = $"{DateTime.UtcNow.AddDays(15):yyyy-MM-ddTHH:mm:ss}"
            });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var responseCspValues).Should().BeTrue();
        responseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedInvoiceEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var invoiceId = await CreateTestInvoiceAndReturnIdAsync(client, WebAdminTestFactory.TestOrderId, Guid.Empty);

        using var invoiceTokenResponse = await SendHtmxGetAsync(
            client,
            $"/Crm/EditInvoice?id={invoiceId}");
        invoiceTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var invoiceTokenHtml = await invoiceTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var initialRowVersion = ExtractHiddenInputValue(invoiceTokenHtml, "RowVersion");

        using var draftTransitionResponse = await SendHtmxPostAsync(
            client,
            "/Crm/TransitionInvoiceStatus",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = invoiceId.ToString(),
                    ["RowVersion"] = initialRowVersion,
                    ["TargetStatus"] = "0"
                },
                invoiceTokenHtml));
        draftTransitionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        draftTransitionResponse.Headers.TryGetValues("HX-Redirect", out var draftRedirectValues).Should().BeTrue();
        draftRedirectValues!.Single().Should().Contain($"/Crm/EditInvoice");

        using var draftTokenResponse = await SendHtmxGetAsync(client, $"/Crm/EditInvoice?id={invoiceId}");
        draftTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var draftTokenHtml = await draftTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var draftRowVersion = ExtractHiddenInputValue(draftTokenHtml, "RowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Crm/EditInvoice",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = invoiceId.ToString(),
                    ["RowVersion"] = draftRowVersion,
                    ["Status"] = "Draft",
                    ["Currency"] = "EUR",
                    ["TotalNetMinor"] = "1000",
                    ["TotalTaxMinor"] = "190",
                    ["TotalGrossMinor"] = "1190",
                    ["DueDateUtc"] = $"{DateTime.UtcNow.AddDays(16):yyyy-MM-ddTHH:mm:ss}"
                },
                draftTokenHtml));
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();

        using var staleTokenResponse = await SendHtmxGetAsync(client, $"/Crm/EditInvoice?id={invoiceId}");
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleTokenHtml = await staleTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Crm/EditInvoice",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = invoiceId.ToString(),
                    ["RowVersion"] = initialRowVersion,
                    ["Status"] = "Draft",
                    ["Currency"] = "EUR",
                    ["TotalNetMinor"] = "1000",
                    ["TotalTaxMinor"] = "190",
                    ["TotalGrossMinor"] = "1190",
                    ["DueDateUtc"] = $"{DateTime.UtcNow.AddDays(17):yyyy-MM-ddTHH:mm:ss}"
                },
                staleTokenHtml));
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (
            staleResponseHtml.Contains("Concurrency", StringComparison.OrdinalIgnoreCase) ||
            staleResponseHtml.Contains("Invoice not found", StringComparison.Ordinal))
            .Should().BeTrue();
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleResponseCspValues).Should().BeTrue();
        staleResponseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedInvoiceTransition_WithMissingRowVersion_ShouldShowFailureMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var invoiceId = await CreateTestInvoiceAndReturnIdAsync(client, WebAdminTestFactory.TestOrderId, Guid.Empty);

        using var editorResponse = await SendHtmxGetAsync(
            client,
            $"/Crm/EditInvoice?id={invoiceId}");
        editorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var editorHtmlResponse = await editorResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var response = await SendHtmxPostAsync(
            client,
            "/Crm/TransitionInvoiceStatus",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(editorHtmlResponse),
                ["Id"] = invoiceId.ToString(),
                ["RowVersion"] = string.Empty,
                ["TargetStatus"] = "1"
            });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (response.Headers.TryGetValues("HX-Redirect", out var responseRedirectValues))
        {
            using var redirectedResponse = await SendHtmxGetAsync(client, responseRedirectValues.Single());
            redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var redirectedHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            responseHtml = redirectedHtml;
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (
            responseHtml.Contains("RowVersion is required.", StringComparison.Ordinal) ||
            responseHtml.Contains("Failed to update invoice status.", StringComparison.Ordinal))
            .Should().BeTrue();
        response.Headers.TryGetValues("Content-Security-Policy", out var responseCspValues).Should().BeTrue();
        responseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedInvoiceTransition_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var invoiceId = await CreateTestInvoiceAndReturnIdAsync(client, WebAdminTestFactory.TestOrderId, Guid.Empty);

        var editorPath = $"/Crm/EditInvoice?id={invoiceId}";
        using var editorTokenResponse = await SendHtmxGetAsync(client, editorPath);
        editorTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var editorTokenHtml = await editorTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(editorTokenHtml, "RowVersion");

        using var firstTransitionResponse = await SendHtmxPostAsync(
            client,
            "/Crm/TransitionInvoiceStatus",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = invoiceId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["TargetStatus"] = "3"
                },
                editorTokenHtml));
        firstTransitionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstTransitionResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();

        using var staleTokenResponse = await SendHtmxGetAsync(client, editorPath);
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleTokenHtml = await staleTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Crm/TransitionInvoiceStatus",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = invoiceId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["TargetStatus"] = "1"
                },
                staleTokenHtml));
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (staleResponse.Headers.TryGetValues("HX-Redirect", out var staleResponseRedirectValues))
        {
            using var staleRedirectedResponse = await SendHtmxGetAsync(client, staleResponseRedirectValues.Single());
            staleRedirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            staleResponseHtml = await staleRedirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (
            staleResponseHtml.Contains("Concurrency conflict. Reload the invoice and try again.", StringComparison.Ordinal) ||
            staleResponseHtml.Contains("Concurrency", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleResponseCspValues).Should().BeTrue();
        staleResponseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedInvoiceEInvoiceDownload_WithMissingSnapshot_ShouldRedirectToEditor()
    {
        var invoiceId = Guid.NewGuid();
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Invoice>().Add(new Invoice
                {
                    Id = invoiceId,
                    Status = InvoiceStatus.Open,
                    Currency = "EUR",
                    DueDateUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    IssuedAtUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                    RowVersion = new byte[] { 1 }
                });
            });

        using var response = await client.GetAsync($"/Crm/DownloadInvoiceEInvoiceArtifact?id={invoiceId}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Be($"/Crm/EditInvoice/{invoiceId}");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedInvoiceEInvoiceDownload_WithGeneratedSnapshotAndReadyGenerator_ShouldReturnFileContent()
    {
        var invoiceId = Guid.NewGuid();
        var expectedContent = new byte[] { 1, 2, 3, 4 };
        var generator = new RecordingEInvoiceGenerationService(new EInvoiceGenerationResult(
            EInvoiceGenerationStatus.Generated,
            "Generated",
            new EInvoiceArtifact(
                invoiceId,
                EInvoiceArtifactFormat.ZugferdFacturX,
                "application/pdf",
                "invoice.xml",
                expectedContent,
                "factur-x-test-profile",
                new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc))));
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Invoice>().Add(new Invoice
                {
                    Id = invoiceId,
                    Status = InvoiceStatus.Open,
                    Currency = "EUR",
                    DueDateUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    IssuedAtUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                    IssuedSnapshotJson = BuildReadyInvoiceSnapshot(invoiceId),
                    RowVersion = new byte[] { 1 }
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IEInvoiceGenerationService>();
                services.AddSingleton<IEInvoiceGenerationService>(generator);
            });

        using var response = await client.GetAsync($"/Crm/DownloadInvoiceEInvoiceArtifact?id={invoiceId}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        var content = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        content.Should().Equal(expectedContent);
        generator.Calls.Should().Be(1);
        generator.LastInvoiceId.Should().Be(invoiceId);
        generator.LastFormat.Should().Be(EInvoiceArtifactFormat.ZugferdFacturX);
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedInvoiceEInvoiceDownload_WithGeneratedSnapshot_ShouldPersistGeneratedArtifactBeforeReturningFile()
    {
        var invoiceId = Guid.NewGuid();
        var expectedContent = new byte[] { 9, 8, 7, 6 };
        var generatedAtUtc = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        var generator = new RecordingEInvoiceGenerationService(new EInvoiceGenerationResult(
            EInvoiceGenerationStatus.Generated,
            "Generated",
            new EInvoiceArtifact(
                invoiceId,
                EInvoiceArtifactFormat.ZugferdFacturX,
                "application/pdf",
                "invoice-factur-x.pdf",
                expectedContent,
                "factur-x-storage-profile",
                generatedAtUtc)));
        var storage = new RecordingEInvoiceArtifactStorage();
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Invoice>().Add(new Invoice
                {
                    Id = invoiceId,
                    Status = InvoiceStatus.Open,
                    Currency = "EUR",
                    DueDateUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    IssuedAtUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                    IssuedSnapshotJson = BuildReadyInvoiceSnapshot(invoiceId),
                    RowVersion = new byte[] { 1 }
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IEInvoiceGenerationService>();
                services.RemoveAll<IEInvoiceArtifactStorage>();
                services.AddSingleton<IEInvoiceGenerationService>(generator);
                services.AddSingleton<IEInvoiceArtifactStorage>(storage);
            });

        using var response = await client.GetAsync($"/Crm/DownloadInvoiceEInvoiceArtifact?id={invoiceId}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        response.Content.Headers.ContentDisposition!.FileNameStar.Should().Be("invoice-factur-x.pdf");
        var content = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        content.Should().Equal(expectedContent);
        storage.Calls.Should().Be(1);
        storage.Artifact.Should().NotBeNull();
        storage.Artifact!.InvoiceId.Should().Be(invoiceId);
        storage.Artifact.Format.Should().Be(EInvoiceArtifactFormat.ZugferdFacturX);
        storage.Artifact.Content.Should().Equal(expectedContent);
        storage.Artifact.ValidationProfile.Should().Be("factur-x-storage-profile");
        storage.Artifact.GeneratedAtUtc.Should().Be(generatedAtUtc);
    }

    [Fact]
    public async Task AuthenticatedInvoiceEInvoiceDownload_WithXRechnungFormat_ShouldReturnXmlContent()
    {
        var invoiceId = Guid.NewGuid();
        var expectedContent = new byte[] { 5, 6, 7, 8 };
        var generator = new RecordingEInvoiceGenerationService(new EInvoiceGenerationResult(
            EInvoiceGenerationStatus.Generated,
            "Generated",
            new EInvoiceArtifact(
                invoiceId,
                EInvoiceArtifactFormat.XRechnung,
                "application/xml",
                "rechnung.xml",
                expectedContent,
                "xrechnung-test-profile",
                new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc))));
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Invoice>().Add(new Invoice
                {
                    Id = invoiceId,
                    Status = InvoiceStatus.Open,
                    Currency = "EUR",
                    DueDateUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    IssuedAtUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                    IssuedSnapshotJson = BuildReadyInvoiceSnapshot(invoiceId),
                    RowVersion = new byte[] { 1 }
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IEInvoiceGenerationService>();
                services.AddSingleton<IEInvoiceGenerationService>(generator);
            });

        using var response = await client.GetAsync($"/Crm/DownloadInvoiceEInvoiceArtifact?id={invoiceId}&format=XRechnung", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/xml");
        var content = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        content.Should().Equal(expectedContent);
        generator.Calls.Should().Be(1);
        generator.LastInvoiceId.Should().Be(invoiceId);
        generator.LastFormat.Should().Be(EInvoiceArtifactFormat.XRechnung);
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedInvoiceEInvoiceDownload_WithUnsupportedFormat_ShouldRedirectToEditor()
    {
        var invoiceId = Guid.NewGuid();
        var generator = new RecordingEInvoiceGenerationService(new EInvoiceGenerationResult(
            EInvoiceGenerationStatus.NotConfigured,
            "Not configured"));
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Invoice>().Add(new Invoice
                {
                    Id = invoiceId,
                    Status = InvoiceStatus.Open,
                    Currency = "EUR",
                    DueDateUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    IssuedAtUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                    IssuedSnapshotJson = BuildReadyInvoiceSnapshot(invoiceId),
                    RowVersion = new byte[] { 1 }
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IEInvoiceGenerationService>();
                services.AddSingleton<IEInvoiceGenerationService>(generator);
            });

        using var response = await client.GetAsync($"/Crm/DownloadInvoiceEInvoiceArtifact?id={invoiceId}&format=999", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Be($"/Crm/EditInvoice/{invoiceId}");
        generator.Calls.Should().Be(0);
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedInvoiceRefund_WithMissingRowVersion_ShouldShowFailureMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var invoiceId = await CreateTestInvoiceAndReturnIdAsync(client, WebAdminTestFactory.TestOrderId, WebAdminTestFactory.TestOrderPaymentId);

        using var editorResponse = await SendHtmxGetAsync(client, $"/Crm/EditInvoice?id={invoiceId}");
        editorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var editorHtml = await editorResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var response = await SendHtmxPostAsync(
            client,
            "/Crm/RefundInvoice",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(editorHtml),
                ["InvoiceId"] = invoiceId.ToString(),
                ["RowVersion"] = string.Empty,
                ["AmountMinor"] = "1",
                ["Currency"] = "EUR",
                ["Reason"] = "Smoke missing row version invoice refund."
            });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (response.Headers.TryGetValues("HX-Redirect", out var responseRedirectValues))
        {
            using var redirectedResponse = await SendHtmxGetAsync(client, responseRedirectValues.Single());
            redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var redirectedHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            responseHtml = redirectedHtml;
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (
            responseHtml.Contains("RowVersion is required.", StringComparison.Ordinal) ||
            responseHtml.Contains("Failed to record invoice refund.", StringComparison.Ordinal))
            .Should().BeTrue();
        response.Headers.TryGetValues("Content-Security-Policy", out var responseCspValues).Should().BeTrue();
        responseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedInvoiceRefund_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var invoiceId = await CreateTestInvoiceAndReturnIdAsync(client, WebAdminTestFactory.TestOrderId, WebAdminTestFactory.TestOrderPaymentId);

        var editorPath = $"/Crm/EditInvoice?id={invoiceId}";
        using var editorTokenResponse = await SendHtmxGetAsync(client, editorPath);
        editorTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var editorTokenHtml = await editorTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(editorTokenHtml, "RowVersion");

        using var firstRefundResponse = await SendHtmxPostAsync(
            client,
            "/Crm/RefundInvoice",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["InvoiceId"] = invoiceId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["AmountMinor"] = "1",
                    ["Currency"] = "EUR",
                    ["Reason"] = "Smoke first invoice refund."
                },
                editorTokenHtml));
        firstRefundResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstRefundResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();

        using var staleTokenResponse = await SendHtmxGetAsync(client, editorPath);
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleTokenHtml = await staleTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Crm/RefundInvoice",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["InvoiceId"] = invoiceId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["AmountMinor"] = "1",
                    ["Currency"] = "EUR",
                    ["Reason"] = "Smoke stale invoice refund."
                },
                staleTokenHtml));
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (staleResponse.Headers.TryGetValues("HX-Redirect", out var staleResponseRedirectValues))
        {
            using var staleRedirectedResponse = await SendHtmxGetAsync(client, staleResponseRedirectValues.Single());
            staleRedirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            staleResponseHtml = await staleRedirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (
            staleResponseHtml.Contains("Concurrency conflict. Reload the invoice and try again.", StringComparison.Ordinal) ||
            staleResponseHtml.Contains("Concurrency", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleResponseCspValues).Should().BeTrue();
        staleResponseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedSegmentEdit_WithMissingRowVersion_ShouldShowValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var segmentName = $"SmokeSegment-{Guid.NewGuid():N}";

        var segmentId = await CreateTestSegmentAndReturnIdAsync(client, segmentName);

        using var editResponse = await SendHtmxGetAsync(client, $"/Crm/EditSegment?id={segmentId}");
        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var editHtml = await editResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/Crm/EditSegment",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(editHtml),
                ["Id"] = segmentId.ToString(),
                ["RowVersion"] = string.Empty,
                ["Name"] = segmentName,
                ["Description"] = "Smoke missing row-version for segment edit."
            });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedSegmentEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var segmentName = $"SmokeSegment-{Guid.NewGuid():N}";

        var segmentId = await CreateTestSegmentAndReturnIdAsync(client, segmentName);
        var editorPath = $"/Crm/EditSegment?id={segmentId}";

        using var initialTokenResponse = await SendHtmxGetAsync(client, editorPath);
        initialTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var initialTokenHtml = await initialTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(initialTokenHtml, "RowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Crm/EditSegment",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = segmentId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["Name"] = $"{segmentName} v1",
                    ["Description"] = "Smoke stale segment flow v1"
                },
                initialTokenHtml));
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();

        using var staleTokenResponse = await SendHtmxGetAsync(client, editorPath);
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleTokenHtml = await staleTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Crm/EditSegment",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = segmentId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["Name"] = $"{segmentName} v2",
                    ["Description"] = "Smoke stale segment flow v2"
                },
                staleTokenHtml));
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues))
        {
            using var staleRedirectedResponse = await SendHtmxGetAsync(client, staleRedirectValues.Single());
            staleRedirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            staleResponseHtml = await staleRedirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponseHtml.Should().Contain("Concurrency");
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleResponseCspValues).Should().BeTrue();
        staleResponseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedWarehouseEdit_WithMissingRowVersion_ShouldShowValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        var warehouseName = $"Smoke Warehouse {Guid.NewGuid():N}";
        var warehouseId = await CreateTestWarehouseAndReturnIdAsync(
            client,
            warehouseName,
            "Berlin-Edit",
            "Smoke warehouse for row-version validation.",
            isDefault: true);
        var editorPath = $"/Inventory/EditWarehouse?id={warehouseId}";

        using var tokenResponse = await SendHtmxGetAsync(client, editorPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/Inventory/EditWarehouse",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["Id"] = warehouseId.ToString(),
                ["RowVersion"] = string.Empty,
                ["BusinessId"] = ExtractHiddenInputValue(tokenHtml, "BusinessId"),
                ["Name"] = $"{warehouseName} updated",
                ["Location"] = "Berlin West",
                ["Description"] = "Row-version smoke update.",
                ["IsDefault"] = "false"
            });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedWarehouseEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        var warehouseName = $"Smoke Warehouse {Guid.NewGuid():N}";
        var warehouseId = await CreateTestWarehouseAndReturnIdAsync(
            client,
            warehouseName,
            "Berlin-Edit",
            "Smoke warehouse for stale row-version.",
            isDefault: true);
        var editorPath = $"/Inventory/EditWarehouse?id={warehouseId}";

        using var baselineTokenResponse = await SendHtmxGetAsync(client, editorPath);
        baselineTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineTokenHtml = await baselineTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineTokenHtml, "RowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Inventory/EditWarehouse",
            AddRequestVerificationToken(
                BuildWarehouseEditPayload(
                    warehouseId.ToString(),
                    staleRowVersion,
                    $"{warehouseName} - first",
                    "Berlin West",
                    "Smoke warehouse stale update phase 1.",
                    false),
                baselineTokenHtml));
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var firstRedirectValues).Should().BeTrue();
        firstRedirectValues!.Single().Should().Contain("/Inventory/EditWarehouse");

        using var staleTokenResponse = await SendHtmxGetAsync(client, editorPath);
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleTokenHtml = await staleTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Inventory/EditWarehouse",
            AddRequestVerificationToken(
                BuildWarehouseEditPayload(
                    warehouseId.ToString(),
                    staleRowVersion,
                    $"{warehouseName} - stale",
                    "Berlin East",
                    "Smoke warehouse stale update phase 2.",
                    true),
                staleTokenHtml));
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues))
        {
            using var staleRedirectResponse = await SendHtmxGetAsync(client, staleRedirectValues.Single());
            staleRedirectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            staleResponseHtml = await staleRedirectResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponseHtml.Should().Contain("Concurrency");
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleResponseCspValues).Should().BeTrue();
        staleResponseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedSupplierEdit_WithMissingRowVersion_ShouldShowValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        var supplierName = $"Smoke Supplier {Guid.NewGuid():N}";
        var supplierEmail = $"smoke-{Guid.NewGuid():N}@example.test";
        var supplierId = await CreateTestSupplierAndReturnIdAsync(client, supplierName, supplierEmail);
        var editorPath = $"/Inventory/EditSupplier?id={supplierId}";

        using var tokenResponse = await SendHtmxGetAsync(client, editorPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/Inventory/EditSupplier",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["Id"] = supplierId.ToString(),
                ["RowVersion"] = string.Empty,
                ["BusinessId"] = ExtractHiddenInputValue(tokenHtml, "BusinessId"),
                ["Name"] = $"{supplierName} updated",
                ["Email"] = supplierEmail,
                ["Phone"] = "+49301234567",
                ["Address"] = "Supplier Street 2, Berlin",
                ["Notes"] = "Supplier row-version smoke update."
            });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedSupplierEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        var supplierName = $"Smoke Supplier {Guid.NewGuid():N}";
        var supplierEmail = $"smoke-{Guid.NewGuid():N}@example.test";
        var supplierId = await CreateTestSupplierAndReturnIdAsync(client, supplierName, supplierEmail);
        var editorPath = $"/Inventory/EditSupplier?id={supplierId}";

        using var baselineTokenResponse = await SendHtmxGetAsync(client, editorPath);
        baselineTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineTokenHtml = await baselineTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineTokenHtml, "RowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Inventory/EditSupplier",
            AddRequestVerificationToken(
                BuildSupplierEditPayload(
                    supplierId.ToString(),
                    staleRowVersion,
                    $"{supplierName} - first",
                    supplierEmail,
                    "+49301234567",
                    "Supplier Street 1, Berlin",
                    "Smoke supplier stale update phase 1."),
                baselineTokenHtml));
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var firstRedirectValues).Should().BeTrue();
        firstRedirectValues!.Single().Should().Contain("/Inventory/EditSupplier");

        using var staleTokenResponse = await SendHtmxGetAsync(client, editorPath);
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleTokenHtml = await staleTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Inventory/EditSupplier",
            AddRequestVerificationToken(
                BuildSupplierEditPayload(
                    supplierId.ToString(),
                    staleRowVersion,
                    $"{supplierName} - stale",
                    supplierEmail,
                    "+49301230000",
                    "Supplier Street 2, Berlin",
                    "Smoke supplier stale update phase 2."),
                staleTokenHtml));
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues))
        {
            using var staleRedirectResponse = await SendHtmxGetAsync(client, staleRedirectValues.Single());
            staleRedirectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            staleResponseHtml = await staleRedirectResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponseHtml.Should().Contain("Concurrency");
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleResponseCspValues).Should().BeTrue();
        staleResponseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedSupplierEdit_WithSaveChangesConcurrency_ShouldSurfaceFailureMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                var supplierId = Guid.Parse("33333333-3333-3333-3333-333333333345");
                db.Set<Supplier>().Add(new Supplier
                {
                    Id = supplierId,
                    BusinessId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = "Smoke Supplier Concurrency",
                    Email = "smoke-concurrency@example.test",
                    Phone = "+49301230000",
                    Address = "Supplier Street 1, Berlin",
                    Notes = "Seeded for post-save concurrency smoke coverage.",
                    RowVersion = [1]
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        var supplierId = Guid.Parse("33333333-3333-3333-3333-333333333345");
        var editorPath = $"/Inventory/EditSupplier?id={supplierId}";

        using var tokenResponse = await SendHtmxGetAsync(client, editorPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(tokenHtml, "RowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            "/Inventory/EditSupplier",
            AddRequestVerificationToken(
                BuildSupplierEditPayload(
                    supplierId.ToString(),
                    rowVersion,
                    "Smoke Supplier Concurrency - updated",
                    "smoke-concurrency@example.test",
                    "+49301230000",
                    "Supplier Street 1, Berlin",
                    "Post-save concurrency smoke update."),
                tokenHtml));

        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (response.Headers.TryGetValues("HX-Redirect", out var redirectValues))
        {
            using var redirectedResponse = await SendHtmxGetAsync(client, redirectValues.Single());
            redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            responseHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("Concurrency");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedBusinessEdit_WithPostSaveConcurrency_ShouldSurfaceFailureMessage()
    {
        var businessId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Business>().Add(new Business
                {
                    Id = businessId,
                    Name = "Smoke Business For Concurrency Edit",
                    LegalName = "Smoke Business Concurrency Edit GmbH",
                    ContactEmail = "edit-biz-concurrency@example.test",
                    DefaultCurrency = "EUR",
                    DefaultCulture = "de-DE",
                    DefaultTimeZoneId = "Europe/Berlin",
                    Category = BusinessCategoryKind.Cafe,
                    OperationalStatus = BusinessOperationalStatus.Approved,
                    ApprovedAtUtc = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = WebAdminTestFactory.TestLifecycleUserId,
                    ModifiedByUserId = WebAdminTestFactory.TestLifecycleUserId,
                    RowVersion = [1]
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        var editPath = $"/Businesses/Edit?id={businessId}";

        using var tokenResponse = await SendHtmxGetAsync(client, editPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(tokenHtml, "RowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            "/Businesses/Edit",
            AddRequestVerificationToken(
                BuildBusinessEditPayload(
                    businessId.ToString(),
                    rowVersion,
                    "Smoke Business For Concurrency Edit - updated",
                    "Smoke Business Concurrency Edit GmbH",
                    "DE123456789",
                    "Updated by post-save concurrency smoke.",
                    "https://edit-business-concurrency.example.test",
                    "edit-biz-concurrency-updated@example.test",
                    "+49301230001",
                    "support@edit-business-concurrency.example.test",
                    "Smoke Onboarding Team",
                    "noreply@edit-business-concurrency.example.test",
                    true,
                    false,
                    true),
                tokenHtml));

        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (response.Headers.TryGetValues("HX-Redirect", out var redirectValues))
        {
            using var redirectedResponse = await SendHtmxGetAsync(client, redirectValues.Single());
            redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            responseHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("Concurrency");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedBusinessDelete_WithPostSaveConcurrency_ShouldSurfaceFailureMessage()
    {
        var businessId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Business>().Add(new Business
                {
                    Id = businessId,
                    Name = "Smoke Business For Concurrency Delete",
                    LegalName = "Smoke Business Concurrency Delete GmbH",
                    ContactEmail = "delete-biz-concurrency@example.test",
                    DefaultCurrency = "EUR",
                    DefaultCulture = "de-DE",
                    DefaultTimeZoneId = "Europe/Berlin",
                    Category = BusinessCategoryKind.Cafe,
                    OperationalStatus = BusinessOperationalStatus.Approved,
                    ApprovedAtUtc = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = WebAdminTestFactory.TestLifecycleUserId,
                    ModifiedByUserId = WebAdminTestFactory.TestLifecycleUserId,
                    RowVersion = [1]
                });

                db.Set<BusinessLocation>().Add(new BusinessLocation
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    Name = "Delete Concurrency Hub",
                    City = "Berlin",
                    CountryCode = "DE",
                    IsPrimary = true,
                    RowVersion = [1]
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        using var editResponse = await SendHtmxGetAsync(client, $"/Businesses/Edit?id={businessId}");
        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var editHtml = await editResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(editHtml, "RowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            "/Businesses/Delete",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(editHtml),
                ["id"] = businessId.ToString(),
                ["rowVersion"] = rowVersion
            });

        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (response.Headers.TryGetValues("HX-Redirect", out var redirectValues))
        {
            using var redirectedResponse = await SendHtmxGetAsync(client, redirectValues.Single());
            redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            responseHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Match(
            html => html.Contains("Concurrency", StringComparison.Ordinal)
                || html.Contains("Unable to remove the business", StringComparison.Ordinal));
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedBusinessLocationEdit_WithPostSaveConcurrency_ShouldSurfaceFailureMessage()
    {
        var businessId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Business>().Add(new Business
                {
                    Id = businessId,
                    Name = "Smoke Business For Location Concurrency",
                    ContactEmail = "location-biz-concurrency@example.test",
                    DefaultCurrency = "EUR",
                    DefaultCulture = "de-DE",
                    DefaultTimeZoneId = "Europe/Berlin",
                    Category = BusinessCategoryKind.Cafe,
                    OperationalStatus = BusinessOperationalStatus.Approved,
                    ApprovedAtUtc = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = WebAdminTestFactory.TestLifecycleUserId,
                    ModifiedByUserId = WebAdminTestFactory.TestLifecycleUserId,
                    RowVersion = [1]
                });

                db.Set<BusinessLocation>().Add(new BusinessLocation
                {
                    Id = locationId,
                    BusinessId = businessId,
                    Name = "Concurrency Location",
                    City = "Berlin",
                    CountryCode = "DE",
                    PostalCode = "10115",
                    IsPrimary = true,
                    RowVersion = [1]
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        using var tokenResponse = await SendHtmxGetAsync(client, $"/Businesses/EditLocation?id={locationId}");
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(tokenHtml, "RowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            "/Businesses/EditLocation",
            AddRequestVerificationToken(
                BuildBusinessLocationEditPayload(
                    locationId.ToString(),
                    businessId.ToString(),
                    rowVersion,
                    "Updated Concurrency Location",
                    "Berlin PostSave",
                    true,
                    "Seeded for post-save concurrency smoke coverage.",
                    "{\"wed\" : \"08:00-18:00\"}"),
                tokenHtml));

        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (response.Headers.TryGetValues("HX-Redirect", out var redirectValues))
        {
            using var redirectedResponse = await SendHtmxGetAsync(client, redirectValues.Single());
            redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            responseHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("Concurrency");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedBusinessLocationDelete_WithPostSaveConcurrency_ShouldSurfaceFailureMessage()
    {
        var businessId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Business>().Add(new Business
                {
                    Id = businessId,
                    Name = "Smoke Business For Location Delete Concurrency",
                    ContactEmail = "location-delete-biz-concurrency@example.test",
                    DefaultCurrency = "EUR",
                    DefaultCulture = "de-DE",
                    DefaultTimeZoneId = "Europe/Berlin",
                    Category = BusinessCategoryKind.Cafe,
                    OperationalStatus = BusinessOperationalStatus.Approved,
                    ApprovedAtUtc = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = WebAdminTestFactory.TestLifecycleUserId,
                    ModifiedByUserId = WebAdminTestFactory.TestLifecycleUserId,
                    RowVersion = [1]
                });

                db.Set<BusinessLocation>().Add(new BusinessLocation
                {
                    Id = locationId,
                    BusinessId = businessId,
                    Name = "Concurrency Delete Location",
                    City = "Berlin",
                    CountryCode = "DE",
                    IsPrimary = true,
                    RowVersion = [1]
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        using var tokenResponse = await SendHtmxGetAsync(client, $"/Businesses/EditLocation?id={locationId}");
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(tokenHtml, "RowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            "/Businesses/DeleteLocation",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = locationId.ToString(),
                    ["userId"] = businessId.ToString(),
                    ["rowVersion"] = rowVersion
                },
                tokenHtml));

        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (response.Headers.TryGetValues("HX-Redirect", out var redirectValues))
        {
            using var redirectedResponse = await SendHtmxGetAsync(client, redirectValues.Single());
            redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            responseHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Match(
            html => html.Contains("Concurrency", StringComparison.Ordinal)
                || html.Contains("Failed to archive location.", StringComparison.Ordinal));
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedBusinessMemberEdit_WithPostSaveConcurrency_ShouldSurfaceFailureMessage()
    {
        var businessId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var userId = WebAdminTestFactory.TestMemberUserId;

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Business>().Add(new Business
                {
                    Id = businessId,
                    Name = "Smoke Business For Member Concurrency",
                    LegalName = "Smoke Business Concurrency Member GmbH",
                    ContactEmail = "member-biz-concurrency@example.test",
                    DefaultCurrency = "EUR",
                    DefaultCulture = "de-DE",
                    DefaultTimeZoneId = "Europe/Berlin",
                    Category = BusinessCategoryKind.Cafe,
                    OperationalStatus = BusinessOperationalStatus.Approved,
                    ApprovedAtUtc = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = WebAdminTestFactory.TestLifecycleUserId,
                    ModifiedByUserId = WebAdminTestFactory.TestLifecycleUserId,
                    RowVersion = [1]
                });

                db.Set<BusinessMember>().Add(new BusinessMember
                {
                    Id = memberId,
                    BusinessId = businessId,
                    UserId = userId,
                    Role = BusinessMemberRole.Staff,
                    IsActive = true,
                    RowVersion = [1]
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        using var tokenResponse = await SendHtmxGetAsync(client, $"/Businesses/EditMember?id={memberId}");
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var currentRowVersion = ExtractHiddenInputValue(tokenHtml, "RowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            "/Businesses/EditMember",
            AddRequestVerificationToken(
                BuildBusinessMemberEditPayload(
                    memberId.ToString(),
                    businessId.ToString(),
                    userId.ToString(),
                    currentRowVersion,
                    "Owner",
                    true,
                    false,
                    string.Empty,
                    "1",
                    "20",
                    string.Empty,
                    "All"),
                tokenHtml));

        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (response.Headers.TryGetValues("HX-Redirect", out var redirectValues))
        {
            using var redirectedResponse = await SendHtmxGetAsync(client, redirectValues.Single());
            redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            responseHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Match(
            html => html.Contains("Concurrency", StringComparison.Ordinal)
                || html.Contains("Unable to update the business member", StringComparison.Ordinal));
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedBusinessMemberDelete_WithPostSaveConcurrency_ShouldSurfaceFailureMessage()
    {
        var businessId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Business>().Add(new Business
                {
                    Id = businessId,
                    Name = "Smoke Business For Member Delete Concurrency",
                    LegalName = "Smoke Business Concurrency Member Delete GmbH",
                    ContactEmail = "member-delete-biz-concurrency@example.test",
                    DefaultCurrency = "EUR",
                    DefaultCulture = "de-DE",
                    DefaultTimeZoneId = "Europe/Berlin",
                    Category = BusinessCategoryKind.Cafe,
                    OperationalStatus = BusinessOperationalStatus.Approved,
                    ApprovedAtUtc = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = WebAdminTestFactory.TestLifecycleUserId,
                    ModifiedByUserId = WebAdminTestFactory.TestLifecycleUserId,
                    RowVersion = [1]
                });

                db.Set<BusinessMember>().Add(new BusinessMember
                {
                    Id = memberId,
                    BusinessId = businessId,
                    UserId = WebAdminTestFactory.TestMemberUserId,
                    Role = BusinessMemberRole.Manager,
                    IsActive = true,
                    RowVersion = [1]
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        using var tokenResponse = await SendHtmxGetAsync(client, $"/Businesses/EditMember?id={memberId}");
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(tokenHtml, "RowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            "/Businesses/DeleteMember",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = memberId.ToString(),
                    ["userId"] = businessId.ToString(),
                    ["rowVersion"] = rowVersion
                },
                tokenHtml));

        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (response.Headers.TryGetValues("HX-Redirect", out var redirectValues))
        {
            using var redirectedResponse = await SendHtmxGetAsync(client, redirectValues.Single());
            redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            responseHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Match(
            html => html.Contains("Concurrency", StringComparison.Ordinal)
                || html.Contains("Unable to remove the business member", StringComparison.Ordinal));
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedBusinessProvisionOnboarding_WithPostSaveConcurrency_ShouldSurfaceFailureMessage()
    {
        var businessId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Business>().Add(new Business
                {
                    Id = businessId,
                    Name = "Smoke Business For Provisioning Concurrency",
                    LegalName = "Smoke Business Concurrency Provision GmbH",
                    ContactEmail = "provision-biz-concurrency@example.test",
                    DefaultCurrency = "EUR",
                    DefaultCulture = "de-DE",
                    DefaultTimeZoneId = "Europe/Berlin",
                    Category = BusinessCategoryKind.Cafe,
                    OperationalStatus = BusinessOperationalStatus.PendingApproval,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = WebAdminTestFactory.TestLifecycleUserId,
                    ModifiedByUserId = WebAdminTestFactory.TestLifecycleUserId,
                    RowVersion = [1]
                });

                db.Set<BusinessLocation>().Add(new BusinessLocation
                {
                    Id = locationId,
                    BusinessId = businessId,
                    Name = "Concurrency Provision Primary Location",
                    AddressLine1 = "Concurrency Provision Street 1",
                    AddressLine2 = "Suite 1",
                    City = "Berlin",
                    Region = "Berlin",
                    CountryCode = "DE",
                    PostalCode = "10115",
                    IsPrimary = true,
                    RowVersion = [1]
                });

                db.Set<BusinessMember>().Add(new BusinessMember
                {
                    BusinessId = businessId,
                    UserId = WebAdminTestFactory.TestMemberUserId,
                    Role = BusinessMemberRole.Owner,
                    IsActive = true,
                    RowVersion = [1]
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        using var tokenResponse = await SendHtmxGetAsync(client, $"/Businesses/Edit?id={businessId}");
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(tokenHtml, "RowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            "/Businesses/ProvisionOnboarding",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = businessId.ToString(),
                    ["rowVersion"] = rowVersion
                },
                tokenHtml));

        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (response.Headers.TryGetValues("HX-Redirect", out var redirectValues))
        {
            using var redirectedResponse = await SendHtmxGetAsync(client, redirectValues.Single());
            redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            responseHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().MatchRegex("could not be finalized|konnte nicht finalisiert");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedWarehouseEdit_WithPostSaveConcurrency_ShouldSurfaceFailureMessage()
    {
        var warehouseId = Guid.NewGuid();
        var warehouseName = $"Smoke Warehouse {Guid.NewGuid():N}";

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Warehouse>().Add(new Warehouse
                {
                    Id = warehouseId,
                    BusinessId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = warehouseName,
                    Location = "Berlin-PostSave",
                    Description = "Seeded for warehouse post-save concurrency smoke coverage.",
                    IsDefault = true,
                    RowVersion = [1]
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        var editorPath = $"/Inventory/EditWarehouse?id={warehouseId}";

        using var tokenResponse = await SendHtmxGetAsync(client, editorPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(tokenHtml, "RowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            "/Inventory/EditWarehouse",
            AddRequestVerificationToken(
                BuildWarehouseEditPayload(
                    warehouseId.ToString(),
                    rowVersion,
                    $"{warehouseName} - updated",
                    "Berlin West PostSave",
                    "Warehouse post-save concurrency smoke update.",
                    false),
                tokenHtml));

        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (response.Headers.TryGetValues("HX-Redirect", out var redirectValues))
        {
            using var redirectedResponse = await SendHtmxGetAsync(client, redirectValues.Single());
            redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            responseHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("Concurrency");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedStockLevelEdit_WithMissingRowVersion_ShouldShowValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        var (warehouseId, stockLevelId) = await CreateTestStockLevelAndReturnIdAsync(client);
        var editPath = $"/Inventory/EditStockLevel?id={stockLevelId}";

        using var tokenResponse = await SendHtmxGetAsync(client, editPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/Inventory/EditStockLevel",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["Id"] = stockLevelId.ToString(),
                ["RowVersion"] = string.Empty,
                ["WarehouseId"] = warehouseId.ToString(),
                ["ProductVariantId"] = ExtractHiddenInputValue(tokenHtml, "ProductVariantId"),
                ["AvailableQuantity"] = "33",
                ["ReservedQuantity"] = "1",
                ["ReorderPoint"] = "7",
                ["ReorderQuantity"] = "15",
                ["InTransitQuantity"] = "4"
            });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedStockLevelEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        var (warehouseId, stockLevelId) = await CreateTestStockLevelAndReturnIdAsync(client);
        var editPath = $"/Inventory/EditStockLevel?id={stockLevelId}";

        using var baselineTokenResponse = await SendHtmxGetAsync(client, editPath);
        baselineTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineTokenHtml = await baselineTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineTokenHtml, "RowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Inventory/EditStockLevel",
            AddRequestVerificationToken(
                BuildStockLevelEditPayload(
                    stockLevelId.ToString(),
                    staleRowVersion,
                    warehouseId.ToString(),
                    ExtractHiddenInputValue(baselineTokenHtml, "ProductVariantId"),
                    "33",
                    "1",
                    "7",
                    "15",
                    "4"),
                baselineTokenHtml));
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var firstRedirectValues).Should().BeTrue();
        firstRedirectValues!.Single().Should().Contain("/Inventory/EditStockLevel");

        using var staleTokenResponse = await SendHtmxGetAsync(client, editPath);
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleTokenHtml = await staleTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Inventory/EditStockLevel",
            AddRequestVerificationToken(
                BuildStockLevelEditPayload(
                    stockLevelId.ToString(),
                    staleRowVersion,
                    warehouseId.ToString(),
                    ExtractHiddenInputValue(staleTokenHtml, "ProductVariantId"),
                    "34",
                    "2",
                    "8",
                    "16",
                    "5"),
                staleTokenHtml));
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues))
        {
            using var staleRedirectResponse = await SendHtmxGetAsync(client, staleRedirectValues.Single());
            staleRedirectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            staleResponseHtml = await staleRedirectResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponseHtml.Should().Contain("Concurrency");
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleResponseCspValues).Should().BeTrue();
        staleResponseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedStockLevelEdit_WithPostSaveConcurrency_ShouldSurfaceFailureMessage()
    {
        var warehouseId = Guid.NewGuid();
        var stockLevelId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Warehouse>().Add(new Warehouse
                {
                    Id = warehouseId,
                    BusinessId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = $"Smoke Warehouse {Guid.NewGuid():N}",
                    Location = "Berlin PostSave",
                    Description = "Seeded for stock-level post-save concurrency smoke coverage.",
                    IsDefault = true,
                    RowVersion = [1]
                });

                db.Set<StockLevel>().Add(new StockLevel
                {
                    Id = stockLevelId,
                    WarehouseId = warehouseId,
                    ProductVariantId = WebAdminTestFactory.TestProductVariantId,
                    AvailableQuantity = 20,
                    ReservedQuantity = 3,
                    ReorderPoint = 6,
                    ReorderQuantity = 12,
                    InTransitQuantity = 2,
                    RowVersion = [1]
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        var editPath = $"/Inventory/EditStockLevel?id={stockLevelId}";

        using var tokenResponse = await SendHtmxGetAsync(client, editPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(tokenHtml, "RowVersion");
        var productVariantId = ExtractHiddenInputValue(tokenHtml, "ProductVariantId");

        using var response = await SendHtmxPostAsync(
            client,
            "/Inventory/EditStockLevel",
            AddRequestVerificationToken(
                BuildStockLevelEditPayload(
                    stockLevelId.ToString(),
                    rowVersion,
                    warehouseId.ToString(),
                    productVariantId,
                    "21",
                    "2",
                    "7",
                    "15",
                    "3"),
                tokenHtml));

        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (response.Headers.TryGetValues("HX-Redirect", out var redirectValues))
        {
            using var redirectedResponse = await SendHtmxGetAsync(client, redirectValues.Single());
            redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            responseHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("Concurrency");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedCustomerSegmentMembershipLifecycle_ShouldAssignAndRemoveSegment()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        var crmCustomerFirstName = $"SmokeCustomer{Guid.NewGuid():N}";
        var crmCustomerLastName = "SegmentOps";
        var crmCustomerEmail = $"segment-customer-{Guid.NewGuid():N}@example.test";
        var crmSegmentName = $"SmokeSegment-{Guid.NewGuid():N}";

        var customerId = await CreateTestCustomerAndReturnIdAsync(client, crmCustomerFirstName, crmCustomerLastName, crmCustomerEmail);
        var segmentId = await CreateTestSegmentAndReturnIdAsync(client, crmSegmentName);

        using var customerResponse = await SendHtmxGetAsync(client, $"/Crm/EditCustomer?id={customerId}");
        customerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var customerHtml = await customerResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var assignResponse = await SendHtmxPostAsync(
            client,
            "/Crm/CustomerSegmentMemberships",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["CustomerId"] = customerId.ToString(),
                    ["CustomerSegmentId"] = segmentId.ToString()
                },
                customerHtml));
        var assignResponseHtml = await assignResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        assignResponseHtml.Should().Contain(crmSegmentName);
        assignResponse.Headers.TryGetValues("Content-Security-Policy", out var assignResponseCspValues).Should().BeTrue();
        assignResponseCspValues!.Single().Should().Contain("form-action 'self'");

        var membershipIdMatch = Regex.Match(
            assignResponseHtml,
            "name=\"membershipId\" value=\"(?<id>[0-9a-fA-F-]{36})\"",
            RegexOptions.IgnoreCase);
        membershipIdMatch.Success.Should().BeTrue();
        var membershipId = membershipIdMatch.Groups["id"].Value;

        using var removeResponse = await SendHtmxPostAsync(
            client,
            "/Crm/RemoveCustomerSegmentMembership",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(customerHtml),
                ["customerId"] = customerId.ToString(),
                ["membershipId"] = membershipId
            });
        var removeResponseHtml = await removeResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        removeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        removeResponseHtml.Should().NotContain(crmSegmentName);
        removeResponse.Headers.TryGetValues("Content-Security-Policy", out var removeResponseCspValues).Should().BeTrue();
        removeResponseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedStockTransferLifecycle_WithMissingRowVersion_ShouldShowValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        var (stockTransferId, sourceWarehouseId, transferTargetWarehouseName) =
            await CreateStockTransferLifecycleSeedAsync(client);
        var listPath =
            $"/Inventory/StockTransfers?businessId=44444444-4444-4444-4444-444444444444&warehouseId={sourceWarehouseId}&q={Uri.EscapeDataString(transferTargetWarehouseName)}";

        using var tokenResponse = await SendHtmxGetAsync(client, listPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            $"/Inventory/UpdateStockTransferLifecycle?id={stockTransferId}",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["id"] = stockTransferId.ToString(),
                ["rowVersion"] = string.Empty,
                ["action"] = "MarkInTransit",
                ["businessId"] = "44444444-4444-4444-4444-444444444444",
                ["warehouseId"] = sourceWarehouseId.ToString(),
                ["page"] = "1",
                ["pageSize"] = "20",
                ["q"] = transferTargetWarehouseName,
                ["filter"] = "All"
            });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedStockTransferLifecycle_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        var (stockTransferId, sourceWarehouseId, transferTargetWarehouseName) =
            await CreateStockTransferLifecycleSeedAsync(client);
        var listPath =
            $"/Inventory/StockTransfers?businessId=44444444-4444-4444-4444-444444444444&warehouseId={sourceWarehouseId}&q={Uri.EscapeDataString(transferTargetWarehouseName)}";

        using var initialTokenResponse = await SendHtmxGetAsync(client, listPath);
        initialTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var initialTokenHtml = await initialTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(initialTokenHtml, "rowVersion");

        using var firstResponse = await SendHtmxPostAsync(
            client,
            $"/Inventory/UpdateStockTransferLifecycle?id={stockTransferId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = stockTransferId.ToString(),
                    ["rowVersion"] = staleRowVersion,
                    ["action"] = "MarkInTransit",
                    ["businessId"] = "44444444-4444-4444-4444-444444444444",
                    ["warehouseId"] = sourceWarehouseId.ToString(),
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["q"] = transferTargetWarehouseName,
                    ["filter"] = "All"
                },
                initialTokenHtml));

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();
        firstResponse.Headers.TryGetValues("Content-Security-Policy", out var firstCspValues).Should().BeTrue();
        firstCspValues!.Single().Should().Contain("form-action 'self'");
        using var secondTokenResponse = await SendHtmxGetAsync(client, listPath);
        secondTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondTokenHtml = await secondTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(
            client,
            $"/Inventory/UpdateStockTransferLifecycle?id={stockTransferId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = stockTransferId.ToString(),
                    ["rowVersion"] = staleRowVersion,
                    ["action"] = "Complete",
                    ["businessId"] = "44444444-4444-4444-4444-444444444444",
                    ["warehouseId"] = sourceWarehouseId.ToString(),
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["q"] = transferTargetWarehouseName,
                    ["filter"] = "All"
                },
                secondTokenHtml));
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues))
        {
            using var staleRedirectedResponse = await SendHtmxGetAsync(client, staleRedirectValues.Single());
            staleRedirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            staleResponseHtml = await staleRedirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponseHtml.Should().Contain("Concurrency");
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleResponseCspValues).Should().BeTrue();
        staleResponseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedStockTransferLifecycle_WithWhitespaceAction_ShouldBeTrimmedAndCaseInsensitive()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        var (stockTransferId, sourceWarehouseId, transferTargetWarehouseName) =
            await CreateStockTransferLifecycleSeedAsync(client);

        await PostValidStockTransferLifecycleActionAndAssertStatusAsync(
            client,
            stockTransferId,
            sourceWarehouseId,
            transferTargetWarehouseName,
            "  markintransit  ",
            "InTransit");
    }

    [Fact]
    public async Task AuthenticatedStockTransferLifecycle_WithPostSaveConcurrency_ShouldSurfaceFailureMessage()
    {
        var stockTransferId = Guid.NewGuid();
        var transferTargetWarehouseName = $"Smoke-Target-{Guid.NewGuid():N}";
        var sourceWarehouseId = Guid.NewGuid();
        var targetWarehouseId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Warehouse>().Add(new Warehouse
                {
                    Id = sourceWarehouseId,
                    BusinessId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = $"Smoke-Source-{Guid.NewGuid():N}",
                    Location = "Berlin Source",
                    Description = "Seeded for stock-transfer post-save concurrency smoke coverage.",
                    IsDefault = true,
                    RowVersion = [1]
                });

                db.Set<Warehouse>().Add(new Warehouse
                {
                    Id = targetWarehouseId,
                    BusinessId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = transferTargetWarehouseName,
                    Location = "Berlin Target",
                    Description = "Seeded for stock-transfer post-save concurrency smoke coverage.",
                    IsDefault = false,
                    RowVersion = [1]
                });

                db.Set<StockTransfer>().Add(new StockTransfer
                {
                    Id = stockTransferId,
                    FromWarehouseId = sourceWarehouseId,
                    ToWarehouseId = targetWarehouseId,
                    Status = TransferStatus.Draft,
                    RowVersion = [1]
                });

                db.Set<StockTransferLine>().Add(new StockTransferLine
                {
                    Id = Guid.NewGuid(),
                    StockTransferId = stockTransferId,
                    ProductVariantId = WebAdminTestFactory.TestProductVariantId,
                    Quantity = 4
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        var listPath =
            $"/Inventory/StockTransfers?businessId=44444444-4444-4444-4444-444444444444&warehouseId={sourceWarehouseId}&q={Uri.EscapeDataString(transferTargetWarehouseName)}";

        using var tokenResponse = await SendHtmxGetAsync(client, listPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(tokenHtml, "rowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            $"/Inventory/UpdateStockTransferLifecycle?id={stockTransferId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = stockTransferId.ToString(),
                    ["rowVersion"] = rowVersion,
                    ["action"] = "MarkInTransit",
                    ["businessId"] = "44444444-4444-4444-4444-444444444444",
                    ["warehouseId"] = sourceWarehouseId.ToString(),
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["q"] = transferTargetWarehouseName,
                    ["filter"] = "All"
                },
                tokenHtml));

        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (response.Headers.TryGetValues("HX-Redirect", out var redirectValues))
        {
            using var redirectedResponse = await SendHtmxGetAsync(client, redirectValues.Single());
            redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            responseHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("Concurrency");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedPurchaseOrderLifecycle_WithMissingRowVersion_ShouldShowValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        var (purchaseOrderId, orderNumber) = await CreatePurchaseOrderLifecycleSeedAsync(client);
        var listPath =
            $"/Inventory/PurchaseOrders?businessId=44444444-4444-4444-4444-444444444444&q={Uri.EscapeDataString(orderNumber)}";

        using var tokenResponse = await SendHtmxGetAsync(client, listPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            $"/Inventory/UpdatePurchaseOrderLifecycle?id={purchaseOrderId}",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["id"] = purchaseOrderId.ToString(),
                ["rowVersion"] = string.Empty,
                ["action"] = "Issue",
                ["businessId"] = "44444444-4444-4444-4444-444444444444",
                ["page"] = "1",
                ["pageSize"] = "20",
                ["q"] = orderNumber,
                ["filter"] = "All"
            });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedPurchaseOrderLifecycle_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        var (purchaseOrderId, orderNumber) = await CreatePurchaseOrderLifecycleSeedAsync(client);
        var listPath =
            $"/Inventory/PurchaseOrders?businessId=44444444-4444-4444-4444-444444444444&q={Uri.EscapeDataString(orderNumber)}";

        using var initialTokenResponse = await SendHtmxGetAsync(client, listPath);
        initialTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var initialTokenHtml = await initialTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(initialTokenHtml, "rowVersion");

        using var firstResponse = await SendHtmxPostAsync(
            client,
            $"/Inventory/UpdatePurchaseOrderLifecycle?id={purchaseOrderId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = purchaseOrderId.ToString(),
                    ["rowVersion"] = staleRowVersion,
                    ["action"] = "Issue",
                    ["businessId"] = "44444444-4444-4444-4444-444444444444",
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["q"] = orderNumber,
                    ["filter"] = "All"
                },
                initialTokenHtml));
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstResponse.Headers.TryGetValues("Content-Security-Policy", out var firstResponseCspValues).Should().BeTrue();
        firstResponseCspValues!.Single().Should().Contain("form-action 'self'");

        using var staleResponse = await SendHtmxPostAsync(
            client,
            $"/Inventory/UpdatePurchaseOrderLifecycle?id={purchaseOrderId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = purchaseOrderId.ToString(),
                    ["rowVersion"] = staleRowVersion,
                    ["action"] = "Receive",
                    ["businessId"] = "44444444-4444-4444-4444-444444444444",
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["q"] = orderNumber,
                    ["filter"] = "All"
                },
                initialTokenHtml));
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues))
        {
            using var staleRedirectedResponse = await SendHtmxGetAsync(client, staleRedirectValues.Single());
            staleRedirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            staleResponseHtml = await staleRedirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponseHtml.Should().Contain("Concurrency");
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleResponseCspValues).Should().BeTrue();
        staleResponseCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedPurchaseOrderLifecycle_WithPostSaveConcurrency_ShouldSurfaceFailureMessage()
    {
        var supplierId = Guid.NewGuid();
        var purchaseOrderId = Guid.NewGuid();
        var orderNumber = $"PO-{Guid.NewGuid():N}";

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Supplier>().Add(new Supplier
                {
                    Id = supplierId,
                    BusinessId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = "Smoke Supplier PostSave",
                    Email = $"postsave-{Guid.NewGuid():N}@example.test",
                    Phone = "+49301234567",
                    Address = "Supplier Street 1, Berlin",
                    Notes = "Seeded for purchase-order post-save concurrency smoke coverage.",
                    RowVersion = [1]
                });

                db.Set<PurchaseOrder>().Add(new PurchaseOrder
                {
                    Id = purchaseOrderId,
                    SupplierId = supplierId,
                    BusinessId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Status = PurchaseOrderStatus.Draft,
                    OrderNumber = orderNumber,
                    OrderedAtUtc = DateTime.UtcNow,
                    RowVersion = [1]
                });

                db.Set<PurchaseOrderLine>().Add(new PurchaseOrderLine
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = purchaseOrderId,
                    ProductVariantId = WebAdminTestFactory.TestProductVariantId,
                    Quantity = 6,
                    UnitCostMinor = 700,
                    TotalCostMinor = 4200
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        var listPath =
            $"/Inventory/PurchaseOrders?businessId=44444444-4444-4444-4444-444444444444&q={Uri.EscapeDataString(orderNumber)}";

        using var tokenResponse = await SendHtmxGetAsync(client, listPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(tokenHtml, "rowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            $"/Inventory/UpdatePurchaseOrderLifecycle?id={purchaseOrderId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = purchaseOrderId.ToString(),
                    ["rowVersion"] = rowVersion,
                    ["action"] = "Issue",
                    ["businessId"] = "44444444-4444-4444-4444-444444444444",
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["q"] = orderNumber,
                    ["filter"] = "All"
                },
                tokenHtml));
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (response.Headers.TryGetValues("HX-Redirect", out var redirectValues))
        {
            using var redirectedResponse = await SendHtmxGetAsync(client, redirectValues.Single());
            redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            responseHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("Concurrency");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedPurchaseOrderLifecycle_WithCaseInsensitiveAction_ShouldBeAcceptedAndExecuted()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();

        var (purchaseOrderId, orderNumber) = await CreatePurchaseOrderLifecycleSeedAsync(client);

        await PostValidPurchaseOrderLifecycleActionAndAssertStatusAsync(
            client,
            purchaseOrderId,
            orderNumber,
            "  iSsUe  ",
            "Issued");
    }

    [Fact]
    public async Task AuthenticatedBillingWebhookDelivery_WithWhitespaceAction_ShouldBeTrimmedAndCaseInsensitive()
    {
        var webhookSubscriptionId = Guid.NewGuid();
        var webhookDeliveryId = Guid.NewGuid();
        var webhookIdempotencyKey = $"smoke-webhook-{Guid.NewGuid():N}";

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<WebhookSubscription>().Add(new WebhookSubscription
                {
                    Id = webhookSubscriptionId,
                    EventType = "charge.succeeded",
                    CallbackUrl = "https://localhost/webhooks/smoke",
                    Secret = "smoke-secret",
                    IsActive = true,
                    RowVersion = [1]
                });

                db.Set<WebhookDelivery>().Add(new WebhookDelivery
                {
                    Id = webhookDeliveryId,
                    SubscriptionId = webhookSubscriptionId,
                    Status = "Pending",
                    RetryCount = 0,
                    ResponseCode = null,
                    IdempotencyKey = webhookIdempotencyKey,
                    RowVersion = [1]
                });
            });

        var listPath =
            $"/Billing/Webhooks?q={Uri.EscapeDataString(webhookIdempotencyKey)}";

        using var listResponse = await SendHtmxGetAsync(client, listPath);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listHtml = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var listRowVersion = ExtractHiddenInputValue(listHtml, "rowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            $"/Billing/UpdateWebhookDelivery?id={webhookDeliveryId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = webhookDeliveryId.ToString(),
                    ["rowVersion"] = listRowVersion,
                    ["action"] = "  reQueue  ",
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["q"] = webhookIdempotencyKey
                },
                listHtml));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedBillingWebhookDelivery_WithPostSaveConcurrency_ShouldSurfaceFailureMessage()
    {
        var webhookSubscriptionId = Guid.NewGuid();
        var webhookDeliveryId = Guid.NewGuid();
        var webhookIdempotencyKey = $"smoke-webhook-{Guid.NewGuid():N}";

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<WebhookSubscription>().Add(new WebhookSubscription
                {
                    Id = webhookSubscriptionId,
                    EventType = "charge.succeeded",
                    CallbackUrl = "https://localhost/webhooks/smoke",
                    Secret = "smoke-secret",
                    IsActive = true,
                    RowVersion = [1]
                });

                db.Set<WebhookDelivery>().Add(new WebhookDelivery
                {
                    Id = webhookDeliveryId,
                    SubscriptionId = webhookSubscriptionId,
                    Status = "Pending",
                    RetryCount = 0,
                    ResponseCode = null,
                    IdempotencyKey = webhookIdempotencyKey,
                    RowVersion = [1]
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        var listPath =
            $"/Billing/Webhooks?q={Uri.EscapeDataString(webhookIdempotencyKey)}";

        using var listResponse = await SendHtmxGetAsync(client, listPath);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listHtml = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var listRowVersion = ExtractHiddenInputValue(listHtml, "rowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            $"/Billing/UpdateWebhookDelivery?id={webhookDeliveryId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = webhookDeliveryId.ToString(),
                    ["rowVersion"] = listRowVersion,
                    ["action"] = "ReQueue",
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["q"] = webhookIdempotencyKey
                },
                listHtml));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var redirectValues).Should().BeTrue();
        var redirectUrl = redirectValues!.Single();
        using var redirectedResponse = await SendHtmxGetAsync(client, redirectUrl);
        var redirectedHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        redirectedHtml.Should().Contain("Concurrency");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedBillingWebhookDelivery_WithReQueue_ShouldClearRetryStateForNextWorkerAttempt()
    {
        var webhookSubscriptionId = Guid.NewGuid();
        var webhookDeliveryId = Guid.NewGuid();
        var webhookIdempotencyKey = $"smoke-webhook-{Guid.NewGuid():N}";
        var seededAttemptAtUtc = DateTime.UtcNow.AddMinutes(-25);
        var seededAttemptText = seededAttemptAtUtc
            .ToLocalTime()
            .ToString(System.Globalization.CultureInfo.CurrentCulture);

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<WebhookSubscription>().Add(new WebhookSubscription
                {
                    Id = webhookSubscriptionId,
                    EventType = "charge.succeeded",
                    CallbackUrl = "https://localhost/webhooks/smoke",
                    Secret = "smoke-secret",
                    IsActive = true,
                    RowVersion = [1]
                });

                db.Set<WebhookDelivery>().Add(new WebhookDelivery
                {
                    Id = webhookDeliveryId,
                    SubscriptionId = webhookSubscriptionId,
                    Status = "Failed",
                    RetryCount = 5,
                    ResponseCode = 500,
                    LastAttemptAtUtc = seededAttemptAtUtc,
                    IdempotencyKey = webhookIdempotencyKey,
                    RowVersion = [1]
                });
            });

        var listPath =
            $"/Billing/Webhooks?q={Uri.EscapeDataString(webhookIdempotencyKey)}";

        using var initialListResponse = await SendHtmxGetAsync(client, listPath);
        initialListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var initialListHtml = await initialListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var initialRowHtml = ExtractTableRowContainingText(initialListHtml, webhookIdempotencyKey);
        initialRowHtml.Should().Contain("<div>5</div>");
        initialRowHtml.Should().Contain(seededAttemptText);

        using var response = await SendHtmxPostAsync(
            client,
            $"/Billing/UpdateWebhookDelivery?id={webhookDeliveryId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = webhookDeliveryId.ToString(),
                    ["rowVersion"] = ExtractHiddenInputValue(initialListHtml, "rowVersion"),
                    ["action"] = "ReQueue",
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["q"] = webhookIdempotencyKey
                },
                initialListHtml));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");

        var updatedListPath =
            $"/Billing/Webhooks?q={Uri.EscapeDataString(webhookIdempotencyKey)}";
        using var updatedListResponse = await SendHtmxGetAsync(client, updatedListPath);
        updatedListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedListHtml = await updatedListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var updatedRowHtml = ExtractTableRowContainingText(updatedListHtml, webhookIdempotencyKey);
        updatedRowHtml.Should().Contain("<div>0</div>");
        updatedRowHtml.Should().NotContain("<div>5</div>");
        updatedRowHtml.Should().NotContain(seededAttemptText);
    }

    [Fact]
    public async Task AuthenticatedPaymentDisputeReview_WithWhitespaceAction_ShouldBeTrimmedAndCaseInsensitive()
    {
        var businessId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var paymentId = Guid.NewGuid();
        var paymentIntentRef = $"smoke-payment-{Guid.NewGuid():N}";

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Payment>().Add(new Payment
                {
                    Id = paymentId,
                    BusinessId = businessId,
                    Status = PaymentStatus.Failed,
                    Currency = "EUR",
                    AmountMinor = 2599,
                    Provider = "Stripe",
                    ProviderPaymentIntentRef = paymentIntentRef,
                    UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    RowVersion = [1]
                });
            });

        var listPath =
            $"/Billing/Payments?businessId={businessId}&q={Uri.EscapeDataString(paymentIntentRef)}";

        using var listResponse = await SendHtmxGetAsync(client, listPath);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listHtml = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(listHtml, "rowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            $"/Billing/UpdatePaymentDisputeReview?id={paymentId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = paymentId.ToString(),
                    ["rowVersion"] = rowVersion,
                    ["action"] = "  EvIdEnCeSuBmItTeD  ",
                    ["businessId"] = businessId.ToString(),
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["q"] = paymentIntentRef,
                    ["queue"] = ""
                },
                listHtml));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedPaymentDisputeReview_WithPostSaveConcurrency_ShouldSurfaceFailureMessage()
    {
        var businessId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var paymentId = Guid.NewGuid();
        var paymentIntentRef = $"smoke-payment-{Guid.NewGuid():N}";

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Payment>().Add(new Payment
                {
                    Id = paymentId,
                    BusinessId = businessId,
                    Status = PaymentStatus.Failed,
                    Currency = "EUR",
                    AmountMinor = 2599,
                    Provider = "Stripe",
                    ProviderPaymentIntentRef = paymentIntentRef,
                    UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    RowVersion = [1]
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        var listPath =
            $"/Billing/Payments?businessId={businessId}&q={Uri.EscapeDataString(paymentIntentRef)}";

        using var listResponse = await SendHtmxGetAsync(client, listPath);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listHtml = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(listHtml, "rowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            $"/Billing/UpdatePaymentDisputeReview?id={paymentId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = paymentId.ToString(),
                    ["rowVersion"] = rowVersion,
                    ["action"] = "  EvIdEnCeSuBmItTeD  ",
                    ["businessId"] = businessId.ToString(),
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["q"] = paymentIntentRef,
                    ["queue"] = ""
                },
                listHtml));
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("Concurrency");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Theory]
    [InlineData(PaymentStatus.Failed, PaymentStatus.Captured)]
    [InlineData(PaymentStatus.Refunded, PaymentStatus.Captured)]
    [InlineData(PaymentStatus.Voided, PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Completed, PaymentStatus.Captured)]
    public async Task AuthenticatedPaymentEdit_WithUnsupportedStatusTransition_ShouldSurfaceFailureMessage(
        PaymentStatus fromStatus,
        PaymentStatus toStatus)
    {
        var businessId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var paymentId = Guid.NewGuid();
        var providerRef = $"smoke-payment-{Guid.NewGuid():N}";

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Payment>().Add(new Payment
                {
                    Id = paymentId,
                    BusinessId = businessId,
                    Status = fromStatus,
                    Currency = "EUR",
                    AmountMinor = 2599,
                    Provider = "Stripe",
                    ProviderPaymentIntentRef = providerRef,
                    UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    RowVersion = [1]
                });
            });

        var editPath = $"/Billing/EditPayment?id={paymentId}";

        using var editResponse = await SendHtmxGetAsync(client, editPath);
        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var editHtml = await editResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/Billing/EditPayment",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = paymentId.ToString(),
                    ["RowVersion"] = ExtractHiddenInputValue(editHtml, "RowVersion"),
                    ["BusinessId"] = businessId.ToString(),
                    ["Status"] = ((int)toStatus).ToString(),
                    ["AmountMinor"] = ExtractHiddenInputValue(editHtml, "AmountMinor"),
                    ["Currency"] = ExtractHiddenInputValue(editHtml, "Currency"),
                    ["Provider"] = ExtractHiddenInputValue(editHtml, "Provider")
                },
                editHtml));
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("Unable to update the payment right now.");
        response.Headers.TryGetValues("HX-Redirect", out var _).Should().BeFalse();
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedPaymentEdit_WithMissingRowVersion_ShouldSurfaceValidationError()
    {
        var businessId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var paymentId = Guid.NewGuid();
        var providerRef = $"smoke-payment-{Guid.NewGuid():N}";

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Payment>().Add(new Payment
                {
                    Id = paymentId,
                    BusinessId = businessId,
                    Status = PaymentStatus.Captured,
                    Currency = "EUR",
                    AmountMinor = 2599,
                    Provider = "Stripe",
                    ProviderPaymentIntentRef = providerRef,
                    UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    RowVersion = [1]
                });
            });

        var editPath = $"/Billing/EditPayment?id={paymentId}";

        using var editResponse = await SendHtmxGetAsync(client, editPath);
        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var editHtml = await editResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/Billing/EditPayment",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = paymentId.ToString(),
                    ["RowVersion"] = string.Empty,
                    ["BusinessId"] = businessId.ToString(),
                    ["Status"] = ((int)PaymentStatus.Captured).ToString(),
                    ["AmountMinor"] = "2599",
                    ["Currency"] = "EUR",
                    ["Provider"] = "Stripe"
                },
                editHtml));
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedPaymentEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        var businessId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var paymentId = Guid.NewGuid();
        var providerRef = $"smoke-payment-{Guid.NewGuid():N}";

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Payment>().Add(new Payment
                {
                    Id = paymentId,
                    BusinessId = businessId,
                    Status = PaymentStatus.Captured,
                    Currency = "EUR",
                    AmountMinor = 2599,
                    Provider = "Stripe",
                    ProviderPaymentIntentRef = providerRef,
                    UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    RowVersion = [1]
                });
            });

        var editPath = $"/Billing/EditPayment?id={paymentId}";

        using var baselineEditResponse = await SendHtmxGetAsync(client, editPath);
        baselineEditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineEditHtml = await baselineEditResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineEditHtml, "RowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Billing/EditPayment",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = paymentId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["BusinessId"] = businessId.ToString(),
                    ["Status"] = ((int)PaymentStatus.Captured).ToString(),
                    ["AmountMinor"] = "2599",
                    ["Currency"] = "EUR",
                    ["Provider"] = "Stripe"
                },
                baselineEditHtml));
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Billing/EditPayment",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = paymentId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["BusinessId"] = businessId.ToString(),
                    ["Status"] = ((int)PaymentStatus.Captured).ToString(),
                    ["AmountMinor"] = "2599",
                    ["Currency"] = "EUR",
                    ["Provider"] = "Stripe"
                },
                baselineEditHtml));
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues).Should().BeTrue();
        using var staleRedirectedResponse = await SendHtmxGetAsync(client, staleRedirectValues!.Single());
        var staleRedirectedHtml = await staleRedirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        staleRedirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleRedirectedHtml.Should().Contain("Concurrency conflict. Reload the payment and try again.");
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleCspValues).Should().BeTrue();
        staleCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedPaymentEdit_WithPostSaveConcurrency_ShouldSurfaceFailureMessage()
    {
        var businessId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var paymentId = Guid.NewGuid();
        var providerRef = $"smoke-payment-{Guid.NewGuid():N}";

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Payment>().Add(new Payment
                {
                    Id = paymentId,
                    BusinessId = businessId,
                    Status = PaymentStatus.Captured,
                    Currency = "EUR",
                    AmountMinor = 2599,
                    Provider = "Stripe",
                    ProviderPaymentIntentRef = providerRef,
                    UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    RowVersion = [1]
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        var editPath = $"/Billing/EditPayment?id={paymentId}";

        using var editResponse = await SendHtmxGetAsync(client, editPath);
        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var editHtml = await editResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(editHtml, "RowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            "/Billing/EditPayment",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = paymentId.ToString(),
                    ["RowVersion"] = rowVersion,
                    ["BusinessId"] = businessId.ToString(),
                    ["Status"] = ((int)PaymentStatus.Captured).ToString(),
                    ["AmountMinor"] = "2599",
                    ["Currency"] = "EUR",
                    ["Provider"] = "Stripe"
                },
                editHtml));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var redirectValues).Should().BeTrue();
        using var redirectedResponse = await SendHtmxGetAsync(client, redirectValues!.Single());
        var redirectedHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        redirectedHtml.Should().Contain("Concurrency conflict. Reload the payment and try again.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedFinancialAccountEdit_WithMissingRowVersion_ShouldShowValidationError()
    {
        var businessId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var financialAccountId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<FinancialAccount>().Add(new FinancialAccount
                {
                    Id = financialAccountId,
                    BusinessId = businessId,
                    Name = "Smoke financial account",
                    Type = AccountType.Asset,
                    Code = "smoke-asset-qa",
                    RowVersion = [1]
                });
            });

        var editPath = $"/Billing/EditFinancialAccount?id={financialAccountId}";

        using var editResponse = await SendHtmxGetAsync(client, editPath);
        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var editHtml = await editResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/Billing/EditFinancialAccount",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = financialAccountId.ToString(),
                    ["RowVersion"] = string.Empty,
                    ["BusinessId"] = businessId.ToString(),
                    ["Name"] = "Smoke financial account - missing row version",
                    ["Type"] = "Asset",
                    ["Code"] = "smoke-asset-qa-missing"
                },
                editHtml));
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedFinancialAccountEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        var businessId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var financialAccountId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<FinancialAccount>().Add(new FinancialAccount
                {
                    Id = financialAccountId,
                    BusinessId = businessId,
                    Name = "Smoke financial account",
                    Type = AccountType.Asset,
                    Code = "smoke-asset-qa",
                    RowVersion = [1]
                });
            });

        var editPath = $"/Billing/EditFinancialAccount?id={financialAccountId}";

        using var baselineEditResponse = await SendHtmxGetAsync(client, editPath);
        baselineEditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineEditHtml = await baselineEditResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineEditHtml, "RowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Billing/EditFinancialAccount",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = financialAccountId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["BusinessId"] = businessId.ToString(),
                    ["Name"] = "Smoke financial account - first",
                    ["Type"] = "Asset",
                    ["Code"] = "smoke-asset-qa-updated"
                },
                baselineEditHtml));
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Billing/EditFinancialAccount",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = financialAccountId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["BusinessId"] = businessId.ToString(),
                    ["Name"] = "Smoke financial account - stale",
                    ["Type"] = "Asset",
                    ["Code"] = "smoke-asset-qa-stale"
                },
                baselineEditHtml));
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues).Should().BeTrue();
        using var staleRedirectedResponse = await SendHtmxGetAsync(client, staleRedirectValues!.Single());
        var staleRedirectedHtml = await staleRedirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        staleRedirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleRedirectedHtml.Should().Contain("Concurrency conflict. Reload the financial account and try again.");
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleCspValues).Should().BeTrue();
        staleCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedFinancialAccountEdit_WithPostSaveConcurrency_ShouldSurfaceFailureMessage()
    {
        var businessId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var financialAccountId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<FinancialAccount>().Add(new FinancialAccount
                {
                    Id = financialAccountId,
                    BusinessId = businessId,
                    Name = "Smoke financial account",
                    Type = AccountType.Asset,
                    Code = "smoke-asset-qa",
                    RowVersion = [1]
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        var editPath = $"/Billing/EditFinancialAccount?id={financialAccountId}";

        using var editResponse = await SendHtmxGetAsync(client, editPath);
        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var editHtml = await editResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(editHtml, "RowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            "/Billing/EditFinancialAccount",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = financialAccountId.ToString(),
                    ["RowVersion"] = rowVersion,
                    ["BusinessId"] = businessId.ToString(),
                    ["Name"] = "Smoke financial account - post-save",
                    ["Type"] = "Asset",
                    ["Code"] = "smoke-asset-qa-postsave"
                },
                editHtml));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var redirectValues).Should().BeTrue();
        using var redirectedResponse = await SendHtmxGetAsync(client, redirectValues!.Single());
        var redirectedHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        redirectedHtml.Should().Contain("Concurrency conflict. Reload the financial account and try again.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedExpenseEdit_WithMissingRowVersion_ShouldShowValidationError()
    {
        var businessId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var expenseId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Expense>().Add(new Expense
                {
                    Id = expenseId,
                    BusinessId = businessId,
                    SupplierId = null,
                    Category = "Smoke",
                    AmountMinor = 3499,
                    Description = "Smoke expense baseline",
                    ExpenseDateUtc = new DateTime(2026, 4, 24),
                    RowVersion = [1]
                });
            });

        var editPath = $"/Billing/EditExpense?id={expenseId}";

        using var editResponse = await SendHtmxGetAsync(client, editPath);
        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var editHtml = await editResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/Billing/EditExpense",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = expenseId.ToString(),
                    ["RowVersion"] = string.Empty,
                    ["BusinessId"] = businessId.ToString(),
                    ["SupplierId"] = string.Empty,
                    ["ExpenseDateUtc"] = "2026-04-24",
                    ["Category"] = "Smoke",
                    ["AmountMinor"] = "3499",
                    ["Description"] = "Smoke expense baseline"
                },
                editHtml));
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedExpenseEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        var businessId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var expenseId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Expense>().Add(new Expense
                {
                    Id = expenseId,
                    BusinessId = businessId,
                    SupplierId = null,
                    Category = "Smoke",
                    AmountMinor = 3499,
                    Description = "Smoke expense baseline",
                    ExpenseDateUtc = new DateTime(2026, 4, 24),
                    RowVersion = [1]
                });
            });

        var editPath = $"/Billing/EditExpense?id={expenseId}";

        using var baselineEditResponse = await SendHtmxGetAsync(client, editPath);
        baselineEditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineEditHtml = await baselineEditResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineEditHtml, "RowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Billing/EditExpense",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = expenseId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["BusinessId"] = businessId.ToString(),
                    ["SupplierId"] = string.Empty,
                    ["ExpenseDateUtc"] = "2026-04-24",
                    ["Category"] = "Smoke",
                    ["AmountMinor"] = "3499",
                    ["Description"] = "Smoke expense updated"
                },
                baselineEditHtml));
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Billing/EditExpense",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = expenseId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["BusinessId"] = businessId.ToString(),
                    ["SupplierId"] = string.Empty,
                    ["ExpenseDateUtc"] = "2026-04-24",
                    ["Category"] = "Smoke",
                    ["AmountMinor"] = "3499",
                    ["Description"] = "Smoke expense stale"
                },
                baselineEditHtml));
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues).Should().BeTrue();
        using var staleRedirectedResponse = await SendHtmxGetAsync(client, staleRedirectValues!.Single());
        var staleRedirectedHtml = await staleRedirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        staleRedirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleRedirectedHtml.Should().Contain("Concurrency conflict. Reload the expense and try again.");
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleCspValues).Should().BeTrue();
        staleCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedExpenseEdit_WithPostSaveConcurrency_ShouldSurfaceFailureMessage()
    {
        var businessId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var expenseId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Expense>().Add(new Expense
                {
                    Id = expenseId,
                    BusinessId = businessId,
                    SupplierId = null,
                    Category = "Smoke",
                    AmountMinor = 3499,
                    Description = "Smoke expense baseline",
                    ExpenseDateUtc = new DateTime(2026, 4, 24),
                    RowVersion = [1]
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        var editPath = $"/Billing/EditExpense?id={expenseId}";

        using var editResponse = await SendHtmxGetAsync(client, editPath);
        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var editHtml = await editResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(editHtml, "RowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            "/Billing/EditExpense",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = expenseId.ToString(),
                    ["RowVersion"] = rowVersion,
                    ["BusinessId"] = businessId.ToString(),
                    ["SupplierId"] = string.Empty,
                    ["ExpenseDateUtc"] = "2026-04-24",
                    ["Category"] = "Smoke",
                    ["AmountMinor"] = "3499",
                    ["Description"] = "Smoke expense post-save"
                },
                editHtml));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var redirectValues).Should().BeTrue();
        using var redirectedResponse = await SendHtmxGetAsync(client, redirectValues!.Single());
        var redirectedHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        redirectedHtml.Should().Contain("Concurrency conflict. Reload the expense and try again.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedJournalEntryEdit_WithMissingRowVersion_ShouldShowValidationError()
    {
        var businessId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var journalEntryId = Guid.NewGuid();
        var firstAccountId = Guid.NewGuid();
        var secondAccountId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<FinancialAccount>().Add(new FinancialAccount
                {
                    Id = firstAccountId,
                    BusinessId = businessId,
                    Name = "QA Asset",
                    Type = AccountType.Asset,
                    Code = "smoke-asset-a",
                    RowVersion = [1]
                });
                db.Set<FinancialAccount>().Add(new FinancialAccount
                {
                    Id = secondAccountId,
                    BusinessId = businessId,
                    Name = "QA Revenue",
                    Type = AccountType.Revenue,
                    Code = "smoke-revenue-a",
                    RowVersion = [1]
                });
                db.Set<JournalEntry>().Add(new JournalEntry
                {
                    Id = journalEntryId,
                    BusinessId = businessId,
                    EntryDateUtc = new DateTime(2026, 4, 24),
                    Description = "Smoke journal baseline",
                    RowVersion = [1],
                    Lines = new List<JournalEntryLine>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            AccountId = firstAccountId,
                            DebitMinor = 1000,
                            CreditMinor = 0,
                            Memo = "Debit line"
                        },
                        new()
                        {
                            Id = Guid.NewGuid(),
                            AccountId = secondAccountId,
                            DebitMinor = 0,
                            CreditMinor = 1000,
                            Memo = "Credit line"
                        }
                    }
                });
            });

        var editPath = $"/Billing/EditJournalEntry?id={journalEntryId}";

        using var editResponse = await SendHtmxGetAsync(client, editPath);
        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var editHtml = await editResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var lineOneId = ExtractHiddenInputValue(editHtml, "Lines[0].Id");
        var lineTwoId = ExtractHiddenInputValue(editHtml, "Lines[1].Id");

        using var response = await SendHtmxPostAsync(
            client,
            "/Billing/EditJournalEntry",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = journalEntryId.ToString(),
                    ["RowVersion"] = string.Empty,
                    ["BusinessId"] = businessId.ToString(),
                    ["EntryDateUtc"] = "2026-04-24",
                    ["Description"] = "Smoke journal baseline",
                    ["Lines[0].Id"] = lineOneId,
                    ["Lines[0].AccountId"] = firstAccountId.ToString(),
                    ["Lines[0].DebitMinor"] = "1000",
                    ["Lines[0].CreditMinor"] = "0",
                    ["Lines[0].Memo"] = "Debit line",
                    ["Lines[1].Id"] = lineTwoId,
                    ["Lines[1].AccountId"] = secondAccountId.ToString(),
                    ["Lines[1].DebitMinor"] = "0",
                    ["Lines[1].CreditMinor"] = "1000",
                    ["Lines[1].Memo"] = "Credit line"
                },
                editHtml));
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedJournalEntryEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        var businessId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var journalEntryId = Guid.NewGuid();
        var lineOneId = Guid.NewGuid();
        var lineTwoId = Guid.NewGuid();
        var firstAccountId = Guid.NewGuid();
        var secondAccountId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<FinancialAccount>().Add(new FinancialAccount
                {
                    Id = firstAccountId,
                    BusinessId = businessId,
                    Name = "QA Asset",
                    Type = AccountType.Asset,
                    Code = "smoke-asset-b",
                    RowVersion = [1]
                });
                db.Set<FinancialAccount>().Add(new FinancialAccount
                {
                    Id = secondAccountId,
                    BusinessId = businessId,
                    Name = "QA Revenue",
                    Type = AccountType.Revenue,
                    Code = "smoke-revenue-b",
                    RowVersion = [1]
                });
                db.Set<JournalEntry>().Add(new JournalEntry
                {
                    Id = journalEntryId,
                    BusinessId = businessId,
                    EntryDateUtc = new DateTime(2026, 4, 24),
                    Description = "Smoke journal baseline",
                    RowVersion = [1],
                    Lines = new List<JournalEntryLine>
                    {
                        new()
                        {
                            Id = lineOneId,
                            AccountId = firstAccountId,
                            DebitMinor = 1000,
                            CreditMinor = 0,
                            Memo = "Debit line"
                        },
                        new()
                        {
                            Id = lineTwoId,
                            AccountId = secondAccountId,
                            DebitMinor = 0,
                            CreditMinor = 1000,
                            Memo = "Credit line"
                        }
                    }
                });
            });

        var editPath = $"/Billing/EditJournalEntry?id={journalEntryId}";

        using var baselineEditResponse = await SendHtmxGetAsync(client, editPath);
        baselineEditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineEditHtml = await baselineEditResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineEditHtml, "RowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Billing/EditJournalEntry",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = journalEntryId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["BusinessId"] = businessId.ToString(),
                    ["EntryDateUtc"] = "2026-04-24",
                    ["Description"] = "Smoke journal first",
                    ["Lines[0].Id"] = lineOneId.ToString(),
                    ["Lines[0].AccountId"] = firstAccountId.ToString(),
                    ["Lines[0].DebitMinor"] = "1000",
                    ["Lines[0].CreditMinor"] = "0",
                    ["Lines[0].Memo"] = "Debit line updated",
                    ["Lines[1].Id"] = lineTwoId.ToString(),
                    ["Lines[1].AccountId"] = secondAccountId.ToString(),
                    ["Lines[1].DebitMinor"] = "0",
                    ["Lines[1].CreditMinor"] = "1000",
                    ["Lines[1].Memo"] = "Credit line"
                },
                baselineEditHtml));
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Billing/EditJournalEntry",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = journalEntryId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["BusinessId"] = businessId.ToString(),
                    ["EntryDateUtc"] = "2026-04-24",
                    ["Description"] = "Smoke journal stale",
                    ["Lines[0].Id"] = lineOneId.ToString(),
                    ["Lines[0].AccountId"] = firstAccountId.ToString(),
                    ["Lines[0].DebitMinor"] = "1000",
                    ["Lines[0].CreditMinor"] = "0",
                    ["Lines[0].Memo"] = "Debit line stale",
                    ["Lines[1].Id"] = lineTwoId.ToString(),
                    ["Lines[1].AccountId"] = secondAccountId.ToString(),
                    ["Lines[1].DebitMinor"] = "0",
                    ["Lines[1].CreditMinor"] = "1000",
                    ["Lines[1].Memo"] = "Credit line"
                },
                baselineEditHtml));
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues).Should().BeTrue();
        using var staleRedirectedResponse = await SendHtmxGetAsync(client, staleRedirectValues!.Single());
        var staleRedirectedHtml = await staleRedirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        staleRedirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleRedirectedHtml.Should().Contain("Concurrency conflict. Reload the journal entry and try again.");
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleCspValues).Should().BeTrue();
        staleCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedJournalEntryEdit_WithPostSaveConcurrency_ShouldSurfaceFailureMessage()
    {
        var businessId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var journalEntryId = Guid.NewGuid();
        var lineOneId = Guid.NewGuid();
        var lineTwoId = Guid.NewGuid();
        var firstAccountId = Guid.NewGuid();
        var secondAccountId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<FinancialAccount>().Add(new FinancialAccount
                {
                    Id = firstAccountId,
                    BusinessId = businessId,
                    Name = "QA Asset",
                    Type = AccountType.Asset,
                    Code = "smoke-asset-c",
                    RowVersion = [1]
                });
                db.Set<FinancialAccount>().Add(new FinancialAccount
                {
                    Id = secondAccountId,
                    BusinessId = businessId,
                    Name = "QA Revenue",
                    Type = AccountType.Revenue,
                    Code = "smoke-revenue-c",
                    RowVersion = [1]
                });
                db.Set<JournalEntry>().Add(new JournalEntry
                {
                    Id = journalEntryId,
                    BusinessId = businessId,
                    EntryDateUtc = new DateTime(2026, 4, 24),
                    Description = "Smoke journal baseline",
                    RowVersion = [1],
                    Lines = new List<JournalEntryLine>
                    {
                        new()
                        {
                            Id = lineOneId,
                            AccountId = firstAccountId,
                            DebitMinor = 1000,
                            CreditMinor = 0,
                            Memo = "Debit line"
                        },
                        new()
                        {
                            Id = lineTwoId,
                            AccountId = secondAccountId,
                            DebitMinor = 0,
                            CreditMinor = 1000,
                            Memo = "Credit line"
                        }
                    }
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        var editPath = $"/Billing/EditJournalEntry?id={journalEntryId}";

        using var editResponse = await SendHtmxGetAsync(client, editPath);
        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var editHtml = await editResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(editHtml, "RowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            "/Billing/EditJournalEntry",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = journalEntryId.ToString(),
                    ["RowVersion"] = rowVersion,
                    ["BusinessId"] = businessId.ToString(),
                    ["EntryDateUtc"] = "2026-04-24",
                    ["Description"] = "Smoke journal post-save",
                    ["Lines[0].Id"] = lineOneId.ToString(),
                    ["Lines[0].AccountId"] = firstAccountId.ToString(),
                    ["Lines[0].DebitMinor"] = "1000",
                    ["Lines[0].CreditMinor"] = "0",
                    ["Lines[0].Memo"] = "Debit line",
                    ["Lines[1].Id"] = lineTwoId.ToString(),
                    ["Lines[1].AccountId"] = secondAccountId.ToString(),
                    ["Lines[1].DebitMinor"] = "0",
                    ["Lines[1].CreditMinor"] = "1000",
                    ["Lines[1].Memo"] = "Credit line"
                },
                editHtml));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var redirectValues).Should().BeTrue();
        using var redirectedResponse = await SendHtmxGetAsync(client, redirectValues!.Single());
        var redirectedHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        redirectedHtml.Should().Contain("Concurrency conflict. Reload the journal entry and try again.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedSetSubscriptionCancelAtPeriodEnd_WithMissingRowVersion_ShouldSurfaceFailureMessage()
    {
        var businessId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var subscriptionId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<BusinessSubscription>().Add(new BusinessSubscription
                {
                    Id = subscriptionId,
                    BusinessId = businessId,
                    BillingPlanId = WebAdminTestFactory.TestBillingPlanId,
                    Provider = "Stripe",
                    Status = SubscriptionStatus.Active,
                    StartedAtUtc = new DateTime(2026, 4, 1),
                    UnitPriceMinor = 1990,
                    Currency = "EUR",
                    CancelAtPeriodEnd = false,
                    TrialEndsAtUtc = new DateTime(2026, 4, 30),
                    RowVersion = [1]
                });
            });

        var subscriptionPath = $"/Businesses/Subscription?businessId={businessId}";
        using var subscriptionResponse = await SendHtmxGetAsync(client, subscriptionPath);
        subscriptionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var subscriptionHtml = await subscriptionResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(
            client,
            "/Businesses/SetSubscriptionCancelAtPeriodEnd",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["businessId"] = businessId.ToString(),
                    ["subscriptionId"] = subscriptionId.ToString(),
                    ["rowVersion"] = string.Empty,
                    ["cancelAtPeriodEnd"] = "true"
                },
                subscriptionHtml));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var redirectValues).Should().BeTrue();
        using var redirectedResponse = await SendHtmxGetAsync(client, redirectValues!.Single());
        var redirectedHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        redirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        redirectedHtml.Should().Contain("Failed to update subscription cancellation policy.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedSetSubscriptionCancelAtPeriodEnd_WithStaleRowVersion_ShouldSurfaceFailureMessage()
    {
        var businessId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var subscriptionId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<BusinessSubscription>().Add(new BusinessSubscription
                {
                    Id = subscriptionId,
                    BusinessId = businessId,
                    BillingPlanId = WebAdminTestFactory.TestBillingPlanId,
                    Provider = "Stripe",
                    Status = SubscriptionStatus.Active,
                    StartedAtUtc = new DateTime(2026, 4, 1),
                    UnitPriceMinor = 1990,
                    Currency = "EUR",
                    CancelAtPeriodEnd = false,
                    TrialEndsAtUtc = new DateTime(2026, 4, 30),
                    RowVersion = [1]
                });
            });

        var subscriptionPath = $"/Businesses/Subscription?businessId={businessId}";
        using var subscriptionResponse = await SendHtmxGetAsync(client, subscriptionPath);
        subscriptionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var subscriptionHtml = await subscriptionResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(subscriptionHtml, "rowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Businesses/SetSubscriptionCancelAtPeriodEnd",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["businessId"] = businessId.ToString(),
                    ["subscriptionId"] = subscriptionId.ToString(),
                    ["rowVersion"] = staleRowVersion,
                    ["cancelAtPeriodEnd"] = "true"
                },
                subscriptionHtml));
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Businesses/SetSubscriptionCancelAtPeriodEnd",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["businessId"] = businessId.ToString(),
                    ["subscriptionId"] = subscriptionId.ToString(),
                    ["rowVersion"] = staleRowVersion,
                    ["cancelAtPeriodEnd"] = "false"
                },
                subscriptionHtml));
        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues).Should().BeTrue();
        using var staleRedirectedResponse = await SendHtmxGetAsync(client, staleRedirectValues!.Single());
        var staleRedirectedHtml = await staleRedirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        staleRedirectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleRedirectedHtml.Should().Contain("Failed to update subscription cancellation policy.");
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleCspValues).Should().BeTrue();
        staleCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedProviderCallback_WithWhitespaceAction_ShouldBeTrimmedAndCaseInsensitive()
    {
        var callbackId = Guid.NewGuid();
        var callbackIdempotencyKey = $"smoke-callback-{Guid.NewGuid():N}";

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
                {
                    Id = callbackId,
                    Provider = "Stripe",
                    CallbackType = "charge.succeeded",
                    PayloadJson = $$"""{"event":"{{callbackIdempotencyKey}}"}""",
                    Status = "Pending",
                    AttemptCount = 1,
                    IdempotencyKey = callbackIdempotencyKey,
                    RowVersion = [1]
                });
            });

        var listPath =
            $"/BusinessCommunications/ProviderCallbacks?provider=Stripe&query={Uri.EscapeDataString(callbackIdempotencyKey)}";

        using var listResponse = await SendHtmxGetAsync(client, listPath);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listHtml = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(listHtml, "rowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            $"/BusinessCommunications/UpdateProviderCallback?id={callbackId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = callbackId.ToString(),
                    ["rowVersion"] = rowVersion,
                    ["action"] = "  reQueue  ",
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["query"] = callbackIdempotencyKey,
                    ["provider"] = "Stripe",
                    ["status"] = "",
                    ["stalePendingOnly"] = "false",
                    ["failedOnly"] = "false",
                    ["deliveryFailureOnly"] = "false"
                },
                listHtml));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedProviderCallback_WithReQueue_ShouldClearRetryStateForNextWorkerAttempt()
    {
        var callbackId = Guid.NewGuid();
        var callbackIdempotencyKey = $"smoke-callback-{Guid.NewGuid():N}";
        var seededAttemptAtUtc = DateTime.UtcNow.AddMinutes(-18);
        var seededAttemptText = seededAttemptAtUtc.ToString("yyyy-MM-dd HH:mm");

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
                {
                    Id = callbackId,
                    Provider = "Stripe",
                    CallbackType = "charge.succeeded",
                    PayloadJson = $$"""{"event":"{{callbackIdempotencyKey}}"}""",
                    Status = "Failed",
                    AttemptCount = 8,
                    LastAttemptAtUtc = seededAttemptAtUtc,
                    IdempotencyKey = callbackIdempotencyKey,
                    RowVersion = [1]
                });
            });

        var listPath =
            $"/BusinessCommunications/ProviderCallbacks?provider=Stripe&query={Uri.EscapeDataString(callbackIdempotencyKey)}";

        using var initialListResponse = await SendHtmxGetAsync(client, listPath);
        initialListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var initialListHtml = await initialListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var initialRowHtml = ExtractTableRowContainingText(initialListHtml, callbackIdempotencyKey);
        initialRowHtml.Should().Contain("<div>8</div>");
        initialRowHtml.Should().Contain(seededAttemptText);

        using var response = await SendHtmxPostAsync(
            client,
            $"/BusinessCommunications/UpdateProviderCallback?id={callbackId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = callbackId.ToString(),
                    ["rowVersion"] = ExtractHiddenInputValue(initialListHtml, "rowVersion"),
                    ["action"] = "ReQueue",
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["query"] = callbackIdempotencyKey,
                    ["provider"] = "Stripe",
                    ["status"] = "",
                    ["stalePendingOnly"] = "false",
                    ["failedOnly"] = "false",
                    ["deliveryFailureOnly"] = "false"
                },
                initialListHtml));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");

        using var updatedListResponse = await SendHtmxGetAsync(
            client,
            listPath);
        updatedListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedListHtml = await updatedListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var updatedRowHtml = ExtractTableRowContainingText(updatedListHtml, callbackIdempotencyKey);
        updatedRowHtml.Should().Contain("<div>0</div>");
        updatedRowHtml.Should().NotContain("<div>8</div>");
        updatedRowHtml.Should().NotContain(seededAttemptText);
    }

    [Fact]
    public async Task AuthenticatedProviderCallback_WithPostSaveConcurrency_ShouldSurfaceFailureMessage()
    {
        var callbackId = Guid.NewGuid();
        var callbackIdempotencyKey = $"smoke-callback-{Guid.NewGuid():N}";

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
                {
                    Id = callbackId,
                    Provider = "Stripe",
                    CallbackType = "charge.succeeded",
                    PayloadJson = $$"""{"event":"{{callbackIdempotencyKey}}"}""",
                    Status = "Pending",
                    AttemptCount = 1,
                    IdempotencyKey = callbackIdempotencyKey,
                    RowVersion = [1]
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        var listPath =
            $"/BusinessCommunications/ProviderCallbacks?provider=Stripe&query={Uri.EscapeDataString(callbackIdempotencyKey)}";

        using var listResponse = await SendHtmxGetAsync(client, listPath);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listHtml = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(listHtml, "rowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            $"/BusinessCommunications/UpdateProviderCallback?id={callbackId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = callbackId.ToString(),
                    ["rowVersion"] = rowVersion,
                    ["action"] = "ReQueue",
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["query"] = callbackIdempotencyKey,
                    ["provider"] = "Stripe",
                    ["status"] = "",
                    ["stalePendingOnly"] = "false",
                    ["failedOnly"] = "false",
                    ["deliveryFailureOnly"] = "false"
                },
                listHtml));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var redirectValues).Should().BeTrue();
        using var redirectedResponse = await SendHtmxGetAsync(client, redirectValues!.Single());
        var redirectedHtml = await redirectedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        redirectedHtml.Should().Contain("Concurrency");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedLeadLifecycle_WithWhitespaceAction_ShouldBeTrimmedAndCaseInsensitive()
    {
        var firstName = $"Smoke{Guid.NewGuid():N}";
        var lastName = $"Lead{Guid.NewGuid():N}";
        var email = $"lead-whitespace-{Guid.NewGuid():N}@example.test";

        using var createLeadResponse = await SendHtmxGetAsync(_factory.CreateAuthenticatedDatabaseNoRedirectClient(), "/Crm/CreateLead");
        using var createLeadClient = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var createLeadHtml = await createLeadResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var seedLeadResponse = await SendHtmxPostAsync(
            createLeadClient,
            "/Crm/CreateLead",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(createLeadHtml),
                ["FirstName"] = firstName,
                ["LastName"] = lastName,
                ["CompanyName"] = "Smoke Lead Operations",
                ["Status"] = "New",
                ["Email"] = email,
                ["Phone"] = "+493012345678",
                ["AssignedToUserId"] = string.Empty,
                ["CustomerId"] = string.Empty,
                ["Source"] = "Smoke",
                ["Notes"] = "Smoke lead lifecycle trim/case test."
            });
        seedLeadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        seedLeadResponse.Headers.TryGetValues("HX-Redirect", out var createLeadRedirectValues).Should().BeTrue();
        createLeadRedirectValues!.Single().Should().Contain("/Crm/Leads");

        using var leadListResponse = await SendHtmxGetAsync(
            createLeadClient,
            $"/Crm/Leads?query={Uri.EscapeDataString(email)}");
        leadListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var leadListHtml = await leadListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var leadId = ExtractHrefQueryGuid(leadListHtml, "/Crm/EditLead", "id");
        var rowVersion = ExtractHiddenInputValue(leadListHtml, "rowVersion");

        using var response = await SendHtmxPostAsync(
            createLeadClient,
            $"/Crm/UpdateLeadLifecycle?id={leadId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = leadId.ToString(),
                    ["rowVersion"] = rowVersion,
                    ["action"] = "  qUaLiFy  ",
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["q"] = email,
                    ["filter"] = "All"
                },
                leadListHtml));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedOpportunityLifecycle_WithWhitespaceAction_ShouldBeTrimmedAndCaseInsensitive()
    {
        using var leadClient = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var customerEmail = $"smoke-opportunity-owner-{Guid.NewGuid():N}@example.test";
        var customerFirstName = $"Opportunity{Guid.NewGuid():N}";
        var customerLastName = $"Owner{Guid.NewGuid():N}";
        var opportunityTitle = $"Smoke Opportunity {Guid.NewGuid():N}";
        var opportunityCustomerId = await CreateTestCustomerAndReturnIdAsync(
            leadClient,
            customerFirstName,
            customerLastName,
            customerEmail);

        using var createOpportunityGetResponse = await SendHtmxGetAsync(leadClient, $"/Crm/CreateOpportunity?customerId={opportunityCustomerId}");
        createOpportunityGetResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createOpportunityHtml = await createOpportunityGetResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var createOpportunityPostResponse = await SendHtmxPostAsync(
            leadClient,
            "/Crm/CreateOpportunity",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(createOpportunityHtml),
                ["CustomerId"] = opportunityCustomerId.ToString(),
                ["Title"] = opportunityTitle,
                ["Stage"] = "Prospect",
                ["EstimatedValueMinor"] = "109900",
                ["Currency"] = "EUR",
                ["ExpectedCloseDateUtc"] = "2026-06-24",
                ["AssignedToUserId"] = string.Empty,
                ["ExpectedCloseDateDisplay"] = "2026-06-24",
                ["CustomerId"] = opportunityCustomerId.ToString(),
                ["Notes"] = "Smoke opportunity lifecycle trim/case test."
            });
        createOpportunityPostResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        createOpportunityPostResponse.Headers.TryGetValues("HX-Redirect", out var createOpportunityRedirectValues).Should().BeTrue();
        createOpportunityRedirectValues!.Single().Should().Contain("/Crm/Opportunities");

        using var opportunityListResponse = await SendHtmxGetAsync(
            leadClient,
            $"/Crm/Opportunities?query={Uri.EscapeDataString(opportunityTitle)}");
        opportunityListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var opportunityListHtml = await opportunityListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var opportunityId = ExtractHrefQueryGuid(opportunityListHtml, "/Crm/EditOpportunity", "id");
        var opportunityRowVersion = ExtractHiddenInputValue(opportunityListHtml, "rowVersion");

        using var response = await SendHtmxPostAsync(
            leadClient,
            $"/Crm/UpdateOpportunityLifecycle?id={opportunityId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = opportunityId.ToString(),
                    ["rowVersion"] = opportunityRowVersion,
                    ["action"] = "  aDvAnCe  ",
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["q"] = opportunityTitle,
                    ["filter"] = "All"
                },
                opportunityListHtml));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedOpportunityLifecycle_WithClosedOpportunity_ShouldNotAdvanceOrTransitionAgain()
    {
        using var leadClient = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var customerEmail = $"smoke-opportunity-owner-{Guid.NewGuid():N}@example.test";
        var customerFirstName = $"Opportunity{Guid.NewGuid():N}";
        var customerLastName = $"Owner{Guid.NewGuid():N}";
        var opportunityTitle = $"Smoke Opportunity {Guid.NewGuid():N}";
        var opportunityCustomerId = await CreateTestCustomerAndReturnIdAsync(
            leadClient,
            customerFirstName,
            customerLastName,
            customerEmail);

        using var createOpportunityGetResponse = await SendHtmxGetAsync(leadClient, $"/Crm/CreateOpportunity?customerId={opportunityCustomerId}");
        createOpportunityGetResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createOpportunityHtml = await createOpportunityGetResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var createOpportunityPostResponse = await SendHtmxPostAsync(
            leadClient,
            "/Crm/CreateOpportunity",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(createOpportunityHtml),
                ["CustomerId"] = opportunityCustomerId.ToString(),
                ["Title"] = opportunityTitle,
                ["Stage"] = "Prospect",
                ["EstimatedValueMinor"] = "109900",
                ["Currency"] = "EUR",
                ["ExpectedCloseDateUtc"] = "2026-06-24",
                ["AssignedToUserId"] = string.Empty,
                ["ExpectedCloseDateDisplay"] = "2026-06-24",
                ["CustomerId"] = opportunityCustomerId.ToString(),
                ["Notes"] = "Smoke opportunity lifecycle close-then-advance test."
            });
        createOpportunityPostResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        createOpportunityPostResponse.Headers.TryGetValues("HX-Redirect", out var createOpportunityRedirectValues).Should().BeTrue();
        createOpportunityRedirectValues!.Single().Should().Contain("/Crm/Opportunities");

        using var opportunityListResponse = await SendHtmxGetAsync(
            leadClient,
            $"/Crm/Opportunities?query={Uri.EscapeDataString(opportunityTitle)}");
        opportunityListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var opportunityListHtml = await opportunityListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var opportunityId = ExtractHrefQueryGuid(opportunityListHtml, "/Crm/EditOpportunity", "id");
        var closeRowVersion = ExtractHiddenInputValue(opportunityListHtml, "rowVersion");

        using var closeResponse = await SendHtmxPostAsync(
            leadClient,
            $"/Crm/UpdateOpportunityLifecycle?id={opportunityId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = opportunityId.ToString(),
                    ["rowVersion"] = closeRowVersion,
                    ["action"] = "CloseWon",
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["q"] = opportunityTitle,
                    ["filter"] = "All"
                },
                opportunityListHtml));
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        closeResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();
        closeResponse.Headers.TryGetValues("Content-Security-Policy", out var closeCspValues).Should().BeTrue();
        closeCspValues!.Single().Should().Contain("form-action 'self'");

        using var closedListResponse = await SendHtmxGetAsync(
            leadClient,
            $"/Crm/Opportunities?query={Uri.EscapeDataString(opportunityTitle)}");
        closedListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var closedListHtml = await closedListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var closedRowVersion = ExtractHiddenInputValue(closedListHtml, "rowVersion");
        var closedRowHtml = ExtractTableRowContainingText(closedListHtml, opportunityTitle);
        closedRowHtml.Should().Contain("Closed won");

        using var advanceResponse = await SendHtmxPostAsync(
            leadClient,
            $"/Crm/UpdateOpportunityLifecycle?id={opportunityId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = opportunityId.ToString(),
                    ["rowVersion"] = closedRowVersion,
                    ["action"] = "Advance",
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["q"] = opportunityTitle,
                    ["filter"] = "All"
                },
                closedListHtml));
        advanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        advanceResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();
        advanceResponse.Headers.TryGetValues("Content-Security-Policy", out var advanceCspValues).Should().BeTrue();
        advanceCspValues!.Single().Should().Contain("form-action 'self'");

        using var finalListResponse = await SendHtmxGetAsync(
            leadClient,
            $"/Crm/Opportunities?query={Uri.EscapeDataString(opportunityTitle)}");
        finalListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var finalListHtml = await finalListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var finalRowHtml = ExtractTableRowContainingText(finalListHtml, opportunityTitle);
        finalRowHtml.Should().Contain("Closed won");
    }

    [Fact]
    public async Task AuthenticatedShipmentProviderOperation_WithWhitespaceAction_ShouldBeTrimmedAndCaseInsensitive()
    {
        var shipmentOperationId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<ShipmentProviderOperation>().Add(new ShipmentProviderOperation
                {
                    Id = shipmentOperationId,
                    ShipmentId = WebAdminTestFactory.TestDhlLabelShipmentId,
                    Provider = "DHL",
                    OperationType = "CreateShipment",
                    Status = "Failed",
                    AttemptCount = 2,
                    RowVersion = [1]
                });
            });

        using var listResponse = await SendHtmxGetAsync(
            client,
            "/Orders/ShipmentProviderOperations?provider=DHL&operationType=CreateShipment&status=Failed");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listHtml = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(listHtml, "rowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            $"/Orders/UpdateShipmentProviderOperation?id={shipmentOperationId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = shipmentOperationId.ToString(),
                    ["rowVersion"] = rowVersion,
                    ["action"] = "  mArKePrOcEsSeD  ",
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["query"] = "",
                    ["provider"] = "DHL",
                    ["operationType"] = "CreateShipment",
                    ["status"] = "Failed"
                },
                listHtml));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedShipmentProviderOperation_WithReQueue_ShouldClearRetryStateForNextWorkerAttempt()
    {
        var shipmentOperationId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var operationMarker = shipmentOperationId.ToString();
        var seededAttemptAtUtc = DateTime.UtcNow.AddMinutes(-22);
        var seededAttemptText = seededAttemptAtUtc.ToString("yyyy-MM-dd HH:mm");

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<Shipment>().Add(new Shipment
                {
                    Id = shipmentId,
                    OrderId = WebAdminTestFactory.TestOrderId,
                    Carrier = "DHL",
                    Service = "DHL-SMOKE-REQUEUE",
                    ProviderShipmentReference = $"DHL-REQUEUE-{shipmentOperationId:N}",
                    TotalWeight = 250,
                    Status = ShipmentStatus.Packed,
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                    CreatedByUserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    RowVersion = [1]
                });
                db.Set<ShipmentProviderOperation>().Add(new ShipmentProviderOperation
                {
                    Id = shipmentOperationId,
                    ShipmentId = shipmentId,
                    Provider = "DHL",
                    OperationType = "CreateShipment",
                    Status = "Failed",
                    AttemptCount = 11,
                    LastAttemptAtUtc = seededAttemptAtUtc,
                    RowVersion = [1]
                });
            });

        var listPath =
            "/Orders/ShipmentProviderOperations?provider=DHL&operationType=CreateShipment&status=Failed";

        using var initialListResponse = await SendHtmxGetAsync(client, listPath);
        initialListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var initialListHtml = await initialListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var initialRowHtml = ExtractTableRowContainingText(initialListHtml, operationMarker);
        initialRowHtml.Should().Contain("<div>11</div>");
        initialRowHtml.Should().Contain(seededAttemptText);

        using var response = await SendHtmxPostAsync(
            client,
            $"/Orders/UpdateShipmentProviderOperation?id={shipmentOperationId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = shipmentOperationId.ToString(),
                    ["rowVersion"] = ExtractHiddenInputValue(initialListHtml, "rowVersion"),
                    ["action"] = "ReQueue",
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["query"] = string.Empty,
                    ["provider"] = "DHL",
                    ["operationType"] = "CreateShipment",
                    ["status"] = "Failed"
                },
                initialListHtml));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");

        using var updatedListResponse = await SendHtmxGetAsync(
            client,
            "/Orders/ShipmentProviderOperations?provider=DHL&operationType=CreateShipment&status=Pending");
        updatedListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedListHtml = await updatedListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var updatedRowHtml = ExtractTableRowContainingText(updatedListHtml, operationMarker);
        updatedRowHtml.Should().Contain("<div>0</div>");
        updatedRowHtml.Should().NotContain("<div>11</div>");
        updatedRowHtml.Should().NotContain(seededAttemptText);
    }

    [Fact]
    public async Task AuthenticatedShipmentProviderOperation_WithPostSaveConcurrency_ShouldSurfaceFailureMessage()
    {
        var shipmentOperationId = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient(
            seedDatabase: db =>
            {
                db.Set<ShipmentProviderOperation>().Add(new ShipmentProviderOperation
                {
                    Id = shipmentOperationId,
                    ShipmentId = WebAdminTestFactory.TestDhlLabelShipmentId,
                    Provider = "DHL",
                    OperationType = "CreateShipment",
                    Status = "Failed",
                    AttemptCount = 2,
                    RowVersion = [1]
                });
            },
            configureServices: services =>
            {
                services.RemoveAll<IAppDbContext>();
                services.AddScoped<IAppDbContext>(sp => new ConcurrencyThrowingAppDbContext(
                    sp.GetRequiredService<DarwinDbContext>()));
            });

        using var listResponse = await SendHtmxGetAsync(
            client,
            "/Orders/ShipmentProviderOperations?provider=DHL&operationType=CreateShipment&status=Failed");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listHtml = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var rowVersion = ExtractHiddenInputValue(listHtml, "rowVersion");

        using var response = await SendHtmxPostAsync(
            client,
            $"/Orders/UpdateShipmentProviderOperation?id={shipmentOperationId}",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["id"] = shipmentOperationId.ToString(),
                    ["rowVersion"] = rowVersion,
                    ["action"] = "MarkProcessed",
                    ["page"] = "1",
                    ["pageSize"] = "20",
                    ["query"] = string.Empty,
                    ["provider"] = "DHL",
                    ["operationType"] = "CreateShipment",
                    ["status"] = "Failed"
                },
                listHtml));
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        if (response.Headers.TryGetValues("HX-Redirect", out var redirectValues))
        {
            using var redirectResponse = await SendHtmxGetAsync(client, redirectValues.Single());
            responseHtml = await redirectResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            redirectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        responseHtml.Should().Contain("Concurrency");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    private static async Task<Guid> CreateTestCustomerAndReturnIdAsync(
        HttpClient client,
        string firstName,
        string lastName,
        string email)
    {
        using var createCustomerResponse = await SendHtmxGetAsync(client, "/Crm/CreateCustomer");
        createCustomerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createCustomerHtml = await createCustomerResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var createResponse = await SendHtmxPostAsync(
            client,
            "/Crm/CreateCustomer",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(createCustomerHtml),
                ["UserId"] = string.Empty,
                ["CompanyName"] = string.Empty,
                ["TaxProfileType"] = "Consumer",
                ["VatId"] = string.Empty,
                ["FirstName"] = firstName,
                ["LastName"] = lastName,
                ["Email"] = email,
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
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        createResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();

        using var customerListResponse = await SendHtmxGetAsync(
            client,
            $"/Crm/Customers?query={Uri.EscapeDataString(email)}");
        customerListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var customerListHtml = await customerListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return ExtractHrefQueryGuid(customerListHtml, "/Crm/EditCustomer", "id");
    }

    private static async Task<(Guid TransferId, Guid SourceWarehouseId, string TargetWarehouseName)> CreateStockTransferLifecycleSeedAsync(
        HttpClient client)
    {
        var warehouseFromName = $"Smoke-Source-{Guid.NewGuid():N}";
        var warehouseToName = $"Smoke-Target-{Guid.NewGuid():N}";

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
                ["Location"] = "Berlin Source",
                ["Description"] = "Smoke-created source warehouse.",
                ["IsDefault"] = "true"
            });

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
                ["Location"] = "Berlin Target",
                ["Description"] = "Smoke-created destination warehouse.",
                ["IsDefault"] = "false"
            });

        var warehouseFromId = ExtractQueryGuid(warehouseFromRedirect, "id");
        var warehouseToId = ExtractQueryGuid(warehouseToRedirect, "id");

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

        var stockTransferRedirect = await PostValidEditorMutationAndAssertListedAsync(
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

        return (
            ExtractQueryGuid(stockTransferRedirect, "id"),
            warehouseFromId,
            warehouseToName);
    }

    private static async Task<(Guid PurchaseOrderId, string OrderNumber)> CreatePurchaseOrderLifecycleSeedAsync(
        HttpClient client)
    {
        var supplierName = $"SmokeSupplier-{Guid.NewGuid():N}";
        var supplierEmail = $"s-{Guid.NewGuid():N}@example.test";

        var supplierRedirect = await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Inventory/CreateSupplier?businessId=44444444-4444-4444-4444-444444444444",
            "/Inventory/CreateSupplier",
            $"/Inventory/Suppliers?businessId=44444444-4444-4444-4444-444444444444&q={Uri.EscapeDataString(supplierName)}",
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

        var orderNumber = $"PO-{Guid.NewGuid():N}";

        var purchaseOrderRedirect = await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Inventory/CreatePurchaseOrder?businessId=44444444-4444-4444-4444-444444444444",
            "/Inventory/CreatePurchaseOrder",
            $"/Inventory/PurchaseOrders?businessId=44444444-4444-4444-4444-444444444444&q={Uri.EscapeDataString(orderNumber)}",
            orderNumber,
            new Dictionary<string, string>
            {
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["SupplierId"] = supplierId.ToString(),
                ["Status"] = "Draft",
                ["OrderNumber"] = orderNumber,
                ["OrderedAtUtc"] = "2026-04-24T12:00:00",
                ["Lines[0].ProductVariantId"] = WebAdminTestFactory.TestProductVariantId.ToString(),
                ["Lines[0].Quantity"] = "6",
                ["Lines[0].UnitCostMinor"] = "700",
                ["Lines[0].TotalCostMinor"] = "4200"
            });

        return (ExtractQueryGuid(purchaseOrderRedirect, "id"), orderNumber);
    }

    private static async Task<Guid> CreateTestWarehouseAndReturnIdAsync(
        HttpClient client,
        string warehouseName,
        string location,
        string description,
        bool isDefault)
    {
        var warehouseRedirect = await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Inventory/CreateWarehouse?businessId=44444444-4444-4444-4444-444444444444",
            "/Inventory/CreateWarehouse",
            $"/Inventory/Warehouses?businessId=44444444-4444-4444-4444-444444444444&q={Uri.EscapeDataString(warehouseName)}",
            warehouseName,
            new Dictionary<string, string>
            {
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["Name"] = warehouseName,
                ["Location"] = location,
                ["Description"] = description,
                ["IsDefault"] = isDefault.ToString().ToLowerInvariant()
            });

        return ExtractQueryGuid(warehouseRedirect, "id");
    }

    private static async Task<Guid> CreateTestSupplierAndReturnIdAsync(
        HttpClient client,
        string supplierName,
        string supplierEmail)
    {
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

        return ExtractQueryGuid(supplierRedirect, "id");
    }

    private static async Task<(Guid WarehouseId, Guid StockLevelId)> CreateTestStockLevelAndReturnIdAsync(HttpClient client)
    {
        var warehouseName = $"Smoke-StockLevel-{Guid.NewGuid():N}";
        var warehouseId = await CreateTestWarehouseAndReturnIdAsync(
            client,
            warehouseName,
            "Berlin StockLevel",
            "Smoke stock-level warehouse.",
            false);

        var stockLevelRedirect = await PostValidEditorMutationAndAssertListedAsync(
            client,
            $"/Inventory/CreateStockLevel?businessId=44444444-4444-4444-4444-444444444444&warehouseId={warehouseId}",
            "/Inventory/CreateStockLevel",
            $"/Inventory/StockLevels?businessId=44444444-4444-4444-4444-444444444444&warehouseId={warehouseId}&q=WEBADMIN-SMOKE-VARIANT",
            "WEBADMIN-SMOKE-VARIANT",
            new Dictionary<string, string>
            {
                ["WarehouseId"] = warehouseId.ToString(),
                ["ProductVariantId"] = WebAdminTestFactory.TestProductVariantId.ToString(),
                ["AvailableQuantity"] = "25",
                ["ReservedQuantity"] = "2",
                ["ReorderPoint"] = "5",
                ["ReorderQuantity"] = "20",
                ["InTransitQuantity"] = "3"
            });

        return (warehouseId, ExtractQueryGuid(stockLevelRedirect, "id"));
    }

    private static async Task<Guid> CreateTestSegmentAndReturnIdAsync(
        HttpClient client,
        string segmentName)
    {
        using var createSegmentTokenResponse = await SendHtmxGetAsync(client, "/Crm/CreateSegment");
        createSegmentTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createSegmentTokenHtml = await createSegmentTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var createSegmentResponse = await SendHtmxPostAsync(
            client,
            "/Crm/CreateSegment",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(createSegmentTokenHtml),
                ["Name"] = segmentName,
                ["Description"] = "Smoke-created CRM segment."
            });
        createSegmentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        createSegmentResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();

        using var segmentListResponse = await SendHtmxGetAsync(
            client,
            $"/Crm/Segments?q={Uri.EscapeDataString(segmentName)}");
        segmentListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var segmentListHtml = await segmentListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return ExtractHrefQueryGuid(segmentListHtml, "/Crm/EditSegment", "id");
    }

    private static async Task<Guid> CreateTestInvoiceAndReturnIdAsync(
        HttpClient client,
        Guid orderId,
        Guid paymentId)
    {
        const string createPath = "/Orders/CreateInvoice";

        using var createInvoiceTokenResponse = await SendHtmxGetAsync(client, $"/Orders/CreateInvoice?orderId={orderId}");
        createInvoiceTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createInvoiceTokenHtml = await createInvoiceTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var createInvoiceResponse = await SendHtmxPostAsync(
            client,
            createPath,
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(createInvoiceTokenHtml),
                ["OrderId"] = orderId.ToString(),
                ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
                ["CustomerId"] = string.Empty,
                ["PaymentId"] = paymentId == Guid.Empty ? string.Empty : paymentId.ToString(),
                ["DueAtUtc"] = $"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss}"
            });
        createInvoiceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        createInvoiceResponse.Headers.TryGetValues("HX-Redirect", out var createRedirectValues).Should().BeTrue();
        var createInvoiceRedirect = createRedirectValues!.Single();

        using var invoiceListResponse = await SendHtmxGetAsync(client, createInvoiceRedirect);
        invoiceListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var invoiceListHtml = await invoiceListResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        return ExtractHrefQueryGuid(invoiceListHtml, "/Crm/EditInvoice", "id");
    }

    private static Dictionary<string, string> AddRequestVerificationToken(
        Dictionary<string, string> form,
        string tokenHtml)
    {
        form["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml);
        return form;
    }

    private static string BuildReadyInvoiceSnapshot(Guid invoiceId)
        => $$"""
        {
          "invoiceId": "{{invoiceId}}",
          "currency": "EUR",
          "issuedAtUtc": "2026-05-01T00:00:00Z",
          "totalGrossMinor": 11900,
          "issuer": {
            "legalName": "Darwin GmbH",
            "taxId": "DE123456789",
            "addressLine1": "Issuer Street 1",
            "postalCode": "10115",
            "city": "Berlin",
            "country": "DE"
          },
          "customer": {
            "companyName": "Customer GmbH",
            "addressLine1": "Customer Street 2",
            "postalCode": "10115",
            "city": "Berlin",
            "country": "DE"
          },
          "lines": [
            {
              "id": "11111111-1111-1111-1111-111111111111",
              "description": "Invoice line",
              "quantity": 1,
              "unitPriceNetMinor": 10000,
              "totalNetMinor": 10000,
              "totalGrossMinor": 11900
            }
          ]
        }
        """;

    private sealed class RecordingEInvoiceGenerationService : IEInvoiceGenerationService
    {
        private readonly EInvoiceGenerationResult _result;

        public RecordingEInvoiceGenerationService(EInvoiceGenerationResult result)
        {
            _result = result;
        }

        public int Calls { get; private set; }
        public EInvoiceArtifactFormat? LastFormat { get; private set; }
        public Guid LastInvoiceId { get; private set; }

        public Task<EInvoiceGenerationResult> GenerateAsync(
            Invoice invoice,
            EInvoiceGenerationRequest request,
            CancellationToken ct = default)
        {
            Calls++;
            LastFormat = request.Format;
            LastInvoiceId = invoice.Id;
            return Task.FromResult(_result);
        }
    }

    private sealed class RecordingEInvoiceArtifactStorage : IEInvoiceArtifactStorage
    {
        public int Calls { get; private set; }
        public EInvoiceArtifact? Artifact { get; private set; }

        public Task<EInvoiceArtifactStorageResult> SaveAsync(EInvoiceArtifact artifact, CancellationToken ct = default)
        {
            Calls++;
            Artifact = artifact;
            return Task.FromResult(new EInvoiceArtifactStorageResult(
                "TestStorage",
                "invoice-archive",
                $"invoices/{artifact.InvoiceId}/e-invoice/{artifact.FileName}",
                "version-1",
                "sha256-test",
                artifact.Content.LongLength,
                artifact.GeneratedAtUtc,
                artifact.GeneratedAtUtc.AddYears(10),
                true));
        }
    }

    private sealed class ConcurrencyThrowingAppDbContext : IAppDbContext
    {
        private readonly DarwinDbContext _inner;

        public ConcurrencyThrowingAppDbContext(DarwinDbContext inner)
        {
            _inner = inner;
        }

        public DbSet<T> Set<T>() where T : class => _inner.Set<T>();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => throw new DbUpdateConcurrencyException("Simulated concurrency conflict after row-version check.");
    }

    [Fact]
    public async Task AuthenticatedBillingPlanEdit_WithMissingRowVersion_ShouldShowValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var editorPath = $"/Billing/EditPlan?id={WebAdminTestFactory.TestBillingPlanId}";
        const string updatedName = "Smoke Billing Plan missing row version";

        using var tokenResponse = await SendHtmxGetAsync(client, editorPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(client, "/Billing/EditPlan", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
            ["Id"] = WebAdminTestFactory.TestBillingPlanId.ToString(),
            ["RowVersion"] = string.Empty,
            ["Code"] = ExtractHiddenInputValue(tokenHtml, "Code"),
            ["Name"] = updatedName,
            ["Description"] = "Smoke billing plan with missing row version.",
            ["PriceMinor"] = "1290",
            ["Currency"] = "EUR",
            ["Interval"] = "Month",
            ["IntervalCount"] = "1",
            ["TrialDays"] = "14",
            ["IsActive"] = "false",
            ["FeaturesJson"] = "{\"smoke\":true,\"missingRowVersion\":true}"
        });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedBillingPlanEdit_WithInvalidRowVersion_ShouldSurfaceValidationError()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var editorPath = $"/Billing/EditPlan?id={WebAdminTestFactory.TestBillingPlanId}";

        using var tokenResponse = await SendHtmxGetAsync(client, editorPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(client, "/Billing/EditPlan", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
            ["Id"] = WebAdminTestFactory.TestBillingPlanId.ToString(),
            ["RowVersion"] = "not-valid-base64!!!",
            ["Code"] = ExtractHiddenInputValue(tokenHtml, "Code"),
            ["Name"] = "Smoke Billing Plan invalid row version",
            ["Description"] = "Smoke billing plan with invalid row version.",
            ["PriceMinor"] = "1290",
            ["Currency"] = "EUR",
            ["Interval"] = "Month",
            ["IntervalCount"] = "1",
            ["TrialDays"] = "14",
            ["IsActive"] = "false",
            ["FeaturesJson"] = "{\"smoke\":true,\"invalidRowVersion\":true}"
        });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseHtml.Should().Contain("RowVersion is required.");
        responseHtml.Should().NotContain("FormatException");
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedBillingPlanEdit_WithStaleRowVersion_ShouldSurfaceConcurrencyMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var editorPath = $"/Billing/EditPlan?id={WebAdminTestFactory.TestBillingPlanId}";

        using var baselineTokenResponse = await SendHtmxGetAsync(client, editorPath);
        baselineTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineTokenHtml = await baselineTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineTokenHtml, "RowVersion");
        var planCode = ExtractHiddenInputValue(baselineTokenHtml, "Code");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Billing/EditPlan",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = WebAdminTestFactory.TestBillingPlanId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["Code"] = planCode,
                    ["Name"] = "Smoke Billing Plan stale v1",
                    ["Description"] = "Smoke-updated inactive billing plan.",
                    ["PriceMinor"] = "1290",
                    ["Currency"] = "EUR",
                    ["Interval"] = "Month",
                    ["IntervalCount"] = "1",
                    ["TrialDays"] = "14",
                    ["IsActive"] = "false",
                    ["FeaturesJson"] = "{\"smoke\":true,\"stale\":false}"
                },
                baselineTokenHtml));
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();

        using var staleTokenResponse = await SendHtmxGetAsync(client, editorPath);
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleTokenHtml = await staleTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Billing/EditPlan",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["Id"] = WebAdminTestFactory.TestBillingPlanId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["Code"] = planCode,
                    ["Name"] = "Smoke Billing Plan stale v2",
                    ["Description"] = "Smoke-updated inactive billing plan.",
                    ["PriceMinor"] = "1290",
                    ["Currency"] = "EUR",
                    ["Interval"] = "Month",
                    ["IntervalCount"] = "1",
                    ["TrialDays"] = "14",
                    ["IsActive"] = "false",
                    ["FeaturesJson"] = "{\"smoke\":true,\"stale\":true}"
                },
                staleTokenHtml));
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleResponse.Headers.TryGetValues("HX-Redirect", out var staleRedirectValues).Should().BeTrue();
        var staleRedirect = staleRedirectValues!.Single();
        using var staleEditorResponse = await SendHtmxGetAsync(client, staleRedirect);
        var staleEditorHtml = await staleEditorResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        staleEditorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        staleEditorHtml.Should().Contain("Concurrency conflict. Reload the billing plan and try again.");
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleCspValues).Should().BeTrue();
        staleCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedRolePermissionsEdit_WithMissingRowVersion_ShouldShowFailureMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var editorPath = $"/Roles/Permissions?id={WebAdminTestFactory.TestRoleId}";

        using var tokenResponse = await SendHtmxGetAsync(client, editorPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(client, "/Roles/Permissions", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
            ["RoleId"] = WebAdminTestFactory.TestRoleId.ToString(),
            ["RowVersion"] = string.Empty,
            ["SelectedPermissionIds"] = WebAdminTestFactory.TestPermissionId.ToString()
        });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (
            responseHtml.Contains("RowVersion is required.", StringComparison.Ordinal) ||
            responseHtml.Contains("RowVersion ist erforderlich.", StringComparison.Ordinal) ||
            responseHtml.Contains("Failed to update role permissions.", StringComparison.Ordinal) ||
            responseHtml.Contains("Die Rollenberechtigungen konnten nicht aktualisiert werden.", StringComparison.Ordinal))
            .Should().BeTrue();
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedRolePermissionsEdit_WithStaleRowVersion_ShouldSurfaceFailureMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var editorPath = $"/Roles/Permissions?id={WebAdminTestFactory.TestRoleId}";

        using var baselineTokenResponse = await SendHtmxGetAsync(client, editorPath);
        baselineTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineTokenHtml = await baselineTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineTokenHtml, "RowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(client, "/Roles/Permissions", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(baselineTokenHtml),
            ["RoleId"] = WebAdminTestFactory.TestRoleId.ToString(),
            ["RowVersion"] = staleRowVersion,
            ["SelectedPermissionIds"] = WebAdminTestFactory.TestPermissionId.ToString()
        });
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();

        using var staleTokenResponse = await SendHtmxGetAsync(client, editorPath);
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleTokenHtml = await staleTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(client, "/Roles/Permissions", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(staleTokenHtml),
            ["RoleId"] = WebAdminTestFactory.TestRoleId.ToString(),
            ["RowVersion"] = staleRowVersion,
            ["SelectedPermissionIds"] = WebAdminTestFactory.TestPermissionId.ToString()
        });
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (
            staleResponseHtml.Contains("Failed to update role permissions.", StringComparison.Ordinal) ||
            staleResponseHtml.Contains("Die Rollenberechtigungen konnten nicht aktualisiert werden.", StringComparison.Ordinal) ||
            staleResponseHtml.Contains("RowVersion is required.", StringComparison.Ordinal) ||
            staleResponseHtml.Contains("RowVersion ist erforderlich.", StringComparison.Ordinal) ||
            staleResponseHtml.Contains("Gleichzeitigkeitskonflikt", StringComparison.OrdinalIgnoreCase) ||
            staleResponseHtml.Contains("Concurrency", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleCspValues).Should().BeTrue();
        staleCspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedUserRolesEdit_WithMissingRowVersion_ShouldShowFailureMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var editorPath = $"/Users/Roles?id={WebAdminTestFactory.TestLifecycleUserId}";

        using var tokenResponse = await SendHtmxGetAsync(client, editorPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var response = await SendHtmxPostAsync(client, "/Users/Roles", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
            ["UserId"] = WebAdminTestFactory.TestLifecycleUserId.ToString(),
            ["RowVersion"] = string.Empty,
            ["UserEmail"] = ExtractHiddenInputValue(tokenHtml, "UserEmail"),
            ["UserDisplay"] = ExtractHiddenInputValue(tokenHtml, "UserEmail"),
            ["SelectedRoleIds"] = WebAdminTestFactory.TestRoleId.ToString(),
            ["ReturnToIndex"] = "false",
            ["Query"] = string.Empty,
            ["Filter"] = "All",
            ["Page"] = "1",
            ["PageSize"] = "20"
        });
        var responseHtml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (
            responseHtml.Contains("RowVersion is required.", StringComparison.Ordinal) ||
            responseHtml.Contains("Failed to update user roles.", StringComparison.Ordinal))
            .Should().BeTrue();
        response.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");
    }

    [Fact]
    public async Task AuthenticatedUserRolesEdit_WithStaleRowVersion_ShouldSurfaceFailureMessage()
    {
        using var client = _factory.CreateAuthenticatedDatabaseNoRedirectClient();
        var editorPath = $"/Users/Roles?id={WebAdminTestFactory.TestLifecycleUserId}";

        using var baselineTokenResponse = await SendHtmxGetAsync(client, editorPath);
        baselineTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var baselineTokenHtml = await baselineTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var staleRowVersion = ExtractHiddenInputValue(baselineTokenHtml, "RowVersion");

        using var firstUpdateResponse = await SendHtmxPostAsync(
            client,
            "/Users/Roles",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["UserId"] = WebAdminTestFactory.TestLifecycleUserId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["UserEmail"] = ExtractHiddenInputValue(baselineTokenHtml, "UserEmail"),
                    ["UserDisplay"] = ExtractHiddenInputValue(baselineTokenHtml, "UserEmail"),
                    ["SelectedRoleIds"] = WebAdminTestFactory.TestRoleId.ToString(),
                    ["ReturnToIndex"] = "false",
                    ["Query"] = string.Empty,
                    ["Filter"] = "All",
                    ["Page"] = "1",
                    ["PageSize"] = "20"
                },
                baselineTokenHtml));
        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstUpdateResponse.Headers.TryGetValues("HX-Redirect", out var _).Should().BeTrue();

        using var staleTokenResponse = await SendHtmxGetAsync(client, editorPath);
        staleTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var staleTokenHtml = await staleTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var staleResponse = await SendHtmxPostAsync(
            client,
            "/Users/Roles",
            AddRequestVerificationToken(
                new Dictionary<string, string>
                {
                    ["UserId"] = WebAdminTestFactory.TestLifecycleUserId.ToString(),
                    ["RowVersion"] = staleRowVersion,
                    ["UserEmail"] = ExtractHiddenInputValue(staleTokenHtml, "UserEmail"),
                    ["UserDisplay"] = ExtractHiddenInputValue(staleTokenHtml, "UserEmail"),
                    ["SelectedRoleIds"] = WebAdminTestFactory.TestRoleId.ToString(),
                    ["ReturnToIndex"] = "false",
                    ["Query"] = string.Empty,
                    ["Filter"] = "All",
                    ["Page"] = "1",
                    ["PageSize"] = "20"
                },
                staleTokenHtml));
        var staleResponseHtml = await staleResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        staleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (
            staleResponseHtml.Contains("Failed to update user roles.", StringComparison.Ordinal) ||
            staleResponseHtml.Contains("RowVersion is required.", StringComparison.Ordinal) ||
            staleResponseHtml.Contains("Concurrency", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
        staleResponse.Headers.TryGetValues("Content-Security-Policy", out var staleCspValues).Should().BeTrue();
        staleCspValues!.Single().Should().Contain("form-action 'self'");
    }

    private static async Task<Guid> CreateShippingMethodAndGetIdAsync(
        HttpClient client,
        string name,
        string carrier,
        string service)
    {
        const string createPath = "/ShippingMethods/Create";

        using var tokenResponse = await SendHtmxGetAsync(client, createPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var createResponse = await SendHtmxPostAsync(
            client,
            createPath,
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["Name"] = name,
                ["Carrier"] = carrier,
                ["Service"] = service,
                ["CountriesCsv"] = "DE",
                ["IsActive"] = "true",
                ["Currency"] = "EUR",
                ["Rates[0].MaxShipmentMass"] = "1500",
                ["Rates[0].MaxSubtotalNetMinor"] = "3000",
                ["Rates[0].PriceMinor"] = "1299",
                ["Rates[0].SortOrder"] = "0"
            });

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        createResponse.Headers.TryGetValues("HX-Redirect", out var createRedirectValues)
            .Should().BeTrue("successful shipping create should redirect");
        createRedirectValues!.Single().Should().Contain("/ShippingMethods");
        createResponse.Headers.TryGetValues("Content-Security-Policy", out var createCspValues).Should().BeTrue();
        createCspValues!.Single().Should().Contain("form-action 'self'");

        using var indexResponse = await SendHtmxGetAsync(
            client,
            $"/ShippingMethods?query={Uri.EscapeDataString(name)}");
        indexResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var indexHtml = await indexResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        return ExtractShippingMethodIdFromIndexHtml(indexHtml, name);
    }

    private static async Task<Guid> CreateLoyaltyAccountAndGetIdAsync(
        HttpClient client,
        Guid businessId,
        Guid userId)
    {
        var createPath = $"/Loyalty/CreateAccount?businessId={businessId}";
        using var tokenResponse = await SendHtmxGetAsync(client, createPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var createResponse = await SendHtmxPostAsync(
            client,
            "/Loyalty/CreateAccount",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["BusinessId"] = businessId.ToString(),
                ["UserId"] = userId.ToString()
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        createResponse.Headers.TryGetValues("HX-Redirect", out var createRedirectValues).Should().BeTrue();
        var createRedirect = createRedirectValues!.Single();

        return ExtractQueryGuid(createRedirect, "id");
    }

    private static async Task<Guid> CreateLoyaltyCampaignAndGetIdAsync(
        HttpClient client,
        Guid businessId,
        string campaignName,
        string campaignTitle,
        string landingUrl)
    {
        var createPath = $"/Loyalty/CreateCampaign?businessId={businessId}";
        using var tokenResponse = await SendHtmxGetAsync(client, createPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var createResponse = await SendHtmxPostAsync(
            client,
            "/Loyalty/CreateCampaign",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["BusinessId"] = businessId.ToString(),
                ["Name"] = campaignName,
                ["Title"] = campaignTitle,
                ["Subtitle"] = "Smoke campaign subtitle",
                ["Body"] = $"Smoke campaign body for {campaignName}.",
                ["MediaUrl"] = string.Empty,
                ["LandingUrl"] = landingUrl,
                ["Channels"] = "3",
                ["StartsAtUtc"] = string.Empty,
                ["EndsAtUtc"] = string.Empty,
                ["TargetingJson"] = "{}",
                ["PayloadJson"] = "{}"
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        createResponse.Headers.TryGetValues("HX-Redirect", out var createRedirectValues).Should().BeTrue();
        var createRedirect = createRedirectValues!.Single();

        return ExtractQueryGuid(createRedirect, "id");
    }

    private static async Task<Guid> CreateLoyaltyProgramAndGetIdAsync(
        HttpClient client,
        Guid businessId,
        string programName)
    {
        var createPath = $"/Loyalty/CreateProgram?businessId={businessId}";
        using var tokenResponse = await SendHtmxGetAsync(client, createPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var createResponse = await SendHtmxPostAsync(
            client,
            "/Loyalty/CreateProgram",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["BusinessId"] = businessId.ToString(),
                ["Name"] = programName,
                ["AccrualMode"] = "PerVisit",
                ["PointsPerCurrencyUnit"] = "1",
                ["RulesJson"] = "{}",
                ["IsActive"] = "true"
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        createResponse.Headers.TryGetValues("HX-Redirect", out var createRedirectValues).Should().BeTrue();
        var createRedirect = createRedirectValues!.Single();
        return ExtractQueryGuid(createRedirect, "id");
    }

    private static async Task<Guid> CreateLoyaltyRewardTierAndGetIdAsync(
        HttpClient client,
        Guid loyaltyProgramId,
        Guid businessId,
        string programName,
        string description)
    {
        var createPath = $"/Loyalty/CreateRewardTier?loyaltyProgramId={loyaltyProgramId}";
        using var tokenResponse = await SendHtmxGetAsync(client, createPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var createResponse = await SendHtmxPostAsync(
            client,
            "/Loyalty/CreateRewardTier",
            new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
                ["LoyaltyProgramId"] = loyaltyProgramId.ToString(),
                ["BusinessId"] = businessId.ToString(),
                ["ProgramName"] = programName,
                ["PointsRequired"] = "100",
                ["RewardType"] = "FreeItem",
                ["RewardValue"] = string.Empty,
                ["Description"] = description,
                ["MetadataJson"] = "{}",
                ["AllowSelfRedemption"] = "true"
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        createResponse.Headers.TryGetValues("HX-Redirect", out var createRedirectValues).Should().BeTrue();
        var createRedirect = createRedirectValues!.Single();
        return ExtractQueryGuid(createRedirect, "id");
    }

    private static void SeedCampaignAndDeliveryForStatusActionTests(
        Darwin.Infrastructure.Persistence.Db.DarwinDbContext db,
        Guid businessId,
        Guid campaignId,
        Guid deliveryId)
    {
        db.Set<Campaign>()
            .Add(new Campaign
            {
                Id = campaignId,
                BusinessId = businessId,
                Name = $"Smoke Delivery Campaign {campaignId:N}",
                Title = "Smoke Delivery Campaign Title",
                Subtitle = "Seeded for campaign delivery status action test",
                Body = "Seeded campaign delivery body for status action testing.",
                Channels = CampaignChannels.InApp,
                IsActive = true,
                TargetingJson = "{}",
                PayloadJson = "{}",
                RowVersion = [1]
            });

        db.Set<CampaignDelivery>().Add(new CampaignDelivery
        {
            Id = deliveryId,
            CampaignId = campaignId,
            RecipientUserId = WebAdminTestFactory.TestMemberUserId,
            BusinessId = businessId,
            Channel = CampaignDeliveryChannel.InApp,
            Status = CampaignDeliveryStatus.Pending,
            Destination = "smoke-recipient@example.test",
            AttemptCount = 0,
            RowVersion = [1]
        });
    }

    private static Dictionary<string, string> BuildShippingMethodEditPayload(
        string id,
        string rowVersion,
        string name,
        string carrier,
        string service)
    {
        return new Dictionary<string, string>
        {
            ["Id"] = id,
            ["RowVersion"] = rowVersion,
            ["Name"] = name,
            ["Carrier"] = carrier,
            ["Service"] = service,
            ["CountriesCsv"] = "DE",
            ["IsActive"] = "true",
            ["Currency"] = "EUR",
            ["Rates[0].MaxShipmentMass"] = "1600",
            ["Rates[0].MaxSubtotalNetMinor"] = "3500",
            ["Rates[0].PriceMinor"] = "1399",
            ["Rates[0].SortOrder"] = "0"
        };
    }

    private static Dictionary<string, string> BuildBusinessEditPayload(
        string id,
        string rowVersion,
        string name,
        string legalName,
        string taxId,
        string shortDescription,
        string websiteUrl,
        string contactEmail,
        string contactPhone,
        string supportEmail,
        string communicationReplyToEmail,
        string communicationSenderName,
        bool customerEmailNotificationsEnabled,
        bool customerMarketingEmailsEnabled,
        bool operationalAlertEmailsEnabled,
        bool isActive = true)
    {
        return new Dictionary<string, string>
        {
            ["Id"] = id,
            ["RowVersion"] = rowVersion,
            ["Name"] = name,
            ["LegalName"] = legalName,
            ["TaxId"] = taxId,
            ["ShortDescription"] = shortDescription,
            ["WebsiteUrl"] = websiteUrl,
            ["ContactEmail"] = contactEmail,
            ["ContactPhoneE164"] = contactPhone,
            ["Category"] = BusinessCategoryKind.Cafe.ToString(),
            ["DefaultCurrency"] = "EUR",
            ["DefaultCulture"] = "de-DE",
            ["DefaultTimeZoneId"] = "Europe/Berlin",
            ["AdminTextOverridesJson"] = "{}",
            ["BrandDisplayName"] = string.Empty,
            ["BrandLogoUrl"] = string.Empty,
            ["BrandPrimaryColorHex"] = string.Empty,
            ["BrandSecondaryColorHex"] = string.Empty,
            ["SupportEmail"] = supportEmail,
            ["CommunicationSenderName"] = communicationSenderName,
            ["CommunicationReplyToEmail"] = communicationReplyToEmail,
            ["CustomerEmailNotificationsEnabled"] = customerEmailNotificationsEnabled.ToString().ToLowerInvariant(),
            ["CustomerMarketingEmailsEnabled"] = customerMarketingEmailsEnabled.ToString().ToLowerInvariant(),
            ["OperationalAlertEmailsEnabled"] = operationalAlertEmailsEnabled.ToString().ToLowerInvariant(),
            ["IsActive"] = isActive.ToString().ToLowerInvariant()
        };
    }

    private static Dictionary<string, string> BuildBusinessLocationEditPayload(
        string id,
        string businessId,
        string rowVersion,
        string name,
        string city,
        bool isPrimary,
        string internalNote,
        string openingHoursJson)
    {
        return new Dictionary<string, string>
        {
            ["Id"] = id,
            ["BusinessId"] = businessId,
            ["Page"] = "1",
            ["PageSize"] = "20",
            ["Query"] = string.Empty,
            ["Filter"] = "All",
            ["RowVersion"] = rowVersion,
            ["Name"] = name,
            ["AddressLine1"] = $"{name} Street 1",
            ["AddressLine2"] = string.Empty,
            ["City"] = city,
            ["Region"] = city,
            ["CountryCode"] = "DE",
            ["PostalCode"] = "10115",
            ["Latitude"] = "52.5200",
            ["Longitude"] = "13.4050",
            ["AltitudeMeters"] = "12",
            ["IsPrimary"] = isPrimary.ToString().ToLowerInvariant(),
            ["OpeningHoursJson"] = openingHoursJson,
            ["InternalNote"] = internalNote
        };
    }

    private static Dictionary<string, string> BuildBusinessMemberEditPayload(
        string id,
        string businessId,
        string userId,
        string rowVersion,
        string role,
        bool isActive,
        bool allowLastOwnerOverride,
        string overrideReason,
        string page,
        string pageSize,
        string query,
        string filter)
    {
        return new Dictionary<string, string>
        {
            ["Id"] = id,
            ["BusinessId"] = businessId,
            ["UserId"] = userId,
            ["RowVersion"] = rowVersion,
            ["Role"] = role,
            ["IsActive"] = isActive.ToString().ToLowerInvariant(),
            ["AllowLastOwnerOverride"] = allowLastOwnerOverride.ToString().ToLowerInvariant(),
            ["OverrideReason"] = overrideReason,
            ["Page"] = page,
            ["PageSize"] = pageSize,
            ["Query"] = query,
            ["Filter"] = filter
        };
    }

    private static Dictionary<string, string> BuildWarehouseEditPayload(
        string id,
        string rowVersion,
        string name,
        string location,
        string description,
        bool isDefault)
    {
        return new Dictionary<string, string>
        {
            ["Id"] = id,
            ["RowVersion"] = rowVersion,
            ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
            ["Name"] = name,
            ["Location"] = location,
            ["Description"] = description,
            ["IsDefault"] = isDefault.ToString().ToLowerInvariant()
        };
    }

    private static Dictionary<string, string> BuildSupplierEditPayload(
        string id,
        string rowVersion,
        string name,
        string email,
        string phone,
        string address,
        string notes)
    {
        return new Dictionary<string, string>
        {
            ["Id"] = id,
            ["RowVersion"] = rowVersion,
            ["BusinessId"] = "44444444-4444-4444-4444-444444444444",
            ["Name"] = name,
            ["Email"] = email,
            ["Phone"] = phone,
            ["Address"] = address,
            ["Notes"] = notes
        };
    }

    private static Dictionary<string, string> BuildStockLevelEditPayload(
        string id,
        string rowVersion,
        string warehouseId,
        string productVariantId,
        string availableQuantity,
        string reservedQuantity,
        string reorderPoint,
        string reorderQuantity,
        string inTransitQuantity)
    {
        return new Dictionary<string, string>
        {
            ["Id"] = id,
            ["RowVersion"] = rowVersion,
            ["WarehouseId"] = warehouseId,
            ["ProductVariantId"] = productVariantId,
            ["AvailableQuantity"] = availableQuantity,
            ["ReservedQuantity"] = reservedQuantity,
            ["ReorderPoint"] = reorderPoint,
            ["ReorderQuantity"] = reorderQuantity,
            ["InTransitQuantity"] = inTransitQuantity
        };
    }

    private static Dictionary<string, string> BuildMediaAssetEditPayload(
        string id,
        string rowVersion,
        string title,
        string alt,
        string url,
        string originalFileName,
        string sizeBytes,
        string width,
        string height,
        string role)
    {
        return new Dictionary<string, string>
        {
            ["Id"] = id,
            ["RowVersion"] = rowVersion,
            ["Url"] = url,
            ["OriginalFileName"] = originalFileName,
            ["SizeBytes"] = sizeBytes,
            ["Width"] = width,
            ["Height"] = height,
            ["Alt"] = alt,
            ["Title"] = title,
            ["Role"] = role
        };
    }

    private static Guid ExtractShippingMethodIdFromIndexHtml(string html, string shippingMethodName)
    {
        var pattern = $@"{Regex.Escape(shippingMethodName)}.*?href=""\/ShippingMethods\/Edit\?id=(?<id>[0-9a-fA-F-]{{36}})""";
        var match = Regex.Match(html, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
        match.Success.Should().BeTrue("shipping method '{0}' should be present in the index view", shippingMethodName);
        return Guid.Parse(match.Groups["id"].Value);
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
            ["TransactionalEmailProvider"] = "SMTP",
            ["SupportEmail"] = "support@example.test",
            ["BillingEmail"] = "billing@example.test",
            ["NoReplyEmail"] = "no-reply@example.test",
            ["SystemAdminEmail"] = "admin@example.test",
            ["BrevoBaseUrl"] = "https://api.brevo.com/v3/",
            ["BrevoApiKey"] = string.Empty,
            ["BrevoWebhookUsername"] = string.Empty,
            ["BrevoWebhookPassword"] = string.Empty,
            ["BrevoSandboxMode"] = "true",
            ["BrevoTestRecipientEmail"] = "communication-smoke@example.test",
            ["SmsEnabled"] = "true",
            ["SmsProvider"] = "Twilio",
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
            ["Url"] = ExtractHiddenInputValue(tokenHtml, "Url"),
            ["OriginalFileName"] = ExtractHiddenInputValue(tokenHtml, "OriginalFileName"),
            ["SizeBytes"] = ExtractHiddenInputValue(tokenHtml, "SizeBytes"),
            ["Width"] = ExtractHiddenInputValue(tokenHtml, "Width"),
            ["Height"] = ExtractHiddenInputValue(tokenHtml, "Height"),
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

        using var deleteTokenResponse = await SendHtmxGetAsync(client, editPath);
        deleteTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deleteTokenHtml = await deleteTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var deleteResponse = await SendHtmxPostAsync(client, "/Media/Delete", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(deleteTokenHtml),
            ["id"] = WebAdminTestFactory.TestMediaAssetId.ToString(),
            ["rowVersion"] = ExtractHiddenInputValue(deleteTokenHtml, "RowVersion")
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

        using var deleteTokenResponse = await SendHtmxGetAsync(
            client,
            $"/Businesses/EditLocation?id={WebAdminTestFactory.TestBusinessLocationLifecycleId}");
        deleteTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deleteTokenHtml = await deleteTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var deleteResponse = await SendHtmxPostAsync(client, "/Businesses/DeleteLocation", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(deleteTokenHtml),
            ["id"] = WebAdminTestFactory.TestBusinessLocationLifecycleId.ToString(),
            ["userId"] = "44444444-4444-4444-4444-444444444444",
            ["rowVersion"] = ExtractHiddenInputValue(deleteTokenHtml, "RowVersion")
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

        using var deleteTokenResponse = await SendHtmxGetAsync(client, editPath);
        deleteTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deleteTokenHtml = await deleteTokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var deleteResponse = await SendHtmxPostAsync(client, "/Brands/Delete", new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(deleteTokenHtml),
            ["id"] = WebAdminTestFactory.TestBrandLifecycleId.ToString(),
            ["rowVersion"] = ExtractHiddenInputValue(deleteTokenHtml, "RowVersion")
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

    private static async Task<Guid> CreateHostedBusinessAsync(
        HttpClient client,
        string businessName,
        string contactEmail,
        Guid? ownerUserId,
        string legalName,
        bool isActive)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var redirectPath = await PostValidEditorMutationAndAssertListedAsync(
            client,
            "/Businesses/Create",
            "/Businesses/Create",
            $"/Businesses?query={Uri.EscapeDataString(businessName)}",
            businessName,
            new Dictionary<string, string>
            {
                ["Name"] = businessName,
                ["LegalName"] = legalName,
                ["Category"] = "Cafe",
                ["DefaultCurrency"] = "EUR",
                ["DefaultCulture"] = "de-DE",
                ["DefaultTimeZoneId"] = "Europe/Berlin",
                ["TaxId"] = string.IsNullOrWhiteSpace(legalName) ? string.Empty : $"DE{suffix}",
                ["WebsiteUrl"] = $"https://{businessName.Replace(" ", "-", StringComparison.Ordinal).ToLowerInvariant()}.example.test",
                ["ContactEmail"] = contactEmail,
                ["ContactPhoneE164"] = "+4915112345678",
                ["ShortDescription"] = "Hosted business smoke.",
                ["OwnerUserId"] = ownerUserId?.ToString() ?? string.Empty,
                ["OwnerInviteEmail"] = string.Empty,
                ["BrandDisplayName"] = businessName,
                ["SupportEmail"] = contactEmail,
                ["CommunicationSenderName"] = businessName,
                ["CommunicationReplyToEmail"] = contactEmail,
                ["CustomerEmailNotificationsEnabled"] = "true",
                ["CustomerMarketingEmailsEnabled"] = "false",
                ["OperationalAlertEmailsEnabled"] = "true",
                ["IsActive"] = isActive.ToString().ToLowerInvariant()
            });

        return ExtractQueryGuid(redirectPath, "id");
    }

    private static async Task CreateHostedBusinessLocationAsync(
        HttpClient client,
        Guid businessId,
        string locationName,
        bool isPrimary)
    {
        await PostValidEditorMutationAndAssertListedAsync(
            client,
            $"/Businesses/CreateLocation?businessId={businessId}",
            "/Businesses/CreateLocation",
            $"/Businesses/Locations?businessId={businessId}&query={Uri.EscapeDataString(locationName)}",
            locationName,
            new Dictionary<string, string>
            {
                ["BusinessId"] = businessId.ToString(),
                ["Page"] = "1",
                ["PageSize"] = "20",
                ["Query"] = string.Empty,
                ["Filter"] = "All",
                ["Name"] = locationName,
                ["AddressLine1"] = "Hosted Street 1",
                ["AddressLine2"] = string.Empty,
                ["City"] = "Berlin",
                ["Region"] = "Berlin",
                ["CountryCode"] = "DE",
                ["PostalCode"] = "10115",
                ["OpeningHoursJson"] = "{\"mon\":\"09:00-17:00\"}",
                ["InternalNote"] = "Hosted business lifecycle smoke location.",
                ["IsPrimary"] = isPrimary.ToString().ToLowerInvariant()
            });
    }

    private static async Task<string> PostHostedBusinessLifecycleActionAsync(
        HttpClient client,
        Guid businessId,
        string action,
        Dictionary<string, string>? additionalFormValues = null)
    {
        var setupPath = $"/Businesses/Setup?id={businessId}";
        using var tokenResponse = await SendHtmxGetAsync(client, setupPath);
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenHtml = await tokenResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
            ["id"] = businessId.ToString(),
            ["rowVersion"] = ExtractHiddenInputValue(tokenHtml, "rowVersion"),
            ["returnToSetup"] = "true"
        };

        if (additionalFormValues is not null)
        {
            foreach (var pair in additionalFormValues)
            {
                form[pair.Key] = pair.Value;
            }
        }

        using var postResponse = await SendHtmxPostAsync(client, $"/Businesses/{action}", form);
        var postHtml = await postResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var postPreview = postHtml.Length > 600 ? postHtml[..600] : postHtml;

        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        postResponse.Headers.TryGetValues("HX-Redirect", out var redirectValues)
            .Should().BeTrue("business lifecycle action should return an HTMX redirect; response preview: {0}", postPreview);
        redirectValues!.Single().Should().Contain("/Businesses/Setup");
        postResponse.Headers.TryGetValues("Content-Security-Policy", out var cspValues).Should().BeTrue();
        cspValues!.Single().Should().Contain("form-action 'self'");

        using var updatedResponse = await SendHtmxGetAsync(client, setupPath);
        updatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return await updatedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private static Dictionary<string, string> BuildHostedBusinessLifecycleForm(string tokenHtml, Guid businessId)
    {
        return new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiForgeryToken(tokenHtml),
            ["id"] = businessId.ToString(),
            ["rowVersion"] = ExtractHiddenInputValue(tokenHtml, "rowVersion"),
            ["returnToSetup"] = "true"
        };
    }

    private static string ExtractTableRowContainingText(string html, string text)
    {
        var rowMatch = Regex.Matches(html, "<tr[^>]*>.*?</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .FirstOrDefault(match => match.Value.Contains(text, StringComparison.OrdinalIgnoreCase));
        rowMatch.Should().NotBeNull();

        return rowMatch!.Value;
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
        string newStatus,
        Guid? warehouseId = null)
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
            ["WarehouseId"] = warehouseId?.ToString() ?? string.Empty
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

    private static Guid ExtractHrefQueryGuid(string html, string routePath, string parameterName)
    {
        var hrefPattern = @"href\s*=\s*(?<quote>[""'])(?<url>[^""']+)\k<quote>";
        foreach (Match match in Regex.Matches(html, hrefPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            var href = match.Groups["url"].Value;
            if (!href.Contains(routePath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return ExtractQueryGuid(href, parameterName);
        }

        throw new InvalidOperationException(
            $"Could not find href for '{routePath}' containing query parameter '{parameterName}' in provided HTML.");
    }

}
