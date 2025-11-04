using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Darwin.Web.Areas.Admin.ViewModels.Inventory
{
    /// <summary>
    /// Lightweight view model representing a single ledger row for display in Admin.
    /// This mirrors the projection returned by the Inventory ledger query
    /// (fields are kept generic to avoid tight coupling to the entity).
    /// </summary>
    public sealed class InventoryLedgerItemVm
    {
        /// <summary>Associated product variant identifier.</summary>
        public Guid VariantId { get; set; }

        /// <summary>Signed quantity delta; positive means stock added, negative means stock removed.</summary>
        public int QuantityDelta { get; set; }

        /// <summary>Reason label stored with the transaction (e.g., "OrderPaid-Reserve").</summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>Optional correlation identifier (e.g., OrderId or ReturnId) to achieve idempotency and traceability.</summary>
        public Guid? ReferenceId { get; set; }

        /// <summary>Creation timestamp (UTC).</summary>
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>
    /// Paged list view model for browsing the inventory ledger.
    /// Provides optional filters (by variant, date range) to narrow down results.
    /// </summary>
    public sealed class InventoryLedgerListVm
    {
        /// <summary>Optional filter by variant id.</summary>
        public Guid? VariantId { get; set; }

        /// <summary>Optional start date (UTC) filter.</summary>
        public DateTime? FromUtc { get; set; }

        /// <summary>Optional end date (UTC) filter.</summary>
        public DateTime? ToUtc { get; set; }

        /// <summary>Current page items.</summary>
        public List<InventoryLedgerItemVm> Items { get; set; } = new();

        /// <summary>1-based page number.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Items per page.</summary>
        public int PageSize { get; set; } = 20;

        /// <summary>Total number of matching rows.</summary>
        public int Total { get; set; }

        /// <summary>Drop-down items for page size selection.</summary>
        public IEnumerable<SelectListItem> PageSizeItems { get; set; } = new List<SelectListItem>();
    }

    /// <summary>
    /// Summary snapshot for a product variant stock, used in Admin for quick inspection.
    /// </summary>
    public sealed class VariantStockSummaryVm
    {
        /// <summary>Target variant id.</summary>
        public Guid VariantId { get; set; }

        /// <summary>On-hand stock (physically available on shelf).</summary>
        public int StockOnHand { get; set; }

        /// <summary>Reserved stock (blocked for orders not yet shipped).</summary>
        public int StockReserved { get; set; }

        /// <summary>Computed availability (on-hand minus reserved).</summary>
        public int Available => StockOnHand - StockReserved;

        /// <summary>Concurrency token for optimistic concurrency when exposing inline operations.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Form view model for manual stock adjustments (increase/decrease on-hand).
    /// Aligns with InventoryAdjustDto in the Application layer.
    /// </summary>
    public sealed class InventoryAdjustVm
    {
        /// <summary>Target variant id.</summary>
        public Guid VariantId { get; set; }

        /// <summary>Delta to apply to on-hand stock (positive or negative).</summary>
        public int QuantityDelta { get; set; }

        /// <summary>Reason label (e.g., "ManualAdjustment", "StockCount", "SupplierReceipt").</summary>
        public string Reason { get; set; } = "ManualAdjustment";

        /// <summary>Optional correlation id (e.g., a count-session id) to make the operation idempotent.</summary>
        public Guid? ReferenceId { get; set; }
    }

    /// <summary>
    /// Form view model for reserving stock (does not change on-hand).
    /// Aligns with InventoryReserveDto in the Application layer.
    /// </summary>
    public sealed class InventoryReserveVm
    {
        /// <summary>Target variant id.</summary>
        public Guid VariantId { get; set; }

        /// <summary>Quantity to reserve (must be positive).</summary>
        public int Quantity { get; set; }

        /// <summary>Reason label (e.g., "ManualReserve", "OrderPaid-Reserve").</summary>
        public string Reason { get; set; } = "ManualReserve";

        /// <summary>Optional correlation id to avoid duplicate reservations.</summary>
        public Guid? ReferenceId { get; set; }
    }

    /// <summary>
    /// Form view model for releasing a reservation (does not change on-hand).
    /// Aligns with InventoryReleaseReservationDto in the Application layer.
    /// </summary>
    public sealed class InventoryReleaseReservationVm
    {
        /// <summary>Target variant id.</summary>
        public Guid VariantId { get; set; }

        /// <summary>Quantity to release (must be positive).</summary>
        public int Quantity { get; set; }

        /// <summary>Reason label (e.g., "ManualRelease", "OrderCancelled-Release").</summary>
        public string Reason { get; set; } = "ManualRelease";

        /// <summary>Optional correlation id to avoid duplicate releases.</summary>
        public Guid? ReferenceId { get; set; }
    }
}
