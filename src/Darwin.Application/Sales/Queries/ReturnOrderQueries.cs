using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Sales.DTOs;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Sales.Queries;

public sealed class GetReturnOrdersPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;

    public GetReturnOrdersPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<(List<ReturnOrderListItemDto> Items, int Total)> HandleAsync(
        int page,
        int pageSize,
        string? query = null,
        ReturnOrderDocumentFilter filter = ReturnOrderDocumentFilter.All,
        Guid? businessId = null,
        Guid? customerId = null,
        DateTime? createdFromUtc = null,
        DateTime? createdToUtc = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var baseQuery = _db.Set<ReturnOrder>().AsNoTracking().Where(x => !x.IsDeleted);
        var q = query?.Trim();
        if (!string.IsNullOrWhiteSpace(q))
        {
            baseQuery = baseQuery.Where(x => x.ReturnOrderNumber != null && x.ReturnOrderNumber.Contains(q));
        }

        if (businessId.HasValue && businessId.Value != Guid.Empty)
        {
            baseQuery = baseQuery.Where(x => x.BusinessId == businessId.Value);
        }

        if (customerId.HasValue && customerId.Value != Guid.Empty)
        {
            baseQuery = baseQuery.Where(x => x.CustomerId == customerId.Value);
        }

        if (createdFromUtc.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.CreatedAtUtc >= createdFromUtc.Value);
        }

        if (createdToUtc.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.CreatedAtUtc <= createdToUtc.Value);
        }

        baseQuery = filter switch
        {
            ReturnOrderDocumentFilter.Requested => baseQuery.Where(x => x.Status == ReturnOrderStatus.Requested),
            ReturnOrderDocumentFilter.Approved => baseQuery.Where(x => x.Status == ReturnOrderStatus.Approved),
            ReturnOrderDocumentFilter.ReturnShipmentQueued => baseQuery.Where(x => x.Status == ReturnOrderStatus.ReturnShipmentQueued),
            ReturnOrderDocumentFilter.Received => baseQuery.Where(x => x.Status == ReturnOrderStatus.Received),
            ReturnOrderDocumentFilter.Inspected => baseQuery.Where(x => x.Status == ReturnOrderStatus.Inspected),
            ReturnOrderDocumentFilter.RefundReady => baseQuery.Where(x => x.Status == ReturnOrderStatus.RefundReady),
            ReturnOrderDocumentFilter.Refunded => baseQuery.Where(x => x.Status == ReturnOrderStatus.Refunded),
            ReturnOrderDocumentFilter.Closed => baseQuery.Where(x => x.Status == ReturnOrderStatus.Closed),
            ReturnOrderDocumentFilter.Cancelled => baseQuery.Where(x => x.Status == ReturnOrderStatus.Cancelled),
            ReturnOrderDocumentFilter.Open => baseQuery.Where(x => x.Status != ReturnOrderStatus.Closed && x.Status != ReturnOrderStatus.Cancelled && x.Status != ReturnOrderStatus.Rejected),
            _ => baseQuery
        };

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        var items = await baseQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ReturnOrderListItemDto
            {
                Id = x.Id,
                OrderId = x.OrderId,
                ShipmentId = x.ShipmentId,
                InvoiceId = x.InvoiceId,
                ReturnOrderNumber = x.ReturnOrderNumber,
                Status = x.Status,
                BusinessId = x.BusinessId,
                CustomerId = x.CustomerId,
                Currency = x.Currency,
                RequestedQuantity = x.RequestedQuantity,
                ApprovedQuantity = x.ApprovedQuantity,
                ReceivedQuantity = x.ReceivedQuantity,
                AcceptedQuantity = x.AcceptedQuantity,
                RestockQuantity = x.RestockQuantity,
                RefundEligibleGrossMinor = x.RefundEligibleGrossMinor,
                LinkedRefundGrossMinor = x.RefundLinks.Where(link => !link.IsDeleted).Sum(link => link.AmountMinor),
                RemainingRefundGrossMinor = x.RefundEligibleGrossMinor - x.RefundLinks.Where(link => !link.IsDeleted).Sum(link => link.AmountMinor) < 0
                    ? 0
                    : x.RefundEligibleGrossMinor - x.RefundLinks.Where(link => !link.IsDeleted).Sum(link => link.AmountMinor),
                CreatedAtUtc = x.CreatedAtUtc,
                ApprovedAtUtc = x.ApprovedAtUtc,
                ReceivedAtUtc = x.ReceivedAtUtc,
                InspectedAtUtc = x.InspectedAtUtc,
                RefundedAtUtc = x.RefundedAtUtc,
                RowVersion = x.RowVersion
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return (items, total);
    }
}

