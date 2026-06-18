using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Application.Inventory.DTOs;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Inventory.Queries;

public sealed class GetProductTrackingPoliciesPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;

    public GetProductTrackingPoliciesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<(List<ProductTrackingPolicyListItemDto> Items, int Total)> HandleAsync(Guid businessId, int page, int pageSize, string? query, ProductTrackingPolicyQueueFilter filter, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return (new List<ProductTrackingPolicyListItemDto>(), 0);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var rows =
            from policy in _db.Set<ProductTrackingPolicy>().AsNoTracking()
            join variant in _db.Set<ProductVariant>().AsNoTracking() on policy.ProductVariantId equals variant.Id
            where policy.BusinessId == businessId && !policy.IsDeleted && !variant.IsDeleted
            select new { policy, variant };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.ContainsInvariant(query);
            rows = rows.Where(x => EF.Functions.Like(x.variant.Sku.ToUpper(), term, QueryLikePattern.EscapeCharacter));
        }

        rows = filter switch
        {
            ProductTrackingPolicyQueueFilter.Active => rows.Where(x => x.policy.Status == ProductTrackingPolicyStatus.Active),
            ProductTrackingPolicyQueueFilter.Inactive => rows.Where(x => x.policy.Status == ProductTrackingPolicyStatus.Inactive),
            ProductTrackingPolicyQueueFilter.Archived => rows.Where(x => x.policy.Status == ProductTrackingPolicyStatus.Archived),
            ProductTrackingPolicyQueueFilter.Tracked => rows.Where(x => x.policy.TrackingMode != ProductTrackingMode.Untracked),
            _ => rows
        };

        var total = await rows.CountAsync(ct).ConfigureAwait(false);
        var items = await rows
            .OrderBy(x => x.variant.Sku)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProductTrackingPolicyListItemDto
            {
                Id = x.policy.Id,
                RowVersion = x.policy.RowVersion,
                BusinessId = x.policy.BusinessId,
                ProductVariantId = x.policy.ProductVariantId,
                VariantSku = x.variant.Sku,
                TrackingMode = x.policy.TrackingMode,
                Status = x.policy.Status,
                RequiresSupplierLot = x.policy.RequiresSupplierLot,
                RequiresExpiryDate = x.policy.RequiresExpiryDate,
                RequiresHandlingUnit = x.policy.RequiresHandlingUnit
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return (items, total);
    }

    public async Task<ProductTrackingPolicyOpsSummaryDto> GetSummaryAsync(Guid businessId, CancellationToken ct = default)
    {
        var query = _db.Set<ProductTrackingPolicy>().AsNoTracking().Where(x => x.BusinessId == businessId && !x.IsDeleted);
        return await query.GroupBy(_ => 1).Select(g => new ProductTrackingPolicyOpsSummaryDto
        {
            TotalCount = g.Count(),
            ActiveCount = g.Count(x => x.Status == ProductTrackingPolicyStatus.Active),
            TrackedCount = g.Count(x => x.TrackingMode != ProductTrackingMode.Untracked),
            RequiresExpiryCount = g.Count(x => x.RequiresExpiryDate),
            RequiresHandlingUnitCount = g.Count(x => x.RequiresHandlingUnit)
        }).FirstOrDefaultAsync(ct).ConfigureAwait(false) ?? new ProductTrackingPolicyOpsSummaryDto();
    }
}

public sealed class GetProductTrackingPolicyDetailHandler
{
    private readonly IAppDbContext _db;
    public GetProductTrackingPolicyDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public Task<ProductTrackingPolicyDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return Task.FromResult<ProductTrackingPolicyDetailDto?>(null);
        return (from policy in _db.Set<ProductTrackingPolicy>().AsNoTracking()
                join variant in _db.Set<ProductVariant>().AsNoTracking() on policy.ProductVariantId equals variant.Id
                where policy.Id == id && !policy.IsDeleted && !variant.IsDeleted
                select new ProductTrackingPolicyDetailDto
                {
                    Id = policy.Id,
                    RowVersion = policy.RowVersion,
                    BusinessId = policy.BusinessId,
                    ProductVariantId = policy.ProductVariantId,
                    VariantSku = variant.Sku,
                    TrackingMode = policy.TrackingMode,
                    Status = policy.Status,
                    RequiresSupplierLot = policy.RequiresSupplierLot,
                    RequiresExpiryDate = policy.RequiresExpiryDate,
                    RequiresHandlingUnit = policy.RequiresHandlingUnit,
                    Notes = policy.Notes,
                    MetadataJson = policy.MetadataJson
                }).FirstOrDefaultAsync(ct);
    }
}

