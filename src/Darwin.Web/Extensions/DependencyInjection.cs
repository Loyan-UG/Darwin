using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.CartCheckout.Commands;
using Darwin.Application.CartCheckout.DTOs;
using Darwin.Application.CartCheckout.Queries;
using Darwin.Application.CartCheckout.Validators;
using Darwin.Application.Catalog.Commands;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Queries;
using Darwin.Application.Catalog.Services;
using Darwin.Application.Catalog.Validators;
using Darwin.Application.CMS.Commands;
using Darwin.Application.CMS.DTOs;
using Darwin.Application.CMS.Queries;
using Darwin.Application.CMS.Validators;
using Darwin.Application.Common.Html;
using Darwin.Application.Extensions;
using Darwin.Application.Identity.Auth.Commands;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Settings.Commands;
using Darwin.Application.Settings.DTOs;
using Darwin.Application.Settings.Queries;
using Darwin.Application.Settings.Validators;
using Darwin.Application.Shipping.Commands;
using Darwin.Application.Shipping.DTOs;
using Darwin.Application.Shipping.Queries;
using Darwin.Application.Shipping.Validators;
using Darwin.Infrastructure.Adapters.Time;
using Darwin.Infrastructure.Extensions;
using Darwin.Web.Auth;
using Darwin.Web.Services.Seo;
using Darwin.Web.Services.Settings;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace Darwin.Web.Extensions
{
    /// <summary>
    /// Service registration for the Web layer. Aggregates MVC/Razor setup,
    /// localization primitives, model binders/formatters, and Web-specific services
    /// into a single discoverable entrypoint that can be called from Program.cs.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds all Web-layer services to the <see cref="IServiceCollection"/>.
        /// This method should be invoked in Program.cs to bootstrap the Web layer.
        /// </summary>
        public static IServiceCollection AddWebComposition(this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<IClock, SystemClock>();

            // Identity handlers required for Auth flows
            services.AddScoped<SignInHandler>();
            services.AddScoped<RegisterUserHandler>();
            services.AddScoped<VerifyTotpForLoginHandler>();
            services.AddScoped<BeginLoginHandler>();
            services.AddScoped<FinishLoginHandler>();
            services.AddScoped<BeginRegistrationHandler>();
            services.AddScoped<FinishRegistrationHandler>();

            // Password hashing (Argon2id) + Security stamp service + WebAuthn: RP provider + Fido2 adapter + TOTP service
            // cookie auth, Argon2, stamps, WebAuthn service, etc. :contentReference[oaicite:2]{index=2}
            services.AddIdentityInfrastructure();

            // SMTP email sender. :contentReference[oaicite:3]{index=3}
            services.AddNotificationsInfrastructure(config);

            // MVC
            services.AddControllersWithViews();



            // Register handlers — Products
            services.AddScoped<CreateProductHandler>();
            services.AddScoped<UpdateProductHandler>();
            services.AddScoped<GetProductsPageHandler>();
            services.AddScoped<GetProductForEditHandler>();

            // Register handlers — Categories
            services.AddScoped<CreateCategoryHandler>();
            services.AddScoped<UpdateCategoryHandler>();
            services.AddScoped<GetCategoriesPageHandler>();
            services.AddScoped<GetCategoryForEditHandler>();

            // Register handlers — CMS pages
            services.AddScoped<CreatePageHandler>();
            services.AddScoped<UpdatePageHandler>();
            services.AddScoped<GetPagesPageHandler>();
            services.AddScoped<GetPageForEditHandler>();

            // Register handlers — Shipping methods (admin)
            services.AddScoped<CreateShippingMethodHandler>();
            services.AddScoped<UpdateShippingMethodHandler>();
            services.AddScoped<GetShippingMethodsPageHandler>();
            services.AddScoped<GetShippingMethodForEditHandler>();

            // Lookups and cultures
            services.AddScoped<GetCatalogLookupsHandler>();
            services.AddScoped<GetCulturesHandler>();

            // Site settings handlers
            services.AddScoped<GetSiteSettingHandler>();
            services.AddScoped<UpdateSiteSettingHandler>();

            // HttpContext accessor and user service
            services.AddHttpContextAccessor();
            services.AddScoped<ICanonicalUrlService, CanonicalUrlService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            // Settings cache
            services.AddMemoryCache();
            services.AddScoped<ISiteSettingCache, SiteSettingCache>();


            // MVC and localization. RuntimeCompilation is useful during development; remove in production.
            services
                .AddControllersWithViews(options =>
                {
                    // Avoid implicit [Required] for non-nullable reference types.
                    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                })
                .AddViewLocalization()
                .AddDataAnnotationsLocalization()
#if DEBUG
                .AddRazorRuntimeCompilation()
#endif
                ;

            // Application layer (includes AutoMapper + validators scanning)
            services.AddApplication();

            // Register Application services with concrete types
            services.AddScoped<IAddOnPricingService, AddOnPricingService>();


            services.AddSharedHostingDataProtection(config);

            // Persistence (DbContext + IAppDbContext + Seeder)
            services.AddPersistence(config);



            // Anti-forgery defaults for Admin forms
            services.AddAntiforgery();

            services.AddNotificationsInfrastructure(config);



            // HTML Sanitizer using our factory (singleton across the app)
            services.AddSingleton<IHtmlSanitizer>(_ => HtmlSanitizerFactory.Create());

            // Composite validators for Product (Create/Edit) combining base + unique slug
            services.AddScoped<IValidator<ProductCreateDto>>(sp =>
            {
                // Base product validation (translations, variants, required fields)
                var baseValidator = new ProductCreateDtoValidator();
                // Unique slug validation for product
                var unique = new ProductCreateUniqueSlugValidator(sp.GetRequiredService<IAppDbContext>());
                return new InlineCompositeValidator<ProductCreateDto>(baseValidator, unique);
            });

            services.AddScoped<IValidator<ProductEditDto>>(sp =>
            {
                var baseValidator = new ProductEditDtoValidator();
                var unique = new ProductEditUniqueSlugValidator(sp.GetRequiredService<IAppDbContext>());
                return new InlineCompositeValidator<ProductEditDto>(baseValidator, unique);
            });

            // Composite validators for Category (Create/Edit) combining base + unique slug
            services.AddScoped<IValidator<CategoryCreateDto>>(sp =>
            {
                var baseValidator = new CategoryCreateDtoValidator();
                var unique = new CategoryCreateUniqueSlugValidator(sp.GetRequiredService<IAppDbContext>());
                return new InlineCompositeValidator<CategoryCreateDto>(baseValidator, unique);
            });

            services.AddScoped<IValidator<CategoryEditDto>>(sp =>
            {
                var baseValidator = new CategoryEditDtoValidator();
                var unique = new CategoryEditUniqueSlugValidator(sp.GetRequiredService<IAppDbContext>());
                return new InlineCompositeValidator<CategoryEditDto>(baseValidator, unique);
            });

            // Composite validators for CMS pages (Create/Edit) combining base + unique slug
            services.AddScoped<IValidator<PageCreateDto>>(sp =>
            {
                var baseValidator = new PageCreateDtoValidator();
                var unique = new PageCreateUniqueSlugValidator(sp.GetRequiredService<IAppDbContext>());
                return new InlineCompositeValidator<PageCreateDto>(baseValidator, unique);
            });

            services.AddScoped<IValidator<PageEditDto>>(sp =>
            {
                var baseValidator = new PageEditDtoValidator();
                var unique = new PageEditUniqueSlugValidator(sp.GetRequiredService<IAppDbContext>());
                return new InlineCompositeValidator<PageEditDto>(baseValidator, unique);
            });
            // Settings validator for SiteSettingDto (combined read/update).  We use
            // SiteSettingEditValidator as the unified validator for SiteSettingDto.
            services.AddScoped<IValidator<SiteSettingDto>, SiteSettingEditValidator>();

            // Composite validators for Brands (Create/Edit) combining base + unique slug
            services.AddScoped<IValidator<BrandCreateDto>>(sp =>
            {
                var baseValidator = new BrandCreateDtoValidator();
                var unique = new BrandCreateUniqueSlugValidator(sp.GetRequiredService<IAppDbContext>());
                return new InlineCompositeValidator<BrandCreateDto>(baseValidator, unique);
            });

            services.AddScoped<IValidator<BrandEditDto>>(sp =>
            {
                var baseValidator = new BrandEditDtoValidator();
                var unique = new BrandEditUniqueSlugValidator(sp.GetRequiredService<IAppDbContext>());
                return new InlineCompositeValidator<BrandEditDto>(baseValidator, unique);
            });

            // Composite validators for ShippingMethods (Create/Edit) combining base + unique name
            services.AddScoped<IValidator<ShippingMethodCreateDto>>(sp =>
            {
                var baseValidator = new ShippingMethodCreateValidator();
                var unique = new ShippingMethodCreateUniqueNameValidator(sp.GetRequiredService<IAppDbContext>());
                return new InlineCompositeValidator<ShippingMethodCreateDto>(baseValidator, unique);
            });

            services.AddScoped<IValidator<ShippingMethodEditDto>>(sp =>
            {
                var baseValidator = new ShippingMethodEditValidator();
                var unique = new ShippingMethodEditUniqueNameValidator(sp.GetRequiredService<IAppDbContext>());
                return new InlineCompositeValidator<ShippingMethodEditDto>(baseValidator, unique);
            });

            


            // -----------------------------------------------------------------------------
            // Cart handlers and validators
            //
            // The cart module allows managing shopping carts and their items in the Admin
            // interface (for support scenarios such as editing abandoned carts or
            // verifying orders).  We register the command/query handlers and explicit
            // validators for each DTO.  These validators enforce required fields and
            // concurrency tokens.  They are also discoverable via the assembly scan below.
            services.AddScoped<AddItemToCartHandler>();
            services.AddScoped<AddOrIncreaseCartItemHandler>();
            services.AddScoped<RemoveCartItemHandler>();
            services.AddScoped<ApplyCouponHandler>();
            services.AddScoped<UpdateCartItemQuantityHandler>();

            // DTO validators for the cart module.  Explicit registration ensures that
            // validators are available to the handlers via dependency injection.
            services.AddScoped<IValidator<CartKeyDto>, CartKeyValidator>();
            services.AddScoped<IValidator<CartAddItemDto>, CartAddItemValidator>();
            services.AddScoped<IValidator<CartUpdateQtyDto>, CartUpdateQtyValidator>();
            services.AddScoped<IValidator<CartRemoveItemDto>, CartRemoveItemValidator>();
            services.AddScoped<IValidator<CartApplyCouponDto>, CartApplyCouponValidator>();

            // Register all other validators from Application assembly
            services.AddValidatorsFromAssembly(Assembly.Load("Darwin.Application"), includeInternalTypes: true);

            return services;
        }

        /// <summary>
        /// Minimal composite validator that combines multiple FluentValidation validators without using
        /// an external library. It simply includes each provided validator into a single pipeline.
        /// </summary>
        /// <typeparam name="T">Type being validated.</typeparam>
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