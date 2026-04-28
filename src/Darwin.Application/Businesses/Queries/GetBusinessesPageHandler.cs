using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Returns a paged list of businesses for Admin grids.
    /// Pure query with AsNoTracking; respects global soft-delete filter.
    /// </summary>
    public sealed class GetBusinessesPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;
        public GetBusinessesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<BusinessListItemDto> Items, int Total)> HandleAsync(
            int page,
            int pageSize,
            string? query = null,
            BusinessOperationalStatus? operationalStatus = null,
            bool attentionOnly = false,
            BusinessReadinessQueueFilter? readinessFilter = null,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var baseQuery = _db.Set<Business>().AsNoTracking().Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                baseQuery = baseQuery.Where(x =>
                    x.Name.Contains(q) ||
                    (x.LegalName != null && x.LegalName.Contains(q)));
            }

            if (operationalStatus.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.OperationalStatus == operationalStatus.Value);
            }

            if (attentionOnly)
            {
                baseQuery = baseQuery.Where(x =>
                    x.OperationalStatus != BusinessOperationalStatus.Approved ||
                    !x.IsActive ||
                    !_db.Set<BusinessMember>().Any(m => m.BusinessId == x.Id && !m.IsDeleted && m.IsActive && m.Role == BusinessMemberRole.Owner) ||
                    !_db.Set<BusinessLocation>().Any(l => l.BusinessId == x.Id && !l.IsDeleted && l.IsPrimary) ||
                    string.IsNullOrWhiteSpace(x.ContactEmail) ||
                    string.IsNullOrWhiteSpace(x.LegalName));
            }

            if (readinessFilter == BusinessReadinessQueueFilter.MissingOwner)
            {
                baseQuery = baseQuery.Where(x =>
                    !_db.Set<BusinessMember>().Any(m => m.BusinessId == x.Id && !m.IsDeleted && m.IsActive && m.Role == BusinessMemberRole.Owner));
            }
            else if (readinessFilter == BusinessReadinessQueueFilter.MissingPrimaryLocation)
            {
                baseQuery = baseQuery.Where(x =>
                    !_db.Set<BusinessLocation>().Any(l => l.BusinessId == x.Id && !l.IsDeleted && l.IsPrimary));
            }
            else if (readinessFilter == BusinessReadinessQueueFilter.MissingContactEmail)
            {
                baseQuery = baseQuery.Where(x => string.IsNullOrWhiteSpace(x.ContactEmail));
            }
            else if (readinessFilter == BusinessReadinessQueueFilter.MissingLegalName)
            {
                baseQuery = baseQuery.Where(x => string.IsNullOrWhiteSpace(x.LegalName));
            }
            else if (readinessFilter == BusinessReadinessQueueFilter.PendingInvites)
            {
                baseQuery = baseQuery.Where(x =>
                    _db.Set<BusinessInvitation>().Any(i => i.BusinessId == x.Id && !i.IsDeleted && i.Status == BusinessInvitationStatus.Pending));
            }
            else if (readinessFilter == BusinessReadinessQueueFilter.ApprovedInactive)
            {
                baseQuery = baseQuery.Where(x =>
                    x.OperationalStatus == BusinessOperationalStatus.Approved &&
                    !x.IsActive);
            }

            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderByDescending(x => x.ModifiedAtUtc ?? x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new BusinessListItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    LegalName = x.LegalName,
                    Category = x.Category,
                    IsActive = x.IsActive,
                    OperationalStatus = x.OperationalStatus,
                    MemberCount = _db.Set<BusinessMember>().Count(m => m.BusinessId == x.Id && !m.IsDeleted),
                    ActiveOwnerCount = _db.Set<BusinessMember>().Count(m => m.BusinessId == x.Id && !m.IsDeleted && m.IsActive && m.Role == BusinessMemberRole.Owner),
                    LocationCount = _db.Set<BusinessLocation>().Count(l => l.BusinessId == x.Id && !l.IsDeleted),
                    PrimaryLocationCount = _db.Set<BusinessLocation>().Count(l => l.BusinessId == x.Id && !l.IsDeleted && l.IsPrimary),
                    InvitationCount = _db.Set<BusinessInvitation>().Count(i => i.BusinessId == x.Id && !i.IsDeleted && i.Status == BusinessInvitationStatus.Pending),
                    HasContactEmailConfigured = !string.IsNullOrWhiteSpace(x.ContactEmail),
                    HasLegalNameConfigured = !string.IsNullOrWhiteSpace(x.LegalName),
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }
}
