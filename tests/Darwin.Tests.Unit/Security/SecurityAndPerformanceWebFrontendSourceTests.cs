using System.Text.Json;
using FluentAssertions;

namespace Darwin.Tests.Unit.Security;

public sealed class SecurityAndPerformanceWebFrontendSourceTests : SecurityAndPerformanceSourceTestBase
{
    private static readonly string[] LocalizationResourceNames =
    [
        "shared",
        "home",
        "catalog",
        "commerce",
        "member",
        "shell"
    ];

    [Fact]
    public void WebFrontendPackageAndTooling_Should_KeepNextRuntimeStrictTypesAndLocalTestsWired()
    {
        var packageSource = ReadWebFrontendFile("package.json");
        var tsconfigSource = ReadWebFrontendFile("tsconfig.json");
        var nextConfigSource = ReadWebFrontendFile("next.config.ts");
        var envExampleSource = ReadWebFrontendFile(".env.example");

        packageSource.Should().Contain("\"name\": \"darwin.web\"");
        packageSource.Should().Contain("\"private\": true");
        packageSource.Should().Contain("\"dev\": \"next dev\"");
        packageSource.Should().Contain("\"build\": \"next build\"");
        packageSource.Should().Contain("\"start\": \"next start\"");
        packageSource.Should().Contain("\"test\": \"tsx --require ./src/test/server-only-shim.cjs --test src/**/*.test.ts\"");
        packageSource.Should().Contain("\"next\": \"16.2.1\"");
        packageSource.Should().Contain("\"react\": \"19.2.4\"");
        packageSource.Should().Contain("\"react-dom\": \"19.2.4\"");
        packageSource.Should().Contain("\"server-only\"");

        tsconfigSource.Should().Contain("\"strict\": true");
        tsconfigSource.Should().Contain("\"moduleResolution\": \"bundler\"");
        tsconfigSource.Should().Contain("\"jsx\": \"react-jsx\"");
        tsconfigSource.Should().Contain("\"@/*\": [\"./src/*\"]");

        nextConfigSource.Should().Contain("process.env.DARWIN_WEBAPI_BASE_URL ?? \"http://localhost:5134\"");
        nextConfigSource.Should().Contain("process.env.DARWIN_WEB_ALLOW_INSECURE_WEBAPI_TLS");
        envExampleSource.Should().Contain("DARWIN_WEBAPI_BASE_URL=");
        envExampleSource.Should().NotContain("sk_");
        envExampleSource.Should().NotContain("rk_");
        envExampleSource.Should().NotContain("whsec_");
    }

    [Fact]
    public void WebFrontendLocalizationResources_Should_KeepEnglishAndGermanKeyParity()
    {
        foreach (var resourceName in LocalizationResourceNames)
        {
            var englishKeys = ReadJsonKeys(Path.Combine("src", "localization", "resources", $"{resourceName}.en-US.json"));
            var germanKeys = ReadJsonKeys(Path.Combine("src", "localization", "resources", $"{resourceName}.de-DE.json"));

            germanKeys.Should().BeEquivalentTo(englishKeys, $"{resourceName} resources should keep en-US/de-DE parity");
        }
    }

    [Fact]
    public void WebFrontendRoutes_Should_KeepCustomerFacingCanonicalSurfacesWired()
    {
        var routeFiles = new[]
        {
            Path.Combine("src", "app", "page.tsx"),
            Path.Combine("src", "app", "catalog", "page.tsx"),
            Path.Combine("src", "app", "catalog", "[slug]", "page.tsx"),
            Path.Combine("src", "app", "page", "[slug]", "page.tsx"),
            Path.Combine("src", "app", "cms", "page.tsx"),
            Path.Combine("src", "app", "cms", "[slug]", "page.tsx"),
            Path.Combine("src", "app", "help", "page.tsx"),
            Path.Combine("src", "app", "account", "page.tsx"),
            Path.Combine("src", "app", "cart", "page.tsx"),
            Path.Combine("src", "app", "checkout", "page.tsx"),
            Path.Combine("src", "app", "checkout", "orders", "[orderId]", "confirmation", "page.tsx"),
            Path.Combine("src", "app", "orders", "page.tsx"),
            Path.Combine("src", "app", "invoices", "page.tsx"),
            Path.Combine("src", "app", "loyalty", "page.tsx")
        };

        foreach (var routeFile in routeFiles)
        {
            File.Exists(ResolveRepositoryPath("src", "Darwin.Web", routeFile))
                .Should()
                .BeTrue($"customer-facing route should exist: {routeFile}");
        }

        var cmsPageRouteSource = ReadWebFrontendFile(Path.Combine("src", "app", "page", "[slug]", "page.tsx"));
        cmsPageRouteSource.Should().Contain("getCmsDetailPageContext");
        cmsPageRouteSource.Should().Contain("getCmsSeoMetadata");
        cmsPageRouteSource.Should().Contain("buildCmsPagePath");
        cmsPageRouteSource.Should().Contain("notFound()");
    }

