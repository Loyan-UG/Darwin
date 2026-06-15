using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Billing.Queries;
using Darwin.Application.Billing.Services;
using Darwin.Application.Foundation;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Commands;

public sealed class SupplierPaymentWorkflowPolicy
{
    public Result CanUpdate(SupplierPayment payment) => payment.Status == SupplierPaymentStatus.Draft
        ? Result.Ok()
        : Result.Fail("SupplierPaymentLifecycleUnsupportedAction");

    public Result CanPost(SupplierPayment payment) => payment.Status == SupplierPaymentStatus.Draft
        ? Result.Ok()
        : Result.Fail("SupplierPaymentLifecycleUnsupportedAction");

    public Result CanCancel(SupplierPayment payment) => payment.Status == SupplierPaymentStatus.Draft
        ? Result.Ok()
        : Result.Fail("SupplierPaymentLifecycleUnsupportedAction");

    public Result CanReverse(SupplierPayment payment) => payment.Status == SupplierPaymentStatus.Posted
        ? Result.Ok()
        : Result.Fail("SupplierPaymentLifecycleUnsupportedAction");
}

public sealed class CreateSupplierPaymentHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreateSupplierPaymentHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Guid> HandleAsync(SupplierPaymentCreateDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        SupplierPaymentSupport.ValidateCreate(dto);
        await SupplierPaymentSupport.ValidateSupplierAsync(_db, dto.BusinessId, dto.SupplierId, ct).ConfigureAwait(false);
        var allocations = await SupplierPaymentSupport.MapAndValidateAllocationsAsync(_db, dto, null, ct).ConfigureAwait(false);

        var payment = new SupplierPayment
        {
            BusinessId = dto.BusinessId,
            SupplierId = dto.SupplierId,
            PaymentMethod = dto.PaymentMethod,
            PaymentDateUtc = SupplierInvoiceSupport.EnsureUtc(dto.PaymentDateUtc == default ? _clock.UtcNow : dto.PaymentDateUtc),
            Currency = SupplierInvoiceSupport.NormalizeCurrency(dto.Currency),
            Reference = SupplierInvoiceSupport.Optional(dto.Reference, 256),
            InternalNotes = SupplierInvoiceSupport.Optional(dto.InternalNotes, 4000),
            MetadataJson = SupplierInvoiceSupport.NormalizeMetadata(dto.MetadataJson),
            Allocations = allocations
        };
        SupplierPaymentSupport.RecalculateTotals(payment);
        _db.Set<SupplierPayment>().Add(payment);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await SupplierPaymentSupport.RecordEvidenceAsync(_events, payment, "created", AuditTrailAction.Created, _clock.UtcNow, ct).ConfigureAwait(false);
        return payment.Id;
    }
}

public sealed class UpdateSupplierPaymentHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly SupplierPaymentWorkflowPolicy _workflow;
    private readonly BusinessEventService? _events;

    public UpdateSupplierPaymentHandler(IAppDbContext db, IClock clock, SupplierPaymentWorkflowPolicy workflow, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        _events = events;
    }

    public async Task HandleAsync(SupplierPaymentEditDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        SupplierPaymentSupport.ValidateUpdate(dto);
        var payment = await SupplierPaymentSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        SupplierInvoiceSupport.ThrowIfFailed(_workflow.CanUpdate(payment));
        await SupplierPaymentSupport.ValidateSupplierAsync(_db, dto.BusinessId, dto.SupplierId, ct).ConfigureAwait(false);
        var allocations = await SupplierPaymentSupport.MapAndValidateAllocationsAsync(_db, dto, payment.Id, ct).ConfigureAwait(false);

        payment.BusinessId = dto.BusinessId;
        payment.SupplierId = dto.SupplierId;
        payment.PaymentMethod = dto.PaymentMethod;
        payment.PaymentDateUtc = SupplierInvoiceSupport.EnsureUtc(dto.PaymentDateUtc);
        payment.Currency = SupplierInvoiceSupport.NormalizeCurrency(dto.Currency);
        payment.Reference = SupplierInvoiceSupport.Optional(dto.Reference, 256);
        payment.InternalNotes = SupplierInvoiceSupport.Optional(dto.InternalNotes, 4000);
        payment.MetadataJson = SupplierInvoiceSupport.NormalizeMetadata(dto.MetadataJson);
        payment.Allocations.RemoveAll(_ => true);
        payment.Allocations.AddRange(allocations);
        SupplierPaymentSupport.RecalculateTotals(payment);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await SupplierPaymentSupport.RecordEvidenceAsync(_events, payment, "updated", AuditTrailAction.Updated, _clock.UtcNow, ct).ConfigureAwait(false);
    }
}

