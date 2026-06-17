using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.Services;
using Darwin.Application.Foundation;
using Darwin.Application.HumanResources.DTOs;
using Darwin.Application.HumanResources.Queries;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.HumanResources.Commands;

public sealed class PayrollPaymentWorkflowPolicy
{
    public Result CanUpdate(PayrollPayment payment) => payment.Status == PayrollPaymentStatus.Draft
        ? Result.Ok()
        : Result.Fail("PayrollPaymentLifecycleUnsupportedAction");

    public Result CanPost(PayrollPayment payment) => payment.Status == PayrollPaymentStatus.Draft
        ? Result.Ok()
        : Result.Fail("PayrollPaymentLifecycleUnsupportedAction");

    public Result CanCancel(PayrollPayment payment) => payment.Status == PayrollPaymentStatus.Draft
        ? Result.Ok()
        : Result.Fail("PayrollPaymentLifecycleUnsupportedAction");

    public Result CanReverse(PayrollPayment payment) => payment.Status == PayrollPaymentStatus.Posted && !payment.BankSettledAtUtc.HasValue && !payment.BankSettlementJournalEntryId.HasValue
        ? Result.Ok()
        : Result.Fail("PayrollPaymentLifecycleUnsupportedAction");

    public Result CanBankSettle(PayrollPayment payment) => payment.Status == PayrollPaymentStatus.Posted && !payment.BankSettledAtUtc.HasValue && !payment.BankSettlementJournalEntryId.HasValue
        ? Result.Ok()
        : Result.Fail("PayrollPaymentLifecycleUnsupportedAction");
}

public sealed class CreatePayrollPaymentHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreatePayrollPaymentHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Guid> HandleAsync(PayrollPaymentCreateDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        PayrollPaymentSupport.ValidateCreate(dto);
        var run = await PayrollPaymentSupport.LoadPostedRunAsync(_db, dto.BusinessId, dto.PayrollRunId, ct).ConfigureAwait(false);
        var allocations = await PayrollPaymentSupport.MapAndValidateAllocationsAsync(_db, dto, run, null, ct).ConfigureAwait(false);
        var payment = new PayrollPayment
        {
            BusinessId = dto.BusinessId,
            PayrollRunId = dto.PayrollRunId,
            PaymentMethod = dto.PaymentMethod,
            PaymentDateUtc = PayrollPaymentSupport.EnsureUtc(dto.PaymentDateUtc == default ? _clock.UtcNow : dto.PaymentDateUtc),
            Currency = PayrollPaymentSupport.NormalizeCurrency(dto.Currency),
            Reference = HrCoreSupport.Optional(dto.Reference),
            InternalNotes = HrCoreSupport.Optional(dto.InternalNotes),
            MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson),
            Allocations = allocations
        };
        PayrollPaymentSupport.RecalculateTotals(payment);
        _db.Set<PayrollPayment>().Add(payment);
        await PayrollPaymentSupport.RecordEvidenceAsync(_db, _events, _clock, payment, "created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return payment.Id;
    }
}

public sealed class UpdatePayrollPaymentHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly PayrollPaymentWorkflowPolicy _workflow;
    private readonly BusinessEventService? _events;

    public UpdatePayrollPaymentHandler(IAppDbContext db, IClock clock, PayrollPaymentWorkflowPolicy workflow, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        _events = events;
    }

    public async Task HandleAsync(PayrollPaymentEditDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        PayrollPaymentSupport.ValidateUpdate(dto);
        var payment = await PayrollPaymentSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        PayrollPaymentSupport.ThrowIfFailed(_workflow.CanUpdate(payment));
        var run = await PayrollPaymentSupport.LoadPostedRunAsync(_db, dto.BusinessId, dto.PayrollRunId, ct).ConfigureAwait(false);
        var allocations = await PayrollPaymentSupport.MapAndValidateAllocationsAsync(_db, dto, run, payment.Id, ct).ConfigureAwait(false);

        payment.BusinessId = dto.BusinessId;
        payment.PayrollRunId = dto.PayrollRunId;
        payment.PaymentMethod = dto.PaymentMethod;
        payment.PaymentDateUtc = PayrollPaymentSupport.EnsureUtc(dto.PaymentDateUtc);
        payment.Currency = PayrollPaymentSupport.NormalizeCurrency(dto.Currency);
        payment.Reference = HrCoreSupport.Optional(dto.Reference);
        payment.InternalNotes = HrCoreSupport.Optional(dto.InternalNotes);
        payment.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
        payment.Allocations.RemoveAll(_ => true);
        payment.Allocations.AddRange(allocations);
        PayrollPaymentSupport.RecalculateTotals(payment);

        await PayrollPaymentSupport.RecordEvidenceAsync(_db, _events, _clock, payment, "updated", AuditTrailAction.Updated, ct).ConfigureAwait(false);
    }
}

public sealed class PostPayrollPaymentHandler
{
    public const string PostingKeyPrefix = "payroll-payment-posted";

    private static readonly FinancePostingAccountRole[] RequiredRoles =
    [
        FinancePostingAccountRole.PayrollPayable,
        FinancePostingAccountRole.CashClearing
    ];

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly NumberSequenceService _numbers;
    private readonly PayrollPaymentWorkflowPolicy _workflow;
    private readonly FinanceAccountMappingService _accounts;
    private readonly FinancePostingService _posting;
    private readonly BusinessEventService? _events;

