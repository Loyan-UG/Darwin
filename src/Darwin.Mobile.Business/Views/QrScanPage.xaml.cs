using ZXing.Net.Maui;

namespace Darwin.Mobile.Business.Views;

public partial class QrScanPage : ContentPage
{
    private bool _isBarcodeSubscribed;
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

        SubscribeBarcodeReader();
    }

    public event EventHandler<string?>? Completed;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SubscribeBarcodeReader();
    }

    protected override void OnDisappearing()
    {
        UnsubscribeBarcodeReader();
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

    /// <summary>
    /// Subscribes to camera barcode events once per visible scan page instance.
    /// </summary>
    private void SubscribeBarcodeReader()
    {
        if (_isBarcodeSubscribed)
        {
            return;
        }

        CameraView.BarcodesDetected += OnBarcodesDetected;
        _isBarcodeSubscribed = true;
    }

    /// <summary>
    /// Detaches camera barcode events when the page is no longer visible to avoid stale callbacks.
    /// </summary>
    private void UnsubscribeBarcodeReader()
    {
        if (!_isBarcodeSubscribed)
        {
            return;
        }

        CameraView.BarcodesDetected -= OnBarcodesDetected;
        _isBarcodeSubscribed = false;
    }
}
