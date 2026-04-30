using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Soft-deletes a role if it is not a system role.
    /// </summary>
    public sealed class DeleteRoleHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public DeleteRoleHandler(IAppDbContext db, IClock clock, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(Guid id, byte[]? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
                return Result.Fail(_localizer["RoleNotFound"]);

            if (rowVersion is null || rowVersion.Length == 0)
                return Result.Fail(_localizer["RowVersionRequired"]);

            var role = await _db.Set<Role>().FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);
            if (role is null)
                return Result.Fail(_localizer["RoleNotFound"]);

            if (role.IsSystem)
                return Result.Fail(_localizer["SystemProtectedRoleCannotBeDeleted"]);

            var currentVersion = role.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(rowVersion))
                return Result.Fail(_localizer["ConcurrencyConflict"]);

            role.IsDeleted = true;
            role.ModifiedAtUtc = _clock.UtcNow;

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["ConcurrencyConflict"]);
            }

            return Result.Ok();
        }
    }
}