    public PostPayrollPaymentHandler(
        IAppDbContext db,
        IClock clock,
        NumberSequenceService numbers,
        PayrollPaymentWorkflowPolicy workflow,
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

    public async Task HandleAsync(PayrollPaymentLifecycleActionDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var payment = await PayrollPaymentSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        PayrollPaymentSupport.ThrowIfFailed(_workflow.CanPost(payment));
        await PayrollPaymentSupport.ValidateAllocationsAgainstOpenPayableAsync(_db, payment, payment.Id, ct).ConfigureAwait(false);

        var accountResult = await _accounts.ResolveRequiredAccountsAsync(payment.BusinessId, RequiredRoles, ct).ConfigureAwait(false);
        if (!accountResult.Succeeded) throw new InvalidOperationException(accountResult.Error);

        if (string.IsNullOrWhiteSpace(payment.PaymentNumber))
        {
            var number = await _numbers.ReserveNextAsync(new NumberSequenceRequest(payment.BusinessId, NumberSequenceDocumentType.PayrollPayment, NumberSequenceService.GlobalScopeKey), ct).ConfigureAwait(false);
            if (number.Succeeded && !string.IsNullOrWhiteSpace(number.Value)) payment.PaymentNumber = number.Value;
        }

        PayrollPaymentSupport.RecalculateTotals(payment);
        if (payment.TotalAmountMinor <= 0) throw new InvalidOperationException("PayrollPaymentAmountRequired");
        var accounts = accountResult.Value!;
        var postingResult = await _posting.PostAsync(new FinancePostingCommand(
            payment.BusinessId,
            payment.PaymentDateUtc,
            JournalEntryPostingKind.PayrollPaymentPosted,
            $"{PostingKeyPrefix}:{payment.Id}",
            "PayrollPayment",
            payment.Id,
            "Payroll payment posted",
            [
                new FinancePostingLineCommand(accounts[FinancePostingAccountRole.PayrollPayable], payment.TotalAmountMinor, 0, "Payroll payable settlement"),
                new FinancePostingLineCommand(accounts[FinancePostingAccountRole.CashClearing], 0, payment.TotalAmountMinor, "Cash clearing")
            ],
            SourceDocumentNumber: payment.PaymentNumber ?? payment.Reference,
            PostingReason: "Payroll salary payment",
            MetadataJson: $$"""{"payrollPaymentId":"{{payment.Id}}","payrollRunId":"{{payment.PayrollRunId}}","currency":"{{payment.Currency}}","totalAmountMinor":{{payment.TotalAmountMinor}},"allocationCount":{{payment.Allocations.Count(x => !x.IsDeleted)}}}"""), ct).ConfigureAwait(false);
        if (!postingResult.Succeeded) throw new InvalidOperationException(postingResult.Error);

        payment.PostingJournalEntryId = postingResult.Value!.JournalEntryId;
        payment.PostedAtUtc ??= _clock.UtcNow;
        payment.Status = PayrollPaymentStatus.Posted;
        await PayrollPaymentSupport.RecordEvidenceAsync(_db, _events, _clock, payment, "posted", AuditTrailAction.StatusChanged, ct).ConfigureAwait(false);
    }
}

public sealed class CancelPayrollPaymentHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly PayrollPaymentWorkflowPolicy _workflow;
    private readonly BusinessEventService? _events;

    public CancelPayrollPaymentHandler(IAppDbContext db, IClock clock, PayrollPaymentWorkflowPolicy workflow, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        _events = events;
    }

    public async Task HandleAsync(PayrollPaymentLifecycleActionDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var payment = await PayrollPaymentSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        PayrollPaymentSupport.ThrowIfFailed(_workflow.CanCancel(payment));
        payment.Status = PayrollPaymentStatus.Cancelled;
        payment.CancelledAtUtc ??= _clock.UtcNow;
        await PayrollPaymentSupport.RecordEvidenceAsync(_db, _events, _clock, payment, "cancelled", AuditTrailAction.StatusChanged, ct).ConfigureAwait(false);
    }
}

public sealed class ReversePayrollPaymentHandler
{
    public const string PostingKeyPrefix = "payroll-payment-reversed";

    private static readonly FinancePostingAccountRole[] RequiredRoles =
    [
        FinancePostingAccountRole.PayrollPayable,
        FinancePostingAccountRole.CashClearing
    ];

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly PayrollPaymentWorkflowPolicy _workflow;
    private readonly FinanceAccountMappingService _accounts;
    private readonly FinancePostingService _posting;
    private readonly BusinessEventService? _events;

