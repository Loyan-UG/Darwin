using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CRM.DTOs;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CRM.Queries
{
    public sealed class GetOpportunitiesPageHandler
    {
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

            var baseQuery =
                from opportunity in _db.Set<Opportunity>().AsNoTracking()
                join customer in _db.Set<Customer>().AsNoTracking() on opportunity.CustomerId equals customer.Id
                join user in _db.Set<User>().AsNoTracking() on customer.UserId equals (Guid?)user.Id into users
                from user in users.DefaultIfEmpty()
                select new { opportunity, customer, user };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                baseQuery = baseQuery.Where(x =>
                    x.opportunity.Title.Contains(q) ||
                    x.customer.FirstName.Contains(q) ||
                    x.customer.LastName.Contains(q) ||
                    (x.user != null && x.user.Email.Contains(q)) ||
                    (x.user != null && x.user.FirstName != null && x.user.FirstName.Contains(q)) ||
                    (x.user != null && x.user.LastName != null && x.user.LastName.Contains(q)));
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
                    ItemCount = x.opportunity.Items.Count,
                    InteractionCount = x.opportunity.Interactions.Count,
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
                .FirstOrDefaultAsync(x => x.Id == id, ct)
                .ConfigureAwait(false);

            if (opportunity is null)
            {
                return null;
            }

            var customerName = await (
                    from customer in _db.Set<Customer>().AsNoTracking()
                    join user in _db.Set<User>().AsNoTracking() on customer.UserId equals (Guid?)user.Id into users
                    from user in users.DefaultIfEmpty()
                    where customer.Id == opportunity.CustomerId
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
                Title = opportunity.Title,
                EstimatedValueMinor = opportunity.EstimatedValueMinor,
                Stage = opportunity.Stage,
                ExpectedCloseDateUtc = opportunity.ExpectedCloseDateUtc,
                AssignedToUserId = opportunity.AssignedToUserId,
                Items = opportunity.Items
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
