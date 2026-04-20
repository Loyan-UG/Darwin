using FluentAssertions;

namespace Darwin.Tests.Unit.Security;

public sealed class SecurityAndPerformanceWebFrontendSourceTests : SecurityAndPerformanceSourceTestBase
{

    [Fact]
    public void WebFrontendPackageAndLocalizationResources_Should_KeepRuntimeAndBilingualBaselineWired()
    {
        var packageSource = ReadWebFrontendFile("package.json");
        var tsconfigSource = ReadWebFrontendFile("tsconfig.json");
        var sharedGermanSource = ReadWebFrontendFile(Path.Combine("src", "localization", "resources", "shared.de-DE.json"));
        var sharedEnglishSource = ReadWebFrontendFile(Path.Combine("src", "localization", "resources", "shared.en-US.json"));

        packageSource.Should().Contain("\"name\": \"darwin.web\"");
        packageSource.Should().Contain("\"version\": \"0.1.0\"");
        packageSource.Should().Contain("\"private\": true");
        packageSource.Should().Contain("\"dev\": \"next dev\"");
        packageSource.Should().Contain("\"build\": \"next build\"");
        packageSource.Should().Contain("\"start\": \"next start\"");
        packageSource.Should().Contain("\"lint\": \"eslint\"");
        packageSource.Should().Contain("\"test\": \"tsx --test src/**/*.test.ts\"");
        packageSource.Should().Contain("\"next\": \"16.2.1\"");
        packageSource.Should().Contain("\"react\": \"19.2.4\"");
        packageSource.Should().Contain("\"react-dom\": \"19.2.4\"");
        packageSource.Should().Contain("\"qrcode\": \"^1.5.4\"");
        packageSource.Should().Contain("\"undici\": \"^7.24.7\"");
        packageSource.Should().Contain("\"eslint-config-next\": \"16.2.1\"");
        packageSource.Should().Contain("\"tailwindcss\": \"^4\"");
        packageSource.Should().Contain("\"tsx\": \"^4.21.0\"");
        packageSource.Should().Contain("\"typescript\": \"^5\"");

        tsconfigSource.Should().Contain("\"target\": \"ES2017\"");
        tsconfigSource.Should().Contain("\"lib\": [\"dom\", \"dom.iterable\", \"esnext\"]");
        tsconfigSource.Should().Contain("\"allowJs\": true");
        tsconfigSource.Should().Contain("\"skipLibCheck\": true");
        tsconfigSource.Should().Contain("\"strict\": true");
        tsconfigSource.Should().Contain("\"noEmit\": true");
        tsconfigSource.Should().Contain("\"esModuleInterop\": true");
        tsconfigSource.Should().Contain("\"module\": \"esnext\"");
        tsconfigSource.Should().Contain("\"moduleResolution\": \"bundler\"");
        tsconfigSource.Should().Contain("\"resolveJsonModule\": true");
        tsconfigSource.Should().Contain("\"isolatedModules\": true");
        tsconfigSource.Should().Contain("\"jsx\": \"react-jsx\"");
        tsconfigSource.Should().Contain("\"incremental\": true");
        tsconfigSource.Should().Contain("\"name\": \"next\"");
        tsconfigSource.Should().Contain("\"@/*\": [\"./src/*\"]");
        tsconfigSource.Should().Contain("\"next-env.d.ts\"");
        tsconfigSource.Should().Contain("\"**/*.ts\"");
        tsconfigSource.Should().Contain("\"**/*.tsx\"");
        tsconfigSource.Should().Contain("\".next/types/**/*.ts\"");
        tsconfigSource.Should().Contain("\".next/dev/types/**/*.ts\"");

        sharedGermanSource.Should().Contain("\"siteTitle\": \"Darwin Storefront\"");
        sharedGermanSource.Should().Contain("\"cmsBreadcrumbHome\": \"Startseite\"");
        sharedGermanSource.Should().Contain("\"cmsPageNotFoundTitle\": \"Seite nicht gefunden\"");
        sharedGermanSource.Should().Contain("\"publicApiHttpErrorMessage\": \"Ein Teil der Storefront-Inhalte konnte gerade nicht geladen werden.\"");
        sharedGermanSource.Should().Contain("\"cmsOpenIndexCta\": \"CMS-Index oeffnen\"");
        sharedGermanSource.Should().Contain("\"cmsFollowUpCatalogCta\": \"Katalog browsen\"");
        sharedGermanSource.Should().Contain("\"cmsVisibleSearchLabel\": \"CMS-Suche\"");
        sharedGermanSource.Should().Contain("\"cmsVisibleSortTitleAscendingOption\": \"Titel A-Z\"");
        sharedGermanSource.Should().Contain("\"cmsIndexTitle\": \"Veroeffentlichte Inhaltsseiten aus Darwin.WebApi\"");

        sharedEnglishSource.Should().Contain("\"siteTitle\": \"Darwin Storefront\"");
        sharedEnglishSource.Should().Contain("\"cmsBreadcrumbHome\": \"Home\"");
        sharedEnglishSource.Should().Contain("\"cmsPageNotFoundTitle\": \"Page not found\"");
        sharedEnglishSource.Should().Contain("\"publicApiHttpErrorMessage\": \"Part of the storefront content could not be loaded right now.\"");
        sharedEnglishSource.Should().Contain("\"cmsOpenIndexCta\": \"Open CMS index\"");
        sharedEnglishSource.Should().Contain("\"cmsFollowUpCatalogCta\": \"Browse catalog\"");
        sharedEnglishSource.Should().Contain("\"cmsVisibleSearchLabel\": \"CMS search\"");
        sharedEnglishSource.Should().Contain("\"cmsVisibleSortTitleAscendingOption\": \"Title A-Z\"");
        sharedEnglishSource.Should().Contain("\"cmsIndexTitle\": \"Published content pages from Darwin.WebApi\"");
    }


    [Fact]
    public void WebFrontendRouteLocalizationResources_Should_KeepHomeCatalogCommerceMemberAndShellBilingualBaselineWired()
    {
        var homeGermanSource = NormalizeJsonKeyValueSpacing(ReadWebFrontendFile(Path.Combine("src", "localization", "resources", "home.de-DE.json")));
        var homeEnglishSource = NormalizeJsonKeyValueSpacing(ReadWebFrontendFile(Path.Combine("src", "localization", "resources", "home.en-US.json")));
        var catalogGermanSource = NormalizeJsonKeyValueSpacing(ReadWebFrontendFile(Path.Combine("src", "localization", "resources", "catalog.de-DE.json")));
        var catalogEnglishSource = NormalizeJsonKeyValueSpacing(ReadWebFrontendFile(Path.Combine("src", "localization", "resources", "catalog.en-US.json")));
        var commerceGermanSource = NormalizeJsonKeyValueSpacing(ReadWebFrontendFile(Path.Combine("src", "localization", "resources", "commerce.de-DE.json")));
        var commerceEnglishSource = NormalizeJsonKeyValueSpacing(ReadWebFrontendFile(Path.Combine("src", "localization", "resources", "commerce.en-US.json")));
        var memberGermanSource = NormalizeJsonKeyValueSpacing(ReadWebFrontendFile(Path.Combine("src", "localization", "resources", "member.de-DE.json")));
        var memberEnglishSource = NormalizeJsonKeyValueSpacing(ReadWebFrontendFile(Path.Combine("src", "localization", "resources", "member.en-US.json")));
        var shellGermanSource = NormalizeJsonKeyValueSpacing(ReadWebFrontendFile(Path.Combine("src", "localization", "resources", "shell.de-DE.json")));
        var shellEnglishSource = NormalizeJsonKeyValueSpacing(ReadWebFrontendFile(Path.Combine("src", "localization", "resources", "shell.en-US.json")));

        homeGermanSource.Should().Contain("\"heroEyebrow\": \"Darwin.Web\"");
        homeGermanSource.Should().Contain("\"heroTitle\": \"Die Storefront-Komposition ist jetzt real, waehrend die Startseite modular bleibt.\"");
        homeGermanSource.Should().Contain("\"browseCatalogCta\": \"Katalog oeffnen\"");
        homeGermanSource.Should().Contain("\"openCheckoutCta\": \"Checkout oeffnen\"");
        homeGermanSource.Should().Contain("\"metricsTitle\": \"Die aktuelle oeffentliche Auslieferung ist messbar und keine versteckte Starter-Shell mehr.\"");
        homeGermanSource.Should().Contain("\"homeCompositionTitle\": \"Home soll die aktuelle Journey zeigen und nicht nur Einstiegsabschnitte.\"");

        homeEnglishSource.Should().Contain("\"heroEyebrow\": \"Darwin.Web\"");
        homeEnglishSource.Should().Contain("\"heroTitle\": \"Storefront composition is now real, while the home page stays modular.\"");
        homeEnglishSource.Should().Contain("\"browseCatalogCta\": \"Browse catalog\"");
        homeEnglishSource.Should().Contain("\"openCheckoutCta\": \"Open checkout\"");
        homeEnglishSource.Should().Contain("\"metricsTitle\": \"Current public delivery is measurable, not a hidden starter shell.\"");
        homeEnglishSource.Should().Contain("\"homeCompositionTitle\": \"Home should expose the current journey, not only entry sections.\"");

        catalogGermanSource.Should().Contain("\"catalogMetaTitle\": \"Katalog\"");
        catalogGermanSource.Should().Contain("\"catalogMetaDescription\": \"Veroeffentlichte Kategorien und Produkte aus Darwin.WebApi.\"");
        catalogGermanSource.Should().Contain("\"degradedTitle\": \"Storefront-Daten laufen im degradierten Modus.\"");
        catalogGermanSource.Should().Contain("\"resultSummaryTitle\": \"Aktuelles Ergebnisfenster\"");
        catalogGermanSource.Should().Contain("\"catalogCompositionJourneyTitle\": \"Katalog-Journey\"");
        catalogGermanSource.Should().Contain("\"offerWindowTitle\": \"Angebotsfokus\"");
        catalogGermanSource.Should().Contain("\"catalogReadinessTitle\": \"Sortimentsbereitschaft\"");
        catalogGermanSource.Should().Contain("\"catalogReviewTitle\": \"Review-Aktionszentrum\"");

        catalogEnglishSource.Should().Contain("\"catalogMetaTitle\": \"Catalog\"");
        catalogEnglishSource.Should().Contain("\"catalogMetaDescription\": \"Published categories and products delivered from Darwin.WebApi.\"");
        catalogEnglishSource.Should().Contain("\"degradedTitle\": \"Storefront data is running in degraded mode.\"");
        catalogEnglishSource.Should().Contain("\"resultSummaryTitle\": \"Current result window\"");
        catalogEnglishSource.Should().Contain("\"catalogCompositionJourneyTitle\": \"Catalog journey\"");
        catalogEnglishSource.Should().Contain("\"offerWindowTitle\": \"Offer focus\"");
        catalogEnglishSource.Should().Contain("\"catalogReadinessTitle\": \"Assortment readiness\"");
        catalogEnglishSource.Should().Contain("\"catalogReviewTitle\": \"Review action center\"");

        commerceGermanSource.Should().Contain("\"cartMetaTitle\": \"Warenkorb\"");
        commerceGermanSource.Should().Contain("\"checkoutMetaTitle\": \"Checkout\"");
        commerceGermanSource.Should().Contain("\"confirmationMetaTitle\": \"Bestellbestaetigung\"");
        commerceGermanSource.Should().Contain("\"cartHeroTitle\": \"Der Storefront-Warenkorb ist jetzt ein echter oeffentlicher Commerce-Slice\"");
        commerceGermanSource.Should().Contain("\"checkoutHeroTitle\": \"Der Checkout laeuft jetzt auf den Live-Storefront-Contracts fuer Intent und Bestellung\"");
        commerceGermanSource.Should().Contain("\"confirmationCompositionJourneyTitle\": \"After-Purchase-Journey\"");
        commerceGermanSource.Should().Contain("\"mockCheckoutEyebrow\": \"Mock Hosted Checkout\"");

        commerceEnglishSource.Should().Contain("\"cartMetaTitle\": \"Cart\"");
        commerceEnglishSource.Should().Contain("\"checkoutMetaTitle\": \"Checkout\"");
        commerceEnglishSource.Should().Contain("\"confirmationMetaTitle\": \"Order confirmation\"");
        commerceEnglishSource.Should().Contain("\"cartHeroTitle\": \"Storefront cart is now a real public commerce slice\"");
        commerceEnglishSource.Should().Contain("\"checkoutHeroTitle\": \"Checkout now runs on the live storefront intent and order contracts\"");
        commerceEnglishSource.Should().Contain("\"confirmationCompositionJourneyTitle\": \"After-purchase journey\"");
        commerceEnglishSource.Should().Contain("\"mockCheckoutEyebrow\": \"Mock hosted checkout\"");

        memberGermanSource.Should().Contain("\"accountMetaTitle\": \"Konto\"");
        memberGermanSource.Should().Contain("\"ordersMetaTitle\": \"Bestellungen\"");
        memberGermanSource.Should().Contain("\"invoicesMetaTitle\": \"Rechnungen\"");
        memberGermanSource.Should().Contain("\"loyaltyMetaTitle\": \"Loyalty\"");
        memberGermanSource.Should().Contain("\"memberAuthRequiredRouteSummaryTitle\": \"Zusammenfassung der geschuetzten Route\"");
        memberGermanSource.Should().Contain("\"accountHubTitle\": \"Oeffentliche Self-Service-Flows sind jetzt von der spaeteren Member-Session getrennt.\"");
        memberGermanSource.Should().Contain("\"ordersTitle\": \"Der Bestellverlauf liest jetzt aus der authentifizierten Member-Portal-Oberflaeche\"");
        memberGermanSource.Should().Contain("\"invoicesTitle\": \"Der Rechnungsverlauf liest jetzt aus der authentifizierten Member-Billing-Oberflaeche\"");

        memberEnglishSource.Should().Contain("\"accountMetaTitle\": \"Account\"");
        memberEnglishSource.Should().Contain("\"ordersMetaTitle\": \"Orders\"");
        memberEnglishSource.Should().Contain("\"invoicesMetaTitle\": \"Invoices\"");
        memberEnglishSource.Should().Contain("\"loyaltyMetaTitle\": \"Loyalty\"");
        memberEnglishSource.Should().Contain("\"memberAuthRequiredRouteSummaryTitle\": \"Protected route summary\"");
        memberEnglishSource.Should().Contain("\"accountHubTitle\": \"Public self-service flows are now split from the future member session.\"");
        memberEnglishSource.Should().Contain("\"ordersTitle\": \"Order history now reads from the authenticated member portal surface\"");
        memberEnglishSource.Should().Contain("\"invoicesTitle\": \"Invoice history now reads from the authenticated member billing surface\"");

        shellGermanSource.Should().Contain("\"shellTagline\": \"Web storefront\"");
        shellGermanSource.Should().Contain("\"footerTitle\": \"Grundlage fuer den oeffentlichen Storefront und das Member-Portal\"");
        shellGermanSource.Should().Contain("\"footerNavigationTitle\": \"Service\"");
        shellGermanSource.Should().Contain("\"label\": \"Warenkorb\"");
        shellGermanSource.Should().Contain("\"label\": \"Kasse\"");
        shellGermanSource.Should().Contain("\"label\": \"Mock-Kasse\"");

        shellEnglishSource.Should().Contain("\"shellTagline\": \"Web storefront\"");
        shellEnglishSource.Should().Contain("\"footerTitle\": \"Public storefront and member portal foundation\"");
        shellEnglishSource.Should().Contain("\"footerNavigationTitle\": \"Service\"");
        shellEnglishSource.Should().Contain("\"label\": \"Cart\"");
        shellEnglishSource.Should().Contain("\"label\": \"Checkout\"");
        shellEnglishSource.Should().Contain("\"label\": \"Mock checkout\"");
    }


