using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Application.HumanResources.DTOs;
using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.HumanResources.Queries;

public sealed class GetWorkSchedulesPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;
    public GetWorkSchedulesPageHandler(IAppDbContext db) => _db = db;

    public async Task<(List<WorkScheduleListItemDto> Items, int Total)> HandleAsync(Guid businessId, Guid? employeeId, int page, int pageSize, string? query, WorkScheduleQueueFilter filter, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return (new List<WorkScheduleListItemDto>(), 0);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var rows =
            from schedule in _db.Set<WorkSchedule>().AsNoTracking()
            join employee in _db.Set<Employee>().AsNoTracking() on schedule.EmployeeId equals employee.Id
            where schedule.BusinessId == businessId && !schedule.IsDeleted
            select new { schedule, employee };
        if (employeeId.HasValue) rows = rows.Where(x => x.schedule.EmployeeId == employeeId.Value);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.ContainsInvariant(query);
            rows = rows.Where(x => EF.Functions.Like(x.schedule.ScheduleCode.ToUpper(), term, QueryLikePattern.EscapeCharacter) || EF.Functions.Like((x.employee.FirstName + " " + x.employee.LastName).ToUpper(), term, QueryLikePattern.EscapeCharacter));
        }
        rows = filter switch
        {
            WorkScheduleQueueFilter.Draft => rows.Where(x => x.schedule.Status == WorkScheduleStatus.Draft),
            WorkScheduleQueueFilter.Active => rows.Where(x => x.schedule.Status == WorkScheduleStatus.Active),
            WorkScheduleQueueFilter.Inactive => rows.Where(x => x.schedule.Status == WorkScheduleStatus.Inactive),
            WorkScheduleQueueFilter.Archived => rows.Where(x => x.schedule.Status == WorkScheduleStatus.Archived),
            _ => rows
        };
        var total = await rows.CountAsync(ct).ConfigureAwait(false);
        var items = await rows.OrderByDescending(x => x.schedule.EffectiveFromUtc).ThenBy(x => x.schedule.ScheduleCode)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new WorkScheduleListItemDto
            {
                Id = x.schedule.Id,
                RowVersion = x.schedule.RowVersion,
                BusinessId = x.schedule.BusinessId,
                EmployeeId = x.schedule.EmployeeId,
                EmployeeName = (x.employee.FirstName + " " + x.employee.LastName).Trim(),
                ScheduleCode = x.schedule.ScheduleCode,
                Status = x.schedule.Status,
                EffectiveFromUtc = x.schedule.EffectiveFromUtc,
                EffectiveToUtc = x.schedule.EffectiveToUtc,
                MondayMinutes = x.schedule.MondayMinutes,
                TuesdayMinutes = x.schedule.TuesdayMinutes,
                WednesdayMinutes = x.schedule.WednesdayMinutes,
                ThursdayMinutes = x.schedule.ThursdayMinutes,
                FridayMinutes = x.schedule.FridayMinutes,
                SaturdayMinutes = x.schedule.SaturdayMinutes,
                SundayMinutes = x.schedule.SundayMinutes,
                InternalNotes = x.schedule.InternalNotes,
                MetadataJson = x.schedule.MetadataJson
            }).ToListAsync(ct).ConfigureAwait(false);
        return (items, total);
    }
}

public sealed class GetWorkScheduleDetailHandler
{
    private readonly IAppDbContext _db;
    public GetWorkScheduleDetailHandler(IAppDbContext db) => _db = db;

    public async Task<WorkScheduleListItemDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return null;
        var dto = await (
            from schedule in _db.Set<WorkSchedule>().AsNoTracking()
            join employee in _db.Set<Employee>().AsNoTracking() on schedule.EmployeeId equals employee.Id
            where schedule.Id == id && !schedule.IsDeleted
            select new WorkScheduleListItemDto
            {
                Id = schedule.Id,
                RowVersion = schedule.RowVersion,
                BusinessId = schedule.BusinessId,
                EmployeeId = schedule.EmployeeId,
                EmployeeName = (employee.FirstName + " " + employee.LastName).Trim(),
                ScheduleCode = schedule.ScheduleCode,
                Status = schedule.Status,
                EffectiveFromUtc = schedule.EffectiveFromUtc,
                EffectiveToUtc = schedule.EffectiveToUtc,
                MondayMinutes = schedule.MondayMinutes,
                TuesdayMinutes = schedule.TuesdayMinutes,
                WednesdayMinutes = schedule.WednesdayMinutes,
                ThursdayMinutes = schedule.ThursdayMinutes,
                FridayMinutes = schedule.FridayMinutes,
                SaturdayMinutes = schedule.SaturdayMinutes,
                SundayMinutes = schedule.SundayMinutes,
                InternalNotes = schedule.InternalNotes,
                MetadataJson = schedule.MetadataJson
            }).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (dto is null) return null;
        dto.Exceptions = await _db.Set<WorkScheduleException>().AsNoTracking()
            .Where(x => x.WorkScheduleId == id && !x.IsDeleted)
            .OrderBy(x => x.WorkDateUtc)
            .Select(x => new WorkScheduleExceptionDto
            {
                Id = x.Id,
                RowVersion = x.RowVersion,
                BusinessId = x.BusinessId,
                WorkScheduleId = x.WorkScheduleId,
                WorkDateUtc = x.WorkDateUtc,
                ScheduledMinutes = x.ScheduledMinutes,
                Reason = x.Reason,
                InternalNotes = x.InternalNotes,
                MetadataJson = x.MetadataJson
            }).ToListAsync(ct).ConfigureAwait(false);
        return dto;
    }
}

