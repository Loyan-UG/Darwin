using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Entities.Marketing;
using Darwin.Domain.Entities.Notifications;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Notifications;

public sealed class GetNotificationInboxHandler
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetNotificationInboxHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public async Task<Result<NotificationInboxPageDto>> HandleAsync(NotificationInboxQueryDto dto, CancellationToken ct = default)
    {
        var userId = _currentUser.GetCurrentUserId();
        var page = dto.Page <= 0 ? 1 : dto.Page;
        var pageSize = dto.PageSize <= 0 ? 30 : Math.Min(dto.PageSize, 100);
        var targetApp = dto.TargetApp;
        var category = dto.Category;

        var nowUtc = DateTime.UtcNow;
        var query =
            from recipient in _db.Set<NotificationRecipient>().AsNoTracking()
            join message in _db.Set<NotificationMessage>().AsNoTracking()
                on recipient.NotificationMessageId equals message.Id
            where !recipient.IsDeleted &&
                  !message.IsDeleted &&
                  recipient.UserId == userId &&
                  recipient.ArchivedAtUtc == null &&
                  (!message.ExpiresAtUtc.HasValue || message.ExpiresAtUtc.Value > nowUtc) &&
                  (targetApp == NotificationTargetApp.Both ||
                   message.TargetApp == NotificationTargetApp.Both ||
                   message.TargetApp == targetApp)
            select new { recipient, message };

        if (category.HasValue)
        {
            query = query.Where(x => x.message.Category == category.Value);
        }

        var total = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(x => x.message.PublishedAtUtc)
            .ThenByDescending(x => x.recipient.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new NotificationInboxItemDto
            {
                Id = x.recipient.Id,
                MessageId = x.message.Id,
                Title = x.message.Title,
                Body = x.message.Body ?? string.Empty,
                Category = x.message.Category,
                CreatedAtUtc = x.message.PublishedAtUtc,
                ReadAtUtc = x.recipient.ReadAtUtc,
                DeepLink = x.message.DeepLink,
                SourceType = x.message.SourceType,
                SourceId = x.message.SourceId
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return Result<NotificationInboxPageDto>.Ok(new NotificationInboxPageDto
        {
            Items = items,
            Total = total
        });
    }
}

public sealed class GetNotificationUnreadCountHandler
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetNotificationUnreadCountHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public async Task<Result<int>> HandleAsync(NotificationTargetApp targetApp, CancellationToken ct = default)
    {
        var userId = _currentUser.GetCurrentUserId();
        var domainTarget = targetApp;
        var nowUtc = DateTime.UtcNow;

        var count = await (from recipient in _db.Set<NotificationRecipient>().AsNoTracking()
                           join message in _db.Set<NotificationMessage>().AsNoTracking()
                               on recipient.NotificationMessageId equals message.Id
                           where !recipient.IsDeleted &&
                                 !message.IsDeleted &&
                                 recipient.UserId == userId &&
                                 recipient.ReadAtUtc == null &&
                                 recipient.ArchivedAtUtc == null &&
                                 (!message.ExpiresAtUtc.HasValue || message.ExpiresAtUtc.Value > nowUtc) &&
                                 (domainTarget == NotificationTargetApp.Both ||
                                  message.TargetApp == NotificationTargetApp.Both ||
                                  message.TargetApp == domainTarget)
                           select recipient.Id)
            .CountAsync(ct)
            .ConfigureAwait(false);

        return Result<int>.Ok(count);
    }
}

