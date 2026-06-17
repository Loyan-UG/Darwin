using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Application.Inventory.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Inventory.Queries;

public sealed class GetStockCountsPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;

    public GetStockCountsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<(List<StockCountListItemDto> Items, int Total)> HandleAsync(
        Guid businessId,
        Guid? warehouseId,
        int page,
        int pageSize,
        string? query = null,
        StockCountQueueFilter filter = StockCountQueueFilter.All,
        CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return (new List<StockCountListItemDto>(), 0);
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        var countsQuery =
            from session in _db.Set<StockCountSession>().AsNoTracking()
            join warehouse in _db.Set<Warehouse>().AsNoTracking() on session.WarehouseId equals warehouse.Id
            join location in _db.Set<WarehouseLocation>().AsNoTracking() on session.LocationId equals location.Id into locationJoin
            from location in locationJoin.DefaultIfEmpty()
            where session.BusinessId == businessId &&
                  !session.IsDeleted &&
                  !warehouse.IsDeleted &&
                  (location == null || !location.IsDeleted)
            select new { session, warehouse, location };

        if (warehouseId.HasValue && warehouseId.Value != Guid.Empty)
        {
            countsQuery = countsQuery.Where(x => x.session.WarehouseId == warehouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.ContainsInvariant(query);
            countsQuery = countsQuery.Where(x =>
                EF.Functions.Like(x.session.Title.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                (x.session.CountNumber != null && EF.Functions.Like(x.session.CountNumber.ToUpper(), term, QueryLikePattern.EscapeCharacter)) ||
                EF.Functions.Like(x.warehouse.Name.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                (x.location != null && EF.Functions.Like(x.location.Code.ToUpper(), term, QueryLikePattern.EscapeCharacter)));
        }

        countsQuery = filter switch
        {
            StockCountQueueFilter.Draft => countsQuery.Where(x => x.session.Status == StockCountSessionStatus.Draft),
            StockCountQueueFilter.InProgress => countsQuery.Where(x => x.session.Status == StockCountSessionStatus.InProgress),
            StockCountQueueFilter.ReviewPending => countsQuery.Where(x => x.session.Status == StockCountSessionStatus.ReviewPending),
            StockCountQueueFilter.Approved => countsQuery.Where(x => x.session.Status == StockCountSessionStatus.Approved),
            StockCountQueueFilter.Posted => countsQuery.Where(x => x.session.Status == StockCountSessionStatus.Posted),
            StockCountQueueFilter.Cancelled => countsQuery.Where(x => x.session.Status == StockCountSessionStatus.Cancelled),
            StockCountQueueFilter.Variance => countsQuery.Where(x => x.session.Lines.Any(line => !line.IsDeleted && line.VarianceQuantity != 0)),
            _ => countsQuery
        };

        var total = await countsQuery.CountAsync(ct).ConfigureAwait(false);
        var rows = await countsQuery
            .OrderByDescending(x => x.session.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                Session = x.session,
                WarehouseName = x.warehouse.Name,
                LocationCode = x.location == null ? null : x.location.Code,
                LineCount = x.session.Lines.Count(line => !line.IsDeleted),
                VarianceLineCount = x.session.Lines.Count(line => !line.IsDeleted && line.VarianceQuantity != 0),
                TotalExpectedQuantity = x.session.Lines.Where(line => !line.IsDeleted).Sum(line => line.ExpectedQuantity),
                TotalCountedQuantity = x.session.Lines.Where(line => !line.IsDeleted).Sum(line => line.CountedQuantity),
                TotalVarianceQuantity = x.session.Lines.Where(line => !line.IsDeleted).Sum(line => line.VarianceQuantity)
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (rows.Select(x => new StockCountListItemDto
        {
            Id = x.Session.Id,
            RowVersion = x.Session.RowVersion,
            BusinessId = x.Session.BusinessId,
            WarehouseId = x.Session.WarehouseId,
            WarehouseName = x.WarehouseName,
            LocationId = x.Session.LocationId,
            LocationCode = x.LocationCode,
            AssignedToUserId = x.Session.AssignedToUserId,
            CountNumber = x.Session.CountNumber,
            Title = x.Session.Title,
            CountType = x.Session.CountType,
            Status = x.Session.Status,
            CountWindowStartUtc = x.Session.CountWindowStartUtc,
            CountWindowEndUtc = x.Session.CountWindowEndUtc,
            LineCount = x.LineCount,
            VarianceLineCount = x.VarianceLineCount,
            TotalExpectedQuantity = x.TotalExpectedQuantity,
            TotalCountedQuantity = x.TotalCountedQuantity,
            TotalVarianceQuantity = x.TotalVarianceQuantity
        }).ToList(), total);
    }

    public async Task<StockCountOpsSummaryDto> GetSummaryAsync(Guid businessId, Guid? warehouseId, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return new StockCountOpsSummaryDto();

        var query = _db.Set<StockCountSession>()
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId && !x.IsDeleted);
        if (warehouseId.HasValue && warehouseId.Value != Guid.Empty)
        {
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        }

        return await query
            .GroupBy(_ => 1)
            .Select(g => new StockCountOpsSummaryDto
            {
                TotalCount = g.Count(),
                DraftCount = g.Count(x => x.Status == StockCountSessionStatus.Draft),
                InProgressCount = g.Count(x => x.Status == StockCountSessionStatus.InProgress),
                ReviewPendingCount = g.Count(x => x.Status == StockCountSessionStatus.ReviewPending),
                ApprovedCount = g.Count(x => x.Status == StockCountSessionStatus.Approved),
                PostedCount = g.Count(x => x.Status == StockCountSessionStatus.Posted),
                VarianceCount = g.Count(x => x.Lines.Any(line => !line.IsDeleted && line.VarianceQuantity != 0))
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false) ?? new StockCountOpsSummaryDto();
    }
}

public sealed class GetStockCountDetailHandler
{
    private readonly IAppDbContext _db;

    public GetStockCountDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<StockCountDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return null;

        var session = await _db.Set<StockCountSession>()
            .AsNoTracking()
            .Include(x => x.Lines.Where(line => !line.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (session is null) return null;

        var warehouseName = await _db.Set<Warehouse>()
            .AsNoTracking()
            .Where(x => x.Id == session.WarehouseId && !x.IsDeleted)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false) ?? string.Empty;
        var locationCode = session.LocationId.HasValue
            ? await _db.Set<WarehouseLocation>()
                .AsNoTracking()
                .Where(x => x.Id == session.LocationId.Value && !x.IsDeleted)
                .Select(x => x.Code)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false)
            : null;

        return new StockCountDetailDto
        {
            Id = session.Id,
            RowVersion = session.RowVersion,
            BusinessId = session.BusinessId,
            WarehouseId = session.WarehouseId,
            WarehouseName = warehouseName,
            LocationId = session.LocationId,
            LocationCode = locationCode,
            AssignedToUserId = session.AssignedToUserId,
            CountNumber = session.CountNumber,
            Title = session.Title,
            CountType = session.CountType,
            Status = session.Status,
            CountWindowStartUtc = session.CountWindowStartUtc,
            CountWindowEndUtc = session.CountWindowEndUtc,
            PreparedAtUtc = session.PreparedAtUtc,
            StartedAtUtc = session.StartedAtUtc,
            CountedAtUtc = session.CountedAtUtc,
            ReviewRequestedAtUtc = session.ReviewRequestedAtUtc,
            ApprovedAtUtc = session.ApprovedAtUtc,
            PostedAtUtc = session.PostedAtUtc,
            RejectedAtUtc = session.RejectedAtUtc,
            CancelledAtUtc = session.CancelledAtUtc,
            ReviewNotes = session.ReviewNotes,
            InternalNotes = session.InternalNotes,
            MetadataJson = session.MetadataJson,
            Lines = session.Lines
                .OrderBy(x => x.SortOrder)
                .Select(x => new StockCountLineDto
                {
                    Id = x.Id,
                    ProductVariantId = x.ProductVariantId,
                    LocationId = x.LocationId,
                    SkuSnapshot = x.SkuSnapshot,
                    Description = x.Description,
                    ExpectedQuantity = x.ExpectedQuantity,
                    CountedQuantity = x.CountedQuantity,
                    VarianceQuantity = x.VarianceQuantity,
                    ReviewStatus = x.ReviewStatus,
                    AdjustmentPosted = x.AdjustmentPosted,
                    ReviewNotes = x.ReviewNotes,
                    SortOrder = x.SortOrder,
                    MetadataJson = x.MetadataJson
                })
                .ToList()
        };
    }
}
