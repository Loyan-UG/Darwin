using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Application.HumanResources.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.HumanResources.Commands;

public sealed class CreateEmployeeHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<EmployeeEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreateEmployeeHandler(IAppDbContext db, IValidator<EmployeeEditDto> validator, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Guid> HandleAsync(EmployeeEditDto dto, CancellationToken ct = default)
    {
        HrCoreSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrCoreSupport.EnsureSafe(dto.EmployeeNumber, dto.FirstName, dto.LastName, dto.PreferredName, dto.WorkEmail, dto.WorkPhone, dto.InternalNotes, dto.MetadataJson);
        await HrCoreSupport.EnsureEmployeeLinksAsync(_db, dto.BusinessId, dto.BusinessMemberId, dto.DepartmentId, dto.PositionId, ct).ConfigureAwait(false);
        await HrCoreSupport.EnsureEmployeeNumberAvailableAsync(_db, dto.BusinessId, dto.EmployeeNumber, null, ct).ConfigureAwait(false);

        var employee = new Employee
        {
            BusinessId = dto.BusinessId,
            BusinessMemberId = dto.BusinessMemberId,
            DepartmentId = dto.DepartmentId,
            PositionId = dto.PositionId,
            EmployeeNumber = dto.EmployeeNumber,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PreferredName = dto.PreferredName,
            WorkEmail = dto.WorkEmail,
            WorkPhone = dto.WorkPhone,
            Status = dto.Status,
            HireDateUtc = dto.HireDateUtc,
            TerminationDateUtc = dto.TerminationDateUtc,
            PrivacyClassification = dto.PrivacyClassification,
            InternalNotes = dto.InternalNotes,
            MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson)
        };

        _db.Set<Employee>().Add(employee);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, employee.BusinessId, "Employee", employee.Id, "hr.employee.created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return employee.Id;
    }
}

public sealed class UpdateEmployeeHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<EmployeeEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public UpdateEmployeeHandler(IAppDbContext db, IValidator<EmployeeEditDto> validator, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task HandleAsync(EmployeeEditDto dto, CancellationToken ct = default)
    {
        HrCoreSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrCoreSupport.EnsureSafe(dto.EmployeeNumber, dto.FirstName, dto.LastName, dto.PreferredName, dto.WorkEmail, dto.WorkPhone, dto.InternalNotes, dto.MetadataJson);

        var employee = await _db.Set<Employee>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (employee is null) throw new InvalidOperationException("EmployeeNotFound");
        HrCoreSupport.EnsureRowVersion(employee.RowVersion, dto.RowVersion);
        await HrCoreSupport.EnsureEmployeeLinksAsync(_db, dto.BusinessId, dto.BusinessMemberId, dto.DepartmentId, dto.PositionId, ct).ConfigureAwait(false);
        await HrCoreSupport.EnsureEmployeeNumberAvailableAsync(_db, dto.BusinessId, dto.EmployeeNumber, dto.Id, ct).ConfigureAwait(false);

        employee.BusinessId = dto.BusinessId;
        employee.BusinessMemberId = dto.BusinessMemberId;
        employee.DepartmentId = dto.DepartmentId;
        employee.PositionId = dto.PositionId;
        employee.EmployeeNumber = dto.EmployeeNumber;
        employee.FirstName = dto.FirstName;
        employee.LastName = dto.LastName;
        employee.PreferredName = dto.PreferredName;
        employee.WorkEmail = dto.WorkEmail;
        employee.WorkPhone = dto.WorkPhone;
        employee.Status = dto.Status;
        employee.HireDateUtc = dto.HireDateUtc;
        employee.TerminationDateUtc = dto.TerminationDateUtc;
        employee.PrivacyClassification = dto.PrivacyClassification;
        employee.InternalNotes = dto.InternalNotes;
        employee.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);

        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, employee.BusinessId, "Employee", employee.Id, "hr.employee.updated", AuditTrailAction.Updated, ct).ConfigureAwait(false);
    }
}

