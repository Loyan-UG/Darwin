using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Foundation;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Services;

/// <summary>
/// Resolves business-specific finance posting account roles to concrete financial accounts.
/// </summary>
public sealed class FinanceAccountMappingService
{
    private readonly IAppDbContext _db;

    public FinanceAccountMappingService(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<Result<Guid>> UpsertMappingAsync(UpsertFinanceAccountMappingCommand command, CancellationToken ct = default)
    {
        var description = FoundationInputNormalizer.Optional(command.Description);
        var metadataJson = FoundationInputNormalizer.Json(command.MetadataJson);
        var validation = ValidateCommand(command, description, metadataJson);
        if (!validation.Succeeded)
        {
            return Result<Guid>.Fail(validation.Error!);
        }

        var account = await _db.Set<FinancialAccount>()
            .FirstOrDefaultAsync(x => x.Id == command.FinancialAccountId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (account is null)
        {
            return Result<Guid>.Fail("Financial account was not found.");
        }

        if (account.BusinessId != command.BusinessId)
        {
            return Result<Guid>.Fail("Financial account must belong to the mapping business.");
        }

        if (!IsAccountTypeAllowed(command.Role, account.Type))
        {
            return Result<Guid>.Fail("Financial account type is not compatible with the posting role.");
        }

        var mapping = await _db.Set<FinancePostingAccountMapping>()
            .FirstOrDefaultAsync(x =>
                x.BusinessId == command.BusinessId &&
                x.Role == command.Role &&
                !x.IsDeleted,
                ct)
            .ConfigureAwait(false);

        if (mapping is null)
        {
            mapping = new FinancePostingAccountMapping
            {
                BusinessId = command.BusinessId,
                Role = command.Role
            };
            _db.Set<FinancePostingAccountMapping>().Add(mapping);
        }

        mapping.FinancialAccountId = command.FinancialAccountId;
        mapping.IsActive = command.IsActive;
        mapping.Description = description;
        mapping.MetadataJson = metadataJson;

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Guid>.Ok(mapping.Id);
    }

    public async Task<IReadOnlyList<FinanceAccountMappingDto>> GetMappingsForBusinessAsync(Guid businessId, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty)
        {
            return Array.Empty<FinanceAccountMappingDto>();
        }

        return await _db.Set<FinancePostingAccountMapping>()
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId && !x.IsDeleted)
            .Join(
                _db.Set<FinancialAccount>().AsNoTracking().Where(x => !x.IsDeleted),
                mapping => mapping.FinancialAccountId,
                account => account.Id,
                (mapping, account) => new FinanceAccountMappingDto(
                    mapping.Id,
                    mapping.BusinessId,
                    mapping.Role,
                    mapping.FinancialAccountId,
                    account.Name,
                    account.Code,
                    account.Type,
                    mapping.IsActive,
                    mapping.Description,
                    mapping.MetadataJson))
            .OrderBy(x => x.Role)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<Result<IReadOnlyDictionary<FinancePostingAccountRole, Guid>>> ResolveRequiredAccountsAsync(
        Guid businessId,
        IReadOnlyCollection<FinancePostingAccountRole>? roles,
        CancellationToken ct = default)
    {
        if (businessId == Guid.Empty)
        {
            return Result<IReadOnlyDictionary<FinancePostingAccountRole, Guid>>.Fail("Business id is required.");
        }

        var requiredRoles = (roles ?? Array.Empty<FinancePostingAccountRole>())
            .Distinct()
            .ToArray();
        if (requiredRoles.Length == 0)
        {
            return Result<IReadOnlyDictionary<FinancePostingAccountRole, Guid>>.Fail("At least one posting account role is required.");
        }

        if (requiredRoles.Any(role => !Enum.IsDefined(typeof(FinancePostingAccountRole), role)))
        {
            return Result<IReadOnlyDictionary<FinancePostingAccountRole, Guid>>.Fail("Posting account role is invalid.");
        }

        var mappings = await _db.Set<FinancePostingAccountMapping>()
            .AsNoTracking()
            .Where(x =>
                x.BusinessId == businessId &&
                x.IsActive &&
                !x.IsDeleted &&
                requiredRoles.Contains(x.Role))
            .Join(
                _db.Set<FinancialAccount>().AsNoTracking().Where(x => !x.IsDeleted),
                mapping => mapping.FinancialAccountId,
                account => account.Id,
                (mapping, account) => new { mapping.Role, mapping.FinancialAccountId, account.BusinessId, account.Type })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var invalidAccount = mappings.FirstOrDefault(x =>
            x.BusinessId != businessId ||
            !IsAccountTypeAllowed(x.Role, x.Type));
        if (invalidAccount is not null)
        {
            return Result<IReadOnlyDictionary<FinancePostingAccountRole, Guid>>.Fail("Posting account mapping is not valid for the business.");
        }

        var resolved = mappings
            .GroupBy(x => x.Role)
            .ToDictionary(x => x.Key, x => x.First().FinancialAccountId);
        var missing = requiredRoles.Where(role => !resolved.ContainsKey(role)).ToArray();
        if (missing.Length > 0)
        {
            return Result<IReadOnlyDictionary<FinancePostingAccountRole, Guid>>.Fail(
                $"Missing finance account mappings: {string.Join(", ", missing.Select(x => x.ToString()))}.");
        }

        return Result<IReadOnlyDictionary<FinancePostingAccountRole, Guid>>.Ok(resolved);
    }

    public static bool IsAccountTypeAllowed(FinancePostingAccountRole role, AccountType accountType)
        => role switch
        {
            FinancePostingAccountRole.Receivables => accountType == AccountType.Asset,
            FinancePostingAccountRole.SalesRevenue => accountType == AccountType.Revenue,
            FinancePostingAccountRole.TaxPayable => accountType == AccountType.Liability,
            FinancePostingAccountRole.CashClearing => accountType == AccountType.Asset,
            FinancePostingAccountRole.RefundClearing => accountType is AccountType.Asset or AccountType.Liability,
            FinancePostingAccountRole.Rounding => accountType is AccountType.Expense or AccountType.Revenue,
            FinancePostingAccountRole.AccountsPayable => accountType == AccountType.Liability,
            FinancePostingAccountRole.PurchaseExpense => accountType == AccountType.Expense,
            FinancePostingAccountRole.InventoryClearing => accountType == AccountType.Asset,
            FinancePostingAccountRole.TaxReceivable => accountType == AccountType.Asset,
            _ => false
        };

    private static Result ValidateCommand(UpsertFinanceAccountMappingCommand command, string? description, string metadataJson)
    {
        if (command.BusinessId == Guid.Empty)
        {
            return Result.Fail("Business id is required.");
        }

        if (!Enum.IsDefined(typeof(FinancePostingAccountRole), command.Role))
        {
            return Result.Fail("Posting account role is required.");
        }

        if (command.FinancialAccountId == Guid.Empty)
        {
            return Result.Fail("Financial account id is required.");
        }

        if (FoundationInputNormalizer.LooksSensitive(description) ||
            FoundationInputNormalizer.LooksSensitive(metadataJson))
        {
            return Result.Fail("Sensitive secrets must not be stored in finance account mapping metadata.");
        }

        return Result.Ok();
    }
}

public sealed record UpsertFinanceAccountMappingCommand(
    Guid BusinessId,
    FinancePostingAccountRole Role,
    Guid FinancialAccountId,
    bool IsActive = true,
    string? Description = null,
    string? MetadataJson = null);

public sealed record FinanceAccountMappingDto(
    Guid Id,
    Guid BusinessId,
    FinancePostingAccountRole Role,
    Guid FinancialAccountId,
    string FinancialAccountName,
    string? FinancialAccountCode,
    AccountType FinancialAccountType,
    bool IsActive,
    string? Description,
    string MetadataJson);
