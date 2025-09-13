using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.Commands;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Queries;
using Darwin.Application.Catalog.Queries.GetProductForEdit;
using Darwin.Application.Catalog.Queries.GetProductsPage;
using Darwin.Application.Catalog.Validators;
using Darwin.Application.CMS.Commands;
using Darwin.Application.CMS.Queries;
using Darwin.Application.Common.Html;
using Darwin.Application.Extensions;
using Darwin.Application.Settings.Commands;
using Darwin.Application.Settings.DTOs;
using Darwin.Application.Settings.Queries;
using Darwin.Application.Settings.Validators;
using Darwin.Infrastructure.Extensions;
using Darwin.Web.Auth;
using Darwin.Web.Services.Seo;
using Darwin.Web.Services.Settings;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;





namespace Darwin.Web.Extensions
{
    /// <summary>
    ///     Service registration for the Web layer (composition root). Aggregates MVC/Razor setup,
    ///     localization primitives, model binders/formatters, and Web-specific services into a single
    ///     discoverable entrypoint that can be called from <c>Program.cs</c>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Goals:
    ///         <list type="bullet">
    ///             <item>Keep <c>Program.cs</c> concise and declarative.</item>
    ///             <item>Group related service registrations to reduce scattering and improve evolvability.</item>
    ///             <item>Provide a single place to add/remap Admin-specific services (settings cache, tag helpers, view locations).</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Notes:
    ///         This extension should remain free of infrastructure-specific registrations (EF, logging),
    ///         which are handled in the Infrastructure project.
    ///     </para>
    /// </remarks>
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers all web-layer services and cross-cutting components used by MVC.
        /// Keep infrastructure-specific registrations in Infrastructure project.
        /// </summary>
        public static IServiceCollection AddWebComposition(this IServiceCollection services, IConfiguration config)
        {
            // 1) MVC + Localization
            services
                .AddControllersWithViews(options =>
                {
                    // Avoid implicit [Required] for non-nullable reference types (we rely on explicit validators).
                    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                })
                .AddViewLocalization()
                .AddDataAnnotationsLocalization()
#if DEBUG
                .AddRazorRuntimeCompilation()
#endif
                ;

            // 2) Application layer (AutoMapper + Validator scanning)
            services.AddApplication();

            // 3) Persistence (DbContext + IAppDbContext + Seeder)
            services.AddPersistence(config);

            // 4) Anti-forgery defaults (Admin forms)
            services.AddAntiforgery();

            // 5) Handlers — Products
            services.AddScoped<CreateProductHandler>();
            services.AddScoped<UpdateProductHandler>();
            services.AddScoped<GetProductsPageHandler>();
            services.AddScoped<GetProductForEditHandler>();

            // 6) Handlers — Categories / CMS
            services.AddScoped<CreateCategoryHandler>();
            services.AddScoped<UpdateCategoryHandler>();
            services.AddScoped<GetCategoriesPageHandler>();
            services.AddScoped<GetCategoryForEditHandler>();

            services.AddScoped<CreatePageHandler>();
            services.AddScoped<UpdatePageHandler>();
            services.AddScoped<GetPagesPageHandler>();
            services.AddScoped<GetPageForEditHandler>();

            // 7) Lookups & Settings
            services.AddScoped<GetCatalogLookupsHandler>();
            services.AddScoped<GetCulturesHandler>();

            services.AddScoped<GetSiteSettingHandler>();
            services.AddScoped<UpdateSiteSettingHandler>();

            // 8) Web services
            services.AddHttpContextAccessor();
            services.AddScoped<ICanonicalUrlService, CanonicalUrlService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            // 9) Settings cache (used by controllers/views)
            services.AddMemoryCache();
            services.AddScoped<ISiteSettingCache, SiteSettingCache>();

            // 10) HTML Sanitizer (Ganss.Xss via our factory)
            services.AddSingleton<IHtmlSanitizer>(_ => HtmlSanitizerFactory.Create());

            // 11) Product validators: base + unique slug composition (no extra libs)
            services.AddScoped<IValidator<ProductCreateDto>>(sp =>
            {
                var baseValidator = new ProductCreateDtoValidator();
                var unique = new ProductCreateUniqueSlugValidator(sp.GetRequiredService<IAppDbContext>());
                return new InlineCompositeValidator<ProductCreateDto>(baseValidator, unique);
            });

            services.AddScoped<IValidator<ProductEditDto>>(sp =>
            {
                var baseValidator = new ProductEditDtoValidator();
                var unique = new ProductEditUniqueSlugValidator(sp.GetRequiredService<IAppDbContext>());
                return new InlineCompositeValidator<ProductEditDto>(baseValidator, unique);
            });

            // 12) Settings validator (Update DTO)
            services.AddScoped<IValidator<SiteSettingDto>, SiteSettingEditValidator>();

            // 13) Register all validators from Application assembly (for other DTOs)
            services.AddValidatorsFromAssembly(
                Assembly.Load("Darwin.Application"),
                includeInternalTypes: true
            );

            return services;
        }

        /// <summary>
        /// Minimal composite validator to combine multiple FluentValidation validators without extra packages.
        /// </summary>
        public sealed class InlineCompositeValidator<T> : AbstractValidator<T>
        {
            public InlineCompositeValidator(params IValidator<T>[] validators)
            {
                foreach (var v in validators)
                    Include(v);
            }
        }
    }
}
