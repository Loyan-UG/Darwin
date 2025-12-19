using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common.DTOs;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Loyalty;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Loyalty.Queries
{
    /// <summary>
    /// Returns a paged list of businesses where the current user has an existing <see cref="LoyaltyAccount"/>.
    /// This is designed for consumer/mobile "My places" screens and intentionally joins:
    /// <list type="bullet">
    /// <item><see cref="Business"/> for basic card data</item>
    /// <item>Primary <see cref="BusinessLocation"/> for city/coordinate</item>
    /// <item><see cref="BusinessMedia"/> (primary) for logo/image URL</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para>
    /// This query is DB-agnostic and does not require spatial extensions.
    /// It relies on standard joins and subqueries only.
    /// </para>
    /// <para>
    /// It returns business ids (public) and does not expose any internal scan-session identifiers.
    /// </para>
    /// </remarks>
    public sealed class GetMyLoyaltyBusinessesHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;

        /// <summary>
        /// Initializes a new instance of the handler.
        /// </summary>
        public GetMyLoyaltyBusinessesHandler(IAppDbContext db, ICurrentUserService currentUser)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        }

        /// <summary>
        /// Handles the query.
        /// </summary>
        /// <param name="request">Paging/filter request.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Paged items and total count.</returns>
        public async Task<(List<MyLoyaltyBusinessListItemDto> Items, int Total)> HandleAsync(
            MyLoyaltyBusinessListRequestDto request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var userId = _currentUser.GetCurrentUserId();
            if (userId == Guid.Empty)
                throw new InvalidOperationException("Current user id is not available.");

            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 50 : request.PageSize;

            // Base: loyalty accounts for current user.
            var accountsQuery = _db.Set<LoyaltyAccount>()
                .AsNoTracking()
                .Where(a => a.UserId == userId);

            // Businesses joined for card fields.
            var businessesQuery = _db.Set<Business>()
                .AsNoTracking();

            if (!request.IncludeInactiveBusinesses)
            {
                businessesQuery = businessesQuery.Where(b => b.IsActive);
            }

            // Primary location is optional.
            var primaryLocationsQuery = _db.Set<BusinessLocation>()
                .AsNoTracking()
                .Where(l => l.IsPrimary);

            // Compose.
            var composed =
                from a in accountsQuery
                join b in businessesQuery
                    on a.BusinessId equals b.Id
                join l in primaryLocationsQuery
                    on b.Id equals l.BusinessId into lg
                from l in lg.DefaultIfEmpty()
                select new MyLoyaltyBusinessListItemDto
                {
                    BusinessId = b.Id,
                    BusinessName = b.Name,
                    Category = b.Category,
                    IsBusinessActive = b.IsActive,
                    City = l != null ? l.City : null,
                    Coordinate = l != null && l.Coordinate != null
                        ? new GeoCoordinateDto
                        {
                            Latitude = l.Coordinate.Latitude,
                            Longitude = l.Coordinate.Longitude,
                            AltitudeMeters = l.Coordinate.AltitudeMeters
                        }
                        : null,
                    PrimaryImageUrl = _db.Set<BusinessMedia>()
                        .AsNoTracking()
                        .Where(m => m.BusinessId == b.Id && m.IsPrimary)
                        .OrderBy(m => m.SortOrder)
                        .Select(m => m.Url)
                        .FirstOrDefault(),
                    AccountStatus = a.Status,
                    PointsBalance = a.PointsBalance,
                    LifetimePoints = a.LifetimePoints,
                    LastAccrualAtUtc = a.LastAccrualAtUtc
                };

            var total = await composed.CountAsync(ct).ConfigureAwait(false);

            // Default ordering: most recently active first, then name.
            var items = await composed
                .OrderByDescending(x => x.LastAccrualAtUtc.HasValue)
                .ThenByDescending(x => x.LastAccrualAtUtc)
                .ThenBy(x => x.BusinessName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            // Ensure non-null business names (defensive; should never be null due to domain constraints).
            foreach (var item in items)
            {
                item.BusinessName = item.BusinessName ?? string.Empty;
            }

            return (items, total);
        }
    }
}
