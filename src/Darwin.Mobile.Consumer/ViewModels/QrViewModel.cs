using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Shared.ViewModels;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.Commands;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Models.Loyalty;
using Darwin.Shared.Results;
using QRCoder;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// View model for the consumer QR screen.
/// Prepares scan sessions and generates a QR code image from the returned token.
/// </summary>
public sealed class QrViewModel : BaseViewModel
{
    private readonly ILoyaltyService _loyaltyService;
    private string _qrToken = string.Empty;
    private ImageSource? _qrImage;
    private DateTimeOffset? _expiresAtUtc;
    private LoyaltyScanMode _mode = LoyaltyScanMode.Accrual;
    private Guid _businessId;

    public QrViewModel(ILoyaltyService loyaltyService)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        RefreshAccrualSessionCommand = new AsyncCommand(RefreshAccrualSessionAsync);
        RefreshRedemptionSessionCommand = new AsyncCommand(RefreshRedemptionSessionAsync);
    }

    /// <summary>Current scan session token. Setting it regenerates the QR image.</summary>
    public string QrToken
    {
        get => _qrToken;
        private set
        {
            if (SetProperty(ref _qrToken, value))
            {
                GenerateQrImage();
            }
        }
    }

    /// <summary>Image representation of the current token.</summary>
    public ImageSource? QrImage
    {
        get => _qrImage;
        private set => SetProperty(ref _qrImage, value);
    }

    /// <summary>UTC expiry time of the current session, if provided.</summary>
    public DateTimeOffset? ExpiresAtUtc
    {
        get => _expiresAtUtc;
        private set => SetProperty(ref _expiresAtUtc, value);
    }

    /// <summary>Current scan mode.</summary>
    public LoyaltyScanMode Mode
    {
        get => _mode;
        private set => SetProperty(ref _mode, value);
    }

    /// <summary>Command to prepare a new accrual session.</summary>
    public AsyncCommand RefreshAccrualSessionCommand { get; }

    /// <summary>Command to prepare a new redemption session.</summary>
    public AsyncCommand RefreshRedemptionSessionCommand { get; }

    /// <summary>Sets the current business context (must be called before refreshing).</summary>
    public void SetBusiness(Guid businessId)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException("Business id must not be empty.", nameof(businessId));

        _businessId = businessId;
    }

    public override async Task OnAppearingAsync()
    {
        // On first appearance, prepare a default accrual session.
        if (_businessId != Guid.Empty && string.IsNullOrEmpty(QrToken))
        {
            await RefreshAccrualSessionAsync();
        }
    }

    private async Task RefreshAccrualSessionAsync()
    {
        if (_businessId == Guid.Empty)
        {
            ErrorMessage = "Business context is not set.";
            return;
        }

        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _loyaltyService.PrepareScanSessionAsync(
                _businessId, LoyaltyScanMode.Accrual, null, CancellationToken.None);

            ApplySessionResult(result);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshRedemptionSessionAsync()
    {
        if (_businessId == Guid.Empty)
        {
            ErrorMessage = "Business context is not set.";
            return;
        }

        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _loyaltyService.PrepareScanSessionAsync(
                _businessId, LoyaltyScanMode.Redemption, null, CancellationToken.None);

            ApplySessionResult(result);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplySessionResult(Result<ScanSessionClientModel> result)
    {
        if (!result.Succeeded || result.Value is null)
        {
            ErrorMessage = result.Error ?? "Failed to prepare scan session.";
            QrToken = string.Empty;
            ExpiresAtUtc = null;
            return;
        }

        var session = result.Value;
        Mode = session.Mode;
        QrToken = session.Token;
        ExpiresAtUtc = session.ExpiresAtUtc;
    }

    /// <summary>
    /// Generates a PNG QR code from the current token using QRCoder.
    /// </summary>
    private void GenerateQrImage()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_qrToken))
            {
                QrImage = null;
                return;
            }

            var generator = new QRCodeGenerator();
            var data = generator.CreateQrCode(_qrToken, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            var bytes = png.GetGraphic(20); // pixels per module
            QrImage = ImageSource.FromStream(() => new MemoryStream(bytes));
        }
        catch
        {
            QrImage = null; // silently drop image on error
        }
    }
}
