using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands;

/// <summary>
/// Updates the current authenticated user's profile image without requiring a profile RowVersion round trip.
/// </summary>
public sealed class UpdateCurrentUserProfileImageHandler
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public UpdateCurrentUserProfileImageHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public async Task<Result> HandleAsync(string? profileImageUrl, CancellationToken ct = default)
    {
        var userId = _currentUser.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Result.Fail(_localizer["UserNotAuthenticated"]);
        }

        var user = await _db.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted && u.IsActive, ct)
            .ConfigureAwait(false);

        if (user is null)
        {
            return Result.Fail(_localizer["UserNotFound"]);
        }

        user.ProfileImageUrl = string.IsNullOrWhiteSpace(profileImageUrl) ? null : profileImageUrl.Trim();
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Ok();
    }
}
