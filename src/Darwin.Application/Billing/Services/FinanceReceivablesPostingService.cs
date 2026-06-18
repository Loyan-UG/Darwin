using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;

namespace Darwin.Application.Billing.Services;

/// <summary>
/// Posts invoice, payment, and refund facts into the finance journal foundation.
/// </summary>
public sealed class FinanceReceivablesPostingService
{
    private const string InvoiceEntityType = "Invoice";
    private const string PaymentEntityType = "Payment";
    private const string RefundEntityType = "Refund";
    private const string CreditNoteEntityType = "CreditNote";

    private readonly FinanceAccountMappingService _accountMappings;
    private readonly FinancePostingService _postingService;

    public FinanceReceivablesPostingService(
        FinanceAccountMappingService accountMappings,
        FinancePostingService postingService)
    {
        _accountMappings = accountMappings ?? throw new ArgumentNullException(nameof(accountMappings));
        _postingService = postingService ?? throw new ArgumentNullException(nameof(postingService));
    }

    public async Task<Result<FinancePostingResult?>> PostInvoiceIssuedAsync(
        Invoice invoice,
        DateTime entryDateUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        if (invoice.BusinessId is null || invoice.BusinessId.Value == Guid.Empty)
        {
            return Result<FinancePostingResult?>.Ok(null);
        }

        if (invoice.Id == Guid.Empty)
        {
            return Result<FinancePostingResult?>.Fail("Invoice id is required for finance posting.");
        }

        if (invoice.Status == InvoiceStatus.Draft)
        {
            return Result<FinancePostingResult?>.Ok(null);
        }

        if (invoice.TotalGrossMinor <= 0)
        {
            return Result<FinancePostingResult?>.Fail("Invoice gross amount must be positive for finance posting.");
        }

        var requiredRoles = invoice.TotalTaxMinor == 0
            ? [FinancePostingAccountRole.Receivables, FinancePostingAccountRole.SalesRevenue]
            : new[]
            {
                FinancePostingAccountRole.Receivables,
                FinancePostingAccountRole.SalesRevenue,
                FinancePostingAccountRole.TaxPayable
            };
        var accounts = await ResolveAccountsAsync(invoice.BusinessId.Value, requiredRoles, ct).ConfigureAwait(false);
        if (!accounts.Succeeded)
        {
            return Result<FinancePostingResult?>.Fail(accounts.Error!);
        }

        var lines = new List<FinancePostingLineCommand>
        {
            new(accounts.Value![FinancePostingAccountRole.Receivables], invoice.TotalGrossMinor, 0, "Invoice receivable")
        };
        if (invoice.TotalNetMinor > 0)
        {
            lines.Add(new FinancePostingLineCommand(accounts.Value[FinancePostingAccountRole.SalesRevenue], 0, invoice.TotalNetMinor, "Sales revenue"));
        }

        if (invoice.TotalTaxMinor > 0)
        {
            lines.Add(new FinancePostingLineCommand(accounts.Value[FinancePostingAccountRole.TaxPayable], 0, invoice.TotalTaxMinor, "Tax payable"));
        }

        if (lines.Sum(x => x.DebitMinor) != lines.Sum(x => x.CreditMinor))
        {
            return Result<FinancePostingResult?>.Fail("Invoice posting lines must be balanced.");
        }

        var result = await _postingService.PostAsync(new FinancePostingCommand(
            invoice.BusinessId.Value,
            entryDateUtc,
            JournalEntryPostingKind.InvoiceIssued,
            PostingKey("invoice.issued", invoice.Id),
            InvoiceEntityType,
            invoice.Id,
            "Invoice issued",
            lines,
            SourceDocumentNumber: invoice.InvoiceNumber,
            MetadataJson: $$"""{"invoiceId":"{{invoice.Id:N}}","status":"{{invoice.Status}}","currency":"{{Escape(invoice.Currency)}}","grossMinor":{{invoice.TotalGrossMinor}},"netMinor":{{invoice.TotalNetMinor}},"taxMinor":{{invoice.TotalTaxMinor}}}"""),
            ct).ConfigureAwait(false);

        return result.Succeeded
            ? Result<FinancePostingResult?>.Ok(result.Value)
            : Result<FinancePostingResult?>.Fail(result.Error!);
    }

