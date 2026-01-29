using System;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Extensions;
using Darwin.Mobile.Shared.Integration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Mobile.Business.Extensions;

/// <summary>
/// Composes Business app-specific services: configuration binding, shared mobile services,
/// platform implementations for scanning/location, plus pages and view models.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Business app services and pages into the dependency injection container.
    /// </summary>
    public static IServiceCollection AddBusinessApp(this IServiceCollection services)
    {
        // 1) Load appsettings.mobile.json from output directory (Content copied to bin).
        var basePath = AppContext.BaseDirectory;
        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.mobile.json", optional: false, reloadOnChange: false)
#if DEBUG
            .AddJsonFile("appsettings.mobile.Development.json", optional: true, reloadOnChange: false)
#endif
            .Build();

        var apiOptions = config.GetSection("Api").Get<ApiOptions>()
            ?? throw new InvalidOperationException("Missing 'Api' section in appsettings.mobile.json");

        // 2) Shared services: HttpClient + retry + Auth/Loyalty/Profile + navigation + token store
        services.AddDarwinMobileShared(apiOptions);

        // 3) Platform services: scanner & location
        services.AddSingleton<IScanner, Services.Platform.ScannerPlatformService>();
        services.AddSingleton<ILocation, Services.Platform.LocationPlatformService>();

        // 4) Register all pages and view models. Use transient for pages/viewmodels to avoid stale state.
        services.AddTransient<ViewModels.HomeViewModel>();
        services.AddTransient<Views.HomePage>();

        services.AddTransient<ViewModels.LoginViewModel>();
        services.AddTransient<Views.LoginPage>();

        services.AddTransient<ViewModels.ScannerViewModel>();
        services.AddTransient<Views.ScannerPage>();

        services.AddTransient<Views.ComingSoonPage>();

        services.AddTransient<ViewModels.SessionViewModel>();
        services.AddTransient<Views.SessionPage>();

        services.AddTransient<ViewModels.SessionViewModel>();
        services.AddTransient<Views.SessionPage>();


        return services;
    }
}
