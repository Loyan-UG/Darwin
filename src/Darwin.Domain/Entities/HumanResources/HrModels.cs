using System;
using System.Collections.Generic;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.HumanResources
{
    /// <summary>
    /// Formal HR/personnel record. Access and permissions remain owned by BusinessMember.
    /// </summary>
    public sealed class Employee : BaseEntity
    {
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
        public List<EmploymentContract> EmploymentContracts { get; set; } = new();
    }

    /// <summary>
    /// HR organization department. This does not grant system permissions.
    /// </summary>
    public sealed class Department : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid? ParentDepartmentId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DepartmentStatus Status { get; set; } = DepartmentStatus.Active;
        public int SortOrder { get; set; }
        public string? Description { get; set; }
        public string? MetadataJson { get; set; }
        public List<Department> Children { get; set; } = new();
    }

    /// <summary>
    /// HR job position. This is separate from identity/security roles.
    /// </summary>
    public sealed class Position : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid? DepartmentId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public PositionStatus Status { get; set; } = PositionStatus.Active;
        public int SortOrder { get; set; }
        public string? Description { get; set; }
        public string? MetadataJson { get; set; }
    }

    /// <summary>
    /// HR employment contract metadata. Legal payroll calculation is owned by later payroll slices.
    /// </summary>
    public sealed class EmploymentContract : BaseEntity
    {
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

    /// <summary>
    /// Weekly internal HR work schedule. It is time evidence, not payroll calculation.
    /// </summary>
    public sealed class WorkSchedule : BaseEntity
    {
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
        public List<WorkScheduleException> Exceptions { get; set; } = new();
    }

    /// <summary>
    /// Date-specific schedule exception. Leave and absence remain owned by a later slice.
    /// </summary>
    public sealed class WorkScheduleException : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid WorkScheduleId { get; set; }
        public DateTime WorkDateUtc { get; set; }
        public int ScheduledMinutes { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? InternalNotes { get; set; }
        public string? MetadataJson { get; set; }
    }

    /// <summary>
    /// Attendance fact for internal HR evidence. Native/mobile clock-in is not introduced here.
    /// </summary>
    public sealed class AttendanceEvent : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid EmployeeId { get; set; }
        public AttendanceEventType EventType { get; set; } = AttendanceEventType.ClockIn;
        public DateTime OccurredAtUtc { get; set; }
        public string? SourceReference { get; set; }
        public string? InternalNotes { get; set; }
        public string? MetadataJson { get; set; }
    }

    /// <summary>
    /// Raw time entry. Calculations can read it, but payroll/legal liability is outside this slice.
    /// </summary>
    public sealed class TimeEntry : BaseEntity
    {
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

    /// <summary>
    /// Period approval container for employee time entries.
    /// </summary>
    public sealed class Timesheet : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid EmployeeId { get; set; }
        public string TimesheetNumber { get; set; } = string.Empty;
        public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;
        public DateTime PeriodStartUtc { get; set; }
        public DateTime PeriodEndUtc { get; set; }
        public int TotalWorkMinutes { get; set; }
        public int TotalBreakMinutes { get; set; }
        public DateTime? SubmittedAtUtc { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public Guid? ReviewedByUserId { get; set; }
        public string? ReviewNotes { get; set; }
        public string? InternalNotes { get; set; }
        public string? MetadataJson { get; set; }
        public List<TimesheetLine> Lines { get; set; } = new();
    }

    /// <summary>
    /// Explicit link between a timesheet period and the time entries it approves.
    /// </summary>
    public sealed class TimesheetLine : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid TimesheetId { get; set; }
        public Guid TimeEntryId { get; set; }
        public DateTime WorkDateUtc { get; set; }
        public int DurationMinutes { get; set; }
        public int BreakMinutes { get; set; }
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// Employee leave request with explicit approval state.
    /// </summary>
    public sealed class LeaveRequest : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid EmployeeId { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public LeaveType LeaveType { get; set; } = LeaveType.Vacation;
        public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Draft;
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }
        public int RequestedMinutes { get; set; }
        public DateTime? SubmittedAtUtc { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public Guid? ReviewedByUserId { get; set; }
        public string? ReviewNotes { get; set; }
        public HrPrivacyClassification PrivacyClassification { get; set; } = HrPrivacyClassification.Confidential;
        public string? InternalNotes { get; set; }
        public string? MetadataJson { get; set; }
    }

    /// <summary>
    /// Formal absence fact derived from approved leave or entered as internal HR evidence.
    /// </summary>
    public sealed class AbsenceRecord : BaseEntity
    {
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

    /// <summary>
    /// Export-ready HR/payroll summary period. It does not calculate legal payroll.
    /// </summary>
    public sealed class PayrollPeriod : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public string PeriodCode { get; set; } = string.Empty;
        public PayrollPeriodStatus Status { get; set; } = PayrollPeriodStatus.Draft;
        public DateTime PeriodStartUtc { get; set; }
        public DateTime PeriodEndUtc { get; set; }
        public int EmployeeCount { get; set; }
        public int TotalWorkMinutes { get; set; }
        public int TotalBreakMinutes { get; set; }
        public int TotalAbsenceMinutes { get; set; }
        public DateTime? PreparedAtUtc { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }
        public string? ReviewNotes { get; set; }
        public string? InternalNotes { get; set; }
        public string? MetadataJson { get; set; }
        public List<PayrollPeriodLine> Lines { get; set; } = new();
    }

    /// <summary>
    /// Employee-level period summary from approved time and confirmed absence facts.
    /// </summary>
    public sealed class PayrollPeriodLine : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid PayrollPeriodId { get; set; }
        public Guid EmployeeId { get; set; }
        public int WorkMinutes { get; set; }
        public int BreakMinutes { get; set; }
        public int AbsenceMinutes { get; set; }
        public int ApprovedTimesheetCount { get; set; }
        public int ConfirmedAbsenceCount { get; set; }
        public string? SummaryJson { get; set; }
    }

    /// <summary>
    /// Versioned legal payroll rule foundation. It stores rule definitions, not payroll run output.
    /// </summary>
    public sealed class PayrollRuleSet : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public string JurisdictionCode { get; set; } = "DE";
        public string RuleSetCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string RuleVersion { get; set; } = string.Empty;
        public PayrollRuleSetStatus Status { get; set; } = PayrollRuleSetStatus.Draft;
        public DateTime EffectiveFromUtc { get; set; }
        public DateTime? EffectiveToUtc { get; set; }
        public string Currency { get; set; } = "EUR";
        public string? Description { get; set; }
        public string? MetadataJson { get; set; }
        public List<PayrollRuleComponent> Components { get; set; } = new();
    }

    /// <summary>
    /// Versioned payroll rule component. The later calculation engine consumes these definitions.
    /// </summary>
    public sealed class PayrollRuleComponent : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid PayrollRuleSetId { get; set; }
        public string ComponentCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public PayrollRuleComponentType ComponentType { get; set; } = PayrollRuleComponentType.GrossPay;
        public PayrollRuleCalculationMethod CalculationMethod { get; set; } = PayrollRuleCalculationMethod.Percentage;
        public PayrollRuleBasis Basis { get; set; } = PayrollRuleBasis.GrossPay;
        public int? RateBasisPoints { get; set; }
        public long? AmountMinor { get; set; }
        public string? ThresholdJson { get; set; }
        public bool IsEmployerCost { get; set; }
        public int SortOrder { get; set; }
        public string? MetadataJson { get; set; }
    }

    /// <summary>
    /// Internal payroll run over approved period summaries and configured rule snapshots.
    /// </summary>
    public sealed class PayrollRun : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid PayrollPeriodId { get; set; }
        public Guid PayrollRuleSetId { get; set; }
        public string RunNumber { get; set; } = string.Empty;
        public PayrollRunStatus Status { get; set; } = PayrollRunStatus.Draft;
        public string JurisdictionCode { get; set; } = "DE";
        public string RuleSetCode { get; set; } = string.Empty;
        public string RuleVersion { get; set; } = string.Empty;
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
        public List<PayrollRunLine> Lines { get; set; } = new();
    }

    /// <summary>
    /// Employee-level payroll run snapshot. It is internal calculation evidence, not a payslip.
    /// </summary>
    public sealed class PayrollRunLine : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid PayrollRunId { get; set; }
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
        public string? EmployeeSnapshotJson { get; set; }
        public string? ContractSnapshotJson { get; set; }
        public string? PeriodLineSnapshotJson { get; set; }
        public List<PayrollRunLineComponent> Components { get; set; } = new();
    }

    /// <summary>
    /// Payroll run component result from a configured rule component snapshot.
    /// </summary>
    public sealed class PayrollRunLineComponent : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid PayrollRunId { get; set; }
        public Guid PayrollRunLineId { get; set; }
        public Guid PayrollRuleComponentId { get; set; }
        public string ComponentCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public PayrollRuleComponentType ComponentType { get; set; } = PayrollRuleComponentType.GrossPay;
        public PayrollRuleCalculationMethod CalculationMethod { get; set; } = PayrollRuleCalculationMethod.Percentage;
        public PayrollRuleBasis Basis { get; set; } = PayrollRuleBasis.GrossPay;
        public long AmountMinor { get; set; }
        public bool IsEmployerCost { get; set; }
        public int SortOrder { get; set; }
        public string? RuleSnapshotJson { get; set; }
    }

    /// <summary>
    /// Immutable internal payslip artifact metadata generated from an approved payroll run line.
    /// </summary>
    public sealed class PayrollPayslip : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid PayrollRunId { get; set; }
        public Guid PayrollRunLineId { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid DocumentRecordId { get; set; }
        public string PayslipNumber { get; set; } = string.Empty;
        public PayrollPayslipStatus Status { get; set; } = PayrollPayslipStatus.Generated;
        public string Currency { get; set; } = "EUR";
        public DateTime PeriodStartUtc { get; set; }
        public DateTime PeriodEndUtc { get; set; }
        public long GrossPayMinor { get; set; }
        public long EmployeeDeductionMinor { get; set; }
        public long EmployerCostMinor { get; set; }
        public long NetPayMinor { get; set; }
        public DateTime GeneratedAtUtc { get; set; }
        public string? SnapshotJson { get; set; }
        public string? MetadataJson { get; set; }
    }

    /// <summary>
    /// Internal salary payment over posted payroll run liabilities.
    /// </summary>
    public sealed class PayrollPayment : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid PayrollRunId { get; set; }
        public string? PaymentNumber { get; set; }
        public PayrollPaymentStatus Status { get; set; } = PayrollPaymentStatus.Draft;
        public PayrollPaymentMethod PaymentMethod { get; set; } = PayrollPaymentMethod.BankTransfer;
        public DateTime PaymentDateUtc { get; set; }
        public string Currency { get; set; } = "EUR";
        public long TotalAmountMinor { get; set; }
        public string? Reference { get; set; }
        public Guid? PostingJournalEntryId { get; set; }
        public DateTime? PostedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
        public Guid? ReversalJournalEntryId { get; set; }
        public DateTime? ReversedAtUtc { get; set; }
        public string? ReversalReason { get; set; }
        public DateTime? BankSettledAtUtc { get; set; }
        public Guid? BankSettlementJournalEntryId { get; set; }
        public Guid? BankSettlementReconciliationMatchId { get; set; }
        public string? BankSettlementNotes { get; set; }
        public string? InternalNotes { get; set; }
        public string? MetadataJson { get; set; }
        public List<PayrollPaymentAllocation> Allocations { get; set; } = new();
    }

    /// <summary>
    /// Employee-level salary payment allocation against a payroll run line.
    /// </summary>
    public sealed class PayrollPaymentAllocation : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid PayrollPaymentId { get; set; }
        public Guid PayrollRunId { get; set; }
        public Guid PayrollRunLineId { get; set; }
        public Guid EmployeeId { get; set; }
        public long AmountMinor { get; set; }
        public string? Memo { get; set; }
    }

    /// <summary>
    /// Evidence-backed correction for a bank-settled payroll payment.
    /// </summary>
    public sealed class PayrollPaymentBankCorrection : BaseEntity
    {
        public Guid BusinessId { get; set; }
        public Guid PayrollPaymentId { get; set; }
        public Guid BankReconciliationMatchId { get; set; }
        public Guid? BankStatementLineId { get; set; }
        public Guid? OriginalBankSettlementJournalEntryId { get; set; }
        public Guid? CorrectionJournalEntryId { get; set; }
        public PayrollPaymentBankCorrectionType CorrectionType { get; set; } = PayrollPaymentBankCorrectionType.ReturnedTransfer;
        public PayrollPaymentBankCorrectionStatus Status { get; set; } = PayrollPaymentBankCorrectionStatus.Draft;
        public DateTime CorrectionDateUtc { get; set; }
        public DateTime? PostedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
        public string Currency { get; set; } = "EUR";
        public long AmountMinor { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? InternalNotes { get; set; }
        public string? MetadataJson { get; set; } = "{}";
    }
}
