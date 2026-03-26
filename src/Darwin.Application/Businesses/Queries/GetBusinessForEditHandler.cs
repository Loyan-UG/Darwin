using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Loads a single business for editing, including RowVersion for concurrency.
    /// </summary>
    public sealed class GetBusinessForEditHandler
    {
        private readonly IAppDbContext _db;
        public GetBusinessForEditHandler(IAppDbContext db) => _db = db;

        public async Task<BusinessEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Set<Business>()
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new BusinessEditDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    Name = x.Name,
                    LegalName = x.LegalName,
                    TaxId = x.TaxId,
                    ShortDescription = x.ShortDescription,
                    WebsiteUrl = x.WebsiteUrl,
                    ContactEmail = x.ContactEmail,
                    ContactPhoneE164 = x.ContactPhoneE164,
                    Category = x.Category,
                    DefaultCurrency = x.DefaultCurrency,
                    DefaultCulture = x.DefaultCulture,
                    IsActive = x.IsActive,
                    OperationalStatus = x.OperationalStatus,
                    ApprovedAtUtc = x.ApprovedAtUtc,
                    SuspendedAtUtc = x.SuspendedAtUtc,
                    SuspensionReason = x.SuspensionReason,
                    MemberCount = _db.Set<BusinessMember>().Count(m => m.BusinessId == x.Id),
                    ActiveOwnerCount = _db.Set<BusinessMember>().Count(m => m.BusinessId == x.Id && m.IsActive && m.Role == BusinessMemberRole.Owner),
                    LocationCount = _db.Set<BusinessLocation>().Count(l => l.BusinessId == x.Id && !l.IsDeleted),
                    PrimaryLocationCount = _db.Set<BusinessLocation>().Count(l => l.BusinessId == x.Id && !l.IsDeleted && l.IsPrimary),
                    InvitationCount = _db.Set<BusinessInvitation>().Count(i => i.BusinessId == x.Id && i.Status == BusinessInvitationStatus.Pending),
                    HasContactEmailConfigured = !string.IsNullOrWhiteSpace(x.ContactEmail),
                    HasLegalNameConfigured = !string.IsNullOrWhiteSpace(x.LegalName)
                })
                .FirstOrDefaultAsync(ct);
        }
    }
}
