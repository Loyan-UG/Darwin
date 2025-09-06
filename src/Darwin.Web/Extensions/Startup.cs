using System.Globalization;
using Darwin.Infrastructure.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Darwin.Web.Extensions
{
    /// <summary>
    /// Centralized pipeline & routing setup to keep Program.cs tidy.
    /// </summary>
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
