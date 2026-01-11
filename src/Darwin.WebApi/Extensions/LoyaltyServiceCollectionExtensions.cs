using Microsoft.Extensions.DependencyInjection;
using Darwin.WebApi.Services;

namespace Darwin.WebApi.Extensions
{
    /// <summary>
    /// Extension helpers to register loyalty presentation services for WebApi.
    /// Call AddLoyaltyPresentationServices(...) from the WebApi composition root.
    /// </summary>
    public static class LoyaltyServiceCollectionExtensions
    {
        public static IServiceCollection AddLoyaltyPresentationServices(this IServiceCollection services)
        {
            // IMemoryCache is typically already registered via AddMemoryCache in WebApi startup.
            services.AddScoped<ILoyaltyPresentationService, LoyaltyPresentationService>();
            return services;
        }
    }
}