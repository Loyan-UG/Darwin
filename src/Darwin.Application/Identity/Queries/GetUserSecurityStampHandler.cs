using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Queries
{
    /// <summary>
    /// Returns the current SecurityStamp of the specified user.
    /// Used by Web after a successful 2FA verification to issue the auth cookie.
    /// </summary>
    public sealed class GetUserSecurityStampHandler
    {
        private readonly IAppDbContext _db;
        public GetUserSecurityStampHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Queries the user table and returns the latest SecurityStamp for the given user id.
        /// </summary>
        /// <param name="userId">Target user id.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Result with stamp on success, or failure if user not found.</returns>
        public async Task<Result<string>> HandleAsync(Guid userId, CancellationToken ct = default)
        {
            var stamp = await _db.Set<Darwin.Domain.Entities.Identity.User>()
                .AsNoTracking()
                .Where(u => u.Id == userId && !u.IsDeleted)
                .Select(u => u.SecurityStamp)
                .FirstOrDefaultAsync(ct);

            return string.IsNullOrWhiteSpace(stamp)
                ? Result<string>.Fail("User not found or stamp missing.")
                : Result<string>.Ok(stamp);
        }
    }
}
