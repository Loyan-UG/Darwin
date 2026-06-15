using AutoMapper;
using Darwin.Application.Abstractions.Invoicing;
using Darwin.Application.Billing.Services;
using Darwin.Application.Catalog.Services;
using Darwin.Application.CRM.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Orders.Services;
using Darwin.Application.Sales.Services;
using Darwin.Application.Integration;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration? configuration = null)
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
            services.AddSingleton(_ =>
            {
                var selection = new InvoiceArchiveStorageSelection();
                configuration?.GetSection("InvoiceArchiveStorage").Bind(selection);
                return selection;
            });
            services.AddSingleton(_ =>
            {
                var options = new FileSystemInvoiceArchiveStorageOptions();
                configuration?.GetSection("InvoiceArchiveStorage:FileSystem").Bind(options);
                return options;
            });
            services.AddScoped<DatabaseInvoiceArchiveStorage>();
            services.AddScoped<FileSystemInvoiceArchiveStorage>();
            services.AddScoped<IInvoiceArchiveStorageProvider>(provider => provider.GetRequiredService<DatabaseInvoiceArchiveStorage>());
            services.AddScoped<IInvoiceArchiveStorageProvider>(provider => provider.GetRequiredService<FileSystemInvoiceArchiveStorage>());
            if (IsExternalObjectStorageProviderSelected(configuration))
            {
                services.AddScoped<ObjectStorageInvoiceArchiveStorage>();
                services.AddScoped<IInvoiceArchiveStorageProvider>(provider => provider.GetRequiredService<ObjectStorageInvoiceArchiveStorage>());
                services.AddScoped<IEInvoiceArtifactStorage, ObjectStorageEInvoiceArtifactStorage>();
            }
            else
            {
                services.AddScoped<IEInvoiceArtifactStorage, NullEInvoiceArtifactStorage>();
            }

            services.AddScoped<IInvoiceArchiveStorage, InvoiceArchiveStorageRouter>();
            services.AddScoped<CustomFieldService>();
            services.AddScoped<EntityTimelineService>();
            services.AddScoped<DocumentRecordService>();
            services.AddScoped<NumberSequenceService>();
            services.AddScoped<FinancePostingService>();
            services.AddScoped<FinanceAccountMappingService>();
            services.AddScoped<FinanceExportBatchService>();
            services.AddScoped<FinanceExportPackageBuilderService>();
            services.AddScoped<FinanceExportPackageStorageService>();
            services.AddScoped<FinanceExportConnectorDeliveryService>();
            services.AddScoped<ReceivablesProjectionService>();
            services.AddScoped<FinanceReceivablesPostingService>();
            services.AddScoped<OrderCreationService>();
            services.AddScoped<BusinessEventService>();
            services.AddScoped<SalesLifecycleEventService>();
            services.AddScoped<SalesQuoteLifecycleEventService>();
            services.AddScoped<DeliveryNoteLifecycleEventService>();
            services.AddScoped<DeliveryNoteWorkflowPolicy>();
            services.AddScoped<ReturnOrderLifecycleEventService>();
            services.AddScoped<ReturnOrderWorkflowPolicy>();
            services.AddScoped<CreditNoteLifecycleEventService>();
            services.AddScoped<CreditNoteWorkflowPolicy>();
            services.AddScoped<FeatureAreaService>();
            services.AddScoped<ExternalSystemReferenceService>();
            services.AddScoped<CrmFoundationPrimitiveService>();
            services.AddSingleton<EInvoiceSourceReadinessValidator>();
            // Default fallback contract: services.AddScoped<IEInvoiceGenerationService, NotConfiguredEInvoiceGenerationService>();
            services.TryAddScoped<IEInvoiceGenerationService, NotConfiguredEInvoiceGenerationService>();

            return services;
        }

        private static bool IsExternalObjectStorageProviderSelected(IConfiguration? configuration)
        {
            var providerName = configuration?["InvoiceArchiveStorage:ProviderName"];
            return string.Equals(providerName, InvoiceArchiveStorageProviderNames.S3Compatible, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(providerName, InvoiceArchiveStorageProviderNames.Minio, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(providerName, InvoiceArchiveStorageProviderNames.AwsS3, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(providerName, InvoiceArchiveStorageProviderNames.AzureBlob, StringComparison.OrdinalIgnoreCase);
        }
    }
}
