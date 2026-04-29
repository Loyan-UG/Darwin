using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Inventory.DTOs;
using Darwin.Application.Inventory;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Inventory.Commands
{
    public sealed class CreateWarehouseHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<WarehouseCreateDto> _validator;

        public CreateWarehouseHandler(IAppDbContext db, IValidator<WarehouseCreateDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<Guid> HandleAsync(WarehouseCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            if (dto.IsDefault)
            {
                var existingDefaults = await _db.Set<Warehouse>()
                    .Where(x => x.BusinessId == dto.BusinessId && x.IsDefault)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                foreach (var warehouse in existingDefaults)
                {
                    warehouse.IsDefault = false;
                }
            }

            var warehouseEntity = new Warehouse
            {
                BusinessId = dto.BusinessId,
                Name = dto.Name.Trim(),
                Description = InventoryManagementHandlerSupport.NormalizeOptional(dto.Description),
                Location = InventoryManagementHandlerSupport.NormalizeOptional(dto.Location),
                IsDefault = dto.IsDefault
            };

            _db.Set<Warehouse>().Add(warehouseEntity);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return warehouseEntity.Id;
        }

    }

    public sealed class UpdateWarehouseHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<WarehouseEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateWarehouseHandler(
            IAppDbContext db,
            IValidator<WarehouseEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(WarehouseEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var warehouse = await _db.Set<Warehouse>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (warehouse is null)
            {
                throw new InvalidOperationException(_localizer["WarehouseNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = warehouse.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            if (dto.IsDefault)
            {
                var existingDefaults = await _db.Set<Warehouse>()
                    .Where(x => x.BusinessId == dto.BusinessId && x.IsDefault && x.Id != dto.Id)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                foreach (var existing in existingDefaults)
                {
                    existing.IsDefault = false;
                }
            }

            warehouse.BusinessId = dto.BusinessId;
            warehouse.Name = dto.Name.Trim();
            warehouse.Description = InventoryManagementHandlerSupport.NormalizeOptional(dto.Description);
            warehouse.Location = InventoryManagementHandlerSupport.NormalizeOptional(dto.Location);
            warehouse.IsDefault = dto.IsDefault;

            await InventoryManagementHandlerSupport.SaveChangesOrThrowConcurrencyAsync(_db, _localizer, ct).ConfigureAwait(false);
        }

    }

    public sealed class CreateSupplierHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<SupplierCreateDto> _validator;

        public CreateSupplierHandler(IAppDbContext db, IValidator<SupplierCreateDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<Guid> HandleAsync(SupplierCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var supplier = new Supplier
            {
                BusinessId = dto.BusinessId,
                Name = dto.Name.Trim(),
                Email = dto.Email.Trim(),
                Phone = dto.Phone.Trim(),
                Address = InventoryManagementHandlerSupport.NormalizeOptional(dto.Address),
                Notes = InventoryManagementHandlerSupport.NormalizeOptional(dto.Notes)
            };

            _db.Set<Supplier>().Add(supplier);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return supplier.Id;
        }

    }

    public sealed class UpdateSupplierHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<SupplierEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateSupplierHandler(
            IAppDbContext db,
            IValidator<SupplierEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(SupplierEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var supplier = await _db.Set<Supplier>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (supplier is null)
            {
                throw new InvalidOperationException(_localizer["SupplierNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = supplier.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            supplier.BusinessId = dto.BusinessId;
            supplier.Name = dto.Name.Trim();
            supplier.Email = dto.Email.Trim();
            supplier.Phone = dto.Phone.Trim();
            supplier.Address = InventoryManagementHandlerSupport.NormalizeOptional(dto.Address);
            supplier.Notes = InventoryManagementHandlerSupport.NormalizeOptional(dto.Notes);

            await InventoryManagementHandlerSupport.SaveChangesOrThrowConcurrencyAsync(_db, _localizer, ct).ConfigureAwait(false);
        }

    }

    public sealed class CreateStockLevelHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<StockLevelCreateDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public CreateStockLevelHandler(
            IAppDbContext db,
            IValidator<StockLevelCreateDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Guid> HandleAsync(StockLevelCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var exists = await _db.Set<StockLevel>()
                .AsNoTracking()
                .AnyAsync(x => x.WarehouseId == dto.WarehouseId && x.ProductVariantId == dto.ProductVariantId, ct)
                .ConfigureAwait(false);

            if (exists)
            {
                throw new InvalidOperationException(_localizer["StockLevelAlreadyExistsForWarehouseAndVariant"]);
            }

            var stockLevel = new StockLevel
            {
                WarehouseId = dto.WarehouseId,
                ProductVariantId = dto.ProductVariantId,
                AvailableQuantity = dto.AvailableQuantity,
                ReservedQuantity = dto.ReservedQuantity,
                ReorderPoint = dto.ReorderPoint,
                ReorderQuantity = dto.ReorderQuantity,
                InTransitQuantity = dto.InTransitQuantity
            };

            _db.Set<StockLevel>().Add(stockLevel);
            await Darwin.Application.Inventory.InventoryStockHelper.RefreshLegacyVariantStockAsync(_db, dto.ProductVariantId, _localizer, ct);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return stockLevel.Id;
        }
    }

    public sealed class UpdateStockLevelHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<StockLevelEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateStockLevelHandler(
            IAppDbContext db,
            IValidator<StockLevelEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(StockLevelEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var stockLevel = await _db.Set<StockLevel>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (stockLevel is null)
            {
                throw new InvalidOperationException(_localizer["StockLevelNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = stockLevel.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            stockLevel.WarehouseId = dto.WarehouseId;
            stockLevel.ProductVariantId = dto.ProductVariantId;
            stockLevel.AvailableQuantity = dto.AvailableQuantity;
            stockLevel.ReservedQuantity = dto.ReservedQuantity;
            stockLevel.ReorderPoint = dto.ReorderPoint;
            stockLevel.ReorderQuantity = dto.ReorderQuantity;
            stockLevel.InTransitQuantity = dto.InTransitQuantity;

            await Darwin.Application.Inventory.InventoryStockHelper.RefreshLegacyVariantStockAsync(_db, dto.ProductVariantId, _localizer, ct);
            await InventoryManagementHandlerSupport.SaveChangesOrThrowConcurrencyAsync(_db, _localizer, ct).ConfigureAwait(false);
        }
    }

    public sealed class CreateStockTransferHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<StockTransferCreateDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public CreateStockTransferHandler(
            IAppDbContext db,
            IValidator<StockTransferCreateDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Guid> HandleAsync(StockTransferCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var transfer = new StockTransfer
            {
                FromWarehouseId = dto.FromWarehouseId,
                ToWarehouseId = dto.ToWarehouseId,
                Status = InventoryManagementHandlerSupport.ParseTransferStatus(dto.Status, _localizer),
                Lines = dto.Lines.Select(x => new StockTransferLine
                {
                    ProductVariantId = x.ProductVariantId,
                    Quantity = x.Quantity
                }).ToList()
            };

            _db.Set<StockTransfer>().Add(transfer);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return transfer.Id;
        }
    }

    public sealed class UpdateStockTransferHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<StockTransferEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateStockTransferHandler(
            IAppDbContext db,
            IValidator<StockTransferEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(StockTransferEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var transfer = await _db.Set<StockTransfer>()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (transfer is null)
            {
                throw new InvalidOperationException(_localizer["StockTransferNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = transfer.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            transfer.FromWarehouseId = dto.FromWarehouseId;
            transfer.ToWarehouseId = dto.ToWarehouseId;
            transfer.Status = InventoryManagementHandlerSupport.ParseTransferStatus(dto.Status, _localizer);

            _db.Set<StockTransferLine>().RemoveRange(transfer.Lines);
            transfer.Lines = dto.Lines.Select(x => new StockTransferLine
            {
                ProductVariantId = x.ProductVariantId,
                Quantity = x.Quantity
            }).ToList();

            await InventoryManagementHandlerSupport.SaveChangesOrThrowConcurrencyAsync(_db, _localizer, ct).ConfigureAwait(false);
        }
    }

    public sealed class UpdateStockTransferLifecycleHandler
    {
        public const string MarkInTransitAction = "MarkInTransit";
        public const string CompleteAction = "Complete";
        public const string CancelAction = "Cancel";

        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateStockTransferLifecycleHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(StockTransferLifecycleActionDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty)
            {
                return Result.Fail(_localizer["InvalidDeleteRequest"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0)
            {
                return Result.Fail(_localizer["RowVersionRequired"]);
            }

            var transfer = await _db.Set<StockTransfer>()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (transfer is null)
            {
                return Result.Fail(_localizer["StockTransferNotFound"]);
            }

            var currentVersion = transfer.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(rowVersion))
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            var variantIdsToRefresh = new HashSet<Guid>();
            var action = (dto.Action ?? string.Empty).Trim();

            if (string.Equals(action, MarkInTransitAction, StringComparison.OrdinalIgnoreCase))
            {
                var result = await MarkInTransitAsync(transfer, variantIdsToRefresh, ct).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    return result;
                }
            }
            else if (string.Equals(action, CompleteAction, StringComparison.OrdinalIgnoreCase))
            {
                var result = await CompleteAsync(transfer, variantIdsToRefresh, ct).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    return result;
                }
            }
            else if (string.Equals(action, CancelAction, StringComparison.OrdinalIgnoreCase))
            {
                var result = await CancelAsync(transfer, variantIdsToRefresh, ct).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    return result;
                }
            }
            else
            {
                return Result.Fail(_localizer["StockTransferLifecycleUnsupportedAction"]);
            }

            foreach (var variantId in variantIdsToRefresh)
            {
                await InventoryStockHelper.RefreshLegacyVariantStockAsync(_db, variantId, _localizer, ct).ConfigureAwait(false);
            }

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            return Result.Ok();
        }

        private async Task<Result> MarkInTransitAsync(StockTransfer transfer, HashSet<Guid> variantIdsToRefresh, CancellationToken ct)
        {
            if (transfer.Status != TransferStatus.Draft)
            {
                return Result.Fail(_localizer["StockTransferLifecycleUnsupportedAction"]);
            }

            foreach (var line in transfer.Lines)
            {
                var source = await InventoryStockHelper.GetOrCreateStockLevelAsync(_db, transfer.FromWarehouseId, line.ProductVariantId, ct).ConfigureAwait(false);
                if (source.AvailableQuantity < line.Quantity)
                {
                    return Result.Fail(_localizer["StockTransferLifecycleInsufficientSourceStock"]);
                }

                source.AvailableQuantity -= line.Quantity;
                source.InTransitQuantity += line.Quantity;
                AddInventoryTransaction(transfer.FromWarehouseId, line.ProductVariantId, -line.Quantity, "StockTransferDispatched", transfer.Id);
                variantIdsToRefresh.Add(line.ProductVariantId);
            }

            transfer.Status = TransferStatus.InTransit;
            return Result.Ok();
        }

        private async Task<Result> CompleteAsync(StockTransfer transfer, HashSet<Guid> variantIdsToRefresh, CancellationToken ct)
        {
            if (transfer.Status != TransferStatus.InTransit)
            {
                return Result.Fail(_localizer["StockTransferLifecycleUnsupportedAction"]);
            }

            foreach (var line in transfer.Lines)
            {
                var source = await InventoryStockHelper.GetOrCreateStockLevelAsync(_db, transfer.FromWarehouseId, line.ProductVariantId, ct).ConfigureAwait(false);
                if (source.InTransitQuantity < line.Quantity)
                {
                    return Result.Fail(_localizer["StockTransferLifecycleInsufficientInTransitStock"]);
                }

                var destination = await InventoryStockHelper.GetOrCreateStockLevelAsync(_db, transfer.ToWarehouseId, line.ProductVariantId, ct).ConfigureAwait(false);
                source.InTransitQuantity -= line.Quantity;
                destination.AvailableQuantity += line.Quantity;
                AddInventoryTransaction(transfer.ToWarehouseId, line.ProductVariantId, line.Quantity, "StockTransferReceived", transfer.Id);
                variantIdsToRefresh.Add(line.ProductVariantId);
            }

            transfer.Status = TransferStatus.Completed;
            return Result.Ok();
        }

        private async Task<Result> CancelAsync(StockTransfer transfer, HashSet<Guid> variantIdsToRefresh, CancellationToken ct)
        {
            if (transfer.Status == TransferStatus.Draft)
            {
                transfer.Status = TransferStatus.Cancelled;
                return Result.Ok();
            }

            if (transfer.Status != TransferStatus.InTransit)
            {
                return Result.Fail(_localizer["StockTransferLifecycleUnsupportedAction"]);
            }

            foreach (var line in transfer.Lines)
            {
                var source = await InventoryStockHelper.GetOrCreateStockLevelAsync(_db, transfer.FromWarehouseId, line.ProductVariantId, ct).ConfigureAwait(false);
                if (source.InTransitQuantity < line.Quantity)
                {
                    return Result.Fail(_localizer["StockTransferLifecycleInsufficientInTransitStock"]);
                }

                source.InTransitQuantity -= line.Quantity;
                source.AvailableQuantity += line.Quantity;
                AddInventoryTransaction(transfer.FromWarehouseId, line.ProductVariantId, line.Quantity, "StockTransferCancelled", transfer.Id);
                variantIdsToRefresh.Add(line.ProductVariantId);
            }

            transfer.Status = TransferStatus.Cancelled;
            return Result.Ok();
        }

        private void AddInventoryTransaction(Guid warehouseId, Guid productVariantId, int quantityDelta, string reason, Guid referenceId)
        {
            _db.Set<InventoryTransaction>().Add(new InventoryTransaction
            {
                WarehouseId = warehouseId,
                ProductVariantId = productVariantId,
                QuantityDelta = quantityDelta,
                Reason = reason,
                ReferenceId = referenceId
            });
        }
    }

    public sealed class CreatePurchaseOrderHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<PurchaseOrderCreateDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public CreatePurchaseOrderHandler(
            IAppDbContext db,
            IValidator<PurchaseOrderCreateDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Guid> HandleAsync(PurchaseOrderCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var order = new PurchaseOrder
            {
                SupplierId = dto.SupplierId,
                BusinessId = dto.BusinessId,
                OrderNumber = dto.OrderNumber.Trim(),
                OrderedAtUtc = dto.OrderedAtUtc,
                Status = InventoryManagementHandlerSupport.ParsePurchaseOrderStatus(dto.Status, _localizer),
                Lines = dto.Lines.Select(x => new PurchaseOrderLine
                {
                    ProductVariantId = x.ProductVariantId,
                    Quantity = x.Quantity,
                    UnitCostMinor = x.UnitCostMinor,
                    TotalCostMinor = x.TotalCostMinor
                }).ToList()
            };

            _db.Set<PurchaseOrder>().Add(order);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return order.Id;
        }
    }

    public sealed class UpdatePurchaseOrderHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<PurchaseOrderEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdatePurchaseOrderHandler(
            IAppDbContext db,
            IValidator<PurchaseOrderEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(PurchaseOrderEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var order = await _db.Set<PurchaseOrder>()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (order is null)
            {
                throw new InvalidOperationException(_localizer["PurchaseOrderNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = order.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            order.SupplierId = dto.SupplierId;
            order.BusinessId = dto.BusinessId;
            order.OrderNumber = dto.OrderNumber.Trim();
            order.OrderedAtUtc = dto.OrderedAtUtc;
            order.Status = InventoryManagementHandlerSupport.ParsePurchaseOrderStatus(dto.Status, _localizer);

            _db.Set<PurchaseOrderLine>().RemoveRange(order.Lines);
            order.Lines = dto.Lines.Select(x => new PurchaseOrderLine
            {
                ProductVariantId = x.ProductVariantId,
                Quantity = x.Quantity,
                UnitCostMinor = x.UnitCostMinor,
                TotalCostMinor = x.TotalCostMinor
            }).ToList();

            await InventoryManagementHandlerSupport.SaveChangesOrThrowConcurrencyAsync(_db, _localizer, ct).ConfigureAwait(false);
        }
    }

    public sealed class UpdatePurchaseOrderLifecycleHandler
    {
        public const string IssueAction = "Issue";
        public const string ReceiveAction = "Receive";
        public const string CancelAction = "Cancel";

        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdatePurchaseOrderLifecycleHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(PurchaseOrderLifecycleActionDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty)
            {
                return Result.Fail(_localizer["InvalidDeleteRequest"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0)
            {
                return Result.Fail(_localizer["RowVersionRequired"]);
            }

            var order = await _db.Set<PurchaseOrder>()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (order is null)
            {
                return Result.Fail(_localizer["PurchaseOrderNotFound"]);
            }

            var currentVersion = order.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(rowVersion))
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            var action = (dto.Action ?? string.Empty).Trim();
            if (string.Equals(action, IssueAction, StringComparison.OrdinalIgnoreCase))
            {
                if (order.Status != PurchaseOrderStatus.Draft)
                {
                    return Result.Fail(_localizer["PurchaseOrderLifecycleUnsupportedAction"]);
                }

                order.Status = PurchaseOrderStatus.Issued;
            }
            else if (string.Equals(action, ReceiveAction, StringComparison.OrdinalIgnoreCase))
            {
                var result = await ReceiveAsync(order, ct).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    return result;
                }
            }
            else if (string.Equals(action, CancelAction, StringComparison.OrdinalIgnoreCase))
            {
                if (order.Status is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Issued))
                {
                    return Result.Fail(_localizer["PurchaseOrderLifecycleUnsupportedAction"]);
                }

                order.Status = PurchaseOrderStatus.Cancelled;
            }
            else
            {
                return Result.Fail(_localizer["PurchaseOrderLifecycleUnsupportedAction"]);
            }

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            return Result.Ok();
        }

        private async Task<Result> ReceiveAsync(PurchaseOrder order, CancellationToken ct)
        {
            if (order.Status != PurchaseOrderStatus.Issued)
            {
                return Result.Fail(_localizer["PurchaseOrderLifecycleUnsupportedAction"]);
            }

            var warehouseId = await _db.Set<Warehouse>()
                .AsNoTracking()
                .Where(x => x.BusinessId == order.BusinessId)
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.Name)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (warehouseId == Guid.Empty)
            {
                return Result.Fail(_localizer["NoWarehouseIsConfigured"]);
            }

            var variantIdsToRefresh = new HashSet<Guid>();
            foreach (var line in order.Lines)
            {
                var stockLevel = await InventoryStockHelper.GetOrCreateStockLevelAsync(_db, warehouseId, line.ProductVariantId, ct).ConfigureAwait(false);
                stockLevel.AvailableQuantity += line.Quantity;
                _db.Set<InventoryTransaction>().Add(new InventoryTransaction
                {
                    WarehouseId = warehouseId,
                    ProductVariantId = line.ProductVariantId,
                    QuantityDelta = line.Quantity,
                    Reason = "PurchaseOrderReceived",
                    ReferenceId = order.Id
                });
                variantIdsToRefresh.Add(line.ProductVariantId);
            }

            order.Status = PurchaseOrderStatus.Received;

            foreach (var variantId in variantIdsToRefresh)
            {
                await InventoryStockHelper.RefreshLegacyVariantStockAsync(_db, variantId, _localizer, ct).ConfigureAwait(false);
            }

            return Result.Ok();
        }
    }

    internal static class InventoryManagementHandlerSupport
    {
        public static TransferStatus ParseTransferStatus(string value, IStringLocalizer<ValidationResource> localizer)
        {
            if (!Enum.TryParse<TransferStatus>(value, true, out var status))
            {
                throw new ValidationException(localizer["InvalidStockTransferStatus"]);
            }

            return status;
        }

        public static PurchaseOrderStatus ParsePurchaseOrderStatus(string value, IStringLocalizer<ValidationResource> localizer)
        {
            if (!Enum.TryParse<PurchaseOrderStatus>(value, true, out var status))
            {
                throw new ValidationException(localizer["InvalidPurchaseOrderStatus"]);
            }

            return status;
        }

        public static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        public static async Task SaveChangesOrThrowConcurrencyAsync(
            IAppDbContext db,
            IStringLocalizer<ValidationResource> localizer,
            CancellationToken ct)
        {
            try
            {
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException(localizer["ConcurrencyConflictDetected"]);
            }
        }
    }
}
