using Darwin.Mobile.Business.Services.Identity;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Extensions;
using Darwin.Mobile.Shared.Integration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;
using System;
using System.IO;
using System.Reflection;

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


        if (string.IsNullOrWhiteSpace(apiOptions.BaseUrl))
        {
            // Fail fast with a clear message instead of hitting HttpClient invalid URI errors later.
            throw new InvalidOperationException(
                "Api:BaseUrl is empty after configuration binding. " +
                "Check Resources/Raw/appsettings.mobile.json and rebuild/reinstall the app.");
        }

        // Ensure app role is explicitly set for client-side validation (defensive).
        apiOptions.AppRole = MobileAppRole.Business;

        services.AddDarwinMobileShared(apiOptions);

        services.AddSingleton<IScanner, Services.Platform.ScannerPlatformService>();
        services.AddSingleton<ILocation, Services.Platform.LocationPlatformService>();

        // Business identity context for Home dashboard
        services.AddSingleton<IBusinessIdentityContextService, BusinessIdentityContextService>();

        services.AddTransient<ViewModels.HomeViewModel>();
        services.AddTransient<Views.HomePage>();

        services.AddTransient<ViewModels.LoginViewModel>();
        services.AddTransient<Views.LoginPage>();

        services.AddTransient<ViewModels.ScannerViewModel>();
        services.AddTransient<Views.ScannerPage>();

        services.AddTransient<Views.ComingSoonPage>();

        services.AddTransient<ViewModels.SessionViewModel>();
        services.AddTransient<Views.SessionPage>();

        // Settings
        services.AddTransient<ViewModels.SettingsViewModel>();
        services.AddTransient<Views.SettingsPage>();

        services.AddTransient<ViewModels.ProfileViewModel>();
        services.AddTransient<Views.ProfilePage>();

        services.AddTransient<ViewModels.ChangePasswordViewModel>();
        services.AddTransient<Views.ChangePasswordPage>();

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