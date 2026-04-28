using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CRM.DTOs;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CRM.Queries
{
    public sealed class GetOpportunitiesPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;

        public GetOpportunitiesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<OpportunityListItemDto> Items, int Total)> HandleAsync(
            int page,
            int pageSize,
            string? query = null,
            OpportunityQueueFilter filter = OpportunityQueueFilter.All,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var baseQuery =
                from opportunity in _db.Set<Opportunity>().AsNoTracking()
                join customer in _db.Set<Customer>().AsNoTracking() on opportunity.CustomerId equals customer.Id
                join user in _db.Set<User>().AsNoTracking() on customer.UserId equals (Guid?)user.Id into users
                from user in users.DefaultIfEmpty()
                join assignedUser in _db.Set<User>().AsNoTracking() on opportunity.AssignedToUserId equals (Guid?)assignedUser.Id into assignedUsers
                from assignedUser in assignedUsers.DefaultIfEmpty()
                where !opportunity.IsDeleted &&
                      !customer.IsDeleted &&
                      (user == null || !user.IsDeleted) &&
                      (assignedUser == null || !assignedUser.IsDeleted)
                select new { opportunity, customer, user, assignedUser };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim().ToLowerInvariant();
                baseQuery = baseQuery.Where(x =>
                    x.opportunity.Title.ToLower().Contains(q) ||
                    x.customer.FirstName.ToLower().Contains(q) ||
                    x.customer.LastName.ToLower().Contains(q) ||
                    (x.user != null && x.user.Email.ToLower().Contains(q)) ||
                    (x.user != null && x.user.FirstName != null && x.user.FirstName.ToLower().Contains(q)) ||
                    (x.user != null && x.user.LastName != null && x.user.LastName.ToLower().Contains(q)));
            }

            var closingSoonThreshold = DateTime.UtcNow.Date.AddDays(14);

            baseQuery = filter switch
            {
                OpportunityQueueFilter.Open => baseQuery.Where(x =>
                    x.opportunity.Stage != Domain.Enums.OpportunityStage.ClosedWon &&
                    x.opportunity.Stage != Domain.Enums.OpportunityStage.ClosedLost),
                OpportunityQueueFilter.ClosingSoon => baseQuery.Where(x =>
                    x.opportunity.Stage != Domain.Enums.OpportunityStage.ClosedWon &&
                    x.opportunity.Stage != Domain.Enums.OpportunityStage.ClosedLost &&
                    x.opportunity.ExpectedCloseDateUtc.HasValue &&
                    x.opportunity.ExpectedCloseDateUtc.Value <= closingSoonThreshold),
                OpportunityQueueFilter.HighValue => baseQuery.Where(x =>
                    x.opportunity.EstimatedValueMinor >= 100000),
                _ => baseQuery
            };

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await baseQuery
                .OrderByDescending(x => x.opportunity.ModifiedAtUtc ?? x.opportunity.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new OpportunityListItemDto
                {
                    Id = x.opportunity.Id,
                    CustomerId = x.opportunity.CustomerId,
                    CustomerDisplayName = x.customer.UserId.HasValue && x.user != null
                        ? (((x.user.FirstName ?? string.Empty) + " " + (x.user.LastName ?? string.Empty)).Trim())
                        : ((x.customer.FirstName + " " + x.customer.LastName).Trim()),
                    Title = x.opportunity.Title,
                    EstimatedValueMinor = x.opportunity.EstimatedValueMinor,
                    Stage = x.opportunity.Stage,
                    ExpectedCloseDateUtc = x.opportunity.ExpectedCloseDateUtc,
                    AssignedToUserId = x.opportunity.AssignedToUserId,
                    AssignedToUserDisplayName = x.assignedUser == null
                        ? null
                        : (((x.assignedUser.FirstName ?? string.Empty) + " " + (x.assignedUser.LastName ?? string.Empty)).Trim()),
                    ItemCount = x.opportunity.Items.Count(item => !item.IsDeleted),
                    InteractionCount = x.opportunity.Interactions.Count(interaction => !interaction.IsDeleted),
                    CreatedAtUtc = x.opportunity.CreatedAtUtc,
                    ModifiedAtUtc = x.opportunity.ModifiedAtUtc,
                    RowVersion = x.opportunity.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }

    public sealed class GetOpportunityForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetOpportunityForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<OpportunityEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var opportunity = await _db.Set<Opportunity>()
                .AsNoTracking()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (opportunity is null)
            {
                return null;
            }

            var customerName = await (
                    from customer in _db.Set<Customer>().AsNoTracking()
                    join user in _db.Set<User>().AsNoTracking() on customer.UserId equals (Guid?)user.Id into users
                    from user in users.DefaultIfEmpty()
                    where customer.Id == opportunity.CustomerId &&
                          !customer.IsDeleted &&
                          (user == null || !user.IsDeleted)
                    select customer.UserId.HasValue && user != null
                        ? (((user.FirstName ?? string.Empty) + " " + (user.LastName ?? string.Empty)).Trim())
                        : ((customer.FirstName + " " + customer.LastName).Trim()))
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            return new OpportunityEditDto
            {
                Id = opportunity.Id,
                RowVersion = opportunity.RowVersion,
                CustomerId = opportunity.CustomerId,
                CustomerDisplayName = customerName ?? string.Empty,
                AssignedToUserDisplayName = opportunity.AssignedToUserId.HasValue
                    ? await _db.Set<User>().AsNoTracking()
                        .Where(x => x.Id == opportunity.AssignedToUserId.Value)
                        .Select(x => ((x.FirstName ?? string.Empty) + " " + (x.LastName ?? string.Empty)).Trim())
                        .FirstOrDefaultAsync(ct)
                        .ConfigureAwait(false)
                    : null,
                Title = opportunity.Title,
                EstimatedValueMinor = opportunity.EstimatedValueMinor,
                Stage = opportunity.Stage,
                ExpectedCloseDateUtc = opportunity.ExpectedCloseDateUtc,
                AssignedToUserId = opportunity.AssignedToUserId,
                InteractionCount = await _db.Set<Interaction>().AsNoTracking()
                    .CountAsync(x => x.OpportunityId == opportunity.Id && !x.IsDeleted, ct)
                    .ConfigureAwait(false),
                Items = opportunity.Items
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.CreatedAtUtc)
                    .Select(x => new OpportunityItemDto
                    {
                        Id = x.Id,
                        ProductVariantId = x.ProductVariantId,
                        Quantity = x.Quantity,
                        UnitPriceMinor = x.UnitPriceMinor
                    })
                    .ToList()
            };
        }
    }
}
