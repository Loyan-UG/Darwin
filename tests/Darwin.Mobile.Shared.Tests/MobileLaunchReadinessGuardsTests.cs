using System.Xml.Linq;
using FluentAssertions;

namespace Darwin.Mobile.Shared.Tests;

/// <summary>
/// Source-contract guards for mobile launch-readiness hardening.
/// </summary>
public sealed class MobileLaunchReadinessGuardsTests
{
    [Fact]
    public void AndroidManifests_Should_Not_Allow_CleartextTraffic()
    {
        var root = FindRepositoryRoot();
        var manifests = new[]
        {
            root.Combine("src", "Darwin.Mobile.Consumer", "Platforms", "Android", "AndroidManifest.xml"),
            root.Combine("src", "Darwin.Mobile.Business", "Platforms", "Android", "AndroidManifest.xml")
        };

        foreach (var manifest in manifests)
        {
            var text = File.ReadAllText(manifest);
            text.Should().Contain("android:usesCleartextTraffic=\"false\"");
            text.Should().NotContain("android:usesCleartextTraffic=\"true\"");
        }
    }

    [Fact]
    public void MobileSource_Should_Not_Contain_Hardcoded_GoogleApiKeys()
    {
        var root = FindRepositoryRoot();
        var mobileRoot = root.Combine("src", "Darwin.Mobile.Consumer");
        var files = Directory.EnumerateFiles(mobileRoot, "*", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !string.Equals(Path.GetFileName(path), "google-services.json", StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            text.Should().NotContain("AI" + "za", because: $"{file} must not commit Google API keys");
        }
    }

    [Fact]
    public void UnsafeCertificateTrust_Should_FailFast_OutsideDebugBuilds()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Shared", "Extensions", "ServiceCollectionExtensions.cs"));

        source.Should().Contain("#if !DEBUG");
        source.Should().Contain("UnsafeTrustAnyServerCertificate is allowed only in DEBUG builds.");
        source.Should().Contain("DangerousAcceptAnyServerCertificateValidator");
    }

    [Theory]
    [InlineData("Darwin.Mobile.Consumer")]
    [InlineData("Darwin.Mobile.Business")]
    public void MobileResourceFiles_Should_Have_EnglishAndGermanKeyParity(string projectName)
    {
        var root = FindRepositoryRoot();
        var resourcesRoot = root.Combine("src", projectName, "Resources");
        var englishKeys = ReadResourceKeys(Path.Combine(resourcesRoot, "Strings.resx"));
        var germanKeys = ReadResourceKeys(Path.Combine(resourcesRoot, "Strings.de.resx"));

        germanKeys.Should().BeEquivalentTo(englishKeys);
    }

    private static IReadOnlyCollection<string> ReadResourceKeys(string path)
        => XDocument.Load(path)
            .Descendants("data")
            .Select(element => element.Attribute("name")?.Value)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src", "Darwin.Mobile.Shared")))
            {
                return current;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Darwin repository root.");
    }
}

internal static class DirectoryInfoTestExtensions
{
    public static string Combine(this DirectoryInfo directory, params string[] paths)
        => Path.Combine([directory.FullName, .. paths]);
}
