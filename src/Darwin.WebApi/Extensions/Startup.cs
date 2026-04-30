using Darwin.Infrastructure.Extensions;
using Darwin.WebApi.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Darwin.WebApi.Extensions
{
    /// <summary>
    ///     WebApi startup extensions that configure the ASP.NET Core middleware
    ///     pipeline and endpoint routing. This keeps Program.cs lean and centralizes
    ///     cross-cutting concerns (exception handling, HTTPS redirection, auth,
    ///     Swagger, routing) in a single place.
    /// </summary>
    public static class Startup
    {
        /// <summary>
        ///     Configures the HTTP request pipeline for the Darwin WebApi host.
        ///     Call this once from Program.cs after building the <see cref="WebApplication"/>.
        /// </summary>
        /// <param name="app">The configured <see cref="WebApplication"/> instance.</param>
        /// <returns>The same <see cref="WebApplication"/> for chaining (if desired).</returns>
        public static async Task<WebApplication> UseWebApiStartupAsync(this WebApplication app)
        {
            if (app is null) throw new ArgumentNullException(nameof(app));

            var env = app.Environment;

            if (env.IsDevelopment())
            {
                // Developer-friendly exception page, DB bootstrap, and Swagger UI in development.
                app.UseDeveloperExceptionPage();
                await app.Services.MigrateAndSeedAsync(
                    app.Configuration.GetValue("DatabaseStartup:ApplyMigrations", true),
                    app.Configuration.GetValue("DatabaseStartup:Seed", true));
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                // In production, use a generic exception handler and HSTS.
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }

            app.UseForwardedHeaders();

            if (!env.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();

            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseRequestTimeouts();

            // Apply ASP.NET Core rate limiting policies before authentication,
            // so rejected requests are short-circuited early.
            app.UseRateLimiter();

            app.UseAuthentication();

            // Idempotency middleware scopes duplicate detection to the authenticated
            // principal, route, and request body before authorization executes handlers.
            app.UseMiddleware<Darwin.WebApi.Middleware.IdempotencyMiddleware>();

            app.UseAuthorization();

            app.MapControllers();
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            });
            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            });

            return app;
        }
    }
}
