using System;
using System.Collections.Generic;
using System.IO;
using Darwin.Infrastructure.Extensions;
using Darwin.WebAdmin.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Darwin.WebAdmin.Tests.Security;

public sealed class DataProtectionCompositionTests
{
    [Fact]
    public void AddWebComposition_Should_UseConfiguredDataProtectionKeysPath_AndDefaultDarwinApplicationName()
    {
        var sharedPath = Path.Combine(Path.GetTempPath(), "darwin-webadmin-dataprotection-" + Guid.NewGuid());

        try
        {
            var configuration = BuildCompositionConfiguration(sharedPath);
            var services = new ServiceCollection();
            services.AddWebComposition(configuration);

            using var provider = services.BuildServiceProvider();
            var protector = provider
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("Darwin.WebAdmin.DataProtection.Composition");
            var appName = provider
                .GetRequiredService<IOptions<DataProtectionOptions>>()
                .Value
                .ApplicationDiscriminator;

        var roundTrip = provider
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("Darwin.WebAdmin.DataProtection.Composition")
                .Unprotect(protector.Protect("webadmin-dataprotection"));

            roundTrip.Should().Be("webadmin-dataprotection");
            Directory.Exists(sharedPath).Should().BeTrue();
            appName.Should().Be("Darwin");
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
    public void AddWebComposition_Should_FailToStart_WhenKeyEncryptionRequiredAndCertificateThumbprintCannotBeResolved()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "darwin-webadmin-dataprotection-fail-" + Guid.NewGuid());

        try
        {
            var configuration = BuildCompositionConfiguration(
                tempPath,
                requireKeyEncryption: true,
                certificateThumbprint: "DEADBEEF");

            var services = new ServiceCollection();
            Action act = () => services.AddWebComposition(configuration);

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
            new("Persistence:Provider", "SqlServer"),
            new("ConnectionStrings:DefaultConnection", "Server=(localdb)\\MSSQLLocalDB;Database=Darwin_WebAdmin_Unit;Trusted_Connection=True;TrustServerCertificate=True"),
            new("DataProtection:KeysPath", dataProtectionPath),
            new("DataProtection:RequireKeyEncryption", requireKeyEncryption.ToString().ToLowerInvariant()),
            new("Email:Provider", "SMTP")
        };

        if (!string.IsNullOrWhiteSpace(certificateThumbprint))
        {
            entries.Add(new("DataProtection:CertificateThumbprint", certificateThumbprint));
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(entries)
            .Build();
    }
}
