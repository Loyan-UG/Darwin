using Darwin.Application.Abstractions.Auth;
using Darwin.Infrastructure.Security.Passwords;
using Darwin.Infrastructure.Security.Stamps;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Infrastructure.Extensions
{
    /// <summary>
    /// Registers Infrastructure services used by the Identity application layer:
    /// - IUserPasswordHasher
    /// - ISecurityStampService
    /// </summary>
    public static class ServiceCollectionExtensionsIdentity
    {
        public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
        {
            // TODO: if you adopt BCrypt.Net-Next, configure work factor via options.
            services.AddSingleton<IUserPasswordHasher>(new BcryptPasswordHasher(/*workFactor*/ 12));
            services.AddSingleton<ISecurityStampService, SecurityStampService>();
            return services;
        }
    }
}
