using System;
using System.IO;
using System.Reflection;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Extensions;
using Darwin.Mobile.Shared.Integration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;

namespace Darwin.Mobile.Business.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessApp(this IServiceCollection services)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFileFromMauiAsset("appsettings.mobile.json", optional: false)
#if DEBUG
            .AddJsonFileFromMauiAsset("appsettings.mobile.Development.json", optional: true)
#endif
            .Build();

        var apiOptions = config.GetSection("Api").Get<ApiOptions>()
            ?? throw new InvalidOperationException("Missing 'Api' section in appsettings.mobile.json");

        services.AddDarwinMobileShared(apiOptions);

        services.AddSingleton<IScanner, Services.Platform.ScannerPlatformService>();
        services.AddSingleton<ILocation, Services.Platform.LocationPlatformService>();

        services.AddTransient<ViewModels.HomeViewModel>();
        services.AddTransient<Views.HomePage>();

        services.AddTransient<ViewModels.LoginViewModel>();
        services.AddTransient<Views.LoginPage>();

        services.AddTransient<ViewModels.ScannerViewModel>();
        services.AddTransient<Views.ScannerPage>();

        services.AddTransient<Views.ComingSoonPage>();

        services.AddTransient<ViewModels.SessionViewModel>();
        services.AddTransient<Views.SessionPage>();

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