    public async Task<Result<FinancePostingResult?>> PostPaymentRecordedAsync(
        Payment payment,
        DateTime entryDateUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(payment);
        if (payment.BusinessId is null || payment.BusinessId.Value == Guid.Empty)
        {
            return Result<FinancePostingResult?>.Ok(null);
        }

        if (payment.Id == Guid.Empty)
        {
            return Result<FinancePostingResult?>.Fail("Payment id is required for finance posting.");
        }

        if (payment.Status is not (PaymentStatus.Captured or PaymentStatus.Completed))
        {
            return Result<FinancePostingResult?>.Ok(null);
        }

        if (payment.AmountMinor <= 0)
        {
            return Result<FinancePostingResult?>.Fail("Payment amount must be positive for finance posting.");
        }

        var accounts = await ResolveAccountsAsync(
            payment.BusinessId.Value,
            [FinancePostingAccountRole.CashClearing, FinancePostingAccountRole.Receivables],
            ct).ConfigureAwait(false);
        if (!accounts.Succeeded)
        {
            return Result<FinancePostingResult?>.Fail(accounts.Error!);
        }

        var result = await _postingService.PostAsync(new FinancePostingCommand(
            payment.BusinessId.Value,
            entryDateUtc,
            JournalEntryPostingKind.PaymentRecorded,
            PostingKey("payment.recorded", payment.Id),
            PaymentEntityType,
            payment.Id,
            "Payment recorded",
            [
                new FinancePostingLineCommand(accounts.Value![FinancePostingAccountRole.CashClearing], payment.AmountMinor, 0, "Cash clearing"),
                new FinancePostingLineCommand(accounts.Value[FinancePostingAccountRole.Receivables], 0, payment.AmountMinor, "Receivable settlement")
            ],
            SourceDocumentNumber: payment.ProviderTransactionRef ?? payment.ProviderPaymentIntentRef,
            MetadataJson: $$"""{"paymentId":"{{payment.Id:N}}","orderId":{{JsonGuid(payment.OrderId)}},"invoiceId":{{JsonGuid(payment.InvoiceId)}},"status":"{{payment.Status}}","currency":"{{Escape(payment.Currency)}}","amountMinor":{{payment.AmountMinor}}}"""),
            ct).ConfigureAwait(false);

        return result.Succeeded
            ? Result<FinancePostingResult?>.Ok(result.Value)
            : Result<FinancePostingResult?>.Fail(result.Error!);
    }

    public async Task<Result<FinancePostingResult?>> PostRefundRecordedAsync(
        Refund refund,
        Payment payment,
        DateTime entryDateUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(refund);
        ArgumentNullException.ThrowIfNull(payment);
        if (payment.BusinessId is null || payment.BusinessId.Value == Guid.Empty)
        {
            return Result<FinancePostingResult?>.Ok(null);
        }

        if (refund.Id == Guid.Empty)
        {
            return Result<FinancePostingResult?>.Fail("Refund id is required for finance posting.");
        }

        if (refund.Status != RefundStatus.Completed)
        {
            return Result<FinancePostingResult?>.Ok(null);
        }

        if (refund.AmountMinor <= 0)
        {
            return Result<FinancePostingResult?>.Fail("Refund amount must be positive for finance posting.");
        }

        if (!string.Equals(refund.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase))
        {
            return Result<FinancePostingResult?>.Fail("Refund currency must match payment currency for finance posting.");
        }

        var accounts = await ResolveAccountsAsync(
            payment.BusinessId.Value,
            [FinancePostingAccountRole.Receivables, FinancePostingAccountRole.CashClearing],
            ct).ConfigureAwait(false);
        if (!accounts.Succeeded)
        {
            return Result<FinancePostingResult?>.Fail(accounts.Error!);
        }

        var result = await _postingService.PostAsync(new FinancePostingCommand(
            payment.BusinessId.Value,
            entryDateUtc,
            JournalEntryPostingKind.RefundRecorded,
            PostingKey("refund.recorded", refund.Id),
            RefundEntityType,
            refund.Id,
            "Refund recorded",
            [
                new FinancePostingLineCommand(accounts.Value![FinancePostingAccountRole.Receivables], refund.AmountMinor, 0, "Receivable refund settlement"),
                new FinancePostingLineCommand(accounts.Value[FinancePostingAccountRole.CashClearing], 0, refund.AmountMinor, "Cash clearing reversal")
            ],
            SourceDocumentNumber: refund.ProviderRefundReference,
            MetadataJson: $$"""{"refundId":"{{refund.Id:N}}","paymentId":"{{payment.Id:N}}","orderId":{{JsonGuid(refund.OrderId)}},"status":"{{refund.Status}}","currency":"{{Escape(refund.Currency)}}","amountMinor":{{refund.AmountMinor}}}"""),
            ct).ConfigureAwait(false);

        return result.Succeeded
            ? Result<FinancePostingResult?>.Ok(result.Value)
            : Result<FinancePostingResult?>.Fail(result.Error!);
    }

