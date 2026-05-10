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
using Darwin.Application.CMS.Media.Commands;
using Darwin.Application.CMS.Media.Queries;
using Darwin.Application.CMS.Queries;
using Darwin.Application.CMS.Validators;
using Darwin.Application.CRM.Commands;
using Darwin.Application.CRM.Queries;
using Darwin.Application.Common.Queries;
using Darwin.Application.Common.Html;
using Darwin.Application.Extensions;
using Darwin.Application.Billing;
using Darwin.Application.Billing.Commands;
using Darwin.Application.Billing.Queries;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.Queries;
using Darwin.Application.Identity.Auth.Commands;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.Queries;
using Darwin.Application.Inventory.Commands;
using Darwin.Application.Inventory.Queries;
using Darwin.Application.Loyalty.Commands;
using Darwin.Application.Loyalty.Campaigns;
using Darwin.Application.Loyalty.Queries;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.Queries;
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
using Darwin.Infrastructure.Health;
using Darwin.Infrastructure.Media;
using Darwin.Infrastructure.Security.Jwt;
using Darwin.Infrastructure.Security.LoginRateLimiter;
using Darwin.WebAdmin.Auth;
using Darwin.WebAdmin.Infrastructure;
using Darwin.WebAdmin.Localization;
using Darwin.WebAdmin;
using Darwin.WebAdmin.Services.Admin;
using Darwin.WebAdmin.Services.Seo;
using Darwin.WebAdmin.Services.Security;
using Darwin.WebAdmin.Services.Settings;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Reflection;

