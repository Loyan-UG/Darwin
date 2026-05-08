using Darwin.Application.Abstractions.Shipping;
using Darwin.Infrastructure.Shipping.Dhl;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Infrastructure.Extensions;

public static class ServiceCollectionExtensionsShippingProviders
{
    public static IServiceCollection AddShippingProviderInfrastructure(this IServiceCollection services)
    {
        services.AddHttpClient<IDhlShipmentProviderClient, DhlShipmentProviderClient>();
        services.AddScoped<IShipmentLabelStorage, FileSystemShipmentLabelStorage>();
        return services;
    }
}
