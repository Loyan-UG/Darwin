using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Application.Inventory.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Inventory.Queries;

public sealed class GetWarehouseTasksPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;

    public GetWarehouseTasksPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<(List<WarehouseTaskListItemDto> Items, int Total)> HandleAsync(
        Guid businessId,
        Guid? warehouseId,
        int page,
        int pageSize,
        string? query = null,
        WarehouseTaskQueueFilter filter = WarehouseTaskQueueFilter.All,
        CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return (new List<WarehouseTaskListItemDto>(), 0);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, MaxPageSize);

        var now = DateTime.UtcNow;
        var tasks =
            from task in _db.Set<WarehouseTask>().AsNoTracking()
            join warehouse in _db.Set<Warehouse>().AsNoTracking() on task.WarehouseId equals warehouse.Id
            join fromLocation in _db.Set<WarehouseLocation>().AsNoTracking() on task.FromLocationId equals fromLocation.Id into fromLocationJoin
            from fromLocation in fromLocationJoin.DefaultIfEmpty()
            join toLocation in _db.Set<WarehouseLocation>().AsNoTracking() on task.ToLocationId equals toLocation.Id into toLocationJoin
            from toLocation in toLocationJoin.DefaultIfEmpty()
            join user in _db.Set<User>().AsNoTracking() on task.AssignedToUserId equals user.Id into userJoin
            from assignee in userJoin.DefaultIfEmpty()
            where task.BusinessId == businessId &&
                  !task.IsDeleted &&
                  !warehouse.IsDeleted &&
                  (!warehouseId.HasValue || task.WarehouseId == warehouseId.Value)
            select new { task, warehouse, fromLocation, toLocation, assignee };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.ContainsInvariant(query);
            tasks = tasks.Where(x =>
                EF.Functions.Like(x.task.Title.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                (x.task.TaskNumber != null && EF.Functions.Like(x.task.TaskNumber.ToUpper(), term, QueryLikePattern.EscapeCharacter)) ||
                EF.Functions.Like(x.warehouse.Name.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                (x.fromLocation != null && EF.Functions.Like(x.fromLocation.Code.ToUpper(), term, QueryLikePattern.EscapeCharacter)) ||
                (x.toLocation != null && EF.Functions.Like(x.toLocation.Code.ToUpper(), term, QueryLikePattern.EscapeCharacter)));
        }

        tasks = filter switch
        {
            WarehouseTaskQueueFilter.Draft => tasks.Where(x => x.task.Status == WarehouseTaskStatus.Draft),
            WarehouseTaskQueueFilter.Ready => tasks.Where(x => x.task.Status == WarehouseTaskStatus.Ready),
            WarehouseTaskQueueFilter.Assigned => tasks.Where(x => x.task.Status == WarehouseTaskStatus.Assigned),
            WarehouseTaskQueueFilter.InProgress => tasks.Where(x => x.task.Status == WarehouseTaskStatus.InProgress),
            WarehouseTaskQueueFilter.Completed => tasks.Where(x => x.task.Status == WarehouseTaskStatus.Completed),
            WarehouseTaskQueueFilter.Cancelled => tasks.Where(x => x.task.Status == WarehouseTaskStatus.Cancelled),
            WarehouseTaskQueueFilter.NeedsAssignment => tasks.Where(x => x.task.AssignedToUserId == null && x.task.Status == WarehouseTaskStatus.Ready),
            WarehouseTaskQueueFilter.Overdue => tasks.Where(x => x.task.DueAtUtc != null && x.task.DueAtUtc < now && x.task.Status != WarehouseTaskStatus.Completed && x.task.Status != WarehouseTaskStatus.Cancelled),
            WarehouseTaskQueueFilter.Shortage => tasks.Where(x => x.task.TaskType == WarehouseTaskType.Picking && x.task.Lines.Any(line => !line.IsDeleted && line.ShortQuantity > 0)),
            _ => tasks
        };

        var total = await tasks.CountAsync(ct).ConfigureAwait(false);
        var items = await tasks
            .OrderByDescending(x => x.task.Priority)
            .ThenBy(x => x.task.DueAtUtc ?? DateTime.MaxValue)
            .ThenBy(x => x.task.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new WarehouseTaskListItemDto
            {
                Id = x.task.Id,
                RowVersion = x.task.RowVersion,
                BusinessId = x.task.BusinessId,
                WarehouseId = x.task.WarehouseId,
                WarehouseName = x.warehouse.Name,
                FromLocationId = x.task.FromLocationId,
                FromLocationCode = x.fromLocation == null ? null : x.fromLocation.Code,
                ToLocationId = x.task.ToLocationId,
                ToLocationCode = x.toLocation == null ? null : x.toLocation.Code,
                AssignedToUserId = x.task.AssignedToUserId,
                AssignedToDisplayName = x.assignee == null ? null : ((x.assignee.FirstName ?? string.Empty) + " " + (x.assignee.LastName ?? string.Empty)).Trim(),
                TaskNumber = x.task.TaskNumber,
                Title = x.task.Title,
                TaskType = x.task.TaskType,
                Status = x.task.Status,
                Priority = x.task.Priority,
                SourceType = x.task.SourceType,
                SourceEntityId = x.task.SourceEntityId,
                DueAtUtc = x.task.DueAtUtc,
                LineCount = x.task.Lines.Count(line => !line.IsDeleted),
                RequestedQuantity = x.task.Lines.Where(line => !line.IsDeleted).Sum(line => line.RequestedQuantity),
                CompletedQuantity = x.task.Lines.Where(line => !line.IsDeleted).Sum(line => line.CompletedQuantity),
                ShortQuantity = x.task.TaskType == WarehouseTaskType.Picking
                    ? x.task.Lines.Where(line => !line.IsDeleted).Sum(line => line.ShortQuantity)
                    : 0,
                HasShortage = x.task.TaskType == WarehouseTaskType.Picking && x.task.Lines.Any(line => !line.IsDeleted && line.ShortQuantity > 0)
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var item in items.Where(x => string.IsNullOrWhiteSpace(x.AssignedToDisplayName) && x.AssignedToUserId.HasValue))
        {
            item.AssignedToDisplayName = item.AssignedToUserId.GetValueOrDefault().ToString("N");
        }

        return (items, total);
    }

    public async Task<WarehouseTaskOpsSummaryDto> GetSummaryAsync(Guid businessId, Guid? warehouseId, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return new WarehouseTaskOpsSummaryDto();
        var now = DateTime.UtcNow;
        var query = _db.Set<WarehouseTask>()
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId && !x.IsDeleted && (!warehouseId.HasValue || x.WarehouseId == warehouseId.Value));

        return new WarehouseTaskOpsSummaryDto
        {
            TotalCount = await query.CountAsync(ct).ConfigureAwait(false),
            ReadyCount = await query.CountAsync(x => x.Status == WarehouseTaskStatus.Ready, ct).ConfigureAwait(false),
            AssignedCount = await query.CountAsync(x => x.Status == WarehouseTaskStatus.Assigned, ct).ConfigureAwait(false),
            InProgressCount = await query.CountAsync(x => x.Status == WarehouseTaskStatus.InProgress, ct).ConfigureAwait(false),
            NeedsAssignmentCount = await query.CountAsync(x => x.Status == WarehouseTaskStatus.Ready && x.AssignedToUserId == null, ct).ConfigureAwait(false),
            OverdueCount = await query.CountAsync(x => x.DueAtUtc != null && x.DueAtUtc < now && x.Status != WarehouseTaskStatus.Completed && x.Status != WarehouseTaskStatus.Cancelled, ct).ConfigureAwait(false),
            ShortageCount = await query.CountAsync(x => x.TaskType == WarehouseTaskType.Picking && x.Lines.Any(line => !line.IsDeleted && line.ShortQuantity > 0), ct).ConfigureAwait(false),
            CompletedCount = await query.CountAsync(x => x.Status == WarehouseTaskStatus.Completed, ct).ConfigureAwait(false),
            CancelledCount = await query.CountAsync(x => x.Status == WarehouseTaskStatus.Cancelled, ct).ConfigureAwait(false)
        };
    }
}

