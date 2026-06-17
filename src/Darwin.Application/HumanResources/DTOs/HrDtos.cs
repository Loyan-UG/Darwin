using Darwin.Domain.Enums;

namespace Darwin.Application.HumanResources.DTOs;

public enum EmployeeQueueFilter
{
    All = 0,
    Active = 1,
    Inactive = 2,
    Archived = 3,
    LinkedToBusinessMember = 4,
    MissingBusinessMember = 5
}

public enum DepartmentQueueFilter
{
    All = 0,
    Active = 1,
    Inactive = 2,
    Archived = 3
}

public enum PositionQueueFilter
{
    All = 0,
    Active = 1,
    Inactive = 2,
    Archived = 3
}

public enum EmploymentContractQueueFilter
{
    All = 0,
    Draft = 1,
    Active = 2,
    Ended = 3,
    Archived = 4
}

public class EmployeeEditDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid BusinessId { get; set; }
    public Guid? BusinessMemberId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PreferredName { get; set; }
    public string? WorkEmail { get; set; }
    public string? WorkPhone { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public DateTime? HireDateUtc { get; set; }
    public DateTime? TerminationDateUtc { get; set; }
    public HrPrivacyClassification PrivacyClassification { get; set; } = HrPrivacyClassification.Confidential;
    public string? InternalNotes { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class EmployeeListItemDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid BusinessId { get; set; }
    public Guid? BusinessMemberId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? WorkEmail { get; set; }
    public string? DepartmentName { get; set; }
    public string? PositionName { get; set; }
    public EmployeeStatus Status { get; set; }
    public HrPrivacyClassification PrivacyClassification { get; set; }
}

public sealed class EmployeeDetailDto : EmployeeEditDto
{
    public string? DepartmentName { get; set; }
    public string? PositionName { get; set; }
    public List<EmploymentContractListItemDto> Contracts { get; set; } = new();
}

public class DepartmentEditDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid BusinessId { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DepartmentStatus Status { get; set; } = DepartmentStatus.Active;
    public int SortOrder { get; set; }
    public string? Description { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class DepartmentListItemDto : DepartmentEditDto
{
    public string? ParentDepartmentName { get; set; }
}

public class PositionEditDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid BusinessId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public PositionStatus Status { get; set; } = PositionStatus.Active;
    public int SortOrder { get; set; }
    public string? Description { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class PositionListItemDto : PositionEditDto
{
    public string? DepartmentName { get; set; }
}

public class EmploymentContractEditDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid BusinessId { get; set; }
    public Guid EmployeeId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
    public EmploymentContractStatus Status { get; set; } = EmploymentContractStatus.Draft;
    public DateTime StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }
    public int? WeeklyHoursMinor { get; set; }
    public HrPrivacyClassification PrivacyClassification { get; set; } = HrPrivacyClassification.Confidential;
    public string? InternalNotes { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class EmploymentContractListItemDto : EmploymentContractEditDto
{
    public string EmployeeName { get; set; } = string.Empty;
}

public sealed class HrArchiveDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public enum WorkScheduleQueueFilter
{
    All = 0,
    Draft = 1,
    Active = 2,
    Inactive = 3,
    Archived = 4
}

public enum AttendanceEventQueueFilter
{
    All = 0,
    ClockIn = 1,
    ClockOut = 2,
    BreakStart = 3,
    BreakEnd = 4,
    ManualCorrection = 5
}

public enum TimeEntryQueueFilter
{
    All = 0,
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Rejected = 4,
    Cancelled = 5
}

public enum TimesheetQueueFilter
{
    All = 0,
    Draft = 1,
    Submitted = 2,
    InReview = 3,
    Approved = 4,
    Rejected = 5,
    Cancelled = 6
}

public class WorkScheduleEditDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid BusinessId { get; set; }
    public Guid EmployeeId { get; set; }
    public string ScheduleCode { get; set; } = string.Empty;
    public WorkScheduleStatus Status { get; set; } = WorkScheduleStatus.Draft;
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public int MondayMinutes { get; set; }
    public int TuesdayMinutes { get; set; }
    public int WednesdayMinutes { get; set; }
    public int ThursdayMinutes { get; set; }
    public int FridayMinutes { get; set; }
    public int SaturdayMinutes { get; set; }
    public int SundayMinutes { get; set; }
    public string? InternalNotes { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class WorkScheduleListItemDto : WorkScheduleEditDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public int WeeklyMinutes => MondayMinutes + TuesdayMinutes + WednesdayMinutes + ThursdayMinutes + FridayMinutes + SaturdayMinutes + SundayMinutes;
    public List<WorkScheduleExceptionDto> Exceptions { get; set; } = new();
}

public class WorkScheduleExceptionDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid BusinessId { get; set; }
    public Guid WorkScheduleId { get; set; }
    public DateTime WorkDateUtc { get; set; }
    public int ScheduledMinutes { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? InternalNotes { get; set; }
    public string? MetadataJson { get; set; }
}

public class AttendanceEventEditDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid BusinessId { get; set; }
    public Guid EmployeeId { get; set; }
    public AttendanceEventType EventType { get; set; } = AttendanceEventType.ClockIn;
    public DateTime OccurredAtUtc { get; set; }
    public string? SourceReference { get; set; }
    public string? InternalNotes { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class AttendanceEventListItemDto : AttendanceEventEditDto
{
    public string EmployeeName { get; set; } = string.Empty;
}

public class TimeEntryEditDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid BusinessId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? WorkScheduleId { get; set; }
    public DateTime WorkDateUtc { get; set; }
    public int DurationMinutes { get; set; }
    public int BreakMinutes { get; set; }
    public TimeEntrySource Source { get; set; } = TimeEntrySource.Manual;
    public TimeEntryStatus Status { get; set; } = TimeEntryStatus.Draft;
    public string WorkType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RejectionReason { get; set; }
    public string? InternalNotes { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class TimeEntryListItemDto : TimeEntryEditDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public string? ScheduleCode { get; set; }
}

public class TimesheetEditDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid BusinessId { get; set; }
    public Guid EmployeeId { get; set; }
    public string TimesheetNumber { get; set; } = string.Empty;
    public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public string? ReviewNotes { get; set; }
    public string? InternalNotes { get; set; }
    public string? MetadataJson { get; set; }
    public List<Guid> TimeEntryIds { get; set; } = new();
}

public sealed class TimesheetListItemDto : TimesheetEditDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public int TotalWorkMinutes { get; set; }
    public int TotalBreakMinutes { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public List<TimesheetLineDto> Lines { get; set; } = new();
}

public sealed class TimesheetLineDto
{
    public Guid Id { get; set; }
    public Guid TimeEntryId { get; set; }
    public DateTime WorkDateUtc { get; set; }
    public int DurationMinutes { get; set; }
    public int BreakMinutes { get; set; }
    public string WorkType { get; set; } = string.Empty;
}

public sealed class HrTimeLifecycleDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public string? Notes { get; set; }
}

public enum LeaveRequestQueueFilter
{
    All = 0,
    Draft = 1,
    Submitted = 2,
    InReview = 3,
    Approved = 4,
    Rejected = 5,
    Cancelled = 6
}

public enum AbsenceRecordQueueFilter
{
    All = 0,
    Draft = 1,
    Confirmed = 2,
    Cancelled = 3
}

public class LeaveRequestEditDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid BusinessId { get; set; }
    public Guid EmployeeId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public LeaveType LeaveType { get; set; } = LeaveType.Vacation;
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Draft;
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public int RequestedMinutes { get; set; }
    public string? ReviewNotes { get; set; }
    public HrPrivacyClassification PrivacyClassification { get; set; } = HrPrivacyClassification.Confidential;
    public string? InternalNotes { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class LeaveRequestListItemDto : LeaveRequestEditDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}

public class AbsenceRecordEditDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid BusinessId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? LeaveRequestId { get; set; }
    public LeaveType AbsenceType { get; set; } = LeaveType.Vacation;
    public AbsenceStatus Status { get; set; } = AbsenceStatus.Draft;
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public int AbsenceMinutes { get; set; }
    public HrPrivacyClassification PrivacyClassification { get; set; } = HrPrivacyClassification.Confidential;
    public string? InternalNotes { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class AbsenceRecordListItemDto : AbsenceRecordEditDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public string? LeaveRequestNumber { get; set; }
}

public enum PayrollPeriodQueueFilter
{
    All = 0,
    Draft = 1,
    Prepared = 2,
    Reviewed = 3,
    Approved = 4,
    Cancelled = 5
}

public class PayrollPeriodEditDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid BusinessId { get; set; }
    public string PeriodCode { get; set; } = string.Empty;
    public PayrollPeriodStatus Status { get; set; } = PayrollPeriodStatus.Draft;
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public string? ReviewNotes { get; set; }
    public string? InternalNotes { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class PayrollPeriodListItemDto : PayrollPeriodEditDto
{
    public int EmployeeCount { get; set; }
    public int TotalWorkMinutes { get; set; }
    public int TotalBreakMinutes { get; set; }
    public int TotalAbsenceMinutes { get; set; }
    public DateTime? PreparedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public List<PayrollPeriodLineDto> Lines { get; set; } = new();
}

public sealed class PayrollPeriodLineDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public int WorkMinutes { get; set; }
    public int BreakMinutes { get; set; }
    public int AbsenceMinutes { get; set; }
    public int ApprovedTimesheetCount { get; set; }
    public int ConfirmedAbsenceCount { get; set; }
}
