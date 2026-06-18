using Darwin.Application.HumanResources.DTOs;
using Darwin.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Darwin.WebAdmin.ViewModels.HumanResources;

public sealed class HrListVm<TItem, TFilter>
{
    public Guid? BusinessId { get; set; }
    public string Query { get; set; } = string.Empty;
    public TFilter Filter { get; set; } = default!;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int Total { get; set; }
    public List<TItem> Items { get; set; } = new();
    [ValidateNever] public IEnumerable<SelectListItem> BusinessOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
}

public sealed class EmployeeEditVm
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    [Required] public Guid BusinessId { get; set; }
    public Guid? BusinessMemberId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    [Required, MaxLength(64)] public string EmployeeNumber { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string LastName { get; set; } = string.Empty;
    [MaxLength(100)] public string? PreferredName { get; set; }
    [MaxLength(254)] public string? WorkEmail { get; set; }
    [MaxLength(64)] public string? WorkPhone { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public DateTime? HireDateUtc { get; set; }
    public DateTime? TerminationDateUtc { get; set; }
    public HrPrivacyClassification PrivacyClassification { get; set; } = HrPrivacyClassification.Confidential;
    [MaxLength(4000)] public string? InternalNotes { get; set; }
    [MaxLength(8000)] public string? MetadataJson { get; set; }
    public string? DepartmentName { get; set; }
    public string? PositionName { get; set; }
    public List<EmploymentContractListItemDto> Contracts { get; set; } = new();
    public List<PersonnelDocumentListItemDto> Documents { get; set; } = new();
    [ValidateNever] public IEnumerable<SelectListItem> BusinessOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> DepartmentOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> PositionOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> PrivacyOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> DocumentKindOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> DocumentPrivacyOptions { get; set; } = new List<SelectListItem>();
}

public sealed class PersonnelDocumentUploadVm
{
    public Guid BusinessId { get; set; }
    public Guid EmployeeId { get; set; }
    public DocumentRecordKind DocumentKind { get; set; } = DocumentRecordKind.StaffDocument;
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    public HrPrivacyClassification PrivacyClassification { get; set; } = HrPrivacyClassification.Restricted;
    public DateTime? RetentionUntilUtc { get; set; }
    public bool LegalHold { get; set; }
    [MaxLength(8000)] public string? MetadataJson { get; set; }
    public IFormFile? File { get; set; }
}

public sealed class PayrollRuleSetEditVm
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    [Required] public Guid BusinessId { get; set; }
    [Required, MaxLength(16)] public string JurisdictionCode { get; set; } = "DE";
    [Required, MaxLength(64)] public string RuleSetCode { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string DisplayName { get; set; } = string.Empty;
    [Required, MaxLength(64)] public string RuleVersion { get; set; } = string.Empty;
    public PayrollRuleSetStatus Status { get; set; } = PayrollRuleSetStatus.Draft;
    [Required] public DateTime EffectiveFromUtc { get; set; } = DateTime.UtcNow.Date;
    public DateTime? EffectiveToUtc { get; set; }
    [Required, MaxLength(3)] public string Currency { get; set; } = "EUR";
    [MaxLength(1000)] public string? Description { get; set; }
    [MaxLength(8000)] public string? MetadataJson { get; set; }
    public int ComponentCount { get; set; }
    public List<PayrollRuleComponentDto> Components { get; set; } = new();
    public PayrollRuleComponentVm NewComponent { get; set; } = new();
    [ValidateNever] public IEnumerable<SelectListItem> BusinessOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> ComponentTypeOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> CalculationMethodOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> BasisOptions { get; set; } = new List<SelectListItem>();
}

public sealed class PayrollRuleComponentVm
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid BusinessId { get; set; }
    public Guid PayrollRuleSetId { get; set; }
    [Required, MaxLength(64)] public string ComponentCode { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string DisplayName { get; set; } = string.Empty;
    public PayrollRuleComponentType ComponentType { get; set; } = PayrollRuleComponentType.GrossPay;
    public PayrollRuleCalculationMethod CalculationMethod { get; set; } = PayrollRuleCalculationMethod.Percentage;
    public PayrollRuleBasis Basis { get; set; } = PayrollRuleBasis.GrossPay;
    public int? RateBasisPoints { get; set; }
    public long? AmountMinor { get; set; }
    [MaxLength(8000)] public string? ThresholdJson { get; set; }
    public bool IsEmployerCost { get; set; }
    public int SortOrder { get; set; }
    [MaxLength(8000)] public string? MetadataJson { get; set; }
}

public sealed class PayrollRunCreateVm
{
    [Required] public Guid BusinessId { get; set; }
    [Required] public Guid PayrollPeriodId { get; set; }
    [Required] public Guid PayrollRuleSetId { get; set; }
    [MaxLength(1000)] public string? ReviewNotes { get; set; }
    [MaxLength(8000)] public string? MetadataJson { get; set; }
    [ValidateNever] public IEnumerable<SelectListItem> BusinessOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> PayrollPeriodOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> PayrollRuleSetOptions { get; set; } = new List<SelectListItem>();
}

public sealed class PayrollRunDetailVm
{
    public PayrollRunListItemDto Run { get; set; } = new();
    public List<PayrollPayslipListItemDto> Payslips { get; set; } = new();
    public bool PayslipStorageReady { get; set; }
}

public sealed class PayrollPaymentEditVm
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    [Required] public Guid BusinessId { get; set; }
    [Required] public Guid PayrollRunId { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public PayrollPaymentStatus Status { get; set; } = PayrollPaymentStatus.Draft;
    public PayrollPaymentMethod PaymentMethod { get; set; } = PayrollPaymentMethod.BankTransfer;
    [Required] public DateTime PaymentDateUtc { get; set; } = DateTime.UtcNow;
    [Required, MaxLength(3)] public string Currency { get; set; } = "EUR";
    public long TotalAmountMinor { get; set; }
    [MaxLength(256)] public string? Reference { get; set; }
    public Guid? PostingJournalEntryId { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public Guid? ReversalJournalEntryId { get; set; }
    public DateTime? ReversedAtUtc { get; set; }
    [MaxLength(1000)] public string? ReversalReason { get; set; }
    public DateTime? BankSettledAtUtc { get; set; }
    public Guid? BankSettlementJournalEntryId { get; set; }
    public Guid? BankSettlementReconciliationMatchId { get; set; }
    [MaxLength(1000)] public string? BankSettlementNotes { get; set; }
    [MaxLength(4000)] public string? InternalNotes { get; set; }
    [MaxLength(8000)] public string? MetadataJson { get; set; } = "{}";
    public List<PayrollPaymentAllocationDto> Allocations { get; set; } = new();
    public List<PayrollPaymentBankSettlementCandidateDto> BankSettlementCandidates { get; set; } = new();
    public List<PayrollPaymentBankSettlementCandidateDto> BankCorrectionCandidates { get; set; } = new();
    public List<PayrollPaymentBankCorrectionListItemDto> BankCorrections { get; set; } = new();
    [ValidateNever] public IEnumerable<SelectListItem> BusinessOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> PaymentMethodOptions { get; set; } = new List<SelectListItem>();
}

public sealed class DepartmentEditVm
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    [Required] public Guid BusinessId { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    [Required, MaxLength(64)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string DisplayName { get; set; } = string.Empty;
    public DepartmentStatus Status { get; set; } = DepartmentStatus.Active;
    public int SortOrder { get; set; }
    [MaxLength(1000)] public string? Description { get; set; }
    [MaxLength(8000)] public string? MetadataJson { get; set; }
    public string? ParentDepartmentName { get; set; }
    [ValidateNever] public IEnumerable<SelectListItem> BusinessOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> ParentDepartmentOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
}

public sealed class PositionEditVm
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    [Required] public Guid BusinessId { get; set; }
    public Guid? DepartmentId { get; set; }
    [Required, MaxLength(64)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string DisplayName { get; set; } = string.Empty;
    public PositionStatus Status { get; set; } = PositionStatus.Active;
    public int SortOrder { get; set; }
    [MaxLength(1000)] public string? Description { get; set; }
    [MaxLength(8000)] public string? MetadataJson { get; set; }
    public string? DepartmentName { get; set; }
    [ValidateNever] public IEnumerable<SelectListItem> BusinessOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> DepartmentOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
}

public sealed class EmploymentContractEditVm
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    [Required] public Guid BusinessId { get; set; }
    [Required] public Guid EmployeeId { get; set; }
    [Required, MaxLength(100)] public string ContractNumber { get; set; } = string.Empty;
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
    public EmploymentContractStatus Status { get; set; } = EmploymentContractStatus.Draft;
    [Required] public DateTime StartDateUtc { get; set; } = DateTime.UtcNow.Date;
    public DateTime? EndDateUtc { get; set; }
    public int? WeeklyHoursMinor { get; set; }
    public HrPrivacyClassification PrivacyClassification { get; set; } = HrPrivacyClassification.Confidential;
    [MaxLength(4000)] public string? InternalNotes { get; set; }
    [MaxLength(8000)] public string? MetadataJson { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    [ValidateNever] public IEnumerable<SelectListItem> BusinessOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> EmployeeOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> EmploymentTypeOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> PrivacyOptions { get; set; } = new List<SelectListItem>();
}

public sealed class WorkScheduleEditVm
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    [Required] public Guid BusinessId { get; set; }
    [Required] public Guid EmployeeId { get; set; }
    [Required, MaxLength(64)] public string ScheduleCode { get; set; } = string.Empty;
    public WorkScheduleStatus Status { get; set; } = WorkScheduleStatus.Draft;
    [Required] public DateTime EffectiveFromUtc { get; set; } = DateTime.UtcNow.Date;
    public DateTime? EffectiveToUtc { get; set; }
    public int MondayMinutes { get; set; }
    public int TuesdayMinutes { get; set; }
    public int WednesdayMinutes { get; set; }
    public int ThursdayMinutes { get; set; }
    public int FridayMinutes { get; set; }
    public int SaturdayMinutes { get; set; }
    public int SundayMinutes { get; set; }
    [MaxLength(4000)] public string? InternalNotes { get; set; }
    [MaxLength(8000)] public string? MetadataJson { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public List<WorkScheduleExceptionDto> Exceptions { get; set; } = new();
    [ValidateNever] public IEnumerable<SelectListItem> BusinessOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> EmployeeOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
}

public sealed class WorkScheduleExceptionVm
{
    public Guid BusinessId { get; set; }
    public Guid WorkScheduleId { get; set; }
    [Required] public DateTime WorkDateUtc { get; set; } = DateTime.UtcNow.Date;
    public int ScheduledMinutes { get; set; }
    [Required, MaxLength(200)] public string Reason { get; set; } = string.Empty;
    [MaxLength(4000)] public string? InternalNotes { get; set; }
    [MaxLength(8000)] public string? MetadataJson { get; set; }
}

public sealed class AttendanceEventEditVm
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    [Required] public Guid BusinessId { get; set; }
    [Required] public Guid EmployeeId { get; set; }
    public AttendanceEventType EventType { get; set; } = AttendanceEventType.ClockIn;
    [Required] public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(200)] public string? SourceReference { get; set; }
    [MaxLength(4000)] public string? InternalNotes { get; set; }
    [MaxLength(8000)] public string? MetadataJson { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    [ValidateNever] public IEnumerable<SelectListItem> BusinessOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> EmployeeOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> EventTypeOptions { get; set; } = new List<SelectListItem>();
}

public sealed class TimeEntryEditVm
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    [Required] public Guid BusinessId { get; set; }
    [Required] public Guid EmployeeId { get; set; }
    public Guid? WorkScheduleId { get; set; }
    [Required] public DateTime WorkDateUtc { get; set; } = DateTime.UtcNow.Date;
    public int DurationMinutes { get; set; } = 480;
    public int BreakMinutes { get; set; }
    public TimeEntrySource Source { get; set; } = TimeEntrySource.Manual;
    public TimeEntryStatus Status { get; set; } = TimeEntryStatus.Draft;
    [Required, MaxLength(100)] public string WorkType { get; set; } = "REGULAR";
    [MaxLength(1000)] public string? Description { get; set; }
    [MaxLength(1000)] public string? RejectionReason { get; set; }
    [MaxLength(4000)] public string? InternalNotes { get; set; }
    [MaxLength(8000)] public string? MetadataJson { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? ScheduleCode { get; set; }
    [ValidateNever] public IEnumerable<SelectListItem> BusinessOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> EmployeeOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> ScheduleOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> SourceOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
}

public sealed class TimesheetEditVm
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    [Required] public Guid BusinessId { get; set; }
    [Required] public Guid EmployeeId { get; set; }
    [Required, MaxLength(100)] public string TimesheetNumber { get; set; } = string.Empty;
    public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;
    [Required] public DateTime PeriodStartUtc { get; set; } = DateTime.UtcNow.Date;
    [Required] public DateTime PeriodEndUtc { get; set; } = DateTime.UtcNow.Date;
    [MaxLength(1000)] public string? ReviewNotes { get; set; }
    [MaxLength(4000)] public string? InternalNotes { get; set; }
    [MaxLength(8000)] public string? MetadataJson { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int TotalWorkMinutes { get; set; }
    public int TotalBreakMinutes { get; set; }
    public List<Guid> TimeEntryIds { get; set; } = new();
    public List<TimesheetLineDto> Lines { get; set; } = new();
    [ValidateNever] public IEnumerable<SelectListItem> BusinessOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> EmployeeOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> TimeEntryOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
}

public sealed class LeaveRequestEditVm
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    [Required] public Guid BusinessId { get; set; }
    [Required] public Guid EmployeeId { get; set; }
    [Required, MaxLength(100)] public string RequestNumber { get; set; } = string.Empty;
    public LeaveType LeaveType { get; set; } = LeaveType.Vacation;
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Draft;
    [Required] public DateTime StartDateUtc { get; set; } = DateTime.UtcNow.Date;
    [Required] public DateTime EndDateUtc { get; set; } = DateTime.UtcNow.Date;
    public int RequestedMinutes { get; set; } = 480;
    [MaxLength(1000)] public string? ReviewNotes { get; set; }
    public HrPrivacyClassification PrivacyClassification { get; set; } = HrPrivacyClassification.Confidential;
    [MaxLength(4000)] public string? InternalNotes { get; set; }
    [MaxLength(8000)] public string? MetadataJson { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    [ValidateNever] public IEnumerable<SelectListItem> BusinessOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> EmployeeOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> LeaveTypeOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> PrivacyOptions { get; set; } = new List<SelectListItem>();
}

public sealed class AbsenceRecordEditVm
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    [Required] public Guid BusinessId { get; set; }
    [Required] public Guid EmployeeId { get; set; }
    public Guid? LeaveRequestId { get; set; }
    public LeaveType AbsenceType { get; set; } = LeaveType.Vacation;
    public AbsenceStatus Status { get; set; } = AbsenceStatus.Draft;
    [Required] public DateTime StartDateUtc { get; set; } = DateTime.UtcNow.Date;
    [Required] public DateTime EndDateUtc { get; set; } = DateTime.UtcNow.Date;
    public int AbsenceMinutes { get; set; } = 480;
    public HrPrivacyClassification PrivacyClassification { get; set; } = HrPrivacyClassification.Confidential;
    [MaxLength(4000)] public string? InternalNotes { get; set; }
    [MaxLength(8000)] public string? MetadataJson { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? LeaveRequestNumber { get; set; }
    [ValidateNever] public IEnumerable<SelectListItem> BusinessOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> EmployeeOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> LeaveRequestOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> AbsenceTypeOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> PrivacyOptions { get; set; } = new List<SelectListItem>();
}

public sealed class PayrollPeriodEditVm
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    [Required] public Guid BusinessId { get; set; }
    [Required, MaxLength(100)] public string PeriodCode { get; set; } = string.Empty;
    public PayrollPeriodStatus Status { get; set; } = PayrollPeriodStatus.Draft;
    [Required] public DateTime PeriodStartUtc { get; set; } = DateTime.UtcNow.Date;
    [Required] public DateTime PeriodEndUtc { get; set; } = DateTime.UtcNow.Date;
    public int EmployeeCount { get; set; }
    public int TotalWorkMinutes { get; set; }
    public int TotalBreakMinutes { get; set; }
    public int TotalAbsenceMinutes { get; set; }
    [MaxLength(1000)] public string? ReviewNotes { get; set; }
    [MaxLength(4000)] public string? InternalNotes { get; set; }
    [MaxLength(8000)] public string? MetadataJson { get; set; }
    public List<PayrollPeriodLineDto> Lines { get; set; } = new();
    [ValidateNever] public IEnumerable<SelectListItem> BusinessOptions { get; set; } = new List<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
}
