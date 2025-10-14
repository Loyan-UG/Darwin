using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Shared.Results;
using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Soft-deletes a role if it is not a system role. System roles are protected
    /// and cannot be removed. When the role does not exist, a not-found result is
    /// returned. This handler updates audit fields and respects global query filters.
    /// </summary>
    public sealed class DeleteRoleHandler
    {
        private readonly IAppDbContext _db;

        /// <summary>
        /// Creates a new handler using the application's DbContext abstraction.
        /// </summary>
        public DeleteRoleHandler(IAppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Deletes the specified role by id using a soft-delete. If the role is
        /// marked as system (<c>IsSystem</c>), the operation is rejected with a
        /// validation-like failure.
        /// </summary>
        /// <param name="id">The role identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A successful result when deleted; a failure result when not found or
        /// when the role is system-protected.
        /// </returns>
        public async Task<Result> HandleAsync(Guid id, CancellationToken ct = default)
        {
            // Global filter hides IsDeleted==true; explicit single-row fetch
            var role = await _db.Set<Role>().FirstOrDefaultAsync(r => r.Id == id, ct);
            if (role is null)
                return Result.Fail("Role not found.");

            if (role.IsSystem)
                return Result.Fail("This role is system-protected and cannot be deleted.");

            // Soft delete
            role.IsDeleted = true;
            role.ModifiedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
