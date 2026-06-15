using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Application.Inventory.DTOs;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Inventory.Queries;

public sealed class GetGoodsReceiptsPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;

    public GetGoodsReceiptsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<(List<GoodsReceiptListItemDto> Items, int Total)> HandleAsync(
        Guid businessId,
        int page,
        int pageSize,
        string? query = null,
        GoodsReceiptQueueFilter filter = GoodsReceiptQueueFilter.All,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        var receiptsQuery =
            from receipt in _db.Set<GoodsReceipt>().AsNoTracking()
            join supplier in _db.Set<Supplier>().AsNoTracking() on receipt.SupplierId equals supplier.Id
            join order in _db.Set<PurchaseOrder>().AsNoTracking() on receipt.PurchaseOrderId equals order.Id
            join warehouse in _db.Set<Warehouse>().AsNoTracking() on receipt.WarehouseId equals warehouse.Id
            where receipt.BusinessId == businessId &&
                  !receipt.IsDeleted &&
                  !supplier.IsDeleted &&
                  !order.IsDeleted &&
                  !warehouse.IsDeleted
            select new { receipt, supplier, order, warehouse };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.ContainsInvariant(query);
            receiptsQuery = receiptsQuery.Where(x =>
                (x.receipt.GoodsReceiptNumber != null && EF.Functions.Like(x.receipt.GoodsReceiptNumber.ToUpper(), term, QueryLikePattern.EscapeCharacter)) ||
                EF.Functions.Like(x.order.OrderNumber.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                EF.Functions.Like(x.supplier.Name.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                EF.Functions.Like(x.warehouse.Name.ToUpper(), term, QueryLikePattern.EscapeCharacter));
        }

        receiptsQuery = filter switch
        {
            GoodsReceiptQueueFilter.Draft => receiptsQuery.Where(x => x.receipt.Status == GoodsReceiptStatus.Draft),
            GoodsReceiptQueueFilter.Received => receiptsQuery.Where(x => x.receipt.Status == GoodsReceiptStatus.Received),
            GoodsReceiptQueueFilter.Inspected => receiptsQuery.Where(x => x.receipt.Status == GoodsReceiptStatus.Inspected),
            GoodsReceiptQueueFilter.Posted => receiptsQuery.Where(x => x.receipt.Status == GoodsReceiptStatus.Posted),
            GoodsReceiptQueueFilter.Cancelled => receiptsQuery.Where(x => x.receipt.Status == GoodsReceiptStatus.Cancelled),
            _ => receiptsQuery
        };

        var total = await receiptsQuery.CountAsync(ct).ConfigureAwait(false);

        var rows = await receiptsQuery
            .OrderByDescending(x => x.receipt.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                Receipt = x.receipt,
                SupplierName = x.supplier.Name,
                PurchaseOrderNumber = x.order.OrderNumber,
                WarehouseName = x.warehouse.Name,
                LineCount = x.receipt.Lines.Count(line => !line.IsDeleted),
                ReceivedQuantity = x.receipt.Lines.Where(line => !line.IsDeleted).Sum(line => line.ReceivedQuantity),
                AcceptedQuantity = x.receipt.Lines.Where(line => !line.IsDeleted).Sum(line => line.AcceptedQuantity)
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (rows.Select(x => new GoodsReceiptListItemDto
        {
            Id = x.Receipt.Id,
            BusinessId = x.Receipt.BusinessId,
            SupplierId = x.Receipt.SupplierId,
            PurchaseOrderId = x.Receipt.PurchaseOrderId,
            WarehouseId = x.Receipt.WarehouseId,
            SupplierName = x.SupplierName,
            PurchaseOrderNumber = x.PurchaseOrderNumber,
            WarehouseName = x.WarehouseName,
            GoodsReceiptNumber = x.Receipt.GoodsReceiptNumber,
            Status = x.Receipt.Status.ToString(),
            CreatedAtUtc = x.Receipt.CreatedAtUtc,
            ReceivedAtUtc = x.Receipt.ReceivedAtUtc,
            PostedAtUtc = x.Receipt.PostedAtUtc,
            LineCount = x.LineCount,
            ReceivedQuantity = x.ReceivedQuantity,
            AcceptedQuantity = x.AcceptedQuantity,
            RowVersion = x.Receipt.RowVersion
        }).ToList(), total);
    }

    public async Task<GoodsReceiptOpsSummaryDto> GetSummaryAsync(Guid businessId, CancellationToken ct = default)
    {
        return await _db.Set<GoodsReceipt>()
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId && !x.IsDeleted)
            .GroupBy(_ => 1)
            .Select(g => new GoodsReceiptOpsSummaryDto
            {
                TotalCount = g.Count(),
                DraftCount = g.Count(x => x.Status == GoodsReceiptStatus.Draft),
                ReceivedCount = g.Count(x => x.Status == GoodsReceiptStatus.Received),
                InspectedCount = g.Count(x => x.Status == GoodsReceiptStatus.Inspected),
                PostedCount = g.Count(x => x.Status == GoodsReceiptStatus.Posted),
                CancelledCount = g.Count(x => x.Status == GoodsReceiptStatus.Cancelled)
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false) ?? new GoodsReceiptOpsSummaryDto();
    }
}

public sealed class GetGoodsReceiptDetailHandler
{
    private readonly IAppDbContext _db;

    public GetGoodsReceiptDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<GoodsReceiptDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var receipt = await _db.Set<GoodsReceipt>()
            .AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (receipt is null)
        {
            return null;
        }

        var supplierName = await _db.Set<Supplier>()
            .AsNoTracking()
            .Where(x => x.Id == receipt.SupplierId && !x.IsDeleted)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false) ?? string.Empty;
        var purchaseOrderNumber = await _db.Set<PurchaseOrder>()
            .AsNoTracking()
            .Where(x => x.Id == receipt.PurchaseOrderId && !x.IsDeleted)
            .Select(x => x.OrderNumber)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false) ?? string.Empty;
        var warehouseName = await _db.Set<Warehouse>()
            .AsNoTracking()
            .Where(x => x.Id == receipt.WarehouseId && !x.IsDeleted)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false) ?? string.Empty;

        return new GoodsReceiptDetailDto
        {
            Id = receipt.Id,
            RowVersion = receipt.RowVersion,
            BusinessId = receipt.BusinessId,
            SupplierId = receipt.SupplierId,
            PurchaseOrderId = receipt.PurchaseOrderId,
            WarehouseId = receipt.WarehouseId,
            SupplierName = supplierName,
            PurchaseOrderNumber = purchaseOrderNumber,
            WarehouseName = warehouseName,
            GoodsReceiptNumber = receipt.GoodsReceiptNumber,
            Status = receipt.Status.ToString(),
            ReceivedAtUtc = receipt.ReceivedAtUtc,
            InspectedAtUtc = receipt.InspectedAtUtc,
            PostedAtUtc = receipt.PostedAtUtc,
            CancelledAtUtc = receipt.CancelledAtUtc,
            InternalNotes = receipt.InternalNotes,
            Lines = receipt.Lines
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .Select(x => new GoodsReceiptLineDto
                {
                    Id = x.Id,
                    PurchaseOrderLineId = x.PurchaseOrderLineId,
                    ProductVariantId = x.ProductVariantId,
                    SupplierSku = x.SupplierSku,
                    Description = x.Description,
                    OrderedQuantity = x.OrderedQuantity,
                    PreviouslyReceivedQuantity = x.PreviouslyReceivedQuantity,
                    ReceivedQuantity = x.ReceivedQuantity,
                    AcceptedQuantity = x.AcceptedQuantity,
                    RejectedQuantity = x.RejectedQuantity,
                    DamagedQuantity = x.DamagedQuantity,
                    UnitCostMinor = x.UnitCostMinor,
                    TotalCostMinor = x.TotalCostMinor,
                    SortOrder = x.SortOrder
                })
                .ToList()
        };
    }
}
