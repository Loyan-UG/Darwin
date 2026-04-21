using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Identity.DTOs;
using Darwin.Shared.Results;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Queries;

/// <summary>
/// Returns the reusable address book for the current authenticated user.
/// </summary>
public sealed class GetCurrentUserAddressesHandler
{
    private readonly ICurrentUserService _currentUser;
    private readonly GetUserWithAddressesForEditHandler _getUserWithAddressesForEditHandler;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCurrentUserAddressesHandler"/> class.
    /// </summary>
    public GetCurrentUserAddressesHandler(
        ICurrentUserService currentUser,
        GetUserWithAddressesForEditHandler getUserWithAddressesForEditHandler,
        IStringLocalizer<ValidationResource> localizer)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _getUserWithAddressesForEditHandler = getUserWithAddressesForEditHandler ?? throw new ArgumentNullException(nameof(getUserWithAddressesForEditHandler));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
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
            return Result<IReadOnlyList<AddressListItemDto>>.Fail(result.Error ?? _localizer["UserNotFound"]);
        }

        return Result<IReadOnlyList<AddressListItemDto>>.Ok(result.Value.Addresses);
    }
}
