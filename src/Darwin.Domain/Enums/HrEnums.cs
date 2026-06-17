namespace Darwin.Domain.Enums
{
    /// <summary>
    /// Represents whether an employee record can be used in HR workflows.
    /// </summary>
    public enum EmployeeStatus : short
    {
        Active = 0,
        Inactive = 1,
        Archived = 2
    }

    /// <summary>
    /// Represents whether a department can be assigned to employees.
    /// </summary>
    public enum DepartmentStatus : short
    {
        Active = 0,
        Inactive = 1,
        Archived = 2
    }

    /// <summary>
    /// Represents whether a position can be assigned to employees.
    /// </summary>
    public enum PositionStatus : short
    {
        Active = 0,
        Inactive = 1,
        Archived = 2
    }

    /// <summary>
    /// Represents employment relationship type for HR metadata.
    /// </summary>
    public enum EmploymentType : short
    {
        FullTime = 0,
        PartTime = 1,
        Temporary = 2,
        Contractor = 3,
        Intern = 4,
        Other = 5
    }

    /// <summary>
    /// Represents lifecycle state for an employment contract metadata record.
    /// </summary>
    public enum EmploymentContractStatus : short
    {
        Draft = 0,
        Active = 1,
        Ended = 2,
        Archived = 3
    }

    /// <summary>
    /// Privacy classification for HR document metadata and personnel references.
    /// </summary>
    public enum HrPrivacyClassification : short
    {
        Internal = 0,
        Confidential = 1,
        Restricted = 2
    }

    /// <summary>
    /// Represents whether a recurring work schedule can be used for time tracking.
    /// </summary>
    public enum WorkScheduleStatus : short
    {
        Draft = 0,
        Active = 1,
        Inactive = 2,
        Archived = 3
    }

    /// <summary>
    /// Represents attendance event direction for internal HR time evidence.
    /// </summary>
    public enum AttendanceEventType : short
    {
        ClockIn = 0,
        ClockOut = 1,
        BreakStart = 2,
        BreakEnd = 3,
        ManualCorrection = 4
    }

    /// <summary>
    /// Represents lifecycle state for a time entry.
    /// </summary>
    public enum TimeEntryStatus : short
    {
        Draft = 0,
        Submitted = 1,
        Approved = 2,
        Rejected = 3,
        Cancelled = 4
    }

    /// <summary>
    /// Represents whether time was entered manually or from attendance evidence.
    /// </summary>
    public enum TimeEntrySource : short
    {
        Manual = 0,
        Attendance = 1,
        Import = 2
    }

    /// <summary>
    /// Represents the approval workflow state for a payroll-period-facing timesheet.
    /// </summary>
    public enum TimesheetStatus : short
    {
        Draft = 0,
        Submitted = 1,
        InReview = 2,
        Approved = 3,
        Rejected = 4,
        Cancelled = 5
    }

    /// <summary>
    /// High-level leave category. Sensitive details stay out of metadata and audit payloads.
    /// </summary>
    public enum LeaveType : short
    {
        Vacation = 0,
        Sick = 1,
        Personal = 2,
        Unpaid = 3,
        Other = 4
    }

    /// <summary>
    /// Lifecycle for employee leave requests.
    /// </summary>
    public enum LeaveRequestStatus : short
    {
        Draft = 0,
        Submitted = 1,
        InReview = 2,
        Approved = 3,
        Rejected = 4,
        Cancelled = 5
    }

    /// <summary>
    /// Lifecycle for formal absence evidence.
    /// </summary>
    public enum AbsenceStatus : short
    {
        Draft = 0,
        Confirmed = 1,
        Cancelled = 2
    }

    /// <summary>
    /// Lifecycle for export-ready payroll period summaries. This is not legal payroll.
    /// </summary>
    public enum PayrollPeriodStatus : short
    {
        Draft = 0,
        Prepared = 1,
        Reviewed = 2,
        Approved = 3,
        Cancelled = 4
    }

    /// <summary>
    /// Lifecycle for legal payroll rule sets. Rule sets define versioned inputs; they do not run payroll.
    /// </summary>
    public enum PayrollRuleSetStatus : short
    {
        Draft = 0,
        Active = 1,
        Archived = 2
    }

    /// <summary>
    /// High-level payroll rule component purpose.
    /// </summary>
    public enum PayrollRuleComponentType : short
    {
        GrossPay = 0,
        TaxWithholding = 1,
        SocialInsuranceEmployee = 2,
        SocialInsuranceEmployer = 3,
        Deduction = 4,
        Allowance = 5,
        EmployerCost = 6
    }

    /// <summary>
    /// Defines how a payroll rule component is interpreted by the later calculation engine.
    /// </summary>
    public enum PayrollRuleCalculationMethod : short
    {
        FixedAmount = 0,
        Percentage = 1,
        ThresholdTable = 2,
        Reference = 3
    }

    /// <summary>
    /// Defines the calculation basis for a payroll rule component.
    /// </summary>
    public enum PayrollRuleBasis : short
    {
        GrossPay = 0,
        TaxableIncome = 1,
        HoursWorked = 2,
        ContractRate = 3,
        EmployerCost = 4
    }

    /// <summary>
    /// Lifecycle for payroll runs. Runs snapshot and evaluate configured rules; they do not produce payslips or postings in this slice.
    /// </summary>
    public enum PayrollRunStatus : short
    {
        Draft = 0,
        Calculated = 1,
        Reviewed = 2,
        Approved = 3,
        Cancelled = 4,
        Posted = 5
    }

    /// <summary>
    /// Lifecycle for immutable payroll payslip artifacts generated from approved payroll runs.
    /// </summary>
    public enum PayrollPayslipStatus : short
    {
        Generated = 0,
        Archived = 1
    }

    /// <summary>
    /// Lifecycle for internal payroll salary payment records.
    /// </summary>
    public enum PayrollPaymentStatus : short
    {
        Draft = 0,
        Posted = 1,
        Cancelled = 2,
        Reversed = 3
    }

    /// <summary>
    /// Operator-facing salary payment method metadata. Accounting still depends on finance posting.
    /// </summary>
    public enum PayrollPaymentMethod : short
    {
        BankTransfer = 0,
        Cash = 1,
        Card = 2,
        DirectDebit = 3,
        Other = 4
    }

    /// <summary>
    /// Evidence category for correcting a bank-settled salary payment.
    /// </summary>
    public enum PayrollPaymentBankCorrectionType : short
    {
        ReturnedTransfer = 0,
        DuplicatePayment = 1
    }

    /// <summary>
    /// Lifecycle for payroll payment bank correction records.
    /// </summary>
    public enum PayrollPaymentBankCorrectionStatus : short
    {
        Draft = 0,
        Posted = 1,
        Cancelled = 2
    }
}
