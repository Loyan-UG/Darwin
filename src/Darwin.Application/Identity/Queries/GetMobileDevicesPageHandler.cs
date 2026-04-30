using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Common;
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
        Guid? businessId = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var staleCutoffUtc = _clock.UtcNow.Subtract(StaleThreshold);

        var baseQuery =
            from device in _db.Set<UserDevice>().AsNoTracking()
            join user in _db.Set<User>().AsNoTracking() on device.UserId equals user.Id
            where !device.IsDeleted && !user.IsDeleted
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
            var term = QueryLikePattern.Contains(query);
            baseQuery = baseQuery.Where(x =>
                EF.Functions.Like(x.user.Email, term, QueryLikePattern.EscapeCharacter) ||
                EF.Functions.Like(((x.user.FirstName ?? string.Empty) + " " + (x.user.LastName ?? string.Empty)), term, QueryLikePattern.EscapeCharacter) ||
                EF.Functions.Like(x.device.DeviceId, term, QueryLikePattern.EscapeCharacter) ||
                (x.device.AppVersion != null && EF.Functions.Like(x.device.AppVersion, term, QueryLikePattern.EscapeCharacter)) ||
                (x.device.DeviceModel != null && EF.Functions.Like(x.device.DeviceModel, term, QueryLikePattern.EscapeCharacter)));
        }

        if (platform.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.device.Platform == platform.Value);
        }

        if (businessId.HasValue)
        {
            baseQuery = baseQuery.Where(x => _db.Set<BusinessMember>()
                .AsNoTracking()
                .Any(member => !member.IsDeleted &&
                               member.IsActive &&
                               member.BusinessId == businessId.Value &&
                               member.UserId == x.device.UserId));
        }

        if (!string.IsNullOrWhiteSpace(state))
        {
            baseQuery = state switch
            {
                "stale" => baseQuery.Where(x => !x.device.LastSeenAtUtc.HasValue || x.device.LastSeenAtUtc < staleCutoffUtc),
                "missing-push" => baseQuery.Where(x => x.device.PushToken == null || x.device.PushToken.Trim() == string.Empty),
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
                HasPushToken = x.device.PushToken != null && x.device.PushToken.Trim() != string.Empty,
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
