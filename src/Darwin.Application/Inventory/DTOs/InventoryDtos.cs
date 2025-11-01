using System;

namespace Darwin.Application.Inventory.DTOs
{
    /// <summary>Manual or system-driven inventory adjustment.</summary>
    public sealed class InventoryAdjustDto
    {
        public Guid VariantId { get; set; }
        /// <summary>Delta applied to on-hand stock (positive receipt, negative write-off).</summary>
        public int QuantityDelta { get; set; }
        /// <summary>Reason code or description (e.g., GoodsReceipt, Adjustment, ShipmentAllocation).</summary>
        public string Reason { get; set; } = "Adjustment";
        /// <summary>Optional reference aggregate id (order, return, etc.).</summary>
        public Guid? ReferenceId { get; set; }
    }

    /// <summary>Reserve quantity for outstanding carts/orders.</summary>
    public sealed class InventoryReserveDto
    {
        public Guid VariantId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; } = "Reservation";
        public Guid? ReferenceId { get; set; }
    }

    /// <summary>Release previously reserved quantity (e.g., cart abandonment, order cancel).</summary>
    public sealed class InventoryReleaseReservationDto
    {
        public Guid VariantId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; } = "ReservationRelease";
        public Guid? ReferenceId { get; set; }
    }

    /// <summary>Lightweight ledger row for admin reports.</summary>
    public sealed class InventoryTransactionRowDto
    {
        public Guid Id { get; set; }
        public Guid VariantId { get; set; }
        public int QuantityDelta { get; set; }
        public string Reason { get; set; } = string.Empty;
        public Guid? ReferenceId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>
    /// Request to allocate inventory for a placed order.
    /// This operation reduces on-hand stock and releases an equal reserved amount per line.
    /// One ledger row (negative QuantityDelta) is appended per affected variant.
    /// </summary>
    public sealed class InventoryAllocateForOrderDto
    {
        /// <summary>Order identifier used for correlation and idempotency.</summary>
        public Guid OrderId { get; set; }

        /// <summary>Lines to allocate; each entry references a variant and a quantity to allocate.</summary>
        public List<InventoryAllocateForOrderLineDto> Lines { get; set; } = new();
    }

    /// <summary>Single allocation line for a specific variant.</summary>
    public sealed class InventoryAllocateForOrderLineDto
    {
        /// <summary>Target variant id.</summary>
        public Guid VariantId { get; set; }

        /// <summary>Quantity to allocate (must be positive).</summary>
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Request to process a customer return (goods received back to stock).
    /// This operation increases on-hand stock and appends a positive ledger row.
    /// </summary>
    public sealed class InventoryReturnReceiptDto
    {
        /// <summary>Returned variant id.</summary>
        public Guid VariantId { get; set; }

        /// <summary>Positive quantity to receive back.</summary>
        public int Quantity { get; set; }

        /// <summary>Optional return/case identifier (used for correlation/idempotency).</summary>
        public Guid? ReferenceId { get; set; }

        /// <summary>Reason tag recorded in the ledger; default is 'ReturnReceipt'.</summary>
        public string Reason { get; set; } = "ReturnReceipt";
    }
}
