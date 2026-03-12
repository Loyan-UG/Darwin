using Microsoft.AspNetCore.Mvc.Testing;

namespace Darwin.Tests.Integration.TestInfrastructure;

/// <summary>
///     Centralizes creation of HTTPS clients for integration tests so status-code
///     assertions remain deterministic and unaffected by HTTP-to-HTTPS redirects.
/// </summary>
public static class IntegrationTestClientFactory
{
    /// <summary>
    ///     Creates an HTTPS client bound to localhost for a given test host factory.
    /// </summary>
    /// <param name="factory">Web application factory configured for integration tests.</param>
    /// <returns>Configured HTTPS HttpClient instance.</returns>
    public static HttpClient CreateHttpsClient(WebApplicationFactory<Program> factory)
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }
}
