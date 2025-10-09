using AutoMapper;
using Darwin.Application.Catalog.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Darwin.Application.Extensions
{
    /// <summary>
    /// Registers Application-layer services:
    ///  - AutoMapper profiles (scanned via marker type) using the *core* AutoMapper package (v13+).
    ///  - FluentValidation validators (assembly scan).
    ///
    /// Notes:
    ///  - The old package AutoMapper.Extensions.Microsoft.DependencyInjection is deprecated.
    ///  - The current signature expects a configuration action first, then assemblies or marker types.
    ///    See docs: services.AddAutoMapper(cfg => { }, typeof(ProfileMarkerFromAssembly1), ...)
    /// </summary>
    public static class ServiceCollectionExtensionsApplication
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Choose a *marker type* that lives in the Darwin.Application assembly where your Profiles are.
            // Here we use CatalogProfile as the marker. Replace/add more markers if you split profiles across assemblies.
            var markerType = typeof(Darwin.Application.Catalog.Mapping.CatalogProfile);

            // AutoMapper v13+: pass a config action (can be empty) + marker types or assemblies.
            services.AddAutoMapper(cfg =>
            {
                // Optional global config goes here, e.g. cfg.AllowNullCollections = true;
            },
            markerType // you can pass multiple marker types: markerType1, markerType2, ...
            );

            // Register FluentValidation validators from the same assembly as the marker type.
            // (This extension method comes from FluentValidation.DependencyInjectionExtensions package.)
            services.AddValidatorsFromAssembly(markerType.Assembly);

            

            return services;
        }
    }
}
