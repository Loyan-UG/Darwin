using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Inventory;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Common.Queries
{
    /// <summary>
    /// Loads business lookup items for admin filters and forms.
    /// </summary>
    public sealed class GetBusinessLookupHandler
    {
        private readonly IAppDbContext _db;

        public GetBusinessLookupHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<List<LookupItemDto>> HandleAsync(CancellationToken ct = default)
        {
            return _db.Set<Business>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new LookupItemDto
                {
                    Id = x.Id,
                    Label = x.Name,
                    SecondaryLabel = x.DefaultCurrency
                })
                .ToListAsync(ct);
        }
    }

    /// <summary>
    /// Loads active user lookup items for admin ownership and CRM assignment fields.
    /// </summary>
    public sealed class GetUserLookupHandler
    {
        private readonly IAppDbContext _db;

        public GetUserLookupHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<List<LookupItemDto>> HandleAsync(CancellationToken ct = default)
        {
            return _db.Set<User>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Email)
                .Select(x => new LookupItemDto
                {
                    Id = x.Id,
                    Label = string.IsNullOrWhiteSpace((x.FirstName ?? string.Empty) + (x.LastName ?? string.Empty))
                        ? x.Email
                        : ((x.FirstName ?? string.Empty) + " " + (x.LastName ?? string.Empty)).Trim(),
                    SecondaryLabel = x.Email
                })
                .ToListAsync(ct);
        }
    }

    /// <summary>
    /// Loads CRM customer lookup items using identity-backed names when available.
    /// </summary>
    public sealed class GetCustomerLookupHandler
    {
        private readonly IAppDbContext _db;

        public GetCustomerLookupHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<List<LookupItemDto>> HandleAsync(CancellationToken ct = default)
        {
            return (
                from customer in _db.Set<Customer>().AsNoTracking()
                join user in _db.Set<User>().AsNoTracking() on customer.UserId equals user.Id into userJoin
                from user in userJoin.DefaultIfEmpty()
                where !customer.IsDeleted
                orderby customer.LastName, customer.FirstName, customer.Email
                select new LookupItemDto
                {
                    Id = customer.Id,
                    Label = customer.UserId.HasValue && user != null
                        ? string.IsNullOrWhiteSpace((user.FirstName ?? string.Empty) + (user.LastName ?? string.Empty))
                            ? user.Email
                            : ((user.FirstName ?? string.Empty) + " " + (user.LastName ?? string.Empty)).Trim()
                        : ((customer.FirstName + " " + customer.LastName).Trim()),
                    SecondaryLabel = customer.UserId.HasValue && user != null ? user.Email : customer.Email
                })
                .ToListAsync(ct);
        }
    }

    /// <summary>
    /// Loads CRM segment lookup items for customer assignment forms.
    /// </summary>
    public sealed class GetCustomerSegmentLookupHandler
    {
        private readonly IAppDbContext _db;

        public GetCustomerSegmentLookupHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<List<LookupItemDto>> HandleAsync(CancellationToken ct = default)
        {
            return _db.Set<CustomerSegment>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Name)
                .Select(x => new LookupItemDto
                {
                    Id = x.Id,
                    Label = x.Name,
                    SecondaryLabel = x.Description
                })
                .ToListAsync(ct);
        }
    }

    /// <summary>
    /// Loads product variant lookup items for inventory and CRM quotation forms.
    /// </summary>
    public sealed class GetProductVariantLookupHandler
    {
        private readonly IAppDbContext _db;

        public GetProductVariantLookupHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<List<LookupItemDto>> HandleAsync(CancellationToken ct = default)
        {
            return _db.Set<ProductVariant>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Sku)
                .Select(x => new LookupItemDto
                {
                    Id = x.Id,
                    Label = x.Sku + " - " + (
                        _db.Set<ProductTranslation>()
                            .Where(t => t.ProductId == x.ProductId && !t.IsDeleted)
                            .OrderByDescending(t => t.Culture == Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCultureDefault)
                            .ThenBy(t => t.Culture)
                            .Select(t => t.Name)
                            .FirstOrDefault() ?? "Unnamed product"),
                    SecondaryLabel = x.Currency
                })
                .ToListAsync(ct);
        }
    }

    /// <summary>
    /// Loads supplier lookup items scoped to a business.
    /// </summary>
    public sealed class GetSupplierLookupHandler
    {
        private readonly IAppDbContext _db;

        public GetSupplierLookupHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<List<LookupItemDto>> HandleAsync(Guid businessId, CancellationToken ct = default)
        {
            return _db.Set<Supplier>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.BusinessId == businessId)
                .OrderBy(x => x.Name)
                .Select(x => new LookupItemDto
                {
                    Id = x.Id,
                    Label = x.Name,
                    SecondaryLabel = x.Email
                })
                .ToListAsync(ct);
        }
    }

    /// <summary>
    /// Loads financial account lookup items scoped to a business.
    /// </summary>
    public sealed class GetFinancialAccountLookupHandler
    {
        private readonly IAppDbContext _db;

        public GetFinancialAccountLookupHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<List<LookupItemDto>> HandleAsync(Guid businessId, CancellationToken ct = default)
        {
            var rows = await _db.Set<FinancialAccount>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.BusinessId == businessId)
                .OrderBy(x => x.Code)
                .ThenBy(x => x.Name)
                .Select(x => new
                {
                    Id = x.Id,
                    x.Code,
                    x.Name,
                    x.Type
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return rows
                .Select(x => new LookupItemDto
                {
                    Id = x.Id,
                    Label = string.IsNullOrWhiteSpace(x.Code) ? x.Name : x.Code + " - " + x.Name,
                    SecondaryLabel = x.Type.ToString()
                })
                .ToList();
        }
    }

    /// <summary>
    /// Loads payment lookup items for invoice linkage and refund selection scenarios.
    /// </summary>
    public sealed class GetPaymentLookupHandler
    {
        private readonly IAppDbContext _db;

        public GetPaymentLookupHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<List<LookupItemDto>> HandleAsync(CancellationToken ct = default)
        {
            var rows = await _db.Set<Payment>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.PaidAtUtc ?? x.CreatedAtUtc)
                .Select(x => new
                {
                    Id = x.Id,
                    x.Provider,
                    x.Currency,
                    x.AmountMinor,
                    x.Status
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return rows
                .Select(x => new LookupItemDto
                {
                    Id = x.Id,
                    Label = x.Provider + " - " + x.Currency + " " + ((decimal)x.AmountMinor / 100M).ToString("0.00", CultureInfo.InvariantCulture),
                    SecondaryLabel = x.Status.ToString()
                })
                .ToList();
        }
    }
}
