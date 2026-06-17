using Darwin.Application.HumanResources.DTOs;
using FluentValidation;

namespace Darwin.Application.HumanResources.Validators;

public sealed class EmployeeEditDtoValidator : AbstractValidator<EmployeeEditDto>
{
    public EmployeeEditDtoValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.EmployeeNumber).NotEmpty().MaximumLength(64);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PreferredName).MaximumLength(100);
        RuleFor(x => x.WorkEmail).MaximumLength(254).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.WorkEmail));
        RuleFor(x => x.WorkPhone).MaximumLength(64);
        RuleFor(x => x.InternalNotes).MaximumLength(4000);
        RuleFor(x => x.MetadataJson).MaximumLength(8000);
        RuleFor(x => x.TerminationDateUtc)
            .GreaterThanOrEqualTo(x => x.HireDateUtc)
            .When(x => x.HireDateUtc.HasValue && x.TerminationDateUtc.HasValue);
    }
}

public sealed class DepartmentEditDtoValidator : AbstractValidator<DepartmentEditDto>
{
    public DepartmentEditDtoValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.MetadataJson).MaximumLength(8000);
    }
}

public sealed class PositionEditDtoValidator : AbstractValidator<PositionEditDto>
{
    public PositionEditDtoValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.MetadataJson).MaximumLength(8000);
    }
}

public sealed class EmploymentContractEditDtoValidator : AbstractValidator<EmploymentContractEditDto>
{
    public EmploymentContractEditDtoValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ContractNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StartDateUtc).NotEmpty();
        RuleFor(x => x.EndDateUtc)
            .GreaterThanOrEqualTo(x => x.StartDateUtc)
            .When(x => x.EndDateUtc.HasValue);
        RuleFor(x => x.WeeklyHoursMinor).GreaterThan(0).When(x => x.WeeklyHoursMinor.HasValue);
        RuleFor(x => x.InternalNotes).MaximumLength(4000);
        RuleFor(x => x.MetadataJson).MaximumLength(8000);
    }
}

public sealed class WorkScheduleEditDtoValidator : AbstractValidator<WorkScheduleEditDto>
{
    public WorkScheduleEditDtoValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ScheduleCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.EffectiveFromUtc).NotEmpty();
        RuleFor(x => x.EffectiveToUtc).GreaterThanOrEqualTo(x => x.EffectiveFromUtc).When(x => x.EffectiveToUtc.HasValue);
        RuleFor(x => x.MondayMinutes).InclusiveBetween(0, 1440);
        RuleFor(x => x.TuesdayMinutes).InclusiveBetween(0, 1440);
        RuleFor(x => x.WednesdayMinutes).InclusiveBetween(0, 1440);
        RuleFor(x => x.ThursdayMinutes).InclusiveBetween(0, 1440);
        RuleFor(x => x.FridayMinutes).InclusiveBetween(0, 1440);
        RuleFor(x => x.SaturdayMinutes).InclusiveBetween(0, 1440);
        RuleFor(x => x.SundayMinutes).InclusiveBetween(0, 1440);
        RuleFor(x => x.InternalNotes).MaximumLength(4000);
        RuleFor(x => x.MetadataJson).MaximumLength(8000);
    }
}

public sealed class WorkScheduleExceptionDtoValidator : AbstractValidator<WorkScheduleExceptionDto>
{
    public WorkScheduleExceptionDtoValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.WorkScheduleId).NotEmpty();
        RuleFor(x => x.WorkDateUtc).NotEmpty();
        RuleFor(x => x.ScheduledMinutes).InclusiveBetween(0, 1440);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
        RuleFor(x => x.InternalNotes).MaximumLength(4000);
        RuleFor(x => x.MetadataJson).MaximumLength(8000);
    }
}

public sealed class AttendanceEventEditDtoValidator : AbstractValidator<AttendanceEventEditDto>
{
    public AttendanceEventEditDtoValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.OccurredAtUtc).NotEmpty();
        RuleFor(x => x.SourceReference).MaximumLength(200);
        RuleFor(x => x.InternalNotes).MaximumLength(4000);
        RuleFor(x => x.MetadataJson).MaximumLength(8000);
    }
}

