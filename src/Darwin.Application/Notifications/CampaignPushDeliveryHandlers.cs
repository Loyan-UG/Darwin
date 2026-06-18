using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Security;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Entities.Marketing;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Notifications;

public sealed class CampaignPushDeliveryWriter
{
    private const string PushCampaignAnalyticsLabel = "campaign_push";

    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public CampaignPushDeliveryWriter(IAppDbContext db, IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task QueueForActivatedCampaignAsync(Campaign campaign, CancellationToken ct = default)
    {
        if (campaign.Id == Guid.Empty ||
            campaign.BusinessId is not { } businessId ||
            (campaign.Channels & CampaignChannels.Push) != CampaignChannels.Push)
        {
            return;
        }

        var recipientUserIds = await _db.Set<LoyaltyAccount>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.BusinessId == businessId && x.Status == LoyaltyAccountStatus.Active)
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (recipientUserIds.Count == 0)
        {
            return;
        }

        var devices = await _db.Set<UserDevice>()
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.IsActive &&
                x.NotificationsEnabled &&
                x.PushToken != null &&
                recipientUserIds.Contains(x.UserId))
            .Select(x => new
            {
                x.UserId,
                x.DeviceId
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (devices.Count == 0)
        {
            return;
        }

        var idempotencyKeys = devices
            .Select(x => BuildIdempotencyKey(campaign.Id, x.UserId, x.DeviceId))
            .ToList();
        var existingKeys = await _db.Set<CampaignDelivery>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IdempotencyKey != null && idempotencyKeys.Contains(x.IdempotencyKey))
            .Select(x => x.IdempotencyKey!)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var existing = existingKeys.ToHashSet(StringComparer.Ordinal);
        var nowUtc = _clock.UtcNow;
        var payloadHash = BuildPayloadHash(campaign);

        foreach (var device in devices)
        {
            var idempotencyKey = BuildIdempotencyKey(campaign.Id, device.UserId, device.DeviceId);
            if (existing.Contains(idempotencyKey))
            {
                continue;
            }

            _db.Set<CampaignDelivery>().Add(new CampaignDelivery
            {
                CampaignId = campaign.Id,
                RecipientUserId = device.UserId,
                BusinessId = businessId,
                Channel = CampaignDeliveryChannel.Push,
                Status = CampaignDeliveryStatus.Pending,
                Destination = device.DeviceId,
                AttemptCount = 0,
                IdempotencyKey = idempotencyKey,
                PayloadHash = payloadHash,
                LastAttemptAtUtc = null,
                FirstAttemptAtUtc = null
            });
        }
    }

    internal static string BuildIdempotencyKey(Guid campaignId, Guid userId, string deviceId)
        => $"campaign-push:{campaignId:N}:{userId:N}:{NormalizeDeviceId(deviceId)}";

    internal static string BuildPayloadHash(Campaign campaign)
    {
        var input = $"{campaign.Title}\n{campaign.Body}\n{campaign.Subtitle}\n{campaign.LandingUrl}\n{PushCampaignAnalyticsLabel}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }

    private static string NormalizeDeviceId(string deviceId)
    {
        var raw = string.IsNullOrWhiteSpace(deviceId) ? "unknown" : deviceId.Trim();
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant()[..24];
    }
}

public sealed class ProcessPendingPushDeliveriesHandler
{
    private const int MaxAttempts = 3;
    private const int BatchSize = 25;

    private readonly IAppDbContext _db;
    private readonly IPushNotificationSender _pushSender;
    private readonly IProtectedStringService _protectedStringService;
    private readonly NotificationInboxWriter _notificationInboxWriter;
    private readonly IClock _clock;

