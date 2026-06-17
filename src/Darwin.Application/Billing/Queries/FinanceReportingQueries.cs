using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Billing.Services;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Queries;

public sealed class GetFinanceOverviewHandler
{
    private readonly IAppDbContext _db;
    private readonly ReceivablesProjectionService _receivables;

    public GetFinanceOverviewHandler(IAppDbContext db, ReceivablesProjectionService receivables)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _receivables = receivables ?? throw new ArgumentNullException(nameof(receivables));
    }

    public async Task<FinanceOverviewDto> HandleAsync(Guid? businessId = null, CancellationToken ct = default)
    {
        var context = await FinanceReportingQuerySupport.ResolveBusinessContextAsync(_db, businessId, ct).ConfigureAwait(false);
        var dto = new FinanceOverviewDto
        {
            BusinessId = context.BusinessId,
            BusinessName = context.BusinessName,
            Currency = context.Currency,
            BusinessOptions = context.BusinessOptions
        };

        if (!context.BusinessId.HasValue)
        {
            dto.ReceivablesReadinessMessage = "FinanceReportingNoBusiness";
            return dto;
        }

        var selectedBusinessId = context.BusinessId.Value;
        var entries = _db.Set<JournalEntry>()
            .AsNoTracking()
            .Where(x => x.BusinessId == selectedBusinessId && !x.IsDeleted);

        dto.PostedJournalEntryCount = await entries.CountAsync(x => x.PostingStatus == JournalEntryPostingStatus.Posted, ct).ConfigureAwait(false);
        dto.DraftJournalEntryCount = await entries.CountAsync(x => x.PostingStatus == JournalEntryPostingStatus.Draft, ct).ConfigureAwait(false);
        dto.ReversedJournalEntryCount = await entries.CountAsync(x => x.PostingStatus == JournalEntryPostingStatus.Reversed, ct).ConfigureAwait(false);
        dto.SourceLinkedPostingCount = await entries.CountAsync(x => x.SourceEntityId.HasValue && x.SourceEntityType != null && x.SourceEntityType != string.Empty, ct).ConfigureAwait(false);
        dto.MissingSourcePostingCount = await entries.CountAsync(x => !x.SourceEntityId.HasValue || x.SourceEntityType == null || x.SourceEntityType == string.Empty, ct).ConfigureAwait(false);
        dto.IssuedCreditNoteCount = await _db.Set<CreditNote>()
            .AsNoTracking()
            .CountAsync(x => x.BusinessId == selectedBusinessId && x.Status == CreditNoteStatus.Issued && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        dto.UnpostedIssuedCreditNoteCount = await _db.Set<CreditNote>()
            .AsNoTracking()
            .CountAsync(x => x.BusinessId == selectedBusinessId && x.Status == CreditNoteStatus.Issued && !x.PostingJournalEntryId.HasValue && !x.IsDeleted, ct)
            .ConfigureAwait(false);

        dto.PostingKindBreakdown = await entries
            .Select(x => new
            {
                x.PostingKind,
                DebitMinor = x.Lines.Where(line => !line.IsDeleted).Sum(line => (long?)line.DebitMinor) ?? 0,
                CreditMinor = x.Lines.Where(line => !line.IsDeleted).Sum(line => (long?)line.CreditMinor) ?? 0
            })
            .GroupBy(x => x.PostingKind)
            .Select(x => new FinancePostingKindBreakdownDto
            {
                PostingKind = x.Key,
                Count = x.Count(),
                DebitMinor = x.Sum(row => row.DebitMinor),
                CreditMinor = x.Sum(row => row.CreditMinor)
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.PostingKind)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var receivables = await _receivables.GetSummaryAsync(new ReceivablesProjectionQuery(selectedBusinessId), ct).ConfigureAwait(false);
        if (receivables.Succeeded)
        {
            dto.ReceivablesDebitMinor = receivables.Value!.TotalDebitMinor;
            dto.ReceivablesCreditMinor = receivables.Value.TotalCreditMinor;
            dto.OpenReceivablesMinor = receivables.Value.OpenBalanceMinor;
            dto.TopReceivables = receivables.Value.Sources
                .Where(x => x.OpenBalanceMinor != 0)
                .OrderByDescending(x => Math.Abs(x.OpenBalanceMinor))
                .ThenByDescending(x => x.LastEntryDateUtc)
                .Take(8)
                .Select(FinanceReportingQuerySupport.MapReceivable)
                .ToList();
        }
        else
        {
            dto.ReceivablesReadinessMessage = receivables.Error;
        }

        dto.RecentPostings = await FinanceReportingQuerySupport.MapPostings(entries
                .OrderByDescending(x => x.EntryDateUtc)
                .ThenByDescending(x => x.CreatedAtUtc)
                .Take(10))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return dto;
    }
}

public sealed class GetFinanceReceivablesPageHandler
{
    private readonly IAppDbContext _db;
    private readonly ReceivablesProjectionService _receivables;

    public GetFinanceReceivablesPageHandler(IAppDbContext db, ReceivablesProjectionService receivables)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _receivables = receivables ?? throw new ArgumentNullException(nameof(receivables));
    }

    public async Task<FinanceReceivablesPageDto> HandleAsync(
        Guid? businessId = null,
        string? query = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var normalizedQuery = query?.Trim() ?? string.Empty;
        var context = await FinanceReportingQuerySupport.ResolveBusinessContextAsync(_db, businessId, ct).ConfigureAwait(false);
        var dto = new FinanceReceivablesPageDto
        {
            BusinessId = context.BusinessId,
            BusinessName = context.BusinessName,
            Currency = context.Currency,
            Query = normalizedQuery,
            Page = page,
            PageSize = pageSize,
            BusinessOptions = context.BusinessOptions
        };

        if (!context.BusinessId.HasValue)
        {
            dto.ReadinessMessage = "FinanceReportingNoBusiness";
            return dto;
        }

        var result = await _receivables.GetSummaryAsync(new ReceivablesProjectionQuery(context.BusinessId.Value), ct).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            dto.ReadinessMessage = result.Error;
            return dto;
        }

        var rows = result.Value!.Sources
            .Select(FinanceReportingQuerySupport.MapReceivable)
            .Where(x => string.IsNullOrWhiteSpace(normalizedQuery) ||
                        x.SourceEntityType.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                        x.SourceDocumentNumber.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                        x.LastPostingKey.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => Math.Abs(x.OpenBalanceMinor))
            .ThenByDescending(x => x.LastEntryDateUtc)
            .ToList();

        dto.TotalDebitMinor = result.Value.TotalDebitMinor;
        dto.TotalCreditMinor = result.Value.TotalCreditMinor;
        dto.OpenBalanceMinor = result.Value.OpenBalanceMinor;
        dto.Total = rows.Count;
        dto.Items = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return dto;
    }
}

