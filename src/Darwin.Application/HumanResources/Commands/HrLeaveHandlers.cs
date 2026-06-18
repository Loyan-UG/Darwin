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

public sealed class CreateLeaveRequestHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<LeaveRequestEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;
    public CreateLeaveRequestHandler(IAppDbContext db, IValidator<LeaveRequestEditDto> validator, IClock clock, BusinessEventService? events = null) { _db = db; _validator = validator; _clock = clock; _events = events; }

    public async Task<Guid> HandleAsync(LeaveRequestEditDto dto, CancellationToken ct = default)
    {
        HrLeaveSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrLeaveSupport.EnsureSafe(dto.RequestNumber, dto.ReviewNotes, dto.InternalNotes, dto.MetadataJson);
        await HrCoreSupport.EnsureEmployeeExistsAsync(_db, dto.BusinessId, dto.EmployeeId, ct).ConfigureAwait(false);
        await HrLeaveSupport.EnsureLeaveNumberAvailableAsync(_db, dto.BusinessId, dto.EmployeeId, dto.RequestNumber, null, ct).ConfigureAwait(false);
        var entity = new LeaveRequest
        {
            BusinessId = dto.BusinessId,
            EmployeeId = dto.EmployeeId,
            RequestNumber = dto.RequestNumber,
            LeaveType = dto.LeaveType,
            Status = dto.Status,
            StartDateUtc = dto.StartDateUtc.Date,
            EndDateUtc = dto.EndDateUtc.Date,
            RequestedMinutes = dto.RequestedMinutes,
            ReviewNotes = dto.ReviewNotes,
            PrivacyClassification = dto.PrivacyClassification,
            InternalNotes = dto.InternalNotes,
            MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson)
        };
        _db.Set<LeaveRequest>().Add(entity);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "LeaveRequest", entity.Id, "hr.leave_request.created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return entity.Id;
    }
}

public sealed class UpdateLeaveRequestHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<LeaveRequestEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;
    public UpdateLeaveRequestHandler(IAppDbContext db, IValidator<LeaveRequestEditDto> validator, IClock clock, BusinessEventService? events = null) { _db = db; _validator = validator; _clock = clock; _events = events; }

    public async Task HandleAsync(LeaveRequestEditDto dto, CancellationToken ct = default)
    {
        HrLeaveSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrLeaveSupport.EnsureSafe(dto.RequestNumber, dto.ReviewNotes, dto.InternalNotes, dto.MetadataJson);
        var entity = await _db.Set<LeaveRequest>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (entity is null) throw new InvalidOperationException("LeaveRequestNotFound");
        HrCoreSupport.EnsureRowVersion(entity.RowVersion, dto.RowVersion);
        if (entity.Status is LeaveRequestStatus.Approved or LeaveRequestStatus.Cancelled) throw new InvalidOperationException("LeaveRequestLocked");
        await HrCoreSupport.EnsureEmployeeExistsAsync(_db, dto.BusinessId, dto.EmployeeId, ct).ConfigureAwait(false);
        await HrLeaveSupport.EnsureLeaveNumberAvailableAsync(_db, dto.BusinessId, dto.EmployeeId, dto.RequestNumber, dto.Id, ct).ConfigureAwait(false);
        entity.BusinessId = dto.BusinessId;
        entity.EmployeeId = dto.EmployeeId;
        entity.RequestNumber = dto.RequestNumber;
        entity.LeaveType = dto.LeaveType;
        entity.Status = dto.Status;
        entity.StartDateUtc = dto.StartDateUtc.Date;
        entity.EndDateUtc = dto.EndDateUtc.Date;
        entity.RequestedMinutes = dto.RequestedMinutes;
        entity.ReviewNotes = dto.ReviewNotes;
        entity.PrivacyClassification = dto.PrivacyClassification;
        entity.InternalNotes = dto.InternalNotes;
        entity.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "LeaveRequest", entity.Id, "hr.leave_request.updated", AuditTrailAction.Updated, ct).ConfigureAwait(false);
    }
}

