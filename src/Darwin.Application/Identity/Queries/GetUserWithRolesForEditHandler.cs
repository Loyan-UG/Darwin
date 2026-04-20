using System;
using System.Linq;
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
    /// Loads a user with current role assignments for editing, including the full role catalog.
    /// </summary>
    public sealed class GetUserWithRolesForEditHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public GetUserWithRolesForEditHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Returns the edit payload including the AllRoles list for UI.
        /// </summary>
        public async Task<Result<UserRolesEditDto>> HandleAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await _db.Set<User>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted && u.IsActive, ct);

            if (user is null)
                return Result<UserRolesEditDto>.Fail(_localizer["UserNotFoundOrInactive"]);

            var currentRoleIds = await _db.Set<UserRole>()
                .AsNoTracking()
                .Where(ur => ur.UserId == userId && !ur.IsDeleted)
                .Select(ur => ur.RoleId)
                .ToListAsync(ct);

            var allRoles = await _db.Set<Role>()
                .AsNoTracking()
                .Where(r => !r.IsDeleted)
                .OrderBy(r => r.DisplayName ?? r.Key)
                .Select(r => new RoleListItemDto
                {
                    Id = r.Id,
                    DisplayName = r.DisplayName ?? string.Empty,
                    Description = r.Description,
                    IsSystem = r.IsSystem,
                    RowVersion = r.RowVersion
                })
                .ToListAsync(ct);

            return Result<UserRolesEditDto>.Ok(new UserRolesEditDto
            {
                UserId = user.Id,
                RowVersion = user.RowVersion,
                RoleIds = currentRoleIds,
                UserEmail = user.Email,
                AllRoles = allRoles
            });
        }
    }
}