    public ReversePayrollPaymentHandler(
        IAppDbContext db,
        IClock clock,
        PayrollPaymentWorkflowPolicy workflow,
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

    public async Task HandleAsync(PayrollPaymentLifecycleActionDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var reason = HrCoreSupport.Optional(dto.Reason);
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("PayrollPaymentReversalReasonRequired");
        if (reason.Length > 1000) throw new ArgumentException("PayrollPaymentReversalReasonTooLong");
        HrCoreSupport.EnsureSafe(reason);

        var payment = await PayrollPaymentSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        PayrollPaymentSupport.ThrowIfFailed(_workflow.CanReverse(payment));
        if (!payment.PostingJournalEntryId.HasValue) throw new InvalidOperationException("PayrollPaymentPostingRequired");
        if (payment.ReversalJournalEntryId.HasValue || payment.ReversedAtUtc.HasValue) throw new InvalidOperationException("PayrollPaymentAlreadyReversed");
        PayrollPaymentSupport.RecalculateTotals(payment);
        if (payment.TotalAmountMinor <= 0) throw new InvalidOperationException("PayrollPaymentAmountRequired");
        if (payment.Allocations.Count == 0 || payment.Allocations.All(x => x.IsDeleted)) throw new InvalidOperationException("PayrollPaymentAllocationsRequired");

        var accountResult = await _accounts.ResolveRequiredAccountsAsync(payment.BusinessId, RequiredRoles, ct).ConfigureAwait(false);
        if (!accountResult.Succeeded) throw new InvalidOperationException(accountResult.Error);

        var accounts = accountResult.Value!;
        var postingResult = await _posting.PostAsync(new FinancePostingCommand(
            payment.BusinessId,
            _clock.UtcNow,
            JournalEntryPostingKind.Reversal,
            $"{PostingKeyPrefix}:{payment.Id}",
            "PayrollPayment",
            payment.Id,
            "Payroll payment reversed",
            [
                new FinancePostingLineCommand(accounts[FinancePostingAccountRole.CashClearing], payment.TotalAmountMinor, 0, "Cash clearing reversal"),
                new FinancePostingLineCommand(accounts[FinancePostingAccountRole.PayrollPayable], 0, payment.TotalAmountMinor, "Payroll payable reinstatement")
            ],
            SourceDocumentNumber: payment.PaymentNumber ?? payment.Reference,
            PostingReason: reason,
            MetadataJson: $$"""{"payrollPaymentId":"{{payment.Id}}","originalJournalEntryId":"{{payment.PostingJournalEntryId}}","payrollRunId":"{{payment.PayrollRunId}}","currency":"{{payment.Currency}}","totalAmountMinor":{{payment.TotalAmountMinor}},"allocationCount":{{payment.Allocations.Count(x => !x.IsDeleted)}}}"""), ct).ConfigureAwait(false);
        if (!postingResult.Succeeded) throw new InvalidOperationException(postingResult.Error);

        payment.ReversalJournalEntryId = postingResult.Value!.JournalEntryId;
        payment.ReversedAtUtc ??= _clock.UtcNow;
        payment.ReversalReason = reason;
        payment.Status = PayrollPaymentStatus.Reversed;
        await PayrollPaymentSupport.RecordEvidenceAsync(_db, _events, _clock, payment, "reversed", AuditTrailAction.StatusChanged, ct).ConfigureAwait(false);
    }
}

public sealed class SettlePayrollPaymentFromBankReconciliationHandler
{
    public const string PostingKeyPrefix = "payroll-payment-bank-settled";

    private static readonly FinancePostingAccountRole[] RequiredRoles =
    [
        FinancePostingAccountRole.CashClearing
    ];

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly PayrollPaymentWorkflowPolicy _workflow;
    private readonly FinanceAccountMappingService _accounts;
    private readonly FinancePostingService _posting;
    private readonly BusinessEventService? _events;

