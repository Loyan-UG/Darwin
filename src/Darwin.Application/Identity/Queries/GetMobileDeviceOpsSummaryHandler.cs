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

        var deviceSummary = await baseDevices
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalActiveDevices = g.Count(),
                StaleDevicesCount = g.Count(x => !x.LastSeenAtUtc.HasValue || x.LastSeenAtUtc < staleCutoffUtc),
                DevicesMissingPushTokenCount = g.Count(x => x.PushToken == null || x.PushToken.Trim() == string.Empty),
                NotificationsDisabledCount = g.Count(x => !x.NotificationsEnabled),
                BusinessMemberDevicesCount = g.Count(x => businessMemberUserIds.Contains(x.UserId)),
                AndroidDevicesCount = g.Count(x => x.Platform == MobilePlatform.Android),
                IosDevicesCount = g.Count(x => x.Platform == MobilePlatform.iOS)
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var recentVersions = await baseDevices
            .Where(x => x.AppVersion != null && x.AppVersion.Trim() != string.Empty)
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
            TotalActiveDevices = deviceSummary?.TotalActiveDevices ?? 0,
            BusinessMemberDevicesCount = deviceSummary?.BusinessMemberDevicesCount ?? 0,
            StaleDevicesCount = deviceSummary?.StaleDevicesCount ?? 0,
            DevicesMissingPushTokenCount = deviceSummary?.DevicesMissingPushTokenCount ?? 0,
            NotificationsDisabledCount = deviceSummary?.NotificationsDisabledCount ?? 0,
            AndroidDevicesCount = deviceSummary?.AndroidDevicesCount ?? 0,
            IosDevicesCount = deviceSummary?.IosDevicesCount ?? 0,
            RecentVersions = recentVersions
        };
    }
}
