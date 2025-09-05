using System;
using Darwin.Domain.Common;


namespace Darwin.Domain.Entities.Inventory
{
    /// <summary>
    /// Captures stock movements for auditing and troubleshooting inventory discrepancies.
    /// </summary>
    public sealed class InventoryTransaction : BaseEntity
    {
        /// <summary>Related variant id whose stock is affected.</summary>
        public Guid VariantId { get; set; }
        /// <summary>Delta applied to StockOnHand (positive for receipt, negative for adjustment/shipment).</summary>
        public int QuantityDelta { get; set; }
        /// <summary>Human-readable reason or system code (e.g., GoodsReceipt, Adjustment, ShipmentAllocation).</summary>
        public string Reason { get; set; } = string.Empty;
        /// <summary>Optional reference id (e.g., order id, return id) for correlation.</summary>
        public Guid? ReferenceId { get; set; }
    }
}