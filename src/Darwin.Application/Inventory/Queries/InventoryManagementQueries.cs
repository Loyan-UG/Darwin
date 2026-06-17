using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Common;
using Darwin.Application.Inventory.DTOs;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Net;

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
                .Where(x => !x.IsDeleted)
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
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;

        public GetWarehousesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<WarehouseListItemDto> Items, int Total)> HandleAsync(Guid businessId, int page, int pageSize, string? query = null, WarehouseQueueFilter filter = WarehouseQueueFilter.All, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var warehousesQuery = _db.Set<Warehouse>().AsNoTracking().Where(x => x.BusinessId == businessId && !x.IsDeleted);
            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = QueryLikePattern.ContainsInvariant(query);
                warehousesQuery = warehousesQuery.Where(x =>
                    EF.Functions.Like(x.Name.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                    (x.Description != null && EF.Functions.Like(x.Description.ToUpper(), term, QueryLikePattern.EscapeCharacter)) ||
                    (x.Location != null && EF.Functions.Like(x.Location.ToUpper(), term, QueryLikePattern.EscapeCharacter)));
            }

            warehousesQuery = filter switch
            {
                WarehouseQueueFilter.Default => warehousesQuery.Where(x => x.IsDefault),
                WarehouseQueueFilter.NoStockLevels => warehousesQuery.Where(x => !x.StockLevels.Any(stockLevel => !stockLevel.IsDeleted)),
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
                    StockLevelCount = x.StockLevels.Count(stockLevel => !stockLevel.IsDeleted),
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }

        public async Task<WarehouseOpsSummaryDto> GetSummaryAsync(Guid businessId, CancellationToken ct = default)
        {
            var warehousesQuery = _db.Set<Warehouse>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted);

            return await warehousesQuery
                .GroupBy(_ => 1)
                .Select(g => new WarehouseOpsSummaryDto
                {
                    TotalCount = g.Count(),
                    DefaultCount = g.Count(x => x.IsDefault),
                    NoStockLevelsCount = g.Count(x => !x.StockLevels.Any(stockLevel => !stockLevel.IsDeleted))
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false) ?? new WarehouseOpsSummaryDto();
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
                .Where(x => x.Id == id && !x.IsDeleted)
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

    public sealed class GetWarehouseLocationsPageHandler
    {
        private const int MaxPageSize = 200;
        private readonly IAppDbContext _db;

        public GetWarehouseLocationsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<WarehouseLocationListItemDto> Items, int Total)> HandleAsync(Guid businessId, Guid? warehouseId, int page, int pageSize, string? query = null, WarehouseLocationQueueFilter filter = WarehouseLocationQueueFilter.All, CancellationToken ct = default)
        {
            if (businessId == Guid.Empty) return (new List<WarehouseLocationListItemDto>(), 0);
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var locationsQuery =
                from location in _db.Set<WarehouseLocation>().AsNoTracking()
                join warehouse in _db.Set<Warehouse>().AsNoTracking() on location.WarehouseId equals warehouse.Id
                join parentLocation in _db.Set<WarehouseLocation>().AsNoTracking() on location.ParentLocationId equals parentLocation.Id into parentJoin
                from parent in parentJoin.DefaultIfEmpty()
                where location.BusinessId == businessId &&
                      !location.IsDeleted &&
                      !warehouse.IsDeleted &&
                      (!warehouseId.HasValue || location.WarehouseId == warehouseId.Value)
                select new { location, warehouse, parent };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = QueryLikePattern.ContainsInvariant(query);
                locationsQuery = locationsQuery.Where(x =>
                    EF.Functions.Like(x.location.Code.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.location.DisplayName.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                    (x.location.Barcode != null && EF.Functions.Like(x.location.Barcode.ToUpper(), term, QueryLikePattern.EscapeCharacter)) ||
                    EF.Functions.Like(x.warehouse.Name.ToUpper(), term, QueryLikePattern.EscapeCharacter));
            }

            locationsQuery = filter switch
            {
                WarehouseLocationQueueFilter.Active => locationsQuery.Where(x => x.location.Status == WarehouseLocationStatus.Active),
                WarehouseLocationQueueFilter.Inactive => locationsQuery.Where(x => x.location.Status == WarehouseLocationStatus.Inactive),
                WarehouseLocationQueueFilter.Blocked => locationsQuery.Where(x => x.location.Status == WarehouseLocationStatus.Blocked),
                WarehouseLocationQueueFilter.Bins => locationsQuery.Where(x => x.location.LocationType == WarehouseLocationType.Bin),
                WarehouseLocationQueueFilter.Docks => locationsQuery.Where(x => x.location.LocationType == WarehouseLocationType.Dock),
                WarehouseLocationQueueFilter.QualityHold => locationsQuery.Where(x => x.location.LocationType == WarehouseLocationType.QualityHold),
                _ => locationsQuery
            };

            var total = await locationsQuery.CountAsync(ct).ConfigureAwait(false);
            var items = await locationsQuery
                .OrderBy(x => x.warehouse.Name)
                .ThenBy(x => x.location.SortOrder)
                .ThenBy(x => x.location.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new WarehouseLocationListItemDto
                {
                    Id = x.location.Id,
                    RowVersion = x.location.RowVersion,
                    BusinessId = x.location.BusinessId,
                    WarehouseId = x.location.WarehouseId,
                    WarehouseName = x.warehouse.Name,
                    ParentLocationId = x.location.ParentLocationId,
                    ParentCode = x.parent == null ? null : x.parent.Code,
                    Code = x.location.Code,
                    DisplayName = x.location.DisplayName,
                    LocationType = x.location.LocationType,
                    Status = x.location.Status,
                    Barcode = x.location.Barcode,
                    SortOrder = x.location.SortOrder,
                    ChildCount = _db.Set<WarehouseLocation>().Count(child => child.ParentLocationId == x.location.Id && !child.IsDeleted)
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }

        public async Task<WarehouseLocationOpsSummaryDto> GetSummaryAsync(Guid businessId, Guid? warehouseId, CancellationToken ct = default)
        {
            var query = _db.Set<WarehouseLocation>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted && (!warehouseId.HasValue || x.WarehouseId == warehouseId.Value));

            return await query
                .GroupBy(_ => 1)
                .Select(g => new WarehouseLocationOpsSummaryDto
                {
                    TotalCount = g.Count(),
                    ActiveCount = g.Count(x => x.Status == WarehouseLocationStatus.Active),
                    BlockedCount = g.Count(x => x.Status == WarehouseLocationStatus.Blocked),
                    BinCount = g.Count(x => x.LocationType == WarehouseLocationType.Bin),
                    DockCount = g.Count(x => x.LocationType == WarehouseLocationType.Dock),
                    QualityHoldCount = g.Count(x => x.LocationType == WarehouseLocationType.QualityHold)
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false) ?? new WarehouseLocationOpsSummaryDto();
        }
    }

    public sealed class GetWarehouseLocationDetailHandler
    {
        private readonly IAppDbContext _db;

        public GetWarehouseLocationDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<WarehouseLocationDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty) return null;
            var row = await (
                    from location in _db.Set<WarehouseLocation>().AsNoTracking()
                    join warehouse in _db.Set<Warehouse>().AsNoTracking() on location.WarehouseId equals warehouse.Id
                    join parentLocation in _db.Set<WarehouseLocation>().AsNoTracking() on location.ParentLocationId equals parentLocation.Id into parentJoin
                    from parent in parentJoin.DefaultIfEmpty()
                    where location.Id == id && !location.IsDeleted && !warehouse.IsDeleted
                    select new { location, warehouse, parent })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
            if (row is null) return null;

            var children = await _db.Set<WarehouseLocation>()
                .AsNoTracking()
                .Where(x => x.ParentLocationId == id && !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Code)
                .Select(x => new WarehouseLocationTreeItemDto
                {
                    Id = x.Id,
                    ParentLocationId = x.ParentLocationId,
                    Code = x.Code,
                    DisplayName = x.DisplayName,
                    LocationType = x.LocationType,
                    Status = x.Status,
                    SortOrder = x.SortOrder
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return new WarehouseLocationDetailDto
            {
                Id = row.location.Id,
                RowVersion = row.location.RowVersion,
                BusinessId = row.location.BusinessId,
                WarehouseId = row.location.WarehouseId,
                WarehouseName = row.warehouse.Name,
                ParentLocationId = row.location.ParentLocationId,
                ParentCode = row.parent?.Code,
                Code = row.location.Code,
                DisplayName = row.location.DisplayName,
                LocationType = row.location.LocationType,
                Status = row.location.Status,
                Barcode = row.location.Barcode,
                SortOrder = row.location.SortOrder,
                Description = row.location.Description,
                MetadataJson = row.location.MetadataJson,
                Children = children
            };
        }
    }

    public sealed class GetWarehouseLocationTreeHandler
    {
        private readonly IAppDbContext _db;

        public GetWarehouseLocationTreeHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<List<WarehouseLocationTreeItemDto>> HandleAsync(Guid businessId, Guid? warehouseId, CancellationToken ct = default)
        {
            if (businessId == Guid.Empty) return new List<WarehouseLocationTreeItemDto>();

            var flat = await _db.Set<WarehouseLocation>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted && (!warehouseId.HasValue || x.WarehouseId == warehouseId.Value))
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Code)
                .Select(x => new WarehouseLocationTreeItemDto
                {
                    Id = x.Id,
                    ParentLocationId = x.ParentLocationId,
                    Code = x.Code,
                    DisplayName = x.DisplayName,
                    LocationType = x.LocationType,
                    Status = x.Status,
                    SortOrder = x.SortOrder
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var byParent = flat.GroupBy(x => x.ParentLocationId ?? Guid.Empty).ToDictionary(x => x.Key, x => x.ToList());
            foreach (var item in flat)
            {
                if (byParent.TryGetValue(item.Id, out var children))
                {
                    item.Children = children;
                }
            }

            return byParent.TryGetValue(Guid.Empty, out var roots) ? roots : new List<WarehouseLocationTreeItemDto>();
        }
    }

    public sealed class GetWarehouseLabelTemplatesPageHandler
    {
        private readonly IAppDbContext _db;

        public GetWarehouseLabelTemplatesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<WarehouseLabelTemplateListItemDto> Items, int Total)> HandleAsync(Guid businessId, int page, int pageSize, string? query, WarehouseLabelTemplateQueueFilter filter, CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                return (new List<WarehouseLabelTemplateListItemDto>(), 0);
            }

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 5, 100);

            var templates = _db.Set<WarehouseLabelTemplate>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                templates = templates.Where(x => x.Name.Contains(term) || x.TemplateKey.Contains(term));
            }

            templates = filter switch
            {
                WarehouseLabelTemplateQueueFilter.Active => templates.Where(x => x.Status == WarehouseLabelTemplateStatus.Active),
                WarehouseLabelTemplateQueueFilter.Inactive => templates.Where(x => x.Status == WarehouseLabelTemplateStatus.Inactive),
                WarehouseLabelTemplateQueueFilter.Default => templates.Where(x => x.IsDefault),
                _ => templates
            };

            var total = await templates.CountAsync(ct).ConfigureAwait(false);
            var items = await templates
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new WarehouseLabelTemplateListItemDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    TemplateKey = x.TemplateKey,
                    Status = x.Status,
                    Format = x.Format,
                    IsDefault = x.IsDefault,
                    WidthMm = x.WidthMm,
                    HeightMm = x.HeightMm
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }

        public async Task<WarehouseLabelTemplateOpsSummaryDto> GetSummaryAsync(Guid businessId, CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                return new WarehouseLabelTemplateOpsSummaryDto();
            }

            var query = _db.Set<WarehouseLabelTemplate>().AsNoTracking().Where(x => x.BusinessId == businessId && !x.IsDeleted);
            return new WarehouseLabelTemplateOpsSummaryDto
            {
                TotalCount = await query.CountAsync(ct).ConfigureAwait(false),
                ActiveCount = await query.CountAsync(x => x.Status == WarehouseLabelTemplateStatus.Active, ct).ConfigureAwait(false),
                DefaultCount = await query.CountAsync(x => x.IsDefault, ct).ConfigureAwait(false)
            };
        }
    }

    public sealed class GetWarehouseLabelTemplateDetailHandler
    {
        private readonly IAppDbContext _db;

        public GetWarehouseLabelTemplateDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<WarehouseLabelTemplateDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                return Task.FromResult<WarehouseLabelTemplateDetailDto?>(null);
            }

            return _db.Set<WarehouseLabelTemplate>()
                .AsNoTracking()
                .Where(x => x.Id == id && !x.IsDeleted)
                .Select(x => new WarehouseLabelTemplateDetailDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    TemplateKey = x.TemplateKey,
                    Status = x.Status,
                    Format = x.Format,
                    IsDefault = x.IsDefault,
                    WidthMm = x.WidthMm,
                    HeightMm = x.HeightMm,
                    ContentTemplate = x.ContentTemplate,
                    Description = x.Description,
                    MetadataJson = x.MetadataJson
                })
                .FirstOrDefaultAsync(ct);
        }
    }

    public sealed class RenderWarehouseLocationLabelsHandler
    {
        private readonly IAppDbContext _db;

        public RenderWarehouseLocationLabelsHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<WarehouseLocationLabelRenderDto?> HandleAsync(Guid businessId, Guid templateId, IReadOnlyCollection<Guid> locationIds, CancellationToken ct = default)
        {
            if (businessId == Guid.Empty || templateId == Guid.Empty || locationIds.Count == 0)
            {
                return null;
            }

            var template = await _db.Set<WarehouseLabelTemplate>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == templateId && x.BusinessId == businessId && !x.IsDeleted && x.Status == WarehouseLabelTemplateStatus.Active, ct)
                .ConfigureAwait(false);
            if (template is null)
            {
                return null;
            }

            var ids = locationIds.Where(x => x != Guid.Empty).Distinct().ToArray();
            var locations = await (
                    from location in _db.Set<WarehouseLocation>().AsNoTracking()
                    join warehouse in _db.Set<Warehouse>().AsNoTracking() on location.WarehouseId equals warehouse.Id
                    join parent in _db.Set<WarehouseLocation>().AsNoTracking() on location.ParentLocationId equals parent.Id into parentJoin
                    from parent in parentJoin.DefaultIfEmpty()
                    where ids.Contains(location.Id)
                        && location.BusinessId == businessId
                        && !location.IsDeleted
                        && !warehouse.IsDeleted
                    orderby warehouse.Name, location.SortOrder, location.Code
                    select new
                    {
                        location.Id,
                        WarehouseName = warehouse.Name,
                        location.Code,
                        location.DisplayName,
                        location.LocationType,
                        location.Status,
                        location.Barcode,
                        ParentCode = parent == null ? string.Empty : parent.Code
                    })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return new WarehouseLocationLabelRenderDto
            {
                BusinessId = businessId,
                TemplateId = template.Id,
                Format = template.Format,
                WidthMm = template.WidthMm,
                HeightMm = template.HeightMm,
                Labels = locations.Select(location => new WarehouseLocationLabelItemDto
                {
                    LocationId = location.Id,
                    WarehouseName = location.WarehouseName,
                    Code = location.Code,
                    DisplayName = location.DisplayName,
                    LocationType = location.LocationType.ToString(),
                    Status = location.Status.ToString(),
                    Barcode = string.IsNullOrWhiteSpace(location.Barcode) ? location.Code : location.Barcode,
                    ParentCode = location.ParentCode,
                    RenderedContent = RenderTemplate(template.ContentTemplate, location.WarehouseName, location.Code, location.DisplayName, location.LocationType.ToString(), location.Status.ToString(), string.IsNullOrWhiteSpace(location.Barcode) ? location.Code : location.Barcode, location.ParentCode)
                }).ToList()
            };
        }

        private static string RenderTemplate(string template, string warehouseName, string code, string displayName, string locationType, string status, string barcode, string parentCode)
        {
            return template
                .Replace("{WarehouseName}", WebUtility.HtmlEncode(warehouseName), StringComparison.Ordinal)
                .Replace("{Code}", WebUtility.HtmlEncode(code), StringComparison.Ordinal)
                .Replace("{DisplayName}", WebUtility.HtmlEncode(displayName), StringComparison.Ordinal)
                .Replace("{LocationType}", WebUtility.HtmlEncode(locationType), StringComparison.Ordinal)
                .Replace("{Status}", WebUtility.HtmlEncode(status), StringComparison.Ordinal)
                .Replace("{Barcode}", WebUtility.HtmlEncode(barcode), StringComparison.Ordinal)
                .Replace("{ParentCode}", WebUtility.HtmlEncode(parentCode), StringComparison.Ordinal);
        }
    }

    public sealed class GetSuppliersPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;

        public GetSuppliersPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<SupplierListItemDto> Items, int Total)> HandleAsync(Guid businessId, int page, int pageSize, string? query = null, SupplierQueueFilter filter = SupplierQueueFilter.All, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var suppliersQuery = _db.Set<Supplier>().AsNoTracking().Where(x => x.BusinessId == businessId && !x.IsDeleted);
            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = QueryLikePattern.ContainsInvariant(query);
                suppliersQuery = suppliersQuery.Where(x =>
                    EF.Functions.Like(x.Name.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                    (x.Code != null && EF.Functions.Like(x.Code.ToUpper(), term, QueryLikePattern.EscapeCharacter)) ||
                    EF.Functions.Like(x.Email.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.Phone.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                    (x.Address != null && EF.Functions.Like(x.Address.ToUpper(), term, QueryLikePattern.EscapeCharacter)));
            }

            suppliersQuery = filter switch
            {
                SupplierQueueFilter.MissingAddress => suppliersQuery.Where(x => x.Address == null || x.Address.Trim() == string.Empty),
                SupplierQueueFilter.HasPurchaseOrders => suppliersQuery.Where(x => x.PurchaseOrders.Any(order => !order.IsDeleted)),
                SupplierQueueFilter.Inactive => suppliersQuery.Where(x => x.Status == Domain.Enums.SupplierStatus.Inactive),
                SupplierQueueFilter.Blocked => suppliersQuery.Where(x => x.Status == Domain.Enums.SupplierStatus.Blocked),
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
                    Code = x.Code,
                    Status = x.Status.ToString(),
                    PreferredCurrency = x.PreferredCurrency,
                    PaymentTermDays = x.PaymentTermDays,
                    LeadTimeDays = x.LeadTimeDays,
                    Website = x.Website,
                    TaxRegistrationNumber = x.TaxRegistrationNumber,
                    PurchaseOrderCount = x.PurchaseOrders.Count(order => !order.IsDeleted),
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }

        public async Task<SupplierOpsSummaryDto> GetSummaryAsync(Guid businessId, CancellationToken ct = default)
        {
            var suppliersQuery = _db.Set<Supplier>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted);

            return await suppliersQuery
                .GroupBy(_ => 1)
                .Select(g => new SupplierOpsSummaryDto
                {
                    TotalCount = g.Count(),
                    MissingAddressCount = g.Count(x => x.Address == null || x.Address.Trim() == string.Empty),
                    HasPurchaseOrdersCount = g.Count(x => x.PurchaseOrders.Any(order => !order.IsDeleted)),
                    InactiveCount = g.Count(x => x.Status == Domain.Enums.SupplierStatus.Inactive),
                    BlockedCount = g.Count(x => x.Status == Domain.Enums.SupplierStatus.Blocked)
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false) ?? new SupplierOpsSummaryDto();
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
                .Include(x => x.Contacts)
                .Where(x => x.Id == id && !x.IsDeleted)
                .Select(x => new SupplierEditDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    Code = x.Code,
                    Status = x.Status.ToString(),
                    Email = x.Email,
                    Phone = x.Phone,
                    Address = x.Address,
                    Notes = x.Notes,
                    PreferredCurrency = x.PreferredCurrency,
                    PaymentTermDays = x.PaymentTermDays,
                    LeadTimeDays = x.LeadTimeDays,
                    Website = x.Website,
                    TaxRegistrationNumber = x.TaxRegistrationNumber,
                    ExternalNotes = x.ExternalNotes,
                    Contacts = x.Contacts
                        .Where(contact => !contact.IsDeleted)
                        .OrderByDescending(contact => contact.IsPrimary)
                        .ThenBy(contact => contact.Role)
                        .ThenBy(contact => contact.Name)
                        .Select(contact => new SupplierContactDto
                        {
                            Id = contact.Id,
                            RowVersion = contact.RowVersion,
                            BusinessId = contact.BusinessId,
                            SupplierId = contact.SupplierId,
                            Role = contact.Role,
                            Name = contact.Name,
                            JobTitle = contact.JobTitle,
                            Email = contact.Email,
                            Phone = contact.Phone,
                            LanguageCode = contact.LanguageCode,
                            IsPrimary = contact.IsPrimary,
                            Notes = contact.Notes
                        }).ToList()
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IReadOnlyList<SupplierDocumentDto>> GetDocumentsAsync(Guid supplierId, CancellationToken ct = default)
        {
            if (supplierId == Guid.Empty) return Array.Empty<SupplierDocumentDto>();
            return await _db.Set<DocumentRecord>()
                .AsNoTracking()
                .Where(x => x.EntityType == "Supplier" && x.EntityId == supplierId && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ThenBy(x => x.Title)
                .Select(x => new SupplierDocumentDto
                {
                    Id = x.Id,
                    DocumentKind = x.DocumentKind,
                    Title = x.Title,
                    FileName = x.FileName,
                    ContentType = x.ContentType,
                    SizeBytes = x.SizeBytes,
                    Visibility = x.Visibility,
                    MetadataJson = x.MetadataJson
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }
    }

    public sealed class GetStockLevelsPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;

        public GetStockLevelsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<StockLevelListItemDto> Items, int Total)> HandleAsync(Guid warehouseId, int page, int pageSize, string? query = null, StockLevelQueueFilter filter = StockLevelQueueFilter.All, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var stockLevelsQuery =
                from stockLevel in _db.Set<StockLevel>().AsNoTracking()
                join warehouse in _db.Set<Warehouse>().AsNoTracking() on stockLevel.WarehouseId equals warehouse.Id
                join variant in _db.Set<ProductVariant>().AsNoTracking() on stockLevel.ProductVariantId equals variant.Id
                where stockLevel.WarehouseId == warehouseId &&
                      !stockLevel.IsDeleted &&
                      !warehouse.IsDeleted &&
                      !variant.IsDeleted
                select new { stockLevel, warehouse, variant };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = QueryLikePattern.ContainsInvariant(query);
                stockLevelsQuery = stockLevelsQuery.Where(x =>
                    EF.Functions.Like(x.variant.Sku.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.warehouse.Name.ToUpper(), term, QueryLikePattern.EscapeCharacter));
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
                .Where(x => x.Id == id && !x.IsDeleted)
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
        private static readonly TimeSpan StaleInTransitAge = TimeSpan.FromDays(14);
        private const int MaxPageSize = 200;
        private readonly IAppDbContext _db;
        private readonly IClock _clock;

        public GetStockTransfersPageHandler(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task<(List<StockTransferListItemDto> Items, int Total)> HandleAsync(Guid warehouseId, int page, int pageSize, string? query = null, StockTransferQueueFilter filter = StockTransferQueueFilter.All, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var staleInTransitCutoffUtc = _clock.UtcNow.Subtract(StaleInTransitAge);
            var stockTransfersQuery =
                from transfer in _db.Set<StockTransfer>().AsNoTracking()
                join fromWarehouse in _db.Set<Warehouse>().AsNoTracking() on transfer.FromWarehouseId equals fromWarehouse.Id
                join toWarehouse in _db.Set<Warehouse>().AsNoTracking() on transfer.ToWarehouseId equals toWarehouse.Id
                where (transfer.FromWarehouseId == warehouseId || transfer.ToWarehouseId == warehouseId) &&
                      !transfer.IsDeleted &&
                      !fromWarehouse.IsDeleted &&
                      !toWarehouse.IsDeleted
                select new { transfer, fromWarehouse, toWarehouse };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = QueryLikePattern.ContainsInvariant(query);
                var statusMatches = InventorySearchTermResolver.ResolveTransferStatusSearch(query);
                stockTransfersQuery = stockTransfersQuery.Where(x =>
                    EF.Functions.Like(x.fromWarehouse.Name.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.toWarehouse.Name.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                    statusMatches.Contains(x.transfer.Status));
            }

            stockTransfersQuery = filter switch
            {
                StockTransferQueueFilter.Draft => stockTransfersQuery.Where(x => x.transfer.Status == Domain.Enums.TransferStatus.Draft),
                StockTransferQueueFilter.InTransit => stockTransfersQuery.Where(x => x.transfer.Status == Domain.Enums.TransferStatus.InTransit),
                StockTransferQueueFilter.Completed => stockTransfersQuery.Where(x => x.transfer.Status == Domain.Enums.TransferStatus.Completed),
                StockTransferQueueFilter.Cancelled => stockTransfersQuery.Where(x => x.transfer.Status == Domain.Enums.TransferStatus.Cancelled),
                StockTransferQueueFilter.StaleInTransit => stockTransfersQuery.Where(x => x.transfer.Status == Domain.Enums.TransferStatus.InTransit && x.transfer.CreatedAtUtc <= staleInTransitCutoffUtc),
                _ => stockTransfersQuery
            };

            var total = await stockTransfersQuery.CountAsync(ct).ConfigureAwait(false);

            var stockTransferRows = await stockTransfersQuery
                .OrderByDescending(x => x.transfer.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    Id = x.transfer.Id,
                    FromWarehouseId = x.transfer.FromWarehouseId,
                    ToWarehouseId = x.transfer.ToWarehouseId,
                    FromWarehouseName = x.fromWarehouse.Name,
                    ToWarehouseName = x.toWarehouse.Name,
                    Status = x.transfer.Status,
                    LineCount = x.transfer.Lines.Count(line => !line.IsDeleted),
                    CreatedAtUtc = x.transfer.CreatedAtUtc,
                    IsStale = x.transfer.Status == Domain.Enums.TransferStatus.InTransit && x.transfer.CreatedAtUtc <= staleInTransitCutoffUtc,
                    RowVersion = x.transfer.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var items = stockTransferRows
                .Select(x => new StockTransferListItemDto
                {
                    Id = x.Id,
                    FromWarehouseId = x.FromWarehouseId,
                    ToWarehouseId = x.ToWarehouseId,
                    FromWarehouseName = x.FromWarehouseName,
                    ToWarehouseName = x.ToWarehouseName,
                    Status = x.Status.ToString(),
                    LineCount = x.LineCount,
                    CreatedAtUtc = x.CreatedAtUtc,
                    IsStale = x.IsStale,
                    RowVersion = x.RowVersion
                })
                .ToList();

            return (items, total);
        }

        public async Task<StockTransferOpsSummaryDto> GetSummaryAsync(Guid warehouseId, CancellationToken ct = default)
        {
            var staleInTransitCutoffUtc = _clock.UtcNow.Subtract(StaleInTransitAge);
            var transfersQuery = _db.Set<StockTransfer>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && (x.FromWarehouseId == warehouseId || x.ToWarehouseId == warehouseId));

            return await transfersQuery
                .GroupBy(_ => 1)
                .Select(g => new StockTransferOpsSummaryDto
                {
                    TotalCount = g.Count(),
                    DraftCount = g.Count(x => x.Status == Domain.Enums.TransferStatus.Draft),
                    InTransitCount = g.Count(x => x.Status == Domain.Enums.TransferStatus.InTransit),
                    CompletedCount = g.Count(x => x.Status == Domain.Enums.TransferStatus.Completed),
                    CancelledCount = g.Count(x => x.Status == Domain.Enums.TransferStatus.Cancelled),
                    StaleInTransitCount = g.Count(x => x.Status == Domain.Enums.TransferStatus.InTransit && x.CreatedAtUtc <= staleInTransitCutoffUtc)
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false) ?? new StockTransferOpsSummaryDto();
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
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
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
                    .Where(x => !x.IsDeleted)
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
        private static readonly TimeSpan StaleIssuedAge = TimeSpan.FromDays(14);
        private const int MaxPageSize = 200;
        private readonly IAppDbContext _db;
        private readonly IClock _clock;

        public GetPurchaseOrdersPageHandler(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task<(List<PurchaseOrderListItemDto> Items, int Total)> HandleAsync(Guid businessId, int page, int pageSize, string? query = null, PurchaseOrderQueueFilter filter = PurchaseOrderQueueFilter.All, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var staleIssuedCutoffUtc = _clock.UtcNow.Subtract(StaleIssuedAge);
            var purchaseOrdersQuery =
                from order in _db.Set<PurchaseOrder>().AsNoTracking()
                join supplier in _db.Set<Supplier>().AsNoTracking() on order.SupplierId equals supplier.Id
                where order.BusinessId == businessId &&
                      !order.IsDeleted &&
                      !supplier.IsDeleted
                select new { order, supplier };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = QueryLikePattern.ContainsInvariant(query);
                var statusMatches = InventorySearchTermResolver.ResolvePurchaseOrderStatusSearch(query);
                purchaseOrdersQuery = purchaseOrdersQuery.Where(x =>
                    EF.Functions.Like(x.order.OrderNumber.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.supplier.Name.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.order.Currency.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                    statusMatches.Contains(x.order.Status));
            }

            purchaseOrdersQuery = filter switch
            {
                PurchaseOrderQueueFilter.Draft => purchaseOrdersQuery.Where(x => x.order.Status == Domain.Enums.PurchaseOrderStatus.Draft),
                PurchaseOrderQueueFilter.Issued => purchaseOrdersQuery.Where(x => x.order.Status == Domain.Enums.PurchaseOrderStatus.Issued),
                PurchaseOrderQueueFilter.Received => purchaseOrdersQuery.Where(x => x.order.Status == Domain.Enums.PurchaseOrderStatus.Received),
                PurchaseOrderQueueFilter.Cancelled => purchaseOrdersQuery.Where(x => x.order.Status == Domain.Enums.PurchaseOrderStatus.Cancelled),
                PurchaseOrderQueueFilter.StaleIssued => purchaseOrdersQuery.Where(x => x.order.Status == Domain.Enums.PurchaseOrderStatus.Issued && x.order.OrderedAtUtc <= staleIssuedCutoffUtc),
                _ => purchaseOrdersQuery
            };

            var total = await purchaseOrdersQuery.CountAsync(ct).ConfigureAwait(false);

            var purchaseOrderRows = await purchaseOrdersQuery
                .OrderByDescending(x => x.order.OrderedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    Id = x.order.Id,
                    SupplierId = x.order.SupplierId,
                    BusinessId = x.order.BusinessId,
                    OrderNumber = x.order.OrderNumber,
                    SupplierName = x.supplier.Name,
                    Status = x.order.Status,
                    Currency = x.order.Currency,
                    OrderedAtUtc = x.order.OrderedAtUtc,
                    ExpectedDeliveryDateUtc = x.order.ExpectedDeliveryDateUtc,
                    IssuedAtUtc = x.order.IssuedAtUtc,
                    ReceivedAtUtc = x.order.ReceivedAtUtc,
                    CancelledAtUtc = x.order.CancelledAtUtc,
                    LineCount = x.order.Lines.Count(line => !line.IsDeleted),
                    OrderedQuantity = x.order.Lines.Where(line => !line.IsDeleted).Sum(line => line.Quantity),
                    ReceivedQuantity = x.order.Lines.Where(line => !line.IsDeleted).Sum(line => line.ReceivedQuantity),
                    IsStale = x.order.Status == Domain.Enums.PurchaseOrderStatus.Issued && x.order.OrderedAtUtc <= staleIssuedCutoffUtc,
                    RowVersion = x.order.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var items = purchaseOrderRows
                .Select(x => new PurchaseOrderListItemDto
                {
                    Id = x.Id,
                    SupplierId = x.SupplierId,
                    BusinessId = x.BusinessId,
                    OrderNumber = x.OrderNumber,
                    SupplierName = x.SupplierName,
                    Status = x.Status.ToString(),
                    Currency = x.Currency,
                    OrderedAtUtc = x.OrderedAtUtc,
                    ExpectedDeliveryDateUtc = x.ExpectedDeliveryDateUtc,
                    IssuedAtUtc = x.IssuedAtUtc,
                    ReceivedAtUtc = x.ReceivedAtUtc,
                    CancelledAtUtc = x.CancelledAtUtc,
                    LineCount = x.LineCount,
                    OrderedQuantity = x.OrderedQuantity,
                    ReceivedQuantity = x.ReceivedQuantity,
                    IsStale = x.IsStale,
                    RowVersion = x.RowVersion
                })
                .ToList();

            return (items, total);
        }

        public async Task<PurchaseOrderOpsSummaryDto> GetSummaryAsync(Guid businessId, CancellationToken ct = default)
        {
            var staleIssuedCutoffUtc = _clock.UtcNow.Subtract(StaleIssuedAge);
            var ordersQuery = _db.Set<PurchaseOrder>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted);

            return await ordersQuery
                .GroupBy(_ => 1)
                .Select(g => new PurchaseOrderOpsSummaryDto
                {
                    TotalCount = g.Count(),
                    DraftCount = g.Count(x => x.Status == Domain.Enums.PurchaseOrderStatus.Draft),
                    IssuedCount = g.Count(x => x.Status == Domain.Enums.PurchaseOrderStatus.Issued),
                    ReceivedCount = g.Count(x => x.Status == Domain.Enums.PurchaseOrderStatus.Received),
                    CancelledCount = g.Count(x => x.Status == Domain.Enums.PurchaseOrderStatus.Cancelled),
                    StaleIssuedCount = g.Count(x => x.Status == Domain.Enums.PurchaseOrderStatus.Issued && x.OrderedAtUtc <= staleIssuedCutoffUtc),
                    PartiallyReceivedCount = g.Count(x => x.Status == Domain.Enums.PurchaseOrderStatus.Issued && x.Lines.Any(line => !line.IsDeleted && line.ReceivedQuantity > 0 && line.ReceivedQuantity < line.Quantity))
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false) ?? new PurchaseOrderOpsSummaryDto();
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
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
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
                Currency = order.Currency,
                ExpectedDeliveryDateUtc = order.ExpectedDeliveryDateUtc,
                InternalNotes = order.InternalNotes,
                Lines = order.Lines
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.CreatedAtUtc)
                    .Select(x => new PurchaseOrderLineDto
                    {
                        ProductVariantId = x.ProductVariantId,
                        SupplierSku = x.SupplierSku,
                        Description = x.Description,
                        Quantity = x.Quantity,
                        ReceivedQuantity = x.ReceivedQuantity,
                        CancelledQuantity = x.CancelledQuantity,
                        UnitCostMinor = x.UnitCostMinor,
                        TotalCostMinor = x.TotalCostMinor
                    })
                    .ToList()
            };
        }
    }

    internal static class InventorySearchTermResolver
    {
        public static IReadOnlyList<Domain.Enums.TransferStatus> ResolveTransferStatusSearch(string term)
        {
            return Resolve(term, new (Domain.Enums.TransferStatus Value, string[] Tokens)[]
            {
                (Domain.Enums.TransferStatus.Draft, ["draft"]),
                (Domain.Enums.TransferStatus.InTransit, ["intransit", "in transit", "transit"]),
                (Domain.Enums.TransferStatus.Completed, ["completed", "complete"]),
                (Domain.Enums.TransferStatus.Cancelled, ["cancelled", "canceled", "cancel"])
            });
        }

        public static IReadOnlyList<Domain.Enums.PurchaseOrderStatus> ResolvePurchaseOrderStatusSearch(string term)
        {
            return Resolve(term, new (Domain.Enums.PurchaseOrderStatus Value, string[] Tokens)[]
            {
                (Domain.Enums.PurchaseOrderStatus.Draft, ["draft"]),
                (Domain.Enums.PurchaseOrderStatus.Issued, ["issued", "issue"]),
                (Domain.Enums.PurchaseOrderStatus.Received, ["received", "receive"]),
                (Domain.Enums.PurchaseOrderStatus.Cancelled, ["cancelled", "canceled", "cancel"])
            });
        }

        private static IReadOnlyList<T> Resolve<T>(string term, IReadOnlyList<(T Value, string[] Tokens)> entries)
            where T : struct, Enum
        {
            var normalized = term.Trim();
            if (normalized.Length == 0)
            {
                return Array.Empty<T>();
            }

            return entries
                .Where(entry => entry.Tokens.Any(token => token.Contains(normalized, StringComparison.OrdinalIgnoreCase)))
                .Select(entry => entry.Value)
                .ToArray();
        }
    }
}