    [Fact]
    public void WebFrontendRuntimeConfigFiles_Should_KeepMiddlewareEnvAndToolingBaselineWired()
    {
        var middlewareSource = ReadWebFrontendFile("middleware.ts");
        var nextConfigSource = ReadWebFrontendFile("next.config.ts");
        var eslintSource = ReadWebFrontendFile("eslint.config.mjs");
        var postCssSource = ReadWebFrontendFile("postcss.config.mjs");
        var envExampleSource = ReadWebFrontendFile(".env.example");

        middlewareSource.Should().Contain("REQUEST_CULTURE_HEADER");
        middlewareSource.Should().Contain("getSiteRuntimeConfig()");
        middlewareSource.Should().Contain("const searchParamCulture = request.nextUrl.searchParams.get(\"culture\")");
        middlewareSource.Should().Contain("const cookieCulture = request.cookies.get(runtimeConfig.cultureCookieName)?.value;");
        middlewareSource.Should().Contain("stripCulturePrefix(request.nextUrl.pathname)");
        middlewareSource.Should().Contain("runtimeConfig.supportedCultures.includes(searchParamCulture)");
        middlewareSource.Should().Contain("runtimeConfig.supportedCultures.includes(cookieCulture)");
        middlewareSource.Should().Contain("runtimeConfig.defaultCulture");
        middlewareSource.Should().Contain("buildLocalizedPath(pathnameContext.pathname, validSearchCulture)");
        middlewareSource.Should().Contain("NextResponse.redirect(redirectUrl)");
        middlewareSource.Should().Contain("NextResponse.rewrite(rewrittenUrl");
        middlewareSource.Should().Contain("NextResponse.next({");
        middlewareSource.Should().Contain("response.cookies.set(runtimeConfig.cultureCookieName, effectiveCulture");
        middlewareSource.Should().Contain("matcher: [\"/((?!api|_next/static|_next/image|favicon.ico).*)\"]");

        nextConfigSource.Should().Contain("function shouldAllowInsecureLocalTls()");
        nextConfigSource.Should().Contain("process.env.DARWIN_WEB_ALLOW_INSECURE_WEBAPI_TLS");
        nextConfigSource.Should().Contain("process.env.DARWIN_WEBAPI_BASE_URL ?? \"http://localhost:5134\"");
        nextConfigSource.Should().Contain("url.hostname === \"localhost\"");
        nextConfigSource.Should().Contain("url.hostname === \"127.0.0.1\"");
        nextConfigSource.Should().Contain("url.hostname === \"::1\"");
        nextConfigSource.Should().Contain("process.env.NODE_ENV !== \"production\" && isLocalHost");
        nextConfigSource.Should().Contain("process.env.NODE_TLS_REJECT_UNAUTHORIZED = \"0\";");
        nextConfigSource.Should().Contain("const nextConfig: NextConfig = {");
        nextConfigSource.Should().Contain("export default nextConfig;");

        eslintSource.Should().Contain("defineConfig");
        eslintSource.Should().Contain("globalIgnores");
        eslintSource.Should().Contain("eslint-config-next/core-web-vitals");
        eslintSource.Should().Contain("eslint-config-next/typescript");
        eslintSource.Should().Contain("\".next/**\"");
        eslintSource.Should().Contain("\"out/**\"");
        eslintSource.Should().Contain("\"build/**\"");
        eslintSource.Should().Contain("\"next-env.d.ts\"");
        eslintSource.Should().Contain("export default eslintConfig;");

        postCssSource.Should().Contain("const config = {");
        postCssSource.Should().Contain("\"@tailwindcss/postcss\": {}");
        postCssSource.Should().Contain("export default config;");

        envExampleSource.Should().Contain("DARWIN_WEBAPI_BASE_URL=http://localhost:5134");
        envExampleSource.Should().Contain("DARWIN_WEB_SITE_URL=http://localhost:3000");
        envExampleSource.Should().Contain("DARWIN_WEB_MAIN_MENU_NAME=main-navigation");
        envExampleSource.Should().Contain("DARWIN_WEB_THEME=grocer");
        envExampleSource.Should().Contain("DARWIN_WEB_CULTURE=de-DE");
        envExampleSource.Should().Contain("DARWIN_WEB_SUPPORTED_CULTURES=de-DE,en-US");
        envExampleSource.Should().Contain("DARWIN_WEB_CULTURE_COOKIE_NAME=darwin-web-culture");
    }


    [Fact]
    public void WebFrontendRootAppAndSeoFiles_Should_KeepShellMetadataAndRuntimeBridgeWired()
    {
        var layoutSource = ReadWebFrontendFile(Path.Combine("src", "app", "layout.tsx"));
        var homePageSource = ReadWebFrontendFile(Path.Combine("src", "app", "page.tsx"));
        var robotsSource = ReadWebFrontendFile(Path.Combine("src", "app", "robots.ts"));
        var sitemapSource = ReadWebFrontendFile(Path.Combine("src", "app", "sitemap.ts"));
        var runtimeConfigSource = ReadWebFrontendFile(Path.Combine("src", "lib", "site-runtime-config.ts"));

        layoutSource.Should().Contain("import type { Metadata } from \"next\";");
        layoutSource.Should().Contain("SiteShell");
        layoutSource.Should().Contain("getShellModel");
        layoutSource.Should().Contain("getSharedResource");
        layoutSource.Should().Contain("getRequestCulture");
        layoutSource.Should().Contain("getSiteMetadataBase()");
        layoutSource.Should().Contain("getSiteRuntimeConfig()");
        layoutSource.Should().Contain("title: {");
        layoutSource.Should().Contain("template: `%s | ${shared.siteTitle}`");
        layoutSource.Should().Contain("description: shared.siteDescription");
        layoutSource.Should().Contain("lang={shellModel.culture}");
        layoutSource.Should().Contain("data-theme={runtimeConfig.theme}");
        layoutSource.Should().Contain("suppressHydrationWarning");
        layoutSource.Should().Contain("<SiteShell model={shellModel}>{children}</SiteShell>");

        homePageSource.Should().Contain("PageComposer");
        homePageSource.Should().Contain("getHomePageView");
        homePageSource.Should().Contain("getHomeSeoMetadata");
        homePageSource.Should().Contain("getRequestCulture");
        homePageSource.Should().Contain("const { metadata } = await getHomeSeoMetadata(culture);");
        homePageSource.Should().Contain("const view = await getHomePageView(culture);");
        homePageSource.Should().Contain("return <PageComposer parts={view.parts} culture={view.culture} />;");

        robotsSource.Should().Contain("MetadataRoute.Robots");
        robotsSource.Should().Contain("getSiteRuntimeConfig()");
        robotsSource.Should().Contain("const { siteUrl } = getSiteRuntimeConfig();");
        robotsSource.Should().Contain("userAgent: \"*\"");
        robotsSource.Should().Contain("allow: \"/\"");
        robotsSource.Should().Contain("\"/checkout/orders/*/confirmation/finalize\"");
        robotsSource.Should().Contain("sitemap: `${siteUrl}/sitemap.xml`");

        sitemapSource.Should().Contain("MetadataRoute.Sitemap");
        sitemapSource.Should().Contain("getPublicSitemapContext");
        sitemapSource.Should().Contain("const { entries } = await getPublicSitemapContext();");
        sitemapSource.Should().Contain("return entries;");

        runtimeConfigSource.Should().Contain("availableThemes, type ThemeId");
        runtimeConfigSource.Should().Contain("type SiteRuntimeConfig = {");
        runtimeConfigSource.Should().Contain("webApiBaseUrl: string;");
        runtimeConfigSource.Should().Contain("siteUrl: string;");
        runtimeConfigSource.Should().Contain("mainMenuName: string;");
        runtimeConfigSource.Should().Contain("footerMenuName: string;");
        runtimeConfigSource.Should().Contain("allowInsecureWebApiTls: boolean;");
        runtimeConfigSource.Should().Contain("defaultCulture: string;");
        runtimeConfigSource.Should().Contain("supportedCultures: string[];");
        runtimeConfigSource.Should().Contain("cultureCookieName: string;");
        runtimeConfigSource.Should().Contain("return value.endsWith(\"/\") ? value.slice(0, -1) : value;");
        runtimeConfigSource.Should().Contain("const items = (value ?? \"de-DE,en-US\")");
        runtimeConfigSource.Should().Contain("return items.length > 0 ? Array.from(new Set(items)) : [\"de-DE\", \"en-US\"];");
        runtimeConfigSource.Should().Contain("const availableThemeIds = new Set<string>(availableThemes.map((theme) => theme.id));");
        runtimeConfigSource.Should().Contain("return availableThemes[0].id;");
        runtimeConfigSource.Should().Contain("process.env.DARWIN_WEB_ALLOW_INSECURE_WEBAPI_TLS === \"true\"");
        runtimeConfigSource.Should().Contain("process.env.DARWIN_WEB_ALLOW_INSECURE_WEBAPI_TLS === \"false\"");
        runtimeConfigSource.Should().Contain("process.env.DARWIN_WEB_CULTURE ?? \"de-DE\"");
        runtimeConfigSource.Should().Contain("process.env.DARWIN_WEB_SUPPORTED_CULTURES");
        runtimeConfigSource.Should().Contain("process.env.DARWIN_WEBAPI_BASE_URL ?? \"http://localhost:5134\"");
        runtimeConfigSource.Should().Contain("process.env.DARWIN_WEB_SITE_URL ?? \"http://localhost:3000\"");
        runtimeConfigSource.Should().Contain("process.env.DARWIN_WEB_MAIN_MENU_NAME ?? \"main-navigation\"");
        runtimeConfigSource.Should().Contain("process.env.DARWIN_WEB_FOOTER_MENU_NAME ?? \"Footer\"");
        runtimeConfigSource.Should().Contain("theme: parseTheme(process.env.DARWIN_WEB_THEME)");
        runtimeConfigSource.Should().Contain("defaultCulture: supportedCultures.includes(defaultCulture)");
        runtimeConfigSource.Should().Contain("process.env.DARWIN_WEB_CULTURE_COOKIE_NAME ?? \"darwin-web-culture\"");
    }


    [Fact]
    public void WebFrontendLocalizationAndShellPrimitives_Should_KeepCultureRoutingAndShellAssemblyWired()
    {
        var localizationSource = ReadWebFrontendFile(Path.Combine("src", "localization", "index.ts"));
        var requestCultureSource = ReadWebFrontendFile(Path.Combine("src", "lib", "request-culture.ts"));
        var localeRoutingSource = ReadWebFrontendFile(Path.Combine("src", "lib", "locale-routing.ts"));
        var shellModelSource = ReadWebFrontendFile(Path.Combine("src", "features", "shell", "get-shell-model.ts"));
        var shellNavigationSource = ReadWebFrontendFile(Path.Combine("src", "features", "shell", "navigation.ts"));

        localizationSource.Should().Contain("const QUERY_LOCALIZATION_PREFIX = \"i18n:\";");
        localizationSource.Should().Contain("const sharedBundles = {");
        localizationSource.Should().Contain("const shellBundles = {");
        localizationSource.Should().Contain("const catalogBundles = {");
        localizationSource.Should().Contain("const commerceBundles = {");
        localizationSource.Should().Contain("const memberBundles = {");
        localizationSource.Should().Contain("const homeBundles = {");
        localizationSource.Should().Contain("const bundleRegistries = [");
        localizationSource.Should().Contain("const language = culture.split(\"-\")[0]?.toLowerCase();");
        localizationSource.Should().Contain("const fallbackKey = Object.keys(bundles).find((key) =>");
        localizationSource.Should().Contain("return (fallbackKey ?? \"de-DE\") as keyof T;");
        localizationSource.Should().Contain("export function getSharedResource(culture: string)");
        localizationSource.Should().Contain("export function getShellResource(culture: string)");
        localizationSource.Should().Contain("export function getCatalogResource(culture: string)");
        localizationSource.Should().Contain("export function getCommerceResource(culture: string)");
        localizationSource.Should().Contain("export function getMemberResource(culture: string)");
        localizationSource.Should().Contain("export function getHomeResource(culture: string)");
        localizationSource.Should().Contain("template.replace(/\\{(\\w+)\\}/g");
        localizationSource.Should().Contain("return `${QUERY_LOCALIZATION_PREFIX}${key}`;");
        localizationSource.Should().Contain("if (!value?.startsWith(QUERY_LOCALIZATION_PREFIX))");
        localizationSource.Should().Contain("const key = value.slice(QUERY_LOCALIZATION_PREFIX.length) as keyof T;");
        localizationSource.Should().Contain("return value;");

        requestCultureSource.Should().Contain("import \"server-only\";");
        requestCultureSource.Should().Contain("cookies, headers");
        requestCultureSource.Should().Contain("REQUEST_CULTURE_HEADER");
        requestCultureSource.Should().Contain("getSiteRuntimeConfig()");
        requestCultureSource.Should().Contain("const normalized = value?.trim();");
        requestCultureSource.Should().Contain("runtimeConfig.supportedCultures.includes(normalized)");
        requestCultureSource.Should().Contain("const headerStore = await headers();");
        requestCultureSource.Should().Contain("const cookieStore = await cookies();");
        requestCultureSource.Should().Contain("headerStore.get(REQUEST_CULTURE_HEADER)");
        requestCultureSource.Should().Contain("cookieStore.get(runtimeConfig.cultureCookieName)?.value");
        requestCultureSource.Should().Contain("runtimeConfig.defaultCulture");
        requestCultureSource.Should().Contain("export function getSupportedCultures()");

        localeRoutingSource.Should().Contain("export const REQUEST_CULTURE_HEADER = \"x-darwin-request-culture\";");
        localeRoutingSource.Should().Contain("function normalizePathname(pathname: string)");
        localeRoutingSource.Should().Contain("return normalized.endsWith(\"/\") ? normalized.slice(0, -1) : normalized;");
        localeRoutingSource.Should().Contain("function isExactOrChildPath(pathname: string, basePath: string)");
        localeRoutingSource.Should().Contain("function isExternalHref(href: string)");
        localeRoutingSource.Should().Contain("function splitHref(href: string)");
        localeRoutingSource.Should().Contain("export function sanitizeAppPath(value: string | undefined | null, fallback = \"/\")");
        localeRoutingSource.Should().Contain("if (!trimmed || isExternalHref(trimmed))");
        localeRoutingSource.Should().Contain("if (!pathname.startsWith(\"/\") || pathname.startsWith(\"//\"))");
        localeRoutingSource.Should().Contain("export function stripCulturePrefix(pathname: string)");
        localeRoutingSource.Should().Contain("const possibleCulture = segments[1];");
        localeRoutingSource.Should().Contain("runtimeConfig.supportedCultures.includes(possibleCulture)");
        localeRoutingSource.Should().Contain("export function isPublicLocalizedPath(pathname: string)");
        localeRoutingSource.Should().Contain("isExactOrChildPath(strippedPath, \"/catalog\")");
        localeRoutingSource.Should().Contain("isExactOrChildPath(strippedPath, \"/cms\")");
        localeRoutingSource.Should().Contain("export function buildLocalizedPath(pathname: string, culture: string)");
        localeRoutingSource.Should().Contain("if (culture === runtimeConfig.defaultCulture)");
        localeRoutingSource.Should().Contain("return normalizedPath === \"/\"");
        localeRoutingSource.Should().Contain("export function localizeHref(href: string, culture: string)");
        localeRoutingSource.Should().Contain("export function buildLocalizedAuthHref(");
        localeRoutingSource.Should().Contain("encodeURIComponent(safeReturnPath)");
        localeRoutingSource.Should().Contain("export function buildLocalizedQueryHref(");
        localeRoutingSource.Should().Contain("export function buildAppQueryPath(");
        localeRoutingSource.Should().Contain("export function appendAppQueryParam(");
        localeRoutingSource.Should().Contain("const separator = path.includes(\"?\") ? \"&\" : \"?\";");

        shellModelSource.Should().Contain("import \"server-only\";");
        shellModelSource.Should().Contain("getFallbackFooterGroups");
        shellModelSource.Should().Contain("getFallbackPrimaryNavigation");
        shellModelSource.Should().Contain("getUtilityLinks");
        shellModelSource.Should().Contain("getShellContext");
        shellModelSource.Should().Contain("resolveShellMenu");
        shellModelSource.Should().Contain("mapMenuItemsToLinks");
        shellModelSource.Should().Contain("formatResource, getSharedResource, getShellResource");
        shellModelSource.Should().Contain("createCachedObservedLoader");
        shellModelSource.Should().Contain("localizeHref, sanitizeAppPath, stripCulturePrefix");
        shellModelSource.Should().Contain("summarizeShellModelHealth");
        shellModelSource.Should().Contain("shellObservationContext");
        shellModelSource.Should().Contain("resolveTheme");
        shellModelSource.Should().Contain("const legacyCmsSlugs = new Set([");
        shellModelSource.Should().Contain("if (pathname === \"/home\")");
        shellModelSource.Should().Contain("if (pathname === \"/c\")");
        shellModelSource.Should().Contain("if (pathname.startsWith(\"/c/\"))");
        shellModelSource.Should().Contain("if (legacyCmsSlugs.has(slug))");
        shellModelSource.Should().Contain("const sanitized = sanitizeAppPath(trimmed, \"/\");");
        shellModelSource.Should().Contain("return toSafeHttpUrl(trimmed);");
        shellModelSource.Should().Contain("href: localizeHref(link.href, culture),");
        shellModelSource.Should().Contain("const key = `${group.title}:${link.href}`;");
        shellModelSource.Should().Contain("shared.menuMessages.emptyMenu");
        shellModelSource.Should().Contain("shared.menuMessages.notFound");
        shellModelSource.Should().Contain("export const getShellModel = createCachedObservedLoader({");
        shellModelSource.Should().Contain("area: \"shell-model\"");
        shellModelSource.Should().Contain("operation: \"load-shell-model\"");
        shellModelSource.Should().Contain("thresholdMs: 250");
        shellModelSource.Should().Contain("getShellContext(culture, runtimeConfig.mainMenuName)");
        shellModelSource.Should().Contain("getShellContext(culture, runtimeConfig.footerMenuName)");
        shellModelSource.Should().Contain("fallbackLinks: getFallbackPrimaryNavigation(culture)");
        shellModelSource.Should().Contain("footerNavigationTitle");
        shellModelSource.Should().Contain("utilityLinks: localizeShellLinks(getUtilityLinks(culture), culture)");
        shellModelSource.Should().Contain("supportedCultures: runtimeConfig.supportedCultures");

        shellNavigationSource.Should().Contain("getShellCopy");
        shellNavigationSource.Should().Contain("export function getFallbackPrimaryNavigation(culture: string): ShellLink[]");
        shellNavigationSource.Should().Contain("return getShellCopy(culture).fallbackPrimaryNavigation;");
        shellNavigationSource.Should().Contain("export function getUtilityLinks(culture: string): ShellLink[]");
        shellNavigationSource.Should().Contain("return getShellCopy(culture).utilityLinks;");
        shellNavigationSource.Should().Contain("export function getFallbackFooterGroups(culture: string): ShellLinkGroup[]");
        shellNavigationSource.Should().Contain("return getShellCopy(culture).footerGroups;");
    }


