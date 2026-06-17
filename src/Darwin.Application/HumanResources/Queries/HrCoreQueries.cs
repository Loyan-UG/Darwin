using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Application.HumanResources.DTOs;
using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.HumanResources.Queries;

public sealed class GetEmployeesPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;
    public GetEmployeesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<(List<EmployeeListItemDto> Items, int Total)> HandleAsync(Guid businessId, int page, int pageSize, string? query, EmployeeQueueFilter filter, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return (new List<EmployeeListItemDto>(), 0);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var rows =
            from employee in _db.Set<Employee>().AsNoTracking()
            join department in _db.Set<Department>().AsNoTracking() on employee.DepartmentId equals department.Id into departments
            from department in departments.DefaultIfEmpty()
            join position in _db.Set<Position>().AsNoTracking() on employee.PositionId equals position.Id into positions
            from position in positions.DefaultIfEmpty()
            where employee.BusinessId == businessId && !employee.IsDeleted
            select new { employee, department, position };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.ContainsInvariant(query);
            rows = rows.Where(x =>
                EF.Functions.Like(x.employee.EmployeeNumber.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                EF.Functions.Like(x.employee.FirstName.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                EF.Functions.Like(x.employee.LastName.ToUpper(), term, QueryLikePattern.EscapeCharacter) ||
                (x.employee.WorkEmail != null && EF.Functions.Like(x.employee.WorkEmail.ToUpper(), term, QueryLikePattern.EscapeCharacter)));
        }

        rows = filter switch
        {
            EmployeeQueueFilter.Active => rows.Where(x => x.employee.Status == EmployeeStatus.Active),
            EmployeeQueueFilter.Inactive => rows.Where(x => x.employee.Status == EmployeeStatus.Inactive),
            EmployeeQueueFilter.Archived => rows.Where(x => x.employee.Status == EmployeeStatus.Archived),
            EmployeeQueueFilter.LinkedToBusinessMember => rows.Where(x => x.employee.BusinessMemberId != null),
            EmployeeQueueFilter.MissingBusinessMember => rows.Where(x => x.employee.BusinessMemberId == null),
            _ => rows
        };

        var total = await rows.CountAsync(ct).ConfigureAwait(false);
        var items = await rows
            .OrderBy(x => x.employee.LastName)
            .ThenBy(x => x.employee.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new EmployeeListItemDto
            {
                Id = x.employee.Id,
                RowVersion = x.employee.RowVersion,
                BusinessId = x.employee.BusinessId,
                BusinessMemberId = x.employee.BusinessMemberId,
                EmployeeNumber = x.employee.EmployeeNumber,
                FullName = (x.employee.FirstName + " " + x.employee.LastName).Trim(),
                WorkEmail = x.employee.WorkEmail,
                DepartmentName = x.department != null ? x.department.DisplayName : null,
                PositionName = x.position != null ? x.position.DisplayName : null,
                Status = x.employee.Status,
                PrivacyClassification = x.employee.PrivacyClassification
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return (items, total);
    }
}

public sealed class GetEmployeeDetailHandler
{
    private readonly IAppDbContext _db;
    public GetEmployeeDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<EmployeeDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return null;
        var employee = await (
            from row in _db.Set<Employee>().AsNoTracking()
            join department in _db.Set<Department>().AsNoTracking() on row.DepartmentId equals department.Id into departments
            from department in departments.DefaultIfEmpty()
            join position in _db.Set<Position>().AsNoTracking() on row.PositionId equals position.Id into positions
            from position in positions.DefaultIfEmpty()
            where row.Id == id && !row.IsDeleted
            select new EmployeeDetailDto
            {
                Id = row.Id,
                RowVersion = row.RowVersion,
                BusinessId = row.BusinessId,
                BusinessMemberId = row.BusinessMemberId,
                DepartmentId = row.DepartmentId,
                PositionId = row.PositionId,
                EmployeeNumber = row.EmployeeNumber,
                FirstName = row.FirstName,
                LastName = row.LastName,
                PreferredName = row.PreferredName,
                WorkEmail = row.WorkEmail,
                WorkPhone = row.WorkPhone,
                Status = row.Status,
                HireDateUtc = row.HireDateUtc,
                TerminationDateUtc = row.TerminationDateUtc,
                PrivacyClassification = row.PrivacyClassification,
                InternalNotes = row.InternalNotes,
                MetadataJson = row.MetadataJson,
                DepartmentName = department != null ? department.DisplayName : null,
                PositionName = position != null ? position.DisplayName : null
            }).FirstOrDefaultAsync(ct).ConfigureAwait(false);

        if (employee is null) return null;
        employee.Contracts = await GetEmploymentContractsPageHandler.QueryContracts(_db, employee.BusinessId, employee.Id, EmploymentContractQueueFilter.All)
            .OrderByDescending(x => x.StartDateUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return employee;
    }
}

public sealed class GetDepartmentsPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;
    public GetDepartmentsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<(List<DepartmentListItemDto> Items, int Total)> HandleAsync(Guid businessId, int page, int pageSize, string? query, DepartmentQueueFilter filter, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return (new List<DepartmentListItemDto>(), 0);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var rows =
            from department in _db.Set<Department>().AsNoTracking()
            join parent in _db.Set<Department>().AsNoTracking() on department.ParentDepartmentId equals parent.Id into parents
            from parent in parents.DefaultIfEmpty()
            where department.BusinessId == businessId && !department.IsDeleted
            select new { department, parent };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.ContainsInvariant(query);
            rows = rows.Where(x => EF.Functions.Like(x.department.Code.ToUpper(), term, QueryLikePattern.EscapeCharacter) || EF.Functions.Like(x.department.DisplayName.ToUpper(), term, QueryLikePattern.EscapeCharacter));
        }

        rows = filter switch
        {
            DepartmentQueueFilter.Active => rows.Where(x => x.department.Status == DepartmentStatus.Active),
            DepartmentQueueFilter.Inactive => rows.Where(x => x.department.Status == DepartmentStatus.Inactive),
            DepartmentQueueFilter.Archived => rows.Where(x => x.department.Status == DepartmentStatus.Archived),
            _ => rows
        };

        var total = await rows.CountAsync(ct).ConfigureAwait(false);
        var items = await rows.OrderBy(x => x.department.SortOrder).ThenBy(x => x.department.Code)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new DepartmentListItemDto
            {
                Id = x.department.Id,
                RowVersion = x.department.RowVersion,
                BusinessId = x.department.BusinessId,
                ParentDepartmentId = x.department.ParentDepartmentId,
                Code = x.department.Code,
                DisplayName = x.department.DisplayName,
                Status = x.department.Status,
                SortOrder = x.department.SortOrder,
                Description = x.department.Description,
                MetadataJson = x.department.MetadataJson,
                ParentDepartmentName = x.parent != null ? x.parent.DisplayName : null
            }).ToListAsync(ct).ConfigureAwait(false);
        return (items, total);
    }
}

