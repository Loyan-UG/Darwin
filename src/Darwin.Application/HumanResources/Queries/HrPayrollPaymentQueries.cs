using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Billing;
using Darwin.Application.HumanResources.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.HumanResources.Queries;

public sealed class GetPayrollPaymentsPageHandler
{
    private readonly IAppDbContext _db;

    public GetPayrollPaymentsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<PayrollPaymentsPageDto> HandleAsync(Guid? businessId = null, string? query = null, PayrollPaymentQueueFilter filter = PayrollPaymentQueueFilter.All, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var normalizedQuery = query?.Trim() ?? string.Empty;
        var businesses = await _db.Set<Business>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var businessOptions = businesses.Select(x => new ValueTuple<Guid, string>(x.Id, x.Name)).ToList();
        var dto = new PayrollPaymentsPageDto
        {
            BusinessId = businessId,
            BusinessName = businessId.HasValue ? businessOptions.FirstOrDefault(x => x.Item1 == businessId.Value).Item2 ?? string.Empty : string.Empty,
            BusinessOptions = businessOptions,
            Query = normalizedQuery,
            Filter = filter,
            Page = page,
            PageSize = pageSize
        };
        if (!businessId.HasValue || businessId.Value == Guid.Empty) return dto;

        var payments = _db.Set<PayrollPayment>().AsNoTracking().Where(x => x.BusinessId == businessId.Value && !x.IsDeleted);
        dto.DraftCount = await payments.CountAsync(x => x.Status == PayrollPaymentStatus.Draft, ct).ConfigureAwait(false);
        dto.PostedCount = await payments.CountAsync(x => x.Status == PayrollPaymentStatus.Posted, ct).ConfigureAwait(false);
        dto.CancelledCount = await payments.CountAsync(x => x.Status == PayrollPaymentStatus.Cancelled, ct).ConfigureAwait(false);
        dto.ReversedCount = await payments.CountAsync(x => x.Status == PayrollPaymentStatus.Reversed, ct).ConfigureAwait(false);

        payments = filter switch
        {
            PayrollPaymentQueueFilter.Draft => payments.Where(x => x.Status == PayrollPaymentStatus.Draft),
            PayrollPaymentQueueFilter.Posted => payments.Where(x => x.Status == PayrollPaymentStatus.Posted),
            PayrollPaymentQueueFilter.Cancelled => payments.Where(x => x.Status == PayrollPaymentStatus.Cancelled),
            PayrollPaymentQueueFilter.Reversed => payments.Where(x => x.Status == PayrollPaymentStatus.Reversed),
            _ => payments
        };

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            payments = payments.Where(x =>
                (x.PaymentNumber != null && x.PaymentNumber.Contains(normalizedQuery)) ||
                (x.Reference != null && x.Reference.Contains(normalizedQuery)) ||
                _db.Set<PayrollRun>().Any(run => run.Id == x.PayrollRunId && run.RunNumber.Contains(normalizedQuery)));
        }

        dto.Total = await payments.CountAsync(ct).ConfigureAwait(false);
        dto.Items = await payments
            .OrderByDescending(x => x.PaymentDateUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PayrollPaymentListItemDto
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                PayrollRunId = x.PayrollRunId,
                PaymentNumber = x.PaymentNumber ?? string.Empty,
                RunNumber = _db.Set<PayrollRun>().Where(run => run.Id == x.PayrollRunId).Select(run => run.RunNumber).FirstOrDefault() ?? string.Empty,
                Status = x.Status,
                PaymentMethod = x.PaymentMethod,
                PaymentDateUtc = x.PaymentDateUtc,
                Currency = x.Currency,
                TotalAmountMinor = x.TotalAmountMinor,
                AllocationCount = x.Allocations.Count(a => !a.IsDeleted),
                PostedAtUtc = x.PostedAtUtc,
                Reference = x.Reference ?? string.Empty,
                RowVersion = x.RowVersion
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return dto;
    }
}

public sealed class GetPayrollPaymentDetailHandler
{
    private readonly IAppDbContext _db;

    public GetPayrollPaymentDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<PayrollPaymentEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return null;
        var payment = await _db.Set<PayrollPayment>()
            .AsNoTracking()
            .Include(x => x.Allocations)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (payment is null) return null;

        var lineIds = payment.Allocations.Where(x => !x.IsDeleted).Select(x => x.PayrollRunLineId).ToArray();
        var lines = await _db.Set<PayrollRunLine>().AsNoTracking().Where(x => lineIds.Contains(x.Id) && !x.IsDeleted).ToDictionaryAsync(x => x.Id, ct).ConfigureAwait(false);
        var paid = await PayrollPaymentQuerySupport.GetPostedPaidByLineAsync(_db, lineIds, payment.Id, ct).ConfigureAwait(false);

