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

public sealed class CreateWorkScheduleHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<WorkScheduleEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreateWorkScheduleHandler(IAppDbContext db, IValidator<WorkScheduleEditDto> validator, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _validator = validator;
        _clock = clock;
        _events = events;
    }

    public async Task<Guid> HandleAsync(WorkScheduleEditDto dto, CancellationToken ct = default)
    {
        HrTimeSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrTimeSupport.EnsureSafe(dto.ScheduleCode, dto.InternalNotes, dto.MetadataJson);
        await HrCoreSupport.EnsureEmployeeExistsAsync(_db, dto.BusinessId, dto.EmployeeId, ct).ConfigureAwait(false);
        await HrTimeSupport.EnsureScheduleCodeAvailableAsync(_db, dto.BusinessId, dto.EmployeeId, dto.ScheduleCode, null, ct).ConfigureAwait(false);

        var entity = new WorkSchedule
        {
            BusinessId = dto.BusinessId,
            EmployeeId = dto.EmployeeId,
            ScheduleCode = dto.ScheduleCode,
            Status = dto.Status,
            EffectiveFromUtc = dto.EffectiveFromUtc,
            EffectiveToUtc = dto.EffectiveToUtc,
            MondayMinutes = dto.MondayMinutes,
            TuesdayMinutes = dto.TuesdayMinutes,
            WednesdayMinutes = dto.WednesdayMinutes,
            ThursdayMinutes = dto.ThursdayMinutes,
            FridayMinutes = dto.FridayMinutes,
            SaturdayMinutes = dto.SaturdayMinutes,
            SundayMinutes = dto.SundayMinutes,
            InternalNotes = dto.InternalNotes,
            MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson)
        };
        _db.Set<WorkSchedule>().Add(entity);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "WorkSchedule", entity.Id, "hr.work_schedule.created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return entity.Id;
    }
}

public sealed class UpdateWorkScheduleHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<WorkScheduleEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public UpdateWorkScheduleHandler(IAppDbContext db, IValidator<WorkScheduleEditDto> validator, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _validator = validator;
        _clock = clock;
        _events = events;
    }

    public async Task HandleAsync(WorkScheduleEditDto dto, CancellationToken ct = default)
    {
        HrTimeSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrTimeSupport.EnsureSafe(dto.ScheduleCode, dto.InternalNotes, dto.MetadataJson);
        var entity = await _db.Set<WorkSchedule>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (entity is null) throw new InvalidOperationException("WorkScheduleNotFound");
        HrCoreSupport.EnsureRowVersion(entity.RowVersion, dto.RowVersion);
        await HrCoreSupport.EnsureEmployeeExistsAsync(_db, dto.BusinessId, dto.EmployeeId, ct).ConfigureAwait(false);
        await HrTimeSupport.EnsureScheduleCodeAvailableAsync(_db, dto.BusinessId, dto.EmployeeId, dto.ScheduleCode, dto.Id, ct).ConfigureAwait(false);

        entity.BusinessId = dto.BusinessId;
        entity.EmployeeId = dto.EmployeeId;
        entity.ScheduleCode = dto.ScheduleCode;
        entity.Status = dto.Status;
        entity.EffectiveFromUtc = dto.EffectiveFromUtc;
        entity.EffectiveToUtc = dto.EffectiveToUtc;
        entity.MondayMinutes = dto.MondayMinutes;
        entity.TuesdayMinutes = dto.TuesdayMinutes;
        entity.WednesdayMinutes = dto.WednesdayMinutes;
        entity.ThursdayMinutes = dto.ThursdayMinutes;
        entity.FridayMinutes = dto.FridayMinutes;
        entity.SaturdayMinutes = dto.SaturdayMinutes;
        entity.SundayMinutes = dto.SundayMinutes;
        entity.InternalNotes = dto.InternalNotes;
        entity.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "WorkSchedule", entity.Id, "hr.work_schedule.updated", AuditTrailAction.Updated, ct).ConfigureAwait(false);
    }
}

public sealed class ArchiveWorkScheduleHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public ArchiveWorkScheduleHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _clock = clock;
        _events = events;
    }

    public async Task<Result> HandleAsync(HrArchiveDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Set<WorkSchedule>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (entity is null) return Result.Fail("WorkScheduleNotFound");
        if (!HrCoreSupport.RowVersionMatches(entity.RowVersion, dto.RowVersion)) return Result.Fail("ItemConcurrencyConflict");
        entity.Status = WorkScheduleStatus.Archived;
        entity.IsDeleted = true;
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "WorkSchedule", entity.Id, "hr.work_schedule.archived", AuditTrailAction.Deleted, ct).ConfigureAwait(false);
        return Result.Ok();
    }
}

public sealed class CreateWorkScheduleExceptionHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<WorkScheduleExceptionDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreateWorkScheduleExceptionHandler(IAppDbContext db, IValidator<WorkScheduleExceptionDto> validator, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _validator = validator;
        _clock = clock;
        _events = events;
    }

    public async Task<Guid> HandleAsync(WorkScheduleExceptionDto dto, CancellationToken ct = default)
    {
        HrTimeSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrTimeSupport.EnsureSafe(dto.Reason, dto.InternalNotes, dto.MetadataJson);
        var schedule = await HrTimeSupport.GetScheduleAsync(_db, dto.BusinessId, dto.WorkScheduleId, ct).ConfigureAwait(false);
        await HrTimeSupport.EnsureScheduleExceptionAvailableAsync(_db, dto.WorkScheduleId, dto.WorkDateUtc, null, ct).ConfigureAwait(false);

        var entity = new WorkScheduleException
        {
            BusinessId = dto.BusinessId,
            WorkScheduleId = dto.WorkScheduleId,
            WorkDateUtc = dto.WorkDateUtc.Date,
            ScheduledMinutes = dto.ScheduledMinutes,
            Reason = dto.Reason,
            InternalNotes = dto.InternalNotes,
            MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson)
        };
        _db.Set<WorkScheduleException>().Add(entity);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, schedule.BusinessId, "WorkScheduleException", entity.Id, "hr.work_schedule_exception.created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return entity.Id;
    }
}

public sealed class CreateAttendanceEventHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<AttendanceEventEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreateAttendanceEventHandler(IAppDbContext db, IValidator<AttendanceEventEditDto> validator, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _validator = validator;
        _clock = clock;
        _events = events;
    }

    public async Task<Guid> HandleAsync(AttendanceEventEditDto dto, CancellationToken ct = default)
    {
        HrTimeSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrTimeSupport.EnsureSafe(dto.SourceReference, dto.InternalNotes, dto.MetadataJson);
        await HrCoreSupport.EnsureEmployeeExistsAsync(_db, dto.BusinessId, dto.EmployeeId, ct).ConfigureAwait(false);
        var entity = new AttendanceEvent
        {
            BusinessId = dto.BusinessId,
            EmployeeId = dto.EmployeeId,
            EventType = dto.EventType,
            OccurredAtUtc = dto.OccurredAtUtc,
            SourceReference = dto.SourceReference,
            InternalNotes = dto.InternalNotes,
            MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson)
        };
        _db.Set<AttendanceEvent>().Add(entity);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "AttendanceEvent", entity.Id, "hr.attendance_event.created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return entity.Id;
    }
}

public sealed class CreateTimeEntryHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<TimeEntryEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreateTimeEntryHandler(IAppDbContext db, IValidator<TimeEntryEditDto> validator, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _validator = validator;
        _clock = clock;
        _events = events;
    }

    public async Task<Guid> HandleAsync(TimeEntryEditDto dto, CancellationToken ct = default)
    {
        HrTimeSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrTimeSupport.EnsureSafe(dto.WorkType, dto.Description, dto.RejectionReason, dto.InternalNotes, dto.MetadataJson);
        await HrTimeSupport.EnsureTimeEntryLinksAsync(_db, dto.BusinessId, dto.EmployeeId, dto.WorkScheduleId, ct).ConfigureAwait(false);
        var entity = new TimeEntry
        {
            BusinessId = dto.BusinessId,
            EmployeeId = dto.EmployeeId,
            WorkScheduleId = dto.WorkScheduleId,
            WorkDateUtc = dto.WorkDateUtc.Date,
            DurationMinutes = dto.DurationMinutes,
            BreakMinutes = dto.BreakMinutes,
            Source = dto.Source,
            Status = dto.Status,
            WorkType = dto.WorkType,
            Description = dto.Description,
            RejectionReason = dto.RejectionReason,
            InternalNotes = dto.InternalNotes,
            MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson)
        };
        _db.Set<TimeEntry>().Add(entity);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "TimeEntry", entity.Id, "hr.time_entry.created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return entity.Id;
    }
}

