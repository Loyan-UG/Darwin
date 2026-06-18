using System.Xml.Linq;
using FluentAssertions;

namespace Darwin.Mobile.Business.Tests;

/// <summary>
/// Source-contract guards for Business mobile launch-readiness hardening.
/// </summary>
public sealed class BusinessLaunchReadinessGuardsTests
{
    [Fact]
    public void AndroidManifests_Should_DisableCleartextAndPlatformBackup_ForBothMobileApps()
    {
        var root = FindRepositoryRoot();
        var manifests = new[]
        {
            root.Combine("src", "Darwin.Mobile.Business", "Platforms", "Android", "AndroidManifest.xml"),
            root.Combine("src", "Darwin.Mobile.Consumer", "Platforms", "Android", "AndroidManifest.xml")
        };

        foreach (var manifestPath in manifests)
        {
            var manifest = File.ReadAllText(manifestPath);

            manifest.Should().Contain("android:usesCleartextTraffic=\"false\"", because: $"{manifestPath} must require HTTPS transport");
            manifest.Should().NotContain("android:usesCleartextTraffic=\"true\"", because: $"{manifestPath} must not allow app-wide HTTP cleartext");
            manifest.Should().Contain("android:allowBackup=\"false\"", because: $"{manifestPath} must not allow broad platform backup of mobile app data");
            manifest.Should().NotContain("android:allowBackup=\"true\"", because: $"{manifestPath} must not re-enable platform backup");
        }
    }

    [Fact]
    public void BusinessAndroidManifest_Should_DeclareCameraWithoutMakingHardwareRequired()
    {
        var root = FindRepositoryRoot();
        var manifest = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Business", "Platforms", "Android", "AndroidManifest.xml"));

        manifest.Should().Contain("<uses-permission android:name=\"android.permission.CAMERA\" />");
        manifest.Should().Contain("<uses-feature android:name=\"android.hardware.camera\" android:required=\"false\" />");
        manifest.Should().Contain("<uses-feature android:name=\"android.hardware.camera.autofocus\" android:required=\"false\" />");
    }

    [Fact]
    public void BusinessAndroidPush_Should_HandleForegroundMessagesAndNotificationTapDeepLinks()
    {
        var root = FindRepositoryRoot();
        var firebaseService = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Business", "Platforms", "Android", "Notifications", "BusinessFirebaseMessagingService.cs"));
        var mainActivity = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Business", "Platforms", "Android", "MainActivity.cs"));
        var navigator = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Business", "Services", "Notifications", "NotificationDeepLinkNavigator.cs"));

        firebaseService.Should().Contain("OnMessageReceived");
        firebaseService.Should().Contain("NotificationChannel");
        firebaseService.Should().Contain("deepLink");
        firebaseService.Should().Contain("notificationId");
        mainActivity.Should().Contain("OnNewIntent");
        mainActivity.Should().Contain("HandleNotificationIntent");
        navigator.Should().Contain("TryNavigatePendingAsync");
        navigator.Should().Contain("Routes.Notifications");
    }

    [Fact]
    public void BusinessApplePlatformFiles_Should_DeclareCameraUsageDescription()
    {
        var root = FindRepositoryRoot();
        var iosInfo = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Business", "Platforms", "iOS", "Info.plist"));
        var macInfo = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Business", "Platforms", "MacCatalyst", "Info.plist"));

        iosInfo.Should().Contain("NSCameraUsageDescription");
        iosInfo.Should().Contain("scan customer loyalty QR codes");
        macInfo.Should().Contain("NSCameraUsageDescription");
        macInfo.Should().Contain("scan customer loyalty QR codes");
    }

    [Fact]
    public void BusinessBrandAssets_Should_Not_Use_DefaultDotNetTemplateArt()
    {
        var root = FindRepositoryRoot();
        var businessRoot = root.Combine("src", "Darwin.Mobile.Business");
        var project = File.ReadAllText(Path.Combine(businessRoot, "Darwin.Mobile.Business.csproj"));
        var appIcon = File.ReadAllText(Path.Combine(businessRoot, "Resources", "AppIcon", "appicon.svg"));
        var splash = File.ReadAllText(Path.Combine(businessRoot, "Resources", "Splash", "splash.svg"));

        project.Should().NotContain("ForegroundFile=\"Resources\\AppIcon\\appiconfg.svg\"");
        project.Should().NotContain("dotnet_bot.png");
        File.Exists(Path.Combine(businessRoot, "Resources", "Images", "dotnet_bot.png")).Should().BeFalse();
        File.Exists(Path.Combine(businessRoot, "Resources", "AppIcon", "appiconfg.svg")).Should().BeFalse();
        appIcon.Should().Contain("radial-gradient");
        appIcon.Should().NotContain(">NET<");
        splash.Should().Contain("radial-gradient");
        splash.Should().NotContain(">NET<");
    }