public sealed class ArchiveEmployeeHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public ArchiveEmployeeHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Result> HandleAsync(HrArchiveDto dto, CancellationToken ct = default)
    {
        var employee = await _db.Set<Employee>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (employee is null) return Result.Fail("EmployeeNotFound");
        if (!HrCoreSupport.RowVersionMatches(employee.RowVersion, dto.RowVersion)) return Result.Fail("ItemConcurrencyConflict");
        employee.Status = EmployeeStatus.Archived;
        employee.IsDeleted = true;
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, employee.BusinessId, "Employee", employee.Id, "hr.employee.archived", AuditTrailAction.Deleted, ct).ConfigureAwait(false);
        return Result.Ok();
    }
}

public sealed class CreateDepartmentHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<DepartmentEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;
    public CreateDepartmentHandler(IAppDbContext db, IValidator<DepartmentEditDto> validator, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _validator = validator;
        _clock = clock;
        _events = events;
    }

    public async Task<Guid> HandleAsync(DepartmentEditDto dto, CancellationToken ct = default)
    {
        HrCoreSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrCoreSupport.EnsureSafe(dto.Code, dto.DisplayName, dto.Description, dto.MetadataJson);
        await HrCoreSupport.EnsureDepartmentParentAsync(_db, dto.BusinessId, dto.ParentDepartmentId, null, ct).ConfigureAwait(false);
        await HrCoreSupport.EnsureDepartmentCodeAvailableAsync(_db, dto.BusinessId, dto.Code, null, ct).ConfigureAwait(false);

        var department = new Department
        {
            BusinessId = dto.BusinessId,
            ParentDepartmentId = dto.ParentDepartmentId,
            Code = dto.Code,
            DisplayName = dto.DisplayName,
            Status = dto.Status,
            SortOrder = dto.SortOrder,
            Description = dto.Description,
            MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson)
        };
        _db.Set<Department>().Add(department);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, department.BusinessId, "Department", department.Id, "hr.department.created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return department.Id;
    }
}

public sealed class UpdateDepartmentHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<DepartmentEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;
    public UpdateDepartmentHandler(IAppDbContext db, IValidator<DepartmentEditDto> validator, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _validator = validator;
        _clock = clock;
        _events = events;
    }

    public async Task HandleAsync(DepartmentEditDto dto, CancellationToken ct = default)
    {
        HrCoreSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrCoreSupport.EnsureSafe(dto.Code, dto.DisplayName, dto.Description, dto.MetadataJson);
        var department = await _db.Set<Department>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (department is null) throw new InvalidOperationException("DepartmentNotFound");
        HrCoreSupport.EnsureRowVersion(department.RowVersion, dto.RowVersion);
        await HrCoreSupport.EnsureDepartmentParentAsync(_db, dto.BusinessId, dto.ParentDepartmentId, dto.Id, ct).ConfigureAwait(false);
        await HrCoreSupport.EnsureDepartmentCodeAvailableAsync(_db, dto.BusinessId, dto.Code, dto.Id, ct).ConfigureAwait(false);

        department.BusinessId = dto.BusinessId;
        department.ParentDepartmentId = dto.ParentDepartmentId;
        department.Code = dto.Code;
        department.DisplayName = dto.DisplayName;
        department.Status = dto.Status;
        department.SortOrder = dto.SortOrder;
        department.Description = dto.Description;
        department.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, department.BusinessId, "Department", department.Id, "hr.department.updated", AuditTrailAction.Updated, ct).ConfigureAwait(false);
    }
}

public sealed class ArchiveDepartmentHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;
    public ArchiveDepartmentHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _clock = clock;
        _events = events;
    }

    public async Task<Result> HandleAsync(HrArchiveDto dto, CancellationToken ct = default)
    {
        var department = await _db.Set<Department>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (department is null) return Result.Fail("DepartmentNotFound");
        if (!HrCoreSupport.RowVersionMatches(department.RowVersion, dto.RowVersion)) return Result.Fail("ItemConcurrencyConflict");
        var hasActiveChildren = await _db.Set<Department>().AnyAsync(x => x.ParentDepartmentId == department.Id && !x.IsDeleted && x.Status != DepartmentStatus.Archived, ct).ConfigureAwait(false);
        if (hasActiveChildren) return Result.Fail("DepartmentHasActiveChildren");
        department.Status = DepartmentStatus.Archived;
        department.IsDeleted = true;
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, department.BusinessId, "Department", department.Id, "hr.department.archived", AuditTrailAction.Deleted, ct).ConfigureAwait(false);
        return Result.Ok();
    }
}