public sealed class UpdateTimeEntryHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<TimeEntryEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public UpdateTimeEntryHandler(IAppDbContext db, IValidator<TimeEntryEditDto> validator, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _validator = validator;
        _clock = clock;
        _events = events;
    }

    public async Task HandleAsync(TimeEntryEditDto dto, CancellationToken ct = default)
    {
        HrTimeSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrTimeSupport.EnsureSafe(dto.WorkType, dto.Description, dto.RejectionReason, dto.InternalNotes, dto.MetadataJson);
        var entity = await _db.Set<TimeEntry>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (entity is null) throw new InvalidOperationException("TimeEntryNotFound");
        HrCoreSupport.EnsureRowVersion(entity.RowVersion, dto.RowVersion);
        if (entity.Status is TimeEntryStatus.Approved or TimeEntryStatus.Cancelled) throw new InvalidOperationException("TimeEntryLocked");
        await HrTimeSupport.EnsureTimeEntryLinksAsync(_db, dto.BusinessId, dto.EmployeeId, dto.WorkScheduleId, ct).ConfigureAwait(false);
        entity.BusinessId = dto.BusinessId;
        entity.EmployeeId = dto.EmployeeId;
        entity.WorkScheduleId = dto.WorkScheduleId;
        entity.WorkDateUtc = dto.WorkDateUtc.Date;
        entity.DurationMinutes = dto.DurationMinutes;
        entity.BreakMinutes = dto.BreakMinutes;
        entity.Source = dto.Source;
        entity.Status = dto.Status;
        entity.WorkType = dto.WorkType;
        entity.Description = dto.Description;
        entity.RejectionReason = dto.RejectionReason;
        entity.InternalNotes = dto.InternalNotes;
        entity.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "TimeEntry", entity.Id, "hr.time_entry.updated", AuditTrailAction.Updated, ct).ConfigureAwait(false);
    }
}

public sealed class CreateTimesheetHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<TimesheetEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreateTimesheetHandler(IAppDbContext db, IValidator<TimesheetEditDto> validator, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _validator = validator;
        _clock = clock;
        _events = events;
    }

    public async Task<Guid> HandleAsync(TimesheetEditDto dto, CancellationToken ct = default)
    {
        HrTimeSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrTimeSupport.EnsureSafe(dto.TimesheetNumber, dto.ReviewNotes, dto.InternalNotes, dto.MetadataJson);
        await HrCoreSupport.EnsureEmployeeExistsAsync(_db, dto.BusinessId, dto.EmployeeId, ct).ConfigureAwait(false);
        await HrTimeSupport.EnsureTimesheetAvailableAsync(_db, dto.BusinessId, dto.EmployeeId, dto.TimesheetNumber, dto.PeriodStartUtc, dto.PeriodEndUtc, null, ct).ConfigureAwait(false);
        var entries = await HrTimeSupport.LoadTimesheetEntriesAsync(_db, dto.BusinessId, dto.EmployeeId, dto.PeriodStartUtc, dto.PeriodEndUtc, dto.TimeEntryIds, ct).ConfigureAwait(false);

        var entity = new Timesheet
        {
            BusinessId = dto.BusinessId,
            EmployeeId = dto.EmployeeId,
            TimesheetNumber = dto.TimesheetNumber,
            Status = dto.Status,
            PeriodStartUtc = dto.PeriodStartUtc.Date,
            PeriodEndUtc = dto.PeriodEndUtc.Date,
            TotalWorkMinutes = entries.Sum(x => x.DurationMinutes),
            TotalBreakMinutes = entries.Sum(x => x.BreakMinutes),
            ReviewNotes = dto.ReviewNotes,
            InternalNotes = dto.InternalNotes,
            MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson),
            Lines = entries.Select((entry, index) => new TimesheetLine
            {
                BusinessId = dto.BusinessId,
                TimeEntryId = entry.Id,
                WorkDateUtc = entry.WorkDateUtc,
                DurationMinutes = entry.DurationMinutes,
                BreakMinutes = entry.BreakMinutes,
                SortOrder = index
            }).ToList()
        };
        _db.Set<Timesheet>().Add(entity);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "Timesheet", entity.Id, "hr.timesheet.created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return entity.Id;
    }
}