    public SettlePayrollPaymentFromBankReconciliationHandler(
        IAppDbContext db,
        IClock clock,
        PayrollPaymentWorkflowPolicy workflow,
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

    public async Task HandleAsync(PayrollPaymentBankSettlementActionDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Id == Guid.Empty || dto.RowVersion.Length == 0 || dto.BankReconciliationMatchId == Guid.Empty) throw new ArgumentException("PayrollPaymentBankSettlementInvalidRequest");
        var notes = HrCoreSupport.Optional(dto.Notes);
        if (notes?.Length > 1000) throw new ArgumentException("PayrollPaymentBankSettlementNotesTooLong");
        HrCoreSupport.EnsureSafe(notes);

        var payment = await PayrollPaymentSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        PayrollPaymentSupport.ThrowIfFailed(_workflow.CanBankSettle(payment));
        if (!payment.PostingJournalEntryId.HasValue) throw new InvalidOperationException("PayrollPaymentPostingRequired");
        if (payment.ReversalJournalEntryId.HasValue || payment.ReversedAtUtc.HasValue) throw new InvalidOperationException("PayrollPaymentReversed");
        PayrollPaymentSupport.RecalculateTotals(payment);
        if (payment.TotalAmountMinor <= 0) throw new InvalidOperationException("PayrollPaymentAmountRequired");
        if (payment.Allocations.Count == 0 || payment.Allocations.All(x => x.IsDeleted)) throw new InvalidOperationException("PayrollPaymentAllocationsRequired");

        var reconciliation = await _db.Set<BankReconciliationMatch>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == dto.BankReconciliationMatchId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (reconciliation is null) throw new InvalidOperationException("BankReconciliationNotFound");
        if (reconciliation.Status != BankReconciliationMatchStatus.Matched) throw new InvalidOperationException("PayrollPaymentBankSettlementRequiresMatchedReconciliation");
        if (reconciliation.BusinessId != payment.BusinessId || !string.Equals(reconciliation.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("PayrollPaymentBankSettlementReconciliationMismatch");
        if (reconciliation.DifferenceMinor != 0) throw new InvalidOperationException("PayrollPaymentBankSettlementRequiresZeroDifference");

        var paymentLines = reconciliation.Lines
            .Where(x =>
                !x.IsDeleted &&
                x.IsActive &&
                x.JournalEntryId == payment.PostingJournalEntryId &&
                string.Equals(x.SourceEntityType, "PayrollPayment", StringComparison.OrdinalIgnoreCase) &&
                x.SourceEntityId == payment.Id)
            .ToList();
        if (paymentLines.Count == 0) throw new InvalidOperationException("PayrollPaymentBankSettlementReconciliationPaymentLinkRequired");
        if (paymentLines.Sum(x => x.AmountMinor) != payment.TotalAmountMinor) throw new InvalidOperationException("PayrollPaymentBankSettlementRequiresFullAmount");

        var bankAccount = await _db.Set<BankAccount>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == reconciliation.BankAccountId && x.BusinessId == payment.BusinessId && !x.IsDeleted && x.Status == BankAccountStatus.Active, ct)
            .ConfigureAwait(false);
        if (bankAccount is null) throw new InvalidOperationException("BankAccountNotFound");
        if (!bankAccount.FinancialAccountId.HasValue) throw new InvalidOperationException("PayrollPaymentBankSettlementRequiresMappedBankAccount");

        var financialAccount = await _db.Set<FinancialAccount>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == bankAccount.FinancialAccountId.Value && x.BusinessId == payment.BusinessId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (financialAccount is null || financialAccount.Type != AccountType.Asset) throw new InvalidOperationException("PayrollPaymentBankSettlementRequiresAssetBankAccount");

        var accountResult = await _accounts.ResolveRequiredAccountsAsync(payment.BusinessId, RequiredRoles, ct).ConfigureAwait(false);
        if (!accountResult.Succeeded) throw new InvalidOperationException(accountResult.Error);

        var accounts = accountResult.Value!;
        var postingResult = await _posting.PostAsync(new FinancePostingCommand(
            payment.BusinessId,
            _clock.UtcNow,
            JournalEntryPostingKind.PayrollPaymentBankSettled,
            $"{PostingKeyPrefix}:{payment.Id}",
            "PayrollPayment",
            payment.Id,
            "Payroll payment bank settled",
            [
                new FinancePostingLineCommand(accounts[FinancePostingAccountRole.CashClearing], payment.TotalAmountMinor, 0, "Cash clearing release"),
                new FinancePostingLineCommand(financialAccount.Id, 0, payment.TotalAmountMinor, "Bank account settlement")
            ],
            SourceDocumentNumber: payment.PaymentNumber ?? payment.Reference,
            PostingReason: "Payroll payment bank settlement",
            MetadataJson: $$"""{"payrollPaymentId":"{{payment.Id}}","bankReconciliationMatchId":"{{reconciliation.Id}}","bankAccountId":"{{bankAccount.Id}}","bankFinancialAccountId":"{{financialAccount.Id}}","payrollRunId":"{{payment.PayrollRunId}}","currency":"{{payment.Currency}}","totalAmountMinor":{{payment.TotalAmountMinor}}}"""), ct).ConfigureAwait(false);
        if (!postingResult.Succeeded) throw new InvalidOperationException(postingResult.Error);

        payment.BankSettlementJournalEntryId = postingResult.Value!.JournalEntryId;
        payment.BankSettlementReconciliationMatchId = reconciliation.Id;
        payment.BankSettledAtUtc ??= _clock.UtcNow;
        payment.BankSettlementNotes = notes;
        await PayrollPaymentSupport.RecordEvidenceAsync(_db, _events, _clock, payment, "bank_settled", AuditTrailAction.StatusChanged, ct).ConfigureAwait(false);
    }
}

public sealed class CreatePayrollPaymentBankCorrectionHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreatePayrollPaymentBankCorrectionHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Guid> HandleAsync(PayrollPaymentBankCorrectionCreateDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.PayrollPaymentId == Guid.Empty || dto.PayrollPaymentRowVersion.Length == 0 || dto.BankReconciliationMatchId == Guid.Empty) throw new ArgumentException("PayrollPaymentBankCorrectionInvalidRequest");
        if (!Enum.IsDefined(typeof(PayrollPaymentBankCorrectionType), dto.CorrectionType)) throw new ArgumentException("PayrollPaymentBankCorrectionInvalidType");
        var reason = HrCoreSupport.Optional(dto.Reason);
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("PayrollPaymentBankCorrectionReasonRequired");
        if (reason.Length > 1000) throw new ArgumentException("PayrollPaymentBankCorrectionReasonTooLong");
        var notes = HrCoreSupport.Optional(dto.InternalNotes);
        if (notes?.Length > 4000) throw new ArgumentException("PayrollPaymentBankCorrectionNotesTooLong");
        HrCoreSupport.EnsureSafe(reason, notes);

