using Darwin.Domain.Enums;

namespace Darwin.Application.CRM.DTOs;

/// <summary>
/// Summary projection used by member-facing invoice history screens.
/// </summary>
public class MemberInvoiceSummaryDto
{
    /// <summary>Gets or sets the invoice identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the optional business identifier.</summary>
    public Guid? BusinessId { get; set; }

    /// <summary>Gets or sets the optional business name.</summary>
    public string? BusinessName { get; set; }

    /// <summary>Gets or sets the optional related order identifier.</summary>
    public Guid? OrderId { get; set; }

    /// <summary>Gets or sets the optional human-friendly order number.</summary>
    public string? OrderNumber { get; set; }

    /// <summary>Gets or sets the invoice currency.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>Gets or sets the total invoice amount in minor units.</summary>
    public long TotalGrossMinor { get; set; }

    /// <summary>Gets or sets the refunded amount in minor units.</summary>
    public long RefundedAmountMinor { get; set; }

    /// <summary>Gets or sets the settled amount in minor units.</summary>
    public long SettledAmountMinor { get; set; }

    /// <summary>Gets or sets the remaining balance in minor units.</summary>
    public long BalanceMinor { get; set; }

    /// <summary>Gets or sets the current invoice lifecycle status.</summary>
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    /// <summary>Gets or sets the due date in UTC.</summary>
    public DateTime DueDateUtc { get; set; }

    /// <summary>Gets or sets the paid timestamp in UTC, when available.</summary>
    public DateTime? PaidAtUtc { get; set; }

    /// <summary>Gets or sets the creation timestamp in UTC.</summary>
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Detailed projection used by member-facing invoice detail screens.
/// </summary>
public sealed class MemberInvoiceDetailDto : MemberInvoiceSummaryDto
{
    /// <summary>Gets or sets the total net amount in minor units.</summary>
    public long TotalNetMinor { get; set; }

    /// <summary>Gets or sets the total tax amount in minor units.</summary>
    public long TotalTaxMinor { get; set; }

    /// <summary>Gets or sets a compact payment summary for the linked payment.</summary>
    public string PaymentSummary { get; set; } = string.Empty;

    /// <summary>Gets or sets the invoice line snapshots.</summary>
    public List<MemberInvoiceLineDto> Lines { get; set; } = new();
}

/// <summary>
/// Member-facing snapshot of an invoice line.
/// </summary>
public sealed class MemberInvoiceLineDto
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
