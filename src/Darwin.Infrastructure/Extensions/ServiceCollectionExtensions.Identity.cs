using Darwin.Application.Abstractions.Auth;
using Darwin.Infrastructure.Auth.WebAuthn;
using Darwin.Infrastructure.Persistence.Converters;
using Darwin.Infrastructure.Security;
using Darwin.Infrastructure.Security.Secrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;

namespace Darwin.Infrastructure.Extensions
{
    /// <summary>
    /// Registers security/identity infrastructure services that Application depends on:
    /// - IUserPasswordHasher: Argon2id implementation
    /// - ISecurityStampService: generator + constant-time comparator
    /// Add more identity-related infrastructure here (e.g., token services, email/SMS adapters) when needed.
    /// </summary>
    public static class ServiceCollectionExtensionsIdentity
    {
        /// <summary>
        /// Adds identity-related infrastructure into DI container.
        /// Call from Darwin.Web startup composition.
        /// </summary>
        public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
        {
            // Password hashing (Argon2id)
            services.AddSingleton<IUserPasswordHasher, Argon2PasswordHasher>();

            // Security stamp service
            services.AddSingleton<ISecurityStampService, SecurityStampService>();

            // WebAuthn: RP provider + Fido2 adapter
            services.AddScoped<IRelyingPartyFromSiteSettingsProvider, RelyingPartyFromSiteSettingsProvider>();
            services.AddScoped<IWebAuthnService, Fido2WebAuthnService>();

            // Adds TOTP service and other security helpers
            services.AddSingleton<ITotpService, TotpService>();


            services.AddDataProtection(); // data protection keys (persist to folder/redis later if needed)
            services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();

            // Initialize EF converter factory once (so configurations can use it)
            services.AddSingleton(_ =>
            {
                var protector = _.GetRequiredService<ISecretProtector>();
                SecretProtectionConverterFactory.Initialize(protector);
                return protector; // keep DI happy
            });

            return services;
        }

    }
}
