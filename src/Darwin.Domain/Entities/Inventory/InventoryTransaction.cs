using System;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Inventory
{
    /// <summary>
    /// Warehouse aggregate root used for multi-warehouse stock management.
    /// </summary>
    public sealed class Warehouse : BaseEntity
    {
        /// <summary>Warehouse display name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional serialized address payload.</summary>
        public string? AddressJson { get; set; }

        /// <summary>True when this is the default warehouse for small-business onboarding.</summary>
        public bool IsDefault { get; set; }
    }

    /// <summary>
    /// Current stock state for a product variant within a specific warehouse.
    /// Composite key should be configured as (WarehouseId, VariantId) in Infrastructure.
    /// </summary>
    public sealed class StockLevel : BaseEntity
    {
        /// <summary>Owning warehouse identifier.</summary>
        public Guid WarehouseId { get; set; }

        /// <summary>Product variant identifier.</summary>
        public Guid VariantId { get; set; }

        /// <summary>Physical available stock in warehouse.</summary>
        public int OnHand { get; set; }

        /// <summary>Reserved quantity for carts/orders not yet finalized.</summary>
        public int Reserved { get; set; }

        /// <summary>Optional reorder threshold.</summary>
        public int? ReorderPoint { get; set; }
    }

    /// <summary>
    /// Records stock movement between warehouses for auditability.
    /// </summary>
    public sealed class StockTransfer : BaseEntity
    {
        /// <summary>Source warehouse identifier.</summary>
        public Guid FromWarehouseId { get; set; }

        /// <summary>Destination warehouse identifier.</summary>
        public Guid ToWarehouseId { get; set; }

        /// <summary>Transferred variant identifier.</summary>
        public Guid VariantId { get; set; }

        /// <summary>Transferred quantity.</summary>
        public int Quantity { get; set; }

        /// <summary>Human-readable reason.</summary>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Captures inventory movement events for ledger/history views.
    /// </summary>
    public sealed class InventoryTransaction : BaseEntity
    {
        /// <summary>Related warehouse id.</summary>
        public Guid WarehouseId { get; set; }

        /// <summary>Related variant id whose stock is affected.</summary>
        public Guid VariantId { get; set; }

        /// <summary>Delta applied to OnHand (positive for receipt, negative for issue).</summary>
        public int QuantityDelta { get; set; }

        /// <summary>Human-readable reason or system code.</summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>Optional reference id (e.g., order id, transfer id).</summary>
        public Guid? ReferenceId { get; set; }
    }
}