    [Fact]
    public void WebFrontendThemeAndCompositionPrimitives_Should_KeepThemeShellAndWebPartFloorWired()
    {
        var themeRegistrySource = ReadWebFrontendFile(Path.Combine("src", "themes", "registry.ts"));
        var pageComposerSource = ReadWebFrontendFile(Path.Combine("src", "web-parts", "page-composer.tsx"));
        var webPartTypesSource = ReadWebFrontendFile(Path.Combine("src", "web-parts", "types.ts"));
        var siteShellSource = ReadWebFrontendFile(Path.Combine("src", "components", "shell", "site-shell.tsx"));
        var shellTypesSource = ReadWebFrontendFile(Path.Combine("src", "features", "shell", "types.ts"));

        themeRegistrySource.Should().Contain("cartzillaGroceryTheme");
        themeRegistrySource.Should().Contain("harborEditorialTheme");
        themeRegistrySource.Should().Contain("noirBazaarTheme");
        themeRegistrySource.Should().Contain("solsticeMarketTheme");
        themeRegistrySource.Should().Contain("export const availableThemes = [");
        themeRegistrySource.Should().Contain("id: \"atelier\"");
        themeRegistrySource.Should().Contain("displayName: \"Atelier\"");
        themeRegistrySource.Should().Contain("displayName: \"Harbor\"");
        themeRegistrySource.Should().Contain("displayName: \"Noir\"");
        themeRegistrySource.Should().Contain("displayName: \"Solstice\"");
        themeRegistrySource.Should().Contain("title: \"Darwin Storefront\"");
        themeRegistrySource.Should().Contain("Darwin.Web public storefront and member portal foundation with a theme-isolated, CMS-aware shell.");
        themeRegistrySource.Should().Contain("export type ThemeId = (typeof availableThemes)[number][\"id\"];");
        themeRegistrySource.Should().Contain("export function resolveTheme(themeId: string)");
        themeRegistrySource.Should().Contain("availableThemes.find((theme) => theme.id === themeId) ?? availableThemes[0]");

        pageComposerSource.Should().Contain("type PageComposerProps = {");
        pageComposerSource.Should().Contain("parts: WebPagePart[];");
        pageComposerSource.Should().Contain("culture: string;");
        pageComposerSource.Should().Contain("const heroPart = parts.find((part) => part.kind === \"hero\");");
        pageComposerSource.Should().Contain(".filter((part) => part.kind !== \"hero\")");
        pageComposerSource.Should().Contain("const quickNavTitle = heroPart?.eyebrow ?? sectionLinks[0]?.label ?? \"\";");
        pageComposerSource.Should().Contain("const quickNavDescription = heroPart?.title ?? sectionLinks[0]?.title ?? \"\";");
        pageComposerSource.Should().Contain("if (part.kind === \"hero\")");
        pageComposerSource.Should().Contain("if (part.kind === \"stat-grid\")");
        pageComposerSource.Should().Contain("if (part.kind === \"card-grid\")");
        pageComposerSource.Should().Contain("if (part.kind === \"link-list\")");
        pageComposerSource.Should().Contain("if (part.kind === \"status-list\")");
        pageComposerSource.Should().Contain("if (part.kind === \"stage-flow\")");
        pageComposerSource.Should().Contain("if (part.kind === \"pair-panel\")");
        pageComposerSource.Should().Contain("if (part.kind === \"agenda-columns\")");
        pageComposerSource.Should().Contain("if (part.kind === \"route-map\")");
        pageComposerSource.Should().Contain("shared.statusWarningLabel");
        pageComposerSource.Should().Contain("shared.statusOkLabel");
        pageComposerSource.Should().Contain("shared.openLinkCta");
        pageComposerSource.Should().Contain("href={localizeHref(action.href, culture)}");
        pageComposerSource.Should().Contain("href={localizeHref(card.href, culture)}");
        pageComposerSource.Should().Contain("href={localizeHref(item.href, culture)}");
        pageComposerSource.Should().Contain("href={localizeHref(panel.href, culture)}");
        pageComposerSource.Should().Contain("href={localizeHref(column.href, culture)}");
        pageComposerSource.Should().Contain("href={localizeHref(item.primaryHref, culture)}");
        pageComposerSource.Should().Contain("item.secondaryHref && item.secondaryCtaLabel");

        webPartTypesSource.Should().Contain("export type WebPageAction = {");
        webPartTypesSource.Should().Contain("variant?: \"primary\" | \"secondary\";");
        webPartTypesSource.Should().Contain("export type WebPageCard = {");
        webPartTypesSource.Should().Contain("export type WebPageLinkItem = {");
        webPartTypesSource.Should().Contain("export type WebPageStatusItem = {");
        webPartTypesSource.Should().Contain("tone?: \"ok\" | \"warning\";");
        webPartTypesSource.Should().Contain("export type WebPageStageItem = {");
        webPartTypesSource.Should().Contain("export type WebPagePairPanel = {");
        webPartTypesSource.Should().Contain("export type WebPageAgendaColumn = {");
        webPartTypesSource.Should().Contain("bullets: string[];");
        webPartTypesSource.Should().Contain("export type WebPageRouteMapItem = {");
        webPartTypesSource.Should().Contain("secondaryHref?: string;");
        webPartTypesSource.Should().Contain("secondaryCtaLabel?: string;");
        webPartTypesSource.Should().Contain("export type WebPageMetric = {");
        webPartTypesSource.Should().Contain("kind: \"hero\";");
        webPartTypesSource.Should().Contain("kind: \"card-grid\";");
        webPartTypesSource.Should().Contain("kind: \"link-list\";");
        webPartTypesSource.Should().Contain("kind: \"stat-grid\";");
        webPartTypesSource.Should().Contain("kind: \"status-list\";");
        webPartTypesSource.Should().Contain("kind: \"stage-flow\";");
        webPartTypesSource.Should().Contain("kind: \"pair-panel\";");
        webPartTypesSource.Should().Contain("kind: \"agenda-columns\";");
        webPartTypesSource.Should().Contain("kind: \"route-map\";");
        webPartTypesSource.Should().Contain("kind: \"blank-state\";");
        webPartTypesSource.Should().Contain("export type WebPagePart =");

        siteShellSource.Should().Contain("import type { ReactNode } from \"react\";");
        siteShellSource.Should().Contain("SiteFooter");
        siteShellSource.Should().Contain("SiteHeader");
        siteShellSource.Should().Contain("type SiteShellProps = {");
        siteShellSource.Should().Contain("children: ReactNode;");
        siteShellSource.Should().Contain("model: ShellModel;");
        siteShellSource.Should().Contain("className=\"relative flex min-h-screen flex-col\"");
        siteShellSource.Should().Contain("SiteHeader");
        siteShellSource.Should().Contain("navigation={model.primaryNavigation}");
        siteShellSource.Should().Contain("utilityLinks={model.utilityLinks}");
        siteShellSource.Should().Contain("culture={model.culture}");
        siteShellSource.Should().Contain("supportedCultures={model.supportedCultures}");
        siteShellSource.Should().Contain("<main className=\"flex flex-1 flex-col\">{children}</main>");
        siteShellSource.Should().Contain("SiteFooter groups={model.footerGroups} culture={model.culture} />");

        shellTypesSource.Should().Contain("export type ShellLink = {");
        shellTypesSource.Should().Contain("label: string;");
        shellTypesSource.Should().Contain("href: string;");
        shellTypesSource.Should().Contain("export type ShellLinkGroup = {");
        shellTypesSource.Should().Contain("title: string;");
        shellTypesSource.Should().Contain("links: ShellLink[];");
        shellTypesSource.Should().Contain("export type ShellModel = {");
        shellTypesSource.Should().Contain("activeThemeName: string;");
        shellTypesSource.Should().Contain("culture: string;");
        shellTypesSource.Should().Contain("supportedCultures: string[];");
        shellTypesSource.Should().Contain("menuSource: \"cms\" | \"fallback\";");
        shellTypesSource.Should().Contain("\"empty-menu\"");
        shellTypesSource.Should().Contain("\"not-found\"");
        shellTypesSource.Should().Contain("\"network-error\"");
        shellTypesSource.Should().Contain("\"http-error\"");
        shellTypesSource.Should().Contain("\"invalid-payload\"");
        shellTypesSource.Should().Contain("menuMessage?: string;");
        shellTypesSource.Should().Contain("primaryNavigation: ShellLink[];");
        shellTypesSource.Should().Contain("utilityLinks: ShellLink[];");
        shellTypesSource.Should().Contain("footerGroups: ShellLinkGroup[];");
    }


    [Fact]
    public void WebFrontendSeoAndPublicApiPrimitives_Should_KeepMetadataUrlAndJsonPipelineWired()
    {
        var seoSource = ReadWebFrontendFile(Path.Combine("src", "lib", "seo.ts"));
        var sitemapSource = ReadWebFrontendFile(Path.Combine("src", "lib", "sitemap.ts"));
        var webApiUrlSource = ReadWebFrontendFile(Path.Combine("src", "lib", "webapi-url.ts"));
        var fetchPublicJsonSource = ReadWebFrontendFile(Path.Combine("src", "lib", "api", "fetch-public-json.ts"));
        var publicJsonRequestSource = ReadWebFrontendFile(Path.Combine("src", "lib", "api", "public-json-request.ts"));

        seoSource.Should().Contain("import type { Metadata } from \"next\";");
        seoSource.Should().Contain("canonicalizeLanguageAlternates");
        seoSource.Should().Contain("buildLocalizedPath, isPublicLocalizedPath");
        seoSource.Should().Contain("getSiteRuntimeConfig");
        seoSource.Should().Contain("toWebApiUrl");
        seoSource.Should().Contain("type SeoMetadataInput = {");
        seoSource.Should().Contain("allowLanguageAlternates?: boolean;");
        seoSource.Should().Contain("languageAlternates?: Record<string, string>;");
        seoSource.Should().Contain("function collapseWhitespace(value: string)");
        seoSource.Should().Contain("function stripHtml(value: string)");
        seoSource.Should().Contain("function truncateText(value: string, maxLength = 160)");
        seoSource.Should().Contain("export function getSiteMetadataBase()");
        seoSource.Should().Contain("const { siteUrl } = getSiteRuntimeConfig();");
        seoSource.Should().Contain("return new URL(siteUrl);");
        seoSource.Should().Contain("const localizedCanonicalPath = isPublicLocalizedPath(normalizedPath)");
        seoSource.Should().Contain("const resolvedImageUrl = imageUrl ? toWebApiUrl(imageUrl) : undefined;");
        seoSource.Should().Contain("\"x-default\":");
        seoSource.Should().Contain("runtimeConfig.supportedCultures.map((supportedCulture) => [");
        seoSource.Should().Contain("alternates: {");
        seoSource.Should().Contain("canonical: localizedCanonicalPath,");
        seoSource.Should().Contain("openGraph: {");
        seoSource.Should().Contain("twitter: {");
        seoSource.Should().Contain("robots: {");
        seoSource.Should().Contain("export function buildNoIndexMetadata(");
        seoSource.Should().Contain("noIndex: true,");
        seoSource.Should().Contain("export function deriveSeoDescription(");
        seoSource.Should().Contain("value.includes(\"<\")");
        seoSource.Should().Contain("return truncateText(normalized);");

        sitemapSource.Should().Contain("import \"server-only\";");
        sitemapSource.Should().Contain("MetadataRoute");
        sitemapSource.Should().Contain("getPublicSitemapContext");
        sitemapSource.Should().Contain("export async function buildPublicSitemapEntries(): Promise<MetadataRoute.Sitemap>");
        sitemapSource.Should().Contain("const { entries } = await getPublicSitemapContext();");
        sitemapSource.Should().Contain("return entries;");

        webApiUrlSource.Should().Contain("getSiteRuntimeConfig");
        webApiUrlSource.Should().Contain("export function toSafeHttpUrl(path: string)");
        webApiUrlSource.Should().Contain("!/^https?:\\/\\//i.test(path)");
        webApiUrlSource.Should().Contain("const url = new URL(path);");
        webApiUrlSource.Should().Contain("/^https?:$/i.test(url.protocol) ? url.toString() : \"\";");
        webApiUrlSource.Should().Contain("export function toWebApiUrl(path: string)");
        webApiUrlSource.Should().Contain("if (!path) {");
        webApiUrlSource.Should().Contain("if (/^https?:\\/\\//i.test(path))");
        webApiUrlSource.Should().Contain("if (!path.startsWith(\"/\") || path.startsWith(\"//\"))");
        webApiUrlSource.Should().Contain("const { webApiBaseUrl } = getSiteRuntimeConfig();");
        webApiUrlSource.Should().Contain("return new URL(path, `${webApiBaseUrl}/`).toString();");

        fetchPublicJsonSource.Should().Contain("import \"server-only\";");
        fetchPublicJsonSource.Should().Contain("cache } from \"react\"");
        fetchPublicJsonSource.Should().Contain("buildPublicJsonFetcher");
        fetchPublicJsonSource.Should().Contain("buildCachedPublicJsonArgs");
        fetchPublicJsonSource.Should().Contain("buildPublicJsonWebApiExecutionContext");
        fetchPublicJsonSource.Should().Contain("buildPostPublicJsonInit");
        fetchPublicJsonSource.Should().Contain("executePublicJsonRequest");
        fetchPublicJsonSource.Should().Contain("buildWebApiFetchInit");
        fetchPublicJsonSource.Should().Contain("type PublicApiFetchStatus =");
        fetchPublicJsonSource.Should().Contain("async function sendPublicJson<T>(");
        fetchPublicJsonSource.Should().Contain("const { webApiBaseUrl } = getSiteRuntimeConfig();");
        fetchPublicJsonSource.Should().Contain("const executionContext = buildPublicJsonWebApiExecutionContext(");
        fetchPublicJsonSource.Should().Contain("buildPublicJsonFetcher(fetch, buildWebApiFetchInit)");
        fetchPublicJsonSource.Should().Contain("const getCachedPublicJson = cache((path: string, key: string) =>");
        fetchPublicJsonSource.Should().Contain("export async function fetchPublicJson<T>(");
        fetchPublicJsonSource.Should().Contain("getCachedPublicJson(...buildCachedPublicJsonArgs(path, key))");
        fetchPublicJsonSource.Should().Contain("export async function postPublicJson<T>(");
        fetchPublicJsonSource.Should().Contain("buildPostPublicJsonInit(body)");

        publicJsonRequestSource.Should().Contain("createDiagnostics");
        publicJsonRequestSource.Should().Contain("getResponseDiagnostics");
        publicJsonRequestSource.Should().Contain("logApiFailure");
        publicJsonRequestSource.Should().Contain("withFailureDiagnostics");
        publicJsonRequestSource.Should().Contain("getPublicApiRequestPlan");
        publicJsonRequestSource.Should().Contain("normalizePublicApiCachePath");
        publicJsonRequestSource.Should().Contain("toLocalizedQueryMessage");
        publicJsonRequestSource.Should().Contain("export type PublicApiFailureStatus =");
        publicJsonRequestSource.Should().Contain("export type PublicApiResult<T> = {");
        publicJsonRequestSource.Should().Contain("export type PublicJsonExecutionContext = {");
        publicJsonRequestSource.Should().Contain("export type PublicJsonExecutionPlan = {");
        publicJsonRequestSource.Should().Contain("export type PublicJsonParsedResponse<T> = {");
        publicJsonRequestSource.Should().Contain("export function buildPublicJsonFetcher(");
        publicJsonRequestSource.Should().Contain("buildPublicJsonFetchRequestInit(");
        publicJsonRequestSource.Should().Contain("export function shouldIncludePublicJsonContentType(init?: RequestInit)");
        publicJsonRequestSource.Should().Contain("Accept: \"application/json\"");
        publicJsonRequestSource.Should().Contain("\"Content-Type\": \"application/json\"");
        publicJsonRequestSource.Should().Contain("return JSON.stringify(body);");
        publicJsonRequestSource.Should().Contain("buildPublicJsonWebApiRequestPlan(");
        publicJsonRequestSource.Should().Contain("buildPublicJsonWebApiExecutionContext(");
        publicJsonRequestSource.Should().Contain("export function buildPostPublicJsonInit(body: unknown): RequestInit");
        publicJsonRequestSource.Should().Contain("method: \"POST\"");
        publicJsonRequestSource.Should().Contain("export function getCachedPublicJsonKey(path: string)");
        publicJsonRequestSource.Should().Contain("normalizePublicApiCachePath(path)");
        publicJsonRequestSource.Should().Contain("return [getCachedPublicJsonKey(path), key] as const;");
        publicJsonRequestSource.Should().Contain("case \"not-found\":");
        publicJsonRequestSource.Should().Contain("return \"publicApiNotFoundMessage\";");
        publicJsonRequestSource.Should().Contain("return status !== \"not-found\";");
        publicJsonRequestSource.Should().Contain("data: null as T | null,");
        publicJsonRequestSource.Should().Contain("statusCode === 404");
        publicJsonRequestSource.Should().Contain("statusCode >= 400");
        publicJsonRequestSource.Should().Contain("buildPublicApiSuccessOutcome");
        publicJsonRequestSource.Should().Contain("buildPublicApiNetworkFailureOutcome");
        publicJsonRequestSource.Should().Contain("buildPublicJsonInvalidPayloadFailure");
        publicJsonRequestSource.Should().Contain("responseContext.response.json()");
        publicJsonRequestSource.Should().Contain("Boolean(parsedResponse.failureStatus)");
        publicJsonRequestSource.Should().Contain("const response = await fetcher(requestUrl, requestInit);");
        publicJsonRequestSource.Should().Contain("return buildPublicJsonExecutionNetworkFailureResult<T>(requestContext, error);");
    }


