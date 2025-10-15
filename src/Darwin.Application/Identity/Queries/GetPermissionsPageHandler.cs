using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Queries
{
    /// <summary>
    /// Returns a paginated list of permissions for Admin grid with optional search.
    /// </summary>
    public sealed class GetPermissionsPageHandler
    {
        private readonly IAppDbContext _db;
        public GetPermissionsPageHandler(IAppDbContext db) => _db = db;

        public async Task<(List<PermissionListItemDto> Items, int Total)> HandleAsync(string? q, int page, int pageSize, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var baseQuery = _db.Set<Permission>().AsNoTracking().Where(p => !p.IsDeleted);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                baseQuery = baseQuery.Where(p => p.Key.Contains(term) || (p.DisplayName ?? "").Contains(term));
            }

            var total = await baseQuery.CountAsync(ct);

            var rows = await baseQuery
                .OrderByDescending(p => p.ModifiedAtUtc ?? p.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PermissionListItemDto
                {
                    Id = p.Id,
                    Key = p.Key,
                    DisplayName = p.DisplayName ?? p.Key,
                    IsSystem = p.IsSystem
                    //RowVersion = p.RowVersion,
                    //ModifiedAtUtc = p.ModifiedAtUtc
                })
                .ToListAsync(ct);

            return (rows, total);
        }
    }
}
