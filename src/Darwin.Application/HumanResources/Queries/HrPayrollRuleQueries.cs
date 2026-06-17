using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Application.HumanResources.DTOs;
using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.HumanResources.Queries;

public sealed class GetPayrollRuleSetsPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;

    public GetPayrollRuleSetsPageHandler(IAppDbContext db) => _db = db;

    public async Task<(List<PayrollRuleSetListItemDto> Items, int Total)> HandleAsync(Guid businessId, int page, int pageSize, string? query, PayrollRuleSetQueueFilter filter, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return (new List<PayrollRuleSetListItemDto>(), 0);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var rows = _db.Set<PayrollRuleSet>().AsNoTracking().Where(x => x.BusinessId == businessId && !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.ContainsInvariant(query);
            rows = rows.Where(x => EF.Functions.Like(x.RuleSetCode.ToUpper(), term, QueryLikePattern.EscapeCharacter)
                || EF.Functions.Like(x.DisplayName.ToUpper(), term, QueryLikePattern.EscapeCharacter)
                || EF.Functions.Like(x.RuleVersion.ToUpper(), term, QueryLikePattern.EscapeCharacter));
        }

        rows = filter switch
        {
            PayrollRuleSetQueueFilter.Draft => rows.Where(x => x.Status == PayrollRuleSetStatus.Draft),
            PayrollRuleSetQueueFilter.Active => rows.Where(x => x.Status == PayrollRuleSetStatus.Active),
            PayrollRuleSetQueueFilter.Archived => rows.Where(x => x.Status == PayrollRuleSetStatus.Archived),
            _ => rows
        };

        var total = await rows.CountAsync(ct).ConfigureAwait(false);
        var items = await rows
            .OrderBy(x => x.JurisdictionCode)
            .ThenBy(x => x.RuleSetCode)
            .ThenByDescending(x => x.EffectiveFromUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PayrollRuleSetListItemDto
            {
                Id = x.Id,
                RowVersion = x.RowVersion,
                BusinessId = x.BusinessId,
                JurisdictionCode = x.JurisdictionCode,
                RuleSetCode = x.RuleSetCode,
                DisplayName = x.DisplayName,
                RuleVersion = x.RuleVersion,
                Status = x.Status,
                EffectiveFromUtc = x.EffectiveFromUtc,
                EffectiveToUtc = x.EffectiveToUtc,
                Currency = x.Currency,
                Description = x.Description,
                MetadataJson = x.MetadataJson,
                ComponentCount = _db.Set<PayrollRuleComponent>().Count(c => c.PayrollRuleSetId == x.Id && !c.IsDeleted)
            })
            .ToListAsync(ct).ConfigureAwait(false);
        return (items, total);
    }
}

public sealed class GetPayrollRuleSetDetailHandler
{
    private readonly IAppDbContext _db;

    public GetPayrollRuleSetDetailHandler(IAppDbContext db) => _db = db;

    public async Task<PayrollRuleSetListItemDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return null;
        var dto = await _db.Set<PayrollRuleSet>().AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new PayrollRuleSetListItemDto
            {
                Id = x.Id,
                RowVersion = x.RowVersion,
                BusinessId = x.BusinessId,
                JurisdictionCode = x.JurisdictionCode,
                RuleSetCode = x.RuleSetCode,
                DisplayName = x.DisplayName,
                RuleVersion = x.RuleVersion,
                Status = x.Status,
                EffectiveFromUtc = x.EffectiveFromUtc,
                EffectiveToUtc = x.EffectiveToUtc,
                Currency = x.Currency,
                Description = x.Description,
                MetadataJson = x.MetadataJson
            })
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (dto is null) return null;

        dto.Components = await _db.Set<PayrollRuleComponent>().AsNoTracking()
            .Where(x => x.PayrollRuleSetId == id && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ComponentCode)
            .Select(x => new PayrollRuleComponentDto
            {
                Id = x.Id,
                RowVersion = x.RowVersion,
                BusinessId = x.BusinessId,
                PayrollRuleSetId = x.PayrollRuleSetId,
                ComponentCode = x.ComponentCode,
                DisplayName = x.DisplayName,
                ComponentType = x.ComponentType,
                CalculationMethod = x.CalculationMethod,
                Basis = x.Basis,
                RateBasisPoints = x.RateBasisPoints,
                AmountMinor = x.AmountMinor,
                ThresholdJson = x.ThresholdJson,
                IsEmployerCost = x.IsEmployerCost,
                SortOrder = x.SortOrder,
                MetadataJson = x.MetadataJson
            })
            .ToListAsync(ct).ConfigureAwait(false);
        dto.ComponentCount = dto.Components.Count;
        return dto;
    }
}
