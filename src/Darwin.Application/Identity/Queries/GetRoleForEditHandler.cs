using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Queries
{
    /// <summary>
    /// Loads a single role for editing, projecting to <see cref="RoleEditDto"/>.
    /// Includes the concurrency token.
    /// </summary>
    public sealed class GetRoleForEditHandler
    {
        private readonly IAppDbContext _db;
        public GetRoleForEditHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Returns the role edit DTO or null when not found or soft-deleted.
        /// </summary>
        public async Task<RoleEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Set<Role>()
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct)
                .ContinueWith(t =>
                {
                    var r = t.Result;
                    if (r is null) return null;
                    return new RoleEditDto
                    {
                        Id = r.Id,
                        RowVersion = r.RowVersion,
                        DisplayName = r.DisplayName ?? string.Empty,
                        Description = r.Description
                    };
                }, ct);
        }
    }
}
