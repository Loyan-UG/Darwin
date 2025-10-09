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
    /// <remarks>
    ///     <para>
    ///         The auditing pipeline runs just before EF saves changes, iterating over tracked entities
    ///         derived from <c>BaseEntity</c>. For added entities, it sets creation timestamp and creator identity.
    ///         For modified entities, it updates modification timestamp and modifier identity.
    ///     </para>
    ///     <para>
    ///         Identity Source:
    ///         <list type="bullet">
    ///             <item>At runtime, <c>ICurrentUserService</c> is resolved from DI and used to obtain the current user id.</item>
    ///             <item>At design-time or when unavailable, the auditing fallbacks to <c>WellKnownIds.SystemUserId</c>.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Concurrency:
    ///         <list type="bullet">
    ///             <item>The <c>RowVersion</c> property participates in optimistic concurrency checks.</item>
    ///             <item>On INSERT, EF/SQL Server populates the rowversion column automatically.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         This file intentionally does not define <c>DbSet</c>s nor configuration; it only augments the behavior
    ///         of the main context via the partial class mechanism.
    ///     </para>
    /// </remarks>
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
            var userId = _currentUser?.GetCurrentUserId() ?? Darwin.Shared.Constants.WellKnownIds.AdministratorUserId;

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
