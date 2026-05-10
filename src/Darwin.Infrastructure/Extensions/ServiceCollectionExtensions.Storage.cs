using Darwin.Infrastructure.Storage;
using Darwin.Application.Abstractions.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Extensions;

public static class ServiceCollectionExtensionsStorage
{
    public static IServiceCollection AddObjectStorageInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<ObjectStorageOptions>, ObjectStorageOptionsValidator>();
        services.AddOptions<ObjectStorageOptions>()
            .Bind(configuration.GetSection(ObjectStorageOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<ObjectStorageCapabilityReporter>();
        services.AddScoped<S3CompatibleObjectStorageService>();
        services.AddScoped<IObjectStorageService>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ObjectStorageOptions>>().Value;
            return options.Provider switch
            {
                ObjectStorageProviderKind.S3Compatible => serviceProvider.GetRequiredService<S3CompatibleObjectStorageService>(),
                _ => throw new InvalidOperationException(
                    $"Generic object storage service is not implemented for provider '{options.Provider}'. Use invoice archive fallback providers or configure ObjectStorage:Provider=S3Compatible.")
            };
        });
        return services;
    }
}
