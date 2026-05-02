using Darwin.Mobile.Business.Services.Identity;
using Darwin.Mobile.Business.Services.Reporting;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Configuration;
using Darwin.Mobile.Shared.Extensions;
using Darwin.Mobile.Shared.Integration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;
using System;
using System.IO;
using System.Reflection;

namespace Darwin.Mobile.Business.Extensions;

/// <summary>
/// Composes Business app-specific services: configuration binding, shared mobile services,
/// platform scanning/location, business authorization context, reporting helpers, pages, and view models.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Business app services and pages into DI.
    /// </summary>
    public static IServiceCollection AddBusinessApp(this IServiceCollection services)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFileFromMauiAsset("appsettings.mobile.json", optional: false)
            .AddJsonFileFromMauiAsset($"appsettings.mobile.{ResolveEnvironmentName()}.json", optional: true)
            .Build();

        var apiOptions = config.GetSection("Api").Get<ApiOptions>()
            ?? throw new InvalidOperationException("Missing 'Api' section in appsettings.mobile.json");

        var legalLinksOptions = config.GetSection("LegalLinks").Get<LegalLinksOptions>()
            ?? throw new InvalidOperationException("Missing 'LegalLinks' section in appsettings.mobile.json");


        if (string.IsNullOrWhiteSpace(apiOptions.BaseUrl))
        {
            // Fail fast with a clear message instead of hitting HttpClient invalid URI errors later.
            throw new InvalidOperationException(
                "Api:BaseUrl is empty after configuration binding. " +
                "Check Resources/Raw/appsettings.mobile.json and rebuild/reinstall the app.");
        }

        // Ensure app role is explicitly set for client-side validation (defensive).
        apiOptions.AppRole = MobileAppRole.Business;

        services.AddDarwinMobileShared(apiOptions, legalLinksOptions);

        services.AddSingleton<IScanner, Services.Platform.ScannerPlatformService>();
        services.AddSingleton<ILocation, Services.Platform.LocationPlatformService>();

        // Business identity context for Home dashboard
        services.AddSingleton<IBusinessIdentityContextService, BusinessIdentityContextService>();
        services.AddSingleton<IBusinessAuthorizationService, BusinessAuthorizationService>();

        // Local activity tracker powering dashboard and lightweight reporting cards.
        services.AddSingleton<IBusinessActivityTracker, BusinessActivityTracker>();

        services.AddTransient<ViewModels.HomeViewModel>();
        services.AddTransient<Views.HomePage>();

        services.AddTransient<ViewModels.LoginViewModel>();
        services.AddTransient<Views.LoginPage>();
        services.AddTransient<ViewModels.AcceptInvitationViewModel>();
        services.AddTransient<Views.AcceptInvitationPage>();

        services.AddTransient<ViewModels.ScannerViewModel>();
        services.AddTransient<Views.ScannerPage>();

        services.AddTransient<ViewModels.DashboardViewModel>();
        services.AddTransient<Views.DashboardPage>();

        services.AddTransient<ViewModels.RewardsViewModel>();
        services.AddTransient<Views.RewardsPage>();

        services.AddTransient<ViewModels.SessionViewModel>();
        services.AddTransient<Views.SessionPage>();

        // Settings
        services.AddTransient<ViewModels.SettingsViewModel>();
        services.AddTransient<Views.SettingsPage>();

        services.AddTransient<ViewModels.ProfileViewModel>();
        services.AddTransient<Views.ProfilePage>();

        services.AddTransient<ViewModels.ChangePasswordViewModel>();
        services.AddTransient<Views.ChangePasswordPage>();

        services.AddTransient<ViewModels.StaffAccessBadgeViewModel>();
        services.AddTransient<Views.StaffAccessBadgePage>();

        services.AddTransient<ViewModels.SubscriptionViewModel>();
        services.AddTransient<Views.SubscriptionPage>();
        services.AddTransient<ViewModels.LegalHubViewModel>();
        services.AddTransient<Views.LegalHubPage>();
        services.AddTransient<ViewModels.AccountDeletionViewModel>();
        services.AddTransient<Views.AccountDeletionPage>();

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

            return builder.AddJsonStream(ms); // Configuration will dispose ms when done
        }
        catch (FileNotFoundException)
        {
            if (optional) return builder;
            throw;
        }
    }
}
