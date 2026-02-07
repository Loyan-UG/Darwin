using System;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Extensions;
using Darwin.Mobile.Shared.Integration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Mobile.Consumer.Extensions;

/// <summary>
/// Composes Consumer app-specific services: configuration binding, shared mobile services,
/// platform scanning/location, and pages/view models.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Consumer app services and pages into DI.
    /// </summary>
    public static IServiceCollection AddConsumerApp(this IServiceCollection services)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFileFromMauiAsset("appsettings.mobile.json", optional: false)
         #if DEBUG
            .AddJsonFileFromMauiAsset("appsettings.mobile.Development.json", optional: true)
        #endif
            .Build();

        var apiOptions = config.GetSection("Api").Get<ApiOptions>()
            ?? throw new InvalidOperationException("Missing 'Api' section in appsettings.mobile.json");

        // Register shared services (ApiClient, AuthService, LoyaltyService, etc.)
        services.AddDarwinMobileShared(apiOptions);

        // Platform services (scanner, location)
        services.AddSingleton<IScanner, Services.Platform.ScannerPlatformService>();
        services.AddSingleton<ILocation, Services.Platform.LocationPlatformService>();

        // ViewModels
        services.AddTransient<ViewModels.LoginViewModel>();
        services.AddTransient<ViewModels.QrViewModel>();
        services.AddTransient<ViewModels.DiscoverViewModel>();
        services.AddTransient<ViewModels.RewardsViewModel>();
        services.AddTransient<ViewModels.ProfileViewModel>();

        // Pages
        services.AddTransient<Views.LoginPage>();
        services.AddTransient<Views.QrPage>();
        services.AddTransient<Views.DiscoverPage>();
        services.AddTransient<Views.RewardsPage>();
        services.AddTransient<Views.ProfilePage>();

        return services;
    }


    private static IConfigurationBuilder AddJsonFileFromMauiAsset(
        this IConfigurationBuilder builder,
        string assetName,
        bool optional)
    {
        try
        {
            using var assetStream = FileSystem
                .OpenAppPackageFileAsync(assetName)
                .GetAwaiter()
                .GetResult();

            var ms = new MemoryStream();
            assetStream.CopyTo(ms);
            ms.Position = 0;

            return builder.AddJsonStream(ms); // Configuration will dispose ms when done
        }
        catch (FileNotFoundException)
        {
            if (optional) return builder;
            throw;
        }
    }
}
