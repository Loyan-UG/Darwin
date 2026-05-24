using System;
using System.Collections.Generic;
using System.IO;
using Darwin.Application;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Extensions;
using Darwin.Infrastructure.Adapters.Time;
using Darwin.Infrastructure.Extensions;
using Darwin.Infrastructure.Media;
using Darwin.WebApi.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Darwin.WebApi.Tests.Composition;

public sealed class DataProtectionCompositionTests
{
    [Fact]
    public void AddWebApiAndWorker_Should_ShareConfiguredDataProtection_Settings_WithDarwinApplicationName()
    {
        var sharedPath = Path.Combine(Path.GetTempPath(), "darwin-shared-dataprotection-" + Guid.NewGuid());

        try
        {
            var webApiConfiguration = BuildCompositionConfiguration(sharedPath);
            var webApiServices = new ServiceCollection();
            webApiServices.AddWebApiComposition(webApiConfiguration);

            using var webApiProvider = webApiServices.BuildServiceProvider();
            var webApiProtector = webApiProvider
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("Darwin.DataProtection.Composition");
            var webApiAppName = webApiProvider
                .GetRequiredService<IOptions<DataProtectionOptions>>()
                .Value
                .ApplicationDiscriminator;

            var workerServices = new ServiceCollection();
            AddWorkerCompositionServices(workerServices, BuildCompositionConfiguration(sharedPath));
            using var workerProvider = workerServices.BuildServiceProvider();
            var workerProtector = workerProvider
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("Darwin.DataProtection.Composition");
            var workerAppName = workerProvider
                .GetRequiredService<IOptions<DataProtectionOptions>>()
                .Value
                .ApplicationDiscriminator;

            var protectedValue = webApiProtector.Protect("shared-dataprotection");
            var roundTripValue = workerProtector.Unprotect(protectedValue);

            roundTripValue.Should().Be("shared-dataprotection");
            Directory.Exists(sharedPath).Should().BeTrue();
            webApiAppName.Should().Be("Darwin");
            workerAppName.Should().Be("Darwin");
        }
        finally
        {
            if (Directory.Exists(sharedPath))
            {
                Directory.Delete(sharedPath, recursive: true);
            }
        }
    }

    [Fact]
    public void AddWebApiComposition_Should_FailToStart_WhenKeyEncryptionRequiredAndCertificateThumbprintCannotBeResolved()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "darwin-webapi-dataprotection-fail-" + Guid.NewGuid());

        try
        {
            var configuration = BuildCompositionConfiguration(
                tempPath,
                requireKeyEncryption: true,
                certificateThumbprint: "DEADBEEF");

            var services = new ServiceCollection();
            Action act = () => services.AddWebApiComposition(configuration);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*DataProtection:CertificateThumbprint is configured*");
        }
        finally
        {
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, recursive: true);
            }
        }
    }

    [Fact]
    public void AddWorkerComposition_Should_FailToStart_WhenKeyEncryptionRequiredAndCertificateThumbprintCannotBeResolved()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "darwin-worker-dataprotection-fail-" + Guid.NewGuid());

        try
        {
            var configuration = BuildCompositionConfiguration(
                tempPath,
                requireKeyEncryption: true,
                certificateThumbprint: "DEADBEEF");

            var services = new ServiceCollection();
            Action act = () => AddWorkerCompositionServices(services, configuration);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*DataProtection:CertificateThumbprint is configured*");
        }
        finally
        {
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, recursive: true);
            }
        }
    }

    private static IConfiguration BuildCompositionConfiguration(
        string dataProtectionPath,
        bool requireKeyEncryption = false,
        string? certificateThumbprint = null)
    {
        var entries = new List<KeyValuePair<string, string?>>
        {
            new("Persistence:Provider", "PostgreSql"),
            new("ConnectionStrings:PostgreSql", "Host=localhost;Database=darwin;Username=postgres;Password=postgres;"),
            new("DataProtection:KeysPath", dataProtectionPath),
            new("DataProtection:RequireKeyEncryption", requireKeyEncryption.ToString().ToLowerInvariant())
        };

        if (!string.IsNullOrWhiteSpace(certificateThumbprint))
        {
            entries.Add(new("DataProtection:CertificateThumbprint", certificateThumbprint));
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(entries)
            .Build();
    }

    private static void AddWorkerCompositionServices(IServiceCollection services, IConfiguration configuration)
    {
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
    }
}
