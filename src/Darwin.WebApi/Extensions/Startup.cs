using Darwin.Infrastructure.Extensions;
using Darwin.WebApi.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

            if (!env.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();

            // Apply ASP.NET Core rate limiting policies before authentication,
            // so rejected requests are short-circuited early.
            app.UseRateLimiter();

            // Idempotency middleware: prevents duplicate processing of mutating requests
            app.UseMiddleware<Darwin.WebApi.Middleware.IdempotencyMiddleware>();

            app.UseAuthentication();

            app.UseMiddleware<ErrorHandlingMiddleware>();

            app.UseAuthorization();

            app.MapControllers();

            return app;
        }
    }
}
