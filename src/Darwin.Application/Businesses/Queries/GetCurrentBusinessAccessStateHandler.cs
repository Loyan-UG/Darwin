using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries;

/// <summary>
/// Returns the current operational access state for a business-facing client session.
/// </summary>
public sealed class GetCurrentBusinessAccessStateHandler
{
    private readonly IAppDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCurrentBusinessAccessStateHandler"/> class.
    /// </summary>
    public GetCurrentBusinessAccessStateHandler(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// Loads the access-state snapshot for the specified business identifier.
    /// </summary>
    public Task<BusinessAccessStateDto?> HandleAsync(Guid businessId, CancellationToken ct = default)
    {
        return _db.Set<Business>()
            .AsNoTracking()
            .Where(x => x.Id == businessId)
            .Select(x => new BusinessAccessStateDto
            {
                BusinessId = x.Id,
                BusinessName = x.Name,
                OperationalStatus = x.OperationalStatus,
                IsActive = x.IsActive,
                ApprovedAtUtc = x.ApprovedAtUtc,
                SuspendedAtUtc = x.SuspendedAtUtc,
                SuspensionReason = x.SuspensionReason,
                HasActiveOwner = _db.Set<BusinessMember>()
                    .Any(m => m.BusinessId == x.Id && m.IsActive && m.Role == BusinessMemberRole.Owner),
                HasPrimaryLocation = _db.Set<BusinessLocation>()
                    .Any(l => l.BusinessId == x.Id && !l.IsDeleted && l.IsPrimary),
                HasContactEmail = !string.IsNullOrWhiteSpace(x.ContactEmail),
                HasLegalName = !string.IsNullOrWhiteSpace(x.LegalName)
            })
            .FirstOrDefaultAsync(ct);
    }
}
