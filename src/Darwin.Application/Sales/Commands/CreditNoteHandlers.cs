using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Sales.DTOs;
using Darwin.Application.Sales.Services;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Sales.Commands;

public sealed class CreateCreditNoteHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<CreditNoteCreateDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly CreditNoteLifecycleEventService _events;
    private readonly IClock _clock;

    public CreateCreditNoteHandler(
        IAppDbContext db,
        IValidator<CreditNoteCreateDto> validator,
        IStringLocalizer<ValidationResource> localizer,
        CreditNoteLifecycleEventService events,
        IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<Guid> HandleAsync(CreditNoteCreateDto dto, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        var invoice = await LoadIssuedInvoiceAsync(dto.InvoiceId, tracking: false, ct).ConfigureAwait(false);
        await ValidateOptionalLinksAsync(dto, invoice, ct).ConfigureAwait(false);

        var inputLines = dto.Lines
            .Where(x => x.InvoiceLineId != Guid.Empty && x.CreditedQuantity > 0)
            .GroupBy(x => x.InvoiceLineId)
            .ToDictionary(x => x.Key, x => x.Sum(line => line.CreditedQuantity));
        var priorCredits = await LoadCreditedQuantitiesAsync(invoice.Id, currentCreditNoteId: null, ct).ConfigureAwait(false);
        var note = new CreditNote
        {
            Id = Guid.NewGuid(),
            BusinessId = invoice.BusinessId,
            CustomerId = invoice.CustomerId,
            InvoiceId = invoice.Id,
            ReturnOrderId = NormalizeGuid(dto.ReturnOrderId),
            RefundId = NormalizeGuid(dto.RefundId),
            Status = CreditNoteStatus.Draft,
            Reason = dto.Reason,
            Currency = NormalizeCurrency(invoice.Currency),
            OriginalInvoiceNumber = NormalizeOptional(invoice.InvoiceNumber),
            InternalNotes = NormalizeOptional(dto.InternalNotes),
            MetadataJson = "{}"
        };

        var sort = 0;
        foreach (var invoiceLine in invoice.Lines.Where(x => !x.IsDeleted).OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id))
        {
            if (!inputLines.TryGetValue(invoiceLine.Id, out var quantity) || quantity <= 0)
            {
                continue;
            }

            var alreadyCredited = priorCredits.GetValueOrDefault(invoiceLine.Id);
            if (quantity + alreadyCredited > invoiceLine.Quantity)
            {
                throw new ValidationException(_localizer["CreditNoteLineExceedsInvoiceQuantity"]);
            }

            note.Lines.Add(CreateLine(invoiceLine, quantity, sort++));
        }

        if (note.Lines.Count == 0)
        {
            throw new ValidationException(_localizer["CreditNoteRequiresLines"]);
        }

        RecalculateTotals(note);
        _db.Set<CreditNote>().Add(note);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        var eventResult = await _events.RecordCreatedAsync(note, _clock.UtcNow, ct).ConfigureAwait(false);
        if (!eventResult.Succeeded)
        {
            throw new ValidationException(eventResult.Error);
        }

        return note.Id;
    }

    private async Task<Invoice> LoadIssuedInvoiceAsync(Guid invoiceId, bool tracking, CancellationToken ct)
    {
        if (invoiceId == Guid.Empty)
        {
            throw new ValidationException(_localizer["InvoiceNotFound"]);
        }

        var query = _db.Set<Invoice>().Include(x => x.Lines).Where(x => x.Id == invoiceId && !x.IsDeleted);
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        var invoice = await query.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (invoice is null || invoice.Status == InvoiceStatus.Draft)
        {
            throw new ValidationException(_localizer["InvoiceNotFound"]);
        }

        if (string.IsNullOrWhiteSpace(invoice.IssuedSnapshotJson) || string.IsNullOrWhiteSpace(invoice.IssuedSnapshotHashSha256))
        {
            throw new ValidationException(_localizer["CreditNoteRequiresIssuedInvoiceSource"]);
        }

        return invoice;
    }

    private async Task ValidateOptionalLinksAsync(CreditNoteCreateDto dto, Invoice invoice, CancellationToken ct)
    {
        var returnOrderId = NormalizeGuid(dto.ReturnOrderId);
        if (returnOrderId.HasValue)
        {
            var valid = await _db.Set<ReturnOrder>()
                .AsNoTracking()
                .AnyAsync(x => x.Id == returnOrderId.Value && x.InvoiceId == invoice.Id && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (!valid)
            {
                throw new ValidationException(_localizer["ReturnOrderNotFound"]);
            }
        }

        var refundId = NormalizeGuid(dto.RefundId);
        if (refundId.HasValue)
        {
            var refund = await _db.Set<Refund>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == refundId.Value && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (refund is null || refund.Status != RefundStatus.Completed)
            {
                throw new ValidationException(_localizer["RefundNotFound"]);
            }

            if (!string.Equals(refund.Currency, invoice.Currency, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException(_localizer["RefundCurrencyMustMatchLinkedInvoiceAndPaymentCurrency"]);
            }

            if (refund.OrderId.HasValue && (!invoice.OrderId.HasValue || refund.OrderId.Value != invoice.OrderId.Value))
            {
                throw new ValidationException(_localizer["RefundNotFound"]);
            }

            var payment = await _db.Set<Payment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == refund.PaymentId && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (payment is null || !string.Equals(payment.Currency, invoice.Currency, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException(_localizer["RefundNotFound"]);
            }

            if (payment.InvoiceId.HasValue && payment.InvoiceId.Value != invoice.Id)
            {
                throw new ValidationException(_localizer["RefundNotFound"]);
            }

            if (!payment.InvoiceId.HasValue && (!invoice.OrderId.HasValue || payment.OrderId != invoice.OrderId.Value))
            {
                throw new ValidationException(_localizer["RefundNotFound"]);
            }
        }
    }

    internal static CreditNoteLine CreateLine(InvoiceLine invoiceLine, int creditedQuantity, int sortOrder)
    {
        var net = creditedQuantity * invoiceLine.UnitPriceNetMinor;
        var gross = invoiceLine.Quantity <= 0
            ? invoiceLine.TotalGrossMinor
            : checked(invoiceLine.TotalGrossMinor * creditedQuantity / invoiceLine.Quantity);
        var tax = Math.Max(0, gross - net);
        return new CreditNoteLine
        {
            Id = Guid.NewGuid(),
            InvoiceLineId = invoiceLine.Id,
            Description = invoiceLine.Description.Trim(),
            OriginalQuantity = invoiceLine.Quantity,
            CreditedQuantity = creditedQuantity,
            UnitPriceNetMinor = invoiceLine.UnitPriceNetMinor,
            TaxRate = invoiceLine.TaxRate,
            TotalNetMinor = net,
            TotalTaxMinor = tax,
            TotalGrossMinor = gross,
            SourceLineJson = BuildLineSourceJson(invoiceLine, creditedQuantity, net, tax, gross),
            SortOrder = sortOrder
        };
    }

    internal static void RecalculateTotals(CreditNote note)
    {
        var lines = note.Lines.Where(x => !x.IsDeleted).ToList();
        note.TotalNetMinor = lines.Sum(x => x.TotalNetMinor);
        note.TotalTaxMinor = lines.Sum(x => x.TotalTaxMinor);
        note.TotalGrossMinor = lines.Sum(x => x.TotalGrossMinor);
    }

    internal async Task<IReadOnlyDictionary<Guid, int>> LoadCreditedQuantitiesAsync(Guid invoiceId, Guid? currentCreditNoteId, CancellationToken ct)
        => await _db.Set<CreditNote>()
            .AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId && x.Status == CreditNoteStatus.Issued && !x.IsDeleted && (!currentCreditNoteId.HasValue || x.Id != currentCreditNoteId.Value))
            .SelectMany(x => x.Lines.Where(line => !line.IsDeleted))
            .Where(x => x.InvoiceLineId.HasValue)
            .GroupBy(x => x.InvoiceLineId!.Value)
            .Select(x => new { InvoiceLineId = x.Key, Quantity = x.Sum(line => line.CreditedQuantity) })
            .ToDictionaryAsync(x => x.InvoiceLineId, x => x.Quantity, ct)
            .ConfigureAwait(false);

    internal static string BuildSourceModelJson(CreditNote note, Invoice invoice)
        => JsonSerializer.Serialize(new
        {
            creditNoteId = note.Id,
            creditNoteNumber = note.CreditNoteNumber,
            invoiceId = invoice.Id,
            invoiceNumber = invoice.InvoiceNumber,
            invoiceIssuedAtUtc = invoice.IssuedAtUtc,
            invoiceSourceHashSha256 = invoice.IssuedSnapshotHashSha256,
            reason = note.Reason.ToString(),
            currency = note.Currency,
            totalNetMinor = note.TotalNetMinor,
            totalTaxMinor = note.TotalTaxMinor,
            totalGrossMinor = note.TotalGrossMinor,
            lines = note.Lines.Where(x => !x.IsDeleted).OrderBy(x => x.SortOrder).Select(x => new
            {
                invoiceLineId = x.InvoiceLineId,
                description = x.Description,
                originalQuantity = x.OriginalQuantity,
                creditedQuantity = x.CreditedQuantity,
                unitPriceNetMinor = x.UnitPriceNetMinor,
                taxRate = x.TaxRate,
                totalNetMinor = x.TotalNetMinor,
                totalTaxMinor = x.TotalTaxMinor,
                totalGrossMinor = x.TotalGrossMinor
            })
        });

    internal static string Sha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string BuildLineSourceJson(InvoiceLine invoiceLine, int creditedQuantity, long net, long tax, long gross)
        => JsonSerializer.Serialize(new
        {
            invoiceLineId = invoiceLine.Id,
            description = invoiceLine.Description,
            originalQuantity = invoiceLine.Quantity,
            creditedQuantity,
            unitPriceNetMinor = invoiceLine.UnitPriceNetMinor,
            taxRate = invoiceLine.TaxRate,
            originalTotalNetMinor = invoiceLine.TotalNetMinor,
            originalTotalTaxMinor = invoiceLine.TotalTaxMinor,
            originalTotalGrossMinor = invoiceLine.TotalGrossMinor,
            totalNetMinor = net,
            totalTaxMinor = tax,
            totalGrossMinor = gross
        });

    internal static Guid? NormalizeGuid(Guid? value) => value.HasValue && value.Value != Guid.Empty ? value.Value : null;
    internal static string NormalizeCurrency(string? value) => string.IsNullOrWhiteSpace(value) ? "EUR" : value.Trim().ToUpperInvariant();
    internal static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class UpdateCreditNoteLifecycleHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<CreditNoteLifecycleDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly NumberSequenceService _numberSequence;
    private readonly CreditNoteWorkflowPolicy _policy;
    private readonly CreditNoteLifecycleEventService _events;
    private readonly FinanceReceivablesPostingService _posting;

    public UpdateCreditNoteLifecycleHandler(
        IAppDbContext db,
        IValidator<CreditNoteLifecycleDto> validator,
        IStringLocalizer<ValidationResource> localizer,
        IClock clock,
        NumberSequenceService numberSequence,
        CreditNoteWorkflowPolicy policy,
        CreditNoteLifecycleEventService events,
        FinanceReceivablesPostingService posting)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _numberSequence = numberSequence ?? throw new ArgumentNullException(nameof(numberSequence));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _posting = posting ?? throw new ArgumentNullException(nameof(posting));
    }

    public async Task IssueAsync(CreditNoteLifecycleDto dto, CancellationToken ct = default)
    {
        await TransitionAsync(dto, CreditNoteStatus.Issued, async note =>
        {
            var invoice = await _db.Set<Invoice>()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == note.InvoiceId && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (invoice is null || invoice.Status == InvoiceStatus.Draft)
            {
                throw new ValidationException(_localizer["InvoiceNotFound"]);
            }

            var priorCredits = await _db.Set<CreditNote>()
                .AsNoTracking()
                .Where(x => x.InvoiceId == note.InvoiceId && x.Status == CreditNoteStatus.Issued && x.Id != note.Id && !x.IsDeleted)
                .SelectMany(x => x.Lines.Where(line => !line.IsDeleted))
                .Where(x => x.InvoiceLineId.HasValue)
                .GroupBy(x => x.InvoiceLineId!.Value)
                .Select(x => new { InvoiceLineId = x.Key, Quantity = x.Sum(line => line.CreditedQuantity) })
                .ToDictionaryAsync(x => x.InvoiceLineId, x => x.Quantity, ct)
                .ConfigureAwait(false);

            var invoiceLines = invoice.Lines.Where(x => !x.IsDeleted).ToDictionary(x => x.Id);
            foreach (var line in note.Lines.Where(x => !x.IsDeleted))
            {
                if (!line.InvoiceLineId.HasValue || !invoiceLines.TryGetValue(line.InvoiceLineId.Value, out var invoiceLine))
                {
                    throw new ValidationException(_localizer["CreditNoteInvalidInvoiceLine"]);
                }

                if (line.CreditedQuantity + priorCredits.GetValueOrDefault(invoiceLine.Id) > invoiceLine.Quantity)
                {
                    throw new ValidationException(_localizer["CreditNoteLineExceedsInvoiceQuantity"]);
                }
            }

            if (string.IsNullOrWhiteSpace(note.CreditNoteNumber))
            {
                note.CreditNoteNumber = await ReserveCreditNoteNumberAsync(note.BusinessId, ct).ConfigureAwait(false);
            }

            note.IssuedAtUtc = _clock.UtcNow;
            note.IssuedByUserId = CreateCreditNoteHandler.NormalizeGuid(dto.ActorUserId);
            note.SourceModelJson = CreateCreditNoteHandler.BuildSourceModelJson(note, invoice);
            note.SourceModelHashSha256 = CreateCreditNoteHandler.Sha256(note.SourceModelJson);
            note.ArchiveGeneratedAtUtc = note.IssuedAtUtc;
            note.ArchiveRetainUntilUtc = note.IssuedAtUtc?.AddYears(10);
            note.ArchiveRetentionPolicyVersion = "credit-note-v1";
        }, async note =>
        {
            var postingResult = await _posting.PostCreditNoteIssuedAsync(note, note.IssuedAtUtc ?? _clock.UtcNow, ct).ConfigureAwait(false);
            if (!postingResult.Succeeded)
            {
                throw new ValidationException(postingResult.Error);
            }

            if (postingResult.Value is not null)
            {
                note.PostingJournalEntryId = postingResult.Value.JournalEntryId;
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
        }, ct, runAfterMutateBeforeSave: true).ConfigureAwait(false);
    }

    public Task CancelAsync(CreditNoteLifecycleDto dto, CancellationToken ct = default)
        => TransitionAsync(dto, CreditNoteStatus.Cancelled, note =>
        {
            note.CancelledAtUtc = _clock.UtcNow;
            note.CancelledByUserId = CreateCreditNoteHandler.NormalizeGuid(dto.ActorUserId);
            return Task.CompletedTask;
        }, AfterSave: null, ct);

    public async Task VoidAsync(CreditNoteLifecycleDto dto, CancellationToken ct = default)
    {
        await TransitionAsync(dto, CreditNoteStatus.Voided, note =>
        {
            note.VoidedAtUtc = _clock.UtcNow;
            note.VoidedByUserId = CreateCreditNoteHandler.NormalizeGuid(dto.ActorUserId);
            return Task.CompletedTask;
        }, async note =>
        {
            var postingResult = await _posting.PostCreditNoteVoidedAsync(note, note.VoidedAtUtc ?? _clock.UtcNow, ct).ConfigureAwait(false);
            if (!postingResult.Succeeded)
            {
                throw new ValidationException(postingResult.Error);
            }
        }, ct, runAfterMutateBeforeSave: true).ConfigureAwait(false);
    }

    private async Task TransitionAsync(
        CreditNoteLifecycleDto dto,
        CreditNoteStatus target,
        Func<CreditNote, Task> mutate,
        Func<CreditNote, Task>? AfterSave,
        CancellationToken ct,
        bool runAfterMutateBeforeSave = false)
    {
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        var note = await _db.Set<CreditNote>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (note is null)
        {
            throw new ValidationException(_localizer["CreditNoteNotFound"]);
        }

        SalesQuoteGuard.EnsureRowVersion(note.RowVersion, dto.RowVersion, _localizer);
        var from = note.Status;
        if (!_policy.CanTransition(from, target))
        {
            throw new ValidationException(_localizer["CreditNoteInvalidLifecycleTransition"]);
        }

        await mutate(note).ConfigureAwait(false);
        note.Status = target;
        CreateCreditNoteHandler.RecalculateTotals(note);

        if (runAfterMutateBeforeSave && AfterSave is not null)
        {
            await AfterSave(note).ConfigureAwait(false);
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        if (!runAfterMutateBeforeSave && AfterSave is not null)
        {
            await AfterSave(note).ConfigureAwait(false);
        }

        var eventResult = await _events.RecordStatusChangedAsync(note, from, target, _clock.UtcNow, ct).ConfigureAwait(false);
        if (!eventResult.Succeeded)
        {
            throw new ValidationException(eventResult.Error);
        }
    }

    private async Task<string> ReserveCreditNoteNumberAsync(Guid? businessId, CancellationToken ct)
    {
        var result = await _numberSequence
            .ReserveNextAsync(new NumberSequenceRequest(businessId, NumberSequenceDocumentType.CreditNote, NumberSequenceService.GlobalScopeKey), ct)
            .ConfigureAwait(false);
        if (!result.Succeeded && businessId.HasValue)
        {
            result = await _numberSequence
                .ReserveNextAsync(new NumberSequenceRequest(null, NumberSequenceDocumentType.CreditNote, NumberSequenceService.GlobalScopeKey), ct)
                .ConfigureAwait(false);
        }

        if (!result.Succeeded)
        {
            throw new ValidationException(_localizer["CreditNoteNumberSequenceRequired"]);
        }

        return result.Value!;
    }
}
