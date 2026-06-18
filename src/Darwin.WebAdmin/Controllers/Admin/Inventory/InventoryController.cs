using Darwin.Application.Abstractions.Services;
using Darwin.Application.Inventory.Commands;
using Darwin.Application.Inventory.DTOs;
using Darwin.Application.Inventory.Queries;
using Darwin.Domain.Enums;
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
        private readonly GetWarehouseLocationsPageHandler _getWarehouseLocationsPage;
        private readonly GetWarehouseLocationDetailHandler _getWarehouseLocationDetail;
        private readonly GetWarehouseLocationTreeHandler _getWarehouseLocationTree;
        private readonly CreateWarehouseLocationHandler _createWarehouseLocation;
        private readonly UpdateWarehouseLocationHandler _updateWarehouseLocation;
        private readonly ArchiveWarehouseLocationHandler _archiveWarehouseLocation;
        private readonly GetWarehouseLabelTemplatesPageHandler _getWarehouseLabelTemplatesPage;
        private readonly GetWarehouseLabelTemplateDetailHandler _getWarehouseLabelTemplateDetail;
        private readonly RenderWarehouseLocationLabelsHandler _renderWarehouseLocationLabels;
        private readonly CreateWarehouseLabelTemplateHandler _createWarehouseLabelTemplate;
        private readonly UpdateWarehouseLabelTemplateHandler _updateWarehouseLabelTemplate;
        private readonly ArchiveWarehouseLabelTemplateHandler _archiveWarehouseLabelTemplate;
        private readonly GetWarehouseTasksPageHandler _getWarehouseTasksPage;
        private readonly GetWarehouseTaskDetailHandler _getWarehouseTaskDetail;
        private readonly CreateWarehouseTaskHandler _createWarehouseTask;
        private readonly UpdateWarehouseTaskHandler _updateWarehouseTask;
        private readonly UpdateWarehouseTaskLifecycleHandler _updateWarehouseTaskLifecycle;
        private readonly GetStockCountsPageHandler _getStockCountsPage;
        private readonly GetStockCountDetailHandler _getStockCountDetail;
        private readonly CreateStockCountHandler _createStockCount;
        private readonly UpdateStockCountHandler _updateStockCount;
        private readonly UpdateStockCountLifecycleHandler _updateStockCountLifecycle;
        private readonly CreateWarehouseReceivingTaskFromGoodsReceiptHandler _createWarehouseReceivingTaskFromGoodsReceipt;
        private readonly CreateWarehousePutawayTaskFromGoodsReceiptHandler _createWarehousePutawayTaskFromGoodsReceipt;
        private readonly CreateWarehousePickingTaskFromOrderHandler _createWarehousePickingTaskFromOrder;
        private readonly GetSuppliersPageHandler _getSuppliersPage;
        private readonly GetSupplierForEditHandler _getSupplierForEdit;
        private readonly CreateSupplierHandler _createSupplier;
        private readonly UpdateSupplierHandler _updateSupplier;
        private readonly CreateSupplierContactHandler _createSupplierContact;
        private readonly UpdateSupplierContactHandler _updateSupplierContact;
        private readonly ArchiveSupplierContactHandler _archiveSupplierContact;
        private readonly RegisterSupplierDocumentHandler _registerSupplierDocument;
        private readonly GetStockLevelsPageHandler _getStockLevelsPage;
        private readonly GetBinStockDerivationHandler _getBinStockDerivation;
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
        private readonly UpdateStockTransferLifecycleHandler _updateStockTransferLifecycle;
        private readonly GetPurchaseOrdersPageHandler _getPurchaseOrdersPage;
        private readonly GetPurchaseOrderForEditHandler _getPurchaseOrderForEdit;
        private readonly CreatePurchaseOrderHandler _createPurchaseOrder;
        private readonly UpdatePurchaseOrderHandler _updatePurchaseOrder;
        private readonly UpdatePurchaseOrderLifecycleHandler _updatePurchaseOrderLifecycle;
        private readonly GetGoodsReceiptsPageHandler _getGoodsReceiptsPage;
        private readonly GetGoodsReceiptDetailHandler _getGoodsReceiptDetail;
        private readonly CreateGoodsReceiptFromPurchaseOrderHandler _createGoodsReceipt;
        private readonly CreateGoodsReceiptInlineIdentityHandler _createGoodsReceiptInlineIdentity;
        private readonly UpdateGoodsReceiptLifecycleHandler _updateGoodsReceiptLifecycle;
        private readonly GetInventoryLotsPageHandler _getInventoryLotsPage;
        private readonly GetInventorySerialUnitsPageHandler _getInventorySerialUnitsPage;
        private readonly GetHandlingUnitsPageHandler _getHandlingUnitsPage;
        private readonly GetInventoryLedgerHandler _getLedger;
        private readonly AdminReferenceDataService _referenceData;
        private readonly IClock _clock;

        public InventoryController(
            GetWarehousesPageHandler getWarehousesPage,
            GetWarehouseForEditHandler getWarehouseForEdit,
            CreateWarehouseHandler createWarehouse,
            UpdateWarehouseHandler updateWarehouse,
            GetWarehouseLocationsPageHandler getWarehouseLocationsPage,
            GetWarehouseLocationDetailHandler getWarehouseLocationDetail,
            GetWarehouseLocationTreeHandler getWarehouseLocationTree,
            CreateWarehouseLocationHandler createWarehouseLocation,
            UpdateWarehouseLocationHandler updateWarehouseLocation,
            ArchiveWarehouseLocationHandler archiveWarehouseLocation,
            GetWarehouseLabelTemplatesPageHandler getWarehouseLabelTemplatesPage,
            GetWarehouseLabelTemplateDetailHandler getWarehouseLabelTemplateDetail,
            RenderWarehouseLocationLabelsHandler renderWarehouseLocationLabels,
            CreateWarehouseLabelTemplateHandler createWarehouseLabelTemplate,
            UpdateWarehouseLabelTemplateHandler updateWarehouseLabelTemplate,
            ArchiveWarehouseLabelTemplateHandler archiveWarehouseLabelTemplate,
            GetWarehouseTasksPageHandler getWarehouseTasksPage,
            GetWarehouseTaskDetailHandler getWarehouseTaskDetail,
            CreateWarehouseTaskHandler createWarehouseTask,
            UpdateWarehouseTaskHandler updateWarehouseTask,
            UpdateWarehouseTaskLifecycleHandler updateWarehouseTaskLifecycle,
            GetStockCountsPageHandler getStockCountsPage,
            GetStockCountDetailHandler getStockCountDetail,
            CreateStockCountHandler createStockCount,
            UpdateStockCountHandler updateStockCount,
            UpdateStockCountLifecycleHandler updateStockCountLifecycle,
            CreateWarehouseReceivingTaskFromGoodsReceiptHandler createWarehouseReceivingTaskFromGoodsReceipt,
            CreateWarehousePutawayTaskFromGoodsReceiptHandler createWarehousePutawayTaskFromGoodsReceipt,
            CreateWarehousePickingTaskFromOrderHandler createWarehousePickingTaskFromOrder,
            GetSuppliersPageHandler getSuppliersPage,
            GetSupplierForEditHandler getSupplierForEdit,
            CreateSupplierHandler createSupplier,
            UpdateSupplierHandler updateSupplier,
            CreateSupplierContactHandler createSupplierContact,
            UpdateSupplierContactHandler updateSupplierContact,
            ArchiveSupplierContactHandler archiveSupplierContact,
            RegisterSupplierDocumentHandler registerSupplierDocument,
            GetStockLevelsPageHandler getStockLevelsPage,
            GetBinStockDerivationHandler getBinStockDerivation,
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
            UpdateStockTransferLifecycleHandler updateStockTransferLifecycle,
            GetPurchaseOrdersPageHandler getPurchaseOrdersPage,
            GetPurchaseOrderForEditHandler getPurchaseOrderForEdit,
            CreatePurchaseOrderHandler createPurchaseOrder,
            UpdatePurchaseOrderHandler updatePurchaseOrder,
            UpdatePurchaseOrderLifecycleHandler updatePurchaseOrderLifecycle,
            GetGoodsReceiptsPageHandler getGoodsReceiptsPage,
            GetGoodsReceiptDetailHandler getGoodsReceiptDetail,
            CreateGoodsReceiptFromPurchaseOrderHandler createGoodsReceipt,
            CreateGoodsReceiptInlineIdentityHandler createGoodsReceiptInlineIdentity,
            UpdateGoodsReceiptLifecycleHandler updateGoodsReceiptLifecycle,
            GetInventoryLotsPageHandler getInventoryLotsPage,
            GetInventorySerialUnitsPageHandler getInventorySerialUnitsPage,
            GetHandlingUnitsPageHandler getHandlingUnitsPage,
            GetInventoryLedgerHandler getLedger,
            AdminReferenceDataService referenceData,
            IClock clock)
        {
            _getWarehousesPage = getWarehousesPage ?? throw new ArgumentNullException(nameof(getWarehousesPage));
            _getWarehouseForEdit = getWarehouseForEdit ?? throw new ArgumentNullException(nameof(getWarehouseForEdit));
            _createWarehouse = createWarehouse ?? throw new ArgumentNullException(nameof(createWarehouse));
            _updateWarehouse = updateWarehouse ?? throw new ArgumentNullException(nameof(updateWarehouse));
            _getWarehouseLocationsPage = getWarehouseLocationsPage ?? throw new ArgumentNullException(nameof(getWarehouseLocationsPage));
            _getWarehouseLocationDetail = getWarehouseLocationDetail ?? throw new ArgumentNullException(nameof(getWarehouseLocationDetail));
            _getWarehouseLocationTree = getWarehouseLocationTree ?? throw new ArgumentNullException(nameof(getWarehouseLocationTree));
            _createWarehouseLocation = createWarehouseLocation ?? throw new ArgumentNullException(nameof(createWarehouseLocation));
            _updateWarehouseLocation = updateWarehouseLocation ?? throw new ArgumentNullException(nameof(updateWarehouseLocation));
            _archiveWarehouseLocation = archiveWarehouseLocation ?? throw new ArgumentNullException(nameof(archiveWarehouseLocation));
            _getWarehouseLabelTemplatesPage = getWarehouseLabelTemplatesPage ?? throw new ArgumentNullException(nameof(getWarehouseLabelTemplatesPage));
            _getWarehouseLabelTemplateDetail = getWarehouseLabelTemplateDetail ?? throw new ArgumentNullException(nameof(getWarehouseLabelTemplateDetail));
            _renderWarehouseLocationLabels = renderWarehouseLocationLabels ?? throw new ArgumentNullException(nameof(renderWarehouseLocationLabels));
            _createWarehouseLabelTemplate = createWarehouseLabelTemplate ?? throw new ArgumentNullException(nameof(createWarehouseLabelTemplate));
            _updateWarehouseLabelTemplate = updateWarehouseLabelTemplate ?? throw new ArgumentNullException(nameof(updateWarehouseLabelTemplate));
            _archiveWarehouseLabelTemplate = archiveWarehouseLabelTemplate ?? throw new ArgumentNullException(nameof(archiveWarehouseLabelTemplate));
            _getWarehouseTasksPage = getWarehouseTasksPage ?? throw new ArgumentNullException(nameof(getWarehouseTasksPage));
            _getWarehouseTaskDetail = getWarehouseTaskDetail ?? throw new ArgumentNullException(nameof(getWarehouseTaskDetail));
            _createWarehouseTask = createWarehouseTask ?? throw new ArgumentNullException(nameof(createWarehouseTask));
            _updateWarehouseTask = updateWarehouseTask ?? throw new ArgumentNullException(nameof(updateWarehouseTask));
            _updateWarehouseTaskLifecycle = updateWarehouseTaskLifecycle ?? throw new ArgumentNullException(nameof(updateWarehouseTaskLifecycle));
            _getStockCountsPage = getStockCountsPage ?? throw new ArgumentNullException(nameof(getStockCountsPage));
            _getStockCountDetail = getStockCountDetail ?? throw new ArgumentNullException(nameof(getStockCountDetail));
            _createStockCount = createStockCount ?? throw new ArgumentNullException(nameof(createStockCount));
            _updateStockCount = updateStockCount ?? throw new ArgumentNullException(nameof(updateStockCount));
            _updateStockCountLifecycle = updateStockCountLifecycle ?? throw new ArgumentNullException(nameof(updateStockCountLifecycle));
            _createWarehouseReceivingTaskFromGoodsReceipt = createWarehouseReceivingTaskFromGoodsReceipt ?? throw new ArgumentNullException(nameof(createWarehouseReceivingTaskFromGoodsReceipt));
            _createWarehousePutawayTaskFromGoodsReceipt = createWarehousePutawayTaskFromGoodsReceipt ?? throw new ArgumentNullException(nameof(createWarehousePutawayTaskFromGoodsReceipt));
            _createWarehousePickingTaskFromOrder = createWarehousePickingTaskFromOrder ?? throw new ArgumentNullException(nameof(createWarehousePickingTaskFromOrder));
            _getSuppliersPage = getSuppliersPage ?? throw new ArgumentNullException(nameof(getSuppliersPage));
            _getSupplierForEdit = getSupplierForEdit ?? throw new ArgumentNullException(nameof(getSupplierForEdit));
            _createSupplier = createSupplier ?? throw new ArgumentNullException(nameof(createSupplier));
            _updateSupplier = updateSupplier ?? throw new ArgumentNullException(nameof(updateSupplier));
            _createSupplierContact = createSupplierContact ?? throw new ArgumentNullException(nameof(createSupplierContact));
            _updateSupplierContact = updateSupplierContact ?? throw new ArgumentNullException(nameof(updateSupplierContact));
            _archiveSupplierContact = archiveSupplierContact ?? throw new ArgumentNullException(nameof(archiveSupplierContact));
            _registerSupplierDocument = registerSupplierDocument ?? throw new ArgumentNullException(nameof(registerSupplierDocument));
            _getStockLevelsPage = getStockLevelsPage ?? throw new ArgumentNullException(nameof(getStockLevelsPage));
            _getBinStockDerivation = getBinStockDerivation ?? throw new ArgumentNullException(nameof(getBinStockDerivation));
            _getStockLevelForEdit = getStockLevelForEdit ?? throw new ArgumentNullException(nameof(getStockLevelForEdit));
            _createStockLevel = createStockLevel ?? throw new ArgumentNullException(nameof(createStockLevel));
            _updateStockLevel = updateStockLevel ?? throw new ArgumentNullException(nameof(updateStockLevel));
            _adjustInventory = adjustInventory ?? throw new ArgumentNullException(nameof(adjustInventory));
            _reserveInventory = reserveInventory ?? throw new ArgumentNullException(nameof(reserveInventory));
            _releaseInventoryReservation = releaseInventoryReservation ?? throw new ArgumentNullException(nameof(releaseInventoryReservation));
            _processReturnReceipt = processReturnReceipt ?? throw new ArgumentNullException(nameof(processReturnReceipt));
            _getStockTransfersPage = getStockTransfersPage ?? throw new ArgumentNullException(nameof(getStockTransfersPage));
            _getStockTransferForEdit = getStockTransferForEdit ?? throw new ArgumentNullException(nameof(getStockTransferForEdit));
            _createStockTransfer = createStockTransfer ?? throw new ArgumentNullException(nameof(createStockTransfer));
            _updateStockTransfer = updateStockTransfer ?? throw new ArgumentNullException(nameof(updateStockTransfer));
            _updateStockTransferLifecycle = updateStockTransferLifecycle ?? throw new ArgumentNullException(nameof(updateStockTransferLifecycle));
            _getPurchaseOrdersPage = getPurchaseOrdersPage ?? throw new ArgumentNullException(nameof(getPurchaseOrdersPage));
            _getPurchaseOrderForEdit = getPurchaseOrderForEdit ?? throw new ArgumentNullException(nameof(getPurchaseOrderForEdit));
            _createPurchaseOrder = createPurchaseOrder ?? throw new ArgumentNullException(nameof(createPurchaseOrder));
            _updatePurchaseOrder = updatePurchaseOrder ?? throw new ArgumentNullException(nameof(updatePurchaseOrder));
            _updatePurchaseOrderLifecycle = updatePurchaseOrderLifecycle ?? throw new ArgumentNullException(nameof(updatePurchaseOrderLifecycle));
            _getGoodsReceiptsPage = getGoodsReceiptsPage ?? throw new ArgumentNullException(nameof(getGoodsReceiptsPage));
            _getGoodsReceiptDetail = getGoodsReceiptDetail ?? throw new ArgumentNullException(nameof(getGoodsReceiptDetail));
            _createGoodsReceipt = createGoodsReceipt ?? throw new ArgumentNullException(nameof(createGoodsReceipt));
            _createGoodsReceiptInlineIdentity = createGoodsReceiptInlineIdentity ?? throw new ArgumentNullException(nameof(createGoodsReceiptInlineIdentity));
            _updateGoodsReceiptLifecycle = updateGoodsReceiptLifecycle ?? throw new ArgumentNullException(nameof(updateGoodsReceiptLifecycle));
            _getInventoryLotsPage = getInventoryLotsPage ?? throw new ArgumentNullException(nameof(getInventoryLotsPage));
            _getInventorySerialUnitsPage = getInventorySerialUnitsPage ?? throw new ArgumentNullException(nameof(getInventorySerialUnitsPage));
            _getHandlingUnitsPage = getHandlingUnitsPage ?? throw new ArgumentNullException(nameof(getHandlingUnitsPage));
            _getLedger = getLedger ?? throw new ArgumentNullException(nameof(getLedger));
            _referenceData = referenceData ?? throw new ArgumentNullException(nameof(referenceData));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
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
            await PopulateWarehouseOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderWarehouseEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWarehouse(WarehouseEditVm vm, CancellationToken ct = default)
        {
            if (vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage("WarehouseCreateFailedMessage");
                return RedirectOrHtmx(nameof(Warehouses), new { });
            }

            if (!ModelState.IsValid)
            {
                await PopulateWarehouseOptionsAsync(vm, ct).ConfigureAwait(false);
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
            catch (Exception)
            {
                AddModelErrorMessage("WarehouseCreateFailedMessage");
                await PopulateWarehouseOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderWarehouseEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditWarehouse(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("WarehouseNotFoundMessage");
                return RedirectOrHtmx(nameof(Warehouses), new { });
            }

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
            await PopulateWarehouseOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderWarehouseEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWarehouse(WarehouseEditVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty || vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage(vm.Id == Guid.Empty ? "WarehouseNotFoundMessage" : "WarehouseUpdateFailedMessage");
                return RedirectOrHtmx(nameof(Warehouses), new { businessId = vm.BusinessId == Guid.Empty ? (Guid?)null : vm.BusinessId });
            }

            if (!ModelState.IsValid)
            {
                await PopulateWarehouseOptionsAsync(vm, ct).ConfigureAwait(false);
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
            catch (Exception)
            {
                AddModelErrorMessage("WarehouseUpdateFailedMessage");
                await PopulateWarehouseOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderWarehouseEditor(vm, isCreate: false);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Locations(Guid? businessId = null, Guid? warehouseId = null, int page = 1, int pageSize = 20, string? q = null, WarehouseLocationQueueFilter filter = WarehouseLocationQueueFilter.All, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);

            var items = new List<WarehouseLocationListItemVm>();
            var treeItems = new List<WarehouseLocationTreeItemVm>();
            var total = 0;
            var summary = new WarehouseLocationOpsSummaryVm();

            if (businessId.HasValue)
            {
                var result = await _getWarehouseLocationsPage.HandleAsync(businessId.Value, warehouseId, page, pageSize, q, filter, ct).ConfigureAwait(false);
                var summaryDto = await _getWarehouseLocationsPage.GetSummaryAsync(businessId.Value, warehouseId, ct).ConfigureAwait(false);
                var treeDto = await _getWarehouseLocationTree.HandleAsync(businessId.Value, warehouseId, ct).ConfigureAwait(false);

                items = result.Items.Select(MapWarehouseLocationItem).ToList();
                treeItems = treeDto.Select(MapWarehouseLocationTreeItem).ToList();
                total = result.Total;
                summary = new WarehouseLocationOpsSummaryVm
                {
                    TotalCount = summaryDto.TotalCount,
                    ActiveCount = summaryDto.ActiveCount,
                    BlockedCount = summaryDto.BlockedCount,
                    BinCount = summaryDto.BinCount,
                    DockCount = summaryDto.DockCount,
                    QualityHoldCount = summaryDto.QualityHoldCount
                };
            }

            var vm = new WarehouseLocationsListVm
            {
                BusinessId = businessId,
                WarehouseId = warehouseId,
                Query = q ?? string.Empty,
                Filter = filter,
                FilterItems = BuildWarehouseLocationFilterItems(filter),
                Summary = summary,
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                WarehouseOptions = await GetWarehouseOptionsAsync(warehouseId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items,
                TreeItems = treeItems
            };

            return RenderWarehouseLocationsWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateLocation(Guid? businessId = null, Guid? warehouseId = null, Guid? parentLocationId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var vm = new WarehouseLocationEditVm
            {
                BusinessId = businessId ?? Guid.Empty,
                WarehouseId = warehouseId ?? Guid.Empty,
                ParentLocationId = parentLocationId,
                Status = WarehouseLocationStatus.Active,
                LocationType = WarehouseLocationType.Bin
            };
            await PopulateWarehouseLocationOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderWarehouseLocationEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLocation(WarehouseLocationEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateWarehouseLocationOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderWarehouseLocationEditor(vm, isCreate: true);
            }

            try
            {
                var id = await _createWarehouseLocation.HandleAsync(new WarehouseLocationCreateDto
                {
                    BusinessId = vm.BusinessId,
                    WarehouseId = vm.WarehouseId,
                    ParentLocationId = vm.ParentLocationId,
                    Code = vm.Code,
                    DisplayName = vm.DisplayName,
                    LocationType = vm.LocationType,
                    Status = vm.Status,
                    Barcode = vm.Barcode,
                    SortOrder = vm.SortOrder,
                    Description = vm.Description,
                    MetadataJson = vm.MetadataJson
                }, ct).ConfigureAwait(false);
                SetSuccessMessage("WarehouseLocationCreatedMessage");
                return RedirectOrHtmx(nameof(EditLocation), new { id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("WarehouseLocationCreateFailedMessage");
                await PopulateWarehouseLocationOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderWarehouseLocationEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditLocation(Guid id, CancellationToken ct = default)
        {
            var dto = await _getWarehouseLocationDetail.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("WarehouseLocationNotFoundMessage");
                return RedirectOrHtmx(nameof(Locations), new { });
            }

            var vm = MapWarehouseLocationEditor(dto);
            await PopulateWarehouseLocationOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderWarehouseLocationEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLocation(WarehouseLocationEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateWarehouseLocationOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderWarehouseLocationEditor(vm, isCreate: false);
            }

            try
            {
                await _updateWarehouseLocation.HandleAsync(new WarehouseLocationEditDto
                {
                    Id = vm.Id,
                    RowVersion = vm.RowVersion,
                    BusinessId = vm.BusinessId,
                    WarehouseId = vm.WarehouseId,
                    ParentLocationId = vm.ParentLocationId,
                    Code = vm.Code,
                    DisplayName = vm.DisplayName,
                    LocationType = vm.LocationType,
                    Status = vm.Status,
                    Barcode = vm.Barcode,
                    SortOrder = vm.SortOrder,
                    Description = vm.Description,
                    MetadataJson = vm.MetadataJson
                }, ct).ConfigureAwait(false);
                SetSuccessMessage("WarehouseLocationUpdatedMessage");
                return RedirectOrHtmx(nameof(EditLocation), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("WarehouseLocationConcurrencyMessage");
                return RedirectOrHtmx(nameof(EditLocation), new { id = vm.Id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("WarehouseLocationUpdateFailedMessage");
                await PopulateWarehouseLocationOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderWarehouseLocationEditor(vm, isCreate: false);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveLocation(WarehouseLocationArchiveDto dto, CancellationToken ct = default)
        {
            var result = await _archiveWarehouseLocation.HandleAsync(dto, ct).ConfigureAwait(false);
            if (result.Succeeded)
            {
                SetSuccessMessage("WarehouseLocationArchivedMessage");
                return RedirectOrHtmx(nameof(Locations), new { });
            }

            SetErrorMessage(result.Error ?? "WarehouseLocationArchiveFailedMessage");
            return RedirectOrHtmx(nameof(EditLocation), new { id = dto.Id });
        }

        [HttpGet]
        public async Task<IActionResult> LabelTemplates(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, WarehouseLabelTemplateQueueFilter filter = WarehouseLabelTemplateQueueFilter.All, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = new List<WarehouseLabelTemplateListItemVm>();
            var summary = new WarehouseLabelTemplateOpsSummaryVm();
            var total = 0;
            if (businessId.HasValue)
            {
                var result = await _getWarehouseLabelTemplatesPage.HandleAsync(businessId.Value, page, pageSize, q, filter, ct).ConfigureAwait(false);
                var summaryDto = await _getWarehouseLabelTemplatesPage.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);
                items = result.Items.Select(MapWarehouseLabelTemplateItem).ToList();
                total = result.Total;
                summary = new WarehouseLabelTemplateOpsSummaryVm
                {
                    TotalCount = summaryDto.TotalCount,
                    ActiveCount = summaryDto.ActiveCount,
                    DefaultCount = summaryDto.DefaultCount
                };
            }

            var vm = new WarehouseLabelTemplatesListVm
            {
                BusinessId = businessId,
                Query = q ?? string.Empty,
                Filter = filter,
                FilterItems = BuildWarehouseLabelTemplateFilterItems(filter),
                Summary = summary,
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                Items = items,
                Page = page,
                PageSize = pageSize,
                Total = total
            };
            return RenderWarehouseLabelTemplatesWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateLabelTemplate(Guid? businessId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var vm = new WarehouseLabelTemplateEditVm { BusinessId = businessId ?? Guid.Empty };
            await PopulateWarehouseLabelTemplateOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderWarehouseLabelTemplateEditor(vm, true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLabelTemplate(WarehouseLabelTemplateEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateWarehouseLabelTemplateOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderWarehouseLabelTemplateEditor(vm, true);
            }

            try
            {
                var id = await _createWarehouseLabelTemplate.HandleAsync(MapWarehouseLabelTemplateCreateDto(vm), ct).ConfigureAwait(false);
                SetSuccessMessage("WarehouseLabelTemplateCreatedMessage");
                return RedirectOrHtmx(nameof(EditLabelTemplate), new { id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("WarehouseLabelTemplateCreateFailedMessage");
                await PopulateWarehouseLabelTemplateOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderWarehouseLabelTemplateEditor(vm, true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditLabelTemplate(Guid id, CancellationToken ct = default)
        {
            var dto = await _getWarehouseLabelTemplateDetail.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("WarehouseLabelTemplateNotFoundMessage");
                return RedirectOrHtmx(nameof(LabelTemplates), new { });
            }

            var vm = MapWarehouseLabelTemplateEditor(dto);
            await PopulateWarehouseLabelTemplateOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderWarehouseLabelTemplateEditor(vm, false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLabelTemplate(WarehouseLabelTemplateEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateWarehouseLabelTemplateOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderWarehouseLabelTemplateEditor(vm, false);
            }

            try
            {
                await _updateWarehouseLabelTemplate.HandleAsync(MapWarehouseLabelTemplateEditDto(vm), ct).ConfigureAwait(false);
                SetSuccessMessage("WarehouseLabelTemplateUpdatedMessage");
                return RedirectOrHtmx(nameof(EditLabelTemplate), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("WarehouseLabelTemplateConcurrencyMessage");
                return RedirectOrHtmx(nameof(EditLabelTemplate), new { id = vm.Id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("WarehouseLabelTemplateUpdateFailedMessage");
                await PopulateWarehouseLabelTemplateOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderWarehouseLabelTemplateEditor(vm, false);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveLabelTemplate(WarehouseLabelTemplateArchiveDto dto, CancellationToken ct = default)
        {
            var result = await _archiveWarehouseLabelTemplate.HandleAsync(dto, ct).ConfigureAwait(false);
            if (result.Succeeded)
            {
                SetSuccessMessage("WarehouseLabelTemplateArchivedMessage");
                return RedirectOrHtmx(nameof(LabelTemplates), new { });
            }

            SetErrorMessage(result.Error ?? "WarehouseLabelTemplateArchiveFailedMessage");
            return RedirectOrHtmx(nameof(EditLabelTemplate), new { id = dto.Id });
        }

        [HttpGet]
        public async Task<IActionResult> PrintLocationLabels(Guid businessId, Guid templateId, Guid[] locationIds, CancellationToken ct = default)
        {
            businessId = businessId == Guid.Empty ? await _referenceData.ResolveBusinessIdAsync(null, ct).ConfigureAwait(false) ?? Guid.Empty : businessId;
            var vm = await BuildWarehouseLocationLabelPrintVmAsync(businessId, templateId, locationIds, ct).ConfigureAwait(false);
            return View("PrintLocationLabels", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewLocationLabels(Guid businessId, Guid templateId, Guid[] locationIds, CancellationToken ct = default)
        {
            var vm = await BuildWarehouseLocationLabelPrintVmAsync(businessId, templateId, locationIds, ct).ConfigureAwait(false);
            return View("PrintLocationLabels", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadLocationLabels(Guid businessId, Guid templateId, Guid[] locationIds, CancellationToken ct = default)
        {
            var render = await _renderWarehouseLocationLabels.HandleAsync(businessId, templateId, locationIds, ct).ConfigureAwait(false);
            if (render is null)
            {
                SetErrorMessage("WarehouseLabelDownloadFailedMessage");
                return RedirectOrHtmx(nameof(Locations), new { businessId });
            }

            var content = string.Join(Environment.NewLine + "---" + Environment.NewLine, render.Labels.Select(x => x.RenderedContent));
            var fileName = $"warehouse-labels-{DateTime.UtcNow:yyyyMMddHHmmss}.txt";
            return File(System.Text.Encoding.UTF8.GetBytes(content), "text/plain; charset=utf-8", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> WarehouseTasks(Guid? businessId = null, Guid? warehouseId = null, int page = 1, int pageSize = 20, string? q = null, WarehouseTaskQueueFilter filter = WarehouseTaskQueueFilter.All, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = new List<WarehouseTaskListItemVm>();
            var total = 0;
            var summary = new WarehouseTaskOpsSummaryVm();
            if (businessId.HasValue)
            {
                var result = await _getWarehouseTasksPage.HandleAsync(businessId.Value, warehouseId, page, pageSize, q, filter, ct).ConfigureAwait(false);
                var summaryDto = await _getWarehouseTasksPage.GetSummaryAsync(businessId.Value, warehouseId, ct).ConfigureAwait(false);
                items = result.Items.Select(MapWarehouseTaskItem).ToList();
                total = result.Total;
                summary = new WarehouseTaskOpsSummaryVm
                {
                    TotalCount = summaryDto.TotalCount,
                    ReadyCount = summaryDto.ReadyCount,
                    AssignedCount = summaryDto.AssignedCount,
                    InProgressCount = summaryDto.InProgressCount,
                    NeedsAssignmentCount = summaryDto.NeedsAssignmentCount,
                    OverdueCount = summaryDto.OverdueCount,
                    ShortageCount = summaryDto.ShortageCount,
                    CompletedCount = summaryDto.CompletedCount,
                    CancelledCount = summaryDto.CancelledCount
                };
            }

            var vm = new WarehouseTasksListVm
            {
                BusinessId = businessId,
                WarehouseId = warehouseId,
                Query = q ?? string.Empty,
                Filter = filter,
                FilterItems = BuildWarehouseTaskFilterItems(filter),
                Summary = summary,
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                WarehouseOptions = await GetWarehouseOptionsAsync(warehouseId, ct).ConfigureAwait(false),
                LocationOptions = businessId.HasValue && warehouseId.HasValue
                    ? await GetWarehouseTaskLocationOptionsAsync(businessId.Value, warehouseId.Value, null, ct).ConfigureAwait(false)
                    : new List<SelectListItem>(),
                PickingTask = new WarehousePickingTaskCreateVm
                {
                    BusinessId = businessId ?? Guid.Empty,
                    WarehouseId = warehouseId ?? Guid.Empty,
                    Priority = WarehouseTaskPriority.Normal
                },
                Items = items,
                Page = page,
                PageSize = pageSize,
                Total = total
            };

            return RenderWarehouseTasksWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> WarehousePwa(Guid? businessId = null, Guid? warehouseId = null, string? q = null, WarehouseTaskQueueFilter taskFilter = WarehouseTaskQueueFilter.Ready, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            warehouseId = await _referenceData.ResolveWarehouseIdAsync(warehouseId, businessId, ct).ConfigureAwait(false);

            var tasks = new List<WarehouseTaskListItemVm>();
            var taskSummary = new WarehouseTaskOpsSummaryVm();
            if (businessId.HasValue)
            {
                var result = await _getWarehouseTasksPage.HandleAsync(businessId.Value, warehouseId, 1, 50, q, taskFilter, ct).ConfigureAwait(false);
                var summaryDto = await _getWarehouseTasksPage.GetSummaryAsync(businessId.Value, warehouseId, ct).ConfigureAwait(false);
                tasks = result.Items.Select(MapWarehouseTaskItem).ToList();
                taskSummary = new WarehouseTaskOpsSummaryVm
                {
                    TotalCount = summaryDto.TotalCount,
                    ReadyCount = summaryDto.ReadyCount,
                    AssignedCount = summaryDto.AssignedCount,
                    InProgressCount = summaryDto.InProgressCount,
                    NeedsAssignmentCount = summaryDto.NeedsAssignmentCount,
                    OverdueCount = summaryDto.OverdueCount,
                    ShortageCount = summaryDto.ShortageCount,
                    CompletedCount = summaryDto.CompletedCount,
                    CancelledCount = summaryDto.CancelledCount
                };
            }

            var binStock = await _getBinStockDerivation.HandleAsync(businessId, warehouseId, 1, 8, q, BinStockQueueFilter.WithAttention, ct).ConfigureAwait(false);
            var binSummary = await _getBinStockDerivation.GetSummaryAsync(businessId, warehouseId, q, ct).ConfigureAwait(false);
            var vm = new WarehousePwaVm
            {
                BusinessId = businessId,
                WarehouseId = warehouseId,
                Query = q ?? string.Empty,
                TaskFilter = taskFilter,
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                WarehouseOptions = await _referenceData.GetWarehouseOptionsAsync(warehouseId, businessId, ct).ConfigureAwait(false),
                TaskSummary = taskSummary,
                BinStockSummary = new BinStockOpsSummaryVm
                {
                    RowCount = binSummary.RowCount,
                    AssignedCount = binSummary.AssignedCount,
                    UnassignedCount = binSummary.UnassignedCount,
                    AttentionCount = binSummary.AttentionCount
                },
                Tasks = tasks,
                BinStockItems = binStock.Items.Select(x => new BinStockListItemVm
                {
                    WarehouseId = x.WarehouseId,
                    WarehouseName = x.WarehouseName,
                    ProductVariantId = x.ProductVariantId,
                    VariantSku = x.VariantSku,
                    LocationId = x.LocationId,
                    LocationCode = x.LocationCode,
                    LocationDisplayName = x.LocationDisplayName,
                    DerivedQuantity = x.DerivedQuantity,
                    AvailableQuantity = x.AvailableQuantity,
                    UnassignedQuantity = x.UnassignedQuantity,
                    HasAttention = x.HasAttention,
                    AttentionCode = x.AttentionCode,
                    Identities = x.Identities.Select(identity => new BinStockIdentityBreakdownVm
                    {
                        InventoryLotId = identity.InventoryLotId,
                        InventorySerialUnitId = identity.InventorySerialUnitId,
                        HandlingUnitId = identity.HandlingUnitId,
                        LotCode = identity.LotCode,
                        SerialNumber = identity.SerialNumber,
                        HandlingUnitCode = identity.HandlingUnitCode,
                        Quantity = identity.Quantity
                    }).ToList()
                }).ToList()
            };

            return RenderWarehousePwaWorkspace(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWarehousePwaTaskLifecycle(WarehouseTaskLifecycleActionDto dto, Guid? businessId = null, Guid? warehouseId = null, string? q = null, WarehouseTaskQueueFilter taskFilter = WarehouseTaskQueueFilter.Ready, CancellationToken ct = default)
        {
            var result = await _updateWarehouseTaskLifecycle.HandleAsync(dto, ct).ConfigureAwait(false);
            if (result.Succeeded)
            {
                SetSuccessMessage("WarehouseTaskLifecycleUpdatedMessage");
            }
            else
            {
                SetErrorMessage(result.Error ?? "WarehouseTaskLifecycleFailedMessage");
            }

            return RedirectOrHtmx(nameof(WarehousePwa), new { businessId, warehouseId, q, taskFilter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePickingTaskFromOrder(WarehousePickingTaskCreateVm vm, CancellationToken ct = default)
        {
            if (vm.BusinessId == Guid.Empty || vm.WarehouseId == Guid.Empty || vm.OrderId == Guid.Empty)
            {
                SetErrorMessage("WarehouseTaskCreateFailedMessage");
                return RedirectOrHtmx(nameof(WarehouseTasks), new { businessId = vm.BusinessId == Guid.Empty ? (Guid?)null : vm.BusinessId, warehouseId = vm.WarehouseId == Guid.Empty ? (Guid?)null : vm.WarehouseId });
            }

            var result = await _createWarehousePickingTaskFromOrder.HandleAsync(new CreateWarehousePickingTaskFromOrderDto
            {
                BusinessId = vm.BusinessId,
                WarehouseId = vm.WarehouseId,
                OrderId = vm.OrderId,
                FromLocationId = vm.FromLocationId,
                AssignedToUserId = vm.AssignedToUserId,
                Priority = vm.Priority,
                DueAtUtc = vm.DueAtUtc,
                InternalNotes = vm.InternalNotes
            }, ct).ConfigureAwait(false);

            if (result.Succeeded)
            {
                SetSuccessMessage("WarehouseTaskCreatedMessage");
                return RedirectOrHtmx(nameof(EditWarehouseTask), new { id = result.Value });
            }

            SetErrorMessage(result.Error ?? "WarehouseTaskCreateFailedMessage");
            return RedirectOrHtmx(nameof(WarehouseTasks), new { businessId = vm.BusinessId, warehouseId = vm.WarehouseId });
        }

        [HttpGet]
        public async Task<IActionResult> CreateWarehouseTask(Guid? businessId = null, Guid? warehouseId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var vm = new WarehouseTaskEditVm
            {
                BusinessId = businessId ?? Guid.Empty,
                WarehouseId = warehouseId ?? Guid.Empty,
                Status = WarehouseTaskStatus.Draft,
                Priority = WarehouseTaskPriority.Normal,
                TaskType = WarehouseTaskType.General,
                SourceType = WarehouseTaskSourceType.Manual,
                Lines = new List<WarehouseTaskLineVm> { new() { RequestedQuantity = 1, SortOrder = 1 } }
            };
            await PopulateWarehouseTaskOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderWarehouseTaskEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWarehouseTask(WarehouseTaskEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateWarehouseTaskOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderWarehouseTaskEditor(vm, isCreate: true);
            }

            try
            {
                var id = await _createWarehouseTask.HandleAsync(MapWarehouseTaskCreateDto(vm), ct).ConfigureAwait(false);
                SetSuccessMessage("WarehouseTaskCreatedMessage");
                return RedirectOrHtmx(nameof(EditWarehouseTask), new { id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("WarehouseTaskCreateFailedMessage");
                await PopulateWarehouseTaskOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderWarehouseTaskEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditWarehouseTask(Guid id, CancellationToken ct = default)
        {
            var dto = await _getWarehouseTaskDetail.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("WarehouseTaskNotFoundMessage");
                return RedirectOrHtmx(nameof(WarehouseTasks), new { });
            }

            var vm = MapWarehouseTaskEditor(dto);
            await PopulateWarehouseTaskOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderWarehouseTaskEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWarehouseTask(WarehouseTaskEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateWarehouseTaskOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderWarehouseTaskEditor(vm, isCreate: false);
            }

            try
            {
                await _updateWarehouseTask.HandleAsync(MapWarehouseTaskEditDto(vm), ct).ConfigureAwait(false);
                SetSuccessMessage("WarehouseTaskUpdatedMessage");
                return RedirectOrHtmx(nameof(EditWarehouseTask), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("WarehouseTaskConcurrencyMessage");
                return RedirectOrHtmx(nameof(EditWarehouseTask), new { id = vm.Id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("WarehouseTaskUpdateFailedMessage");
                await PopulateWarehouseTaskOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderWarehouseTaskEditor(vm, isCreate: false);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWarehouseTaskLifecycle(WarehouseTaskLifecycleActionDto dto, CancellationToken ct = default)
        {
            var result = await _updateWarehouseTaskLifecycle.HandleAsync(dto, ct).ConfigureAwait(false);
            if (result.Succeeded)
            {
                SetSuccessMessage("WarehouseTaskLifecycleUpdatedMessage");
            }
            else
            {
                SetErrorMessage(result.Error ?? "WarehouseTaskLifecycleFailedMessage");
            }

            return RedirectOrHtmx(nameof(EditWarehouseTask), new { id = dto.Id });
        }

        [HttpGet]
        public async Task<IActionResult> StockCounts(Guid? businessId = null, Guid? warehouseId = null, int page = 1, int pageSize = 20, string? q = null, StockCountQueueFilter filter = StockCountQueueFilter.All, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = new List<StockCountListItemVm>();
            var total = 0;
            var summary = new StockCountOpsSummaryVm();
            if (businessId.HasValue)
            {
                var result = await _getStockCountsPage.HandleAsync(businessId.Value, warehouseId, page, pageSize, q, filter, ct).ConfigureAwait(false);
                var summaryDto = await _getStockCountsPage.GetSummaryAsync(businessId.Value, warehouseId, ct).ConfigureAwait(false);
                items = result.Items.Select(MapStockCountItem).ToList();
                total = result.Total;
                summary = new StockCountOpsSummaryVm
                {
                    TotalCount = summaryDto.TotalCount,
                    DraftCount = summaryDto.DraftCount,
                    InProgressCount = summaryDto.InProgressCount,
                    ReviewPendingCount = summaryDto.ReviewPendingCount,
                    ApprovedCount = summaryDto.ApprovedCount,
                    PostedCount = summaryDto.PostedCount,
                    VarianceCount = summaryDto.VarianceCount
                };
            }

            var vm = new StockCountsListVm
            {
                BusinessId = businessId,
                WarehouseId = warehouseId,
                Query = q ?? string.Empty,
                Filter = filter,
                FilterItems = BuildStockCountFilterItems(filter),
                Summary = summary,
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                WarehouseOptions = await GetWarehouseOptionsAsync(warehouseId, ct).ConfigureAwait(false),
                Items = items,
                Page = page,
                PageSize = pageSize,
                Total = total
            };
            return RenderStockCountsWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateStockCount(Guid? businessId = null, Guid? warehouseId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var vm = new StockCountEditVm
            {
                BusinessId = businessId ?? Guid.Empty,
                WarehouseId = warehouseId ?? Guid.Empty,
                CountType = StockCountType.Cycle,
                Status = StockCountSessionStatus.Draft,
                Lines = new List<StockCountLineVm> { new() { SortOrder = 1 } }
            };
            await PopulateStockCountOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderStockCountEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStockCount(StockCountEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                EnsureStockCountRows(vm);
                await PopulateStockCountOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderStockCountEditor(vm, isCreate: true);
            }

            try
            {
                var id = await _createStockCount.HandleAsync(MapStockCountCreateDto(vm), ct).ConfigureAwait(false);
                SetSuccessMessage("StockCountCreatedMessage");
                return RedirectOrHtmx(nameof(EditStockCount), new { id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("StockCountCreateFailedMessage");
                EnsureStockCountRows(vm);
                await PopulateStockCountOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderStockCountEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditStockCount(Guid id, CancellationToken ct = default)
        {
            var dto = await _getStockCountDetail.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("StockCountNotFoundMessage");
                return RedirectOrHtmx(nameof(StockCounts), new { });
            }

            var vm = MapStockCountEditor(dto);
            await PopulateStockCountOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderStockCountEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStockCount(StockCountEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                EnsureStockCountRows(vm);
                await PopulateStockCountOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderStockCountEditor(vm, isCreate: false);
            }

            try
            {
                await _updateStockCount.HandleAsync(MapStockCountEditDto(vm), ct).ConfigureAwait(false);
                SetSuccessMessage("StockCountUpdatedMessage");
                return RedirectOrHtmx(nameof(EditStockCount), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("StockCountConcurrencyMessage");
                return RedirectOrHtmx(nameof(EditStockCount), new { id = vm.Id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("StockCountUpdateFailedMessage");
                EnsureStockCountRows(vm);
                await PopulateStockCountOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderStockCountEditor(vm, isCreate: false);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStockCountLifecycle(StockCountLifecycleActionDto dto, CancellationToken ct = default)
        {
            var result = await _updateStockCountLifecycle.HandleAsync(dto, ct).ConfigureAwait(false);
            if (result.Succeeded)
            {
                SetSuccessMessage("StockCountLifecycleUpdatedMessage");
            }
            else
            {
                SetErrorMessage(result.Error ?? "StockCountLifecycleFailedMessage");
            }

            return RedirectOrHtmx(nameof(EditStockCount), new { id = dto.Id });
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
                    Code = x.Code,
                    Status = x.Status,
                    PreferredCurrency = x.PreferredCurrency,
                    PaymentTermDays = x.PaymentTermDays,
                    LeadTimeDays = x.LeadTimeDays,
                    Website = x.Website,
                    TaxRegistrationNumber = x.TaxRegistrationNumber,
                    PurchaseOrderCount = x.PurchaseOrderCount,
                    RowVersion = x.RowVersion
                }).ToList();
                total = result.Total;
                summary = new SupplierOpsSummaryVm
                {
                    TotalCount = summaryDto.TotalCount,
                    MissingAddressCount = summaryDto.MissingAddressCount,
                    HasPurchaseOrdersCount = summaryDto.HasPurchaseOrdersCount,
                    InactiveCount = summaryDto.InactiveCount,
                    BlockedCount = summaryDto.BlockedCount
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
            await PopulateSupplierOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderSupplierEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupplier(SupplierEditVm vm, CancellationToken ct = default)
        {
            if (vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage("SupplierCreateFailedMessage");
                return RedirectOrHtmx(nameof(Suppliers), new { });
            }

            if (!ModelState.IsValid)
            {
                await PopulateSupplierOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderSupplierEditor(vm, isCreate: true);
            }

            var dto = new SupplierCreateDto
            {
                BusinessId = vm.BusinessId,
                Name = vm.Name,
                Code = vm.Code,
                Status = vm.Status,
                Email = vm.Email,
                Phone = vm.Phone,
                Address = vm.Address,
                Notes = vm.Notes,
                PreferredCurrency = vm.PreferredCurrency,
                PaymentTermDays = vm.PaymentTermDays,
                LeadTimeDays = vm.LeadTimeDays,
                Website = vm.Website,
                TaxRegistrationNumber = vm.TaxRegistrationNumber,
                ExternalNotes = vm.ExternalNotes
            };

            try
            {
                var id = await _createSupplier.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("SupplierCreatedMessage");
                return RedirectOrHtmx(nameof(EditSupplier), new { id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("SupplierCreateFailedMessage");
                await PopulateSupplierOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderSupplierEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditSupplier(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("SupplierNotFoundMessage");
                return RedirectOrHtmx(nameof(Suppliers), new { });
            }

            var dto = await _getSupplierForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("SupplierNotFoundMessage");
                return RedirectOrHtmx(nameof(Suppliers), new { });
            }

            dto.Documents = (await _getSupplierForEdit.GetDocumentsAsync(id, ct).ConfigureAwait(false)).ToList();
            var vm = MapSupplierEditVm(dto);
            await PopulateSupplierOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderSupplierEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupplier(SupplierEditVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty || vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage(vm.Id == Guid.Empty ? "SupplierNotFoundMessage" : "SupplierUpdateFailedMessage");
                return RedirectOrHtmx(nameof(Suppliers), new { businessId = vm.BusinessId == Guid.Empty ? (Guid?)null : vm.BusinessId });
            }

            if (!ModelState.IsValid)
            {
                await PopulateSupplierOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderSupplierEditor(vm, isCreate: false);
            }

            var dto = new SupplierEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                BusinessId = vm.BusinessId,
                Name = vm.Name,
                Code = vm.Code,
                Status = vm.Status,
                Email = vm.Email,
                Phone = vm.Phone,
                Address = vm.Address,
                Notes = vm.Notes,
                PreferredCurrency = vm.PreferredCurrency,
                PaymentTermDays = vm.PaymentTermDays,
                LeadTimeDays = vm.LeadTimeDays,
                Website = vm.Website,
                TaxRegistrationNumber = vm.TaxRegistrationNumber,
                ExternalNotes = vm.ExternalNotes
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
            catch (Exception)
            {
                AddModelErrorMessage("SupplierUpdateFailedMessage");
                await PopulateSupplierOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderSupplierEditor(vm, isCreate: false);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupplierContact(SupplierContactVm vm, CancellationToken ct = default)
        {
            if (!IsValidSupplierContactAction(vm.SupplierId, vm.BusinessId))
            {
                SetErrorMessage("SupplierContactCreateFailedMessage");
                return RedirectOrHtmx(nameof(Suppliers), new { businessId = vm.BusinessId == Guid.Empty ? (Guid?)null : vm.BusinessId });
            }

            try
            {
                await _createSupplierContact.HandleAsync(MapSupplierContactEditDto(vm), ct).ConfigureAwait(false);
                SetSuccessMessage("SupplierContactCreatedMessage");
            }
            catch (Exception)
            {
                SetErrorMessage("SupplierContactCreateFailedMessage");
            }

            return RedirectOrHtmx(nameof(EditSupplier), new { id = vm.SupplierId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupplierContact(SupplierContactVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty || !IsValidSupplierContactAction(vm.SupplierId, vm.BusinessId))
            {
                SetErrorMessage("SupplierContactUpdateFailedMessage");
                return RedirectOrHtmx(nameof(Suppliers), new { businessId = vm.BusinessId == Guid.Empty ? (Guid?)null : vm.BusinessId });
            }

            try
            {
                await _updateSupplierContact.HandleAsync(MapSupplierContactEditDto(vm), ct).ConfigureAwait(false);
                SetSuccessMessage("SupplierContactUpdatedMessage");
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("SupplierContactConcurrencyMessage");
            }
            catch (Exception)
            {
                SetErrorMessage("SupplierContactUpdateFailedMessage");
            }

            return RedirectOrHtmx(nameof(EditSupplier), new { id = vm.SupplierId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveSupplierContact(Guid id, Guid supplierId, byte[] rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty || supplierId == Guid.Empty)
            {
                SetErrorMessage("SupplierContactArchiveFailedMessage");
                return RedirectOrHtmx(nameof(Suppliers), new { });
            }

            try
            {
                await _archiveSupplierContact.HandleAsync(id, rowVersion ?? Array.Empty<byte>(), ct).ConfigureAwait(false);
                SetSuccessMessage("SupplierContactArchivedMessage");
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("SupplierContactConcurrencyMessage");
            }
            catch (Exception)
            {
                SetErrorMessage("SupplierContactArchiveFailedMessage");
            }

            return RedirectOrHtmx(nameof(EditSupplier), new { id = supplierId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterSupplierDocument(SupplierDocumentRegisterVm vm, CancellationToken ct = default)
        {
            if (vm.SupplierId == Guid.Empty || vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage("SupplierDocumentRegisterFailedMessage");
                return RedirectOrHtmx(nameof(Suppliers), new { businessId = vm.BusinessId == Guid.Empty ? (Guid?)null : vm.BusinessId });
            }

            try
            {
                await _registerSupplierDocument.HandleAsync(new SupplierDocumentRegisterDto
                {
                    SupplierId = vm.SupplierId,
                    BusinessId = vm.BusinessId,
                    DocumentKind = vm.DocumentKind,
                    Title = vm.Title,
                    FileName = vm.FileName,
                    ContentType = vm.ContentType,
                    SizeBytes = vm.SizeBytes,
                    ContentHash = vm.ContentHash,
                    StorageProvider = vm.StorageProvider,
                    StorageContainer = vm.StorageContainer,
                    StorageKey = vm.StorageKey,
                    Visibility = vm.Visibility,
                    MetadataJson = vm.MetadataJson
                }, ct).ConfigureAwait(false);
                SetSuccessMessage("SupplierDocumentRegisteredMessage");
            }
            catch (Exception)
            {
                SetErrorMessage("SupplierDocumentRegisterFailedMessage");
            }

            return RedirectOrHtmx(nameof(EditSupplier), new { id = vm.SupplierId });
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
            return RenderStockLevelsWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> BinStock(Guid? warehouseId = null, Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, BinStockQueueFilter filter = BinStockQueueFilter.All, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            warehouseId = await _referenceData.ResolveWarehouseIdAsync(warehouseId, businessId, ct).ConfigureAwait(false);

            var result = await _getBinStockDerivation.HandleAsync(businessId, warehouseId, page, pageSize, q, filter, ct).ConfigureAwait(false);
            var summary = await _getBinStockDerivation.GetSummaryAsync(businessId, warehouseId, q, ct).ConfigureAwait(false);

            var vm = new BinStockListVm
            {
                BusinessId = businessId,
                WarehouseId = warehouseId,
                Query = q ?? string.Empty,
                Filter = filter,
                FilterItems = BuildBinStockFilterItems(filter),
                WarehouseOptions = await _referenceData.GetWarehouseOptionsAsync(warehouseId, businessId, ct).ConfigureAwait(false),
                Summary = new BinStockOpsSummaryVm
                {
                    RowCount = summary.RowCount,
                    AssignedCount = summary.AssignedCount,
                    UnassignedCount = summary.UnassignedCount,
                    AttentionCount = summary.AttentionCount
                },
                Items = result.Items.Select(x => new BinStockListItemVm
                {
                    WarehouseId = x.WarehouseId,
                    WarehouseName = x.WarehouseName,
                    ProductVariantId = x.ProductVariantId,
                    VariantSku = x.VariantSku,
                    LocationId = x.LocationId,
                    LocationCode = x.LocationCode,
                    LocationDisplayName = x.LocationDisplayName,
                    DerivedQuantity = x.DerivedQuantity,
                    AvailableQuantity = x.AvailableQuantity,
                    UnassignedQuantity = x.UnassignedQuantity,
                    HasAttention = x.HasAttention,
                    AttentionCode = x.AttentionCode,
                    Identities = x.Identities.Select(identity => new BinStockIdentityBreakdownVm
                    {
                        InventoryLotId = identity.InventoryLotId,
                        InventorySerialUnitId = identity.InventorySerialUnitId,
                        HandlingUnitId = identity.HandlingUnitId,
                        LotCode = identity.LotCode,
                        SerialNumber = identity.SerialNumber,
                        HandlingUnitCode = identity.HandlingUnitCode,
                        Quantity = identity.Quantity
                    }).ToList()
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                Total = result.Total
            };

            return RenderBinStockWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> AdjustStock(Guid stockLevelId, Guid? businessId = null, CancellationToken ct = default)
        {
            if (stockLevelId == Guid.Empty)
            {
                SetErrorMessage("StockLevelNotFoundMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { businessId });
            }

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
            if (vm.WarehouseId == Guid.Empty || vm.ProductVariantId == Guid.Empty)
            {
                SetErrorMessage("StockAdjustFailedMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { businessId = vm.BusinessId, warehouseId = vm.WarehouseId == Guid.Empty ? (Guid?)null : vm.WarehouseId });
            }

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
            catch (Exception)
            {
                AddModelErrorMessage("StockAdjustFailedMessage");
                await PopulateInventoryStockActionOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderAdjustStockEditor(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ReserveStock(Guid stockLevelId, Guid? businessId = null, CancellationToken ct = default)
        {
            if (stockLevelId == Guid.Empty)
            {
                SetErrorMessage("StockLevelNotFoundMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { businessId });
            }

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
            if (vm.WarehouseId == Guid.Empty || vm.ProductVariantId == Guid.Empty)
            {
                SetErrorMessage("StockReserveFailedMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { businessId = vm.BusinessId, warehouseId = vm.WarehouseId == Guid.Empty ? (Guid?)null : vm.WarehouseId });
            }

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
            catch (Exception)
            {
                AddModelErrorMessage("StockReserveFailedMessage");
                await PopulateInventoryStockActionOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderReserveStockEditor(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ReleaseReservation(Guid stockLevelId, Guid? businessId = null, CancellationToken ct = default)
        {
            if (stockLevelId == Guid.Empty)
            {
                SetErrorMessage("StockLevelNotFoundMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { businessId });
            }

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
            if (vm.WarehouseId == Guid.Empty || vm.ProductVariantId == Guid.Empty)
            {
                SetErrorMessage("ReservationReleaseFailedMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { businessId = vm.BusinessId, warehouseId = vm.WarehouseId == Guid.Empty ? (Guid?)null : vm.WarehouseId });
            }

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
            catch (Exception)
            {
                AddModelErrorMessage("ReservationReleaseFailedMessage");
                await PopulateInventoryStockActionOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderReleaseReservationEditor(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ReturnReceipt(Guid stockLevelId, Guid? businessId = null, CancellationToken ct = default)
        {
            if (stockLevelId == Guid.Empty)
            {
                SetErrorMessage("StockLevelNotFoundMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { businessId });
            }

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
            if (vm.WarehouseId == Guid.Empty || vm.ProductVariantId == Guid.Empty)
            {
                SetErrorMessage("ReturnReceiptProcessFailedMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { businessId = vm.BusinessId, warehouseId = vm.WarehouseId == Guid.Empty ? (Guid?)null : vm.WarehouseId });
            }

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
            catch (Exception)
            {
                AddModelErrorMessage("ReturnReceiptProcessFailedMessage");
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
            if (vm.WarehouseId == Guid.Empty || vm.ProductVariantId == Guid.Empty)
            {
                SetErrorMessage("StockLevelCreateFailedMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { warehouseId = vm.WarehouseId == Guid.Empty ? (Guid?)null : vm.WarehouseId });
            }

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
            catch (Exception)
            {
                AddModelErrorMessage("StockLevelCreateFailedMessage");
                await PopulateStockLevelOptionsAsync(vm, null, ct).ConfigureAwait(false);
                return RenderStockLevelEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditStockLevel(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("StockLevelNotFoundMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { });
            }

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
            if (vm.Id == Guid.Empty || vm.WarehouseId == Guid.Empty || vm.ProductVariantId == Guid.Empty)
            {
                SetErrorMessage(vm.Id == Guid.Empty ? "StockLevelNotFoundMessage" : "StockLevelUpdateFailedMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { warehouseId = vm.WarehouseId == Guid.Empty ? (Guid?)null : vm.WarehouseId });
            }

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
            catch (Exception)
            {
                AddModelErrorMessage("StockLevelUpdateFailedMessage");
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
                    FromWarehouseId = x.FromWarehouseId,
                    ToWarehouseId = x.ToWarehouseId,
                    FromWarehouseName = x.FromWarehouseName,
                    ToWarehouseName = x.ToWarehouseName,
                    Status = x.Status,
                    LineCount = x.LineCount,
                    CreatedAtUtc = x.CreatedAtUtc,
                    IsStale = x.IsStale,
                    RowVersion = x.RowVersion
                }).ToList();
                total = result.Total;
                summary = new StockTransferOpsSummaryVm
                {
                    TotalCount = summaryDto.TotalCount,
                    DraftCount = summaryDto.DraftCount,
                    InTransitCount = summaryDto.InTransitCount,
                    CompletedCount = summaryDto.CompletedCount,
                    CancelledCount = summaryDto.CancelledCount,
                    StaleInTransitCount = summaryDto.StaleInTransitCount
                };
            }

            var vm = new StockTransfersListVm
            {
                BusinessId = businessId,
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
            if (vm.FromWarehouseId == Guid.Empty || vm.ToWarehouseId == Guid.Empty)
            {
                SetErrorMessage("StockTransferCreateFailedMessage");
                return RedirectOrHtmx(nameof(StockTransfers), new { warehouseId = vm.FromWarehouseId == Guid.Empty ? (Guid?)null : vm.FromWarehouseId });
            }

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
                    Quantity = x.Quantity,
                    Identities = x.Identities.Select(MapIdentityDto).ToList()
                }).ToList()
            };

            try
            {
                var id = await _createStockTransfer.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("StockTransferCreatedMessage");
                return RedirectOrHtmx(nameof(EditStockTransfer), new { id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("StockTransferCreateFailedMessage");
                EnsureStockTransferRows(vm);
                await PopulateStockTransferOptionsAsync(vm, null, ct).ConfigureAwait(false);
                return RenderStockTransferEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditStockTransfer(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("StockTransferNotFoundMessage");
                return RedirectOrHtmx(nameof(StockTransfers), new { });
            }

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
                    Quantity = x.Quantity,
                    Identities = x.Identities.Select(MapIdentityVm).ToList()
                }).ToList()
            };
            EnsureStockTransferRows(vm);
            await PopulateStockTransferOptionsAsync(vm, null, ct).ConfigureAwait(false);
            return RenderStockTransferEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStockTransferLifecycle(
            Guid id,
            string rowVersion,
            string action,
            Guid? businessId = null,
            Guid? warehouseId = null,
            int page = 1,
            int pageSize = 20,
            string? q = null,
            StockTransferQueueFilter filter = StockTransferQueueFilter.All,
            CancellationToken ct = default)
        {
            if (id == Guid.Empty || string.IsNullOrWhiteSpace(action))
            {
                return RenderStockTransferLifecycleFailure(
                    T("StockTransferLifecycleUpdateFailedMessage"),
                    businessId,
                    warehouseId,
                    page,
                    pageSize,
                    q,
                    filter);
            }

            var version = DecodeBase64RowVersion(rowVersion);

            try
            {
                var result = await _updateStockTransferLifecycle
                    .HandleAsync(new StockTransferLifecycleActionDto
                    {
                        Id = id,
                        RowVersion = version,
                        Action = action
                    }, ct)
                    .ConfigureAwait(false);

                if (result.Succeeded)
                {
                    SetSuccessMessage(action switch
                    {
                        UpdateStockTransferLifecycleHandler.MarkInTransitAction => "StockTransferMarkedInTransitMessage",
                        UpdateStockTransferLifecycleHandler.CompleteAction => "StockTransferCompletedMessage",
                        UpdateStockTransferLifecycleHandler.CancelAction => "StockTransferCancelledMessage",
                        _ => "StockTransferLifecycleUpdatedMessage"
                    });
                }
                else
                {
                    return RenderStockTransferLifecycleFailure(
                        result.Error ?? T("StockTransferLifecycleUpdateFailedMessage"),
                        businessId,
                        warehouseId,
                        page,
                        pageSize,
                        q,
                        filter);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                return RenderStockTransferLifecycleFailure(
                    T("StockTransferConcurrencyMessage"),
                    businessId,
                    warehouseId,
                    page,
                    pageSize,
                    q,
                    filter);
            }

            return RedirectOrHtmx(nameof(StockTransfers), new { businessId, warehouseId, page, pageSize, q, filter });
        }

        private IActionResult RenderStockTransferLifecycleFailure(
            string message,
            Guid? businessId,
            Guid? warehouseId,
            int page,
            int pageSize,
            string? q,
            StockTransferQueueFilter filter)
        {
            if (IsHtmxRequest())
            {
                return Content(message, "text/html");
            }

            TempData["Error"] = message;
            return RedirectToAction(nameof(StockTransfers), new { businessId, warehouseId, page, pageSize, q, filter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStockTransfer(StockTransferEditVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty || vm.FromWarehouseId == Guid.Empty || vm.ToWarehouseId == Guid.Empty)
            {
                SetErrorMessage(vm.Id == Guid.Empty ? "StockTransferNotFoundMessage" : "StockTransferUpdateFailedMessage");
                return RedirectOrHtmx(nameof(StockTransfers), new { warehouseId = vm.FromWarehouseId == Guid.Empty ? (Guid?)null : vm.FromWarehouseId });
            }

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
                    Quantity = x.Quantity,
                    Identities = x.Identities.Select(MapIdentityDto).ToList()
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
            catch (Exception)
            {
                AddModelErrorMessage("StockTransferUpdateFailedMessage");
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
                    BusinessId = x.BusinessId,
                    OrderNumber = x.OrderNumber,
                    SupplierName = x.SupplierName,
                    Status = x.Status,
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
                }).ToList();
                total = result.Total;
                summary = new PurchaseOrderOpsSummaryVm
                {
                    TotalCount = summaryDto.TotalCount,
                    DraftCount = summaryDto.DraftCount,
                    IssuedCount = summaryDto.IssuedCount,
                    ReceivedCount = summaryDto.ReceivedCount,
                    CancelledCount = summaryDto.CancelledCount,
                    StaleIssuedCount = summaryDto.StaleIssuedCount,
                    PartiallyReceivedCount = summaryDto.PartiallyReceivedCount
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
        public async Task<IActionResult> GoodsReceipts(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, GoodsReceiptQueueFilter filter = GoodsReceiptQueueFilter.All, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = new List<GoodsReceiptListItemVm>();
            var total = 0;
            var summary = new GoodsReceiptOpsSummaryVm();
            if (businessId.HasValue)
            {
                var result = await _getGoodsReceiptsPage.HandleAsync(businessId.Value, page, pageSize, q, filter, ct).ConfigureAwait(false);
                var summaryDto = await _getGoodsReceiptsPage.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);
                items = result.Items.Select(MapGoodsReceiptListItem).ToList();
                total = result.Total;
                summary = new GoodsReceiptOpsSummaryVm
                {
                    TotalCount = summaryDto.TotalCount,
                    DraftCount = summaryDto.DraftCount,
                    ReceivedCount = summaryDto.ReceivedCount,
                    InspectedCount = summaryDto.InspectedCount,
                    PostedCount = summaryDto.PostedCount,
                    CancelledCount = summaryDto.CancelledCount
                };
            }

            var vm = new GoodsReceiptsListVm
            {
                BusinessId = businessId,
                Query = q ?? string.Empty,
                Filter = filter,
                FilterItems = BuildGoodsReceiptFilterItems(filter),
                Summary = summary,
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                PurchaseOrderOptions = await BuildGoodsReceiptPurchaseOrderOptionsAsync(businessId, ct).ConfigureAwait(false),
                WarehouseOptions = await _referenceData.GetWarehouseOptionsAsync(null, businessId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };
            return RenderGoodsReceiptsWorkspace(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGoodsReceipt(GoodsReceiptsListVm vm, CancellationToken ct = default)
        {
            if (vm.PurchaseOrderId == Guid.Empty || vm.WarehouseId == Guid.Empty)
            {
                SetErrorMessage("GoodsReceiptCreateFailedMessage");
                return RedirectOrHtmx(nameof(GoodsReceipts), new { businessId = vm.BusinessId });
            }

            try
            {
                var result = await _createGoodsReceipt.HandleAsync(new GoodsReceiptCreateDto
                {
                    PurchaseOrderId = vm.PurchaseOrderId,
                    WarehouseId = vm.WarehouseId,
                    InternalNotes = vm.InternalNotes
                }, ct).ConfigureAwait(false);

                if (result.Succeeded && result.Value != Guid.Empty)
                {
                    SetSuccessMessage("GoodsReceiptCreatedMessage");
                    return RedirectOrHtmx(nameof(EditGoodsReceipt), new { id = result.Value });
                }
            }
            catch (Exception)
            {
                // Use the same safe operator-facing failure message as validation failures.
            }

            SetErrorMessage("GoodsReceiptCreateFailedMessage");
            return RedirectOrHtmx(nameof(GoodsReceipts), new { businessId = vm.BusinessId });
        }

        [HttpGet]
        public async Task<IActionResult> EditGoodsReceipt(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("GoodsReceiptNotFoundMessage");
                return RedirectOrHtmx(nameof(GoodsReceipts), new { });
            }

            var dto = await _getGoodsReceiptDetail.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("GoodsReceiptNotFoundMessage");
                return RedirectOrHtmx(nameof(GoodsReceipts), new { });
            }

            var vm = MapGoodsReceiptDetail(dto);
            vm.PutawayLocationOptions = await GetWarehouseTaskLocationOptionsAsync(vm.BusinessId, vm.WarehouseId, vm.WarehouseTaskAction.ToLocationId, ct).ConfigureAwait(false);
            await PopulateGoodsReceiptIdentityOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderGoodsReceiptDetail(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGoodsReceiptLifecycle(GoodsReceiptDetailVm vm, string rowVersion, string action, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty || string.IsNullOrWhiteSpace(action))
            {
                SetErrorMessage("GoodsReceiptLifecycleUpdateFailedMessage");
                return RedirectOrHtmx(nameof(GoodsReceipts), new { businessId = vm.BusinessId == Guid.Empty ? (Guid?)null : vm.BusinessId });
            }

            var result = await _updateGoodsReceiptLifecycle.HandleAsync(new GoodsReceiptLifecycleActionDto
            {
                Id = vm.Id,
                RowVersion = DecodeBase64RowVersion(rowVersion),
                Action = action,
                Lines = vm.Lines.Select(x => new GoodsReceiptLineDto
                {
                    Id = x.Id,
                    PurchaseOrderLineId = x.PurchaseOrderLineId,
                    ProductVariantId = x.ProductVariantId,
                    ReceivedQuantity = x.ReceivedQuantity,
                    AcceptedQuantity = x.AcceptedQuantity,
                    RejectedQuantity = x.RejectedQuantity,
                    DamagedQuantity = x.DamagedQuantity,
                    Identities = x.Identities.Select(identity => new GoodsReceiptLineIdentityDto
                    {
                        Id = identity.Id,
                        GoodsReceiptLineId = x.Id,
                        ProductVariantId = x.ProductVariantId,
                        InventoryLotId = identity.InventoryLotId,
                        InventorySerialUnitId = identity.InventorySerialUnitId,
                        HandlingUnitId = identity.HandlingUnitId,
                        Quantity = identity.Quantity,
                        SortOrder = identity.SortOrder,
                        MetadataJson = identity.MetadataJson
                    }).ToList()
                }).ToList()
            }, ct).ConfigureAwait(false);

            if (result.Succeeded)
            {
                SetSuccessMessage("GoodsReceiptLifecycleUpdatedMessage");
            }
            else
            {
                SetErrorMessage("GoodsReceiptLifecycleUpdateFailedMessage");
            }
            return RedirectOrHtmx(nameof(EditGoodsReceipt), new { id = vm.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGoodsReceiptInlineIdentity(GoodsReceiptInlineIdentityCreateVm vm, string rowVersion, CancellationToken ct = default)
        {
            if (vm.GoodsReceiptId == Guid.Empty || vm.GoodsReceiptLineId == Guid.Empty)
            {
                SetErrorMessage("GoodsReceiptIdentityCreateFailedMessage");
                return RedirectOrHtmx(nameof(GoodsReceipts), new { });
            }

            var result = await _createGoodsReceiptInlineIdentity.HandleAsync(new GoodsReceiptInlineIdentityCreateDto
            {
                GoodsReceiptId = vm.GoodsReceiptId,
                GoodsReceiptLineId = vm.GoodsReceiptLineId,
                RowVersion = DecodeBase64RowVersion(rowVersion),
                IdentityType = vm.IdentityType,
                LotCode = vm.LotCode,
                SupplierLotCode = vm.SupplierLotCode,
                ExpiryDateUtc = vm.ExpiryDateUtc,
                SerialNumber = vm.SerialNumber,
                InventoryLotId = vm.InventoryLotId,
                HandlingUnitCode = vm.HandlingUnitCode,
                HandlingUnitDisplayName = vm.HandlingUnitDisplayName,
                Quantity = vm.Quantity,
                MetadataJson = vm.MetadataJson
            }, ct).ConfigureAwait(false);

            if (result.Succeeded)
            {
                SetSuccessMessage("GoodsReceiptIdentityCreatedMessage");
            }
            else
            {
                SetErrorMessage(result.Error ?? "GoodsReceiptIdentityCreateFailedMessage");
            }

            return RedirectOrHtmx(nameof(EditGoodsReceipt), new { id = vm.GoodsReceiptId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReceivingTaskFromGoodsReceipt(GoodsReceiptWarehouseTaskVm vm, CancellationToken ct = default)
        {
            if (vm.GoodsReceiptId == Guid.Empty)
            {
                SetErrorMessage("GoodsReceiptNotFoundMessage");
                return RedirectOrHtmx(nameof(GoodsReceipts), new { businessId = vm.BusinessId == Guid.Empty ? (Guid?)null : vm.BusinessId });
            }

            var result = await _createWarehouseReceivingTaskFromGoodsReceipt.HandleAsync(new CreateWarehouseReceivingTaskFromGoodsReceiptDto
            {
                GoodsReceiptId = vm.GoodsReceiptId,
                AssignedToUserId = vm.AssignedToUserId,
                DueAtUtc = vm.DueAtUtc,
                Priority = vm.Priority,
                InternalNotes = vm.InternalNotes
            }, ct).ConfigureAwait(false);

            if (result.Succeeded)
            {
                SetSuccessMessage("WarehouseTaskCreatedMessage");
                return RedirectOrHtmx(nameof(EditWarehouseTask), new { id = result.Value });
            }

            SetErrorMessage(result.Error ?? "WarehouseTaskCreateFailedMessage");
            return RedirectOrHtmx(nameof(EditGoodsReceipt), new { id = vm.GoodsReceiptId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePutawayTaskFromGoodsReceipt(GoodsReceiptWarehouseTaskVm vm, CancellationToken ct = default)
        {
            if (vm.GoodsReceiptId == Guid.Empty)
            {
                SetErrorMessage("GoodsReceiptNotFoundMessage");
                return RedirectOrHtmx(nameof(GoodsReceipts), new { businessId = vm.BusinessId == Guid.Empty ? (Guid?)null : vm.BusinessId });
            }

            var result = await _createWarehousePutawayTaskFromGoodsReceipt.HandleAsync(new CreateWarehousePutawayTaskFromGoodsReceiptDto
            {
                GoodsReceiptId = vm.GoodsReceiptId,
                ToLocationId = vm.ToLocationId,
                AssignedToUserId = vm.AssignedToUserId,
                DueAtUtc = vm.DueAtUtc,
                Priority = vm.Priority,
                InternalNotes = vm.InternalNotes
            }, ct).ConfigureAwait(false);

            if (result.Succeeded)
            {
                SetSuccessMessage("WarehouseTaskCreatedMessage");
                return RedirectOrHtmx(nameof(EditWarehouseTask), new { id = result.Value });
            }

            SetErrorMessage(result.Error ?? "WarehouseTaskCreateFailedMessage");
            return RedirectOrHtmx(nameof(EditGoodsReceipt), new { id = vm.GoodsReceiptId });
        }

        [HttpGet]
        public async Task<IActionResult> CreatePurchaseOrder(Guid? businessId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var nowUtc = _clock.UtcNow;
            var vm = new PurchaseOrderEditVm
            {
                BusinessId = businessId ?? Guid.Empty,
                OrderedAtUtc = nowUtc,
                OrderNumber = string.Empty,
                Currency = "EUR",
                Status = "Draft"
            };
            EnsurePurchaseOrderRows(vm);
            await PopulatePurchaseOrderOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderPurchaseOrderEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePurchaseOrder(PurchaseOrderEditVm vm, CancellationToken ct = default)
        {
            if (vm.BusinessId == Guid.Empty || vm.SupplierId == Guid.Empty)
            {
                SetErrorMessage("PurchaseOrderCreateFailedMessage");
                return RedirectOrHtmx(nameof(PurchaseOrders), new { businessId = vm.BusinessId == Guid.Empty ? (Guid?)null : vm.BusinessId });
            }

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
                OrderNumber = vm.OrderNumber ?? string.Empty,
                OrderedAtUtc = vm.OrderedAtUtc,
                Currency = vm.Currency,
                ExpectedDeliveryDateUtc = vm.ExpectedDeliveryDateUtc,
                Status = vm.Status,
                InternalNotes = vm.InternalNotes,
                Lines = vm.Lines.Select(x => new PurchaseOrderLineDto
                {
                    ProductVariantId = x.ProductVariantId,
                    SupplierSku = x.SupplierSku,
                    Description = x.Description,
                    Quantity = x.Quantity,
                    ReceivedQuantity = x.ReceivedQuantity,
                    CancelledQuantity = x.CancelledQuantity,
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
            catch (Exception)
            {
                AddModelErrorMessage("PurchaseOrderCreateFailedMessage");
                EnsurePurchaseOrderRows(vm);
                await PopulatePurchaseOrderOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderPurchaseOrderEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditPurchaseOrder(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("PurchaseOrderNotFoundMessage");
                return RedirectOrHtmx(nameof(PurchaseOrders), new { });
            }

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
                Currency = dto.Currency,
                ExpectedDeliveryDateUtc = dto.ExpectedDeliveryDateUtc,
                Status = dto.Status,
                InternalNotes = dto.InternalNotes,
                Lines = dto.Lines.Select(x => new PurchaseOrderLineVm
                {
                    ProductVariantId = x.ProductVariantId,
                    SupplierSku = x.SupplierSku,
                    Description = x.Description,
                    Quantity = x.Quantity,
                    ReceivedQuantity = x.ReceivedQuantity,
                    CancelledQuantity = x.CancelledQuantity,
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
        public async Task<IActionResult> UpdatePurchaseOrderLifecycle(
            Guid id,
            string rowVersion,
            string action,
            Guid? businessId = null,
            int page = 1,
            int pageSize = 20,
            string? q = null,
            PurchaseOrderQueueFilter filter = PurchaseOrderQueueFilter.All,
            CancellationToken ct = default)
        {
            if (id == Guid.Empty || string.IsNullOrWhiteSpace(action))
            {
                SetErrorMessage("PurchaseOrderLifecycleUpdateFailedMessage");
                return RedirectOrHtmx(nameof(PurchaseOrders), new { businessId, page, pageSize, q, filter });
            }

            var version = DecodeBase64RowVersion(rowVersion);

            try
            {
                var result = await _updatePurchaseOrderLifecycle
                    .HandleAsync(new PurchaseOrderLifecycleActionDto
                    {
                        Id = id,
                        RowVersion = version,
                        Action = action
                    }, ct)
                    .ConfigureAwait(false);

                if (result.Succeeded)
                {
                    SetSuccessMessage(action switch
                    {
                        UpdatePurchaseOrderLifecycleHandler.IssueAction => "PurchaseOrderIssuedMessage",
                        UpdatePurchaseOrderLifecycleHandler.ReceiveAction => "PurchaseOrderReceivedMessage",
                        UpdatePurchaseOrderLifecycleHandler.CancelAction => "PurchaseOrderCancelledMessage",
                        _ => "PurchaseOrderLifecycleUpdatedMessage"
                    });
                }
                else
                {
                SetErrorMessage("PurchaseOrderLifecycleUpdateFailedMessage");
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("PurchaseOrderConcurrencyMessage");
            }

            return RedirectOrHtmx(nameof(PurchaseOrders), new { businessId, page, pageSize, q, filter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPurchaseOrder(PurchaseOrderEditVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty || vm.BusinessId == Guid.Empty || vm.SupplierId == Guid.Empty)
            {
                SetErrorMessage(vm.Id == Guid.Empty ? "PurchaseOrderNotFoundMessage" : "PurchaseOrderUpdateFailedMessage");
                return RedirectOrHtmx(nameof(PurchaseOrders), new { businessId = vm.BusinessId == Guid.Empty ? (Guid?)null : vm.BusinessId });
            }

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
                OrderNumber = vm.OrderNumber ?? string.Empty,
                OrderedAtUtc = vm.OrderedAtUtc,
                Currency = vm.Currency,
                ExpectedDeliveryDateUtc = vm.ExpectedDeliveryDateUtc,
                Status = vm.Status,
                InternalNotes = vm.InternalNotes,
                Lines = vm.Lines.Select(x => new PurchaseOrderLineDto
                {
                    ProductVariantId = x.ProductVariantId,
                    SupplierSku = x.SupplierSku,
                    Description = x.Description,
                    Quantity = x.Quantity,
                    ReceivedQuantity = x.ReceivedQuantity,
                    CancelledQuantity = x.CancelledQuantity,
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
            catch (Exception)
            {
                AddModelErrorMessage("PurchaseOrderUpdateFailedMessage");
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
            if (variantId == Guid.Empty)
            {
                SetErrorMessage("StockLevelNotFoundMessage");
                return RedirectOrHtmx(nameof(StockLevels), new { warehouseId });
            }

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

        private IEnumerable<SelectListItem> BuildInventoryLedgerFilterItems(InventoryLedgerQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllLedgerEntries"), InventoryLedgerQueueFilter.All.ToString(), selectedFilter == InventoryLedgerQueueFilter.All);
            yield return new SelectListItem(T("Inbound"), InventoryLedgerQueueFilter.Inbound.ToString(), selectedFilter == InventoryLedgerQueueFilter.Inbound);
            yield return new SelectListItem(T("Outbound"), InventoryLedgerQueueFilter.Outbound.ToString(), selectedFilter == InventoryLedgerQueueFilter.Outbound);
            yield return new SelectListItem(T("Reservations"), InventoryLedgerQueueFilter.Reservations.ToString(), selectedFilter == InventoryLedgerQueueFilter.Reservations);
        }

        private IEnumerable<SelectListItem> BuildBinStockFilterItems(BinStockQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("All"), BinStockQueueFilter.All.ToString(), selectedFilter == BinStockQueueFilter.All);
            yield return new SelectListItem(T("NeedsAttention"), BinStockQueueFilter.WithAttention.ToString(), selectedFilter == BinStockQueueFilter.WithAttention);
            yield return new SelectListItem(T("Assigned"), BinStockQueueFilter.Assigned.ToString(), selectedFilter == BinStockQueueFilter.Assigned);
            yield return new SelectListItem(T("Unassigned"), BinStockQueueFilter.Unassigned.ToString(), selectedFilter == BinStockQueueFilter.Unassigned);
        }

        private List<InventoryOpsPlaybookVm> BuildInventoryLedgerPlaybooks()
        {
            return new List<InventoryOpsPlaybookVm>
            {
                new()
                {
                    Title = T("InventoryLedgerPlaybookInboundTitle"),
                    ScopeNote = T("InventoryLedgerPlaybookInboundScope"),
                    OperatorAction = T("InventoryLedgerPlaybookInboundAction")
                },
                new()
                {
                    Title = T("InventoryLedgerPlaybookOutboundTitle"),
                    ScopeNote = T("InventoryLedgerPlaybookOutboundScope"),
                    OperatorAction = T("InventoryLedgerPlaybookOutboundAction")
                }
            };
        }

        private List<InventoryOpsPlaybookVm> BuildStockTransferPlaybooks()
        {
            return new List<InventoryOpsPlaybookVm>
            {
                new()
                {
                    Title = T("InventoryTransfersPlaybookDraftTitle"),
                    ScopeNote = T("InventoryTransfersPlaybookDraftScope"),
                    OperatorAction = T("InventoryTransfersPlaybookDraftAction")
                },
                new()
                {
                    Title = T("InventoryTransfersPlaybookInTransitTitle"),
                    ScopeNote = T("InventoryTransfersPlaybookInTransitScope"),
                    OperatorAction = T("InventoryTransfersPlaybookInTransitAction")
                }
            };
        }

        private List<InventoryOpsPlaybookVm> BuildPurchaseOrderPlaybooks()
        {
            return new List<InventoryOpsPlaybookVm>
            {
                new()
                {
                    Title = T("InventoryPurchaseOrdersPlaybookDraftTitle"),
                    ScopeNote = T("InventoryPurchaseOrdersPlaybookDraftScope"),
                    OperatorAction = T("InventoryPurchaseOrdersPlaybookDraftAction")
                },
                new()
                {
                    Title = T("InventoryPurchaseOrdersPlaybookIssuedTitle"),
                    ScopeNote = T("InventoryPurchaseOrdersPlaybookIssuedScope"),
                    OperatorAction = T("InventoryPurchaseOrdersPlaybookIssuedAction")
                }
            };
        }

        private List<InventoryOpsPlaybookVm> BuildWarehousePlaybooks()
        {
            return new List<InventoryOpsPlaybookVm>
            {
                new()
                {
                    Title = T("WarehousePlaybookDefaultTitle"),
                    ScopeNote = T("WarehousePlaybookDefaultScope"),
                    OperatorAction = T("WarehousePlaybookDefaultAction")
                },
                new()
                {
                    Title = T("WarehousePlaybookEmptyTitle"),
                    ScopeNote = T("WarehousePlaybookEmptyScope"),
                    OperatorAction = T("WarehousePlaybookEmptyAction")
                }
            };
        }

        private List<InventoryOpsPlaybookVm> BuildSupplierPlaybooks()
        {
            return new List<InventoryOpsPlaybookVm>
            {
                new()
                {
                    Title = T("SupplierPlaybookContactHygieneTitle"),
                    ScopeNote = T("SupplierPlaybookContactHygieneScope"),
                    OperatorAction = T("SupplierPlaybookContactHygieneAction")
                },
                new()
                {
                    Title = T("SupplierPlaybookActiveReviewTitle"),
                    ScopeNote = T("SupplierPlaybookActiveReviewScope"),
                    OperatorAction = T("SupplierPlaybookActiveReviewAction")
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
            if (stockLevelId == Guid.Empty)
            {
                return null;
            }

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
            if (stockLevelId == Guid.Empty)
            {
                return null;
            }

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
            if (stockLevelId == Guid.Empty)
            {
                return null;
            }

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
            if (stockLevelId == Guid.Empty)
            {
                return null;
            }

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

        private IEnumerable<SelectListItem> BuildStockLevelFilterItems(StockLevelQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllStockLevels"), StockLevelQueueFilter.All.ToString(), selectedFilter == StockLevelQueueFilter.All);
            yield return new SelectListItem(T("LowStock"), StockLevelQueueFilter.LowStock.ToString(), selectedFilter == StockLevelQueueFilter.LowStock);
            yield return new SelectListItem(T("Reserved"), StockLevelQueueFilter.Reserved.ToString(), selectedFilter == StockLevelQueueFilter.Reserved);
            yield return new SelectListItem(T("InTransit"), StockLevelQueueFilter.InTransit.ToString(), selectedFilter == StockLevelQueueFilter.InTransit);
        }

        private IEnumerable<SelectListItem> BuildPurchaseOrderFilterItems(PurchaseOrderQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllPurchaseOrders"), PurchaseOrderQueueFilter.All.ToString(), selectedFilter == PurchaseOrderQueueFilter.All);
            yield return new SelectListItem(T("Draft"), PurchaseOrderQueueFilter.Draft.ToString(), selectedFilter == PurchaseOrderQueueFilter.Draft);
            yield return new SelectListItem(T("Issued"), PurchaseOrderQueueFilter.Issued.ToString(), selectedFilter == PurchaseOrderQueueFilter.Issued);
            yield return new SelectListItem(T("Received"), PurchaseOrderQueueFilter.Received.ToString(), selectedFilter == PurchaseOrderQueueFilter.Received);
            yield return new SelectListItem(T("Cancelled"), PurchaseOrderQueueFilter.Cancelled.ToString(), selectedFilter == PurchaseOrderQueueFilter.Cancelled);
            yield return new SelectListItem(T("StaleIssued"), PurchaseOrderQueueFilter.StaleIssued.ToString(), selectedFilter == PurchaseOrderQueueFilter.StaleIssued);
        }

        private IEnumerable<SelectListItem> BuildGoodsReceiptFilterItems(GoodsReceiptQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllGoodsReceipts"), GoodsReceiptQueueFilter.All.ToString(), selectedFilter == GoodsReceiptQueueFilter.All);
            yield return new SelectListItem(T("Draft"), GoodsReceiptQueueFilter.Draft.ToString(), selectedFilter == GoodsReceiptQueueFilter.Draft);
            yield return new SelectListItem(T("Received"), GoodsReceiptQueueFilter.Received.ToString(), selectedFilter == GoodsReceiptQueueFilter.Received);
            yield return new SelectListItem(T("Inspected"), GoodsReceiptQueueFilter.Inspected.ToString(), selectedFilter == GoodsReceiptQueueFilter.Inspected);
            yield return new SelectListItem(T("Posted"), GoodsReceiptQueueFilter.Posted.ToString(), selectedFilter == GoodsReceiptQueueFilter.Posted);
            yield return new SelectListItem(T("Cancelled"), GoodsReceiptQueueFilter.Cancelled.ToString(), selectedFilter == GoodsReceiptQueueFilter.Cancelled);
        }

        private IEnumerable<SelectListItem> BuildStockTransferFilterItems(StockTransferQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllTransfers"), StockTransferQueueFilter.All.ToString(), selectedFilter == StockTransferQueueFilter.All);
            yield return new SelectListItem(T("Draft"), StockTransferQueueFilter.Draft.ToString(), selectedFilter == StockTransferQueueFilter.Draft);
            yield return new SelectListItem(T("InTransit"), StockTransferQueueFilter.InTransit.ToString(), selectedFilter == StockTransferQueueFilter.InTransit);
            yield return new SelectListItem(T("Completed"), StockTransferQueueFilter.Completed.ToString(), selectedFilter == StockTransferQueueFilter.Completed);
            yield return new SelectListItem(T("Cancelled"), StockTransferQueueFilter.Cancelled.ToString(), selectedFilter == StockTransferQueueFilter.Cancelled);
            yield return new SelectListItem(T("StaleInTransit"), StockTransferQueueFilter.StaleInTransit.ToString(), selectedFilter == StockTransferQueueFilter.StaleInTransit);
        }

        private IEnumerable<SelectListItem> BuildWarehouseFilterItems(WarehouseQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllWarehouses"), WarehouseQueueFilter.All.ToString(), selectedFilter == WarehouseQueueFilter.All);
            yield return new SelectListItem(T("Default"), WarehouseQueueFilter.Default.ToString(), selectedFilter == WarehouseQueueFilter.Default);
            yield return new SelectListItem(T("NoStockLevels"), WarehouseQueueFilter.NoStockLevels.ToString(), selectedFilter == WarehouseQueueFilter.NoStockLevels);
        }

        private IEnumerable<SelectListItem> BuildWarehouseLocationFilterItems(WarehouseLocationQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllLocations"), WarehouseLocationQueueFilter.All.ToString(), selectedFilter == WarehouseLocationQueueFilter.All);
            yield return new SelectListItem(T("Active"), WarehouseLocationQueueFilter.Active.ToString(), selectedFilter == WarehouseLocationQueueFilter.Active);
            yield return new SelectListItem(T("Inactive"), WarehouseLocationQueueFilter.Inactive.ToString(), selectedFilter == WarehouseLocationQueueFilter.Inactive);
            yield return new SelectListItem(T("Blocked"), WarehouseLocationQueueFilter.Blocked.ToString(), selectedFilter == WarehouseLocationQueueFilter.Blocked);
            yield return new SelectListItem(T("Bins"), WarehouseLocationQueueFilter.Bins.ToString(), selectedFilter == WarehouseLocationQueueFilter.Bins);
            yield return new SelectListItem(T("Docks"), WarehouseLocationQueueFilter.Docks.ToString(), selectedFilter == WarehouseLocationQueueFilter.Docks);
            yield return new SelectListItem(T("QualityHold"), WarehouseLocationQueueFilter.QualityHold.ToString(), selectedFilter == WarehouseLocationQueueFilter.QualityHold);
        }

        private IEnumerable<SelectListItem> BuildWarehouseLabelTemplateFilterItems(WarehouseLabelTemplateQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllTemplates"), WarehouseLabelTemplateQueueFilter.All.ToString(), selectedFilter == WarehouseLabelTemplateQueueFilter.All);
            yield return new SelectListItem(T("Active"), WarehouseLabelTemplateQueueFilter.Active.ToString(), selectedFilter == WarehouseLabelTemplateQueueFilter.Active);
            yield return new SelectListItem(T("Inactive"), WarehouseLabelTemplateQueueFilter.Inactive.ToString(), selectedFilter == WarehouseLabelTemplateQueueFilter.Inactive);
            yield return new SelectListItem(T("Default"), WarehouseLabelTemplateQueueFilter.Default.ToString(), selectedFilter == WarehouseLabelTemplateQueueFilter.Default);
        }

        private IEnumerable<SelectListItem> BuildWarehouseTaskFilterItems(WarehouseTaskQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllWarehouseTasks"), WarehouseTaskQueueFilter.All.ToString(), selectedFilter == WarehouseTaskQueueFilter.All);
            yield return new SelectListItem(T("Draft"), WarehouseTaskQueueFilter.Draft.ToString(), selectedFilter == WarehouseTaskQueueFilter.Draft);
            yield return new SelectListItem(T("Ready"), WarehouseTaskQueueFilter.Ready.ToString(), selectedFilter == WarehouseTaskQueueFilter.Ready);
            yield return new SelectListItem(T("Assigned"), WarehouseTaskQueueFilter.Assigned.ToString(), selectedFilter == WarehouseTaskQueueFilter.Assigned);
            yield return new SelectListItem(T("InProgress"), WarehouseTaskQueueFilter.InProgress.ToString(), selectedFilter == WarehouseTaskQueueFilter.InProgress);
            yield return new SelectListItem(T("Completed"), WarehouseTaskQueueFilter.Completed.ToString(), selectedFilter == WarehouseTaskQueueFilter.Completed);
            yield return new SelectListItem(T("Cancelled"), WarehouseTaskQueueFilter.Cancelled.ToString(), selectedFilter == WarehouseTaskQueueFilter.Cancelled);
            yield return new SelectListItem(T("NeedsAssignment"), WarehouseTaskQueueFilter.NeedsAssignment.ToString(), selectedFilter == WarehouseTaskQueueFilter.NeedsAssignment);
            yield return new SelectListItem(T("Overdue"), WarehouseTaskQueueFilter.Overdue.ToString(), selectedFilter == WarehouseTaskQueueFilter.Overdue);
            yield return new SelectListItem(T("Shortage"), WarehouseTaskQueueFilter.Shortage.ToString(), selectedFilter == WarehouseTaskQueueFilter.Shortage);
        }

        private IEnumerable<SelectListItem> BuildStockCountFilterItems(StockCountQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllStockCounts"), StockCountQueueFilter.All.ToString(), selectedFilter == StockCountQueueFilter.All);
            yield return new SelectListItem(T("Draft"), StockCountQueueFilter.Draft.ToString(), selectedFilter == StockCountQueueFilter.Draft);
            yield return new SelectListItem(T("InProgress"), StockCountQueueFilter.InProgress.ToString(), selectedFilter == StockCountQueueFilter.InProgress);
            yield return new SelectListItem(T("ReviewPending"), StockCountQueueFilter.ReviewPending.ToString(), selectedFilter == StockCountQueueFilter.ReviewPending);
            yield return new SelectListItem(T("Approved"), StockCountQueueFilter.Approved.ToString(), selectedFilter == StockCountQueueFilter.Approved);
            yield return new SelectListItem(T("Posted"), StockCountQueueFilter.Posted.ToString(), selectedFilter == StockCountQueueFilter.Posted);
            yield return new SelectListItem(T("Cancelled"), StockCountQueueFilter.Cancelled.ToString(), selectedFilter == StockCountQueueFilter.Cancelled);
            yield return new SelectListItem(T("Variance"), StockCountQueueFilter.Variance.ToString(), selectedFilter == StockCountQueueFilter.Variance);
        }

        private IEnumerable<SelectListItem> BuildSupplierFilterItems(SupplierQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllSuppliers"), SupplierQueueFilter.All.ToString(), selectedFilter == SupplierQueueFilter.All);
            yield return new SelectListItem(T("MissingAddress"), SupplierQueueFilter.MissingAddress.ToString(), selectedFilter == SupplierQueueFilter.MissingAddress);
            yield return new SelectListItem(T("HasPurchaseOrders"), SupplierQueueFilter.HasPurchaseOrders.ToString(), selectedFilter == SupplierQueueFilter.HasPurchaseOrders);
            yield return new SelectListItem(T("Inactive"), SupplierQueueFilter.Inactive.ToString(), selectedFilter == SupplierQueueFilter.Inactive);
            yield return new SelectListItem(T("Blocked"), SupplierQueueFilter.Blocked.ToString(), selectedFilter == SupplierQueueFilter.Blocked);
        }

        private async Task PopulateWarehouseOptionsAsync(WarehouseEditVm vm, CancellationToken ct)
        {
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
        }

        private async Task PopulateWarehouseLocationOptionsAsync(WarehouseLocationEditVm vm, CancellationToken ct)
        {
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            vm.WarehouseOptions = await _referenceData.GetWarehouseOptionsAsync(vm.WarehouseId == Guid.Empty ? null : vm.WarehouseId, vm.BusinessId == Guid.Empty ? null : vm.BusinessId, ct).ConfigureAwait(false);
            vm.ParentLocationOptions = await GetWarehouseLocationParentOptionsAsync(vm.BusinessId, vm.WarehouseId, vm.Id, vm.ParentLocationId, ct).ConfigureAwait(false);
            vm.LocationTypeOptions = Enum.GetValues<WarehouseLocationType>()
                .Select(x => new SelectListItem(T(x.ToString()), x.ToString(), x == vm.LocationType))
                .ToList();
            vm.StatusOptions = Enum.GetValues<WarehouseLocationStatus>()
                .Select(x => new SelectListItem(T(x.ToString()), x.ToString(), x == vm.Status))
                .ToList();
        }

        private async Task PopulateWarehouseLabelTemplateOptionsAsync(WarehouseLabelTemplateEditVm vm, CancellationToken ct)
        {
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            vm.StatusOptions = Enum.GetValues<WarehouseLabelTemplateStatus>()
                .Where(x => x != WarehouseLabelTemplateStatus.Archived)
                .Select(x => new SelectListItem(T(x.ToString()), x.ToString(), x == vm.Status))
                .ToList();
            vm.FormatOptions = Enum.GetValues<WarehouseLabelTemplateFormat>()
                .Select(x => new SelectListItem(T(x.ToString()), x.ToString(), x == vm.Format))
                .ToList();
        }

        private async Task PopulateWarehouseTaskOptionsAsync(WarehouseTaskEditVm vm, CancellationToken ct)
        {
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            vm.WarehouseOptions = await _referenceData.GetWarehouseOptionsAsync(vm.WarehouseId == Guid.Empty ? null : vm.WarehouseId, vm.BusinessId == Guid.Empty ? null : vm.BusinessId, ct).ConfigureAwait(false);
            vm.LocationOptions = await GetWarehouseTaskLocationOptionsAsync(vm.BusinessId, vm.WarehouseId, vm.FromLocationId ?? vm.ToLocationId, ct).ConfigureAwait(false);
            vm.UserOptions = await _referenceData.GetUserOptionsAsync(vm.AssignedToUserId, includeEmpty: true, ct).ConfigureAwait(false);
            vm.TaskTypeOptions = Enum.GetValues<WarehouseTaskType>().Select(x => new SelectListItem(T(x.ToString()), x.ToString(), x == vm.TaskType)).ToList();
            vm.StatusOptions = Enum.GetValues<WarehouseTaskStatus>()
                .Where(x => x is WarehouseTaskStatus.Draft or WarehouseTaskStatus.Ready)
                .Select(x => new SelectListItem(T(x.ToString()), x.ToString(), x == vm.Status))
                .ToList();
            vm.PriorityOptions = Enum.GetValues<WarehouseTaskPriority>().Select(x => new SelectListItem(T(x.ToString()), x.ToString(), x == vm.Priority)).ToList();
            vm.SourceTypeOptions = Enum.GetValues<WarehouseTaskSourceType>().Select(x => new SelectListItem(T(x.ToString()), x.ToString(), x == vm.SourceType)).ToList();
            vm.Lines ??= new List<WarehouseTaskLineVm>();
            if (vm.Lines.Count == 0)
            {
                vm.Lines.Add(new WarehouseTaskLineVm { RequestedQuantity = 1, SortOrder = 1 });
            }
            foreach (var line in vm.Lines)
            {
                EnsureInventoryIdentityRows(line.Identities, Math.Max(1, line.RequestedQuantity - line.ShortQuantity));
            }
            await PopulateInventoryIdentityOptionsAsync(
                lots => vm.LotOptions = lots,
                serials => vm.SerialUnitOptions = serials,
                handlingUnits => vm.HandlingUnitOptions = handlingUnits,
                ct).ConfigureAwait(false);
        }

        private async Task PopulateStockCountOptionsAsync(StockCountEditVm vm, CancellationToken ct)
        {
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            vm.WarehouseOptions = await _referenceData.GetWarehouseOptionsAsync(vm.WarehouseId == Guid.Empty ? null : vm.WarehouseId, vm.BusinessId == Guid.Empty ? null : vm.BusinessId, ct).ConfigureAwait(false);
            vm.LocationOptions = await GetWarehouseTaskLocationOptionsAsync(vm.BusinessId, vm.WarehouseId, vm.LocationId, ct).ConfigureAwait(false);
            vm.UserOptions = await _referenceData.GetUserOptionsAsync(vm.AssignedToUserId, includeEmpty: true, ct).ConfigureAwait(false);
            vm.CountTypeOptions = Enum.GetValues<StockCountType>().Select(x => new SelectListItem(T(x.ToString()), x.ToString(), x == vm.CountType)).ToList();
            vm.ReviewStatusOptions = Enum.GetValues<StockCountLineReviewStatus>().Select(x => new SelectListItem(T(x.ToString()), x.ToString())).ToList();
            EnsureStockCountRows(vm);
            foreach (var line in vm.Lines)
            {
                EnsureInventoryIdentityRows(line.Identities, Math.Max(1, line.CountedQuantity));
            }
            await PopulateInventoryIdentityOptionsAsync(
                lots => vm.LotOptions = lots,
                serials => vm.SerialUnitOptions = serials,
                handlingUnits => vm.HandlingUnitOptions = handlingUnits,
                ct).ConfigureAwait(false);
        }

        private async Task<List<SelectListItem>> GetWarehouseOptionsAsync(Guid? selectedWarehouseId, CancellationToken ct)
        {
            var businessId = await _referenceData.ResolveBusinessIdAsync(null, ct).ConfigureAwait(false);
            return await _referenceData.GetWarehouseOptionsAsync(selectedWarehouseId, businessId, ct).ConfigureAwait(false);
        }

        private async Task<List<SelectListItem>> GetWarehouseLocationParentOptionsAsync(Guid businessId, Guid warehouseId, Guid currentLocationId, Guid? selectedParentLocationId, CancellationToken ct)
        {
            var options = new List<SelectListItem> { new(T("NoParentLocation"), string.Empty, !selectedParentLocationId.HasValue) };
            if (businessId == Guid.Empty || warehouseId == Guid.Empty)
            {
                return options;
            }

            var tree = await _getWarehouseLocationTree.HandleAsync(businessId, warehouseId, ct).ConfigureAwait(false);
            void AddItems(IEnumerable<WarehouseLocationTreeItemDto> items, int depth)
            {
                foreach (var item in items)
                {
                    if (item.Id != currentLocationId)
                    {
                        var prefix = depth == 0 ? string.Empty : new string('-', depth * 2) + " ";
                        options.Add(new SelectListItem($"{prefix}{item.Code} - {item.DisplayName}", item.Id.ToString(), selectedParentLocationId == item.Id));
                    }

                    AddItems(item.Children, depth + 1);
                }
            }

            AddItems(tree, 0);
            return options;
        }

        private static WarehouseLocationListItemVm MapWarehouseLocationItem(WarehouseLocationListItemDto dto) => new()
        {
            Id = dto.Id,
            RowVersion = dto.RowVersion,
            BusinessId = dto.BusinessId,
            WarehouseId = dto.WarehouseId,
            WarehouseName = dto.WarehouseName,
            ParentLocationId = dto.ParentLocationId,
            ParentCode = dto.ParentCode,
            Code = dto.Code,
            DisplayName = dto.DisplayName,
            LocationType = dto.LocationType,
            Status = dto.Status,
            Barcode = dto.Barcode,
            SortOrder = dto.SortOrder,
            ChildCount = dto.ChildCount
        };

        private static WarehouseLocationTreeItemVm MapWarehouseLocationTreeItem(WarehouseLocationTreeItemDto dto) => new()
        {
            Id = dto.Id,
            ParentLocationId = dto.ParentLocationId,
            Code = dto.Code,
            DisplayName = dto.DisplayName,
            LocationType = dto.LocationType,
            Status = dto.Status,
            SortOrder = dto.SortOrder,
            Children = dto.Children.Select(MapWarehouseLocationTreeItem).ToList()
        };

        private static WarehouseLabelTemplateListItemVm MapWarehouseLabelTemplateItem(WarehouseLabelTemplateListItemDto dto) => new()
        {
            Id = dto.Id,
            RowVersion = dto.RowVersion,
            BusinessId = dto.BusinessId,
            Name = dto.Name,
            TemplateKey = dto.TemplateKey,
            Status = dto.Status,
            Format = dto.Format,
            IsDefault = dto.IsDefault,
            WidthMm = dto.WidthMm,
            HeightMm = dto.HeightMm
        };

        private static WarehouseLabelTemplateEditVm MapWarehouseLabelTemplateEditor(WarehouseLabelTemplateDetailDto dto) => new()
        {
            Id = dto.Id,
            RowVersion = dto.RowVersion,
            BusinessId = dto.BusinessId,
            Name = dto.Name,
            TemplateKey = dto.TemplateKey,
            Status = dto.Status,
            Format = dto.Format,
            IsDefault = dto.IsDefault,
            WidthMm = dto.WidthMm,
            HeightMm = dto.HeightMm,
            ContentTemplate = dto.ContentTemplate,
            Description = dto.Description,
            MetadataJson = dto.MetadataJson
        };

        private static WarehouseLabelTemplateCreateDto MapWarehouseLabelTemplateCreateDto(WarehouseLabelTemplateEditVm vm) => new()
        {
            BusinessId = vm.BusinessId,
            Name = vm.Name,
            TemplateKey = vm.TemplateKey,
            Status = vm.Status,
            Format = vm.Format,
            IsDefault = vm.IsDefault,
            WidthMm = vm.WidthMm,
            HeightMm = vm.HeightMm,
            ContentTemplate = vm.ContentTemplate,
            Description = vm.Description,
            MetadataJson = vm.MetadataJson
        };

        private static WarehouseLabelTemplateEditDto MapWarehouseLabelTemplateEditDto(WarehouseLabelTemplateEditVm vm) => new()
        {
            Id = vm.Id,
            RowVersion = vm.RowVersion,
            BusinessId = vm.BusinessId,
            Name = vm.Name,
            TemplateKey = vm.TemplateKey,
            Status = vm.Status,
            Format = vm.Format,
            IsDefault = vm.IsDefault,
            WidthMm = vm.WidthMm,
            HeightMm = vm.HeightMm,
            ContentTemplate = vm.ContentTemplate,
            Description = vm.Description,
            MetadataJson = vm.MetadataJson
        };

        private static WarehouseTaskListItemVm MapWarehouseTaskItem(WarehouseTaskListItemDto dto) => new()
        {
            Id = dto.Id,
            RowVersion = dto.RowVersion,
            BusinessId = dto.BusinessId,
            WarehouseId = dto.WarehouseId,
            WarehouseName = dto.WarehouseName,
            FromLocationCode = dto.FromLocationCode,
            ToLocationCode = dto.ToLocationCode,
            AssignedToUserId = dto.AssignedToUserId,
            AssignedToDisplayName = dto.AssignedToDisplayName,
            TaskNumber = dto.TaskNumber,
            Title = dto.Title,
            TaskType = dto.TaskType,
            Status = dto.Status,
            Priority = dto.Priority,
            SourceType = dto.SourceType,
            SourceEntityId = dto.SourceEntityId,
            DueAtUtc = dto.DueAtUtc,
            LineCount = dto.LineCount,
            RequestedQuantity = dto.RequestedQuantity,
            CompletedQuantity = dto.CompletedQuantity,
            ShortQuantity = dto.ShortQuantity,
            HasShortage = dto.HasShortage
        };

        private static WarehouseTaskEditVm MapWarehouseTaskEditor(WarehouseTaskDetailDto dto) => new()
        {
            Id = dto.Id,
            RowVersion = dto.RowVersion,
            BusinessId = dto.BusinessId,
            WarehouseId = dto.WarehouseId,
            WarehouseName = dto.WarehouseName,
            FromLocationId = dto.FromLocationId,
            FromLocationCode = dto.FromLocationCode,
            ToLocationId = dto.ToLocationId,
            ToLocationCode = dto.ToLocationCode,
            AssignedToUserId = dto.AssignedToUserId,
            AssignedToDisplayName = dto.AssignedToDisplayName,
            TaskNumber = dto.TaskNumber,
            Title = dto.Title,
            TaskType = dto.TaskType,
            Status = dto.Status,
            Priority = dto.Priority,
            SourceType = dto.SourceType,
            SourceEntityId = dto.SourceEntityId,
            DueAtUtc = dto.DueAtUtc,
            ReadyAtUtc = dto.ReadyAtUtc,
            AssignedAtUtc = dto.AssignedAtUtc,
            StartedAtUtc = dto.StartedAtUtc,
            CompletedAtUtc = dto.CompletedAtUtc,
            CancelledAtUtc = dto.CancelledAtUtc,
            InternalNotes = dto.InternalNotes,
            MetadataJson = dto.MetadataJson,
            Lines = dto.Lines.Select(MapWarehouseTaskLineVm).ToList()
        };

        private static WarehouseTaskLineVm MapWarehouseTaskLineVm(WarehouseTaskLineDto dto) => new()
        {
            Id = dto.Id,
            ProductVariantId = dto.ProductVariantId,
            FromLocationId = dto.FromLocationId,
            ToLocationId = dto.ToLocationId,
            SkuSnapshot = dto.SkuSnapshot,
            Description = dto.Description,
            RequestedQuantity = dto.RequestedQuantity,
            CompletedQuantity = dto.CompletedQuantity,
            ShortQuantity = dto.ShortQuantity,
            ShortReason = dto.ShortReason,
            SortOrder = dto.SortOrder,
            SourceLineType = dto.SourceLineType,
            SourceLineId = dto.SourceLineId,
            MetadataJson = dto.MetadataJson,
            Identities = dto.Identities.Select(MapIdentityVm).ToList()
        };

        private static WarehouseTaskCreateDto MapWarehouseTaskCreateDto(WarehouseTaskEditVm vm) => new()
        {
            BusinessId = vm.BusinessId,
            WarehouseId = vm.WarehouseId,
            FromLocationId = vm.FromLocationId,
            ToLocationId = vm.ToLocationId,
            AssignedToUserId = vm.AssignedToUserId,
            Title = vm.Title,
            TaskType = vm.TaskType,
            Status = vm.Status,
            Priority = vm.Priority,
            SourceType = vm.SourceType,
            SourceEntityId = vm.SourceEntityId,
            DueAtUtc = vm.DueAtUtc,
            InternalNotes = vm.InternalNotes,
            MetadataJson = vm.MetadataJson,
            Lines = vm.Lines.Select(MapWarehouseTaskLineDto).ToList()
        };

        private static WarehouseTaskEditDto MapWarehouseTaskEditDto(WarehouseTaskEditVm vm)
        {
            var dto = new WarehouseTaskEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                BusinessId = vm.BusinessId,
                WarehouseId = vm.WarehouseId,
                FromLocationId = vm.FromLocationId,
                ToLocationId = vm.ToLocationId,
                AssignedToUserId = vm.AssignedToUserId,
                Title = vm.Title,
                TaskType = vm.TaskType,
                Status = vm.Status,
                Priority = vm.Priority,
                SourceType = vm.SourceType,
                SourceEntityId = vm.SourceEntityId,
                DueAtUtc = vm.DueAtUtc,
                InternalNotes = vm.InternalNotes,
                MetadataJson = vm.MetadataJson,
                Lines = vm.Lines.Select(MapWarehouseTaskLineDto).ToList()
            };
            return dto;
        }

        private static WarehouseTaskLineDto MapWarehouseTaskLineDto(WarehouseTaskLineVm vm) => new()
        {
            Id = vm.Id,
            ProductVariantId = vm.ProductVariantId,
            FromLocationId = vm.FromLocationId,
            ToLocationId = vm.ToLocationId,
            SkuSnapshot = vm.SkuSnapshot,
            Description = vm.Description,
            RequestedQuantity = vm.RequestedQuantity,
            CompletedQuantity = vm.CompletedQuantity,
            ShortQuantity = vm.ShortQuantity,
            ShortReason = vm.ShortReason,
            SortOrder = vm.SortOrder,
            SourceLineType = vm.SourceLineType,
            SourceLineId = vm.SourceLineId,
            MetadataJson = vm.MetadataJson,
            Identities = vm.Identities.Select(MapIdentityDto).ToList()
        };

        private static InventoryIdentityEvidenceVm MapIdentityVm(InventoryIdentityEvidenceDto dto) => new()
        {
            Id = dto.Id,
            InventoryLotId = dto.InventoryLotId,
            InventorySerialUnitId = dto.InventorySerialUnitId,
            HandlingUnitId = dto.HandlingUnitId,
            Quantity = dto.Quantity,
            LotCodeSnapshot = dto.LotCodeSnapshot,
            SupplierLotCodeSnapshot = dto.SupplierLotCodeSnapshot,
            ExpiryDateUtc = dto.ExpiryDateUtc,
            SerialNumberSnapshot = dto.SerialNumberSnapshot,
            HandlingUnitCodeSnapshot = dto.HandlingUnitCodeSnapshot,
            SortOrder = dto.SortOrder,
            MetadataJson = dto.MetadataJson
        };

        private static InventoryIdentityEvidenceDto MapIdentityDto(InventoryIdentityEvidenceVm vm) => new()
        {
            Id = vm.Id,
            InventoryLotId = vm.InventoryLotId,
            InventorySerialUnitId = vm.InventorySerialUnitId,
            HandlingUnitId = vm.HandlingUnitId,
            Quantity = vm.Quantity,
            LotCodeSnapshot = vm.LotCodeSnapshot,
            SupplierLotCodeSnapshot = vm.SupplierLotCodeSnapshot,
            ExpiryDateUtc = vm.ExpiryDateUtc,
            SerialNumberSnapshot = vm.SerialNumberSnapshot,
            HandlingUnitCodeSnapshot = vm.HandlingUnitCodeSnapshot,
            SortOrder = vm.SortOrder,
            MetadataJson = vm.MetadataJson
        };

        private async Task<List<SelectListItem>> GetWarehouseTaskLocationOptionsAsync(Guid businessId, Guid warehouseId, Guid? selectedLocationId, CancellationToken ct)
        {
            var options = new List<SelectListItem> { new(T("NoLocation"), string.Empty, !selectedLocationId.HasValue) };
            if (businessId == Guid.Empty || warehouseId == Guid.Empty)
            {
                return options;
            }

            var locations = await _getWarehouseLocationsPage.HandleAsync(businessId, warehouseId, 1, 200, null, WarehouseLocationQueueFilter.Active, ct).ConfigureAwait(false);
            options.AddRange(locations.Items.Select(x => new SelectListItem($"{x.Code} - {x.DisplayName}", x.Id.ToString(), selectedLocationId == x.Id)));
            return options;
        }

        private async Task PopulateGoodsReceiptIdentityOptionsAsync(GoodsReceiptDetailVm vm, CancellationToken ct)
        {
            vm.LotOptions = new List<SelectListItem> { new(T("NoLot"), string.Empty) };
            vm.SerialUnitOptions = new List<SelectListItem> { new(T("NoSerialUnit"), string.Empty) };
            vm.HandlingUnitOptions = new List<SelectListItem> { new(T("NoHandlingUnit"), string.Empty) };
            if (vm.BusinessId == Guid.Empty)
            {
                return;
            }

            var lots = await _getInventoryLotsPage.HandleAsync(vm.BusinessId, 1, 200, null, InventoryLotQueueFilter.Active, ct).ConfigureAwait(false);
            vm.LotOptions.AddRange(lots.Items.Select(x =>
            {
                var label = string.IsNullOrWhiteSpace(x.SupplierLotCode)
                    ? $"{x.VariantSku} / {x.LotCode}"
                    : $"{x.VariantSku} / {x.LotCode} / {x.SupplierLotCode}";
                return new SelectListItem(label, x.Id.ToString());
            }));

            var serials = await _getInventorySerialUnitsPage.HandleAsync(vm.BusinessId, 1, 200, null, InventorySerialUnitQueueFilter.Received, ct).ConfigureAwait(false);
            vm.SerialUnitOptions.AddRange(serials.Items.Select(x =>
            {
                var label = string.IsNullOrWhiteSpace(x.LotCode)
                    ? $"{x.VariantSku} / {x.SerialNumber}"
                    : $"{x.VariantSku} / {x.SerialNumber} / {x.LotCode}";
                return new SelectListItem(label, x.Id.ToString());
            }));

            var handlingUnits = await _getHandlingUnitsPage.HandleAsync(vm.BusinessId, 1, 200, null, HandlingUnitQueueFilter.Open, ct).ConfigureAwait(false);
            vm.HandlingUnitOptions.AddRange(handlingUnits.Items.Select(x => new SelectListItem($"{x.Code} - {x.DisplayName}", x.Id.ToString())));

            if (string.Equals(vm.Status, GoodsReceiptStatus.Received.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                EnsureGoodsReceiptIdentityRows(vm);
            }
        }

        private static void EnsureGoodsReceiptIdentityRows(GoodsReceiptDetailVm vm)
        {
            foreach (var line in vm.Lines)
            {
                var requiredRows = Math.Max(3, line.AcceptedQuantity > 0 ? line.AcceptedQuantity : line.ReceivedQuantity);
                var targetRows = Math.Min(50, Math.Max(line.Identities.Count, requiredRows));
                while (line.Identities.Count < targetRows)
                {
                    line.Identities.Add(new GoodsReceiptLineIdentityVm
                    {
                        GoodsReceiptLineId = line.Id,
                        ProductVariantId = line.ProductVariantId,
                        SortOrder = line.Identities.Count + 1
                    });
                }
            }
        }

        private async Task PopulateInventoryIdentityOptionsAsync(
            Action<List<SelectListItem>> assignLots,
            Action<List<SelectListItem>> assignSerialUnits,
            Action<List<SelectListItem>> assignHandlingUnits,
            CancellationToken ct)
        {
            var businessId = await _referenceData.ResolveBusinessIdAsync(null, ct).ConfigureAwait(false);
            var lotOptions = new List<SelectListItem> { new(T("NoLot"), string.Empty) };
            var serialOptions = new List<SelectListItem> { new(T("NoSerialUnit"), string.Empty) };
            var handlingUnitOptions = new List<SelectListItem> { new(T("NoHandlingUnit"), string.Empty) };

            if (businessId.HasValue && businessId.Value != Guid.Empty)
            {
                var lots = await _getInventoryLotsPage.HandleAsync(businessId.Value, 1, 200, null, InventoryLotQueueFilter.Active, ct).ConfigureAwait(false);
                lotOptions.AddRange(lots.Items.Select(x =>
                {
                    var label = string.IsNullOrWhiteSpace(x.SupplierLotCode)
                        ? $"{x.VariantSku} / {x.LotCode}"
                        : $"{x.VariantSku} / {x.LotCode} / {x.SupplierLotCode}";
                    return new SelectListItem(label, x.Id.ToString());
                }));

                var serials = await _getInventorySerialUnitsPage.HandleAsync(businessId.Value, 1, 200, null, InventorySerialUnitQueueFilter.Received, ct).ConfigureAwait(false);
                serialOptions.AddRange(serials.Items.Select(x =>
                {
                    var label = string.IsNullOrWhiteSpace(x.LotCode)
                        ? $"{x.VariantSku} / {x.SerialNumber}"
                        : $"{x.VariantSku} / {x.SerialNumber} / {x.LotCode}";
                    return new SelectListItem(label, x.Id.ToString());
                }));

                var handlingUnits = await _getHandlingUnitsPage.HandleAsync(businessId.Value, 1, 200, null, HandlingUnitQueueFilter.Open, ct).ConfigureAwait(false);
                handlingUnitOptions.AddRange(handlingUnits.Items.Select(x => new SelectListItem($"{x.Code} - {x.DisplayName}", x.Id.ToString())));
            }

            assignLots(lotOptions);
            assignSerialUnits(serialOptions);
            assignHandlingUnits(handlingUnitOptions);
        }

        private async Task<WarehouseLocationLabelPrintVm> BuildWarehouseLocationLabelPrintVmAsync(Guid businessId, Guid templateId, Guid[] locationIds, CancellationToken ct)
        {
            var templates = await _getWarehouseLabelTemplatesPage.HandleAsync(businessId, 1, 100, null, WarehouseLabelTemplateQueueFilter.Active, ct).ConfigureAwait(false);
            var locations = await _getWarehouseLocationsPage.HandleAsync(businessId, null, 1, 100, null, WarehouseLocationQueueFilter.All, ct).ConfigureAwait(false);
            var selectedTemplateId = templateId != Guid.Empty
                ? templateId
                : templates.Items.FirstOrDefault(x => x.IsDefault)?.Id ?? templates.Items.FirstOrDefault()?.Id ?? Guid.Empty;
            var selectedLocationIds = locationIds.Where(x => x != Guid.Empty).Distinct().ToArray();
            var render = selectedTemplateId == Guid.Empty || selectedLocationIds.Length == 0
                ? null
                : await _renderWarehouseLocationLabels.HandleAsync(businessId, selectedTemplateId, selectedLocationIds, ct).ConfigureAwait(false);

            return new WarehouseLocationLabelPrintVm
            {
                BusinessId = businessId,
                TemplateId = selectedTemplateId,
                TemplateOptions = templates.Items
                    .Select(x => new SelectListItem($"{x.Name} ({x.WidthMm}x{x.HeightMm} mm)", x.Id.ToString(), x.Id == selectedTemplateId))
                    .ToList(),
                Locations = locations.Items.Select(MapWarehouseLocationItem).ToList(),
                LocationIds = selectedLocationIds.ToList(),
                Render = render is null ? null : new WarehouseLocationLabelRenderVm
                {
                    Format = render.Format,
                    WidthMm = render.WidthMm,
                    HeightMm = render.HeightMm,
                    Labels = render.Labels.Select(x => new WarehouseLocationLabelItemVm
                    {
                        LocationId = x.LocationId,
                        WarehouseName = x.WarehouseName,
                        Code = x.Code,
                        DisplayName = x.DisplayName,
                        Barcode = x.Barcode,
                        RenderedContent = x.RenderedContent
                    }).ToList()
                }
            };
        }

        private static WarehouseLocationEditVm MapWarehouseLocationEditor(WarehouseLocationDetailDto dto) => new()
        {
            Id = dto.Id,
            RowVersion = dto.RowVersion,
            BusinessId = dto.BusinessId,
            WarehouseId = dto.WarehouseId,
            WarehouseName = dto.WarehouseName,
            ParentLocationId = dto.ParentLocationId,
            ParentCode = dto.ParentCode,
            Code = dto.Code,
            DisplayName = dto.DisplayName,
            LocationType = dto.LocationType,
            Status = dto.Status,
            Barcode = dto.Barcode,
            SortOrder = dto.SortOrder,
            Description = dto.Description,
            MetadataJson = dto.MetadataJson,
            Children = dto.Children.Select(MapWarehouseLocationTreeItem).ToList()
        };

        private async Task PopulateSupplierOptionsAsync(SupplierEditVm vm, CancellationToken ct)
        {
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            vm.Contacts ??= new List<SupplierContactVm>();
            vm.Documents ??= new List<SupplierDocumentVm>();
            vm.NewContact ??= new SupplierContactVm();
            vm.NewContact.BusinessId = vm.BusinessId;
            vm.NewContact.SupplierId = vm.Id;
            vm.NewDocument ??= new SupplierDocumentRegisterVm();
            vm.NewDocument.BusinessId = vm.BusinessId;
            vm.NewDocument.SupplierId = vm.Id;
        }

        private static bool IsValidSupplierContactAction(Guid supplierId, Guid businessId)
            => supplierId != Guid.Empty && businessId != Guid.Empty;

        private static SupplierEditVm MapSupplierEditVm(SupplierEditDto dto)
        {
            var vm = new SupplierEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                BusinessId = dto.BusinessId,
                Name = dto.Name,
                Code = dto.Code,
                Status = dto.Status,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                Notes = dto.Notes,
                PreferredCurrency = dto.PreferredCurrency,
                PaymentTermDays = dto.PaymentTermDays,
                LeadTimeDays = dto.LeadTimeDays,
                Website = dto.Website,
                TaxRegistrationNumber = dto.TaxRegistrationNumber,
                ExternalNotes = dto.ExternalNotes,
                Contacts = dto.Contacts.Select(MapSupplierContactVm).ToList(),
                Documents = dto.Documents.Select(MapSupplierDocumentVm).ToList()
            };
            vm.NewContact.BusinessId = vm.BusinessId;
            vm.NewContact.SupplierId = vm.Id;
            vm.NewDocument.BusinessId = vm.BusinessId;
            vm.NewDocument.SupplierId = vm.Id;
            return vm;
        }

        private static SupplierContactVm MapSupplierContactVm(SupplierContactDto dto) => new()
        {
            Id = dto.Id,
            RowVersion = dto.RowVersion,
            BusinessId = dto.BusinessId,
            SupplierId = dto.SupplierId,
            Role = dto.Role,
            Name = dto.Name,
            JobTitle = dto.JobTitle,
            Email = dto.Email,
            Phone = dto.Phone,
            LanguageCode = dto.LanguageCode,
            IsPrimary = dto.IsPrimary,
            Notes = dto.Notes
        };

        private static SupplierDocumentVm MapSupplierDocumentVm(SupplierDocumentDto dto) => new()
        {
            Id = dto.Id,
            DocumentKind = dto.DocumentKind,
            Title = dto.Title,
            FileName = dto.FileName,
            ContentType = dto.ContentType,
            SizeBytes = dto.SizeBytes,
            Visibility = dto.Visibility,
            MetadataJson = dto.MetadataJson
        };

        private static SupplierContactEditDto MapSupplierContactEditDto(SupplierContactVm vm) => new()
        {
            Id = vm.Id,
            RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
            BusinessId = vm.BusinessId,
            SupplierId = vm.SupplierId,
            Role = vm.Role,
            Name = vm.Name,
            JobTitle = vm.JobTitle,
            Email = vm.Email,
            Phone = vm.Phone,
            LanguageCode = vm.LanguageCode,
            IsPrimary = vm.IsPrimary,
            Notes = vm.Notes
        };

        private async Task PopulateStockTransferOptionsAsync(StockTransferEditVm vm, Guid? businessId, CancellationToken ct)
        {
            var resolvedBusinessId = businessId ?? await _referenceData.ResolveBusinessIdAsync(null, ct).ConfigureAwait(false);
            vm.WarehouseOptions = await _referenceData.GetWarehouseOptionsAsync(null, resolvedBusinessId, ct).ConfigureAwait(false);
            vm.VariantOptions = await _referenceData.GetVariantOptionsAsync(null, ct).ConfigureAwait(false);
            foreach (var line in vm.Lines)
            {
                EnsureInventoryIdentityRows(line.Identities, Math.Max(1, line.Quantity));
            }
            await PopulateInventoryIdentityOptionsAsync(
                lots => vm.LotOptions = lots,
                serials => vm.SerialUnitOptions = serials,
                handlingUnits => vm.HandlingUnitOptions = handlingUnits,
                ct).ConfigureAwait(false);
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

        private async Task<List<SelectListItem>> BuildGoodsReceiptPurchaseOrderOptionsAsync(Guid? businessId, CancellationToken ct)
        {
            if (!businessId.HasValue)
            {
                return new List<SelectListItem>();
            }

            var result = await _getPurchaseOrdersPage
                .HandleAsync(businessId.Value, page: 1, pageSize: 200, query: null, filter: PurchaseOrderQueueFilter.Issued, ct)
                .ConfigureAwait(false);

            return result.Items
                .Where(x => x.OrderedQuantity > x.ReceivedQuantity)
                .Select(x => new SelectListItem($"{x.OrderNumber} - {x.SupplierName}", x.Id.ToString()))
                .ToList();
        }

        private static void EnsureStockTransferRows(StockTransferEditVm vm)
        {
            vm.Lines ??= new List<StockTransferLineVm>();
            if (vm.Lines.Count == 0)
            {
                vm.Lines.Add(new StockTransferLineVm());
            }
        }

        private static void EnsureInventoryIdentityRows(List<InventoryIdentityEvidenceVm> identities, int requiredQuantity)
        {
            var targetRows = Math.Min(20, Math.Max(identities.Count, Math.Min(3, Math.Max(1, requiredQuantity))));
            while (identities.Count < targetRows)
            {
                identities.Add(new InventoryIdentityEvidenceVm { Quantity = 1, SortOrder = identities.Count + 1 });
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

        private IActionResult RenderWarehouseLocationsWorkspace(WarehouseLocationsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/Locations.cshtml", vm);
            }

            return View("Locations", vm);
        }

        private IActionResult RenderWarehouseLocationEditor(WarehouseLocationEditVm vm, bool isCreate)
        {
            ViewData["IsCreate"] = isCreate;
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/LocationEditor.cshtml", vm);
            }

            return View("LocationEditor", vm);
        }

        private IActionResult RenderWarehouseLabelTemplatesWorkspace(WarehouseLabelTemplatesListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/LabelTemplates.cshtml", vm);
            }

            return View("LabelTemplates", vm);
        }

        private IActionResult RenderWarehouseLabelTemplateEditor(WarehouseLabelTemplateEditVm vm, bool isCreate)
        {
            ViewData["IsCreate"] = isCreate;
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/LabelTemplateEditor.cshtml", vm);
            }

            return View("LabelTemplateEditor", vm);
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

        private IActionResult RenderBinStockWorkspace(BinStockListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/BinStock.cshtml", vm);
            }

            return View("BinStock", vm);
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

        private IActionResult RenderGoodsReceiptsWorkspace(GoodsReceiptsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/GoodsReceipts.cshtml", vm);
            }

            return View("GoodsReceipts", vm);
        }

        private IActionResult RenderGoodsReceiptDetail(GoodsReceiptDetailVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/EditGoodsReceipt.cshtml", vm);
            }

            return View("EditGoodsReceipt", vm);
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

        private IActionResult RenderWarehouseTasksWorkspace(WarehouseTasksListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/WarehouseTasks.cshtml", vm);
            }

            return View("WarehouseTasks", vm);
        }

        private IActionResult RenderWarehousePwaWorkspace(WarehousePwaVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/WarehousePwa.cshtml", vm);
            }

            return View("WarehousePwa", vm);
        }

        private IActionResult RenderWarehouseTaskEditor(WarehouseTaskEditVm vm, bool isCreate)
        {
            if (IsHtmxRequest())
            {
                ViewData["IsCreate"] = isCreate;
                return PartialView("~/Views/Inventory/_WarehouseTaskEditorShell.cshtml", vm);
            }

            return isCreate ? View("CreateWarehouseTask", vm) : View("EditWarehouseTask", vm);
        }

        private IActionResult RenderStockCountsWorkspace(StockCountsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Inventory/StockCounts.cshtml", vm);
            }

            return View("StockCounts", vm);
        }

        private IActionResult RenderStockCountEditor(StockCountEditVm vm, bool isCreate)
        {
            if (IsHtmxRequest())
            {
                ViewData["IsCreate"] = isCreate;
                return PartialView("~/Views/Inventory/_StockCountEditorShell.cshtml", vm);
            }

            return isCreate ? View("CreateStockCount", vm) : View("EditStockCount", vm);
        }

        private static StockCountListItemVm MapStockCountItem(StockCountListItemDto dto) => new()
        {
            Id = dto.Id,
            RowVersion = dto.RowVersion,
            BusinessId = dto.BusinessId,
            WarehouseId = dto.WarehouseId,
            WarehouseName = dto.WarehouseName,
            LocationId = dto.LocationId,
            LocationCode = dto.LocationCode,
            CountNumber = dto.CountNumber,
            Title = dto.Title,
            CountType = dto.CountType,
            Status = dto.Status,
            CountWindowStartUtc = dto.CountWindowStartUtc,
            CountWindowEndUtc = dto.CountWindowEndUtc,
            LineCount = dto.LineCount,
            VarianceLineCount = dto.VarianceLineCount,
            TotalExpectedQuantity = dto.TotalExpectedQuantity,
            TotalCountedQuantity = dto.TotalCountedQuantity,
            TotalVarianceQuantity = dto.TotalVarianceQuantity
        };

        private static StockCountEditVm MapStockCountEditor(StockCountDetailDto dto) => new()
        {
            Id = dto.Id,
            RowVersion = dto.RowVersion,
            BusinessId = dto.BusinessId,
            WarehouseId = dto.WarehouseId,
            WarehouseName = dto.WarehouseName,
            LocationId = dto.LocationId,
            LocationCode = dto.LocationCode,
            AssignedToUserId = dto.AssignedToUserId,
            CountNumber = dto.CountNumber,
            Title = dto.Title,
            CountType = dto.CountType,
            Status = dto.Status,
            CountWindowStartUtc = dto.CountWindowStartUtc,
            CountWindowEndUtc = dto.CountWindowEndUtc,
            PreparedAtUtc = dto.PreparedAtUtc,
            StartedAtUtc = dto.StartedAtUtc,
            CountedAtUtc = dto.CountedAtUtc,
            ReviewRequestedAtUtc = dto.ReviewRequestedAtUtc,
            ApprovedAtUtc = dto.ApprovedAtUtc,
            PostedAtUtc = dto.PostedAtUtc,
            RejectedAtUtc = dto.RejectedAtUtc,
            CancelledAtUtc = dto.CancelledAtUtc,
            ReviewNotes = dto.ReviewNotes,
            InternalNotes = dto.InternalNotes,
            MetadataJson = dto.MetadataJson,
            Lines = dto.Lines.Select(MapStockCountLine).ToList()
        };

        private static StockCountLineVm MapStockCountLine(StockCountLineDto dto) => new()
        {
            Id = dto.Id,
            ProductVariantId = dto.ProductVariantId,
            LocationId = dto.LocationId,
            SkuSnapshot = dto.SkuSnapshot,
            Description = dto.Description,
            ExpectedQuantity = dto.ExpectedQuantity,
            CountedQuantity = dto.CountedQuantity,
            VarianceQuantity = dto.VarianceQuantity,
            ReviewStatus = dto.ReviewStatus,
            AdjustmentPosted = dto.AdjustmentPosted,
            ReviewNotes = dto.ReviewNotes,
            SortOrder = dto.SortOrder,
            MetadataJson = dto.MetadataJson,
            Identities = dto.Identities.Select(MapIdentityVm).ToList()
        };

        private static StockCountCreateDto MapStockCountCreateDto(StockCountEditVm vm) => new()
        {
            BusinessId = vm.BusinessId,
            WarehouseId = vm.WarehouseId,
            LocationId = vm.LocationId,
            AssignedToUserId = vm.AssignedToUserId,
            Title = vm.Title,
            CountType = vm.CountType,
            CountWindowStartUtc = vm.CountWindowStartUtc,
            CountWindowEndUtc = vm.CountWindowEndUtc,
            InternalNotes = vm.InternalNotes,
            MetadataJson = vm.MetadataJson,
            Lines = vm.Lines.Select(MapStockCountLineDto).ToList()
        };

        private static StockCountEditDto MapStockCountEditDto(StockCountEditVm vm)
        {
            var dto = new StockCountEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                BusinessId = vm.BusinessId,
                WarehouseId = vm.WarehouseId,
                LocationId = vm.LocationId,
                AssignedToUserId = vm.AssignedToUserId,
                Title = vm.Title,
                CountType = vm.CountType,
                CountWindowStartUtc = vm.CountWindowStartUtc,
                CountWindowEndUtc = vm.CountWindowEndUtc,
                InternalNotes = vm.InternalNotes,
                MetadataJson = vm.MetadataJson,
                Lines = vm.Lines.Select(MapStockCountLineDto).ToList()
            };
            return dto;
        }

        private static StockCountLineDto MapStockCountLineDto(StockCountLineVm vm) => new()
        {
            Id = vm.Id,
            ProductVariantId = vm.ProductVariantId,
            LocationId = vm.LocationId,
            SkuSnapshot = vm.SkuSnapshot,
            Description = vm.Description,
            ExpectedQuantity = vm.ExpectedQuantity,
            CountedQuantity = vm.CountedQuantity,
            VarianceQuantity = vm.CountedQuantity - vm.ExpectedQuantity,
            ReviewStatus = vm.ReviewStatus,
            AdjustmentPosted = vm.AdjustmentPosted,
            ReviewNotes = vm.ReviewNotes,
            SortOrder = vm.SortOrder,
            MetadataJson = vm.MetadataJson,
            Identities = vm.Identities.Select(MapIdentityDto).ToList()
        };

        private static void EnsureStockCountRows(StockCountEditVm vm)
        {
            vm.Lines ??= new List<StockCountLineVm>();
            if (vm.Lines.Count == 0)
            {
                vm.Lines.Add(new StockCountLineVm { SortOrder = 1 });
            }
        }

        private static GoodsReceiptListItemVm MapGoodsReceiptListItem(GoodsReceiptListItemDto dto)
        {
            return new GoodsReceiptListItemVm
            {
                Id = dto.Id,
                BusinessId = dto.BusinessId,
                SupplierId = dto.SupplierId,
                PurchaseOrderId = dto.PurchaseOrderId,
                WarehouseId = dto.WarehouseId,
                SupplierName = dto.SupplierName,
                PurchaseOrderNumber = dto.PurchaseOrderNumber,
                WarehouseName = dto.WarehouseName,
                GoodsReceiptNumber = dto.GoodsReceiptNumber,
                Status = dto.Status,
                CreatedAtUtc = dto.CreatedAtUtc,
                ReceivedAtUtc = dto.ReceivedAtUtc,
                PostedAtUtc = dto.PostedAtUtc,
                LineCount = dto.LineCount,
                ReceivedQuantity = dto.ReceivedQuantity,
                AcceptedQuantity = dto.AcceptedQuantity,
                RowVersion = dto.RowVersion
            };
        }

        private static GoodsReceiptDetailVm MapGoodsReceiptDetail(GoodsReceiptDetailDto dto)
        {
            return new GoodsReceiptDetailVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                BusinessId = dto.BusinessId,
                SupplierId = dto.SupplierId,
                PurchaseOrderId = dto.PurchaseOrderId,
                WarehouseId = dto.WarehouseId,
                SupplierName = dto.SupplierName,
                PurchaseOrderNumber = dto.PurchaseOrderNumber,
                WarehouseName = dto.WarehouseName,
                GoodsReceiptNumber = dto.GoodsReceiptNumber,
                Status = dto.Status,
                ReceivedAtUtc = dto.ReceivedAtUtc,
                InspectedAtUtc = dto.InspectedAtUtc,
                PostedAtUtc = dto.PostedAtUtc,
                CancelledAtUtc = dto.CancelledAtUtc,
                InternalNotes = dto.InternalNotes,
                WarehouseTaskAction = new GoodsReceiptWarehouseTaskVm
                {
                    GoodsReceiptId = dto.Id,
                    BusinessId = dto.BusinessId,
                    Priority = WarehouseTaskPriority.Normal
                },
                InlineIdentity = new GoodsReceiptInlineIdentityCreateVm
                {
                    GoodsReceiptId = dto.Id,
                    RowVersion = dto.RowVersion,
                    Quantity = 1
                },
                Lines = dto.Lines.Select(x => new GoodsReceiptLineVm
                {
                    Id = x.Id,
                    PurchaseOrderLineId = x.PurchaseOrderLineId,
                    ProductVariantId = x.ProductVariantId,
                    SupplierSku = x.SupplierSku,
                    Description = x.Description,
                    OrderedQuantity = x.OrderedQuantity,
                    PreviouslyReceivedQuantity = x.PreviouslyReceivedQuantity,
                    RemainingQuantity = x.RemainingQuantity,
                    ReceivedQuantity = x.ReceivedQuantity,
                    AcceptedQuantity = x.AcceptedQuantity,
                    RejectedQuantity = x.RejectedQuantity,
                    DamagedQuantity = x.DamagedQuantity,
                    UnitCostMinor = x.UnitCostMinor,
                    TotalCostMinor = x.TotalCostMinor,
                    SortOrder = x.SortOrder,
                    Identities = x.Identities.Select(identity => new GoodsReceiptLineIdentityVm
                    {
                        Id = identity.Id,
                        GoodsReceiptLineId = identity.GoodsReceiptLineId,
                        ProductVariantId = identity.ProductVariantId,
                        InventoryLotId = identity.InventoryLotId,
                        InventorySerialUnitId = identity.InventorySerialUnitId,
                        HandlingUnitId = identity.HandlingUnitId,
                        Quantity = identity.Quantity,
                        LotCodeSnapshot = identity.LotCodeSnapshot,
                        SupplierLotCodeSnapshot = identity.SupplierLotCodeSnapshot,
                        SerialNumberSnapshot = identity.SerialNumberSnapshot,
                        HandlingUnitCodeSnapshot = identity.HandlingUnitCodeSnapshot,
                        ExpiryDateUtc = identity.ExpiryDateUtc,
                        SortOrder = identity.SortOrder,
                        MetadataJson = identity.MetadataJson
                    }).ToList()
                }).ToList()
            };
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
