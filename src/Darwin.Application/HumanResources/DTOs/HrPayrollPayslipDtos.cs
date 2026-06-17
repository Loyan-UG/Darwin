using Darwin.Domain.Enums;

namespace Darwin.Application.HumanResources.DTOs;

public sealed class GeneratePayrollPayslipsDto
{
    public Guid PayrollRunId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class PayrollPayslipListItemDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid PayrollRunId { get; set; }
    public Guid PayrollRunLineId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid DocumentRecordId { get; set; }
    public string PayslipNumber { get; set; } = string.Empty;
    public PayrollPayslipStatus Status { get; set; } = PayrollPayslipStatus.Generated;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Currency { get; set; } = "EUR";
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public long GrossPayMinor { get; set; }
    public long EmployeeDeductionMinor { get; set; }
    public long EmployerCostMinor { get; set; }
    public long NetPayMinor { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
}

public sealed record PayrollPayslipDownloadResult(
    Stream Content,
    string ContentType,
    string FileName,
    long? SizeBytes,
    string? ContentHash);

