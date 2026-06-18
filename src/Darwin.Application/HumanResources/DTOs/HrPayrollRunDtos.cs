using Darwin.Domain.Enums;

namespace Darwin.Application.HumanResources.DTOs;

public enum PayrollRunQueueFilter
{
    All = 0,
    Draft = 1,
    Calculated = 2,
    Reviewed = 3,
    Approved = 4,
    Cancelled = 5,
    Posted = 6
}

public sealed class PayrollRunCreateDto
{
    public Guid BusinessId { get; set; }
    public Guid PayrollPeriodId { get; set; }
    public Guid PayrollRuleSetId { get; set; }
    public string? ReviewNotes { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class PayrollRunLifecycleDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public string? Notes { get; set; }
}

public sealed class PayrollRunListItemDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid BusinessId { get; set; }
    public Guid PayrollPeriodId { get; set; }
    public Guid PayrollRuleSetId { get; set; }
    public string RunNumber { get; set; } = string.Empty;
    public PayrollRunStatus Status { get; set; } = PayrollRunStatus.Draft;
    public string PeriodCode { get; set; } = string.Empty;
    public string RuleSetCode { get; set; } = string.Empty;
    public string RuleVersion { get; set; } = string.Empty;
    public string JurisdictionCode { get; set; } = "DE";
    public string Currency { get; set; } = "EUR";
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public int EmployeeCount { get; set; }
    public long GrossPayMinor { get; set; }
    public long EmployeeDeductionMinor { get; set; }
    public long EmployerCostMinor { get; set; }
    public long NetPayMinor { get; set; }
    public DateTime? CalculatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public Guid? PostingJournalEntryId { get; set; }
    public string? ReviewNotes { get; set; }
    public string? SourceSnapshotJson { get; set; }
    public string? MetadataJson { get; set; }
    public List<PayrollRunLineDto> Lines { get; set; } = new();
}

public sealed class PayrollRunLineDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? EmploymentContractId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public int WorkMinutes { get; set; }
    public int BreakMinutes { get; set; }
    public int AbsenceMinutes { get; set; }
    public long GrossPayMinor { get; set; }
    public long EmployeeDeductionMinor { get; set; }
    public long EmployerCostMinor { get; set; }
    public long NetPayMinor { get; set; }
    public List<PayrollRunLineComponentDto> Components { get; set; } = new();
}

public sealed class PayrollRunLineComponentDto
{
    public Guid Id { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public PayrollRuleComponentType ComponentType { get; set; }
    public PayrollRuleCalculationMethod CalculationMethod { get; set; }
    public PayrollRuleBasis Basis { get; set; }
    public long AmountMinor { get; set; }
    public bool IsEmployerCost { get; set; }
    public int SortOrder { get; set; }
}