    [Fact]
    public void WebFrontendShell_Should_UseTemplatesLogoAndConfigurableFooterColumns()
    {
        var shellSource = ReadWebFrontendFile(Path.Combine("src", "components", "shell", "site-shell.tsx"));
        var headerSource = ReadWebFrontendFile(Path.Combine("src", "components", "shell", "site-header-template.tsx"));
        var footerSource = ReadWebFrontendFile(Path.Combine("src", "components", "shell", "site-footer-template.tsx"));
        var shellModelSource = ReadWebFrontendFile(Path.Combine("src", "features", "shell", "get-shell-model.ts"));

        shellSource.Should().Contain("SiteHeader");
        shellSource.Should().Contain("SiteFooter");
        shellSource.Should().Contain("columnCount={model.footerColumnCount}");

        headerSource.Should().Contain("DarwinJustLogo.png");
        headerSource.Should().Contain("brandName");
        headerSource.Should().NotContain("shellTagline");
        headerSource.Should().NotContain("Web storefront");

        footerSource.Should().Contain("resolvedColumnCount");
        footerSource.Should().Contain("Math.min(Math.max(columnCount, 2), 6)");
        footerSource.Should().Contain("gridTemplateColumns");

        shellModelSource.Should().Contain("footerColumnCount");
        shellModelSource.Should().Contain("footerGroups");
    }

    [Fact]
    public void WebFrontendCmsAndMediaRendering_Should_KeepPublicPageContentCustomerFacing()
    {
        var detailSource = ReadWebFrontendFile(Path.Combine("src", "components", "cms", "help-page-detail.tsx"));
        var htmlFragmentSource = ReadWebFrontendFile(Path.Combine("src", "lib", "html-fragment.ts"));
        var webApiUrlSource = ReadWebFrontendFile(Path.Combine("src", "lib", "webapi-url.ts"));
        var entityPathsSource = ReadWebFrontendFile(Path.Combine("src", "lib", "entity-paths.ts"));

        detailSource.Should().Contain("sanitizeHtmlFragment");
        detailSource.Should().Contain("resolveRelativeHtmlMediaUrls");
        detailSource.Should().Contain("toWebApiUrl");
        detailSource.Should().NotContain("Meta description");
        detailSource.Should().NotContain("ProviderCallback");
        detailSource.Should().NotContain("TaxCompliance");

        htmlFragmentSource.Should().Contain("resolveRelativeHtmlMediaUrls");
        webApiUrlSource.Should().Contain("getSiteRuntimeConfig");
        webApiUrlSource.Should().Contain("webApiBaseUrl");
        entityPathsSource.Should().Contain("buildCmsPagePath");
        entityPathsSource.Should().Contain("/page/");
    }

    [Fact]
    public void WebFrontendPublicApiAndMemberBoundaries_Should_KeepStorefrontSeparateFromOperatorDiagnostics()
    {
        var publicCatalogSource = ReadWebFrontendFile(Path.Combine("src", "features", "catalog", "api", "public-catalog.ts"));
        var publicCmsSource = ReadWebFrontendFile(Path.Combine("src", "features", "cms", "api", "public-cms.ts"));
        var publicCheckoutSource = ReadWebFrontendFile(Path.Combine("src", "features", "checkout", "api", "public-checkout.ts"));
        var memberPortalSource = ReadWebFrontendFile(Path.Combine("src", "features", "member-portal", "api", "member-portal.ts"));
        var memberSessionSource = ReadWebFrontendFile(Path.Combine("src", "features", "member-session", "server.ts"));

        publicCatalogSource.Should().Contain("public/catalog");
        publicCmsSource.Should().Contain("public/cms");
        publicCheckoutSource.Should().Contain("public/checkout");
        memberPortalSource.Should().Contain("member/");
        memberSessionSource.Should().Contain("server-only");

        var combined = string.Join(
            Environment.NewLine,
            publicCatalogSource,
            publicCmsSource,
            publicCheckoutSource,
            memberPortalSource);
        combined.Should().NotContain("/admin/");
        combined.Should().NotContain("ProviderCallback");
        combined.Should().NotContain("BusinessCommunications");
    }

    private static ISet<string> ReadJsonKeys(string relativePath)
    {
        using var document = JsonDocument.Parse(ReadWebFrontendFile(relativePath));
        document.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
        return document.RootElement.EnumerateObject()
            .Select(static property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
    }
}
