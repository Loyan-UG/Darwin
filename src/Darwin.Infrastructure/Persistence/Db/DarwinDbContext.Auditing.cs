using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Persistence.Db
{
    /// <summary>
    ///     Auditing partial of <see cref="DarwinDbContext"/> that populates audit fields
    ///     (CreatedAtUtc, ModifiedAtUtc, CreatedByUserId, ModifiedByUserId) on <c>SaveChanges</c>.
    /// </summary>
    public sealed partial class DarwinDbContext
    {
        private readonly ICurrentUserService? _currentUser;

        // This ctor is used by DI when ICurrentUserService is available (web runtime).
        public DarwinDbContext(DbContextOptions<DarwinDbContext> options, ICurrentUserService currentUser)
            : this(options) // call base part ctor
        {
            _currentUser = currentUser;
        }

        public override int SaveChanges()
        {
            ApplyAudit();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAudit();
            return base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Applies audit fields for added/modified entities derived from BaseEntity.
        /// </summary>
        private void ApplyAudit()
        {
            var now = DateTime.UtcNow;

            // Try to obtain the current user id from ICurrentUserService.
            // If the current user service is not available or throws (e.g. when no authenticated user),
            // fall back to the well-known Administrator user id to ensure auditing continues.
            //
            // Rationale:
            // - Some operations (like issuing refresh tokens during login) happen before an authenticated
            //   principal exists. The auditing pipeline must not throw in these normal flows.
            // - Using a well-known system/administrator id is an accepted fallback for audit attribution
            //   when no real user is available.
            Guid userId;
            try
            {
                userId = _currentUser?.GetCurrentUserId() ?? Darwin.Shared.Constants.WellKnownIds.AdministratorUserId;
            }
            catch (Exception)
            {
                // Swallow exceptions from CurrentUserService and use the system fallback.
                // Avoid allowing auditing to break higher-level flows (login, seeding, etc.).
                userId = Darwin.Shared.Constants.WellKnownIds.AdministratorUserId;
            }

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAtUtc = now;
                        entry.Entity.ModifiedAtUtc = null;
                        entry.Entity.CreatedByUserId = userId;
                        entry.Entity.ModifiedByUserId = userId;
                        if (entry.Entity.RowVersion == null || entry.Entity.RowVersion.Length == 0)
                            entry.Entity.RowVersion = Array.Empty<byte>();
                        break;

                    case EntityState.Modified:
                        entry.Entity.ModifiedAtUtc = now;
                        entry.Entity.ModifiedByUserId = userId;
                        break;
                }
            }
        }
    }
}