public sealed class MarkNotificationReadHandler
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public MarkNotificationReadHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IClock clock,
        IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public async Task<Result<int>> HandleAsync(Guid recipientId, NotificationTargetApp targetApp, CancellationToken ct = default)
    {
        if (recipientId == Guid.Empty)
        {
            return Result<int>.Fail(_localizer["InvalidDeleteRequest"]);
        }

        var userId = _currentUser.GetCurrentUserId();
        var recipient = await (from row in _db.Set<NotificationRecipient>()
                               join message in _db.Set<NotificationMessage>()
                                   on row.NotificationMessageId equals message.Id
                               where !row.IsDeleted &&
                                     !message.IsDeleted &&
                                     row.Id == recipientId &&
                                     row.UserId == userId &&
                                     (targetApp == NotificationTargetApp.Both ||
                                      message.TargetApp == NotificationTargetApp.Both ||
                                      message.TargetApp == targetApp)
                               select row)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (recipient is null)
        {
            return Result<int>.Fail(_localizer["NotificationNotFound"]);
        }

        recipient.ReadAtUtc ??= _clock.UtcNow;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return await new GetNotificationUnreadCountHandler(_db, _currentUser)
            .HandleAsync(targetApp, ct)
            .ConfigureAwait(false);
    }
}

public sealed class MarkAllNotificationsReadHandler
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;

    public MarkAllNotificationsReadHandler(IAppDbContext db, ICurrentUserService currentUser, IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<Result<int>> HandleAsync(NotificationTargetApp targetApp, CancellationToken ct = default)
    {
        var userId = _currentUser.GetCurrentUserId();
        var domainTarget = targetApp;

        var recipients = await (from recipient in _db.Set<NotificationRecipient>()
                                join message in _db.Set<NotificationMessage>()
                                    on recipient.NotificationMessageId equals message.Id
                                where !recipient.IsDeleted &&
                                      !message.IsDeleted &&
                                      recipient.UserId == userId &&
                                      recipient.ReadAtUtc == null &&
                                      recipient.ArchivedAtUtc == null &&
                                      (domainTarget == NotificationTargetApp.Both ||
                                       message.TargetApp == NotificationTargetApp.Both ||
                                       message.TargetApp == domainTarget)
                                select recipient)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var nowUtc = _clock.UtcNow;
        foreach (var recipient in recipients)
        {
            recipient.ReadAtUtc = nowUtc;
        }

        if (recipients.Count > 0)
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return Result<int>.Ok(0);
    }
}

public sealed class NotificationInboxWriter
{
    private const string CampaignSourceType = "campaign";

    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public NotificationInboxWriter(IAppDbContext db, IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task CreateForActivatedCampaignAsync(Campaign campaign, CancellationToken ct = default)
    {
        if (campaign.Id == Guid.Empty ||
            campaign.BusinessId is not { } businessId ||
            (campaign.Channels & CampaignChannels.InApp) != CampaignChannels.InApp)
        {
            return;
        }

        var nowUtc = _clock.UtcNow;
        var message = await _db.Set<NotificationMessage>()
            .FirstOrDefaultAsync(x =>
                !x.IsDeleted &&
                x.SourceType == CampaignSourceType &&
                x.SourceId == campaign.Id &&
                x.TargetApp == NotificationTargetApp.Consumer, ct)
            .ConfigureAwait(false);

        if (message is null)
        {
            message = new NotificationMessage
            {
                Id = Guid.NewGuid(),
                Category = NotificationCategory.Campaign,
                TargetApp = NotificationTargetApp.Consumer,
                Title = campaign.Title,
                Body = string.IsNullOrWhiteSpace(campaign.Body) ? campaign.Subtitle : campaign.Body,
                DeepLink = BuildCampaignDeepLink(campaign),
                SourceType = CampaignSourceType,
                SourceId = campaign.Id,
                PublishedAtUtc = nowUtc,
                ExpiresAtUtc = campaign.EndsAtUtc
            };

            _db.Set<NotificationMessage>().Add(message);
        }
        else
        {
            message.Title = campaign.Title;
            message.Body = string.IsNullOrWhiteSpace(campaign.Body) ? campaign.Subtitle : campaign.Body;
            message.DeepLink = BuildCampaignDeepLink(campaign);
            message.PublishedAtUtc = nowUtc;
            message.ExpiresAtUtc = campaign.EndsAtUtc;
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

        var existingRecipientUserIds = await _db.Set<NotificationRecipient>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.NotificationMessageId == message.Id)
            .Select(x => x.UserId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var existingSet = existingRecipientUserIds.ToHashSet();
        foreach (var userId in recipientUserIds)
        {
            if (existingSet.Contains(userId))
            {
                continue;
            }

            _db.Set<NotificationRecipient>().Add(new NotificationRecipient
            {
                NotificationMessageId = message.Id,
                UserId = userId,
                DeliveredAtUtc = nowUtc
            });
        }
    }

    public async Task CreateForUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        NotificationCategory category,
        NotificationTargetApp targetApp,
        string title,
        string? body,
        string? deepLink,
        string? sourceType,
        Guid? sourceId,
        DateTime? expiresAtUtc = null,
        CancellationToken ct = default)
        => await CreateForUsersCoreAsync(userIds, category, targetApp, title, body, deepLink, sourceType, sourceId, expiresAtUtc, reuseExistingMessage: false, ct)
            .ConfigureAwait(false);

    public async Task CreateOrUpdateForUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        NotificationCategory category,
        NotificationTargetApp targetApp,
        string title,
        string? body,
        string? deepLink,
        string? sourceType,
        Guid? sourceId,
        DateTime? expiresAtUtc = null,
        CancellationToken ct = default)
        => await CreateForUsersCoreAsync(userIds, category, targetApp, title, body, deepLink, sourceType, sourceId, expiresAtUtc, reuseExistingMessage: true, ct)
            .ConfigureAwait(false);

