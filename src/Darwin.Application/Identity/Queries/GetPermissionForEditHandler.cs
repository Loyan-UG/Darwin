using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Queries
{
    /// <summary>
    /// Loads an existing permission for editing. Only DisplayName and Description may be edited.
    /// </summary>
    public sealed class GetPermissionForEditHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public GetPermissionForEditHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Retrieves the permission and maps it to a PermissionEditDto.
        /// </summary>
        public async Task<Result<PermissionEditDto>> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var permission = await _db.Set<Permission>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

            if (permission is null)
                return Result<PermissionEditDto>.Fail(_localizer["PermissionNotFound"]);

            return Result<PermissionEditDto>.Ok(new PermissionEditDto
            {
                Id = permission.Id,
                Key = permission.Key,
                RowVersion = permission.RowVersion,
                DisplayName = permission.DisplayName,
                Description = permission.Description,
                IsSystem = permission.IsSystem
            });
        }
    }
}