        var payment = await PayrollPaymentSupport.LoadForUpdateAsync(_db, dto.PayrollPaymentId, dto.PayrollPaymentRowVersion, ct).ConfigureAwait(false);
        PayrollPaymentBankCorrectionSupport.ValidateBankSettledPayment(payment);
        var reconciliation = await PayrollPaymentBankCorrectionSupport.LoadAndValidateEvidenceAsync(_db, payment, dto.BankReconciliationMatchId, dto.BankStatementLineId, ct).ConfigureAwait(false);
        var activeExists = await _db.Set<PayrollPaymentBankCorrection>()
            .AnyAsync(x =>
                x.PayrollPaymentId == payment.Id &&
                x.BankReconciliationMatchId == reconciliation.Id &&
                x.CorrectionType == dto.CorrectionType &&
                x.Status != PayrollPaymentBankCorrectionStatus.Cancelled &&
                !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (activeExists) throw new InvalidOperationException("PayrollPaymentBankCorrectionDuplicate");

        var statementLineId = dto.BankStatementLineId ?? reconciliation.Lines.Where(x => !x.IsDeleted && x.IsActive).OrderBy(x => x.SortOrder).Select(x => (Guid?)x.BankStatementLineId).FirstOrDefault();
        var correction = new PayrollPaymentBankCorrection
        {
            BusinessId = payment.BusinessId,
            PayrollPaymentId = payment.Id,
            BankReconciliationMatchId = reconciliation.Id,
            BankStatementLineId = statementLineId,
            OriginalBankSettlementJournalEntryId = payment.BankSettlementJournalEntryId,
            CorrectionType = dto.CorrectionType,
            Status = PayrollPaymentBankCorrectionStatus.Draft,
            CorrectionDateUtc = _clock.UtcNow,
            Currency = payment.Currency,
            AmountMinor = payment.TotalAmountMinor,
            Reason = reason,
            InternalNotes = notes,
            MetadataJson = "{}"
        };

        _db.Set<PayrollPaymentBankCorrection>().Add(correction);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await PayrollPaymentBankCorrectionSupport.RecordEvidenceAsync(_events, correction, "created", AuditTrailAction.Created, _clock.UtcNow, ct).ConfigureAwait(false);
        return correction.Id;
    }
}

public sealed class PostPayrollPaymentBankCorrectionHandler
{
    public const string PostingKeyPrefix = "payroll-payment-bank-correction";

    private static readonly FinancePostingAccountRole[] RequiredRoles =
    [
        FinancePostingAccountRole.CashClearing
    ];

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly FinanceAccountMappingService _accounts;
    private readonly FinancePostingService _posting;
    private readonly BusinessEventService? _events;

    public PostPayrollPaymentBankCorrectionHandler(
        IAppDbContext db,
        IClock clock,
        FinanceAccountMappingService accounts,
        FinancePostingService posting,
        BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _posting = posting ?? throw new ArgumentNullException(nameof(posting));
        _events = events;
    }

