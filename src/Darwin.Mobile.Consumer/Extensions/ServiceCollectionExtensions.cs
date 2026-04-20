using Darwin.Mobile.Consumer.Services.Caching;
using Darwin.Mobile.Consumer.Services.Navigation;
using Darwin.Mobile.Consumer.Services.Notifications;
using Darwin.Mobile.Consumer.Services.Startup;
using Darwin.Mobile.Consumer.ViewModels;
using Darwin.Mobile.Consumer.Views;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Configuration;
using Darwin.Mobile.Shared.Extensions;
using Darwin.Mobile.Shared.Integration;
using Microsoft.Extensions.Configuration;

namespace Darwin.Mobile.Consumer.Extensions;

/// <summary>
/// Composes Consumer app-specific services: configuration binding, shared mobile services,
/// platform scanning/location, startup warmup, and pages/view models.
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
            .AddJsonFileFromMauiAsset($"appsettings.mobile.{ResolveEnvironmentName()}.json", optional: true)
            .Build();

        services.AddSingleton<IConfiguration>(config);

        var apiOptions = config.GetSection("Api").Get<ApiOptions>()
            ?? throw new InvalidOperationException("Missing 'Api' section in appsettings.mobile.json");

        var legalLinksOptions = config.GetSection("LegalLinks").Get<LegalLinksOptions>()
            ?? throw new InvalidOperationException("Missing 'LegalLinks' section in appsettings.mobile.json");

        if (string.IsNullOrWhiteSpace(apiOptions.BaseUrl))
        {
            throw new InvalidOperationException(
                "Api:BaseUrl is empty after configuration binding. " +
                "Check Resources/Raw/appsettings.mobile.json and rebuild/reinstall the app.");
        }

        apiOptions.AppRole = MobileAppRole.Consumer;

        services.AddDarwinMobileShared(apiOptions, legalLinksOptions);

        services.AddSingleton<IAppRootNavigator, AppRootNavigator>();
        services.AddSingleton<IConsumerLoyaltySnapshotCache, ConsumerLoyaltySnapshotCache>();
        services.AddSingleton<IConsumerStartupWarmupCoordinator, ConsumerStartupWarmupCoordinator>();
        services.AddSingleton<IConsumerPushTokenProvider, ConsumerPlatformPushTokenProvider>();
        services.AddSingleton<IConsumerPushRegistrationCoordinator, ConsumerPushRegistrationCoordinator>();
        services.AddSingleton<IConsumerNotificationPermissionService, ConsumerNotificationPermissionService>();

        services.AddSingleton<IScanner, Services.Platform.ScannerPlatformService>();
        services.AddSingleton<ILocation, Services.Platform.LocationPlatformService>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<QrViewModel>();
        services.AddTransient<DiscoverViewModel>();
        services.AddTransient<RewardsViewModel>();
        services.AddTransient<FeedViewModel>();
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<MemberAddressesViewModel>();
        services.AddTransient<MemberCommerceViewModel>();
        services.AddTransient<MemberPreferencesViewModel>();
        services.AddTransient<MemberCustomerContextViewModel>();
        services.AddTransient<ChangePasswordViewModel>();
        services.AddTransient<BusinessDetailViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<ActivationViewModel>();
        services.AddTransient<ForgotPasswordViewModel>();
        services.AddTransient<ResetPasswordViewModel>();
        services.AddTransient<LegalHubViewModel>();
        services.AddTransient<AccountDeletionViewModel>();

        services.AddTransient<LoginPage>();
        services.AddTransient<QrPage>();
        services.AddTransient<DiscoverPage>();
        services.AddTransient<RewardsPage>();
        services.AddTransient<FeedPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<ProfilePage>();
        services.AddTransient<MemberAddressesPage>();
        services.AddTransient<MemberCommercePage>();
        services.AddTransient<MemberPreferencesPage>();
        services.AddTransient<MemberCustomerContextPage>();
        services.AddTransient<RegisterPage>();
        services.AddTransient<ActivationPage>();
        services.AddTransient<ForgotPasswordPage>();
        services.AddTransient<ResetPasswordPage>();
        services.AddTransient<ChangePasswordPage>();
        services.AddTransient<BusinessDetailPage>();
        services.AddTransient<LegalHubPage>();
        services.AddTransient<AccountDeletionPage>();

        return services;
    }

    private static string ResolveEnvironmentName()
    {
        var configured = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        }

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

#if DEBUG
        return "Development";
#else
        return "Production";
#endif
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

            return builder.AddJsonStream(ms);
        }
        catch (FileNotFoundException)
        {
            if (optional)
            {
                return builder;
            }

            throw;
        }
    }
}