        return new PayrollPaymentEditDto
        {
            Id = payment.Id,
            RowVersion = payment.RowVersion,
            BusinessId = payment.BusinessId,
            PayrollRunId = payment.PayrollRunId,
            PaymentNumber = payment.PaymentNumber ?? string.Empty,
            Status = payment.Status,
            PaymentMethod = payment.PaymentMethod,
            PaymentDateUtc = payment.PaymentDateUtc,
            Currency = payment.Currency,
            TotalAmountMinor = payment.TotalAmountMinor,
            Reference = payment.Reference,
            PostingJournalEntryId = payment.PostingJournalEntryId,
            PostedAtUtc = payment.PostedAtUtc,
            CancelledAtUtc = payment.CancelledAtUtc,
            ReversalJournalEntryId = payment.ReversalJournalEntryId,
            ReversedAtUtc = payment.ReversedAtUtc,
            ReversalReason = payment.ReversalReason,
            BankSettledAtUtc = payment.BankSettledAtUtc,
            BankSettlementJournalEntryId = payment.BankSettlementJournalEntryId,
            BankSettlementReconciliationMatchId = payment.BankSettlementReconciliationMatchId,
            BankSettlementNotes = payment.BankSettlementNotes,
            InternalNotes = payment.InternalNotes,
            MetadataJson = payment.MetadataJson,
            BankSettlementCandidates = await PayrollPaymentQuerySupport.GetBankSettlementCandidatesAsync(_db, payment, ct).ConfigureAwait(false),
            BankCorrectionCandidates = await PayrollPaymentQuerySupport.GetBankCorrectionCandidatesAsync(_db, payment, ct).ConfigureAwait(false),
            BankCorrections = await PayrollPaymentQuerySupport.GetBankCorrectionsAsync(_db, payment.Id, ct).ConfigureAwait(false),
            Allocations = payment.Allocations.Where(x => !x.IsDeleted).OrderBy(x => x.CreatedAtUtc).Select(allocation =>
            {
                lines.TryGetValue(allocation.PayrollRunLineId, out var line);
                var alreadyPaid = paid.GetValueOrDefault(allocation.PayrollRunLineId);
                var net = line?.NetPayMinor ?? 0;
                return new PayrollPaymentAllocationDto
                {
                    PayrollRunLineId = allocation.PayrollRunLineId,
                    EmployeeId = allocation.EmployeeId,
                    EmployeeNumber = line?.EmployeeNumber ?? string.Empty,
                    EmployeeName = line?.EmployeeName ?? string.Empty,
                    LineNetPayMinor = net,
                    AlreadyPaidMinor = alreadyPaid,
                    OpenAmountMinor = Math.Max(0, net - alreadyPaid),
                    AmountMinor = allocation.AmountMinor,
                    Memo = allocation.Memo
                };
            }).ToList()
        };
    }
}

public sealed class GetPayrollPaymentDraftHandler
{
    private readonly IAppDbContext _db;

    public GetPayrollPaymentDraftHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<PayrollPaymentEditDto> HandleAsync(Guid? businessId, Guid? payrollRunId, CancellationToken ct = default)
    {
        var draft = new PayrollPaymentEditDto
        {
            BusinessId = businessId ?? Guid.Empty,
            PayrollRunId = payrollRunId ?? Guid.Empty,
            PaymentDateUtc = DateTime.UtcNow,
            Currency = "EUR",
            MetadataJson = "{}"
        };
        if (payrollRunId is not { } runId || runId == Guid.Empty) return draft;

        var run = await _db.Set<PayrollRun>().AsNoTracking().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == runId && !x.IsDeleted && x.Status == PayrollRunStatus.Posted, ct).ConfigureAwait(false);
        if (run is null) return draft;

