using Darwin.Domain.Enums;

namespace Darwin.Application.CRM.DTOs
{
    public sealed class InvoiceListItemDto
    {
        public Guid Id { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public Guid? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public Guid? PaymentId { get; set; }
        public string PaymentSummary { get; set; } = string.Empty;
        public InvoiceStatus Status { get; set; }
        public string Currency { get; set; } = "EUR";
        public long TotalGrossMinor { get; set; }
        public DateTime DueDateUtc { get; set; }
        public DateTime? PaidAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class InvoiceEditDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public Guid? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public Guid? PaymentId { get; set; }
        public string PaymentSummary { get; set; } = string.Empty;
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
        public string Currency { get; set; } = "EUR";
        public long TotalNetMinor { get; set; }
        public long TotalTaxMinor { get; set; }
        public long TotalGrossMinor { get; set; }
        public DateTime DueDateUtc { get; set; }
        public DateTime? PaidAtUtc { get; set; }
    }
}
