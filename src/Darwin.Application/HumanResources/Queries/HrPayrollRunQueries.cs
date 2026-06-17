using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Application.HumanResources.DTOs;
using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.HumanResources.Queries;

public sealed class GetPayrollRunsPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;

    public GetPayrollRunsPageHandler(IAppDbContext db) => _db = db;

    public async Task<(List<PayrollRunListItemDto> Items, int Total)> HandleAsync(Guid businessId, int page, int pageSize, string? query, PayrollRunQueueFilter filter, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return (new List<PayrollRunListItemDto>(), 0);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var rows =
            from run in _db.Set<PayrollRun>().AsNoTracking()
            join period in _db.Set<PayrollPeriod>().AsNoTracking() on run.PayrollPeriodId equals period.Id
            where run.BusinessId == businessId && !run.IsDeleted
            select new { run, period };
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.ContainsInvariant(query);
            rows = rows.Where(x => EF.Functions.Like(x.run.RunNumber.ToUpper(), term, QueryLikePattern.EscapeCharacter)
                || EF.Functions.Like(x.period.PeriodCode.ToUpper(), term, QueryLikePattern.EscapeCharacter)
                || EF.Functions.Like(x.run.RuleSetCode.ToUpper(), term, QueryLikePattern.EscapeCharacter));
        }
        rows = filter switch
        {
            PayrollRunQueueFilter.Draft => rows.Where(x => x.run.Status == PayrollRunStatus.Draft),
            PayrollRunQueueFilter.Calculated => rows.Where(x => x.run.Status == PayrollRunStatus.Calculated),
            PayrollRunQueueFilter.Reviewed => rows.Where(x => x.run.Status == PayrollRunStatus.Reviewed),
            PayrollRunQueueFilter.Approved => rows.Where(x => x.run.Status == PayrollRunStatus.Approved),
            PayrollRunQueueFilter.Cancelled => rows.Where(x => x.run.Status == PayrollRunStatus.Cancelled),
            PayrollRunQueueFilter.Posted => rows.Where(x => x.run.Status == PayrollRunStatus.Posted),
            _ => rows
        };

        var total = await rows.CountAsync(ct).ConfigureAwait(false);
        var items = await rows.OrderByDescending(x => x.run.PeriodStartUtc)
            .ThenBy(x => x.run.RunNumber)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new PayrollRunListItemDto
            {
                Id = x.run.Id,
                RowVersion = x.run.RowVersion,
                BusinessId = x.run.BusinessId,
                PayrollPeriodId = x.run.PayrollPeriodId,
                PayrollRuleSetId = x.run.PayrollRuleSetId,
                RunNumber = x.run.RunNumber,
                Status = x.run.Status,
                PeriodCode = x.period.PeriodCode,
                RuleSetCode = x.run.RuleSetCode,
                RuleVersion = x.run.RuleVersion,
                JurisdictionCode = x.run.JurisdictionCode,
                Currency = x.run.Currency,
                PeriodStartUtc = x.run.PeriodStartUtc,
                PeriodEndUtc = x.run.PeriodEndUtc,
                EmployeeCount = x.run.EmployeeCount,
                GrossPayMinor = x.run.GrossPayMinor,
                EmployeeDeductionMinor = x.run.EmployeeDeductionMinor,
                EmployerCostMinor = x.run.EmployerCostMinor,
                NetPayMinor = x.run.NetPayMinor,
                CalculatedAtUtc = x.run.CalculatedAtUtc,
                ReviewedAtUtc = x.run.ReviewedAtUtc,
                ApprovedAtUtc = x.run.ApprovedAtUtc,
                PostedAtUtc = x.run.PostedAtUtc,
                PostingJournalEntryId = x.run.PostingJournalEntryId,
                ReviewNotes = x.run.ReviewNotes,
                SourceSnapshotJson = x.run.SourceSnapshotJson,
                MetadataJson = x.run.MetadataJson
            })
            .ToListAsync(ct).ConfigureAwait(false);
        return (items, total);
    }
}

