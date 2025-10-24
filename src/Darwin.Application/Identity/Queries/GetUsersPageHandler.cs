using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Queries
{
    /// <summary>
    /// Returns a paged set of users for Admin listing. Uses simple filters/sorts for now.
    /// </summary>
    public sealed class GetUsersPageHandler
    {
        private readonly IAppDbContext _db;
        public GetUsersPageHandler(IAppDbContext db) => _db = db;

        public async Task<(IReadOnlyList<UserListItemDto> Items, int Total)> HandleAsync(
            int page, int pageSize, string? emailFilter, CancellationToken ct = default)
        {
            var q = _db.Set<Darwin.Domain.Entities.Identity.User>().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(emailFilter))
                q = q.Where(u => EF.Functions.Like(u.Email, $"%{emailFilter}%"));

            var total = await q.CountAsync(ct);

            var items = await q.OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(u => new UserListItemDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    IsActive = u.IsActive,
                    IsSystem = u.IsSystem,
                    RowVersion = u.RowVersion
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }
}
