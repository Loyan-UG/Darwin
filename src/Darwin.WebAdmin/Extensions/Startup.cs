using System.Globalization;
using Darwin.Infrastructure.Extensions;
using Darwin.Infrastructure.Media;
using Darwin.WebAdmin.Localization;
using Darwin.WebAdmin.Services.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Darwin.WebAdmin.Extensions
{
    /// <summary>
    ///     Web application startup extensions that configure the ASP.NET Core middleware pipeline
    ///     and endpoint routing for the Darwin WebAdmin entrypoint. This keeps <c>Program.cs</c> lean by
    ///     centralizing cross-cutting web concerns (exception handling, static files, localization,
    ///     authentication/authorization, and controller routing) in a dedicated extension.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Responsibilities:
    ///         <list type="bullet">
    ///             <item>Register global exception pages and developer-friendly error handling in Development.</item>
    ///             <item>Enable static file serving for admin assets and WYSIWYG dependencies (e.g., Quill) via <c>UseStaticFiles()</c>.</item>
    ///             <item>Configure request localization (culture, UI culture) according to <c>SiteSetting</c> defaults.</item>
    ///             <item>Apply authentication/authorization middleware when identity is enabled.</item>
    ///             <item>Map the back-office root route so the dashboard opens directly at <c>/</c>.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Ordering:
    ///         <list type="bullet">
    ///             <item>Place exception handling early, static files before routing, and auth before endpoints.</item>
    ///             <item>When enabling APIs, ensure ProblemDetails and CORS are wired in correct order.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Startup Tasks:
    ///         Consider invoking database migration and seeding here (once) to keep the app self-bootstrapping for dev/test.
    ///     </para>
    /// </remarks>
    public static class Startup
    {
        public static async Task UseWebStartupAsync(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                // Apply migrations + seed before any middleware reads settings from a fresh database.
                await app.Services.MigrateAndSeedAsync(
                    app.Configuration.GetValue("DatabaseStartup:ApplyMigrations", true),
                    app.Configuration.GetValue("DatabaseStartup:Seed", true));
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            var localizationSettings = await LoadLocalizationSettingsAsync(app.Services);
            var requestLocalizationOptions = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(localizationSettings.DefaultCulture),
                SupportedCultures = localizationSettings.SupportedCultures,
                SupportedUICultures = localizationSettings.SupportedCultures
            };
            requestLocalizationOptions.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
            requestLocalizationOptions.RequestCultureProviders.Insert(1, new CookieRequestCultureProvider());
            app.UseForwardedHeaders();
            app.UseRequestLocalization(requestLocalizationOptions);

            app.UseHttpsRedirection();
            app.UseWebAdminSecurityHeaders();
            app.UseMediaStaticFiles();
            app.UseStaticFiles(BuildStaticFileOptions());

            app.UseRouting();
            app.UseAuthentication();
            app.UseAdminTextOverrides();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            });
            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            });
        }

        private static async Task<(CultureInfo[] SupportedCultures, string DefaultCulture)> LoadLocalizationSettingsAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var siteSettingCache = scope.ServiceProvider.GetRequiredService<ISiteSettingCache>();
            var settings = await siteSettingCache.GetAsync().ConfigureAwait(false);
            var cultureNames = AdminCultureCatalog.NormalizeSupportedCultureNames(settings.SupportedCulturesCsv);

            var supportedCultures = cultureNames.Select(static x => new CultureInfo(x)).ToArray();
            var configuredDefaultCulture = AdminCultureCatalog.NormalizeUiCulture(settings.DefaultCulture);
            var defaultCulture = supportedCultures.FirstOrDefault(x => string.Equals(x.Name, configuredDefaultCulture, StringComparison.OrdinalIgnoreCase))?.Name
                                 ?? supportedCultures[0].Name;
            return (supportedCultures, defaultCulture);
        }

        private static void UseAdminTextOverrides(this WebApplication app)
        {
            app.Use(async (context, next) =>
            {
                var siteSettingCache = context.RequestServices.GetRequiredService<ISiteSettingCache>();
                var settings = await siteSettingCache.GetAsync(context.RequestAborted).ConfigureAwait(false);
                context.Items[AdminTextOverrideRequestContext.PlatformOverridesItemKey] =
                    AdminTextOverrideCatalog.Parse(settings.AdminTextOverridesJson);

                IFormCollection? form = null;
                if (CanReadBusinessOverrideForm(context.Request))
                {
                    form = await context.Request.ReadFormAsync(context.RequestAborted).ConfigureAwait(false);
                }

                var businessId = AdminTextOverrideRequestContext.TryResolveCurrentBusinessId(context, form);
                if (businessId.HasValue)
                {
                    var businessSettingsCache = context.RequestServices.GetRequiredService<IBusinessEffectiveSettingsCache>();
                    var businessSettings = await businessSettingsCache.GetAsync(businessId.Value, context.RequestAborted).ConfigureAwait(false);
                    context.Items[AdminTextOverrideRequestContext.BusinessOverridesItemKey] =
                        AdminTextOverrideCatalog.Parse(businessSettings?.AdminTextOverridesJson);
                }
                else
                {
                    context.Items[AdminTextOverrideRequestContext.BusinessOverridesItemKey] =
                        AdminTextOverrideCatalog.Empty;
                }

                await next().ConfigureAwait(false);
            });
        }

        private static bool CanReadBusinessOverrideForm(HttpRequest request)
        {
            return request.HttpContext.User?.Identity?.IsAuthenticated == true &&
                   request.ContentLength is > 0 and <= 16 * 1024 &&
                   request.HasFormContentType &&
                   request.ContentType?.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static StaticFileOptions BuildStaticFileOptions()
        {
            return new StaticFileOptions
            {
                OnPrepareResponse = context =>
                {
                    if (context.Context.Request.Path.StartsWithSegments("/uploads", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
                    }
                }
            };
        }

        private static void UseMediaStaticFiles(this WebApplication app)
        {
            var options = app.Services.GetRequiredService<IOptions<MediaStorageOptions>>().Value;
            var uploadsRoot = MediaStoragePathResolver.ResolveRootPath(app.Environment.ContentRootPath, options);
            Directory.CreateDirectory(uploadsRoot);

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(uploadsRoot),
                RequestPath = MediaStoragePathResolver.NormalizeRequestPath(options.RequestPath),
                OnPrepareResponse = PrepareUploadStaticFileResponse
            });
        }

        private static void PrepareUploadStaticFileResponse(StaticFileResponseContext context)
        {
            context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        }

        private static void UseWebAdminSecurityHeaders(this WebApplication app)
        {
            var mediaImgSource = ResolveMediaImgSource(app.Services);
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    var headers = context.Response.Headers;
                    var scriptSrc = app.Environment.IsDevelopment()
                        ? "script-src 'self' 'unsafe-inline'"
                        : "script-src 'self'";
                    var connectSrc = app.Environment.IsDevelopment()
                        ? "connect-src 'self' http://localhost:* https://localhost:* ws://localhost:* wss://localhost:*"
                        : "connect-src 'self'";

                    headers.TryAdd("Content-Security-Policy", string.Join("; ",
                        "default-src 'self'",
                        scriptSrc,
                        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
                        $"img-src 'self' data: blob:{mediaImgSource}",
                        "font-src 'self' https://fonts.gstatic.com",
                        connectSrc,
                        "media-src 'self' blob:",
                        "object-src 'none'",
                        "base-uri 'self'",
                        "form-action 'self'",
                        "frame-ancestors 'none'"));
                    headers.TryAdd("X-Content-Type-Options", "nosniff");
                    headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
                    headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=(), payment=()");
                    return Task.CompletedTask;
                });

                await next().ConfigureAwait(false);
            });
        }

        private static string ResolveMediaImgSource(IServiceProvider services)
        {
            var options = services.GetRequiredService<IOptions<MediaStorageOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.PublicBaseUrl) ||
                !Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return string.Empty;
            }

            return $" {uri.GetLeftPart(UriPartial.Authority)}";
        }
    }
}
