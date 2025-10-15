using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Queries
{
    /// <summary>
    /// Loads an existing permission for editing. Only DisplayName and Description may be edited.
    /// </summary>
    public sealed class GetPermissionForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetPermissionForEditHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Retrieves the permission and maps it to a PermissionEditDto.
        /// </summary>
        /// <param name="id">Identifier of the permission.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Result containing PermissionEditDto or failure.</returns>
        public async Task<Result<PermissionEditDto>> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var permission = await _db.Set<Permission>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

            if (permission is null)
                return Result<PermissionEditDto>.Fail("Permission not found.");

            return Result<PermissionEditDto>.Ok(new PermissionEditDto
            {
                Id = permission.Id,
                RowVersion = permission.RowVersion,
                DisplayName = permission.DisplayName,
                Description = permission.Description
            });
        }
    }
}
