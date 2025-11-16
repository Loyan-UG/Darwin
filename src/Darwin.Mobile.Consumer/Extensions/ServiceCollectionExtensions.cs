using System.IO;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Extensions;
using Darwin.Mobile.Shared.Integration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration.Json;

namespace Darwin.Mobile.Consumer.Extensions
{
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

            services.AddDarwinMobileShared(apiOptions);

            services.AddSingleton<IScanner, Services.Platform.ScannerPlatformService>();
            services.AddSingleton<ILocation, Services.Platform.LocationPlatformService>();

            services.AddTransient<ViewModels.LoginViewModel>();
            services.AddTransient<Pages.LoginPage>();
            services.AddTransient<ViewModels.QrViewModel>();
            services.AddTransient<Pages.QrPage>();
            services.AddTransient<ViewModels.DiscoverViewModel>();
            services.AddTransient<Pages.DiscoverPage>();
            services.AddTransient<ViewModels.RewardsViewModel>();
            services.AddTransient<Pages.RewardsPage>();

            return services;
        }
    }
}