public sealed class UpdateLeaveRequestLifecycleHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;
    public UpdateLeaveRequestLifecycleHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null) { _db = db; _clock = clock; _events = events; }

    public async Task<Result> HandleAsync(HrTimeLifecycleDto dto, LeaveRequestStatus target, CancellationToken ct = default)
    {
        dto.Notes = HrCoreSupport.Optional(dto.Notes);
        HrLeaveSupport.EnsureSafe(dto.Notes);
        var entity = await _db.Set<LeaveRequest>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (entity is null) return Result.Fail("LeaveRequestNotFound");
        if (!HrCoreSupport.RowVersionMatches(entity.RowVersion, dto.RowVersion)) return Result.Fail("ItemConcurrencyConflict");
        if (!HrLeaveSupport.CanTransition(entity.Status, target)) return Result.Fail("LeaveRequestInvalidTransition");
        if (target == LeaveRequestStatus.Rejected && string.IsNullOrWhiteSpace(dto.Notes)) return Result.Fail("LeaveRejectionReasonRequired");
        var now = _clock.UtcNow;
        entity.Status = target;
        entity.ReviewNotes = dto.Notes ?? entity.ReviewNotes;
        if (target == LeaveRequestStatus.Submitted) entity.SubmittedAtUtc = now;
        if (target is LeaveRequestStatus.Approved or LeaveRequestStatus.Rejected) entity.ReviewedAtUtc = now;
        if (target == LeaveRequestStatus.Approved)
        {
            var absenceExists = await _db.Set<AbsenceRecord>().AnyAsync(x => x.LeaveRequestId == entity.Id && !x.IsDeleted, ct).ConfigureAwait(false);
            if (!absenceExists)
            {
                _db.Set<AbsenceRecord>().Add(new AbsenceRecord
                {
                    BusinessId = entity.BusinessId,
                    EmployeeId = entity.EmployeeId,
                    LeaveRequestId = entity.Id,
                    AbsenceType = entity.LeaveType,
                    Status = AbsenceStatus.Confirmed,
                    StartDateUtc = entity.StartDateUtc,
                    EndDateUtc = entity.EndDateUtc,
                    AbsenceMinutes = entity.RequestedMinutes,
                    PrivacyClassification = entity.PrivacyClassification,
                    InternalNotes = entity.InternalNotes,
                    MetadataJson = "{}"
                });
            }
        }
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "LeaveRequest", entity.Id, $"hr.leave_request.{target.ToString().ToLowerInvariant()}", AuditTrailAction.StatusChanged, ct).ConfigureAwait(false);
        return Result.Ok();
    }
}

public sealed class CreateAbsenceRecordHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<AbsenceRecordEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;
    public CreateAbsenceRecordHandler(IAppDbContext db, IValidator<AbsenceRecordEditDto> validator, IClock clock, BusinessEventService? events = null) { _db = db; _validator = validator; _clock = clock; _events = events; }

    public async Task<Guid> HandleAsync(AbsenceRecordEditDto dto, CancellationToken ct = default)
    {
        HrLeaveSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrLeaveSupport.EnsureSafe(dto.InternalNotes, dto.MetadataJson);
        await HrLeaveSupport.EnsureAbsenceLinksAsync(_db, dto.BusinessId, dto.EmployeeId, dto.LeaveRequestId, ct).ConfigureAwait(false);
        var entity = new AbsenceRecord
        {
            BusinessId = dto.BusinessId,
            EmployeeId = dto.EmployeeId,
            LeaveRequestId = dto.LeaveRequestId,
            AbsenceType = dto.AbsenceType,
            Status = dto.Status,
            StartDateUtc = dto.StartDateUtc.Date,
            EndDateUtc = dto.EndDateUtc.Date,
            AbsenceMinutes = dto.AbsenceMinutes,
            PrivacyClassification = dto.PrivacyClassification,
            InternalNotes = dto.InternalNotes,
            MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson)
        };
        _db.Set<AbsenceRecord>().Add(entity);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "AbsenceRecord", entity.Id, "hr.absence_record.created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return entity.Id;
    }
}

public sealed class UpdateAbsenceRecordHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<AbsenceRecordEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;
    public UpdateAbsenceRecordHandler(IAppDbContext db, IValidator<AbsenceRecordEditDto> validator, IClock clock, BusinessEventService? events = null) { _db = db; _validator = validator; _clock = clock; _events = events; }

    public async Task HandleAsync(AbsenceRecordEditDto dto, CancellationToken ct = default)
    {
        HrLeaveSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrLeaveSupport.EnsureSafe(dto.InternalNotes, dto.MetadataJson);
        var entity = await _db.Set<AbsenceRecord>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (entity is null) throw new InvalidOperationException("AbsenceRecordNotFound");
        HrCoreSupport.EnsureRowVersion(entity.RowVersion, dto.RowVersion);
        if (entity.Status == AbsenceStatus.Cancelled) throw new InvalidOperationException("AbsenceRecordLocked");
        await HrLeaveSupport.EnsureAbsenceLinksAsync(_db, dto.BusinessId, dto.EmployeeId, dto.LeaveRequestId, ct).ConfigureAwait(false);
        entity.BusinessId = dto.BusinessId;
        entity.EmployeeId = dto.EmployeeId;
        entity.LeaveRequestId = dto.LeaveRequestId;
        entity.AbsenceType = dto.AbsenceType;
        entity.Status = dto.Status;
        entity.StartDateUtc = dto.StartDateUtc.Date;
        entity.EndDateUtc = dto.EndDateUtc.Date;
        entity.AbsenceMinutes = dto.AbsenceMinutes;
        entity.PrivacyClassification = dto.PrivacyClassification;
        entity.InternalNotes = dto.InternalNotes;
        entity.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, entity.BusinessId, "AbsenceRecord", entity.Id, "hr.absence_record.updated", AuditTrailAction.Updated, ct).ConfigureAwait(false);
    }
}

internal static class HrLeaveSupport
{
    public static void Normalize(LeaveRequestEditDto dto)
    {
        dto.RequestNumber = HrCoreSupport.NormalizeCode(dto.RequestNumber);
        dto.StartDateUtc = dto.StartDateUtc.Date;
        dto.EndDateUtc = dto.EndDateUtc.Date;
        dto.ReviewNotes = HrCoreSupport.Optional(dto.ReviewNotes);
        dto.InternalNotes = HrCoreSupport.Optional(dto.InternalNotes);
        dto.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
    }

    public static void Normalize(AbsenceRecordEditDto dto)
    {
        dto.StartDateUtc = dto.StartDateUtc.Date;
        dto.EndDateUtc = dto.EndDateUtc.Date;
        dto.InternalNotes = HrCoreSupport.Optional(dto.InternalNotes);
        dto.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
    }

    public static void EnsureSafe(params string?[] values) => HrCoreSupport.EnsureSafe(values);

    public static async Task EnsureLeaveNumberAvailableAsync(IAppDbContext db, Guid businessId, Guid employeeId, string number, Guid? excludingId, CancellationToken ct)
    {
        var query = db.Set<LeaveRequest>().AsNoTracking().Where(x => x.BusinessId == businessId && x.EmployeeId == employeeId && x.RequestNumber == number && !x.IsDeleted);
        if (excludingId.HasValue) query = query.Where(x => x.Id != excludingId.Value);
        if (await query.AnyAsync(ct).ConfigureAwait(false)) throw new InvalidOperationException("LeaveRequestNumberDuplicate");
    }

    public static async Task EnsureAbsenceLinksAsync(IAppDbContext db, Guid businessId, Guid employeeId, Guid? leaveRequestId, CancellationToken ct)
    {
        await HrCoreSupport.EnsureEmployeeExistsAsync(db, businessId, employeeId, ct).ConfigureAwait(false);
        if (!leaveRequestId.HasValue) return;
        var exists = await db.Set<LeaveRequest>().AnyAsync(x => x.Id == leaveRequestId.Value && x.BusinessId == businessId && x.EmployeeId == employeeId && !x.IsDeleted && x.Status == LeaveRequestStatus.Approved, ct).ConfigureAwait(false);
        if (!exists) throw new InvalidOperationException("ApprovedLeaveRequestNotFound");
    }

    public static bool CanTransition(LeaveRequestStatus current, LeaveRequestStatus target) => (current, target) switch
    {
        (LeaveRequestStatus.Draft, LeaveRequestStatus.Submitted) => true,
        (LeaveRequestStatus.Submitted, LeaveRequestStatus.InReview) => true,
        (LeaveRequestStatus.Submitted, LeaveRequestStatus.Approved) => true,
        (LeaveRequestStatus.InReview, LeaveRequestStatus.Approved) => true,
        (LeaveRequestStatus.Submitted, LeaveRequestStatus.Rejected) => true,
        (LeaveRequestStatus.InReview, LeaveRequestStatus.Rejected) => true,
        (LeaveRequestStatus.Draft, LeaveRequestStatus.Cancelled) => true,
        (LeaveRequestStatus.Rejected, LeaveRequestStatus.Submitted) => true,
        _ => false
    };
}
