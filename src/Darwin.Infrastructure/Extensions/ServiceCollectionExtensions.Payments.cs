using Darwin.Application.Abstractions.Payments;
using Darwin.Infrastructure.Payments;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Infrastructure.Extensions;

public static class ServiceCollectionExtensionsPayments
{
    public static IServiceCollection AddPaymentProviderInfrastructure(this IServiceCollection services)
    {
        services.AddHttpClient<IRefundProviderClient, StripeRefundProviderClient>();
        return services;
    }
}