public sealed class PostSupplierPaymentHandler
{
    public const string PostingKeyPrefix = "supplier-payment-posted";

    private static readonly FinancePostingAccountRole[] RequiredRoles =
    [
        FinancePostingAccountRole.AccountsPayable,
        FinancePostingAccountRole.CashClearing
    ];

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly NumberSequenceService _numbers;
    private readonly SupplierPaymentWorkflowPolicy _workflow;
    private readonly FinanceAccountMappingService _accounts;
    private readonly FinancePostingService _posting;
    private readonly BusinessEventService? _events;

    public PostSupplierPaymentHandler(
        IAppDbContext db,
        IClock clock,
        NumberSequenceService numbers,
        SupplierPaymentWorkflowPolicy workflow,
        FinanceAccountMappingService accounts,
        FinancePostingService posting,
        BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _posting = posting ?? throw new ArgumentNullException(nameof(posting));
        _events = events;
    }

    public async Task HandleAsync(SupplierPaymentLifecycleActionDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var payment = await SupplierPaymentSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        SupplierInvoiceSupport.ThrowIfFailed(_workflow.CanPost(payment));
        await SupplierPaymentSupport.ValidateAllocationsAgainstOpenPayableAsync(_db, payment, payment.Id, ct).ConfigureAwait(false);

        var accountResult = await _accounts.ResolveRequiredAccountsAsync(payment.BusinessId, RequiredRoles, ct).ConfigureAwait(false);
        if (!accountResult.Succeeded)
        {
            throw new InvalidOperationException(accountResult.Error);
        }

        if (string.IsNullOrWhiteSpace(payment.PaymentNumber))
        {
            var number = await _numbers.ReserveNextAsync(new NumberSequenceRequest(payment.BusinessId, NumberSequenceDocumentType.SupplierPayment, NumberSequenceService.GlobalScopeKey), ct).ConfigureAwait(false);
            if (number.Succeeded && !string.IsNullOrWhiteSpace(number.Value))
            {
                payment.PaymentNumber = number.Value;
            }
        }

        SupplierPaymentSupport.RecalculateTotals(payment);
        if (payment.TotalAmountMinor <= 0) throw new InvalidOperationException("SupplierPaymentAmountRequired");
        var accounts = accountResult.Value!;
        var postingResult = await _posting.PostAsync(new FinancePostingCommand(
            payment.BusinessId,
            payment.PaymentDateUtc,
            JournalEntryPostingKind.SupplierPaymentPosted,
            $"{PostingKeyPrefix}:{payment.Id}",
            "SupplierPayment",
            payment.Id,
            "Supplier payment posted",
            [
                new FinancePostingLineCommand(accounts[FinancePostingAccountRole.AccountsPayable], payment.TotalAmountMinor, 0, "Accounts payable settlement"),
                new FinancePostingLineCommand(accounts[FinancePostingAccountRole.CashClearing], 0, payment.TotalAmountMinor, "Cash clearing")
            ],
            SourceDocumentNumber: payment.PaymentNumber ?? payment.Reference,
            PostingReason: "Supplier payment settlement",
            MetadataJson: $$"""{"supplierPaymentId":"{{payment.Id}}","supplierId":"{{payment.SupplierId}}","currency":"{{payment.Currency}}","totalAmountMinor":{{payment.TotalAmountMinor}},"allocationCount":{{payment.Allocations.Count(x => !x.IsDeleted)}}}"""), ct).ConfigureAwait(false);
        if (!postingResult.Succeeded)
        {
            throw new InvalidOperationException(postingResult.Error);
        }

        payment.PostingJournalEntryId = postingResult.Value!.JournalEntryId;
        payment.PostedAtUtc ??= _clock.UtcNow;
        payment.Status = SupplierPaymentStatus.Posted;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await SupplierPaymentSupport.RecordEvidenceAsync(_events, payment, "posted", AuditTrailAction.StatusChanged, _clock.UtcNow, ct).ConfigureAwait(false);
    }
}