public sealed class CreatePositionHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<PositionEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;
    public CreatePositionHandler(IAppDbContext db, IValidator<PositionEditDto> validator, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _validator = validator;
        _clock = clock;
        _events = events;
    }

    public async Task<Guid> HandleAsync(PositionEditDto dto, CancellationToken ct = default)
    {
        HrCoreSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrCoreSupport.EnsureSafe(dto.Code, dto.DisplayName, dto.Description, dto.MetadataJson);
        await HrCoreSupport.EnsureDepartmentExistsAsync(_db, dto.BusinessId, dto.DepartmentId, ct).ConfigureAwait(false);
        await HrCoreSupport.EnsurePositionCodeAvailableAsync(_db, dto.BusinessId, dto.Code, null, ct).ConfigureAwait(false);
        var position = new Position
        {
            BusinessId = dto.BusinessId,
            DepartmentId = dto.DepartmentId,
            Code = dto.Code,
            DisplayName = dto.DisplayName,
            Status = dto.Status,
            SortOrder = dto.SortOrder,
            Description = dto.Description,
            MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson)
        };
        _db.Set<Position>().Add(position);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, position.BusinessId, "Position", position.Id, "hr.position.created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return position.Id;
    }
}

public sealed class UpdatePositionHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<PositionEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;
    public UpdatePositionHandler(IAppDbContext db, IValidator<PositionEditDto> validator, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _validator = validator;
        _clock = clock;
        _events = events;
    }

    public async Task HandleAsync(PositionEditDto dto, CancellationToken ct = default)
    {
        HrCoreSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrCoreSupport.EnsureSafe(dto.Code, dto.DisplayName, dto.Description, dto.MetadataJson);
        var position = await _db.Set<Position>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (position is null) throw new InvalidOperationException("PositionNotFound");
        HrCoreSupport.EnsureRowVersion(position.RowVersion, dto.RowVersion);
        await HrCoreSupport.EnsureDepartmentExistsAsync(_db, dto.BusinessId, dto.DepartmentId, ct).ConfigureAwait(false);
        await HrCoreSupport.EnsurePositionCodeAvailableAsync(_db, dto.BusinessId, dto.Code, dto.Id, ct).ConfigureAwait(false);
        position.BusinessId = dto.BusinessId;
        position.DepartmentId = dto.DepartmentId;
        position.Code = dto.Code;
        position.DisplayName = dto.DisplayName;
        position.Status = dto.Status;
        position.SortOrder = dto.SortOrder;
        position.Description = dto.Description;
        position.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, position.BusinessId, "Position", position.Id, "hr.position.updated", AuditTrailAction.Updated, ct).ConfigureAwait(false);
    }
}

public sealed class ArchivePositionHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;
    public ArchivePositionHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _clock = clock;
        _events = events;
    }

    public async Task<Result> HandleAsync(HrArchiveDto dto, CancellationToken ct = default)
    {
        var position = await _db.Set<Position>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (position is null) return Result.Fail("PositionNotFound");
        if (!HrCoreSupport.RowVersionMatches(position.RowVersion, dto.RowVersion)) return Result.Fail("ItemConcurrencyConflict");
        position.Status = PositionStatus.Archived;
        position.IsDeleted = true;
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, position.BusinessId, "Position", position.Id, "hr.position.archived", AuditTrailAction.Deleted, ct).ConfigureAwait(false);
        return Result.Ok();
    }
}

public sealed class CreateEmploymentContractHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<EmploymentContractEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;
    public CreateEmploymentContractHandler(IAppDbContext db, IValidator<EmploymentContractEditDto> validator, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _validator = validator;
        _clock = clock;
        _events = events;
    }

    public async Task<Guid> HandleAsync(EmploymentContractEditDto dto, CancellationToken ct = default)
    {
        HrCoreSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrCoreSupport.EnsureSafe(dto.ContractNumber, dto.InternalNotes, dto.MetadataJson);
        await HrCoreSupport.EnsureEmployeeExistsAsync(_db, dto.BusinessId, dto.EmployeeId, ct).ConfigureAwait(false);
        await HrCoreSupport.EnsureContractNumberAvailableAsync(_db, dto.BusinessId, dto.ContractNumber, null, ct).ConfigureAwait(false);
        var contract = new EmploymentContract
        {
            BusinessId = dto.BusinessId,
            EmployeeId = dto.EmployeeId,
            ContractNumber = dto.ContractNumber,
            EmploymentType = dto.EmploymentType,
            Status = dto.Status,
            StartDateUtc = dto.StartDateUtc,
            EndDateUtc = dto.EndDateUtc,
            WeeklyHoursMinor = dto.WeeklyHoursMinor,
            PrivacyClassification = dto.PrivacyClassification,
            InternalNotes = dto.InternalNotes,
            MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson)
        };
        _db.Set<EmploymentContract>().Add(contract);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, contract.BusinessId, "EmploymentContract", contract.Id, "hr.employment_contract.created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return contract.Id;
    }
}

