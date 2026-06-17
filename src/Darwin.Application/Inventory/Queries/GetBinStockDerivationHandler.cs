using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Application.Inventory.DTOs;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Inventory.Queries
{
    public sealed class GetBinStockDerivationHandler
    {
        private const int MaxPageSize = 200;
        private readonly IAppDbContext _db;

        public GetBinStockDerivationHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<BinStockListItemDto> Items, int Total)> HandleAsync(
            Guid? businessId,
            Guid? warehouseId,
            int page,
            int pageSize,
            string? query = null,
            BinStockQueueFilter filter = BinStockQueueFilter.All,
            CancellationToken ct = default)
        {
            var rows = await BuildRowsAsync(businessId, warehouseId, query, ct).ConfigureAwait(false);
            rows = ApplyFilter(rows, filter);

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var total = rows.Count;
            var items = rows
                .OrderBy(x => x.WarehouseName)
                .ThenBy(x => x.LocationCode.Length == 0 ? "~" : x.LocationCode)
                .ThenBy(x => x.VariantSku)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (items, total);
        }

        public async Task<BinStockOpsSummaryDto> GetSummaryAsync(Guid? businessId, Guid? warehouseId, string? query = null, CancellationToken ct = default)
        {
            var rows = await BuildRowsAsync(businessId, warehouseId, query, ct).ConfigureAwait(false);
            return new BinStockOpsSummaryDto
            {
                RowCount = rows.Count,
                AssignedCount = rows.Count(x => x.LocationId.HasValue),
                UnassignedCount = rows.Count(x => !x.LocationId.HasValue),
                AttentionCount = rows.Count(x => x.HasAttention)
            };
        }

        private async Task<List<BinStockListItemDto>> BuildRowsAsync(Guid? businessId, Guid? warehouseId, string? query, CancellationToken ct)
        {
            var stockLevelsQuery =
                from stock in _db.Set<StockLevel>().AsNoTracking()
                join warehouse in _db.Set<Warehouse>().AsNoTracking() on stock.WarehouseId equals warehouse.Id
                join variant in _db.Set<ProductVariant>().AsNoTracking() on stock.ProductVariantId equals variant.Id
                where !stock.IsDeleted && !warehouse.IsDeleted && !variant.IsDeleted
                select new
                {
                    Stock = stock,
                    Warehouse = warehouse,
                    Variant = variant
                };

            if (businessId.HasValue)
            {
                stockLevelsQuery = stockLevelsQuery.Where(x => x.Warehouse.BusinessId == businessId.Value);
            }

            if (warehouseId.HasValue)
            {
                stockLevelsQuery = stockLevelsQuery.Where(x => x.Stock.WarehouseId == warehouseId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = QueryLikePattern.ContainsInvariant(query);
                stockLevelsQuery = stockLevelsQuery.Where(x =>
                    EF.Functions.Like(x.Variant.Sku.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.Warehouse.Name.ToUpper(), term, QueryLikePattern.EscapeCharacter));
            }

            var stockLevels = await stockLevelsQuery.ToListAsync(ct).ConfigureAwait(false);
            if (stockLevels.Count == 0)
            {
                return new List<BinStockListItemDto>();
            }

            var warehouseIds = stockLevels.Select(x => x.Stock.WarehouseId).Distinct().ToList();
            var variantIds = stockLevels.Select(x => x.Stock.ProductVariantId).Distinct().ToList();

            var taskRows = await LoadTaskRowsAsync(warehouseIds, variantIds, ct).ConfigureAwait(false);
            var countRows = await LoadCountRowsAsync(warehouseIds, variantIds, ct).ConfigureAwait(false);
            var locationIds = taskRows.Select(x => x.LocationId)
                .Concat(countRows.Select(x => x.LocationId))
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToList();

            var locations = await _db.Set<WarehouseLocation>()
                .AsNoTracking()
                .Where(x => locationIds.Contains(x.Id) && !x.IsDeleted)
                .ToDictionaryAsync(x => x.Id, ct)
                .ConfigureAwait(false);

            var taskGroups = taskRows
                .GroupBy(x => new BinStockKey(x.WarehouseId, x.ProductVariantId, x.LocationId))
                .ToDictionary(x => x.Key, x => x.ToList());

            var countGroups = countRows
                .GroupBy(x => new BinStockKey(x.WarehouseId, x.ProductVariantId, x.LocationId))
                .ToDictionary(x => x.Key, x => x.ToList());

            var rows = new List<BinStockListItemDto>();

            foreach (var stock in stockLevels)
            {
                var keys = taskGroups.Keys
                    .Concat(countGroups.Keys)
                    .Where(x => x.WarehouseId == stock.Stock.WarehouseId && x.ProductVariantId == stock.Stock.ProductVariantId)
                    .Distinct()
                    .ToList();

                var assignedQuantity = 0;
                foreach (var key in keys.Where(x => x.LocationId.HasValue))
                {
                    var taskQuantity = taskGroups.TryGetValue(key, out var taskGroup) ? taskGroup.Sum(x => x.Quantity) : 0;
                    assignedQuantity += taskQuantity;
                    var countQuantity = countGroups.TryGetValue(key, out var countGroup) ? countGroup.OrderByDescending(x => x.ObservedAtUtc).First().Quantity : (int?)null;
                    var quantity = countQuantity ?? taskQuantity;

                    rows.Add(CreateRow(stock.Stock.WarehouseId, stock.Warehouse.Name, stock.Stock.ProductVariantId, stock.Variant.Sku, key.LocationId, locations, quantity, stock.Stock.AvailableQuantity, 0, taskGroup ?? new List<BinStockEvidenceRow>(), countGroup ?? new List<BinStockEvidenceRow>()));
                }

                var unassignedQuantity = stock.Stock.AvailableQuantity - assignedQuantity;
                if (unassignedQuantity != 0 || !keys.Any(x => x.LocationId.HasValue))
                {
                    rows.Add(new BinStockListItemDto
                    {
                        WarehouseId = stock.Stock.WarehouseId,
                        WarehouseName = stock.Warehouse.Name,
                        ProductVariantId = stock.Stock.ProductVariantId,
                        VariantSku = stock.Variant.Sku,
                        DerivedQuantity = unassignedQuantity,
                        AvailableQuantity = stock.Stock.AvailableQuantity,
                        UnassignedQuantity = unassignedQuantity,
                        HasAttention = unassignedQuantity != 0,
                        AttentionCode = unassignedQuantity == 0 ? string.Empty : "BinStockUnassignedQuantity"
                    });
                }
            }

            return rows;
        }

        private static List<BinStockListItemDto> ApplyFilter(List<BinStockListItemDto> rows, BinStockQueueFilter filter)
        {
            return filter switch
            {
                BinStockQueueFilter.WithAttention => rows.Where(x => x.HasAttention).ToList(),
                BinStockQueueFilter.Assigned => rows.Where(x => x.LocationId.HasValue).ToList(),
                BinStockQueueFilter.Unassigned => rows.Where(x => !x.LocationId.HasValue).ToList(),
                _ => rows
            };
        }

        private static BinStockListItemDto CreateRow(
            Guid warehouseId,
            string warehouseName,
            Guid productVariantId,
            string variantSku,
            Guid? locationId,
            Dictionary<Guid, WarehouseLocation> locations,
            int quantity,
            int availableQuantity,
            int unassignedQuantity,
            List<BinStockEvidenceRow> taskRows,
            List<BinStockEvidenceRow> countRows)
        {
            var location = locationId.HasValue && locations.TryGetValue(locationId.Value, out var found) ? found : null;
            var identities = taskRows.Concat(countRows)
                .SelectMany(x => x.Identities.Select(identity => new
                {
                    identity.InventoryLotId,
                    identity.InventorySerialUnitId,
                    identity.HandlingUnitId,
                    identity.LotCode,
                    identity.SerialNumber,
                    identity.HandlingUnitCode,
                    Quantity = x.Sign * identity.Quantity
                }))
                .GroupBy(x => new { x.InventoryLotId, x.InventorySerialUnitId, x.HandlingUnitId, x.LotCode, x.SerialNumber, x.HandlingUnitCode })
                .Select(x => new BinStockIdentityBreakdownDto
                {
                    InventoryLotId = x.Key.InventoryLotId,
                    InventorySerialUnitId = x.Key.InventorySerialUnitId,
                    HandlingUnitId = x.Key.HandlingUnitId,
                    LotCode = x.Key.LotCode,
                    SerialNumber = x.Key.SerialNumber,
                    HandlingUnitCode = x.Key.HandlingUnitCode,
                    Quantity = x.Sum(y => y.Quantity)
                })
                .Where(x => x.Quantity != 0)
                .OrderBy(x => x.LotCode)
                .ThenBy(x => x.SerialNumber)
                .ThenBy(x => x.HandlingUnitCode)
                .ToList();

            var hasNegativeQuantity = quantity < 0 || identities.Any(x => x.Quantity < 0);
            return new BinStockListItemDto
            {
                WarehouseId = warehouseId,
                WarehouseName = warehouseName,
                ProductVariantId = productVariantId,
                VariantSku = variantSku,
                LocationId = locationId,
                LocationCode = location?.Code ?? string.Empty,
                LocationDisplayName = location?.DisplayName ?? string.Empty,
                DerivedQuantity = quantity,
                AvailableQuantity = availableQuantity,
                UnassignedQuantity = unassignedQuantity,
                HasAttention = hasNegativeQuantity,
                AttentionCode = hasNegativeQuantity ? "BinStockNegativeEvidence" : string.Empty,
                Identities = identities
            };
        }

        private async Task<List<BinStockEvidenceRow>> LoadTaskRowsAsync(List<Guid> warehouseIds, List<Guid> variantIds, CancellationToken ct)
        {
            var tasks = await _db.Set<WarehouseTask>()
                .AsNoTracking()
                .Include(x => x.Lines).ThenInclude(x => x.Identities)
                .Where(x => warehouseIds.Contains(x.WarehouseId) &&
                    x.Status == WarehouseTaskStatus.Completed &&
                    (x.TaskType == WarehouseTaskType.Putaway || x.TaskType == WarehouseTaskType.Picking) &&
                    !x.IsDeleted)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var rows = new List<BinStockEvidenceRow>();
            foreach (var task in tasks)
            {
                foreach (var line in task.Lines.Where(x => !x.IsDeleted && x.ProductVariantId.HasValue && variantIds.Contains(x.ProductVariantId.Value)))
                {
                    var isPutaway = task.TaskType == WarehouseTaskType.Putaway;
                    var locationId = isPutaway ? line.ToLocationId : line.FromLocationId;
                    var quantity = isPutaway ? line.CompletedQuantity : Math.Max(0, line.RequestedQuantity - line.ShortQuantity);
                    if (quantity == 0)
                    {
                        continue;
                    }

                    rows.Add(new BinStockEvidenceRow(
                        task.WarehouseId,
                        line.ProductVariantId!.Value,
                        locationId,
                        isPutaway ? quantity : -quantity,
                        isPutaway ? 1 : -1,
                        task.CompletedAtUtc ?? task.ModifiedAtUtc ?? task.CreatedAtUtc,
                        line.Identities.Where(x => !x.IsDeleted).Select(ToIdentityEvidence).ToList()));
                }
            }

            return rows;
        }

        private async Task<List<BinStockEvidenceRow>> LoadCountRowsAsync(List<Guid> warehouseIds, List<Guid> variantIds, CancellationToken ct)
        {
            var sessions = await _db.Set<StockCountSession>()
                .AsNoTracking()
                .Include(x => x.Lines).ThenInclude(x => x.Identities)
                .Where(x => warehouseIds.Contains(x.WarehouseId) &&
                    x.Status == StockCountSessionStatus.Posted &&
                    !x.IsDeleted)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var rows = new List<BinStockEvidenceRow>();
            foreach (var session in sessions)
            {
                foreach (var line in session.Lines.Where(x => !x.IsDeleted && variantIds.Contains(x.ProductVariantId)))
                {
                    var locationId = line.LocationId ?? session.LocationId;
                    rows.Add(new BinStockEvidenceRow(
                        session.WarehouseId,
                        line.ProductVariantId,
                        locationId,
                        line.CountedQuantity,
                        1,
                        session.PostedAtUtc ?? session.ApprovedAtUtc ?? session.ModifiedAtUtc ?? session.CreatedAtUtc,
                        line.Identities.Where(x => !x.IsDeleted).Select(ToIdentityEvidence).ToList()));
                }
            }

            return rows;
        }

        private static BinStockIdentityEvidence ToIdentityEvidence(WarehouseTaskLineIdentity identity)
        {
            return new BinStockIdentityEvidence(
                identity.InventoryLotId,
                identity.InventorySerialUnitId,
                identity.HandlingUnitId,
                identity.LotCodeSnapshot ?? string.Empty,
                identity.SerialNumberSnapshot ?? string.Empty,
                identity.HandlingUnitCodeSnapshot ?? string.Empty,
                identity.Quantity);
        }

        private static BinStockIdentityEvidence ToIdentityEvidence(StockCountLineIdentity identity)
        {
            return new BinStockIdentityEvidence(
                identity.InventoryLotId,
                identity.InventorySerialUnitId,
                identity.HandlingUnitId,
                identity.LotCodeSnapshot ?? string.Empty,
                identity.SerialNumberSnapshot ?? string.Empty,
                identity.HandlingUnitCodeSnapshot ?? string.Empty,
                identity.Quantity);
        }

        private readonly record struct BinStockKey(Guid WarehouseId, Guid ProductVariantId, Guid? LocationId);

        private sealed record BinStockEvidenceRow(
            Guid WarehouseId,
            Guid ProductVariantId,
            Guid? LocationId,
            int Quantity,
            int Sign,
            DateTime ObservedAtUtc,
            List<BinStockIdentityEvidence> Identities);

        private sealed record BinStockIdentityEvidence(
            Guid? InventoryLotId,
            Guid? InventorySerialUnitId,
            Guid? HandlingUnitId,
            string LotCode,
            string SerialNumber,
            string HandlingUnitCode,
            int Quantity);
    }
}
