using System.Security.Cryptography;
using System.Text;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Foundation;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Commands;

public sealed class CreateBankAccountHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreateBankAccountHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Guid> HandleAsync(BankAccountCreateDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        BankTreasurySupport.ValidateBankAccount(dto);
        await BankTreasurySupport.ValidateFinancialAccountAsync(_db, dto.BusinessId, dto.FinancialAccountId, ct).ConfigureAwait(false);
        var account = new BankAccount
        {
            BusinessId = dto.BusinessId,
            FinancialAccountId = BankTreasurySupport.NormalizeGuid(dto.FinancialAccountId),
            Code = BankTreasurySupport.Required(dto.Code, 64),
            DisplayName = BankTreasurySupport.Required(dto.DisplayName, 200),
            BankName = BankTreasurySupport.Optional(dto.BankName, 200),
            Currency = BankTreasurySupport.NormalizeCurrency(dto.Currency),
            MaskedAccountIdentifier = BankTreasurySupport.Optional(dto.MaskedAccountIdentifier, 128),
            IsDefault = dto.IsDefault,
            MetadataJson = SupplierInvoiceSupport.NormalizeMetadata(dto.MetadataJson)
        };
        _db.Set<BankAccount>().Add(account);
        if (account.IsDefault) await BankTreasurySupport.ClearOtherDefaultsAsync(_db, account.BusinessId, account.Id, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await BankTreasurySupport.RecordBankAccountEvidenceAsync(_events, account, "created", AuditTrailAction.Created, _clock.UtcNow, ct).ConfigureAwait(false);
        return account.Id;
    }
}

public sealed class UpdateBankAccountHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public UpdateBankAccountHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task HandleAsync(BankAccountEditDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Id == Guid.Empty || dto.RowVersion.Length == 0) throw new ArgumentException("BankAccountInvalidUpdate");
        BankTreasurySupport.ValidateBankAccount(dto);
        var account = await BankTreasurySupport.LoadBankAccountForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        if (account.Status == BankAccountStatus.Archived) throw new InvalidOperationException("BankAccountArchived");
        await BankTreasurySupport.ValidateFinancialAccountAsync(_db, dto.BusinessId, dto.FinancialAccountId, ct).ConfigureAwait(false);
        account.BusinessId = dto.BusinessId;
        account.FinancialAccountId = BankTreasurySupport.NormalizeGuid(dto.FinancialAccountId);
        account.Code = BankTreasurySupport.Required(dto.Code, 64);
        account.DisplayName = BankTreasurySupport.Required(dto.DisplayName, 200);
        account.BankName = BankTreasurySupport.Optional(dto.BankName, 200);
        account.Currency = BankTreasurySupport.NormalizeCurrency(dto.Currency);
        account.MaskedAccountIdentifier = BankTreasurySupport.Optional(dto.MaskedAccountIdentifier, 128);
        account.IsDefault = dto.IsDefault;
        account.MetadataJson = SupplierInvoiceSupport.NormalizeMetadata(dto.MetadataJson);
        if (account.IsDefault) await BankTreasurySupport.ClearOtherDefaultsAsync(_db, account.BusinessId, account.Id, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await BankTreasurySupport.RecordBankAccountEvidenceAsync(_events, account, "updated", AuditTrailAction.Updated, _clock.UtcNow, ct).ConfigureAwait(false);
    }
}

public sealed class ArchiveBankAccountHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public ArchiveBankAccountHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task HandleAsync(BankStatementImportLifecycleActionDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var account = await BankTreasurySupport.LoadBankAccountForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        account.Status = BankAccountStatus.Archived;
        account.IsDefault = false;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await BankTreasurySupport.RecordBankAccountEvidenceAsync(_events, account, "archived", AuditTrailAction.StatusChanged, _clock.UtcNow, ct).ConfigureAwait(false);
    }
}

