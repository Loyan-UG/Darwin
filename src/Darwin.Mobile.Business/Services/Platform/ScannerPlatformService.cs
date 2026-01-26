using Darwin.Mobile.Shared.Integration;
using ZXing.Net.Maui;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Business.Services.Platform;

/// <summary>
/// Platform-specific scanner implementation using ZXing.Net.MAUI.
/// </summary>
public sealed class ScannerPlatformService : IScanner
{
    /// <summary>
    /// Initiates a barcode scan and returns the decoded text, or null if nothing was scanned.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The scanned QR code payload.</returns>
    public async Task<string?> ScanAsync(CancellationToken ct)
    {
        // Create scanner options if needed (optional).
        var options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormat.QrCode,
            AutoRotate = true
        };

        // Create and show the scanner. ZXing controls handle UI themselves.
        var scanner = new ZXing.Net.Maui.MauiBarcodeScanner();
        var result = await scanner.ScanAsync(options, ct);

        return result?.Value;
    }
}