    public async Task HandleAsync(PayrollPaymentBankCorrectionActionDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var correction = await PayrollPaymentBankCorrectionSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        if (correction.Status != PayrollPaymentBankCorrectionStatus.Draft) throw new InvalidOperationException("PayrollPaymentBankCorrectionNotPostable");
        if (correction.CorrectionType != PayrollPaymentBankCorrectionType.ReturnedTransfer) throw new InvalidOperationException("PayrollPaymentBankCorrectionDuplicatePaymentIsAttentionOnly");

        var payment = await _db.Set<PayrollPayment>()
            .Include(x => x.Allocations)
            .FirstOrDefaultAsync(x => x.Id == correction.PayrollPaymentId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (payment is null) throw new InvalidOperationException("PayrollPaymentNotFound");
        PayrollPaymentBankCorrectionSupport.ValidateBankSettledPayment(payment);
        if (payment.BankSettlementJournalEntryId != correction.OriginalBankSettlementJournalEntryId) throw new InvalidOperationException("PayrollPaymentBankCorrectionSettlementMismatch");
        PayrollPaymentSupport.RecalculateTotals(payment);
        if (payment.TotalAmountMinor != correction.AmountMinor) throw new InvalidOperationException("PayrollPaymentBankCorrectionAmountMismatch");

        var reconciliation = await PayrollPaymentBankCorrectionSupport.LoadAndValidateEvidenceAsync(_db, payment, correction.BankReconciliationMatchId, correction.BankStatementLineId, ct).ConfigureAwait(false);
        var bankAccount = await _db.Set<BankAccount>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == reconciliation.BankAccountId && x.BusinessId == payment.BusinessId && !x.IsDeleted && x.Status == BankAccountStatus.Active, ct)
            .ConfigureAwait(false);
        if (bankAccount is null || !bankAccount.FinancialAccountId.HasValue) throw new InvalidOperationException("PayrollPaymentBankCorrectionRequiresMappedBankAccount");
        var bankFinancialAccount = await _db.Set<FinancialAccount>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == bankAccount.FinancialAccountId.Value && x.BusinessId == payment.BusinessId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (bankFinancialAccount is null || bankFinancialAccount.Type != AccountType.Asset) throw new InvalidOperationException("PayrollPaymentBankCorrectionRequiresAssetBankAccount");

        var accountResult = await _accounts.ResolveRequiredAccountsAsync(payment.BusinessId, RequiredRoles, ct).ConfigureAwait(false);
        if (!accountResult.Succeeded) throw new InvalidOperationException(accountResult.Error);

        var accounts = accountResult.Value!;
        var postingResult = await _posting.PostAsync(new FinancePostingCommand(
            payment.BusinessId,
            _clock.UtcNow,
            JournalEntryPostingKind.PayrollPaymentBankCorrection,
            $"{PostingKeyPrefix}:{correction.Id}",
            "PayrollPaymentBankCorrection",
            correction.Id,
            "Payroll payment bank correction",
            [
                new FinancePostingLineCommand(bankFinancialAccount.Id, correction.AmountMinor, 0, "Bank settlement correction"),
                new FinancePostingLineCommand(accounts[FinancePostingAccountRole.CashClearing], 0, correction.AmountMinor, "Cash clearing reinstatement")
            ],
            SourceDocumentNumber: payment.PaymentNumber ?? payment.Reference,
            PostingReason: correction.Reason,
            MetadataJson: $$"""{"payrollPaymentBankCorrectionId":"{{correction.Id}}","payrollPaymentId":"{{payment.Id}}","bankReconciliationMatchId":"{{reconciliation.Id}}","originalBankSettlementJournalEntryId":"{{correction.OriginalBankSettlementJournalEntryId}}","payrollRunId":"{{payment.PayrollRunId}}","currency":"{{correction.Currency}}","amountMinor":{{correction.AmountMinor}},"correctionType":"{{correction.CorrectionType}}"}"""), ct).ConfigureAwait(false);
        if (!postingResult.Succeeded) throw new InvalidOperationException(postingResult.Error);

        correction.CorrectionJournalEntryId = postingResult.Value!.JournalEntryId;
        correction.PostedAtUtc ??= _clock.UtcNow;
        correction.Status = PayrollPaymentBankCorrectionStatus.Posted;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await PayrollPaymentBankCorrectionSupport.RecordEvidenceAsync(_events, correction, "posted", AuditTrailAction.StatusChanged, _clock.UtcNow, ct).ConfigureAwait(false);
    }
}

public sealed class CancelPayrollPaymentBankCorrectionHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CancelPayrollPaymentBankCorrectionHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task HandleAsync(PayrollPaymentBankCorrectionActionDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var correction = await PayrollPaymentBankCorrectionSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        if (correction.Status != PayrollPaymentBankCorrectionStatus.Draft) throw new InvalidOperationException("PayrollPaymentBankCorrectionNotCancellable");
        correction.Status = PayrollPaymentBankCorrectionStatus.Cancelled;
        correction.CancelledAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await PayrollPaymentBankCorrectionSupport.RecordEvidenceAsync(_events, correction, "cancelled", AuditTrailAction.StatusChanged, _clock.UtcNow, ct).ConfigureAwait(false);
    }
}

internal static class PayrollPaymentBankCorrectionSupport
{
    public static void ValidateBankSettledPayment(PayrollPayment payment)
    {
        if (payment.Status != PayrollPaymentStatus.Posted) throw new InvalidOperationException("PayrollPaymentBankCorrectionRequiresPostedPayment");
        if (!payment.PostingJournalEntryId.HasValue) throw new InvalidOperationException("PayrollPaymentPostingRequired");
        if (!payment.BankSettledAtUtc.HasValue || !payment.BankSettlementJournalEntryId.HasValue || !payment.BankSettlementReconciliationMatchId.HasValue) throw new InvalidOperationException("PayrollPaymentBankCorrectionRequiresBankSettlement");
        if (payment.ReversalJournalEntryId.HasValue || payment.ReversedAtUtc.HasValue) throw new InvalidOperationException("PayrollPaymentReversed");
        PayrollPaymentSupport.RecalculateTotals(payment);
        if (payment.TotalAmountMinor <= 0) throw new InvalidOperationException("PayrollPaymentAmountRequired");
        if (payment.Allocations.Count == 0 || payment.Allocations.All(x => x.IsDeleted)) throw new InvalidOperationException("PayrollPaymentAllocationsRequired");
    }

    public static async Task<PayrollPaymentBankCorrection> LoadForUpdateAsync(IAppDbContext db, Guid id, byte[] rowVersion, CancellationToken ct)
    {
        if (id == Guid.Empty || rowVersion.Length == 0) throw new ArgumentException("PayrollPaymentBankCorrectionInvalidUpdate");
        var correction = await db.Set<PayrollPaymentBankCorrection>().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (correction is null) throw new InvalidOperationException("PayrollPaymentBankCorrectionNotFound");
        if (!HrCoreSupport.RowVersionMatches(correction.RowVersion, rowVersion)) throw new InvalidOperationException("ItemConcurrencyConflict");
        return correction;
    }

