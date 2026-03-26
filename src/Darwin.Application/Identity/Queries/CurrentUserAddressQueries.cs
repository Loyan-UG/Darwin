using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Identity.DTOs;
using Darwin.Shared.Results;

namespace Darwin.Application.Identity.Queries;

/// <summary>
/// Returns the reusable address book for the current authenticated user.
/// </summary>
public sealed class GetCurrentUserAddressesHandler
{
    private readonly ICurrentUserService _currentUser;
    private readonly GetUserWithAddressesForEditHandler _getUserWithAddressesForEditHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCurrentUserAddressesHandler"/> class.
    /// </summary>
    public GetCurrentUserAddressesHandler(
        ICurrentUserService currentUser,
        GetUserWithAddressesForEditHandler getUserWithAddressesForEditHandler)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _getUserWithAddressesForEditHandler = getUserWithAddressesForEditHandler ?? throw new ArgumentNullException(nameof(getUserWithAddressesForEditHandler));
    }

    /// <summary>
    /// Loads the current user's address book ordered for profile-management screens.
    /// </summary>
    public async Task<Result<IReadOnlyList<AddressListItemDto>>> HandleAsync(CancellationToken ct = default)
    {
        var result = await _getUserWithAddressesForEditHandler
            .HandleAsync(_currentUser.GetCurrentUserId(), ct)
            .ConfigureAwait(false);

        if (!result.Succeeded || result.Value is null)
        {
            return Result<IReadOnlyList<AddressListItemDto>>.Fail(result.Error ?? "User not found.");
        }

        return Result<IReadOnlyList<AddressListItemDto>>.Ok(result.Value.Addresses);
    }
}
