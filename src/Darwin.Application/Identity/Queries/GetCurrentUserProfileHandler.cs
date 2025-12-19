using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Queries
{
    /// <summary>
    /// Returns the current authenticated user's profile information in a shape suitable for edit screens.
    /// This handler is primarily used by mobile clients (Consumer/Business) and WebApi.
    /// </summary>
    public sealed class GetCurrentUserProfileHandler
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="db">Application DbContext abstraction.</param>
        /// <param name="currentUser">Provides access to the current authenticated user context.</param>
        /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
        public GetCurrentUserProfileHandler(IAppDbContext db, ICurrentUserService currentUser)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        }

        /// <summary>
        /// Loads the current user's profile from persistence.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="Result{T}"/> that contains a <see cref="UserProfileEditDto"/> on success.
        /// Returns a failed result when the user is not authenticated or cannot be found.
        /// </returns>
        public async Task<Result<UserProfileEditDto>> HandleAsync(CancellationToken ct = default)
        {
            // IMPORTANT: We deliberately do not accept userId input here.
            // The only supported target is the authenticated user from the current request context.
            var userId = _currentUser.GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Result<UserProfileEditDto>.Fail("User is not authenticated.");
            }

            // Read-only query: project directly to DTO to avoid materializing the aggregate.
            var dto = await _db.Set<User>()
                .AsNoTracking()
                .Where(u => u.Id == userId && !u.IsDeleted)
                .Select(u => new UserProfileEditDto
                {
                    Id = u.Id,
                    RowVersion = u.RowVersion,

                    // Names and contact fields
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    PhoneE164 = u.PhoneE164,

                    // Preferences (kept simple; validation and normalization happens in UpdateCurrentUserHandler)
                    Locale = u.Locale,
                    Timezone = u.Timezone,
                    Currency = u.Currency
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (dto is null)
            {
                return Result<UserProfileEditDto>.Fail("User not found.");
            }

            // Defensive: ensure RowVersion is never null for callers that require optimistic concurrency.
            dto.RowVersion ??= Array.Empty<byte>();

            return Result<UserProfileEditDto>.Ok(dto);
        }
    }
}
