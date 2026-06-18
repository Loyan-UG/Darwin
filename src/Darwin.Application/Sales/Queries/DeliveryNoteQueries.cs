using Darwin.Application.Sales.DTOs;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Darwin.Application.Abstractions.Persistence;

namespace Darwin.Application.Sales.Queries;

public sealed class GetDeliveryNotesPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;

    public GetDeliveryNotesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<(List<DeliveryNoteListItemDto> Items, int Total)> HandleAsync(
        int page,
        int pageSize,
        string? query = null,
        DeliveryNoteDocumentFilter filter = DeliveryNoteDocumentFilter.All,
        Guid? businessId = null,
        Guid? customerId = null,
        DateTime? issuedFromUtc = null,
        DateTime? issuedToUtc = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var baseQuery = _db.Set<DeliveryNote>().AsNoTracking().Where(x => !x.IsDeleted);
        var q = query?.Trim();
        if (!string.IsNullOrWhiteSpace(q))
        {
            baseQuery = baseQuery.Where(x =>
                (x.DeliveryNoteNumber != null && x.DeliveryNoteNumber.Contains(q)) ||
                (x.TrackingNumber != null && x.TrackingNumber.Contains(q)) ||
                (x.Carrier != null && x.Carrier.Contains(q)));
        }

        if (businessId.HasValue && businessId.Value != Guid.Empty)
        {
            baseQuery = baseQuery.Where(x => x.BusinessId == businessId.Value);
        }

        if (customerId.HasValue && customerId.Value != Guid.Empty)
        {
            baseQuery = baseQuery.Where(x => x.CustomerId == customerId.Value);
        }

        if (issuedFromUtc.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.IssuedAtUtc >= issuedFromUtc.Value);
        }

        if (issuedToUtc.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.IssuedAtUtc <= issuedToUtc.Value);
        }

        baseQuery = filter switch
        {
            DeliveryNoteDocumentFilter.Draft => baseQuery.Where(x => x.Status == DeliveryNoteStatus.Draft),
            DeliveryNoteDocumentFilter.Prepared => baseQuery.Where(x => x.Status == DeliveryNoteStatus.Prepared),
            DeliveryNoteDocumentFilter.Issued => baseQuery.Where(x => x.Status == DeliveryNoteStatus.Issued),
            DeliveryNoteDocumentFilter.Shipped => baseQuery.Where(x => x.Status == DeliveryNoteStatus.Shipped),
            DeliveryNoteDocumentFilter.Delivered => baseQuery.Where(x => x.Status == DeliveryNoteStatus.Delivered),
            DeliveryNoteDocumentFilter.Cancelled => baseQuery.Where(x => x.Status == DeliveryNoteStatus.Cancelled),
            DeliveryNoteDocumentFilter.Open => baseQuery.Where(x => x.Status != DeliveryNoteStatus.Delivered && x.Status != DeliveryNoteStatus.Cancelled),
            _ => baseQuery
        };

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        var items = await baseQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DeliveryNoteListItemDto
            {
                Id = x.Id,
                OrderId = x.OrderId,
                ShipmentId = x.ShipmentId,
                DeliveryNoteNumber = x.DeliveryNoteNumber,
                Status = x.Status,
                BusinessId = x.BusinessId,
                CustomerId = x.CustomerId,
                Currency = x.Currency,
                Carrier = x.Carrier,
                TrackingNumber = x.TrackingNumber,
                TotalQuantity = x.TotalQuantity,
                TotalGrossMinor = x.TotalGrossMinor,
                IssuedAtUtc = x.IssuedAtUtc,
                CreatedAtUtc = x.CreatedAtUtc,
                RowVersion = x.RowVersion
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, total);
    }
}

public sealed class GetDeliveryNoteDetailHandler
{
    private readonly IAppDbContext _db;

    public GetDeliveryNoteDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<DeliveryNoteDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        return await _db.Set<DeliveryNote>()
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new DeliveryNoteDetailDto
            {
                Id = x.Id,
                OrderId = x.OrderId,
                ShipmentId = x.ShipmentId,
                DeliveryNoteNumber = x.DeliveryNoteNumber,
                Status = x.Status,
                BusinessId = x.BusinessId,
                CustomerId = x.CustomerId,
                Currency = x.Currency,
                Carrier = x.Carrier,
                Service = x.Service,
                TrackingNumber = x.TrackingNumber,
                ProviderShipmentReference = x.ProviderShipmentReference,
                ShippingAddressJson = x.ShippingAddressJson,
                TotalQuantity = x.TotalQuantity,
                TotalNetMinor = x.TotalNetMinor,
                TotalTaxMinor = x.TotalTaxMinor,
                TotalGrossMinor = x.TotalGrossMinor,
                PreparedByUserId = x.PreparedByUserId,
                IssuedByUserId = x.IssuedByUserId,
                ShippedByUserId = x.ShippedByUserId,
                DeliveredByUserId = x.DeliveredByUserId,
                CancelledByUserId = x.CancelledByUserId,
                PreparedAtUtc = x.PreparedAtUtc,
                IssuedAtUtc = x.IssuedAtUtc,
                ShippedAtUtc = x.ShippedAtUtc,
                DeliveredAtUtc = x.DeliveredAtUtc,
                CancelledAtUtc = x.CancelledAtUtc,
                CreatedAtUtc = x.CreatedAtUtc,
                InternalNotes = x.InternalNotes,
                MetadataJson = x.MetadataJson,
                OrderNumber = _db.Set<Order>().Where(o => o.Id == x.OrderId && !o.IsDeleted).Select(o => o.OrderNumber).FirstOrDefault(),
                RowVersion = x.RowVersion,
                Lines = x.Lines
                    .Where(line => !line.IsDeleted)
                    .OrderBy(line => line.SortOrder)
                    .ThenBy(line => line.CreatedAtUtc)
                    .Select(line => new DeliveryNoteLineDetailDto
                    {
                        Id = line.Id,
                        OrderLineId = line.OrderLineId,
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
    }
}
