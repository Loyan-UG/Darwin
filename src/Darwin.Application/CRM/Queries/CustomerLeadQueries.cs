using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CRM.DTOs;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CRM.Queries
{
    public sealed class GetCustomersPageHandler
    {
        private readonly IAppDbContext _db;

        public GetCustomersPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<CustomerListItemDto> Items, int Total)> HandleAsync(
            int page,
            int pageSize,
            string? query = null,
            CustomerQueueFilter filter = CustomerQueueFilter.All,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var baseQuery =
                from customer in _db.Set<Customer>().AsNoTracking()
                join user in _db.Set<User>().AsNoTracking() on customer.UserId equals (Guid?)user.Id into users
                from user in users.DefaultIfEmpty()
                select new { customer, user };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                baseQuery = baseQuery.Where(x =>
                    x.customer.FirstName.Contains(q) ||
                    x.customer.LastName.Contains(q) ||
                    x.customer.Email.Contains(q) ||
                    (x.customer.CompanyName != null && x.customer.CompanyName.Contains(q)) ||
                    (x.user != null && x.user.Email.Contains(q)) ||
                    (x.user != null && x.user.FirstName != null && x.user.FirstName.Contains(q)) ||
                    (x.user != null && x.user.LastName != null && x.user.LastName.Contains(q)));
            }

            baseQuery = filter switch
            {
                CustomerQueueFilter.LinkedUser => baseQuery.Where(x => x.customer.UserId.HasValue),
                CustomerQueueFilter.NeedsSegmentation => baseQuery.Where(x => x.customer.CustomerSegments.Count == 0),
                CustomerQueueFilter.HasOpportunities => baseQuery.Where(x => x.customer.Opportunities.Count > 0),
                _ => baseQuery
            };

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await baseQuery
                .OrderByDescending(x => x.customer.ModifiedAtUtc ?? x.customer.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CustomerListItemDto
                {
                    Id = x.customer.Id,
                    UserId = x.customer.UserId,
                    DisplayName = x.customer.UserId.HasValue && x.user != null
                        ? (((x.user.FirstName ?? string.Empty) + " " + (x.user.LastName ?? string.Empty)).Trim())
                        : ((x.customer.FirstName + " " + x.customer.LastName).Trim()),
                    Email = x.customer.UserId.HasValue && x.user != null ? x.user.Email : x.customer.Email,
                    Phone = x.customer.UserId.HasValue && x.user != null ? x.user.PhoneE164 : x.customer.Phone,
                    CompanyName = x.customer.CompanyName,
                    SegmentCount = x.customer.CustomerSegments.Count,
                    OpportunityCount = x.customer.Opportunities.Count,
                    CreatedAtUtc = x.customer.CreatedAtUtc,
                    ModifiedAtUtc = x.customer.ModifiedAtUtc,
                    RowVersion = x.customer.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }

    public sealed class GetCustomerForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetCustomerForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<CustomerEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var customer = await _db.Set<Customer>()
                .AsNoTracking()
                .Include(x => x.Addresses)
                .FirstOrDefaultAsync(x => x.Id == id, ct)
                .ConfigureAwait(false);

            if (customer is null)
            {
                return null;
            }

            User? user = null;
            IdentityAddressSummaryDto? billingAddress = null;
            IdentityAddressSummaryDto? shippingAddress = null;

            if (customer.UserId.HasValue)
            {
                user = await _db.Set<User>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == customer.UserId.Value, ct)
                    .ConfigureAwait(false);

                if (user?.DefaultBillingAddressId is Guid billingAddressId)
                {
                    billingAddress = await _db.Set<Address>()
                        .AsNoTracking()
                        .Where(x => x.Id == billingAddressId)
                        .Select(x => new IdentityAddressSummaryDto
                        {
                            Id = x.Id,
                            FullName = x.FullName,
                            Street1 = x.Street1,
                            Street2 = x.Street2,
                            PostalCode = x.PostalCode,
                            City = x.City,
                            State = x.State,
                            CountryCode = x.CountryCode,
                            PhoneE164 = x.PhoneE164,
                            IsDefaultBilling = x.IsDefaultBilling,
                            IsDefaultShipping = x.IsDefaultShipping
                        })
                        .FirstOrDefaultAsync(ct)
                        .ConfigureAwait(false);
                }

                if (user?.DefaultShippingAddressId is Guid shippingAddressId)
                {
                    shippingAddress = await _db.Set<Address>()
                        .AsNoTracking()
                        .Where(x => x.Id == shippingAddressId)
                        .Select(x => new IdentityAddressSummaryDto
                        {
                            Id = x.Id,
                            FullName = x.FullName,
                            Street1 = x.Street1,
                            Street2 = x.Street2,
                            PostalCode = x.PostalCode,
                            City = x.City,
                            State = x.State,
                            CountryCode = x.CountryCode,
                            PhoneE164 = x.PhoneE164,
                            IsDefaultBilling = x.IsDefaultBilling,
                            IsDefaultShipping = x.IsDefaultShipping
                        })
                        .FirstOrDefaultAsync(ct)
                        .ConfigureAwait(false);
                }
            }

            return new CustomerEditDto
            {
                Id = customer.Id,
                RowVersion = customer.RowVersion,
                UserId = customer.UserId,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Notes = customer.Notes,
                EffectiveFirstName = user?.FirstName ?? customer.FirstName,
                EffectiveLastName = user?.LastName ?? customer.LastName,
                EffectiveEmail = user?.Email ?? customer.Email,
                EffectivePhone = user?.PhoneE164 ?? customer.Phone,
                DefaultBillingAddress = billingAddress,
                DefaultShippingAddress = shippingAddress,
                Addresses = customer.Addresses
                    .OrderByDescending(x => x.IsDefaultBilling)
                    .ThenByDescending(x => x.IsDefaultShipping)
                    .ThenBy(x => x.City)
                    .Select(x => new CustomerAddressDto
                    {
                        Id = x.Id,
                        AddressId = x.AddressId,
                        Line1 = x.Line1,
                        Line2 = x.Line2,
                        City = x.City,
                        State = x.State,
                        PostalCode = x.PostalCode,
                        Country = x.Country,
                        IsDefaultBilling = x.IsDefaultBilling,
                        IsDefaultShipping = x.IsDefaultShipping
                    })
                    .ToList()
            };
        }
    }

    public sealed class GetLeadsPageHandler
    {
        private readonly IAppDbContext _db;

        public GetLeadsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<LeadListItemDto> Items, int Total)> HandleAsync(
            int page,
            int pageSize,
            string? query = null,
            LeadQueueFilter filter = LeadQueueFilter.All,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var baseQuery = _db.Set<Lead>().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                baseQuery = baseQuery.Where(x =>
                    x.FirstName.Contains(q) ||
                    x.LastName.Contains(q) ||
                    x.Email.Contains(q) ||
                    x.Phone.Contains(q) ||
                    (x.CompanyName != null && x.CompanyName.Contains(q)));
            }

            baseQuery = filter switch
            {
                LeadQueueFilter.Qualified => baseQuery.Where(x => x.Status == LeadStatus.Qualified),
                LeadQueueFilter.Unassigned => baseQuery.Where(x => !x.AssignedToUserId.HasValue),
                LeadQueueFilter.Unconverted => baseQuery.Where(x => !x.CustomerId.HasValue),
                _ => baseQuery
            };

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await baseQuery
                .OrderByDescending(x => x.ModifiedAtUtc ?? x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new LeadListItemDto
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    CompanyName = x.CompanyName,
                    Email = x.Email,
                    Phone = x.Phone,
                    Status = x.Status,
                    AssignedToUserId = x.AssignedToUserId,
                    CustomerId = x.CustomerId,
                    InteractionCount = x.Interactions.Count,
                    CreatedAtUtc = x.CreatedAtUtc,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }

    public sealed class GetLeadForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetLeadForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<LeadEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return _db.Set<Lead>()
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new LeadEditDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    CompanyName = x.CompanyName,
                    Email = x.Email,
                    Phone = x.Phone,
                    Source = x.Source,
                    Notes = x.Notes,
                    Status = x.Status,
                    AssignedToUserId = x.AssignedToUserId,
                    CustomerId = x.CustomerId
                })
                .FirstOrDefaultAsync(ct);
        }
    }

    public sealed class GetCrmSummaryHandler
    {
        private readonly IAppDbContext _db;

        public GetCrmSummaryHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<CrmSummaryDto> HandleAsync(CancellationToken ct = default)
        {
            var customerCount = await _db.Set<Customer>().AsNoTracking().CountAsync(ct).ConfigureAwait(false);
            var leadCount = await _db.Set<Lead>().AsNoTracking().CountAsync(ct).ConfigureAwait(false);
            var qualifiedLeadCount = await _db.Set<Lead>().AsNoTracking().CountAsync(x => x.Status == LeadStatus.Qualified, ct).ConfigureAwait(false);
            var openOpportunityCount = await _db.Set<Opportunity>().AsNoTracking()
                .CountAsync(x => x.Stage != OpportunityStage.ClosedWon && x.Stage != OpportunityStage.ClosedLost, ct)
                .ConfigureAwait(false);
            var openPipelineMinor = await _db.Set<Opportunity>().AsNoTracking()
                .Where(x => x.Stage != OpportunityStage.ClosedWon && x.Stage != OpportunityStage.ClosedLost)
                .SumAsync(x => (long?)x.EstimatedValueMinor, ct)
                .ConfigureAwait(false) ?? 0L;
            var segmentCount = await _db.Set<CustomerSegment>().AsNoTracking().CountAsync(ct).ConfigureAwait(false);
            var recentInteractionCount = await _db.Set<Interaction>().AsNoTracking()
                .CountAsync(x => x.CreatedAtUtc >= DateTime.UtcNow.AddDays(-7), ct)
                .ConfigureAwait(false);

            return new CrmSummaryDto
            {
                CustomerCount = customerCount,
                LeadCount = leadCount,
                QualifiedLeadCount = qualifiedLeadCount,
                OpenOpportunityCount = openOpportunityCount,
                OpenPipelineMinor = openPipelineMinor,
                SegmentCount = segmentCount,
                RecentInteractionCount = recentInteractionCount
            };
        }
    }
}
