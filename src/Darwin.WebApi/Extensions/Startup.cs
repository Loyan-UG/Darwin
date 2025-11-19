using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Swashbuckle.AspNetCore.SwaggerUI;
using Swashbuckle.AspNetCore.Swagger;

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
        public static WebApplication UseWebApiStartup(this WebApplication app)
        {
            if (app is null) throw new ArgumentNullException(nameof(app));

            var env = app.Environment;

            if (env.IsDevelopment())
            {
                // Developer-friendly exception page and Swagger UI in development.
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(); // This requires Swashbuckle.AspNetCore.SwaggerUI package and using directive
            }
            else
            {
                // In production, use a generic exception handler and HSTS.
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            return app;
        }
    }
}