public sealed class CancelSupplierPaymentHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly SupplierPaymentWorkflowPolicy _workflow;
    private readonly BusinessEventService? _events;

    public CancelSupplierPaymentHandler(IAppDbContext db, IClock clock, SupplierPaymentWorkflowPolicy workflow, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        _events = events;
    }

    public async Task HandleAsync(SupplierPaymentLifecycleActionDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var payment = await SupplierPaymentSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        SupplierInvoiceSupport.ThrowIfFailed(_workflow.CanCancel(payment));
        payment.Status = SupplierPaymentStatus.Cancelled;
        payment.CancelledAtUtc ??= _clock.UtcNow;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await SupplierPaymentSupport.RecordEvidenceAsync(_events, payment, "cancelled", AuditTrailAction.StatusChanged, _clock.UtcNow, ct).ConfigureAwait(false);
    }
}

public sealed class ReverseSupplierPaymentHandler
{
    public const string PostingKeyPrefix = "supplier-payment-reversed";

    private static readonly FinancePostingAccountRole[] RequiredRoles =
    [
        FinancePostingAccountRole.AccountsPayable,
        FinancePostingAccountRole.CashClearing
    ];

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly SupplierPaymentWorkflowPolicy _workflow;
    private readonly FinanceAccountMappingService _accounts;
    private readonly FinancePostingService _posting;
    private readonly BusinessEventService? _events;

    public ReverseSupplierPaymentHandler(
        IAppDbContext db,
        IClock clock,
        SupplierPaymentWorkflowPolicy workflow,
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

    public async Task HandleAsync(SupplierPaymentLifecycleActionDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var reason = SupplierInvoiceSupport.Optional(dto.Reason, 1000);
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("SupplierPaymentReversalReasonRequired");
        _ = SupplierInvoiceSupport.NormalizeMetadata(reason);

        var payment = await SupplierPaymentSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        SupplierInvoiceSupport.ThrowIfFailed(_workflow.CanReverse(payment));
        if (!payment.PostingJournalEntryId.HasValue) throw new InvalidOperationException("SupplierPaymentPostingRequired");
        SupplierPaymentSupport.RecalculateTotals(payment);
        if (payment.TotalAmountMinor <= 0) throw new InvalidOperationException("SupplierPaymentAmountRequired");
        if (payment.Allocations.Count == 0 || payment.Allocations.All(x => x.IsDeleted)) throw new InvalidOperationException("SupplierPaymentAllocationsRequired");
        await SupplierPaymentSupport.ValidateSupplierAsync(_db, payment.BusinessId, payment.SupplierId, ct).ConfigureAwait(false);

        var accountResult = await _accounts.ResolveRequiredAccountsAsync(payment.BusinessId, RequiredRoles, ct).ConfigureAwait(false);
        if (!accountResult.Succeeded)
        {
            throw new InvalidOperationException(accountResult.Error);
        }

        var accounts = accountResult.Value!;
        var postingResult = await _posting.PostAsync(new FinancePostingCommand(
            payment.BusinessId,
            _clock.UtcNow,
            JournalEntryPostingKind.Reversal,
            $"{PostingKeyPrefix}:{payment.Id}",
            "SupplierPayment",
            payment.Id,
            "Supplier payment reversed",
            [
                new FinancePostingLineCommand(accounts[FinancePostingAccountRole.CashClearing], payment.TotalAmountMinor, 0, "Cash clearing reversal"),
                new FinancePostingLineCommand(accounts[FinancePostingAccountRole.AccountsPayable], 0, payment.TotalAmountMinor, "Accounts payable reinstatement")
            ],
            SourceDocumentNumber: payment.PaymentNumber ?? payment.Reference,
            PostingReason: reason,
            MetadataJson: $$"""{"supplierPaymentId":"{{payment.Id}}","originalJournalEntryId":"{{payment.PostingJournalEntryId}}","supplierId":"{{payment.SupplierId}}","currency":"{{payment.Currency}}","totalAmountMinor":{{payment.TotalAmountMinor}},"allocationCount":{{payment.Allocations.Count(x => !x.IsDeleted)}}}"""), ct).ConfigureAwait(false);
        if (!postingResult.Succeeded)
        {
            throw new InvalidOperationException(postingResult.Error);
        }

        payment.ReversalJournalEntryId = postingResult.Value!.JournalEntryId;
        payment.ReversedAtUtc ??= _clock.UtcNow;
        payment.ReversalReason = reason;
        payment.Status = SupplierPaymentStatus.Reversed;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await SupplierPaymentSupport.RecordEvidenceAsync(_events, payment, "reversed", AuditTrailAction.StatusChanged, _clock.UtcNow, ct).ConfigureAwait(false);
    }
}

internal static class SupplierPaymentSupport
{
    public static void ValidateCreate(SupplierPaymentCreateDto dto)
    {
        if (dto.BusinessId == Guid.Empty || dto.SupplierId == Guid.Empty) throw new ArgumentException("SupplierPaymentInvalidLink");
        if (!Enum.IsDefined(typeof(SupplierPaymentMethod), dto.PaymentMethod)) throw new ArgumentException("SupplierPaymentInvalidMethod");
        _ = SupplierInvoiceSupport.NormalizeCurrency(dto.Currency);
        _ = SupplierInvoiceSupport.NormalizeMetadata(dto.MetadataJson);
        if (dto.Allocations.Count == 0) throw new ArgumentException("SupplierPaymentAllocationsRequired");
        foreach (var allocation in dto.Allocations) ValidateAllocation(allocation);
    }