    [Fact]
    public void WebFrontendSessionCookieAndActionPrimitives_Should_KeepStatefulStorefrontAndMemberFlowsWired()
    {
        var memberSessionServerSource = ReadWebFrontendFile(Path.Combine("src", "features", "member-session", "server.ts"));
        var memberSessionCookiesSource = ReadWebFrontendFile(Path.Combine("src", "features", "member-session", "cookies.ts"));
        var cartCookiesSource = ReadWebFrontendFile(Path.Combine("src", "features", "cart", "cookies.ts"));
        var checkoutCookiesSource = ReadWebFrontendFile(Path.Combine("src", "features", "checkout", "cookies.ts"));
        var accountActionsSource = ReadWebFrontendFile(Path.Combine("src", "features", "account", "actions.ts"));
        var cartActionsSource = ReadWebFrontendFile(Path.Combine("src", "features", "cart", "actions.ts"));
        var checkoutActionsSource = ReadWebFrontendFile(Path.Combine("src", "features", "checkout", "actions.ts"));

        memberSessionServerSource.Should().Contain("import \"server-only\";");
        memberSessionServerSource.Should().Contain("clearMemberSession, getMemberAccessToken, getMemberRefreshToken, getMemberSession, writeMemberSession");
        memberSessionServerSource.Should().Contain("refreshMember");
        memberSessionServerSource.Should().Contain("parseUtcTimestamp");
        memberSessionServerSource.Should().Contain("function expiresSoon(expiresAtUtc: string)");
        memberSessionServerSource.Should().Contain("return value <= Date.now() + 60_000;");
        memberSessionServerSource.Should().Contain("export async function getFreshMemberAccessToken(forceRefresh = false)");
        memberSessionServerSource.Should().Contain("const [session, accessToken, refreshToken] = await Promise.all([");
        memberSessionServerSource.Should().Contain("if (!session || !accessToken || !refreshToken)");
        memberSessionServerSource.Should().Contain("if (!forceRefresh && !expiresSoon(session.accessTokenExpiresAtUtc))");
        memberSessionServerSource.Should().Contain("const refreshResult = await refreshMember({");
        memberSessionServerSource.Should().Contain("await clearMemberSession();");
        memberSessionServerSource.Should().Contain("await writeMemberSession({");
        memberSessionServerSource.Should().Contain("return refreshResult.data.accessToken;");

        memberSessionCookiesSource.Should().Contain("import \"server-only\";");
        memberSessionCookiesSource.Should().Contain("const ACCESS_TOKEN_COOKIE = \"darwin-member-access-token\";");
        memberSessionCookiesSource.Should().Contain("const REFRESH_TOKEN_COOKIE = \"darwin-member-refresh-token\";");
        memberSessionCookiesSource.Should().Contain("const SESSION_COOKIE = \"darwin-member-session\";");
        memberSessionCookiesSource.Should().Contain("function isMemberSession(value: unknown): value is MemberSession");
        memberSessionCookiesSource.Should().Contain("typeof session.userId === \"string\"");
        memberSessionCookiesSource.Should().Contain("session.email.includes(\"@\")");
        memberSessionCookiesSource.Should().Contain("isValidUtcTimestamp(session.accessTokenExpiresAtUtc)");
        memberSessionCookiesSource.Should().Contain("secure: process.env.NODE_ENV === \"production\"");
        memberSessionCookiesSource.Should().Contain("maxAge: 60 * 60 * 24 * 14");
        memberSessionCookiesSource.Should().Contain("const raw = cookieStore.get(SESSION_COOKIE)?.value;");
        memberSessionCookiesSource.Should().Contain("const parsed = JSON.parse(raw) as unknown;");
        memberSessionCookiesSource.Should().Contain("return isMemberSession(parsed) ? parsed : null;");
        memberSessionCookiesSource.Should().Contain("cookieStore.set(ACCESS_TOKEN_COOKIE, input.accessToken, options);");
        memberSessionCookiesSource.Should().Contain("cookieStore.set(REFRESH_TOKEN_COOKIE, input.refreshToken, options);");
        memberSessionCookiesSource.Should().Contain("cookieStore.set(SESSION_COOKIE, JSON.stringify(input.session), options);");
        memberSessionCookiesSource.Should().Contain("cookieStore.delete(ACCESS_TOKEN_COOKIE);");
        memberSessionCookiesSource.Should().Contain("cookieStore.delete(REFRESH_TOKEN_COOKIE);");
        memberSessionCookiesSource.Should().Contain("cookieStore.delete(SESSION_COOKIE);");

        cartCookiesSource.Should().Contain("import \"server-only\";");
        cartCookiesSource.Should().Contain("const ANONYMOUS_CART_COOKIE = \"darwin-storefront-anonymous-id\";");
        cartCookiesSource.Should().Contain("const CART_DISPLAY_COOKIE = \"darwin-storefront-cart-display\";");
        cartCookiesSource.Should().Contain("function isCartDisplaySnapshot(value: unknown): value is CartDisplaySnapshot");
        cartCookiesSource.Should().Contain("sanitizeAppPath(snapshot.href, \"/catalog\").startsWith(\"/\")");
        cartCookiesSource.Should().Contain("maxAge: 60 * 60 * 24 * 30");
        cartCookiesSource.Should().Contain("const existing = cookieStore.get(ANONYMOUS_CART_COOKIE)?.value;");
        cartCookiesSource.Should().Contain("const created = crypto.randomUUID();");
        cartCookiesSource.Should().Contain("return [] satisfies CartDisplaySnapshot[];");
        cartCookiesSource.Should().Contain("Array.isArray(parsed) ? parsed.filter(isCartDisplaySnapshot) : [];");
        cartCookiesSource.Should().Contain("JSON.stringify(snapshots.slice(0, 50))");
        cartCookiesSource.Should().Contain("...current.filter((item) => item.variantId !== snapshot.variantId)");
        cartCookiesSource.Should().Contain("activeVariantIds.includes(item.variantId)");
        cartCookiesSource.Should().Contain("cookieStore.delete(ANONYMOUS_CART_COOKIE);");
        cartCookiesSource.Should().Contain("cookieStore.delete(CART_DISPLAY_COOKIE);");

        checkoutCookiesSource.Should().Contain("import \"server-only\";");
        checkoutCookiesSource.Should().Contain("const STOREFRONT_PAYMENT_HANDOFF_COOKIE = \"darwin-storefront-payment-handoff\";");
        checkoutCookiesSource.Should().Contain("export type StorefrontPaymentHandoffState = {");
        checkoutCookiesSource.Should().Contain("orderId: string;");
        checkoutCookiesSource.Should().Contain("paymentId: string;");
        checkoutCookiesSource.Should().Contain("function isStorefrontPaymentHandoffState(");
        checkoutCookiesSource.Should().Contain("typeof state.orderId === \"string\"");
        checkoutCookiesSource.Should().Contain("typeof state.paymentId === \"string\"");
        checkoutCookiesSource.Should().Contain("maxAge: 60 * 30");
        checkoutCookiesSource.Should().Contain("const raw = cookieStore.get(STOREFRONT_PAYMENT_HANDOFF_COOKIE)?.value;");
        checkoutCookiesSource.Should().Contain("if (!isStorefrontPaymentHandoffState(parsed))");
        checkoutCookiesSource.Should().Contain("JSON.stringify(state)");
        checkoutCookiesSource.Should().Contain("cookieStore.delete(STOREFRONT_PAYMENT_HANDOFF_COOKIE);");

        accountActionsSource.Should().Contain("\"use server\";");
        accountActionsSource.Should().Contain("redirect } from \"next/navigation\"");
        accountActionsSource.Should().Contain("confirmMemberEmail");
        accountActionsSource.Should().Contain("registerMember");
        accountActionsSource.Should().Contain("requestMemberEmailConfirmation");
        accountActionsSource.Should().Contain("requestMemberPasswordReset");
        accountActionsSource.Should().Contain("resetMemberPassword");
        accountActionsSource.Should().Contain("readNormalizedEmail, readTrimmedFormText");
        accountActionsSource.Should().Contain("buildAppQueryPath, sanitizeAppPath");
        accountActionsSource.Should().Contain("toLocalizedQueryMessage");
        accountActionsSource.Should().Contain("function buildAccountFlowPath(");
        accountActionsSource.Should().Contain("...values,");
        accountActionsSource.Should().Contain("returnPath,");
        accountActionsSource.Should().Contain("export async function registerMemberAction(formData: FormData)");
        accountActionsSource.Should().Contain("registrationFieldsRequiredMessage");
        accountActionsSource.Should().Contain("registrationFailedMessage");
        accountActionsSource.Should().Contain("registerStatus: \"registered\"");
        accountActionsSource.Should().Contain("export async function requestEmailConfirmationAction(formData: FormData)");
        accountActionsSource.Should().Contain("activationEmailRequiredMessage");
        accountActionsSource.Should().Contain("activationRequestFailedMessage");
        accountActionsSource.Should().Contain("activationStatus: \"requested\"");
        accountActionsSource.Should().Contain("export async function confirmEmailAction(formData: FormData)");
        accountActionsSource.Should().Contain("activationEmailTokenRequiredMessage");
        accountActionsSource.Should().Contain("activationConfirmFailedMessage");
        accountActionsSource.Should().Contain("activationStatus: \"confirmed\"");
        accountActionsSource.Should().Contain("export async function requestPasswordResetAction(formData: FormData)");
        accountActionsSource.Should().Contain("passwordRequestEmailRequiredMessage");
        accountActionsSource.Should().Contain("passwordRequestFailedMessage");
        accountActionsSource.Should().Contain("passwordStatus: \"requested\"");
        accountActionsSource.Should().Contain("export async function resetPasswordAction(formData: FormData)");
        accountActionsSource.Should().Contain("passwordResetFieldsRequiredMessage");
        accountActionsSource.Should().Contain("passwordResetFailedMessage");
        accountActionsSource.Should().Contain("passwordStatus: \"reset\"");

        cartActionsSource.Should().Contain("\"use server\";");
        cartActionsSource.Should().Contain("revalidatePath");
        cartActionsSource.Should().Contain("redirect");
        cartActionsSource.Should().Contain("getOrCreateAnonymousCartId");
        cartActionsSource.Should().Contain("pruneCartDisplaySnapshots");
        cartActionsSource.Should().Contain("upsertCartDisplaySnapshot");
        cartActionsSource.Should().Contain("addItemToPublicCart");
        cartActionsSource.Should().Contain("applyPublicCartCoupon");
        cartActionsSource.Should().Contain("removePublicCartItem");
        cartActionsSource.Should().Contain("updatePublicCartItem");
        cartActionsSource.Should().Contain("normalizeCouponCode");
        cartActionsSource.Should().Contain("readQuantityFromFormData");
        cartActionsSource.Should().Contain("appendAppQueryParam, sanitizeAppPath");
        cartActionsSource.Should().Contain("function withCartFlash(path: string, key: string, value: string)");
        cartActionsSource.Should().Contain("for (const path of paths) {");
        cartActionsSource.Should().Contain("export async function addToCartAction(formData: FormData)");
        cartActionsSource.Should().Contain("cartInvalidRequestMessage");
        cartActionsSource.Should().Contain("const anonymousId = await getOrCreateAnonymousCartId();");
        cartActionsSource.Should().Contain("await upsertCartDisplaySnapshot({");
        cartActionsSource.Should().Contain("await pruneCartDisplaySnapshots(result.data.items.map((item) => item.variantId));");
        cartActionsSource.Should().Contain("revalidateStorefrontPaths([\"/cart\", \"/catalog\", returnPath]);");
        cartActionsSource.Should().Contain("redirect(withCartFlash(\"/cart\", \"cartStatus\", \"added\"));");
        cartActionsSource.Should().Contain("export async function updateCartQuantityAction(formData: FormData)");
        cartActionsSource.Should().Contain("cartUpdateInvalidMessage");
        cartActionsSource.Should().Contain("cartUpdateFailedMessage");
        cartActionsSource.Should().Contain("redirect(withCartFlash(\"/cart\", \"cartStatus\", \"updated\"));");
        cartActionsSource.Should().Contain("export async function removeCartItemAction(formData: FormData)");
        cartActionsSource.Should().Contain("cartRemoveInvalidMessage");
        cartActionsSource.Should().Contain("cartRemoveFailedMessage");
        cartActionsSource.Should().Contain("redirect(withCartFlash(\"/cart\", \"cartStatus\", \"removed\"));");
        cartActionsSource.Should().Contain("export async function applyCartCouponAction(formData: FormData)");
        cartActionsSource.Should().Contain("cartCouponMissingCartIdMessage");
        cartActionsSource.Should().Contain("cartCouponApplyFailedMessage");
        cartActionsSource.Should().Contain("couponCode ? \"coupon-applied\" : \"coupon-cleared\"");

        checkoutActionsSource.Should().Contain("\"use server\";");
        checkoutActionsSource.Should().Contain("revalidatePath");
        checkoutActionsSource.Should().Contain("redirect");
        checkoutActionsSource.Should().Contain("clearStorefrontCartState");
        checkoutActionsSource.Should().Contain("createPublicStorefrontPaymentIntent");
        checkoutActionsSource.Should().Contain("placePublicStorefrontOrder");
        checkoutActionsSource.Should().Contain("writeStorefrontPaymentHandoff");
        checkoutActionsSource.Should().Contain("buildCheckoutDraftSearch");
        checkoutActionsSource.Should().Contain("isCheckoutAddressComplete");
        checkoutActionsSource.Should().Contain("readNonNegativeIntegerFromFormData");
        checkoutActionsSource.Should().Contain("readCheckoutDraftFromFormData");
        checkoutActionsSource.Should().Contain("toCheckoutAddress");
        checkoutActionsSource.Should().Contain("buildAppQueryPath");
        checkoutActionsSource.Should().Contain("toLocalizedQueryMessage");
        checkoutActionsSource.Should().Contain("getRequestCulture");
        checkoutActionsSource.Should().Contain("function revalidateCheckoutPaths()");
        checkoutActionsSource.Should().Contain("revalidatePath(\"/cart\");");
        checkoutActionsSource.Should().Contain("revalidatePath(\"/checkout\");");
        checkoutActionsSource.Should().Contain("export async function placeStorefrontOrderAction(formData: FormData)");
        checkoutActionsSource.Should().Contain("checkoutInvalidOrderRequestMessage");
        checkoutActionsSource.Should().Contain("checkoutAddressIncompleteErrorMessage");
        checkoutActionsSource.Should().Contain("culture: await getRequestCulture(),");
        checkoutActionsSource.Should().Contain("checkoutPlaceOrderFailedMessage");
        checkoutActionsSource.Should().Contain("await clearStorefrontCartState();");
        checkoutActionsSource.Should().Contain("buildAppQueryPath(`/checkout/orders/${orderResult.data.orderId}/confirmation`, {");
        checkoutActionsSource.Should().Contain("checkoutStatus: \"order-placed\"");
        checkoutActionsSource.Should().Contain("export async function createStorefrontPaymentIntentAction(formData: FormData)");
        checkoutActionsSource.Should().Contain("checkoutMissingOrderIdentifierMessage");
        checkoutActionsSource.Should().Contain("checkoutHostedCheckoutStartFailedMessage");
        checkoutActionsSource.Should().Contain("await writeStorefrontPaymentHandoff({");
        checkoutActionsSource.Should().Contain("redirect(paymentResult.data.checkoutUrl);");
    }


