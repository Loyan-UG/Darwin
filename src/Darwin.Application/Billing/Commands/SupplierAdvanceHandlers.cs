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

public sealed class SupplierAdvanceWorkflowPolicy
{
    public Result CanUpdate(SupplierAdvance advance) => advance.Status == SupplierAdvanceStatus.Draft
        ? Result.Ok()
        : Result.Fail("SupplierAdvanceLifecycleUnsupportedAction");

    public Result CanPost(SupplierAdvance advance) => advance.Status == SupplierAdvanceStatus.Draft
        ? Result.Ok()
        : Result.Fail("SupplierAdvanceLifecycleUnsupportedAction");

    public Result CanCancel(SupplierAdvance advance) => advance.Status == SupplierAdvanceStatus.Draft
        ? Result.Ok()
        : Result.Fail("SupplierAdvanceLifecycleUnsupportedAction");

    public Result CanReverse(SupplierAdvance advance)
        => advance.Status == SupplierAdvanceStatus.Posted
            ? Result.Ok()
            : Result.Fail("SupplierAdvanceLifecycleUnsupportedAction");

    public Result CanApply(SupplierAdvance advance)
        => advance.Status == SupplierAdvanceStatus.Posted && advance.OpenAmountMinor > 0
            ? Result.Ok()
            : Result.Fail("SupplierAdvanceLifecycleUnsupportedAction");
}

public sealed class CreateSupplierAdvanceHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreateSupplierAdvanceHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Guid> HandleAsync(SupplierAdvanceCreateDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        SupplierAdvanceSupport.ValidateCreate(dto);
        await SupplierPaymentSupport.ValidateSupplierAsync(_db, dto.BusinessId, dto.SupplierId, ct).ConfigureAwait(false);

        var advance = new SupplierAdvance
        {
            BusinessId = dto.BusinessId,
            SupplierId = dto.SupplierId,
            PaymentMethod = dto.PaymentMethod,
            AdvanceDateUtc = SupplierInvoiceSupport.EnsureUtc(dto.AdvanceDateUtc == default ? _clock.UtcNow : dto.AdvanceDateUtc),
            Currency = SupplierInvoiceSupport.NormalizeCurrency(dto.Currency),
            TotalAmountMinor = dto.TotalAmountMinor,
            OpenAmountMinor = dto.TotalAmountMinor,
            Reference = SupplierInvoiceSupport.Optional(dto.Reference, 256),
            InternalNotes = SupplierInvoiceSupport.Optional(dto.InternalNotes, 4000),
            MetadataJson = SupplierInvoiceSupport.NormalizeMetadata(dto.MetadataJson)
        };
        _db.Set<SupplierAdvance>().Add(advance);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await SupplierAdvanceSupport.RecordEvidenceAsync(_events, advance, "created", AuditTrailAction.Created, _clock.UtcNow, ct).ConfigureAwait(false);
        return advance.Id;
    }
}

public sealed class UpdateSupplierAdvanceHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly SupplierAdvanceWorkflowPolicy _workflow;
    private readonly BusinessEventService? _events;

    public UpdateSupplierAdvanceHandler(IAppDbContext db, IClock clock, SupplierAdvanceWorkflowPolicy workflow, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        _events = events;
    }

    public async Task HandleAsync(SupplierAdvanceEditDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        SupplierAdvanceSupport.ValidateUpdate(dto);
        var advance = await SupplierAdvanceSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        SupplierInvoiceSupport.ThrowIfFailed(_workflow.CanUpdate(advance));
        await SupplierPaymentSupport.ValidateSupplierAsync(_db, dto.BusinessId, dto.SupplierId, ct).ConfigureAwait(false);

        advance.BusinessId = dto.BusinessId;
        advance.SupplierId = dto.SupplierId;
        advance.PaymentMethod = dto.PaymentMethod;
        advance.AdvanceDateUtc = SupplierInvoiceSupport.EnsureUtc(dto.AdvanceDateUtc);
        advance.Currency = SupplierInvoiceSupport.NormalizeCurrency(dto.Currency);
        advance.TotalAmountMinor = dto.TotalAmountMinor;
        advance.OpenAmountMinor = dto.TotalAmountMinor;
        advance.Reference = SupplierInvoiceSupport.Optional(dto.Reference, 256);
        advance.InternalNotes = SupplierInvoiceSupport.Optional(dto.InternalNotes, 4000);
        advance.MetadataJson = SupplierInvoiceSupport.NormalizeMetadata(dto.MetadataJson);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await SupplierAdvanceSupport.RecordEvidenceAsync(_events, advance, "updated", AuditTrailAction.Updated, _clock.UtcNow, ct).ConfigureAwait(false);
    }
}