public sealed class GetFinancePostingsPageHandler
{
    private readonly IAppDbContext _db;

    public GetFinancePostingsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<FinancePostingsPageDto> HandleAsync(
        Guid? businessId = null,
        string? query = null,
        JournalEntryPostingKind? postingKind = null,
        JournalEntryPostingStatus? postingStatus = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var normalizedQuery = query?.Trim() ?? string.Empty;
        var context = await FinanceReportingQuerySupport.ResolveBusinessContextAsync(_db, businessId, ct).ConfigureAwait(false);
        var dto = new FinancePostingsPageDto
        {
            BusinessId = context.BusinessId,
            BusinessName = context.BusinessName,
            Query = normalizedQuery,
            PostingKind = postingKind,
            PostingStatus = postingStatus,
            Page = page,
            PageSize = pageSize,
            BusinessOptions = context.BusinessOptions
        };

        if (!context.BusinessId.HasValue)
        {
            return dto;
        }

        var entries = _db.Set<JournalEntry>()
            .AsNoTracking()
            .Where(x => x.BusinessId == context.BusinessId.Value && !x.IsDeleted);

        if (postingKind.HasValue)
        {
            entries = entries.Where(x => x.PostingKind == postingKind.Value);
        }

        if (postingStatus.HasValue)
        {
            entries = entries.Where(x => x.PostingStatus == postingStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            entries = entries.Where(x =>
                x.Description.Contains(normalizedQuery) ||
                (x.PostingKey != null && x.PostingKey.Contains(normalizedQuery)) ||
                (x.SourceEntityType != null && x.SourceEntityType.Contains(normalizedQuery)) ||
                (x.SourceDocumentNumber != null && x.SourceDocumentNumber.Contains(normalizedQuery)));
        }

        dto.Total = await entries.CountAsync(ct).ConfigureAwait(false);
        dto.Items = await FinanceReportingQuerySupport.MapPostings(entries
                .OrderByDescending(x => x.EntryDateUtc)
                .ThenByDescending(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return dto;
    }
}

public sealed class GetFinanceAccountMappingsPageHandler
{
    private static readonly FinancePostingAccountRole[] RequiredSalesPostingRoles =
    [
        FinancePostingAccountRole.Receivables,
        FinancePostingAccountRole.SalesRevenue,
        FinancePostingAccountRole.TaxPayable,
        FinancePostingAccountRole.CashClearing
    ];

    private static readonly FinancePostingAccountRole[] RequiredPayablesPostingRoles =
    [
        FinancePostingAccountRole.AccountsPayable,
        FinancePostingAccountRole.PurchaseExpense,
        FinancePostingAccountRole.InventoryClearing,
        FinancePostingAccountRole.TaxReceivable
    ];

    private readonly IAppDbContext _db;
    private readonly FinanceAccountMappingService _mappingService;

    public GetFinanceAccountMappingsPageHandler(IAppDbContext db, FinanceAccountMappingService mappingService)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _mappingService = mappingService ?? throw new ArgumentNullException(nameof(mappingService));
    }

    public async Task<FinanceAccountMappingsPageDto> HandleAsync(Guid? businessId = null, CancellationToken ct = default)
    {
        var context = await FinanceReportingQuerySupport.ResolveBusinessContextAsync(_db, businessId, ct).ConfigureAwait(false);
        var dto = new FinanceAccountMappingsPageDto
        {
            BusinessId = context.BusinessId,
            BusinessName = context.BusinessName,
            BusinessOptions = context.BusinessOptions
        };

        if (!context.BusinessId.HasValue)
        {
            return dto;
        }

        var selectedBusinessId = context.BusinessId.Value;
        var accounts = await _db.Set<FinancialAccount>()
            .AsNoTracking()
            .Where(x => x.BusinessId == selectedBusinessId && !x.IsDeleted)
            .OrderBy(x => x.Type)
            .ThenBy(x => x.Code)
            .ThenBy(x => x.Name)
            .Select(x => new FinanceAccountOptionDto
            {
                Id = x.Id,
                Type = x.Type,
                Label = (x.Code == null || x.Code == string.Empty ? x.Name : x.Code + " - " + x.Name) + " (" + x.Type + ")"
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var mappings = (await _mappingService.GetMappingsForBusinessAsync(selectedBusinessId, ct).ConfigureAwait(false))
            .ToDictionary(x => x.Role);

        dto.Rows = Enum.GetValues<FinancePostingAccountRole>()
            .Select(role =>
            {
                mappings.TryGetValue(role, out var mapping);
                var compatibleAccounts = accounts.Where(x => FinanceAccountMappingService.IsAccountTypeAllowed(role, x.Type)).ToList();
                return new FinanceAccountMappingRowDto
                {
                    MappingId = mapping?.Id,
                    Role = role,
                    RoleName = role.ToString(),
                    AllowedAccountTypesLabel = BuildAllowedAccountTypesLabel(role),
                    FinancialAccountId = mapping?.FinancialAccountId,
                    FinancialAccountName = mapping?.FinancialAccountName ?? string.Empty,
                    FinancialAccountCode = mapping?.FinancialAccountCode ?? string.Empty,
                    FinancialAccountType = mapping?.FinancialAccountType,
                    IsActive = mapping?.IsActive ?? false,
                    Description = mapping?.Description ?? string.Empty,
                    IsCompatible = mapping is null || FinanceAccountMappingService.IsAccountTypeAllowed(role, mapping.FinancialAccountType),
                    IsRequiredForSalesPosting = RequiredSalesPostingRoles.Contains(role),
                    IsRequiredForPayablesPosting = RequiredPayablesPostingRoles.Contains(role),
                    CompatibleAccountOptions = compatibleAccounts
                };
            })
            .OrderByDescending(x => x.IsRequiredForSalesPosting || x.IsRequiredForPayablesPosting)
            .ThenBy(x => x.Role)
            .ToList();
        return dto;
    }

    private static string BuildAllowedAccountTypesLabel(FinancePostingAccountRole role)
        => role switch
        {
            FinancePostingAccountRole.Receivables => AccountType.Asset.ToString(),
            FinancePostingAccountRole.SalesRevenue => AccountType.Revenue.ToString(),
            FinancePostingAccountRole.TaxPayable => AccountType.Liability.ToString(),
            FinancePostingAccountRole.CashClearing => AccountType.Asset.ToString(),
            FinancePostingAccountRole.RefundClearing => $"{AccountType.Asset}, {AccountType.Liability}",
            FinancePostingAccountRole.Rounding => $"{AccountType.Expense}, {AccountType.Revenue}",
            FinancePostingAccountRole.AccountsPayable => AccountType.Liability.ToString(),
            FinancePostingAccountRole.PurchaseExpense => AccountType.Expense.ToString(),
            FinancePostingAccountRole.InventoryClearing => AccountType.Asset.ToString(),
            FinancePostingAccountRole.TaxReceivable => AccountType.Asset.ToString(),
            FinancePostingAccountRole.SupplierAdvance => AccountType.Asset.ToString(),
            FinancePostingAccountRole.PayrollExpense => AccountType.Expense.ToString(),
            FinancePostingAccountRole.EmployerPayrollTaxExpense => AccountType.Expense.ToString(),
            FinancePostingAccountRole.PayrollPayable => AccountType.Liability.ToString(),
            FinancePostingAccountRole.PayrollTaxPayable => AccountType.Liability.ToString(),
            FinancePostingAccountRole.SocialInsurancePayable => AccountType.Liability.ToString(),
            _ => string.Empty
        };
}

public sealed class UpsertFinanceAccountMappingHandler
{
    private readonly FinanceAccountMappingService _mappingService;

    public UpsertFinanceAccountMappingHandler(FinanceAccountMappingService mappingService)
    {
        _mappingService = mappingService ?? throw new ArgumentNullException(nameof(mappingService));
    }

    public async Task<Guid> HandleAsync(FinanceAccountMappingUpsertDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var result = await _mappingService.UpsertMappingAsync(new UpsertFinanceAccountMappingCommand(
                dto.BusinessId,
                dto.Role,
                dto.FinancialAccountId,
                dto.IsActive,
                dto.Description,
                MetadataJson: "{}"),
            ct).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.Error);
        }

        return result.Value;
    }
}

public sealed class GetFinanceExportsPageHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly FinanceExportPackageStorageService _storage;
    private readonly IReadOnlyList<IFinanceExportConnectorAdapter> _connectorAdapters;

    public GetFinanceExportsPageHandler(
        IAppDbContext db,
        IClock clock,
        FinanceExportPackageStorageService storage,
        IEnumerable<IFinanceExportConnectorAdapter> connectorAdapters)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _connectorAdapters = connectorAdapters?.ToList() ?? throw new ArgumentNullException(nameof(connectorAdapters));
    }

