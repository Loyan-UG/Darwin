using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Queries
{
    /// <summary>Returns user's external login bindings.</summary>
    public sealed class ListExternalLoginsHandler
    {
        private readonly IAppDbContext _db;
        public ListExternalLoginsHandler(IAppDbContext db) => _db = db;

        public async Task<IReadOnlyList<(string Provider, string ProviderKey, string? DisplayName)>> HandleAsync(System.Guid userId, CancellationToken ct = default)
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
