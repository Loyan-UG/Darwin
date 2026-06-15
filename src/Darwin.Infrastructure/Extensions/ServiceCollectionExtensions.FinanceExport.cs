using Darwin.Application.Abstractions.Storage;
using Darwin.Application.Billing.Services;
using Darwin.Infrastructure.Billing;
using Darwin.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Infrastructure.Extensions;

public static class ServiceCollectionExtensionsFinanceExport
{
    public static IServiceCollection AddFinanceExportFileDeliveryAdapterIfConfigured(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var profileSection = configuration.GetSection($"{ObjectStorageOptions.SectionName}:Profiles:{FinanceExportFileDeliveryAdapter.ProfileName}");
        var provider = profileSection["Provider"];
        if (Enum.TryParse<ObjectStorageProviderKind>(provider, ignoreCase: true, out var providerKind) &&
            providerKind is ObjectStorageProviderKind.FileSystem or ObjectStorageProviderKind.S3Compatible or ObjectStorageProviderKind.AzureBlob)
        {
            services.AddScoped<IFinanceExportConnectorAdapter, FinanceExportFileDeliveryAdapter>();
        }

        return services;
    }
}