public sealed class GetWarehouseTaskDetailHandler
{
    private readonly IAppDbContext _db;

    public GetWarehouseTaskDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<WarehouseTaskDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return null;
        var row = await (
                from task in _db.Set<WarehouseTask>().AsNoTracking().Include(x => x.Lines)
                join warehouse in _db.Set<Warehouse>().AsNoTracking() on task.WarehouseId equals warehouse.Id
                join fromLocation in _db.Set<WarehouseLocation>().AsNoTracking() on task.FromLocationId equals fromLocation.Id into fromLocationJoin
                from fromLocation in fromLocationJoin.DefaultIfEmpty()
                join toLocation in _db.Set<WarehouseLocation>().AsNoTracking() on task.ToLocationId equals toLocation.Id into toLocationJoin
                from toLocation in toLocationJoin.DefaultIfEmpty()
                join user in _db.Set<User>().AsNoTracking() on task.AssignedToUserId equals user.Id into userJoin
                from assignee in userJoin.DefaultIfEmpty()
                where task.Id == id && !task.IsDeleted && !warehouse.IsDeleted
                select new { task, warehouse, fromLocation, toLocation, assignee })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (row is null) return null;
        var assignedName = row.assignee == null
            ? null
            : $"{row.assignee.FirstName} {row.assignee.LastName}".Trim();