    public async Task<FinanceExportsPageDto> HandleAsync(
        Guid? businessId = null,
        Guid? externalSystemId = null,
        DateTime? periodStartUtc = null,
        DateTime? periodEndUtc = null,
        FinanceExportPostingStatusMode postingStatusMode = FinanceExportPostingStatusMode.PostedAndReversed,
        CancellationToken ct = default)
    {
        var context = await FinanceReportingQuerySupport.ResolveBusinessContextAsync(_db, businessId, ct).ConfigureAwait(false);
        var now = _clock.UtcNow;
        var defaultStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var start = EnsureUtc(periodStartUtc ?? defaultStart);
        var end = EnsureUtc(periodEndUtc ?? defaultStart.AddMonths(1));
        if (start >= end)
        {
            end = start.AddMonths(1);
        }

        var targets = await _db.Set<ExternalSystem>()
            .AsNoTracking()
            .Where(x => x.Kind == ExternalSystemKind.Accounting && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new FinanceExportTargetOptionDto
            {
                Id = x.Id,
                Label = x.Name
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var selectedTarget = externalSystemId.HasValue && externalSystemId.Value != Guid.Empty
            ? externalSystemId.Value
            : targets.FirstOrDefault()?.Id;

        var dto = new FinanceExportsPageDto
        {
            BusinessId = context.BusinessId,
            BusinessName = context.BusinessName,
            BusinessOptions = context.BusinessOptions,
            ExternalSystemId = selectedTarget == Guid.Empty ? null : selectedTarget,
            ExternalSystemOptions = targets,
            PeriodStartUtc = start,
            PeriodEndUtc = end,
            PostingStatusMode = postingStatusMode,
            StorageReady = _storage.IsStorageReady(),
            StorageReadinessMessage = _storage.IsStorageReady()
                ? string.Empty
                : "FinanceExportStorageNotConfigured",
            ConnectorAdapterReady = false,
            ConnectorReadinessMessage = "FinanceExportConnectorNotConfigured"
        };

        var selectedTargetEntity = selectedTarget.HasValue
            ? await _db.Set<ExternalSystem>()
                .AsNoTracking()
                .Where(x => x.Id == selectedTarget.Value && x.Kind == ExternalSystemKind.Accounting && x.IsActive && !x.IsDeleted)
                .Select(x => new FinanceExportConnectorTarget(x.Id, x.Code, x.Name, x.Kind, x.BaseUrl, x.MetadataJson))
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false)
            : null;
        if (selectedTargetEntity is not null)
        {
            dto.ConnectorAdapterReady = _connectorAdapters.Any(x => x.CanDeliver(selectedTargetEntity));
            dto.ConnectorReadinessMessage = dto.ConnectorAdapterReady
                ? string.Empty
                : "FinanceExportConnectorNotConfigured";
        }

        if (!context.BusinessId.HasValue)
        {
            return dto;
        }

        var documentBatchIds = await _db.Set<DocumentRecord>()
            .AsNoTracking()
            .Where(x =>
                x.EntityType == FinanceExportPackageStorageService.EntityType &&
                x.DocumentKind == DocumentRecordKind.Evidence &&
                x.ContentType == FinanceExportPackageBuilderService.PackageContentType &&
                !x.IsDeleted)
            .Select(x => x.EntityId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var documentBatchIdSet = documentBatchIds.ToHashSet();

        var batches = await _db.Set<FinanceExportBatch>()
            .AsNoTracking()
            .Include(x => x.Attempts)
            .Where(x => x.BusinessId == context.BusinessId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.PeriodStartUtc)
            .Take(50)
            .Select(x => new
            {
                Batch = x,
                ExternalSystemName = _db.Set<ExternalSystem>()
                    .Where(system => system.Id == x.ExternalSystemId)
                    .Select(system => system.Name)
                    .FirstOrDefault() ?? string.Empty
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var batchIds = batches.Select(x => x.Batch.Id).ToList();
        var deliveryReferences = await _db.Set<ExternalReference>()
            .AsNoTracking()
            .Where(x =>
                x.EntityType == FinanceExportConnectorDeliveryService.EntityType &&
                batchIds.Contains(x.EntityId) &&
                x.ReferenceKind == ExternalReferenceKind.Export &&
                x.IsActive &&
                !x.IsDeleted)
            .Select(x => new
            {
                x.EntityId,
                x.ExternalId,
                x.ExternalDisplayId
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var deliveryReferenceByBatch = deliveryReferences
            .GroupBy(x => x.EntityId)
            .ToDictionary(
                x => x.Key,
                x => x.Select(row => string.IsNullOrWhiteSpace(row.ExternalDisplayId) ? row.ExternalId : row.ExternalDisplayId)
                    .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty);

        dto.DraftBatchCount = batches.Count(x => x.Batch.Status == FinanceExportBatchStatus.Draft);
        dto.FailedBatchCount = batches.Count(x => x.Batch.Status == FinanceExportBatchStatus.Failed);
        dto.GeneratedBatchCount = batches.Count(x => x.Batch.Status == FinanceExportBatchStatus.Generated);
        dto.DeliveredBatchCount = batches.Count(x => x.Batch.Status == FinanceExportBatchStatus.Delivered);
        dto.Items = batches.Select(row =>
        {
            var attempts = row.Batch.Attempts.Where(x => !x.IsDeleted).OrderByDescending(x => x.AttemptNumber).ToList();
            var lastAttempt = attempts.FirstOrDefault();
            return new FinanceExportBatchListItemDto
            {
                Id = row.Batch.Id,
                BusinessId = row.Batch.BusinessId,
                ExternalSystemId = row.Batch.ExternalSystemId,
                ExternalSystemName = row.ExternalSystemName,
                ExportKey = row.Batch.ExportKey,
                PeriodStartUtc = row.Batch.PeriodStartUtc,
                PeriodEndUtc = row.Batch.PeriodEndUtc,
                PostingStatusMode = row.Batch.PostingStatusMode,
                Status = row.Batch.Status,
                GeneratedAtUtc = row.Batch.GeneratedAtUtc,
                DeliveredAtUtc = row.Batch.DeliveredAtUtc,
                PackageHashSha256 = row.Batch.PackageHashSha256 ?? string.Empty,
                PackageFileName = row.Batch.PackageFileName ?? string.Empty,
                AttemptCount = attempts.Count,
                LastAttemptStatus = lastAttempt?.Status,
                LastAttemptAtUtc = lastAttempt?.CompletedAtUtc ?? lastAttempt?.FailedAtUtc ?? lastAttempt?.StartedAtUtc,
                ErrorSummary = row.Batch.ErrorSummary ?? lastAttempt?.ErrorSummary ?? string.Empty,
                HasPackageDocument = documentBatchIdSet.Contains(row.Batch.Id),
                CanPush = row.Batch.Status == FinanceExportBatchStatus.Generated &&
                    documentBatchIdSet.Contains(row.Batch.Id) &&
                    dto.ConnectorAdapterReady,
                HasDeliveryReference = deliveryReferenceByBatch.ContainsKey(row.Batch.Id),
                DeliveryReferenceDisplay = deliveryReferenceByBatch.GetValueOrDefault(row.Batch.Id) ?? string.Empty
            };
        }).ToList();
        return dto;
    }

    private static DateTime EnsureUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}

public sealed class CreateFinanceExportBatchHandler
{
    private readonly FinanceExportBatchService _batchService;

    public CreateFinanceExportBatchHandler(FinanceExportBatchService batchService)
    {
        _batchService = batchService ?? throw new ArgumentNullException(nameof(batchService));
    }

    public async Task<FinanceExportBatchResult> HandleAsync(FinanceExportBatchCreateDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var result = await _batchService.GetOrCreateBatchAsync(
                new FinanceExportBatchCommand(
                    dto.BusinessId,
                    dto.ExternalSystemId,
                    EnsureUtc(dto.PeriodStartUtc),
                    EnsureUtc(dto.PeriodEndUtc),
                    dto.PostingStatusMode,
                    "{}"),
                ct)
            .ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.Error);
        }

        return result.Value!;
    }

    private static DateTime EnsureUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}

public sealed class GenerateFinanceExportPackageHandler
{
    private readonly FinanceExportPackageStorageService _storage;

    public GenerateFinanceExportPackageHandler(FinanceExportPackageStorageService storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public async Task<FinanceExportStoredPackageResult> HandleAsync(Guid batchId, CancellationToken ct = default)
    {
        var result = await _storage.GenerateAndStoreAsync(batchId, ct).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.Error);
        }

        return result.Value!;
    }
}

public sealed class DownloadFinanceExportPackageHandler
{
    private readonly FinanceExportPackageStorageService _storage;

    public DownloadFinanceExportPackageHandler(FinanceExportPackageStorageService storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public async Task<FinanceExportPackageDownloadResult> HandleAsync(Guid batchId, CancellationToken ct = default)
    {
        var result = await _storage.GetStoredPackageAsync(batchId, ct).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.Error);
        }

        return result.Value!;
    }
}

public sealed class PushFinanceExportPackageHandler
{
    private readonly FinanceExportConnectorDeliveryService _delivery;

    public PushFinanceExportPackageHandler(FinanceExportConnectorDeliveryService delivery)
    {
        _delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
    }

    public async Task<FinanceExportConnectorDeliveryResult> HandleAsync(Guid batchId, CancellationToken ct = default)
    {
        var result = await _delivery.DeliverAsync(new FinanceExportConnectorDeliveryCommand(batchId), ct).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.Error);
        }

        return result.Value!;
    }
}

