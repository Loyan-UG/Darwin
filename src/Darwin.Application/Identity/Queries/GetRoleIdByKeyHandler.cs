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
    /// Resolves a role id by its immutable <see cref="Role.Key"/> (case-insensitive).
    /// Useful for assigning default roles like "Members" at registration time.
    /// </summary>
    public sealed class GetRoleIdByKeyHandler
    {
        private readonly IAppDbContext _db;

        /// <summary>Creates a new instance of the handler.</summary>
        public GetRoleIdByKeyHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Returns the role id if found.
        /// </summary>
        public async Task<Result<Guid>> HandleAsync(string roleKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(roleKey))
                return Result<Guid>.Fail("Role key is required.");

            var role = await _db.Set<Role>()
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Key == roleKey, ct);

            return role is null
                ? Result<Guid>.Fail("Role not found.")
                : Result<Guid>.Ok(role.Id);
        }
    }
}