public sealed class GetInventoryLotsPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;
    public GetInventoryLotsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<(List<InventoryLotListItemDto> Items, int Total)> HandleAsync(Guid businessId, int page, int pageSize, string? query, InventoryLotQueueFilter filter, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return (new List<InventoryLotListItemDto>(), 0);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var rows =
            from lot in _db.Set<InventoryLot>().AsNoTracking()
            join variant in _db.Set<ProductVariant>().AsNoTracking() on lot.ProductVariantId equals variant.Id
            where lot.BusinessId == businessId && !lot.IsDeleted && !variant.IsDeleted
            select new { lot, variant };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.ContainsInvariant(query);
            rows = rows.Where(x => EF.Functions.Like(x.lot.LotCode.ToUpper(), term, QueryLikePattern.EscapeCharacter) || (x.lot.SupplierLotCode != null && EF.Functions.Like(x.lot.SupplierLotCode.ToUpper(), term, QueryLikePattern.EscapeCharacter)) || EF.Functions.Like(x.variant.Sku.ToUpper(), term, QueryLikePattern.EscapeCharacter));
        }

        rows = filter switch
        {
            InventoryLotQueueFilter.Draft => rows.Where(x => x.lot.Status == InventoryLotStatus.Draft),
            InventoryLotQueueFilter.Active => rows.Where(x => x.lot.Status == InventoryLotStatus.Active),
            InventoryLotQueueFilter.Quarantined => rows.Where(x => x.lot.Status == InventoryLotStatus.Quarantined),
            InventoryLotQueueFilter.Expired => rows.Where(x => x.lot.Status == InventoryLotStatus.Expired),
            InventoryLotQueueFilter.Recalled => rows.Where(x => x.lot.Status == InventoryLotStatus.Recalled),
            InventoryLotQueueFilter.Closed => rows.Where(x => x.lot.Status == InventoryLotStatus.Closed),
            _ => rows
        };

        var total = await rows.CountAsync(ct).ConfigureAwait(false);
        var items = await rows.OrderBy(x => x.variant.Sku).ThenBy(x => x.lot.LotCode).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new InventoryLotListItemDto
            {
                Id = x.lot.Id,
                RowVersion = x.lot.RowVersion,
                BusinessId = x.lot.BusinessId,
                ProductVariantId = x.lot.ProductVariantId,
                VariantSku = x.variant.Sku,
                LotCode = x.lot.LotCode,
                SupplierLotCode = x.lot.SupplierLotCode,
                ExpiryDateUtc = x.lot.ExpiryDateUtc,
                Status = x.lot.Status
            }).ToListAsync(ct).ConfigureAwait(false);
        return (items, total);
    }

    public async Task<InventoryLotOpsSummaryDto> GetSummaryAsync(Guid businessId, CancellationToken ct = default)
    {
        var query = _db.Set<InventoryLot>().AsNoTracking().Where(x => x.BusinessId == businessId && !x.IsDeleted);
        return await query.GroupBy(_ => 1).Select(g => new InventoryLotOpsSummaryDto
        {
            TotalCount = g.Count(),
            ActiveCount = g.Count(x => x.Status == InventoryLotStatus.Active),
            QuarantinedCount = g.Count(x => x.Status == InventoryLotStatus.Quarantined),
            ExpiredCount = g.Count(x => x.Status == InventoryLotStatus.Expired),
            RecalledCount = g.Count(x => x.Status == InventoryLotStatus.Recalled)
        }).FirstOrDefaultAsync(ct).ConfigureAwait(false) ?? new InventoryLotOpsSummaryDto();
    }
}

public sealed class GetInventoryLotDetailHandler
{
    private readonly IAppDbContext _db;
    public GetInventoryLotDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public Task<InventoryLotDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return Task.FromResult<InventoryLotDetailDto?>(null);
        return (from lot in _db.Set<InventoryLot>().AsNoTracking()
                join variant in _db.Set<ProductVariant>().AsNoTracking() on lot.ProductVariantId equals variant.Id
                where lot.Id == id && !lot.IsDeleted && !variant.IsDeleted
                select new InventoryLotDetailDto
                {
                    Id = lot.Id,
                    RowVersion = lot.RowVersion,
                    BusinessId = lot.BusinessId,
                    ProductVariantId = lot.ProductVariantId,
                    VariantSku = variant.Sku,
                    LotCode = lot.LotCode,
                    SupplierLotCode = lot.SupplierLotCode,
                    ManufactureDateUtc = lot.ManufactureDateUtc,
                    ExpiryDateUtc = lot.ExpiryDateUtc,
                    Status = lot.Status,
                    Notes = lot.Notes,
                    MetadataJson = lot.MetadataJson
                }).FirstOrDefaultAsync(ct);
    }
}

public sealed class GetInventorySerialUnitsPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;
    public GetInventorySerialUnitsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<(List<InventorySerialUnitListItemDto> Items, int Total)> HandleAsync(Guid businessId, int page, int pageSize, string? query, InventorySerialUnitQueueFilter filter, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return (new List<InventorySerialUnitListItemDto>(), 0);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var rows =
            from serial in _db.Set<InventorySerialUnit>().AsNoTracking()
            join variant in _db.Set<ProductVariant>().AsNoTracking() on serial.ProductVariantId equals variant.Id
            join lot in _db.Set<InventoryLot>().AsNoTracking() on serial.InventoryLotId equals lot.Id into lotJoin
            from lot in lotJoin.DefaultIfEmpty()
            where serial.BusinessId == businessId && !serial.IsDeleted && !variant.IsDeleted
            select new { serial, variant, lot };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.ContainsInvariant(query);
            rows = rows.Where(x => EF.Functions.Like(x.serial.SerialNumber.ToUpper(), term, QueryLikePattern.EscapeCharacter) || EF.Functions.Like(x.variant.Sku.ToUpper(), term, QueryLikePattern.EscapeCharacter) || (x.lot != null && EF.Functions.Like(x.lot.LotCode.ToUpper(), term, QueryLikePattern.EscapeCharacter)));
        }

        rows = filter switch
        {
            InventorySerialUnitQueueFilter.Received => rows.Where(x => x.serial.Status == InventorySerialUnitStatus.Received),
            InventorySerialUnitQueueFilter.Available => rows.Where(x => x.serial.Status == InventorySerialUnitStatus.Available),
            InventorySerialUnitQueueFilter.Reserved => rows.Where(x => x.serial.Status == InventorySerialUnitStatus.Reserved),
            InventorySerialUnitQueueFilter.Picked => rows.Where(x => x.serial.Status == InventorySerialUnitStatus.Picked),
            InventorySerialUnitQueueFilter.Shipped => rows.Where(x => x.serial.Status == InventorySerialUnitStatus.Shipped),
            InventorySerialUnitQueueFilter.Quarantined => rows.Where(x => x.serial.Status == InventorySerialUnitStatus.Quarantined),
            InventorySerialUnitQueueFilter.Scrapped => rows.Where(x => x.serial.Status == InventorySerialUnitStatus.Scrapped),
            _ => rows
        };

        var total = await rows.CountAsync(ct).ConfigureAwait(false);
        var items = await rows.OrderBy(x => x.variant.Sku).ThenBy(x => x.serial.SerialNumber).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new InventorySerialUnitListItemDto
            {
                Id = x.serial.Id,
                RowVersion = x.serial.RowVersion,
                BusinessId = x.serial.BusinessId,
                ProductVariantId = x.serial.ProductVariantId,
                VariantSku = x.variant.Sku,
                InventoryLotId = x.serial.InventoryLotId,
                LotCode = x.lot == null ? null : x.lot.LotCode,
                SerialNumber = x.serial.SerialNumber,
                ExpiryDateUtc = x.serial.ExpiryDateUtc,
                Status = x.serial.Status
            }).ToListAsync(ct).ConfigureAwait(false);
        return (items, total);
    }

    public async Task<InventorySerialUnitOpsSummaryDto> GetSummaryAsync(Guid businessId, CancellationToken ct = default)
    {
        var query = _db.Set<InventorySerialUnit>().AsNoTracking().Where(x => x.BusinessId == businessId && !x.IsDeleted);
        return await query.GroupBy(_ => 1).Select(g => new InventorySerialUnitOpsSummaryDto
        {
            TotalCount = g.Count(),
            AvailableCount = g.Count(x => x.Status == InventorySerialUnitStatus.Available),
            ReservedCount = g.Count(x => x.Status == InventorySerialUnitStatus.Reserved),
            QuarantinedCount = g.Count(x => x.Status == InventorySerialUnitStatus.Quarantined),
            ScrappedCount = g.Count(x => x.Status == InventorySerialUnitStatus.Scrapped)
        }).FirstOrDefaultAsync(ct).ConfigureAwait(false) ?? new InventorySerialUnitOpsSummaryDto();
    }
}

