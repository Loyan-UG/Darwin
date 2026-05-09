using Darwin.Application.Abstractions.Compliance;
using Darwin.Infrastructure.Compliance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Extensions;

public static class ServiceCollectionExtensionsCompliance
{
    public static IServiceCollection AddComplianceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ViesVatValidationOptions>(configuration.GetSection("Compliance:VatValidation:Vies"));
        services.AddHttpClient<IVatValidationProvider, ViesVatValidationProvider>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ViesVatValidationOptions>>().Value;
            var endpointUrl = string.IsNullOrWhiteSpace(options.EndpointUrl)
                ? "https://ec.europa.eu/taxation_customs/vies/services/checkVatService"
                : options.EndpointUrl.Trim();

            client.BaseAddress = new Uri(endpointUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 60));
        });

        return services;
    }
}