public sealed class PostSupplierAdvanceHandler
{
    public const string PostingKeyPrefix = "supplier-advance-posted";

    private static readonly FinancePostingAccountRole[] RequiredRoles =
    [
        FinancePostingAccountRole.SupplierAdvance,
        FinancePostingAccountRole.CashClearing
    ];

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly NumberSequenceService _numbers;
    private readonly SupplierAdvanceWorkflowPolicy _workflow;
    private readonly FinanceAccountMappingService _accounts;
    private readonly FinancePostingService _posting;
    private readonly BusinessEventService? _events;

    public PostSupplierAdvanceHandler(
        IAppDbContext db,
        IClock clock,
        NumberSequenceService numbers,
        SupplierAdvanceWorkflowPolicy workflow,
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

    public async Task HandleAsync(SupplierAdvanceLifecycleActionDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var advance = await SupplierAdvanceSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        SupplierInvoiceSupport.ThrowIfFailed(_workflow.CanPost(advance));
        SupplierAdvanceSupport.ValidatePostedAmount(advance);

        var accountResult = await _accounts.ResolveRequiredAccountsAsync(advance.BusinessId, RequiredRoles, ct).ConfigureAwait(false);
        if (!accountResult.Succeeded) throw new InvalidOperationException(accountResult.Error);

        if (string.IsNullOrWhiteSpace(advance.AdvanceNumber))
        {
            var number = await _numbers.ReserveNextAsync(new NumberSequenceRequest(advance.BusinessId, NumberSequenceDocumentType.SupplierAdvance, NumberSequenceService.GlobalScopeKey), ct).ConfigureAwait(false);
            if (number.Succeeded && !string.IsNullOrWhiteSpace(number.Value))
            {
                advance.AdvanceNumber = number.Value;
            }
        }

        var accounts = accountResult.Value!;
        var postingResult = await _posting.PostAsync(new FinancePostingCommand(
            advance.BusinessId,
            advance.AdvanceDateUtc,
            JournalEntryPostingKind.SupplierAdvancePosted,
            $"{PostingKeyPrefix}:{advance.Id}",
            "SupplierAdvance",
            advance.Id,
            "Supplier advance posted",
            [
                new FinancePostingLineCommand(accounts[FinancePostingAccountRole.SupplierAdvance], advance.TotalAmountMinor, 0, "Supplier advance asset"),
                new FinancePostingLineCommand(accounts[FinancePostingAccountRole.CashClearing], 0, advance.TotalAmountMinor, "Cash clearing")
            ],
            SourceDocumentNumber: advance.AdvanceNumber ?? advance.Reference,
            PostingReason: "Supplier advance asset recognition",
            MetadataJson: $$"""{"supplierAdvanceId":"{{advance.Id}}","supplierId":"{{advance.SupplierId}}","currency":"{{advance.Currency}}","totalAmountMinor":{{advance.TotalAmountMinor}}}"""), ct).ConfigureAwait(false);
        if (!postingResult.Succeeded) throw new InvalidOperationException(postingResult.Error);

        advance.PostingJournalEntryId = postingResult.Value!.JournalEntryId;
        advance.PostedAtUtc ??= _clock.UtcNow;
        advance.OpenAmountMinor = advance.TotalAmountMinor;
        advance.Status = SupplierAdvanceStatus.Posted;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await SupplierAdvanceSupport.RecordEvidenceAsync(_events, advance, "posted", AuditTrailAction.StatusChanged, _clock.UtcNow, ct).ConfigureAwait(false);
    }
}

public sealed class CancelSupplierAdvanceHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly SupplierAdvanceWorkflowPolicy _workflow;
    private readonly BusinessEventService? _events;

    public CancelSupplierAdvanceHandler(IAppDbContext db, IClock clock, SupplierAdvanceWorkflowPolicy workflow, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        _events = events;
    }

