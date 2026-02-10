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
            var status = await Permissions.RequestAsync<Permissions.Camera>().ConfigureAwait(false);
            if (status != PermissionStatus.Granted)
            {
                // Permission not granted — fallback to manual entry.
                return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
            }

            // Create scanning page and await its Completed event.
            var scanPage = new QrScanPage();
            var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Completed handler
            void CompletedHandler(object? sender, string? token)
            {
                tcs.TrySetResult(token);
            }

            scanPage.Completed += CompletedHandler;

            // Show the scanning page modally on UI thread
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current!.Navigation.PushModalAsync(scanPage).ConfigureAwait(false);
            }).ConfigureAwait(false);

            // Wait for scan or cancellation
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

            // Ensure page is closed and handler removed
            scanPage.Completed -= CompletedHandler;
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Pop modal if still present
                if (Shell.Current?.Navigation.ModalStack?.Count > 0)
                {
                    await Shell.Current.Navigation.PopModalAsync().ConfigureAwait(false);
                }
            }).ConfigureAwait(false);

            // If result is null (user canceled or no code) return fallback manual entry
            if (string.IsNullOrWhiteSpace(result))
            {
                return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // On any unexpected error, attempt manual prompt so user/dev can continue testing.
            return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
        }
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