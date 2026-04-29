using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands;

/// <summary>
/// Creates an address owned by the current authenticated user.
/// </summary>
public sealed class CreateCurrentUserAddressHandler
{
    private readonly ICurrentUserService _currentUser;
    private readonly CreateUserAddressHandler _createUserAddressHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCurrentUserAddressHandler"/> class.
    /// </summary>
    public CreateCurrentUserAddressHandler(ICurrentUserService currentUser, CreateUserAddressHandler createUserAddressHandler)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _createUserAddressHandler = createUserAddressHandler ?? throw new ArgumentNullException(nameof(createUserAddressHandler));
    }

    /// <summary>
    /// Creates a new address for the current authenticated user.
    /// </summary>
    public Task<Result<Guid>> HandleAsync(AddressCreateDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.UserId = _currentUser.GetCurrentUserId();
        return _createUserAddressHandler.HandleAsync(dto, ct);
    }
}

/// <summary>
/// Updates an address owned by the current authenticated user.
/// </summary>
public sealed class UpdateCurrentUserAddressHandler
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly UpdateUserAddressHandler _updateUserAddressHandler;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCurrentUserAddressHandler"/> class.
    /// </summary>
    public UpdateCurrentUserAddressHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        UpdateUserAddressHandler updateUserAddressHandler,
        IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _updateUserAddressHandler = updateUserAddressHandler ?? throw new ArgumentNullException(nameof(updateUserAddressHandler));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>
    /// Updates the address when it belongs to the current user.
    /// </summary>
    public async Task<Result> HandleAsync(AddressEditDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var userId = _currentUser.GetCurrentUserId();
        var isOwned = await _db.Set<Address>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == dto.Id && x.UserId == userId && !x.IsDeleted, ct)
            .ConfigureAwait(false);

        if (!isOwned)
        {
            return Result.Fail(_localizer["AddressNotOwnedByUser"]);
        }

        return await _updateUserAddressHandler.HandleAsync(dto, ct).ConfigureAwait(false);
    }
}

/// <summary>
/// Soft-deletes an address owned by the current authenticated user.
/// </summary>
public sealed class DeleteCurrentUserAddressHandler
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly SoftDeleteUserAddressHandler _softDeleteUserAddressHandler;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteCurrentUserAddressHandler"/> class.
    /// </summary>
    public DeleteCurrentUserAddressHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        SoftDeleteUserAddressHandler softDeleteUserAddressHandler,
        IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _softDeleteUserAddressHandler = softDeleteUserAddressHandler ?? throw new ArgumentNullException(nameof(softDeleteUserAddressHandler));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>
    /// Soft-deletes the address when it belongs to the current user.
    /// </summary>
    public async Task<Result> HandleAsync(AddressDeleteDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var userId = _currentUser.GetCurrentUserId();
        var isOwned = await _db.Set<Address>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == dto.Id && x.UserId == userId && !x.IsDeleted, ct)
            .ConfigureAwait(false);

        if (!isOwned)
        {
            return Result.Fail(_localizer["AddressNotOwnedByUser"]);
        }

        return await _softDeleteUserAddressHandler.HandleAsync(dto, ct).ConfigureAwait(false);
    }
}

/// <summary>
/// Sets default billing or shipping flags for an address owned by the current user.
/// </summary>
public sealed class SetCurrentUserDefaultAddressHandler
{
    private readonly ICurrentUserService _currentUser;
    private readonly SetDefaultUserAddressHandler _setDefaultUserAddressHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetCurrentUserDefaultAddressHandler"/> class.
    /// </summary>
    public SetCurrentUserDefaultAddressHandler(ICurrentUserService currentUser, SetDefaultUserAddressHandler setDefaultUserAddressHandler)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _setDefaultUserAddressHandler = setDefaultUserAddressHandler ?? throw new ArgumentNullException(nameof(setDefaultUserAddressHandler));
    }

    /// <summary>
    /// Sets the requested default flags on an address owned by the current user.
    /// </summary>
    public Task<Result> HandleAsync(Guid addressId, bool asBilling, bool asShipping, CancellationToken ct = default)
        => _setDefaultUserAddressHandler.HandleAsync(_currentUser.GetCurrentUserId(), addressId, asBilling, asShipping, rowVersion: null, ct);
}
