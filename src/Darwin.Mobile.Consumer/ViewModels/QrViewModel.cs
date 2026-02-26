using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Models.Loyalty;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.ViewModels;
using Darwin.Shared.Results;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using QRCoder;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// View model for the consumer QR screen.
///
/// Responsibilities:
/// - Prepare scan sessions in accrual/redemption mode.
/// - Render QR bitmap from the server-issued opaque token.
/// - Keep QR token fresh with a controlled auto-refresh loop.
///
/// Refresh policy:
/// - UI countdown updates every second for smooth UX.
/// - Automatic network refresh is allowed at minimum every 5 minutes,
///   unless the token is already expired (then immediate refresh is allowed).
/// </summary>
public sealed class QrViewModel : BaseViewModel
{
    /// <summary>
    /// UI tick interval. Kept at 1 second so countdown feels natural.
    /// </summary>
    private static readonly TimeSpan RotationCheckInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// If token is close to expiry, we consider a refresh (subject to minimum cadence guard).
    /// </summary>
    private static readonly TimeSpan RotationRenewThreshold = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Product-required lower bound for automatic refresh cadence.
    /// </summary>
    private static readonly TimeSpan MinimumAutoRotationInterval = TimeSpan.FromMinutes(5);

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

    private DateTimeOffset? _lastSuccessfulSessionRefreshUtc;
    private CancellationTokenSource? _rotationLoopCts;

    public QrViewModel(ILoyaltyService loyaltyService)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        RefreshAccrualSessionCommand = new AsyncCommand(RefreshAccrualSessionAsync);
        RefreshRedemptionSessionCommand = new AsyncCommand(RefreshRedemptionSessionAsync);
    }

    /// <summary>
    /// Current scan session token. Setting it regenerates the QR bitmap.
    /// </summary>
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

    public ImageSource? QrImage
    {
        get => _qrImage;
        private set => SetProperty(ref _qrImage, value);
    }

    /// <summary>
    /// Server-side expiry time for the current QR session.
    /// </summary>
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

    /// <summary>
    /// Countdown text to the next auto-refresh slot (not server expiry).
    /// This is intentionally based on the 5-minute product policy.
    /// </summary>
    public string ExpiresInText
    {
        get => _expiresInText;
        private set => SetProperty(ref _expiresInText, value);
    }

    public LoyaltyScanMode Mode
    {
        get => _mode;
        private set => SetProperty(ref _mode, value);
    }

    public string BusinessDisplayName
    {
        get => _businessDisplayName;
        private set => SetProperty(ref _businessDisplayName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string GuidanceMessage
    {
        get => _guidanceMessage;
        private set => SetProperty(ref _guidanceMessage, value);
    }

    public bool HasBusinessContext => _businessId != Guid.Empty;

    public AsyncCommand RefreshAccrualSessionCommand { get; }

    public AsyncCommand RefreshRedemptionSessionCommand { get; }

    public void SetBusiness(Guid businessId)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException("Business id must not be empty.", nameof(businessId));

        if (_businessId != businessId)
        {
            RunOnMain(() =>
            {
                _businessId = businessId;
                _lastSuccessfulSessionRefreshUtc = null;

                QrToken = string.Empty;
                ExpiresAtUtc = null;
                ErrorMessage = null;

                OnPropertyChanged(nameof(HasBusinessContext));
                UpdateGuidanceMessage();
            });
        }
    }

    public void SetBusinessDisplayName(string? businessName)
    {
        RunOnMain(() => BusinessDisplayName = string.IsNullOrWhiteSpace(businessName) ? string.Empty : businessName.Trim());
    }

    public void SetJoinedStatus(bool justJoined)
    {
        RunOnMain(() =>
        {
            StatusMessage = justJoined
                ? "You have successfully joined this loyalty program. Show this QR code to the business scanner."
                : string.Empty;
        });
    }

    public override async Task OnAppearingAsync()
    {
        if (_businessId != Guid.Empty && string.IsNullOrWhiteSpace(QrToken))
        {
            await RefreshAccrualSessionAsync();
        }

        StartRotationLoop();
        RunOnMain(UpdateGuidanceMessage);
    }

    public override Task OnDisappearingAsync()
    {
        StopRotationLoop();
        return Task.CompletedTask;
    }

    private async Task RefreshAccrualSessionAsync()
    {
        await PrepareSessionAsync(LoyaltyScanMode.Accrual);
    }

    private async Task RefreshRedemptionSessionAsync()
    {
        await PrepareSessionAsync(LoyaltyScanMode.Redemption);
    }

    private async Task PrepareSessionAsync(LoyaltyScanMode mode)
    {
        if (_businessId == Guid.Empty)
        {
            RunOnMain(() =>
            {
                ErrorMessage = "No business selected yet. Please go to Discover, open a business, and join first.";
                UpdateGuidanceMessage();
            });
            return;
        }

        if (IsBusy)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
        });

        try
        {
            var result = await _loyaltyService.PrepareScanSessionAsync(
                _businessId,
                mode,
                selectedRewardIds: null,
                CancellationToken.None);

            RunOnMain(() => ApplySessionResult(result));
        }
        finally
        {
            RunOnMain(() => IsBusy = false);
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
        _lastSuccessfulSessionRefreshUtc = DateTimeOffset.UtcNow;

        UpdateGuidanceMessage();
        UpdateExpiresInText();
    }

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
            var bytes = png.GetGraphic(20);
            QrImage = ImageSource.FromStream(() => new MemoryStream(bytes));
        }
        catch
        {
            QrImage = null;
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
                await Task.Delay(RotationCheckInterval, ct);

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

                var remainingUntilExpiry = expiry.Value - DateTimeOffset.UtcNow;
                var elapsedSinceLastRefresh = DateTimeOffset.UtcNow - (_lastSuccessfulSessionRefreshUtc ?? DateTimeOffset.MinValue);

                // Refresh immediately when token is already expired.
                if (remainingUntilExpiry <= TimeSpan.Zero)
                {
                    await MainThread.InvokeOnMainThreadAsync(() => PrepareSessionAsync(Mode));
                    continue;
                }

                // If token is near expiry, refresh only if the 5-minute cadence guard has passed.
                if (remainingUntilExpiry <= RotationRenewThreshold &&
                    elapsedSinceLastRefresh >= MinimumAutoRotationInterval)
                {
                    await MainThread.InvokeOnMainThreadAsync(() => PrepareSessionAsync(Mode));
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // A single background tick failure must not kill the loop.
            }
        }
    }

    private void UpdateExpiresInText()
    {
        if (!_lastSuccessfulSessionRefreshUtc.HasValue)
        {
            ExpiresInText = string.Empty;
            return;
        }

        var due = _lastSuccessfulSessionRefreshUtc.Value + MinimumAutoRotationInterval;
        var remaining = due - DateTimeOffset.UtcNow;

        if (remaining <= TimeSpan.Zero)
        {
            ExpiresInText = "Auto refresh is due now.";
            return;
        }

        // Floor to seconds for stable one-by-one countdown (avoids visual jumps like 5:01/4:59 oscillations).
        var seconds = Math.Max(0, (int)Math.Floor(remaining.TotalSeconds));
        var display = TimeSpan.FromSeconds(seconds);

        ExpiresInText = $"Auto refresh in {display:mm\\:ss}";
    }

    private void UpdateGuidanceMessage()
    {
        GuidanceMessage = _businessId == Guid.Empty
            ? DiscoverGuidanceMessage
            : RefreshGuidanceMessage;
    }
}
