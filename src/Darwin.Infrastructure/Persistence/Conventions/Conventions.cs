using System;
using System.Linq;
using System.Linq.Expressions;
using Darwin.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Persistence.Conventions
{
    /// <summary>
    ///     Centralized EF Core model conventions applied to all entities in the Darwin model:
    ///     keys, required audit fields, property lengths, soft delete, and concurrency token mapping.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Goals:
    ///         <list type="bullet">
    ///             <item>Reduce duplication in per-entity configurations.</item>
    ///             <item>Guarantee consistent schema for cross-cutting concerns (audit, rowversion).</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Conventions should remain deterministic and side-effect free; entity-specific rules belong in dedicated configurations.
    ///     </para>
    /// </remarks>
    public static class Conventions
    {
        public static void Apply(ModelBuilder modelBuilder)
        {
            // Apply to all entities that inherit from BaseEntity
            foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType)))
            {
                ConfigureBaseEntity(modelBuilder, entityType);
                ConfigureStringColumnDefaults(entityType);
            }

            // Decimal precision examples (VAT rates etc.). Most money is Minor (long), so decimals are rare.
            // If needed, iterate properties by name to set precision globally.
        }

        private static void ConfigureBaseEntity(ModelBuilder modelBuilder, IMutableEntityType entityType)
        {
            // RowVersion as concurrency token (byte[])
            var rowVersionProp = entityType.FindProperty(nameof(BaseEntity.RowVersion));
            if (rowVersionProp is not null)
            {
                rowVersionProp.IsConcurrencyToken = true;
                rowVersionProp.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate;
                rowVersionProp.SetColumnType("rowversion");
            }

            // Global Query Filter: IsDeleted == false
            // Build expression: (BaseEntity e) => e.IsDeleted == false
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var prop = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            var compare = Expression.Equal(prop, Expression.Constant(false));
            var lambda = Expression.Lambda(compare, parameter);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }

        private static void ConfigureStringColumnDefaults(IMutableEntityType entityType)
        {
            // Default string column length to NVARCHAR(400) unless explicitly configured.
            foreach (var p in entityType.GetProperties().Where(p => p.ClrType == typeof(string) && p.GetMaxLength() is null))
            {
                p.SetMaxLength(400);
            }
        }
    }
}