public sealed class CreateBankStatementImportHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreateBankStatementImportHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Guid> HandleAsync(BankStatementImportCreateDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        BankTreasurySupport.ValidateStatementImport(dto);
        var account = await _db.Set<BankAccount>().FirstOrDefaultAsync(x => x.Id == dto.BankAccountId && x.BusinessId == dto.BusinessId && !x.IsDeleted, ct).ConfigureAwait(false);
        if (account is null || account.Status != BankAccountStatus.Active) throw new InvalidOperationException("BankAccountNotFound");
        var lines = BankTreasurySupport.MapStatementLines(dto, account.Currency);
        var import = new BankStatementImport
        {
            BusinessId = dto.BusinessId,
            BankAccountId = dto.BankAccountId,
            StatementReference = BankTreasurySupport.Required(dto.StatementReference, 128),
            PeriodStartUtc = SupplierInvoiceSupport.EnsureUtc(dto.PeriodStartUtc),
            PeriodEndUtc = SupplierInvoiceSupport.EnsureUtc(dto.PeriodEndUtc),
            ImportedAtUtc = _clock.UtcNow,
            MetadataJson = SupplierInvoiceSupport.NormalizeMetadata(dto.MetadataJson),
            Lines = lines
        };
        BankTreasurySupport.RecalculateTotals(import);
        _db.Set<BankStatementImport>().Add(import);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await BankTreasurySupport.RecordStatementEvidenceAsync(_events, import, "imported", AuditTrailAction.Created, _clock.UtcNow, ct).ConfigureAwait(false);
        return import.Id;
    }
}

public sealed class CancelBankStatementImportHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CancelBankStatementImportHandler(IAppDbContext db, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task HandleAsync(BankStatementImportLifecycleActionDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Id == Guid.Empty || dto.RowVersion.Length == 0) throw new ArgumentException("BankStatementImportInvalidUpdate");
        var import = await _db.Set<BankStatementImport>().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (import is null) throw new InvalidOperationException("BankStatementImportNotFound");
        if (!(import.RowVersion ?? Array.Empty<byte>()).SequenceEqual(dto.RowVersion)) throw new InvalidOperationException("ItemConcurrencyConflict");
        import.Status = BankStatementImportStatus.Cancelled;
        foreach (var line in import.Lines.Where(x => !x.IsDeleted))
        {
            line.ReviewStatus = BankStatementLineReviewStatus.Ignored;
        }
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await BankTreasurySupport.RecordStatementEvidenceAsync(_events, import, "cancelled", AuditTrailAction.StatusChanged, _clock.UtcNow, ct).ConfigureAwait(false);
    }
}

internal static class BankTreasurySupport
{
    private static readonly string[] RawAccountMarkers = ["iban", "accountnumber", "routingnumber", "bic", "swift", "pan"];

    public static void ValidateBankAccount(BankAccountCreateDto dto)
    {
        if (dto.BusinessId == Guid.Empty) throw new ArgumentException("BankAccountInvalidBusiness");
        _ = Required(dto.Code, 64);
        _ = Required(dto.DisplayName, 200);
        _ = NormalizeCurrency(dto.Currency);
        _ = SupplierInvoiceSupport.NormalizeMetadata(dto.MetadataJson);
        RejectSensitivePlainText(dto.Code);
        RejectSensitivePlainText(dto.DisplayName);
        RejectSensitivePlainText(dto.BankName);
        RejectRawAccountIdentifier(dto.MaskedAccountIdentifier);
    }

    public static void ValidateStatementImport(BankStatementImportCreateDto dto)
    {
        if (dto.BusinessId == Guid.Empty || dto.BankAccountId == Guid.Empty) throw new ArgumentException("BankStatementImportInvalidLink");
        _ = Required(dto.StatementReference, 128);
        _ = SupplierInvoiceSupport.NormalizeMetadata(dto.MetadataJson);
        if (dto.PeriodStartUtc == default || dto.PeriodEndUtc == default || SupplierInvoiceSupport.EnsureUtc(dto.PeriodStartUtc) >= SupplierInvoiceSupport.EnsureUtc(dto.PeriodEndUtc)) throw new ArgumentException("BankStatementImportInvalidPeriod");
        if (dto.Lines.Count == 0) throw new ArgumentException("BankStatementLinesRequired");
    }