    private async Task CreateForUsersCoreAsync(
        IReadOnlyCollection<Guid> userIds,
        NotificationCategory category,
        NotificationTargetApp targetApp,
        string title,
        string? body,
        string? deepLink,
        string? sourceType,
        Guid? sourceId,
        DateTime? expiresAtUtc,
        bool reuseExistingMessage,
        CancellationToken ct)
    {
        if (userIds.Count == 0 || string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        var nowUtc = _clock.UtcNow;
        var normalizedSourceType = string.IsNullOrWhiteSpace(sourceType) ? null : sourceType.Trim();
        NotificationMessage? message = null;
        if (reuseExistingMessage && !string.IsNullOrWhiteSpace(normalizedSourceType) && sourceId.HasValue)
        {
            message = await _db.Set<NotificationMessage>()
                .FirstOrDefaultAsync(x =>
                    !x.IsDeleted &&
                    x.TargetApp == targetApp &&
                    x.SourceType == normalizedSourceType &&
                    x.SourceId == sourceId.Value, ct)
                .ConfigureAwait(false);
        }

        if (message is null)
        {
            message = new NotificationMessage
            {
                Id = Guid.NewGuid(),
                Category = category,
                TargetApp = targetApp,
                SourceType = normalizedSourceType,
                SourceId = sourceId
            };
            _db.Set<NotificationMessage>().Add(message);
        }

        message.Category = category;
        message.TargetApp = targetApp;
        message.Title = title.Trim();
        message.Body = string.IsNullOrWhiteSpace(body) ? null : body.Trim();
        message.DeepLink = NormalizeInternalDeepLink(deepLink);
        message.PublishedAtUtc = nowUtc;
        message.ExpiresAtUtc = expiresAtUtc;

        var distinctUserIds = userIds.Where(x => x != Guid.Empty).Distinct().ToList();
        var existingRecipientIds = await _db.Set<NotificationRecipient>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.NotificationMessageId == message.Id)
            .Select(x => x.UserId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var existingRecipientSet = existingRecipientIds.ToHashSet();

        foreach (var userId in distinctUserIds)
        {
            if (existingRecipientSet.Contains(userId))
            {
                continue;
            }

            _db.Set<NotificationRecipient>().Add(new NotificationRecipient
            {
                NotificationMessageId = message.Id,
                UserId = userId,
                DeliveredAtUtc = nowUtc
            });
        }
    }

    private static string BuildCampaignDeepLink(Campaign campaign)
        => campaign.BusinessId is { } businessId && businessId != Guid.Empty
            ? $"loyan://business/{businessId:D}"
            : "loyan://feed";

    private static string? NormalizeInternalDeepLink(string? deepLink)
    {
        if (string.IsNullOrWhiteSpace(deepLink))
        {
            return null;
        }

        var trimmed = deepLink.Trim();
        return trimmed.StartsWith("loyan://", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : null;
    }
}
