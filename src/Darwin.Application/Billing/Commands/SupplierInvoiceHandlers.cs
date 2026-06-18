using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Billing.Services;
using Darwin.Application.Foundation;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Commands;

public sealed class SupplierInvoiceWorkflowPolicy
{
    public Result CanUpdate(SupplierInvoice invoice) => invoice.Status == SupplierInvoiceStatus.Draft
        ? Result.Ok()
        : Result.Fail("SupplierInvoiceLifecycleUnsupportedAction");

    public Result CanMatch(SupplierInvoice invoice) => invoice.Status is SupplierInvoiceStatus.Draft or SupplierInvoiceStatus.Matched
        ? Result.Ok()
        : Result.Fail("SupplierInvoiceLifecycleUnsupportedAction");

    public Result CanApprove(SupplierInvoice invoice) => invoice.Status == SupplierInvoiceStatus.Matched
        ? Result.Ok()
        : Result.Fail("SupplierInvoiceLifecycleUnsupportedAction");

    public Result CanVoid(SupplierInvoice invoice) => invoice.Status is SupplierInvoiceStatus.Draft or SupplierInvoiceStatus.Matched or SupplierInvoiceStatus.Approved
        ? Result.Ok()
        : Result.Fail("SupplierInvoiceLifecycleUnsupportedAction");

    public Result CanPost(SupplierInvoice invoice) => invoice.Status == SupplierInvoiceStatus.Approved
        ? Result.Ok()
        : Result.Fail("SupplierInvoiceLifecycleUnsupportedAction");
}

public sealed class CreateSupplierInvoiceHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreateSupplierInvoiceHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Guid> HandleAsync(SupplierInvoiceCreateDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        SupplierInvoiceSupport.ValidateCreate(dto);
        await SupplierInvoiceSupport.ValidateLinkedSourcesAsync(_db, dto.BusinessId, dto.SupplierId, dto.PurchaseOrderId, dto.GoodsReceiptId, ct).ConfigureAwait(false);

        var invoice = new SupplierInvoice
        {
            BusinessId = dto.BusinessId,
            SupplierId = dto.SupplierId,
            PurchaseOrderId = SupplierInvoiceSupport.NormalizeGuid(dto.PurchaseOrderId),
            GoodsReceiptId = SupplierInvoiceSupport.NormalizeGuid(dto.GoodsReceiptId),
            SupplierInvoiceNumber = SupplierInvoiceSupport.Required(dto.SupplierInvoiceNumber, 128),
            InvoiceDateUtc = SupplierInvoiceSupport.EnsureUtc(dto.InvoiceDateUtc == default ? _clock.UtcNow : dto.InvoiceDateUtc),
            ReceivedAtUtc = SupplierInvoiceSupport.EnsureUtc(dto.ReceivedAtUtc),
            DueDateUtc = SupplierInvoiceSupport.EnsureUtc(dto.DueDateUtc),
            PaymentTermDays = dto.PaymentTermDays,
            Currency = SupplierInvoiceSupport.NormalizeCurrency(dto.Currency),
            InternalNotes = SupplierInvoiceSupport.Optional(dto.InternalNotes, 4000),
            MetadataJson = SupplierInvoiceSupport.NormalizeMetadata(dto.MetadataJson),
            Lines = SupplierInvoiceSupport.MapLines(dto.Lines)
        };
        SupplierInvoiceSupport.RecalculateTotals(invoice);
        _db.Set<SupplierInvoice>().Add(invoice);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await SupplierInvoiceSupport.RecordEvidenceAsync(_events, invoice, "created", AuditTrailAction.Created, _clock.UtcNow, ct).ConfigureAwait(false);
        return invoice.Id;
    }
}

