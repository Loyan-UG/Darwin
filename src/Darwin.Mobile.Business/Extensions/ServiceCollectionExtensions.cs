using System.IO;
using System.Reflection;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Extensions;
using Darwin.Mobile.Shared.Integration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration.Json;

namespace Darwin.Mobile.Business.Extensions
{
    /// <summary>
    /// Composes Business app-specific services: configuration binding, shared mobile services,
    /// and platform implementations for scanning/location, plus pages and view models.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Business app services and pages into DI.
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

            // 2) Shared (HttpClient + retry + services)
            services.AddDarwinMobileShared(apiOptions);

            // 3) Platform services (stub implementations for now)
            services.AddSingleton<IScanner, Services.Platform.ScannerPlatformService>();
            services.AddSingleton<ILocation, Services.Platform.LocationPlatformService>();

            // 4) Pages & ViewModels
            services.AddTransient<ViewModels.LoginViewModel>();
            services.AddTransient<Pages.LoginPage>();
            services.AddTransient<ViewModels.ScannerViewModel>();
            services.AddTransient<Pages.ScannerPage>();
            services.AddTransient<ViewModels.SessionViewModel>();
            services.AddTransient<Pages.SessionPage>();

            return services;
        }
    }
}
