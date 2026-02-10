using Darwin.Mobile.Business.Views;
using Darwin.Mobile.Shared.Integration;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Business.Services.Platform;

/// <summary>
/// Provides a platform-specific QR scanner using ZXing.Net.Maui (QrScanPage).
/// 
/// Improved permission handling for camera-based scanning.
/// Rationale: handle Denied/Disabled states explicitly and provide a Settings redirect when appropriate.
/// Pitfalls: on iOS if permission is permanently denied the only remedy is to open Settings.
/// Example: call ScanAsync from a ViewModel and handle null as user-cancel/failed scan.
/// 
/// Rationale:
/// - Business app is tablet-first and uses a modal QrScanPage that raises a Completed event
///   containing the decoded QR payload. This keeps scanning UX decoupled from ViewModel logic.
/// - Emulators/dev machines may not have a working camera. To allow testing, the scanner falls back
///   to a manual token entry prompt when camera permission is denied or scanning page fails to initialize.
///
/// Pitfalls:
/// - Make sure ZXing.Net.MAUI package is referenced and QrScanPage XAML registers CameraView with
///   the expected BarcodesDetected event. The page must raise a Completed event with signature
///   EventHandler<string  ></string  > (matching existing implementation).
/// - Always respect cancellation tokens: if the caller cancels, do not leave a modal page open.
/// </summary>
public sealed class ScannerPlatformService : IScanner
{
    /// <summary>
    /// Initiates a QR code scan and returns the decoded value, or null if cancelled or no code scanned.
    /// Handles permission request and falls back to manual paste prompt when needed.
    /// </summary>
    public async Task<string?> ScanAsync(CancellationToken ct)
    {
        try
        {
            // Check current status first (faster UX)
            var currentStatus = await Permissions.CheckStatusAsync<Permissions.Camera>().ConfigureAwait(false);

            if (currentStatus == PermissionStatus.Granted)
            {
                // proceed to scanning page
                return await LaunchScanPageOrFallbackAsync(ct).ConfigureAwait(false);
            }

            // If we should show rationale (platform supports), optionally show a dialog to explain
            if (Permissions.ShouldShowRationale<Permissions.Camera>())
            {
                // Dispatch to UI thread to show explanation and ask user to continue to permission request
                var allow = await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    return await Shell.Current!.DisplayAlert(
                        "Camera access required",
                        "Darwin needs access to your camera to scan QR codes. Please allow camera access.",
                        "Allow",
                        "Cancel").ConfigureAwait(false);
                }).ConfigureAwait(false);

                if (!allow)
                {
                    // User chose not to request permission
                    return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
                }
            }

            // Request permission
            var status = await Permissions.RequestAsync<Permissions.Camera>().ConfigureAwait(false);

            if (status == PermissionStatus.Granted)
            {
                return await LaunchScanPageOrFallbackAsync(ct).ConfigureAwait(false);
            }
            else if (status == PermissionStatus.Denied)
            {
                // Permission denied - suggest opening settings (permanent denial)
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var open = await Shell.Current!.DisplayAlert(
                        "Camera permission denied",
                        "Camera access has been denied. Open app settings to enable the camera?",
                        "Open settings",
                        "Cancel").ConfigureAwait(false);

                    if (open)
                    {
                        AppInfo.ShowSettingsUI();
                    }
                }).ConfigureAwait(false);

                // Fall back to manual prompt so testing/dev can continue
                return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
            }
            else
            {
                // Unknown/Restricted/Disabled — fallback to manual entry
                return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Any unexpected error -> fallback manual prompt
            return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    private static async Task<string?> LaunchScanPageOrFallbackAsync(CancellationToken ct)
    {
        // This helper encapsulates creating and awaiting the QrScanPage like before.
        var scanPage = new QrScanPage();
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        void CompletedHandler(object? sender, string? token) => tcs.TrySetResult(token);
        scanPage.Completed += CompletedHandler;

        // Show scanning page on UI thread
        await MainThread.InvokeOnMainThreadAsync(async () => await Shell.Current!.Navigation.PushModalAsync(scanPage).ConfigureAwait(false)).ConfigureAwait(false);

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

        scanPage.Completed -= CompletedHandler;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Shell.Current?.Navigation.ModalStack?.Count > 0)
                await Shell.Current.Navigation.PopModalAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(result) ? null : result;
    }


    /// <summary>
    /// Shows a UI prompt to allow pasting a scan token manually (useful for emulators and test scenarios).
    /// Returns the entered token or null if canceled.
    /// </summary>
    private static Task<string?> PromptForManualTokenAsync(CancellationToken ct)
    {
        // Use Shell.DisplayPromptAsync on the main thread. Respect cancellation by returning null if cancelled.
        var tcs = new TaskCompletionSource<string?>();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var result = await Shell.Current!.DisplayPromptAsync(
                    title: "Manual scan token",
                    message: "Camera unavailable. Paste ScanSessionToken (or cancel).",
                    accept: "OK",
                    cancel: "Cancel",
                    placeholder: "paste token here",
                    keyboard: Keyboard.Text).ConfigureAwait(false);

                tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        // Respect caller cancellation token
        ct.Register(() => tcs.TrySetCanceled(ct));

        return tcs.Task;
    }
}