    public async Task HandleAsync(SupplierAdvanceLifecycleActionDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var advance = await SupplierAdvanceSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        SupplierInvoiceSupport.ThrowIfFailed(_workflow.CanCancel(advance));
        advance.Status = SupplierAdvanceStatus.Cancelled;
        advance.CancelledAtUtc ??= _clock.UtcNow;
        advance.OpenAmountMinor = 0;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await SupplierAdvanceSupport.RecordEvidenceAsync(_events, advance, "cancelled", AuditTrailAction.StatusChanged, _clock.UtcNow, ct).ConfigureAwait(false);
    }
}

public sealed class ReverseSupplierAdvanceHandler
{
    public const string PostingKeyPrefix = "supplier-advance-reversed";

    private static readonly FinancePostingAccountRole[] RequiredRoles =
    [
        FinancePostingAccountRole.SupplierAdvance,
        FinancePostingAccountRole.CashClearing
    ];

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly SupplierAdvanceWorkflowPolicy _workflow;
    private readonly FinanceAccountMappingService _accounts;
    private readonly FinancePostingService _posting;
    private readonly BusinessEventService? _events;

    public ReverseSupplierAdvanceHandler(
        IAppDbContext db,
        IClock clock,
        SupplierAdvanceWorkflowPolicy workflow,
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

    public async Task HandleAsync(SupplierAdvanceLifecycleActionDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var reason = SupplierInvoiceSupport.Optional(dto.Reason, 1000);
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("SupplierAdvanceReversalReasonRequired");
        if (FoundationInputNormalizer.LooksSensitive(reason)) throw new ArgumentException("SupplierAdvanceReversalSensitiveReasonRejected");

        var advance = await SupplierAdvanceSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        SupplierInvoiceSupport.ThrowIfFailed(_workflow.CanReverse(advance));
        SupplierAdvanceSupport.RecalculateOpenAmount(advance);
        if (!advance.PostingJournalEntryId.HasValue) throw new InvalidOperationException("SupplierAdvancePostingRequired");
        if (advance.ReversalJournalEntryId.HasValue || advance.ReversedAtUtc.HasValue) throw new InvalidOperationException("SupplierAdvanceAlreadyReversed");
        if (advance.Applications.Any(x => !x.IsDeleted && !x.ReversedAtUtc.HasValue)) throw new InvalidOperationException("SupplierAdvanceReversalRequiresUnappliedAdvance");
        if (advance.OpenAmountMinor != advance.TotalAmountMinor) throw new InvalidOperationException("SupplierAdvanceReversalRequiresFullOpenAdvance");
        SupplierAdvanceSupport.ValidatePostedAmount(advance);
        await SupplierPaymentSupport.ValidateSupplierAsync(_db, advance.BusinessId, advance.SupplierId, ct).ConfigureAwait(false);

        var accountResult = await _accounts.ResolveRequiredAccountsAsync(advance.BusinessId, RequiredRoles, ct).ConfigureAwait(false);
        if (!accountResult.Succeeded) throw new InvalidOperationException(accountResult.Error);

        var accounts = accountResult.Value!;
        var postingResult = await _posting.PostAsync(new FinancePostingCommand(
            advance.BusinessId,
            _clock.UtcNow,
            JournalEntryPostingKind.Reversal,
            $"{PostingKeyPrefix}:{advance.Id}",
            "SupplierAdvance",
            advance.Id,
            "Supplier advance reversed",
            [
                new FinancePostingLineCommand(accounts[FinancePostingAccountRole.CashClearing], advance.TotalAmountMinor, 0, "Cash clearing reversal"),
                new FinancePostingLineCommand(accounts[FinancePostingAccountRole.SupplierAdvance], 0, advance.TotalAmountMinor, "Supplier advance asset reversal")
            ],
            SourceDocumentNumber: advance.AdvanceNumber ?? advance.Reference,
            PostingReason: reason,
            MetadataJson: $$"""{"supplierAdvanceId":"{{advance.Id}}","originalJournalEntryId":"{{advance.PostingJournalEntryId}}","supplierId":"{{advance.SupplierId}}","currency":"{{advance.Currency}}","totalAmountMinor":{{advance.TotalAmountMinor}}}"""), ct).ConfigureAwait(false);
        if (!postingResult.Succeeded) throw new InvalidOperationException(postingResult.Error);

        advance.ReversalJournalEntryId = postingResult.Value!.JournalEntryId;
        advance.ReversedAtUtc ??= _clock.UtcNow;
        advance.ReversalReason = reason;
        advance.OpenAmountMinor = 0;
        advance.Status = SupplierAdvanceStatus.Reversed;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await SupplierAdvanceSupport.RecordEvidenceAsync(_events, advance, "reversed", AuditTrailAction.StatusChanged, _clock.UtcNow, ct).ConfigureAwait(false);
    }
}

public sealed class ApplySupplierAdvanceHandler
{
    public const string PostingKeyPrefix = "supplier-advance-applied";

