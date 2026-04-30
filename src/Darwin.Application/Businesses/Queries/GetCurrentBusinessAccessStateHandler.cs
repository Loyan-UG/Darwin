using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries;

/// <summary>
/// Returns the current operational access state for a business-facing client session.
/// </summary>
public sealed class GetCurrentBusinessAccessStateHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCurrentBusinessAccessStateHandler"/> class.
    /// </summary>
    public GetCurrentBusinessAccessStateHandler(IAppDbContext db, IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>
    /// Loads the access-state snapshot for the specified business identifier.
    /// </summary>
    public Task<BusinessAccessStateDto?> HandleAsync(Guid businessId, Guid userId, CancellationToken ct = default)
    {
        var nowUtc = _clock.UtcNow;
        return _db.Set<Business>()
            .AsNoTracking()
            .Where(x => x.Id == businessId && !x.IsDeleted)
            .Select(x => new BusinessAccessStateDto
            {
                UserId = userId,
                BusinessId = x.Id,
                BusinessName = x.Name,
                OperationalStatus = x.OperationalStatus,
                IsActive = x.IsActive,
                ApprovedAtUtc = x.ApprovedAtUtc,
                SuspendedAtUtc = x.SuspendedAtUtc,
                SuspensionReason = x.SuspensionReason,
                HasActiveOwner = _db.Set<BusinessMember>()
                    .Any(m => m.BusinessId == x.Id && !m.IsDeleted && m.IsActive && m.Role == BusinessMemberRole.Owner),
                HasPrimaryLocation = _db.Set<BusinessLocation>()
                    .Any(l => l.BusinessId == x.Id && !l.IsDeleted && l.IsPrimary),
                HasContactEmail = !string.IsNullOrWhiteSpace(x.ContactEmail),
                HasLegalName = !string.IsNullOrWhiteSpace(x.LegalName),
                HasActiveMembership = _db.Set<BusinessMember>()
                    .Any(m => m.BusinessId == x.Id &&
                              m.UserId == userId &&
                              !m.IsDeleted &&
                              m.IsActive),
                IsUserActive = _db.Set<User>()
                    .Any(u => u.Id == userId &&
                              !u.IsDeleted &&
                              u.IsActive),
                IsUserEmailConfirmed = _db.Set<User>()
                    .Any(u => u.Id == userId &&
                              !u.IsDeleted &&
                              u.IsActive &&
                              u.EmailConfirmed),
                IsUserLockedOut = _db.Set<User>()
                    .Any(u => u.Id == userId &&
                              !u.IsDeleted &&
                              u.LockoutEndUtc.HasValue &&
                              u.LockoutEndUtc.Value > nowUtc)
            })
            .FirstOrDefaultAsync(ct);
    }
}
