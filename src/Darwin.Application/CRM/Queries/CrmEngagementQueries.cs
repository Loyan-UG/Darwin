using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Application.CRM.DTOs;
using Darwin.Domain.Entities.CRM;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CRM.Queries
{
    /// <summary>
    /// Returns CRM interactions filtered by customer.
    /// </summary>
    public sealed class GetCustomerInteractionsPageHandler
    {
        private readonly IAppDbContext _db;

        public GetCustomerInteractionsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<(List<InteractionListItemDto> Items, int Total)> HandleAsync(Guid customerId, int page, int pageSize, CancellationToken ct = default) =>
            CrmInteractionQueryHelper.GetInteractionsAsync(_db.Set<Interaction>().AsNoTracking().Where(x => x.CustomerId == customerId), page, pageSize, ct);
    }

    /// <summary>
    /// Returns CRM interactions filtered by lead.
    /// </summary>
    public sealed class GetLeadInteractionsPageHandler
    {
        private readonly IAppDbContext _db;

        public GetLeadInteractionsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<(List<InteractionListItemDto> Items, int Total)> HandleAsync(Guid leadId, int page, int pageSize, CancellationToken ct = default) =>
            CrmInteractionQueryHelper.GetInteractionsAsync(_db.Set<Interaction>().AsNoTracking().Where(x => x.LeadId == leadId), page, pageSize, ct);
    }

    /// <summary>
    /// Returns CRM interactions filtered by opportunity.
    /// </summary>
    public sealed class GetOpportunityInteractionsPageHandler
    {
        private readonly IAppDbContext _db;

        public GetOpportunityInteractionsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<(List<InteractionListItemDto> Items, int Total)> HandleAsync(Guid opportunityId, int page, int pageSize, CancellationToken ct = default) =>
            CrmInteractionQueryHelper.GetInteractionsAsync(_db.Set<Interaction>().AsNoTracking().Where(x => x.OpportunityId == opportunityId), page, pageSize, ct);
    }

    /// <summary>
    /// Returns consent history rows for a CRM customer.
    /// </summary>
    public sealed class GetCustomerConsentsPageHandler
    {
        private readonly IAppDbContext _db;

        public GetCustomerConsentsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<ConsentListItemDto> Items, int Total)> HandleAsync(Guid customerId, int page, int pageSize, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > CrmInteractionQueryHelper.MaxPageSize) pageSize = CrmInteractionQueryHelper.MaxPageSize;

            var baseQuery = _db.Set<Consent>()
                .AsNoTracking()
                .Where(x => x.CustomerId == customerId && !x.IsDeleted);

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
            var items = await baseQuery
                .OrderByDescending(x => x.GrantedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ConsentListItemDto
                {
                    Id = x.Id,
                    CustomerId = x.CustomerId,
                    Type = x.Type,
                    Granted = x.Granted,
                    GrantedAtUtc = x.GrantedAtUtc,
                    RevokedAtUtc = x.RevokedAtUtc,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }

    /// <summary>
    /// Returns CRM segment definitions for admin list screens.
    /// </summary>
    public sealed class GetCustomerSegmentsPageHandler
    {
        private readonly IAppDbContext _db;

        public GetCustomerSegmentsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<CustomerSegmentListItemDto> Items, int Total)> HandleAsync(int page, int pageSize, string? query = null, CustomerSegmentQueueFilter filter = CustomerSegmentQueueFilter.All, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > CrmInteractionQueryHelper.MaxPageSize) pageSize = CrmInteractionQueryHelper.MaxPageSize;

            var baseQuery = _db.Set<CustomerSegment>().AsNoTracking().Where(x => !x.IsDeleted);
            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = QueryLikePattern.Contains(query);
                baseQuery = baseQuery.Where(x =>
                    EF.Functions.Like(x.Name, term, QueryLikePattern.EscapeCharacter) ||
                    (x.Description != null && EF.Functions.Like(x.Description, term, QueryLikePattern.EscapeCharacter)));
            }

            baseQuery = filter switch
            {
                CustomerSegmentQueueFilter.Empty => baseQuery.Where(x => x.Memberships.Count(membership => !membership.IsDeleted) == 0),
                CustomerSegmentQueueFilter.InUse => baseQuery.Where(x => x.Memberships.Count(membership => !membership.IsDeleted) > 0),
                CustomerSegmentQueueFilter.MissingDescription => baseQuery.Where(x => x.Description == null || x.Description == string.Empty),
                _ => baseQuery
            };

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
            var items = await baseQuery
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CustomerSegmentListItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    MemberCount = x.Memberships.Count(membership => !membership.IsDeleted),
                    HasDescription = x.Description != null && x.Description != string.Empty,
                    CreatedAtUtc = x.CreatedAtUtc,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }

        public async Task<CustomerSegmentOpsSummaryDto> GetSummaryAsync(CancellationToken ct = default)
        {
            var baseQuery = _db.Set<CustomerSegment>().AsNoTracking().Where(x => !x.IsDeleted);

            return new CustomerSegmentOpsSummaryDto
            {
                TotalCount = await baseQuery.CountAsync(ct).ConfigureAwait(false),
                EmptyCount = await baseQuery.CountAsync(x => x.Memberships.Count(membership => !membership.IsDeleted) == 0, ct).ConfigureAwait(false),
                InUseCount = await baseQuery.CountAsync(x => x.Memberships.Count(membership => !membership.IsDeleted) > 0, ct).ConfigureAwait(false),
                MissingDescriptionCount = await baseQuery.CountAsync(x => x.Description == null || x.Description == string.Empty, ct).ConfigureAwait(false)
            };
        }
    }

    /// <summary>
    /// Returns a CRM segment definition for editing.
    /// </summary>
    public sealed class GetCustomerSegmentForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetCustomerSegmentForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<CustomerSegmentEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return _db.Set<CustomerSegment>()
                .AsNoTracking()
                .Where(x => x.Id == id && !x.IsDeleted)
                .Select(x => new CustomerSegmentEditDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    Name = x.Name,
                    Description = x.Description
                })
                .FirstOrDefaultAsync(ct);
        }
    }

    /// <summary>
    /// Returns segment memberships for a given customer.
    /// </summary>
    public sealed class GetCustomerSegmentMembershipsHandler
    {
        private readonly IAppDbContext _db;

        public GetCustomerSegmentMembershipsHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<List<CustomerSegmentMembershipListItemDto>> HandleAsync(Guid customerId, CancellationToken ct = default)
        {
            return _db.Set<CustomerSegmentMembership>()
                .AsNoTracking()
                .Where(x => x.CustomerId == customerId && !x.IsDeleted)
                .Join(
                    _db.Set<CustomerSegment>().AsNoTracking().Where(segment => !segment.IsDeleted),
                    membership => membership.CustomerSegmentId,
                    segment => segment.Id,
                    (membership, segment) => new CustomerSegmentMembershipListItemDto
                    {
                        MembershipId = membership.Id,
                        SegmentId = segment.Id,
                        Name = segment.Name,
                        Description = segment.Description
                    })
                .OrderBy(x => x.Name)
                .ToListAsync(ct);
        }
    }

    internal static class CrmInteractionQueryHelper
    {
        public const int MaxPageSize = 200;

        public static async Task<(List<InteractionListItemDto> Items, int Total)> GetInteractionsAsync(
            IQueryable<Interaction> baseQuery,
            int page,
            int pageSize,
            CancellationToken ct)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            baseQuery = baseQuery.Where(x => !x.IsDeleted);

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
            var items = await baseQuery
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new InteractionListItemDto
                {
                    Id = x.Id,
                    CustomerId = x.CustomerId,
                    LeadId = x.LeadId,
                    OpportunityId = x.OpportunityId,
                    Type = x.Type,
                    Channel = x.Channel,
                    Subject = x.Subject,
                    Content = x.Content,
                    UserId = x.UserId,
                    CreatedAtUtc = x.CreatedAtUtc,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }
}
