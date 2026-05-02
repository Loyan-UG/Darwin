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

    /// <summary>
    /// Upper bound for best-effort promotion analytics fired from the QR screen.
    /// The QR flow must stay responsive even when analytics cannot be delivered quickly.
    /// </summary>
    private static readonly TimeSpan PromotionTrackingTimeout = TimeSpan.FromSeconds(5);

    private readonly ILoyaltyService _loyaltyService;
    private readonly TimeProvider _timeProvider;

    private string _qrToken = string.Empty;
    private string _qrTokenDisplay = string.Empty;
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
    private CancellationTokenSource? _sessionRequestCts;

    public QrViewModel(ILoyaltyService loyaltyService, TimeProvider timeProvider)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        RefreshAccrualSessionCommand = new AsyncCommand(RefreshAccrualSessionAsync, () => !IsBusy);
        RefreshRedemptionSessionCommand = new AsyncCommand(RefreshRedemptionSessionAsync, () => !IsBusy);
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
                QrTokenDisplay = ToSafeTokenDisplay(value);
                GenerateQrImage();
            }
        }
    }

    /// <summary>
    /// Gets a redacted QR token fingerprint that is safe to show on screen.
    /// </summary>
    public string QrTokenDisplay
    {
        get => _qrTokenDisplay;
        private set => SetProperty(ref _qrTokenDisplay, value);
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
        CancelInFlightSessionRequest();
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
            RaiseSessionCommandStates();
        });

        var requestCts = StartSessionRequest();

        try
        {
            var result = await _loyaltyService.PrepareScanSessionAsync(
                _businessId,
                mode,
                selectedRewardIds: null,
                requestCts.Token).ConfigureAwait(false);

            RunOnMain(() => ApplySessionResult(result));
        }
        catch (OperationCanceledException)
        {
            // Session preparation is cancelled when the page disappears or a newer request supersedes it.
        }
        finally
        {
            CompleteSessionRequest(requestCts);

            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseSessionCommandStates();
            });
        }
    }

    /// <summary>
    /// Keeps manual QR refresh actions disabled while a scan session request is in flight.
    /// </summary>
    private void RaiseSessionCommandStates()
    {
        RefreshAccrualSessionCommand.RaiseCanExecuteChanged();
        RefreshRedemptionSessionCommand.RaiseCanExecuteChanged();
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

            using var trackingCancellation = CreatePromotionTrackingCancellation();
            await _loyaltyService.TrackPromotionInteractionAsync(new TrackPromotionInteractionRequest
            {
                BusinessId = _businessId,
                BusinessName = string.IsNullOrWhiteSpace(BusinessDisplayName) ? string.Empty : BusinessDisplayName,
                Title = "RewardClaimIntent",
                CtaKind = "OpenRedemptionQr",
                EventType = PromotionInteractionEventType.Claim,
                OccurredAtUtc = _timeProvider.GetUtcNow().UtcDateTime
            }, trackingCancellation.Token).ConfigureAwait(false);
        }
        catch
        {
            // Intentionally ignore tracking failures to keep QR flow uninterrupted.
        }
    }

    /// <summary>
    /// Creates a bounded token for non-critical QR analytics calls.
    /// This prevents telemetry from surviving longer than the user-facing operation deserves.
    /// </summary>
    /// <returns>A cancellation token that expires after the promotion tracking timeout.</returns>
    private static CancellationTokenSource CreatePromotionTrackingCancellation()
    {
        return new CancellationTokenSource(PromotionTrackingTimeout);
    }

    private void GenerateQrImage()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_qrToken))
            {
                RunOnMain(() => QrImage = null);
                return;
            }

            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(_qrToken, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            var bytes = png.GetGraphic(20);
            RunOnMain(() => QrImage = ImageSource.FromStream(() => new MemoryStream(bytes)));
        }
        catch
        {
            RunOnMain(() => QrImage = null);
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

    /// <summary>
    /// Creates a cancellable scope for the current scan-session request and cancels any older in-flight request.
    /// </summary>
    /// <returns>The request cancellation source owned by the current operation.</returns>
    private CancellationTokenSource StartSessionRequest()
    {
        var requestCts = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _sessionRequestCts, requestCts);
        previous?.Cancel();
        return requestCts;
    }

    /// <summary>
    /// Disposes the completed request source only if it is still the active request source.
    /// </summary>
    /// <param name="requestCts">Request cancellation source created for the completed operation.</param>
    private void CompleteSessionRequest(CancellationTokenSource requestCts)
    {
        if (ReferenceEquals(_sessionRequestCts, requestCts))
        {
            _sessionRequestCts = null;
        }

        requestCts.Dispose();
    }

    /// <summary>
    /// Cancels any scan-session request that should no longer update this view model.
    /// </summary>
    private void CancelInFlightSessionRequest()
    {
        var requestCts = Interlocked.Exchange(ref _sessionRequestCts, null);
        requestCts?.Cancel();
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

    /// <summary>
    /// Redacts the scan-session token before showing it in the UI while keeping a useful support fingerprint.
    /// </summary>
    /// <param name="token">Raw scan-session token.</param>
    /// <returns>A shortened token fingerprint safe for on-screen display.</returns>
    private static string ToSafeTokenDisplay(string token)
    {
        var normalized = string.IsNullOrWhiteSpace(token) ? string.Empty : token.Trim();
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        if (normalized.Length <= 12)
        {
            return "••••";
        }

        return $"{normalized[..6]}…{normalized[^6..]}";
    }
}
