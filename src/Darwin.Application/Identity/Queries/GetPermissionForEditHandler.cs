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
    /// Loads a single permission for edit (includes RowVersion).
    /// </summary>
    public sealed class GetPermissionForEditHandler
    {
        private readonly IAppDbContext _db;
        public GetPermissionForEditHandler(IAppDbContext db) => _db = db;

        public async Task<PermissionEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var p = await _db.Set<Permission>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
            if (p == null) return null;

            return new PermissionEditDto
            {
                Id = p.Id,
                RowVersion = p.RowVersion,
                DisplayName = p.DisplayName ?? p.Key,
                Description = p.Description,
                Key = p.Key,
                IsSystem = p.IsSystem
            };
        }
    }
}