        draft.BusinessId = run.BusinessId;
        draft.PayrollRunId = run.Id;
        draft.Currency = run.Currency;
        var lineIds = run.Lines.Where(x => !x.IsDeleted && x.NetPayMinor > 0).Select(x => x.Id).ToArray();
        var paid = await PayrollPaymentQuerySupport.GetPostedPaidByLineAsync(_db, lineIds, null, ct).ConfigureAwait(false);
        draft.Allocations = run.Lines.Where(x => !x.IsDeleted && x.NetPayMinor > 0).OrderBy(x => x.EmployeeName).Select(line =>
        {
            var alreadyPaid = paid.GetValueOrDefault(line.Id);
            var open = Math.Max(0, line.NetPayMinor - alreadyPaid);
            return new PayrollPaymentAllocationDto
            {
                PayrollRunLineId = line.Id,
                EmployeeId = line.EmployeeId,
                EmployeeNumber = line.EmployeeNumber,
                EmployeeName = line.EmployeeName,
                LineNetPayMinor = line.NetPayMinor,
                AlreadyPaidMinor = alreadyPaid,
                OpenAmountMinor = open,
                AmountMinor = open
            };
        }).Where(x => x.AmountMinor > 0).ToList();
        return draft;
    }
}

internal static class PayrollPaymentQuerySupport
{
    public static async Task<Dictionary<Guid, long>> GetPostedPaidByLineAsync(IAppDbContext db, IReadOnlyCollection<Guid> lineIds, Guid? excludingPaymentId, CancellationToken ct)
    {
        if (lineIds.Count == 0) return new Dictionary<Guid, long>();
        return await db.Set<PayrollPaymentAllocation>()
            .AsNoTracking()
            .Where(allocation =>
                lineIds.Contains(allocation.PayrollRunLineId) &&
                !allocation.IsDeleted &&
                db.Set<PayrollPayment>().Any(payment =>
                    payment.Id == allocation.PayrollPaymentId &&
                    payment.Status == PayrollPaymentStatus.Posted &&
                    !payment.IsDeleted &&
                    (!excludingPaymentId.HasValue || payment.Id != excludingPaymentId.Value)))
            .GroupBy(x => x.PayrollRunLineId)
            .Select(x => new { LineId = x.Key, Amount = x.Sum(a => a.AmountMinor) })
            .ToDictionaryAsync(x => x.LineId, x => x.Amount, ct)
            .ConfigureAwait(false);
    }