    [Fact]
    public void WebFrontendFormStateHelpersAndTypes_Should_KeepAccountCartCheckoutAndSessionContractsWired()
    {
        var accountEntryViewSource = ReadWebFrontendFile(Path.Combine("src", "features", "account", "account-entry-view.ts"));
        var accountTypesSource = ReadWebFrontendFile(Path.Combine("src", "features", "account", "types.ts"));
        var storefrontShoppingSource = ReadWebFrontendFile(Path.Combine("src", "features", "cart", "storefront-shopping.ts"));
        var cartTypesSource = ReadWebFrontendFile(Path.Combine("src", "features", "cart", "types.ts"));
        var checkoutHelpersSource = ReadWebFrontendFile(Path.Combine("src", "features", "checkout", "helpers.ts"));
        var checkoutTypesSource = ReadWebFrontendFile(Path.Combine("src", "features", "checkout", "types.ts"));
        var memberSessionTypesSource = ReadWebFrontendFile(Path.Combine("src", "features", "member-session", "types.ts"));

        accountEntryViewSource.Should().Contain("import type { ComponentProps } from \"react\";");
        accountEntryViewSource.Should().Contain("type AccountHubProps = ComponentProps<typeof AccountHubPage>;");
        accountEntryViewSource.Should().Contain("type MemberDashboardProps = ComponentProps<typeof MemberDashboardPage>;");
        accountEntryViewSource.Should().Contain("createStorefrontContinuationWithCartAndLinkedProps");
        accountEntryViewSource.Should().Contain("createStorefrontContinuationWithCartProps");
        accountEntryViewSource.Should().Contain("sanitizeAppPath");
        accountEntryViewSource.Should().Contain("export type AccountEntryView =");
        accountEntryViewSource.Should().Contain("kind: \"public\";");
        accountEntryViewSource.Should().Contain("kind: \"member\";");
        accountEntryViewSource.Should().Contain("export function buildAccountEntryView(options: {");
        accountEntryViewSource.Should().Contain("if (!session || !memberRouteContext) {");
        accountEntryViewSource.Should().Contain("publicRouteContext!.storefrontContext");
        accountEntryViewSource.Should().Contain("returnPath: sanitizeAppPath(returnPath, \"/account\"),");
        accountEntryViewSource.Should().Contain("profile: identityContext.profileResult.data ?? null,");
        accountEntryViewSource.Should().Contain("addresses: identityContext.addressesResult.data ?? [],");
        accountEntryViewSource.Should().Contain("recentOrders: commerceSummaryContext.ordersResult.data?.items ?? [],");
        accountEntryViewSource.Should().Contain("recentInvoices: commerceSummaryContext.invoicesResult.data?.items ?? [],");
        accountEntryViewSource.Should().Contain("loyaltyBusinesses: loyaltyBusinessesResult.data?.items ?? [],");
        accountEntryViewSource.Should().Contain("...storefrontProps,");

        accountTypesSource.Should().Contain("export type MemberRegisterResponse = {");
        accountTypesSource.Should().Contain("displayName: string;");
        accountTypesSource.Should().Contain("confirmationEmailSent: boolean;");

        storefrontShoppingSource.Should().Contain("import type { CartDisplaySnapshot } from \"@/features/cart/types\";");
        storefrontShoppingSource.Should().Contain("export function extractCartLinkedProductSlugs(");
        storefrontShoppingSource.Should().Contain("const seen = new Set<string>();");
        storefrontShoppingSource.Should().Contain("const linkedSlugs: string[] = [];");
        storefrontShoppingSource.Should().Contain("const match = snapshot.href.match(/\\/catalog\\/([^/?#]+)/i);");
        storefrontShoppingSource.Should().Contain("if (!slug || seen.has(slug)) {");
        storefrontShoppingSource.Should().Contain("seen.add(slug);");
        storefrontShoppingSource.Should().Contain("linkedSlugs.push(slug);");
        storefrontShoppingSource.Should().Contain("return linkedSlugs;");

        cartTypesSource.Should().Contain("export type PublicCartItemRow = {");
        cartTypesSource.Should().Contain("variantId: string;");
        cartTypesSource.Should().Contain("quantity: number;");
        cartTypesSource.Should().Contain("unitPriceNetMinor: number;");
        cartTypesSource.Should().Contain("lineGrossMinor: number;");
        cartTypesSource.Should().Contain("selectedAddOnValueIdsJson: string;");
        cartTypesSource.Should().Contain("export type PublicCartSummary = {");
        cartTypesSource.Should().Contain("cartId: string;");
        cartTypesSource.Should().Contain("currency: string;");
        cartTypesSource.Should().Contain("items: PublicCartItemRow[];");
        cartTypesSource.Should().Contain("couponCode?: string | null;");
        cartTypesSource.Should().Contain("export type CartDisplaySnapshot = {");
        cartTypesSource.Should().Contain("imageUrl?: string | null;");
        cartTypesSource.Should().Contain("imageAlt?: string | null;");
        cartTypesSource.Should().Contain("sku?: string | null;");

        checkoutHelpersSource.Should().Contain("const DEFAULT_COUNTRY_CODE = \"DE\";");
        checkoutHelpersSource.Should().Contain("const DEFAULT_COUPON_CODE_MAX_LENGTH = 64;");
        checkoutHelpersSource.Should().Contain("function normalizeValue(value: DraftValue)");
        checkoutHelpersSource.Should().Contain("return String(value ?? \"\").trim();");
        checkoutHelpersSource.Should().Contain("function normalizeCountryCode(value: DraftValue)");
        checkoutHelpersSource.Should().Contain("return normalizeValue(value).toUpperCase().slice(0, 2);");
        checkoutHelpersSource.Should().Contain("function parseStrictInteger(value: DraftValue)");
        checkoutHelpersSource.Should().Contain("if (!/^-?\\d+$/.test(normalized)) {");
        checkoutHelpersSource.Should().Contain("return Number.isSafeInteger(parsed) ? parsed : null;");
        checkoutHelpersSource.Should().Contain("function parseStrictFiniteNumber(value: DraftValue)");
        checkoutHelpersSource.Should().Contain("if (!/^-?\\d+(?:\\.\\d+)?$/.test(normalized)) {");
        checkoutHelpersSource.Should().Contain("return Number.isFinite(parsed) ? parsed : null;");
        checkoutHelpersSource.Should().Contain("export function normalizeCouponCode(");
        checkoutHelpersSource.Should().Contain("return normalizeValue(value).toUpperCase().slice(0, maxLength);");
        checkoutHelpersSource.Should().Contain("function createEmptyDraft(): CheckoutDraft {");
        checkoutHelpersSource.Should().Contain("countryCode: DEFAULT_COUNTRY_CODE,");
        checkoutHelpersSource.Should().Contain("selectedShippingMethodId: \"\",");
        checkoutHelpersSource.Should().Contain("export function readCheckoutDraftFromSearchParams(");
        checkoutHelpersSource.Should().Contain("if (!searchParams) {");
        checkoutHelpersSource.Should().Contain("countryCode: normalizeCountryCode(searchParams.countryCode) || DEFAULT_COUNTRY_CODE,");
        checkoutHelpersSource.Should().Contain("export function readCheckoutDraftFromFormData(formData: FormData): CheckoutDraft");
        checkoutHelpersSource.Should().Contain("export function hasCheckoutDraftValues(draft: CheckoutDraft)");
        checkoutHelpersSource.Should().Contain("draft.countryCode && draft.countryCode !== DEFAULT_COUNTRY_CODE");
        checkoutHelpersSource.Should().Contain("export function toCheckoutDraftFromMemberAddress(");
        checkoutHelpersSource.Should().Contain("company: address.company ?? \"\",");
        checkoutHelpersSource.Should().Contain("phoneE164: address.phoneE164 ?? \"\",");
        checkoutHelpersSource.Should().Contain("export function toCheckoutDraftFromMemberProfile(");
        checkoutHelpersSource.Should().Contain(".map((value) => value?.trim() ?? \"\")");
        checkoutHelpersSource.Should().Contain(".filter(Boolean)");
        checkoutHelpersSource.Should().Contain(".join(\" \");");
        checkoutHelpersSource.Should().Contain("export function mergeCheckoutDraft(");
        checkoutHelpersSource.Should().Contain("selectedShippingMethodId:");
        checkoutHelpersSource.Should().Contain("export function isCheckoutAddressComplete(draft: CheckoutDraft)");
        checkoutHelpersSource.Should().Contain("export function toCheckoutAddress(draft: CheckoutDraft): PublicCheckoutAddress");
        checkoutHelpersSource.Should().Contain("company: draft.company || null,");
        checkoutHelpersSource.Should().Contain("state: draft.state || null,");
        checkoutHelpersSource.Should().Contain("phoneE164: draft.phoneE164 || null,");
        checkoutHelpersSource.Should().Contain("export function buildCheckoutDraftSearch(");
        checkoutHelpersSource.Should().Contain("return buildQuerySuffix({");
        checkoutHelpersSource.Should().Contain("export function readSingleSearchParam(value: SearchParamValue)");
        checkoutHelpersSource.Should().Contain("export function readPositiveIntegerSearchParam(");
        checkoutHelpersSource.Should().Contain("return typeof parsed === \"number\" && parsed > 0 ? parsed : fallback;");
        checkoutHelpersSource.Should().Contain("export function readSearchTextParam(");
        checkoutHelpersSource.Should().Contain("return normalizeValue(value).slice(0, maxLength) || undefined;");
        checkoutHelpersSource.Should().Contain("export function readAllowedSearchParam<T extends string>(");
        checkoutHelpersSource.Should().Contain("allowedValues.includes(normalized as T)");
        checkoutHelpersSource.Should().Contain("export function readBoundedNumericSearchParam(");
        checkoutHelpersSource.Should().Contain("if (typeof options?.min === \"number\" && parsed < options.min) {");
        checkoutHelpersSource.Should().Contain("if (typeof options?.max === \"number\" && parsed > options.max) {");
        checkoutHelpersSource.Should().Contain("export function readUppercaseSearchTextParam(");
        checkoutHelpersSource.Should().Contain("return normalized ? normalized.toUpperCase() : undefined;");
        checkoutHelpersSource.Should().Contain("export function readQuantityFromFormData(");
        checkoutHelpersSource.Should().Contain("return normalizeQuantityValue(formData.get(key), fallback);");
        checkoutHelpersSource.Should().Contain("export function readNonNegativeIntegerFromFormData(");
        checkoutHelpersSource.Should().Contain("return typeof parsed === \"number\" && parsed >= 0 ? parsed : null;");

        checkoutTypesSource.Should().Contain("export type PublicCheckoutAddress = {");
        checkoutTypesSource.Should().Contain("fullName: string;");
        checkoutTypesSource.Should().Contain("countryCode: string;");
        checkoutTypesSource.Should().Contain("phoneE164?: string | null;");
        checkoutTypesSource.Should().Contain("export type CheckoutDraft = {");
        checkoutTypesSource.Should().Contain("selectedShippingMethodId: string;");
        checkoutTypesSource.Should().Contain("export type PublicShippingOption = {");
        checkoutTypesSource.Should().Contain("carrier: string;");
        checkoutTypesSource.Should().Contain("service: string;");
        checkoutTypesSource.Should().Contain("export type PublicCheckoutIntent = {");
        checkoutTypesSource.Should().Contain("shipmentMass: number;");
        checkoutTypesSource.Should().Contain("requiresShipping: boolean;");
        checkoutTypesSource.Should().Contain("shippingOptions: PublicShippingOption[];");
        checkoutTypesSource.Should().Contain("export type PlaceOrderFromCartResponse = {");
        checkoutTypesSource.Should().Contain("orderNumber: string;");
        checkoutTypesSource.Should().Contain("grandTotalGrossMinor: number;");
        checkoutTypesSource.Should().Contain("export type PublicStorefrontPaymentIntent = {");
        checkoutTypesSource.Should().Contain("providerReference: string;");
        checkoutTypesSource.Should().Contain("checkoutUrl: string;");
        checkoutTypesSource.Should().Contain("returnUrl: string;");
        checkoutTypesSource.Should().Contain("cancelUrl: string;");
        checkoutTypesSource.Should().Contain("expiresAtUtc: string;");
        checkoutTypesSource.Should().Contain("export type PublicStorefrontOrderConfirmationLine = {");
        checkoutTypesSource.Should().Contain("unitPriceGrossMinor: number;");
        checkoutTypesSource.Should().Contain("export type PublicStorefrontOrderConfirmationPayment = {");
        checkoutTypesSource.Should().Contain("paidAtUtc?: string | null;");
        checkoutTypesSource.Should().Contain("export type PublicStorefrontOrderConfirmation = {");
        checkoutTypesSource.Should().Contain("billingAddressJson: string;");
        checkoutTypesSource.Should().Contain("shippingAddressJson: string;");
        checkoutTypesSource.Should().Contain("lines: PublicStorefrontOrderConfirmationLine[];");
        checkoutTypesSource.Should().Contain("payments: PublicStorefrontOrderConfirmationPayment[];");
        checkoutTypesSource.Should().Contain("export type PublicStorefrontPaymentCompletion = {");
        checkoutTypesSource.Should().Contain("paymentStatus: string;");

        memberSessionTypesSource.Should().Contain("export type MemberSession = {");
        memberSessionTypesSource.Should().Contain("userId: string;");
        memberSessionTypesSource.Should().Contain("email: string;");
        memberSessionTypesSource.Should().Contain("accessTokenExpiresAtUtc: string;");
    }


