using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Support;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Businesses.Commands;

public sealed class ProvisionBusinessOnboardingHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public ProvisionBusinessOnboardingHandler(
        IAppDbContext db,
        IClock clock,
        IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public async Task<Result<BusinessProvisionOnboardingResultDto>> HandleAsync(BusinessLifecycleActionDto dto, CancellationToken ct = default)
    {
        if (dto.Id == Guid.Empty || dto.RowVersion.Length == 0)
        {
            return Result<BusinessProvisionOnboardingResultDto>.Fail(_localizer["InvalidDeleteRequest"]);
        }

        var business = await _db.Set<Business>()
            .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
            .ConfigureAwait(false);

        if (business is null)
        {
            return Result<BusinessProvisionOnboardingResultDto>.Fail(_localizer["BusinessNotFound"]);
        }

        if (!business.RowVersion.SequenceEqual(dto.RowVersion))
        {
            return Result<BusinessProvisionOnboardingResultDto>.Fail(_localizer["ConcurrencyConflictDetected"]);
        }

        var ownerUser = await LoadOwnerUserAsync(business.Id, ct).ConfigureAwait(false);
        var missing = await ResolveMissingPrerequisitesAsync(business, ownerUser, ct).ConfigureAwait(false);
        if (missing.Count > 0)
        {
            return Result<BusinessProvisionOnboardingResultDto>.Fail(string.Format(
                _localizer["BusinessOnboardingProvisioningBlocked"],
                string.Join(", ", missing)));
        }

        var customerResult = await EnsureCustomerProfileAsync(business, ownerUser, ct).ConfigureAwait(false);
        var wasApproved = business.OperationalStatus != BusinessOperationalStatus.Approved;
        var wasActivated = !business.IsActive;

        business.OperationalStatus = BusinessOperationalStatus.Approved;
        business.ApprovedAtUtc ??= _clock.UtcNow;
        business.SuspendedAtUtc = null;
        business.SuspensionReason = null;
        business.IsActive = true;

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result<BusinessProvisionOnboardingResultDto>.Ok(new BusinessProvisionOnboardingResultDto
        {
            BusinessId = business.Id,
            CustomerId = customerResult.CustomerId,
            CustomerCreated = customerResult.CustomerCreated,
            CustomerUpdated = customerResult.CustomerUpdated,
            WasApproved = wasApproved,
            WasActivated = wasActivated
        });
    }

    private async Task<List<string>> ResolveMissingPrerequisitesAsync(Business business, User? ownerUser, CancellationToken ct)
    {
        var missing = new List<string>();

        var activeOwnerExists = ownerUser is not null;
        if (!activeOwnerExists)
        {
            missing.Add(_localizer["BusinessOnboardingMissingOwner"]);
        }

        var primaryLocationExists = await _db.Set<BusinessLocation>()
            .AsNoTracking()
            .AnyAsync(x => x.BusinessId == business.Id && !x.IsDeleted && x.IsPrimary, ct)
            .ConfigureAwait(false);
        if (!primaryLocationExists)
        {
            missing.Add(_localizer["BusinessOnboardingMissingPrimaryLocation"]);
        }

        if (string.IsNullOrWhiteSpace(business.LegalName))
        {
            missing.Add(_localizer["BusinessOnboardingMissingLegalName"]);
        }

        var customerMissingReason = BusinessOnboardingCustomerProfileSupport.GetMissingReason(business, ownerUser);
        if (!string.IsNullOrWhiteSpace(customerMissingReason))
        {
            missing.Add(customerMissingReason);
        }

        return missing;
    }

    private async Task<CustomerProvisioningResult> EnsureCustomerProfileAsync(Business business, User? ownerUser, CancellationToken ct)
    {
        var notes = BusinessOnboardingCustomerProfileSupport.BuildNotes(business.Id);
        var customer = await _db.Set<Customer>()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Notes == notes, ct)
            .ConfigureAwait(false);

        if (customer is null)
        {
            customer = new Customer();
            customer.Id = Guid.NewGuid();
            BusinessOnboardingCustomerProfileSupport.ApplyManagedValues(customer, business, ownerUser);
            _db.Set<Customer>().Add(customer);
            return new CustomerProvisioningResult(customer.Id, CustomerCreated: true, CustomerUpdated: false);
        }

        var changed = BusinessOnboardingCustomerProfileSupport.ApplyManagedValues(customer, business, ownerUser);
        return new CustomerProvisioningResult(customer.Id, CustomerCreated: false, CustomerUpdated: changed);
    }

    private async Task<User?> LoadOwnerUserAsync(Guid businessId, CancellationToken ct)
    {
        var ownerUserId = await _db.Set<BusinessMember>()
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId &&
                        !x.IsDeleted &&
                        x.IsActive &&
                        x.Role == BusinessMemberRole.Owner)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => (Guid?)x.UserId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (!ownerUserId.HasValue)
        {
            return null;
        }

        return await _db.Set<User>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == ownerUserId.Value && !x.IsDeleted, ct)
            .ConfigureAwait(false);
    }

    private sealed record CustomerProvisioningResult(Guid CustomerId, bool CustomerCreated, bool CustomerUpdated);
}
