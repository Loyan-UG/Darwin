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
/// Prepares scan sessions, generates QR images, and keeps sessions fresh by auto-rotation.
/// </summary>
public sealed class QrViewModel : BaseViewModel
{
    private static readonly TimeSpan RotationCheckInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RotationRenewThreshold = TimeSpan.FromSeconds(300);

    private const string DiscoverGuidanceMessage = "To generate a QR code, first go to Discover, open a business, and join its loyalty program.";
    private const string RefreshGuidanceMessage = "Accrual creates a QR for earning points. Redemption creates a QR for spending points/rewards.";

    private readonly ILoyaltyService _loyaltyService;

    private string _qrToken = string.Empty;
    private ImageSource? _qrImage;
    private DateTimeOffset? _expiresAtUtc;
    private LoyaltyScanMode _mode = LoyaltyScanMode.Accrual;
    private Guid _businessId;
    private string _businessDisplayName = string.Empty;
    private string _statusMessage = string.Empty;
    private string _guidanceMessage = DiscoverGuidanceMessage;
    private string _expiresInText = string.Empty;

    private CancellationTokenSource? _rotationLoopCts;

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
        private set
        {
            if (SetProperty(ref _expiresAtUtc, value))
            {
                UpdateExpiresInText();
            }
        }
    }

    /// <summary>Shows a user-friendly countdown to the next automatic QR rotation.</summary>
    public string ExpiresInText
    {
        get => _expiresInText;
        private set => SetProperty(ref _expiresInText, value);
    }

    /// <summary>Current scan mode.</summary>
    public LoyaltyScanMode Mode
    {
        get => _mode;
        private set => SetProperty(ref _mode, value);
    }

    /// <summary>
    /// Friendly business name shown above the QR
    /// </summary>
    public string BusinessDisplayName
    {
        get => _businessDisplayName;
        private set => SetProperty(ref _businessDisplayName, value);
    }

    /// <summary>
    /// User-facing status message for contextual actions (for example, successful join confirmation).
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// UX guidance message shown on the QR screen.
    /// It helps users understand what to do when no business context exists,
    /// and also explains the purpose of accrual vs redemption refresh actions.
    /// </summary>
    public string GuidanceMessage
    {
        get => _guidanceMessage;
        private set => SetProperty(ref _guidanceMessage, value);
    }

    /// <summary>
    /// Indicates whether a business context has already been selected for QR generation.
    /// </summary>
    public bool HasBusinessContext => _businessId != Guid.Empty;

    /// <summary>Command to prepare a new accrual session.</summary>
    public AsyncCommand RefreshAccrualSessionCommand { get; }

    /// <summary>Command to prepare a new redemption session.</summary>
    public AsyncCommand RefreshRedemptionSessionCommand { get; }

    /// <summary>Sets the current business context (must be called before refreshing).</summary>
    public void SetBusiness(Guid businessId)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException("Business id must not be empty.", nameof(businessId));

        // When business context changes, reset prior QR/session artifacts so the new business session can be loaded.
        if (_businessId != businessId)
        {
            _businessId = businessId;
            QrToken = string.Empty;
            ExpiresAtUtc = null;
            ErrorMessage = null;
            OnPropertyChanged(nameof(HasBusinessContext));
            UpdateGuidanceMessage();
        }
    }

    /// <summary>
    /// Sets the display name for the currently active business context.
    /// </summary>
    public void SetBusinessDisplayName(string? businessName)
    {
        BusinessDisplayName = string.IsNullOrWhiteSpace(businessName) ? string.Empty : businessName.Trim();
    }

    /// <summary>
    /// Applies a contextual message when the user just joined a loyalty program.
    /// </summary>
    public void SetJoinedStatus(bool justJoined)
    {
        StatusMessage = justJoined
            ? "You have successfully joined this loyalty program. Show this QR code to the business scanner."
            : string.Empty;
    }

    public override async Task OnAppearingAsync()
    {
        // Ensure a fresh QR session is prepared as soon as business context exists and no token has been generated yet.
        if (_businessId != Guid.Empty && string.IsNullOrWhiteSpace(QrToken))
        {
            await RefreshAccrualSessionAsync().ConfigureAwait(false);
        }

        StartRotationLoop();
        UpdateGuidanceMessage();
    }

    public override Task OnDisappearingAsync()
    {
        StopRotationLoop();
        return Task.CompletedTask;
    }

    private async Task RefreshAccrualSessionAsync()
    {
        await PrepareSessionAsync(LoyaltyScanMode.Accrual).ConfigureAwait(false);
    }

    private async Task RefreshRedemptionSessionAsync()
    {
        await PrepareSessionAsync(LoyaltyScanMode.Redemption).ConfigureAwait(false);
    }

    private async Task PrepareSessionAsync(LoyaltyScanMode mode)
    {
        if (_businessId == Guid.Empty)
        {
            ErrorMessage = "No business selected yet. Please go to Discover, open a business, and join first.";
            UpdateGuidanceMessage();
            return;
        }

        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _loyaltyService.PrepareScanSessionAsync(
                 _businessId,
                mode,
                selectedRewardIds: null,
                CancellationToken.None).ConfigureAwait(false);

            RunOnMain(() => ApplySessionResult(result));
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
        UpdateGuidanceMessage();
        UpdateExpiresInText();
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


    private void StartRotationLoop()
    {
        if (_rotationLoopCts is not null)
        {
            return;
        }

        _rotationLoopCts = new CancellationTokenSource();
        _ = RunRotationLoopAsync(_rotationLoopCts.Token);
    }

    private void StopRotationLoop()
    {
        if (_rotationLoopCts is null)
        {
            return;
        }

        _rotationLoopCts.Cancel();
        _rotationLoopCts.Dispose();
        _rotationLoopCts = null;
    }

    private async Task RunRotationLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(RotationCheckInterval, ct).ConfigureAwait(false);

                if (_businessId == Guid.Empty)
                {
                    continue;
                }

                RunOnMain(UpdateExpiresInText);

                var expiry = ExpiresAtUtc;
                if (!expiry.HasValue)
                {
                    continue;
                }

                var remaining = expiry.Value - DateTimeOffset.UtcNow;
                if (remaining <= RotationRenewThreshold)
                {
                    await PrepareSessionAsync(Mode).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Keep the loop alive; single tick failures should not kill auto-rotation.
            }
        }
    }


    private void UpdateExpiresInText()
    {
        if (!_expiresAtUtc.HasValue)
        {
            ExpiresInText = string.Empty;
            return;
        }

        var remaining = _expiresAtUtc.Value - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            ExpiresInText = "Refreshing QR...";
            return;
        }

        ExpiresInText = $"Rotates in {remaining:mm\\:ss}";
    }


    private void UpdateGuidanceMessage()
    {
        GuidanceMessage = _businessId == Guid.Empty
            ? DiscoverGuidanceMessage
            : RefreshGuidanceMessage;
    }
}
