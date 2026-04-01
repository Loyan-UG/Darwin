using System.Globalization;
using Darwin.Infrastructure.Extensions;
using Darwin.WebAdmin.Localization;
using Darwin.WebAdmin.Services.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            var localizationSettings = await LoadLocalizationSettingsAsync(app.Services);
            var requestLocalizationOptions = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(localizationSettings.DefaultCulture),
                SupportedCultures = localizationSettings.SupportedCultures,
                SupportedUICultures = localizationSettings.SupportedCultures
            };
            requestLocalizationOptions.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
            requestLocalizationOptions.RequestCultureProviders.Insert(1, new CookieRequestCultureProvider());
            app.UseRequestLocalization(requestLocalizationOptions);

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                // Apply migrations + seed in dev
                await app.Services.MigrateAndSeedAsync();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        }

        private static async Task<(CultureInfo[] SupportedCultures, string DefaultCulture)> LoadLocalizationSettingsAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var siteSettingCache = scope.ServiceProvider.GetRequiredService<ISiteSettingCache>();
            var settings = await siteSettingCache.GetAsync().ConfigureAwait(false);
            var cultureNames = AdminCultureCatalog.NormalizeSupportedCultureNames(settings.SupportedCulturesCsv);

            var supportedCultures = cultureNames.Select(static x => new CultureInfo(x)).ToArray();
            var defaultCulture = supportedCultures.FirstOrDefault(static x => string.Equals(x.Name, AdminCultureCatalog.DefaultCulture, StringComparison.OrdinalIgnoreCase))?.Name
                                 ?? supportedCultures[0].Name;
            return (supportedCultures, defaultCulture);
        }
    }
}
