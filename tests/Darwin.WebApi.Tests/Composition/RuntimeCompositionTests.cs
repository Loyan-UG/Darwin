using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Security;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Abstractions.Storage;
using Darwin.Application.Billing;
using Darwin.Application.CRM.Commands;
using Darwin.Application.Extensions;
using Darwin.Application.Identity.Services;
using Darwin.Application.Notifications;
using Darwin.Application.Orders.Commands;
using Darwin.Infrastructure.Adapters.Time;
using Darwin.Infrastructure.Extensions;
using Darwin.Infrastructure.Media;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Infrastructure.Security.Secrets;
using Darwin.Worker;
using Darwin.WebApi.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Darwin.WebApi.Tests.Composition;

public sealed class RuntimeCompositionTests
{
    [Fact]
    public void AddWebApiComposition_Should_RegisterPostgreSqlCompositionCoreServices()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "darwin-webapi-composition-" + Guid.NewGuid());
        var configuration = BuildCompositionConfiguration(tempPath);

        try
        {
            var services = new ServiceCollection();
            services.AddWebApiComposition(configuration);

            using var provider = services.BuildServiceProvider();

            provider.GetRequiredService<IStringLocalizerFactory>().Should().NotBeNull();
            provider.GetRequiredService<IClock>().Should().NotBeNull();
            provider.GetRequiredService<IDataProtectionProvider>().Should().NotBeNull();
            provider.GetRequiredService<IUserPasswordHasher>().Should().NotBeNull();
            provider.GetRequiredService<ISecretProtector>().Should().NotBeNull();
            provider.GetRequiredService<IPermissionService>().Should().NotBeNull();
            provider.GetRequiredService<ITotpService>().Should().NotBeNull();
            provider.GetRequiredService<IWebAuthnService>().Should().NotBeNull();
            provider.GetRequiredService<IAuthAntiBotVerifier>().Should().NotBeNull();
            provider.GetRequiredService<DbContextOptions<DarwinDbContext>>().Extensions
                .OfType<IDbContextOptionsExtension>()
                .Any(x => x.GetType().Name.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
                .Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }
        }
    }

    [Fact]
    public void AddWorkerServices_Should_ValidateBackgroundWorkerDI_WhenPostgreSqlIsConfigured()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "darwin-worker-composition-" + Guid.NewGuid());
        var configuration = BuildCompositionConfiguration(tempPath);

        try
        {
            var services = new ServiceCollection();
            services.AddApplication(configuration);
            services.AddLocalization();
            services.AddSingleton<IClock, SystemClock>();
            services.Configure<MediaStorageOptions>(configuration.GetSection(MediaStorageOptions.SectionName));
            services.AddConfiguredPersistence(configuration);
            services.AddSharedHostingDataProtection(configuration);
            services.AddIdentityInfrastructure();
            services.AddObjectStorageInfrastructure(configuration);
            services.AddNotificationsInfrastructure(configuration);
            services.AddPaymentProviderInfrastructure();
            services.AddShippingProviderInfrastructure();
            services.AddComplianceInfrastructure(configuration);
            services.AddHttpClient();
            services.Configure<InactiveReminderWorkerOptions>(configuration.GetSection("InactiveReminderWorker"));
            services.Configure<EmailDispatchOperationWorkerOptions>(configuration.GetSection("EmailDispatchOperationWorker"));
            services.Configure<ChannelDispatchOperationWorkerOptions>(configuration.GetSection("ChannelDispatchOperationWorker"));
            services.Configure<ProviderCallbackWorkerOptions>(configuration.GetSection("ProviderCallbackWorker"));
            services.Configure<ShipmentProviderOperationWorkerOptions>(configuration.GetSection("ShipmentProviderOperationWorker"));
            services.Configure<WebhookDeliveryWorkerOptions>(configuration.GetSection("WebhookDeliveryWorker"));
            services.Configure<InvoiceArchiveMaintenanceWorkerOptions>(configuration.GetSection("InvoiceArchiveMaintenanceWorker"));
            services.Configure<VatValidationRetryWorkerOptions>(configuration.GetSection("VatValidationRetryWorker"));
            services.AddScoped<ApplyDhlShipmentCreateOperationHandler>();
            services.AddScoped<ApplyDhlShipmentLabelOperationHandler>();
            services.AddScoped<ApplyDhlReturnShipmentCreateOperationHandler>();
            services.AddScoped<ApplyShipmentCarrierEventHandler>();
            services.AddScoped<ProcessStripeWebhookHandler>();
            services.AddScoped<ProcessBrevoTransactionalEmailWebhookHandler>();
            services.AddScoped<PurgeExpiredInvoiceArchivesHandler>();
            services.AddScoped<RetryUnknownCustomerVatValidationBatchHandler>();
            services.AddHostedService<EmailDispatchOperationBackgroundService>();
            services.AddHostedService<ChannelDispatchOperationBackgroundService>();
            services.AddHostedService<InactiveReminderBackgroundService>();
            services.AddHostedService<ProviderCallbackBackgroundService>();
            services.AddHostedService<ShipmentProviderOperationBackgroundService>();
            services.AddHostedService<WebhookDeliveryBackgroundService>();
            services.AddHostedService<InvoiceArchiveMaintenanceBackgroundService>();
            services.AddHostedService<VatValidationRetryBackgroundService>();

            using var provider = services.BuildServiceProvider();

            provider.GetRequiredService<IStringLocalizerFactory>().Should().NotBeNull();
            provider.GetRequiredService<IClock>().Should().NotBeNull();
            provider.GetRequiredService<IDataProtectionProvider>().Should().NotBeNull();
            provider.GetRequiredService<IUserPasswordHasher>().Should().NotBeNull();
            provider.GetRequiredService<ISecretProtector>().Should().NotBeNull();
            provider.GetRequiredService<IPermissionService>().Should().NotBeNull();
            provider.GetRequiredService<ITotpService>().Should().NotBeNull();
            provider.GetRequiredService<IWebAuthnService>().Should().NotBeNull();
            var hostedServices = provider.GetServices<IHostedService>().ToList();
            hostedServices.Should().Contain(x => x is EmailDispatchOperationBackgroundService);
            hostedServices.Should().Contain(x => x is ChannelDispatchOperationBackgroundService);
            hostedServices.Should().Contain(x => x is InactiveReminderBackgroundService);
            hostedServices.Should().Contain(x => x is ProviderCallbackBackgroundService);
            hostedServices.Should().Contain(x => x is ShipmentProviderOperationBackgroundService);
            hostedServices.Should().Contain(x => x is WebhookDeliveryBackgroundService);
            hostedServices.Should().Contain(x => x is InvoiceArchiveMaintenanceBackgroundService);
            hostedServices.Should().Contain(x => x is VatValidationRetryBackgroundService);
            hostedServices.Should().HaveCount(8);
        }
        finally
        {
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }
        }
    }

    private static IConfiguration BuildCompositionConfiguration(string dataProtectionPath)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Persistence:Provider", "PostgreSql"),
                new KeyValuePair<string, string?>("ConnectionStrings:PostgreSql", "Host=localhost;Database=darwin;Username=postgres;Password=postgres;"),
                new KeyValuePair<string, string?>("DataProtection:KeysPath", dataProtectionPath)
            })
            .Build();
    }
}
