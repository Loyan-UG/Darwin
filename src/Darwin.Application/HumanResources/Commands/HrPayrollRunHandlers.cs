using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.Services;
using Darwin.Application.Foundation;
using Darwin.Application.HumanResources.DTOs;
using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.HumanResources.Commands;

public sealed class CreatePayrollRunHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreatePayrollRunHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _clock = clock;
        _events = events;
    }

    public async Task<Guid> HandleAsync(PayrollRunCreateDto dto, CancellationToken ct = default)
    {
        PayrollRunSupport.Normalize(dto);
        PayrollRunSupport.EnsureSafe(dto.ReviewNotes, dto.MetadataJson);
        if (dto.BusinessId == Guid.Empty || dto.PayrollPeriodId == Guid.Empty || dto.PayrollRuleSetId == Guid.Empty) throw new ArgumentException("PayrollRunInvalidIds");

        var period = await _db.Set<PayrollPeriod>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == dto.PayrollPeriodId && !x.IsDeleted, ct).ConfigureAwait(false);
        if (period is null) throw new InvalidOperationException("PayrollPeriodNotFound");
        if (period.BusinessId != dto.BusinessId || period.Status != PayrollPeriodStatus.Approved) throw new InvalidOperationException("PayrollPeriodNotApproved");

        var ruleSet = await _db.Set<PayrollRuleSet>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == dto.PayrollRuleSetId && !x.IsDeleted, ct).ConfigureAwait(false);
        if (ruleSet is null) throw new InvalidOperationException("PayrollRuleSetNotFound");
        if (ruleSet.BusinessId != dto.BusinessId || ruleSet.Status != PayrollRuleSetStatus.Active) throw new InvalidOperationException("PayrollRuleSetNotActive");
        if (ruleSet.EffectiveFromUtc.Date > period.PeriodEndUtc.Date || (ruleSet.EffectiveToUtc?.Date ?? DateTime.MaxValue.Date) < period.PeriodStartUtc.Date) throw new InvalidOperationException("PayrollRuleSetNotEffectiveForPeriod");

        var exists = await _db.Set<PayrollRun>().AnyAsync(x => x.BusinessId == dto.BusinessId && x.PayrollPeriodId == dto.PayrollPeriodId && x.PayrollRuleSetId == dto.PayrollRuleSetId && !x.IsDeleted, ct).ConfigureAwait(false);
        if (exists) throw new InvalidOperationException("PayrollRunDuplicate");

        var run = new PayrollRun
        {
            Id = Guid.NewGuid(),
            BusinessId = dto.BusinessId,
            PayrollPeriodId = period.Id,
            PayrollRuleSetId = ruleSet.Id,
            RunNumber = PayrollRunSupport.BuildRunNumber(period, ruleSet),
            Status = PayrollRunStatus.Draft,
            JurisdictionCode = ruleSet.JurisdictionCode,
            RuleSetCode = ruleSet.RuleSetCode,
            RuleVersion = ruleSet.RuleVersion,
            Currency = ruleSet.Currency,
            PeriodStartUtc = period.PeriodStartUtc.Date,
            PeriodEndUtc = period.PeriodEndUtc.Date,
            ReviewNotes = dto.ReviewNotes,
            SourceSnapshotJson = PayrollRunSupport.Json(new { source = "approved-payroll-period", period.Id, period.PeriodCode, ruleSet.RuleSetCode, ruleSet.RuleVersion }),
            MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson)
        };

        _db.Set<PayrollRun>().Add(run);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, run.BusinessId, "PayrollRun", run.Id, "hr.payroll_run.created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return run.Id;
    }
}

