using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Queries;

public sealed class GetMobileDeviceOpsSummaryHandler
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromDays(30);

    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public GetMobileDeviceOpsSummaryHandler(IAppDbContext db, IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<MobileDeviceOpsSummaryDto> HandleAsync(CancellationToken ct = default)
    {
        var staleCutoffUtc = _clock.UtcNow.Subtract(StaleThreshold);

        var baseDevices = _db.Set<UserDevice>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive && _db.Set<User>().Any(u => u.Id == x.UserId && !u.IsDeleted));

        var businessMemberUserIds = _db.Set<BusinessMember>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .Select(x => x.UserId)
            .Distinct();

        var totalActiveDevices = await baseDevices.CountAsync(ct).ConfigureAwait(false);
        var staleDevicesCount = await baseDevices.CountAsync(x => !x.LastSeenAtUtc.HasValue || x.LastSeenAtUtc < staleCutoffUtc, ct).ConfigureAwait(false);
        var devicesMissingPushTokenCount = await baseDevices.CountAsync(x => string.IsNullOrWhiteSpace(x.PushToken), ct).ConfigureAwait(false);
        var notificationsDisabledCount = await baseDevices.CountAsync(x => !x.NotificationsEnabled, ct).ConfigureAwait(false);
        var businessMemberDevicesCount = await baseDevices.CountAsync(x => businessMemberUserIds.Contains(x.UserId), ct).ConfigureAwait(false);
        var androidDevicesCount = await baseDevices.CountAsync(x => x.Platform == MobilePlatform.Android, ct).ConfigureAwait(false);
        var iosDevicesCount = await baseDevices.CountAsync(x => x.Platform == MobilePlatform.iOS, ct).ConfigureAwait(false);

        var recentVersions = await baseDevices
            .Where(x => !string.IsNullOrWhiteSpace(x.AppVersion))
            .GroupBy(x => new { x.Platform, x.AppVersion })
            .Select(x => new MobileAppVersionSnapshotDto
            {
                Platform = x.Key.Platform,
                AppVersion = x.Key.AppVersion!,
                DeviceCount = x.Count(),
                LastSeenAtUtc = x.Max(y => y.LastSeenAtUtc)
            })
            .OrderByDescending(x => x.LastSeenAtUtc)
            .ThenByDescending(x => x.DeviceCount)
            .Take(8)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new MobileDeviceOpsSummaryDto
        {
            TotalActiveDevices = totalActiveDevices,
            BusinessMemberDevicesCount = businessMemberDevicesCount,
            StaleDevicesCount = staleDevicesCount,
            DevicesMissingPushTokenCount = devicesMissingPushTokenCount,
            NotificationsDisabledCount = notificationsDisabledCount,
            AndroidDevicesCount = androidDevicesCount,
            IosDevicesCount = iosDevicesCount,
            RecentVersions = recentVersions
        };
    }
}
