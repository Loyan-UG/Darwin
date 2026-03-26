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
    public string Currency { get; set; } = "EUR";

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
}