public sealed class CalculatePayrollRunHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CalculatePayrollRunHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _clock = clock;
        _events = events;
    }

    public async Task<Result> HandleAsync(PayrollRunLifecycleDto dto, CancellationToken ct = default)
    {
        var run = await _db.Set<PayrollRun>().Include(x => x.Lines).ThenInclude(x => x.Components).FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (run is null) return Result.Fail("PayrollRunNotFound");
        if (!HrCoreSupport.RowVersionMatches(run.RowVersion, dto.RowVersion)) return Result.Fail("ItemConcurrencyConflict");
        if (run.Status is PayrollRunStatus.Approved or PayrollRunStatus.Cancelled or PayrollRunStatus.Posted) return Result.Fail("PayrollRunLocked");

        var period = await _db.Set<PayrollPeriod>().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == run.PayrollPeriodId && !x.IsDeleted, ct).ConfigureAwait(false);
        if (period is null || period.Status != PayrollPeriodStatus.Approved) return Result.Fail("PayrollPeriodNotApproved");
        var ruleSet = await _db.Set<PayrollRuleSet>().Include(x => x.Components).FirstOrDefaultAsync(x => x.Id == run.PayrollRuleSetId && !x.IsDeleted, ct).ConfigureAwait(false);
        if (ruleSet is null || ruleSet.Status != PayrollRuleSetStatus.Active) return Result.Fail("PayrollRuleSetNotActive");

        var employeeIds = period.Lines.Where(x => !x.IsDeleted).Select(x => x.EmployeeId).Distinct().ToList();
        var employees = await _db.Set<Employee>().AsNoTracking().Where(x => employeeIds.Contains(x.Id) && !x.IsDeleted).ToListAsync(ct).ConfigureAwait(false);
        var contracts = await _db.Set<EmploymentContract>().AsNoTracking()
            .Where(x => employeeIds.Contains(x.EmployeeId) && x.BusinessId == run.BusinessId && !x.IsDeleted && x.Status == EmploymentContractStatus.Active && x.StartDateUtc <= period.PeriodEndUtc && (x.EndDateUtc == null || x.EndDateUtc >= period.PeriodStartUtc))
            .OrderByDescending(x => x.StartDateUtc)
            .ToListAsync(ct).ConfigureAwait(false);

        _db.Set<PayrollRunLineComponent>().RemoveRange(run.Lines.SelectMany(x => x.Components));
        _db.Set<PayrollRunLine>().RemoveRange(run.Lines);
        run.Lines = new List<PayrollRunLine>();

        foreach (var periodLine in period.Lines.Where(x => !x.IsDeleted).OrderBy(x => x.EmployeeId))
        {
            var employee = employees.FirstOrDefault(x => x.Id == periodLine.EmployeeId);
            if (employee is null) return Result.Fail("PayrollRunEmployeeMissing");
            var contract = contracts.FirstOrDefault(x => x.EmployeeId == employee.Id);
            var line = PayrollRunSupport.BuildLine(run, periodLine, employee, contract, ruleSet.Components.Where(x => !x.IsDeleted).OrderBy(x => x.SortOrder).ThenBy(x => x.ComponentCode).ToList());
            run.Lines.Add(line);
        }

        run.EmployeeCount = run.Lines.Count;
        run.GrossPayMinor = run.Lines.Sum(x => x.GrossPayMinor);
        run.EmployeeDeductionMinor = run.Lines.Sum(x => x.EmployeeDeductionMinor);
        run.EmployerCostMinor = run.Lines.Sum(x => x.EmployerCostMinor);
        run.NetPayMinor = run.Lines.Sum(x => x.NetPayMinor);
        run.Status = PayrollRunStatus.Calculated;
        run.CalculatedAtUtc = _clock.UtcNow;
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, run.BusinessId, "PayrollRun", run.Id, "hr.payroll_run.calculated", AuditTrailAction.StatusChanged, ct).ConfigureAwait(false);
        return Result.Ok();
    }
}

public sealed class UpdatePayrollRunLifecycleHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public UpdatePayrollRunLifecycleHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _clock = clock;
        _events = events;
    }

    public async Task<Result> HandleAsync(PayrollRunLifecycleDto dto, PayrollRunStatus target, CancellationToken ct = default)
    {
        dto.Notes = HrCoreSupport.Optional(dto.Notes);
        PayrollRunSupport.EnsureSafe(dto.Notes);
        var run = await _db.Set<PayrollRun>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (run is null) return Result.Fail("PayrollRunNotFound");
        if (!HrCoreSupport.RowVersionMatches(run.RowVersion, dto.RowVersion)) return Result.Fail("ItemConcurrencyConflict");
        if (!PayrollRunSupport.CanTransition(run.Status, target)) return Result.Fail("PayrollRunInvalidTransition");
        var now = _clock.UtcNow;
        run.Status = target;
        run.ReviewNotes = dto.Notes ?? run.ReviewNotes;
        if (target == PayrollRunStatus.Reviewed) run.ReviewedAtUtc = now;
        if (target == PayrollRunStatus.Approved) run.ApprovedAtUtc = now;
        if (target == PayrollRunStatus.Cancelled) run.IsDeleted = true;
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, run.BusinessId, "PayrollRun", run.Id, $"hr.payroll_run.{target.ToString().ToLowerInvariant()}", AuditTrailAction.StatusChanged, ct).ConfigureAwait(false);
        return Result.Ok();
    }
}

public sealed class PostPayrollRunHandler
{
    public const string PostingKeyPrefix = "payroll-run-posted";

    private static readonly FinancePostingAccountRole[] RequiredRoles =
    [
        FinancePostingAccountRole.PayrollExpense,
        FinancePostingAccountRole.EmployerPayrollTaxExpense,
        FinancePostingAccountRole.PayrollPayable,
        FinancePostingAccountRole.PayrollTaxPayable,
        FinancePostingAccountRole.SocialInsurancePayable
    ];

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly FinanceAccountMappingService _accounts;
    private readonly FinancePostingService _posting;
    private readonly BusinessEventService? _events;

    public PostPayrollRunHandler(IAppDbContext db, IClock clock, FinanceAccountMappingService accounts, FinancePostingService posting, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _posting = posting ?? throw new ArgumentNullException(nameof(posting));
        _events = events;
    }

