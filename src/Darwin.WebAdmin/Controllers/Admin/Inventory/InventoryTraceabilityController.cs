using Darwin.Application.Inventory.Commands;
using Darwin.Application.Inventory.DTOs;
using Darwin.Application.Inventory.Queries;
using Darwin.Domain.Enums;
using Darwin.WebAdmin.Controllers.Admin;
using Darwin.WebAdmin.Services.Admin;
using Darwin.WebAdmin.ViewModels.Inventory;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Darwin.WebAdmin.Controllers.Admin.Inventory;

public sealed class InventoryTraceabilityController : AdminBaseController
{
    private readonly GetProductTrackingPoliciesPageHandler _getPolicies;
    private readonly GetProductTrackingPolicyDetailHandler _getPolicy;
    private readonly CreateProductTrackingPolicyHandler _createPolicy;
    private readonly UpdateProductTrackingPolicyHandler _updatePolicy;
    private readonly ArchiveProductTrackingPolicyHandler _archivePolicy;
    private readonly GetInventoryLotsPageHandler _getLots;
    private readonly GetInventoryLotDetailHandler _getLot;
    private readonly CreateInventoryLotHandler _createLot;
    private readonly UpdateInventoryLotHandler _updateLot;
    private readonly ArchiveInventoryLotHandler _archiveLot;
    private readonly GetInventorySerialUnitsPageHandler _getSerials;
    private readonly GetInventorySerialUnitDetailHandler _getSerial;
    private readonly CreateInventorySerialUnitHandler _createSerial;
    private readonly UpdateInventorySerialUnitHandler _updateSerial;
    private readonly ArchiveInventorySerialUnitHandler _archiveSerial;
    private readonly GetHandlingUnitsPageHandler _getHandlingUnits;
    private readonly GetHandlingUnitDetailHandler _getHandlingUnit;
    private readonly CreateHandlingUnitHandler _createHandlingUnit;
    private readonly UpdateHandlingUnitHandler _updateHandlingUnit;
    private readonly ArchiveHandlingUnitHandler _archiveHandlingUnit;
    private readonly GetWarehouseLocationsPageHandler _getLocations;
    private readonly AdminReferenceDataService _referenceData;