public sealed class GetAttendanceEventsPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;
    public GetAttendanceEventsPageHandler(IAppDbContext db) => _db = db;

    public async Task<(List<AttendanceEventListItemDto> Items, int Total)> HandleAsync(Guid businessId, Guid? employeeId, int page, int pageSize, AttendanceEventQueueFilter filter, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return (new List<AttendanceEventListItemDto>(), 0);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var rows =
            from evt in _db.Set<AttendanceEvent>().AsNoTracking()
            join employee in _db.Set<Employee>().AsNoTracking() on evt.EmployeeId equals employee.Id
            where evt.BusinessId == businessId && !evt.IsDeleted
            select new { evt, employee };
        if (employeeId.HasValue) rows = rows.Where(x => x.evt.EmployeeId == employeeId.Value);
        if (fromUtc.HasValue) rows = rows.Where(x => x.evt.OccurredAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) rows = rows.Where(x => x.evt.OccurredAtUtc <= toUtc.Value);
        rows = filter switch
        {
            AttendanceEventQueueFilter.ClockIn => rows.Where(x => x.evt.EventType == AttendanceEventType.ClockIn),
            AttendanceEventQueueFilter.ClockOut => rows.Where(x => x.evt.EventType == AttendanceEventType.ClockOut),
            AttendanceEventQueueFilter.BreakStart => rows.Where(x => x.evt.EventType == AttendanceEventType.BreakStart),
            AttendanceEventQueueFilter.BreakEnd => rows.Where(x => x.evt.EventType == AttendanceEventType.BreakEnd),
            AttendanceEventQueueFilter.ManualCorrection => rows.Where(x => x.evt.EventType == AttendanceEventType.ManualCorrection),
            _ => rows
        };
        var total = await rows.CountAsync(ct).ConfigureAwait(false);
        var items = await rows.OrderByDescending(x => x.evt.OccurredAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AttendanceEventListItemDto
            {
                Id = x.evt.Id,
                RowVersion = x.evt.RowVersion,
                BusinessId = x.evt.BusinessId,
                EmployeeId = x.evt.EmployeeId,
                EmployeeName = (x.employee.FirstName + " " + x.employee.LastName).Trim(),
                EventType = x.evt.EventType,
                OccurredAtUtc = x.evt.OccurredAtUtc,
                SourceReference = x.evt.SourceReference,
                InternalNotes = x.evt.InternalNotes,
                MetadataJson = x.evt.MetadataJson
            }).ToListAsync(ct).ConfigureAwait(false);
        return (items, total);
    }
}

