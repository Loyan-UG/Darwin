using Darwin.Application.Abstractions.Auth;
using Darwin.Infrastructure.Auth.WebAuthn;
using Darwin.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

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


            return services;
        }

    }
}