internal static class FinanceReportingQuerySupport
{
    public static async Task<FinanceBusinessContext> ResolveBusinessContextAsync(
        IAppDbContext db,
        Guid? requestedBusinessId,
        CancellationToken ct)
    {
        var options = await db.Set<Business>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new FinanceBusinessOptionDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var selectedBusinessId = requestedBusinessId.HasValue && requestedBusinessId.Value != Guid.Empty
            ? requestedBusinessId.Value
            : options.FirstOrDefault()?.Id;
        var selected = selectedBusinessId.HasValue
            ? options.FirstOrDefault(x => x.Id == selectedBusinessId.Value)
            : null;
        if (selected is null)
        {
            return new FinanceBusinessContext(null, string.Empty, "EUR", options);
        }

        var currency = await db.Set<Business>()
            .AsNoTracking()
            .Where(x => x.Id == selected.Id && !x.IsDeleted)
            .Select(x => x.DefaultCurrency)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return new FinanceBusinessContext(selected.Id, selected.Name, string.IsNullOrWhiteSpace(currency) ? "EUR" : currency, options);
    }

    public static IQueryable<FinancePostingListItemDto> MapPostings(IQueryable<JournalEntry> entries)
        => entries.Select(x => new FinancePostingListItemDto
        {
            Id = x.Id,
            BusinessId = x.BusinessId,
            EntryDateUtc = x.EntryDateUtc,
            Description = x.Description,
            PostingStatus = x.PostingStatus,
            PostingKind = x.PostingKind,
            PostingKey = x.PostingKey ?? string.Empty,
            SourceEntityType = x.SourceEntityType ?? string.Empty,
            SourceEntityId = x.SourceEntityId,
            SourceDocumentNumber = x.SourceDocumentNumber ?? string.Empty,
            DebitMinor = x.Lines.Where(line => !line.IsDeleted).Sum(line => (long?)line.DebitMinor) ?? 0,
            CreditMinor = x.Lines.Where(line => !line.IsDeleted).Sum(line => (long?)line.CreditMinor) ?? 0,
            LineCount = x.Lines.Count(line => !line.IsDeleted),
            PostedAtUtc = x.PostedAtUtc,
            ReversedAtUtc = x.ReversedAtUtc
        });

    public static FinanceReceivableSourceDto MapReceivable(ReceivableSourceBalanceDto source)
        => new()
        {
            SourceEntityType = source.SourceEntityType ?? string.Empty,
            SourceEntityId = source.SourceEntityId,
            SourceDocumentNumber = source.SourceDocumentNumber ?? string.Empty,
            DebitMinor = source.DebitMinor,
            CreditMinor = source.CreditMinor,
            OpenBalanceMinor = source.OpenBalanceMinor,
            LastEntryDateUtc = source.LastEntryDateUtc,
            LastPostingKind = source.LastPostingKind,
            LastPostingStatus = source.LastPostingStatus,
            LastPostingKey = source.LastPostingKey ?? string.Empty
        };
}

internal sealed record FinanceBusinessContext(
    Guid? BusinessId,
    string BusinessName,
    string Currency,
    List<FinanceBusinessOptionDto> BusinessOptions);
