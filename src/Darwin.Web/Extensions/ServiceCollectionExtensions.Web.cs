using Darwin.Application.Abstractions.Services;
using Darwin.Application.Extensions;
using Darwin.Infrastructure.Adapters.Time;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace Darwin.Web.Extensions
{
    public static class ServiceCollectionExtensionsWeb
    {
        public static IServiceCollection AddWebComposition(this IServiceCollection services)
        {
            services.AddApplication();     // AutoMapper + FluentValidation (assembly scan)
            services.AddScoped<IClock, SystemClock>();
            return services;
        }
    }
}
