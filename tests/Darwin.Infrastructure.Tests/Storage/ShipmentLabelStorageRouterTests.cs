using Darwin.Application.Abstractions.Shipping;
using Darwin.Infrastructure.Extensions;
using Darwin.Infrastructure.Media;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Darwin.Infrastructure.Tests.Storage;

public sealed class ShipmentLabelStorageRouterTests
{
    [Fact]
    public async Task Router_Should_Keep_FileSystem_Fallback_When_ObjectStorage_Is_Not_Selected()
    {
        var root = Path.Combine(Path.GetTempPath(), $"darwin_label_storage_{Guid.NewGuid():N}");
        try
        {
            var configuration = new ConfigurationBuilder().Build();
            var services = new ServiceCollection();
            services.AddSingleton<IHostEnvironment>(new TestHostEnvironment(root));
            services.Configure<MediaStorageOptions>(options =>
            {
                options.RootPath = root;
                options.RequestPath = "/uploads";
            });
            services.AddObjectStorageInfrastructure(configuration);
            services.AddShippingProviderInfrastructure();

            using var provider = services.BuildServiceProvider(validateScopes: true);
            using var scope = provider.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<IShipmentLabelStorage>();
            var shipmentId = Guid.NewGuid();

            var url = await storage.SaveLabelAsync(
                shipmentId,
                "DHL",
                new byte[] { 1, 2, 3 },
                "application/pdf",
                TestContext.Current.CancellationToken);

            url.Should().Be($"/uploads/dhl-label-{shipmentId:N}.pdf");
            File.Exists(Path.Combine(root, $"dhl-label-{shipmentId:N}.pdf")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Router_Should_Use_Generic_ObjectStorage_For_FileSystem_Profile()
    {
        var root = Path.Combine(Path.GetTempPath(), $"darwin_label_object_storage_{Guid.NewGuid():N}");
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ObjectStorage:Profiles:ShipmentLabels:Provider"] = "FileSystem",
                    ["ObjectStorage:Profiles:ShipmentLabels:ContainerName"] = "labels",
                    ["ObjectStorage:Profiles:ShipmentLabels:Prefix"] = "shipments",
                    ["ObjectStorage:FileSystem:RootPath"] = root,
                    ["ObjectStorage:FileSystem:PublicBaseUrl"] = "https://assets.example.test"
                })
                .Build();
            var services = new ServiceCollection();
            services.Configure<MediaStorageOptions>(options =>
            {
                options.RootPath = Path.Combine(root, "legacy");
                options.RequestPath = "/uploads";
            });
            services.AddObjectStorageInfrastructure(configuration);
            services.AddShippingProviderInfrastructure();

            using var provider = services.BuildServiceProvider(validateScopes: true);
            using var scope = provider.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<IShipmentLabelStorage>();
            var shipmentId = Guid.NewGuid();

            var url = await storage.SaveLabelAsync(
                shipmentId,
                "DHL",
                new byte[] { 1, 2, 3 },
                "application/pdf",
                TestContext.Current.CancellationToken);

            url.Should().StartWith("https://assets.example.test/labels/shipments/dhl/");
            File.Exists(Path.Combine(root, "labels", "shipments", "dhl", shipmentId.ToString("N"), "labels", $"dhl-label-{shipmentId:N}.pdf")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Router_Should_Apply_MultiSegment_Profile_Prefix_Only_Once()
    {
        var root = Path.Combine(Path.GetTempPath(), $"darwin_label_object_storage_prefix_{Guid.NewGuid():N}");
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ObjectStorage:Profiles:ShipmentLabels:Provider"] = "FileSystem",
                    ["ObjectStorage:Profiles:ShipmentLabels:ContainerName"] = "labels",
                    ["ObjectStorage:Profiles:ShipmentLabels:Prefix"] = "carrier/labels",
                    ["ObjectStorage:FileSystem:RootPath"] = root,
                    ["ObjectStorage:FileSystem:PublicBaseUrl"] = "https://assets.example.test"
                })
                .Build();
            var services = new ServiceCollection();
            services.Configure<MediaStorageOptions>(options =>
            {
                options.RootPath = Path.Combine(root, "legacy");
                options.RequestPath = "/uploads";
            });
            services.AddObjectStorageInfrastructure(configuration);
            services.AddShippingProviderInfrastructure();

            using var provider = services.BuildServiceProvider(validateScopes: true);
            using var scope = provider.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<IShipmentLabelStorage>();
            var shipmentId = Guid.NewGuid();

            var url = await storage.SaveLabelAsync(
                shipmentId,
                "DHL",
                new byte[] { 1, 2, 3 },
                "application/pdf",
                TestContext.Current.CancellationToken);

            url.Should().StartWith("https://assets.example.test/labels/carrier/labels/dhl/");
            File.Exists(Path.Combine(root, "labels", "carrier", "labels", "dhl", shipmentId.ToString("N"), "labels", $"dhl-label-{shipmentId:N}.pdf")).Should().BeTrue();
            File.Exists(Path.Combine(root, "labels", "carrier", "labels", "carrier", "labels", "dhl", shipmentId.ToString("N"), "labels", $"dhl-label-{shipmentId:N}.pdf")).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string contentRootPath)
        {
            Directory.CreateDirectory(contentRootPath);
            ContentRootPath = contentRootPath;
            ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
        }

        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Darwin.Infrastructure.Tests";
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