public sealed class GetReturnOrderDetailHandler
{
    private readonly IAppDbContext _db;

    public GetReturnOrderDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<ReturnOrderDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        return await _db.Set<ReturnOrder>()
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new ReturnOrderDetailDto
            {
                Id = x.Id,
                OrderId = x.OrderId,
                ShipmentId = x.ShipmentId,
                InvoiceId = x.InvoiceId,
                ReturnOrderNumber = x.ReturnOrderNumber,
                Status = x.Status,
                BusinessId = x.BusinessId,
                CustomerId = x.CustomerId,
                Currency = x.Currency,
                RequestedQuantity = x.RequestedQuantity,
                ApprovedQuantity = x.ApprovedQuantity,
                ReceivedQuantity = x.ReceivedQuantity,
                AcceptedQuantity = x.AcceptedQuantity,
                RestockQuantity = x.RestockQuantity,
                RefundEligibleGrossMinor = x.RefundEligibleGrossMinor,
                LinkedRefundGrossMinor = x.RefundLinks.Where(link => !link.IsDeleted).Sum(link => link.AmountMinor),
                RemainingRefundGrossMinor = x.RefundEligibleGrossMinor - x.RefundLinks.Where(link => !link.IsDeleted).Sum(link => link.AmountMinor) < 0
                    ? 0
                    : x.RefundEligibleGrossMinor - x.RefundLinks.Where(link => !link.IsDeleted).Sum(link => link.AmountMinor),
                RequestedGrossMinor = x.RequestedGrossMinor,
                ApprovedGrossMinor = x.ApprovedGrossMinor,
                AcceptedGrossMinor = x.AcceptedGrossMinor,
                CreatedAtUtc = x.CreatedAtUtc,
                ApprovedAtUtc = x.ApprovedAtUtc,
                ReceivedAtUtc = x.ReceivedAtUtc,
                InspectedAtUtc = x.InspectedAtUtc,
                RefundedAtUtc = x.RefundedAtUtc,
                CustomerSnapshotJson = x.CustomerSnapshotJson,
                ShippingAddressJson = x.ShippingAddressJson,
                InternalNotes = x.InternalNotes,
                MetadataJson = x.MetadataJson,
                OrderNumber = _db.Set<Order>().Where(o => o.Id == x.OrderId && !o.IsDeleted).Select(o => o.OrderNumber).FirstOrDefault(),
                RowVersion = x.RowVersion,
                Lines = x.Lines
                    .Where(line => !line.IsDeleted)
                    .OrderBy(line => line.SortOrder)
                    .ThenBy(line => line.CreatedAtUtc)
                    .Select(line => new ReturnOrderLineDetailDto
                    {
                        Id = line.Id,
                        OrderLineId = line.OrderLineId,
                        ShipmentLineId = line.ShipmentLineId,
                        ProductVariantId = line.ProductVariantId,
                        RestockWarehouseId = line.RestockWarehouseId,
                        Name = line.Name,
                        Sku = line.Sku,
                        Description = line.Description,
                        RequestedQuantity = line.RequestedQuantity,
                        ApprovedQuantity = line.ApprovedQuantity,
                        ReceivedQuantity = line.ReceivedQuantity,
                        AcceptedQuantity = line.AcceptedQuantity,
                        RejectedQuantity = line.RejectedQuantity,
                        ScrappedQuantity = line.ScrappedQuantity,
                        RestockQuantity = line.RestockQuantity,
                        UnitPriceNetMinor = line.UnitPriceNetMinor,
                        UnitPriceGrossMinor = line.UnitPriceGrossMinor,
                        TaxRate = line.TaxRate,
                        RequestedGrossMinor = line.RequestedGrossMinor,
                        ApprovedGrossMinor = line.ApprovedGrossMinor,
                        AcceptedGrossMinor = line.AcceptedGrossMinor,
                        Disposition = line.Disposition,
                        SortOrder = line.SortOrder
                    })
                    .ToList(),
                RefundLinks = x.RefundLinks
                    .Where(link => !link.IsDeleted)
                    .OrderBy(link => link.CreatedAtUtc)
                    .Select(link => new ReturnOrderRefundLinkDto
                    {
                        Id = link.Id,
                        RefundId = link.RefundId,
                        AmountMinor = link.AmountMinor,
                        Currency = link.Currency,
                        Notes = link.Notes,
                        CreatedAtUtc = link.CreatedAtUtc
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}
