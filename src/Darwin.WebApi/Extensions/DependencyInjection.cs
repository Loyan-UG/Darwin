using System;
using System.Text;
using System.Text.Json;
using Darwin.Application.Extensions;
using Darwin.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;

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
        public static IServiceCollection AddWebApiComposition(this IServiceCollection services, IConfiguration configuration)
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

            // JWT bearer authentication.
            ConfigureJwtBearerAuthentication(services, configuration);


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