public sealed class UpdateEmploymentContractHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<EmploymentContractEditDto> _validator;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;
    public UpdateEmploymentContractHandler(IAppDbContext db, IValidator<EmploymentContractEditDto> validator, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _validator = validator;
        _clock = clock;
        _events = events;
    }

    public async Task HandleAsync(EmploymentContractEditDto dto, CancellationToken ct = default)
    {
        HrCoreSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        HrCoreSupport.EnsureSafe(dto.ContractNumber, dto.InternalNotes, dto.MetadataJson);
        var contract = await _db.Set<EmploymentContract>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (contract is null) throw new InvalidOperationException("EmploymentContractNotFound");
        HrCoreSupport.EnsureRowVersion(contract.RowVersion, dto.RowVersion);
        await HrCoreSupport.EnsureEmployeeExistsAsync(_db, dto.BusinessId, dto.EmployeeId, ct).ConfigureAwait(false);
        await HrCoreSupport.EnsureContractNumberAvailableAsync(_db, dto.BusinessId, dto.ContractNumber, dto.Id, ct).ConfigureAwait(false);
        contract.BusinessId = dto.BusinessId;
        contract.EmployeeId = dto.EmployeeId;
        contract.ContractNumber = dto.ContractNumber;
        contract.EmploymentType = dto.EmploymentType;
        contract.Status = dto.Status;
        contract.StartDateUtc = dto.StartDateUtc;
        contract.EndDateUtc = dto.EndDateUtc;
        contract.WeeklyHoursMinor = dto.WeeklyHoursMinor;
        contract.PrivacyClassification = dto.PrivacyClassification;
        contract.InternalNotes = dto.InternalNotes;
        contract.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, contract.BusinessId, "EmploymentContract", contract.Id, "hr.employment_contract.updated", AuditTrailAction.Updated, ct).ConfigureAwait(false);
    }
}

public sealed class ArchiveEmploymentContractHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;
    public ArchiveEmploymentContractHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db;
        _clock = clock;
        _events = events;
    }

    public async Task<Result> HandleAsync(HrArchiveDto dto, CancellationToken ct = default)
    {
        var contract = await _db.Set<EmploymentContract>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (contract is null) return Result.Fail("EmploymentContractNotFound");
        if (!HrCoreSupport.RowVersionMatches(contract.RowVersion, dto.RowVersion)) return Result.Fail("ItemConcurrencyConflict");
        contract.Status = EmploymentContractStatus.Archived;
        contract.IsDeleted = true;
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, contract.BusinessId, "EmploymentContract", contract.Id, "hr.employment_contract.archived", AuditTrailAction.Deleted, ct).ConfigureAwait(false);
        return Result.Ok();
    }
}

internal static class HrCoreSupport
{
    public static void Normalize(EmployeeEditDto dto)
    {
        dto.EmployeeNumber = NormalizeCode(dto.EmployeeNumber);
        dto.FirstName = Required(dto.FirstName);
        dto.LastName = Required(dto.LastName);
        dto.PreferredName = Optional(dto.PreferredName);
        dto.WorkEmail = Optional(dto.WorkEmail)?.ToLowerInvariant();
        dto.WorkPhone = Optional(dto.WorkPhone);
        dto.InternalNotes = Optional(dto.InternalNotes);
        dto.MetadataJson = NormalizeMetadataJson(dto.MetadataJson);
    }

    public static void Normalize(DepartmentEditDto dto)
    {
        dto.Code = NormalizeCode(dto.Code);
        dto.DisplayName = Required(dto.DisplayName);
        dto.Description = Optional(dto.Description);
        dto.MetadataJson = NormalizeMetadataJson(dto.MetadataJson);
    }