    public static void ValidateUpdate(SupplierPaymentEditDto dto)
    {
        if (dto.Id == Guid.Empty || dto.RowVersion.Length == 0) throw new ArgumentException("SupplierPaymentInvalidUpdate");
        ValidateCreate(dto);
    }

    public static async Task ValidateSupplierAsync(IAppDbContext db, Guid businessId, Guid supplierId, CancellationToken ct)
    {
        var supplierExists = await db.Set<Supplier>().AnyAsync(x => x.Id == supplierId && x.BusinessId == businessId && !x.IsDeleted, ct).ConfigureAwait(false);
        if (!supplierExists) throw new InvalidOperationException("SupplierNotFound");
    }

    public static async Task<List<SupplierPaymentAllocation>> MapAndValidateAllocationsAsync(IAppDbContext db, SupplierPaymentCreateDto dto, Guid? excludingPaymentId, CancellationToken ct)
    {
        var currency = SupplierInvoiceSupport.NormalizeCurrency(dto.Currency);
        var grouped = dto.Allocations
            .Where(x => x.SupplierInvoiceId != Guid.Empty && x.AmountMinor > 0)
            .GroupBy(x => x.SupplierInvoiceId)
            .Select(x => new SupplierPaymentAllocationDto
            {
                SupplierInvoiceId = x.Key,
                AmountMinor = x.Sum(a => a.AmountMinor),
                Memo = SupplierInvoiceSupport.Optional(string.Join("; ", x.Select(a => a.Memo).Where(m => !string.IsNullOrWhiteSpace(m))), 1000)
            })
            .ToList();
        if (grouped.Count == 0) throw new ArgumentException("SupplierPaymentAllocationsRequired");

        var invoiceIds = grouped.Select(x => x.SupplierInvoiceId).ToArray();
        var invoices = await db.Set<SupplierInvoice>()
            .AsNoTracking()
            .Where(x => invoiceIds.Contains(x.Id) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id, ct)
            .ConfigureAwait(false);
        foreach (var allocation in grouped)
        {
            if (!invoices.TryGetValue(allocation.SupplierInvoiceId, out var invoice)) throw new InvalidOperationException("SupplierInvoiceNotFound");
            if (invoice.BusinessId != dto.BusinessId || invoice.SupplierId != dto.SupplierId) throw new InvalidOperationException("SupplierPaymentInvoiceMismatch");
            if (invoice.Status != SupplierInvoiceStatus.Posted || !invoice.PostingJournalEntryId.HasValue) throw new InvalidOperationException("SupplierPaymentRequiresPostedInvoice");
            if (!string.Equals(invoice.Currency, currency, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("SupplierPaymentCurrencyMismatch");
        }

        var alreadyPaid = await SupplierPaymentQuerySupport.GetPostedPaidByInvoiceAsync(db, invoiceIds, excludingPaymentId, ct).ConfigureAwait(false);
        foreach (var allocation in grouped)
        {
            var invoice = invoices[allocation.SupplierInvoiceId];
            var paid = alreadyPaid.GetValueOrDefault(invoice.Id);
            if (paid + allocation.AmountMinor > invoice.TotalGrossMinor) throw new InvalidOperationException("SupplierPaymentOverpaymentRejected");
        }

        return grouped.Select(x => new SupplierPaymentAllocation
        {
            SupplierInvoiceId = x.SupplierInvoiceId,
            AmountMinor = x.AmountMinor,
            Memo = SupplierInvoiceSupport.Optional(x.Memo, 1000)
        }).ToList();
    }

    public static async Task ValidateAllocationsAgainstOpenPayableAsync(IAppDbContext db, SupplierPayment payment, Guid? excludingPaymentId, CancellationToken ct)
    {
        if (payment.Allocations.Count == 0 || payment.Allocations.All(x => x.IsDeleted)) throw new InvalidOperationException("SupplierPaymentAllocationsRequired");
        var dto = new SupplierPaymentCreateDto
        {
            BusinessId = payment.BusinessId,
            SupplierId = payment.SupplierId,
            Currency = payment.Currency,
            PaymentMethod = payment.PaymentMethod,
            PaymentDateUtc = payment.PaymentDateUtc,
            MetadataJson = payment.MetadataJson,
            Allocations = payment.Allocations.Where(x => !x.IsDeleted).Select(x => new SupplierPaymentAllocationDto
            {
                SupplierInvoiceId = x.SupplierInvoiceId,
                AmountMinor = x.AmountMinor,
                Memo = x.Memo
            }).ToList()
        };
        _ = await MapAndValidateAllocationsAsync(db, dto, excludingPaymentId, ct).ConfigureAwait(false);
    }

    public static void RecalculateTotals(SupplierPayment payment)
        => payment.TotalAmountMinor = payment.Allocations.Where(x => !x.IsDeleted).Sum(x => x.AmountMinor);

    public static async Task<SupplierPayment> LoadForUpdateAsync(IAppDbContext db, Guid id, byte[] rowVersion, CancellationToken ct)
    {
        if (id == Guid.Empty || rowVersion.Length == 0) throw new ArgumentException("SupplierPaymentInvalidUpdate");
        var payment = await db.Set<SupplierPayment>().Include(x => x.Allocations).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (payment is null) throw new InvalidOperationException("SupplierPaymentNotFound");
        if (!(payment.RowVersion ?? Array.Empty<byte>()).SequenceEqual(rowVersion)) throw new InvalidOperationException("ItemConcurrencyConflict");
        return payment;
    }

    public static async Task RecordEvidenceAsync(BusinessEventService? events, SupplierPayment payment, string action, AuditTrailAction auditAction, DateTime now, CancellationToken ct)
    {
        if (events is null) return;
        var payload = $$"""{"supplierPaymentId":"{{payment.Id}}","businessId":"{{payment.BusinessId}}","supplierId":"{{payment.SupplierId}}","status":"{{payment.Status}}","currency":"{{payment.Currency}}","totalAmountMinor":{{payment.TotalAmountMinor}},"allocationCount":{{payment.Allocations.Count(x => !x.IsDeleted)}}}""";
        var eventResult = await events.AddEventAsync(new AddBusinessEventCommand(
            payment.BusinessId,
            "SupplierPayment",
            payment.Id,
            $"payables.supplier_payment.{action}",
            $"payables.supplier_payment.{action}:{payment.Id}:{payment.Status}",
            now,
            null,
            BusinessEventSource.User,
            BusinessEventSeverity.Info,
            FoundationVisibility.Internal,
            $"Supplier payment {payment.Status}",
            null,
            null,
            null,
            payload), ct).ConfigureAwait(false);
        if (!eventResult.Succeeded) throw new InvalidOperationException(eventResult.Error);
        var auditResult = await events.AddAuditTrailAsync(new AddAuditTrailCommand(
            payment.BusinessId,
            "SupplierPayment",
            payment.Id,
            auditAction,
            now,
            null,
            eventResult.Value,
            $"Supplier payment {action}",
            null,
            payload), ct).ConfigureAwait(false);
        if (!auditResult.Succeeded) throw new InvalidOperationException(auditResult.Error);
    }

    private static void ValidateAllocation(SupplierPaymentAllocationDto allocation)
    {
        if (allocation.SupplierInvoiceId == Guid.Empty) throw new ArgumentException("SupplierPaymentInvalidAllocation");
        if (allocation.AmountMinor <= 0) throw new ArgumentException("SupplierPaymentInvalidAmount");
        _ = SupplierInvoiceSupport.Optional(allocation.Memo, 1000);
    }
}
