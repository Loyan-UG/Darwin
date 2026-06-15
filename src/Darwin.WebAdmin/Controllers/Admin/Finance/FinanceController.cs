using Darwin.Application.Billing.DTOs;
using Darwin.Application.Billing.Commands;
using Darwin.Application.Billing.Queries;
using Darwin.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.WebAdmin.Controllers.Admin.Finance;

/// <summary>
/// Read-only finance reporting workspace over the existing billing journal and receivables foundations.
/// </summary>
public sealed class FinanceController : AdminBaseController
{
    private readonly GetFinanceOverviewHandler _getOverview;
    private readonly GetFinanceReceivablesPageHandler _getReceivables;
    private readonly GetFinancePostingsPageHandler _getPostings;
    private readonly GetFinanceAccountMappingsPageHandler _getMappings;
    private readonly UpsertFinanceAccountMappingHandler _upsertMapping;
    private readonly GetFinanceExportsPageHandler _getExports;
    private readonly CreateFinanceExportBatchHandler _createExportBatch;
    private readonly GenerateFinanceExportPackageHandler _generateExportPackage;
    private readonly DownloadFinanceExportPackageHandler _downloadExportPackage;
    private readonly PushFinanceExportPackageHandler _pushExportPackage;
    private readonly GetSupplierInvoicesPageHandler _getSupplierInvoices;
    private readonly GetSupplierInvoiceDetailHandler _getSupplierInvoice;
    private readonly CreateSupplierInvoiceHandler _createSupplierInvoice;
    private readonly UpdateSupplierInvoiceHandler _updateSupplierInvoice;
    private readonly UpdateSupplierInvoiceLifecycleHandler _updateSupplierInvoiceLifecycle;
    private readonly PostSupplierInvoiceHandler _postSupplierInvoice;
    private readonly GetSupplierPaymentsPageHandler _getSupplierPayments;
    private readonly GetSupplierPaymentDetailHandler _getSupplierPayment;
    private readonly GetSupplierPaymentDraftHandler _getSupplierPaymentDraft;
    private readonly CreateSupplierPaymentHandler _createSupplierPayment;
    private readonly UpdateSupplierPaymentHandler _updateSupplierPayment;
    private readonly PostSupplierPaymentHandler _postSupplierPayment;
    private readonly CancelSupplierPaymentHandler _cancelSupplierPayment;
    private readonly ReverseSupplierPaymentHandler _reverseSupplierPayment;