    [Fact]
    public void BusinessSource_Should_Not_Contain_HardcodedGoogleOrFirebaseSecretPatterns()
    {
        var root = FindRepositoryRoot();
        var businessRoot = root.Combine("src", "Darwin.Mobile.Business");
        var files = Directory.EnumerateFiles(businessRoot, "*", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);

            text.Should().NotContain("AI" + "za", because: $"{file} must not commit Google API keys");
            text.Should().NotContain("-----BEGIN PRIVATE KEY-----", because: $"{file} must not commit Firebase service-account private keys");
            text.Should().NotContain("firebase_private_key", because: $"{file} must not commit Firebase service-account material");
        }
    }

    [Fact]
    public void SharedMobilePackages_Should_Not_Drift_Between_Consumer_And_Business()
    {
        var root = FindRepositoryRoot();
        var consumerPackages = ReadPackageVersions(root.Combine("src", "Darwin.Mobile.Consumer", "Darwin.Mobile.Consumer.csproj"));
        var businessPackages = ReadPackageVersions(root.Combine("src", "Darwin.Mobile.Business", "Darwin.Mobile.Business.csproj"));
        var sharedPackages = new[]
        {
            "CommunityToolkit.Maui",
            "CommunityToolkit.Mvvm",
            "HtmlSanitizer",
            "Microsoft.Extensions.Configuration.Json",
            "Microsoft.Maui.Controls",
            "Microsoft.Extensions.Logging.Debug",
            "QRCoder",
            "Syncfusion.Maui.Toolkit",
            "UraniumUI",
            "UraniumUI.Icons.FontAwesome",
            "UraniumUI.Icons.MaterialIcons",
            "ZXing.Net.Maui",
            "ZXing.Net.Maui.Controls"
        };

        foreach (var packageId in sharedPackages)
        {
            businessPackages.Should().ContainKey(packageId, because: $"Business should keep {packageId} aligned with Consumer");
            consumerPackages.Should().ContainKey(packageId, because: $"Consumer should keep {packageId} aligned with Business");
            businessPackages[packageId].Should().Be(consumerPackages[packageId], because: $"{packageId} should not drift between mobile apps");
        }
    }

    [Fact]
    public void Tizen_Should_Remain_OutOfScope_For_Current_Mobile_Launch()
    {
        var root = FindRepositoryRoot();
        var docs = string.Concat(
            File.ReadAllText(root.Combine("DarwinMobile.md")),
            File.ReadAllText(root.Combine("docs", "go-live-status.md")),
            File.ReadAllText(root.Combine("docs", "module-audit.md")));
        var businessProject = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Business", "Darwin.Mobile.Business.csproj"));
        var consumerProject = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Consumer", "Darwin.Mobile.Consumer.csproj"));

        docs.Should().Contain("Tizen");
        docs.Should().Contain("out-of-scope");
        businessProject.Should().Contain("<!-- <TargetFrameworks>$(TargetFrameworks);net9.0-tizen</TargetFrameworks> -->");
        consumerProject.Should().Contain("<!-- <TargetFrameworks>$(TargetFrameworks);net9.0-tizen</TargetFrameworks> -->");
    }

    [Fact]
    public void BusinessResourceFiles_Should_Have_EnglishAndGermanKeyParity()
    {
        var root = FindRepositoryRoot();
        var resourcesRoot = root.Combine("src", "Darwin.Mobile.Business", "Resources");
        var englishKeys = ReadResourceKeys(Path.Combine(resourcesRoot, "Strings.resx"));
        var germanKeys = ReadResourceKeys(Path.Combine(resourcesRoot, "Strings.de.resx"));

        germanKeys.Should().BeEquivalentTo(englishKeys);
    }

    [Fact]
    public void BusinessSubscriptionSurface_Should_Remain_ReadOnlyWebsiteHandoff()
    {
        var root = FindRepositoryRoot();
        var viewModel = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Business", "ViewModels", "SubscriptionViewModel.cs"));
        var page = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Business", "Views", "SubscriptionPage.xaml"));

        viewModel.Should().Contain("Presents a read-only subscription snapshot");
        viewModel.Should().Contain("OpenManagementWebsiteCommand");
        viewModel.Should().Contain("Browser.OpenAsync");
        viewModel.Should().Contain("BusinessManagementWebsiteUrl");
        viewModel.Should().NotContain("CreateCheckoutIntent", because: "Business mobile must not start provider checkout until product scope changes");
        viewModel.Should().NotContain("SetCancelAtPeriodEnd", because: "Business mobile must not mutate subscription cancellation state");

        page.Should().Contain("SubscriptionOpenManagementWebsiteButton");
        page.Should().NotContain("SubscriptionStartCheckoutButton", because: "checkout remains on the management website");
        page.Should().NotContain("SubscriptionSetCancelAtPeriodEndButton", because: "cancellation remains on the management website");
        page.Should().NotContain("SubscriptionUndoCancelAtPeriodEndButton", because: "cancellation remains on the management website");
    }

    [Fact]
    public void BusinessLiveOperationViewModels_Should_CheckAccessStateBeforeMutating()
    {
        var root = FindRepositoryRoot();
        var businessRoot = root.Combine("src", "Darwin.Mobile.Business", "ViewModels");
        var guardedViewModels = new[]
        {
            ("HomeViewModel.cs", "ScanCommand", "IsOperationsAllowed"),
            ("ScannerViewModel.cs", "ScanAsync", "EnsureOperationsAllowedAsync"),
            ("SessionViewModel.cs", "ConfirmAccrualAsync", "EnsureOperationsAllowedAsync"),
            ("SessionViewModel.cs", "ConfirmRedemptionAsync", "EnsureOperationsAllowedAsync"),
            ("RewardsViewModel.cs", "SaveAsync", "EnsureOperationsAllowedAsync"),
            ("RewardsViewModel.cs", "DeleteAsync", "EnsureOperationsAllowedAsync"),
            ("RewardsViewModel.cs", "SaveCampaignAsync", "EnsureOperationsAllowedAsync"),
            ("RewardsViewModel.cs", "ToggleCampaignActivationAsync", "EnsureOperationsAllowedAsync"),
            ("DashboardViewModel.cs", "EnsureOperationsAllowedAsync", "GetCurrentAccessStateAsync")
        };

        foreach (var (fileName, operationMarker, guardMarker) in guardedViewModels)
        {
            var source = File.ReadAllText(Path.Combine(businessRoot, fileName));

            source.Should().Contain(operationMarker, because: $"{fileName} should keep the guarded live-operation entry point visible");
            source.Should().Contain(guardMarker, because: $"{fileName} must enforce Business access-state before live operations");
            source.Should().Contain("GetCurrentAccessStateAsync", because: $"{fileName} must refresh server-backed access-state instead of trusting stale UI state");
        }
    }

    [Fact]
    public void BusinessScanner_Should_Not_CancelActiveScan_WhenModalScannerTemporarilyHidesParentPage()
    {
        var root = FindRepositoryRoot();
        var viewModel = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Business", "ViewModels", "ScannerViewModel.cs"));

        viewModel.Should().Contain("Volatile.Write(ref _isScannerInteractionActive, true)");
        viewModel.Should().Contain("if (!Volatile.Read(ref _isScannerInteractionActive))");
        viewModel.Should().Contain("CancelActiveScan();");
    }

    [Fact]
    public void BusinessScannerPermissionFlow_Should_RequestCameraOnMainThreadWithFallback()
    {
        var root = FindRepositoryRoot();
        var service = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Business", "Services", "Platform", "ScannerPlatformService.cs"));

        service.Should().Contain("MainThread.InvokeOnMainThreadAsync(Permissions.CheckStatusAsync<Permissions.Camera>)");
        service.Should().Contain("MainThread.InvokeOnMainThreadAsync(Permissions.RequestAsync<Permissions.Camera>)");
        service.Should().Contain("AppInfo.ShowSettingsUI()");
        service.Should().Contain("PromptForManualTokenAsync");
    }

    [Fact]
    public void BusinessScannerCameraPage_Should_Not_CoverPreviewWithPrimaryCancelOverlay()
    {
        var root = FindRepositoryRoot();
        var page = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Business", "Views", "QrScanPage.xaml"));

        page.Should().Contain("CameraBarcodeReaderView", because: "the live camera preview must remain the scanner surface");
        page.Should().Contain("VerticalOptions=\"Fill\"", because: "the camera preview must fill the scanner page behind controls");
        page.Should().Contain("BackgroundColor=\"Black\"", because: "camera startup gaps should not show the normal app surface color");
        page.Should().Contain("Style=\"{StaticResource SyncfusionOutlinedButtonStyle}\"", because: "the cancel action must be a low-emphasis overlay control");
        page.Should().Contain("WidthRequest=\"132\"", because: "the cancel action must not expand into a full-page primary button");
        page.Should().NotContain("Style=\"{StaticResource SyncfusionPrimaryButtonStyle}\"", because: "a primary business button can cover the camera preview with the brand color");
        page.Should().NotContain("BackgroundColor=\"{StaticResource CameraControlScrimColor}\"", because: "the control scrim should be a compact overlay, not a full-width bottom band");
    }

    [Fact]
    public void WebAdmin_Should_Keep_MobileLaunchSupportSurfaces()
    {
        var root = FindRepositoryRoot();
        var webAdminRoot = root.Combine("src", "Darwin.WebAdmin");
        var mobileOpsController = File.ReadAllText(Path.Combine(webAdminRoot, "Controllers", "Admin", "Mobile", "MobileOperationsController.cs"));
        var businessesController = File.ReadAllText(Path.Combine(webAdminRoot, "Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var billingController = File.ReadAllText(Path.Combine(webAdminRoot, "Controllers", "Admin", "Billing", "BillingController.cs"));
        var communicationsController = File.ReadAllText(Path.Combine(webAdminRoot, "Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        mobileOpsController.Should().Contain("ClearPushToken");
        mobileOpsController.Should().Contain("DeactivateDevice");
        businessesController.Should().Contain("SupportQueue");
        businessesController.Should().Contain("MerchantReadiness");
        businessesController.Should().Contain("OnboardingWizard");
        businessesController.Should().Contain("Invitations");
        businessesController.Should().Contain("ResendInvitation");
        businessesController.Should().Contain("RevokeInvitation");
        billingController.Should().Contain("Subscriptions");
        billingController.Should().Contain("Webhooks");
        billingController.Should().Contain("UpdatePaymentDisputeReview");
        communicationsController.Should().Contain("EmailAudits");
        communicationsController.Should().Contain("ChannelAudits");
        communicationsController.Should().Contain("ProviderCallbacks");
    }

    [Fact]
    public void MobileShells_Should_Not_Log_ExceptionDetails_During_Navigation_Failures()
    {
        var root = FindRepositoryRoot();
        var shellSources = new[]
        {
            root.Combine("src", "Darwin.Mobile.Business", "AppShell.xaml.cs"),
            root.Combine("src", "Darwin.Mobile.Consumer", "AppShell.xaml.cs")
        };

        foreach (var shellSource in shellSources)
        {
            var source = File.ReadAllText(shellSource);

            source.Should().NotContain("$\"Business startup navigation", because: "startup routes can contain app-link tokens");
            source.Should().NotContain("$\"Business logout navigation", because: "logout failures must not log exception details");
            source.Should().NotContain("$\"Consumer logout navigation", because: "logout failures must not log exception details");
            source.Should().NotContain("{ex}", because: "mobile navigation failure logs should not include raw exception details");
        }
    }

    [Fact]
    public void MobileOutbox_Should_Remain_Documented_As_Inactive_Launch_Scaffolding()
    {
        var root = FindRepositoryRoot();
        var outboxRepository = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Shared", "Storage", "Repositories", "OutboxRepository.cs"));
        var docs = string.Concat(
            File.ReadAllText(root.Combine("DarwinMobile.md")),
            File.ReadAllText(root.Combine("BACKLOG.md")));

        outboxRepository.Should().Contain("SQLite-backed outbox repository");
        docs.Should().Contain("offline mutation processor");
        docs.Should().Contain("inactive scaffolding");
    }

    [Fact]
    public void SharedMobileConfiguration_Should_RejectUnsafeCertificateTrustOutsideDebug()
    {
        var root = FindRepositoryRoot();
        var registration = File.ReadAllText(root.Combine("src", "Darwin.Mobile.Shared", "Extensions", "ServiceCollectionExtensions.cs"));

        registration.Should().Contain("#if DEBUG");
        registration.Should().Contain("UnsafeTrustAnyServerCertificate is allowed only in DEBUG builds.");
        registration.Should().Contain("throw new InvalidOperationException");
    }

    private static IReadOnlyCollection<string> ReadResourceKeys(string path)
        => XDocument.Load(path)
            .Descendants("data")
            .Select(element => element.Attribute("name")?.Value)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyDictionary<string, string> ReadPackageVersions(string path)
    {
        var document = XDocument.Load(path);
        return document.Descendants("PackageReference")
            .Select(element => new
            {
                Include = element.Attribute("Include")?.Value,
                Version = element.Attribute("Version")?.Value
            })
            .Where(package => !string.IsNullOrWhiteSpace(package.Include) && !string.IsNullOrWhiteSpace(package.Version))
            .ToDictionary(package => package.Include!, package => package.Version!, StringComparer.OrdinalIgnoreCase);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src", "Darwin.Mobile.Business")))
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