    public InventoryTraceabilityController(
        GetProductTrackingPoliciesPageHandler getPolicies,
        GetProductTrackingPolicyDetailHandler getPolicy,
        CreateProductTrackingPolicyHandler createPolicy,
        UpdateProductTrackingPolicyHandler updatePolicy,
        ArchiveProductTrackingPolicyHandler archivePolicy,
        GetInventoryLotsPageHandler getLots,
        GetInventoryLotDetailHandler getLot,
        CreateInventoryLotHandler createLot,
        UpdateInventoryLotHandler updateLot,
        ArchiveInventoryLotHandler archiveLot,
        GetInventorySerialUnitsPageHandler getSerials,
        GetInventorySerialUnitDetailHandler getSerial,
        CreateInventorySerialUnitHandler createSerial,
        UpdateInventorySerialUnitHandler updateSerial,
        ArchiveInventorySerialUnitHandler archiveSerial,
        GetHandlingUnitsPageHandler getHandlingUnits,
        GetHandlingUnitDetailHandler getHandlingUnit,
        CreateHandlingUnitHandler createHandlingUnit,
        UpdateHandlingUnitHandler updateHandlingUnit,
        ArchiveHandlingUnitHandler archiveHandlingUnit,
        GetWarehouseLocationsPageHandler getLocations,
        AdminReferenceDataService referenceData)
    {
        _getPolicies = getPolicies ?? throw new ArgumentNullException(nameof(getPolicies));
        _getPolicy = getPolicy ?? throw new ArgumentNullException(nameof(getPolicy));
        _createPolicy = createPolicy ?? throw new ArgumentNullException(nameof(createPolicy));
        _updatePolicy = updatePolicy ?? throw new ArgumentNullException(nameof(updatePolicy));
        _archivePolicy = archivePolicy ?? throw new ArgumentNullException(nameof(archivePolicy));
        _getLots = getLots ?? throw new ArgumentNullException(nameof(getLots));
        _getLot = getLot ?? throw new ArgumentNullException(nameof(getLot));
        _createLot = createLot ?? throw new ArgumentNullException(nameof(createLot));
        _updateLot = updateLot ?? throw new ArgumentNullException(nameof(updateLot));
        _archiveLot = archiveLot ?? throw new ArgumentNullException(nameof(archiveLot));
        _getSerials = getSerials ?? throw new ArgumentNullException(nameof(getSerials));
        _getSerial = getSerial ?? throw new ArgumentNullException(nameof(getSerial));
        _createSerial = createSerial ?? throw new ArgumentNullException(nameof(createSerial));
        _updateSerial = updateSerial ?? throw new ArgumentNullException(nameof(updateSerial));
        _archiveSerial = archiveSerial ?? throw new ArgumentNullException(nameof(archiveSerial));
        _getHandlingUnits = getHandlingUnits ?? throw new ArgumentNullException(nameof(getHandlingUnits));
        _getHandlingUnit = getHandlingUnit ?? throw new ArgumentNullException(nameof(getHandlingUnit));
        _createHandlingUnit = createHandlingUnit ?? throw new ArgumentNullException(nameof(createHandlingUnit));
        _updateHandlingUnit = updateHandlingUnit ?? throw new ArgumentNullException(nameof(updateHandlingUnit));
        _archiveHandlingUnit = archiveHandlingUnit ?? throw new ArgumentNullException(nameof(archiveHandlingUnit));
        _getLocations = getLocations ?? throw new ArgumentNullException(nameof(getLocations));
        _referenceData = referenceData ?? throw new ArgumentNullException(nameof(referenceData));
    }

    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(ProductTrackingPolicies));

    [HttpGet]
    public async Task<IActionResult> ProductTrackingPolicies(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, ProductTrackingPolicyQueueFilter filter = ProductTrackingPolicyQueueFilter.All, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
        var vm = new ProductTrackingPoliciesListVm
        {
            BusinessId = businessId,
            Query = q ?? string.Empty,
            Filter = filter,
            FilterItems = BuildEnumOptions(filter),
            BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
            Page = page,
            PageSize = pageSize
        };
        if (businessId.HasValue)
        {
            var result = await _getPolicies.HandleAsync(businessId.Value, page, pageSize, q, filter, ct).ConfigureAwait(false);
            vm.Items = result.Items.Select(MapPolicy).ToList();
            vm.Total = result.Total;
            var summary = await _getPolicies.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);
            vm.Summary = new ProductTrackingPolicyOpsSummaryVm { TotalCount = summary.TotalCount, ActiveCount = summary.ActiveCount, TrackedCount = summary.TrackedCount, RequiresExpiryCount = summary.RequiresExpiryCount, RequiresHandlingUnitCount = summary.RequiresHandlingUnitCount };
        }
        return Render("ProductTrackingPolicies", vm);
    }

    [HttpGet]
    public async Task<IActionResult> CreateProductTrackingPolicy(Guid? businessId = null, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
        var vm = new ProductTrackingPolicyEditVm { BusinessId = businessId ?? Guid.Empty };
        await PopulatePolicyOptionsAsync(vm, ct).ConfigureAwait(false);
        return RenderEditor("ProductTrackingPolicyEditor", vm, true);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProductTrackingPolicy(ProductTrackingPolicyEditVm vm, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            await PopulatePolicyOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderEditor("ProductTrackingPolicyEditor", vm, true);
        }
        try
        {
            await _createPolicy.HandleAsync(MapPolicyCreate(vm), ct).ConfigureAwait(false);
            SetSuccessMessage("ProductTrackingPolicyCreatedMessage");
            return RedirectOrHtmx(nameof(ProductTrackingPolicies), new { businessId = vm.BusinessId });
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ValidationException)
        {
            AddModelErrorMessage("ProductTrackingPolicySaveFailedMessage");
            await PopulatePolicyOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderEditor("ProductTrackingPolicyEditor", vm, true);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditProductTrackingPolicy(Guid id, CancellationToken ct = default)
    {
        var dto = await _getPolicy.HandleAsync(id, ct).ConfigureAwait(false);
        if (dto is null)
        {
            SetErrorMessage("ProductTrackingPolicyNotFound");
            return RedirectToAction(nameof(ProductTrackingPolicies));
        }
        var vm = MapPolicyEditor(dto);
        await PopulatePolicyOptionsAsync(vm, ct).ConfigureAwait(false);
        return RenderEditor("ProductTrackingPolicyEditor", vm, false);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProductTrackingPolicy(ProductTrackingPolicyEditVm vm, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            await PopulatePolicyOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderEditor("ProductTrackingPolicyEditor", vm, false);
        }
        try
        {
            await _updatePolicy.HandleAsync(MapPolicyEdit(vm), ct).ConfigureAwait(false);
            SetSuccessMessage("ProductTrackingPolicyUpdatedMessage");
            return RedirectOrHtmx(nameof(ProductTrackingPolicies), new { businessId = vm.BusinessId });
        }
        catch (DbUpdateConcurrencyException)
        {
            SetErrorMessage("ProductTrackingPolicyConcurrencyMessage");
            return RedirectToAction(nameof(EditProductTrackingPolicy), new { id = vm.Id });
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ValidationException)
        {
            AddModelErrorMessage("ProductTrackingPolicySaveFailedMessage");
            await PopulatePolicyOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderEditor("ProductTrackingPolicyEditor", vm, false);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveProductTrackingPolicy(ProductTrackingPolicyArchiveDto dto, CancellationToken ct = default)
    {
        var result = await _archivePolicy.HandleAsync(dto, ct).ConfigureAwait(false);
        if (!result.Succeeded) SetErrorMessage(result.Error ?? "ProductTrackingPolicyArchiveFailedMessage"); else SetSuccessMessage("ProductTrackingPolicyArchivedMessage");
        return RedirectToAction(nameof(ProductTrackingPolicies));
    }

    [HttpGet]
    public async Task<IActionResult> Lots(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, InventoryLotQueueFilter filter = InventoryLotQueueFilter.All, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
        var vm = new InventoryLotsListVm { BusinessId = businessId, Query = q ?? string.Empty, Filter = filter, FilterItems = BuildEnumOptions(filter), BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false), Page = page, PageSize = pageSize };
        if (businessId.HasValue)
        {
            var result = await _getLots.HandleAsync(businessId.Value, page, pageSize, q, filter, ct).ConfigureAwait(false);
            vm.Items = result.Items.Select(MapLot).ToList();
            vm.Total = result.Total;
            var summary = await _getLots.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);
            vm.Summary = new InventoryLotOpsSummaryVm { TotalCount = summary.TotalCount, ActiveCount = summary.ActiveCount, QuarantinedCount = summary.QuarantinedCount, ExpiredCount = summary.ExpiredCount, RecalledCount = summary.RecalledCount };
        }
        return Render("Lots", vm);
    }

    [HttpGet]
    public async Task<IActionResult> CreateLot(Guid? businessId = null, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
        var vm = new InventoryLotEditVm { BusinessId = businessId ?? Guid.Empty };
        await PopulateLotOptionsAsync(vm, ct).ConfigureAwait(false);
        return RenderEditor("LotEditor", vm, true);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLot(InventoryLotEditVm vm, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLotOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderEditor("LotEditor", vm, true);
        }
        try
        {
            await _createLot.HandleAsync(MapLotCreate(vm), ct).ConfigureAwait(false);
            SetSuccessMessage("InventoryLotCreatedMessage");
            return RedirectOrHtmx(nameof(Lots), new { businessId = vm.BusinessId });
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ValidationException)
        {
            AddModelErrorMessage("InventoryLotSaveFailedMessage");
            await PopulateLotOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderEditor("LotEditor", vm, true);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditLot(Guid id, CancellationToken ct = default)
    {
        var dto = await _getLot.HandleAsync(id, ct).ConfigureAwait(false);
        if (dto is null)
        {
            SetErrorMessage("InventoryLotNotFound");
            return RedirectToAction(nameof(Lots));
        }
        var vm = MapLotEditor(dto);
        await PopulateLotOptionsAsync(vm, ct).ConfigureAwait(false);
        return RenderEditor("LotEditor", vm, false);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLot(InventoryLotEditVm vm, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLotOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderEditor("LotEditor", vm, false);
        }
        try
        {
            await _updateLot.HandleAsync(MapLotEdit(vm), ct).ConfigureAwait(false);
            SetSuccessMessage("InventoryLotUpdatedMessage");
            return RedirectOrHtmx(nameof(Lots), new { businessId = vm.BusinessId });
        }
        catch (DbUpdateConcurrencyException)
        {
            SetErrorMessage("InventoryLotConcurrencyMessage");
            return RedirectToAction(nameof(EditLot), new { id = vm.Id });
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ValidationException)
        {
            AddModelErrorMessage("InventoryLotSaveFailedMessage");
            await PopulateLotOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderEditor("LotEditor", vm, false);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveLot(InventoryLotArchiveDto dto, CancellationToken ct = default)
    {
        var result = await _archiveLot.HandleAsync(dto, ct).ConfigureAwait(false);
        if (!result.Succeeded) SetErrorMessage(result.Error ?? "InventoryLotArchiveFailedMessage"); else SetSuccessMessage("InventoryLotArchivedMessage");
        return RedirectToAction(nameof(Lots));
    }

    [HttpGet]
    public async Task<IActionResult> SerialUnits(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, InventorySerialUnitQueueFilter filter = InventorySerialUnitQueueFilter.All, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
        var vm = new InventorySerialUnitsListVm { BusinessId = businessId, Query = q ?? string.Empty, Filter = filter, FilterItems = BuildEnumOptions(filter), BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false), Page = page, PageSize = pageSize };
        if (businessId.HasValue)
        {
            var result = await _getSerials.HandleAsync(businessId.Value, page, pageSize, q, filter, ct).ConfigureAwait(false);
            vm.Items = result.Items.Select(MapSerial).ToList();
            vm.Total = result.Total;
            var summary = await _getSerials.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);
            vm.Summary = new InventorySerialUnitOpsSummaryVm { TotalCount = summary.TotalCount, AvailableCount = summary.AvailableCount, ReservedCount = summary.ReservedCount, QuarantinedCount = summary.QuarantinedCount, ScrappedCount = summary.ScrappedCount };
        }
        return Render("SerialUnits", vm);
    }

    [HttpGet]
    public async Task<IActionResult> CreateSerialUnit(Guid? businessId = null, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
        var vm = new InventorySerialUnitEditVm { BusinessId = businessId ?? Guid.Empty };
        await PopulateSerialOptionsAsync(vm, ct).ConfigureAwait(false);
        return RenderEditor("SerialUnitEditor", vm, true);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSerialUnit(InventorySerialUnitEditVm vm, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSerialOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderEditor("SerialUnitEditor", vm, true);
        }
        try
        {
            await _createSerial.HandleAsync(MapSerialCreate(vm), ct).ConfigureAwait(false);
            SetSuccessMessage("InventorySerialUnitCreatedMessage");
            return RedirectOrHtmx(nameof(SerialUnits), new { businessId = vm.BusinessId });
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ValidationException)
        {
            AddModelErrorMessage("InventorySerialUnitSaveFailedMessage");
            await PopulateSerialOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderEditor("SerialUnitEditor", vm, true);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditSerialUnit(Guid id, CancellationToken ct = default)
    {
        var dto = await _getSerial.HandleAsync(id, ct).ConfigureAwait(false);
        if (dto is null)
        {
            SetErrorMessage("InventorySerialUnitNotFound");
            return RedirectToAction(nameof(SerialUnits));
        }
        var vm = MapSerialEditor(dto);
        await PopulateSerialOptionsAsync(vm, ct).ConfigureAwait(false);
        return RenderEditor("SerialUnitEditor", vm, false);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSerialUnit(InventorySerialUnitEditVm vm, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSerialOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderEditor("SerialUnitEditor", vm, false);
        }
        try
        {
            await _updateSerial.HandleAsync(MapSerialEdit(vm), ct).ConfigureAwait(false);
            SetSuccessMessage("InventorySerialUnitUpdatedMessage");
            return RedirectOrHtmx(nameof(SerialUnits), new { businessId = vm.BusinessId });
        }
        catch (DbUpdateConcurrencyException)
        {
            SetErrorMessage("InventorySerialUnitConcurrencyMessage");
            return RedirectToAction(nameof(EditSerialUnit), new { id = vm.Id });
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ValidationException)
        {
            AddModelErrorMessage("InventorySerialUnitSaveFailedMessage");
            await PopulateSerialOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderEditor("SerialUnitEditor", vm, false);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveSerialUnit(InventorySerialUnitArchiveDto dto, CancellationToken ct = default)
    {
        var result = await _archiveSerial.HandleAsync(dto, ct).ConfigureAwait(false);
        if (!result.Succeeded) SetErrorMessage(result.Error ?? "InventorySerialUnitArchiveFailedMessage"); else SetSuccessMessage("InventorySerialUnitArchivedMessage");
        return RedirectToAction(nameof(SerialUnits));
    }

    [HttpGet]
    public async Task<IActionResult> HandlingUnits(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, HandlingUnitQueueFilter filter = HandlingUnitQueueFilter.All, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
        var vm = new HandlingUnitsListVm { BusinessId = businessId, Query = q ?? string.Empty, Filter = filter, FilterItems = BuildEnumOptions(filter), BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false), Page = page, PageSize = pageSize };
        if (businessId.HasValue)
        {
            var result = await _getHandlingUnits.HandleAsync(businessId.Value, page, pageSize, q, filter, ct).ConfigureAwait(false);
            vm.Items = result.Items.Select(MapHandlingUnit).ToList();
            vm.Total = result.Total;
            var summary = await _getHandlingUnits.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);
            vm.Summary = new HandlingUnitOpsSummaryVm { TotalCount = summary.TotalCount, OpenCount = summary.OpenCount, ClosedCount = summary.ClosedCount, InTransitCount = summary.InTransitCount, ReceivedCount = summary.ReceivedCount };
        }
        return Render("HandlingUnits", vm);
    }

    [HttpGet]
    public async Task<IActionResult> CreateHandlingUnit(Guid? businessId = null, CancellationToken ct = default)
    {
        businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
        var vm = new HandlingUnitEditVm { BusinessId = businessId ?? Guid.Empty };
        EnsureHandlingUnitRows(vm);
        await PopulateHandlingUnitOptionsAsync(vm, ct).ConfigureAwait(false);
        return RenderEditor("HandlingUnitEditor", vm, true);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateHandlingUnit(HandlingUnitEditVm vm, CancellationToken ct = default)
    {
        EnsureHandlingUnitRows(vm);
        if (!ModelState.IsValid)
        {
            await PopulateHandlingUnitOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderEditor("HandlingUnitEditor", vm, true);
        }
        try
        {
            await _createHandlingUnit.HandleAsync(MapHandlingUnitCreate(vm), ct).ConfigureAwait(false);
            SetSuccessMessage("HandlingUnitCreatedMessage");
            return RedirectOrHtmx(nameof(HandlingUnits), new { businessId = vm.BusinessId });
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ValidationException)
        {
            AddModelErrorMessage("HandlingUnitSaveFailedMessage");
            await PopulateHandlingUnitOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderEditor("HandlingUnitEditor", vm, true);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditHandlingUnit(Guid id, CancellationToken ct = default)
    {
        var dto = await _getHandlingUnit.HandleAsync(id, ct).ConfigureAwait(false);
        if (dto is null)
        {
            SetErrorMessage("HandlingUnitNotFound");
            return RedirectToAction(nameof(HandlingUnits));
        }
        var vm = MapHandlingUnitEditor(dto);
        EnsureHandlingUnitRows(vm);
        await PopulateHandlingUnitOptionsAsync(vm, ct).ConfigureAwait(false);
        return RenderEditor("HandlingUnitEditor", vm, false);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditHandlingUnit(HandlingUnitEditVm vm, CancellationToken ct = default)
    {
        EnsureHandlingUnitRows(vm);
        if (!ModelState.IsValid)
        {
            await PopulateHandlingUnitOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderEditor("HandlingUnitEditor", vm, false);
        }
        try
        {
            await _updateHandlingUnit.HandleAsync(MapHandlingUnitEdit(vm), ct).ConfigureAwait(false);
            SetSuccessMessage("HandlingUnitUpdatedMessage");
            return RedirectOrHtmx(nameof(HandlingUnits), new { businessId = vm.BusinessId });
        }
        catch (DbUpdateConcurrencyException)
        {
            SetErrorMessage("HandlingUnitConcurrencyMessage");
            return RedirectToAction(nameof(EditHandlingUnit), new { id = vm.Id });
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ValidationException)
        {
            AddModelErrorMessage("HandlingUnitSaveFailedMessage");
            await PopulateHandlingUnitOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderEditor("HandlingUnitEditor", vm, false);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveHandlingUnit(HandlingUnitArchiveDto dto, CancellationToken ct = default)
    {
        var result = await _archiveHandlingUnit.HandleAsync(dto, ct).ConfigureAwait(false);
        if (!result.Succeeded) SetErrorMessage(result.Error ?? "HandlingUnitArchiveFailedMessage"); else SetSuccessMessage("HandlingUnitArchivedMessage");
        return RedirectToAction(nameof(HandlingUnits));
    }

    private async Task PopulatePolicyOptionsAsync(ProductTrackingPolicyEditVm vm, CancellationToken ct)
    {
        vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
        vm.VariantOptions = await _referenceData.GetVariantOptionsAsync(vm.ProductVariantId, ct).ConfigureAwait(false);
        vm.TrackingModeOptions = BuildEnumOptions(vm.TrackingMode);
        vm.StatusOptions = BuildEnumOptions(vm.Status);
    }

    private async Task PopulateLotOptionsAsync(InventoryLotEditVm vm, CancellationToken ct)
    {
        vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
        vm.VariantOptions = await _referenceData.GetVariantOptionsAsync(vm.ProductVariantId, ct).ConfigureAwait(false);
        vm.StatusOptions = BuildEnumOptions(vm.Status);
    }

    private async Task PopulateSerialOptionsAsync(InventorySerialUnitEditVm vm, CancellationToken ct)
    {
        vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
        vm.VariantOptions = await _referenceData.GetVariantOptionsAsync(vm.ProductVariantId, ct).ConfigureAwait(false);
        vm.LotOptions = await BuildLotOptionsAsync(vm.BusinessId, vm.InventoryLotId, ct).ConfigureAwait(false);
        vm.StatusOptions = BuildEnumOptions(vm.Status);
    }

    private async Task PopulateHandlingUnitOptionsAsync(HandlingUnitEditVm vm, CancellationToken ct)
    {
        vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
        vm.WarehouseOptions = await _referenceData.GetWarehouseOptionsAsync(vm.WarehouseId, vm.BusinessId, ct).ConfigureAwait(false);
        vm.LocationOptions = await BuildLocationOptionsAsync(vm.BusinessId, vm.WarehouseId, vm.LocationId, ct).ConfigureAwait(false);
        vm.ParentHandlingUnitOptions = await BuildHandlingUnitOptionsAsync(vm.BusinessId, vm.ParentHandlingUnitId, vm.Id, ct).ConfigureAwait(false);
        vm.TypeOptions = BuildEnumOptions(vm.HandlingUnitType);
        vm.StatusOptions = BuildEnumOptions(vm.Status);
        vm.VariantOptions = await _referenceData.GetVariantOptionsAsync(null, ct).ConfigureAwait(false);
        vm.LotOptions = await BuildLotOptionsAsync(vm.BusinessId, null, ct).ConfigureAwait(false);
        vm.SerialOptions = await BuildSerialOptionsAsync(vm.BusinessId, null, ct).ConfigureAwait(false);
    }

    private async Task<List<SelectListItem>> BuildLotOptionsAsync(Guid businessId, Guid? selectedId, CancellationToken ct)
    {
        var options = new List<SelectListItem> { new("Select lot", string.Empty, !selectedId.HasValue) };
        if (businessId == Guid.Empty) return options;
        var result = await _getLots.HandleAsync(businessId, 1, 200, null, InventoryLotQueueFilter.All, ct).ConfigureAwait(false);
        options.AddRange(result.Items.Select(x => new SelectListItem($"{x.LotCode} ({x.VariantSku})", x.Id.ToString(), selectedId == x.Id)));
        return options;
    }

    private async Task<List<SelectListItem>> BuildSerialOptionsAsync(Guid businessId, Guid? selectedId, CancellationToken ct)
    {
        var options = new List<SelectListItem> { new("Select serial", string.Empty, !selectedId.HasValue) };
        if (businessId == Guid.Empty) return options;
        var result = await _getSerials.HandleAsync(businessId, 1, 200, null, InventorySerialUnitQueueFilter.All, ct).ConfigureAwait(false);
        options.AddRange(result.Items.Select(x => new SelectListItem($"{x.SerialNumber} ({x.VariantSku})", x.Id.ToString(), selectedId == x.Id)));
        return options;
    }

    private async Task<List<SelectListItem>> BuildLocationOptionsAsync(Guid businessId, Guid? warehouseId, Guid? selectedId, CancellationToken ct)
    {
        var options = new List<SelectListItem> { new("Select location", string.Empty, !selectedId.HasValue) };
        if (businessId == Guid.Empty) return options;
        var result = await _getLocations.HandleAsync(businessId, warehouseId, 1, 200, null, WarehouseLocationQueueFilter.Active, ct).ConfigureAwait(false);
        options.AddRange(result.Items.Select(x => new SelectListItem($"{x.Code} - {x.DisplayName}", x.Id.ToString(), selectedId == x.Id)));
        return options;
    }

    private async Task<List<SelectListItem>> BuildHandlingUnitOptionsAsync(Guid businessId, Guid? selectedId, Guid currentId, CancellationToken ct)
    {
        var options = new List<SelectListItem> { new("No parent", string.Empty, !selectedId.HasValue) };
        if (businessId == Guid.Empty) return options;
        var result = await _getHandlingUnits.HandleAsync(businessId, 1, 200, null, HandlingUnitQueueFilter.All, ct).ConfigureAwait(false);
        options.AddRange(result.Items.Where(x => x.Id != currentId).Select(x => new SelectListItem($"{x.Code} - {x.DisplayName}", x.Id.ToString(), selectedId == x.Id)));
        return options;
    }

    private static List<SelectListItem> BuildEnumOptions<TEnum>(TEnum selected) where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().Select(x => new SelectListItem(x.ToString(), x.ToString(), EqualityComparer<TEnum>.Default.Equals(x, selected))).ToList();

    private IActionResult Render(string viewName, object model) => IsHtmxRequest() ? PartialView($"~/Views/InventoryTraceability/{viewName}.cshtml", model) : View(viewName, model);

    private IActionResult RenderEditor(string viewName, object model, bool isCreate)
    {
        ViewData["IsCreate"] = isCreate;
        return IsHtmxRequest() ? PartialView($"~/Views/InventoryTraceability/{viewName}.cshtml", model) : View(viewName, model);
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

    private bool IsHtmxRequest() => string.Equals(Request.Headers["HX-Request"], "true", StringComparison.OrdinalIgnoreCase);

    private static void EnsureHandlingUnitRows(HandlingUnitEditVm vm)
    {
        vm.Contents ??= new List<HandlingUnitContentVm>();
        if (vm.Contents.Count == 0)
        {
            vm.Contents.Add(new HandlingUnitContentVm { SortOrder = 1, Quantity = 1 });
        }
    }

    private static ProductTrackingPolicyListItemVm MapPolicy(ProductTrackingPolicyListItemDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, ProductVariantId = dto.ProductVariantId, VariantSku = dto.VariantSku, TrackingMode = dto.TrackingMode, Status = dto.Status, RequiresSupplierLot = dto.RequiresSupplierLot, RequiresExpiryDate = dto.RequiresExpiryDate, RequiresHandlingUnit = dto.RequiresHandlingUnit };
    private static ProductTrackingPolicyEditVm MapPolicyEditor(ProductTrackingPolicyDetailDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, ProductVariantId = dto.ProductVariantId, VariantSku = dto.VariantSku, TrackingMode = dto.TrackingMode, Status = dto.Status, RequiresSupplierLot = dto.RequiresSupplierLot, RequiresExpiryDate = dto.RequiresExpiryDate, RequiresHandlingUnit = dto.RequiresHandlingUnit, Notes = dto.Notes, MetadataJson = dto.MetadataJson };
    private static ProductTrackingPolicyCreateDto MapPolicyCreate(ProductTrackingPolicyEditVm vm) => new() { BusinessId = vm.BusinessId, ProductVariantId = vm.ProductVariantId, TrackingMode = vm.TrackingMode, Status = vm.Status, RequiresSupplierLot = vm.RequiresSupplierLot, RequiresExpiryDate = vm.RequiresExpiryDate, RequiresHandlingUnit = vm.RequiresHandlingUnit, Notes = vm.Notes, MetadataJson = vm.MetadataJson };
    private static ProductTrackingPolicyEditDto MapPolicyEdit(ProductTrackingPolicyEditVm vm) => new() { Id = vm.Id, RowVersion = vm.RowVersion, BusinessId = vm.BusinessId, ProductVariantId = vm.ProductVariantId, TrackingMode = vm.TrackingMode, Status = vm.Status, RequiresSupplierLot = vm.RequiresSupplierLot, RequiresExpiryDate = vm.RequiresExpiryDate, RequiresHandlingUnit = vm.RequiresHandlingUnit, Notes = vm.Notes, MetadataJson = vm.MetadataJson };

    private static InventoryLotListItemVm MapLot(InventoryLotListItemDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, ProductVariantId = dto.ProductVariantId, VariantSku = dto.VariantSku, LotCode = dto.LotCode, SupplierLotCode = dto.SupplierLotCode, ExpiryDateUtc = dto.ExpiryDateUtc, Status = dto.Status };
    private static InventoryLotEditVm MapLotEditor(InventoryLotDetailDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, ProductVariantId = dto.ProductVariantId, VariantSku = dto.VariantSku, LotCode = dto.LotCode, SupplierLotCode = dto.SupplierLotCode, ManufactureDateUtc = dto.ManufactureDateUtc, ExpiryDateUtc = dto.ExpiryDateUtc, Status = dto.Status, Notes = dto.Notes, MetadataJson = dto.MetadataJson };
    private static InventoryLotCreateDto MapLotCreate(InventoryLotEditVm vm) => new() { BusinessId = vm.BusinessId, ProductVariantId = vm.ProductVariantId, LotCode = vm.LotCode, SupplierLotCode = vm.SupplierLotCode, ManufactureDateUtc = vm.ManufactureDateUtc, ExpiryDateUtc = vm.ExpiryDateUtc, Status = vm.Status, Notes = vm.Notes, MetadataJson = vm.MetadataJson };
    private static InventoryLotEditDto MapLotEdit(InventoryLotEditVm vm) => new() { Id = vm.Id, RowVersion = vm.RowVersion, BusinessId = vm.BusinessId, ProductVariantId = vm.ProductVariantId, LotCode = vm.LotCode, SupplierLotCode = vm.SupplierLotCode, ManufactureDateUtc = vm.ManufactureDateUtc, ExpiryDateUtc = vm.ExpiryDateUtc, Status = vm.Status, Notes = vm.Notes, MetadataJson = vm.MetadataJson };

    private static InventorySerialUnitListItemVm MapSerial(InventorySerialUnitListItemDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, ProductVariantId = dto.ProductVariantId, VariantSku = dto.VariantSku, InventoryLotId = dto.InventoryLotId, LotCode = dto.LotCode, SerialNumber = dto.SerialNumber, ExpiryDateUtc = dto.ExpiryDateUtc, Status = dto.Status };
    private static InventorySerialUnitEditVm MapSerialEditor(InventorySerialUnitDetailDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, ProductVariantId = dto.ProductVariantId, VariantSku = dto.VariantSku, InventoryLotId = dto.InventoryLotId, LotCode = dto.LotCode, SerialNumber = dto.SerialNumber, ManufactureDateUtc = dto.ManufactureDateUtc, ExpiryDateUtc = dto.ExpiryDateUtc, Status = dto.Status, Notes = dto.Notes, MetadataJson = dto.MetadataJson };
    private static InventorySerialUnitCreateDto MapSerialCreate(InventorySerialUnitEditVm vm) => new() { BusinessId = vm.BusinessId, ProductVariantId = vm.ProductVariantId, InventoryLotId = vm.InventoryLotId, SerialNumber = vm.SerialNumber, ManufactureDateUtc = vm.ManufactureDateUtc, ExpiryDateUtc = vm.ExpiryDateUtc, Status = vm.Status, Notes = vm.Notes, MetadataJson = vm.MetadataJson };
    private static InventorySerialUnitEditDto MapSerialEdit(InventorySerialUnitEditVm vm) => new() { Id = vm.Id, RowVersion = vm.RowVersion, BusinessId = vm.BusinessId, ProductVariantId = vm.ProductVariantId, InventoryLotId = vm.InventoryLotId, SerialNumber = vm.SerialNumber, ManufactureDateUtc = vm.ManufactureDateUtc, ExpiryDateUtc = vm.ExpiryDateUtc, Status = vm.Status, Notes = vm.Notes, MetadataJson = vm.MetadataJson };

    private static HandlingUnitListItemVm MapHandlingUnit(HandlingUnitListItemDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, WarehouseId = dto.WarehouseId, WarehouseName = dto.WarehouseName, LocationId = dto.LocationId, LocationCode = dto.LocationCode, ParentHandlingUnitId = dto.ParentHandlingUnitId, ParentCode = dto.ParentCode, Code = dto.Code, DisplayName = dto.DisplayName, Barcode = dto.Barcode, HandlingUnitType = dto.HandlingUnitType, Status = dto.Status, ContentCount = dto.ContentCount, TotalQuantity = dto.TotalQuantity };
    private static HandlingUnitEditVm MapHandlingUnitEditor(HandlingUnitDetailDto dto) => new() { Id = dto.Id, RowVersion = dto.RowVersion, BusinessId = dto.BusinessId, WarehouseId = dto.WarehouseId, WarehouseName = dto.WarehouseName, LocationId = dto.LocationId, LocationCode = dto.LocationCode, ParentHandlingUnitId = dto.ParentHandlingUnitId, ParentCode = dto.ParentCode, Code = dto.Code, DisplayName = dto.DisplayName, Barcode = dto.Barcode, HandlingUnitType = dto.HandlingUnitType, Status = dto.Status, Notes = dto.Notes, MetadataJson = dto.MetadataJson, Contents = dto.Contents.Select(MapHandlingUnitContentVm).ToList() };
    private static HandlingUnitCreateDto MapHandlingUnitCreate(HandlingUnitEditVm vm) => new() { BusinessId = vm.BusinessId, WarehouseId = vm.WarehouseId, LocationId = vm.LocationId, ParentHandlingUnitId = vm.ParentHandlingUnitId, Code = vm.Code, DisplayName = vm.DisplayName, Barcode = vm.Barcode, HandlingUnitType = vm.HandlingUnitType, Status = vm.Status, Notes = vm.Notes, MetadataJson = vm.MetadataJson, Contents = vm.Contents.Select(MapHandlingUnitContentDto).ToList() };
    private static HandlingUnitEditDto MapHandlingUnitEdit(HandlingUnitEditVm vm) { var dto = MapHandlingUnitCreate(vm) as HandlingUnitEditDto ?? new HandlingUnitEditDto(); dto.Id = vm.Id; dto.RowVersion = vm.RowVersion; dto.BusinessId = vm.BusinessId; dto.WarehouseId = vm.WarehouseId; dto.LocationId = vm.LocationId; dto.ParentHandlingUnitId = vm.ParentHandlingUnitId; dto.Code = vm.Code; dto.DisplayName = vm.DisplayName; dto.Barcode = vm.Barcode; dto.HandlingUnitType = vm.HandlingUnitType; dto.Status = vm.Status; dto.Notes = vm.Notes; dto.MetadataJson = vm.MetadataJson; dto.Contents = vm.Contents.Select(MapHandlingUnitContentDto).ToList(); return dto; }
    private static HandlingUnitContentVm MapHandlingUnitContentVm(HandlingUnitContentDto dto) => new() { Id = dto.Id, ProductVariantId = dto.ProductVariantId, InventoryLotId = dto.InventoryLotId, InventorySerialUnitId = dto.InventorySerialUnitId, SkuSnapshot = dto.SkuSnapshot, Description = dto.Description, Quantity = dto.Quantity, SortOrder = dto.SortOrder, MetadataJson = dto.MetadataJson };
    private static HandlingUnitContentDto MapHandlingUnitContentDto(HandlingUnitContentVm vm) => new() { Id = vm.Id, ProductVariantId = vm.ProductVariantId, InventoryLotId = vm.InventoryLotId, InventorySerialUnitId = vm.InventorySerialUnitId, SkuSnapshot = vm.SkuSnapshot, Description = vm.Description, Quantity = vm.Quantity, SortOrder = vm.SortOrder, MetadataJson = vm.MetadataJson };
}
