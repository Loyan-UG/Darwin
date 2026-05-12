using System.Diagnostics;
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
        compositionSource.Should().Contain("services.AddApplication(configuration);");
        compositionSource.Should().Contain("AddConfiguredPersistence");
        compositionSource.Should().Contain("AddSharedHostingDataProtection");
        compositionSource.Should().Contain("AddNotificationsInfrastructure");
        compositionSource.Should().Contain("AddShippingProviderInfrastructure");
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
    public void ProviderSmokeScripts_Should_StayGuardedAndAvoidSecretOutput()
    {
        var externalSmokeInputsSource = ReadRepositoryFile(Path.Combine("docs", "external-smoke-inputs.md"));
        var scripts = new[]
        {
            "smoke-stripe-testmode.ps1",
            "check-stripe-webhook-forwarding.ps1",
            "smoke-dhl-live.ps1",
            "smoke-vies-live.ps1",
            "smoke-brevo-readiness.ps1",
            "smoke-object-storage.ps1",
            "check-go-live-readiness.ps1"
        };

        foreach (var script in scripts)
        {
            var source = ReadRepositoryFile(Path.Combine("scripts", script));

            source.Should().NotContain("sk_live_");
            source.Should().NotContain("sk_test_");
            source.Should().NotContain("rk_live_");
            source.Should().NotContain("rk_test_");
            source.Should().NotContain("whsec_");
            source.Should().NotContain("Write-Host $response");
            source.Should().NotContain("Write-Host $json");
        }

        foreach (var script in scripts.Where(script => script.StartsWith("smoke-", StringComparison.Ordinal)))
        {
            var source = ReadRepositoryFile(Path.Combine("scripts", script));

            source.Should().Contain("[switch]$Execute");
            source.Should().Contain("if (-not $Execute)");
            source.Should().Contain("exit 2");
        }

        ReadRepositoryFile(Path.Combine("scripts", "check-go-live-readiness.ps1"))
            .Should()
            .Contain("ExpectedBlockedExitCode = 2")
            .And.Contain("Blocked go-live prerequisites")
            .And.Contain("scripts\\check-secrets.ps1")
            .And.Contain("scripts\\smoke-stripe-testmode.ps1")
            .And.Contain("scripts\\check-stripe-webhook-forwarding.ps1")
            .And.Contain("\"-CreateSmokeOrder\"")
            .And.Contain("\"-RequireRuntimePipeline\"")
            .And.Contain("scripts\\smoke-dhl-live.ps1")
            .And.Contain("scripts\\smoke-brevo-readiness.ps1")
            .And.Contain("\"-RequireDeliveryPipeline\"")
            .And.Contain("scripts\\smoke-vies-live.ps1")
            .And.Contain("scripts\\smoke-object-storage.ps1")
            .And.Contain("docs\\archive-storage-provider-decision.md")
            .And.Contain("docs\\e-invoice-tooling-decision.md")
            .And.Contain("No option is selected yet\\.")
            .And.Contain("No library or tooling is selected yet\\.");

        externalSmokeInputsSource.Should().Contain("Do not store real secret values");
        externalSmokeInputsSource.Should().Contain("scripts\\check-go-live-readiness.ps1");
        externalSmokeInputsSource.Should().Contain("DARWIN_WEBAPI_BASE_URL");
        externalSmokeInputsSource.Should().Contain("DARWIN_STRIPE_WEBHOOK_PUBLIC_URL");
        externalSmokeInputsSource.Should().Contain("DARWIN_DHL_API_BASE_URL");
        externalSmokeInputsSource.Should().Contain("DARWIN_BREVO_API_KEY");
        externalSmokeInputsSource.Should().Contain("DARWIN_VIES_VALID_VAT_ID");
        externalSmokeInputsSource.Should().Contain("DARWIN_OBJECT_STORAGE_PROVIDER");
        externalSmokeInputsSource.Should().Contain("DARWIN_OBJECT_STORAGE_S3_BUCKET");
        externalSmokeInputsSource.Should().Contain("DARWIN_OBJECT_STORAGE_AZURE_CONTAINER");
        externalSmokeInputsSource.Should().Contain("Provider failures must remain `Unknown`");
        externalSmokeInputsSource.Should().NotContain("sk_live_");
        externalSmokeInputsSource.Should().NotContain("sk_test_");
        externalSmokeInputsSource.Should().NotContain("rk_live_");
        externalSmokeInputsSource.Should().NotContain("rk_test_");
        externalSmokeInputsSource.Should().NotContain("whsec_");

        ReadRepositoryFile(Path.Combine("scripts", "smoke-stripe-testmode.ps1"))
            .Should()
            .NotContain("DARWIN_STRIPE_SECRET")
            .And.NotContain("Write-Host $intent.checkoutUrl")
            .And.NotContain("Write-Host $checkoutUri")
            .And.NotContain("Write-Host $response.checkoutUrl",
                "the subscription checkout smoke must not print the provider checkout URL")
            .And.Contain("[switch]$CreateSmokeOrder")
            .And.Contain("function New-StripeSmokeOrder")
            .And.Contain("[switch]$OpenCheckout")
            .And.Contain("[switch]$WaitForWebhookFinalization")
            .And.Contain("[switch]$RequireRuntimePipeline")
            .And.Contain("[switch]$CheckBusinessSubscriptionCheckout",
                "the subscription checkout smoke switch must be present")
            .And.Contain("function Invoke-BusinessSubscriptionCheckoutSmoke",
                "the subscription checkout smoke function must be declared")
            .And.Contain("DARWIN_STRIPE_PROVIDER_CALLBACK_WORKER_CONFIRMED")
            .And.Contain("WebhookWaitSeconds")
            .And.Contain("Creating a public storefront smoke order before Stripe handoff.")
            .And.Contain("Start-Process -FilePath $checkoutUri.AbsoluteUri | Out-Null")
            .And.Contain("The checkout URL and provider references were not printed.")
            .And.Contain("Provider references will not be printed.")
            .And.Contain("Verified Stripe webhook finalization reached payment status")
            .And.Contain("function Write-SafeWebError")
            .And.Contain("function Hide-SensitiveText")
            .And.Contain("Darwin WebApi request failed with HTTP")
            .And.Contain("https://checkout\\.stripe\\.com")
            .And.Contain("[redacted]")
            .And.Contain("Stripe test keys and webhook signing secret must be entered through Settings or secure configuration, not this script.");

        ReadRepositoryFile(Path.Combine("scripts", "check-stripe-webhook-forwarding.ps1"))
            .Should()
            .Contain("DARWIN_STRIPE_WEBHOOK_PUBLIC_URL")
            .And.Contain("DARWIN_STRIPE_WEBHOOK_FORWARDING_CONFIRMED")
            .And.Contain("/api/v1/public/billing/stripe/webhooks")
            .And.Contain("No webhook signing secret is accepted or printed")
            .And.NotContain("whsec_")
            .And.NotContain("StripeSecretKey");

        ReadRepositoryFile(Path.Combine("scripts", "smoke-dhl-live.ps1"))
            .Should()
            .Contain("No secrets or response payloads will be printed.")
            .And.Contain("DARWIN_DHL_API_BASE_URL must be an absolute URL.")
            .And.Contain("DARWIN_DHL_API_BASE_URL must use HTTPS for non-local endpoints.");

        ReadRepositoryFile(Path.Combine("scripts", "smoke-brevo-readiness.ps1"))
            .Should()
            .Contain("No secrets or response payloads will be printed.")
            .And.Contain("DARWIN_BREVO_BASE_URL must be an absolute URL.")
            .And.Contain("DARWIN_BREVO_BASE_URL must use HTTPS for non-local endpoints.")
            .And.Contain("[switch]$RequireDeliveryPipeline")
            .And.Contain("DARWIN_BREVO_WEBHOOK_PUBLIC_URL must end with /api/v1/public/notifications/brevo/webhooks.")
            .And.Contain("DARWIN_BREVO_PROVIDER_CALLBACK_WORKER_CONFIRMED=true")
            .And.Contain("DARWIN_BREVO_EMAIL_DISPATCH_WORKER_CONFIRMED=true");

        ReadRepositoryFile(Path.Combine("scripts", "smoke-vies-live.ps1"))
            .Should()
            .Contain("DARWIN_VIES_ENDPOINT_URL must be an absolute URL.")
            .And.Contain("DARWIN_VIES_ENDPOINT_URL must use HTTPS for non-local endpoints.")
            .And.Contain("DARWIN_VIES_TIMEOUT_SECONDS must be between 1 and 120.");

        ReadRepositoryFile(Path.Combine("scripts", "smoke-object-storage.ps1"))
            .Should()
            .Contain("Provider credentials must be supplied through environment or secure configuration.")
            .And.Contain("No secrets, object payloads, object keys, or provider credentials are printed.")
            .And.Contain("DARWIN_OBJECT_STORAGE_PREFIX")
            .And.Contain("[string]$FileRoot")
            .And.Contain("[switch]$SmokeRetention")
            .And.Contain("DARWIN_OBJECT_STORAGE_SMOKE_RETENTION")
            .And.Contain("Add -SmokeRetention")
            .And.Contain("ObjectStorageKeyBuilder.Build")
            .And.Contain("ObjectStorage:AzureBlob:ConnectionString")
            .And.NotContain("Write-Host $env:DARWIN_OBJECT_STORAGE_S3_SECRET_KEY")
            .And.NotContain("Write-Host $env:DARWIN_OBJECT_STORAGE_AZURE_CONNECTION_STRING");
    }

    [Fact]
    public async Task GoLiveReadinessScript_Should_RunDryRunAndAvoidSecretOutput()
    {
        var root = ResolveRepositoryPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(Path.Combine("scripts", "check-go-live-readiness.ps1"));

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start go-live readiness script.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);

        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        var output = $"{await stdoutTask}{Environment.NewLine}{await stderrTask}";

        process.ExitCode.Should().BeOneOf(0, 2);
        output.Should().Contain("Darwin go-live readiness dry-run summary:");
        output.Should().Contain("Secrets scan: Ready");
        output.Should().Contain("Stripe test-mode smoke prerequisites");
        output.Should().Contain("Stripe webhook forwarding prerequisites");
        output.Should().Contain("DHL live smoke prerequisites");
        output.Should().Contain("Brevo readiness smoke prerequisites");
        output.Should().Contain("VIES live smoke prerequisites");
        output.Should().Contain("Object storage MediaAssets profile prerequisites");
        output.Should().Contain("Object storage ShipmentLabels profile prerequisites");
        output.Should().NotContain("System.Management.Automation.RemoteException");

        var secretPattern = new Regex(@"\b(sk|rk)_(live|test)_[A-Za-z0-9]{12,}\b|\bwhsec_[A-Za-z0-9]{12,}\b", RegexOptions.IgnoreCase);
        secretPattern.IsMatch(output).Should().BeFalse("readiness dry-run output must not print provider secrets");
    }

    [Fact]
    public async Task GoLiveReadinessScript_Should_ReportProviderReadyAndKeepOpenDecisionsBlocked()
    {
        var root = ResolveRepositoryPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(Path.Combine("scripts", "check-go-live-readiness.ps1"));

        foreach (var key in startInfo.Environment.Keys.Cast<string>().Where(key => key.StartsWith("DARWIN_", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            startInfo.Environment.Remove(key);
        }

        foreach (var item in AllProviderReadyDryRunEnvironment())
        {
            startInfo.Environment[item.Key] = item.Value;
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start go-live readiness script.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);

        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        var output = $"{await stdoutTask}{Environment.NewLine}{await stderrTask}";

        process.ExitCode.Should().Be(2);
        output.Should().Contain("Stripe test-mode smoke prerequisites: Ready");
        output.Should().Contain("Stripe webhook forwarding prerequisites: Ready");
        output.Should().Contain("DHL live smoke prerequisites: Ready");
        output.Should().Contain("Brevo readiness smoke prerequisites: Ready");
        output.Should().Contain("VIES live smoke prerequisites: Ready");
        output.Should().Contain("Object storage smoke prerequisites: Ready");
        output.Should().Contain("Object storage MediaAssets profile prerequisites: Ready");
        output.Should().Contain("Object storage ShipmentLabels profile prerequisites: Ready");
        output.Should().Contain("Invoice archive object-storage provider decision: Ready");
        output.Should().Contain("E-invoice tooling decision: Blocked");
        output.Should().NotContain("System.Management.Automation.RemoteException");

        var secretPattern = new Regex(@"\b(sk|rk)_(live|test)_[A-Za-z0-9]{12,}\b|\bwhsec_[A-Za-z0-9]{12,}\b", RegexOptions.IgnoreCase);
        secretPattern.IsMatch(output).Should().BeFalse("readiness dry-run output must not print provider secrets");
    }

    [Theory]
    [InlineData("smoke-stripe-testmode.ps1", "Stripe test-mode smoke is blocked.")]
    [InlineData("check-stripe-webhook-forwarding.ps1", "Stripe webhook forwarding is blocked.")]
    [InlineData("smoke-dhl-live.ps1", "DHL live smoke is blocked.")]
    [InlineData("smoke-brevo-readiness.ps1", "Brevo readiness smoke is blocked.")]
    [InlineData("smoke-vies-live.ps1", "VIES live smoke is blocked.")]
    [InlineData("smoke-object-storage.ps1", "Object storage smoke is blocked.")]
    public async Task ProviderSmokeScripts_Should_BlockDryRunWhenPrerequisitesAreMissing(
        string scriptName,
        string expectedBlockedMessage)
    {
        var root = ResolveRepositoryPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(Path.Combine("scripts", scriptName));

        foreach (var key in startInfo.Environment.Keys.Cast<string>().Where(key => key.StartsWith("DARWIN_", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            startInfo.Environment.Remove(key);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start {scriptName}.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);

        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        var output = $"{await stdoutTask}{Environment.NewLine}{await stderrTask}";

        process.ExitCode.Should().Be(2);
        output.Should().Contain(expectedBlockedMessage);
        output.Should().Contain("Configure these environment variables first:");
        output.Should().NotContain("Run with -Execute to call");
        output.Should().NotContain("System.Management.Automation.RemoteException");

        var secretPattern = new Regex(@"\b(sk|rk)_(live|test)_[A-Za-z0-9]{12,}\b|\bwhsec_[A-Za-z0-9]{12,}\b", RegexOptions.IgnoreCase);
        secretPattern.IsMatch(output).Should().BeFalse($"{scriptName} dry-run output must not print provider secrets");
    }

    [Theory]
    [MemberData(nameof(ProviderSmokeScriptReadyDryRunCases))]
    public async Task ProviderSmokeScripts_Should_ReportReadyDryRunWithoutExecutingExternalCalls(
        string scriptName,
        string expectedReadyMessage,
        IReadOnlyDictionary<string, string> environment)
    {
        var root = ResolveRepositoryPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(Path.Combine("scripts", scriptName));

        foreach (var key in startInfo.Environment.Keys.Cast<string>().Where(key => key.StartsWith("DARWIN_", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            startInfo.Environment.Remove(key);
        }

        foreach (var item in environment)
        {
            startInfo.Environment[item.Key] = item.Value;
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start {scriptName}.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);

        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        var output = $"{await stdoutTask}{Environment.NewLine}{await stderrTask}";

        process.ExitCode.Should().Be(0);
        output.Should().Contain(expectedReadyMessage);
        if (string.Equals(scriptName, "check-stripe-webhook-forwarding.ps1", StringComparison.Ordinal))
        {
            output.Should().Contain("smoke-stripe-testmode.ps1 -Execute");
        }
        else
        {
            output.Should().Contain("Run with -Execute");
        }
        if (string.Equals(scriptName, "smoke-stripe-testmode.ps1", StringComparison.Ordinal))
        {
            output.Should().Contain("Add -CreateSmokeOrder");
            output.Should().Contain("Add -CheckBusinessSubscriptionCheckout",
                "the subscription checkout flag must be described in the dry-run output");
            output.Should().Contain("Add -OpenCheckout");
            output.Should().Contain("Add -WaitForWebhookFinalization");
        }

        output.Should().NotContain("blocked");
        output.Should().NotContain("Invoke-RestMethod");
        output.Should().NotContain("Invoke-WebRequest");
        output.Should().NotContain("System.Management.Automation.RemoteException");

        var secretPattern = new Regex(@"\b(sk|rk)_(live|test)_[A-Za-z0-9]{12,}\b|\bwhsec_[A-Za-z0-9]{12,}\b", RegexOptions.IgnoreCase);
        secretPattern.IsMatch(output).Should().BeFalse($"{scriptName} ready dry-run output must not print provider secrets");
    }

    [Fact]
    public async Task StripeSubscriptionCheckoutSmoke_Should_BlockWhenCheckoutPrerequisitesMissing()
    {
        // Proves that smoke-stripe-testmode.ps1 exits 2 when -CheckBusinessSubscriptionCheckout
        // is passed but DARWIN_BUSINESS_API_BEARER_TOKEN and/or DARWIN_STRIPE_SMOKE_BILLING_PLAN_ID
        // are absent, and that the blocked output does not print any provider secrets.
        var root = ResolveRepositoryPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(Path.Combine("scripts", "smoke-stripe-testmode.ps1"));
        startInfo.ArgumentList.Add("-CheckBusinessSubscriptionCheckout");

        foreach (var key in startInfo.Environment.Keys.Cast<string>()
                     .Where(key => key.StartsWith("DARWIN_", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            startInfo.Environment.Remove(key);
        }

        // Intentionally omit DARWIN_BUSINESS_API_BEARER_TOKEN and DARWIN_STRIPE_SMOKE_BILLING_PLAN_ID
        // Only DARWIN_WEBAPI_BASE_URL is absent as well, confirming multi-prerequisite validation
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start smoke-stripe-testmode.ps1.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);

        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        var output = $"{await stdoutTask}{Environment.NewLine}{await stderrTask}";

        process.ExitCode.Should().Be(2,
            "smoke-stripe-testmode.ps1 with -CheckBusinessSubscriptionCheckout and missing prerequisites must exit 2");
        output.Should().Contain("Stripe test-mode smoke is blocked.",
            "the blocked message must be written to output when prerequisites are missing");

        var secretPattern = new Regex(@"\b(sk|rk)_(live|test)_[A-Za-z0-9]{12,}\b|\bwhsec_[A-Za-z0-9]{12,}\b", RegexOptions.IgnoreCase);
        secretPattern.IsMatch(output).Should().BeFalse("blocked output must not print provider secrets");
    }

    [Fact]
    public async Task StripeSubscriptionCheckoutSmoke_Should_ReportReadyDryRunWithCheckoutPrerequisites()
    {
        // Proves that smoke-stripe-testmode.ps1 exits 0 when -CheckBusinessSubscriptionCheckout
        // is passed and all required checkout prerequisites are present (without -Execute).
        var root = ResolveRepositoryPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(Path.Combine("scripts", "smoke-stripe-testmode.ps1"));
        startInfo.ArgumentList.Add("-CheckBusinessSubscriptionCheckout");

        foreach (var key in startInfo.Environment.Keys.Cast<string>()
                     .Where(key => key.StartsWith("DARWIN_", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            startInfo.Environment.Remove(key);
        }

        startInfo.Environment["DARWIN_WEBAPI_BASE_URL"] = "http://127.0.0.1:5134";
        startInfo.Environment["DARWIN_BUSINESS_API_BEARER_TOKEN"] = "fake-bearer-token-for-unit-test";
        startInfo.Environment["DARWIN_STRIPE_SMOKE_BILLING_PLAN_ID"] = "22222222-2222-2222-2222-222222222222";

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start smoke-stripe-testmode.ps1.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);

        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        var output = $"{await stdoutTask}{Environment.NewLine}{await stderrTask}";

        process.ExitCode.Should().Be(0,
            "smoke-stripe-testmode.ps1 with -CheckBusinessSubscriptionCheckout and all prerequisites present must exit 0 in dry-run mode");
        output.Should().Contain("Stripe test-mode smoke configuration is present.");
        output.Should().NotContain("blocked");
        output.Should().NotContain("Invoke-RestMethod");
        output.Should().NotContain("Invoke-WebRequest");
        output.Should().NotContain("System.Management.Automation.RemoteException");

        var secretPattern = new Regex(@"\b(sk|rk)_(live|test)_[A-Za-z0-9]{12,}\b|\bwhsec_[A-Za-z0-9]{12,}\b", RegexOptions.IgnoreCase);
        secretPattern.IsMatch(output).Should().BeFalse("subscription checkout dry-run output must not print provider secrets");
    }

    [Fact]
    public async Task ObjectStorageSmoke_Should_BlockWhenProviderNameIsUnsupported()
    {
        var root = ResolveRepositoryPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(Path.Combine("scripts", "smoke-object-storage.ps1"));

        foreach (var key in startInfo.Environment.Keys.Cast<string>()
                     .Where(key => key.StartsWith("DARWIN_", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            startInfo.Environment.Remove(key);
        }

        startInfo.Environment["DARWIN_OBJECT_STORAGE_PROVIDER"] = "Unsupported";
        startInfo.Environment["DARWIN_OBJECT_STORAGE_CONTAINER"] = "smoke";

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start smoke-object-storage.ps1.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);

        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        var output = $"{await stdoutTask}{Environment.NewLine}{await stderrTask}";

        process.ExitCode.Should().Be(2);
        output.Should().Contain("Object storage smoke is blocked. Provider must be S3Compatible, AzureBlob, or FileSystem.");
        output.Should().NotContain("System.Management.Automation.RemoteException");
    }

    [Fact]
    public async Task ObjectStorageSmoke_Should_BlockS3DryRunWhenEndpointAndRegionAreBothMissing()
    {
        var root = ResolveRepositoryPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(Path.Combine("scripts", "smoke-object-storage.ps1"));

        foreach (var key in startInfo.Environment.Keys.Cast<string>()
                     .Where(key => key.StartsWith("DARWIN_", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            startInfo.Environment.Remove(key);
        }

        startInfo.Environment["DARWIN_OBJECT_STORAGE_PROVIDER"] = "S3Compatible";
        startInfo.Environment["DARWIN_OBJECT_STORAGE_CONTAINER"] = "smoke";
        startInfo.Environment["DARWIN_OBJECT_STORAGE_S3_BUCKET"] = "smoke-bucket";
        startInfo.Environment["DARWIN_OBJECT_STORAGE_S3_ACCESS_KEY"] = "smoke-access";
        startInfo.Environment["DARWIN_OBJECT_STORAGE_S3_SECRET_KEY"] = "smoke-secret";

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start smoke-object-storage.ps1.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);

        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        var output = $"{await stdoutTask}{Environment.NewLine}{await stderrTask}";

        process.ExitCode.Should().Be(2);
        output.Should().Contain("Object storage smoke is blocked. Configure these environment variables first:");
        output.Should().Contain("DARWIN_OBJECT_STORAGE_S3_ENDPOINT_OR_REGION");
        output.Should().NotContain("System.Management.Automation.RemoteException");
    }

    [Fact]
    public async Task ObjectStorageSmoke_Should_ReportReadyForAzureManagedIdentityWithoutConnectionString()
    {
        var root = ResolveRepositoryPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(Path.Combine("scripts", "smoke-object-storage.ps1"));

        foreach (var key in startInfo.Environment.Keys.Cast<string>()
                     .Where(key => key.StartsWith("DARWIN_", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            startInfo.Environment.Remove(key);
        }

        startInfo.Environment["DARWIN_OBJECT_STORAGE_PROVIDER"] = "AzureBlob";
        startInfo.Environment["DARWIN_OBJECT_STORAGE_CONTAINER"] = "smoke";
        startInfo.Environment["DARWIN_OBJECT_STORAGE_AZURE_CONTAINER"] = "smoke-azure";
        startInfo.Environment["DARWIN_OBJECT_STORAGE_AZURE_ACCOUNT_NAME"] = "storageaccount";
        startInfo.Environment["DARWIN_OBJECT_STORAGE_AZURE_USE_MANAGED_IDENTITY"] = "true";

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start smoke-object-storage.ps1.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);

        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        var output = $"{await stdoutTask}{Environment.NewLine}{await stderrTask}";

        process.ExitCode.Should().Be(0);
        output.Should().Contain("Object storage smoke configuration is present for provider 'AzureBlob'.");
        output.Should().Contain("Run with -Execute");
        output.Should().NotContain("blocked");
        output.Should().NotContain("System.Management.Automation.RemoteException");
    }

    public static IEnumerable<object[]> ProviderSmokeScriptReadyDryRunCases()
    {
        yield return new object[]
        {
            "smoke-stripe-testmode.ps1",
            "Stripe test-mode smoke configuration is present.",
            new Dictionary<string, string>
            {
                ["DARWIN_WEBAPI_BASE_URL"] = "http://127.0.0.1:5134",
                ["DARWIN_STRIPE_SMOKE_ORDER_ID"] = "11111111-1111-1111-1111-111111111111",
                ["DARWIN_STRIPE_SMOKE_ORDER_NUMBER"] = "SMOKE-STRIPE-001"
            }
        };

        yield return new object[]
        {
            "check-stripe-webhook-forwarding.ps1",
            "Stripe webhook forwarding prerequisites are present.",
            new Dictionary<string, string>
            {
                ["DARWIN_STRIPE_WEBHOOK_PUBLIC_URL"] = "https://stripe-webhook.example.test/api/v1/public/billing/stripe/webhooks",
                ["DARWIN_STRIPE_WEBHOOK_FORWARDING_CONFIRMED"] = "true"
            }
        };

        yield return new object[]
        {
            "smoke-dhl-live.ps1",
            "DHL live smoke configuration is present.",
            new Dictionary<string, string>
            {
                ["DARWIN_DHL_API_BASE_URL"] = "http://127.0.0.1:5135",
                ["DARWIN_DHL_API_KEY"] = "local-api-key",
                ["DARWIN_DHL_API_SECRET"] = "local-api-secret",
                ["DARWIN_DHL_ACCOUNT_NUMBER"] = "1234567890",
                ["DARWIN_DHL_SHIPPER_NAME"] = "Darwin Smoke Sender",
                ["DARWIN_DHL_SHIPPER_STREET"] = "Sender Street 1",
                ["DARWIN_DHL_SHIPPER_POSTAL_CODE"] = "10115",
                ["DARWIN_DHL_SHIPPER_CITY"] = "Berlin",
                ["DARWIN_DHL_SHIPPER_COUNTRY"] = "DE",
                ["DARWIN_DHL_SHIPPER_EMAIL"] = "sender@example.test",
                ["DARWIN_DHL_SHIPPER_PHONE_E164"] = "+491234567890",
                ["DARWIN_DHL_TEST_RECEIVER_NAME"] = "Darwin Smoke Receiver",
                ["DARWIN_DHL_TEST_RECEIVER_STREET"] = "Receiver Street 2",
                ["DARWIN_DHL_TEST_RECEIVER_POSTAL_CODE"] = "10115",
                ["DARWIN_DHL_TEST_RECEIVER_CITY"] = "Berlin",
                ["DARWIN_DHL_TEST_RECEIVER_COUNTRY"] = "DE"
            }
        };

        yield return new object[]
        {
            "smoke-brevo-readiness.ps1",
            "Brevo readiness smoke configuration is present.",
            new Dictionary<string, string>
            {
                ["DARWIN_BREVO_API_KEY"] = "local-api-key",
                ["DARWIN_BREVO_SENDER_EMAIL"] = "sender@example.test",
                ["DARWIN_BREVO_TEST_RECIPIENT_EMAIL"] = "recipient@example.test"
            }
        };

        yield return new object[]
        {
            "smoke-vies-live.ps1",
            "VIES live smoke configuration is present.",
            new Dictionary<string, string>
            {
                ["DARWIN_VIES_VALID_VAT_ID"] = "DE123456789",
                ["DARWIN_VIES_INVALID_VAT_ID"] = "DE000000000",
                ["DARWIN_VIES_ENDPOINT_URL"] = "http://127.0.0.1:5135/vies"
            }
        };

        yield return new object[]
        {
            "smoke-object-storage.ps1",
            "Object storage smoke configuration is present",
            new Dictionary<string, string>
            {
                ["DARWIN_OBJECT_STORAGE_PROVIDER"] = "FileSystem",
                ["DARWIN_OBJECT_STORAGE_CONTAINER"] = "smoke",
                ["DARWIN_OBJECT_STORAGE_FILE_ROOT"] = Path.Combine(Path.GetTempPath(), "darwin-object-storage-ready-dry-run")
            }
        };
    }

    private static IReadOnlyDictionary<string, string> AllProviderReadyDryRunEnvironment() =>
        new Dictionary<string, string>
        {
            ["DARWIN_WEBAPI_BASE_URL"] = "http://127.0.0.1:5134",
            ["DARWIN_STRIPE_SMOKE_ORDER_ID"] = "11111111-1111-1111-1111-111111111111",
            ["DARWIN_STRIPE_SMOKE_ORDER_NUMBER"] = "SMOKE-STRIPE-001",
            ["DARWIN_STRIPE_WEBHOOK_PUBLIC_URL"] = "https://stripe-webhook.example.test/api/v1/public/billing/stripe/webhooks",
            ["DARWIN_STRIPE_WEBHOOK_FORWARDING_CONFIRMED"] = "true",
            ["DARWIN_STRIPE_PROVIDER_CALLBACK_WORKER_CONFIRMED"] = "true",
            ["DARWIN_DHL_API_BASE_URL"] = "http://127.0.0.1:5135",
            ["DARWIN_DHL_API_KEY"] = "local-api-key",
            ["DARWIN_DHL_API_SECRET"] = "local-api-secret",
            ["DARWIN_DHL_ACCOUNT_NUMBER"] = "1234567890",
            ["DARWIN_DHL_SHIPPER_NAME"] = "Darwin Smoke Sender",
            ["DARWIN_DHL_SHIPPER_STREET"] = "Sender Street 1",
            ["DARWIN_DHL_SHIPPER_POSTAL_CODE"] = "10115",
            ["DARWIN_DHL_SHIPPER_CITY"] = "Berlin",
            ["DARWIN_DHL_SHIPPER_COUNTRY"] = "DE",
            ["DARWIN_DHL_SHIPPER_EMAIL"] = "sender@example.test",
            ["DARWIN_DHL_SHIPPER_PHONE_E164"] = "+491234567890",
            ["DARWIN_DHL_TEST_RECEIVER_NAME"] = "Darwin Smoke Receiver",
            ["DARWIN_DHL_TEST_RECEIVER_STREET"] = "Receiver Street 2",
            ["DARWIN_DHL_TEST_RECEIVER_POSTAL_CODE"] = "10115",
            ["DARWIN_DHL_TEST_RECEIVER_CITY"] = "Berlin",
            ["DARWIN_DHL_TEST_RECEIVER_COUNTRY"] = "DE",
            ["DARWIN_DHL_SHIPMENT_PROVIDER_OPERATION_WORKER_CONFIRMED"] = "true",
            ["DARWIN_DHL_PROVIDER_CALLBACK_WORKER_CONFIRMED"] = "true",
            ["DARWIN_DHL_SHIPMENT_LABELS_STORAGE_CONFIRMED"] = "true",
            ["DARWIN_BREVO_API_KEY"] = "local-api-key",
            ["DARWIN_BREVO_SENDER_EMAIL"] = "sender@example.test",
            ["DARWIN_BREVO_TEST_RECIPIENT_EMAIL"] = "recipient@example.test",
            ["DARWIN_BREVO_WEBHOOK_PUBLIC_URL"] = "https://brevo-webhook.example.test/api/v1/public/notifications/brevo/webhooks",
            ["DARWIN_BREVO_WEBHOOK_CONFIGURED_CONFIRMED"] = "true",
            ["DARWIN_BREVO_TRANSACTIONAL_EVENTS_CONFIRMED"] = "true",
            ["DARWIN_BREVO_PROVIDER_CALLBACK_WORKER_CONFIRMED"] = "true",
            ["DARWIN_BREVO_EMAIL_DISPATCH_WORKER_CONFIRMED"] = "true",
            ["DARWIN_VIES_VALID_VAT_ID"] = "DE123456789",
            ["DARWIN_VIES_INVALID_VAT_ID"] = "DE000000000",
            ["DARWIN_VIES_ENDPOINT_URL"] = "http://127.0.0.1:5135/vies",
            ["DARWIN_OBJECT_STORAGE_PROVIDER"] = "FileSystem",
            ["DARWIN_OBJECT_STORAGE_CONTAINER"] = "smoke",
            ["DARWIN_OBJECT_STORAGE_FILE_ROOT"] = Path.Combine(Path.GetTempPath(), "darwin-object-storage-ready-dry-run")
        };

    [Fact]
    public void RepositoryDocsAndOperationalScripts_Should_AvoidAssistantMentionsAndCommittedSecretPatterns()
    {
        var root = ResolveRepositoryPath();
        var docsAndScripts = Directory
            .GetFiles(Path.Combine(root, "docs"), "*.md", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(root, "scripts"), "*.ps1", SearchOption.TopDirectoryOnly))
            .Concat(Directory.GetFiles(root, "*.md", SearchOption.TopDirectoryOnly))
            .Select(path => new
            {
                Path = Path.GetRelativePath(root, path),
                Source = File.ReadAllText(path)
            })
            .ToList();

        docsAndScripts.Should().NotBeEmpty();

        var assistantMentionPattern = new Regex(@"\b(ChatGPT|Codex|OpenAI|AI-generated|AI generated)\b", RegexOptions.IgnoreCase);
        docsAndScripts
            .Where(file => assistantMentionPattern.IsMatch(file.Source))
            .Select(file => file.Path)
            .Should()
            .BeEmpty("repository documentation and operational scripts should not mention assistant tooling");

        var secretPattern = new Regex(@"\b(sk|rk)_(live|test)_[A-Za-z0-9]{12,}\b|\bwhsec_[A-Za-z0-9]{12,}\b", RegexOptions.IgnoreCase);
        docsAndScripts
            .Where(file => secretPattern.IsMatch(file.Source))
            .Select(file => file.Path)
            .Should()
            .BeEmpty("repository documentation and operational scripts must not contain real provider secret values");
    }

    [Fact]
    public void InvoiceArchiveStorage_Should_KeepInternalFallbackBoundaryAndAvoidImplicitProviderChoice()
    {
        var abstractionSource = ReadApplicationFile(Path.Combine("Abstractions", "Invoicing", "IInvoiceArchiveStorage.cs"));
        var databaseProviderSource = ReadApplicationFile(Path.Combine("CRM", "Services", "DatabaseInvoiceArchiveStorage.cs"));
        var fileSystemProviderSource = ReadApplicationFile(Path.Combine("CRM", "Services", "FileSystemInvoiceArchiveStorage.cs"));
        var routerSource = ReadApplicationFile(Path.Combine("CRM", "Services", "InvoiceArchiveStorageRouter.cs"));
        var applicationCompositionSource = ReadApplicationFile(Path.Combine("Extensions", "ServiceCollectionExtensions.Application.cs"));
        var providerDecisionSource = ReadRepositoryFile(Path.Combine("docs", "archive-storage-provider-decision.md"));

        abstractionSource.Should().Contain("public interface IInvoiceArchiveStorage");
        abstractionSource.Should().Contain("Task<InvoiceArchiveStorageResult> SaveAsync");
        abstractionSource.Should().Contain("Task<InvoiceArchiveStorageArtifact?> ReadAsync");
        abstractionSource.Should().Contain("Task<bool> ExistsAsync");
        abstractionSource.Should().Contain("Task PurgePayloadAsync");
        abstractionSource.Should().Contain("string HashSha256");
        abstractionSource.Should().Contain("DateTime RetainUntilUtc");
        abstractionSource.Should().Contain("string RetentionPolicyVersion");
        abstractionSource.Should().Contain("public interface IInvoiceArchiveStorageProvider : IInvoiceArchiveStorage");
        abstractionSource.Should().Contain("string ProviderName");
        abstractionSource.Should().Contain("InvoiceArchiveStorageProviderNames");
        abstractionSource.Should().Contain("InternalDatabase");
        abstractionSource.Should().Contain("AzureBlob");
        abstractionSource.Should().Contain("AwsS3");
        abstractionSource.Should().Contain("Minio");
        abstractionSource.Should().Contain("FileSystem");

        databaseProviderSource.Should().Contain("public sealed class DatabaseInvoiceArchiveStorage : IInvoiceArchiveStorageProvider");
        databaseProviderSource.Should().Contain("ProviderName => InvoiceArchiveStorageProviderNames.InternalDatabase");
        databaseProviderSource.Should().Contain("SHA256.HashData");
        databaseProviderSource.Should().Contain("InvoiceArchiveRetentionYears");
        databaseProviderSource.Should().Contain("Math.Clamp(settings ?? 10, 1, 30)");
        databaseProviderSource.Should().Contain("ArchivePurgedAtUtc");
        databaseProviderSource.Should().Contain("ArchivePurgeReason");

        fileSystemProviderSource.Should().Contain("public sealed class FileSystemInvoiceArchiveStorage : IInvoiceArchiveStorageProvider");
        fileSystemProviderSource.Should().Contain("ProviderName => InvoiceArchiveStorageProviderNames.FileSystem");
        fileSystemProviderSource.Should().Contain("Invoice archive file-system root path is not configured.");
        fileSystemProviderSource.Should().Contain("Path.GetFullPath");
        fileSystemProviderSource.Should().Contain("File.WriteAllTextAsync");
        fileSystemProviderSource.Should().Contain("File.ReadAllTextAsync");
        fileSystemProviderSource.Should().Contain("WriteMetadataAsync");
        fileSystemProviderSource.Should().Contain("ReadMetadataAsync");
        fileSystemProviderSource.Should().Contain("StoredArchiveMetadata");
        fileSystemProviderSource.Should().Contain("JsonException");
        fileSystemProviderSource.Should().Contain("File.Delete");
        fileSystemProviderSource.Should().NotContain("Azure.Storage.Blobs");
        fileSystemProviderSource.Should().NotContain("Amazon.S3");
        fileSystemProviderSource.Should().NotContain("using Minio");

        routerSource.Should().Contain("public sealed class InvoiceArchiveStorageRouter : IInvoiceArchiveStorage");
        routerSource.Should().Contain("IEnumerable<IInvoiceArchiveStorageProvider>");
        routerSource.Should().Contain("InvoiceArchiveStorageSelection");
        routerSource.Should().Contain("Invoice archive storage provider");

        applicationCompositionSource.Should().Contain("services.AddSingleton(_ =>");
        applicationCompositionSource.Should().Contain("configuration?.GetSection(\"InvoiceArchiveStorage\").Bind(selection);");
        applicationCompositionSource.Should().Contain("configuration?.GetSection(\"InvoiceArchiveStorage:FileSystem\").Bind(options);");
        applicationCompositionSource.Should().Contain("services.AddScoped<DatabaseInvoiceArchiveStorage>();");
        applicationCompositionSource.Should().Contain("services.AddScoped<FileSystemInvoiceArchiveStorage>();");
        applicationCompositionSource.Should().Contain("InvoiceArchiveStorageProviderNames.AzureBlob");
        applicationCompositionSource.Should().Contain("services.AddScoped<IInvoiceArchiveStorageProvider>");
        applicationCompositionSource.Should().Contain("services.AddScoped<IInvoiceArchiveStorage, InvoiceArchiveStorageRouter>();");

        var objectStorageArchiveProviderSource = ReadApplicationFile(Path.Combine("CRM", "Services", "ObjectStorageInvoiceArchiveStorage.cs"));
        objectStorageArchiveProviderSource.Should().Contain("private const string InvoiceArchiveProfileName = \"InvoiceArchive\";");
        objectStorageArchiveProviderSource.Should().Contain("ProfileName: InvoiceArchiveProfileName");
        objectStorageArchiveProviderSource.Should().Contain("InvoiceArchiveStorageProviderNames.AzureBlob");
        objectStorageArchiveProviderSource.Should().Contain("AllowLockedDelete: invoice.ArchiveRetainUntilUtc <= purgedAtUtc");

        var applicationSources = Directory
            .GetFiles(ResolveRepositoryPath("src", "Darwin.Application"), "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText)
            .ToArray();

        applicationSources.Should().NotContain(source => source.Contains("Azure.Storage.Blobs", StringComparison.Ordinal));
        applicationSources.Should().NotContain(source => source.Contains("Amazon.S3", StringComparison.Ordinal));
        applicationSources.Should().NotContain(source => source.Contains("using Minio", StringComparison.OrdinalIgnoreCase));
        applicationSources.Should().NotContain(source => source.Contains("new Minio", StringComparison.OrdinalIgnoreCase));
        applicationSources.Should().NotContain(source => source.Contains("Azure.Storage.Blobs", StringComparison.Ordinal));
        applicationSources.Should().NotContain(source => source.Contains("AmazonS3", StringComparison.Ordinal));

        providerDecisionSource.Should().Contain("Production provider direction is now selected");
        providerDecisionSource.Should().Contain("MinIO as the recommended self-hosted production target");
        providerDecisionSource.Should().Contain("Immutable retention or legal hold support");
        providerDecisionSource.Should().Contain("Audit logs for create, read, retention-policy change, legal-hold change, and delete/purge attempts.");
        providerDecisionSource.Should().Contain("Darwin must connect to MinIO through the generic S3-compatible provider.");
        providerDecisionSource.Should().NotContain("Selected provider:");
        providerDecisionSource.Should().NotContain("Production immutable archive storage is complete");
    }

    [Fact]
    public void EInvoiceDocs_Should_KeepPlanningStatusAndAvoidFalseComplianceClaims()
    {
        var complianceDecisionSource = ReadRepositoryFile(Path.Combine("docs", "compliance-decisions.md"));
        var toolingDecisionSource = ReadRepositoryFile(Path.Combine("docs", "e-invoice-tooling-decision.md"));
        var goLiveStatusSource = ReadRepositoryFile(Path.Combine("docs", "go-live-status.md"));
        var productionSetupSource = ReadRepositoryFile(Path.Combine("docs", "production-setup.md"));
        var backlogSource = ReadRepositoryFile("BACKLOG.md");
        var abstractionSource = ReadApplicationFile(Path.Combine("Abstractions", "Invoicing", "IEInvoiceGenerationService.cs"));
        var invoiceHandlersSource = ReadApplicationFile(Path.Combine("CRM", "Commands", "InvoiceHandlers.cs"));
        var defaultServiceSource = ReadApplicationFile(Path.Combine("CRM", "Services", "NotConfiguredEInvoiceGenerationService.cs"));
        var readinessValidatorSource = ReadApplicationFile(Path.Combine("CRM", "Services", "EInvoiceSourceReadinessValidator.cs"));
        var applicationCompositionSource = ReadApplicationFile(Path.Combine("Extensions", "ServiceCollectionExtensions.Application.cs"));

        complianceDecisionSource.Should().Contain("Primary target: ZUGFeRD/Factur-X");
        complianceDecisionSource.Should().Contain("Secondary target: XRechnung export");
        complianceDecisionSource.Should().Contain("not full e-invoice compliance");
        complianceDecisionSource.Should().Contain("The ZUGFeRD/Factur-X library/tooling decision is still open.");
        complianceDecisionSource.Should().Contain("PDF/A-3");
        complianceDecisionSource.Should().Contain("embedded XML");
        complianceDecisionSource.Should().Contain("docs/e-invoice-tooling-decision.md");

        toolingDecisionSource.Should().Contain("No library or tooling is selected yet.");
        toolingDecisionSource.Should().Contain("PDF/A-3 generation or embedding support");
        toolingDecisionSource.Should().Contain("Structured XML validation before download.");
        toolingDecisionSource.Should().Contain("Failure modes that keep the invoice in manual review instead of exposing invalid artifacts.");
        toolingDecisionSource.Should().Contain("Do not claim full e-invoice compliance from JSON, HTML, structured source-model JSON, or CSV outputs.");
        toolingDecisionSource.Should().Contain("MaxArtifactBytes");
        toolingDecisionSource.Should().Contain("non-PDF ZUGFeRD/Factur-X");
        toolingDecisionSource.Should().Contain("malformed XRechnung XML");
        toolingDecisionSource.Should().Contain("IEInvoiceGenerationService");
        toolingDecisionSource.Should().Contain("NotConfigured");
        toolingDecisionSource.Should().NotContain("Selected library:");
        toolingDecisionSource.Should().NotContain("Full e-invoice compliance is complete");

        goLiveStatusSource.Should().Contain("Full e-invoicing compliance is not implemented.");
        goLiveStatusSource.Should().Contain("Current JSON/HTML/CSV/source-model exports are not full e-invoice compliance.");
        goLiveStatusSource.Should().Contain("E-invoice phase 1 now has a structured invoice source-model export from issued snapshots, a minimum source-readiness validator");
        goLiveStatusSource.Should().Contain("non-PDF ZUGFeRD/Factur-X");
        goLiveStatusSource.Should().Contain("malformed XRechnung XML");
        goLiveStatusSource.Should().Contain("docs/e-invoice-tooling-decision.md");

        productionSetupSource.Should().Contain("\"MaxArtifactBytes\": 20971520");
        productionSetupSource.Should().Contain("non-PDF ZUGFeRD/Factur-X outputs");
        productionSetupSource.Should().Contain("malformed XRechnung XML outputs");
        productionSetupSource.Should().Contain("not full legal validation");

        backlogSource.Should().Contain("structured invoice source-model JSON, minimum source-readiness validation, and a provider-neutral `IEInvoiceGenerationService` boundary now exist");
        backlogSource.Should().Contain("E-invoice library/tooling for ZUGFeRD/Factur-X PDF/A-3 embedding and structured XML validation.");

        abstractionSource.Should().Contain("public interface IEInvoiceGenerationService");
        abstractionSource.Should().Contain("Task<EInvoiceGenerationResult> GenerateAsync");
        abstractionSource.Should().Contain("EInvoiceSourceReadinessResult");
        abstractionSource.Should().Contain("EInvoiceArtifactFormat");
        abstractionSource.Should().Contain("ZugferdFacturX");
        abstractionSource.Should().Contain("XRechnung");
        abstractionSource.Should().Contain("EInvoiceGenerationStatus");
        abstractionSource.Should().Contain("NotConfigured");
        abstractionSource.Should().Contain("ValidationFailed");

        invoiceHandlersSource.Should().Contain("public sealed class GenerateInvoiceEInvoiceArtifactHandler");
        invoiceHandlersSource.Should().Contain("EInvoiceGenerationStatus.InvoiceUnavailable");
        invoiceHandlersSource.Should().Contain("EInvoiceGenerationStatus.SourceSnapshotUnavailable");
        invoiceHandlersSource.Should().Contain("EInvoiceGenerationStatus.UnsupportedFormat");
        invoiceHandlersSource.Should().Contain("EInvoiceGenerationStatus.ValidationFailed");
        invoiceHandlersSource.Should().Contain("EInvoiceSourceReadinessValidator");
        invoiceHandlersSource.Should().Contain("Generated e-invoice artifact invoice id does not match the requested invoice.");
        invoiceHandlersSource.Should().Contain("Generated e-invoice artifact metadata is incomplete.");

        readinessValidatorSource.Should().Contain("public sealed class EInvoiceSourceReadinessValidator");
        readinessValidatorSource.Should().Contain("issuedSnapshotJson.validJson");
        readinessValidatorSource.Should().Contain("issuer.legalName");
        readinessValidatorSource.Should().Contain("customer.name");
        readinessValidatorSource.Should().Contain("lines[");

        defaultServiceSource.Should().Contain("public sealed class NotConfiguredEInvoiceGenerationService : IEInvoiceGenerationService");
        defaultServiceSource.Should().Contain("EInvoiceGenerationStatus.NotConfigured");
        defaultServiceSource.Should().Contain("not legal e-invoice artifacts");
        defaultServiceSource.Should().NotContain("new EInvoiceArtifact");
        applicationCompositionSource.Should().Contain("services.AddSingleton<EInvoiceSourceReadinessValidator>();");
        applicationCompositionSource.Should().Contain("services.AddScoped<IEInvoiceGenerationService, NotConfiguredEInvoiceGenerationService>();");

        var externalCommandOptionsSource = ReadInfrastructureFile(Path.Combine("Compliance", "ExternalCommandEInvoiceOptions.cs"));
        var externalCommandServiceSource = ReadInfrastructureFile(Path.Combine("Compliance", "ExternalCommandEInvoiceGenerationService.cs"));
        externalCommandOptionsSource.Should().Contain("MaxArtifactBytes");
        externalCommandServiceSource.Should().Contain("ValidateArtifactContent");
        externalCommandServiceSource.Should().Contain("LooksLikePdf");
        externalCommandServiceSource.Should().Contain("LooksLikeXml");
        externalCommandServiceSource.Should().Contain("Math.Clamp(options.MaxArtifactBytes");

        var guardedSources = new[]
        {
            complianceDecisionSource,
            toolingDecisionSource,
            goLiveStatusSource,
            productionSetupSource,
            backlogSource
        };

        guardedSources.Should().NotContain(source => source.Contains("full e-invoice compliance is implemented", StringComparison.OrdinalIgnoreCase));
        guardedSources.Should().NotContain(source => source.Contains("ZUGFeRD/Factur-X generation is complete", StringComparison.OrdinalIgnoreCase));
        guardedSources.Should().NotContain(source => source.Contains("XRechnung export is complete", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DhlReturnsRmaDocsAndClient_Should_KeepCarrierAutomationPendingAndAvoidFakeLabels()
    {
        var goLiveStatusSource = ReadRepositoryFile(Path.Combine("docs", "go-live-status.md"));
        var backlogSource = ReadRepositoryFile("BACKLOG.md");
        var productionSetupSource = ReadRepositoryFile(Path.Combine("docs", "production-setup.md"));
        var dhlClientSource = ReadInfrastructureFile(Path.Combine("Shipping", "Dhl", "DhlShipmentProviderClient.cs"));
        var dhlContractSource = ReadApplicationFile(Path.Combine("Abstractions", "Shipping", "IDhlShipmentProviderClient.cs"));

        goLiveStatusSource.Should().Contain("Returns/RMA flows are visible, but full carrier-integrated RMA automation remains a go-live task.");
        goLiveStatusSource.Should().Contain("Carrier-integrated RMA automation remains under the DHL/shipping go-live slice.");
        backlogSource.Should().Contain("Carrier-integrated DHL RMA/returns automation beyond the current return-label queue path, returns queue, and shipment provider operations.");
        productionSetupSource.Should().Contain("Do not create fake DHL labels, fake references, or local fake tracking URLs.");

        dhlContractSource.Should().Contain("CreateShipmentAsync");
        dhlContractSource.Should().Contain("CreateReturnShipmentAsync");
        dhlContractSource.Should().Contain("GetLabelAsync");
        dhlContractSource.Should().NotContain("Rma");

        dhlClientSource.Should().Contain("orders?validate=false");
        dhlClientSource.Should().Contain("orders?shipment=");
        dhlClientSource.Should().NotContain("fake", "DHL client must not generate fake provider references or labels");
        dhlClientSource.Should().NotContain("local fake", "DHL client must not generate local fake tracking URLs");
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
