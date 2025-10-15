using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Queries
{
    /// <summary>
    /// Loads user details for editing by an administrator.
    /// Only non-sensitive fields are projected; this does not include email or roles.
    /// </summary>
    public sealed class GetUserForEditHandler
    {
        private readonly IAppDbContext _db;

        /// <summary>
        /// Creates a new instance of the handler.
        /// </summary>
        public GetUserForEditHandler(IAppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Retrieves a user for editing. Returns a <see cref="UserEditDto"/> populated with
        /// the current values and concurrency token.
        /// </summary>
        /// <param name="id">The identifier of the user to load.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A result containing the DTO or a failure message if the user cannot be found.</returns>
        public async Task<Result<UserEditDto>> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var user = await _db.Set<User>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);

            if (user is null)
            {
                return Result<UserEditDto>.Fail("User not found.");
            }

            var dto = new UserEditDto
            {
                Id = user.Id,
                RowVersion = user.RowVersion,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Locale = user.Locale,
                Timezone = user.Timezone,
                Currency = user.Currency,
                PhoneE164 = user.PhoneE164,
                IsActive = user.IsActive
            };

            return Result<UserEditDto>.Ok(dto);
        }
    }
}
