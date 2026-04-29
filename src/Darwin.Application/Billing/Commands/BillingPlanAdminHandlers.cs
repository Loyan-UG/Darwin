using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Billing.Validators;
using Darwin.Domain.Entities.Billing;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Billing.Commands;

public sealed class CreateBillingPlanHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<BillingPlanCreateDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public CreateBillingPlanHandler(
        IAppDbContext db,
        IValidator<BillingPlanCreateDto> validator,
        IStringLocalizer<ValidationResource> localizer)
    {
        _db = db;
        _validator = validator;
        _localizer = localizer;
    }

    public async Task<Guid> HandleAsync(BillingPlanCreateDto dto, CancellationToken ct = default)
    {
        var validation = _validator.Validate(dto);
        if (!validation.IsValid) throw new ValidationException(validation.Errors);

        var normalizedCode = dto.Code.Trim().ToUpperInvariant();
        var exists = await _db.Set<BillingPlan>()
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.Code == normalizedCode, ct)
            .ConfigureAwait(false);
        if (exists)
        {
            throw new ValidationException(_localizer["BillingPlanCodeMustBeUnique"]);
        }

        var entity = new BillingPlan
        {
            Code = normalizedCode,
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            PriceMinor = dto.PriceMinor,
            Currency = dto.Currency.Trim().ToUpperInvariant(),
            Interval = dto.Interval,
            IntervalCount = dto.IntervalCount,
            TrialDays = dto.TrialDays,
            IsActive = dto.IsActive,
            FeaturesJson = string.IsNullOrWhiteSpace(dto.FeaturesJson) ? "{}" : dto.FeaturesJson.Trim()
        };

        _db.Set<BillingPlan>().Add(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return entity.Id;
    }
}

public sealed class UpdateBillingPlanHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<BillingPlanEditDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public UpdateBillingPlanHandler(
        IAppDbContext db,
        IValidator<BillingPlanEditDto> validator,
        IStringLocalizer<ValidationResource> localizer)
    {
        _db = db;
        _validator = validator;
        _localizer = localizer;
    }

    public async Task HandleAsync(BillingPlanEditDto dto, CancellationToken ct = default)
    {
        var validation = _validator.Validate(dto);
        if (!validation.IsValid) throw new ValidationException(validation.Errors);

        var entity = await _db.Set<BillingPlan>()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == dto.Id, ct)
            .ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException(_localizer["BillingPlanNotFound"]);
        }

        var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
        var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
        if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
        {
            throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
        }

        var normalizedCode = dto.Code.Trim().ToUpperInvariant();
        var exists = await _db.Set<BillingPlan>()
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.Id != dto.Id && x.Code == normalizedCode, ct)
            .ConfigureAwait(false);
        if (exists)
        {
            throw new ValidationException(_localizer["BillingPlanCodeMustBeUnique"]);
        }

        entity.Code = normalizedCode;
        entity.Name = dto.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        entity.PriceMinor = dto.PriceMinor;
        entity.Currency = dto.Currency.Trim().ToUpperInvariant();
        entity.Interval = dto.Interval;
        entity.IntervalCount = dto.IntervalCount;
        entity.TrialDays = dto.TrialDays;
        entity.IsActive = dto.IsActive;
        entity.FeaturesJson = string.IsNullOrWhiteSpace(dto.FeaturesJson) ? "{}" : dto.FeaturesJson.Trim();

        try
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
        }
    }
}
