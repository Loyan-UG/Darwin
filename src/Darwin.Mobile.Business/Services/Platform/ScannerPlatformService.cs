using Darwin.Mobile.Business.Views;
using Darwin.Mobile.Shared.Integration;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Business.Services.Platform;

/// <summary>
/// Platform-specific QR scanner for the Business app.
/// 
/// Responsibilities:
/// - Request and validate camera permission in a professional way (rationale, request, denied handling).
/// - Launch the existing modal QrScanPage and await its Completed event.
/// - Respect CancellationToken and ensure modal pages are closed on cancellation/exception.
/// - Provide a manual fallback (prompt) for emulators or when camera access is not possible.
///
/// Rationale:
/// - Camera permission is a sensitive, user-visible permission. We must explain why it's needed
///   (ShouldShowRationale path), request it, and if permanently denied offer the Settings redirect.
/// - In dev/test environments and emulators a manual token entry keeps flows testable without a camera.
///
/// Pitfalls:
/// - Do not use synchronous or obsolete DisplayAlert overloads (use DisplayAlertAsync).
/// - Always perform UI navigation on the main thread. Failure to do so can crash on some platforms.
/// - If the QrScanPage is modified to use a different event signature, adapt the reflection or event hookup accordingly.
/// </summary>
public sealed class ScannerPlatformService : IScanner
{
    /// <summary>
    /// Initiates a QR scan operation and returns the decoded token string, or null if cancelled/failed.
    /// </summary>
    /// <param name="ct">Cancellation token from caller.</param>
    public async Task<string?> ScanAsync(CancellationToken ct)
    {
        try
        {
            // Fast path: check current status to avoid an unnecessary dialog.
            var current = await Permissions.CheckStatusAsync<Permissions.Camera>().ConfigureAwait(false);

            if (current == PermissionStatus.Granted)
            {
                // Permission already granted -> proceed to scan.
                return await LaunchScanPageWithFallbackAsync(ct).ConfigureAwait(false);
            }

            // If we should show a rationale, show a brief, user-friendly explanation first.
            if (Permissions.ShouldShowRationale<Permissions.Camera>())
            {
                var proceed = await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    return await Shell.Current!.DisplayAlertAsync(
                        "Camera access required",
                        "Darwin needs access to the camera to scan QR codes for loyalty sessions. Please allow camera access.",
                        "Allow",
                        "Cancel").ConfigureAwait(false);
                }).ConfigureAwait(false);

                if (!proceed)
                {
                    // User chose not to request permission after rationale -> fallback to manual entry.
                    return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
                }
            }

            // Request permission from the OS.
            var status = await Permissions.RequestAsync<Permissions.Camera>().ConfigureAwait(false);

            if (status == PermissionStatus.Granted)
            {
                return await LaunchScanPageWithFallbackAsync(ct).ConfigureAwait(false);
            }

            if (status == PermissionStatus.Denied)
            {
                // Permanent denial on some platforms -> offer Settings redirect (async).
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var open = await Shell.Current!.DisplayAlertAsync(
                        "Camera permission denied",
                        "Camera access has been denied. You can enable it in app settings to scan QR codes.",
                        "Open settings",
                        "Cancel").ConfigureAwait(false);

                    if (open)
                    {
                        // Opens platform settings UI for this app.
                        AppInfo.ShowSettingsUI();
                    }
                }).ConfigureAwait(false);

                // After informing the user, fallback to manual entry so testing/dev can continue.
                return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
            }

            // Other statuses (Unknown / Restricted) -> fallback to manual token entry.
            return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Propagate cancellation so callers can react accordingly.
            throw;
        }
        catch
        {
            // Unexpected errors (reflection/navigation/etc.) -> fallback to manual prompt.
            return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Encapsulates launching the QrScanPage, awaiting its Completed event, and ensuring proper cleanup.
    /// If the page returns null or is cancelled, the calling method may choose a fallback behavior.
    /// </summary>
    private static async Task<string?> LaunchScanPageWithFallbackAsync(CancellationToken ct)
    {
        // Create page instance (QrScanPage is provided in Business app).
        var scanPage = new QrScanPage();

        // Use a TaskCompletionSource with RunContinuationsAsynchronously to avoid sync context deadlocks.
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Handler to complete the TCS when scan completes.
        void CompletedHandler(object? sender, string? token) => tcs.TrySetResult(token);

        scanPage.Completed += CompletedHandler;

        try
        {
            // Push modal on UI thread.
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current!.Navigation.PushModalAsync(scanPage).ConfigureAwait(false);
            }).ConfigureAwait(false);

            // Wait for scan result or cancellation.
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
            // Always remove handler and ensure modal is closed (safely on UI thread).
            scanPage.Completed -= CompletedHandler;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    if (Shell.Current?.Navigation.ModalStack?.Count > 0)
                        await Shell.Current.Navigation.PopModalAsync().ConfigureAwait(false);
                }
                catch
                {
                    // Swallow navigation errors here to avoid throwing from cleanup.
                }
            }).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Shows a prompt to allow manual pasting of a ScanSessionToken.
    /// This is used as a fallback for emulators or when camera access is not possible.
    /// Returns the entered token or null if cancelled.
    /// </summary>
    private static Task<string?> PromptForManualTokenAsync(CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var token = await Shell.Current!.DisplayPromptAsync(
                    title: "Manual scan token",
                    message: "Camera unavailable. Paste ScanSessionToken (or cancel).",
                    accept: "OK",
                    cancel: "Cancel",
                    placeholder: "paste token here",
                    keyboard: Keyboard.Text).ConfigureAwait(false);

                tcs.TrySetResult(token);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        ct.Register(() => tcs.TrySetCanceled(ct));
        return tcs.Task;
    }
}