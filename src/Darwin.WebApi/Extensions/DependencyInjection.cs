// DependencyInjection.cs
// Composition root for Darwin.WebApi
//
// The registration below makes sure all framework, infrastructure and application
// services that WebApi controllers depend on are available in DI. Application
// "Handler" classes are auto-registered (scoped) to avoid missing-registration
// errors when controllers request concrete handler types. Lifetimes are chosen
// conservatively: handlers are scoped (to match DbContext scope), shared helpers
// either singleton or scoped depending on intended usage.
//
// Comments are descriptive and intended to document why each registration exists.
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Catalog.Services;             // IAddOnPricingService, AddOnPricingService
using Darwin.Application.Common.Html;                  // IHtmlSanitizer + HtmlSanitizerFactory
using Darwin.Application.Extensions;
using Darwin.Application.Loyalty.Queries;
using Darwin.Application.Loyalty.Services;             // ScanSessionTokenResolver
// Application types used to locate assemblies and for explicit registrations
using Darwin.Application.Meta.Queries;                 // marker for Application assembly
// Clock/Time adapter
using Darwin.Infrastructure.Adapters.Time;
using Darwin.Infrastructure.Extensions;
using Darwin.WebApi.Auth;
using Darwin.WebApi.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace Darwin.WebApi.Extensions
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Register WebApi composition:
        /// - Application baseline (AutoMapper, validators)
        /// - Persistence and infrastructure
        /// - Identity and JWT token services
        /// - Controller-related services and JSON options
        /// - Auto-register concrete Application handlers (classes ending with "Handler")
        /// </summary>
        public static IServiceCollection AddWebApiComposition(this IServiceCollection services, IConfiguration configuration)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (configuration is null) throw new ArgumentNullException(nameof(configuration));

            // ------------------------------------------------------------
            // 1) Application baseline (AutoMapper + FluentValidation)
            // ------------------------------------------------------------
            // Registers AutoMapper profiles and validators found in Darwin.Application.
            services.AddApplication();

            // ------------------------------------------------------------
            // 2) Small per-request helpers
            // ------------------------------------------------------------
            // Register a clock abstraction. SystemClock is stateless; registering as scoped
            // keeps it consistent with other per-request dependencies and allows test overrides.
            services.AddScoped<IClock, SystemClock>();

            // HttpContextAccessor and a CurrentUser service used by handlers to obtain caller id.
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            // ------------------------------------------------------------
            // 3) Persistence + DataProtection
            // ------------------------------------------------------------
            // Register EF Core DbContext, IAppDbContext mapping and seeder orchestration.
            services.AddPersistence(configuration);

            // Data Protection (persisted key ring for shared hosting).
            services.AddSharedHostingDataProtection(configuration);

            // ------------------------------------------------------------
            // 4) Identity & Security infrastructure
            // ------------------------------------------------------------
            // Register password hashing, security stamp service, WebAuthn/TOTP, secret protection, permission service.
            services.AddIdentityInfrastructure();

            // Register JWT issuance and login limiter.
            services.AddJwtAuthCore();

            // ------------------------------------------------------------
            // 5) Authorization & permission policy wiring
            // ------------------------------------------------------------
            services.AddAuthorization();
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IAuthorizationHandler, PermissionAuthorizationHandler>());

            // ------------------------------------------------------------
            // 6) Notifications, caching, presentation helpers
            // ------------------------------------------------------------
            services.AddNotificationsInfrastructure(configuration);
            services.AddMemoryCache();

            // Loyalty presentation helpers used by WebApi mapping/presentation layer.
            services.AddLoyaltyPresentationServices();
            services.AddScoped<GetAvailableLoyaltyRewardsForBusinessHandler>();

            // ------------------------------------------------------------
            // 7) Explicit registrations for Application-level helper services
            // ------------------------------------------------------------
            // Some application handlers depend on helper services that are normally registered
            // by the Web composition root (Darwin.Web). When WebApi uses application handlers
            // directly, those helpers must be present as well.

            // HTML sanitizer: create a singleton configured sanitizer the same way Web does.
            // HtmlSanitizerFactory.Create() returns an IHtmlSanitizer adapter.
            services.AddSingleton<IHtmlSanitizer>(_ => HtmlSanitizerFactory.Create());

            // Add-on pricing service used by cart/checkout handlers.
            services.AddScoped<IAddOnPricingService, AddOnPricingService>();

            // Scan session token resolver used by loyalty handlers that accept QR tokens.
            // This resolver depends on IAppDbContext and IClock, so keep it scoped.
            services.AddScoped<ScanSessionTokenResolver>();

            // Note: if additional helper services are required by application handlers,
            // they should be registered here following the same pattern.

            // ------------------------------------------------------------
            // 8) Auto-register Application "Handler" classes (scoped)
            // ------------------------------------------------------------
            // Scan the Application assembly for concrete types named "*Handler" and register them
            // as scoped services. Controllers typically request concrete handler types directly,
            // so registering implementation types suffices.
            try
            {
                var appAssembly = typeof(GetAppBootstrapHandler).Assembly;

                var handlerTypes = appAssembly
                    .GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Handler", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var handlerType in handlerTypes)
                {
                    // Avoid duplicate registration of the exact implementation type.
                    var alreadyRegistered = services.Any(sd => sd.ServiceType == handlerType && sd.ImplementationType == handlerType);
                    if (!alreadyRegistered)
                    {
                        services.AddScoped(handlerType);
                    }
                }
            }
            catch (ReflectionTypeLoadException rtlEx)
            {
                // Collect loader exception messages defensively to avoid any null-reference warnings.
                var loaderExceptions = rtlEx.LoaderExceptions;
                string loaderMessages;
                if (loaderExceptions is null || loaderExceptions.Length == 0)
                {
                    loaderMessages = string.Empty;
                }
                else
                {
                    loaderMessages = string.Join(" | ", loaderExceptions.Select(e => e?.Message ?? e?.ToString() ?? "<null>"));
                }

                throw new InvalidOperationException($"Handler auto-registration failed. Loader errors: {loaderMessages}", rtlEx);
            }

            // ------------------------------------------------------------
            // 9) Controllers, JSON options and OpenAPI
            // ------------------------------------------------------------
            services
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    // Keep JSON behavior consistent with Darwin.Contracts (camelCase).
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // ------------------------------------------------------------
            // 10) JWT Bearer authentication setup (DB-driven via provider)
            // ------------------------------------------------------------
            // JwtSigningParametersProvider reads SiteSetting from DB and supplies signing keys
            // to JwtBearerOptionsSetup which configures token validation. This keeps issuer/audience
            // and keys in sync with the issuing service (IJwtTokenService).
            services.TryAddSingleton<JwtSigningParametersProvider>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<JwtBearerOptions>, JwtBearerOptionsSetup>());

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer();

            // ------------------------------------------------------------
            // 11) Rate limiting policies for auth endpoints
            // ------------------------------------------------------------
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy("auth-login", httpContext =>
                {
                    var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
                    var partitionKey = string.IsNullOrWhiteSpace(remoteIp) ? "unknown" : remoteIp;

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromSeconds(30),
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                });

                options.AddPolicy("auth-refresh", httpContext =>
                {
                    var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
                    var partitionKey = string.IsNullOrWhiteSpace(remoteIp) ? "unknown" : remoteIp;

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 20,
                            Window = TimeSpan.FromSeconds(60),
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                });
            });

            // ------------------------------------------------------------
            // Finalize
            // ------------------------------------------------------------
            return services;
        }

        /// <summary>
        /// Optional helper for appsettings-based JWT configuration.
        /// This helper is not invoked by default; project uses DB-driven JwtSigningParametersProvider.
        /// </summary>
        private static void ConfigureJwtBearerAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            var issuer = configuration["Jwt:Issuer"];
            var audience = configuration["Jwt:Audience"];
            var signingKey = configuration["Jwt:SigningKey"];
            var clockSkewSecondsValue = configuration["Jwt:ClockSkewSeconds"];

            if (string.IsNullOrWhiteSpace(issuer)) throw new InvalidOperationException("Configuration key 'Jwt:Issuer' is missing or empty.");
            if (string.IsNullOrWhiteSpace(audience)) throw new InvalidOperationException("Configuration key 'Jwt:Audience' is missing or empty.");
            if (string.IsNullOrWhiteSpace(signingKey)) throw new InvalidOperationException("Configuration key 'Jwt:SigningKey' is missing or empty.");

            if (!int.TryParse(clockSkewSecondsValue, out var clockSkewSeconds) || clockSkewSeconds < 0) clockSkewSeconds = 60;

            var keyBytes = Encoding.UTF8.GetBytes(signingKey);
            var securityKey = new SymmetricSecurityKey(keyBytes);

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = issuer,
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = securityKey,
                        RequireExpirationTime = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(clockSkewSeconds)
                    };
                });
        }
    }
}