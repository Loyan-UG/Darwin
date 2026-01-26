using Darwin.Mobile.Shared.Integration;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace Darwin.Mobile.Business.Services.Platform;

/// <summary>
/// Platform-specific scanner implementation using ZXing.Net.MAUI 0.7.x.
/// </summary>
public sealed class ScannerPlatformService : IScanner
{
    /// <summary>
    /// Initiates a QR code scan and returns the decoded text, or null if the scan is cancelled.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The scanned QR code payload.</returns>
    public async Task<string?> ScanAsync(CancellationToken ct)
    {
        // Configure options to only scan QR codes.
        var options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple = false
        };

        // Use the default scanner to read a barcode. This shows a platform-specific scanner UI.
        var result = await CameraBarcodeReaderView.Default.ReadAsync(options, ct);

        // Return the scanned value (null if nothing was scanned).
        return result?.Value;
    }
}
