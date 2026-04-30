using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Common;
using Darwin.Application.CRM.DTOs;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CRM.Queries
{
    public sealed class GetCustomersPageHandler
    {
        private const int MaxPageSize = 200;

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
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var baseQuery =
                from customer in _db.Set<Customer>().AsNoTracking()
                join user in _db.Set<User>().AsNoTracking() on customer.UserId equals (Guid?)user.Id into users
                from user in users.DefaultIfEmpty()
                where !customer.IsDeleted &&
                      (user == null || !user.IsDeleted)
                select new { customer, user };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = QueryLikePattern.Contains(query);
                baseQuery = baseQuery.Where(x =>
                    EF.Functions.Like(x.customer.FirstName, q, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.customer.LastName, q, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.customer.Email, q, QueryLikePattern.EscapeCharacter) ||
                    (x.customer.CompanyName != null && EF.Functions.Like(x.customer.CompanyName, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.user != null && EF.Functions.Like(x.user.Email, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.user != null && x.user.FirstName != null && EF.Functions.Like(x.user.FirstName, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.user != null && x.user.LastName != null && EF.Functions.Like(x.user.LastName, q, QueryLikePattern.EscapeCharacter)));
            }

            baseQuery = filter switch
            {
                CustomerQueueFilter.LinkedUser => baseQuery.Where(x => x.customer.UserId.HasValue),
                CustomerQueueFilter.NeedsSegmentation => baseQuery.Where(x => x.customer.CustomerSegments.Count(segment => !segment.IsDeleted) == 0),
                CustomerQueueFilter.HasOpportunities => baseQuery.Where(x => x.customer.Opportunities.Count(opportunity => !opportunity.IsDeleted) > 0),
                CustomerQueueFilter.Business => baseQuery.Where(x => x.customer.TaxProfileType == CustomerTaxProfileType.Business),
                CustomerQueueFilter.MissingVatId => baseQuery.Where(x =>
                    x.customer.TaxProfileType == CustomerTaxProfileType.Business &&
                    (x.customer.VatId == null || x.customer.VatId.Trim() == string.Empty)),
                CustomerQueueFilter.UsesPlatformLocaleFallback => baseQuery.Where(x =>
                    !x.customer.UserId.HasValue ||
                    x.user == null ||
                    x.user.Locale == null ||
                    x.user.Locale.Trim() == string.Empty),
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
                    TaxProfileType = x.customer.TaxProfileType,
                    VatId = x.customer.VatId,
                    Locale = x.customer.UserId.HasValue && x.user != null ? x.user.Locale : null,
                    UsesPlatformLocaleFallback = !x.customer.UserId.HasValue ||
                        x.user == null ||
                        x.user.Locale == null ||
                        x.user.Locale.Trim() == string.Empty,
                    SegmentCount = x.customer.CustomerSegments.Count(segment => !segment.IsDeleted),
                    OpportunityCount = x.customer.Opportunities.Count(opportunity => !opportunity.IsDeleted),
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
                TaxProfileType = customer.TaxProfileType,
                VatId = customer.VatId,
                Notes = customer.Notes,
                EffectiveFirstName = user?.FirstName ?? customer.FirstName,
                EffectiveLastName = user?.LastName ?? customer.LastName,
                EffectiveEmail = user?.Email ?? customer.Email,
                EffectivePhone = user?.PhoneE164 ?? customer.Phone,
                EffectiveLocale = user?.Locale,
                UsesPlatformLocaleFallback = !customer.UserId.HasValue || user == null || string.IsNullOrWhiteSpace(user.Locale),
                SegmentCount = await _db.Set<CustomerSegmentMembership>()
                    .AsNoTracking()
                    .CountAsync(x => x.CustomerId == customer.Id, ct)
                    .ConfigureAwait(false),
                OpportunityCount = await _db.Set<Opportunity>()
                    .AsNoTracking()
                    .CountAsync(x => x.CustomerId == customer.Id, ct)
                    .ConfigureAwait(false),
                InteractionCount = await _db.Set<Interaction>()
                    .AsNoTracking()
                    .CountAsync(x => x.CustomerId == customer.Id, ct)
                    .ConfigureAwait(false),
                ConsentCount = await _db.Set<Consent>()
                    .AsNoTracking()
                    .CountAsync(x => x.CustomerId == customer.Id, ct)
                    .ConfigureAwait(false),
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
        private const int MaxPageSize = 200;

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
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var baseQuery =
                from lead in _db.Set<Lead>().AsNoTracking()
                join assignedUser in _db.Set<User>().AsNoTracking() on lead.AssignedToUserId equals (Guid?)assignedUser.Id into assignedUsers
                from assignedUser in assignedUsers.DefaultIfEmpty()
                where !lead.IsDeleted &&
                      (assignedUser == null || !assignedUser.IsDeleted)
                select new { lead, assignedUser };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = QueryLikePattern.Contains(query);
                baseQuery = baseQuery.Where(x =>
                    EF.Functions.Like(x.lead.FirstName, q, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.lead.LastName, q, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.lead.Email, q, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.lead.Phone, q, QueryLikePattern.EscapeCharacter) ||
                    (x.lead.CompanyName != null && EF.Functions.Like(x.lead.CompanyName, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.assignedUser != null && EF.Functions.Like(x.assignedUser.Email, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.assignedUser != null && x.assignedUser.FirstName != null && EF.Functions.Like(x.assignedUser.FirstName, q, QueryLikePattern.EscapeCharacter)) ||
                    (x.assignedUser != null && x.assignedUser.LastName != null && EF.Functions.Like(x.assignedUser.LastName, q, QueryLikePattern.EscapeCharacter)));
            }

            baseQuery = filter switch
            {
                LeadQueueFilter.Qualified => baseQuery.Where(x => x.lead.Status == LeadStatus.Qualified),
                LeadQueueFilter.Unassigned => baseQuery.Where(x => !x.lead.AssignedToUserId.HasValue),
                LeadQueueFilter.Unconverted => baseQuery.Where(x => !x.lead.CustomerId.HasValue),
                _ => baseQuery
            };

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await baseQuery
                .OrderByDescending(x => x.lead.ModifiedAtUtc ?? x.lead.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new LeadListItemDto
                {
                    Id = x.lead.Id,
                    FirstName = x.lead.FirstName,
                    LastName = x.lead.LastName,
                    CompanyName = x.lead.CompanyName,
                    Email = x.lead.Email,
                    Phone = x.lead.Phone,
                    Status = x.lead.Status,
                    AssignedToUserId = x.lead.AssignedToUserId,
                    AssignedToUserDisplayName = x.assignedUser == null
                        ? null
                        : (((x.assignedUser.FirstName ?? string.Empty) + " " + (x.assignedUser.LastName ?? string.Empty)).Trim()),
                    CustomerId = x.lead.CustomerId,
                    InteractionCount = x.lead.Interactions.Count(interaction => !interaction.IsDeleted),
                    CreatedAtUtc = x.lead.CreatedAtUtc,
                    ModifiedAtUtc = x.lead.ModifiedAtUtc,
                    RowVersion = x.lead.RowVersion
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
                    AssignedToUserDisplayName = x.AssignedToUserId.HasValue
                        ? _db.Set<User>()
                            .Where(u => u.Id == x.AssignedToUserId.Value)
                            .Select(u => ((u.FirstName ?? string.Empty) + " " + (u.LastName ?? string.Empty)).Trim())
                            .FirstOrDefault()
                        : null,
                    CustomerId = x.CustomerId,
                    CustomerDisplayName = x.CustomerId.HasValue
                        ? _db.Set<Customer>()
                            .Where(c => c.Id == x.CustomerId.Value)
                            .Select(c => c.UserId.HasValue
                                ? _db.Set<User>()
                                    .Where(u => u.Id == c.UserId.Value)
                                    .Select(u => ((u.FirstName ?? string.Empty) + " " + (u.LastName ?? string.Empty)).Trim())
                                    .FirstOrDefault() ?? ((c.FirstName + " " + c.LastName).Trim())
                                : ((c.FirstName + " " + c.LastName).Trim()))
                            .FirstOrDefault()
                        : null,
                    InteractionCount = _db.Set<Interaction>().Count(i => i.LeadId == x.Id)
                })
                .FirstOrDefaultAsync(ct);
        }
    }

    public sealed class GetCrmSummaryHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;

        public GetCrmSummaryHandler(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task<CrmSummaryDto> HandleAsync(CancellationToken ct = default)
        {
            var recentInteractionCutoffUtc = _clock.UtcNow.AddDays(-7);
            var customerCount = await _db.Set<Customer>().AsNoTracking().CountAsync(ct).ConfigureAwait(false);
            var leadSummary = await _db.Set<Lead>()
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    LeadCount = g.Count(),
                    QualifiedLeadCount = g.Count(x => x.Status == LeadStatus.Qualified)
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
            var opportunitySummary = await _db.Set<Opportunity>()
                .AsNoTracking()
                .Where(x => x.Stage != OpportunityStage.ClosedWon && x.Stage != OpportunityStage.ClosedLost)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    OpenOpportunityCount = g.Count(),
                    OpenPipelineMinor = g.Sum(x => (long?)x.EstimatedValueMinor) ?? 0L
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
            var segmentCount = await _db.Set<CustomerSegment>().AsNoTracking().CountAsync(ct).ConfigureAwait(false);
            var recentInteractionCount = await _db.Set<Interaction>().AsNoTracking()
                .CountAsync(x => x.CreatedAtUtc >= recentInteractionCutoffUtc, ct)
                .ConfigureAwait(false);

            return new CrmSummaryDto
            {
                CustomerCount = customerCount,
                LeadCount = leadSummary?.LeadCount ?? 0,
                QualifiedLeadCount = leadSummary?.QualifiedLeadCount ?? 0,
                OpenOpportunityCount = opportunitySummary?.OpenOpportunityCount ?? 0,
                OpenPipelineMinor = opportunitySummary?.OpenPipelineMinor ?? 0L,
                SegmentCount = segmentCount,
                RecentInteractionCount = recentInteractionCount
            };
        }
    }
}