namespace Darwin.WebAdmin.Extensions
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
            services.AddSingleton<IClock, SystemClock>();
            services.Configure<MediaStorageOptions>(config.GetSection(MediaStorageOptions.SectionName));

            // Identity handlers required for Auth flows
            services.AddScoped<SignInHandler>();
            services.AddScoped<RegisterUserHandler>();
            services.AddScoped<VerifyTotpForLoginHandler>();
            services.AddScoped<BeginLoginHandler>();
            services.AddScoped<FinishLoginHandler>();
            services.AddScoped<BeginRegistrationHandler>();
            services.AddScoped<FinishRegistrationHandler>();
            services.AddScoped<GetRoleIdByKeyHandler>();

            // required by AccountController constructor
            services.AddScoped<GetSecurityStampHandler>();
            services.AddScoped<AdminReferenceDataService>();
            services.AddScoped<GetMobileDeviceOpsSummaryHandler>();
            services.AddScoped<GetMobileDevicesPageHandler>();
            services.AddScoped<ClearUserDevicePushTokenHandler>();
            services.AddScoped<DeactivateUserDeviceHandler>();
            services.Configure<AuthAntiBotOptions>(config.GetSection("Security:AuthAntiBot"));
            services.AddSingleton<IAuthAntiBotChallengeService, ProtectedAuthAntiBotChallengeService>();
            services.AddSingleton<Application.Abstractions.Security.IAuthAntiBotVerifier>(sp =>
                sp.GetRequiredService<IAuthAntiBotChallengeService>());
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = 1;

                foreach (var proxy in config.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? Array.Empty<string>())
                {
                    if (IPAddress.TryParse(proxy, out var address))
                    {
                        options.KnownProxies.Add(address);
                    }
                }

                foreach (var network in config.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? Array.Empty<string>())
                {
                    if (System.Net.IPNetwork.TryParse(network, out var ipNetwork))
                    {
                        options.KnownIPNetworks.Add(ipNetwork);
                    }
                }
            });

            // Password hashing (Argon2id), security stamps, WebAuthn, TOTP, and JWT helpers.
            services.AddIdentityInfrastructure();
            services.AddJwtAuthCore();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddSingleton<Application.Abstractions.Security.ILoginRateLimiter, MemoryLoginRateLimiter>();



            services.AddHttpContextAccessor();
            services.AddScoped<PermissionRazorHelper>();
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddScoped<IAdminTextLocalizer, AdminTextLocalizer>();
            services.AddSingleton<IDisplayMetadataProvider, SharedDisplayMetadataProvider>();
            services.AddSingleton<IConfigureOptions<MvcOptions>, ConfigureDisplayMetadataLocalization>();

            // Cookie authentication (default scheme for Challenge/SignIn/SignOut)
            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    // Login/Logout/AccessDenied endpoints
                    options.LoginPath = "/account/login";
                    options.LogoutPath = "/account/logout";
                    options.AccessDeniedPath = "/account/login";

                    // Cookie options
                    options.Cookie.Name = "Darwin.Auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromDays(30);

                    // Optional: returnUrl parameter name (defaults to ReturnUrl)
                    // options.ReturnUrlParameter = "returnUrl";
                });


            // Transactional email infrastructure; provider selection is configuration-driven.
            services.AddObjectStorageInfrastructure(config);
            services.AddNotificationsInfrastructure(config);
            services.AddComplianceInfrastructure(config);


            // Register handlers — Products
            services.AddScoped<CreateProductHandler>();
            services.AddScoped<UpdateProductHandler>();
            services.AddScoped<GetProductsPageHandler>();
            services.AddScoped<GetProductOpsSummaryHandler>();
            services.AddScoped<GetProductForEditHandler>();
            services.AddScoped<SoftDeleteProductHandler>();

            // Register handlers — Categories
            services.AddScoped<CreateCategoryHandler>();
            services.AddScoped<UpdateCategoryHandler>();
            services.AddScoped<GetCategoriesPageHandler>();
            services.AddScoped<GetCategoryOpsSummaryHandler>();
            services.AddScoped<GetCategoryForEditHandler>();
            services.AddScoped<SoftDeleteCategoryHandler>();

            // Register handlers — CMS pages
            services.AddScoped<CreatePageHandler>();
            services.AddScoped<UpdatePageHandler>();
            services.AddScoped<GetPagesPageHandler>();
            services.AddScoped<GetPageOpsSummaryHandler>();
            services.AddScoped<GetPageForEditHandler>();
            services.AddScoped<SoftDeletePageHandler>();
            services.AddScoped<GetMenusPageHandler>();
            services.AddScoped<GetMenuForEditHandler>();
            services.AddScoped<CreateMenuHandler>();
            services.AddScoped<UpdateMenuHandler>();
            services.AddScoped<GetMediaAssetsPageHandler>();
            services.AddScoped<GetMediaAssetOpsSummaryHandler>();
            services.AddScoped<GetMediaAssetForEditHandler>();
            services.AddScoped<CreateMediaAssetHandler>();
            services.AddScoped<UpdateMediaAssetHandler>();
            services.AddScoped<SoftDeleteMediaAssetHandler>();
            services.AddScoped<PurgeUnusedMediaAssetHandler>();

            // Roles (Identity)
            services.AddScoped<GetRolesPageHandler>();
            services.AddScoped<GetRoleForEditHandler>();
            services.AddScoped<CreateRoleHandler>();
            services.AddScoped<UpdateRoleHandler>();
            services.AddScoped<DeleteRoleHandler>();

            // --- Identity: Users (Admin) ---
            services.AddScoped<GetUsersPageHandler>();
            services.AddScoped<GetUserOpsSummaryHandler>();
            services.AddScoped<GetUserForEditHandler>();
            services.AddScoped<UpdateUserHandler>();
            services.AddScoped<CreateUserHandler>();
            services.AddScoped<ChangeUserEmailHandler>();
            services.AddScoped<ChangePasswordHandler>();
            services.AddScoped<SetUserPasswordByAdminHandler>();
            services.AddScoped<RequestPasswordResetHandler>();
            services.AddScoped<RequestEmailConfirmationHandler>();
            services.AddScoped<ConfirmUserEmailByAdminHandler>();
            services.AddScoped<LockUserByAdminHandler>();
            services.AddScoped<UnlockUserByAdminHandler>();
            services.AddScoped<SoftDeleteUserHandler>();

            services.AddScoped<GetUserWithAddressesForEditHandler>();
            services.AddScoped<CreateUserAddressHandler>();
            services.AddScoped<UpdateUserAddressHandler>();
            services.AddScoped<SoftDeleteUserAddressHandler>();
            services.AddScoped<SetDefaultUserAddressHandler>();


            // --- Identity: Permissions (Admin) ---
            services.AddScoped<GetPermissionsPageHandler>();
            services.AddScoped<GetPermissionForEditHandler>();
            services.AddScoped<CreatePermissionHandler>();
            services.AddScoped<UpdatePermissionHandler>();
            services.AddScoped<SoftDeletePermissionHandler>();

            // --- Identity: Roles & Permissions (edit screens) ---
            services.AddScoped<GetRoleWithPermissionsForEditHandler>();
            services.AddScoped<UpdateRolePermissionsHandler>();

            // --- Identity: User ? Roles (edit screens) ---
            services.AddScoped<GetUserWithRolesForEditHandler>();
            services.AddScoped<UpdateUserRolesHandler>();


            // Register handlers — Shipping methods (admin)
            services.AddScoped<CreateShippingMethodHandler>();
            services.AddScoped<UpdateShippingMethodHandler>();
            services.AddScoped<GetShippingMethodsPageHandler>();
            services.AddScoped<GetShippingMethodOpsSummaryHandler>();
            services.AddScoped<GetShippingMethodForEditHandler>();

            // Lookups and cultures
            services.AddScoped<GetCatalogLookupsHandler>();
            services.AddScoped<GetCulturesHandler>();

            // Site settings handlers
            services.AddScoped<GetSiteSettingHandler>();
            services.AddScoped<UpdateSiteSettingHandler>();

            // HttpContext accessor and user service
            services.AddScoped<ICanonicalUrlService, CanonicalUrlService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            // Settings cache
            services.AddMemoryCache();
            services.AddHealthChecks()
                .AddCheck<DarwinDbContextHealthCheck>("database", tags: new[] { "ready" });
            services.AddScoped<ISiteSettingCache, SiteSettingCache>();
            services.AddScoped<IBusinessEffectiveSettingsCache, BusinessEffectiveSettingsCache>();

            // Brands
            services.AddScoped<GetBrandsPageHandler>();
            services.AddScoped<GetBrandOpsSummaryHandler>();
            services.AddScoped<GetBrandForEditHandler>();
            services.AddScoped<CreateBrandHandler>();
            services.AddScoped<UpdateBrandHandler>();
            services.AddScoped<SoftDeleteBrandHandler>();
            services.AddScoped<GetBusinessesPageHandler>();
            services.AddScoped<GetBusinessSupportSummaryHandler>();
            services.AddScoped<GetBusinessCommunicationOpsSummaryHandler>();
            services.AddScoped<GetBusinessCommunicationSetupPageHandler>();
            services.AddScoped<GetBusinessCommunicationProfileHandler>();
            services.AddScoped<GetEmailDispatchAuditsPageHandler>();
            services.AddScoped<GetChannelDispatchActivityHandler>();
            services.AddScoped<GetProviderCallbackInboxPageHandler>();
            services.AddScoped<RetryEmailDispatchAuditHandler>();
            services.AddScoped<CancelCommunicationDispatchOperationHandler>();
            services.AddScoped<UpdateProviderCallbackInboxMessageHandler>();
            services.AddScoped<GetBusinessForEditHandler>();
            services.AddScoped<GetBusinessOnboardingCustomerProfileHandler>();
            services.AddScoped<CreateBusinessHandler>();
            services.AddScoped<EnsureBusinessOnboardingCustomerProfileHandler>();
            services.AddScoped<ProvisionBusinessOnboardingHandler>();
            services.AddScoped<UpdateBusinessHandler>();
            services.AddScoped<SoftDeleteBusinessHandler>();
            services.AddScoped<ApproveBusinessHandler>();
            services.AddScoped<SuspendBusinessHandler>();
            services.AddScoped<ReactivateBusinessHandler>();
            services.AddScoped<GetBusinessLocationsPageHandler>();
            services.AddScoped<GetBusinessLocationForEditHandler>();
            services.AddScoped<CreateBusinessLocationHandler>();
            services.AddScoped<UpdateBusinessLocationHandler>();
            services.AddScoped<SoftDeleteBusinessLocationHandler>();
            services.AddScoped<GetBusinessMembersPageHandler>();
            services.AddScoped<GetBusinessMemberForEditHandler>();
            services.AddScoped<GetBusinessOwnerOverrideAuditsPageHandler>();
            services.AddScoped<CreateBusinessMemberHandler>();
            services.AddScoped<UpdateBusinessMemberHandler>();
            services.AddScoped<DeleteBusinessMemberHandler>();
            services.AddScoped<GetBusinessInvitationsPageHandler>();
            services.AddScoped<CreateBusinessInvitationHandler>();
            services.AddScoped<ResendBusinessInvitationHandler>();
            services.AddScoped<RevokeBusinessInvitationHandler>();

            // Orders – queries
            services.AddScoped<GetOrdersPageHandler>();
            services.AddScoped<GetShipmentsPageHandler>();
            services.AddScoped<GetShipmentOpsSummaryHandler>();
            services.AddScoped<GetShipmentProviderOperationsPageHandler>();
            services.AddScoped<GetOrderForViewHandler>();
            services.AddScoped<GetOrderPaymentsPageHandler>();
            services.AddScoped<GetOrderShipmentsPageHandler>();
            services.AddScoped<GetOrderRefundsPageHandler>();
            services.AddScoped<GetOrderInvoicesPageHandler>();
            services.AddScoped<GetWarehouseLookupHandler>();
            services.AddScoped<GetBusinessLookupHandler>();
            services.AddScoped<GetUserLookupHandler>();
            services.AddScoped<GetCustomerLookupHandler>();
            services.AddScoped<GetCustomerSegmentLookupHandler>();
            services.AddScoped<GetProductVariantLookupHandler>();
            services.AddScoped<GetSupplierLookupHandler>();
            services.AddScoped<GetFinancialAccountLookupHandler>();
            services.AddScoped<GetPaymentLookupHandler>();
            services.AddScoped<GetBusinessSubscriptionStatusHandler>();
            services.AddScoped<GetBusinessSubscriptionInvoicesPageHandler>();
            services.AddScoped<GetBusinessSubscriptionInvoiceOpsSummaryHandler>();
            services.AddScoped<GetLoyaltyProgramsPageHandler>();
            services.AddScoped<GetLoyaltyProgramForEditHandler>();
            services.AddScoped<CreateLoyaltyProgramHandler>();
            services.AddScoped<UpdateLoyaltyProgramHandler>();
            services.AddScoped<SoftDeleteLoyaltyProgramHandler>();
            services.AddScoped<GetLoyaltyRewardTiersPageHandler>();
            services.AddScoped<GetLoyaltyRewardTierForEditHandler>();
            services.AddScoped<CreateLoyaltyRewardTierHandler>();
            services.AddScoped<UpdateLoyaltyRewardTierHandler>();
            services.AddScoped<SoftDeleteLoyaltyRewardTierHandler>();
            services.AddScoped<GetLoyaltyAccountsPageHandler>();
            services.AddScoped<GetRecentLoyaltyScanSessionsPageHandler>();
            services.AddScoped<GetLoyaltyRedemptionsPageHandler>();
            services.AddScoped<GetLoyaltyAccountForAdminHandler>();
            services.AddScoped<CreateLoyaltyAccountByAdminHandler>();
            services.AddScoped<GetLoyaltyAccountTransactionsHandler>();
            services.AddScoped<GetLoyaltyAccountRedemptionsHandler>();
            services.AddScoped<ConfirmLoyaltyRewardRedemptionHandler>();
            services.AddScoped<AdjustLoyaltyPointsHandler>();
            services.AddScoped<SuspendLoyaltyAccountHandler>();
            services.AddScoped<ActivateLoyaltyAccountHandler>();
            services.AddScoped<ExpireLoyaltyScanSessionHandler>();
            services.AddScoped<GetBusinessCampaignsHandler>();
            services.AddScoped<CreateBusinessCampaignHandler>();
            services.AddScoped<UpdateBusinessCampaignHandler>();
            services.AddScoped<SetCampaignActivationHandler>();
            services.AddScoped<GetCampaignDeliveriesPageHandler>();
            services.AddScoped<UpdateCampaignDeliveryStatusHandler>();

            // Orders – commands
            services.AddScoped<AddPaymentHandler>();
            services.AddScoped<AddShipmentHandler>();
            services.AddScoped<GenerateDhlShipmentLabelHandler>();
            services.AddScoped<ResolveShipmentCarrierExceptionHandler>();
            services.AddScoped<UpdateShipmentProviderOperationHandler>();
            services.AddScoped<AddRefundHandler>();
            services.AddScoped<CreateOrderInvoiceHandler>();
            services.AddScoped<UpdateOrderStatusHandler>();
            services.AddScoped<GetCustomersPageHandler>();
            services.AddScoped<GetCustomerForEditHandler>();
            services.AddScoped<GetCrmSummaryHandler>();
            services.AddScoped<GetCustomerInteractionsPageHandler>();
            services.AddScoped<GetLeadInteractionsPageHandler>();
            services.AddScoped<GetOpportunityInteractionsPageHandler>();
            services.AddScoped<GetCustomerConsentsPageHandler>();
            services.AddScoped<GetCustomerSegmentsPageHandler>();
            services.AddScoped<GetCustomerSegmentForEditHandler>();
            services.AddScoped<GetCustomerSegmentMembershipsHandler>();
            services.AddScoped<CreateCustomerHandler>();
            services.AddScoped<UpdateCustomerHandler>();
            services.AddScoped<UpdateCustomerTaxProfileHandler>();
            services.AddScoped<ValidateCustomerVatIdHandler>();
            services.AddScoped<GetLeadsPageHandler>();
            services.AddScoped<GetLeadForEditHandler>();
            services.AddScoped<CreateLeadHandler>();
            services.AddScoped<UpdateLeadHandler>();
            services.AddScoped<ConvertLeadToCustomerHandler>();
            services.AddScoped<UpdateLeadLifecycleHandler>();
            services.AddScoped<GetOpportunitiesPageHandler>();
            services.AddScoped<GetOpportunityForEditHandler>();
            services.AddScoped<CreateOpportunityHandler>();
            services.AddScoped<UpdateOpportunityHandler>();
            services.AddScoped<UpdateOpportunityLifecycleHandler>();
            services.AddScoped<CreateInteractionHandler>();
            services.AddScoped<CreateConsentHandler>();
            services.AddScoped<CreateCustomerSegmentHandler>();
            services.AddScoped<UpdateCustomerSegmentHandler>();
            services.AddScoped<AssignCustomerSegmentHandler>();
            services.AddScoped<RemoveCustomerSegmentMembershipHandler>();
            services.AddScoped<GetInvoicesPageHandler>();
            services.AddScoped<GetInvoiceForEditHandler>();
            services.AddScoped<GetInvoiceArchiveSnapshotHandler>();
            services.AddScoped<GetInvoiceArchiveDocumentHandler>();
            services.AddScoped<GetInvoiceStructuredDataExportHandler>();
            services.AddScoped<CreateInvoiceRefundHandler>();
            services.AddScoped<UpdateInvoiceHandler>();
            services.AddScoped<TransitionInvoiceStatusHandler>();
            services.AddScoped<GenerateInvoiceEInvoiceArtifactHandler>();
            services.AddScoped<UpdateInvoiceReverseChargeDecisionHandler>();
            services.AddScoped<UpdateCustomerVatValidationDecisionHandler>();
            services.AddScoped<GetPaymentsPageHandler>();
            services.AddScoped<GetPaymentOpsSummaryHandler>();
            services.AddScoped<GetTaxComplianceOverviewHandler>();
            services.AddScoped<GetTaxComplianceInvoiceExportHandler>();
            services.AddScoped<GetBillingPlansAdminPageHandler>();
            services.AddScoped<GetBillingPlanOpsSummaryHandler>();
            services.AddScoped<GetBillingPlanForEditHandler>();
            services.AddScoped<GetBillingWebhookSubscriptionsPageHandler>();
            services.AddScoped<GetBillingWebhookDeliveriesPageHandler>();
            services.AddScoped<GetBillingWebhookOpsSummaryHandler>();
            services.AddScoped<UpdateBillingWebhookDeliveryHandler>();
            services.AddScoped<UpdatePaymentDisputeReviewHandler>();
            services.AddScoped<GetPaymentForEditHandler>();
            services.AddScoped<GetRefundsPageHandler>();
            services.AddScoped<GetRefundOpsSummaryHandler>();
            services.AddScoped<CreateBillingPlanHandler>();
            services.AddScoped<UpdateBillingPlanHandler>();
            services.AddScoped<CreatePaymentHandler>();
            services.AddScoped<UpdatePaymentHandler>();
            services.AddScoped<GetFinancialAccountsPageHandler>();
            services.AddScoped<GetFinancialAccountForEditHandler>();
            services.AddScoped<GetBillingPlansHandler>();
            services.AddScoped<CreateSubscriptionCheckoutIntentHandler>();
            services.AddScoped<SetCancelAtPeriodEndHandler>();
            services.AddScoped<CreateFinancialAccountHandler>();
            services.AddScoped<UpdateFinancialAccountHandler>();
            services.AddScoped<GetExpensesPageHandler>();
            services.AddScoped<GetExpenseForEditHandler>();
            services.AddScoped<CreateExpenseHandler>();
            services.AddScoped<UpdateExpenseHandler>();
            services.AddScoped<GetJournalEntriesPageHandler>();
            services.AddScoped<GetJournalEntryForEditHandler>();
            services.AddScoped<CreateJournalEntryHandler>();
            services.AddScoped<UpdateJournalEntryHandler>();
            services.AddScoped<GetWarehousesPageHandler>();
            services.AddScoped<GetWarehouseForEditHandler>();
            services.AddScoped<CreateWarehouseHandler>();
            services.AddScoped<UpdateWarehouseHandler>();
            services.AddScoped<GetSuppliersPageHandler>();
            services.AddScoped<GetSupplierForEditHandler>();
            services.AddScoped<CreateSupplierHandler>();
            services.AddScoped<UpdateSupplierHandler>();
            services.AddScoped<GetStockLevelsPageHandler>();
            services.AddScoped<GetStockLevelForEditHandler>();
            services.AddScoped<CreateStockLevelHandler>();
            services.AddScoped<UpdateStockLevelHandler>();
            services.AddScoped<GetStockTransfersPageHandler>();
            services.AddScoped<GetStockTransferForEditHandler>();
            services.AddScoped<CreateStockTransferHandler>();
            services.AddScoped<UpdateStockTransferHandler>();
            services.AddScoped<UpdateStockTransferLifecycleHandler>();
            services.AddScoped<GetPurchaseOrdersPageHandler>();
            services.AddScoped<GetPurchaseOrderForEditHandler>();
            services.AddScoped<CreatePurchaseOrderHandler>();
            services.AddScoped<UpdatePurchaseOrderHandler>();
            services.AddScoped<UpdatePurchaseOrderLifecycleHandler>();
            services.AddScoped<GetInventoryLedgerHandler>();

            // Inventory – commands
            services.AddScoped<AdjustInventoryHandler>();
            services.AddScoped<ReserveInventoryHandler>();
            services.AddScoped<ReleaseInventoryReservationHandler>();
            services.AddScoped<ProcessReturnReceiptHandler>();
            services.AddScoped<AllocateInventoryForOrderHandler>();


            // inside AddWebComposition(...) or your DI extension where other handlers are registered
            services.AddScoped<GetAddOnGroupsPageHandler>();
            services.AddScoped<GetAddOnGroupOpsSummaryHandler>();
            services.AddScoped<GetAddOnGroupForEditHandler>();
            services.AddScoped<CreateAddOnGroupHandler>();
            services.AddScoped<UpdateAddOnGroupHandler>();
            services.AddScoped<SoftDeleteAddOnGroupHandler>();
            services.AddScoped<AttachAddOnGroupToVariantsHandler>();
            services.AddScoped<AttachAddOnGroupToProductsHandler>();
            services.AddScoped<AttachAddOnGroupToCategoriesHandler>();
            services.AddScoped<AttachAddOnGroupToBrandsHandler>();
            services.AddScoped<GetApplicableAddOnGroupsForProductHandler>();
            services.AddScoped<GetVariantsPageHandler>();
            services.AddScoped<GetAddOnGroupAttachedProductIdsHandler>();
            services.AddScoped<GetAddOnGroupAttachedVariantIdsHandler>();
            services.AddScoped<GetAddOnGroupAttachedCategoryIdsHandler>();
            services.AddScoped<GetAddOnGroupAttachedBrandIdsHandler>();


            // MVC and localization. RuntimeCompilation is useful during development; remove in production.
            services
                .AddControllersWithViews(options =>
                {
                    // Avoid implicit [Required] for non-nullable reference types.
                    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                })
                .AddViewLocalization()
                .AddDataAnnotationsLocalization(options =>
                {
                    options.DataAnnotationLocalizerProvider = (_, factory) =>
                        factory.Create(typeof(SharedResource));
                })