    [Fact]
    public void WebFrontendCommerceAndMemberRouteComposition_Should_KeepPageContextAndFinalizeFlowWired()
    {
        var accountPageSource = ReadWebFrontendFile(Path.Combine("src", "app", "account", "page.tsx"));
        var cartPageSource = ReadWebFrontendFile(Path.Combine("src", "app", "cart", "page.tsx"));
        var checkoutPageSource = ReadWebFrontendFile(Path.Combine("src", "app", "checkout", "page.tsx"));
        var confirmationPageSource = ReadWebFrontendFile(Path.Combine("src", "app", "checkout", "orders", "[orderId]", "confirmation", "page.tsx"));
        var confirmationFinalizeRouteSource = ReadWebFrontendFile(Path.Combine("src", "app", "checkout", "orders", "[orderId]", "confirmation", "finalize", "route.ts"));
        var mockCheckoutPageSource = ReadWebFrontendFile(Path.Combine("src", "app", "mock-checkout", "page.tsx"));
        var cartViewModelSource = ReadWebFrontendFile(Path.Combine("src", "features", "cart", "server", "get-cart-view-model.ts"));
        var commerceRouteContextSource = ReadWebFrontendFile(Path.Combine("src", "features", "checkout", "server", "get-commerce-route-context.ts"));
        var memberRouteContextSource = ReadWebFrontendFile(Path.Combine("src", "features", "member-portal", "server", "get-member-route-context.ts"));

        accountPageSource.Should().Contain("AccountHubPage");
        accountPageSource.Should().Contain("MemberDashboardPage");
        accountPageSource.Should().Contain("getAccountPageView");
        accountPageSource.Should().Contain("getPublicAccountSeoMetadata");
        accountPageSource.Should().Contain("getRequestCulture");
        accountPageSource.Should().Contain("export async function generateMetadata()");
        accountPageSource.Should().Contain("const { metadata } = await getPublicAccountSeoMetadata(culture);");
        accountPageSource.Should().Contain("type AccountPageProps = {");
        accountPageSource.Should().Contain("searchParams?: Promise<Record<string, string | string[] | undefined>>;");
        accountPageSource.Should().Contain("const resolvedSearchParams = searchParams ? await searchParams : undefined;");
        accountPageSource.Should().Contain("const returnPath = Array.isArray(resolvedSearchParams?.returnPath)");
        accountPageSource.Should().Contain("const view = await getAccountPageView(culture, returnPath);");
        accountPageSource.Should().Contain("return view.kind === \"public\" ? (");

        cartPageSource.Should().Contain("CartPage");
        cartPageSource.Should().Contain("getCartSeoMetadata");
        cartPageSource.Should().Contain("getCartPageContext");
        cartPageSource.Should().Contain("readAllowedSearchParam");
        cartPageSource.Should().Contain("readSingleSearchParam");
        cartPageSource.Should().Contain("const { routeContext, followUpProducts } = await getCartPageContext(culture);");
        cartPageSource.Should().Contain("const { model, memberSession, identityContext, storefrontContext } = routeContext;");
        cartPageSource.Should().Contain("cartStatus={readAllowedSearchParam(resolvedSearchParams?.cartStatus, [");
        cartPageSource.Should().Contain("\"coupon-cleared\",");
        cartPageSource.Should().Contain("cartError={readSingleSearchParam(resolvedSearchParams?.cartError)}");
        cartPageSource.Should().Contain("hasMemberSession={Boolean(memberSession)}");
        cartPageSource.Should().Contain("followUpProducts={followUpProducts}");
        cartPageSource.Should().Contain("cmsPages={storefrontContext.cmsPages}");
        cartPageSource.Should().Contain("categories={storefrontContext.categories}");

        checkoutPageSource.Should().Contain("CheckoutPage");
        checkoutPageSource.Should().Contain("createPublicCheckoutIntent");
        checkoutPageSource.Should().Contain("getCheckoutPageContext");
        checkoutPageSource.Should().Contain("getCheckoutSeoMetadata");
        checkoutPageSource.Should().Contain("hasCheckoutDraftValues");
        checkoutPageSource.Should().Contain("mergeCheckoutDraft");
        checkoutPageSource.Should().Contain("readCheckoutDraftFromSearchParams");
        checkoutPageSource.Should().Contain("toCheckoutDraftFromMemberAddress");
        checkoutPageSource.Should().Contain("toCheckoutDraftFromMemberProfile");
        checkoutPageSource.Should().Contain("toCheckoutAddress");
        checkoutPageSource.Should().Contain("const requestedDraft = readCheckoutDraftFromSearchParams(resolvedSearchParams);");
        checkoutPageSource.Should().Contain("const checkoutError = readSingleSearchParam(resolvedSearchParams?.checkoutError);");
        checkoutPageSource.Should().Contain("const selectedMemberAddressId = readSingleSearchParam(");
        checkoutPageSource.Should().Contain("memberAddresses.find((address) => address.id === selectedMemberAddressId)");
        checkoutPageSource.Should().Contain("memberAddresses.find((address) => address.isDefaultShipping)");
        checkoutPageSource.Should().Contain("memberAddresses.find((address) => address.isDefaultBilling)");
        checkoutPageSource.Should().Contain("const draft = !hasCheckoutDraftValues(requestedDraft)");
        checkoutPageSource.Should().Contain("const effectiveSelectedMemberAddressId =");
        checkoutPageSource.Should().Contain("let intent = null;");
        checkoutPageSource.Should().Contain("let intentStatus = \"idle\";");
        checkoutPageSource.Should().Contain("let intentMessage: string | undefined;");
        checkoutPageSource.Should().Contain("if (model.cart && isCheckoutAddressComplete(draft)) {");
        checkoutPageSource.Should().Contain("const intentResult = await createPublicCheckoutIntent({");
        checkoutPageSource.Should().Contain("shippingAddress: toCheckoutAddress(draft),");
        checkoutPageSource.Should().Contain("selectedShippingMethodId: draft.selectedShippingMethodId || undefined,");
        checkoutPageSource.Should().Contain("intent={intent}");
        checkoutPageSource.Should().Contain("profilePrefillActive={!preferredMemberAddress && Boolean(memberProfile)}");
        checkoutPageSource.Should().Contain("selectedMemberAddressId={effectiveSelectedMemberAddressId}");

        confirmationPageSource.Should().Contain("redirect } from \"next/navigation\"");
        confirmationPageSource.Should().Contain("OrderConfirmationPage");
        confirmationPageSource.Should().Contain("getConfirmationPageContext");
        confirmationPageSource.Should().Contain("getConfirmationSeoMetadata");
        confirmationPageSource.Should().Contain("readStorefrontPaymentHandoff");
        confirmationPageSource.Should().Contain("buildAppQueryPath");
        confirmationPageSource.Should().Contain("const orderNumber = readSingleSearchParam(resolvedSearchParams?.orderNumber);");
        confirmationPageSource.Should().Contain("const checkoutStatus = readAllowedSearchParam(");
        confirmationPageSource.Should().Contain("const paymentCompletionStatus = readAllowedSearchParam(");
        confirmationPageSource.Should().Contain("const paymentOutcome = readAllowedSearchParam(");
        confirmationPageSource.Should().Contain("const paymentError = readSingleSearchParam(resolvedSearchParams?.paymentError);");
        confirmationPageSource.Should().Contain("const cancelled = readSingleSearchParam(resolvedSearchParams?.cancelled) === \"true\";");
        confirmationPageSource.Should().Contain("const handoff = await readStorefrontPaymentHandoff();");
        confirmationPageSource.Should().Contain("if (");
        confirmationPageSource.Should().Contain("!paymentCompletionStatus &&");
        confirmationPageSource.Should().Contain("handoff?.orderId === resolvedParams.orderId");
        confirmationPageSource.Should().Contain("`/checkout/orders/${resolvedParams.orderId}/confirmation/finalize`");
        confirmationPageSource.Should().Contain("const { routeContext, followUpProducts } = await getConfirmationPageContext(");
        confirmationPageSource.Should().Contain("confirmation={confirmationResult.data}");
        confirmationPageSource.Should().Contain("memberOrders={commerceSummaryContext?.ordersResult.data?.items.slice(0, 2) ?? []}");
        confirmationPageSource.Should().Contain("memberInvoices={commerceSummaryContext?.invoicesResult.data?.items.slice(0, 2) ?? []}");
        confirmationPageSource.Should().Contain("products={");
        confirmationPageSource.Should().Contain("followUpProducts.length > 0 ? followUpProducts : storefrontContext.products");

        confirmationFinalizeRouteSource.Should().Contain("NextResponse");
        confirmationFinalizeRouteSource.Should().Contain("completePublicStorefrontPayment");
        confirmationFinalizeRouteSource.Should().Contain("clearStorefrontPaymentHandoff");
        confirmationFinalizeRouteSource.Should().Contain("readStorefrontPaymentHandoff");
        confirmationFinalizeRouteSource.Should().Contain("readSearchTextParam");
        confirmationFinalizeRouteSource.Should().Contain("buildAppQueryPath, buildLocalizedPath");
        confirmationFinalizeRouteSource.Should().Contain("toLocalizedQueryMessage");
        confirmationFinalizeRouteSource.Should().Contain("getRequestCulture");
        confirmationFinalizeRouteSource.Should().Contain("type FinalizeOutcome = \"Succeeded\" | \"Cancelled\" | \"Failed\";");
        confirmationFinalizeRouteSource.Should().Contain("async function buildRedirectUrl(request: NextRequest, orderId: string)");
        confirmationFinalizeRouteSource.Should().Contain("const confirmationPath = buildLocalizedPath(");
        confirmationFinalizeRouteSource.Should().Contain("function applyRedirectParams(");
        confirmationFinalizeRouteSource.Should().Contain("const pathWithQuery = buildAppQueryPath(redirectUrl.pathname, params);");
        confirmationFinalizeRouteSource.Should().Contain("function resolveOutcome(searchParams: URLSearchParams)");
        confirmationFinalizeRouteSource.Should().Contain("if (searchParams.get(\"cancelled\") === \"true\") {");
        confirmationFinalizeRouteSource.Should().Contain("function readProviderReference(searchParams: URLSearchParams)");
        confirmationFinalizeRouteSource.Should().Contain("function readFailureReason(");
        confirmationFinalizeRouteSource.Should().Contain("\"Shopper cancelled hosted checkout.\"");
        confirmationFinalizeRouteSource.Should().Contain("export async function GET(request: NextRequest, context: FinalizeRouteContext)");
        confirmationFinalizeRouteSource.Should().Contain("const handoff = await readStorefrontPaymentHandoff();");
        confirmationFinalizeRouteSource.Should().Contain("if (!handoff || handoff.orderId !== orderId || !handoff.paymentId) {");
        confirmationFinalizeRouteSource.Should().Contain("paymentCompletionStatus: \"missing-context\",");
        confirmationFinalizeRouteSource.Should().Contain("const result = await completePublicStorefrontPayment({");
        confirmationFinalizeRouteSource.Should().Contain("paymentId: handoff.paymentId,");
        confirmationFinalizeRouteSource.Should().Contain("orderNumber: orderNumber ?? handoff.orderNumber,");
        confirmationFinalizeRouteSource.Should().Contain("providerReference: readProviderReference(searchParams) || handoff.providerReference,");
        confirmationFinalizeRouteSource.Should().Contain("await clearStorefrontPaymentHandoff();");
        confirmationFinalizeRouteSource.Should().Contain("paymentCompletionStatus: \"failed\",");
        confirmationFinalizeRouteSource.Should().Contain("checkoutPaymentCompletionFailedMessage");
        confirmationFinalizeRouteSource.Should().Contain("paymentCompletionStatus: \"completed\",");
        confirmationFinalizeRouteSource.Should().Contain("paymentStatus: result.data.paymentStatus,");
        confirmationFinalizeRouteSource.Should().Contain("orderStatus: result.data.orderStatus,");

        mockCheckoutPageSource.Should().Contain("MockCheckoutPage");
        mockCheckoutPageSource.Should().Contain("getRequestCulture");
        mockCheckoutPageSource.Should().Contain("export function readSearchValue(");
        mockCheckoutPageSource.Should().Contain("const raw = Array.isArray(value) ? value[0] : value;");
        mockCheckoutPageSource.Should().Contain("export function tryParseAbsoluteUrl(value: string)");
        mockCheckoutPageSource.Should().Contain("const url = new URL(value);");
        mockCheckoutPageSource.Should().Contain("export function toFinalizeUrl(value: string)");
        mockCheckoutPageSource.Should().Contain("url.pathname = `${url.pathname.replace(/\\/$/, \"\")}/finalize`;");
        mockCheckoutPageSource.Should().Contain("export function buildOutcomeUrl(");
        mockCheckoutPageSource.Should().Contain("url.searchParams.set(\"providerReference\", providerReference);");
        mockCheckoutPageSource.Should().Contain("url.searchParams.set(\"outcome\", outcome);");
        mockCheckoutPageSource.Should().Contain("url.searchParams.set(\"cancelled\", \"true\");");
        mockCheckoutPageSource.Should().Contain("url.searchParams.set(\"failureReason\", failureReason);");
        mockCheckoutPageSource.Should().Contain("title: culture === \"de-DE\" ? \"Mock Checkout\" : \"Mock checkout\",");
        mockCheckoutPageSource.Should().Contain("const successUrl = buildOutcomeUrl(returnUrl, sessionToken, \"Succeeded\");");
        mockCheckoutPageSource.Should().Contain("const cancelActionUrl = buildOutcomeUrl(cancelUrl, sessionToken, \"Cancelled\");");
        mockCheckoutPageSource.Should().Contain("\"Mock checkout marked the payment as failed.\"");
        mockCheckoutPageSource.Should().Contain("returnUrl={tryParseAbsoluteUrl(returnUrl)?.toString() ?? null}");
        mockCheckoutPageSource.Should().Contain("cancelUrl={tryParseAbsoluteUrl(cancelUrl)?.toString() ?? null}");

        cartViewModelSource.Should().Contain("import \"server-only\";");
        cartViewModelSource.Should().Contain("getAnonymousCartId");
        cartViewModelSource.Should().Contain("pruneCartDisplaySnapshots");
        cartViewModelSource.Should().Contain("readCartDisplaySnapshots");
        cartViewModelSource.Should().Contain("getPublicCart");
        cartViewModelSource.Should().Contain("createCachedObservedLoader");
        cartViewModelSource.Should().Contain("summarizeCartViewModelHealth");
        cartViewModelSource.Should().Contain("export type CartViewRow = PublicCartItemRow & {");
        cartViewModelSource.Should().Contain("display: CartDisplaySnapshot | null;");
        cartViewModelSource.Should().Contain("const getCachedCartViewModel = createCachedObservedLoader({");
        cartViewModelSource.Should().Contain("anonymousCartState: anonymousId ? \"present\" : \"missing\",");
        cartViewModelSource.Should().Contain("if (!anonymousId) {");
        cartViewModelSource.Should().Contain("status: \"empty\",");
        cartViewModelSource.Should().Contain("const [cartResult, displaySnapshots] = await Promise.all([");
        cartViewModelSource.Should().Contain("const activeVariantIds = cartResult.data.items.map((item) => item.variantId);");
        cartViewModelSource.Should().Contain("await pruneCartDisplaySnapshots(activeVariantIds);");
        cartViewModelSource.Should().Contain("displaySnapshots.find((snapshot) => snapshot.variantId === item.variantId) ??");
        cartViewModelSource.Should().Contain("export async function getCartViewModel(): Promise<CartViewModel>");

        commerceRouteContextSource.Should().Contain("import \"server-only\";");
        commerceRouteContextSource.Should().Contain("getPublicStorefrontOrderConfirmation");
        commerceRouteContextSource.Should().Contain("getCartViewModel");
        commerceRouteContextSource.Should().Contain("getMemberSession");
        commerceRouteContextSource.Should().Contain("getMemberCommerceSummaryContext");
        commerceRouteContextSource.Should().Contain("getMemberIdentityContext");
        commerceRouteContextSource.Should().Contain("getPublicStorefrontContext");
        commerceRouteContextSource.Should().Contain("normalizeConfirmationResultArgs");
        commerceRouteContextSource.Should().Contain("normalizeConfirmationRouteArgs");
        commerceRouteContextSource.Should().Contain("normalizeCultureArg");
        commerceRouteContextSource.Should().Contain("summarizeCommerceRouteHealth");
        commerceRouteContextSource.Should().Contain("summarizeConfirmationResultHealth");
        commerceRouteContextSource.Should().Contain("commerceRouteObservationContext");
        commerceRouteContextSource.Should().Contain("export function summarizeCommerceRouteStorefrontSupport(");
        commerceRouteContextSource.Should().Contain("const getCachedConfirmationResult = createCachedObservedLoader({");
        commerceRouteContextSource.Should().Contain("operation: \"load-confirmation-result\",");
        commerceRouteContextSource.Should().Contain("const getCachedCartRouteContext = createCachedObservedLoader({");
        commerceRouteContextSource.Should().Contain("operation: \"load-cart-context\",");
        commerceRouteContextSource.Should().Contain("const [model, memberSession, storefrontContext] = await Promise.all([");
        commerceRouteContextSource.Should().Contain("const identityContext = memberSession ? await getMemberIdentityContext() : null;");
        commerceRouteContextSource.Should().Contain("const getCachedCheckoutRouteContext = createCachedObservedLoader({");
        commerceRouteContextSource.Should().Contain("operation: \"load-checkout-context\",");
        commerceRouteContextSource.Should().Contain("const [identityContext, commerceSummaryContext] = memberSession");
        commerceRouteContextSource.Should().Contain("getMemberCommerceSummaryContext(),");
        commerceRouteContextSource.Should().Contain("const getCachedConfirmationRouteContext = createCachedObservedLoader({");
        commerceRouteContextSource.Should().Contain("operation: \"load-confirmation-context\",");
        commerceRouteContextSource.Should().Contain("const [confirmationResult, memberSession, storefrontContext] =");
        commerceRouteContextSource.Should().Contain("const commerceSummaryContext = memberSession");
        commerceRouteContextSource.Should().Contain("export async function getCartRouteContext(culture: string)");
        commerceRouteContextSource.Should().Contain("export async function getCheckoutRouteContext(culture: string)");
        commerceRouteContextSource.Should().Contain("export async function getConfirmationRouteContext(");

        memberRouteContextSource.Should().Contain("import \"server-only\";");
        memberRouteContextSource.Should().Contain("getCurrentMemberInvoice");
        memberRouteContextSource.Should().Contain("getCurrentMemberLoyaltyBusinesses");
        memberRouteContextSource.Should().Contain("getCurrentMemberOrder");
        memberRouteContextSource.Should().Contain("getMemberCommerceSummaryContext");
        memberRouteContextSource.Should().Contain("getMemberIdentityContext");
        memberRouteContextSource.Should().Contain("getMemberInvoicesPageContext");
        memberRouteContextSource.Should().Contain("getMemberOrdersPageContext");
        memberRouteContextSource.Should().Contain("getPublicStorefrontContext");
        memberRouteContextSource.Should().Contain("normalizeCultureArg");
        memberRouteContextSource.Should().Contain("normalizeEntityRouteArgs");
        memberRouteContextSource.Should().Contain("normalizePagedRouteArgs");
        memberRouteContextSource.Should().Contain("summarizeMemberCollectionHealth");
        memberRouteContextSource.Should().Contain("summarizeMemberDashboardHealth");
        memberRouteContextSource.Should().Contain("summarizeMemberDetailHealth");
        memberRouteContextSource.Should().Contain("summarizeMemberEditorHealth");
        memberRouteContextSource.Should().Contain("memberRouteObservationContext");
        memberRouteContextSource.Should().Contain("export function summarizeMemberRouteStorefrontSupport(");
        memberRouteContextSource.Should().Contain("export const getMemberDashboardRouteContext = createCachedObservedLoader({");
        memberRouteContextSource.Should().Contain("operation: \"load-dashboard-context\",");
        memberRouteContextSource.Should().Contain("getCurrentMemberLoyaltyBusinesses({ page: 1, pageSize: 3 })");
        memberRouteContextSource.Should().Contain("export const getMemberEditorRouteContext = createCachedObservedLoader({");
        memberRouteContextSource.Should().Contain("routeGroup: \"account-editor\"");
        memberRouteContextSource.Should().Contain("export const getMemberOrdersRouteContext = createCachedObservedLoader({");
        memberRouteContextSource.Should().Contain("operation: \"load-orders-context\",");
        memberRouteContextSource.Should().Contain("getMemberOrdersPageContext(page, pageSize)");
        memberRouteContextSource.Should().Contain("export const getMemberInvoicesRouteContext = createCachedObservedLoader({");
        memberRouteContextSource.Should().Contain("operation: \"load-invoices-context\",");
        memberRouteContextSource.Should().Contain("getMemberInvoicesPageContext(page, pageSize)");
        memberRouteContextSource.Should().Contain("export const getMemberOrderDetailRouteContext = createCachedObservedLoader({");
        memberRouteContextSource.Should().Contain("detail: summarizeMemberDetailHealth(result.orderResult),");
        memberRouteContextSource.Should().Contain("getCurrentMemberOrder(id)");
        memberRouteContextSource.Should().Contain("export const getMemberInvoiceDetailRouteContext = createCachedObservedLoader({");
        memberRouteContextSource.Should().Contain("detail: summarizeMemberDetailHealth(result.invoiceResult),");
        memberRouteContextSource.Should().Contain("getCurrentMemberInvoice(id)");
    }


