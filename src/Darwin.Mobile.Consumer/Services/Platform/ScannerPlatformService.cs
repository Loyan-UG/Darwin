using System;
using Darwin.Mobile.Shared.Integration;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer.Services.Platform
{
    /// <summary>
    /// Default scanner integration for the Consumer app.
    ///
    /// Rationale:
    /// - Consumer app primarily displays a QR for the business to scan; however, having a scanner
    ///   helps with testing or fallback scenarios.
    /// - Emulators/simulators often don't expose a camera. We handle camera permission professionally
    ///   and fall back to a manual token entry prompt when needed.
    ///
    /// Pitfalls:
    /// - Never assume camera permission. Always check, explain (rationale), request, and handle denial.
    /// - All UI operations (alerts/navigation) must run on the main thread to avoid platform crashes.
    /// - If a QrScanPage (with a Completed event) is not present in the Consumer app, reflection fails gracefully
    ///   and we fall back to manual prompt.
    ///
    /// Example:
    /// - On a device with camera: we try to open a QR scanning page if available, otherwise prompt for manual entry.
    /// - On an emulator: permission may be denied or camera absent; manual prompt allows pasting a ScanSessionToken.
    /// </summary>
    public sealed class ScannerPlatformService : IScanner
    {
        /// <summary>
        /// Attempts to scan a QR code and returns the decoded token string or null if cancelled/failed.
        /// Applies improved permission handling (rationale, request, denied → settings) and falls back
        /// to a manual input prompt in non-camera environments.
        /// </summary>
        /// <param name="ct">Cancellation token from caller.</param>
        public async Task<string?> ScanAsync(CancellationToken ct)
        {
            try
            {
                // Fast path: check current camera permission status.
                var current = await Permissions.CheckStatusAsync<Permissions.Camera>().ConfigureAwait(false);

                if (current == PermissionStatus.Granted)
                {
                    // Permission already granted → try scanner page or fallback prompt.
                    return await TryOpenScanPageOrPromptAsync(ct).ConfigureAwait(false);
                }

                // If platform recommends showing a rationale, display a short explanation.
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
                        // User rejected the rationale flow → offer manual entry.
                        return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
                    }
                }

                // Request permission from the OS.
                var status = await Permissions.RequestAsync<Permissions.Camera>().ConfigureAwait(false);

                if (status == PermissionStatus.Granted)
                {
                    return await TryOpenScanPageOrPromptAsync(ct).ConfigureAwait(false);
                }

                if (status == PermissionStatus.Denied)
                {
                    // On denial, offer to open app settings (common on iOS for permanently denied states).
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        var open = await Shell.Current!.DisplayAlertAsync(
                            "Camera permission denied",
                            "Camera access has been denied. You can enable it in app settings to scan QR codes.",
                            "Open settings",
                            "Cancel").ConfigureAwait(false);

                        if (open)
                        {
                            AppInfo.ShowSettingsUI();
                        }
                    }).ConfigureAwait(false);

                    // Continue with manual entry so testers/operators can proceed without camera.
                    return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
                }

                // Unknown/Restricted or other cases → manual prompt.
                return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Propagate cancellation to allow upstream flow to react properly.
                throw;
            }
            catch
            {
                // On unexpected failure, return null to let caller handle gracefully (retry, message, etc.).
                return null;
            }
        }

        /// <summary>
        /// Attempts to open a dedicated scanning page (if present with a 'Completed' event) and awaits its result.
        /// Falls back to a manual token prompt when page is unavailable or reflection fails.
        /// </summary>
        private static async Task<string?> TryOpenScanPageOrPromptAsync(CancellationToken ct)
        {
            if (Shell.Current?.Navigation != null)
            {
                try
                {
                    // Try to reflect a QrScanPage inside the Consumer app (optional feature).
                    var scanPageType = Type.GetType("Darwin.Mobile.Consumer.Views.QrScanPage, Darwin.Mobile.Consumer");
                    if (scanPageType is not null)
                    {
                        var scanPage = Activator.CreateInstance(scanPageType) as ContentPage;
                        if (scanPage is not null)
                        {
                            var tcs = new TaskCompletionSource<string?>();

                            // Handler for the expected 'Completed' event: (object? sender, string? token)
                            void Handler(object? s, string? token) => tcs.TrySetResult(token);

                            var evt = scanPage.GetType().GetEvent("Completed");
                            if (evt is not null)
                            {
                                evt.AddEventHandler(scanPage, (EventHandler<string?>)Handler);

                                // Push the modal scanning page on the main thread.
                                await MainThread.InvokeOnMainThreadAsync(async () =>
                                {
                                    await Shell.Current!.Navigation.PushModalAsync(scanPage).ConfigureAwait(false);
                                }).ConfigureAwait(false);

                                // Tie the TaskCompletionSource to cancellation to ensure correct modal cleanup.
                                using (ct.Register(() => tcs.TrySetCanceled(ct)))
                                {
                                    try
                                    {
                                        var result = await tcs.Task.ConfigureAwait(false);

                                        // Pop the modal page in all paths (success/cancel).
                                        await MainThread.InvokeOnMainThreadAsync(async () =>
                                        {
                                            await Shell.Current!.Navigation.PopModalAsync().ConfigureAwait(false);
                                        }).ConfigureAwait(false);

                                        return result;
                                    }
                                    catch (TaskCanceledException)
                                    {
                                        await MainThread.InvokeOnMainThreadAsync(async () =>
                                        {
                                            await Shell.Current!.Navigation.PopModalAsync().ConfigureAwait(false);
                                        }).ConfigureAwait(false);
                                        return null;
                                    }
                                    finally
                                    {
                                        // Always detach event handler to avoid leaks.
                                        evt.RemoveEventHandler(scanPage, (EventHandler<string?>)Handler);
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Reflection or navigation failures are not fatal here; we fall back to prompt.
                }
            }

            // If we reached here, no scanning page is available → prompt for manual token.
            return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Shows a manual prompt allowing the operator/tester to paste a ScanSessionToken.
        /// Returns the pasted token or null when cancelled.
        /// </summary>
        private static async Task<string?> PromptForManualTokenAsync(CancellationToken ct)
        {
            string? promptResult = null;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Note: In a production app, localize these strings (German for Phase 1).
                promptResult = await Shell.Current!.DisplayPromptAsync(
                    title: "Enter scan token",
                    message: "No camera available. Paste ScanSessionToken (or cancel).",
                    accept: "OK",
                    cancel: "Cancel",
                    placeholder: "paste token here",
                    keyboard: Keyboard.Text).ConfigureAwait(false);
            }).ConfigureAwait(false);

            return promptResult;
        }
    }
}