public sealed class GetPayrollRunDetailHandler
{
    private readonly IAppDbContext _db;

    public GetPayrollRunDetailHandler(IAppDbContext db) => _db = db;

    public async Task<PayrollRunListItemDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return null;
        var dto = await (
            from run in _db.Set<PayrollRun>().AsNoTracking()
            join period in _db.Set<PayrollPeriod>().AsNoTracking() on run.PayrollPeriodId equals period.Id
            where run.Id == id && !run.IsDeleted
            select new PayrollRunListItemDto
            {
                Id = run.Id,
                RowVersion = run.RowVersion,
                BusinessId = run.BusinessId,
                PayrollPeriodId = run.PayrollPeriodId,
                PayrollRuleSetId = run.PayrollRuleSetId,
                RunNumber = run.RunNumber,
                Status = run.Status,
                PeriodCode = period.PeriodCode,
                RuleSetCode = run.RuleSetCode,
                RuleVersion = run.RuleVersion,
                JurisdictionCode = run.JurisdictionCode,
                Currency = run.Currency,
                PeriodStartUtc = run.PeriodStartUtc,
                PeriodEndUtc = run.PeriodEndUtc,
                EmployeeCount = run.EmployeeCount,
                GrossPayMinor = run.GrossPayMinor,
                EmployeeDeductionMinor = run.EmployeeDeductionMinor,
                EmployerCostMinor = run.EmployerCostMinor,
                NetPayMinor = run.NetPayMinor,
                CalculatedAtUtc = run.CalculatedAtUtc,
                ReviewedAtUtc = run.ReviewedAtUtc,
                ApprovedAtUtc = run.ApprovedAtUtc,
                PostedAtUtc = run.PostedAtUtc,
                PostingJournalEntryId = run.PostingJournalEntryId,
                ReviewNotes = run.ReviewNotes,
                SourceSnapshotJson = run.SourceSnapshotJson,
                MetadataJson = run.MetadataJson
            }).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (dto is null) return null;

        dto.Lines = await _db.Set<PayrollRunLine>().AsNoTracking()
            .Where(x => x.PayrollRunId == id && !x.IsDeleted)
            .OrderBy(x => x.EmployeeNumber)
            .Select(x => new PayrollRunLineDto
            {
                Id = x.Id,
                EmployeeId = x.EmployeeId,
                EmploymentContractId = x.EmploymentContractId,
                EmployeeNumber = x.EmployeeNumber,
                EmployeeName = x.EmployeeName,
                WorkMinutes = x.WorkMinutes,
                BreakMinutes = x.BreakMinutes,
                AbsenceMinutes = x.AbsenceMinutes,
                GrossPayMinor = x.GrossPayMinor,
                EmployeeDeductionMinor = x.EmployeeDeductionMinor,
                EmployerCostMinor = x.EmployerCostMinor,
                NetPayMinor = x.NetPayMinor
            })
            .ToListAsync(ct).ConfigureAwait(false);
        var lineIds = dto.Lines.Select(x => x.Id).ToList();
        var components = await _db.Set<PayrollRunLineComponent>().AsNoTracking()
            .Where(x => lineIds.Contains(x.PayrollRunLineId) && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ComponentCode)
            .Select(x => new { x.PayrollRunLineId, Dto = new PayrollRunLineComponentDto { Id = x.Id, ComponentCode = x.ComponentCode, DisplayName = x.DisplayName, ComponentType = x.ComponentType, CalculationMethod = x.CalculationMethod, Basis = x.Basis, AmountMinor = x.AmountMinor, IsEmployerCost = x.IsEmployerCost, SortOrder = x.SortOrder } })
            .ToListAsync(ct).ConfigureAwait(false);
        foreach (var line in dto.Lines)
        {
            line.Components = components.Where(x => x.PayrollRunLineId == line.Id).Select(x => x.Dto).ToList();
        }

        return dto;
    }
}
