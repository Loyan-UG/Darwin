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
    /// Loads user details for editing by an administrator.
    /// Only non-sensitive fields are projected; this does not include email or roles.
    /// </summary>
    public sealed class GetUserForEditHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public GetUserForEditHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Retrieves a user for editing.
        /// </summary>
        public async Task<Result<UserEditDto>> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var user = await _db.Set<User>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);

            if (user is null)
            {
                return Result<UserEditDto>.Fail(_localizer["UserNotFound"]);
            }

            var dto = new UserEditDto
            {
                Id = user.Id,
                RowVersion = user.RowVersion,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Locale = user.Locale,
                Timezone = user.Timezone,
                Currency = user.Currency,
                PhoneE164 = user.PhoneE164,
                IsActive = user.IsActive
            };

            return Result<UserEditDto>.Ok(dto);
        }
    }
}
