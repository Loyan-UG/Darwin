using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Support;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Commands
{
    public sealed class EnsureBusinessOnboardingCustomerProfileHandler
    {
        private readonly IAppDbContext _db;

        public EnsureBusinessOnboardingCustomerProfileHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<EnsureBusinessOnboardingCustomerResultDto> HandleAsync(Guid businessId, CancellationToken ct = default)
        {
            var business = await _db.Set<Business>()
                .FirstOrDefaultAsync(x => x.Id == businessId && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (business is null)
            {
                return new EnsureBusinessOnboardingCustomerResultDto
                {
                    CanProvision = false,
                    MissingReason = "Business"
                };
            }

            var ownerUser = await LoadOwnerUserAsync(businessId, ct).ConfigureAwait(false);
            var missingReason = BusinessOnboardingCustomerProfileSupport.GetMissingReason(business, ownerUser);
            if (missingReason is not null)
            {
                return new EnsureBusinessOnboardingCustomerResultDto
                {
                    CanProvision = false,
                    MissingReason = missingReason
                };
            }

            var notes = BusinessOnboardingCustomerProfileSupport.BuildNotes(businessId);
            var customer = await _db.Set<Customer>()
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.Notes == notes, ct)
                .ConfigureAwait(false);

            if (customer is null)
            {
                customer = new Customer();
                BusinessOnboardingCustomerProfileSupport.ApplyManagedValues(customer, business, ownerUser);
                _db.Set<Customer>().Add(customer);
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return new EnsureBusinessOnboardingCustomerResultDto
                {
                    CustomerId = customer.Id,
                    CanProvision = true,
                    WasCreated = true
                };
            }

            var changed = BusinessOnboardingCustomerProfileSupport.ApplyManagedValues(customer, business, ownerUser);
            if (changed)
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            return new EnsureBusinessOnboardingCustomerResultDto
            {
                CustomerId = customer.Id,
                CanProvision = true,
                WasUpdated = changed
            };
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
    }
}
