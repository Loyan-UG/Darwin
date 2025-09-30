using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Queries
{
    /// <summary>
    /// Returns all non-deleted external logins for a given user.
    /// </summary>
    public sealed class ListExternalLoginsHandler
    {
        private readonly IAppDbContext _db;


        /// <summary>
        /// Creates a new instance of the query handler.
        /// </summary>
        /// <param name="db">Application DbContext abstraction used to query identity tables.</param>
        public ListExternalLoginsHandler(IAppDbContext db) => _db = db;


        /// <summary>
        /// Queries the <see cref="UserLogin"/> table and returns a simple list of (Provider, ProviderKey, DisplayName).
        /// </summary>
        /// <param name="userId">The user identifier to list external logins for.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Read-only list of tuples describing each external login.</returns>
        public async Task<IReadOnlyList<(
            string Provider, string ProviderKey, string? DisplayName)>>
            HandleAsync(System.Guid userId, CancellationToken ct = default)
        {
            return await _db.Set<UserLogin>()
                .AsNoTracking()
                .Where(l => l.UserId == userId && !l.IsDeleted)
                .OrderBy(l => l.Provider)
                .Select(l => new ValueTuple<string, string, string?>(l.Provider, l.ProviderKey, l.DisplayName))
                .ToListAsync(ct);
        }
    }
}