        return new WarehouseTaskDetailDto
        {
            Id = row.task.Id,
            RowVersion = row.task.RowVersion,
            BusinessId = row.task.BusinessId,
            WarehouseId = row.task.WarehouseId,
            WarehouseName = row.warehouse.Name,
            FromLocationId = row.task.FromLocationId,
            FromLocationCode = row.fromLocation?.Code,
            ToLocationId = row.task.ToLocationId,
            ToLocationCode = row.toLocation?.Code,
            AssignedToUserId = row.task.AssignedToUserId,
            AssignedToDisplayName = string.IsNullOrWhiteSpace(assignedName) ? row.task.AssignedToUserId?.ToString("N") : assignedName,
            TaskNumber = row.task.TaskNumber,
            Title = row.task.Title,
            TaskType = row.task.TaskType,
            Status = row.task.Status,
            Priority = row.task.Priority,
            SourceType = row.task.SourceType,
            SourceEntityId = row.task.SourceEntityId,
            DueAtUtc = row.task.DueAtUtc,
            ReadyAtUtc = row.task.ReadyAtUtc,
            AssignedAtUtc = row.task.AssignedAtUtc,
            StartedAtUtc = row.task.StartedAtUtc,
            CompletedAtUtc = row.task.CompletedAtUtc,
            CancelledAtUtc = row.task.CancelledAtUtc,
            InternalNotes = row.task.InternalNotes,
            MetadataJson = row.task.MetadataJson,
            Lines = row.task.Lines
                .Where(line => !line.IsDeleted)
                .OrderBy(line => line.SortOrder)
                .ThenBy(line => line.CreatedAtUtc)
                .Select(line => new WarehouseTaskLineDto
                {
                    Id = line.Id,
                    ProductVariantId = line.ProductVariantId,
                    FromLocationId = line.FromLocationId,
                    ToLocationId = line.ToLocationId,
                    SkuSnapshot = line.SkuSnapshot,
                    Description = line.Description,
                    RequestedQuantity = line.RequestedQuantity,
                    CompletedQuantity = line.CompletedQuantity,
                    ShortQuantity = line.ShortQuantity,
                    ShortReason = line.ShortReason,
                    SortOrder = line.SortOrder,
                    SourceLineType = line.SourceLineType,
                    SourceLineId = line.SourceLineId,
                    MetadataJson = line.MetadataJson
                }).ToList()
        };
    }
}