    public async Task<Result<FinancePostingResult?>> PostInvoiceCancelledAsync(
        Invoice invoice,
        DateTime entryDateUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        if (invoice.BusinessId is null || invoice.BusinessId.Value == Guid.Empty)
        {
            return Result<FinancePostingResult?>.Ok(null);
        }

        if (invoice.Id == Guid.Empty)
        {
            return Result<FinancePostingResult?>.Fail("Invoice id is required for finance reversal posting.");
        }

        if (invoice.Status != InvoiceStatus.Cancelled)
        {
            return Result<FinancePostingResult?>.Ok(null);
        }

        if (invoice.TotalGrossMinor <= 0)
        {
            return Result<FinancePostingResult?>.Fail("Invoice gross amount must be positive for finance reversal posting.");
        }

        var requiredRoles = invoice.TotalTaxMinor == 0
            ? [FinancePostingAccountRole.Receivables, FinancePostingAccountRole.SalesRevenue]
            : new[]
            {
                FinancePostingAccountRole.Receivables,
                FinancePostingAccountRole.SalesRevenue,
                FinancePostingAccountRole.TaxPayable
            };
        var accounts = await ResolveAccountsAsync(invoice.BusinessId.Value, requiredRoles, ct).ConfigureAwait(false);
        if (!accounts.Succeeded)
        {
            return Result<FinancePostingResult?>.Fail(accounts.Error!);
        }

        var lines = new List<FinancePostingLineCommand>();
        if (invoice.TotalNetMinor > 0)
        {
            lines.Add(new FinancePostingLineCommand(accounts.Value![FinancePostingAccountRole.SalesRevenue], invoice.TotalNetMinor, 0, "Sales revenue reversal"));
        }

        if (invoice.TotalTaxMinor > 0)
        {
            lines.Add(new FinancePostingLineCommand(accounts.Value![FinancePostingAccountRole.TaxPayable], invoice.TotalTaxMinor, 0, "Tax payable reversal"));
        }

        lines.Add(new FinancePostingLineCommand(accounts.Value![FinancePostingAccountRole.Receivables], 0, invoice.TotalGrossMinor, "Receivable reversal"));

        if (lines.Sum(x => x.DebitMinor) != lines.Sum(x => x.CreditMinor))
        {
            return Result<FinancePostingResult?>.Fail("Invoice reversal posting lines must be balanced.");
        }

        var result = await _postingService.PostAsync(new FinancePostingCommand(
            invoice.BusinessId.Value,
            entryDateUtc,
            JournalEntryPostingKind.Reversal,
            PostingKey("invoice.cancelled", invoice.Id),
            InvoiceEntityType,
            invoice.Id,
            "Invoice cancelled",
            lines,
            SourceDocumentNumber: invoice.InvoiceNumber,
            MetadataJson: $$"""{"invoiceId":"{{invoice.Id:N}}","status":"{{invoice.Status}}","currency":"{{Escape(invoice.Currency)}}","grossMinor":{{invoice.TotalGrossMinor}},"netMinor":{{invoice.TotalNetMinor}},"taxMinor":{{invoice.TotalTaxMinor}}}"""),
            ct).ConfigureAwait(false);

        return result.Succeeded
            ? Result<FinancePostingResult?>.Ok(result.Value)
            : Result<FinancePostingResult?>.Fail(result.Error!);
    }

