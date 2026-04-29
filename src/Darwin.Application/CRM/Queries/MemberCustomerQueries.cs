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

/// <summary>
/// Returns a richer CRM customer context for the current authenticated member identity.
/// </summary>
public sealed class GetCurrentMemberCustomerContextHandler
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCurrentMemberCustomerContextHandler"/> class.
    /// </summary>
    public GetCurrentMemberCustomerContextHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>
    /// Loads member-facing CRM context including segments, consent history, and recent interactions.
    /// </summary>
    public async Task<MemberCustomerContextDto?> HandleAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return null;
        }

        var customer = await (
            from candidate in _db.Set<Customer>().AsNoTracking()
            join user in _db.Set<User>().AsNoTracking() on candidate.UserId equals (Guid?)user.Id
            where candidate.UserId == userId
            select new
            {
                Customer = candidate,
                User = user
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (customer is null)
        {
            return null;
        }

        var segments = await _db.Set<CustomerSegmentMembership>()
            .AsNoTracking()
            .Where(x => x.CustomerId == customer.Customer.Id)
            .Join(
                _db.Set<CustomerSegment>().AsNoTracking(),
                membership => membership.CustomerSegmentId,
                segment => segment.Id,
                (membership, segment) => new MemberCustomerSegmentDto
                {
                    SegmentId = segment.Id,
                    Name = segment.Name,
                    Description = segment.Description
                })
            .OrderBy(x => x.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var consentRows = await _db.Set<Consent>()
            .AsNoTracking()
            .Where(x => x.CustomerId == customer.Customer.Id)
            .OrderByDescending(x => x.GrantedAtUtc)
            .Take(20)
            .Select(x => new
            {
                Id = x.Id,
                x.Type,
                Granted = x.Granted,
                GrantedAtUtc = x.GrantedAtUtc,
                RevokedAtUtc = x.RevokedAtUtc
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var consents = consentRows
            .Select(x => new MemberCustomerConsentDto
            {
                Id = x.Id,
                Type = x.Type.ToString(),
                Granted = x.Granted,
                GrantedAtUtc = x.GrantedAtUtc,
                RevokedAtUtc = x.RevokedAtUtc
            })
            .ToList();

        var interactionRows = await _db.Set<Interaction>()
            .AsNoTracking()
            .Where(x => x.CustomerId == customer.Customer.Id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(20)
            .Select(x => new
            {
                Id = x.Id,
                x.Type,
                x.Channel,
                Subject = x.Subject,
                ContentPreview = x.Content == null
                    ? null
                    : (x.Content.Length <= 160 ? x.Content : x.Content.Substring(0, 160)),
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var interactions = interactionRows
            .Select(x => new MemberCustomerInteractionDto
            {
                Id = x.Id,
                Type = x.Type.ToString(),
                Channel = x.Channel.ToString(),
                Subject = x.Subject,
                ContentPreview = x.ContentPreview,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToList();

        var displayName = ((customer.User.FirstName ?? string.Empty) + " " + (customer.User.LastName ?? string.Empty)).Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = ((customer.Customer.FirstName ?? string.Empty) + " " + (customer.Customer.LastName ?? string.Empty)).Trim();
        }

        return new MemberCustomerContextDto
        {
            Id = customer.Customer.Id,
            UserId = customer.User.Id,
            DisplayName = displayName,
            Email = customer.User.Email,
            Phone = customer.User.PhoneE164 ?? customer.Customer.Phone,
            CompanyName = customer.Customer.CompanyName,
            Notes = customer.Customer.Notes,
            CreatedAtUtc = customer.Customer.CreatedAtUtc,
            LastInteractionAtUtc = interactions.Count == 0 ? null : interactions[0].CreatedAtUtc,
            InteractionCount = await _db.Set<Interaction>()
                .AsNoTracking()
                .CountAsync(x => x.CustomerId == customer.Customer.Id, ct)
                .ConfigureAwait(false),
            Segments = segments,
            Consents = consents,
            RecentInteractions = interactions
        };
    }
}
