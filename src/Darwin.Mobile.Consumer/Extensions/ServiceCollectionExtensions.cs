using Darwin.Mobile.Consumer.ViewModels;
using Darwin.Mobile.Consumer.Views;
using Darwin.Mobile.Consumer.Services.Navigation;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Extensions;
using Darwin.Mobile.Shared.Integration;
using Microsoft.Extensions.Configuration;


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

        if (string.IsNullOrWhiteSpace(apiOptions.BaseUrl))
        {
            // Fail fast with a clear message instead of hitting HttpClient invalid URI errors later.
            throw new InvalidOperationException(
                "Api:BaseUrl is empty after configuration binding. " +
                "Check Resources/Raw/appsettings.mobile.json and rebuild/reinstall the app.");
        }


        // Ensure app role is explicitly set for client-side validation (defensive).
        apiOptions.AppRole = MobileAppRole.Consumer;

        // Register shared services (ApiClient, AuthService, LoyaltyService, etc.)
        services.AddDarwinMobileShared(apiOptions);

        // Root navigation service for window-aware app root switching.
        services.AddSingleton<IAppRootNavigator, AppRootNavigator>();

        // Platform services (scanner, location)
        services.AddSingleton<IScanner, Services.Platform.ScannerPlatformService>();
        services.AddSingleton<ILocation, Services.Platform.LocationPlatformService>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<QrViewModel>();
        services.AddTransient<DiscoverViewModel>();
        services.AddTransient<RewardsViewModel>();
        services.AddTransient<ProfileViewModel>();
        //services.AddTransient<SettingsViewModel>();
        services.AddTransient<ChangePasswordViewModel>();
        services.AddTransient<BusinessDetailViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<ForgotPasswordViewModel>();

        // Pages
        services.AddTransient<LoginPage>();
        services.AddTransient<QrPage>();
        services.AddTransient<DiscoverPage>();
        services.AddTransient<RewardsPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<ProfilePage>();
        services.AddTransient<RegisterPage>();
        services.AddTransient<ForgotPasswordPage>();
        services.AddTransient<ChangePasswordPage>();
        services.AddTransient<BusinessDetailPage>();


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
