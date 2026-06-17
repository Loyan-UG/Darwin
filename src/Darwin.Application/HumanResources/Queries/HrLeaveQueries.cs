using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Application.HumanResources.DTOs;
using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.HumanResources.Queries;

public sealed class GetLeaveRequestsPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;
    public GetLeaveRequestsPageHandler(IAppDbContext db) => _db = db;

    public async Task<(List<LeaveRequestListItemDto> Items, int Total)> HandleAsync(Guid businessId, Guid? employeeId, int page, int pageSize, string? query, LeaveRequestQueueFilter filter, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return (new List<LeaveRequestListItemDto>(), 0);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var rows =
            from request in _db.Set<LeaveRequest>().AsNoTracking()
            join employee in _db.Set<Employee>().AsNoTracking() on request.EmployeeId equals employee.Id
            where request.BusinessId == businessId && !request.IsDeleted
            select new { request, employee };
        if (employeeId.HasValue) rows = rows.Where(x => x.request.EmployeeId == employeeId.Value);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.ContainsInvariant(query);
            rows = rows.Where(x => EF.Functions.Like(x.request.RequestNumber.ToUpper(), term, QueryLikePattern.EscapeCharacter) || EF.Functions.Like((x.employee.FirstName + " " + x.employee.LastName).ToUpper(), term, QueryLikePattern.EscapeCharacter));
        }
        rows = filter switch
        {
            LeaveRequestQueueFilter.Draft => rows.Where(x => x.request.Status == LeaveRequestStatus.Draft),
            LeaveRequestQueueFilter.Submitted => rows.Where(x => x.request.Status == LeaveRequestStatus.Submitted),
            LeaveRequestQueueFilter.InReview => rows.Where(x => x.request.Status == LeaveRequestStatus.InReview),
            LeaveRequestQueueFilter.Approved => rows.Where(x => x.request.Status == LeaveRequestStatus.Approved),
            LeaveRequestQueueFilter.Rejected => rows.Where(x => x.request.Status == LeaveRequestStatus.Rejected),
            LeaveRequestQueueFilter.Cancelled => rows.Where(x => x.request.Status == LeaveRequestStatus.Cancelled),
            _ => rows
        };
        var total = await rows.CountAsync(ct).ConfigureAwait(false);
        var items = await rows.OrderByDescending(x => x.request.StartDateUtc).ThenBy(x => x.request.RequestNumber)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new LeaveRequestListItemDto
            {
                Id = x.request.Id,
                RowVersion = x.request.RowVersion,
                BusinessId = x.request.BusinessId,
                EmployeeId = x.request.EmployeeId,
                EmployeeName = (x.employee.FirstName + " " + x.employee.LastName).Trim(),
                RequestNumber = x.request.RequestNumber,
                LeaveType = x.request.LeaveType,
                Status = x.request.Status,
                StartDateUtc = x.request.StartDateUtc,
                EndDateUtc = x.request.EndDateUtc,
                RequestedMinutes = x.request.RequestedMinutes,
                SubmittedAtUtc = x.request.SubmittedAtUtc,
                ReviewedAtUtc = x.request.ReviewedAtUtc,
                ReviewNotes = x.request.ReviewNotes,
                PrivacyClassification = x.request.PrivacyClassification,
                InternalNotes = x.request.InternalNotes,
                MetadataJson = x.request.MetadataJson
            }).ToListAsync(ct).ConfigureAwait(false);
        return (items, total);
    }
}

public sealed class GetLeaveRequestDetailHandler
{
    private readonly IAppDbContext _db;
    public GetLeaveRequestDetailHandler(IAppDbContext db) => _db = db;

    public Task<LeaveRequestListItemDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return Task.FromResult<LeaveRequestListItemDto?>(null);
        return (from request in _db.Set<LeaveRequest>().AsNoTracking()
                join employee in _db.Set<Employee>().AsNoTracking() on request.EmployeeId equals employee.Id
                where request.Id == id && !request.IsDeleted
                select new LeaveRequestListItemDto
                {
                    Id = request.Id,
                    RowVersion = request.RowVersion,
                    BusinessId = request.BusinessId,
                    EmployeeId = request.EmployeeId,
                    EmployeeName = (employee.FirstName + " " + employee.LastName).Trim(),
                    RequestNumber = request.RequestNumber,
                    LeaveType = request.LeaveType,
                    Status = request.Status,
                    StartDateUtc = request.StartDateUtc,
                    EndDateUtc = request.EndDateUtc,
                    RequestedMinutes = request.RequestedMinutes,
                    SubmittedAtUtc = request.SubmittedAtUtc,
                    ReviewedAtUtc = request.ReviewedAtUtc,
                    ReviewNotes = request.ReviewNotes,
                    PrivacyClassification = request.PrivacyClassification,
                    InternalNotes = request.InternalNotes,
                    MetadataJson = request.MetadataJson
                }).FirstOrDefaultAsync(ct);
    }
}

