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

namespace Darwin.Application.Businesses.Queries
{
    public sealed class GetBusinessOnboardingCustomerProfileHandler
    {
        private readonly IAppDbContext _db;

        public GetBusinessOnboardingCustomerProfileHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<BusinessOnboardingCustomerProfileDto> HandleAsync(Guid businessId, CancellationToken ct = default)
        {
            var business = await _db.Set<Business>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == businessId && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (business is null)
            {
                return new BusinessOnboardingCustomerProfileDto
                {
                    CanProvision = false,
                    MissingReason = "Business"
                };
            }

            var ownerUser = await LoadOwnerUserAsync(businessId, ct).ConfigureAwait(false);
            var notes = BusinessOnboardingCustomerProfileSupport.BuildNotes(businessId);
            var customerId = await _db.Set<Customer>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Notes == notes)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            var missingReason = BusinessOnboardingCustomerProfileSupport.GetMissingReason(business, ownerUser);
            return new BusinessOnboardingCustomerProfileDto
            {
                CustomerId = customerId,
                IsProvisioned = customerId.HasValue,
                CanProvision = missingReason is null,
                CandidateEmail = BusinessOnboardingCustomerProfileSupport.ExtractProvisioningEmail(business, ownerUser),
                CompanyName = BusinessOnboardingCustomerProfileSupport.ExtractCompanyName(business),
                MissingReason = missingReason
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