public sealed class UpdateSupplierInvoiceHandler
{
    private readonly IAppDbContext _db;
    private readonly SupplierInvoiceWorkflowPolicy _workflow;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public UpdateSupplierInvoiceHandler(IAppDbContext db, SupplierInvoiceWorkflowPolicy workflow, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task HandleAsync(SupplierInvoiceEditDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        SupplierInvoiceSupport.ValidateUpdate(dto);
        var invoice = await SupplierInvoiceSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        SupplierInvoiceSupport.ThrowIfFailed(_workflow.CanUpdate(invoice));
        await SupplierInvoiceSupport.ValidateLinkedSourcesAsync(_db, dto.BusinessId, dto.SupplierId, dto.PurchaseOrderId, dto.GoodsReceiptId, ct).ConfigureAwait(false);

        invoice.BusinessId = dto.BusinessId;
        invoice.SupplierId = dto.SupplierId;
        invoice.PurchaseOrderId = SupplierInvoiceSupport.NormalizeGuid(dto.PurchaseOrderId);
        invoice.GoodsReceiptId = SupplierInvoiceSupport.NormalizeGuid(dto.GoodsReceiptId);
        invoice.SupplierInvoiceNumber = SupplierInvoiceSupport.Required(dto.SupplierInvoiceNumber, 128);
        invoice.InvoiceDateUtc = SupplierInvoiceSupport.EnsureUtc(dto.InvoiceDateUtc);
        invoice.ReceivedAtUtc = SupplierInvoiceSupport.EnsureUtc(dto.ReceivedAtUtc);
        invoice.DueDateUtc = SupplierInvoiceSupport.EnsureUtc(dto.DueDateUtc);
        invoice.PaymentTermDays = dto.PaymentTermDays;
        invoice.Currency = SupplierInvoiceSupport.NormalizeCurrency(dto.Currency);
        invoice.InternalNotes = SupplierInvoiceSupport.Optional(dto.InternalNotes, 4000);
        invoice.MetadataJson = SupplierInvoiceSupport.NormalizeMetadata(dto.MetadataJson);
        invoice.Lines.RemoveAll(_ => true);
        invoice.Lines.AddRange(SupplierInvoiceSupport.MapLines(dto.Lines));
        SupplierInvoiceSupport.RecalculateTotals(invoice);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await SupplierInvoiceSupport.RecordEvidenceAsync(_events, invoice, "updated", AuditTrailAction.Updated, _clock.UtcNow, ct).ConfigureAwait(false);
    }
}

public sealed class UpdateSupplierInvoiceLifecycleHandler
{
    public const string MatchAction = "Match";
    public const string ApproveAction = "Approve";
    public const string VoidAction = "Void";

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly NumberSequenceService _numbers;
    private readonly SupplierInvoiceWorkflowPolicy _workflow;
    private readonly BusinessEventService? _events;

    public UpdateSupplierInvoiceLifecycleHandler(
        IAppDbContext db,
        IClock clock,
        NumberSequenceService numbers,
        SupplierInvoiceWorkflowPolicy workflow,
        BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        _events = events;
    }

    public async Task HandleAsync(SupplierInvoiceLifecycleActionDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var invoice = await SupplierInvoiceSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        var action = (dto.Action ?? string.Empty).Trim();
        if (string.Equals(action, MatchAction, StringComparison.OrdinalIgnoreCase))
        {
            SupplierInvoiceSupport.ThrowIfFailed(_workflow.CanMatch(invoice));
            await MatchAsync(invoice, ct).ConfigureAwait(false);
            invoice.Status = invoice.Lines.Any(x => !x.IsDeleted && x.MatchStatus == SupplierInvoiceLineMatchStatus.Discrepancy)
                ? SupplierInvoiceStatus.Draft
                : SupplierInvoiceStatus.Matched;
            invoice.MatchedAtUtc = invoice.Status == SupplierInvoiceStatus.Matched ? _clock.UtcNow : null;
            await SaveWithEvidenceAsync(invoice, "matched", AuditTrailAction.StatusChanged, ct).ConfigureAwait(false);
            return;
        }

        if (string.Equals(action, ApproveAction, StringComparison.OrdinalIgnoreCase))
        {
            SupplierInvoiceSupport.ThrowIfFailed(_workflow.CanApprove(invoice));
            if (invoice.Lines.Count == 0 || invoice.Lines.Any(x => !x.IsDeleted && x.MatchStatus != SupplierInvoiceLineMatchStatus.Matched))
            {
                throw new InvalidOperationException("SupplierInvoiceApprovalRequiresMatchedLines");
            }

            if (string.IsNullOrWhiteSpace(invoice.InternalInvoiceNumber))
            {
                var number = await _numbers.ReserveNextAsync(new NumberSequenceRequest(invoice.BusinessId, NumberSequenceDocumentType.SupplierInvoice, NumberSequenceService.GlobalScopeKey), ct).ConfigureAwait(false);
                if (number.Succeeded && !string.IsNullOrWhiteSpace(number.Value))
                {
                    invoice.InternalInvoiceNumber = number.Value;
                }
            }

            invoice.Status = SupplierInvoiceStatus.Approved;
            invoice.ApprovedAtUtc ??= _clock.UtcNow;
            await SaveWithEvidenceAsync(invoice, "approved", AuditTrailAction.StatusChanged, ct).ConfigureAwait(false);
            return;
        }

        if (string.Equals(action, VoidAction, StringComparison.OrdinalIgnoreCase))
        {
            SupplierInvoiceSupport.ThrowIfFailed(_workflow.CanVoid(invoice));
            invoice.Status = SupplierInvoiceStatus.Voided;
            invoice.VoidedAtUtc ??= _clock.UtcNow;
            await SaveWithEvidenceAsync(invoice, "voided", AuditTrailAction.StatusChanged, ct).ConfigureAwait(false);
            return;
        }

        throw new InvalidOperationException("SupplierInvoiceLifecycleUnsupportedAction");
    }