    public static async Task<BankReconciliationMatch> LoadAndValidateEvidenceAsync(IAppDbContext db, PayrollPayment payment, Guid reconciliationId, Guid? bankStatementLineId, CancellationToken ct)
    {
        var reconciliation = await db.Set<BankReconciliationMatch>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == reconciliationId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (reconciliation is null) throw new InvalidOperationException("BankReconciliationNotFound");
        if (reconciliation.Status != BankReconciliationMatchStatus.Matched) throw new InvalidOperationException("PayrollPaymentBankCorrectionRequiresMatchedReconciliation");
        if (payment.BankSettlementReconciliationMatchId.HasValue && reconciliation.Id == payment.BankSettlementReconciliationMatchId.Value) throw new InvalidOperationException("PayrollPaymentBankCorrectionRequiresSeparateEvidence");
        if (reconciliation.BusinessId != payment.BusinessId || !string.Equals(reconciliation.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("PayrollPaymentBankCorrectionReconciliationMismatch");
        if (reconciliation.DifferenceMinor != 0) throw new InvalidOperationException("PayrollPaymentBankCorrectionRequiresZeroDifference");
        if (bankStatementLineId.HasValue && !reconciliation.Lines.Any(x => !x.IsDeleted && x.IsActive && x.BankStatementLineId == bankStatementLineId.Value)) throw new InvalidOperationException("PayrollPaymentBankCorrectionStatementLineMismatch");

        var linkedAmount = reconciliation.Lines
            .Where(x =>
                !x.IsDeleted &&
                x.IsActive &&
                (x.JournalEntryId == payment.BankSettlementJournalEntryId ||
                 (string.Equals(x.SourceEntityType, "PayrollPayment", StringComparison.OrdinalIgnoreCase) && x.SourceEntityId == payment.Id)))
            .Sum(x => x.AmountMinor);
        if (linkedAmount != payment.TotalAmountMinor) throw new InvalidOperationException("PayrollPaymentBankCorrectionRequiresFullSettlementEvidence");
        return reconciliation;
    }

    public static async Task RecordEvidenceAsync(BusinessEventService? events, PayrollPaymentBankCorrection correction, string action, AuditTrailAction auditAction, DateTime now, CancellationToken ct)
    {
        if (events is null) return;
        var payload = $$"""{"payrollPaymentBankCorrectionId":"{{correction.Id}}","payrollPaymentId":"{{correction.PayrollPaymentId}}","businessId":"{{correction.BusinessId}}","bankReconciliationMatchId":"{{correction.BankReconciliationMatchId}}","status":"{{correction.Status}}","correctionType":"{{correction.CorrectionType}}","currency":"{{correction.Currency}}","amountMinor":{{correction.AmountMinor}},"correctionJournalEntryId":"{{correction.CorrectionJournalEntryId}}"}""";
        var eventResult = await events.AddEventAsync(new AddBusinessEventCommand(correction.BusinessId, "PayrollPaymentBankCorrection", correction.Id, $"hr.payroll_payment_bank_correction.{action}", $"hr.payroll_payment_bank_correction.{action}:{correction.Id}:{correction.Status}", now, null, BusinessEventSource.User, BusinessEventSeverity.Info, FoundationVisibility.Internal, $"Payroll payment bank correction {action}", null, null, null, payload), ct).ConfigureAwait(false);
        if (!eventResult.Succeeded) throw new InvalidOperationException(eventResult.Error);
        var auditResult = await events.AddAuditTrailAsync(new AddAuditTrailCommand(correction.BusinessId, "PayrollPaymentBankCorrection", correction.Id, auditAction, now, null, eventResult.Value, $"Payroll payment bank correction {action}", null, payload), ct).ConfigureAwait(false);
        if (!auditResult.Succeeded) throw new InvalidOperationException(auditResult.Error);
    }
}

internal static class PayrollPaymentSupport
{
    public static void ValidateCreate(PayrollPaymentCreateDto dto)
    {
        if (dto.BusinessId == Guid.Empty || dto.PayrollRunId == Guid.Empty) throw new ArgumentException("PayrollPaymentInvalidIds");
        dto.Currency = NormalizeCurrency(dto.Currency);
        dto.Reference = HrCoreSupport.Optional(dto.Reference);
        dto.InternalNotes = HrCoreSupport.Optional(dto.InternalNotes);
        dto.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
        HrCoreSupport.EnsureSafe(dto.Reference, dto.InternalNotes, dto.MetadataJson);
        if (dto.Allocations is null || dto.Allocations.Count == 0) throw new ArgumentException("PayrollPaymentAllocationsRequired");
    }

    public static void ValidateUpdate(PayrollPaymentEditDto dto)
    {
        ValidateCreate(dto);
        if (dto.Id == Guid.Empty || dto.RowVersion.Length == 0) throw new ArgumentException("PayrollPaymentInvalidIds");
    }

    public static async Task<PayrollPayment> LoadForUpdateAsync(IAppDbContext db, Guid id, byte[] rowVersion, CancellationToken ct)
    {
        if (id == Guid.Empty || rowVersion.Length == 0) throw new InvalidOperationException("PayrollPaymentNotFound");
        var payment = await db.Set<PayrollPayment>()
            .Include(x => x.Allocations)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (payment is null) throw new InvalidOperationException("PayrollPaymentNotFound");
        if (!HrCoreSupport.RowVersionMatches(payment.RowVersion, rowVersion)) throw new InvalidOperationException("ItemConcurrencyConflict");
        return payment;
    }

    public static async Task<PayrollRun> LoadPostedRunAsync(IAppDbContext db, Guid businessId, Guid payrollRunId, CancellationToken ct)
    {
        var run = await db.Set<PayrollRun>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == payrollRunId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (run is null) throw new InvalidOperationException("PayrollRunNotFound");
        if (run.BusinessId != businessId) throw new InvalidOperationException("PayrollPaymentCrossBusiness");
        if (run.Status != PayrollRunStatus.Posted || !run.PostingJournalEntryId.HasValue) throw new InvalidOperationException("PayrollPaymentRequiresPostedRun");
        return run;
    }

    public static async Task<List<PayrollPaymentAllocation>> MapAndValidateAllocationsAsync(IAppDbContext db, PayrollPaymentCreateDto dto, PayrollRun run, Guid? excludingPaymentId, CancellationToken ct)
    {
        var grouped = dto.Allocations
            .Where(x => x.PayrollRunLineId != Guid.Empty)
            .GroupBy(x => x.PayrollRunLineId)
            .Select(group => new PayrollPaymentAllocationDto
            {
                PayrollRunLineId = group.Key,
                AmountMinor = group.Sum(x => x.AmountMinor),
                Memo = HrCoreSupport.Optional(group.Select(x => x.Memo).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)))
            })
            .Where(x => x.AmountMinor > 0)
            .ToList();
        if (grouped.Count == 0) throw new ArgumentException("PayrollPaymentAllocationsRequired");

        var lineIds = grouped.Select(x => x.PayrollRunLineId).ToArray();
        var lines = run.Lines.Where(x => lineIds.Contains(x.Id) && !x.IsDeleted).ToDictionary(x => x.Id);
        if (lines.Count != lineIds.Distinct().Count()) throw new InvalidOperationException("PayrollPaymentLineNotFound");
        var paid = await PayrollPaymentQuerySupport.GetPostedPaidByLineAsync(db, lineIds, excludingPaymentId, ct).ConfigureAwait(false);

        foreach (var allocation in grouped)
        {
            HrCoreSupport.EnsureSafe(allocation.Memo);
            var line = lines[allocation.PayrollRunLineId];
            if (line.BusinessId != dto.BusinessId || line.PayrollRunId != dto.PayrollRunId) throw new InvalidOperationException("PayrollPaymentLineMismatch");
            if (allocation.AmountMinor <= 0) throw new ArgumentException("PayrollPaymentAmountRequired");
            var alreadyPaid = paid.GetValueOrDefault(line.Id);
            if (alreadyPaid + allocation.AmountMinor > line.NetPayMinor) throw new InvalidOperationException("PayrollPaymentExceedsOpenPayable");
            allocation.EmployeeId = line.EmployeeId;
        }

        return grouped.Select(allocation =>
        {
            var line = lines[allocation.PayrollRunLineId];
            return new PayrollPaymentAllocation
            {
                BusinessId = dto.BusinessId,
                PayrollRunId = dto.PayrollRunId,
                PayrollRunLineId = line.Id,
                EmployeeId = line.EmployeeId,
                AmountMinor = allocation.AmountMinor,
                Memo = allocation.Memo
            };
        }).ToList();
    }