    public static async Task ValidateFinancialAccountAsync(IAppDbContext db, Guid businessId, Guid? financialAccountId, CancellationToken ct)
    {
        var id = NormalizeGuid(financialAccountId);
        if (!id.HasValue) return;
        var account = await db.Set<FinancialAccount>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value && !x.IsDeleted, ct).ConfigureAwait(false);
        if (account is null || account.BusinessId != businessId) throw new InvalidOperationException("FinancialAccountNotFound");
        if (account.Type != AccountType.Asset) throw new InvalidOperationException("BankAccountFinancialAccountMustBeAsset");
    }

    public static async Task ClearOtherDefaultsAsync(IAppDbContext db, Guid businessId, Guid currentId, CancellationToken ct)
    {
        var others = await db.Set<BankAccount>().Where(x => x.BusinessId == businessId && x.Id != currentId && x.IsDefault && !x.IsDeleted).ToListAsync(ct).ConfigureAwait(false);
        foreach (var other in others) other.IsDefault = false;
    }

    public static async Task<BankAccount> LoadBankAccountForUpdateAsync(IAppDbContext db, Guid id, byte[] rowVersion, CancellationToken ct)
    {
        if (id == Guid.Empty || rowVersion.Length == 0) throw new ArgumentException("BankAccountInvalidUpdate");
        var account = await db.Set<BankAccount>().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (account is null) throw new InvalidOperationException("BankAccountNotFound");
        if (!(account.RowVersion ?? Array.Empty<byte>()).SequenceEqual(rowVersion)) throw new InvalidOperationException("ItemConcurrencyConflict");
        return account;
    }

    public static List<BankStatementLine> MapStatementLines(BankStatementImportCreateDto dto, string accountCurrency)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return dto.Lines.Select(line =>
        {
            var currency = NormalizeCurrency(line.Currency);
            if (!string.Equals(currency, accountCurrency, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("BankStatementLineCurrencyMismatch");
            if (line.AmountMinor <= 0) throw new ArgumentException("BankStatementLineInvalidAmount");
            if (line.TransactionDateUtc == default) throw new ArgumentException("BankStatementLineInvalidDate");
            _ = SupplierInvoiceSupport.NormalizeMetadata(line.MetadataJson);
            RejectSensitivePlainText(line.CounterpartyName);
            RejectSensitivePlainText(line.CounterpartyReference);
            RejectSensitivePlainText(line.RemittanceInformation);
            var identity = string.IsNullOrWhiteSpace(line.NormalizedIdentityKey)
                ? BuildIdentityKey(line)
                : Required(line.NormalizedIdentityKey, 256).ToUpperInvariant();
            if (!seen.Add(identity)) throw new ArgumentException("BankStatementLineDuplicateIdentity");
            return new BankStatementLine
            {
                BusinessId = dto.BusinessId,
                BankAccountId = dto.BankAccountId,
                TransactionDateUtc = SupplierInvoiceSupport.EnsureUtc(line.TransactionDateUtc),
                ValueDateUtc = SupplierInvoiceSupport.EnsureUtc(line.ValueDateUtc),
                Direction = line.Direction,
                AmountMinor = line.AmountMinor,
                Currency = currency,
                CounterpartyName = Optional(line.CounterpartyName, 256),
                CounterpartyReference = Optional(line.CounterpartyReference, 256),
                RemittanceInformation = Optional(line.RemittanceInformation, 1000),
                NormalizedIdentityKey = identity,
                ReviewStatus = BankStatementLineReviewStatus.Unreviewed,
                MetadataJson = SupplierInvoiceSupport.NormalizeMetadata(line.MetadataJson)
            };
        }).ToList();
    }

    public static void RecalculateTotals(BankStatementImport import)
    {
        import.LineCount = import.Lines.Count(x => !x.IsDeleted);
        import.DebitTotalMinor = import.Lines.Where(x => !x.IsDeleted && x.Direction == BankStatementLineDirection.Debit).Sum(x => x.AmountMinor);
        import.CreditTotalMinor = import.Lines.Where(x => !x.IsDeleted && x.Direction == BankStatementLineDirection.Credit).Sum(x => x.AmountMinor);
    }

    public static async Task RecordBankAccountEvidenceAsync(BusinessEventService? events, BankAccount account, string action, AuditTrailAction auditAction, DateTime now, CancellationToken ct)
    {
        if (events is null) return;
        var payload = $$"""{"bankAccountId":"{{account.Id}}","businessId":"{{account.BusinessId}}","status":"{{account.Status}}","currency":"{{account.Currency}}","isDefault":{{account.IsDefault.ToString().ToLowerInvariant()}}}""";
        var eventResult = await events.AddEventAsync(new AddBusinessEventCommand(account.BusinessId, "BankAccount", account.Id, $"treasury.bank_account.{action}", $"treasury.bank_account.{action}:{account.Id}:{account.Status}", now, null, BusinessEventSource.User, BusinessEventSeverity.Info, FoundationVisibility.Internal, $"Bank account {action}", null, null, null, payload), ct).ConfigureAwait(false);
        if (!eventResult.Succeeded) throw new InvalidOperationException(eventResult.Error);
        var auditResult = await events.AddAuditTrailAsync(new AddAuditTrailCommand(account.BusinessId, "BankAccount", account.Id, auditAction, now, null, eventResult.Value, $"Bank account {action}", null, payload), ct).ConfigureAwait(false);
        if (!auditResult.Succeeded) throw new InvalidOperationException(auditResult.Error);
    }

    public static async Task RecordStatementEvidenceAsync(BusinessEventService? events, BankStatementImport import, string action, AuditTrailAction auditAction, DateTime now, CancellationToken ct)
    {
        if (events is null) return;
        var payload = $$"""{"bankStatementImportId":"{{import.Id}}","businessId":"{{import.BusinessId}}","bankAccountId":"{{import.BankAccountId}}","status":"{{import.Status}}","lineCount":{{import.LineCount}},"debitTotalMinor":{{import.DebitTotalMinor}},"creditTotalMinor":{{import.CreditTotalMinor}}}""";
        var eventResult = await events.AddEventAsync(new AddBusinessEventCommand(import.BusinessId, "BankStatementImport", import.Id, $"treasury.bank_statement.{action}", $"treasury.bank_statement.{action}:{import.Id}:{import.Status}", now, null, BusinessEventSource.User, BusinessEventSeverity.Info, FoundationVisibility.Internal, $"Bank statement {action}", null, null, null, payload), ct).ConfigureAwait(false);
        if (!eventResult.Succeeded) throw new InvalidOperationException(eventResult.Error);
        var auditResult = await events.AddAuditTrailAsync(new AddAuditTrailCommand(import.BusinessId, "BankStatementImport", import.Id, auditAction, now, null, eventResult.Value, $"Bank statement {action}", null, payload), ct).ConfigureAwait(false);
        if (!auditResult.Succeeded) throw new InvalidOperationException(auditResult.Error);
    }

    public static string Required(string? value, int max)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0) throw new ArgumentException("BankTreasuryRequiredField");
        return normalized.Length > max ? normalized[..max] : normalized;
    }

    public static string? Optional(string? value, int max)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        return normalized.Length > max ? normalized[..max] : normalized;
    }

    public static string NormalizeCurrency(string? value) => SupplierInvoiceSupport.NormalizeCurrency(value);
    public static Guid? NormalizeGuid(Guid? value) => value.HasValue && value.Value != Guid.Empty ? value.Value : null;

    private static string BuildIdentityKey(BankStatementLineDto line)
    {
        var raw = string.Join("|", SupplierInvoiceSupport.EnsureUtc(line.TransactionDateUtc).ToString("O"), line.Direction, line.AmountMinor, NormalizeCurrency(line.Currency), line.CounterpartyName?.Trim(), line.CounterpartyReference?.Trim(), line.RemittanceInformation?.Trim());
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    private static void RejectRawAccountIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        RejectSensitivePlainText(value);
        var digits = value.Count(char.IsDigit);
        var hasMask = value.Contains('*', StringComparison.Ordinal) || value.Contains('x', StringComparison.OrdinalIgnoreCase);
        if (digits > 6 && !hasMask) throw new ArgumentException("BankAccountIdentifierMustBeMasked");
    }

    private static void RejectSensitivePlainText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var compact = value.Replace("_", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
        if (RawAccountMarkers.Any(compact.Contains)) throw new ArgumentException("SensitiveMetadataRejected");
        _ = SupplierInvoiceSupport.NormalizeMetadata(value);
    }
}