    private async Task MatchAsync(SupplierInvoice invoice, CancellationToken ct)
    {
        var poLines = invoice.PurchaseOrderId.HasValue
            ? await _db.Set<PurchaseOrderLine>().AsNoTracking().Where(x => !x.IsDeleted && x.PurchaseOrderId == invoice.PurchaseOrderId.Value).ToListAsync(ct).ConfigureAwait(false)
            : new List<PurchaseOrderLine>();
        var receipt = invoice.GoodsReceiptId.HasValue
            ? await _db.Set<GoodsReceipt>().AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == invoice.GoodsReceiptId.Value && !x.IsDeleted, ct).ConfigureAwait(false)
            : null;
        var postedOrApprovedLines = await _db.Set<SupplierInvoiceLine>()
            .AsNoTracking()
            .Where(line =>
                !line.IsDeleted &&
                line.SupplierInvoiceId != invoice.Id &&
                _db.Set<SupplierInvoice>().Any(other =>
                    other.Id == line.SupplierInvoiceId &&
                    other.BusinessId == invoice.BusinessId &&
                    !other.IsDeleted &&
                    (other.Status == SupplierInvoiceStatus.Approved || other.Status == SupplierInvoiceStatus.Posted)))
            .Select(line => new { line.PurchaseOrderLineId, line.GoodsReceiptLineId, line.InvoicedQuantity })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var usedByPurchaseOrderLine = postedOrApprovedLines
            .Where(x => x.PurchaseOrderLineId.HasValue)
            .GroupBy(x => x.PurchaseOrderLineId!.Value)
            .ToDictionary(x => x.Key, x => x.Sum(line => line.InvoicedQuantity));
        var usedByGoodsReceiptLine = postedOrApprovedLines
            .Where(x => x.GoodsReceiptLineId.HasValue)
            .GroupBy(x => x.GoodsReceiptLineId!.Value)
            .ToDictionary(x => x.Key, x => x.Sum(line => line.InvoicedQuantity));
        var currentPurchaseOrderLineQuantities = new Dictionary<Guid, int>();
        var currentGoodsReceiptLineQuantities = new Dictionary<Guid, int>();