    public async Task<Result> HandleAsync(PayrollRunLifecycleDto dto, CancellationToken ct = default)
    {
        if (dto.Id == Guid.Empty || dto.RowVersion.Length == 0) return Result.Fail("PayrollRunNotFound");
        var run = await _db.Set<PayrollRun>()
            .Include(x => x.Lines)
            .ThenInclude(x => x.Components)
            .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (run is null) return Result.Fail("PayrollRunNotFound");
        if (!HrCoreSupport.RowVersionMatches(run.RowVersion, dto.RowVersion)) return Result.Fail("ItemConcurrencyConflict");
        if (run.Status == PayrollRunStatus.Posted && run.PostingJournalEntryId.HasValue) return Result.Ok();
        if (run.Status != PayrollRunStatus.Approved) return Result.Fail("PayrollRunMustBeApprovedForPosting");
        if (run.Lines.Count == 0 || run.Lines.Any(x => x.IsDeleted)) return Result.Fail("PayrollRunPostingRequiresLines");
        if (run.GrossPayMinor <= 0 || run.NetPayMinor < 0) return Result.Fail("PayrollRunPostingRequiresPositiveGross");

        var accountResult = await _accounts.ResolveRequiredAccountsAsync(run.BusinessId, RequiredRoles, ct).ConfigureAwait(false);
        if (!accountResult.Succeeded) return Result.Fail(accountResult.Error ?? "PayrollPostingAccountMappingMissing");

        var lines = BuildPostingLines(run, accountResult.Value!);
        var postingResult = await _posting.PostAsync(new FinancePostingCommand(
            run.BusinessId,
            run.PeriodEndUtc,
            JournalEntryPostingKind.PayrollRunPosted,
            $"{PostingKeyPrefix}:{run.Id}",
            "PayrollRun",
            run.Id,
            $"Payroll run {run.RunNumber}",
            lines,
            SourceDocumentNumber: run.RunNumber,
            PostingReason: "Payroll liability posting",
            MetadataJson: $$"""{"payrollRunId":"{{run.Id}}","runNumber":"{{run.RunNumber}}","jurisdiction":"{{run.JurisdictionCode}}","ruleVersion":"{{run.RuleVersion}}","currency":"{{run.Currency}}","grossPayMinor":{{run.GrossPayMinor}},"netPayMinor":{{run.NetPayMinor}}}"""), ct).ConfigureAwait(false);
        if (!postingResult.Succeeded) return Result.Fail(postingResult.Error ?? "PayrollPostingFailed");

        run.PostingJournalEntryId = postingResult.Value!.JournalEntryId;
        run.PostedAtUtc ??= _clock.UtcNow;
        run.Status = PayrollRunStatus.Posted;
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, run.BusinessId, "PayrollRun", run.Id, "hr.payroll_run.posted", AuditTrailAction.StatusChanged, ct).ConfigureAwait(false);
        return Result.Ok();
    }

    private static IReadOnlyList<FinancePostingLineCommand> BuildPostingLines(PayrollRun run, IReadOnlyDictionary<FinancePostingAccountRole, Guid> accounts)
    {
        var employeeTaxWithholding = SumComponents(run, PayrollRuleComponentType.TaxWithholding);
        var employeeSocialInsurance = SumComponents(run, PayrollRuleComponentType.SocialInsuranceEmployee);
        var otherDeductions = SumComponents(run, PayrollRuleComponentType.Deduction);

        var lines = new List<FinancePostingLineCommand>
        {
            new(accounts[FinancePostingAccountRole.PayrollExpense], run.GrossPayMinor, 0, "Payroll gross expense")
        };

        if (run.EmployerCostMinor > 0)
        {
            lines.Add(new FinancePostingLineCommand(accounts[FinancePostingAccountRole.EmployerPayrollTaxExpense], run.EmployerCostMinor, 0, "Employer payroll cost"));
        }

        if (run.NetPayMinor > 0)
        {
            lines.Add(new FinancePostingLineCommand(accounts[FinancePostingAccountRole.PayrollPayable], 0, run.NetPayMinor, "Net salary payable"));
        }

        if (employeeTaxWithholding > 0)
        {
            lines.Add(new FinancePostingLineCommand(accounts[FinancePostingAccountRole.PayrollTaxPayable], 0, employeeTaxWithholding, "Payroll tax payable"));
        }

        var socialPayable = employeeSocialInsurance + run.EmployerCostMinor;
        if (socialPayable > 0)
        {
            lines.Add(new FinancePostingLineCommand(accounts[FinancePostingAccountRole.SocialInsurancePayable], 0, socialPayable, "Social insurance payable"));
        }

        if (otherDeductions > 0)
        {
            lines.Add(new FinancePostingLineCommand(accounts[FinancePostingAccountRole.PayrollTaxPayable], 0, otherDeductions, "Other payroll deductions payable"));
        }

        return lines;
    }

    private static long SumComponents(PayrollRun run, params PayrollRuleComponentType[] types)
    {
        var set = types.ToHashSet();
        return run.Lines
            .Where(x => !x.IsDeleted)
            .SelectMany(x => x.Components)
            .Where(x => !x.IsDeleted && set.Contains(x.ComponentType))
            .Sum(x => x.AmountMinor);
    }
}

internal static class PayrollRunSupport
{
    public static void Normalize(PayrollRunCreateDto dto)
    {
        dto.ReviewNotes = HrCoreSupport.Optional(dto.ReviewNotes);
        dto.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
    }

    public static void EnsureSafe(params string?[] values) => HrCoreSupport.EnsureSafe(values);

    public static string BuildRunNumber(PayrollPeriod period, PayrollRuleSet ruleSet) =>
        HrCoreSupport.NormalizeCode($"PR-{period.PeriodCode}-{ruleSet.JurisdictionCode}-{ruleSet.RuleVersion}");

