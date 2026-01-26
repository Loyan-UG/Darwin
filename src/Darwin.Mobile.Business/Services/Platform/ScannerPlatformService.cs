using Darwin.Mobile.Business.Views;
using Darwin.Mobile.Shared.Integration;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Threading;

namespace Darwin.Mobile.Business.Services.Platform;

/// <summary>
/// Provides a platform-specific QR scanner using ZXing.Net.Maui.
/// This implementation opens a modal page containing a <see cref="QrScanPage"/>
/// and returns the scanned QR code.
/// </summary>
public sealed class ScannerPlatformService : IScanner
{
    /// <summary>
    /// Initiates a QR code scan and returns the decoded value, or null if cancelled.
    /// </summary>
    /// <param name="ct">A cancellation token for the scan operation.</param>
    /// <returns>The decoded QR code payload, or null if no code was scanned.</returns>
    public async Task<string?> ScanAsync(CancellationToken ct)
    {
        // Ensure camera permission is granted
        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            // Without camera permission, scanning cannot proceed
            return null;
        }

        // Create an instance of the scanning page
        var scanPage = new QrScanPage();
        var tcs = new TaskCompletionSource<string?>();

        // Handle completion event
        void CompletedHandler(object? sender, string? token)
        {
            tcs.TrySetResult(token);
        }

        scanPage.Completed += CompletedHandler;

        // Show the scanning page modally
        await Shell.Current!.Navigation.PushModalAsync(scanPage);

        // Wait for the scan to complete or cancellation
        string? result;
        using (ct.Register(() => tcs.TrySetCanceled(ct)))
        {
            try
            {
                result = await tcs.Task;
            }
            catch (TaskCanceledException)
            {
                result = null;
            }
        }

        // Unsubscribe and close the page
        scanPage.Completed -= CompletedHandler;
        await Shell.Current.Navigation.PopModalAsync();

        return result;
    }
}
