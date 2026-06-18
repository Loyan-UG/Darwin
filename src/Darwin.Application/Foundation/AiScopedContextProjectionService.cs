using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Foundation;

public sealed class AiScopedContextProjectionService
{
    private static readonly string[] AllowedModules = ["Sales", "Finance", "Purchasing", "Inventory", "HR", "Payroll", "Treasury"];
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public AiScopedContextProjectionService(IAppDbContext db, IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<Result<AiScopedContextProjectionDto>> BuildAsync(AiScopedContextProjectionRequest request, CancellationToken ct = default)
    {
        var purposeKey = FoundationInputNormalizer.Key(request.PurposeKey);
        if (purposeKey is null)
        {
            return Result<AiScopedContextProjectionDto>.Fail("AI scoped context requires a purpose key.");
        }

        var requestedModules = NormalizeModules(request.ModuleKeys);
        if (requestedModules.Count == 0)
        {
            return Result<AiScopedContextProjectionDto>.Fail("AI scoped context requires at least one allowed module.");
        }

        var modules = new List<AiScopedModuleContextDto>();
        foreach (var module in requestedModules)
        {
            modules.Add(module switch
            {
                "Sales" => await BuildSalesAsync(request.BusinessId, ct).ConfigureAwait(false),
                "Finance" => await BuildFinanceAsync(request.BusinessId, ct).ConfigureAwait(false),
                "Purchasing" => await BuildPurchasingAsync(request.BusinessId, ct).ConfigureAwait(false),
                "Inventory" => await BuildInventoryAsync(request.BusinessId, ct).ConfigureAwait(false),
                "HR" => await BuildHrAsync(request.BusinessId, ct).ConfigureAwait(false),
                "Payroll" => await BuildPayrollAsync(request.BusinessId, ct).ConfigureAwait(false),
                "Treasury" => await BuildTreasuryAsync(request.BusinessId, ct).ConfigureAwait(false),
                _ => throw new InvalidOperationException("Unsupported AI context module.")
            });
        }

        return Result<AiScopedContextProjectionDto>.Ok(new AiScopedContextProjectionDto
        {
            BusinessId = request.BusinessId,
            PurposeKey = purposeKey,
            GeneratedAtUtc = _clock.UtcNow,
            Modules = modules
        });
    }

    private async Task<AiScopedModuleContextDto> BuildSalesAsync(Guid? businessId, CancellationToken ct)
    {
        var orders = _db.Set<Order>().AsNoTracking().Where(x => !x.IsDeleted);
        if (businessId.HasValue) orders = orders.Where(x => x.BusinessId == businessId);
        var invoices = _db.Set<Invoice>().AsNoTracking().Where(x => !x.IsDeleted);
        if (businessId.HasValue) invoices = invoices.Where(x => x.BusinessId == businessId);
        var quotes = _db.Set<SalesQuote>().AsNoTracking().Where(x => !x.IsDeleted);
        if (businessId.HasValue) quotes = quotes.Where(x => x.BusinessId == businessId);
        var deliveryNotes = _db.Set<DeliveryNote>().AsNoTracking().Where(x => !x.IsDeleted);
        if (businessId.HasValue) deliveryNotes = deliveryNotes.Where(x => x.BusinessId == businessId);
        var returns = _db.Set<ReturnOrder>().AsNoTracking().Where(x => !x.IsDeleted);
        if (businessId.HasValue) returns = returns.Where(x => x.BusinessId == businessId);
        var creditNotes = _db.Set<CreditNote>().AsNoTracking().Where(x => !x.IsDeleted);
        if (businessId.HasValue) creditNotes = creditNotes.Where(x => x.BusinessId == businessId);

        return new AiScopedModuleContextDto
        {
            ModuleKey = "Sales",
            Metrics =
            [
                Metric("orders.open", await orders.CountAsync(x => x.Status != OrderStatus.Completed && x.Status != OrderStatus.Cancelled, ct).ConfigureAwait(false)),
                Metric("orders.gross_total_minor", await orders.SumAsync(x => (long?)x.GrandTotalGrossMinor, ct).ConfigureAwait(false) ?? 0),
                Metric("invoices.open", await invoices.CountAsync(x => x.Status == InvoiceStatus.Open, ct).ConfigureAwait(false)),
                Metric("quotes.open", await quotes.CountAsync(x => x.Status == SalesQuoteStatus.Draft || x.Status == SalesQuoteStatus.Sent || x.Status == SalesQuoteStatus.Accepted, ct).ConfigureAwait(false)),
                Metric("delivery_notes.attention", await deliveryNotes.CountAsync(x => x.Status != DeliveryNoteStatus.Delivered && x.Status != DeliveryNoteStatus.Cancelled, ct).ConfigureAwait(false)),
                Metric("returns.attention", await returns.CountAsync(x => x.Status != ReturnOrderStatus.Closed && x.Status != ReturnOrderStatus.Cancelled && x.Status != ReturnOrderStatus.Rejected, ct).ConfigureAwait(false)),
                Metric("credit_notes.open", await creditNotes.CountAsync(x => x.Status == CreditNoteStatus.Draft, ct).ConfigureAwait(false))
            ]
        };
    }

    private async Task<AiScopedModuleContextDto> BuildFinanceAsync(Guid? businessId, CancellationToken ct)
    {
        var journals = _db.Set<JournalEntry>().AsNoTracking().Where(x => !x.IsDeleted);
        if (businessId.HasValue) journals = journals.Where(x => x.BusinessId == businessId);
        var supplierInvoices = _db.Set<SupplierInvoice>().AsNoTracking().Where(x => !x.IsDeleted);
        if (businessId.HasValue) supplierInvoices = supplierInvoices.Where(x => x.BusinessId == businessId);
        var supplierPayments = _db.Set<SupplierPayment>().AsNoTracking().Where(x => !x.IsDeleted);
        if (businessId.HasValue) supplierPayments = supplierPayments.Where(x => x.BusinessId == businessId);
        var supplierAdvances = _db.Set<SupplierAdvance>().AsNoTracking().Where(x => !x.IsDeleted);
        if (businessId.HasValue) supplierAdvances = supplierAdvances.Where(x => x.BusinessId == businessId);

        return new AiScopedModuleContextDto
        {
            ModuleKey = "Finance",
            Metrics =
            [
                Metric("journal_entries.posted", await journals.CountAsync(x => x.PostingStatus == JournalEntryPostingStatus.Posted, ct).ConfigureAwait(false)),
                Metric("journal_entries.draft", await journals.CountAsync(x => x.PostingStatus == JournalEntryPostingStatus.Draft, ct).ConfigureAwait(false)),
                Metric("supplier_invoices.open_gross_minor", await supplierInvoices.Where(x => x.Status == SupplierInvoiceStatus.Posted).SumAsync(x => (long?)x.TotalGrossMinor, ct).ConfigureAwait(false) ?? 0),
                Metric("supplier_payments.pending", await supplierPayments.CountAsync(x => x.Status == SupplierPaymentStatus.Draft, ct).ConfigureAwait(false)),
                Metric("supplier_advances.open_minor", await supplierAdvances.Where(x => x.Status == SupplierAdvanceStatus.Posted || x.Status == SupplierAdvanceStatus.Applied).SumAsync(x => (long?)x.OpenAmountMinor, ct).ConfigureAwait(false) ?? 0)
            ]
        };
    }

    private async Task<AiScopedModuleContextDto> BuildPurchasingAsync(Guid? businessId, CancellationToken ct)
    {
        var suppliers = _db.Set<Supplier>().AsNoTracking().Where(x => !x.IsDeleted);
        var purchaseOrders = _db.Set<PurchaseOrder>().AsNoTracking().Where(x => !x.IsDeleted);
        var receipts = _db.Set<GoodsReceipt>().AsNoTracking().Where(x => !x.IsDeleted);
        if (businessId.HasValue)
        {
            suppliers = suppliers.Where(x => x.BusinessId == businessId);
            purchaseOrders = purchaseOrders.Where(x => x.BusinessId == businessId);
            receipts = receipts.Where(x => x.BusinessId == businessId);
        }

        return new AiScopedModuleContextDto
        {
            ModuleKey = "Purchasing",
            Metrics =
            [
                Metric("suppliers.active", await suppliers.CountAsync(x => x.Status == SupplierStatus.Active, ct).ConfigureAwait(false)),
                Metric("purchase_orders.open", await purchaseOrders.CountAsync(x => x.Status == PurchaseOrderStatus.Draft || x.Status == PurchaseOrderStatus.Issued, ct).ConfigureAwait(false)),
                Metric("goods_receipts.pending_post", await receipts.CountAsync(x => x.Status == GoodsReceiptStatus.Received || x.Status == GoodsReceiptStatus.Inspected, ct).ConfigureAwait(false))
            ]
        };
    }

    private async Task<AiScopedModuleContextDto> BuildInventoryAsync(Guid? businessId, CancellationToken ct)
    {
        var locations = _db.Set<WarehouseLocation>().AsNoTracking().Where(x => !x.IsDeleted);
        var tasks = _db.Set<WarehouseTask>().AsNoTracking().Where(x => !x.IsDeleted);
        var counts = _db.Set<StockCountSession>().AsNoTracking().Where(x => !x.IsDeleted);
        if (businessId.HasValue)
        {
            locations = locations.Where(x => x.BusinessId == businessId);
            tasks = tasks.Where(x => x.BusinessId == businessId);
            counts = counts.Where(x => x.BusinessId == businessId);
        }

        return new AiScopedModuleContextDto
        {
            ModuleKey = "Inventory",
            Metrics =
            [
                Metric("locations.active", await locations.CountAsync(x => x.Status == WarehouseLocationStatus.Active, ct).ConfigureAwait(false)),
                Metric("locations.blocked", await locations.CountAsync(x => x.Status == WarehouseLocationStatus.Blocked, ct).ConfigureAwait(false)),
                Metric("warehouse_tasks.open", await tasks.CountAsync(x => x.Status != WarehouseTaskStatus.Completed && x.Status != WarehouseTaskStatus.Cancelled, ct).ConfigureAwait(false)),
                Metric("stock_counts.open", await counts.CountAsync(x => x.Status != StockCountSessionStatus.Posted && x.Status != StockCountSessionStatus.Cancelled && x.Status != StockCountSessionStatus.Rejected, ct).ConfigureAwait(false))
            ]
        };
    }

    private async Task<AiScopedModuleContextDto> BuildHrAsync(Guid? businessId, CancellationToken ct)
    {
        var employees = _db.Set<Employee>().AsNoTracking().Where(x => !x.IsDeleted);
        if (businessId.HasValue) employees = employees.Where(x => x.BusinessId == businessId);

        return new AiScopedModuleContextDto
        {
            ModuleKey = "HR",
            Metrics =
            [
                Metric("employees.active", await employees.CountAsync(x => x.Status == EmployeeStatus.Active, ct).ConfigureAwait(false)),
                Metric("employees.inactive", await employees.CountAsync(x => x.Status != EmployeeStatus.Active, ct).ConfigureAwait(false))
            ]
        };
    }

    private async Task<AiScopedModuleContextDto> BuildPayrollAsync(Guid? businessId, CancellationToken ct)
    {
        var runs = _db.Set<PayrollRun>().AsNoTracking().Where(x => !x.IsDeleted);
        var payments = _db.Set<PayrollPayment>().AsNoTracking().Where(x => !x.IsDeleted);
        var payslips = _db.Set<PayrollPayslip>().AsNoTracking().Where(x => !x.IsDeleted);
        if (businessId.HasValue)
        {
            runs = runs.Where(x => x.BusinessId == businessId);
            payments = payments.Where(x => x.BusinessId == businessId);
            payslips = payslips.Where(x => x.BusinessId == businessId);
        }

        return new AiScopedModuleContextDto
        {
            ModuleKey = "Payroll",
            Metrics =
            [
                Metric("payroll_runs.open", await runs.CountAsync(x => x.Status != PayrollRunStatus.Posted && x.Status != PayrollRunStatus.Cancelled, ct).ConfigureAwait(false)),
                Metric("payroll_runs.posted", await runs.CountAsync(x => x.Status == PayrollRunStatus.Posted, ct).ConfigureAwait(false)),
                Metric("payroll_payments.pending", await payments.CountAsync(x => x.Status == PayrollPaymentStatus.Draft, ct).ConfigureAwait(false)),
                Metric("payslips.generated", await payslips.CountAsync(x => x.Status == PayrollPayslipStatus.Generated, ct).ConfigureAwait(false))
            ]
        };
    }

    private async Task<AiScopedModuleContextDto> BuildTreasuryAsync(Guid? businessId, CancellationToken ct)
    {
        var accounts = _db.Set<BankAccount>().AsNoTracking().Where(x => !x.IsDeleted);
        var statements = _db.Set<BankStatementImport>().AsNoTracking().Where(x => !x.IsDeleted);
        var reconciliations = _db.Set<BankReconciliationMatch>().AsNoTracking().Where(x => !x.IsDeleted);
        if (businessId.HasValue)
        {
            accounts = accounts.Where(x => x.BusinessId == businessId);
            statements = statements.Where(x => x.BusinessId == businessId);
            reconciliations = reconciliations.Where(x => x.BusinessId == businessId);
        }

        return new AiScopedModuleContextDto
        {
            ModuleKey = "Treasury",
            Metrics =
            [
                Metric("bank_accounts.active", await accounts.CountAsync(x => x.Status == BankAccountStatus.Active, ct).ConfigureAwait(false)),
                Metric("statements.imported", await statements.CountAsync(x => x.Status == BankStatementImportStatus.Imported, ct).ConfigureAwait(false)),
                Metric("reconciliations.open", await reconciliations.CountAsync(x => x.Status == BankReconciliationMatchStatus.Draft, ct).ConfigureAwait(false)),
                Metric("reconciliations.matched", await reconciliations.CountAsync(x => x.Status == BankReconciliationMatchStatus.Matched, ct).ConfigureAwait(false))
            ]
        };
    }

    private static List<string> NormalizeModules(IReadOnlyCollection<string>? moduleKeys)
    {
        var requested = moduleKeys is { Count: > 0 }
            ? moduleKeys
            : AllowedModules;

        return requested
            .Select(x => AllowedModules.FirstOrDefault(allowed => string.Equals(allowed, x?.Trim(), StringComparison.OrdinalIgnoreCase)))
            .Where(x => x is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
    }

    private static AiScopedMetricDto Metric(string key, long value)
        => new() { Key = key, Value = value };
}

public sealed record AiScopedContextProjectionRequest(
    Guid? BusinessId,
    string? PurposeKey,
    IReadOnlyCollection<string>? ModuleKeys = null);

public sealed class AiScopedContextProjectionDto
{
    public Guid? BusinessId { get; set; }
    public string PurposeKey { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; }
    public List<AiScopedModuleContextDto> Modules { get; set; } = new();
}

public sealed class AiScopedModuleContextDto
{
    public string ModuleKey { get; set; } = string.Empty;
    public List<AiScopedMetricDto> Metrics { get; set; } = new();
}

public sealed class AiScopedMetricDto
{
    public string Key { get; set; } = string.Empty;
    public long Value { get; set; }
}