    private static readonly FinancePostingAccountRole[] RequiredRoles =
    [
        FinancePostingAccountRole.AccountsPayable,
        FinancePostingAccountRole.SupplierAdvance
    ];

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly SupplierAdvanceWorkflowPolicy _workflow;
    private readonly FinanceAccountMappingService _accounts;
    private readonly FinancePostingService _posting;
    private readonly BusinessEventService? _events;

    public ApplySupplierAdvanceHandler(
        IAppDbContext db,
        IClock clock,
        SupplierAdvanceWorkflowPolicy workflow,
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

    public async Task HandleAsync(SupplierAdvanceApplyDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Id == Guid.Empty || dto.RowVersion.Length == 0 || dto.SupplierInvoiceId == Guid.Empty) throw new ArgumentException("SupplierAdvanceInvalidApplication");
        if (dto.AmountMinor <= 0) throw new ArgumentException("SupplierAdvanceApplicationAmountRequired");
        var memo = SupplierInvoiceSupport.Optional(dto.Memo, 1000);
        if (FoundationInputNormalizer.LooksSensitive(memo)) throw new ArgumentException("SupplierAdvanceApplicationSensitiveMemoRejected");

        var advance = await SupplierAdvanceSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        SupplierAdvanceSupport.RecalculateOpenAmount(advance);
        SupplierInvoiceSupport.ThrowIfFailed(_workflow.CanApply(advance));

        var invoice = await _db.Set<SupplierInvoice>()
            .FirstOrDefaultAsync(x => x.Id == dto.SupplierInvoiceId && !x.IsDeleted, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("SupplierInvoiceNotFound");
        SupplierAdvanceSupport.ValidateInvoiceForApplication(advance, invoice);
        var existingSamePair = await _db.Set<SupplierAdvanceApplication>()
            .AsNoTracking()
            .AnyAsync(x => x.SupplierAdvanceId == advance.Id && x.SupplierInvoiceId == invoice.Id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (existingSamePair) throw new InvalidOperationException("SupplierAdvanceApplicationAlreadyExists");

        var openInvoice = await SupplierAdvanceSupport.GetOpenPayableMinorAsync(_db, invoice, null, ct).ConfigureAwait(false);
        if (dto.AmountMinor > advance.OpenAmountMinor) throw new InvalidOperationException("SupplierAdvanceApplicationExceedsOpenAdvance");
        if (dto.AmountMinor > openInvoice) throw new InvalidOperationException("SupplierAdvanceApplicationExceedsOpenInvoice");

        var accountResult = await _accounts.ResolveRequiredAccountsAsync(advance.BusinessId, RequiredRoles, ct).ConfigureAwait(false);
        if (!accountResult.Succeeded) throw new InvalidOperationException(accountResult.Error);

        var application = new SupplierAdvanceApplication
        {
            SupplierAdvanceId = advance.Id,
            SupplierInvoiceId = invoice.Id,
            AmountMinor = dto.AmountMinor,
            Memo = memo,
            AppliedAtUtc = _clock.UtcNow
        };
        _db.Set<SupplierAdvanceApplication>().Add(application);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        var accounts = accountResult.Value!;
        var postingResult = await _posting.PostAsync(new FinancePostingCommand(
            advance.BusinessId,
            _clock.UtcNow,
            JournalEntryPostingKind.SupplierAdvanceApplied,
            $"{PostingKeyPrefix}:{advance.Id}:{invoice.Id}",
            "SupplierAdvanceApplication",
            application.Id,
            "Supplier advance applied",
            [
                new FinancePostingLineCommand(accounts[FinancePostingAccountRole.AccountsPayable], application.AmountMinor, 0, "Accounts payable cleared by advance"),
                new FinancePostingLineCommand(accounts[FinancePostingAccountRole.SupplierAdvance], 0, application.AmountMinor, "Supplier advance applied")
            ],
            SourceDocumentNumber: advance.AdvanceNumber ?? invoice.InternalInvoiceNumber ?? invoice.SupplierInvoiceNumber,
            PostingReason: "Supplier advance application",
            MetadataJson: $$"""{"supplierAdvanceId":"{{advance.Id}}","supplierAdvanceApplicationId":"{{application.Id}}","supplierInvoiceId":"{{invoice.Id}}","supplierId":"{{advance.SupplierId}}","currency":"{{advance.Currency}}","amountMinor":{{application.AmountMinor}}}"""), ct).ConfigureAwait(false);
        if (!postingResult.Succeeded) throw new InvalidOperationException(postingResult.Error);

        application.PostingJournalEntryId = postingResult.Value!.JournalEntryId;
        SupplierAdvanceSupport.RecalculateOpenAmount(advance);
        advance.Status = advance.OpenAmountMinor == 0 ? SupplierAdvanceStatus.Applied : SupplierAdvanceStatus.Posted;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await SupplierAdvanceSupport.RecordEvidenceAsync(_events, advance, "applied", AuditTrailAction.Linked, _clock.UtcNow, ct, application, invoice.Id).ConfigureAwait(false);
    }
}

public sealed class ReverseSupplierAdvanceApplicationHandler
{
    public const string PostingKeyPrefix = "supplier-advance-application-reversed";

    private static readonly FinancePostingAccountRole[] RequiredRoles =
    [
        FinancePostingAccountRole.AccountsPayable,
        FinancePostingAccountRole.SupplierAdvance
    ];

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly FinanceAccountMappingService _accounts;
    private readonly FinancePostingService _posting;
    private readonly BusinessEventService? _events;

    public ReverseSupplierAdvanceApplicationHandler(
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

    public async Task HandleAsync(SupplierAdvanceApplicationReverseDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Id == Guid.Empty || dto.RowVersion.Length == 0 || dto.ApplicationId == Guid.Empty) throw new ArgumentException("SupplierAdvanceApplicationReversalInvalidRequest");
        var reason = SupplierInvoiceSupport.Optional(dto.Reason, 1000);
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("SupplierAdvanceApplicationReversalReasonRequired");
        if (FoundationInputNormalizer.LooksSensitive(reason)) throw new ArgumentException("SupplierAdvanceApplicationReversalSensitiveReasonRejected");

        var advance = await SupplierAdvanceSupport.LoadForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        if (advance.Status is SupplierAdvanceStatus.Draft or SupplierAdvanceStatus.Cancelled or SupplierAdvanceStatus.Reversed) throw new InvalidOperationException("SupplierAdvanceApplicationReversalUnsupportedStatus");
        var application = advance.Applications.FirstOrDefault(x => x.Id == dto.ApplicationId && !x.IsDeleted)
            ?? throw new InvalidOperationException("SupplierAdvanceApplicationNotFound");
        if (!application.PostingJournalEntryId.HasValue) throw new InvalidOperationException("SupplierAdvanceApplicationPostingRequired");
        if (application.ReversalJournalEntryId.HasValue || application.ReversedAtUtc.HasValue) throw new InvalidOperationException("SupplierAdvanceApplicationAlreadyReversed");
        if (application.AmountMinor <= 0) throw new InvalidOperationException("SupplierAdvanceApplicationAmountRequired");

        var invoice = await _db.Set<SupplierInvoice>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == application.SupplierInvoiceId && !x.IsDeleted, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("SupplierInvoiceNotFound");
        SupplierAdvanceSupport.ValidateInvoiceForApplication(advance, invoice);

        var accountResult = await _accounts.ResolveRequiredAccountsAsync(advance.BusinessId, RequiredRoles, ct).ConfigureAwait(false);
        if (!accountResult.Succeeded) throw new InvalidOperationException(accountResult.Error);

        var accounts = accountResult.Value!;
        var postingResult = await _posting.PostAsync(new FinancePostingCommand(
            advance.BusinessId,
            _clock.UtcNow,
            JournalEntryPostingKind.Reversal,
            $"{PostingKeyPrefix}:{application.Id}",
            "SupplierAdvanceApplication",
            application.Id,
            "Supplier advance application reversed",
            [
                new FinancePostingLineCommand(accounts[FinancePostingAccountRole.SupplierAdvance], application.AmountMinor, 0, "Supplier advance asset reinstatement"),
                new FinancePostingLineCommand(accounts[FinancePostingAccountRole.AccountsPayable], 0, application.AmountMinor, "Accounts payable reinstatement")
            ],
            SourceDocumentNumber: advance.AdvanceNumber ?? invoice.InternalInvoiceNumber ?? invoice.SupplierInvoiceNumber,
            PostingReason: reason,
            MetadataJson: $$"""{"supplierAdvanceId":"{{advance.Id}}","supplierAdvanceApplicationId":"{{application.Id}}","supplierInvoiceId":"{{invoice.Id}}","originalJournalEntryId":"{{application.PostingJournalEntryId}}","supplierId":"{{advance.SupplierId}}","currency":"{{advance.Currency}}","amountMinor":{{application.AmountMinor}}}"""), ct).ConfigureAwait(false);
        if (!postingResult.Succeeded) throw new InvalidOperationException(postingResult.Error);

        application.ReversalJournalEntryId = postingResult.Value!.JournalEntryId;
        application.ReversedAtUtc ??= _clock.UtcNow;
        application.ReversalReason = reason;
        SupplierAdvanceSupport.RecalculateOpenAmount(advance);
        advance.Status = advance.OpenAmountMinor == 0 ? SupplierAdvanceStatus.Applied : SupplierAdvanceStatus.Posted;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await SupplierAdvanceSupport.RecordEvidenceAsync(_events, advance, "application_reversed", AuditTrailAction.StatusChanged, _clock.UtcNow, ct, application, invoice.Id).ConfigureAwait(false);
    }
}

internal static class SupplierAdvanceSupport
{
    public static void ValidateCreate(SupplierAdvanceCreateDto dto)
    {
        if (dto.BusinessId == Guid.Empty || dto.SupplierId == Guid.Empty) throw new ArgumentException("SupplierAdvanceInvalidLink");
        if (!Enum.IsDefined(typeof(SupplierPaymentMethod), dto.PaymentMethod)) throw new ArgumentException("SupplierAdvanceInvalidMethod");
        if (dto.TotalAmountMinor <= 0) throw new ArgumentException("SupplierAdvanceAmountRequired");
        _ = SupplierInvoiceSupport.NormalizeCurrency(dto.Currency);
        if (FoundationInputNormalizer.LooksSensitive(dto.Reference) ||
            FoundationInputNormalizer.LooksSensitive(dto.InternalNotes) ||
            FoundationInputNormalizer.LooksSensitive(dto.MetadataJson))
        {
            throw new ArgumentException("SupplierAdvanceSensitiveMetadataRejected");
        }
    }

    public static void ValidateUpdate(SupplierAdvanceEditDto dto)
    {
        if (dto.Id == Guid.Empty || dto.RowVersion.Length == 0) throw new ArgumentException("SupplierAdvanceInvalidUpdate");
        ValidateCreate(dto);
    }

    public static async Task<SupplierAdvance> LoadForUpdateAsync(IAppDbContext db, Guid id, byte[] rowVersion, CancellationToken ct)
    {
        if (id == Guid.Empty || rowVersion.Length == 0) throw new ArgumentException("SupplierAdvanceInvalidUpdate");
        var advance = await db.Set<SupplierAdvance>()
            .Include(x => x.Applications)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("SupplierAdvanceNotFound");
        if (!(advance.RowVersion ?? Array.Empty<byte>()).SequenceEqual(rowVersion)) throw new InvalidOperationException("ItemConcurrencyConflict");
        return advance;
    }

    public static void ValidatePostedAmount(SupplierAdvance advance)
    {
        if (advance.TotalAmountMinor <= 0) throw new InvalidOperationException("SupplierAdvanceAmountRequired");
        if (!string.Equals(advance.Currency, SupplierInvoiceSupport.NormalizeCurrency(advance.Currency), StringComparison.Ordinal)) throw new InvalidOperationException("SupplierAdvanceInvalidCurrency");
    }

    public static void RecalculateOpenAmount(SupplierAdvance advance)
    {
        var applied = advance.Applications.Where(x => !x.IsDeleted && !x.ReversedAtUtc.HasValue).Sum(x => x.AmountMinor);
        advance.OpenAmountMinor = Math.Max(0, advance.TotalAmountMinor - applied);
    }

    public static void ValidateInvoiceForApplication(SupplierAdvance advance, SupplierInvoice invoice)
    {
        if (invoice.BusinessId != advance.BusinessId || invoice.SupplierId != advance.SupplierId) throw new InvalidOperationException("SupplierAdvanceApplicationInvoiceMismatch");
        if (invoice.Status != SupplierInvoiceStatus.Posted || !invoice.PostingJournalEntryId.HasValue) throw new InvalidOperationException("SupplierAdvanceApplicationRequiresPostedInvoice");
        if (!string.Equals(invoice.Currency, advance.Currency, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("SupplierAdvanceApplicationCurrencyMismatch");
    }

    public static async Task<long> GetOpenPayableMinorAsync(IAppDbContext db, SupplierInvoice invoice, Guid? excludingApplicationId, CancellationToken ct)
    {
        var paid = await SupplierPaymentQuerySupport.GetPostedPaidByInvoiceAsync(db, [invoice.Id], null, ct).ConfigureAwait(false);
        var applied = await GetAppliedByInvoiceAsync(db, [invoice.Id], excludingApplicationId, ct).ConfigureAwait(false);
        return Math.Max(0, invoice.TotalGrossMinor - paid.GetValueOrDefault(invoice.Id) - applied.GetValueOrDefault(invoice.Id));
    }

    public static async Task<Dictionary<Guid, long>> GetAppliedByInvoiceAsync(IAppDbContext db, IReadOnlyCollection<Guid> invoiceIds, Guid? excludingApplicationId, CancellationToken ct)
    {
        if (invoiceIds.Count == 0) return new Dictionary<Guid, long>();
        return await db.Set<SupplierAdvanceApplication>()
            .AsNoTracking()
            .Where(application =>
                invoiceIds.Contains(application.SupplierInvoiceId) &&
                !application.IsDeleted &&
                !application.ReversedAtUtc.HasValue &&
                (!excludingApplicationId.HasValue || application.Id != excludingApplicationId.Value) &&
                db.Set<SupplierAdvance>().Any(advance =>
                    advance.Id == application.SupplierAdvanceId &&
                    !advance.IsDeleted &&
                    (advance.Status == SupplierAdvanceStatus.Posted || advance.Status == SupplierAdvanceStatus.Applied)))
            .GroupBy(x => x.SupplierInvoiceId)
            .Select(x => new { SupplierInvoiceId = x.Key, AmountMinor = x.Sum(a => a.AmountMinor) })
            .ToDictionaryAsync(x => x.SupplierInvoiceId, x => x.AmountMinor, ct)
            .ConfigureAwait(false);
    }

    public static async Task RecordEvidenceAsync(
        BusinessEventService? events,
        SupplierAdvance advance,
        string action,
        AuditTrailAction auditAction,
        DateTime now,
        CancellationToken ct,
        SupplierAdvanceApplication? application = null,
        Guid? supplierInvoiceId = null)
    {
        if (events is null) return;
        var payload = $$"""{"supplierAdvanceId":"{{advance.Id}}","supplierId":"{{advance.SupplierId}}","businessId":"{{advance.BusinessId}}","status":"{{advance.Status}}","currency":"{{advance.Currency}}","totalAmountMinor":{{advance.TotalAmountMinor}},"openAmountMinor":{{advance.OpenAmountMinor}},"postingJournalEntryId":"{{advance.PostingJournalEntryId}}","supplierAdvanceApplicationId":"{{application?.Id}}","supplierInvoiceId":"{{supplierInvoiceId}}","applicationAmountMinor":{{application?.AmountMinor ?? 0}}}""";
        var eventResult = await events.AddEventAsync(new AddBusinessEventCommand(advance.BusinessId, "SupplierAdvance", advance.Id, $"payables.supplier_advance.{action}", $"payables.supplier_advance.{action}:{advance.Id}:{advance.Status}", now, null, BusinessEventSource.User, BusinessEventSeverity.Info, FoundationVisibility.Internal, $"Supplier advance {action}", null, null, null, payload), ct).ConfigureAwait(false);
        if (!eventResult.Succeeded) throw new InvalidOperationException(eventResult.Error);
        var auditResult = await events.AddAuditTrailAsync(new AddAuditTrailCommand(advance.BusinessId, "SupplierAdvance", advance.Id, auditAction, now, null, eventResult.Value, $"Supplier advance {action}", null, payload), ct).ConfigureAwait(false);
        if (!auditResult.Succeeded) throw new InvalidOperationException(auditResult.Error);
    }
}