        foreach (var line in invoice.Lines.Where(x => !x.IsDeleted))
        {
            var reasons = new List<string>();
            var poLine = line.PurchaseOrderLineId.HasValue ? poLines.FirstOrDefault(x => x.Id == line.PurchaseOrderLineId.Value) : null;
            if (line.PurchaseOrderLineId.HasValue)
            {
                if (poLine is null) reasons.Add("PurchaseOrderLineMissing");
                else
                {
                    var currentQuantity = AddCurrentQuantity(currentPurchaseOrderLineQuantities, poLine.Id, line.InvoicedQuantity);
                    var alreadyInvoiced = usedByPurchaseOrderLine.GetValueOrDefault(poLine.Id);
                    if (line.InvoicedQuantity <= 0 || alreadyInvoiced + currentQuantity > poLine.Quantity) reasons.Add("QuantityExceedsOrdered");
                    if (line.UnitNetMinor != poLine.UnitCostMinor) reasons.Add("UnitCostMismatch");
                    line.ProductVariantId ??= poLine.ProductVariantId;
                }
            }

            if (line.GoodsReceiptLineId.HasValue)
            {
                if (receipt is null || receipt.Status != GoodsReceiptStatus.Posted) reasons.Add("GoodsReceiptNotPosted");
                var grLine = line.GoodsReceiptLineId.HasValue && receipt is not null
                    ? receipt.Lines.FirstOrDefault(x => x.Id == line.GoodsReceiptLineId.Value && !x.IsDeleted)
                    : null;
                if (grLine is null) reasons.Add("GoodsReceiptLineMissing");
                else
                {
                    var currentQuantity = AddCurrentQuantity(currentGoodsReceiptLineQuantities, grLine.Id, line.InvoicedQuantity);
                    var alreadyInvoiced = usedByGoodsReceiptLine.GetValueOrDefault(grLine.Id);
                    if (alreadyInvoiced + currentQuantity > grLine.AcceptedQuantity) reasons.Add("QuantityExceedsAcceptedReceipt");
                    line.ProductVariantId ??= grLine.ProductVariantId;
                }
            }

            line.MatchStatus = reasons.Count == 0 ? SupplierInvoiceLineMatchStatus.Matched : SupplierInvoiceLineMatchStatus.Discrepancy;
            line.DiscrepancyReason = reasons.Count == 0 ? null : string.Join("; ", reasons.Distinct(StringComparer.OrdinalIgnoreCase));
        }
    }

    private static int AddCurrentQuantity(Dictionary<Guid, int> quantities, Guid id, int quantity)
    {
        quantities.TryGetValue(id, out var current);
        current += quantity;
        quantities[id] = current;
        return current;
    }

    private async Task SaveWithEvidenceAsync(SupplierInvoice invoice, string action, AuditTrailAction auditAction, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await SupplierInvoiceSupport.RecordEvidenceAsync(_events, invoice, action, auditAction, _clock.UtcNow, ct).ConfigureAwait(false);
    }
}

public sealed class PostSupplierInvoiceHandler
{
    public const string PostingKeyPrefix = "supplier-invoice-posted";

    private static readonly FinancePostingAccountRole[] RequiredRoles =
    [
        FinancePostingAccountRole.AccountsPayable,
        FinancePostingAccountRole.PurchaseExpense,
        FinancePostingAccountRole.InventoryClearing,
        FinancePostingAccountRole.TaxReceivable
    ];

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly SupplierInvoiceWorkflowPolicy _workflow;
    private readonly FinanceAccountMappingService _accounts;
    private readonly FinancePostingService _posting;
    private readonly BusinessEventService? _events;

    public PostSupplierInvoiceHandler(
        IAppDbContext db,
        IClock clock,
        SupplierInvoiceWorkflowPolicy workflow,
        FinanceAccountMappingService accounts,
        FinancePostingService posting,
        BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _posting = posting ?? throw new ArgumentNullException(nameof(posting));
        _events = events;
    }