public sealed class GetTimeEntriesPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;
    public GetTimeEntriesPageHandler(IAppDbContext db) => _db = db;

    public async Task<(List<TimeEntryListItemDto> Items, int Total)> HandleAsync(Guid businessId, Guid? employeeId, int page, int pageSize, string? query, TimeEntryQueueFilter filter, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return (new List<TimeEntryListItemDto>(), 0);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var rows =
            from entry in _db.Set<TimeEntry>().AsNoTracking()
            join employee in _db.Set<Employee>().AsNoTracking() on entry.EmployeeId equals employee.Id
            join schedule in _db.Set<WorkSchedule>().AsNoTracking() on entry.WorkScheduleId equals schedule.Id into schedules
            from schedule in schedules.DefaultIfEmpty()
            where entry.BusinessId == businessId && !entry.IsDeleted
            select new { entry, employee, schedule };
        if (employeeId.HasValue) rows = rows.Where(x => x.entry.EmployeeId == employeeId.Value);
        if (fromUtc.HasValue) rows = rows.Where(x => x.entry.WorkDateUtc >= fromUtc.Value.Date);
        if (toUtc.HasValue) rows = rows.Where(x => x.entry.WorkDateUtc <= toUtc.Value.Date);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.ContainsInvariant(query);
            rows = rows.Where(x => EF.Functions.Like(x.entry.WorkType.ToUpper(), term, QueryLikePattern.EscapeCharacter) || EF.Functions.Like((x.employee.FirstName + " " + x.employee.LastName).ToUpper(), term, QueryLikePattern.EscapeCharacter));
        }
        rows = filter switch
        {
            TimeEntryQueueFilter.Draft => rows.Where(x => x.entry.Status == TimeEntryStatus.Draft),
            TimeEntryQueueFilter.Submitted => rows.Where(x => x.entry.Status == TimeEntryStatus.Submitted),
            TimeEntryQueueFilter.Approved => rows.Where(x => x.entry.Status == TimeEntryStatus.Approved),
            TimeEntryQueueFilter.Rejected => rows.Where(x => x.entry.Status == TimeEntryStatus.Rejected),
            TimeEntryQueueFilter.Cancelled => rows.Where(x => x.entry.Status == TimeEntryStatus.Cancelled),
            _ => rows
        };
        var total = await rows.CountAsync(ct).ConfigureAwait(false);
        var items = await rows.OrderByDescending(x => x.entry.WorkDateUtc).ThenBy(x => x.employee.LastName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new TimeEntryListItemDto
            {
                Id = x.entry.Id,
                RowVersion = x.entry.RowVersion,
                BusinessId = x.entry.BusinessId,
                EmployeeId = x.entry.EmployeeId,
                EmployeeName = (x.employee.FirstName + " " + x.employee.LastName).Trim(),
                WorkScheduleId = x.entry.WorkScheduleId,
                ScheduleCode = x.schedule != null ? x.schedule.ScheduleCode : null,
                WorkDateUtc = x.entry.WorkDateUtc,
                DurationMinutes = x.entry.DurationMinutes,
                BreakMinutes = x.entry.BreakMinutes,
                Source = x.entry.Source,
                Status = x.entry.Status,
                WorkType = x.entry.WorkType,
                Description = x.entry.Description,
                RejectionReason = x.entry.RejectionReason,
                InternalNotes = x.entry.InternalNotes,
                MetadataJson = x.entry.MetadataJson
            }).ToListAsync(ct).ConfigureAwait(false);
        return (items, total);
    }
}

public sealed class GetTimeEntryDetailHandler
{
    private readonly IAppDbContext _db;
    public GetTimeEntryDetailHandler(IAppDbContext db) => _db = db;

    public Task<TimeEntryListItemDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return Task.FromResult<TimeEntryListItemDto?>(null);
        return (from entry in _db.Set<TimeEntry>().AsNoTracking()
                join employee in _db.Set<Employee>().AsNoTracking() on entry.EmployeeId equals employee.Id
                join schedule in _db.Set<WorkSchedule>().AsNoTracking() on entry.WorkScheduleId equals schedule.Id into schedules
                from schedule in schedules.DefaultIfEmpty()
                where entry.Id == id && !entry.IsDeleted
                select new TimeEntryListItemDto
                {
                    Id = entry.Id,
                    RowVersion = entry.RowVersion,
                    BusinessId = entry.BusinessId,
                    EmployeeId = entry.EmployeeId,
                    EmployeeName = (employee.FirstName + " " + employee.LastName).Trim(),
                    WorkScheduleId = entry.WorkScheduleId,
                    ScheduleCode = schedule != null ? schedule.ScheduleCode : null,
                    WorkDateUtc = entry.WorkDateUtc,
                    DurationMinutes = entry.DurationMinutes,
                    BreakMinutes = entry.BreakMinutes,
                    Source = entry.Source,
                    Status = entry.Status,
                    WorkType = entry.WorkType,
                    Description = entry.Description,
                    RejectionReason = entry.RejectionReason,
                    InternalNotes = entry.InternalNotes,
                    MetadataJson = entry.MetadataJson
                }).FirstOrDefaultAsync(ct);
    }
}

