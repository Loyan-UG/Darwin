// src/Darwin.Mobile.Shared/Extensions/ServiceCollectionExtensions.cs
// https://github.com/shahramvafadar/Darwin/blob/301147077eba61b84e0eec8656aec08e20a1795a/src/Darwin.Mobile.Shared/Extensions/ServiceCollectionExtensions.cs

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
    /// 
    /// Rationale:
    /// - Centralize all shared-service registrations so both Consumer and Business apps get consistent behavior.
    /// - Configure the typed ApiClient once and provide a configurable message handler for debugging (trust dev certs).
    /// 
    /// Pitfalls:
    /// - Registering AddHttpClient for the same typed client multiple times may cause confusing registrations.
    ///   We register it once and configure everything there.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers shared services used by both mobile apps.
        /// </summary>
        /// <param name="services">The service collection to populate.</param>
        /// <param name="options">API bootstrap options (base URL, audience, refresh cadence).</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddDarwinMobileShared(this IServiceCollection services, ApiOptions options)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (options is null) throw new ArgumentNullException(nameof(options));

            services.AddSingleton(options);

            // Retry policy: small attempt count + exponential backoff with jitter to limit battery/network impact.
            services.AddSingleton<IRetryPolicy>(_ => new ExponentialBackoffRetryPolicy(maxAttempts: 3));

            // Configure HttpClient typed ApiClient once with base address, timeouts, default headers and (DEBUG) handler.
            services.AddHttpClient<IApiClient, ApiClient>(client =>
            {
                if (!string.IsNullOrWhiteSpace(options.BaseUrl))
                {
                    // Normalize base URL to avoid subtle URI composition issues.
                    // Normalize once to avoid subtle BaseAddress composition issues.
                    var baseUrl = options.BaseUrl.Trim();
                    if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
                    {
                        baseUrl += "/";
                    }

                    client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
                }

                client.Timeout = TimeSpan.FromSeconds(20);

                // Helpful header for local tunneling (ngrok) dev scenarios.
                client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "1");
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                //#if DEBUG
                //                // Development: accept any server certificate (trust all).
                //                // WARNING: Unsafe for production. Keep inside #if DEBUG or guard with config flag.
                //                return new HttpClientHandler
                //                {
                //                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                //                };
                //#else
                //                return new HttpClientHandler();
                //#endif

                if (options.UnsafeTrustAnyServerCertificate)
                {
                    // TODO [SECURITY][MOBILE-RELEASE]:
                    // This is temporary for test environments (e.g., ngrok/dev tunnels).
                    // Remove before production and restore strict certificate validation.
                    return new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };
                }

                return new HttpClientHandler();
            });

            // Register device id provider so AuthService (and other features) can obtain a stable installation id
            services.AddSingleton<IDeviceIdProvider, DeviceIdProvider>();

            // Token storage (SecureStorage under the hood in platform-specific implementation).
            services.AddSingleton<ITokenStore, TokenStore>();

            // Navigation service for MAUI Shell integration (shared).
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