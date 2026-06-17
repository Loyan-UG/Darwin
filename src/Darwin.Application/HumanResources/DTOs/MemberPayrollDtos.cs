namespace Darwin.Application.HumanResources.DTOs;

public sealed class MemberPayslipSummaryDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string PayslipNumber { get; set; } = string.Empty;
    public string Currency { get; set; } = "EUR";
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public long GrossPayMinor { get; set; }
    public long EmployeeDeductionMinor { get; set; }
    public long NetPayMinor { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; }
    public string DocumentPath { get; set; } = string.Empty;
}

public sealed record MemberPayslipDownloadResult(
    Stream Content,
    string ContentType,
    string FileName,
    long? SizeBytes,
    string? ContentHash);