public sealed class GetTimesheetsPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;
    public GetTimesheetsPageHandler(IAppDbContext db) => _db = db;

    public async Task<(List<TimesheetListItemDto> Items, int Total)> HandleAsync(Guid businessId, Guid? employeeId, int page, int pageSize, string? query, TimesheetQueueFilter filter, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return (new List<TimesheetListItemDto>(), 0);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var rows =
            from sheet in _db.Set<Timesheet>().AsNoTracking()
            join employee in _db.Set<Employee>().AsNoTracking() on sheet.EmployeeId equals employee.Id
            where sheet.BusinessId == businessId && !sheet.IsDeleted
            select new { sheet, employee };
        if (employeeId.HasValue) rows = rows.Where(x => x.sheet.EmployeeId == employeeId.Value);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.ContainsInvariant(query);
            rows = rows.Where(x => EF.Functions.Like(x.sheet.TimesheetNumber.ToUpper(), term, QueryLikePattern.EscapeCharacter) || EF.Functions.Like((x.employee.FirstName + " " + x.employee.LastName).ToUpper(), term, QueryLikePattern.EscapeCharacter));
        }
        rows = filter switch
        {
            TimesheetQueueFilter.Draft => rows.Where(x => x.sheet.Status == TimesheetStatus.Draft),
            TimesheetQueueFilter.Submitted => rows.Where(x => x.sheet.Status == TimesheetStatus.Submitted),
            TimesheetQueueFilter.InReview => rows.Where(x => x.sheet.Status == TimesheetStatus.InReview),
            TimesheetQueueFilter.Approved => rows.Where(x => x.sheet.Status == TimesheetStatus.Approved),
            TimesheetQueueFilter.Rejected => rows.Where(x => x.sheet.Status == TimesheetStatus.Rejected),
            TimesheetQueueFilter.Cancelled => rows.Where(x => x.sheet.Status == TimesheetStatus.Cancelled),
            _ => rows
        };
        var total = await rows.CountAsync(ct).ConfigureAwait(false);
        var items = await rows.OrderByDescending(x => x.sheet.PeriodStartUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new TimesheetListItemDto
            {
                Id = x.sheet.Id,
                RowVersion = x.sheet.RowVersion,
                BusinessId = x.sheet.BusinessId,
                EmployeeId = x.sheet.EmployeeId,
                EmployeeName = (x.employee.FirstName + " " + x.employee.LastName).Trim(),
                TimesheetNumber = x.sheet.TimesheetNumber,
                Status = x.sheet.Status,
                PeriodStartUtc = x.sheet.PeriodStartUtc,
                PeriodEndUtc = x.sheet.PeriodEndUtc,
                TotalWorkMinutes = x.sheet.TotalWorkMinutes,
                TotalBreakMinutes = x.sheet.TotalBreakMinutes,
                SubmittedAtUtc = x.sheet.SubmittedAtUtc,
                ReviewedAtUtc = x.sheet.ReviewedAtUtc,
                ReviewNotes = x.sheet.ReviewNotes,
                InternalNotes = x.sheet.InternalNotes,
                MetadataJson = x.sheet.MetadataJson
            }).ToListAsync(ct).ConfigureAwait(false);
        return (items, total);
    }
}

public sealed class GetTimesheetDetailHandler
{
    private readonly IAppDbContext _db;
    public GetTimesheetDetailHandler(IAppDbContext db) => _db = db;

    public async Task<TimesheetListItemDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return null;
        var dto = await (
            from sheet in _db.Set<Timesheet>().AsNoTracking()
            join employee in _db.Set<Employee>().AsNoTracking() on sheet.EmployeeId equals employee.Id
            where sheet.Id == id && !sheet.IsDeleted
            select new TimesheetListItemDto
            {
                Id = sheet.Id,
                RowVersion = sheet.RowVersion,
                BusinessId = sheet.BusinessId,
                EmployeeId = sheet.EmployeeId,
                EmployeeName = (employee.FirstName + " " + employee.LastName).Trim(),
                TimesheetNumber = sheet.TimesheetNumber,
                Status = sheet.Status,
                PeriodStartUtc = sheet.PeriodStartUtc,
                PeriodEndUtc = sheet.PeriodEndUtc,
                TotalWorkMinutes = sheet.TotalWorkMinutes,
                TotalBreakMinutes = sheet.TotalBreakMinutes,
                SubmittedAtUtc = sheet.SubmittedAtUtc,
                ReviewedAtUtc = sheet.ReviewedAtUtc,
                ReviewNotes = sheet.ReviewNotes,
                InternalNotes = sheet.InternalNotes,
                MetadataJson = sheet.MetadataJson
            }).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (dto is null) return null;
        dto.Lines = await (
            from line in _db.Set<TimesheetLine>().AsNoTracking()
            join entry in _db.Set<TimeEntry>().AsNoTracking() on line.TimeEntryId equals entry.Id
            where line.TimesheetId == id && !line.IsDeleted
            orderby line.SortOrder
            select new TimesheetLineDto
            {
                Id = line.Id,
                TimeEntryId = line.TimeEntryId,
                WorkDateUtc = line.WorkDateUtc,
                DurationMinutes = line.DurationMinutes,
                BreakMinutes = line.BreakMinutes,
                WorkType = entry.WorkType
            }).ToListAsync(ct).ConfigureAwait(false);
        dto.TimeEntryIds = dto.Lines.Select(x => x.TimeEntryId).ToList();
        return dto;
    }
}
