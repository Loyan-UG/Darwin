using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Application.HumanResources.DTOs;
using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.HumanResources.Commands;

public sealed class CreatePayrollPeriodHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<PayrollPeriodEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;
    public CreatePayrollPeriodHandler(IAppDbContext db, IValidator<PayrollPeriodEditDto> validator, IClock clock, BusinessEventService? events = null) { _db = db; _validator = validator; _clock = clock; _events = events; }

    public async Task<Guid> HandleAsync(PayrollPeriodEditDto dto, CancellationToken ct = default)
    {
        HrPayrollSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrPayrollSupport.EnsureSafe(dto.PeriodCode, dto.ReviewNotes, dto.InternalNotes, dto.MetadataJson);
        await HrPayrollSupport.EnsurePeriodAvailableAsync(_db, dto.BusinessId, dto.PeriodCode, dto.PeriodStartUtc, dto.PeriodEndUtc, null, ct).ConfigureAwait(false);
        var entity = new PayrollPeriod { BusinessId = dto.BusinessId, PeriodCode = dto.PeriodCode, Status = dto.Status, PeriodStartUtc = dto.PeriodStartUtc.Date, PeriodEndUtc = dto.PeriodEndUtc.Date, ReviewNotes = dto.ReviewNotes, InternalNotes = dto.InternalNotes, MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson) };
        _db.Set<PayrollPeriod>().Add(entity);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "PayrollPeriod", entity.Id, "hr.payroll_period.created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return entity.Id;
    }
}

public sealed class UpdatePayrollPeriodHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<PayrollPeriodEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;
    public UpdatePayrollPeriodHandler(IAppDbContext db, IValidator<PayrollPeriodEditDto> validator, IClock clock, BusinessEventService? events = null) { _db = db; _validator = validator; _clock = clock; _events = events; }

    public async Task HandleAsync(PayrollPeriodEditDto dto, CancellationToken ct = default)
    {
        HrPayrollSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrPayrollSupport.EnsureSafe(dto.PeriodCode, dto.ReviewNotes, dto.InternalNotes, dto.MetadataJson);
        var entity = await _db.Set<PayrollPeriod>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (entity is null) throw new InvalidOperationException("PayrollPeriodNotFound");
        HrCoreSupport.EnsureRowVersion(entity.RowVersion, dto.RowVersion);
        if (entity.Status == PayrollPeriodStatus.Approved) throw new InvalidOperationException("PayrollPeriodLocked");
        await HrPayrollSupport.EnsurePeriodAvailableAsync(_db, dto.BusinessId, dto.PeriodCode, dto.PeriodStartUtc, dto.PeriodEndUtc, dto.Id, ct).ConfigureAwait(false);
        entity.BusinessId = dto.BusinessId;
        entity.PeriodCode = dto.PeriodCode;
        entity.PeriodStartUtc = dto.PeriodStartUtc.Date;
        entity.PeriodEndUtc = dto.PeriodEndUtc.Date;
        entity.ReviewNotes = dto.ReviewNotes;
        entity.InternalNotes = dto.InternalNotes;
        entity.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "PayrollPeriod", entity.Id, "hr.payroll_period.updated", AuditTrailAction.Updated, ct).ConfigureAwait(false);
    }
}

public sealed class PreparePayrollPeriodSummaryHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;
    public PreparePayrollPeriodSummaryHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null) { _db = db; _clock = clock; _events = events; }

    public async Task<Result> HandleAsync(HrTimeLifecycleDto dto, CancellationToken ct = default)
    {
        var period = await _db.Set<PayrollPeriod>().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (period is null) return Result.Fail("PayrollPeriodNotFound");
        if (!HrCoreSupport.RowVersionMatches(period.RowVersion, dto.RowVersion)) return Result.Fail("ItemConcurrencyConflict");
        if (period.Status is PayrollPeriodStatus.Approved or PayrollPeriodStatus.Cancelled) return Result.Fail("PayrollPeriodLocked");

        var summaries = await HrPayrollSupport.BuildSummaryAsync(_db, period.BusinessId, period.PeriodStartUtc, period.PeriodEndUtc, ct).ConfigureAwait(false);
        _db.Set<PayrollPeriodLine>().RemoveRange(period.Lines);
        period.Lines = summaries.Select(x => new PayrollPeriodLine
        {
            BusinessId = period.BusinessId,
            PayrollPeriodId = period.Id,
            EmployeeId = x.EmployeeId,
            WorkMinutes = x.WorkMinutes,
            BreakMinutes = x.BreakMinutes,
            AbsenceMinutes = x.AbsenceMinutes,
            ApprovedTimesheetCount = x.ApprovedTimesheetCount,
            ConfirmedAbsenceCount = x.ConfirmedAbsenceCount,
            SummaryJson = $$"""{"source":"approved-hr-time","employeeId":"{{x.EmployeeId}}"}"""
        }).ToList();
        period.EmployeeCount = summaries.Count;
        period.TotalWorkMinutes = summaries.Sum(x => x.WorkMinutes);
        period.TotalBreakMinutes = summaries.Sum(x => x.BreakMinutes);
        period.TotalAbsenceMinutes = summaries.Sum(x => x.AbsenceMinutes);
        period.Status = PayrollPeriodStatus.Prepared;
        period.PreparedAtUtc = _clock.UtcNow;
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, period.BusinessId, "PayrollPeriod", period.Id, "hr.payroll_period.prepared", AuditTrailAction.StatusChanged, ct).ConfigureAwait(false);
        return Result.Ok();
    }
}

public sealed class UpdatePayrollPeriodLifecycleHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;
    public UpdatePayrollPeriodLifecycleHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null) { _db = db; _clock = clock; _events = events; }

    public async Task<Result> HandleAsync(HrTimeLifecycleDto dto, PayrollPeriodStatus target, CancellationToken ct = default)
    {
        dto.Notes = HrCoreSupport.Optional(dto.Notes);
        HrPayrollSupport.EnsureSafe(dto.Notes);
        var period = await _db.Set<PayrollPeriod>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (period is null) return Result.Fail("PayrollPeriodNotFound");
        if (!HrCoreSupport.RowVersionMatches(period.RowVersion, dto.RowVersion)) return Result.Fail("ItemConcurrencyConflict");
        if (!HrPayrollSupport.CanTransition(period.Status, target)) return Result.Fail("PayrollPeriodInvalidTransition");
        var now = _clock.UtcNow;
        period.Status = target;
        period.ReviewNotes = dto.Notes ?? period.ReviewNotes;
        if (target == PayrollPeriodStatus.Reviewed) period.ReviewedAtUtc = now;
        if (target == PayrollPeriodStatus.Approved) period.ApprovedAtUtc = now;
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, period.BusinessId, "PayrollPeriod", period.Id, $"hr.payroll_period.{target.ToString().ToLowerInvariant()}", AuditTrailAction.StatusChanged, ct).ConfigureAwait(false);
        return Result.Ok();
    }
}

internal static class HrPayrollSupport
{
    public sealed record EmployeePayrollSummary(Guid EmployeeId, int WorkMinutes, int BreakMinutes, int AbsenceMinutes, int ApprovedTimesheetCount, int ConfirmedAbsenceCount);

    public static void Normalize(PayrollPeriodEditDto dto)
    {
        dto.PeriodCode = HrCoreSupport.NormalizeCode(dto.PeriodCode);
        dto.PeriodStartUtc = dto.PeriodStartUtc.Date;
        dto.PeriodEndUtc = dto.PeriodEndUtc.Date;
        dto.ReviewNotes = HrCoreSupport.Optional(dto.ReviewNotes);
        dto.InternalNotes = HrCoreSupport.Optional(dto.InternalNotes);
        dto.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
    }

