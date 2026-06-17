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

public sealed class CreatePayrollRuleSetHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<PayrollRuleSetEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreatePayrollRuleSetHandler(IAppDbContext db, IValidator<PayrollRuleSetEditDto> validator, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _validator = validator;
        _clock = clock;
        _events = events;
    }

    public async Task<Guid> HandleAsync(PayrollRuleSetEditDto dto, CancellationToken ct = default)
    {
        PayrollRuleSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        PayrollRuleSupport.EnsureSafe(dto.JurisdictionCode, dto.RuleSetCode, dto.DisplayName, dto.RuleVersion, dto.Currency, dto.Description, dto.MetadataJson);
        await PayrollRuleSupport.EnsureRuleSetAvailableAsync(_db, dto.BusinessId, dto.JurisdictionCode, dto.RuleSetCode, dto.RuleVersion, dto.EffectiveFromUtc, dto.EffectiveToUtc, null, ct).ConfigureAwait(false);

        var entity = new PayrollRuleSet
        {
            BusinessId = dto.BusinessId,
            JurisdictionCode = dto.JurisdictionCode,
            RuleSetCode = dto.RuleSetCode,
            DisplayName = dto.DisplayName,
            RuleVersion = dto.RuleVersion,
            Status = dto.Status,
            EffectiveFromUtc = dto.EffectiveFromUtc.Date,
            EffectiveToUtc = dto.EffectiveToUtc?.Date,
            Currency = dto.Currency,
            Description = dto.Description,
            MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson)
        };

        _db.Set<PayrollRuleSet>().Add(entity);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "PayrollRuleSet", entity.Id, "hr.payroll_rule_set.created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return entity.Id;
    }
}

public sealed class UpdatePayrollRuleSetHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<PayrollRuleSetEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public UpdatePayrollRuleSetHandler(IAppDbContext db, IValidator<PayrollRuleSetEditDto> validator, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _validator = validator;
        _clock = clock;
        _events = events;
    }

    public async Task HandleAsync(PayrollRuleSetEditDto dto, CancellationToken ct = default)
    {
        PayrollRuleSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        PayrollRuleSupport.EnsureSafe(dto.JurisdictionCode, dto.RuleSetCode, dto.DisplayName, dto.RuleVersion, dto.Currency, dto.Description, dto.MetadataJson);

        var entity = await _db.Set<PayrollRuleSet>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (entity is null) throw new InvalidOperationException("PayrollRuleSetNotFound");
        HrCoreSupport.EnsureRowVersion(entity.RowVersion, dto.RowVersion);
        await PayrollRuleSupport.EnsureRuleSetAvailableAsync(_db, dto.BusinessId, dto.JurisdictionCode, dto.RuleSetCode, dto.RuleVersion, dto.EffectiveFromUtc, dto.EffectiveToUtc, dto.Id, ct).ConfigureAwait(false);

        entity.BusinessId = dto.BusinessId;
        entity.JurisdictionCode = dto.JurisdictionCode;
        entity.RuleSetCode = dto.RuleSetCode;
        entity.DisplayName = dto.DisplayName;
        entity.RuleVersion = dto.RuleVersion;
        entity.Status = dto.Status;
        entity.EffectiveFromUtc = dto.EffectiveFromUtc.Date;
        entity.EffectiveToUtc = dto.EffectiveToUtc?.Date;
        entity.Currency = dto.Currency;
        entity.Description = dto.Description;
        entity.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);

        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "PayrollRuleSet", entity.Id, "hr.payroll_rule_set.updated", AuditTrailAction.Updated, ct).ConfigureAwait(false);
    }
}

public sealed class ArchivePayrollRuleSetHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public ArchivePayrollRuleSetHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _clock = clock;
        _events = events;
    }

    public async Task<Result> HandleAsync(HrArchiveDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Set<PayrollRuleSet>().Include(x => x.Components).FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (entity is null) return Result.Fail("PayrollRuleSetNotFound");
        if (!HrCoreSupport.RowVersionMatches(entity.RowVersion, dto.RowVersion)) return Result.Fail("ItemConcurrencyConflict");

        entity.Status = PayrollRuleSetStatus.Archived;
        entity.IsDeleted = true;
        foreach (var component in entity.Components.Where(x => !x.IsDeleted))
        {
            component.IsDeleted = true;
        }

        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "PayrollRuleSet", entity.Id, "hr.payroll_rule_set.archived", AuditTrailAction.Deleted, ct).ConfigureAwait(false);
        return Result.Ok();
    }
}

public sealed class UpsertPayrollRuleComponentHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<PayrollRuleComponentDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public UpsertPayrollRuleComponentHandler(IAppDbContext db, IValidator<PayrollRuleComponentDto> validator, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _validator = validator;
        _clock = clock;
        _events = events;
    }

    public async Task<Result> HandleAsync(PayrollRuleComponentDto dto, CancellationToken ct = default)
    {
        PayrollRuleSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        PayrollRuleSupport.EnsureSafe(dto.ComponentCode, dto.DisplayName, dto.ThresholdJson, dto.MetadataJson);

        var ruleSet = await _db.Set<PayrollRuleSet>().FirstOrDefaultAsync(x => x.Id == dto.PayrollRuleSetId && !x.IsDeleted, ct).ConfigureAwait(false);
        if (ruleSet is null) return Result.Fail("PayrollRuleSetNotFound");
        if (ruleSet.BusinessId != dto.BusinessId) return Result.Fail("PayrollRuleBusinessMismatch");
        await PayrollRuleSupport.EnsureComponentAvailableAsync(_db, dto.PayrollRuleSetId, dto.ComponentCode, dto.Id == Guid.Empty ? null : dto.Id, ct).ConfigureAwait(false);

        PayrollRuleComponent entity;
        var auditAction = AuditTrailAction.Created;
        var eventType = "hr.payroll_rule_component.created";
        if (dto.Id == Guid.Empty)
        {
            entity = new PayrollRuleComponent { PayrollRuleSetId = dto.PayrollRuleSetId, BusinessId = dto.BusinessId };
            _db.Set<PayrollRuleComponent>().Add(entity);
        }
        else
        {
            entity = await _db.Set<PayrollRuleComponent>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false) ?? throw new InvalidOperationException("PayrollRuleComponentNotFound");
            if (!HrCoreSupport.RowVersionMatches(entity.RowVersion, dto.RowVersion)) return Result.Fail("ItemConcurrencyConflict");
            auditAction = AuditTrailAction.Updated;
            eventType = "hr.payroll_rule_component.updated";
        }

        entity.BusinessId = dto.BusinessId;
        entity.PayrollRuleSetId = dto.PayrollRuleSetId;
        entity.ComponentCode = dto.ComponentCode;
        entity.DisplayName = dto.DisplayName;
        entity.ComponentType = dto.ComponentType;
        entity.CalculationMethod = dto.CalculationMethod;
        entity.Basis = dto.Basis;
        entity.RateBasisPoints = dto.RateBasisPoints;
        entity.AmountMinor = dto.AmountMinor;
        entity.ThresholdJson = HrCoreSupport.NormalizeMetadataJson(dto.ThresholdJson);
        entity.IsEmployerCost = dto.IsEmployerCost;
        entity.SortOrder = dto.SortOrder;
        entity.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);

        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "PayrollRuleComponent", entity.Id, eventType, auditAction, ct).ConfigureAwait(false);
        return Result.Ok();
    }
}

public sealed class ArchivePayrollRuleComponentHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public ArchivePayrollRuleComponentHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _clock = clock;
        _events = events;
    }

    public async Task<Result> HandleAsync(HrArchiveDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Set<PayrollRuleComponent>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (entity is null) return Result.Fail("PayrollRuleComponentNotFound");
        if (!HrCoreSupport.RowVersionMatches(entity.RowVersion, dto.RowVersion)) return Result.Fail("ItemConcurrencyConflict");
        entity.IsDeleted = true;
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "PayrollRuleComponent", entity.Id, "hr.payroll_rule_component.archived", AuditTrailAction.Deleted, ct).ConfigureAwait(false);
        return Result.Ok();
    }
}

