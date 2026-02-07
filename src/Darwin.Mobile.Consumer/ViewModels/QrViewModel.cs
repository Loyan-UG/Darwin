using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Models.Loyalty;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.ViewModels;
using Darwin.Shared.Results;
using Microsoft.Maui.Controls;
using QRCoder;
using System.IO;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// View model for the consumer QR screen.
/// Responsible for preparing scan sessions and exposing the QR code image and related state.
/// </summary>
public sealed class QrViewModel : BaseViewModel
{
    private readonly ILoyaltyService _loyaltyService;
    private string _qrToken = string.Empty;
    private DateTimeOffset? _expiresAtUtc;
    private LoyaltyScanMode _mode = LoyaltyScanMode.Accrual;
    private Guid _currentBusinessId;
    private ImageSource? _qrImage;

    /// <summary>
    /// Initializes a new instance of the <see cref="QrViewModel"/> class.
    /// </summary>
    /// <param name="loyaltyService">The loyalty service used to prepare sessions.</param>
    public QrViewModel(ILoyaltyService loyaltyService)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        RefreshAccrualSessionCommand = new AsyncCommand(RefreshAccrualSessionAsync);
        RefreshRedemptionSessionCommand = new AsyncCommand(RefreshRedemptionSessionAsync);
    }

    /// <summary>
    /// Gets the current QR token string that should be rendered as a QR code.
    /// Setting this property regenerates the corresponding QR image.
    /// </summary>
    public string QrToken
    {
        get => _qrToken;
        private set
        {
            if (SetProperty(ref _qrToken, value))
            {
                UpdateQrImage();
            }
        }
    }

    /// <summary>
    /// Gets the image representation of the current QR token.
    /// </summary>
    public ImageSource? QrImage
    {
        get => _qrImage;
        private set => SetProperty(ref _qrImage, value);
    }

    /// <summary>
    /// Gets the UTC expiry of the current scan session, if provided.
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc
    {
        get => _expiresAtUtc;
        private set => SetProperty(ref _expiresAtUtc, value);
    }

    /// <summary>
    /// Gets the current scan mode (accrual or redemption).
    /// </summary>
    public LoyaltyScanMode Mode
    {
        get => _mode;
        private set => SetProperty(ref _mode, value);
    }

    /// <summary>
    /// Command that prepares a new accrual session for the current business.
    /// </summary>
    public AsyncCommand RefreshAccrualSessionCommand { get; }

    /// <summary>
    /// Command that prepares a new redemption session based on the consumer's selected rewards.
    /// </summary>
    public AsyncCommand RefreshRedemptionSessionCommand { get; }

    /// <summary>
    /// Sets the business context for this QR view model.
    /// This must be called by the parent screen before refreshing sessions.
    /// </summary>
    /// <param name="businessId">The active business identifier.</param>
    public void SetBusiness(Guid businessId)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException("Business id must not be empty.", nameof(businessId));

        _currentBusinessId = businessId;
    }

    /// <inheritdoc />
    public override async Task OnAppearingAsync()
    {
        // When the page appears, ensure we have a QR for accrual by default.
        if (_currentBusinessId != Guid.Empty && string.IsNullOrWhiteSpace(QrToken))
        {
            await RefreshAccrualSessionAsync().ConfigureAwait(false);
        }
    }

    private async Task RefreshAccrualSessionAsync()
    {
        if (_currentBusinessId == Guid.Empty)
        {
            ErrorMessage = "Business context is not set.";
            return;
        }

        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _loyaltyService
                .PrepareScanSessionAsync(_currentBusinessId, LoyaltyScanMode.Accrual, null, CancellationToken.None)
                .ConfigureAwait(false);

            ApplyScanSessionResult(result);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshRedemptionSessionAsync()
    {
        if (_currentBusinessId == Guid.Empty)
        {
            ErrorMessage = "Business context is not set.";
            return;
        }

        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _loyaltyService
                .PrepareScanSessionAsync(_currentBusinessId, LoyaltyScanMode.Redemption, null, CancellationToken.None)
                .ConfigureAwait(false);

            ApplyScanSessionResult(result);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Applies the scan session preparation result to the QR-related properties.
    /// </summary>
    private void ApplyScanSessionResult(Result<ScanSessionClientModel> result)
    {
        if (!result.Succeeded || result.Value is null)
        {
            ErrorMessage = result.Error ?? "Failed to prepare scan session.";
            QrToken = string.Empty;
            ExpiresAtUtc = null;
            return;
        }

        var session = result.Value;
        QrToken = session.Token;
        ExpiresAtUtc = session.ExpiresAtUtc;
        Mode = session.Mode;
    }

    /// <summary>
    /// Generates a QR code image from the current <see cref="QrToken"/>.
    /// Uses the QRCoder library to create a PNG in memory and assigns it to <see cref="QrImage"/>.
    /// </summary>
    private void UpdateQrImage()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_qrToken))
            {
                QrImage = null;
                return;
            }

            var generator = new QRCodeGenerator();
            var qrData = generator.CreateQrCode(_qrToken, QRCodeGenerator.ECCLevel.Q);
            var pngGenerator = new PngByteQRCode(qrData);

            // Pixels per module: higher values produce a larger image.
            var pngBytes = pngGenerator.GetGraphic(20);
            QrImage = ImageSource.FromStream(() => new MemoryStream(pngBytes));
        }
        catch
        {
            // Fallback to no image on error. Consider logging if needed.
            QrImage = null;
        }
    }
}
