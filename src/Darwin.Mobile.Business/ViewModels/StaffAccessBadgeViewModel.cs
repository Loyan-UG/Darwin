using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Business.Services.Identity;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.ViewModels;
using QRCoder;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// Provides a short-lived, rotating QR badge for internal staff access workflows.
///
/// Security and product notes:
/// - This badge is a convenience identifier for internal flows only and must not be treated as a standalone authorization credential.
/// - Server-side services remain the source of truth for any access decision.
/// - Payload includes explicit expiry and a nonce so downstream systems can enforce freshness and replay defenses.
/// </summary>
public sealed class StaffAccessBadgeViewModel : BaseViewModel
{
    private static readonly TimeSpan BadgeLifetime = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan CountdownTick = TimeSpan.FromSeconds(1);

    private readonly IBusinessIdentityContextService _identityContextService;
    private readonly IBusinessAuthorizationService _authorizationService;
    private readonly TimeProvider _timeProvider;

    private string _badgePayload = string.Empty;
    private ImageSource? _badgeImage;
    private DateTimeOffset? _expiresAtUtc;
    private string _expiresInText = string.Empty;
    private string _badgeSummary = string.Empty;
    private string _operatorRole = string.Empty;
    private string _operatorRoleDisplay = string.Empty;

    private CancellationTokenSource? _countdownLoopCts;

    public StaffAccessBadgeViewModel(
        IBusinessIdentityContextService identityContextService,
        IBusinessAuthorizationService authorizationService,
        TimeProvider timeProvider)
    {
        _identityContextService = identityContextService ?? throw new ArgumentNullException(nameof(identityContextService));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        RefreshBadgeCommand = new AsyncCommand(RefreshBadgeAsync, () => !IsBusy);
    }

    /// <summary>
    /// Current raw payload encoded into the QR image.
    /// </summary>
    public string BadgePayload
    {
        get => _badgePayload;
        private set
        {
            if (SetProperty(ref _badgePayload, value))
            {
                GenerateBadgeImage();
            }
        }
    }

    /// <summary>
    /// Rendered QR image for the current badge payload.
    /// </summary>
    public ImageSource? BadgeImage
    {
        get => _badgeImage;
        private set => SetProperty(ref _badgeImage, value);
    }

    /// <summary>
    /// Badge expiration timestamp in UTC.
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc
    {
        get => _expiresAtUtc;
        private set
        {
            if (SetProperty(ref _expiresAtUtc, value))
            {
                UpdateExpiryText();
            }
        }
    }

    /// <summary>
    /// Human-readable countdown until badge expiry.
    /// </summary>
    public string ExpiresInText
    {
        get => _expiresInText;
        private set => SetProperty(ref _expiresInText, value);
    }

    /// <summary>
    /// Compact summary displayed under the QR to help operators verify context.
    /// </summary>
    public string BadgeSummary
    {
        get => _badgeSummary;
        private set => SetProperty(ref _badgeSummary, value);
    }

    /// <summary>
    /// Operator role snapshot derived from token capabilities.
    /// </summary>
    public string OperatorRole
    {
        get => _operatorRole;
        private set => SetProperty(ref _operatorRole, value);
    }

    /// <summary>
    /// Localized role line shown below the QR badge.
    /// </summary>
    public string OperatorRoleDisplay
    {
        get => _operatorRoleDisplay;
        private set => SetProperty(ref _operatorRoleDisplay, value);
    }

    public AsyncCommand RefreshBadgeCommand { get; }

    public override async Task OnAppearingAsync()
    {
        if (string.IsNullOrWhiteSpace(BadgePayload))
        {
            await RefreshBadgeAsync().ConfigureAwait(false);
        }

        StartCountdownLoop();
    }

    public override Task OnDisappearingAsync()
    {
        StopCountdownLoop();
        return Task.CompletedTask;
    }