internal static class PayrollRuleSupport
{
    public static void Normalize(PayrollRuleSetEditDto dto)
    {
        dto.JurisdictionCode = HrCoreSupport.NormalizeCode(string.IsNullOrWhiteSpace(dto.JurisdictionCode) ? "DE" : dto.JurisdictionCode);
        dto.RuleSetCode = HrCoreSupport.NormalizeCode(dto.RuleSetCode);
        dto.DisplayName = HrCoreSupport.Required(dto.DisplayName);
        dto.RuleVersion = HrCoreSupport.Required(dto.RuleVersion).ToUpperInvariant();
        dto.EffectiveFromUtc = dto.EffectiveFromUtc.Date;
        dto.EffectiveToUtc = dto.EffectiveToUtc?.Date;
        dto.Currency = HrCoreSupport.NormalizeCode(string.IsNullOrWhiteSpace(dto.Currency) ? "EUR" : dto.Currency);
        dto.Description = HrCoreSupport.Optional(dto.Description);
        dto.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
    }

    public static void Normalize(PayrollRuleComponentDto dto)
    {
        dto.ComponentCode = HrCoreSupport.NormalizeCode(dto.ComponentCode);
        dto.DisplayName = HrCoreSupport.Required(dto.DisplayName);
        dto.ThresholdJson = HrCoreSupport.NormalizeMetadataJson(dto.ThresholdJson);
        dto.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
    }

    public static void EnsureSafe(params string?[] values) => HrCoreSupport.EnsureSafe(values);

    public static async Task EnsureRuleSetAvailableAsync(IAppDbContext db, Guid businessId, string jurisdictionCode, string ruleSetCode, string version, DateTime effectiveFrom, DateTime? effectiveTo, Guid? excludingId, CancellationToken ct)
    {
        var duplicate = db.Set<PayrollRuleSet>().AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.JurisdictionCode == jurisdictionCode && x.RuleSetCode == ruleSetCode && x.RuleVersion == version && !x.IsDeleted);
        var overlap = db.Set<PayrollRuleSet>().AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.JurisdictionCode == jurisdictionCode && x.RuleSetCode == ruleSetCode && x.Status != PayrollRuleSetStatus.Archived && !x.IsDeleted);
        if (excludingId.HasValue)
        {
            duplicate = duplicate.Where(x => x.Id != excludingId.Value);
            overlap = overlap.Where(x => x.Id != excludingId.Value);
        }

        if (await duplicate.AnyAsync(ct).ConfigureAwait(false)) throw new InvalidOperationException("PayrollRuleSetDuplicate");

        var start = effectiveFrom.Date;
        var end = effectiveTo?.Date ?? DateTime.MaxValue.Date;
        var hasOverlap = await overlap.AnyAsync(x => x.EffectiveFromUtc <= end && (x.EffectiveToUtc ?? DateTime.MaxValue) >= start, ct).ConfigureAwait(false);
        if (hasOverlap) throw new InvalidOperationException("PayrollRuleSetEffectiveDateOverlap");
    }

    public static async Task EnsureComponentAvailableAsync(IAppDbContext db, Guid ruleSetId, string componentCode, Guid? excludingId, CancellationToken ct)
    {
        var query = db.Set<PayrollRuleComponent>().AsNoTracking().Where(x => x.PayrollRuleSetId == ruleSetId && x.ComponentCode == componentCode && !x.IsDeleted);
        if (excludingId.HasValue) query = query.Where(x => x.Id != excludingId.Value);
        if (await query.AnyAsync(ct).ConfigureAwait(false)) throw new InvalidOperationException("PayrollRuleComponentDuplicate");
    }
}