    public async Task<Result<FinancePostingResult?>> PostCreditNoteIssuedAsync(
        CreditNote creditNote,
        DateTime entryDateUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(creditNote);
        if (creditNote.BusinessId is null || creditNote.BusinessId.Value == Guid.Empty)
        {
            return Result<FinancePostingResult?>.Ok(null);
        }

        if (creditNote.Id == Guid.Empty)
        {
            return Result<FinancePostingResult?>.Fail("Credit note id is required for finance posting.");
        }

        if (creditNote.Status != CreditNoteStatus.Issued)
        {
            return Result<FinancePostingResult?>.Ok(null);
        }

        if (creditNote.TotalGrossMinor <= 0)
        {
            return Result<FinancePostingResult?>.Fail("Credit note gross amount must be positive for finance posting.");
        }

        var requiredRoles = creditNote.TotalTaxMinor == 0
            ? [FinancePostingAccountRole.Receivables, FinancePostingAccountRole.SalesRevenue]
            : new[]
            {
                FinancePostingAccountRole.Receivables,
                FinancePostingAccountRole.SalesRevenue,
                FinancePostingAccountRole.TaxPayable
            };
        var accounts = await ResolveAccountsAsync(creditNote.BusinessId.Value, requiredRoles, ct).ConfigureAwait(false);
        if (!accounts.Succeeded)
        {
            return Result<FinancePostingResult?>.Fail(accounts.Error!);
        }

        var lines = new List<FinancePostingLineCommand>();
        if (creditNote.TotalNetMinor > 0)
        {
            lines.Add(new FinancePostingLineCommand(accounts.Value![FinancePostingAccountRole.SalesRevenue], creditNote.TotalNetMinor, 0, "Credit note revenue reversal"));
        }

        if (creditNote.TotalTaxMinor > 0)
        {
            lines.Add(new FinancePostingLineCommand(accounts.Value![FinancePostingAccountRole.TaxPayable], creditNote.TotalTaxMinor, 0, "Credit note tax reversal"));
        }

        lines.Add(new FinancePostingLineCommand(accounts.Value![FinancePostingAccountRole.Receivables], 0, creditNote.TotalGrossMinor, "Credit note receivable reduction"));

        if (lines.Sum(x => x.DebitMinor) != lines.Sum(x => x.CreditMinor))
        {
            return Result<FinancePostingResult?>.Fail("Credit note posting lines must be balanced.");
        }

        var result = await _postingService.PostAsync(new FinancePostingCommand(
            creditNote.BusinessId.Value,
            entryDateUtc,
            JournalEntryPostingKind.CreditNoteIssued,
            PostingKey("credit_note.issued", creditNote.Id),
            CreditNoteEntityType,
            creditNote.Id,
            "Credit note issued",
            lines,
            SourceDocumentNumber: creditNote.CreditNoteNumber,
            MetadataJson: $$"""{"creditNoteId":"{{creditNote.Id:N}}","invoiceId":"{{creditNote.InvoiceId:N}}","status":"{{creditNote.Status}}","currency":"{{Escape(creditNote.Currency)}}","grossMinor":{{creditNote.TotalGrossMinor}},"netMinor":{{creditNote.TotalNetMinor}},"taxMinor":{{creditNote.TotalTaxMinor}}}"""),
            ct).ConfigureAwait(false);

        return result.Succeeded
            ? Result<FinancePostingResult?>.Ok(result.Value)
            : Result<FinancePostingResult?>.Fail(result.Error!);
    }