    private async Task RefreshBadgeAsync()
    {
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
            var identityResult = await _identityContextService.GetCurrentAsync(CancellationToken.None).ConfigureAwait(false);
            if (!identityResult.Succeeded || identityResult.Value is null)
            {
                RunOnMain(() => ErrorMessage = AppResources.StaffAccessBadgeLoadFailed);
                return;
            }

            var authorizationResult = await _authorizationService.GetSnapshotAsync(CancellationToken.None).ConfigureAwait(false);

            var context = identityResult.Value;
            var role = authorizationResult.Succeeded && authorizationResult.Value is not null
                ? authorizationResult.Value.RoleDisplayName
                : AppResources.StaffAccessBadgeUnknownRole;

            var expiresAtUtc = _timeProvider.GetUtcNow().Add(BadgeLifetime);
            var payload = BuildBadgePayload(context, role, expiresAtUtc);

            RunOnMain(() =>
            {
                OperatorRole = role;
                OperatorRoleDisplay = string.Format(AppResources.StaffAccessBadgeRoleFormat, role);
                BadgeSummary = string.Format(AppResources.StaffAccessBadgeSummaryFormat, context.BusinessName, context.OperatorEmail);
                ExpiresAtUtc = expiresAtUtc;
                BadgePayload = payload;
                ErrorMessage = null;
            });
        }
        catch
        {
            RunOnMain(() => ErrorMessage = AppResources.StaffAccessBadgeLoadFailed);
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RefreshBadgeCommand.RaiseCanExecuteChanged();
            });
        }
    }

    private string BuildBadgePayload(BusinessIdentityContext context, string roleDisplayName, DateTimeOffset expiresAtUtc)
    {
        var payload = new
        {
            Type = "staff-access-badge",
            Version = 1,
            BusinessId = context.BusinessId,
            BusinessName = context.BusinessName,
            OperatorEmail = context.OperatorEmail,
            Role = roleDisplayName,
            IssuedAtUtc = _timeProvider.GetUtcNow(),
            ExpiresAtUtc = expiresAtUtc,
            Nonce = Guid.NewGuid().ToString("N")
        };

        return JsonSerializer.Serialize(payload);
    }

    private void GenerateBadgeImage()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(BadgePayload))
            {
                BadgeImage = null;
                return;
            }

            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(BadgePayload, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            var bytes = png.GetGraphic(20);

            BadgeImage = ImageSource.FromStream(() => new MemoryStream(bytes));
        }
        catch
        {
            BadgeImage = null;
        }
    }

    private void StartCountdownLoop()
    {
        if (_countdownLoopCts is not null)
        {
            return;
        }

        _countdownLoopCts = new CancellationTokenSource();
        _ = RunCountdownLoopAsync(_countdownLoopCts.Token);
    }

    private void StopCountdownLoop()
    {
        if (_countdownLoopCts is null)
        {
            return;
        }

        _countdownLoopCts.Cancel();
        _countdownLoopCts.Dispose();
        _countdownLoopCts = null;
    }

    private async Task RunCountdownLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(CountdownTick, cancellationToken).ConfigureAwait(false);

                RunOnMain(UpdateExpiryText);

                var expiresAtUtc = ExpiresAtUtc;
                if (!expiresAtUtc.HasValue)
                {
                    continue;
                }

                if (expiresAtUtc.Value <= _timeProvider.GetUtcNow())
                {
                    await RefreshBadgeAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Keep the loop alive; transient failures must not kill countdown updates.
            }
        }
    }

    private void UpdateExpiryText()
    {
        if (!ExpiresAtUtc.HasValue)
        {
            ExpiresInText = string.Empty;
            return;
        }

        var remaining = ExpiresAtUtc.Value - _timeProvider.GetUtcNow();
        if (remaining <= TimeSpan.Zero)
        {
            ExpiresInText = AppResources.StaffAccessBadgeExpired;
            return;
        }

        var totalSeconds = Math.Max(0, (int)Math.Floor(remaining.TotalSeconds));
        var display = TimeSpan.FromSeconds(totalSeconds);
        ExpiresInText = string.Format(AppResources.StaffAccessBadgeExpiresInFormat, display.ToString(@"mm\:ss"));
    }
}