    public FinanceController(
        GetFinanceOverviewHandler getOverview,
        GetFinanceReceivablesPageHandler getReceivables,
        GetFinancePostingsPageHandler getPostings,
        GetFinanceAccountMappingsPageHandler getMappings,
        UpsertFinanceAccountMappingHandler upsertMapping,
        GetFinanceExportsPageHandler getExports,
        CreateFinanceExportBatchHandler createExportBatch,
        GenerateFinanceExportPackageHandler generateExportPackage,
        DownloadFinanceExportPackageHandler downloadExportPackage,
        PushFinanceExportPackageHandler pushExportPackage,
        GetSupplierInvoicesPageHandler getSupplierInvoices,
        GetSupplierInvoiceDetailHandler getSupplierInvoice,
        CreateSupplierInvoiceHandler createSupplierInvoice,
        UpdateSupplierInvoiceHandler updateSupplierInvoice,
        UpdateSupplierInvoiceLifecycleHandler updateSupplierInvoiceLifecycle,
        PostSupplierInvoiceHandler postSupplierInvoice,
        GetSupplierPaymentsPageHandler getSupplierPayments,
        GetSupplierPaymentDetailHandler getSupplierPayment,
        GetSupplierPaymentDraftHandler getSupplierPaymentDraft,
        CreateSupplierPaymentHandler createSupplierPayment,
        UpdateSupplierPaymentHandler updateSupplierPayment,
        PostSupplierPaymentHandler postSupplierPayment,
        CancelSupplierPaymentHandler cancelSupplierPayment,
        ReverseSupplierPaymentHandler reverseSupplierPayment)
    {
        _getOverview = getOverview ?? throw new ArgumentNullException(nameof(getOverview));
        _getReceivables = getReceivables ?? throw new ArgumentNullException(nameof(getReceivables));
        _getPostings = getPostings ?? throw new ArgumentNullException(nameof(getPostings));
        _getMappings = getMappings ?? throw new ArgumentNullException(nameof(getMappings));
        _upsertMapping = upsertMapping ?? throw new ArgumentNullException(nameof(upsertMapping));
        _getExports = getExports ?? throw new ArgumentNullException(nameof(getExports));
        _createExportBatch = createExportBatch ?? throw new ArgumentNullException(nameof(createExportBatch));
        _generateExportPackage = generateExportPackage ?? throw new ArgumentNullException(nameof(generateExportPackage));
        _downloadExportPackage = downloadExportPackage ?? throw new ArgumentNullException(nameof(downloadExportPackage));
        _pushExportPackage = pushExportPackage ?? throw new ArgumentNullException(nameof(pushExportPackage));
        _getSupplierInvoices = getSupplierInvoices ?? throw new ArgumentNullException(nameof(getSupplierInvoices));
        _getSupplierInvoice = getSupplierInvoice ?? throw new ArgumentNullException(nameof(getSupplierInvoice));
        _createSupplierInvoice = createSupplierInvoice ?? throw new ArgumentNullException(nameof(createSupplierInvoice));
        _updateSupplierInvoice = updateSupplierInvoice ?? throw new ArgumentNullException(nameof(updateSupplierInvoice));
        _updateSupplierInvoiceLifecycle = updateSupplierInvoiceLifecycle ?? throw new ArgumentNullException(nameof(updateSupplierInvoiceLifecycle));
        _postSupplierInvoice = postSupplierInvoice ?? throw new ArgumentNullException(nameof(postSupplierInvoice));
        _getSupplierPayments = getSupplierPayments ?? throw new ArgumentNullException(nameof(getSupplierPayments));
        _getSupplierPayment = getSupplierPayment ?? throw new ArgumentNullException(nameof(getSupplierPayment));
        _getSupplierPaymentDraft = getSupplierPaymentDraft ?? throw new ArgumentNullException(nameof(getSupplierPaymentDraft));
        _createSupplierPayment = createSupplierPayment ?? throw new ArgumentNullException(nameof(createSupplierPayment));
        _updateSupplierPayment = updateSupplierPayment ?? throw new ArgumentNullException(nameof(updateSupplierPayment));
        _postSupplierPayment = postSupplierPayment ?? throw new ArgumentNullException(nameof(postSupplierPayment));
        _cancelSupplierPayment = cancelSupplierPayment ?? throw new ArgumentNullException(nameof(cancelSupplierPayment));
        _reverseSupplierPayment = reverseSupplierPayment ?? throw new ArgumentNullException(nameof(reverseSupplierPayment));
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? businessId = null, CancellationToken ct = default)
    {
        var vm = await _getOverview.HandleAsync(businessId, ct).ConfigureAwait(false);
        return RenderOverview(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Receivables(
        Guid? businessId = null,
        string? q = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var vm = await _getReceivables.HandleAsync(businessId, q, page, pageSize, ct).ConfigureAwait(false);
        return RenderReceivables(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Postings(
        Guid? businessId = null,
        string? q = null,
        JournalEntryPostingKind? postingKind = null,
        JournalEntryPostingStatus? postingStatus = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var vm = await _getPostings.HandleAsync(businessId, q, postingKind, postingStatus, page, pageSize, ct).ConfigureAwait(false);
        return RenderPostings(vm);
    }

    [HttpGet]
    public async Task<IActionResult> AccountMappings(Guid? businessId = null, CancellationToken ct = default)
    {
        var vm = await _getMappings.HandleAsync(businessId, ct).ConfigureAwait(false);
        return RenderAccountMappings(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Exports(
        Guid? businessId = null,
        Guid? externalSystemId = null,
        DateTime? periodStartUtc = null,
        DateTime? periodEndUtc = null,
        FinanceExportPostingStatusMode postingStatusMode = FinanceExportPostingStatusMode.PostedAndReversed,
        CancellationToken ct = default)
    {
        var vm = await _getExports.HandleAsync(businessId, externalSystemId, periodStartUtc, periodEndUtc, postingStatusMode, ct).ConfigureAwait(false);
        return RenderExports(vm);
    }

    [HttpGet]
    public async Task<IActionResult> SupplierInvoices(
        Guid? businessId = null,
        string? q = null,
        SupplierInvoiceStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var vm = await _getSupplierInvoices.HandleAsync(businessId, q, status, page, pageSize, ct).ConfigureAwait(false);
        return RenderSupplierInvoices(vm);
    }

    [HttpGet]
    public async Task<IActionResult> SupplierPayments(
        Guid? businessId = null,
        string? q = null,
        SupplierPaymentStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var vm = await _getSupplierPayments.HandleAsync(businessId, q, status, page, pageSize, ct).ConfigureAwait(false);
        return RenderSupplierPayments(vm);
    }

    [HttpGet]
    public async Task<IActionResult> CreateSupplierInvoice(Guid? businessId = null)
    {
        var vm = new SupplierInvoiceEditDto
        {
            BusinessId = businessId ?? Guid.Empty,
            InvoiceDateUtc = DateTime.UtcNow,
            ReceivedAtUtc = DateTime.UtcNow,
            Currency = "EUR",
            MetadataJson = "{}",
            Lines =
            [
                new SupplierInvoiceLineDto { Description = string.Empty, InvoicedQuantity = 1 }
            ]
        };
        return RenderSupplierInvoiceEditor(vm);
    }

    [HttpGet]
    public async Task<IActionResult> CreateSupplierPayment(Guid? businessId = null, Guid? supplierId = null, Guid? supplierInvoiceId = null, CancellationToken ct = default)
    {
        var vm = await _getSupplierPaymentDraft.HandleAsync(businessId, supplierId, supplierInvoiceId, ct).ConfigureAwait(false);
        return RenderSupplierPaymentEditor(vm);
    }

    [HttpGet]
    public async Task<IActionResult> SupplierInvoice(Guid id, CancellationToken ct = default)
    {
        var vm = await _getSupplierInvoice.HandleAsync(id, ct).ConfigureAwait(false);
        if (vm is null)
        {
            SetErrorMessage("SupplierInvoiceNotFound");
            return RedirectOrHtmx(nameof(SupplierInvoices), new { });
        }

        return RenderSupplierInvoiceDetail(vm);
    }

    [HttpGet]
    public async Task<IActionResult> SupplierPayment(Guid id, CancellationToken ct = default)
    {
        var vm = await _getSupplierPayment.HandleAsync(id, ct).ConfigureAwait(false);
        if (vm is null)
        {
            SetErrorMessage("SupplierPaymentNotFound");
            return RedirectOrHtmx(nameof(SupplierPayments), new { });
        }

        return RenderSupplierPaymentDetail(vm);
    }

    [HttpGet]
    public async Task<IActionResult> EditSupplierInvoice(Guid id, CancellationToken ct = default)
    {
        var vm = await _getSupplierInvoice.HandleAsync(id, ct).ConfigureAwait(false);
        if (vm is null)
        {
            SetErrorMessage("SupplierInvoiceNotFound");
            return RedirectOrHtmx(nameof(SupplierInvoices), new { });
        }

        return RenderSupplierInvoiceEditor(vm);
    }

    [HttpGet]
    public async Task<IActionResult> EditSupplierPayment(Guid id, CancellationToken ct = default)
    {
        var vm = await _getSupplierPayment.HandleAsync(id, ct).ConfigureAwait(false);
        if (vm is null)
        {
            SetErrorMessage("SupplierPaymentNotFound");
            return RedirectOrHtmx(nameof(SupplierPayments), new { });
        }

        return RenderSupplierPaymentEditor(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertAccountMapping(FinanceAccountMappingUpsertDto dto, CancellationToken ct = default)
    {
        try
        {
            await _upsertMapping.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("FinanceAccountMappingUpdated");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("FinanceAccountMappingUpdateFailed");
        }

        return RedirectOrHtmx(nameof(AccountMappings), new { businessId = dto.BusinessId == Guid.Empty ? (Guid?)null : dto.BusinessId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSupplierInvoice(SupplierInvoiceEditDto dto, CancellationToken ct = default)
    {
        try
        {
            var id = await _createSupplierInvoice.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierInvoiceCreated");
            return RedirectOrHtmx(nameof(SupplierInvoice), new { id });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierInvoiceCreateFailed");
            return RenderSupplierInvoiceEditor(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSupplierPayment(SupplierPaymentEditDto dto, CancellationToken ct = default)
    {
        try
        {
            var id = await _createSupplierPayment.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierPaymentCreated");
            return RedirectOrHtmx(nameof(SupplierPayment), new { id });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierPaymentCreateFailed");
            return RenderSupplierPaymentEditor(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSupplierInvoice(SupplierInvoiceEditDto dto, CancellationToken ct = default)
    {
        try
        {
            await _updateSupplierInvoice.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierInvoiceUpdated");
            return RedirectOrHtmx(nameof(SupplierInvoice), new { id = dto.Id });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierInvoiceUpdateFailed");
            return RenderSupplierInvoiceEditor(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSupplierPayment(SupplierPaymentEditDto dto, CancellationToken ct = default)
    {
        try
        {
            await _updateSupplierPayment.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierPaymentUpdated");
            return RedirectOrHtmx(nameof(SupplierPayment), new { id = dto.Id });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierPaymentUpdateFailed");
            return RenderSupplierPaymentEditor(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSupplierInvoiceLifecycle(SupplierInvoiceLifecycleActionDto dto, CancellationToken ct = default)
    {
        try
        {
            await _updateSupplierInvoiceLifecycle.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierInvoiceUpdated");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierInvoiceUpdateFailed");
        }

        return RedirectOrHtmx(nameof(SupplierInvoice), new { id = dto.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostSupplierInvoice(SupplierInvoiceLifecycleActionDto dto, CancellationToken ct = default)
    {
        try
        {
            await _postSupplierInvoice.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierInvoicePosted");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierInvoicePostFailed");
        }

        return RedirectOrHtmx(nameof(SupplierInvoice), new { id = dto.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostSupplierPayment(SupplierPaymentLifecycleActionDto dto, CancellationToken ct = default)
    {
        try
        {
            await _postSupplierPayment.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierPaymentPosted");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierPaymentPostFailed");
        }

        return RedirectOrHtmx(nameof(SupplierPayment), new { id = dto.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelSupplierPayment(SupplierPaymentLifecycleActionDto dto, CancellationToken ct = default)
    {
        try
        {
            await _cancelSupplierPayment.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierPaymentCancelled");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierPaymentCancelFailed");
        }

        return RedirectOrHtmx(nameof(SupplierPayment), new { id = dto.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReverseSupplierPayment(SupplierPaymentLifecycleActionDto dto, CancellationToken ct = default)
    {
        try
        {
            await _reverseSupplierPayment.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierPaymentReversed");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierPaymentReverseFailed");
        }

        return RedirectOrHtmx(nameof(SupplierPayment), new { id = dto.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateExportBatch(FinanceExportBatchCreateDto dto, CancellationToken ct = default)
    {
        try
        {
            await _createExportBatch.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("FinanceExportBatchCreated");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("FinanceExportBatchCreateFailed");
        }

        return RedirectOrHtmx(nameof(Exports), BuildExportRoute(dto));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateExportPackage(
        Guid id,
        Guid? businessId = null,
        Guid? externalSystemId = null,
        DateTime? periodStartUtc = null,
        DateTime? periodEndUtc = null,
        FinanceExportPostingStatusMode postingStatusMode = FinanceExportPostingStatusMode.PostedAndReversed,
        CancellationToken ct = default)
    {
        try
        {
            await _generateExportPackage.HandleAsync(id, ct).ConfigureAwait(false);
            SetSuccessMessage("FinanceExportPackageGenerated");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("FinanceExportPackageGenerateFailed");
        }

        return RedirectOrHtmx(nameof(Exports), new { businessId, externalSystemId, periodStartUtc, periodEndUtc, postingStatusMode });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadExportPackage(Guid id, CancellationToken ct = default)
    {
        var package = await _downloadExportPackage.HandleAsync(id, ct).ConfigureAwait(false);
        return File(package.Content, package.ContentType, package.FileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PushExportPackage(
        Guid id,
        Guid? businessId = null,
        Guid? externalSystemId = null,
        DateTime? periodStartUtc = null,
        DateTime? periodEndUtc = null,
        FinanceExportPostingStatusMode postingStatusMode = FinanceExportPostingStatusMode.PostedAndReversed,
        CancellationToken ct = default)
    {
        try
        {
            await _pushExportPackage.HandleAsync(id, ct).ConfigureAwait(false);
            SetSuccessMessage("FinanceExportPackagePushed");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("FinanceExportPackagePushFailed");
        }

        return RedirectOrHtmx(nameof(Exports), new { businessId, externalSystemId, periodStartUtc, periodEndUtc, postingStatusMode });
    }

    private IActionResult RenderOverview(object vm)
    {
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/Index.cshtml", vm);
        }

        return View("~/Views/Finance/Index.cshtml", vm);
    }

    private IActionResult RenderReceivables(object vm)
    {
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/Receivables.cshtml", vm);
        }

        return View("~/Views/Finance/Receivables.cshtml", vm);
    }

    private IActionResult RenderPostings(object vm)
    {
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/Postings.cshtml", vm);
        }

        return View("~/Views/Finance/Postings.cshtml", vm);
    }

    private IActionResult RenderAccountMappings(object vm)
    {
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/AccountMappings.cshtml", vm);
        }

        return View("~/Views/Finance/AccountMappings.cshtml", vm);
    }

    private IActionResult RenderExports(object vm)
    {
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/Exports.cshtml", vm);
        }

        return View("~/Views/Finance/Exports.cshtml", vm);
    }

    private IActionResult RenderSupplierInvoices(object vm)
    {
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/SupplierInvoices.cshtml", vm);
        }

        return View("~/Views/Finance/SupplierInvoices.cshtml", vm);
    }

    private IActionResult RenderSupplierPayments(object vm)
    {
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/SupplierPayments.cshtml", vm);
        }

        return View("~/Views/Finance/SupplierPayments.cshtml", vm);
    }

    private IActionResult RenderSupplierInvoiceDetail(object vm)
    {
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/SupplierInvoice.cshtml", vm);
        }

        return View("~/Views/Finance/SupplierInvoice.cshtml", vm);
    }

    private IActionResult RenderSupplierPaymentDetail(object vm)
    {
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/SupplierPayment.cshtml", vm);
        }

        return View("~/Views/Finance/SupplierPayment.cshtml", vm);
    }

    private IActionResult RenderSupplierInvoiceEditor(SupplierInvoiceEditDto vm)
    {
        EnsureSupplierInvoiceEditorRows(vm);

        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/SupplierInvoiceEditor.cshtml", vm);
        }

        return View("~/Views/Finance/SupplierInvoiceEditor.cshtml", vm);
    }

    private IActionResult RenderSupplierPaymentEditor(SupplierPaymentEditDto vm)
    {
        EnsureSupplierPaymentEditorRows(vm);

        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/SupplierPaymentEditor.cshtml", vm);
        }

        return View("~/Views/Finance/SupplierPaymentEditor.cshtml", vm);
    }

    private static void EnsureSupplierInvoiceEditorRows(SupplierInvoiceEditDto vm)
    {
        while (vm.Lines.Count < 5)
        {
            vm.Lines.Add(new SupplierInvoiceLineDto());
        }
    }

    private static void EnsureSupplierPaymentEditorRows(SupplierPaymentEditDto vm)
    {
        while (vm.Allocations.Count < 5)
        {
            vm.Allocations.Add(new SupplierPaymentAllocationDto());
        }
    }

    private static object BuildExportRoute(FinanceExportBatchCreateDto dto)
        => new
        {
            businessId = dto.BusinessId == Guid.Empty ? (Guid?)null : dto.BusinessId,
            externalSystemId = dto.ExternalSystemId == Guid.Empty ? (Guid?)null : dto.ExternalSystemId,
            dto.PeriodStartUtc,
            dto.PeriodEndUtc,
            dto.PostingStatusMode
        };

    private IActionResult RedirectOrHtmx(string actionName, object routeValues)
    {
        if (IsHtmxRequest())
        {
            Response.Headers["HX-Redirect"] = Url.Action(actionName, routeValues) ?? Url.Action(nameof(Index)) ?? "/Finance";
            return NoContent();
        }

        return RedirectToAction(actionName, routeValues);
    }

    private bool IsHtmxRequest()
    {
        return string.Equals(Request.Headers["HX-Request"], "true", StringComparison.OrdinalIgnoreCase);
    }
}
