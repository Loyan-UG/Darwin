using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Business.Views;
using Darwin.Mobile.Shared.Integration;
using Darwin.Mobile.Shared.Services.Permissions;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#if ANDROID
using Android.Content;
using Android.Hardware.Camera2;
using AndroidBuild = Android.OS.Build;
using JavaInteger = Java.Lang.Integer;
#endif

namespace Darwin.Mobile.Business.Services.Platform;

/// <summary>
/// Platform-specific QR scanner for the Business app.
/// </summary>
/// <remarks>
/// Responsibilities:
/// - Shows a just-in-time privacy disclosure before the operating-system camera permission prompt.
/// - Requests and validates camera permission in a professional way.
/// - Launches the existing modal QrScanPage and awaits its Completed event.
/// - Provides a manual fallback for emulators or when camera access is not possible.
/// </remarks>
public sealed class ScannerPlatformService : IScanner
{
#if ANDROID
    private const int AndroidLensFacingFront = 0;
    private const int AndroidLensFacingBack = 1;
#endif

    private readonly IPermissionDisclosureService _permissionDisclosureService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScannerPlatformService"/> class.
    /// </summary>
    /// <param name="permissionDisclosureService">Service used to show a privacy disclosure before requesting camera access.</param>
    public ScannerPlatformService(IPermissionDisclosureService permissionDisclosureService)
    {
        _permissionDisclosureService = permissionDisclosureService ?? throw new ArgumentNullException(nameof(permissionDisclosureService));
    }

    /// <summary>
    /// Initiates a QR scan operation and returns the decoded token string, or null if cancelled/failed.
    /// </summary>
    /// <param name="ct">Cancellation token from caller.</param>
    public async Task<string?> ScanAsync(CancellationToken ct)
    {
        try
        {
            var current = await MainThread.InvokeOnMainThreadAsync(Permissions.CheckStatusAsync<Permissions.Camera>)
                .ConfigureAwait(false);

            if (current == PermissionStatus.Granted)
            {
                if (!HasUsableCamera())
                {
                    return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
                }

                return await LaunchScanPageWithFallbackAsync(ct).ConfigureAwait(false);
            }

            var shouldProceed = await _permissionDisclosureService.ShowAsync(new PermissionDisclosureRequest
            {
                Title = AppResources.CameraDisclosureTitle,
                PermissionName = AppResources.CameraDisclosurePermissionName,
                WhyThisIsNeeded = AppResources.CameraDisclosurePurpose,
                FeatureRequirementText = AppResources.CameraDisclosureRequirement,
                ContinueButtonText = AppResources.PermissionDisclosureContinueButton,
                CancelButtonText = AppResources.PermissionDisclosureCancelButton,
                LegalReferenceButtonText = AppResources.PermissionDisclosurePrivacyButton,
                LegalReferenceOpenFailedMessage = AppResources.LegalOpenFailed,
                LegalReferenceKind = Darwin.Mobile.Shared.Services.Legal.LegalLinkKind.PrivacyPolicy
            }, ct).ConfigureAwait(false);

            if (!shouldProceed)
            {
                return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
            }

            var status = await MainThread.InvokeOnMainThreadAsync(Permissions.RequestAsync<Permissions.Camera>)
                .ConfigureAwait(false);

            if (status == PermissionStatus.Granted)
            {
                if (!HasUsableCamera())
                {
                    return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
                }

                return await LaunchScanPageWithFallbackAsync(ct).ConfigureAwait(false);
            }

            if (status == PermissionStatus.Denied)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var open = await Shell.Current!.DisplayAlertAsync(
                        AppResources.CameraDisclosureDeniedTitle,
                        AppResources.CameraDisclosureDeniedBody,
                        AppResources.CameraDisclosureOpenSettingsButton,
                        AppResources.PermissionDisclosureCancelButton).ConfigureAwait(false);

                    if (open)
                    {
                        AppInfo.ShowSettingsUI();
                    }
                }).ConfigureAwait(false);

