using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace Darwin.Tests.Unit.Security;

public sealed class SecurityAndPerformanceContractsAndPackagingSourceTests : SecurityAndPerformanceSourceTestBase
{
    [Fact]
    public void WebApiRuntimeAndComposition_Should_KeepMinimalHostAuthAndProviderCallbackBoundariesWired()
    {
        var programSource = ReadWebApiFile("Program.cs");
        var compositionSource = ReadWebApiFile(Path.Combine("Extensions", "DependencyInjection.cs"));
        var startupSource = ReadWebApiFile(Path.Combine("Extensions", "Startup.cs"));
        var stripeWebhookSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "StripeWebhooksController.cs"));
        var dhlWebhookSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "DhlWebhooksController.cs"));
        var brevoWebhookSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "BrevoWebhooksController.cs"));

        programSource.Should().Contain("builder.Services.AddWebApiComposition(builder.Configuration);");
        programSource.Should().Contain("await app.UseWebApiStartupAsync();");
        programSource.Should().Contain("public partial class Program");

        compositionSource.Should().Contain("AddAuthentication");
        compositionSource.Should().Contain("AddAuthorization");
        compositionSource.Should().Contain("AddControllers");
        compositionSource.Should().Contain("AddConfiguredPersistence");
        compositionSource.Should().Contain("AddSharedHostingDataProtection");
        compositionSource.Should().Contain("AddNotificationsInfrastructure");
        compositionSource.Should().Contain("AddComplianceInfrastructure");

        startupSource.Should().Contain("UseAuthentication");
        startupSource.Should().Contain("UseAuthorization");
        startupSource.Should().Contain("MapControllers");

        stripeWebhookSource.Should().Contain("stripe/webhooks");
        stripeWebhookSource.Should().Contain("Stripe-Signature");
        dhlWebhookSource.Should().Contain("dhl/webhooks");
        brevoWebhookSource.Should().Contain("brevo/webhooks");
        brevoWebhookSource.Should().Contain("Basic");
    }

    [Fact]
    public void WebAdminRuntimeAndForms_Should_KeepSecureCookiesLocalizationAndBase64RowVersionPosts()
    {
        var dependencyInjectionSource = ReadWebAdminFile(Path.Combine("Extensions", "DependencyInjection.cs"));
        var adminBaseSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));
        var formSources = ReadWebAdminViewSources()
            .Where(static view => view.Path.EndsWith("Form.cshtml", StringComparison.OrdinalIgnoreCase)
                || view.Path.EndsWith("EditorShell.cshtml", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        dependencyInjectionSource.Should().Contain("options.Cookie.SecurePolicy = CookieSecurePolicy.Always;");
        dependencyInjectionSource.Should().Contain("options.Cookie.HttpOnly = true;");
        dependencyInjectionSource.Should().Contain("options.Cookie.SameSite = SameSiteMode.Lax;");
        dependencyInjectionSource.Should().Contain("services.AddLocalization(options => options.ResourcesPath = \"Resources\")");
        dependencyInjectionSource.Should().Contain("AddViewLocalization");
        dependencyInjectionSource.Should().Contain("AddDataAnnotationsLocalization");
        dependencyInjectionSource.Should().Contain("services.AddAntiforgery");
        dependencyInjectionSource.Should().Contain("options.HeaderName = \"RequestVerificationToken\"");

        adminBaseSource.Should().Contain("DecodeBase64RowVersion");
        adminBaseSource.Should().Contain("Convert.FromBase64String");
        adminBaseSource.Should().Contain("Array.Empty<byte>()");

        formSources.Should().NotBeEmpty();
        formSources
            .Where(static view => view.Source.Contains("name=\"RowVersion\"", StringComparison.Ordinal))
            .Should()
            .OnlyContain(static view => view.Source.Contains("Convert.ToBase64String", StringComparison.Ordinal),
                "row-version form fields should render transport-safe Base64 values");
    }

    [Fact]
    public void CatalogCmsAndShippingEditors_Should_KeepLocalizedGroupedFormsAndClientAssetsWired()
    {
        var productFormSource = ReadWebAdminFile(Path.Combine("Views", "Products", "_ProductForm.cshtml"));
        var pageFormSource = ReadWebAdminFile(Path.Combine("Views", "Pages", "_PageForm.cshtml"));
        var pageScriptSource = ReadWebAdminFile(Path.Combine("wwwroot", "js", "content-editors.js"));
        var brandFormSource = ReadWebAdminFile(Path.Combine("Views", "Brands", "_BrandForm.cshtml"));
        var categoryFormSource = ReadWebAdminFile(Path.Combine("Views", "Categories", "_CategoryEditEditorShell.cshtml"));
        var addOnFormSource = ReadWebAdminFile(Path.Combine("Views", "AddOnGroups", "_AddOnGroupForm.cshtml"));
        var shippingFormSource = ReadWebAdminFile(Path.Combine("Views", "ShippingMethods", "_ShippingMethodForm.cshtml"));

        productFormSource.Should().Contain("admin-form-section");
        productFormSource.Should().Contain("field-help");
        productFormSource.Should().Contain("Translations[@");
        productFormSource.Should().Contain("Variants[@");

        pageFormSource.Should().Contain("Model.Cultures");
        pageFormSource.Should().Contain("pattern=\"[a-z0-9]+(?:-[a-z0-9]+)*\"");
        pageFormSource.Should().Contain("data-page-quill-editor=\"true\"");
        pageFormSource.Should().Contain("ContentHtml");
        pageScriptSource.Should().Contain("initPageEditors");
        pageScriptSource.Should().Contain("pageImageUploadUrl");
        pageScriptSource.Should().Contain("RequestVerificationToken");
        pageScriptSource.Should().Contain("quill.insertEmbed");

        brandFormSource.Should().Contain("Translations[");
        categoryFormSource.Should().Contain("Translations[");
        addOnFormSource.Should().Contain("Currency");
        shippingFormSource.Should().Contain("RateTiers");
        shippingFormSource.Should().Contain("Carrier");
        shippingFormSource.Should().Contain("Service");
    }

    [Fact]
    public void InventoryAndProcurementEditors_Should_KeepLineTemplatesLifecycleFieldsAndRowVersionsWired()
    {
        var stockLevelFormSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "_StockLevelForm.cshtml"));
        var stockTransferFormSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "_StockTransferForm.cshtml"));
        var purchaseOrderFormSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "_PurchaseOrderForm.cshtml"));
        var inventoryControllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Inventory", "InventoryController.cs"));

        stockLevelFormSource.Should().Contain("WarehouseId");
        stockLevelFormSource.Should().Contain("ProductVariantId");
        stockLevelFormSource.Should().Contain("AvailableQuantity");
        stockLevelFormSource.Should().Contain("ReservedQuantity");

        stockTransferFormSource.Should().Contain("FromWarehouseId");
        stockTransferFormSource.Should().Contain("ToWarehouseId");
        stockTransferFormSource.Should().Contain("Lines");
        stockTransferFormSource.Should().Contain("data-dynamic-lines-template");
        stockTransferFormSource.Should().Contain("transferLineTemplate");

        purchaseOrderFormSource.Should().Contain("SupplierId");
        purchaseOrderFormSource.Should().Contain("BusinessOptions");
        purchaseOrderFormSource.Should().Contain("SupplierOptions");
        purchaseOrderFormSource.Should().Contain("Lines");
        purchaseOrderFormSource.Should().Contain("data-dynamic-lines-template");
        purchaseOrderFormSource.Should().Contain("purchaseOrderLineTemplate");

        inventoryControllerSource.Should().Contain("UpdateStockTransferLifecycle");
        inventoryControllerSource.Should().Contain("UpdatePurchaseOrderLifecycle");
        inventoryControllerSource.Should().Contain("DecodeBase64RowVersion");
    }

    [Fact]
    public void PublicCommerceControllers_Should_KeepWebhookOnlyPaymentFinalizationAndSafePublicRoutes()
    {
        var cartControllerSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicCartController.cs"));
        var checkoutControllerSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicCheckoutController.cs"));
        var catalogControllerSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicCatalogController.cs"));
        var cmsControllerSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicCmsController.cs"));
        var stripeHandlerSource = ReadApplicationFile(Path.Combine("Billing", "ProcessStripeWebhookHandler.cs"));

        cartControllerSource.Should().Contain("[AllowAnonymous]");
        cartControllerSource.Should().Contain("public/cart");
        checkoutControllerSource.Should().Contain("public/checkout");
        checkoutControllerSource.Should().Contain("CreateCheckout");
        checkoutControllerSource.Should().NotContain("MarkPaymentCompleted");
        checkoutControllerSource.Should().NotContain("PaymentStatus.Completed");

        catalogControllerSource.Should().Contain("public/catalog");
        cmsControllerSource.Should().Contain("public/cms");
        stripeHandlerSource.Should().Contain("checkout.session.completed");
        stripeHandlerSource.Should().Contain("payment_intent.succeeded");
        stripeHandlerSource.Should().Contain("payment_intent.payment_failed");
    }

    [Fact]
    public void SecurityInfrastructure_Should_KeepJwtTotpRateLimitAndIdempotencySafetyContracts()
    {
        var jwtSource = ReadInfrastructureFile(Path.Combine("Security", "Jwt", "JwtTokenService.cs"));
        var totpSource = ReadInfrastructureFile(Path.Combine("Security", "TotpService.cs"));
        var rateLimiterSource = ReadInfrastructureFile(Path.Combine("Security", "LoginRateLimiter", "MemoryLoginRateLimiter.cs"));
        var idempotencySource = ReadWebApiFile(Path.Combine("Middleware", "IdempotencyMiddleware.cs"));

        jwtSource.Should().Contain("IssueTokens");
        jwtSource.Should().Contain("CreateOpaqueToken");
        jwtSource.Should().Contain("RandomNumberGenerator");
        jwtSource.Should().Contain("SHA256.HashData");

        totpSource.Should().Contain("RFC 6238");
        totpSource.Should().Contain("Math.Clamp(window, 0, MaxWindow)");
        totpSource.Should().Contain("normalizedCode.Length != Digits");
        totpSource.Should().Contain("!int.TryParse(normalizedCode, out var codeInt)");

        rateLimiterSource.Should().Contain("ConcurrentDictionary");
        rateLimiterSource.Should().Contain("IClock");
        rateLimiterSource.Should().Contain("IsAllowedAsync");
        rateLimiterSource.Should().Contain("RecordAsync");

        idempotencySource.Should().Contain("Idempotency-Key");
        idempotencySource.Should().Contain("HttpStatusCode.Conflict");
        idempotencySource.Should().Contain("_cache.Remove");
    }

    [Fact]
    public void SettingsLocalizationAndSeoHelpers_Should_KeepResourceBackedSafeBoundaries()
    {
        var siteSettingCacheSource = ReadWebAdminFile(Path.Combine("Services", "Settings", "SiteSettingCache.cs"));
        var adminTextOverrideSource = ReadWebAdminFile(Path.Combine("Localization", "AdminTextOverrideCatalog.cs"));
        var canonicalUrlSource = ReadWebAdminFile(Path.Combine("Services", "Seo", "CanonicalUrlService.cs"));
        var sharedResourceSource = ReadWebAdminFile(Path.Combine("Resources", "SharedResource.resx"));
        var germanResourceSource = ReadWebAdminFile(Path.Combine("Resources", "SharedResource.de-DE.resx"));

        siteSettingCacheSource.Should().Contain("IMemoryCache");
        siteSettingCacheSource.Should().Contain("DefaultCulture");
        siteSettingCacheSource.Should().Contain("SupportedCultures");
        adminTextOverrideSource.Should().Contain("AdminTextOverrideJsonCatalog.Parse");
        adminTextOverrideSource.Should().Contain("StringComparer.OrdinalIgnoreCase");
        canonicalUrlSource.Should().Contain("IHttpContextAccessor");
        canonicalUrlSource.Should().Contain("UriHelper.BuildAbsolute");
        canonicalUrlSource.Should().Contain("NormalizePathSegment");

        ExtractResxKeys(sharedResourceSource)
            .Except(ExtractResxKeys(germanResourceSource), StringComparer.Ordinal)
            .Should()
            .BeEmpty("WebAdmin German resources should stay in parity with neutral resources");
    }

    [Fact]
    public void DomainAndApplicationProjectFiles_Should_KeepProviderAndValidationDependenciesWired()
    {
        var applicationProjectSource = ReadApplicationFile("Darwin.Application.csproj");
        var infrastructureProjectSource = ReadInfrastructureFile("Darwin.Infrastructure.csproj");
        var webApiProjectSource = ReadWebApiFile("Darwin.WebApi.csproj");
        var webAdminProjectSource = ReadWebAdminFile("Darwin.WebAdmin.csproj");

        applicationProjectSource.Should().Contain("FluentValidation");
        infrastructureProjectSource.Should().Contain("Microsoft.EntityFrameworkCore");
        ReadRepositoryFile(Path.Combine("src", "Darwin.Infrastructure.PostgreSql", "Darwin.Infrastructure.PostgreSql.csproj"))
            .Should()
            .Contain("Npgsql.EntityFrameworkCore.PostgreSQL");
        webApiProjectSource.Should().Contain("Microsoft.AspNetCore.OpenApi");
        webAdminProjectSource.Should().Contain("Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation");
    }

    [Fact]
    public void MobileProfileAndScanSurfaces_Should_KeepResourceBackedNonAdminContracts()
    {
        var consumerProfileSource = ReadMobileConsumerFile(Path.Combine("Views", "ProfilePage.xaml"));
        var businessScanSource = ReadMobileBusinessFile(Path.Combine("Views", "QrScanPage.xaml"));
        var consumerResourcesSource = ReadMobileConsumerFile(Path.Combine("Resources", "Strings.resx"));
        var businessResourcesSource = ReadMobileBusinessFile(Path.Combine("Resources", "Strings.resx"));

        consumerProfileSource.Should().Contain("x:Static res:AppResources");
        consumerProfileSource.Should().Contain("Profile");
        consumerProfileSource.Should().NotContain("WebAdmin");

        businessScanSource.Should().Contain("x:Static res:AppResources");
        businessScanSource.Should().Contain("Scan");
        businessScanSource.Should().NotContain("ProviderCallback");

        consumerResourcesSource.Should().Contain("Profile");
        businessResourcesSource.Should().Contain("Scan");
    }

    [Fact]
    public void SourceContracts_Should_Not_AssertObsoleteDashboardControllerOrLegacyCmsAliases()
    {
        var dashboardControllerPath = ResolveRepositoryPath("src", "Darwin.WebAdmin", "Controllers", "Admin", "Home", "DashboardController.cs");
        File.Exists(dashboardControllerPath).Should().BeFalse("legacy dashboard controller compatibility redirects are retired");

        var webPageRouteSource = ReadWebFrontendFile(Path.Combine("src", "app", "page", "[slug]", "page.tsx"));
        webPageRouteSource.Should().Contain("getCmsDetailPageContext");
        webPageRouteSource.Should().Contain("buildCmsPagePath");
    }

    private static List<(string Path, string Source)> ReadWebAdminViewSources()
    {
        var root = ResolveRepositoryPath("src", "Darwin.WebAdmin", "Views");
        return Directory
            .GetFiles(root, "*.cshtml", SearchOption.AllDirectories)
            .Select(path => (
                Path: Path.GetRelativePath(root, path),
                Source: File.ReadAllText(path)))
            .ToList();
    }

    private static ISet<string> ExtractResxKeys(string source)
    {
        return Regex.Matches(source, "<data name=\"(?<name>[^\"]+)\"")
            .Select(static match => match.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        var path = ResolveRepositoryPath(relativePath);
        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }
}