public sealed class GetDepartmentDetailHandler
{
    private readonly IAppDbContext _db;
    public GetDepartmentDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));
    public Task<DepartmentListItemDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return Task.FromResult<DepartmentListItemDto?>(null);
        return (from department in _db.Set<Department>().AsNoTracking()
                join parent in _db.Set<Department>().AsNoTracking() on department.ParentDepartmentId equals parent.Id into parents
                from parent in parents.DefaultIfEmpty()
                where department.Id == id && !department.IsDeleted
                select new DepartmentListItemDto
                {
                    Id = department.Id,
                    RowVersion = department.RowVersion,
                    BusinessId = department.BusinessId,
                    ParentDepartmentId = department.ParentDepartmentId,
                    Code = department.Code,
                    DisplayName = department.DisplayName,
                    Status = department.Status,
                    SortOrder = department.SortOrder,
                    Description = department.Description,
                    MetadataJson = department.MetadataJson,
                    ParentDepartmentName = parent != null ? parent.DisplayName : null
                }).FirstOrDefaultAsync(ct);
    }
}

public sealed class GetPositionsPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;
    public GetPositionsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<(List<PositionListItemDto> Items, int Total)> HandleAsync(Guid businessId, int page, int pageSize, string? query, PositionQueueFilter filter, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return (new List<PositionListItemDto>(), 0);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var rows =
            from position in _db.Set<Position>().AsNoTracking()
            join department in _db.Set<Department>().AsNoTracking() on position.DepartmentId equals department.Id into departments
            from department in departments.DefaultIfEmpty()
            where position.BusinessId == businessId && !position.IsDeleted
            select new { position, department };
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.ContainsInvariant(query);
            rows = rows.Where(x => EF.Functions.Like(x.position.Code.ToUpper(), term, QueryLikePattern.EscapeCharacter) || EF.Functions.Like(x.position.DisplayName.ToUpper(), term, QueryLikePattern.EscapeCharacter));
        }
        rows = filter switch
        {
            PositionQueueFilter.Active => rows.Where(x => x.position.Status == PositionStatus.Active),
            PositionQueueFilter.Inactive => rows.Where(x => x.position.Status == PositionStatus.Inactive),
            PositionQueueFilter.Archived => rows.Where(x => x.position.Status == PositionStatus.Archived),
            _ => rows
        };
        var total = await rows.CountAsync(ct).ConfigureAwait(false);
        var items = await rows.OrderBy(x => x.position.SortOrder).ThenBy(x => x.position.Code)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new PositionListItemDto
            {
                Id = x.position.Id,
                RowVersion = x.position.RowVersion,
                BusinessId = x.position.BusinessId,
                DepartmentId = x.position.DepartmentId,
                Code = x.position.Code,
                DisplayName = x.position.DisplayName,
                Status = x.position.Status,
                SortOrder = x.position.SortOrder,
                Description = x.position.Description,
                MetadataJson = x.position.MetadataJson,
                DepartmentName = x.department != null ? x.department.DisplayName : null
            }).ToListAsync(ct).ConfigureAwait(false);
        return (items, total);
    }
}