    public static PayrollRunLine BuildLine(PayrollRun run, PayrollPeriodLine periodLine, Employee employee, EmploymentContract? contract, List<PayrollRuleComponent> components)
    {
        var line = new PayrollRunLine
        {
            Id = Guid.NewGuid(),
            BusinessId = run.BusinessId,
            PayrollRunId = run.Id,
            EmployeeId = employee.Id,
            EmploymentContractId = contract?.Id,
            EmployeeNumber = employee.EmployeeNumber,
            EmployeeName = $"{employee.FirstName} {employee.LastName}".Trim(),
            WorkMinutes = periodLine.WorkMinutes,
            BreakMinutes = periodLine.BreakMinutes,
            AbsenceMinutes = periodLine.AbsenceMinutes,
            EmployeeSnapshotJson = Json(new { employee.Id, employee.EmployeeNumber, employee.FirstName, employee.LastName, employee.DepartmentId, employee.PositionId }),
            ContractSnapshotJson = contract is null ? null : Json(new { contract.Id, contract.ContractNumber, contract.EmploymentType, contract.WeeklyHoursMinor, contract.StartDateUtc, contract.EndDateUtc }),
            PeriodLineSnapshotJson = Json(new { periodLine.Id, periodLine.WorkMinutes, periodLine.BreakMinutes, periodLine.AbsenceMinutes, periodLine.ApprovedTimesheetCount, periodLine.ConfirmedAbsenceCount })
        };

        long gross = 0;
        long deductions = 0;
        long employerCost = 0;

        foreach (var component in components)
        {
            var basis = component.Basis switch
            {
                PayrollRuleBasis.GrossPay => gross,
                PayrollRuleBasis.TaxableIncome => Math.Max(0, gross - deductions),
                PayrollRuleBasis.HoursWorked => periodLine.WorkMinutes,
                PayrollRuleBasis.ContractRate => contract?.WeeklyHoursMinor ?? 0,
                PayrollRuleBasis.EmployerCost => employerCost,
                _ => 0
            };
            var amount = component.CalculationMethod switch
            {
                PayrollRuleCalculationMethod.FixedAmount => component.AmountMinor ?? 0,
                PayrollRuleCalculationMethod.Percentage => component.RateBasisPoints.HasValue ? checked(basis * component.RateBasisPoints.Value / 10000) : 0,
                _ => 0
            };
            amount = Math.Max(0, amount);
            var result = new PayrollRunLineComponent
            {
                Id = Guid.NewGuid(),
                BusinessId = run.BusinessId,
                PayrollRunId = run.Id,
                PayrollRunLineId = line.Id,
                PayrollRuleComponentId = component.Id,
                ComponentCode = component.ComponentCode,
                DisplayName = component.DisplayName,
                ComponentType = component.ComponentType,
                CalculationMethod = component.CalculationMethod,
                Basis = component.Basis,
                AmountMinor = amount,
                IsEmployerCost = component.IsEmployerCost,
                SortOrder = component.SortOrder,
                RuleSnapshotJson = Json(new { component.Id, component.ComponentCode, component.ComponentType, component.CalculationMethod, component.Basis, component.RateBasisPoints, component.AmountMinor, component.ThresholdJson })
            };
            line.Components.Add(result);

            if (component.IsEmployerCost || component.ComponentType is PayrollRuleComponentType.SocialInsuranceEmployer or PayrollRuleComponentType.EmployerCost)
            {
                employerCost += amount;
            }
            else if (component.ComponentType is PayrollRuleComponentType.GrossPay or PayrollRuleComponentType.Allowance)
            {
                gross += amount;
            }
            else
            {
                deductions += amount;
            }
        }

        line.GrossPayMinor = gross;
        line.EmployeeDeductionMinor = deductions;
        line.EmployerCostMinor = employerCost;
        line.NetPayMinor = Math.Max(0, gross - deductions);
        return line;
    }

    public static bool CanTransition(PayrollRunStatus current, PayrollRunStatus target) => (current, target) switch
    {
        (PayrollRunStatus.Calculated, PayrollRunStatus.Reviewed) => true,
        (PayrollRunStatus.Calculated, PayrollRunStatus.Approved) => true,
        (PayrollRunStatus.Reviewed, PayrollRunStatus.Approved) => true,
        (PayrollRunStatus.Draft, PayrollRunStatus.Cancelled) => true,
        (PayrollRunStatus.Calculated, PayrollRunStatus.Cancelled) => true,
        (PayrollRunStatus.Reviewed, PayrollRunStatus.Cancelled) => true,
        (PayrollRunStatus.Approved, PayrollRunStatus.Cancelled) => false,
        _ => false
    };

    public static string Json(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
}