    public async Task HandleAsync(SupplierInvoiceLifecycleActionDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var invoice = await SupplierInvoiceSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        SupplierInvoiceSupport.ThrowIfFailed(_workflow.CanPost(invoice));
        if (invoice.Lines.Count == 0 || invoice.Lines.Any(x => !x.IsDeleted && x.MatchStatus != SupplierInvoiceLineMatchStatus.Matched))
        {
            throw new InvalidOperationException("SupplierInvoicePostingRequiresMatchedLines");
        }

        var accountResult = await _accounts.ResolveRequiredAccountsAsync(invoice.BusinessId, RequiredRoles, ct).ConfigureAwait(false);
        if (!accountResult.Succeeded)
        {
            throw new InvalidOperationException(accountResult.Error);
        }

        var accounts = accountResult.Value!;
        var lines = BuildPostingLines(invoice, accounts);
        var postingResult = await _posting.PostAsync(new FinancePostingCommand(
            invoice.BusinessId,
            invoice.InvoiceDateUtc,
            JournalEntryPostingKind.SupplierInvoicePosted,
            $"{PostingKeyPrefix}:{invoice.Id}",
            "SupplierInvoice",
            invoice.Id,
            $"Supplier invoice {invoice.SupplierInvoiceNumber}",
            lines,
            SourceDocumentNumber: string.IsNullOrWhiteSpace(invoice.InternalInvoiceNumber) ? invoice.SupplierInvoiceNumber : invoice.InternalInvoiceNumber,
            PostingReason: "Supplier invoice payable posting",
            MetadataJson: $$"""{"supplierInvoiceId":"{{invoice.Id}}","supplierId":"{{invoice.SupplierId}}","currency":"{{invoice.Currency}}","totalGrossMinor":{{invoice.TotalGrossMinor}}}"""), ct).ConfigureAwait(false);
        if (!postingResult.Succeeded)
        {
            throw new InvalidOperationException(postingResult.Error);
        }

        invoice.PostingJournalEntryId = postingResult.Value!.JournalEntryId;
        invoice.PostedAtUtc ??= _clock.UtcNow;
        invoice.Status = SupplierInvoiceStatus.Posted;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await SupplierInvoiceSupport.RecordEvidenceAsync(_events, invoice, "posted", AuditTrailAction.StatusChanged, _clock.UtcNow, ct).ConfigureAwait(false);
    }

    private static IReadOnlyList<FinancePostingLineCommand> BuildPostingLines(
        SupplierInvoice invoice,
        IReadOnlyDictionary<FinancePostingAccountRole, Guid> accounts)
    {
        var inventoryNet = invoice.Lines
            .Where(x => !x.IsDeleted && (x.GoodsReceiptLineId.HasValue || x.ProductVariantId.HasValue))
            .Sum(x => x.TotalNetMinor);
        var expenseNet = invoice.Lines
            .Where(x => !x.IsDeleted && !x.GoodsReceiptLineId.HasValue && !x.ProductVariantId.HasValue)
            .Sum(x => x.TotalNetMinor);
        var result = new List<FinancePostingLineCommand>();
        if (inventoryNet > 0)
        {
            result.Add(new FinancePostingLineCommand(accounts[FinancePostingAccountRole.InventoryClearing], inventoryNet, 0, "Inventory clearing"));
        }

        if (expenseNet > 0)
        {
            result.Add(new FinancePostingLineCommand(accounts[FinancePostingAccountRole.PurchaseExpense], expenseNet, 0, "Purchase expense"));
        }

        if (invoice.TotalTaxMinor > 0)
        {
            result.Add(new FinancePostingLineCommand(accounts[FinancePostingAccountRole.TaxReceivable], invoice.TotalTaxMinor, 0, "Input tax"));
        }

        result.Add(new FinancePostingLineCommand(accounts[FinancePostingAccountRole.AccountsPayable], 0, invoice.TotalGrossMinor, "Accounts payable"));
        return result;
    }
}

internal static class SupplierInvoiceSupport
{
    private static readonly string[] SensitiveTokens = ["password", "secret", "token", "credential", "privatekey", "connectionstring", "apikey"];

    public static void ValidateCreate(SupplierInvoiceCreateDto dto)
    {
        if (dto.BusinessId == Guid.Empty || dto.SupplierId == Guid.Empty) throw new ArgumentException("SupplierInvoiceInvalidLink");
        _ = Required(dto.SupplierInvoiceNumber, 128);
        _ = NormalizeCurrency(dto.Currency);
        _ = NormalizeMetadata(dto.MetadataJson);
        if (dto.PaymentTermDays is < 0 or > 3650) throw new ArgumentException("SupplierInvoiceInvalidPaymentTerm");
        var meaningfulLines = dto.Lines.Where(IsMeaningfulLine).ToList();
        if (meaningfulLines.Count == 0) throw new ArgumentException("SupplierInvoiceLinesRequired");
        foreach (var line in meaningfulLines) ValidateLine(line);
    }

