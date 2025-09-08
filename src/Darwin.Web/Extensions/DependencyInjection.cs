using Darwin.Application.Catalog.Commands;
using Darwin.Application.Catalog.Queries;
using Darwin.Application.Catalog.Queries.GetProductForEdit;
using Darwin.Application.Catalog.Queries.GetProductsPage;
using Darwin.Application.CMS.Commands;
using Darwin.Application.CMS.Queries;
using Darwin.Application.Extensions;
using Darwin.Application.Settings.Commands;
using Darwin.Application.Settings.Queries;
using Darwin.Application.Settings.Validators;
using Darwin.Infrastructure.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Darwin.Web.Services.Seo;
using Darwin.Application.Abstractions.Auth;
using Darwin.Web.Auth;





namespace Darwin.Web.Extensions
{
    /// <summary>
    /// Note:
    /// We do NOT integrate FluentValidation into MVC's automatic model validation pipeline here.
    /// Instead, validators live in the Application layer and are executed explicitly inside use-case handlers.
    /// Controllers catch FluentValidation.ValidationException and map errors into ModelState.
    /// This keeps validation logic close to use-cases and avoids extra MVC coupling.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddWebComposition(this IServiceCollection services, IConfiguration config)
        {
            // MVC and localization. RuntimeCompilation is useful during development; remove in production if desired.
            services
                .AddControllersWithViews(options =>
                {
                    // Avoids implicit [Required] for non-nullable reference types.
                    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                })
                .AddViewLocalization()
                .AddDataAnnotationsLocalization()
            #if DEBUG
                .AddRazorRuntimeCompilation()  // dev convenience; safe to remove in prod
            #endif
                ;

            // Application layer (AutoMapper + Validators scan)
            services.AddApplication();

            // Persistence (DbContext + IAppDbContext + Seeder)
            services.AddPersistence(config);

            // Anti-forgery defaults (Admin forms)
            services.AddAntiforgery();

            // Register application handlers used by Web
            services.AddScoped<CreateProductHandler>();

            // Handlers — Products
            services.AddScoped<CreateProductHandler>();
            services.AddScoped<UpdateProductHandler>();
            services.AddScoped<GetProductsPageHandler>();
            services.AddScoped<GetProductForEditHandler>();

            // Handlers — Categories
            services.AddScoped<CreateCategoryHandler>();
            services.AddScoped<UpdateCategoryHandler>();
            services.AddScoped<GetCategoriesPageHandler>();
            services.AddScoped<GetCategoryForEditHandler>();

            // Lookups
            services.AddScoped<GetCatalogLookupsHandler>();

            services.AddScoped<CreatePageHandler>();
            services.AddScoped<UpdatePageHandler>();
            services.AddScoped<GetPagesPageHandler>();
            services.AddScoped<GetPageForEditHandler>();

            services.AddScoped<GetCulturesHandler>();

            services.AddScoped<GetSiteSettingHandler>();
            services.AddScoped<UpdateSiteSettingHandler>();

            services.AddHttpContextAccessor();
            services.AddScoped<ICanonicalUrlService, CanonicalUrlService>();

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            // Register all FluentValidation validators from the Application assembly.
            services.AddValidatorsFromAssembly(
                Assembly.Load("Darwin.Application"),
                includeInternalTypes: true
            );


            return services;
        }
    }
}
