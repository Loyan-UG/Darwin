using System;
using System.Collections.Generic;
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

public sealed class GetMobileDevicesPageHandler
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromDays(30);

    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public GetMobileDevicesPageHandler(IAppDbContext db, IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<(IReadOnlyList<MobileDeviceOpsListItemDto> Items, int Total)> HandleAsync(
        int page = 1,
        int pageSize = 20,
        string? query = null,
        MobilePlatform? platform = null,
        string? state = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var staleCutoffUtc = _clock.UtcNow.Subtract(StaleThreshold);

        var baseQuery =
            from device in _db.Set<UserDevice>().AsNoTracking()
            join user in _db.Set<User>().AsNoTracking() on device.UserId equals user.Id
            where !device.IsDeleted
            select new
            {
                device,
                user,
                MembershipCount = _db.Set<BusinessMember>()
                    .AsNoTracking()
                    .Count(x => !x.IsDeleted && x.IsActive && x.UserId == device.UserId)
            };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            baseQuery = baseQuery.Where(x =>
                x.user.Email.Contains(term) ||
                ((x.user.FirstName ?? string.Empty) + " " + (x.user.LastName ?? string.Empty)).Contains(term) ||
                x.device.DeviceId.Contains(term) ||
                (x.device.AppVersion != null && x.device.AppVersion.Contains(term)) ||
                (x.device.DeviceModel != null && x.device.DeviceModel.Contains(term)));
        }

        if (platform.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.device.Platform == platform.Value);
        }

        if (!string.IsNullOrWhiteSpace(state))
        {
            baseQuery = state switch
            {
                "stale" => baseQuery.Where(x => !x.device.LastSeenAtUtc.HasValue || x.device.LastSeenAtUtc < staleCutoffUtc),
                "missing-push" => baseQuery.Where(x => string.IsNullOrWhiteSpace(x.device.PushToken)),
                "notifications-disabled" => baseQuery.Where(x => !x.device.NotificationsEnabled),
                "business-members" => baseQuery.Where(x => x.MembershipCount > 0),
                _ => baseQuery
            };
        }

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);

        var items = await baseQuery
            .OrderByDescending(x => x.device.LastSeenAtUtc ?? x.device.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MobileDeviceOpsListItemDto
            {
                Id = x.device.Id,
                UserId = x.device.UserId,
                UserEmail = x.user.Email,
                UserDisplayName = string.IsNullOrWhiteSpace(((x.user.FirstName ?? string.Empty) + " " + (x.user.LastName ?? string.Empty)).Trim())
                    ? x.user.Email
                    : ((x.user.FirstName ?? string.Empty) + " " + (x.user.LastName ?? string.Empty)).Trim(),
                DeviceId = x.device.DeviceId,
                Platform = x.device.Platform,
                AppVersion = x.device.AppVersion,
                DeviceModel = x.device.DeviceModel,
                NotificationsEnabled = x.device.NotificationsEnabled,
                HasPushToken = !string.IsNullOrWhiteSpace(x.device.PushToken),
                IsActive = x.device.IsActive,
                LastSeenAtUtc = x.device.LastSeenAtUtc,
                BusinessMembershipCount = x.MembershipCount,
                RowVersion = x.device.RowVersion
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, total);
    }
}