    public static async Task<List<PayrollPaymentBankSettlementCandidateDto>> GetBankSettlementCandidatesAsync(IAppDbContext db, PayrollPayment payment, CancellationToken ct)
    {
        if (payment.Id == Guid.Empty ||
            payment.Status != PayrollPaymentStatus.Posted ||
            payment.PostingJournalEntryId is null ||
            payment.BankSettledAtUtc.HasValue ||
            payment.ReversalJournalEntryId.HasValue)
        {
            return new List<PayrollPaymentBankSettlementCandidateDto>();
        }

        return await db.Set<BankReconciliationMatch>()
            .AsNoTracking()
            .Where(match =>
                match.BusinessId == payment.BusinessId &&
                match.Status == BankReconciliationMatchStatus.Matched &&
                match.Currency == payment.Currency &&
                match.DifferenceMinor == 0 &&
                !match.IsDeleted &&
                db.Set<BankAccount>().Any(account =>
                    account.Id == match.BankAccountId &&
                    account.BusinessId == payment.BusinessId &&
                    account.Status == BankAccountStatus.Active &&
                    !account.IsDeleted &&
                    account.FinancialAccountId.HasValue &&
                    db.Set<FinancialAccount>().Any(financialAccount =>
                        financialAccount.Id == account.FinancialAccountId.Value &&
                        financialAccount.BusinessId == payment.BusinessId &&
                        financialAccount.Type == AccountType.Asset &&
                        !financialAccount.IsDeleted)) &&
                match.Lines.Any(line =>
                    !line.IsDeleted &&
                    line.IsActive &&
                    line.JournalEntryId == payment.PostingJournalEntryId &&
                    line.SourceEntityType == "PayrollPayment" &&
                    line.SourceEntityId == payment.Id))
            .Select(match => new
            {
                Match = match,
                BankAccountName = db.Set<BankAccount>()
                    .Where(account => account.Id == match.BankAccountId)
                    .Select(account => account.DisplayName)
                    .FirstOrDefault() ?? string.Empty,
                PaymentAmount = match.Lines
                    .Where(line => !line.IsDeleted && line.IsActive && line.JournalEntryId == payment.PostingJournalEntryId && line.SourceEntityType == "PayrollPayment" && line.SourceEntityId == payment.Id)
                    .Sum(line => line.AmountMinor)
            })
            .Where(x => x.PaymentAmount == payment.TotalAmountMinor)
            .OrderByDescending(x => x.Match.MatchedAtUtc ?? x.Match.MatchDateUtc)
            .ThenByDescending(x => x.Match.CreatedAtUtc)
            .Select(x => new PayrollPaymentBankSettlementCandidateDto
            {
                BankReconciliationMatchId = x.Match.Id,
                MatchNumber = x.Match.MatchNumber ?? string.Empty,
                BankAccountId = x.Match.BankAccountId,
                BankAccountDisplayName = x.BankAccountName,
                MatchDateUtc = x.Match.MatchDateUtc,
                Currency = x.Match.Currency,
                BankTotalMinor = x.Match.BankTotalMinor,
                FinanceTotalMinor = x.Match.FinanceTotalMinor
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public static Task<List<PayrollPaymentBankCorrectionListItemDto>> GetBankCorrectionsAsync(IAppDbContext db, Guid payrollPaymentId, CancellationToken ct)
    {
        if (payrollPaymentId == Guid.Empty)
        {
            return Task.FromResult(new List<PayrollPaymentBankCorrectionListItemDto>());
        }

        return db.Set<PayrollPaymentBankCorrection>()
            .AsNoTracking()
            .Where(x => x.PayrollPaymentId == payrollPaymentId && !x.IsDeleted)
            .OrderByDescending(x => x.CorrectionDateUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new PayrollPaymentBankCorrectionListItemDto
            {
                Id = x.Id,
                PayrollPaymentId = x.PayrollPaymentId,
                BankReconciliationMatchId = x.BankReconciliationMatchId,
                BankStatementLineId = x.BankStatementLineId,
                CorrectionJournalEntryId = x.CorrectionJournalEntryId,
                CorrectionType = x.CorrectionType,
                Status = x.Status,
                CorrectionDateUtc = x.CorrectionDateUtc,
                PostedAtUtc = x.PostedAtUtc,
                Currency = x.Currency,
                AmountMinor = x.AmountMinor,
                Reason = x.Reason,
                RowVersion = x.RowVersion ?? Array.Empty<byte>()
            })
            .ToListAsync(ct);
    }

    public static async Task<List<PayrollPaymentBankSettlementCandidateDto>> GetBankCorrectionCandidatesAsync(IAppDbContext db, PayrollPayment payment, CancellationToken ct)
    {
        if (payment.Id == Guid.Empty ||
            payment.Status != PayrollPaymentStatus.Posted ||
            payment.PostingJournalEntryId is null ||
            payment.BankSettlementJournalEntryId is null ||
            payment.BankSettlementReconciliationMatchId is null ||
            !payment.BankSettledAtUtc.HasValue ||
            payment.ReversalJournalEntryId.HasValue)
        {
            return new List<PayrollPaymentBankSettlementCandidateDto>();
        }

        return await db.Set<BankReconciliationMatch>()
            .AsNoTracking()
            .Where(match =>
                match.Id != payment.BankSettlementReconciliationMatchId.Value &&
                match.BusinessId == payment.BusinessId &&
                match.Status == BankReconciliationMatchStatus.Matched &&
                match.Currency == payment.Currency &&
                match.DifferenceMinor == 0 &&
                !match.IsDeleted &&
                match.Lines.Any(line =>
                    !line.IsDeleted &&
                    line.IsActive &&
                    (line.JournalEntryId == payment.BankSettlementJournalEntryId.Value ||
                     (line.SourceEntityType == "PayrollPayment" && line.SourceEntityId == payment.Id))))
            .Select(match => new
            {
                Match = match,
                BankAccountName = db.Set<BankAccount>()
                    .Where(account => account.Id == match.BankAccountId)
                    .Select(account => account.DisplayName)
                    .FirstOrDefault() ?? string.Empty,
                PaymentAmount = match.Lines
                    .Where(line =>
                        !line.IsDeleted &&
                        line.IsActive &&
                        (line.JournalEntryId == payment.BankSettlementJournalEntryId.Value ||
                         (line.SourceEntityType == "PayrollPayment" && line.SourceEntityId == payment.Id)))
                    .Sum(line => line.AmountMinor)
            })
            .Where(x => x.PaymentAmount == payment.TotalAmountMinor)
            .OrderByDescending(x => x.Match.MatchedAtUtc ?? x.Match.MatchDateUtc)
            .ThenByDescending(x => x.Match.CreatedAtUtc)
            .Select(x => new PayrollPaymentBankSettlementCandidateDto
            {
                BankReconciliationMatchId = x.Match.Id,
                MatchNumber = x.Match.MatchNumber ?? string.Empty,
                BankAccountId = x.Match.BankAccountId,
                BankAccountDisplayName = x.BankAccountName,
                MatchDateUtc = x.Match.MatchDateUtc,
                Currency = x.Match.Currency,
                BankTotalMinor = x.Match.BankTotalMinor,
                FinanceTotalMinor = x.Match.FinanceTotalMinor
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
