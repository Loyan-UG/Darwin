using System.Globalization;
using Darwin.Infrastructure.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Darwin.Web.Extensions
{
    /// <summary>
    ///     Web application startup extensions that configure the ASP.NET Core middleware pipeline
    ///     and endpoint routing for the Darwin Web entrypoint. This keeps <c>Program.cs</c> lean by
    ///     centralizing cross-cutting web concerns (exception handling, static files, localization,
    ///     authentication/authorization, and area routing) in a dedicated extension.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Responsibilities:
    ///         <list type="bullet">
    ///             <item>Register global exception pages and developer-friendly error handling in Development.</item>
    ///             <item>Enable static file serving for admin assets and WYSIWYG dependencies (e.g., Quill) via <c>UseStaticFiles()</c>.</item>
    ///             <item>Configure request localization (culture, UI culture) according to <c>SiteSetting</c> defaults.</item>
    ///             <item>Apply authentication/authorization middleware when identity is enabled.</item>
    ///             <item>Map area routes (<c>/Admin</c>) and default routes (public site in the future).</item>
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
            var supportedCultures = new[] { new CultureInfo("de-DE"), new CultureInfo("en-US") };
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("de-DE"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

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

            // Areas routing (Admin)
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

            // Public routing
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            await Task.CompletedTask;
        }
    }
}