    public ProcessPendingPushDeliveriesHandler(
        IAppDbContext db,
        IPushNotificationSender pushSender,
        IProtectedStringService protectedStringService,
        NotificationInboxWriter notificationInboxWriter,
        IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _pushSender = pushSender ?? throw new ArgumentNullException(nameof(pushSender));
        _protectedStringService = protectedStringService ?? throw new ArgumentNullException(nameof(protectedStringService));
        _notificationInboxWriter = notificationInboxWriter ?? throw new ArgumentNullException(nameof(notificationInboxWriter));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<Result<int>> HandleAsync(CancellationToken ct = default)
    {
        var nowUtc = _clock.UtcNow;
        var deliveries = await _db.Set<CampaignDelivery>()
            .Where(x =>
                !x.IsDeleted &&
                x.Channel == CampaignDeliveryChannel.Push &&
                x.Status == CampaignDeliveryStatus.Pending &&
                x.AttemptCount < MaxAttempts)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(BatchSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (deliveries.Count == 0)
        {
            return Result<int>.Ok(0);
        }

        var processed = 0;
        foreach (var delivery in deliveries)
        {
            ct.ThrowIfCancellationRequested();
            delivery.Status = CampaignDeliveryStatus.InProgress;
            delivery.FirstAttemptAtUtc ??= nowUtc;
            delivery.LastAttemptAtUtc = nowUtc;
            delivery.AttemptCount = Math.Max(0, delivery.AttemptCount) + 1;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            var campaign = await _db.Set<Campaign>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == delivery.CampaignId, ct)
                .ConfigureAwait(false);
            var device = await _db.Set<UserDevice>()
                .FirstOrDefaultAsync(x =>
                    !x.IsDeleted &&
                    x.IsActive &&
                    x.UserId == delivery.RecipientUserId &&
                    x.DeviceId == delivery.Destination, ct)
                .ConfigureAwait(false);

            var pushToken = _protectedStringService.Unprotect(device?.PushToken);
            if (campaign is null || device is null || string.IsNullOrWhiteSpace(pushToken) || !device.NotificationsEnabled)
            {
                delivery.Status = CampaignDeliveryStatus.Failed;
                delivery.LastError = "Gateway.DeviceUnavailable";
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                processed++;
                continue;
            }

            var sendResult = await _pushSender.SendAsync(new PushNotificationSendRequest
            {
                NotificationId = delivery.Id,
                UserId = device.UserId,
                DeviceId = device.DeviceId,
                PushToken = pushToken,
                Platform = device.Platform.ToString(),
                TargetApp = "Consumer",
                Title = campaign.Title,
                Body = string.IsNullOrWhiteSpace(campaign.Body) ? campaign.Subtitle ?? campaign.Title : campaign.Body,
                DeepLink = BuildCampaignDeepLink(campaign),
                SourceType = "campaign",
                SourceId = campaign.Id,
                CollapseKey = $"campaign-{campaign.Id:N}",
                AnalyticsLabel = "campaign_push",
                IdempotencyKey = delivery.IdempotencyKey ?? CampaignPushDeliveryWriter.BuildIdempotencyKey(campaign.Id, device.UserId, device.DeviceId)
            }, ct).ConfigureAwait(false);

            ApplySendResult(delivery, device, sendResult.Value, sendResult.Succeeded);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            if (delivery.Status == CampaignDeliveryStatus.Failed)
            {
                await CreateBusinessFailureNotificationAsync(campaign, delivery.LastError, ct).ConfigureAwait(false);
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            processed++;
        }

        return Result<int>.Ok(processed);
    }

    private static void ApplySendResult(
        CampaignDelivery delivery,
        UserDevice device,
        PushNotificationSendResult? result,
        bool senderSucceeded)
    {
        if (!senderSucceeded || result is null)
        {
            delivery.Status = delivery.AttemptCount >= MaxAttempts ? CampaignDeliveryStatus.Failed : CampaignDeliveryStatus.Pending;
            delivery.LastError = "Gateway.TransportError";
            return;
        }

        delivery.LastResponseCode = result.ResponseCode;
        delivery.ProviderMessageId = string.IsNullOrWhiteSpace(result.ProviderMessageId) ? delivery.ProviderMessageId : result.ProviderMessageId;

        if (string.IsNullOrWhiteSpace(result.FailureCode))
        {
            delivery.Status = CampaignDeliveryStatus.Succeeded;
            delivery.LastError = null;
            return;
        }

        delivery.LastError = Truncate(result.FailureCode, 2000);
        if (result.IsInvalidTokenFailure)
        {
            delivery.Status = CampaignDeliveryStatus.Failed;
            device.PushToken = null;
            device.NotificationsEnabled = false;
            device.IsActive = false;
            return;
        }

        delivery.Status = result.IsTransientFailure && delivery.AttemptCount < MaxAttempts
            ? CampaignDeliveryStatus.Pending
            : CampaignDeliveryStatus.Failed;
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

    private async Task CreateBusinessFailureNotificationAsync(Campaign campaign, string? failureCode, CancellationToken ct)
    {
        if (campaign.BusinessId is not { } businessId || businessId == Guid.Empty)
        {
            return;
        }

        var memberUserIds = await _db.Set<BusinessMember>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive && x.BusinessId == businessId)
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (memberUserIds.Count == 0)
        {
            return;
        }

        await _notificationInboxWriter.CreateOrUpdateForUsersAsync(
                memberUserIds,
                NotificationCategory.Campaign,
                NotificationTargetApp.Business,
                "Push delivery needs attention",
                $"{campaign.Title} has failed push deliveries. Reason: {SafeFailureSummary(failureCode)}.",
                $"loyan://campaign/{campaign.Id:D}",
                "business-campaign-push-failure",
                campaign.Id,
                campaign.EndsAtUtc,
                ct)
            .ConfigureAwait(false);
    }

    private static string BuildCampaignDeepLink(Campaign campaign)
        => campaign.BusinessId is { } businessId && businessId != Guid.Empty
            ? $"loyan://business/{businessId:D}"
            : "loyan://feed";

    private static string SafeFailureSummary(string? failureCode)
    {
        if (string.IsNullOrWhiteSpace(failureCode))
        {
            return "delivery failed";
        }

        return failureCode.Contains("Token", StringComparison.OrdinalIgnoreCase)
            ? "device token unavailable"
            : failureCode.Length <= 120 ? failureCode : failureCode[..120];
    }
}
