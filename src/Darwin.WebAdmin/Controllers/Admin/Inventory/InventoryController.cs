using Darwin.Application.Inventory.Commands;
using Darwin.Application.Inventory.DTOs;
using Darwin.Application.Inventory.Queries;
using Darwin.WebAdmin.Controllers.Admin;
using Darwin.WebAdmin.Services.Admin;
using Darwin.WebAdmin.ViewModels.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Darwin.WebAdmin.Controllers.Admin.Inventory
{
    /// <summary>
    /// Admin inventory controller for warehouses, suppliers, stock levels, transfers, and purchase orders.
    /// </summary>
    public sealed class InventoryController : AdminBaseController
    {
        private readonly GetWarehousesPageHandler _getWarehousesPage;
        private readonly GetWarehouseForEditHandler _getWarehouseForEdit;
        private readonly CreateWarehouseHandler _createWarehouse;
        private readonly UpdateWarehouseHandler _updateWarehouse;
        private readonly GetSuppliersPageHandler _getSuppliersPage;
        private readonly GetSupplierForEditHandler _getSupplierForEdit;
        private readonly CreateSupplierHandler _createSupplier;
        private readonly UpdateSupplierHandler _updateSupplier;
        private readonly GetStockLevelsPageHandler _getStockLevelsPage;
        private readonly GetStockLevelForEditHandler _getStockLevelForEdit;
        private readonly CreateStockLevelHandler _createStockLevel;
        private readonly UpdateStockLevelHandler _updateStockLevel;
        private readonly GetStockTransfersPageHandler _getStockTransfersPage;
        private readonly GetStockTransferForEditHandler _getStockTransferForEdit;
        private readonly CreateStockTransferHandler _createStockTransfer;
        private readonly UpdateStockTransferHandler _updateStockTransfer;
        private readonly GetPurchaseOrdersPageHandler _getPurchaseOrdersPage;
        private readonly GetPurchaseOrderForEditHandler _getPurchaseOrderForEdit;
        private readonly CreatePurchaseOrderHandler _createPurchaseOrder;
        private readonly UpdatePurchaseOrderHandler _updatePurchaseOrder;
        private readonly GetInventoryLedgerHandler _getLedger;
        private readonly AdminReferenceDataService _referenceData;

        public InventoryController(
            GetWarehousesPageHandler getWarehousesPage,
            GetWarehouseForEditHandler getWarehouseForEdit,
            CreateWarehouseHandler createWarehouse,
            UpdateWarehouseHandler updateWarehouse,
            GetSuppliersPageHandler getSuppliersPage,
            GetSupplierForEditHandler getSupplierForEdit,
            CreateSupplierHandler createSupplier,
            UpdateSupplierHandler updateSupplier,
            GetStockLevelsPageHandler getStockLevelsPage,
            GetStockLevelForEditHandler getStockLevelForEdit,
            CreateStockLevelHandler createStockLevel,
            UpdateStockLevelHandler updateStockLevel,
            GetStockTransfersPageHandler getStockTransfersPage,
            GetStockTransferForEditHandler getStockTransferForEdit,
            CreateStockTransferHandler createStockTransfer,
            UpdateStockTransferHandler updateStockTransfer,
            GetPurchaseOrdersPageHandler getPurchaseOrdersPage,
            GetPurchaseOrderForEditHandler getPurchaseOrderForEdit,
            CreatePurchaseOrderHandler createPurchaseOrder,
            UpdatePurchaseOrderHandler updatePurchaseOrder,
            GetInventoryLedgerHandler getLedger,
            AdminReferenceDataService referenceData)
        {
            _getWarehousesPage = getWarehousesPage;
            _getWarehouseForEdit = getWarehouseForEdit;
            _createWarehouse = createWarehouse;
            _updateWarehouse = updateWarehouse;
            _getSuppliersPage = getSuppliersPage;
            _getSupplierForEdit = getSupplierForEdit;
            _createSupplier = createSupplier;
            _updateSupplier = updateSupplier;
            _getStockLevelsPage = getStockLevelsPage;
            _getStockLevelForEdit = getStockLevelForEdit;
            _createStockLevel = createStockLevel;
            _updateStockLevel = updateStockLevel;
            _getStockTransfersPage = getStockTransfersPage;
            _getStockTransferForEdit = getStockTransferForEdit;
            _createStockTransfer = createStockTransfer;
            _updateStockTransfer = updateStockTransfer;
            _getPurchaseOrdersPage = getPurchaseOrdersPage;
            _getPurchaseOrderForEdit = getPurchaseOrderForEdit;
            _createPurchaseOrder = createPurchaseOrder;
            _updatePurchaseOrder = updatePurchaseOrder;
            _getLedger = getLedger;
            _referenceData = referenceData;
        }

        [HttpGet]
        public IActionResult Index() => RedirectToAction(nameof(Warehouses));

        [HttpGet]
        public async Task<IActionResult> Warehouses(Guid? businessId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);

            var items = new List<WarehouseListItemVm>();
            var total = 0;
            if (businessId.HasValue)
            {
                var result = await _getWarehousesPage.HandleAsync(businessId.Value, page, pageSize, ct).ConfigureAwait(false);
                items = result.Items.Select(x => new WarehouseListItemVm
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    Description = x.Description,
                    Location = x.Location,
                    IsDefault = x.IsDefault,
                    StockLevelCount = x.StockLevelCount,
                    RowVersion = x.RowVersion
                }).ToList();
                total = result.Total;
            }

            var vm = new WarehousesListVm
            {
                BusinessId = businessId,
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateWarehouse(Guid? businessId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var vm = new WarehouseEditVm { BusinessId = businessId ?? Guid.Empty };
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWarehouse(WarehouseEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return View(vm);
            }

            var dto = new WarehouseCreateDto
            {
                BusinessId = vm.BusinessId,
                Name = vm.Name,
                Description = vm.Description,
                Location = vm.Location,
                IsDefault = vm.IsDefault
            };

            try
            {
                var id = await _createWarehouse.HandleAsync(dto, ct).ConfigureAwait(false);
                TempData["Success"] = "Warehouse created.";
                return RedirectToAction(nameof(EditWarehouse), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditWarehouse(Guid id, CancellationToken ct = default)
        {
            var dto = await _getWarehouseForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Warehouse not found.";
                return RedirectToAction(nameof(Warehouses));
            }

            var vm = new WarehouseEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                BusinessId = dto.BusinessId,
                Name = dto.Name,
                Description = dto.Description,
                Location = dto.Location,
                IsDefault = dto.IsDefault
            };
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWarehouse(WarehouseEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return View(vm);
            }

            var dto = new WarehouseEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                BusinessId = vm.BusinessId,
                Name = vm.Name,
                Description = vm.Description,
                Location = vm.Location,
                IsDefault = vm.IsDefault
            };

            try
            {
                await _updateWarehouse.HandleAsync(dto, ct).ConfigureAwait(false);
                TempData["Success"] = "Warehouse updated.";
                return RedirectToAction(nameof(EditWarehouse), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the warehouse and try again.";
                return RedirectToAction(nameof(EditWarehouse), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Suppliers(Guid? businessId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = new List<SupplierListItemVm>();
            var total = 0;
            if (businessId.HasValue)
            {
                var result = await _getSuppliersPage.HandleAsync(businessId.Value, page, pageSize, ct).ConfigureAwait(false);
                items = result.Items.Select(x => new SupplierListItemVm
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    Email = x.Email,
                    Phone = x.Phone,
                    Address = x.Address,
                    PurchaseOrderCount = x.PurchaseOrderCount,
                    RowVersion = x.RowVersion
                }).ToList();
                total = result.Total;
            }

            var vm = new SuppliersListVm
            {
                BusinessId = businessId,
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateSupplier(Guid? businessId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var vm = new SupplierEditVm { BusinessId = businessId ?? Guid.Empty };
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupplier(SupplierEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return View(vm);
            }

            var dto = new SupplierCreateDto
            {
                BusinessId = vm.BusinessId,
                Name = vm.Name,
                Email = vm.Email,
                Phone = vm.Phone,
                Address = vm.Address,
                Notes = vm.Notes
            };

            try
            {
                var id = await _createSupplier.HandleAsync(dto, ct).ConfigureAwait(false);
                TempData["Success"] = "Supplier created.";
                return RedirectToAction(nameof(EditSupplier), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditSupplier(Guid id, CancellationToken ct = default)
        {
            var dto = await _getSupplierForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Supplier not found.";
                return RedirectToAction(nameof(Suppliers));
            }

            var vm = new SupplierEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                BusinessId = dto.BusinessId,
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                Notes = dto.Notes
            };
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupplier(SupplierEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return View(vm);
            }

            var dto = new SupplierEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                BusinessId = vm.BusinessId,
                Name = vm.Name,
                Email = vm.Email,
                Phone = vm.Phone,
                Address = vm.Address,
                Notes = vm.Notes
            };

            try
            {
                await _updateSupplier.HandleAsync(dto, ct).ConfigureAwait(false);
                TempData["Success"] = "Supplier updated.";
                return RedirectToAction(nameof(EditSupplier), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the supplier and try again.";
                return RedirectToAction(nameof(EditSupplier), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> StockLevels(Guid? warehouseId = null, Guid? businessId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            warehouseId = await _referenceData.ResolveWarehouseIdAsync(warehouseId, businessId, ct).ConfigureAwait(false);

            var items = new List<StockLevelListItemVm>();
            var total = 0;
            if (warehouseId.HasValue)
            {
                var result = await _getStockLevelsPage.HandleAsync(warehouseId.Value, page, pageSize, ct).ConfigureAwait(false);
                items = result.Items.Select(x => new StockLevelListItemVm
                {
                    Id = x.Id,
                    WarehouseId = x.WarehouseId,
                    ProductVariantId = x.ProductVariantId,
                    WarehouseName = x.WarehouseName,
                    VariantSku = x.VariantSku,
                    AvailableQuantity = x.AvailableQuantity,
                    ReservedQuantity = x.ReservedQuantity,
                    ReorderPoint = x.ReorderPoint,
                    ReorderQuantity = x.ReorderQuantity,
                    InTransitQuantity = x.InTransitQuantity,
                    RowVersion = x.RowVersion
                }).ToList();
                total = result.Total;
            }

            var vm = new StockLevelsListVm
            {
                WarehouseId = warehouseId,
                WarehouseOptions = await _referenceData.GetWarehouseOptionsAsync(warehouseId, businessId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };
            ViewBag.BusinessId = businessId;
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateStockLevel(Guid? businessId = null, Guid? warehouseId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            warehouseId = await _referenceData.ResolveWarehouseIdAsync(warehouseId, businessId, ct).ConfigureAwait(false);
            var vm = new StockLevelEditVm { WarehouseId = warehouseId ?? Guid.Empty };
            await PopulateStockLevelOptionsAsync(vm, businessId, ct).ConfigureAwait(false);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStockLevel(StockLevelEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateStockLevelOptionsAsync(vm, null, ct).ConfigureAwait(false);
                return View(vm);
            }

            var dto = new StockLevelCreateDto
            {
                WarehouseId = vm.WarehouseId,
                ProductVariantId = vm.ProductVariantId,
                AvailableQuantity = vm.AvailableQuantity,
                ReservedQuantity = vm.ReservedQuantity,
                ReorderPoint = vm.ReorderPoint,
                ReorderQuantity = vm.ReorderQuantity,
                InTransitQuantity = vm.InTransitQuantity
            };

            try
            {
                var id = await _createStockLevel.HandleAsync(dto, ct).ConfigureAwait(false);
                TempData["Success"] = "Stock level created.";
                return RedirectToAction(nameof(EditStockLevel), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateStockLevelOptionsAsync(vm, null, ct).ConfigureAwait(false);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditStockLevel(Guid id, CancellationToken ct = default)
        {
            var dto = await _getStockLevelForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Stock level not found.";
                return RedirectToAction(nameof(StockLevels));
            }

            var vm = new StockLevelEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                WarehouseId = dto.WarehouseId,
                ProductVariantId = dto.ProductVariantId,
                AvailableQuantity = dto.AvailableQuantity,
                ReservedQuantity = dto.ReservedQuantity,
                ReorderPoint = dto.ReorderPoint,
                ReorderQuantity = dto.ReorderQuantity,
                InTransitQuantity = dto.InTransitQuantity
            };
            await PopulateStockLevelOptionsAsync(vm, null, ct).ConfigureAwait(false);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStockLevel(StockLevelEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateStockLevelOptionsAsync(vm, null, ct).ConfigureAwait(false);
                return View(vm);
            }

            var dto = new StockLevelEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                WarehouseId = vm.WarehouseId,
                ProductVariantId = vm.ProductVariantId,
                AvailableQuantity = vm.AvailableQuantity,
                ReservedQuantity = vm.ReservedQuantity,
                ReorderPoint = vm.ReorderPoint,
                ReorderQuantity = vm.ReorderQuantity,
                InTransitQuantity = vm.InTransitQuantity
            };

            try
            {
                await _updateStockLevel.HandleAsync(dto, ct).ConfigureAwait(false);
                TempData["Success"] = "Stock level updated.";
                return RedirectToAction(nameof(EditStockLevel), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the stock level and try again.";
                return RedirectToAction(nameof(EditStockLevel), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateStockLevelOptionsAsync(vm, null, ct).ConfigureAwait(false);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> StockTransfers(Guid? warehouseId = null, Guid? businessId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            warehouseId = await _referenceData.ResolveWarehouseIdAsync(warehouseId, businessId, ct).ConfigureAwait(false);

            var items = new List<StockTransferListItemVm>();
            var total = 0;
            if (warehouseId.HasValue)
            {
                var result = await _getStockTransfersPage.HandleAsync(warehouseId.Value, page, pageSize, ct).ConfigureAwait(false);
                items = result.Items.Select(x => new StockTransferListItemVm
                {
                    Id = x.Id,
                    FromWarehouseName = x.FromWarehouseName,
                    ToWarehouseName = x.ToWarehouseName,
                    Status = x.Status,
                    LineCount = x.LineCount,
                    CreatedAtUtc = x.CreatedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList();
                total = result.Total;
            }

            var vm = new StockTransfersListVm
            {
                WarehouseId = warehouseId,
                WarehouseOptions = await _referenceData.GetWarehouseOptionsAsync(warehouseId, businessId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };
            ViewBag.BusinessId = businessId;
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateStockTransfer(Guid? businessId = null, Guid? warehouseId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            warehouseId = await _referenceData.ResolveWarehouseIdAsync(warehouseId, businessId, ct).ConfigureAwait(false);
            var vm = new StockTransferEditVm
            {
                FromWarehouseId = warehouseId ?? Guid.Empty,
                ToWarehouseId = warehouseId ?? Guid.Empty
            };
            EnsureStockTransferRows(vm);
            await PopulateStockTransferOptionsAsync(vm, businessId, ct).ConfigureAwait(false);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStockTransfer(StockTransferEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                EnsureStockTransferRows(vm);
                await PopulateStockTransferOptionsAsync(vm, null, ct).ConfigureAwait(false);
                return View(vm);
            }

            var dto = new StockTransferCreateDto
            {
                FromWarehouseId = vm.FromWarehouseId,
                ToWarehouseId = vm.ToWarehouseId,
                Status = vm.Status,
                Lines = vm.Lines.Select(x => new StockTransferLineDto
                {
                    ProductVariantId = x.ProductVariantId,
                    Quantity = x.Quantity
                }).ToList()
            };

            try
            {
                var id = await _createStockTransfer.HandleAsync(dto, ct).ConfigureAwait(false);
                TempData["Success"] = "Stock transfer created.";
                return RedirectToAction(nameof(EditStockTransfer), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsureStockTransferRows(vm);
                await PopulateStockTransferOptionsAsync(vm, null, ct).ConfigureAwait(false);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditStockTransfer(Guid id, CancellationToken ct = default)
        {
            var dto = await _getStockTransferForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Stock transfer not found.";
                return RedirectToAction(nameof(StockTransfers));
            }

            var vm = new StockTransferEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                FromWarehouseId = dto.FromWarehouseId,
                ToWarehouseId = dto.ToWarehouseId,
                Status = dto.Status,
                Lines = dto.Lines.Select(x => new StockTransferLineVm
                {
                    ProductVariantId = x.ProductVariantId,
                    Quantity = x.Quantity
                }).ToList()
            };
            EnsureStockTransferRows(vm);
            await PopulateStockTransferOptionsAsync(vm, null, ct).ConfigureAwait(false);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStockTransfer(StockTransferEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                EnsureStockTransferRows(vm);
                await PopulateStockTransferOptionsAsync(vm, null, ct).ConfigureAwait(false);
                return View(vm);
            }

            var dto = new StockTransferEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                FromWarehouseId = vm.FromWarehouseId,
                ToWarehouseId = vm.ToWarehouseId,
                Status = vm.Status,
                Lines = vm.Lines.Select(x => new StockTransferLineDto
                {
                    ProductVariantId = x.ProductVariantId,
                    Quantity = x.Quantity
                }).ToList()
            };

            try
            {
                await _updateStockTransfer.HandleAsync(dto, ct).ConfigureAwait(false);
                TempData["Success"] = "Stock transfer updated.";
                return RedirectToAction(nameof(EditStockTransfer), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the stock transfer and try again.";
                return RedirectToAction(nameof(EditStockTransfer), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsureStockTransferRows(vm);
                await PopulateStockTransferOptionsAsync(vm, null, ct).ConfigureAwait(false);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> PurchaseOrders(Guid? businessId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = new List<PurchaseOrderListItemVm>();
            var total = 0;
            if (businessId.HasValue)
            {
                var result = await _getPurchaseOrdersPage.HandleAsync(businessId.Value, page, pageSize, ct).ConfigureAwait(false);
                items = result.Items.Select(x => new PurchaseOrderListItemVm
                {
                    Id = x.Id,
                    SupplierId = x.SupplierId,
                    OrderNumber = x.OrderNumber,
                    SupplierName = x.SupplierName,
                    Status = x.Status,
                    OrderedAtUtc = x.OrderedAtUtc,
                    LineCount = x.LineCount,
                    RowVersion = x.RowVersion
                }).ToList();
                total = result.Total;
            }

            var vm = new PurchaseOrdersListVm
            {
                BusinessId = businessId,
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreatePurchaseOrder(Guid? businessId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var vm = new PurchaseOrderEditVm
            {
                BusinessId = businessId ?? Guid.Empty,
                OrderedAtUtc = DateTime.UtcNow,
                OrderNumber = $"PO-{DateTime.UtcNow:yyyyMMddHHmm}"
            };
            EnsurePurchaseOrderRows(vm);
            await PopulatePurchaseOrderOptionsAsync(vm, ct).ConfigureAwait(false);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePurchaseOrder(PurchaseOrderEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                EnsurePurchaseOrderRows(vm);
                await PopulatePurchaseOrderOptionsAsync(vm, ct).ConfigureAwait(false);
                return View(vm);
            }

            var dto = new PurchaseOrderCreateDto
            {
                SupplierId = vm.SupplierId,
                BusinessId = vm.BusinessId,
                OrderNumber = vm.OrderNumber,
                OrderedAtUtc = vm.OrderedAtUtc,
                Status = vm.Status,
                Lines = vm.Lines.Select(x => new PurchaseOrderLineDto
                {
                    ProductVariantId = x.ProductVariantId,
                    Quantity = x.Quantity,
                    UnitCostMinor = x.UnitCostMinor,
                    TotalCostMinor = x.TotalCostMinor
                }).ToList()
            };

            try
            {
                var id = await _createPurchaseOrder.HandleAsync(dto, ct).ConfigureAwait(false);
                TempData["Success"] = "Purchase order created.";
                return RedirectToAction(nameof(EditPurchaseOrder), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsurePurchaseOrderRows(vm);
                await PopulatePurchaseOrderOptionsAsync(vm, ct).ConfigureAwait(false);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditPurchaseOrder(Guid id, CancellationToken ct = default)
        {
            var dto = await _getPurchaseOrderForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Purchase order not found.";
                return RedirectToAction(nameof(PurchaseOrders));
            }

            var vm = new PurchaseOrderEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                SupplierId = dto.SupplierId,
                BusinessId = dto.BusinessId,
                OrderNumber = dto.OrderNumber,
                OrderedAtUtc = dto.OrderedAtUtc,
                Status = dto.Status,
                Lines = dto.Lines.Select(x => new PurchaseOrderLineVm
                {
                    ProductVariantId = x.ProductVariantId,
                    Quantity = x.Quantity,
                    UnitCostMinor = x.UnitCostMinor,
                    TotalCostMinor = x.TotalCostMinor
                }).ToList()
            };
            EnsurePurchaseOrderRows(vm);
            await PopulatePurchaseOrderOptionsAsync(vm, ct).ConfigureAwait(false);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPurchaseOrder(PurchaseOrderEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                EnsurePurchaseOrderRows(vm);
                await PopulatePurchaseOrderOptionsAsync(vm, ct).ConfigureAwait(false);
                return View(vm);
            }

            var dto = new PurchaseOrderEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                SupplierId = vm.SupplierId,
                BusinessId = vm.BusinessId,
                OrderNumber = vm.OrderNumber,
                OrderedAtUtc = vm.OrderedAtUtc,
                Status = vm.Status,
                Lines = vm.Lines.Select(x => new PurchaseOrderLineDto
                {
                    ProductVariantId = x.ProductVariantId,
                    Quantity = x.Quantity,
                    UnitCostMinor = x.UnitCostMinor,
                    TotalCostMinor = x.TotalCostMinor
                }).ToList()
            };

            try
            {
                await _updatePurchaseOrder.HandleAsync(dto, ct).ConfigureAwait(false);
                TempData["Success"] = "Purchase order updated.";
                return RedirectToAction(nameof(EditPurchaseOrder), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the purchase order and try again.";
                return RedirectToAction(nameof(EditPurchaseOrder), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsurePurchaseOrderRows(vm);
                await PopulatePurchaseOrderOptionsAsync(vm, ct).ConfigureAwait(false);
                return View(vm);
            }
        }

        /// <summary>
        /// Paged ledger for a single variant.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VariantLedger(Guid variantId, Guid? warehouseId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var dto = await _getLedger.HandleAsync(variantId, page, pageSize, warehouseId, ct).ConfigureAwait(false);
            var vm = new InventoryLedgerListVm
            {
                VariantId = variantId,
                WarehouseId = warehouseId,
                Items = dto.Items.Select(x => new InventoryLedgerItemVm
                {
                    WarehouseId = x.WarehouseId,
                    WarehouseName = x.WarehouseName,
                    VariantId = x.VariantId,
                    QuantityDelta = x.QuantityDelta,
                    Reason = x.Reason,
                    ReferenceId = x.ReferenceId,
                    CreatedAtUtc = x.CreatedAtUtc
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                Total = dto.Total
            };

            return View(vm);
        }

        private async Task PopulateStockLevelOptionsAsync(StockLevelEditVm vm, Guid? businessId, CancellationToken ct)
        {
            var resolvedBusinessId = businessId ?? await _referenceData.ResolveBusinessIdAsync(null, ct).ConfigureAwait(false);
            vm.WarehouseOptions = await _referenceData.GetWarehouseOptionsAsync(vm.WarehouseId, resolvedBusinessId, ct).ConfigureAwait(false);
            vm.VariantOptions = await _referenceData.GetVariantOptionsAsync(vm.ProductVariantId, ct).ConfigureAwait(false);
        }

        private async Task PopulateStockTransferOptionsAsync(StockTransferEditVm vm, Guid? businessId, CancellationToken ct)
        {
            var resolvedBusinessId = businessId ?? await _referenceData.ResolveBusinessIdAsync(null, ct).ConfigureAwait(false);
            vm.WarehouseOptions = await _referenceData.GetWarehouseOptionsAsync(null, resolvedBusinessId, ct).ConfigureAwait(false);
            vm.VariantOptions = await _referenceData.GetVariantOptionsAsync(null, ct).ConfigureAwait(false);
        }

        private async Task PopulatePurchaseOrderOptionsAsync(PurchaseOrderEditVm vm, CancellationToken ct)
        {
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            if (vm.BusinessId != Guid.Empty)
            {
                vm.SupplierOptions = await _referenceData.GetSupplierOptionsAsync(vm.BusinessId, vm.SupplierId, includeEmpty: false, ct).ConfigureAwait(false);
            }

            vm.VariantOptions = await _referenceData.GetVariantOptionsAsync(null, ct).ConfigureAwait(false);
        }

        private static void EnsureStockTransferRows(StockTransferEditVm vm)
        {
            vm.Lines ??= new List<StockTransferLineVm>();
            if (vm.Lines.Count == 0)
            {
                vm.Lines.Add(new StockTransferLineVm());
            }
        }

        private static void EnsurePurchaseOrderRows(PurchaseOrderEditVm vm)
        {
            vm.Lines ??= new List<PurchaseOrderLineVm>();
            if (vm.Lines.Count == 0)
            {
                vm.Lines.Add(new PurchaseOrderLineVm());
            }
        }
    }
}
