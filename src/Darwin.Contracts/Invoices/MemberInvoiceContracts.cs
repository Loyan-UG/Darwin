using Darwin.Contracts.Common;

namespace Darwin.Contracts.Invoices;

/// <summary>
/// Member-facing summary of an invoice in invoice history screens.
/// </summary>
public class MemberInvoiceSummary
{
    /// <summary>Gets or sets the invoice identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the optional business identifier.</summary>
    public Guid? BusinessId { get; set; }

    /// <summary>Gets or sets the optional business name.</summary>
    public string? BusinessName { get; set; }

    /// <summary>Gets or sets the optional related order identifier.</summary>
    public Guid? OrderId { get; set; }

    /// <summary>Gets or sets the optional order number.</summary>
    public string? OrderNumber { get; set; }

    /// <summary>Gets or sets the invoice currency.</summary>
    public string Currency { get; set; } = ContractDefaults.DefaultCurrency;

    /// <summary>Gets or sets the total invoice amount in minor units.</summary>
    public long TotalGrossMinor { get; set; }

    /// <summary>Gets or sets the refunded amount in minor units.</summary>
    public long RefundedAmountMinor { get; set; }

    /// <summary>Gets or sets the settled amount in minor units.</summary>
    public long SettledAmountMinor { get; set; }

    /// <summary>Gets or sets the remaining balance in minor units.</summary>
    public long BalanceMinor { get; set; }

    /// <summary>Gets or sets the invoice status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the due date in UTC.</summary>
    public DateTime DueDateUtc { get; set; }

    /// <summary>Gets or sets the paid timestamp in UTC, when available.</summary>
    public DateTime? PaidAtUtc { get; set; }

    /// <summary>Gets or sets the creation timestamp in UTC.</summary>
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Member-facing detailed invoice representation.
/// </summary>
public sealed class MemberInvoiceDetail : MemberInvoiceSummary
{
    /// <summary>Gets or sets the total net amount in minor units.</summary>
    public long TotalNetMinor { get; set; }

    /// <summary>Gets or sets the total tax amount in minor units.</summary>
    public long TotalTaxMinor { get; set; }

    /// <summary>Gets or sets a compact linked payment summary.</summary>
    public string PaymentSummary { get; set; } = string.Empty;

    /// <summary>Gets or sets the billed line snapshots.</summary>
    public IReadOnlyList<MemberInvoiceLine> Lines { get; set; } = Array.Empty<MemberInvoiceLine>();

    /// <summary>Gets or sets the available member actions for this invoice.</summary>
    public MemberInvoiceActions Actions { get; set; } = new();
}

/// <summary>
/// Member-facing invoice line snapshot.
/// </summary>
public sealed class MemberInvoiceLine
{
    /// <summary>Gets or sets the invoice line identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the line description shown to the member.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the billed quantity.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets the unit net amount in minor units.</summary>
    public long UnitPriceNetMinor { get; set; }

    /// <summary>Gets or sets the tax rate as a decimal fraction.</summary>
    public decimal TaxRate { get; set; }

    /// <summary>Gets or sets the total net amount in minor units.</summary>
    public long TotalNetMinor { get; set; }

    /// <summary>Gets or sets the total gross amount in minor units.</summary>
    public long TotalGrossMinor { get; set; }
}

/// <summary>
/// Member-facing action metadata for an invoice detail screen.
/// </summary>
public sealed class MemberInvoiceActions
{
    /// <summary>Gets or sets a value indicating whether the current member may retry payment for the invoice.</summary>
    public bool CanRetryPayment { get; set; }

    /// <summary>Gets or sets the canonical API path for creating a payment intent, when available.</summary>
    public string? PaymentIntentPath { get; set; }

    /// <summary>Gets or sets the canonical API path for navigating to the linked order, when available.</summary>
    public string? OrderPath { get; set; }

    /// <summary>Gets or sets the canonical API path for downloading an invoice document.</summary>
    public string DocumentPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the canonical API path for downloading the printable issued-invoice archive document.</summary>
    public string? ArchiveDocumentPath { get; set; }

    /// <summary>Gets or sets the canonical API path for downloading the structured source-model JSON export.</summary>
    public string? StructuredDataPath { get; set; }

    /// <summary>Gets or sets the canonical API path for downloading the structured source-model XML export.</summary>
    public string? StructuredXmlPath { get; set; }
}
