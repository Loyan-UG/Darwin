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
    private readonly SettleSupplierPaymentFromBankReconciliationHandler _settleSupplierPaymentFromBankReconciliation;
    private readonly CreateSupplierPaymentBankCorrectionHandler _createSupplierPaymentBankCorrection;
    private readonly PostSupplierPaymentBankCorrectionHandler _postSupplierPaymentBankCorrection;
    private readonly CancelSupplierPaymentBankCorrectionHandler _cancelSupplierPaymentBankCorrection;
    private readonly GetSupplierAdvancesPageHandler _getSupplierAdvances;
    private readonly GetSupplierAdvanceDetailHandler _getSupplierAdvance;
    private readonly GetSupplierAdvanceDraftHandler _getSupplierAdvanceDraft;
    private readonly CreateSupplierAdvanceHandler _createSupplierAdvance;
    private readonly UpdateSupplierAdvanceHandler _updateSupplierAdvance;
    private readonly PostSupplierAdvanceHandler _postSupplierAdvance;
    private readonly CancelSupplierAdvanceHandler _cancelSupplierAdvance;
    private readonly ReverseSupplierAdvanceHandler _reverseSupplierAdvance;
    private readonly ApplySupplierAdvanceHandler _applySupplierAdvance;
    private readonly ReverseSupplierAdvanceApplicationHandler _reverseSupplierAdvanceApplication;
    private readonly GetBankAccountsPageHandler _getBankAccounts;
    private readonly GetBankAccountForEditHandler _getBankAccount;
    private readonly CreateBankAccountHandler _createBankAccount;
    private readonly UpdateBankAccountHandler _updateBankAccount;
    private readonly ArchiveBankAccountHandler _archiveBankAccount;
    private readonly GetBankStatementsPageHandler _getBankStatements;
    private readonly GetBankStatementImportDetailHandler _getBankStatement;
    private readonly CreateBankStatementImportHandler _createBankStatementImport;
    private readonly CancelBankStatementImportHandler _cancelBankStatementImport;
    private readonly GetBankReconciliationPageHandler _getBankReconciliation;
    private readonly GetBankReconciliationDetailHandler _getBankReconciliationDetail;
    private readonly GetBankReconciliationDraftHandler _getBankReconciliationDraft;
    private readonly CreateBankReconciliationMatchHandler _createBankReconciliation;
    private readonly UpdateBankReconciliationMatchHandler _updateBankReconciliation;
    private readonly MarkBankReconciliationMatchedHandler _markBankReconciliationMatched;
    private readonly CancelBankReconciliationMatchHandler _cancelBankReconciliation;

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
        ReverseSupplierPaymentHandler reverseSupplierPayment,
        SettleSupplierPaymentFromBankReconciliationHandler settleSupplierPaymentFromBankReconciliation,
        CreateSupplierPaymentBankCorrectionHandler createSupplierPaymentBankCorrection,
        PostSupplierPaymentBankCorrectionHandler postSupplierPaymentBankCorrection,
        CancelSupplierPaymentBankCorrectionHandler cancelSupplierPaymentBankCorrection,
        GetSupplierAdvancesPageHandler getSupplierAdvances,
        GetSupplierAdvanceDetailHandler getSupplierAdvance,
        GetSupplierAdvanceDraftHandler getSupplierAdvanceDraft,
        CreateSupplierAdvanceHandler createSupplierAdvance,
        UpdateSupplierAdvanceHandler updateSupplierAdvance,
        PostSupplierAdvanceHandler postSupplierAdvance,
        CancelSupplierAdvanceHandler cancelSupplierAdvance,
        ReverseSupplierAdvanceHandler reverseSupplierAdvance,
        ApplySupplierAdvanceHandler applySupplierAdvance,
        ReverseSupplierAdvanceApplicationHandler reverseSupplierAdvanceApplication,
        GetBankAccountsPageHandler getBankAccounts,
        GetBankAccountForEditHandler getBankAccount,
        CreateBankAccountHandler createBankAccount,
        UpdateBankAccountHandler updateBankAccount,
        ArchiveBankAccountHandler archiveBankAccount,
        GetBankStatementsPageHandler getBankStatements,
        GetBankStatementImportDetailHandler getBankStatement,
        CreateBankStatementImportHandler createBankStatementImport,
        CancelBankStatementImportHandler cancelBankStatementImport,
        GetBankReconciliationPageHandler getBankReconciliation,
        GetBankReconciliationDetailHandler getBankReconciliationDetail,
        GetBankReconciliationDraftHandler getBankReconciliationDraft,
        CreateBankReconciliationMatchHandler createBankReconciliation,
        UpdateBankReconciliationMatchHandler updateBankReconciliation,
        MarkBankReconciliationMatchedHandler markBankReconciliationMatched,
        CancelBankReconciliationMatchHandler cancelBankReconciliation)
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
        _settleSupplierPaymentFromBankReconciliation = settleSupplierPaymentFromBankReconciliation ?? throw new ArgumentNullException(nameof(settleSupplierPaymentFromBankReconciliation));
        _createSupplierPaymentBankCorrection = createSupplierPaymentBankCorrection ?? throw new ArgumentNullException(nameof(createSupplierPaymentBankCorrection));
        _postSupplierPaymentBankCorrection = postSupplierPaymentBankCorrection ?? throw new ArgumentNullException(nameof(postSupplierPaymentBankCorrection));
        _cancelSupplierPaymentBankCorrection = cancelSupplierPaymentBankCorrection ?? throw new ArgumentNullException(nameof(cancelSupplierPaymentBankCorrection));
        _getSupplierAdvances = getSupplierAdvances ?? throw new ArgumentNullException(nameof(getSupplierAdvances));
        _getSupplierAdvance = getSupplierAdvance ?? throw new ArgumentNullException(nameof(getSupplierAdvance));
        _getSupplierAdvanceDraft = getSupplierAdvanceDraft ?? throw new ArgumentNullException(nameof(getSupplierAdvanceDraft));
        _createSupplierAdvance = createSupplierAdvance ?? throw new ArgumentNullException(nameof(createSupplierAdvance));
        _updateSupplierAdvance = updateSupplierAdvance ?? throw new ArgumentNullException(nameof(updateSupplierAdvance));
        _postSupplierAdvance = postSupplierAdvance ?? throw new ArgumentNullException(nameof(postSupplierAdvance));
        _cancelSupplierAdvance = cancelSupplierAdvance ?? throw new ArgumentNullException(nameof(cancelSupplierAdvance));
        _reverseSupplierAdvance = reverseSupplierAdvance ?? throw new ArgumentNullException(nameof(reverseSupplierAdvance));
        _applySupplierAdvance = applySupplierAdvance ?? throw new ArgumentNullException(nameof(applySupplierAdvance));
        _reverseSupplierAdvanceApplication = reverseSupplierAdvanceApplication ?? throw new ArgumentNullException(nameof(reverseSupplierAdvanceApplication));
        _getBankAccounts = getBankAccounts ?? throw new ArgumentNullException(nameof(getBankAccounts));
        _getBankAccount = getBankAccount ?? throw new ArgumentNullException(nameof(getBankAccount));
        _createBankAccount = createBankAccount ?? throw new ArgumentNullException(nameof(createBankAccount));
        _updateBankAccount = updateBankAccount ?? throw new ArgumentNullException(nameof(updateBankAccount));
        _archiveBankAccount = archiveBankAccount ?? throw new ArgumentNullException(nameof(archiveBankAccount));
        _getBankStatements = getBankStatements ?? throw new ArgumentNullException(nameof(getBankStatements));
        _getBankStatement = getBankStatement ?? throw new ArgumentNullException(nameof(getBankStatement));
        _createBankStatementImport = createBankStatementImport ?? throw new ArgumentNullException(nameof(createBankStatementImport));
        _cancelBankStatementImport = cancelBankStatementImport ?? throw new ArgumentNullException(nameof(cancelBankStatementImport));
        _getBankReconciliation = getBankReconciliation ?? throw new ArgumentNullException(nameof(getBankReconciliation));
        _getBankReconciliationDetail = getBankReconciliationDetail ?? throw new ArgumentNullException(nameof(getBankReconciliationDetail));
        _getBankReconciliationDraft = getBankReconciliationDraft ?? throw new ArgumentNullException(nameof(getBankReconciliationDraft));
        _createBankReconciliation = createBankReconciliation ?? throw new ArgumentNullException(nameof(createBankReconciliation));
        _updateBankReconciliation = updateBankReconciliation ?? throw new ArgumentNullException(nameof(updateBankReconciliation));
        _markBankReconciliationMatched = markBankReconciliationMatched ?? throw new ArgumentNullException(nameof(markBankReconciliationMatched));
        _cancelBankReconciliation = cancelBankReconciliation ?? throw new ArgumentNullException(nameof(cancelBankReconciliation));
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
    public async Task<IActionResult> SupplierAdvances(
        Guid? businessId = null,
        string? q = null,
        SupplierAdvanceStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var vm = await _getSupplierAdvances.HandleAsync(businessId, q, status, page, pageSize, ct).ConfigureAwait(false);
        return RenderSupplierAdvances(vm);
    }

    [HttpGet]
    public async Task<IActionResult> BankAccounts(
        Guid? businessId = null,
        string? q = null,
        BankAccountStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var vm = await _getBankAccounts.HandleAsync(businessId, q, status, page, pageSize, ct).ConfigureAwait(false);
        return RenderBankAccounts(vm);
    }

    [HttpGet]
    public IActionResult CreateBankAccount(Guid? businessId = null)
    {
        var vm = new BankAccountEditDto
        {
            BusinessId = businessId ?? Guid.Empty,
            Currency = "EUR",
            MetadataJson = "{}"
        };
        return RenderBankAccountEditor(vm);
    }

    [HttpGet]
    public async Task<IActionResult> EditBankAccount(Guid id, CancellationToken ct = default)
    {
        var vm = await _getBankAccount.HandleAsync(id, ct).ConfigureAwait(false);
        if (vm is null)
        {
            SetErrorMessage("BankAccountNotFound");
            return RedirectOrHtmx(nameof(BankAccounts), new { });
        }

        return RenderBankAccountEditor(vm);
    }

    [HttpGet]
    public async Task<IActionResult> BankStatements(
        Guid? businessId = null,
        Guid? bankAccountId = null,
        string? q = null,
        BankStatementImportStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var vm = await _getBankStatements.HandleAsync(businessId, bankAccountId, q, status, page, pageSize, ct).ConfigureAwait(false);
        return RenderBankStatements(vm);
    }

    [HttpGet]
    public async Task<IActionResult> BankReconciliation(
        Guid? businessId = null,
        Guid? bankAccountId = null,
        string? q = null,
        BankReconciliationMatchStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var vm = await _getBankReconciliation.HandleAsync(businessId, bankAccountId, q, status, page, pageSize, ct).ConfigureAwait(false);
        return RenderBankReconciliation(vm);
    }

    [HttpGet]
    public async Task<IActionResult> BankReconciliationMatch(Guid id, CancellationToken ct = default)
    {
        var vm = await _getBankReconciliationDetail.HandleAsync(id, ct).ConfigureAwait(false);
        if (vm is null)
        {
            SetErrorMessage("BankReconciliationNotFound");
            return RedirectOrHtmx(nameof(BankReconciliation), new { });
        }

        return RenderBankReconciliationDetail(vm);
    }

    [HttpGet]
    public async Task<IActionResult> BankStatement(Guid id, CancellationToken ct = default)
    {
        var vm = await _getBankStatement.HandleAsync(id, ct).ConfigureAwait(false);
        if (vm is null)
        {
            SetErrorMessage("BankStatementImportNotFound");
            return RedirectOrHtmx(nameof(BankStatements), new { });
        }

        return RenderBankStatementDetail(vm);
    }

    [HttpGet]
    public IActionResult CreateBankStatement(Guid? businessId = null, Guid? bankAccountId = null)
    {
        var vm = new BankStatementImportDetailDto
        {
            BusinessId = businessId ?? Guid.Empty,
            BankAccountId = bankAccountId ?? Guid.Empty,
            PeriodStartUtc = DateTime.UtcNow.Date,
            PeriodEndUtc = DateTime.UtcNow.Date.AddDays(1),
            MetadataJson = "{}",
            Lines =
            [
                new BankStatementLineDto { TransactionDateUtc = DateTime.UtcNow.Date, Currency = "EUR", AmountMinor = 1 }
            ]
        };
        return RenderBankStatementEditor(vm);
    }

    [HttpGet]
    public async Task<IActionResult> CreateBankReconciliation(Guid? businessId = null, Guid? bankAccountId = null, CancellationToken ct = default)
    {
        var vm = await _getBankReconciliationDraft.HandleAsync(businessId, bankAccountId, ct).ConfigureAwait(false);
        return RenderBankReconciliationEditor(vm);
    }

    [HttpGet]
    public async Task<IActionResult> EditBankReconciliation(Guid id, CancellationToken ct = default)
    {
        var vm = await _getBankReconciliationDetail.HandleAsync(id, ct).ConfigureAwait(false);
        if (vm is null)
        {
            SetErrorMessage("BankReconciliationNotFound");
            return RedirectOrHtmx(nameof(BankReconciliation), new { });
        }

        return RenderBankReconciliationEditor(vm);
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
    public async Task<IActionResult> CreateSupplierAdvance(Guid? businessId = null, Guid? supplierId = null, CancellationToken ct = default)
    {
        var vm = await _getSupplierAdvanceDraft.HandleAsync(businessId, supplierId, ct).ConfigureAwait(false);
        return RenderSupplierAdvanceEditor(vm);
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
    public async Task<IActionResult> SupplierAdvance(Guid id, CancellationToken ct = default)
    {
        var vm = await _getSupplierAdvance.HandleAsync(id, ct).ConfigureAwait(false);
        if (vm is null)
        {
            SetErrorMessage("SupplierAdvanceNotFound");
            return RedirectOrHtmx(nameof(SupplierAdvances), new { });
        }

        return RenderSupplierAdvanceDetail(vm);
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

    [HttpGet]
    public async Task<IActionResult> EditSupplierAdvance(Guid id, CancellationToken ct = default)
    {
        var vm = await _getSupplierAdvance.HandleAsync(id, ct).ConfigureAwait(false);
        if (vm is null)
        {
            SetErrorMessage("SupplierAdvanceNotFound");
            return RedirectOrHtmx(nameof(SupplierAdvances), new { });
        }

        return RenderSupplierAdvanceEditor(vm);
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
    public async Task<IActionResult> CreateSupplierAdvance(SupplierAdvanceEditDto dto, CancellationToken ct = default)
    {
        try
        {
            var id = await _createSupplierAdvance.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierAdvanceCreated");
            return RedirectOrHtmx(nameof(SupplierAdvance), new { id });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierAdvanceCreateFailed");
            return RenderSupplierAdvanceEditor(dto);
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
    public async Task<IActionResult> EditSupplierAdvance(SupplierAdvanceEditDto dto, CancellationToken ct = default)
    {
        try
        {
            await _updateSupplierAdvance.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierAdvanceUpdated");
            return RedirectOrHtmx(nameof(SupplierAdvance), new { id = dto.Id });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierAdvanceUpdateFailed");
            return RenderSupplierAdvanceEditor(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBankAccount(BankAccountEditDto dto, CancellationToken ct = default)
    {
        try
        {
            var id = await _createBankAccount.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("BankAccountCreated");
            return RedirectOrHtmx(nameof(EditBankAccount), new { id });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("BankAccountCreateFailed");
            return RenderBankAccountEditor(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditBankAccount(BankAccountEditDto dto, CancellationToken ct = default)
    {
        try
        {
            await _updateBankAccount.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("BankAccountUpdated");
            return RedirectOrHtmx(nameof(BankAccounts), new { businessId = dto.BusinessId });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("BankAccountUpdateFailed");
            return RenderBankAccountEditor(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveBankAccount(BankStatementImportLifecycleActionDto dto, Guid? businessId = null, CancellationToken ct = default)
    {
        try
        {
            await _archiveBankAccount.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("BankAccountArchived");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("BankAccountArchiveFailed");
        }

        return RedirectOrHtmx(nameof(BankAccounts), new { businessId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBankStatement(BankStatementImportDetailDto dto, CancellationToken ct = default)
    {
        try
        {
            var id = await _createBankStatementImport.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("BankStatementImported");
            return RedirectOrHtmx(nameof(BankStatement), new { id });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("BankStatementImportFailed");
            return RenderBankStatementEditor(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelBankStatement(BankStatementImportLifecycleActionDto dto, Guid? businessId = null, Guid? bankAccountId = null, CancellationToken ct = default)
    {
        try
        {
            await _cancelBankStatementImport.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("BankStatementCancelled");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("BankStatementCancelFailed");
        }

        return RedirectOrHtmx(nameof(BankStatements), new { businessId, bankAccountId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBankReconciliation(BankReconciliationEditDto dto, CancellationToken ct = default)
    {
        try
        {
            var id = await _createBankReconciliation.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("BankReconciliationCreated");
            return RedirectOrHtmx(nameof(BankReconciliationMatch), new { id });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("BankReconciliationCreateFailed");
            return RenderBankReconciliationEditor(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditBankReconciliation(BankReconciliationEditDto dto, CancellationToken ct = default)
    {
        try
        {
            await _updateBankReconciliation.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("BankReconciliationUpdated");
            return RedirectOrHtmx(nameof(BankReconciliationMatch), new { id = dto.Id });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("BankReconciliationUpdateFailed");
            return RenderBankReconciliationEditor(dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkBankReconciliationMatched(BankReconciliationLifecycleActionDto dto, CancellationToken ct = default)
    {
        try
        {
            await _markBankReconciliationMatched.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("BankReconciliationMatched");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("BankReconciliationMatchFailed");
        }

        return RedirectOrHtmx(nameof(BankReconciliationMatch), new { id = dto.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelBankReconciliation(BankReconciliationLifecycleActionDto dto, CancellationToken ct = default)
    {
        try
        {
            await _cancelBankReconciliation.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("BankReconciliationCancelled");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("BankReconciliationCancelFailed");
        }

        return RedirectOrHtmx(nameof(BankReconciliationMatch), new { id = dto.Id });
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
    public async Task<IActionResult> SettleSupplierPaymentFromBankReconciliation(SupplierPaymentBankSettlementActionDto dto, CancellationToken ct = default)
    {
        try
        {
            await _settleSupplierPaymentFromBankReconciliation.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierPaymentBankSettled");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierPaymentBankSettlementFailed");
        }

        return RedirectOrHtmx(nameof(SupplierPayment), new { id = dto.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSupplierPaymentBankCorrection(SupplierPaymentBankCorrectionCreateDto dto, CancellationToken ct = default)
    {
        try
        {
            await _createSupplierPaymentBankCorrection.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierPaymentBankCorrectionCreated");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierPaymentBankCorrectionCreateFailed");
        }

        return RedirectOrHtmx(nameof(SupplierPayment), new { id = dto.SupplierPaymentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostSupplierPaymentBankCorrection(SupplierPaymentBankCorrectionActionDto dto, Guid supplierPaymentId, CancellationToken ct = default)
    {
        try
        {
            await _postSupplierPaymentBankCorrection.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierPaymentBankCorrectionPosted");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierPaymentBankCorrectionPostFailed");
        }

        return RedirectOrHtmx(nameof(SupplierPayment), new { id = supplierPaymentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelSupplierPaymentBankCorrection(SupplierPaymentBankCorrectionActionDto dto, Guid supplierPaymentId, CancellationToken ct = default)
    {
        try
        {
            await _cancelSupplierPaymentBankCorrection.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierPaymentBankCorrectionCancelled");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierPaymentBankCorrectionCancelFailed");
        }

        return RedirectOrHtmx(nameof(SupplierPayment), new { id = supplierPaymentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostSupplierAdvance(SupplierAdvanceLifecycleActionDto dto, CancellationToken ct = default)
    {
        try
        {
            await _postSupplierAdvance.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierAdvancePosted");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierAdvancePostFailed");
        }

        return RedirectOrHtmx(nameof(SupplierAdvance), new { id = dto.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelSupplierAdvance(SupplierAdvanceLifecycleActionDto dto, CancellationToken ct = default)
    {
        try
        {
            await _cancelSupplierAdvance.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierAdvanceCancelled");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierAdvanceCancelFailed");
        }

        return RedirectOrHtmx(nameof(SupplierAdvance), new { id = dto.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReverseSupplierAdvance(SupplierAdvanceLifecycleActionDto dto, CancellationToken ct = default)
    {
        try
        {
            await _reverseSupplierAdvance.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierAdvanceReversed");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierAdvanceReverseFailed");
        }

        return RedirectOrHtmx(nameof(SupplierAdvance), new { id = dto.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplySupplierAdvance(SupplierAdvanceApplyDto dto, CancellationToken ct = default)
    {
        try
        {
            await _applySupplierAdvance.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierAdvanceApplied");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierAdvanceApplyFailed");
        }

        return RedirectOrHtmx(nameof(SupplierAdvance), new { id = dto.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReverseSupplierAdvanceApplication(SupplierAdvanceApplicationReverseDto dto, CancellationToken ct = default)
    {
        try
        {
            await _reverseSupplierAdvanceApplication.HandleAsync(dto, ct).ConfigureAwait(false);
            SetSuccessMessage("SupplierAdvanceApplicationReversed");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            SetErrorMessage("SupplierAdvanceApplicationReverseFailed");
        }

        return RedirectOrHtmx(nameof(SupplierAdvance), new { id = dto.Id });
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

    private IActionResult RenderSupplierAdvances(object vm)
    {
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/SupplierAdvances.cshtml", vm);
        }

        return View("~/Views/Finance/SupplierAdvances.cshtml", vm);
    }

    private IActionResult RenderBankAccounts(object vm)
    {
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/BankAccounts.cshtml", vm);
        }

        return View("~/Views/Finance/BankAccounts.cshtml", vm);
    }

    private IActionResult RenderBankStatements(object vm)
    {
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/BankStatements.cshtml", vm);
        }

        return View("~/Views/Finance/BankStatements.cshtml", vm);
    }

    private IActionResult RenderBankReconciliation(object vm)
    {
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/BankReconciliation.cshtml", vm);
        }

        return View("~/Views/Finance/BankReconciliation.cshtml", vm);
    }

    private IActionResult RenderBankStatementDetail(object vm)
    {
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/BankStatement.cshtml", vm);
        }

        return View("~/Views/Finance/BankStatement.cshtml", vm);
    }

    private IActionResult RenderBankReconciliationDetail(object vm)
    {
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/BankReconciliationMatch.cshtml", vm);
        }

        return View("~/Views/Finance/BankReconciliationMatch.cshtml", vm);
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

    private IActionResult RenderSupplierAdvanceDetail(object vm)
    {
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/SupplierAdvance.cshtml", vm);
        }

        return View("~/Views/Finance/SupplierAdvance.cshtml", vm);
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

    private IActionResult RenderSupplierAdvanceEditor(SupplierAdvanceEditDto vm)
    {
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/SupplierAdvanceEditor.cshtml", vm);
        }

        return View("~/Views/Finance/SupplierAdvanceEditor.cshtml", vm);
    }

    private IActionResult RenderBankAccountEditor(BankAccountEditDto vm)
    {
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/BankAccountEditor.cshtml", vm);
        }

        return View("~/Views/Finance/BankAccountEditor.cshtml", vm);
    }

    private IActionResult RenderBankStatementEditor(BankStatementImportDetailDto vm)
    {
        while (vm.Lines.Count < 5)
        {
            vm.Lines.Add(new BankStatementLineDto { Currency = string.IsNullOrWhiteSpace(vm.Lines.FirstOrDefault()?.Currency) ? "EUR" : vm.Lines.First().Currency });
        }

        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/BankStatementEditor.cshtml", vm);
        }

        return View("~/Views/Finance/BankStatementEditor.cshtml", vm);
    }

    private IActionResult RenderBankReconciliationEditor(BankReconciliationEditDto vm)
    {
        EnsureBankReconciliationEditorRows(vm);

        if (IsHtmxRequest())
        {
            return PartialView("~/Views/Finance/BankReconciliationEditor.cshtml", vm);
        }

        return View("~/Views/Finance/BankReconciliationEditor.cshtml", vm);
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

    private static void EnsureBankReconciliationEditorRows(BankReconciliationEditDto vm)
    {
        while (vm.Lines.Count < 6)
        {
            vm.Lines.Add(new BankReconciliationLineDto());
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
