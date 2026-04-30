using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Consumer.Resources;
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

    private readonly ILoyaltyService _loyaltyService;
    private readonly TimeProvider _timeProvider;

    private string _qrToken = string.Empty;
    private ImageSource? _qrImage;
    private DateTimeOffset? _expiresAtUtc;
    private LoyaltyScanMode _mode = LoyaltyScanMode.Accrual;
    private Guid _businessId;
    private string _businessDisplayName = string.Empty;
    private string _statusMessage = string.Empty;
    private string _guidanceMessage = AppResources.QrDiscoverGuidanceMessage;
    private string _expiresInText = string.Empty;

    private DateTimeOffset? _lastSuccessfulSessionRefreshUtc;
    private CancellationTokenSource? _rotationLoopCts;

    public QrViewModel(ILoyaltyService loyaltyService, TimeProvider timeProvider)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
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
                ? AppResources.QrJoinedStatusMessage
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
                ErrorMessage = AppResources.QrNoBusinessSelectedMessage;
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
            ErrorMessage = result.Error ?? AppResources.BusinessScanSessionPrepareFailed;
            QrToken = string.Empty;
            ExpiresAtUtc = null;
            return;
        }

        var session = result.Value;
        Mode = session.Mode;
        QrToken = session.Token;
        ExpiresAtUtc = session.ExpiresAtUtc;
        _lastSuccessfulSessionRefreshUtc = _timeProvider.GetUtcNow();

        if (session.Mode == LoyaltyScanMode.Redemption)
        {
            _ = TrackClaimIntentBestEffortAsync();
        }

        UpdateGuidanceMessage();
        UpdateExpiresInText();
    }

    /// <summary>
    /// Tracks a best-effort claim intent event when redemption QR is generated.
    /// This is used to complete promotions conversion funnel measurement.
    /// </summary>
    private async Task TrackClaimIntentBestEffortAsync()
    {
        try
        {
            if (_businessId == Guid.Empty)
            {
                return;
            }

            await _loyaltyService.TrackPromotionInteractionAsync(new TrackPromotionInteractionRequest
            {
                BusinessId = _businessId,
                BusinessName = string.IsNullOrWhiteSpace(BusinessDisplayName) ? string.Empty : BusinessDisplayName,
                Title = "RewardClaimIntent",
                CtaKind = "OpenRedemptionQr",
                EventType = PromotionInteractionEventType.Claim,
                OccurredAtUtc = _timeProvider.GetUtcNow().UtcDateTime
            }, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Intentionally ignore tracking failures to keep QR flow uninterrupted.
        }
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

                var nowUtc = _timeProvider.GetUtcNow();
                var remainingUntilExpiry = expiry.Value - nowUtc;
                var elapsedSinceLastRefresh = nowUtc - (_lastSuccessfulSessionRefreshUtc ?? DateTimeOffset.MinValue);

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
        var remaining = due - _timeProvider.GetUtcNow();

        if (remaining <= TimeSpan.Zero)
        {
            ExpiresInText = AppResources.QrAutoRefreshDueNow;
            return;
        }

        // Floor to seconds for stable one-by-one countdown (avoids visual jumps like 5:01/4:59 oscillations).
        var seconds = Math.Max(0, (int)Math.Floor(remaining.TotalSeconds));
        var display = TimeSpan.FromSeconds(seconds);

        ExpiresInText = string.Format(AppResources.QrAutoRefreshInFormat, display.ToString(@"mm\:ss"));
    }

    private void UpdateGuidanceMessage()
    {
        GuidanceMessage = _businessId == Guid.Empty
            ? AppResources.QrDiscoverGuidanceMessage
            : AppResources.QrRefreshGuidanceMessage;
    }
}