    public static async Task ValidateAllocationsAgainstOpenPayableAsync(IAppDbContext db, PayrollPayment payment, Guid? excludingPaymentId, CancellationToken ct)
    {
        if (payment.Allocations.Count == 0 || payment.Allocations.All(x => x.IsDeleted)) throw new InvalidOperationException("PayrollPaymentAllocationsRequired");
        var dto = new PayrollPaymentCreateDto
        {
            BusinessId = payment.BusinessId,
            PayrollRunId = payment.PayrollRunId,
            PaymentMethod = payment.PaymentMethod,
            PaymentDateUtc = payment.PaymentDateUtc,
            Currency = payment.Currency,
            Reference = payment.Reference,
            InternalNotes = payment.InternalNotes,
            MetadataJson = payment.MetadataJson,
            Allocations = payment.Allocations.Where(x => !x.IsDeleted).Select(x => new PayrollPaymentAllocationDto
            {
                PayrollRunLineId = x.PayrollRunLineId,
                AmountMinor = x.AmountMinor,
                Memo = x.Memo
            }).ToList()
        };
        var run = await LoadPostedRunAsync(db, payment.BusinessId, payment.PayrollRunId, ct).ConfigureAwait(false);
        await MapAndValidateAllocationsAsync(db, dto, run, excludingPaymentId, ct).ConfigureAwait(false);
    }

    public static void RecalculateTotals(PayrollPayment payment)
        => payment.TotalAmountMinor = payment.Allocations.Where(x => !x.IsDeleted).Sum(x => x.AmountMinor);

    public static string NormalizeCurrency(string? currency)
    {
        var value = (currency ?? string.Empty).Trim().ToUpperInvariant();
        if (value.Length != 3) throw new ArgumentException("PayrollPaymentCurrencyInvalid");
        return value;
    }

    public static DateTime EnsureUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public static void ThrowIfFailed(Result result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(result.Error);
    }

    public static async Task RecordEvidenceAsync(IAppDbContext db, BusinessEventService? events, IClock clock, PayrollPayment payment, string action, AuditTrailAction auditAction, CancellationToken ct)
        => await HrCoreSupport.RecordEvidenceOrSaveAsync(db, events, clock, payment.BusinessId, "PayrollPayment", payment.Id, $"hr.payroll_payment.{action}", auditAction, ct).ConfigureAwait(false);
}