    public static void EnsureSafe(params string?[] values) => HrCoreSupport.EnsureSafe(values);

    public static async Task EnsurePeriodAvailableAsync(IAppDbContext db, Guid businessId, string code, DateTime start, DateTime end, Guid? excludingId, CancellationToken ct)
    {
        var codeQuery = db.Set<PayrollPeriod>().AsNoTracking().Where(x => x.BusinessId == businessId && x.PeriodCode == code && !x.IsDeleted);
        var periodQuery = db.Set<PayrollPeriod>().AsNoTracking().Where(x => x.BusinessId == businessId && x.PeriodStartUtc == start.Date && x.PeriodEndUtc == end.Date && !x.IsDeleted);
        if (excludingId.HasValue)
        {
            codeQuery = codeQuery.Where(x => x.Id != excludingId.Value);
            periodQuery = periodQuery.Where(x => x.Id != excludingId.Value);
        }
        if (await codeQuery.AnyAsync(ct).ConfigureAwait(false)) throw new InvalidOperationException("PayrollPeriodCodeDuplicate");
        if (await periodQuery.AnyAsync(ct).ConfigureAwait(false)) throw new InvalidOperationException("PayrollPeriodRangeDuplicate");
    }

    public static async Task<List<EmployeePayrollSummary>> BuildSummaryAsync(IAppDbContext db, Guid businessId, DateTime start, DateTime end, CancellationToken ct)
    {
        var timesheets = await db.Set<Timesheet>().AsNoTracking()
            .Where(x => x.BusinessId == businessId && !x.IsDeleted && x.Status == TimesheetStatus.Approved && x.PeriodStartUtc >= start.Date && x.PeriodEndUtc <= end.Date)
            .GroupBy(x => x.EmployeeId)
            .Select(x => new { EmployeeId = x.Key, Work = x.Sum(y => y.TotalWorkMinutes), Breaks = x.Sum(y => y.TotalBreakMinutes), Count = x.Count() })
            .ToListAsync(ct).ConfigureAwait(false);
        var absences = await db.Set<AbsenceRecord>().AsNoTracking()
            .Where(x => x.BusinessId == businessId && !x.IsDeleted && x.Status == AbsenceStatus.Confirmed && x.StartDateUtc >= start.Date && x.EndDateUtc <= end.Date)
            .GroupBy(x => x.EmployeeId)
            .Select(x => new { EmployeeId = x.Key, Absence = x.Sum(y => y.AbsenceMinutes), Count = x.Count() })
            .ToListAsync(ct).ConfigureAwait(false);
        var employeeIds = timesheets.Select(x => x.EmployeeId).Concat(absences.Select(x => x.EmployeeId)).Distinct().ToList();
        return employeeIds.Select(id =>
        {
            var time = timesheets.FirstOrDefault(x => x.EmployeeId == id);
            var absence = absences.FirstOrDefault(x => x.EmployeeId == id);
            return new EmployeePayrollSummary(id, time?.Work ?? 0, time?.Breaks ?? 0, absence?.Absence ?? 0, time?.Count ?? 0, absence?.Count ?? 0);
        }).OrderBy(x => x.EmployeeId).ToList();
    }

    public static bool CanTransition(PayrollPeriodStatus current, PayrollPeriodStatus target) => (current, target) switch
    {
        (PayrollPeriodStatus.Prepared, PayrollPeriodStatus.Reviewed) => true,
        (PayrollPeriodStatus.Prepared, PayrollPeriodStatus.Approved) => true,
        (PayrollPeriodStatus.Reviewed, PayrollPeriodStatus.Approved) => true,
        (PayrollPeriodStatus.Draft, PayrollPeriodStatus.Cancelled) => true,
        (PayrollPeriodStatus.Prepared, PayrollPeriodStatus.Cancelled) => true,
        (PayrollPeriodStatus.Reviewed, PayrollPeriodStatus.Cancelled) => true,
        _ => false
    };
}
