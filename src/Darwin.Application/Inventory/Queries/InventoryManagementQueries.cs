using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Inventory.DTOs;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Inventory.Queries
{
    public sealed class GetWarehouseLookupHandler
    {
        private readonly IAppDbContext _db;

        public GetWarehouseLookupHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<List<WarehouseLookupItemDto>> HandleAsync(CancellationToken ct = default)
        {
            return _db.Set<Warehouse>()
                .AsNoTracking()
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.Name)
                .Select(x => new WarehouseLookupItemDto
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    Location = x.Location,
                    IsDefault = x.IsDefault
                })
                .ToListAsync(ct);
        }
    }

    public sealed class GetWarehousesPageHandler
    {
        private readonly IAppDbContext _db;

        public GetWarehousesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<WarehouseListItemDto> Items, int Total)> HandleAsync(Guid businessId, int page, int pageSize, string? query = null, WarehouseQueueFilter filter = WarehouseQueueFilter.All, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var warehousesQuery = _db.Set<Warehouse>().AsNoTracking().Where(x => x.BusinessId == businessId);
            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                warehousesQuery = warehousesQuery.Where(x =>
                    x.Name.Contains(term) ||
                    (x.Description != null && x.Description.Contains(term)) ||
                    (x.Location != null && x.Location.Contains(term)));
            }

            warehousesQuery = filter switch
            {
                WarehouseQueueFilter.Default => warehousesQuery.Where(x => x.IsDefault),
                WarehouseQueueFilter.NoStockLevels => warehousesQuery.Where(x => !x.StockLevels.Any()),
                _ => warehousesQuery
            };

            var total = await warehousesQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await warehousesQuery
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new WarehouseListItemDto
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    Description = x.Description,
                    Location = x.Location,
                    IsDefault = x.IsDefault,
                    StockLevelCount = x.StockLevels.Count,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }

    public sealed class GetWarehouseForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetWarehouseForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<WarehouseEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return _db.Set<Warehouse>()
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new WarehouseEditDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    Description = x.Description,
                    Location = x.Location,
                    IsDefault = x.IsDefault
                })
                .FirstOrDefaultAsync(ct);
        }
    }

    public sealed class GetSuppliersPageHandler
    {
        private readonly IAppDbContext _db;

        public GetSuppliersPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<SupplierListItemDto> Items, int Total)> HandleAsync(Guid businessId, int page, int pageSize, string? query = null, SupplierQueueFilter filter = SupplierQueueFilter.All, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var suppliersQuery = _db.Set<Supplier>().AsNoTracking().Where(x => x.BusinessId == businessId);
            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                suppliersQuery = suppliersQuery.Where(x =>
                    x.Name.Contains(term) ||
                    x.Email.Contains(term) ||
                    x.Phone.Contains(term) ||
                    (x.Address != null && x.Address.Contains(term)));
            }

            suppliersQuery = filter switch
            {
                SupplierQueueFilter.MissingAddress => suppliersQuery.Where(x => x.Address == null || x.Address == string.Empty),
                SupplierQueueFilter.HasPurchaseOrders => suppliersQuery.Where(x => x.PurchaseOrders.Any()),
                _ => suppliersQuery
            };

            var total = await suppliersQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await suppliersQuery
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SupplierListItemDto
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    Email = x.Email,
                    Phone = x.Phone,
                    Address = x.Address,
                    PurchaseOrderCount = x.PurchaseOrders.Count,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }

    public sealed class GetSupplierForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetSupplierForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<SupplierEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return _db.Set<Supplier>()
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new SupplierEditDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    Email = x.Email,
                    Phone = x.Phone,
                    Address = x.Address,
                    Notes = x.Notes
                })
                .FirstOrDefaultAsync(ct);
        }
    }

    public sealed class GetStockLevelsPageHandler
    {
        private readonly IAppDbContext _db;

        public GetStockLevelsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<StockLevelListItemDto> Items, int Total)> HandleAsync(Guid warehouseId, int page, int pageSize, string? query = null, StockLevelQueueFilter filter = StockLevelQueueFilter.All, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var stockLevelsQuery =
                from stockLevel in _db.Set<StockLevel>().AsNoTracking()
                join warehouse in _db.Set<Warehouse>().AsNoTracking() on stockLevel.WarehouseId equals warehouse.Id
                join variant in _db.Set<ProductVariant>().AsNoTracking() on stockLevel.ProductVariantId equals variant.Id
                where stockLevel.WarehouseId == warehouseId
                select new { stockLevel, warehouse, variant };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                stockLevelsQuery = stockLevelsQuery.Where(x =>
                    x.variant.Sku.Contains(term) ||
                    x.warehouse.Name.Contains(term));
            }

            stockLevelsQuery = filter switch
            {
                StockLevelQueueFilter.LowStock => stockLevelsQuery.Where(x => x.stockLevel.AvailableQuantity <= x.stockLevel.ReorderPoint),
                StockLevelQueueFilter.Reserved => stockLevelsQuery.Where(x => x.stockLevel.ReservedQuantity > 0),
                StockLevelQueueFilter.InTransit => stockLevelsQuery.Where(x => x.stockLevel.InTransitQuantity > 0),
                _ => stockLevelsQuery
            };

            var total = await stockLevelsQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await stockLevelsQuery
                .OrderBy(x => x.variant.Sku)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new StockLevelListItemDto
                {
                    Id = x.stockLevel.Id,
                    WarehouseId = x.stockLevel.WarehouseId,
                    ProductVariantId = x.stockLevel.ProductVariantId,
                    WarehouseName = x.warehouse.Name,
                    VariantSku = x.variant.Sku,
                    AvailableQuantity = x.stockLevel.AvailableQuantity,
                    ReservedQuantity = x.stockLevel.ReservedQuantity,
                    ReorderPoint = x.stockLevel.ReorderPoint,
                    ReorderQuantity = x.stockLevel.ReorderQuantity,
                    InTransitQuantity = x.stockLevel.InTransitQuantity,
                    RowVersion = x.stockLevel.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }

    public sealed class GetStockLevelForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetStockLevelForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<StockLevelEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return _db.Set<StockLevel>()
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new StockLevelEditDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    WarehouseId = x.WarehouseId,
                    ProductVariantId = x.ProductVariantId,
                    AvailableQuantity = x.AvailableQuantity,
                    ReservedQuantity = x.ReservedQuantity,
                    ReorderPoint = x.ReorderPoint,
                    ReorderQuantity = x.ReorderQuantity,
                    InTransitQuantity = x.InTransitQuantity
                })
                .FirstOrDefaultAsync(ct);
        }
    }

    public sealed class GetStockTransfersPageHandler
    {
        private readonly IAppDbContext _db;

        public GetStockTransfersPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<StockTransferListItemDto> Items, int Total)> HandleAsync(Guid warehouseId, int page, int pageSize, string? query = null, StockTransferQueueFilter filter = StockTransferQueueFilter.All, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var stockTransfersQuery =
                from transfer in _db.Set<StockTransfer>().AsNoTracking()
                join fromWarehouse in _db.Set<Warehouse>().AsNoTracking() on transfer.FromWarehouseId equals fromWarehouse.Id
                join toWarehouse in _db.Set<Warehouse>().AsNoTracking() on transfer.ToWarehouseId equals toWarehouse.Id
                where transfer.FromWarehouseId == warehouseId || transfer.ToWarehouseId == warehouseId
                select new { transfer, fromWarehouse, toWarehouse };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                stockTransfersQuery = stockTransfersQuery.Where(x =>
                    x.fromWarehouse.Name.Contains(term) ||
                    x.toWarehouse.Name.Contains(term) ||
                    x.transfer.Status.ToString().Contains(term));
            }

            stockTransfersQuery = filter switch
            {
                StockTransferQueueFilter.Draft => stockTransfersQuery.Where(x => x.transfer.Status == Domain.Enums.TransferStatus.Draft),
                StockTransferQueueFilter.InTransit => stockTransfersQuery.Where(x => x.transfer.Status == Domain.Enums.TransferStatus.InTransit),
                StockTransferQueueFilter.Completed => stockTransfersQuery.Where(x => x.transfer.Status == Domain.Enums.TransferStatus.Completed),
                _ => stockTransfersQuery
            };

            var total = await stockTransfersQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await stockTransfersQuery
                .OrderByDescending(x => x.transfer.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new StockTransferListItemDto
                {
                    Id = x.transfer.Id,
                    FromWarehouseId = x.transfer.FromWarehouseId,
                    ToWarehouseId = x.transfer.ToWarehouseId,
                    FromWarehouseName = x.fromWarehouse.Name,
                    ToWarehouseName = x.toWarehouse.Name,
                    Status = x.transfer.Status.ToString(),
                    LineCount = x.transfer.Lines.Count,
                    CreatedAtUtc = x.transfer.CreatedAtUtc,
                    RowVersion = x.transfer.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }

    public sealed class GetStockTransferForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetStockTransferForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<StockTransferEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var transfer = await _db.Set<StockTransfer>()
                .AsNoTracking()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id, ct)
                .ConfigureAwait(false);

            if (transfer is null)
            {
                return null;
            }

            return new StockTransferEditDto
            {
                Id = transfer.Id,
                RowVersion = transfer.RowVersion,
                FromWarehouseId = transfer.FromWarehouseId,
                ToWarehouseId = transfer.ToWarehouseId,
                Status = transfer.Status.ToString(),
                Lines = transfer.Lines
                    .OrderBy(x => x.CreatedAtUtc)
                    .Select(x => new StockTransferLineDto
                    {
                        ProductVariantId = x.ProductVariantId,
                        Quantity = x.Quantity
                    })
                    .ToList()
            };
        }
    }

    public sealed class GetPurchaseOrdersPageHandler
    {
        private readonly IAppDbContext _db;

        public GetPurchaseOrdersPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<PurchaseOrderListItemDto> Items, int Total)> HandleAsync(Guid businessId, int page, int pageSize, string? query = null, PurchaseOrderQueueFilter filter = PurchaseOrderQueueFilter.All, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var purchaseOrdersQuery =
                from order in _db.Set<PurchaseOrder>().AsNoTracking()
                join supplier in _db.Set<Supplier>().AsNoTracking() on order.SupplierId equals supplier.Id
                where order.BusinessId == businessId
                select new { order, supplier };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                purchaseOrdersQuery = purchaseOrdersQuery.Where(x =>
                    x.order.OrderNumber.Contains(term) ||
                    x.supplier.Name.Contains(term) ||
                    x.order.Status.ToString().Contains(term));
            }

            purchaseOrdersQuery = filter switch
            {
                PurchaseOrderQueueFilter.Draft => purchaseOrdersQuery.Where(x => x.order.Status == Domain.Enums.PurchaseOrderStatus.Draft),
                PurchaseOrderQueueFilter.Issued => purchaseOrdersQuery.Where(x => x.order.Status == Domain.Enums.PurchaseOrderStatus.Issued),
                PurchaseOrderQueueFilter.Received => purchaseOrdersQuery.Where(x => x.order.Status == Domain.Enums.PurchaseOrderStatus.Received),
                _ => purchaseOrdersQuery
            };

            var total = await purchaseOrdersQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await purchaseOrdersQuery
                .OrderByDescending(x => x.order.OrderedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new PurchaseOrderListItemDto
                {
                    Id = x.order.Id,
                    SupplierId = x.order.SupplierId,
                    BusinessId = x.order.BusinessId,
                    OrderNumber = x.order.OrderNumber,
                    SupplierName = x.supplier.Name,
                    Status = x.order.Status.ToString(),
                    OrderedAtUtc = x.order.OrderedAtUtc,
                    LineCount = x.order.Lines.Count,
                    RowVersion = x.order.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }

    public sealed class GetPurchaseOrderForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetPurchaseOrderForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<PurchaseOrderEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var order = await _db.Set<PurchaseOrder>()
                .AsNoTracking()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id, ct)
                .ConfigureAwait(false);

            if (order is null)
            {
                return null;
            }

            return new PurchaseOrderEditDto
            {
                Id = order.Id,
                RowVersion = order.RowVersion,
                SupplierId = order.SupplierId,
                BusinessId = order.BusinessId,
                OrderNumber = order.OrderNumber,
                OrderedAtUtc = order.OrderedAtUtc,
                Status = order.Status.ToString(),
                Lines = order.Lines
                    .OrderBy(x => x.CreatedAtUtc)
                    .Select(x => new PurchaseOrderLineDto
                    {
                        ProductVariantId = x.ProductVariantId,
                        Quantity = x.Quantity,
                        UnitCostMinor = x.UnitCostMinor,
                        TotalCostMinor = x.TotalCostMinor
                    })
                    .ToList()
            };
        }
    }
}