    public async Task<Result<FinancePostingResult?>> PostCreditNoteVoidedAsync(
        CreditNote creditNote,
        DateTime entryDateUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(creditNote);
        if (creditNote.BusinessId is null || creditNote.BusinessId.Value == Guid.Empty)
        {
            return Result<FinancePostingResult?>.Ok(null);
        }

        if (creditNote.Id == Guid.Empty)
        {
            return Result<FinancePostingResult?>.Fail("Credit note id is required for finance reversal posting.");
        }

        if (creditNote.Status != CreditNoteStatus.Voided)
        {
            return Result<FinancePostingResult?>.Ok(null);
        }

        if (creditNote.TotalGrossMinor <= 0)
        {
            return Result<FinancePostingResult?>.Fail("Credit note gross amount must be positive for finance reversal posting.");
        }

        var requiredRoles = creditNote.TotalTaxMinor == 0
            ? [FinancePostingAccountRole.Receivables, FinancePostingAccountRole.SalesRevenue]
            : new[]
            {
                FinancePostingAccountRole.Receivables,
                FinancePostingAccountRole.SalesRevenue,
                FinancePostingAccountRole.TaxPayable
            };
        var accounts = await ResolveAccountsAsync(creditNote.BusinessId.Value, requiredRoles, ct).ConfigureAwait(false);
        if (!accounts.Succeeded)
        {
            return Result<FinancePostingResult?>.Fail(accounts.Error!);
        }

        var lines = new List<FinancePostingLineCommand>
        {
            new(accounts.Value![FinancePostingAccountRole.Receivables], creditNote.TotalGrossMinor, 0, "Credit note receivable reversal")
        };
        if (creditNote.TotalNetMinor > 0)
        {
            lines.Add(new FinancePostingLineCommand(accounts.Value[FinancePostingAccountRole.SalesRevenue], 0, creditNote.TotalNetMinor, "Credit note revenue reinstatement"));
        }

        if (creditNote.TotalTaxMinor > 0)
        {
            lines.Add(new FinancePostingLineCommand(accounts.Value[FinancePostingAccountRole.TaxPayable], 0, creditNote.TotalTaxMinor, "Credit note tax reinstatement"));
        }

        if (lines.Sum(x => x.DebitMinor) != lines.Sum(x => x.CreditMinor))
        {
            return Result<FinancePostingResult?>.Fail("Credit note reversal posting lines must be balanced.");
        }

        var result = await _postingService.PostAsync(new FinancePostingCommand(
            creditNote.BusinessId.Value,
            entryDateUtc,
            JournalEntryPostingKind.Reversal,
            PostingKey("credit_note.voided", creditNote.Id),
            CreditNoteEntityType,
            creditNote.Id,
            "Credit note voided",
            lines,
            SourceDocumentNumber: creditNote.CreditNoteNumber,
            MetadataJson: $$"""{"creditNoteId":"{{creditNote.Id:N}}","invoiceId":"{{creditNote.InvoiceId:N}}","status":"{{creditNote.Status}}","currency":"{{Escape(creditNote.Currency)}}","grossMinor":{{creditNote.TotalGrossMinor}},"netMinor":{{creditNote.TotalNetMinor}},"taxMinor":{{creditNote.TotalTaxMinor}}}"""),
            ct).ConfigureAwait(false);

        return result.Succeeded
            ? Result<FinancePostingResult?>.Ok(result.Value)
            : Result<FinancePostingResult?>.Fail(result.Error!);
    }

    private Task<Result<IReadOnlyDictionary<FinancePostingAccountRole, Guid>>> ResolveAccountsAsync(
        Guid businessId,
        IReadOnlyCollection<FinancePostingAccountRole> roles,
        CancellationToken ct)
        => _accountMappings.ResolveRequiredAccountsAsync(businessId, roles, ct);

    private static string PostingKey(string fact, Guid id)
        => $"finance.{fact}:{id:N}";

    private static string Escape(string? value)
        => (value ?? string.Empty).Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private static string JsonGuid(Guid? value)
        => value.HasValue && value.Value != Guid.Empty ? $"\"{value.Value:N}\"" : "null";
}
