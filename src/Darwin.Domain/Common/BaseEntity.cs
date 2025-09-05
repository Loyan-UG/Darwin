using System;


namespace Darwin.Domain.Common
{
    /// <summary>
    /// Base class for all aggregate roots and entities in the domain model.
    /// Provides auditing, soft-delete, and optimistic concurrency control.
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Primary key. Use sequential GUIDs to improve clustered index locality on SQL Server.
        /// </summary>
        public Guid Id { get; set; }


        /// <summary>
        /// UTC timestamp of entity creation. Always stored in UTC.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }


        /// <summary>
        /// UTC timestamp of last modification. Always stored in UTC.
        /// </summary>
        public DateTime? ModifiedAtUtc { get; set; }


        /// <summary>
        /// Optional user id that created this entity (maps to ASP.NET Identity user id).
        /// </summary>
        public Guid? CreatedByUserId { get; set; }


        /// <summary>
        /// Optional user id that last modified this entity (maps to ASP.NET Identity user id).
        /// </summary>
        public Guid? ModifiedByUserId { get; set; }


        /// <summary>
        /// Soft-delete marker. When true, the entity is logically deleted but kept for audit/restore.
        /// Unique indexes must be filtered to IsDeleted = 0 to preserve uniqueness across soft deletes.
        /// </summary>
        public bool IsDeleted { get; set; }


        /// <summary>
        /// Row version token for optimistic concurrency. Mapped to SQL Server 'rowversion'.
        /// EF Core uses this to prevent lost updates when multiple users edit the same record.
        /// </summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}