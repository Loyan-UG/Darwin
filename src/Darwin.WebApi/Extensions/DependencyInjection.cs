using Darwin.Application.Extensions;
using Darwin.Infrastructure.Extensions;
using Darwin.WebApi.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using Darwin.WebApi.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Text;
using System.Text.Json;

namespace Darwin.WebApi.Extensions
{
    /// <summary>
    ///     Service registration for the WebApi entry point.
    ///     Aggregates Application + Infrastructure composition and API-specific
    ///     concerns (controllers, JSON, Swagger, JWT bearer auth) into a single
    ///     discoverable extension method that keeps Program.cs minimal.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        ///     Bootstraps the WebApi layer by registering:
        ///     <list type="bullet">
        ///         <item>Application-layer services (AutoMapper, FluentValidation, etc.).</item>
        ///         <item>Persistence (EF Core DbContext + IAppDbContext abstraction).</item>
        ///         <item>Identity infrastructure (Argon2, WebAuthn, TOTP, permission service).</item>
        ///         <item>JWT issuing + login rate limiter (AddJwtAuthCore).</item>
        ///         <item>Data Protection with a shared key ring for token/encryption.</item>
        ///         <item>SMTP notifications infrastructure (IEmailSender).</item>
        ///         <item>API controllers with System.Text.Json configuration.</item>
        ///         <item>JWT bearer authentication and basic authorization.</item>
        ///         <item>OpenAPI/Swagger for development and testing.</item>
        ///     </list>
        /// </summary>
        /// <param name="services">The DI container.</param>
        /// <param name="configuration">Application configuration (appsettings + environment).</param>
        /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddWebApiComposition(
            this IServiceCollection services, IConfiguration configuration)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (configuration is null) throw new ArgumentNullException(nameof(configuration));

            // Application-layer services (AutoMapper profiles, validators, etc.)
            services.AddApplication();

            // Persistence: DbContext + IAppDbContext abstraction + migrations/seeding helper.
            services.AddPersistence(configuration);

            // Data Protection with persisted key ring (shared across app restarts / processes).
            services.AddSharedHostingDataProtection(configuration);

            // Identity infrastructure: password hashing (Argon2), security stamp, WebAuthn, TOTP, secret protection, permission service.
            services.AddIdentityInfrastructure();

            // JWT issuing + login rate limiter as described in the architecture notes.
            services.AddJwtAuthCore();

            // Permission-based authorization aligned with the admin Web layer.
            // Policies are generated dynamically for names starting with "perm:".
            services.AddAuthorization();

            // Register the dynamic policy provider and the handler that talks to IPermissionService.
            services.TryAddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IAuthorizationHandler, PermissionAuthorizationHandler>());


            // SMTP notifications (password reset emails, etc.).
            services.AddNotificationsInfrastructure(configuration);

            // Access to HttpContext for handlers/controllers that need it.
            services.AddHttpContextAccessor();

            // Controllers + System.Text.Json options.
            services
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    // Use the web defaults (camelCase, case-insensitive) explicitly to keep behavior
                    // in sync with Contracts and the Mobile Shared client (JsonSerializerDefaults.Web).
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                });

            // OpenAPI/Swagger for local development and manual testing of the mobile APIs.
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // --- JWT Bearer validation sourced from SiteSetting ---
            services.TryAddSingleton<JwtSigningParametersProvider>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<JwtBearerOptions>, JwtBearerOptionsSetup>());

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(); // Options are configured via JwtBearerOptionsSetup


            // ASP.NET Core rate limiting for critical auth endpoints (login / refresh).
            // This works in addition to the application-level ILoginRateLimiter, which
            // applies per email/device keys. Here we apply a coarse IP-based throttle
            // to protect the API surface as a whole.
            services.AddRateLimiter(options =>
            {
                // Return 429 for rejected requests.
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy("auth-login", httpContext =>
                {
                    if (httpContext is null)
                    {
                        throw new ArgumentNullException(nameof(httpContext));
                    }

                    var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
                    var partitionKey = string.IsNullOrWhiteSpace(remoteIp) ? "unknown" : remoteIp;

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            // Up to 5 login attempts per 30 seconds per IP.
                            PermitLimit = 5,
                            Window = TimeSpan.FromSeconds(30),
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                });

                options.AddPolicy("auth-refresh", httpContext =>
                {
                    if (httpContext is null)
                    {
                        throw new ArgumentNullException(nameof(httpContext));
                    }

                    var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
                    var partitionKey = string.IsNullOrWhiteSpace(remoteIp) ? "unknown" : remoteIp;

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            // Refresh is less sensitive than login, but still protected.
                            // Here: 20 refreshes per 60 seconds per IP.
                            PermitLimit = 20,
                            Window = TimeSpan.FromSeconds(60),
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                });
            });



            // Basic authorization setup. Fine-grained permission policies can be wired later
            // (reusing the PermissionPolicyProvider/PermissionAuthorizationHandler from Darwin.Web.Auth).
            services.AddAuthorization();

            return services;
        }

        /// <summary>
        ///     Registers JWT bearer authentication using symmetric signing keys.
        ///     For now, values are read from configuration section "Jwt:*".
        ///     These should be kept in sync with SiteSetting.Jwt* values that
        ///     the issuing side (IJwtTokenService) uses.
        /// </summary>
        /// <remarks>
        ///     Expected configuration keys:
        ///     <list type="bullet">
        ///         <item><c>Jwt:Issuer</c> – token issuer (e.g. "Darwin.PublicApi").</item>
        ///         <item><c>Jwt:Audience</c> – logical audience for access tokens.</item>
        ///         <item><c>Jwt:SigningKey</c> – symmetric signing key (long random secret).</item>
        ///         <item><c>Jwt:ClockSkewSeconds</c> – allowed clock skew for validation.</item>
        ///     </list>
        ///     In production, the signing key MUST be a high-entropy value stored securely
        ///     (e.g. environment variable, KeyVault). It must match the key used by
        ///     <c>IJwtTokenService</c> when issuing access tokens.
        /// </remarks>
        private static void ConfigureJwtBearerAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            var issuer = configuration["Jwt:Issuer"];
            var audience = configuration["Jwt:Audience"];
            var signingKey = configuration["Jwt:SigningKey"];
            var clockSkewSecondsValue = configuration["Jwt:ClockSkewSeconds"];

            if (string.IsNullOrWhiteSpace(issuer))
            {
                throw new InvalidOperationException("Configuration key 'Jwt:Issuer' is missing or empty.");
            }

            if (string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException("Configuration key 'Jwt:Audience' is missing or empty.");
            }

            if (string.IsNullOrWhiteSpace(signingKey))
            {
                throw new InvalidOperationException("Configuration key 'Jwt:SigningKey' is missing or empty.");
            }

            if (!int.TryParse(clockSkewSecondsValue, out var clockSkewSeconds) || clockSkewSeconds < 0)
            {
                clockSkewSeconds = 60; // sensible default
            }

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

                    // Optional: allow reading tokens from "Authorization: Bearer <token>" header only.
                    // If in the future you need to support query-string or cookie-based tokens for
                    // specific endpoints, handle it in Events.OnMessageReceived.
                });
        }
    }
}
