using Darwin.Application.Inventory.Commands;
using Darwin.Application.Inventory.DTOs;
using Darwin.Application.Inventory.Queries;
using Darwin.WebAdmin.Controllers.Admin;
using Darwin.WebAdmin.Services.Admin;
using Darwin.WebAdmin.ViewModels.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        private readonly AdjustInventoryHandler _adjustInventory;
        private readonly ReserveInventoryHandler _reserveInventory;
        private readonly ReleaseInventoryReservationHandler _releaseInventoryReservation;
        private readonly ProcessReturnReceiptHandler _processReturnReceipt;
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
            AdjustInventoryHandler adjustInventory,
            ReserveInventoryHandler reserveInventory,
            ReleaseInventoryReservationHandler releaseInventoryReservation,
            ProcessReturnReceiptHandler processReturnReceipt,
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
            _adjustInventory = adjustInventory;
            _reserveInventory = reserveInventory;
            _releaseInventoryReservation = releaseInventoryReservation;
            _processReturnReceipt = processReturnReceipt;
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
        public IActionResult Index() => RedirectOrHtmx(nameof(Warehouses), new { });

        [HttpGet]
        public async Task<IActionResult> Warehouses(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, WarehouseQueueFilter filter = WarehouseQueueFilter.All, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);

            var items = new List<WarehouseListItemVm>();
            var total = 0;
            var summary = new WarehouseOpsSummaryVm();
            if (businessId.HasValue)
            {
                var result = await _getWarehousesPage.HandleAsync(businessId.Value, page, pageSize, q, filter, ct).ConfigureAwait(false);
                var summaryDto = await _getWarehousesPage.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);
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
                summary = new WarehouseOpsSummaryVm
                {
                    TotalCount = summaryDto.TotalCount,
                    DefaultCount = summaryDto.DefaultCount,
                    NoStockLevelsCount = summaryDto.NoStockLevelsCount
                };
            }

            var vm = new WarehousesListVm
            {
                BusinessId = businessId,
                Query = q ?? string.Empty,
                Filter = filter,
                FilterItems = BuildWarehouseFilterItems(filter),
                Summary = summary,
                Playbooks = BuildWarehousePlaybooks(),
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };
            return RenderWarehousesWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateWarehouse(Guid? businessId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var vm = new WarehouseEditVm { BusinessId = businessId ?? Guid.Empty };
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            return RenderWarehouseEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWarehouse(WarehouseEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return RenderWarehouseEditor(vm, isCreate: true);
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
                SetSuccessMessage("WarehouseCreatedMessage");
                return RedirectOrHtmx(nameof(EditWarehouse), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return RenderWarehouseEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditWarehouse(Guid id, CancellationToken ct = default)
        {
            var dto = await _getWarehouseForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("WarehouseNotFoundMessage");
                return RedirectOrHtmx(nameof(Warehouses), new { });
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
            return RenderWarehouseEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWarehouse(WarehouseEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return RenderWarehouseEditor(vm, isCreate: false);
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
                SetSuccessMessage("WarehouseUpdatedMessage");
                return RedirectOrHtmx(nameof(EditWarehouse), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("WarehouseConcurrencyMessage");
                return RedirectOrHtmx(nameof(EditWarehouse), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return RenderWarehouseEditor(vm, isCreate: false);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Suppliers(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, SupplierQueueFilter filter = SupplierQueueFilter.All, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = new List<SupplierListItemVm>();
            var total = 0;
            var summary = new SupplierOpsSummaryVm();
            if (businessId.HasValue)
            {
                var result = await _getSuppliersPage.HandleAsync(businessId.Value, page, pageSize, q, filter, ct).ConfigureAwait(false);
                var summaryDto = await _getSuppliersPage.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);
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
                summary = new SupplierOpsSummaryVm
                {
                    TotalCount = summaryDto.TotalCount,
                    MissingAddressCount = summaryDto.MissingAddressCount,
                    HasPurchaseOrdersCount = summaryDto.HasPurchaseOrdersCount
                };
            }

            var vm = new SuppliersListVm
            {
                BusinessId = businessId,
                Query = q ?? string.Empty,
                Filter = filter,
                FilterItems = BuildSupplierFilterItems(filter),
                Summary = summary,
                Playbooks = BuildSupplierPlaybooks(),
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };
            return RenderSuppliersWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateSupplier(Guid? businessId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var vm = new SupplierEditVm { BusinessId = businessId ?? Guid.Empty };
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            return RenderSupplierEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupplier(SupplierEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return RenderSupplierEditor(vm, isCreate: true);
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
                SetSuccessMessage("SupplierCreatedMessage");
                return RedirectOrHtmx(nameof(EditSupplier), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return RenderSupplierEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditSupplier(Guid id, CancellationToken ct = default)
        {
            var dto = await _getSupplierForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("SupplierNotFoundMessage");
                return RedirectOrHtmx(nameof(Suppliers), new { });
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
            return RenderSupplierEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupplier(SupplierEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return RenderSupplierEditor(vm, isCreate: false);
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
                SetSuccessMessage("SupplierUpdatedMessage");
                return RedirectOrHtmx(nameof(EditSupplier), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("SupplierConcurrencyMessage");
                return RedirectOrHtmx(nameof(EditSupplier), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return RenderSupplierEditor(vm, isCreate: false);
            }
        }

        [HttpGet]
        public async Task<IActionResult> StockLevels(Guid? warehouseId = null, Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, StockLevelQueueFilter filter = StockLevelQueueFilter.All, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            warehouseId = await _referenceData.ResolveWarehouseIdAsync(warehouseId, businessId, ct).ConfigureAwait(false);

            var items = new List<StockLevelListItemVm>();
            var total = 0;
            if (warehouseId.HasValue)
            {
                var result = await _getStockLevelsPage.HandleAsync(warehouseId.Value, page, pageSize, q, filter, ct).ConfigureAwait(false);
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
                BusinessId = businessId,
                WarehouseId = warehouseId,
                Query = q ?? string.Empty,
                Filter = filter,
                FilterItems = BuildStockLevelFilterItems(filter),
                WarehouseOptions = await _referenceData.GetWarehouseOptionsAsync(warehouseId, businessId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };
            ViewBag.BusinessId = businessId;
            return RenderStockLevelsWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> AdjustStock(Guid stockLevelId, Guid? businessId = null, CancellationToken ct = default)
        {
            var vm = await BuildInventoryAdjustActionVmAsync(stockLevelId, businessId, ct).ConfigureAwait(false);
            if (vm is null)
            {
                SetErrorMessage("StockLevelNotFoundMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { businessId });
            }

            return RenderAdjustStockEditor(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(InventoryAdjustActionVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateInventoryStockActionOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderAdjustStockEditor(vm);
            }

            try
            {
                await _adjustInventory.HandleAsync(new InventoryAdjustDto
                {
                    WarehouseId = vm.WarehouseId,
                    VariantId = vm.ProductVariantId,
                    QuantityDelta = vm.QuantityDelta,
                    Reason = vm.Reason,
                    ReferenceId = vm.ReferenceId
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("StockAdjustedMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { businessId = vm.BusinessId, warehouseId = vm.WarehouseId, filter = StockLevelQueueFilter.All });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateInventoryStockActionOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderAdjustStockEditor(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ReserveStock(Guid stockLevelId, Guid? businessId = null, CancellationToken ct = default)
        {
            var vm = await BuildInventoryReserveActionVmAsync(stockLevelId, businessId, ct).ConfigureAwait(false);
            if (vm is null)
            {
                SetErrorMessage("StockLevelNotFoundMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { businessId });
            }

            return RenderReserveStockEditor(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReserveStock(InventoryReserveActionVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateInventoryStockActionOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderReserveStockEditor(vm);
            }

            try
            {
                await _reserveInventory.HandleAsync(new InventoryReserveDto
                {
                    WarehouseId = vm.WarehouseId,
                    VariantId = vm.ProductVariantId,
                    Quantity = vm.Quantity,
                    Reason = vm.Reason,
                    ReferenceId = vm.ReferenceId
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("StockReservedMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { businessId = vm.BusinessId, warehouseId = vm.WarehouseId, filter = StockLevelQueueFilter.Reserved });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateInventoryStockActionOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderReserveStockEditor(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ReleaseReservation(Guid stockLevelId, Guid? businessId = null, CancellationToken ct = default)
        {
            var vm = await BuildInventoryReleaseActionVmAsync(stockLevelId, businessId, ct).ConfigureAwait(false);
            if (vm is null)
            {
                SetErrorMessage("StockLevelNotFoundMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { businessId });
            }

            return RenderReleaseReservationEditor(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReleaseReservation(InventoryReleaseReservationActionVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateInventoryStockActionOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderReleaseReservationEditor(vm);
            }

            try
            {
                await _releaseInventoryReservation.HandleAsync(new InventoryReleaseReservationDto
                {
                    WarehouseId = vm.WarehouseId,
                    VariantId = vm.ProductVariantId,
                    Quantity = vm.Quantity,
                    Reason = vm.Reason,
                    ReferenceId = vm.ReferenceId
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("ReservationReleasedMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { businessId = vm.BusinessId, warehouseId = vm.WarehouseId, filter = StockLevelQueueFilter.Reserved });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateInventoryStockActionOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderReleaseReservationEditor(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ReturnReceipt(Guid stockLevelId, Guid? businessId = null, CancellationToken ct = default)
        {
            var vm = await BuildInventoryReturnReceiptActionVmAsync(stockLevelId, businessId, ct).ConfigureAwait(false);
            if (vm is null)
            {
                SetErrorMessage("StockLevelNotFoundMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { businessId });
            }

            return RenderReturnReceiptEditor(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnReceipt(InventoryReturnReceiptActionVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateInventoryStockActionOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderReturnReceiptEditor(vm);
            }

            try
            {
                await _processReturnReceipt.HandleAsync(new InventoryReturnReceiptDto
                {
                    WarehouseId = vm.WarehouseId,
                    VariantId = vm.ProductVariantId,
                    Quantity = vm.Quantity,
                    Reason = vm.Reason,
                    ReferenceId = vm.ReferenceId
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("ReturnReceiptProcessedMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { businessId = vm.BusinessId, warehouseId = vm.WarehouseId, filter = StockLevelQueueFilter.All });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateInventoryStockActionOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderReturnReceiptEditor(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateStockLevel(Guid? businessId = null, Guid? warehouseId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            warehouseId = await _referenceData.ResolveWarehouseIdAsync(warehouseId, businessId, ct).ConfigureAwait(false);
            var vm = new StockLevelEditVm { WarehouseId = warehouseId ?? Guid.Empty };
            await PopulateStockLevelOptionsAsync(vm, businessId, ct).ConfigureAwait(false);
            return RenderStockLevelEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStockLevel(StockLevelEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateStockLevelOptionsAsync(vm, null, ct).ConfigureAwait(false);
                return RenderStockLevelEditor(vm, isCreate: true);
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
                SetSuccessMessage("StockLevelCreatedMessage");
                return RedirectOrHtmx(nameof(EditStockLevel), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateStockLevelOptionsAsync(vm, null, ct).ConfigureAwait(false);
                return RenderStockLevelEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditStockLevel(Guid id, CancellationToken ct = default)
        {
            var dto = await _getStockLevelForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("StockLevelNotFoundMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { });
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
            return RenderStockLevelEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStockLevel(StockLevelEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateStockLevelOptionsAsync(vm, null, ct).ConfigureAwait(false);
                return RenderStockLevelEditor(vm, isCreate: false);
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
                SetSuccessMessage("StockLevelUpdatedMessage");
                return RedirectOrHtmx(nameof(EditStockLevel), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("StockLevelConcurrencyMessage");
                return RedirectOrHtmx(nameof(EditStockLevel), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateStockLevelOptionsAsync(vm, null, ct).ConfigureAwait(false);
                return RenderStockLevelEditor(vm, isCreate: false);
            }
        }

        [HttpGet]
        public async Task<IActionResult> StockTransfers(Guid? warehouseId = null, Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, StockTransferQueueFilter filter = StockTransferQueueFilter.All, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            warehouseId = await _referenceData.ResolveWarehouseIdAsync(warehouseId, businessId, ct).ConfigureAwait(false);

            var items = new List<StockTransferListItemVm>();
            var total = 0;
            var summary = new StockTransferOpsSummaryVm();
            if (warehouseId.HasValue)
            {
                var result = await _getStockTransfersPage.HandleAsync(warehouseId.Value, page, pageSize, q, filter, ct).ConfigureAwait(false);
                var summaryDto = await _getStockTransfersPage.GetSummaryAsync(warehouseId.Value, ct).ConfigureAwait(false);
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
                summary = new StockTransferOpsSummaryVm
                {
                    TotalCount = summaryDto.TotalCount,
                    DraftCount = summaryDto.DraftCount,
                    InTransitCount = summaryDto.InTransitCount,
                    CompletedCount = summaryDto.CompletedCount
                };
            }

            var vm = new StockTransfersListVm
            {
                WarehouseId = warehouseId,
                Query = q ?? string.Empty,
                Filter = filter,
                FilterItems = BuildStockTransferFilterItems(filter),
                Summary = summary,
                Playbooks = BuildStockTransferPlaybooks(),
                WarehouseOptions = await _referenceData.GetWarehouseOptionsAsync(warehouseId, businessId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };
            ViewBag.BusinessId = businessId;
            return RenderStockTransfersWorkspace(vm);
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
            return RenderStockTransferEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStockTransfer(StockTransferEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                EnsureStockTransferRows(vm);
                await PopulateStockTransferOptionsAsync(vm, null, ct).ConfigureAwait(false);
                return RenderStockTransferEditor(vm, isCreate: true);
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
                SetSuccessMessage("StockTransferCreatedMessage");
                return RedirectOrHtmx(nameof(EditStockTransfer), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsureStockTransferRows(vm);
                await PopulateStockTransferOptionsAsync(vm, null, ct).ConfigureAwait(false);
                return RenderStockTransferEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditStockTransfer(Guid id, CancellationToken ct = default)
        {
            var dto = await _getStockTransferForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("StockTransferNotFoundMessage");
                return RedirectOrHtmx(nameof(StockTransfers), new { });
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
            return RenderStockTransferEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStockTransfer(StockTransferEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                EnsureStockTransferRows(vm);
                await PopulateStockTransferOptionsAsync(vm, null, ct).ConfigureAwait(false);
                return RenderStockTransferEditor(vm, isCreate: false);
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
                SetSuccessMessage("StockTransferUpdatedMessage");
                return RedirectOrHtmx(nameof(EditStockTransfer), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("StockTransferConcurrencyMessage");
                return RedirectOrHtmx(nameof(EditStockTransfer), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsureStockTransferRows(vm);
                await PopulateStockTransferOptionsAsync(vm, null, ct).ConfigureAwait(false);
                return RenderStockTransferEditor(vm, isCreate: false);
            }
        }

        [HttpGet]
        public async Task<IActionResult> PurchaseOrders(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, PurchaseOrderQueueFilter filter = PurchaseOrderQueueFilter.All, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = new List<PurchaseOrderListItemVm>();
            var total = 0;
            var summary = new PurchaseOrderOpsSummaryVm();
            if (businessId.HasValue)
            {
                var result = await _getPurchaseOrdersPage.HandleAsync(businessId.Value, page, pageSize, q, filter, ct).ConfigureAwait(false);
                var summaryDto = await _getPurchaseOrdersPage.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);
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
                summary = new PurchaseOrderOpsSummaryVm
                {
                    TotalCount = summaryDto.TotalCount,
                    DraftCount = summaryDto.DraftCount,
                    IssuedCount = summaryDto.IssuedCount,
                    ReceivedCount = summaryDto.ReceivedCount
                };
            }

            var vm = new PurchaseOrdersListVm
            {
                BusinessId = businessId,
                Query = q ?? string.Empty,
                Filter = filter,
                FilterItems = BuildPurchaseOrderFilterItems(filter),
                Summary = summary,
                Playbooks = BuildPurchaseOrderPlaybooks(),
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };
            return RenderPurchaseOrdersWorkspace(vm);
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
            return RenderPurchaseOrderEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePurchaseOrder(PurchaseOrderEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                EnsurePurchaseOrderRows(vm);
                await PopulatePurchaseOrderOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderPurchaseOrderEditor(vm, isCreate: true);
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
                SetSuccessMessage("PurchaseOrderCreatedMessage");
                return RedirectOrHtmx(nameof(EditPurchaseOrder), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsurePurchaseOrderRows(vm);
                await PopulatePurchaseOrderOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderPurchaseOrderEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditPurchaseOrder(Guid id, CancellationToken ct = default)
        {
            var dto = await _getPurchaseOrderForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("PurchaseOrderNotFoundMessage");
                return RedirectOrHtmx(nameof(PurchaseOrders), new { });
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
            return RenderPurchaseOrderEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPurchaseOrder(PurchaseOrderEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                EnsurePurchaseOrderRows(vm);
                await PopulatePurchaseOrderOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderPurchaseOrderEditor(vm, isCreate: false);
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
                SetSuccessMessage("PurchaseOrderUpdatedMessage");
                return RedirectOrHtmx(nameof(EditPurchaseOrder), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("PurchaseOrderConcurrencyMessage");
                return RedirectOrHtmx(nameof(EditPurchaseOrder), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsurePurchaseOrderRows(vm);
                await PopulatePurchaseOrderOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderPurchaseOrderEditor(vm, isCreate: false);
            }
        }

        /// <summary>
        /// Paged ledger for a single variant.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VariantLedger(Guid variantId, Guid? warehouseId = null, int page = 1, int pageSize = 20, InventoryLedgerQueueFilter filter = InventoryLedgerQueueFilter.All, CancellationToken ct = default)
        {
            var dto = await _getLedger.HandleAsync(variantId, page, pageSize, warehouseId, filter, ct).ConfigureAwait(false);
            var summary = await _getLedger.GetSummaryAsync(variantId, warehouseId, ct).ConfigureAwait(false);
            var vm = new InventoryLedgerListVm
            {
                VariantId = variantId,
                WarehouseId = warehouseId,
                Filter = filter,
                FilterItems = BuildInventoryLedgerFilterItems(filter),
                Summary = new InventoryLedgerOpsSummaryVm
                {
                    TotalCount = summary.TotalCount,
                    InboundCount = summary.InboundCount,
                    OutboundCount = summary.OutboundCount,
                    ReservationCount = summary.ReservationCount
                },
                Playbooks = BuildInventoryLedgerPlaybooks(),
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

            return RenderVariantLedgerWorkspace(vm);
        }

        private static IEnumerable<SelectListItem> BuildInventoryLedgerFilterItems(InventoryLedgerQueueFilter selectedFilter)
        {
            yield return new SelectListItem("All ledger entries", InventoryLedgerQueueFilter.All.ToString(), selectedFilter == InventoryLedgerQueueFilter.All);
            yield return new SelectListItem("Inbound", InventoryLedgerQueueFilter.Inbound.ToString(), selectedFilter == InventoryLedgerQueueFilter.Inbound);
            yield return new SelectListItem("Outbound", InventoryLedgerQueueFilter.Outbound.ToString(), selectedFilter == InventoryLedgerQueueFilter.Outbound);
            yield return new SelectListItem("Reservations", InventoryLedgerQueueFilter.Reservations.ToString(), selectedFilter == InventoryLedgerQueueFilter.Reservations);
        }

        private static List<InventoryOpsPlaybookVm> BuildInventoryLedgerPlaybooks()
        {
            return new List<InventoryOpsPlaybookVm>
            {
                new()
                {
                    Title = "Inbound review",
                    ScopeNote = "Inbound rows usually represent receipts, returns, or positive stock corrections that affect future availability.",
                    OperatorAction = "Review reference ids and reason labels before assuming the increase is final, especially after supplier receipts or return handling."
                },
                new()
                {
                    Title = "Outbound and reservation review",
                    ScopeNote = "Outbound and reservation-heavy ledger history often explains low-stock alerts and order allocation pressure.",
                    OperatorAction = "Compare reservation spikes with order and stock-level workflows before manually adjusting inventory so troubleshooting stays traceable."
                }
            };
        }

        private static List<InventoryOpsPlaybookVm> BuildStockTransferPlaybooks()
        {
            return new List<InventoryOpsPlaybookVm>
            {
                new()
                {
                    Title = "Draft transfer review",
                    ScopeNote = "Draft transfers usually represent internal stock moves that still need warehouse confirmation before goods leave the source location.",
                    OperatorAction = "Check warehouse ownership, line counts, and destination readiness before moving draft transfers into active handoff."
                },
                new()
                {
                    Title = "In-transit follow-up",
                    ScopeNote = "In-transit transfers affect availability at both ends and are the first place to review when warehouse counts look inconsistent.",
                    OperatorAction = "Compare transfer age with receiving activity and reconcile any warehouse-level stock mismatch before manual adjustments."
                }
            };
        }

        private static List<InventoryOpsPlaybookVm> BuildPurchaseOrderPlaybooks()
        {
            return new List<InventoryOpsPlaybookVm>
            {
                new()
                {
                    Title = "Draft order cleanup",
                    ScopeNote = "Draft purchase orders often indicate supplier planning work that is not yet committed and can create confusion during replenishment reviews.",
                    OperatorAction = "Review old draft orders first, confirm supplier intent, and either issue or tidy them up before opening new replenishment cycles."
                },
                new()
                {
                    Title = "Issued and received follow-up",
                    ScopeNote = "Issued orders should move toward receipt, while received orders should align with inbound stock and supplier accounting.",
                    OperatorAction = "Prioritize issued orders that have been open too long, then verify received orders against stock receipts and supplier records."
                }
            };
        }

        private static List<InventoryOpsPlaybookVm> BuildWarehousePlaybooks()
        {
            return new List<InventoryOpsPlaybookVm>
            {
                new()
                {
                    Title = "Default warehouse review",
                    ScopeNote = "Default warehouses act as the most common fallback for stock and transfer workflows and deserve extra review before operations scale.",
                    OperatorAction = "Confirm the default location still matches the business's active fulfillment path before onboarding more stock or staff."
                },
                new()
                {
                    Title = "Empty warehouse cleanup",
                    ScopeNote = "Warehouses without stock levels are often setup leftovers or incomplete go-live steps that can confuse replenishment workflows.",
                    OperatorAction = "Review empty warehouses and either seed stock levels, repurpose the location, or archive the operational intent through naming and notes."
                }
            };
        }

        private static List<InventoryOpsPlaybookVm> BuildSupplierPlaybooks()
        {
            return new List<InventoryOpsPlaybookVm>
            {
                new()
                {
                    Title = "Supplier contact hygiene",
                    ScopeNote = "Suppliers missing address data create friction for billing, returns, and manual procurement follow-up.",
                    OperatorAction = "Prioritize missing-address suppliers before adding more purchase orders so procurement and finance have enough fallback context."
                },
                new()
                {
                    Title = "Active supplier review",
                    ScopeNote = "Suppliers with purchase-order history are the first place to review when replenishment or receipt issues need business context.",
                    OperatorAction = "Use purchase-order-linked suppliers as the primary queue for follow-up on delivery delays, cost mismatches, or receipt reconciliation."
                }
            };
        }

        private async Task PopulateStockLevelOptionsAsync(StockLevelEditVm vm, Guid? businessId, CancellationToken ct)
        {
            var resolvedBusinessId = businessId ?? await _referenceData.ResolveBusinessIdAsync(null, ct).ConfigureAwait(false);
            vm.WarehouseOptions = await _referenceData.GetWarehouseOptionsAsync(vm.WarehouseId, resolvedBusinessId, ct).ConfigureAwait(false);
            vm.VariantOptions = await _referenceData.GetVariantOptionsAsync(vm.ProductVariantId, ct).ConfigureAwait(false);
        }

        private async Task PopulateInventoryStockActionOptionsAsync(InventoryStockActionVm vm, CancellationToken ct)
        {
            var resolvedBusinessId = vm.BusinessId ?? await _referenceData.ResolveBusinessIdAsync(null, ct).ConfigureAwait(false);
            vm.WarehouseOptions = await _referenceData.GetWarehouseOptionsAsync(vm.WarehouseId, resolvedBusinessId, ct).ConfigureAwait(false);
            vm.VariantOptions = await _referenceData.GetVariantOptionsAsync(vm.ProductVariantId, ct).ConfigureAwait(false);
        }

        private async Task<InventoryAdjustActionVm?> BuildInventoryAdjustActionVmAsync(Guid stockLevelId, Guid? businessId, CancellationToken ct)
        {
            var dto = await _getStockLevelForEdit.HandleAsync(stockLevelId, ct).ConfigureAwait(false);
            if (dto is null)
            {
                return null;
            }

            var vm = new InventoryAdjustActionVm
            {
                StockLevelId = dto.Id,
                BusinessId = businessId,
                WarehouseId = dto.WarehouseId,
                ProductVariantId = dto.ProductVariantId,
                AvailableQuantity = dto.AvailableQuantity,
                ReservedQuantity = dto.ReservedQuantity
            };

            await PopulateInventoryStockActionOptionsAsync(vm, ct).ConfigureAwait(false);
            return vm;
        }

        private async Task<InventoryReserveActionVm?> BuildInventoryReserveActionVmAsync(Guid stockLevelId, Guid? businessId, CancellationToken ct)
        {
            var dto = await _getStockLevelForEdit.HandleAsync(stockLevelId, ct).ConfigureAwait(false);
            if (dto is null)
            {
                return null;
            }

            var vm = new InventoryReserveActionVm
            {
                StockLevelId = dto.Id,
                BusinessId = businessId,
                WarehouseId = dto.WarehouseId,
                ProductVariantId = dto.ProductVariantId,
                AvailableQuantity = dto.AvailableQuantity,
                ReservedQuantity = dto.ReservedQuantity
            };

            await PopulateInventoryStockActionOptionsAsync(vm, ct).ConfigureAwait(false);
            return vm;
        }

        private async Task<InventoryReleaseReservationActionVm?> BuildInventoryReleaseActionVmAsync(Guid stockLevelId, Guid? businessId, CancellationToken ct)
        {
            var dto = await _getStockLevelForEdit.HandleAsync(stockLevelId, ct).ConfigureAwait(false);
            if (dto is null)
            {
                return null;
            }

            var vm = new InventoryReleaseReservationActionVm
            {
                StockLevelId = dto.Id,
                BusinessId = businessId,
                WarehouseId = dto.WarehouseId,
                ProductVariantId = dto.ProductVariantId,
                AvailableQuantity = dto.AvailableQuantity,
                ReservedQuantity = dto.ReservedQuantity,
                Quantity = dto.ReservedQuantity > 0 ? dto.ReservedQuantity : 1
            };

            await PopulateInventoryStockActionOptionsAsync(vm, ct).ConfigureAwait(false);
            return vm;
        }

        private async Task<InventoryReturnReceiptActionVm?> BuildInventoryReturnReceiptActionVmAsync(Guid stockLevelId, Guid? businessId, CancellationToken ct)
        {
            var dto = await _getStockLevelForEdit.HandleAsync(stockLevelId, ct).ConfigureAwait(false);
            if (dto is null)
            {
                return null;
            }

            var vm = new InventoryReturnReceiptActionVm
            {
                StockLevelId = dto.Id,
                BusinessId = businessId,
                WarehouseId = dto.WarehouseId,
                ProductVariantId = dto.ProductVariantId,
                AvailableQuantity = dto.AvailableQuantity,
                ReservedQuantity = dto.ReservedQuantity
            };

            await PopulateInventoryStockActionOptionsAsync(vm, ct).ConfigureAwait(false);
            return vm;
        }

        private static IEnumerable<SelectListItem> BuildStockLevelFilterItems(StockLevelQueueFilter selectedFilter)
        {
            yield return new SelectListItem("All stock levels", StockLevelQueueFilter.All.ToString(), selectedFilter == StockLevelQueueFilter.All);
            yield return new SelectListItem("Low stock", StockLevelQueueFilter.LowStock.ToString(), selectedFilter == StockLevelQueueFilter.LowStock);
            yield return new SelectListItem("Reserved", StockLevelQueueFilter.Reserved.ToString(), selectedFilter == StockLevelQueueFilter.Reserved);
            yield return new SelectListItem("In transit", StockLevelQueueFilter.InTransit.ToString(), selectedFilter == StockLevelQueueFilter.InTransit);
        }

        private static IEnumerable<SelectListItem> BuildPurchaseOrderFilterItems(PurchaseOrderQueueFilter selectedFilter)
        {
            yield return new SelectListItem("All purchase orders", PurchaseOrderQueueFilter.All.ToString(), selectedFilter == PurchaseOrderQueueFilter.All);
            yield return new SelectListItem("Draft", PurchaseOrderQueueFilter.Draft.ToString(), selectedFilter == PurchaseOrderQueueFilter.Draft);
            yield return new SelectListItem("Issued", PurchaseOrderQueueFilter.Issued.ToString(), selectedFilter == PurchaseOrderQueueFilter.Issued);
            yield return new SelectListItem("Received", PurchaseOrderQueueFilter.Received.ToString(), selectedFilter == PurchaseOrderQueueFilter.Received);
        }

        private static IEnumerable<SelectListItem> BuildStockTransferFilterItems(StockTransferQueueFilter selectedFilter)
        {
            yield return new SelectListItem("All transfers", StockTransferQueueFilter.All.ToString(), selectedFilter == StockTransferQueueFilter.All);
            yield return new SelectListItem("Draft", StockTransferQueueFilter.Draft.ToString(), selectedFilter == StockTransferQueueFilter.Draft);
            yield return new SelectListItem("In transit", StockTransferQueueFilter.InTransit.ToString(), selectedFilter == StockTransferQueueFilter.InTransit);
            yield return new SelectListItem("Completed", StockTransferQueueFilter.Completed.ToString(), selectedFilter == StockTransferQueueFilter.Completed);
        }

        private static IEnumerable<SelectListItem> BuildWarehouseFilterItems(WarehouseQueueFilter selectedFilter)
        {
            yield return new SelectListItem("All warehouses", WarehouseQueueFilter.All.ToString(), selectedFilter == WarehouseQueueFilter.All);
            yield return new SelectListItem("Default", WarehouseQueueFilter.Default.ToString(), selectedFilter == WarehouseQueueFilter.Default);
            yield return new SelectListItem("No stock levels", WarehouseQueueFilter.NoStockLevels.ToString(), selectedFilter == WarehouseQueueFilter.NoStockLevels);
        }

        private static IEnumerable<SelectListItem> BuildSupplierFilterItems(SupplierQueueFilter selectedFilter)
        {
            yield return new SelectListItem("All suppliers", SupplierQueueFilter.All.ToString(), selectedFilter == SupplierQueueFilter.All);
            yield return new SelectListItem("Missing address", SupplierQueueFilter.MissingAddress.ToString(), selectedFilter == SupplierQueueFilter.MissingAddress);
            yield return new SelectListItem("Has purchase orders", SupplierQueueFilter.HasPurchaseOrders.ToString(), selectedFilter == SupplierQueueFilter.HasPurchaseOrders);
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

        private IActionResult RenderWarehousesWorkspace(WarehousesListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/Warehouses.cshtml", vm);
            }

            return View("Warehouses", vm);
        }

        private IActionResult RenderSuppliersWorkspace(SuppliersListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/Suppliers.cshtml", vm);
            }

            return View("Suppliers", vm);
        }

        private IActionResult RenderStockLevelsWorkspace(StockLevelsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/StockLevels.cshtml", vm);
            }

            return View("StockLevels", vm);
        }

        private IActionResult RenderStockTransfersWorkspace(StockTransfersListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/StockTransfers.cshtml", vm);
            }

            return View("StockTransfers", vm);
        }

        private IActionResult RenderPurchaseOrdersWorkspace(PurchaseOrdersListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/PurchaseOrders.cshtml", vm);
            }

            return View("PurchaseOrders", vm);
        }

        private IActionResult RenderVariantLedgerWorkspace(InventoryLedgerListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/VariantLedger.cshtml", vm);
            }

            return View("VariantLedger", vm);
        }

        private IActionResult RenderWarehouseEditor(WarehouseEditVm vm, bool isCreate)
        {
            if (IsHtmxRequest())
            {
                ViewData["IsCreate"] = isCreate;
                return PartialView("~/Views/Inventory/_WarehouseEditorShell.cshtml", vm);
            }

            return isCreate ? View("CreateWarehouse", vm) : View("EditWarehouse", vm);
        }

        private IActionResult RenderStockLevelEditor(StockLevelEditVm vm, bool isCreate)
        {
            if (IsHtmxRequest())
            {
                ViewData["IsCreate"] = isCreate;
                return PartialView("~/Views/Inventory/_StockLevelEditorShell.cshtml", vm);
            }

            return isCreate ? View("CreateStockLevel", vm) : View("EditStockLevel", vm);
        }

        private IActionResult RenderAdjustStockEditor(InventoryAdjustActionVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/_AdjustStockEditorShell.cshtml", vm);
            }

            return View("AdjustStock", vm);
        }

        private IActionResult RenderReserveStockEditor(InventoryReserveActionVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/_ReserveStockEditorShell.cshtml", vm);
            }

            return View("ReserveStock", vm);
        }

        private IActionResult RenderReleaseReservationEditor(InventoryReleaseReservationActionVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/_ReleaseReservationEditorShell.cshtml", vm);
            }

            return View("ReleaseReservation", vm);
        }

        private IActionResult RenderReturnReceiptEditor(InventoryReturnReceiptActionVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/_ReturnReceiptEditorShell.cshtml", vm);
            }

            return View("ReturnReceipt", vm);
        }

        private IActionResult RenderSupplierEditor(SupplierEditVm vm, bool isCreate)
        {
            if (IsHtmxRequest())
            {
                ViewData["IsCreate"] = isCreate;
                return PartialView("~/Views/Inventory/_SupplierEditorShell.cshtml", vm);
            }

            return isCreate ? View("CreateSupplier", vm) : View("EditSupplier", vm);
        }

        private IActionResult RenderStockTransferEditor(StockTransferEditVm vm, bool isCreate)
        {
            if (IsHtmxRequest())
            {
                ViewData["IsCreate"] = isCreate;
                return PartialView("~/Views/Inventory/_StockTransferEditorShell.cshtml", vm);
            }

            return isCreate ? View("CreateStockTransfer", vm) : View("EditStockTransfer", vm);
        }

        private IActionResult RenderPurchaseOrderEditor(PurchaseOrderEditVm vm, bool isCreate)
        {
            if (IsHtmxRequest())
            {
                ViewData["IsCreate"] = isCreate;
                return PartialView("~/Views/Inventory/_PurchaseOrderEditorShell.cshtml", vm);
            }

            return isCreate ? View("CreatePurchaseOrder", vm) : View("EditPurchaseOrder", vm);
        }

        private IActionResult RedirectOrHtmx(string actionName, object routeValues)
        {
            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = Url.Action(actionName, routeValues) ?? string.Empty;
                return new EmptyResult();
            }

            return RedirectToAction(actionName, routeValues);
        }

        private bool IsHtmxRequest()
        {
            return string.Equals(Request.Headers["HX-Request"], "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
