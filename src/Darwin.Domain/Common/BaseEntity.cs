using System;

namespace Darwin.Domain.Common
{
    /// <summary>
    /// Base type for all entities with audit and soft-delete support.
    /// </summary>
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