public sealed class UpdateTimesheetHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<TimesheetEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public UpdateTimesheetHandler(IAppDbContext db, IValidator<TimesheetEditDto> validator, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _validator = validator;
        _clock = clock;
        _events = events;
    }

    public async Task HandleAsync(TimesheetEditDto dto, CancellationToken ct = default)
    {
        HrTimeSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrTimeSupport.EnsureSafe(dto.TimesheetNumber, dto.ReviewNotes, dto.InternalNotes, dto.MetadataJson);
        var entity = await _db.Set<Timesheet>().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (entity is null) throw new InvalidOperationException("TimesheetNotFound");
        HrCoreSupport.EnsureRowVersion(entity.RowVersion, dto.RowVersion);
        if (entity.Status is TimesheetStatus.Approved or TimesheetStatus.Cancelled) throw new InvalidOperationException("TimesheetLocked");
        await HrCoreSupport.EnsureEmployeeExistsAsync(_db, dto.BusinessId, dto.EmployeeId, ct).ConfigureAwait(false);
        await HrTimeSupport.EnsureTimesheetAvailableAsync(_db, dto.BusinessId, dto.EmployeeId, dto.TimesheetNumber, dto.PeriodStartUtc, dto.PeriodEndUtc, dto.Id, ct).ConfigureAwait(false);
        var entries = await HrTimeSupport.LoadTimesheetEntriesAsync(_db, dto.BusinessId, dto.EmployeeId, dto.PeriodStartUtc, dto.PeriodEndUtc, dto.TimeEntryIds, ct).ConfigureAwait(false);

        entity.BusinessId = dto.BusinessId;
        entity.EmployeeId = dto.EmployeeId;
        entity.TimesheetNumber = dto.TimesheetNumber;
        entity.PeriodStartUtc = dto.PeriodStartUtc.Date;
        entity.PeriodEndUtc = dto.PeriodEndUtc.Date;
        entity.TotalWorkMinutes = entries.Sum(x => x.DurationMinutes);
        entity.TotalBreakMinutes = entries.Sum(x => x.BreakMinutes);
        entity.ReviewNotes = dto.ReviewNotes;
        entity.InternalNotes = dto.InternalNotes;
        entity.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
        _db.Set<TimesheetLine>().RemoveRange(entity.Lines);
        entity.Lines = entries.Select((entry, index) => new TimesheetLine
        {
            BusinessId = dto.BusinessId,
            TimesheetId = entity.Id,
            TimeEntryId = entry.Id,
            WorkDateUtc = entry.WorkDateUtc,
            DurationMinutes = entry.DurationMinutes,
            BreakMinutes = entry.BreakMinutes,
            SortOrder = index
        }).ToList();
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "Timesheet", entity.Id, "hr.timesheet.updated", AuditTrailAction.Updated, ct).ConfigureAwait(false);
    }
}

public sealed class UpdateTimesheetLifecycleHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public UpdateTimesheetLifecycleHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _clock = clock;
        _events = events;
    }

    public async Task<Result> HandleAsync(HrTimeLifecycleDto dto, TimesheetStatus target, CancellationToken ct = default)
    {
        dto.Notes = HrCoreSupport.Optional(dto.Notes);
        HrTimeSupport.EnsureSafe(dto.Notes);
        var entity = await _db.Set<Timesheet>().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (entity is null) return Result.Fail("TimesheetNotFound");
        if (!HrCoreSupport.RowVersionMatches(entity.RowVersion, dto.RowVersion)) return Result.Fail("ItemConcurrencyConflict");
        if (!HrTimeSupport.CanTransition(entity.Status, target)) return Result.Fail("TimesheetInvalidTransition");
        if (target == TimesheetStatus.Rejected && string.IsNullOrWhiteSpace(dto.Notes)) return Result.Fail("TimesheetRejectionReasonRequired");
        if (target is TimesheetStatus.Submitted or TimesheetStatus.InReview or TimesheetStatus.Approved)
        {
            if (entity.Lines.Count == 0) return Result.Fail("TimesheetRequiresLines");
        }

        var now = _clock.UtcNow;
        entity.Status = target;
        entity.ReviewNotes = dto.Notes ?? entity.ReviewNotes;
        if (target == TimesheetStatus.Submitted) entity.SubmittedAtUtc = now;
        if (target is TimesheetStatus.Approved or TimesheetStatus.Rejected) entity.ReviewedAtUtc = now;

        var entryIds = entity.Lines.Select(x => x.TimeEntryId).ToList();
        var entries = await _db.Set<TimeEntry>().Where(x => entryIds.Contains(x.Id) && !x.IsDeleted).ToListAsync(ct).ConfigureAwait(false);
        foreach (var entry in entries)
        {
            entry.Status = target switch
            {
                TimesheetStatus.Submitted or TimesheetStatus.InReview => TimeEntryStatus.Submitted,
                TimesheetStatus.Approved => TimeEntryStatus.Approved,
                TimesheetStatus.Rejected => TimeEntryStatus.Rejected,
                TimesheetStatus.Cancelled => TimeEntryStatus.Cancelled,
                _ => entry.Status
            };
            if (target == TimesheetStatus.Rejected) entry.RejectionReason = dto.Notes;
        }

        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "Timesheet", entity.Id, $"hr.timesheet.{target.ToString().ToLowerInvariant()}", AuditTrailAction.StatusChanged, ct).ConfigureAwait(false);
        return Result.Ok();
    }
}

