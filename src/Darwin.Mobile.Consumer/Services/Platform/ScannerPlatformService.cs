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
    /// - Consumer app does not require scanning in Phase 1 in most deployments (consumer shows QR),
    ///   but we still provide a helpful scanner implementation for testing and occasional uses.
    /// - Emulators/simulators often don't expose a camera. To keep developer/tester productivity high,
    ///   this implementation requests camera permission and, if not available, falls back to a manual
    ///   prompt where the operator/developer can paste a ScanSessionToken.
    ///
    /// Pitfalls:
    /// - Do not assume camera permission is granted; always check and handle denied status.
    /// - For production devices, prefer a proper camera-based page (ZXing.Net.MAUI). This implementation
    ///   only falls back to the prompt when camera cannot be used.
    ///
    /// Example:
    /// - On a phone with camera: the app should open a scanning page (if implemented) or the manual prompt.
    /// - On an emulator: manual prompt will be shown so QA/dev can paste token and continue testing.
    /// </summary>
    public sealed class ScannerPlatformService : IScanner
    {
        /// <summary>
        /// Attempts to scan a QR code. If camera permission is not available or camera cannot be used,
        /// shows a manual input prompt and returns the entered token (or null).
        /// </summary>
        public async Task<string?> ScanAsync(CancellationToken ct)
        {
            // Attempt to request camera permission. If granted, try to open platform scanner page if present.
            // We don't reference any app-specific scanning page here to keep the implementation minimal.
            // If your app provides a dedicated scanning Page (like QrScanPage), replace the fallback below
            // with pushing that page modally and awaiting its result.
            try
            {
                var status = await Permissions.RequestAsync<Permissions.Camera>().ConfigureAwait(false);
                if (status == PermissionStatus.Granted)
                {
                    // If the app has a platform scanning page, prefer to use it.
                    // Many projects implement a modal QrScanPage that raises a Completed event (like Business app).
                    // Try to resolve and use it via Shell navigation if the page exists at runtime.
                    // Fallback: use manual prompt if no platform page is present.
                    if (Shell.Current?.Navigation != null)
                    {
                        // Try to navigate to a scanning page if it exists in the Consumer app.
                        // We use a best-effort approach: if the page is missing, continue to manual prompt.
                        try
                        {
                            var scanPageType = Type.GetType("Darwin.Mobile.Consumer.Views.QrScanPage, Darwin.Mobile.Consumer");
                            if (scanPageType is not null)
                            {
                                var scanPage = Activator.CreateInstance(scanPageType) as ContentPage;
                                if (scanPage is not null)
                                {
                                    var tcs = new TaskCompletionSource<string?>();
                                    void Handler(object? s, string? token) => tcs.TrySetResult(token);

                                    // If the page exposes 'Completed' event similar to Business QrScanPage, subscribe.
                                    var evt = scanPage.GetType().GetEvent("Completed");
                                    if (evt is not null)
                                    {
                                        evt.AddEventHandler(scanPage, (EventHandler<string?>)Handler);
                                        await Shell.Current.Dispatcher.DispatchAsync(async () =>
                                        {
                                            await Shell.Current.Navigation.PushModalAsync(scanPage).ConfigureAwait(false);
                                        }).ConfigureAwait(false);

                                        using (ct.Register(() => tcs.TrySetCanceled(ct)))
                                        {
                                            try
                                            {
                                                var result = await tcs.Task.ConfigureAwait(false);
                                                await Shell.Current.Dispatcher.DispatchAsync(async () =>
                                                {
                                                    await Shell.Current.Navigation.PopModalAsync().ConfigureAwait(false);
                                                }).ConfigureAwait(false);
                                                return result;
                                            }
                                            catch (TaskCanceledException)
                                            {
                                                await Shell.Current.Dispatcher.DispatchAsync(async () =>
                                                {
                                                    // Ensure the modal is closed if cancellation requested.
                                                    await Shell.Current.Navigation.PopModalAsync().ConfigureAwait(false);
                                                }).ConfigureAwait(false);
                                                return null;
                                            }
                                            finally
                                            {
                                                evt.RemoveEventHandler(scanPage, (EventHandler<string?>)Handler);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // If any reflection/navigation error occurs, ignore and fall back to manual prompt.
                        }
                    }
                }

                // Either permission was denied/unavailable or no platform scanning page was found:
                // Show a simple input prompt so tester/operator can paste a ScanSessionToken.
                // Shell.Current.DisplayPromptAsync is safe to use from UI thread; we marshal to main thread.
                string? promptResult = null;
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // title and message should be localized in real app; English here for clarity.
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
            catch (OperationCanceledException)
            {
                // Propagate cancellation to caller
                throw;
            }
            catch
            {
                // On unexpected failure, return null so caller can handle gracefully.
                return null;
            }
        }
    }
}