public sealed class GetAbsenceRecordsPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;
    public GetAbsenceRecordsPageHandler(IAppDbContext db) => _db = db;

    public async Task<(List<AbsenceRecordListItemDto> Items, int Total)> HandleAsync(Guid businessId, Guid? employeeId, int page, int pageSize, AbsenceRecordQueueFilter filter, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return (new List<AbsenceRecordListItemDto>(), 0);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var rows =
            from absence in _db.Set<AbsenceRecord>().AsNoTracking()
            join employee in _db.Set<Employee>().AsNoTracking() on absence.EmployeeId equals employee.Id
            join request in _db.Set<LeaveRequest>().AsNoTracking() on absence.LeaveRequestId equals request.Id into requests
            from request in requests.DefaultIfEmpty()
            where absence.BusinessId == businessId && !absence.IsDeleted
            select new { absence, employee, request };
        if (employeeId.HasValue) rows = rows.Where(x => x.absence.EmployeeId == employeeId.Value);
        rows = filter switch
        {
            AbsenceRecordQueueFilter.Draft => rows.Where(x => x.absence.Status == AbsenceStatus.Draft),
            AbsenceRecordQueueFilter.Confirmed => rows.Where(x => x.absence.Status == AbsenceStatus.Confirmed),
            AbsenceRecordQueueFilter.Cancelled => rows.Where(x => x.absence.Status == AbsenceStatus.Cancelled),
            _ => rows
        };
        var total = await rows.CountAsync(ct).ConfigureAwait(false);
        var items = await rows.OrderByDescending(x => x.absence.StartDateUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AbsenceRecordListItemDto
            {
                Id = x.absence.Id,
                RowVersion = x.absence.RowVersion,
                BusinessId = x.absence.BusinessId,
                EmployeeId = x.absence.EmployeeId,
                EmployeeName = (x.employee.FirstName + " " + x.employee.LastName).Trim(),
                LeaveRequestId = x.absence.LeaveRequestId,
                LeaveRequestNumber = x.request != null ? x.request.RequestNumber : null,
                AbsenceType = x.absence.AbsenceType,
                Status = x.absence.Status,
                StartDateUtc = x.absence.StartDateUtc,
                EndDateUtc = x.absence.EndDateUtc,
                AbsenceMinutes = x.absence.AbsenceMinutes,
                PrivacyClassification = x.absence.PrivacyClassification,
                InternalNotes = x.absence.InternalNotes,
                MetadataJson = x.absence.MetadataJson
            }).ToListAsync(ct).ConfigureAwait(false);
        return (items, total);
    }
}

public sealed class GetAbsenceRecordDetailHandler
{
    private readonly IAppDbContext _db;
    public GetAbsenceRecordDetailHandler(IAppDbContext db) => _db = db;

    public Task<AbsenceRecordListItemDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return Task.FromResult<AbsenceRecordListItemDto?>(null);
        return (from absence in _db.Set<AbsenceRecord>().AsNoTracking()
                join employee in _db.Set<Employee>().AsNoTracking() on absence.EmployeeId equals employee.Id
                join request in _db.Set<LeaveRequest>().AsNoTracking() on absence.LeaveRequestId equals request.Id into requests
                from request in requests.DefaultIfEmpty()
                where absence.Id == id && !absence.IsDeleted
                select new AbsenceRecordListItemDto
                {
                    Id = absence.Id,
                    RowVersion = absence.RowVersion,
                    BusinessId = absence.BusinessId,
                    EmployeeId = absence.EmployeeId,
                    EmployeeName = (employee.FirstName + " " + employee.LastName).Trim(),
                    LeaveRequestId = absence.LeaveRequestId,
                    LeaveRequestNumber = request != null ? request.RequestNumber : null,
                    AbsenceType = absence.AbsenceType,
                    Status = absence.Status,
                    StartDateUtc = absence.StartDateUtc,
                    EndDateUtc = absence.EndDateUtc,
                    AbsenceMinutes = absence.AbsenceMinutes,
                    PrivacyClassification = absence.PrivacyClassification,
                    InternalNotes = absence.InternalNotes,
                    MetadataJson = absence.MetadataJson
                }).FirstOrDefaultAsync(ct);
    }
}
