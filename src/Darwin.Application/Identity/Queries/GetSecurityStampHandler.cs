using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Queries
{
    /// <summary>
    /// Minimal query to retrieve the current security stamp for a given user.
    /// This is used by the Web layer after secondary authentication steps (e.g., TOTP/WebAuthn)
    /// to issue the authentication cookie without direct DbContext access from Web.
    /// </summary>
    public sealed class GetSecurityStampHandler
    {
        private readonly IAppDbContext _db;

        /// <summary>Creates a new instance of the handler.</summary>
        public GetSecurityStampHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Loads the user's security stamp if the user exists and is active.
        /// </summary>
        /// <param name="userId">Target user id.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<Result<string>> HandleAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await _db.Set<User>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted && u.IsActive, ct);

            if (user is null || string.IsNullOrWhiteSpace(user.SecurityStamp))
                return Result<string>.Fail("User not found or inactive.");

            return Result<string>.Ok(user.SecurityStamp);
        }
    }
}