    public static void ValidateUpdate(SupplierInvoiceEditDto dto)
    {
        if (dto.Id == Guid.Empty || dto.RowVersion.Length == 0) throw new ArgumentException("SupplierInvoiceInvalidUpdate");
        ValidateCreate(dto);
    }

    public static List<SupplierInvoiceLine> MapLines(IEnumerable<SupplierInvoiceLineDto> lines)
        => lines.Where(IsMeaningfulLine).Select((line, index) =>
        {
            ValidateLine(line);
            return new SupplierInvoiceLine
            {
                PurchaseOrderLineId = NormalizeGuid(line.PurchaseOrderLineId),
                GoodsReceiptLineId = NormalizeGuid(line.GoodsReceiptLineId),
                ProductVariantId = NormalizeGuid(line.ProductVariantId),
                SupplierSku = Optional(line.SupplierSku, 100),
                Description = Required(line.Description, 1000),
                InvoicedQuantity = line.InvoicedQuantity,
                UnitNetMinor = line.UnitNetMinor,
                UnitTaxMinor = line.UnitTaxMinor,
                UnitGrossMinor = line.UnitGrossMinor,
                TotalNetMinor = line.TotalNetMinor == 0 ? line.UnitNetMinor * line.InvoicedQuantity : line.TotalNetMinor,
                TotalTaxMinor = line.TotalTaxMinor == 0 ? line.UnitTaxMinor * line.InvoicedQuantity : line.TotalTaxMinor,
                TotalGrossMinor = line.TotalGrossMinor == 0 ? line.UnitGrossMinor * line.InvoicedQuantity : line.TotalGrossMinor,
                TaxRate = line.TaxRate,
                MatchStatus = SupplierInvoiceLineMatchStatus.Unmatched,
                SortOrder = index
            };
        }).ToList();

    public static void RecalculateTotals(SupplierInvoice invoice)
    {
        invoice.TotalNetMinor = invoice.Lines.Where(x => !x.IsDeleted).Sum(x => x.TotalNetMinor);
        invoice.TotalTaxMinor = invoice.Lines.Where(x => !x.IsDeleted).Sum(x => x.TotalTaxMinor);
        invoice.TotalGrossMinor = invoice.Lines.Where(x => !x.IsDeleted).Sum(x => x.TotalGrossMinor);
    }

    public static async Task ValidateLinkedSourcesAsync(IAppDbContext db, Guid businessId, Guid supplierId, Guid? purchaseOrderId, Guid? goodsReceiptId, CancellationToken ct)
    {
        var supplierExists = await db.Set<Supplier>().AnyAsync(x => x.Id == supplierId && x.BusinessId == businessId && !x.IsDeleted, ct).ConfigureAwait(false);
        if (!supplierExists) throw new InvalidOperationException("SupplierNotFound");
        if (NormalizeGuid(purchaseOrderId).HasValue)
        {
            var poOk = await db.Set<PurchaseOrder>().AnyAsync(x => x.Id == purchaseOrderId && x.BusinessId == businessId && x.SupplierId == supplierId && !x.IsDeleted, ct).ConfigureAwait(false);
            if (!poOk) throw new InvalidOperationException("PurchaseOrderNotFound");
        }
        if (NormalizeGuid(goodsReceiptId).HasValue)
        {
            var grOk = await db.Set<GoodsReceipt>().AnyAsync(x => x.Id == goodsReceiptId && x.BusinessId == businessId && x.SupplierId == supplierId && !x.IsDeleted, ct).ConfigureAwait(false);
            if (!grOk) throw new InvalidOperationException("GoodsReceiptNotFound");
        }
    }

    public static async Task<SupplierInvoice> LoadForUpdateAsync(IAppDbContext db, Guid id, byte[] rowVersion, CancellationToken ct)
    {
        if (id == Guid.Empty || rowVersion.Length == 0) throw new ArgumentException("SupplierInvoiceInvalidUpdate");
        var invoice = await db.Set<SupplierInvoice>().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (invoice is null) throw new InvalidOperationException("SupplierInvoiceNotFound");
        if (!(invoice.RowVersion ?? Array.Empty<byte>()).SequenceEqual(rowVersion)) throw new InvalidOperationException("ItemConcurrencyConflict");
        return invoice;
    }

