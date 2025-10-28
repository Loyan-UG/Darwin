using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Identity.Queries
{
    /// <summary>
    /// Retrieves a paged list of permissions for admin UIs.
    /// Supports an optional search term on Key or DisplayName.
    /// </summary>
    public sealed class GetPermissionsPageHandler
    {
        private readonly IAppDbContext _db;

        public GetPermissionsPageHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Handles the paged query for permissions.
        /// </summary>
        /// <param name="pageNumber">1-based page number.</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="searchTerm">Optional search term for key/display name.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>PagedResult of PermissionListItemDto.</returns>
        public async Task<Result<PagedResult<PermissionListItemDto>>> HandleAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            CancellationToken ct = default)
        {
            if (pageNumber < 1 || pageSize < 1)
                return Result<PagedResult<PermissionListItemDto>>.Fail("Invalid paging parameters.");

            var query = _db.Set<Permission>().Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(p => p.Key.Contains(term) || p.DisplayName.Contains(term));
            }

            var total = await query.CountAsync(ct);
            var items = await query.OrderBy(p => p.Key)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PermissionListItemDto
                {
                    Id = p.Id,
                    Key = p.Key,
                    DisplayName = p.DisplayName,
                    Description = p.Description,
                    IsSystem = p.IsSystem,
                    RowVersion = p.RowVersion
                })
                .ToListAsync(ct);

            return Result<PagedResult<PermissionListItemDto>>.Ok(
                new PagedResult<PermissionListItemDto>(items, total, pageNumber, pageSize));
        }
    }
}
