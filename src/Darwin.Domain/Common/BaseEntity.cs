using System;

namespace Darwin.Domain.Common
{
    /// <summary>
    ///     Base class for persistent entities providing common fields:
    ///     identifier, auditing (Created*/Modified*), soft delete flag, and concurrency token (RowVersion).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Fields:
    ///         <list type="bullet">
    ///             <item><c>Id</c>: Entity primary key (GUID).</item>
    ///             <item><c>CreatedAtUtc/ModifiedAtUtc</c>: Timestamps in UTC, set by the DbContext auditing.</item>
    ///             <item><c>CreatedByUserId/ModifiedByUserId</c>: Actor identifiers for audit trails.</item>
    ///             <item><c>IsDeleted</c>: Soft delete flag; use query filters to hide deleted items from normal reads.</item>
    ///             <item><c>RowVersion</c>: Optimistic concurrency token mapped to SQL Server <c>rowversion</c>.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Guidelines:
    ///         <list type="bullet">
    ///             <item>Do not manipulate audit fields directly; let the DbContext auditing manage them.</item>
    ///             <item>Use soft delete unless permanent deletion is explicitly required (e.g., privacy requests).</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public abstract class BaseEntity
    {
        /// <summary>Primary key.</summary>
        public Guid Id { get; set; }

        /// <summary>UTC timestamp when the entity was created.</summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>UTC timestamp when the entity was last modified.</summary>
        public DateTime? ModifiedAtUtc { get; set; }

        /// <summary>User who created the entity.</summary>
        public Guid CreatedByUserId { get; set; }

        /// <summary>User who last modified the entity.</summary>
        public Guid ModifiedByUserId { get; set; }

        /// <summary>Soft-delete flag.</summary>
        public bool IsDeleted { get; set; }

        /// <summary>Row version for optimistic concurrency.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
