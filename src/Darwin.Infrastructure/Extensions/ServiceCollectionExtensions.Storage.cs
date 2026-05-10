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
        services.AddScoped<FileSystemObjectStorageService>();
        services.AddScoped<AzureBlobObjectStorageService>();
        services.AddScoped<IObjectStorageService, ObjectStorageServiceRouter>();
        return services;
    }
}