    public static void Normalize(PositionEditDto dto)
    {
        dto.Code = NormalizeCode(dto.Code);
        dto.DisplayName = Required(dto.DisplayName);
        dto.Description = Optional(dto.Description);
        dto.MetadataJson = NormalizeMetadataJson(dto.MetadataJson);
    }

    public static void Normalize(EmploymentContractEditDto dto)
    {
        dto.ContractNumber = NormalizeCode(dto.ContractNumber);
        dto.InternalNotes = Optional(dto.InternalNotes);
        dto.MetadataJson = NormalizeMetadataJson(dto.MetadataJson);
    }

    public static string Required(string? value) => FoundationInputNormalizer.Required(value) ?? string.Empty;
    public static string? Optional(string? value) => FoundationInputNormalizer.Optional(value);
    public static string NormalizeCode(string? value) => Required(value).ToUpperInvariant();
    public static string NormalizeMetadataJson(string? value) => FoundationInputNormalizer.Json(value);

    public static void EnsureSafe(params string?[] values)
    {
        if (values.Any(FoundationInputNormalizer.LooksSensitive))
        {
            throw new ArgumentException("HrSensitiveMetadataRejected");
        }
    }

    public static void EnsureRowVersion(byte[] stored, byte[] supplied)
    {
        if (!RowVersionMatches(stored, supplied)) throw new InvalidOperationException("ItemConcurrencyConflict");
    }

    public static bool RowVersionMatches(byte[] stored, byte[] supplied)
    {
        var left = stored ?? Array.Empty<byte>();
        var right = supplied ?? Array.Empty<byte>();
        return right.Length > 0 && left.SequenceEqual(right);
    }

    public static async Task EnsureEmployeeLinksAsync(IAppDbContext db, Guid businessId, Guid? businessMemberId, Guid? departmentId, Guid? positionId, CancellationToken ct)
    {
        if (businessId == Guid.Empty) throw new ArgumentException("BusinessRequired");

        if (businessMemberId.HasValue)
        {
            var exists = await db.Set<BusinessMember>().AnyAsync(x => x.Id == businessMemberId.Value && x.BusinessId == businessId && !x.IsDeleted, ct).ConfigureAwait(false);
            if (!exists) throw new InvalidOperationException("BusinessMemberNotFound");
        }

        await EnsureDepartmentExistsAsync(db, businessId, departmentId, ct).ConfigureAwait(false);

        if (positionId.HasValue)
        {
            var exists = await db.Set<Position>().AnyAsync(x => x.Id == positionId.Value && x.BusinessId == businessId && !x.IsDeleted && x.Status != PositionStatus.Archived, ct).ConfigureAwait(false);
            if (!exists) throw new InvalidOperationException("PositionNotFound");
        }
    }

    public static async Task EnsureDepartmentExistsAsync(IAppDbContext db, Guid businessId, Guid? departmentId, CancellationToken ct)
    {
        if (!departmentId.HasValue) return;
        var exists = await db.Set<Department>().AnyAsync(x => x.Id == departmentId.Value && x.BusinessId == businessId && !x.IsDeleted && x.Status != DepartmentStatus.Archived, ct).ConfigureAwait(false);
        if (!exists) throw new InvalidOperationException("DepartmentNotFound");
    }

    public static async Task EnsureEmployeeExistsAsync(IAppDbContext db, Guid businessId, Guid employeeId, CancellationToken ct)
    {
        var exists = await db.Set<Employee>().AnyAsync(x => x.Id == employeeId && x.BusinessId == businessId && !x.IsDeleted && x.Status != EmployeeStatus.Archived, ct).ConfigureAwait(false);
        if (!exists) throw new InvalidOperationException("EmployeeNotFound");
    }

    public static async Task EnsureDepartmentParentAsync(IAppDbContext db, Guid businessId, Guid? parentId, Guid? selfId, CancellationToken ct)
    {
        if (!parentId.HasValue) return;
        if (selfId.HasValue && parentId.Value == selfId.Value) throw new InvalidOperationException("DepartmentCycleRejected");
        var parent = await db.Set<Department>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == parentId.Value && x.BusinessId == businessId && !x.IsDeleted, ct).ConfigureAwait(false);
        if (parent is null) throw new InvalidOperationException("DepartmentParentNotFound");