internal static class HrTimeSupport
{
    public static void Normalize(WorkScheduleEditDto dto)
    {
        dto.ScheduleCode = HrCoreSupport.NormalizeCode(dto.ScheduleCode);
        dto.InternalNotes = HrCoreSupport.Optional(dto.InternalNotes);
        dto.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
        dto.EffectiveFromUtc = dto.EffectiveFromUtc.Date;
        dto.EffectiveToUtc = dto.EffectiveToUtc?.Date;
    }

    public static void Normalize(WorkScheduleExceptionDto dto)
    {
        dto.WorkDateUtc = dto.WorkDateUtc.Date;
        dto.Reason = HrCoreSupport.Required(dto.Reason);
        dto.InternalNotes = HrCoreSupport.Optional(dto.InternalNotes);
        dto.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
    }

    public static void Normalize(AttendanceEventEditDto dto)
    {
        dto.SourceReference = HrCoreSupport.Optional(dto.SourceReference);
        dto.InternalNotes = HrCoreSupport.Optional(dto.InternalNotes);
        dto.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
    }

    public static void Normalize(TimeEntryEditDto dto)
    {
        dto.WorkDateUtc = dto.WorkDateUtc.Date;
        dto.WorkType = HrCoreSupport.NormalizeCode(dto.WorkType);
        dto.Description = HrCoreSupport.Optional(dto.Description);
        dto.RejectionReason = HrCoreSupport.Optional(dto.RejectionReason);
        dto.InternalNotes = HrCoreSupport.Optional(dto.InternalNotes);
        dto.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
    }

    public static void Normalize(TimesheetEditDto dto)
    {
        dto.TimesheetNumber = HrCoreSupport.NormalizeCode(dto.TimesheetNumber);
        dto.PeriodStartUtc = dto.PeriodStartUtc.Date;
        dto.PeriodEndUtc = dto.PeriodEndUtc.Date;
        dto.ReviewNotes = HrCoreSupport.Optional(dto.ReviewNotes);
        dto.InternalNotes = HrCoreSupport.Optional(dto.InternalNotes);
        dto.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
        dto.TimeEntryIds = dto.TimeEntryIds.Where(x => x != Guid.Empty).Distinct().ToList();
    }

    public static void EnsureSafe(params string?[] values) => HrCoreSupport.EnsureSafe(values);

    public static async Task<WorkSchedule> GetScheduleAsync(IAppDbContext db, Guid businessId, Guid scheduleId, CancellationToken ct)
    {
        var schedule = await db.Set<WorkSchedule>().FirstOrDefaultAsync(x => x.Id == scheduleId && x.BusinessId == businessId && !x.IsDeleted, ct).ConfigureAwait(false);
        return schedule ?? throw new InvalidOperationException("WorkScheduleNotFound");
    }

    public static async Task EnsureTimeEntryLinksAsync(IAppDbContext db, Guid businessId, Guid employeeId, Guid? scheduleId, CancellationToken ct)
    {
        await HrCoreSupport.EnsureEmployeeExistsAsync(db, businessId, employeeId, ct).ConfigureAwait(false);
        if (!scheduleId.HasValue) return;
        var exists = await db.Set<WorkSchedule>().AnyAsync(x => x.Id == scheduleId.Value && x.BusinessId == businessId && x.EmployeeId == employeeId && !x.IsDeleted && x.Status != WorkScheduleStatus.Archived, ct).ConfigureAwait(false);
        if (!exists) throw new InvalidOperationException("WorkScheduleNotFound");
    }