public sealed class GetPositionDetailHandler
{
    private readonly IAppDbContext _db;
    public GetPositionDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));
    public Task<PositionListItemDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return Task.FromResult<PositionListItemDto?>(null);
        return (from position in _db.Set<Position>().AsNoTracking()
                join department in _db.Set<Department>().AsNoTracking() on position.DepartmentId equals department.Id into departments
                from department in departments.DefaultIfEmpty()
                where position.Id == id && !position.IsDeleted
                select new PositionListItemDto
                {
                    Id = position.Id,
                    RowVersion = position.RowVersion,
                    BusinessId = position.BusinessId,
                    DepartmentId = position.DepartmentId,
                    Code = position.Code,
                    DisplayName = position.DisplayName,
                    Status = position.Status,
                    SortOrder = position.SortOrder,
                    Description = position.Description,
                    MetadataJson = position.MetadataJson,
                    DepartmentName = department != null ? department.DisplayName : null
                }).FirstOrDefaultAsync(ct);
    }
}

public sealed class GetEmploymentContractsPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;
    public GetEmploymentContractsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<(List<EmploymentContractListItemDto> Items, int Total)> HandleAsync(Guid businessId, Guid? employeeId, int page, int pageSize, string? query, EmploymentContractQueueFilter filter, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty) return (new List<EmploymentContractListItemDto>(), 0);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var rows = QueryContracts(_db, businessId, employeeId, filter);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.ContainsInvariant(query);
            rows = rows.Where(x => EF.Functions.Like(x.ContractNumber.ToUpper(), term, QueryLikePattern.EscapeCharacter) || EF.Functions.Like(x.EmployeeName.ToUpper(), term, QueryLikePattern.EscapeCharacter));
        }
        var total = await rows.CountAsync(ct).ConfigureAwait(false);
        var items = await rows.OrderByDescending(x => x.StartDateUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct).ConfigureAwait(false);
        return (items, total);
    }

    internal static IQueryable<EmploymentContractListItemDto> QueryContracts(IAppDbContext db, Guid businessId, Guid? employeeId, EmploymentContractQueueFilter filter)
    {
        var rows =
            from contract in db.Set<EmploymentContract>().AsNoTracking()
            join employee in db.Set<Employee>().AsNoTracking() on contract.EmployeeId equals employee.Id
            where contract.BusinessId == businessId && !contract.IsDeleted && !employee.IsDeleted
            select new { contract, employee };
        if (employeeId.HasValue) rows = rows.Where(x => x.contract.EmployeeId == employeeId.Value);
        rows = filter switch
        {
            EmploymentContractQueueFilter.Draft => rows.Where(x => x.contract.Status == EmploymentContractStatus.Draft),
            EmploymentContractQueueFilter.Active => rows.Where(x => x.contract.Status == EmploymentContractStatus.Active),
            EmploymentContractQueueFilter.Ended => rows.Where(x => x.contract.Status == EmploymentContractStatus.Ended),
            EmploymentContractQueueFilter.Archived => rows.Where(x => x.contract.Status == EmploymentContractStatus.Archived),
            _ => rows
        };
        return rows.Select(x => new EmploymentContractListItemDto
        {
            Id = x.contract.Id,
            RowVersion = x.contract.RowVersion,
            BusinessId = x.contract.BusinessId,
            EmployeeId = x.contract.EmployeeId,
            EmployeeName = (x.employee.FirstName + " " + x.employee.LastName).Trim(),
            ContractNumber = x.contract.ContractNumber,
            EmploymentType = x.contract.EmploymentType,
            Status = x.contract.Status,
            StartDateUtc = x.contract.StartDateUtc,
            EndDateUtc = x.contract.EndDateUtc,
            WeeklyHoursMinor = x.contract.WeeklyHoursMinor,
            PrivacyClassification = x.contract.PrivacyClassification,
            InternalNotes = x.contract.InternalNotes,
            MetadataJson = x.contract.MetadataJson
        });
    }
}

public sealed class GetEmploymentContractDetailHandler
{
    private readonly IAppDbContext _db;
    public GetEmploymentContractDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));
    public Task<EmploymentContractListItemDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return Task.FromResult<EmploymentContractListItemDto?>(null);
        return (from contract in _db.Set<EmploymentContract>().AsNoTracking()
                join employee in _db.Set<Employee>().AsNoTracking() on contract.EmployeeId equals employee.Id
                where contract.Id == id && !contract.IsDeleted && !employee.IsDeleted
                select new EmploymentContractListItemDto
                {
                    Id = contract.Id,
                    RowVersion = contract.RowVersion,
                    BusinessId = contract.BusinessId,
                    EmployeeId = contract.EmployeeId,
                    EmployeeName = (employee.FirstName + " " + employee.LastName).Trim(),
                    ContractNumber = contract.ContractNumber,
                    EmploymentType = contract.EmploymentType,
                    Status = contract.Status,
                    StartDateUtc = contract.StartDateUtc,
                    EndDateUtc = contract.EndDateUtc,
                    WeeklyHoursMinor = contract.WeeklyHoursMinor,
                    PrivacyClassification = contract.PrivacyClassification,
                    InternalNotes = contract.InternalNotes,
                    MetadataJson = contract.MetadataJson
                }).FirstOrDefaultAsync(ct);
    }
}