        var currentParentId = parent.ParentDepartmentId;
        while (currentParentId.HasValue)
        {
            if (selfId.HasValue && currentParentId.Value == selfId.Value) throw new InvalidOperationException("DepartmentCycleRejected");
            currentParentId = await db.Set<Department>().AsNoTracking()
                .Where(x => x.Id == currentParentId.Value && !x.IsDeleted)
                .Select(x => x.ParentDepartmentId)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
        }
    }

    public static Task EnsureEmployeeNumberAvailableAsync(IAppDbContext db, Guid businessId, string number, Guid? excludingId, CancellationToken ct)
        => EnsureUniqueAsync<Employee>(db, businessId, number, excludingId, x => x.EmployeeNumber, ct, "EmployeeNumberDuplicate");

    public static Task EnsureDepartmentCodeAvailableAsync(IAppDbContext db, Guid businessId, string code, Guid? excludingId, CancellationToken ct)
        => EnsureUniqueAsync<Department>(db, businessId, code, excludingId, x => x.Code, ct, "DepartmentCodeDuplicate");

    public static Task EnsurePositionCodeAvailableAsync(IAppDbContext db, Guid businessId, string code, Guid? excludingId, CancellationToken ct)
        => EnsureUniqueAsync<Position>(db, businessId, code, excludingId, x => x.Code, ct, "PositionCodeDuplicate");

    public static Task EnsureContractNumberAvailableAsync(IAppDbContext db, Guid businessId, string number, Guid? excludingId, CancellationToken ct)
        => EnsureUniqueAsync<EmploymentContract>(db, businessId, number, excludingId, x => x.ContractNumber, ct, "EmploymentContractNumberDuplicate");

    private static async Task EnsureUniqueAsync<T>(IAppDbContext db, Guid businessId, string value, Guid? excludingId, System.Linq.Expressions.Expression<Func<T, string>> selector, CancellationToken ct, string error)
        where T : class
    {
        var parameter = selector.Parameters[0];
        var body = System.Linq.Expressions.Expression.Equal(selector.Body, System.Linq.Expressions.Expression.Constant(value));
        var business = System.Linq.Expressions.Expression.Equal(System.Linq.Expressions.Expression.Property(parameter, "BusinessId"), System.Linq.Expressions.Expression.Constant(businessId));
        var notDeleted = System.Linq.Expressions.Expression.Equal(System.Linq.Expressions.Expression.Property(parameter, "IsDeleted"), System.Linq.Expressions.Expression.Constant(false));
        var predicate = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(System.Linq.Expressions.Expression.AndAlso(System.Linq.Expressions.Expression.AndAlso(body, business), notDeleted), parameter);

        var query = db.Set<T>().AsNoTracking().Where(predicate);
        if (excludingId.HasValue)
        {
            query = query.Where(x => EF.Property<Guid>(x, "Id") != excludingId.Value);
        }

        if (await query.AnyAsync(ct).ConfigureAwait(false)) throw new InvalidOperationException(error);
    }

    public static async Task RecordEvidenceOrSaveAsync(IAppDbContext db, BusinessEventService? events, IClock clock, Guid businessId, string entityType, Guid entityId, string eventType, AuditTrailAction auditAction, CancellationToken ct)
    {
        if (events is null)
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        var now = clock.UtcNow;
        var eventKey = $"{eventType}:{entityId:N}";
        var payload = $$"""{"entityType":"{{entityType}}","entityId":"{{entityId}}","source":"hr"}""";
        var eventResult = await events.AddEventAsync(new AddBusinessEventCommand(businessId, entityType, entityId, eventType, eventKey, now, null, BusinessEventSource.User, BusinessEventSeverity.Info, FoundationVisibility.Internal, eventType, null, null, null, payload, """{"source":"hr"}"""), ct).ConfigureAwait(false);
        if (!eventResult.Succeeded) throw new InvalidOperationException(eventResult.Error);
        var auditResult = await events.AddAuditTrailAsync(new AddAuditTrailCommand(businessId, entityType, entityId, auditAction, now, null, eventResult.Value, eventType, null, payload, """{"source":"hr"}"""), ct).ConfigureAwait(false);
        if (!auditResult.Succeeded) throw new InvalidOperationException(auditResult.Error);
    }
}