    public static async Task EnsureScheduleCodeAvailableAsync(IAppDbContext db, Guid businessId, Guid employeeId, string code, Guid? excludingId, CancellationToken ct)
    {
        var query = db.Set<WorkSchedule>().AsNoTracking().Where(x => x.BusinessId == businessId && x.EmployeeId == employeeId && x.ScheduleCode == code && !x.IsDeleted);
        if (excludingId.HasValue) query = query.Where(x => x.Id != excludingId.Value);
        if (await query.AnyAsync(ct).ConfigureAwait(false)) throw new InvalidOperationException("WorkScheduleCodeDuplicate");
    }

    public static async Task EnsureScheduleExceptionAvailableAsync(IAppDbContext db, Guid scheduleId, DateTime date, Guid? excludingId, CancellationToken ct)
    {
        var query = db.Set<WorkScheduleException>().AsNoTracking().Where(x => x.WorkScheduleId == scheduleId && x.WorkDateUtc == date.Date && !x.IsDeleted);
        if (excludingId.HasValue) query = query.Where(x => x.Id != excludingId.Value);
        if (await query.AnyAsync(ct).ConfigureAwait(false)) throw new InvalidOperationException("WorkScheduleExceptionDuplicate");
    }

    public static async Task EnsureTimesheetAvailableAsync(IAppDbContext db, Guid businessId, Guid employeeId, string number, DateTime start, DateTime end, Guid? excludingId, CancellationToken ct)
    {
        var numberQuery = db.Set<Timesheet>().AsNoTracking().Where(x => x.BusinessId == businessId && x.EmployeeId == employeeId && x.TimesheetNumber == number && !x.IsDeleted);
        var periodQuery = db.Set<Timesheet>().AsNoTracking().Where(x => x.BusinessId == businessId && x.EmployeeId == employeeId && x.PeriodStartUtc == start.Date && x.PeriodEndUtc == end.Date && !x.IsDeleted);
        if (excludingId.HasValue)
        {
            numberQuery = numberQuery.Where(x => x.Id != excludingId.Value);
            periodQuery = periodQuery.Where(x => x.Id != excludingId.Value);
        }
        if (await numberQuery.AnyAsync(ct).ConfigureAwait(false)) throw new InvalidOperationException("TimesheetNumberDuplicate");
        if (await periodQuery.AnyAsync(ct).ConfigureAwait(false)) throw new InvalidOperationException("TimesheetPeriodDuplicate");
    }

    public static async Task<List<TimeEntry>> LoadTimesheetEntriesAsync(IAppDbContext db, Guid businessId, Guid employeeId, DateTime start, DateTime end, List<Guid> entryIds, CancellationToken ct)
    {
        if (entryIds.Count == 0) return new List<TimeEntry>();
        var entries = await db.Set<TimeEntry>().Where(x => entryIds.Contains(x.Id) && !x.IsDeleted).ToListAsync(ct).ConfigureAwait(false);
        if (entries.Count != entryIds.Count) throw new InvalidOperationException("TimeEntryNotFound");
        if (entries.Any(x => x.BusinessId != businessId || x.EmployeeId != employeeId)) throw new InvalidOperationException("TimeEntryBusinessMismatch");
        if (entries.Any(x => x.WorkDateUtc.Date < start.Date || x.WorkDateUtc.Date > end.Date)) throw new InvalidOperationException("TimeEntryOutsideTimesheetPeriod");
        if (entries.Any(x => x.Status is TimeEntryStatus.Approved or TimeEntryStatus.Cancelled)) throw new InvalidOperationException("TimeEntryLocked");
        return entries.OrderBy(x => x.WorkDateUtc).ThenBy(x => x.Id).ToList();
    }

    public static bool CanTransition(TimesheetStatus current, TimesheetStatus target) => (current, target) switch
    {
        (TimesheetStatus.Draft, TimesheetStatus.Submitted) => true,
        (TimesheetStatus.Submitted, TimesheetStatus.InReview) => true,
        (TimesheetStatus.Submitted, TimesheetStatus.Approved) => true,
        (TimesheetStatus.InReview, TimesheetStatus.Approved) => true,
        (TimesheetStatus.Submitted, TimesheetStatus.Rejected) => true,
        (TimesheetStatus.InReview, TimesheetStatus.Rejected) => true,
        (TimesheetStatus.Draft, TimesheetStatus.Cancelled) => true,
        (TimesheetStatus.Rejected, TimesheetStatus.Submitted) => true,
        _ => false
    };
}