#if DEBUG
                .AddRazorRuntimeCompilation()
#endif
                ;

            // Application layer (includes AutoMapper + validators scanning)
            services.AddApplication(config);

            // Register Application services with concrete types
            services.AddScoped<IAddOnPricingService, AddOnPricingService>();


            services.AddSharedHostingDataProtection(config);

            // Persistence (DbContext + IAppDbContext + Seeder)
            services.AddConfiguredPersistence(config);



            // Anti-forgery defaults for Admin forms and HTMX mutations.
            services.AddAntiforgery(options =>
            {
                options.HeaderName = "RequestVerificationToken";
                options.Cookie.Name = "Darwin.AntiForgery";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            // HTML Sanitizer using our factory (singleton across the app)
            services.AddSingleton<IHtmlSanitizer>(_ => HtmlSanitizerFactory.Create());

            // Composite validators for Product (Create/Edit) combining base + unique slug
            services.AddScoped<IValidator<ProductCreateDto>>(sp =>
            {
                // Base product validation (translations, variants, required fields)
                var baseValidator = new ProductCreateDtoValidator(sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Darwin.Application.ValidationResource>>());
                // Unique slug validation for product
                var unique = new ProductCreateUniqueSlugValidator(sp.GetRequiredService<IAppDbContext>(), sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Darwin.Application.ValidationResource>>());
                return new InlineCompositeValidator<ProductCreateDto>(baseValidator, unique);
            });

            services.AddScoped<IValidator<ProductEditDto>>(sp =>
            {
                var baseValidator = new ProductEditDtoValidator(sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Darwin.Application.ValidationResource>>());
                var unique = new ProductEditUniqueSlugValidator(sp.GetRequiredService<IAppDbContext>(), sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Darwin.Application.ValidationResource>>());
                return new InlineCompositeValidator<ProductEditDto>(baseValidator, unique);
            });

            // Composite validators for Category (Create/Edit) combining base + unique slug
            services.AddScoped<IValidator<CategoryCreateDto>>(sp =>
            {
                var baseValidator = new CategoryCreateDtoValidator(sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Darwin.Application.ValidationResource>>());
                var unique = new CategoryCreateUniqueSlugValidator(sp.GetRequiredService<IAppDbContext>(), sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Darwin.Application.ValidationResource>>());
                return new InlineCompositeValidator<CategoryCreateDto>(baseValidator, unique);
            });

            services.AddScoped<IValidator<CategoryEditDto>>(sp =>
            {
                var baseValidator = new CategoryEditDtoValidator(sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Darwin.Application.ValidationResource>>());
                var unique = new CategoryEditUniqueSlugValidator(sp.GetRequiredService<IAppDbContext>(), sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Darwin.Application.ValidationResource>>());
                return new InlineCompositeValidator<CategoryEditDto>(baseValidator, unique);
            });

            // Composite validators for CMS pages (Create/Edit) combining base + unique slug
            services.AddScoped<IValidator<PageCreateDto>>(sp =>
            {
                var baseValidator = new PageCreateDtoValidator(sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Darwin.Application.ValidationResource>>());
                var unique = new PageCreateUniqueSlugValidator(sp.GetRequiredService<IAppDbContext>(), sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Darwin.Application.ValidationResource>>());
                return new InlineCompositeValidator<PageCreateDto>(baseValidator, unique);
            });

            services.AddScoped<IValidator<PageEditDto>>(sp =>
            {
                var baseValidator = new PageEditDtoValidator(sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Darwin.Application.ValidationResource>>());
                var unique = new PageEditUniqueSlugValidator(sp.GetRequiredService<IAppDbContext>(), sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Darwin.Application.ValidationResource>>());
                return new InlineCompositeValidator<PageEditDto>(baseValidator, unique);
            });
            // Settings validator for SiteSettingDto (combined read/update).  We use
            // SiteSettingEditValidator as the unified validator for SiteSettingDto.
            services.AddScoped<IValidator<SiteSettingDto>, SiteSettingEditValidator>();

            // Composite validators for Brands (Create/Edit) combining base + unique slug
            services.AddScoped<IValidator<BrandCreateDto>>(sp =>
            {
                var baseValidator = new BrandCreateDtoValidator(sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Darwin.Application.ValidationResource>>());
                var unique = new BrandCreateUniqueSlugValidator(sp.GetRequiredService<IAppDbContext>(), sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Darwin.Application.ValidationResource>>());
                return new InlineCompositeValidator<BrandCreateDto>(baseValidator, unique);
            });

            services.AddScoped<IValidator<BrandEditDto>>(sp =>
            {
                var baseValidator = new BrandEditDtoValidator(sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Darwin.Application.ValidationResource>>());
                var unique = new BrandEditUniqueSlugValidator(sp.GetRequiredService<IAppDbContext>(), sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Darwin.Application.ValidationResource>>());
                return new InlineCompositeValidator<BrandEditDto>(baseValidator, unique);
            });

            // Composite validators for ShippingMethods (Create/Edit) combining base + unique name
            services.AddScoped<IValidator<ShippingMethodCreateDto>>(sp =>
            {
                var baseValidator = new ShippingMethodCreateValidator();
                var unique = new ShippingMethodCreateUniqueNameValidator(sp.GetRequiredService<IAppDbContext>(), sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Darwin.Application.ValidationResource>>());
                return new InlineCompositeValidator<ShippingMethodCreateDto>(baseValidator, unique);
            });

            services.AddScoped<IValidator<ShippingMethodEditDto>>(sp =>
            {
                var baseValidator = new ShippingMethodEditValidator();
                var unique = new ShippingMethodEditUniqueNameValidator(sp.GetRequiredService<IAppDbContext>(), sp.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Darwin.Application.ValidationResource>>());
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