public sealed class GetInventorySerialUnitDetailHandler
{
    private readonly IAppDbContext _db;
    public GetInventorySerialUnitDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public Task<InventorySerialUnitDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return Task.FromResult<InventorySerialUnitDetailDto?>(null);
        return (from serial in _db.Set<InventorySerialUnit>().AsNoTracking()
                join variant in _db.Set<ProductVariant>().AsNoTracking() on serial.ProductVariantId equals variant.Id
                join lot in _db.Set<InventoryLot>().AsNoTracking() on serial.InventoryLotId equals lot.Id into lotJoin
                from lot in lotJoin.DefaultIfEmpty()
                where serial.Id == id && !serial.IsDeleted && !variant.IsDeleted
                select new InventorySerialUnitDetailDto
                {
                    Id = serial.Id,
                    RowVersion = serial.RowVersion,
                    BusinessId = serial.BusinessId,
                    ProductVariantId = serial.ProductVariantId,
                    VariantSku = variant.Sku,
                    InventoryLotId = serial.InventoryLotId,
                    LotCode = lot == null ? null : lot.LotCode,
                    SerialNumber = serial.SerialNumber,
                    ManufactureDateUtc = serial.ManufactureDateUtc,
                    ExpiryDateUtc = serial.ExpiryDateUtc,
                    Status = serial.Status,
                    Notes = serial.Notes,
                    MetadataJson = serial.MetadataJson
                }).FirstOrDefaultAsync(ct);
    }
}

public sealed class GetHandlingUnitsPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;
    public GetHandlingUnitsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<(List<HandlingUnitListItemDto> Items, int Total)> HandleAsync(Guid businessId, int page, int pageSize, string? query, HandlingUnitQueueFilter filter, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return (new List<HandlingUnitListItemDto>(), 0);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var rows =
            from unit in _db.Set<HandlingUnit>().AsNoTracking()
            join warehouse in _db.Set<Warehouse>().AsNoTracking() on unit.WarehouseId equals warehouse.Id into warehouseJoin
            from warehouse in warehouseJoin.DefaultIfEmpty()
            join location in _db.Set<WarehouseLocation>().AsNoTracking() on unit.LocationId equals location.Id into locationJoin
            from location in locationJoin.DefaultIfEmpty()
            join parent in _db.Set<HandlingUnit>().AsNoTracking() on unit.ParentHandlingUnitId equals parent.Id into parentJoin
            from parent in parentJoin.DefaultIfEmpty()
            where unit.BusinessId == businessId && !unit.IsDeleted
            select new { unit, warehouse, location, parent };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.ContainsInvariant(query);
            rows = rows.Where(x => EF.Functions.Like(x.unit.Code.ToUpper(), term, QueryLikePattern.EscapeCharacter) || EF.Functions.Like(x.unit.DisplayName.ToUpper(), term, QueryLikePattern.EscapeCharacter) || (x.unit.Barcode != null && EF.Functions.Like(x.unit.Barcode.ToUpper(), term, QueryLikePattern.EscapeCharacter)));
        }

        rows = filter switch
        {
            HandlingUnitQueueFilter.Open => rows.Where(x => x.unit.Status == HandlingUnitStatus.Open),
            HandlingUnitQueueFilter.Closed => rows.Where(x => x.unit.Status == HandlingUnitStatus.Closed),
            HandlingUnitQueueFilter.InTransit => rows.Where(x => x.unit.Status == HandlingUnitStatus.InTransit),
            HandlingUnitQueueFilter.Received => rows.Where(x => x.unit.Status == HandlingUnitStatus.Received),
            HandlingUnitQueueFilter.BrokenDown => rows.Where(x => x.unit.Status == HandlingUnitStatus.BrokenDown),
            HandlingUnitQueueFilter.Cancelled => rows.Where(x => x.unit.Status == HandlingUnitStatus.Cancelled),
            _ => rows
        };

        var total = await rows.CountAsync(ct).ConfigureAwait(false);
        var items = await rows.OrderBy(x => x.unit.Code).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new HandlingUnitListItemDto
            {
                Id = x.unit.Id,
                RowVersion = x.unit.RowVersion,
                BusinessId = x.unit.BusinessId,
                WarehouseId = x.unit.WarehouseId,
                WarehouseName = x.warehouse == null ? null : x.warehouse.Name,
                LocationId = x.unit.LocationId,
                LocationCode = x.location == null ? null : x.location.Code,
                ParentHandlingUnitId = x.unit.ParentHandlingUnitId,
                ParentCode = x.parent == null ? null : x.parent.Code,
                Code = x.unit.Code,
                DisplayName = x.unit.DisplayName,
                Barcode = x.unit.Barcode,
                HandlingUnitType = x.unit.HandlingUnitType,
                Status = x.unit.Status,
                ContentCount = x.unit.Contents.Count(line => !line.IsDeleted),
                TotalQuantity = x.unit.Contents.Where(line => !line.IsDeleted).Sum(line => line.Quantity)
            }).ToListAsync(ct).ConfigureAwait(false);
        return (items, total);
    }

    public async Task<HandlingUnitOpsSummaryDto> GetSummaryAsync(Guid businessId, CancellationToken ct = default)
    {
        var query = _db.Set<HandlingUnit>().AsNoTracking().Where(x => x.BusinessId == businessId && !x.IsDeleted);
        return await query.GroupBy(_ => 1).Select(g => new HandlingUnitOpsSummaryDto
        {
            TotalCount = g.Count(),
            OpenCount = g.Count(x => x.Status == HandlingUnitStatus.Open),
            ClosedCount = g.Count(x => x.Status == HandlingUnitStatus.Closed),
            InTransitCount = g.Count(x => x.Status == HandlingUnitStatus.InTransit),
            ReceivedCount = g.Count(x => x.Status == HandlingUnitStatus.Received)
        }).FirstOrDefaultAsync(ct).ConfigureAwait(false) ?? new HandlingUnitOpsSummaryDto();
    }
}