                return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
            }

            return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
        }
        catch (System.OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
        }
    }

    private static async Task<string?> LaunchScanPageWithFallbackAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var scanPage = new QrScanPage();
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        void CompletedHandler(object? sender, string? token) => tcs.TrySetResult(token);

        scanPage.Completed += CompletedHandler;

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                ct.ThrowIfCancellationRequested();
                await Shell.Current!.Navigation.PushModalAsync(scanPage).ConfigureAwait(false);
            }).ConfigureAwait(false);

            ct.ThrowIfCancellationRequested();
            string? result;
            using (ct.Register(() => tcs.TrySetCanceled(ct)))
            {
                try
                {
                    result = await tcs.Task.ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    result = null;
                }
            }

            return string.IsNullOrWhiteSpace(result) ? null : result;
        }
        finally
        {
            scanPage.Completed -= CompletedHandler;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    var navigation = Shell.Current?.Navigation;
                    var modalStack = navigation?.ModalStack;
                    if (modalStack?.LastOrDefault() == scanPage)
                    {
                        await navigation!.PopModalAsync().ConfigureAwait(false);
                    }
                }
                catch
                {
                }
            }).ConfigureAwait(false);
        }
    }

    private static async Task<string?> PromptForManualTokenAsync(CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var token = await Shell.Current!.DisplayPromptAsync(
                    title: AppResources.ScannerManualTokenTitle,
                    message: AppResources.ScannerManualTokenMessage,
                    accept: AppResources.ScannerManualTokenAccept,
                    cancel: AppResources.ScannerManualTokenCancel,
                    placeholder: AppResources.ScannerManualTokenPlaceholder,
                    keyboard: Keyboard.Text).ConfigureAwait(false);

                tcs.TrySetResult(token);
            }
            catch (System.Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        // Dispose the cancellation registration after the prompt completes so repeated scans do not retain delegates.
        using var cancellationRegistration = ct.CanBeCanceled
            ? ct.Register(() => tcs.TrySetCanceled(ct))
            : default;

        return await tcs.Task.ConfigureAwait(false);
    }

    private static bool HasUsableCamera()
    {
#if ANDROID
        try
        {
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            var cameraManager = activity?.GetSystemService(Context.CameraService) as CameraManager;
            var cameraIds = cameraManager?.GetCameraIdList();
            if (cameraManager is null || cameraIds is null || cameraIds.Length == 0)
            {
                return false;
            }

            var hasBackCamera = false;
            var hasFrontCamera = false;

            foreach (var cameraId in cameraIds)
            {
                var characteristics = cameraManager.GetCameraCharacteristics(cameraId);
                var lensFacing = characteristics.Get(CameraCharacteristics.LensFacing) as JavaInteger;
                if (lensFacing is null)
                {
                    continue;
                }

                if (lensFacing.IntValue() == AndroidLensFacingBack)
                {
                    hasBackCamera = true;
                }
                else if (lensFacing.IntValue() == AndroidLensFacingFront)
                {
                    hasFrontCamera = true;
                }
            }

            if (!hasBackCamera)
            {
                return false;
            }

            // CameraX can crash inside ZXing on Android emulators that report a partial
            // camera set, especially when the front camera is missing. Use manual fallback there.
            return !IsLikelyAndroidEmulator() || hasFrontCamera;
        }
        catch
        {
            return false;
        }
#else
        return true;
#endif
    }

#if ANDROID
    private static bool IsLikelyAndroidEmulator()
    {
        var fingerprint = AndroidBuild.Fingerprint?.ToLowerInvariant() ?? string.Empty;
        var model = AndroidBuild.Model?.ToLowerInvariant() ?? string.Empty;
        var manufacturer = AndroidBuild.Manufacturer?.ToLowerInvariant() ?? string.Empty;
        var brand = AndroidBuild.Brand?.ToLowerInvariant() ?? string.Empty;
        var device = AndroidBuild.Device?.ToLowerInvariant() ?? string.Empty;
        var product = AndroidBuild.Product?.ToLowerInvariant() ?? string.Empty;
        var hardware = AndroidBuild.Hardware?.ToLowerInvariant() ?? string.Empty;

        return fingerprint.StartsWith("generic", StringComparison.Ordinal)
               || fingerprint.Contains("emulator", StringComparison.Ordinal)
               || model.Contains("emulator", StringComparison.Ordinal)
               || model.Contains("android sdk built for", StringComparison.Ordinal)
               || manufacturer.Contains("genymotion", StringComparison.Ordinal)
               || (brand.StartsWith("generic", StringComparison.Ordinal) && device.StartsWith("generic", StringComparison.Ordinal))
               || product.Contains("sdk", StringComparison.Ordinal)
               || hardware.Contains("goldfish", StringComparison.Ordinal)
               || hardware.Contains("ranchu", StringComparison.Ordinal);
    }
#endif
}
