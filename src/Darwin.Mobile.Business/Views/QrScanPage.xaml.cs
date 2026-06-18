using ZXing.Net.Maui;

namespace Darwin.Mobile.Business.Views;

public partial class QrScanPage : ContentPage
{
    private bool _isBarcodeSubscribed;
    private int _completed;
    private bool _cameraStarted;

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
    }

    public event EventHandler<string?>? Completed;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartCameraDetection();
    }

    protected override void OnDisappearing()
    {
        StopCameraDetection();
        base.OnDisappearing();
    }

    public void CancelFromHost()
    {
        Complete(null);
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        var value = e.Results?.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() => Complete(value));
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        Complete(null);
    }

    private void Complete(string? token)
    {
        if (Interlocked.Exchange(ref _completed, 1) == 1)
        {
            return;
        }

        StopCameraDetection();
        if (!string.IsNullOrWhiteSpace(token))
        {
            Completed?.Invoke(this, token);
            return;
        }

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), () => Completed?.Invoke(this, null));
    }

    private void StartCameraDetection()
    {
        if (_cameraStarted)
        {
            return;
        }

        try
        {
            SubscribeBarcodeReader();
            CameraView.IsDetecting = true;
            _cameraStarted = true;
        }
        catch
        {
            Complete(null);
        }
    }

    private void StopCameraDetection()
    {
        if (!_cameraStarted && !_isBarcodeSubscribed)
        {
            return;
        }

        try
        {
            CameraView.IsDetecting = false;
            CameraView.IsTorchOn = false;
        }
        catch
        {
            // Camera controls can throw while Android is tearing down CameraX.
        }
        finally
        {
            _cameraStarted = false;
            UnsubscribeBarcodeReader();
        }
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
