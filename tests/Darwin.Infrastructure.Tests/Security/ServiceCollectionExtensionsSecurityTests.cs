using System;
using System.IO;
using Darwin.Infrastructure.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Infrastructure.Tests.Security;

public sealed class ServiceCollectionExtensionsSecurityTests
{
    [Fact]
    public void AddSharedHostingDataProtection_Should_RegisterDataProtection_WithConfiguredKeysPath()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "darwin-dpkeys-" + Guid.NewGuid());

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new[]
                    {
                        new KeyValuePair<string, string?>("DataProtection:KeysPath", tempPath)
                    })
                .Build();

            var services = new ServiceCollection();
            var returned = services.AddSharedHostingDataProtection(configuration);

            returned.Should().BeSameAs(services);
            Directory.Exists(tempPath).Should().BeTrue();

            using var serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetService<IDataProtectionProvider>()
                .Should().NotBeNull();
        }
        finally
        {
            Directory.Delete(tempPath, recursive: true);
        }
    }

    [Fact]
    public void AddSharedHostingDataProtection_Should_FallbackToDefaultPath_WhenPathNotConfigured()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        var fallbackPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Darwin",
            "DataProtectionKeys");

        services.AddSharedHostingDataProtection(configuration);

        Directory.Exists(fallbackPath).Should().BeTrue();
        using var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<IDataProtectionProvider>().Should().NotBeNull();
    }
}
