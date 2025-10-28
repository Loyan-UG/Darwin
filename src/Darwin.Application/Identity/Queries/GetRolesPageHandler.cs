using System;
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
    /// Returns a paged list of roles for admin listing screens.
    /// Filtering is a simple case-insensitive "contains" on DisplayName or Description.
    /// </summary>
    public sealed class GetRolesPageHandler
    {
        private readonly IAppDbContext _db;

        /// <summary>
        /// Initializes the handler with the application DbContext abstraction.
        /// </summary>
        public GetRolesPageHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Executes the query and returns a page of <see cref="RoleListItemDto"/>.
        /// </summary>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Items per page (reasonable upper bound is advised by the caller).</param>
        /// <param name="search">Optional search term applied to DisplayName and Description.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Tuple of (items, totalCount).</returns>
        public async Task<(List<RoleListItemDto> Items, int Total)> HandleAsync(
            int page,
            int pageSize,
            string? search = null,
            CancellationToken ct = default)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 20 : pageSize;

            var query = _db.Set<Role>().AsNoTracking().Where(r => !r.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(r =>
                    (r.DisplayName != null && EF.Functions.Like(r.DisplayName, $"%{s}%")) ||
                    (r.Description != null && EF.Functions.Like(r.Description, $"%{s}%")));
            }

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderBy(r => r.DisplayName)
                .ThenBy(r => r.Key)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RoleListItemDto
                {
                    Id = r.Id,
                    DisplayName = r.DisplayName ?? string.Empty,
                    Description = r.Description,
                    IsSystem = r.IsSystem,
                    RowVersion = r.RowVersion
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }
}