    public static async Task RecordEvidenceAsync(BusinessEventService? events, SupplierInvoice invoice, string action, AuditTrailAction auditAction, DateTime now, CancellationToken ct)
    {
        if (events is null) return;
        var payload = $$"""{"supplierInvoiceId":"{{invoice.Id}}","businessId":"{{invoice.BusinessId}}","supplierId":"{{invoice.SupplierId}}","status":"{{invoice.Status}}","currency":"{{invoice.Currency}}","totalGrossMinor":{{invoice.TotalGrossMinor}},"lineCount":{{invoice.Lines.Count(x => !x.IsDeleted)}}}""";
        var eventResult = await events.AddEventAsync(new AddBusinessEventCommand(
            invoice.BusinessId,
            "SupplierInvoice",
            invoice.Id,
            $"payables.supplier_invoice.{action}",
            $"payables.supplier_invoice.{action}:{invoice.Id}:{invoice.Status}",
            now,
            null,
            BusinessEventSource.User,
            BusinessEventSeverity.Info,
            FoundationVisibility.Internal,
            $"Supplier invoice {invoice.Status}",
            null,
            null,
            null,
            payload), ct).ConfigureAwait(false);
        if (!eventResult.Succeeded) throw new InvalidOperationException(eventResult.Error);
        var auditResult = await events.AddAuditTrailAsync(new AddAuditTrailCommand(
            invoice.BusinessId,
            "SupplierInvoice",
            invoice.Id,
            auditAction,
            now,
            null,
            eventResult.Value,
            $"Supplier invoice {action}",
            null,
            payload), ct).ConfigureAwait(false);
        if (!auditResult.Succeeded) throw new InvalidOperationException(auditResult.Error);
    }

    public static void ThrowIfFailed(Result result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(result.Error);
    }

    public static string Required(string? value, int max)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0) throw new ArgumentException("SupplierInvoiceRequiredField");
        return normalized.Length > max ? normalized[..max] : normalized;
    }

    public static string? Optional(string? value, int max)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        return normalized.Length > max ? normalized[..max] : normalized;
    }

    public static string NormalizeCurrency(string? value)
    {
        var normalized = Required(value, 3).ToUpperInvariant();
        if (normalized.Length != 3) throw new ArgumentException("SupplierInvoiceInvalidCurrency");
        return normalized;
    }

    public static string NormalizeMetadata(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "{}" : value.Trim();
        var compact = normalized.Replace("_", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
        if (SensitiveTokens.Any(compact.Contains)) throw new ArgumentException("SensitiveMetadataRejected");
        return normalized;
    }

    public static Guid? NormalizeGuid(Guid? value) => value.HasValue && value.Value != Guid.Empty ? value.Value : null;
    public static DateTime EnsureUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    public static DateTime? EnsureUtc(DateTime? value) => value.HasValue ? EnsureUtc(value.Value) : null;

    private static void ValidateLine(SupplierInvoiceLineDto line)
    {
        _ = Required(line.Description, 1000);
        if (line.InvoicedQuantity <= 0) throw new ArgumentException("SupplierInvoiceInvalidQuantity");
        if (line.UnitNetMinor < 0 || line.UnitTaxMinor < 0 || line.UnitGrossMinor < 0 || line.TotalNetMinor < 0 || line.TotalTaxMinor < 0 || line.TotalGrossMinor < 0) throw new ArgumentException("SupplierInvoiceInvalidAmount");
    }

    private static bool IsMeaningfulLine(SupplierInvoiceLineDto line)
        => !string.IsNullOrWhiteSpace(line.Description) ||
           !string.IsNullOrWhiteSpace(line.SupplierSku) ||
           line.InvoicedQuantity != 0 ||
           line.UnitNetMinor != 0 ||
           line.UnitTaxMinor != 0 ||
           line.UnitGrossMinor != 0 ||
           NormalizeGuid(line.PurchaseOrderLineId).HasValue ||
           NormalizeGuid(line.GoodsReceiptLineId).HasValue;
}
