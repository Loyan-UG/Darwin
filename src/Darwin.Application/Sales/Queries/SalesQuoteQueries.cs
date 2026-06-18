using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Sales.DTOs;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Sales.Queries;

public sealed class GetSalesQuotesPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;

    public GetSalesQuotesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<(List<SalesQuoteListItemDto> Items, int Total)> HandleAsync(
        int page,
        int pageSize,
        string? query = null,
        SalesQuoteDocumentFilter filter = SalesQuoteDocumentFilter.All,
        Guid? businessId = null,
        Guid? customerId = null,
        Guid? opportunityId = null,
        DateTime? validUntilFromUtc = null,
        DateTime? validUntilToUtc = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var baseQuery = _db.Set<SalesQuote>().AsNoTracking().Where(x => !x.IsDeleted);
        var q = query?.Trim();
        if (!string.IsNullOrWhiteSpace(q))
        {
            baseQuery = baseQuery.Where(x =>
                x.Title.Contains(q) ||
                (x.QuoteNumber != null && x.QuoteNumber.Contains(q)));
        }

        if (businessId.HasValue && businessId.Value != Guid.Empty)
        {
            baseQuery = baseQuery.Where(x => x.BusinessId == businessId.Value);
        }

        if (customerId.HasValue && customerId.Value != Guid.Empty)
        {
            baseQuery = baseQuery.Where(x => x.CustomerId == customerId.Value);
        }

        if (opportunityId.HasValue && opportunityId.Value != Guid.Empty)
        {
            baseQuery = baseQuery.Where(x => x.OpportunityId == opportunityId.Value);
        }

        if (validUntilFromUtc.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.ValidUntilUtc >= validUntilFromUtc.Value);
        }

        if (validUntilToUtc.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.ValidUntilUtc <= validUntilToUtc.Value);
        }

        var nowUtc = DateTime.UtcNow;
        baseQuery = filter switch
        {
            SalesQuoteDocumentFilter.Draft => baseQuery.Where(x => x.Status == SalesQuoteStatus.Draft),
            SalesQuoteDocumentFilter.Sent => baseQuery.Where(x => x.Status == SalesQuoteStatus.Sent),
            SalesQuoteDocumentFilter.Accepted => baseQuery.Where(x => x.Status == SalesQuoteStatus.Accepted),
            SalesQuoteDocumentFilter.ExpiringSoon => baseQuery.Where(x => x.Status == SalesQuoteStatus.Sent && x.ValidUntilUtc.HasValue && x.ValidUntilUtc <= nowUtc.AddDays(14)),
            SalesQuoteDocumentFilter.Converted => baseQuery.Where(x => x.Status == SalesQuoteStatus.Converted),
            SalesQuoteDocumentFilter.Closed => baseQuery.Where(x => x.Status == SalesQuoteStatus.Accepted || x.Status == SalesQuoteStatus.Rejected || x.Status == SalesQuoteStatus.Expired || x.Status == SalesQuoteStatus.Converted),
            _ => baseQuery
        };

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        var items = await baseQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SalesQuoteListItemDto
            {
                Id = x.Id,
                QuoteNumber = x.QuoteNumber,
                Title = x.Title,
                Status = x.Status,
                BusinessId = x.BusinessId,
                CustomerId = x.CustomerId,
                OpportunityId = x.OpportunityId,
                ConvertedOrderId = x.ConvertedOrderId,
                Currency = x.Currency,
                TotalNetMinor = x.TotalNetMinor,
                TotalTaxMinor = x.TotalTaxMinor,
                TotalGrossMinor = x.TotalGrossMinor,
                ValidUntilUtc = x.ValidUntilUtc,
                SentAtUtc = x.SentAtUtc,
                CreatedAtUtc = x.CreatedAtUtc,
                LineCount = x.Lines.Count(line => !line.IsDeleted),
                RowVersion = x.RowVersion
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, total);
    }
}

public sealed class GetSalesQuoteDetailHandler
{
    private readonly IAppDbContext _db;

    public GetSalesQuoteDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<SalesQuoteDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        var quote = await _db.Set<SalesQuote>()
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new SalesQuoteDetailDto
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                CustomerId = x.CustomerId,
                OpportunityId = x.OpportunityId,
                ConvertedOrderId = x.ConvertedOrderId,
                ConvertedOrderNumber = x.ConvertedOrderId.HasValue
                    ? _db.Set<Order>().Where(o => o.Id == x.ConvertedOrderId.Value && !o.IsDeleted).Select(o => o.OrderNumber).FirstOrDefault()
                    : null,
                QuoteNumber = x.QuoteNumber,
                Title = x.Title,
                Status = x.Status,
                Currency = x.Currency,
                ValidUntilUtc = x.ValidUntilUtc,
                OwnerUserId = x.OwnerUserId,
                PreparedByUserId = x.PreparedByUserId,
                SentByUserId = x.SentByUserId,
                AcceptedByUserId = x.AcceptedByUserId,
                RejectedByUserId = x.RejectedByUserId,
                ConvertedByUserId = x.ConvertedByUserId,
                SentAtUtc = x.SentAtUtc,
                AcceptedAtUtc = x.AcceptedAtUtc,
                RejectedAtUtc = x.RejectedAtUtc,
                ExpiredAtUtc = x.ExpiredAtUtc,
                ConvertedAtUtc = x.ConvertedAtUtc,
                TotalNetMinor = x.TotalNetMinor,
                TotalTaxMinor = x.TotalTaxMinor,
                TotalGrossMinor = x.TotalGrossMinor,
                CustomerSnapshotJson = x.CustomerSnapshotJson,
                BillingAddressJson = x.BillingAddressJson,
                ShippingAddressJson = x.ShippingAddressJson,
                InternalNotes = x.InternalNotes,
                CreatedAtUtc = x.CreatedAtUtc,
                ModifiedAtUtc = x.ModifiedAtUtc,
                RowVersion = x.RowVersion,
                Lines = x.Lines
                    .Where(line => !line.IsDeleted)
                    .OrderBy(line => line.SortOrder)
                    .ThenBy(line => line.CreatedAtUtc)
                    .Select(line => new SalesQuoteLineDetailDto
                    {
                        Id = line.Id,
                        ProductVariantId = line.ProductVariantId,
                        Name = line.Name,
                        Sku = line.Sku,
                        Description = line.Description,
                        Quantity = line.Quantity,
                        UnitPriceNetMinor = line.UnitPriceNetMinor,
                        UnitPriceGrossMinor = line.UnitPriceGrossMinor,
                        TaxRate = line.TaxRate,
                        TotalNetMinor = line.TotalNetMinor,
                        TotalTaxMinor = line.TotalTaxMinor,
                        TotalGrossMinor = line.TotalGrossMinor,
                        SortOrder = line.SortOrder
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return quote;
    }
}
