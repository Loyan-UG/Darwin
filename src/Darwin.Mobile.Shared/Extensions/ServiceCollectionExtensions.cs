using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.Resilience;
using Darwin.Mobile.Shared.Security;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.Services.Profile;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace Darwin.Mobile.Shared.Extensions
{
    /// <summary>
    /// Provides composition helpers to register shared mobile services into a MAUI app's DI container.
    /// This extension sets up a resilient HTTP client, token storage, and feature services
    /// (authentication, loyalty, and business discovery).
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers shared services used by both mobile apps.
        /// </summary>
        /// <param name="services">The service collection to populate.</param>
        /// <param name="options">API bootstrap options (base URL, audience, refresh cadence).</param>
        /// <returns>The same service collection for chaining.</returns>
        /// <remarks>
        /// Requires the <c>Microsoft.Extensions.Http</c> package, because <c>AddHttpClient</c> is used to configure <see cref="HttpClient"/>.
        /// Keep retry attempts small to preserve battery and user experience.
        /// </remarks>
        public static IServiceCollection AddDarwinMobileShared(this IServiceCollection services, ApiOptions options)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (options is null) throw new ArgumentNullException(nameof(options));

            services.AddSingleton(options);

            // Retry policy: small attempt count + exponential backoff with jitter to limit battery/network impact.
            services.AddSingleton<IRetryPolicy>(_ => new ExponentialBackoffRetryPolicy(maxAttempts: 3));

            // Configure HttpClient with BaseAddress and timeout from options.
            services.AddHttpClient<IApiClient, ApiClient>()
                    .ConfigureHttpClient(c =>
                    {
                        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
                        {
                            c.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                        }
                        c.Timeout = TimeSpan.FromSeconds(15);
                    });

            // Token storage (SecureStorage under the hood in platform-specific implementation).
            services.AddSingleton<ITokenStore, TokenStore>();

            // Register device id provider so AuthService (and other features) can obtain a stable installation id
            services.AddSingleton<IDeviceIdProvider, DeviceIdProvider>();

            // inside AddDarwinMobileShared(ApiOptions options)
            services.AddSingleton<INavigationService, ShellNavigationService>();


            // Feature services
            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<ILoyaltyService, LoyaltyService>();
            services.AddSingleton<IBusinessService, BusinessService>();
            services.AddSingleton<IProfileService, ProfileService>();

            // NOTE: QrTokenRefresher is opt-in; pages/viewmodels can new it up or register as needed.

            return services;
        }
    }
}