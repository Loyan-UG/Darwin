using ZXing.Net.Maui;

namespace Darwin.Mobile.Business.Views;

public partial class QrScanPage : ContentPage
{
    private int _completed;

    public QrScanPage()
    {
        InitializeComponent();

        // Only scan QR codes
        CameraView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple = false
        };

        CameraView.BarcodesDetected += OnBarcodesDetected;
    }

    public event EventHandler<string?>? Completed;

    protected override void OnDisappearing()
    {
        CameraView.BarcodesDetected -= OnBarcodesDetected;
        base.OnDisappearing();
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        var value = e.Results?.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        // prevent double-fire
        if (Interlocked.Exchange(ref _completed, 1) == 1)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() => Completed?.Invoke(this, value));
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        if (Interlocked.Exchange(ref _completed, 1) == 1)
        {
            return;
        }

        Completed?.Invoke(this, null);
    }
}