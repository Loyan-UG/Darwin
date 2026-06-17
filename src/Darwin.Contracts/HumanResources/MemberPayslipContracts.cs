using Darwin.Contracts.Common;

namespace Darwin.Contracts.HumanResources;

/// <summary>
/// Employee-facing payslip summary exposed through the member self-service surface.
/// </summary>
public sealed class MemberPayslipSummary
{
    /// <summary>Gets or sets the payslip identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the business identifier.</summary>
    public Guid BusinessId { get; set; }

    /// <summary>Gets or sets the business display name.</summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>Gets or sets the employee identifier linked to the authenticated member.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Gets or sets the employee number snapshot.</summary>
    public string EmployeeNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the official payslip number.</summary>
    public string PayslipNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the ISO currency code.</summary>
    public string Currency { get; set; } = ContractDefaults.DefaultCurrency;

    /// <summary>Gets or sets the payroll period start date in UTC.</summary>
    public DateTime PeriodStartUtc { get; set; }

    /// <summary>Gets or sets the payroll period end date in UTC.</summary>
    public DateTime PeriodEndUtc { get; set; }

    /// <summary>Gets or sets the gross pay amount in minor units.</summary>
    public long GrossPayMinor { get; set; }

    /// <summary>Gets or sets the employee deduction amount in minor units.</summary>
    public long EmployeeDeductionMinor { get; set; }

    /// <summary>Gets or sets the net pay amount in minor units.</summary>
    public long NetPayMinor { get; set; }

    /// <summary>Gets or sets the payslip artifact status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the high-level employee-visible payment status.</summary>
    public string PaymentStatus { get; set; } = string.Empty;

    /// <summary>Gets or sets the generated timestamp in UTC.</summary>
    public DateTime GeneratedAtUtc { get; set; }

    /// <summary>Gets or sets the official PDF download path.</summary>
    public string DocumentPath { get; set; } = string.Empty;
}