public sealed class TimeEntryEditDtoValidator : AbstractValidator<TimeEntryEditDto>
{
    public TimeEntryEditDtoValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.WorkDateUtc).NotEmpty();
        RuleFor(x => x.DurationMinutes).InclusiveBetween(1, 1440);
        RuleFor(x => x.BreakMinutes).InclusiveBetween(0, 1440);
        RuleFor(x => x).Must(x => x.BreakMinutes < x.DurationMinutes).WithMessage("BreakMustBeLessThanDuration");
        RuleFor(x => x.WorkType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.RejectionReason).MaximumLength(1000);
        RuleFor(x => x.InternalNotes).MaximumLength(4000);
        RuleFor(x => x.MetadataJson).MaximumLength(8000);
    }
}

public sealed class TimesheetEditDtoValidator : AbstractValidator<TimesheetEditDto>
{
    public TimesheetEditDtoValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.TimesheetNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PeriodStartUtc).NotEmpty();
        RuleFor(x => x.PeriodEndUtc).GreaterThanOrEqualTo(x => x.PeriodStartUtc);
        RuleFor(x => x.ReviewNotes).MaximumLength(1000);
        RuleFor(x => x.InternalNotes).MaximumLength(4000);
        RuleFor(x => x.MetadataJson).MaximumLength(8000);
    }
}

public sealed class LeaveRequestEditDtoValidator : AbstractValidator<LeaveRequestEditDto>
{
    public LeaveRequestEditDtoValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.RequestNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StartDateUtc).NotEmpty();
        RuleFor(x => x.EndDateUtc).GreaterThanOrEqualTo(x => x.StartDateUtc);
        RuleFor(x => x.RequestedMinutes).InclusiveBetween(1, 366 * 24 * 60);
        RuleFor(x => x.ReviewNotes).MaximumLength(1000);
        RuleFor(x => x.InternalNotes).MaximumLength(4000);
        RuleFor(x => x.MetadataJson).MaximumLength(8000);
    }
}

public sealed class AbsenceRecordEditDtoValidator : AbstractValidator<AbsenceRecordEditDto>
{
    public AbsenceRecordEditDtoValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.StartDateUtc).NotEmpty();
        RuleFor(x => x.EndDateUtc).GreaterThanOrEqualTo(x => x.StartDateUtc);
        RuleFor(x => x.AbsenceMinutes).InclusiveBetween(1, 366 * 24 * 60);
        RuleFor(x => x.InternalNotes).MaximumLength(4000);
        RuleFor(x => x.MetadataJson).MaximumLength(8000);
    }
}

public sealed class PayrollPeriodEditDtoValidator : AbstractValidator<PayrollPeriodEditDto>
{
    public PayrollPeriodEditDtoValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.PeriodCode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PeriodStartUtc).NotEmpty();
        RuleFor(x => x.PeriodEndUtc).GreaterThanOrEqualTo(x => x.PeriodStartUtc);
        RuleFor(x => x.ReviewNotes).MaximumLength(1000);
        RuleFor(x => x.InternalNotes).MaximumLength(4000);
        RuleFor(x => x.MetadataJson).MaximumLength(8000);
    }
}

public sealed class PayrollRuleSetEditDtoValidator : AbstractValidator<PayrollRuleSetEditDto>
{
    public PayrollRuleSetEditDtoValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.JurisdictionCode).NotEmpty().MaximumLength(16);
        RuleFor(x => x.RuleSetCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RuleVersion).NotEmpty().MaximumLength(64);
        RuleFor(x => x.EffectiveFromUtc).NotEmpty();
        RuleFor(x => x.EffectiveToUtc).GreaterThanOrEqualTo(x => x.EffectiveFromUtc).When(x => x.EffectiveToUtc.HasValue);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.MetadataJson).MaximumLength(8000);
    }
}

public sealed class PayrollRuleComponentDtoValidator : AbstractValidator<PayrollRuleComponentDto>
{
    public PayrollRuleComponentDtoValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.PayrollRuleSetId).NotEmpty();
        RuleFor(x => x.ComponentCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RateBasisPoints).InclusiveBetween(0, 1000000).When(x => x.RateBasisPoints.HasValue);
        RuleFor(x => x.AmountMinor).GreaterThanOrEqualTo(0).When(x => x.AmountMinor.HasValue);
        RuleFor(x => x.ThresholdJson).MaximumLength(8000);
        RuleFor(x => x.MetadataJson).MaximumLength(8000);
    }
}
