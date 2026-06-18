using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Application.HumanResources.DTOs;
using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.HumanResources.Queries;

public sealed class GetPayrollPeriodsPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;
    public GetPayrollPeriodsPageHandler(IAppDbContext db) => _db = db;

    public async Task<(List<PayrollPeriodListItemDto> Items, int Total)> HandleAsync(Guid businessId, int page, int pageSize, string? query, PayrollPeriodQueueFilter filter, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return (new List<PayrollPeriodListItemDto>(), 0);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var rows = _db.Set<PayrollPeriod>().AsNoTracking().Where(x => x.BusinessId == businessId && !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.ContainsInvariant(query);
            rows = rows.Where(x => EF.Functions.Like(x.PeriodCode.ToUpper(), term, QueryLikePattern.EscapeCharacter));
        }
        rows = filter switch
        {
            PayrollPeriodQueueFilter.Draft => rows.Where(x => x.Status == PayrollPeriodStatus.Draft),
            PayrollPeriodQueueFilter.Prepared => rows.Where(x => x.Status == PayrollPeriodStatus.Prepared),
            PayrollPeriodQueueFilter.Reviewed => rows.Where(x => x.Status == PayrollPeriodStatus.Reviewed),
            PayrollPeriodQueueFilter.Approved => rows.Where(x => x.Status == PayrollPeriodStatus.Approved),
            PayrollPeriodQueueFilter.Cancelled => rows.Where(x => x.Status == PayrollPeriodStatus.Cancelled),
            _ => rows
        };
        var total = await rows.CountAsync(ct).ConfigureAwait(false);
        var items = await rows.OrderByDescending(x => x.PeriodStartUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new PayrollPeriodListItemDto
            {
                Id = x.Id,
                RowVersion = x.RowVersion,
                BusinessId = x.BusinessId,
                PeriodCode = x.PeriodCode,
                Status = x.Status,
                PeriodStartUtc = x.PeriodStartUtc,
                PeriodEndUtc = x.PeriodEndUtc,
                EmployeeCount = x.EmployeeCount,
                TotalWorkMinutes = x.TotalWorkMinutes,
                TotalBreakMinutes = x.TotalBreakMinutes,
                TotalAbsenceMinutes = x.TotalAbsenceMinutes,
                PreparedAtUtc = x.PreparedAtUtc,
                ReviewedAtUtc = x.ReviewedAtUtc,
                ApprovedAtUtc = x.ApprovedAtUtc,
                ReviewNotes = x.ReviewNotes,
                InternalNotes = x.InternalNotes,
                MetadataJson = x.MetadataJson
            }).ToListAsync(ct).ConfigureAwait(false);
        return (items, total);
    }
}

public sealed class GetPayrollPeriodDetailHandler
{
    private readonly IAppDbContext _db;
    public GetPayrollPeriodDetailHandler(IAppDbContext db) => _db = db;

    public async Task<PayrollPeriodListItemDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return null;
        var dto = await _db.Set<PayrollPeriod>().AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new PayrollPeriodListItemDto
            {
                Id = x.Id,
                RowVersion = x.RowVersion,
                BusinessId = x.BusinessId,
                PeriodCode = x.PeriodCode,
                Status = x.Status,
                PeriodStartUtc = x.PeriodStartUtc,
                PeriodEndUtc = x.PeriodEndUtc,
                EmployeeCount = x.EmployeeCount,
                TotalWorkMinutes = x.TotalWorkMinutes,
                TotalBreakMinutes = x.TotalBreakMinutes,
                TotalAbsenceMinutes = x.TotalAbsenceMinutes,
                PreparedAtUtc = x.PreparedAtUtc,
                ReviewedAtUtc = x.ReviewedAtUtc,
                ApprovedAtUtc = x.ApprovedAtUtc,
                ReviewNotes = x.ReviewNotes,
                InternalNotes = x.InternalNotes,
                MetadataJson = x.MetadataJson
            }).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (dto is null) return null;
        dto.Lines = await (
            from line in _db.Set<PayrollPeriodLine>().AsNoTracking()
            join employee in _db.Set<Employee>().AsNoTracking() on line.EmployeeId equals employee.Id
            where line.PayrollPeriodId == id && !line.IsDeleted
            orderby employee.LastName, employee.FirstName
            select new PayrollPeriodLineDto
            {
                Id = line.Id,
                EmployeeId = line.EmployeeId,
                EmployeeName = (employee.FirstName + " " + employee.LastName).Trim(),
                EmployeeNumber = employee.EmployeeNumber,
                WorkMinutes = line.WorkMinutes,
                BreakMinutes = line.BreakMinutes,
                AbsenceMinutes = line.AbsenceMinutes,
                ApprovedTimesheetCount = line.ApprovedTimesheetCount,
                ConfirmedAbsenceCount = line.ConfirmedAbsenceCount
            }).ToListAsync(ct).ConfigureAwait(false);
        return dto;
    }
}
