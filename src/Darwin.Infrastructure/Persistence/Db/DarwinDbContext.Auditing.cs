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
    /// Partial DbContext that applies auditing (Created*/Modified* fields) on save.
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
            var userId = _currentUser?.GetCurrentUserId() ?? Darwin.Shared.Constants.WellKnownIds.SystemUserId;

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