    [Fact]
    public void WebFrontendAccountAndMemberSurfaceRoutes_Should_KeepAuthDashboardAndEditorShellsWired()
    {
        var activationRouteSource = ReadWebFrontendFile(Path.Combine("src", "app", "account", "activation", "page.tsx"));
        var addressesRouteSource = ReadWebFrontendFile(Path.Combine("src", "app", "account", "addresses", "page.tsx"));
        var passwordRouteSource = ReadWebFrontendFile(Path.Combine("src", "app", "account", "password", "page.tsx"));
        var preferencesRouteSource = ReadWebFrontendFile(Path.Combine("src", "app", "account", "preferences", "page.tsx"));
        var profileRouteSource = ReadWebFrontendFile(Path.Combine("src", "app", "account", "profile", "page.tsx"));
        var registerRouteSource = ReadWebFrontendFile(Path.Combine("src", "app", "account", "register", "page.tsx"));
        var securityRouteSource = ReadWebFrontendFile(Path.Combine("src", "app", "account", "security", "page.tsx"));
        var signInRouteSource = ReadWebFrontendFile(Path.Combine("src", "app", "account", "sign-in", "page.tsx"));
        var activationPageSource = ReadWebFrontendFile(Path.Combine("src", "components", "account", "activation-page.tsx"));
        var addressesPageSource = ReadWebFrontendFile(Path.Combine("src", "components", "account", "addresses-page.tsx"));
        var profilePageSource = ReadWebFrontendFile(Path.Combine("src", "components", "account", "profile-page.tsx"));
        var preferencesPageSource = ReadWebFrontendFile(Path.Combine("src", "components", "account", "preferences-page.tsx"));
        var securityPageSource = ReadWebFrontendFile(Path.Combine("src", "components", "account", "security-page.tsx"));
        var registerPageSource = ReadWebFrontendFile(Path.Combine("src", "components", "account", "register-page.tsx"));
        var signInPageSource = ReadWebFrontendFile(Path.Combine("src", "components", "account", "sign-in-page.tsx"));
        var memberDashboardPageSource = ReadWebFrontendFile(Path.Combine("src", "components", "account", "member-dashboard-page.tsx"));

        activationRouteSource.Should().Contain("ActivationPage");
        activationRouteSource.Should().Contain("getPublicActivationPageContext");
        activationRouteSource.Should().Contain("getPublicActivationSeoMetadata");
        activationRouteSource.Should().Contain("sanitizeAppPath");
        activationRouteSource.Should().Contain("function readSearchParam(");
        activationRouteSource.Should().Contain("email={readSearchParam(resolvedSearchParams?.email)}");
        activationRouteSource.Should().Contain("token={readSearchParam(resolvedSearchParams?.token)}");
        activationRouteSource.Should().Contain("activationStatus={readSearchParam(resolvedSearchParams?.activationStatus)}");
        activationRouteSource.Should().Contain("activationError={readSearchParam(resolvedSearchParams?.activationError)}");

        passwordRouteSource.Should().Contain("PasswordPage");
        passwordRouteSource.Should().Contain("getPublicPasswordPageContext");
        passwordRouteSource.Should().Contain("getPublicPasswordSeoMetadata");
        passwordRouteSource.Should().Contain("passwordStatus={readSearchParam(resolvedSearchParams?.passwordStatus)}");
        passwordRouteSource.Should().Contain("passwordError={readSearchParam(resolvedSearchParams?.passwordError)}");
        passwordRouteSource.Should().Contain("returnPath={sanitizeAppPath(");

        registerRouteSource.Should().Contain("RegisterPage");
        registerRouteSource.Should().Contain("getPublicRegisterPageContext");
        registerRouteSource.Should().Contain("getPublicRegisterSeoMetadata");
        registerRouteSource.Should().Contain("registerStatus={readSearchParam(resolvedSearchParams?.registerStatus)}");
        registerRouteSource.Should().Contain("registerError={readSearchParam(resolvedSearchParams?.registerError)}");

        signInRouteSource.Should().Contain("SignInPage");
        signInRouteSource.Should().Contain("getPublicSignInPageContext");
        signInRouteSource.Should().Contain("getPublicSignInSeoMetadata");
        signInRouteSource.Should().Contain("signInError={readSearchParam(resolvedSearchParams?.signInError)}");
        signInRouteSource.Should().Contain("returnPath={sanitizeAppPath(readSearchParam(resolvedSearchParams?.returnPath), \"/account\")}");

        addressesRouteSource.Should().Contain("AddressesPage");
        addressesRouteSource.Should().Contain("MemberAuthRequired");
        addressesRouteSource.Should().Contain("getMemberEditorPageContext");
        addressesRouteSource.Should().Contain("getAddressesSeoMetadata");
        addressesRouteSource.Should().Contain("createStorefrontContinuationProps");
        addressesRouteSource.Should().Contain("createStorefrontContinuationWithCartProps");
        addressesRouteSource.Should().Contain("if (!session) {");
        addressesRouteSource.Should().Contain("returnPath=\"/account/addresses\"");
        addressesRouteSource.Should().Contain("addresses={identityContext.addressesResult.data ?? []}");
        addressesRouteSource.Should().Contain("status={identityContext.addressesResult.status}");

        preferencesRouteSource.Should().Contain("PreferencesPage");
        preferencesRouteSource.Should().Contain("MemberAuthRequired");
        preferencesRouteSource.Should().Contain("getPreferencesSeoMetadata");
        preferencesRouteSource.Should().Contain("returnPath=\"/account/preferences\"");
        preferencesRouteSource.Should().Contain("preferences={identityContext.preferencesResult.data}");
        preferencesRouteSource.Should().Contain("profile={identityContext.profileResult.data}");
        preferencesRouteSource.Should().Contain("preferencesStatus={readSearchParam(resolvedSearchParams?.preferencesStatus)}");
        preferencesRouteSource.Should().Contain("preferencesError={readSearchParam(resolvedSearchParams?.preferencesError)}");

        profileRouteSource.Should().Contain("ProfilePage");
        profileRouteSource.Should().Contain("getProfileSeoMetadata");
        profileRouteSource.Should().Contain("getSupportedCultures()");
        profileRouteSource.Should().Contain("returnPath=\"/account/profile\"");
        profileRouteSource.Should().Contain("supportedCultures={supportedCultures}");
        profileRouteSource.Should().Contain("phoneStatus={readSearchParam(resolvedSearchParams?.phoneStatus)}");
        profileRouteSource.Should().Contain("phoneError={readSearchParam(resolvedSearchParams?.phoneError)}");

        securityRouteSource.Should().Contain("SecurityPage");
        securityRouteSource.Should().Contain("getSecuritySeoMetadata");
        securityRouteSource.Should().Contain("returnPath=\"/account/security\"");
        securityRouteSource.Should().Contain("session={session}");
        securityRouteSource.Should().Contain("securityStatus={readSearchParam(resolvedSearchParams?.securityStatus)}");
        securityRouteSource.Should().Contain("securityError={readSearchParam(resolvedSearchParams?.securityError)}");

        activationPageSource.Should().Contain("requestEmailConfirmationAction");
        activationPageSource.Should().Contain("confirmEmailAction");
        activationPageSource.Should().Contain("PublicAuthReturnSummary");
        activationPageSource.Should().Contain("PublicAuthCompositionWindow");
        activationPageSource.Should().Contain("PublicAuthContinuation");
        activationPageSource.Should().Contain("function getActivationMessage(status: string | undefined, culture: string)");
        activationPageSource.Should().Contain("case \"requested\":");
        activationPageSource.Should().Contain("case \"confirmed\":");
        activationPageSource.Should().Contain("const statusMessage = getActivationMessage(activationStatus, culture);");
        activationPageSource.Should().Contain("buildLocalizedAuthHref(\"/account/sign-in\", returnPath, culture)");
        activationPageSource.Should().Contain("buildLocalizedAuthHref(\"/account/password\", returnPath, culture)");
        activationPageSource.Should().Contain("returnPath || \"/account\"");

        addressesPageSource.Should().Contain("AccountContentCompositionWindow");
        addressesPageSource.Should().Contain("AccountStorefrontWindow");
        addressesPageSource.Should().Contain("MemberPortalNav");
        addressesPageSource.Should().Contain("MemberCrossSurfaceRail");
        addressesPageSource.Should().Contain("createMemberAddressAction");
        addressesPageSource.Should().Contain("deleteMemberAddressAction");
        addressesPageSource.Should().Contain("setMemberAddressDefaultAction");
        addressesPageSource.Should().Contain("updateMemberAddressAction");
        addressesPageSource.Should().Contain("const checkoutHref = preferredCheckoutAddress");
        addressesPageSource.Should().Contain("buildCheckoutDraftSearch(");
        addressesPageSource.Should().Contain("toCheckoutDraftFromMemberAddress(preferredCheckoutAddress)");
        addressesPageSource.Should().Contain("id=\"addresses-create\"");
        addressesPageSource.Should().Contain("id=\"addresses-readiness\"");
        addressesPageSource.Should().Contain("id=\"addresses-composition\"");
        addressesPageSource.Should().Contain("id=\"addresses-saved\"");
        addressesPageSource.Should().Contain("name=\"rowVersion\" value={address.rowVersion}");

        profilePageSource.Should().Contain("updateMemberProfileAction");
        profilePageSource.Should().Contain("requestMemberPhoneVerificationAction");
        profilePageSource.Should().Contain("confirmMemberPhoneVerificationAction");
        profilePageSource.Should().Contain("getCultureDisplayName");
        profilePageSource.Should().Contain("function getPhoneStatusBanner(");
        profilePageSource.Should().Contain("phoneStatus === \"requested\"");
        profilePageSource.Should().Contain("phoneStatus === \"confirmed\"");
        profilePageSource.Should().Contain("defaultValue={profile.locale ?? culture}");
        profilePageSource.Should().Contain("supportedCultures.map((supportedCulture) => (");
        profilePageSource.Should().Contain("name=\"channel\"");
        profilePageSource.Should().Contain("value=\"Sms\"");
        profilePageSource.Should().Contain("value=\"WhatsApp\"");
        profilePageSource.Should().Contain("name=\"code\"");

        preferencesPageSource.Should().Contain("updateMemberPreferencesAction");
        preferencesPageSource.Should().Contain("function ToggleField({");
        preferencesPageSource.Should().Contain("name=\"marketingConsent\"");
        preferencesPageSource.Should().Contain("name=\"allowEmailMarketing\"");
        preferencesPageSource.Should().Contain("name=\"allowSmsMarketing\"");
        preferencesPageSource.Should().Contain("name=\"allowWhatsAppMarketing\"");
        preferencesPageSource.Should().Contain("name=\"allowPromotionalPushNotifications\"");
        preferencesPageSource.Should().Contain("name=\"allowOptionalAnalyticsTracking\"");
        preferencesPageSource.Should().Contain("const smsReady = Boolean(");
        preferencesPageSource.Should().Contain("const whatsAppReady = Boolean(");
        preferencesPageSource.Should().Contain("id=\"preferences-form\"");
        preferencesPageSource.Should().Contain("id=\"preferences-summary\"");
        preferencesPageSource.Should().Contain("id=\"preferences-readiness\"");
        preferencesPageSource.Should().Contain("id=\"preferences-composition\"");

        securityPageSource.Should().Contain("changeMemberPasswordAction");
        securityPageSource.Should().Contain("type { MemberSession }");
        securityPageSource.Should().Contain("parseUtcTimestamp");
        securityPageSource.Should().Contain("const hasValidSessionExpiry = parseUtcTimestamp(session.accessTokenExpiresAtUtc) !== null;");
        securityPageSource.Should().Contain("const securityState =");
        securityPageSource.Should().Contain("profileStatus === \"not-found\"");
        securityPageSource.Should().Contain("profileStatus === \"network-error\"");
        securityPageSource.Should().Contain("profileStatus === \"http-error\"");
        securityPageSource.Should().Contain("profileStatus === \"invalid-payload\"");
        securityPageSource.Should().Contain("profileStatus === \"unauthorized\"");
        securityPageSource.Should().Contain("profileStatus === \"unauthenticated\"");
        securityPageSource.Should().Contain("name=\"currentPassword\"");
        securityPageSource.Should().Contain("name=\"newPassword\"");
        securityPageSource.Should().Contain("name=\"confirmPassword\"");
        securityPageSource.Should().Contain("id=\"security-form\"");
        securityPageSource.Should().Contain("id=\"security-state\"");
        securityPageSource.Should().Contain("id=\"security-summary\"");
        securityPageSource.Should().Contain("id=\"security-composition\"");

        registerPageSource.Should().Contain("registerMemberAction");
        registerPageSource.Should().Contain("ActivationRecoveryPanel");
        registerPageSource.Should().Contain("PublicAuthReturnSummary");
        registerPageSource.Should().Contain("PublicAuthCompositionWindow");
        registerPageSource.Should().Contain("PublicAuthContinuation");
        registerPageSource.Should().Contain("buildLocalizedAuthHref(\"/account/activation\", returnPath, culture)");
        registerPageSource.Should().Contain("buildLocalizedAuthHref(\"/account/sign-in\", returnPath, culture)");
        registerPageSource.Should().Contain("registerStatus === \"registered\"");
        registerPageSource.Should().Contain("registerStatus === \"registered\" || Boolean(email)");
        registerPageSource.Should().Contain("action={registerMemberAction}");

        signInPageSource.Should().Contain("signInMemberAction");
        signInPageSource.Should().Contain("ActivationRecoveryPanel");
        signInPageSource.Should().Contain("PublicAuthReturnSummary");
        signInPageSource.Should().Contain("PublicAuthCompositionWindow");
        signInPageSource.Should().Contain("PublicAuthContinuation");
        signInPageSource.Should().Contain("buildLocalizedAuthHref(\"/account/register\", returnPath, culture)");
        signInPageSource.Should().Contain("buildLocalizedAuthHref(\"/account/activation\", returnPath, culture)");
        signInPageSource.Should().Contain("buildLocalizedAuthHref(\"/account/password\", returnPath, culture)");
        signInPageSource.Should().Contain("action={signInMemberAction}");

        memberDashboardPageSource.Should().Contain("buildMemberPromotionLaneCards");
        memberDashboardPageSource.Should().Contain("MemberStorefrontWindow");
        memberDashboardPageSource.Should().Contain("MemberCrossSurfaceRail");
        memberDashboardPageSource.Should().Contain("sortProductsByOpportunity");
        memberDashboardPageSource.Should().Contain("buildCheckoutDraftSearch, toCheckoutDraftFromMemberAddress");
        memberDashboardPageSource.Should().Contain("signOutMemberAction");
        memberDashboardPageSource.Should().Contain("parseUtcTimestamp");
        memberDashboardPageSource.Should().Contain("const securityState =");
        memberDashboardPageSource.Should().Contain("const preferredCheckoutAddress =");
        memberDashboardPageSource.Should().Contain("const checkoutHref = preferredCheckoutAddress");
        memberDashboardPageSource.Should().Contain("const actionItems: DashboardActionItem[] = [");
        memberDashboardPageSource.Should().Contain("id: \"storefront-cart\",");
        memberDashboardPageSource.Should().Contain("id: \"phone-verification\",");
        memberDashboardPageSource.Should().Contain("id: \"security-session\",");
        memberDashboardPageSource.Should().Contain("id: \"address-book\",");
        memberDashboardPageSource.Should().Contain("id: \"invoice-balance\",");
        memberDashboardPageSource.Should().Contain("id: \"loyalty-reward\",");
        memberDashboardPageSource.Should().Contain("const dashboardCompositionRouteCard = {");
        memberDashboardPageSource.Should().Contain("const dashboardCompositionNextCard = attentionOrders[0]");
        memberDashboardPageSource.Should().Contain("const dashboardCompositionRouteMapItems = [");
        memberDashboardPageSource.Should().Contain("action={signOutMemberAction}");
    }


