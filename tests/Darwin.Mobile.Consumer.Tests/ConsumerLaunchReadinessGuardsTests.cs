using System.Xml.Linq;
using FluentAssertions;

namespace Darwin.Mobile.Consumer.Tests;

/// <summary>
/// Source-contract guards for Consumer launch-readiness hardening.
/// </summary>
public sealed class ConsumerLaunchReadinessGuardsTests
{
    [Fact]
    public void AndroidManifest_Should_DisableCleartextAndPlatformBackup()
    {
        var root = FindRepositoryRoot();
        var manifest = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Consumer", "Platforms", "Android", "AndroidManifest.xml"));

        manifest.Should().Contain("android:usesCleartextTraffic=\"false\"");
        manifest.Should().NotContain("android:usesCleartextTraffic=\"true\"");
        manifest.Should().Contain("android:allowBackup=\"false\"");
        manifest.Should().NotContain("android:allowBackup=\"true\"");
    }

    [Fact]
    public void AndroidReleaseBuild_Should_RequireFirebaseAndGoogleMapsConfiguration()
    {
        var root = FindRepositoryRoot();
        var project = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Consumer", "Darwin.Mobile.Consumer.csproj"));

        project.Should().Contain("ValidateAndroidPushFirebaseConfig");
        project.Should().Contain("google-services.json is required for Android Release builds with FCM push integration.");
        project.Should().Contain("ValidateAndroidGoogleMapsApiKey");
        project.Should().Contain("GOOGLE_MAPS_API_KEY is required for Android Release builds.");
        project.Should().Contain("ANDROID_GOOGLE_MAPS_API_KEY");
    }

    [Fact]
    public void AppleReleaseEntitlements_Should_UseProductionPushEnvironment()
    {
        var root = FindRepositoryRoot();
        var ios = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Consumer", "Platforms", "iOS", "Entitlements.Release.plist"));
        var macCatalyst = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Consumer", "Platforms", "MacCatalyst", "Entitlements.Release.plist"));

        ios.Should().Contain("<key>aps-environment</key>");
        ios.Should().Contain("<string>production</string>");
        macCatalyst.Should().Contain("<key>aps-environment</key>");
        macCatalyst.Should().Contain("<string>production</string>");
    }

    [Fact]
    public void ConsumerSource_Should_Not_Contain_HardcodedGoogleApiKeys()
    {
        var root = FindRepositoryRoot();
        var consumerRoot = root.Combine("src", "Darwin.Mobile.Consumer");
        var files = Directory.EnumerateFiles(consumerRoot, "*", SearchOption.AllDirectories)
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
    public void ConsumerResourceFiles_Should_Have_EnglishAndGermanKeyParity()
    {
        var root = FindRepositoryRoot();
        var resourcesRoot = root.Combine("src", "Darwin.Mobile.Consumer", "Resources");
        var englishKeys = ReadResourceKeys(Path.Combine(resourcesRoot, "Strings.resx"));
        var germanKeys = ReadResourceKeys(Path.Combine(resourcesRoot, "Strings.de.resx"));

        germanKeys.Should().BeEquivalentTo(englishKeys);
    }

    [Fact]
    public void FeedPage_Should_Not_RenderCustomerFacingPromotionDiagnostics()
    {
        var root = FindRepositoryRoot();
        var feedPage = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Consumer", "Views", "FeedPage.xaml"));

        feedPage.Should().NotContain("FeedPromotionDiagnosticsTitle");
        feedPage.Should().NotContain("FeedCopyPromotionDiagnosticsButton");
        feedPage.Should().NotContain("FeedPromotionDiagnosticsClearStatusButton");
    }

    [Fact]
    public void QrPage_Should_WrapQrImageAndEmptyStateInsideSingleBorderContent()
    {
        var root = FindRepositoryRoot();
        var qrPage = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Consumer", "Views", "QrPage.xaml"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        qrPage.Should().Contain("<Grid>\n                            <Image Source=\"{Binding QrImage}\"");
        qrPage.Should().Contain("QrEmptyStateMessage");
    }

    [Fact]
    public void SettingsPage_Should_Bind_To_SettingsViewModel_From_DependencyInjection()
    {
        var root = FindRepositoryRoot();
        var settingsPage = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Consumer", "Views", "SettingsPage.xaml.cs"));
        var serviceRegistration = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Consumer", "Extensions", "ServiceCollectionExtensions.cs"));

        settingsPage.Should().Contain("SettingsPage(SettingsViewModel viewModel)");
        settingsPage.Should().Contain("BindingContext = _viewModel;");
        serviceRegistration.Should().Contain("services.AddTransient<SettingsViewModel>();");
    }

    [Fact]
    public void LegacyTemplatePages_Should_Not_Be_Present()
    {
        var root = FindRepositoryRoot();

        File.Exists(root.Combine("src", "Darwin.Mobile.Consumer", "MainPage.xaml")).Should().BeFalse();
        File.Exists(root.Combine("src", "Darwin.Mobile.Consumer", "MainPage.xaml.cs")).Should().BeFalse();
        File.Exists(root.Combine("src", "Darwin.Mobile.Consumer", "Views", "HomePage.xaml")).Should().BeFalse();
        File.Exists(root.Combine("src", "Darwin.Mobile.Consumer", "ViewModels", "HomeViewModel.cs")).Should().BeFalse();
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
            if (Directory.Exists(Path.Combine(current.FullName, "src", "Darwin.Mobile.Consumer")))
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
