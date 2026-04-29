using Darwin.Application.Abstractions.Persistence;
using Darwin.Application;
using Darwin.Application.Common;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
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
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public GetPermissionsPageHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Handles the paged query for permissions.
        /// </summary>
        public async Task<Result<PagedResult<PermissionListItemDto>>> HandleAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            CancellationToken ct = default)
        {
            if (pageNumber < 1 || pageSize < 1)
                return Result<PagedResult<PermissionListItemDto>>.Fail(_localizer["InvalidPagingParameters"]);

            var query = _db.Set<Permission>().Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = QueryLikePattern.Contains(searchTerm);
                query = query.Where(p =>
                    EF.Functions.Like(p.Key, term, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(p.DisplayName, term, QueryLikePattern.EscapeCharacter));
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
