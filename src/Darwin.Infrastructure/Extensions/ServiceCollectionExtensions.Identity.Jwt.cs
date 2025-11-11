using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Security;
using Darwin.Infrastructure.Security.Jwt;
using Darwin.Infrastructure.Security.LoginRateLimiter;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Infrastructure.Extensions
{
    /// <summary>
    /// Registers JWT/Refresh infrastructure and the login rate limiter.
    /// </summary>
    public static class ServiceCollectionExtensionsIdentityJwt
    {
        public static IServiceCollection AddJwtAuthCore(this IServiceCollection services)
        {
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddSingleton<ILoginRateLimiter, MemoryLoginRateLimiter>();
            return services;
        }
    }
}