public sealed class GetHandlingUnitDetailHandler
{
    private readonly IAppDbContext _db;
    public GetHandlingUnitDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<HandlingUnitDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return null;
        var row = await (
            from unit in _db.Set<HandlingUnit>().AsNoTracking()
            join warehouse in _db.Set<Warehouse>().AsNoTracking() on unit.WarehouseId equals warehouse.Id into warehouseJoin
            from warehouse in warehouseJoin.DefaultIfEmpty()
            join location in _db.Set<WarehouseLocation>().AsNoTracking() on unit.LocationId equals location.Id into locationJoin
            from location in locationJoin.DefaultIfEmpty()
            join parent in _db.Set<HandlingUnit>().AsNoTracking() on unit.ParentHandlingUnitId equals parent.Id into parentJoin
            from parent in parentJoin.DefaultIfEmpty()
            where unit.Id == id && !unit.IsDeleted
            select new { unit, warehouse, location, parent }).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (row is null) return null;

        var contents = await _db.Set<HandlingUnitContent>().AsNoTracking()
            .Where(x => x.HandlingUnitId == id && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .Select(x => new HandlingUnitContentDto
            {
                Id = x.Id,
                ProductVariantId = x.ProductVariantId,
                InventoryLotId = x.InventoryLotId,
                InventorySerialUnitId = x.InventorySerialUnitId,
                SkuSnapshot = x.SkuSnapshot,
                Description = x.Description,
                Quantity = x.Quantity,
                SortOrder = x.SortOrder,
                MetadataJson = x.MetadataJson
            }).ToListAsync(ct).ConfigureAwait(false);

        return new HandlingUnitDetailDto
        {
            Id = row.unit.Id,
            RowVersion = row.unit.RowVersion,
            BusinessId = row.unit.BusinessId,
            WarehouseId = row.unit.WarehouseId,
            WarehouseName = row.warehouse?.Name,
            LocationId = row.unit.LocationId,
            LocationCode = row.location?.Code,
            ParentHandlingUnitId = row.unit.ParentHandlingUnitId,
            ParentCode = row.parent?.Code,
            Code = row.unit.Code,
            DisplayName = row.unit.DisplayName,
            Barcode = row.unit.Barcode,
            HandlingUnitType = row.unit.HandlingUnitType,
            Status = row.unit.Status,
            Notes = row.unit.Notes,
            MetadataJson = row.unit.MetadataJson,
            Contents = contents
        };
    }
}