    [Fact]
    public void WebFrontendPublicAuthAndMemberSupportPrimitives_Should_KeepContextRecoveryAndCrossSurfaceRailsWired()
    {
        var accountPageContextSource = ReadWebFrontendFile(Path.Combine("src", "features", "account", "server", "get-account-page-context.ts"));
        var accountPageViewSource = ReadWebFrontendFile(Path.Combine("src", "features", "account", "server", "get-account-page-view.ts"));
        var publicAuthPageContextSource = ReadWebFrontendFile(Path.Combine("src", "features", "account", "server", "get-public-auth-page-context.ts"));
        var publicAuthRouteContextSource = ReadWebFrontendFile(Path.Combine("src", "features", "account", "server", "get-public-auth-route-context.ts"));
        var publicAuthSeoMetadataSource = ReadWebFrontendFile(Path.Combine("src", "features", "account", "server", "get-public-auth-seo-metadata.ts"));
        var memberEntryContextSource = ReadWebFrontendFile(Path.Combine("src", "features", "member-portal", "server", "get-member-entry-context.ts"));
        var memberProtectedPageContextSource = ReadWebFrontendFile(Path.Combine("src", "features", "member-portal", "server", "get-member-protected-page-context.ts"));
        var memberSummaryContextSource = ReadWebFrontendFile(Path.Combine("src", "features", "member-portal", "server", "get-member-summary-context.ts"));
        var activationRecoveryPanelSource = ReadWebFrontendFile(Path.Combine("src", "components", "account", "activation-recovery-panel.tsx"));
        var publicAuthCompositionWindowSource = ReadWebFrontendFile(Path.Combine("src", "components", "account", "public-auth-composition-window.tsx"));
        var publicAuthContinuationSource = ReadWebFrontendFile(Path.Combine("src", "components", "account", "public-auth-continuation.tsx"));
        var publicAuthReturnSummarySource = ReadWebFrontendFile(Path.Combine("src", "components", "account", "public-auth-return-summary.tsx"));
        var memberAuthRequiredSource = ReadWebFrontendFile(Path.Combine("src", "components", "member", "member-auth-required.tsx"));
        var memberCrossSurfaceRailSource = ReadWebFrontendFile(Path.Combine("src", "components", "member", "member-cross-surface-rail.tsx"));

        accountPageContextSource.Should().Contain("export function summarizeAccountPageStorefrontSupport(");
        accountPageContextSource.Should().Contain("result.memberRouteContext?.storefrontContext ??");
        accountPageContextSource.Should().Contain("result.publicRouteContext?.storefrontContext ??");
        accountPageContextSource.Should().Contain("return `session:${result.session ? \"present\" : \"missing\"}|storefront:missing`;");
        accountPageContextSource.Should().Contain("area: \"account-page-context\"");
        accountPageContextSource.Should().Contain("operation: \"load-page-context\"");
        accountPageContextSource.Should().Contain("memberRouteObservationContext(culture, \"/account\")");
        accountPageContextSource.Should().Contain("const session = await getMemberSession();");
        accountPageContextSource.Should().Contain("publicRouteContext: await getPublicAccountRouteContext(culture)");
        accountPageContextSource.Should().Contain("memberRouteContext: await getMemberDashboardRouteContext(culture)");

        accountPageViewSource.Should().Contain("buildAccountEntryView");
        accountPageViewSource.Should().Contain("const pageContext = await getAccountPageContext(culture);");
        accountPageViewSource.Should().Contain("return buildAccountEntryView({");
        accountPageViewSource.Should().Contain("returnPath,");
        accountPageViewSource.Should().Contain("...pageContext,");

        publicAuthPageContextSource.Should().Contain("function createPublicAuthPageLoader(");
        publicAuthPageContextSource.Should().Contain("area: \"public-auth-page-context\"");
        publicAuthPageContextSource.Should().Contain("operation: `load-${route.split(\"/\").at(-1)}-page-context`");
        publicAuthPageContextSource.Should().Contain("publicAuthPageStorefrontSupportFootprint:");
        publicAuthPageContextSource.Should().Contain("createStorefrontContinuationWithCartProps(");
        publicAuthPageContextSource.Should().Contain("const getCachedPublicSignInPageContext = createPublicAuthPageLoader(");
        publicAuthPageContextSource.Should().Contain("const getCachedPublicRegisterPageContext = createPublicAuthPageLoader(");
        publicAuthPageContextSource.Should().Contain("const getCachedPublicActivationPageContext = createPublicAuthPageLoader(");
        publicAuthPageContextSource.Should().Contain("const getCachedPublicPasswordPageContext = createPublicAuthPageLoader(");

        publicAuthRouteContextSource.Should().Contain("export function summarizePublicAuthStorefrontSupport(");
        publicAuthRouteContextSource.Should().Contain("return `cms:${storefront.cmsPagesStatus}:${storefront.cmsPages.length}|categories:${storefront.categoriesStatus}:${storefront.categories.length}|products:${storefront.productsStatus}:${storefront.products.length}|cart:${storefront.storefrontCartStatus}`;");
        publicAuthRouteContextSource.Should().Contain("normalizeArgs: normalizePublicAuthRouteArgs");
        publicAuthRouteContextSource.Should().Contain("operation: \"load-route-context\"");
        publicAuthRouteContextSource.Should().Contain("storefrontContext: await getPublicAuthStorefrontContext(culture)");
        publicAuthRouteContextSource.Should().Contain("return getCachedPublicAuthRouteContext(culture, \"/account\");");
        publicAuthRouteContextSource.Should().Contain("return getCachedPublicAuthRouteContext(culture, \"/account/sign-in\");");

        publicAuthSeoMetadataSource.Should().Contain("type PublicAuthRoute =");
        publicAuthSeoMetadataSource.Should().Contain("function getRouteTitle(culture: string, route: PublicAuthRoute)");
        publicAuthSeoMetadataSource.Should().Contain("return [culture.trim(), route.trim() as PublicAuthRoute];");
        publicAuthSeoMetadataSource.Should().Contain("area: \"public-auth-seo\"");
        publicAuthSeoMetadataSource.Should().Contain("metadata: buildNoIndexMetadata(culture, getRouteTitle(culture, route), undefined, route)");
        publicAuthSeoMetadataSource.Should().Contain("canonicalPath: route,");
        publicAuthSeoMetadataSource.Should().Contain("noIndex: true,");
        publicAuthSeoMetadataSource.Should().Contain("languageAlternates: {},");

        memberEntryContextSource.Should().Contain("kind: \"member-entry\"");
        memberEntryContextSource.Should().Contain("operation: \"load-entry-context\"");
        memberEntryContextSource.Should().Contain("normalizeArgs: normalizePublicAuthRouteArgs");
        memberEntryContextSource.Should().Contain("sharedContextFootprint: buildMemberEntryFootprint({");
        memberEntryContextSource.Should().Contain("memberEntryStorefrontSupportFootprint:");
        memberEntryContextSource.Should().Contain("storefrontContext: session");
        memberEntryContextSource.Should().Contain("? null");
        memberEntryContextSource.Should().Contain(": await getPublicAuthStorefrontContext(culture),");

        memberProtectedPageContextSource.Should().Contain("createMemberProtectedPageLoader({");
        memberProtectedPageContextSource.Should().Contain("operation: \"load-editor-page-context\"");
        memberProtectedPageContextSource.Should().Contain("operation: \"load-orders-page-context\"");
        memberProtectedPageContextSource.Should().Contain("operation: \"load-invoices-page-context\"");
        memberProtectedPageContextSource.Should().Contain("operation: \"load-order-detail-page-context\"");
        memberProtectedPageContextSource.Should().Contain("operation: \"load-invoice-detail-page-context\"");
        memberProtectedPageContextSource.Should().Contain("getEntryRoute: () => \"/orders\"");
        memberProtectedPageContextSource.Should().Contain("getEntryRoute: () => \"/invoices\"");
        memberProtectedPageContextSource.Should().Contain("detail: summarizeMemberDetailHealth(routeContext.orderResult)");
        memberProtectedPageContextSource.Should().Contain("detail: summarizeMemberDetailHealth(routeContext.invoiceResult)");

        memberSummaryContextSource.Should().Contain("export const getMemberIdentityContext = createSharedContextLoader({");
        memberSummaryContextSource.Should().Contain("operation: \"load-identity-context\"");
        memberSummaryContextSource.Should().Contain("Promise.all([");
        memberSummaryContextSource.Should().Contain("getCurrentMemberProfile()");
        memberSummaryContextSource.Should().Contain("getCurrentMemberPreferences()");
        memberSummaryContextSource.Should().Contain("getCurrentMemberCustomerContext()");
        memberSummaryContextSource.Should().Contain("getCurrentMemberAddresses()");
        memberSummaryContextSource.Should().Contain("export const getMemberCommerceSummaryContext = createSharedContextLoader({");
        memberSummaryContextSource.Should().Contain("pageSize: 3,");
        memberSummaryContextSource.Should().Contain("getCurrentMemberLoyaltyOverview()");
        memberSummaryContextSource.Should().Contain("normalizeArgs: normalizePagingArgs");
        memberSummaryContextSource.Should().Contain("collection: \"orders\",");
        memberSummaryContextSource.Should().Contain("collection: \"invoices\",");

        activationRecoveryPanelSource.Should().Contain("requestEmailConfirmationAction");
        activationRecoveryPanelSource.Should().Contain("buildLocalizedAuthHref(");
        activationRecoveryPanelSource.Should().Contain("\"/account/activation\"");
        activationRecoveryPanelSource.Should().Contain("compact = false");
        activationRecoveryPanelSource.Should().Contain("name=\"returnPath\" value={returnPath || \"/account\"}");
        activationRecoveryPanelSource.Should().Contain("copy.activationRecoveryResendCta");
        activationRecoveryPanelSource.Should().Contain("copy.activationRecoveryOpenFlowCta");

        publicAuthCompositionWindowSource.Should().Contain("sortProductsByOpportunity(products)[0] ?? null;");
        publicAuthCompositionWindowSource.Should().Contain("buildPromotionLaneRouteMapItem({");
        publicAuthCompositionWindowSource.Should().Contain("const extendedRouteMapItems = [");
        publicAuthCompositionWindowSource.Should().Contain("copy.publicAuthCompositionRouteMapContentLabel");
        publicAuthCompositionWindowSource.Should().Contain("copy.publicAuthCompositionRouteMapCatalogLabel");
        publicAuthCompositionWindowSource.Should().Contain("localizeHref(card.href, culture)");
        publicAuthCompositionWindowSource.Should().Contain("localizeHref(item.href, culture)");

        publicAuthContinuationSource.Should().Contain("PublicContinuationRail");
        publicAuthContinuationSource.Should().Contain("StorefrontCampaignBoard");
        publicAuthContinuationSource.Should().Contain("buildStorefrontSpotlightSelections({");
        publicAuthContinuationSource.Should().Contain("summarizeCatalogPromotionLanes(productOpportunities).map(");
        publicAuthContinuationSource.Should().Contain("id: \"auth-cart\"");
        publicAuthContinuationSource.Should().Contain("id: \"auth-home\"");
        publicAuthContinuationSource.Should().Contain("copy.publicAuthCampaignTitle");
        publicAuthContinuationSource.Should().Contain("copy.publicAuthPromotionLaneSectionTitle");
        publicAuthContinuationSource.Should().Contain("formatResource(copy.publicAuthStorefrontWindowMessage, {");

        publicAuthReturnSummarySource.Should().Contain("function resolveReturnContext(");
        publicAuthReturnSummarySource.Should().Contain("returnPath.startsWith(\"/checkout\")");
        publicAuthReturnSummarySource.Should().Contain("returnPath.startsWith(\"/orders\")");
        publicAuthReturnSummarySource.Should().Contain("returnPath.startsWith(\"/loyalty\")");
        publicAuthReturnSummarySource.Should().Contain("const safeReturnPath = sanitizeAppPath(returnPath, \"/account\");");
        publicAuthReturnSummarySource.Should().Contain("const localizedReturnHref = localizeHref(safeReturnPath, culture);");
        publicAuthReturnSummarySource.Should().Contain("const localizedCartHref = localizeHref(\"/cart\", culture);");
        publicAuthReturnSummarySource.Should().Contain("safeReturnPath !== \"/cart\"");

        memberAuthRequiredSource.Should().Contain("sanitizeAppPath(returnPath, \"/account\")");
        memberAuthRequiredSource.Should().Contain("buildLocalizedAuthHref(\"/account/sign-in\", safeReturnPath, culture)");
        memberAuthRequiredSource.Should().Contain("buildLocalizedAuthHref(\"/account/register\", safeReturnPath, culture)");
        memberAuthRequiredSource.Should().Contain("MemberCrossSurfaceRail");
        memberAuthRequiredSource.Should().Contain("includeOrders={false}");
        memberAuthRequiredSource.Should().Contain("includeInvoices={false}");
        memberAuthRequiredSource.Should().Contain("includeLoyalty={false}");
        memberAuthRequiredSource.Should().Contain("PublicAuthContinuation");
        memberAuthRequiredSource.Should().Contain("StorefrontOfferBoard");
        memberAuthRequiredSource.Should().Contain("StorefrontCampaignBoard");

        memberCrossSurfaceRailSource.Should().Contain("includeAccount = true");
        memberCrossSurfaceRailSource.Should().Contain("includeOrders = false");
        memberCrossSurfaceRailSource.Should().Contain("includeInvoices = false");
        memberCrossSurfaceRailSource.Should().Contain("includeLoyalty = true");
        memberCrossSurfaceRailSource.Should().Contain("id: \"member-home\"");
        memberCrossSurfaceRailSource.Should().Contain("id: \"member-catalog\"");
        memberCrossSurfaceRailSource.Should().Contain("id: \"member-account\"");
        memberCrossSurfaceRailSource.Should().Contain("id: \"member-orders\"");
        memberCrossSurfaceRailSource.Should().Contain("id: \"member-invoices\"");
        memberCrossSurfaceRailSource.Should().Contain("id: \"member-loyalty\"");
        memberCrossSurfaceRailSource.Should().Contain("PublicContinuationRail");
        memberCrossSurfaceRailSource.Should().Contain("title={copy.memberCrossSurfaceRailTitle}");
    }
}

