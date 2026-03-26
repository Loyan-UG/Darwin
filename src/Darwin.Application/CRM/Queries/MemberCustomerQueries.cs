using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CRM.DTOs;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CRM.Queries;

/// <summary>
/// Returns the CRM customer record linked to the current authenticated member identity.
/// </summary>
public sealed class GetCurrentMemberCustomerProfileHandler
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCurrentMemberCustomerProfileHandler"/> class.
    /// </summary>
    public GetCurrentMemberCustomerProfileHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>
    /// Loads the CRM customer profile linked to the current user, when one exists.
    /// </summary>
    public Task<MemberCustomerProfileDto?> HandleAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.GetCurrentUserId();

        return (
            from customer in _db.Set<Customer>().AsNoTracking()
            join user in _db.Set<User>().AsNoTracking() on customer.UserId equals (Guid?)user.Id
            where customer.UserId == userId
            select new MemberCustomerProfileDto
            {
                Id = customer.Id,
                UserId = user.Id,
                DisplayName = ((user.FirstName ?? string.Empty) + " " + (user.LastName ?? string.Empty)).Trim(),
                Email = user.Email,
                Phone = user.PhoneE164,
                CompanyName = customer.CompanyName,
                CreatedAtUtc = customer.CreatedAtUtc
            })
            .FirstOrDefaultAsync(ct);
    }
}
