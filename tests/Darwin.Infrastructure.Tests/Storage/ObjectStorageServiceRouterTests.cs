using Darwin.Application.Abstractions.Storage;
using Darwin.Infrastructure.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Infrastructure.Tests.Storage;

public sealed class ObjectStorageServiceRouterTests
{
    [Fact]
    public void Resolve_Should_Not_Construct_Unselected_External_Providers()
    {
        var services = new ServiceCollection();
        services.AddObjectStorageInfrastructure(new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var storage = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();

        storage.Should().BeOfType<Darwin.Infrastructure.Storage.ObjectStorageServiceRouter>();
    }

    [Fact]
    public void GetCapabilities_Should_Report_Profile_Selected_Provider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ObjectStorage:Profiles:MediaAssets:Provider"] = "FileSystem",
                ["ObjectStorage:Profiles:MediaAssets:ContainerName"] = "media",
                ["ObjectStorage:FileSystem:RootPath"] = Path.Combine(Path.GetTempPath(), "darwin-router-test")
            })
            .Build();
        var services = new ServiceCollection();
        services.AddObjectStorageInfrastructure(configuration);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();

        var capabilities = storage.GetCapabilities(new ObjectStorageContainerSelection("media", ProfileName: "MediaAssets"));

        capabilities.Provider.Should().Be(ObjectStorageProviderKind.FileSystem);
        capabilities.SupportsNativeImmutability.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAndRead_Should_Apply_Profile_Container_And_Prefix()
    {
        var root = Path.Combine(Path.GetTempPath(), "darwin-router-profile-test-" + Guid.NewGuid().ToString("N"));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ObjectStorage:Profiles:MediaAssets:Provider"] = "FileSystem",
                ["ObjectStorage:Profiles:MediaAssets:ContainerName"] = "media-assets",
                ["ObjectStorage:Profiles:MediaAssets:Prefix"] = "cms/uploads",
                ["ObjectStorage:FileSystem:RootPath"] = root
            })
            .Build();
        var services = new ServiceCollection();
        services.AddObjectStorageInfrastructure(configuration);

        await using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();
        await using var content = new MemoryStream("profile-content"u8.ToArray());

        var ct = TestContext.Current.CancellationToken;
        var write = await storage.SaveAsync(new ObjectStorageWriteRequest(
            ContainerName: string.Empty,
            ObjectKey: "image.png",
            ContentType: "image/png",
            FileName: "image.png",
            Content: content,
            ProfileName: "MediaAssets"), ct);

        write.Provider.Should().Be(ObjectStorageProviderKind.FileSystem);
        write.ContainerName.Should().Be("media-assets");
        write.ObjectKey.Should().Be("cms/uploads/image.png");

        var read = await storage.ReadAsync(new ObjectStorageObjectReference(
            ContainerName: string.Empty,
            ObjectKey: "image.png",
            ProfileName: "MediaAssets"), ct);

        read.Should().NotBeNull();
        read!.FileName.Should().Be("image.png");
        File.Exists(Path.Combine(root, "media-assets", "cms", "uploads", "image.png")).Should().BeTrue();
    }

    [Fact]
    public async Task Save_Should_Not_Duplicate_Profile_Prefix_When_Caller_Already_Uses_It()
    {
        var root = Path.Combine(Path.GetTempPath(), "darwin-router-prefix-test-" + Guid.NewGuid().ToString("N"));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ObjectStorage:Profiles:ShipmentLabels:Provider"] = "FileSystem",
                ["ObjectStorage:Profiles:ShipmentLabels:ContainerName"] = "labels",
                ["ObjectStorage:Profiles:ShipmentLabels:Prefix"] = "shipments",
                ["ObjectStorage:FileSystem:RootPath"] = root
            })
            .Build();
        var services = new ServiceCollection();
        services.AddObjectStorageInfrastructure(configuration);

        await using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IObjectStorageService>();
        await using var content = new MemoryStream("label"u8.ToArray());

        var ct = TestContext.Current.CancellationToken;
        var write = await storage.SaveAsync(new ObjectStorageWriteRequest(
            ContainerName: string.Empty,
            ObjectKey: "shipments/dhl/label.pdf",
            ContentType: "application/pdf",
            FileName: "label.pdf",
            Content: content,
            ProfileName: "ShipmentLabels"), ct);

        write.ObjectKey.Should().Be("shipments/dhl/label.pdf");
        File.Exists(Path.Combine(root, "labels", "shipments", "dhl", "label.pdf")).Should().BeTrue();
        File.Exists(Path.Combine(root, "labels", "shipments", "shipments", "dhl", "label.pdf")).Should().BeFalse();
    }
}
