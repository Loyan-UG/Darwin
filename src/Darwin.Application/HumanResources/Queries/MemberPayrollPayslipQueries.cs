using System.Text.Json;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Abstractions.Storage;
using Darwin.Application.Foundation;
using Darwin.Application.HumanResources.DTOs;
using Darwin.Application.HumanResources.Services;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.HumanResources.Queries;

public sealed class GetMyPayslipsPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyPayslipsPageHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public async Task<(List<MemberPayslipSummaryDto> Items, int Total)> HandleAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        var userId = _currentUser.GetCurrentUserId();
        if (userId == Guid.Empty) return (new List<MemberPayslipSummaryDto>(), 0);

        var employeeScopes = BuildEmployeeScope(userId);
        var baseQuery =
            from payslip in _db.Set<PayrollPayslip>().AsNoTracking()
            join line in _db.Set<PayrollRunLine>().AsNoTracking() on payslip.PayrollRunLineId equals line.Id
            join employee in employeeScopes on payslip.EmployeeId equals employee.Id
            join business in _db.Set<Business>().AsNoTracking() on payslip.BusinessId equals business.Id
            join document in _db.Set<DocumentRecord>().AsNoTracking() on payslip.DocumentRecordId equals document.Id
            where !payslip.IsDeleted &&
                  !line.IsDeleted &&
                  !business.IsDeleted &&
                  !document.IsDeleted &&
                  document.ContentType == PayrollPayslipArtifactService.PdfContentType
            select new { Payslip = payslip, Line = line, Business = business };

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        var rows = await baseQuery
            .OrderByDescending(x => x.Payslip.PeriodEndUtc)
            .ThenByDescending(x => x.Payslip.GeneratedAtUtc)
            .ThenBy(x => x.Payslip.PayslipNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MemberPayslipSummaryDto
            {
                Id = x.Payslip.Id,
                BusinessId = x.Payslip.BusinessId,
                BusinessName = x.Business.Name,
                EmployeeId = x.Payslip.EmployeeId,
                EmployeeNumber = x.Line.EmployeeNumber,
                PayslipNumber = x.Payslip.PayslipNumber,
                Currency = x.Payslip.Currency,
                PeriodStartUtc = x.Payslip.PeriodStartUtc,
                PeriodEndUtc = x.Payslip.PeriodEndUtc,
                GrossPayMinor = x.Payslip.GrossPayMinor,
                EmployeeDeductionMinor = x.Payslip.EmployeeDeductionMinor,
                NetPayMinor = x.Payslip.NetPayMinor,
                Status = x.Payslip.Status.ToString(),
                GeneratedAtUtc = x.Payslip.GeneratedAtUtc,
                DocumentPath = $"api/v1/member/payroll/payslips/{x.Payslip.Id:D}/document"
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        await EnrichPaymentStatusAsync(rows, ct).ConfigureAwait(false);
        return (rows, total);
    }

    private IQueryable<Employee> BuildEmployeeScope(Guid userId)
        => from employee in _db.Set<Employee>().AsNoTracking()
           join member in _db.Set<BusinessMember>().AsNoTracking() on employee.BusinessMemberId equals member.Id
           where !employee.IsDeleted &&
                 !member.IsDeleted &&
                 member.IsActive &&
                 member.UserId == userId &&
                 member.BusinessId == employee.BusinessId
           select employee;

    private async Task EnrichPaymentStatusAsync(IReadOnlyCollection<MemberPayslipSummaryDto> items, CancellationToken ct)
    {
        if (items.Count == 0) return;
        var payslipIds = items.Select(x => x.Id).ToArray();
        var payslipLines = await _db.Set<PayrollPayslip>()
            .AsNoTracking()
            .Where(x => payslipIds.Contains(x.Id) && !x.IsDeleted)
            .Select(x => new { x.Id, x.PayrollRunLineId, x.NetPayMinor })
            .ToDictionaryAsync(x => x.PayrollRunLineId, x => x, ct)
            .ConfigureAwait(false);
        if (payslipLines.Count == 0) return;

        var lineIds = payslipLines.Keys.ToArray();
        var paidByLine = await _db.Set<PayrollPaymentAllocation>()
            .AsNoTracking()
            .Where(allocation =>
                lineIds.Contains(allocation.PayrollRunLineId) &&
                !allocation.IsDeleted &&
                _db.Set<PayrollPayment>().Any(payment =>
                    payment.Id == allocation.PayrollPaymentId &&
                    payment.Status == PayrollPaymentStatus.Posted &&
                    !payment.IsDeleted))
            .GroupBy(x => x.PayrollRunLineId)
            .Select(x => new { LineId = x.Key, Amount = x.Sum(a => a.AmountMinor) })
            .ToDictionaryAsync(x => x.LineId, x => x.Amount, ct)
            .ConfigureAwait(false);

        var attentionLineIds = await _db.Set<PayrollPaymentAllocation>()
            .AsNoTracking()
            .Where(allocation =>
                lineIds.Contains(allocation.PayrollRunLineId) &&
                !allocation.IsDeleted &&
                _db.Set<PayrollPayment>().Any(payment =>
                    payment.Id == allocation.PayrollPaymentId &&
                    !payment.IsDeleted &&
                    _db.Set<PayrollPaymentBankCorrection>().Any(correction =>
                        correction.PayrollPaymentId == payment.Id &&
                        correction.Status != PayrollPaymentBankCorrectionStatus.Cancelled &&
                        !correction.IsDeleted)))
            .Select(x => x.PayrollRunLineId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var attention = attentionLineIds.ToHashSet();
        var payslipById = payslipLines.Values.ToDictionary(x => x.Id);

        foreach (var item in items)
        {
            if (!payslipById.TryGetValue(item.Id, out var line))
            {
                item.PaymentStatus = "Unpaid";
                continue;
            }

            if (attention.Contains(line.PayrollRunLineId))
            {
                item.PaymentStatus = "Attention";
                continue;
            }

            var paid = paidByLine.GetValueOrDefault(line.PayrollRunLineId);
            item.PaymentStatus = paid <= 0
                ? "Unpaid"
                : paid >= line.NetPayMinor
                    ? "Paid"
                    : "PartiallyPaid";
        }
    }
}

public sealed class DownloadMyPayslipDocumentHandler
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IObjectStorageService _storage;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public DownloadMyPayslipDocumentHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IObjectStorageService storage,
        IClock clock,
        BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Result<MemberPayslipDownloadResult>> HandleAsync(Guid payslipId, CancellationToken ct = default)
    {
        if (payslipId == Guid.Empty) return Result<MemberPayslipDownloadResult>.Fail("PayrollPayslipNotFound");
        var userId = _currentUser.GetCurrentUserId();
        if (userId == Guid.Empty) return Result<MemberPayslipDownloadResult>.Fail("PayrollPayslipNotFound");

        var row = await (
            from payslip in _db.Set<PayrollPayslip>().AsNoTracking()
            join employee in _db.Set<Employee>().AsNoTracking() on payslip.EmployeeId equals employee.Id
            join member in _db.Set<BusinessMember>().AsNoTracking() on employee.BusinessMemberId equals member.Id
            join document in _db.Set<DocumentRecord>().AsNoTracking() on payslip.DocumentRecordId equals document.Id
            where payslip.Id == payslipId &&
                  !payslip.IsDeleted &&
                  !employee.IsDeleted &&
                  !member.IsDeleted &&
                  member.IsActive &&
                  member.UserId == userId &&
                  member.BusinessId == payslip.BusinessId &&
                  employee.BusinessId == payslip.BusinessId &&
                  !document.IsDeleted &&
                  document.EntityType == PayrollPayslipArtifactService.EntityType &&
                  document.EntityId == payslip.Id &&
                  document.ContentType == PayrollPayslipArtifactService.PdfContentType
            select new { Payslip = payslip, Employee = employee, Document = document }).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (row is null) return Result<MemberPayslipDownloadResult>.Fail("PayrollPayslipNotFound");

        var read = await _storage.ReadAsync(new ObjectStorageObjectReference(
            row.Document.StorageContainer,
            row.Document.StorageKey,
            ProfileName: PayrollPayslipArtifactService.ProfileName), ct).ConfigureAwait(false);
        if (read is null) return Result<MemberPayslipDownloadResult>.Fail("PayrollPayslipObjectNotFound");

        await RecordDownloadAuditAsync(row.Payslip, row.Employee, userId, ct).ConfigureAwait(false);

        return Result<MemberPayslipDownloadResult>.Ok(new MemberPayslipDownloadResult(
            read.Content,
            string.IsNullOrWhiteSpace(read.ContentType) ? row.Document.ContentType : read.ContentType,
            string.IsNullOrWhiteSpace(read.FileName) ? row.Document.FileName : read.FileName!,
            read.ContentLength ?? row.Document.SizeBytes,
            read.Sha256Hash ?? row.Document.ContentHash));
    }

    private async Task RecordDownloadAuditAsync(PayrollPayslip payslip, Employee employee, Guid userId, CancellationToken ct)
    {
        if (_events is null)
        {
            return;
        }

        var now = _clock.UtcNow;
        var payload = JsonSerializer.Serialize(new
        {
            payslip.Id,
            payslip.BusinessId,
            payslip.EmployeeId,
            employee.BusinessMemberId,
            userId,
            payslip.PayslipNumber,
            Status = payslip.Status.ToString()
        });
        var eventResult = await _events.AddEventAsync(new AddBusinessEventCommand(
            payslip.BusinessId,
            PayrollPayslipArtifactService.EntityType,
            payslip.Id,
            "hr.employee_payslip.downloaded",
            $"hr.employee_payslip.downloaded:{userId:N}:{payslip.Id:N}:{now.Ticks}",
            now,
            userId,
            BusinessEventSource.User,
            BusinessEventSeverity.Info,
            FoundationVisibility.Internal,
            "Employee payslip downloaded",
            Summary: payslip.PayslipNumber,
            PayloadJson: payload), ct).ConfigureAwait(false);
        if (!eventResult.Succeeded)
        {
            throw new InvalidOperationException(eventResult.Error ?? "PayrollPayslipDownloadAuditFailed");
        }

        var auditResult = await _events.AddAuditTrailAsync(new AddAuditTrailCommand(
            payslip.BusinessId,
            PayrollPayslipArtifactService.EntityType,
            payslip.Id,
            AuditTrailAction.Exported,
            now,
            userId,
            eventResult.Value,
            Reason: "Employee self-service payslip download",
            MetadataJson: payload), ct).ConfigureAwait(false);
        if (!auditResult.Succeeded)
        {
            throw new InvalidOperationException(auditResult.Error ?? "PayrollPayslipDownloadAuditFailed");
        }
    }
}
