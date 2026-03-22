using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Darwin.Tests.Common.TestInfrastructure;

/// <summary>
///     Provides helper utilities for creating a consistently configured
///     integration host factory in the Testing environment.
/// </summary>
public static class IntegrationTestHostFactory
{
    /// <summary>
    ///     Creates a derived <see cref="WebApplicationFactory{TEntryPoint}"/> that forces
    ///     ASP.NET Core environment to <c>Testing</c> for deterministic integration behavior.
    /// </summary>
    /// <param name="factory">Base factory provided by xUnit fixture.</param>
    /// <returns>Factory configured with <c>UseEnvironment("Testing")</c>.</returns>
    public static WebApplicationFactory<Program> CreateTestingFactory(WebApplicationFactory<Program> factory)
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                // English comments required by your rule
                config.AddJsonFile("appsettings.Testing.json", optional: true, reloadOnChange: false);
                config.AddJsonFile("appsettings.Testing.Development.json", optional: true, reloadOnChange: false);
                config.AddEnvironmentVariables();
            });
        });
    }
}
