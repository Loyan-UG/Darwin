using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Foundation;
using Darwin.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Inventory;

internal static class InventoryMovementReferencePolicy
{
    public const string ShipmentAllocation = "ShipmentAllocation";
    public const string GoodsReceiptPosted = "GoodsReceiptPosted";
    public const string ReturnOrderRestock = "ReturnOrderRestock";
    public const string StockTransferDispatched = "StockTransferDispatched";
    public const string StockTransferReceived = "StockTransferReceived";
    public const string StockTransferCancelled = "StockTransferCancelled";
    public const string StockCountAdjustment = "StockCountAdjustment";

    private static readonly HashSet<string> SystemOwnedReasons = new(StringComparer.OrdinalIgnoreCase)
    {
        ShipmentAllocation,
        GoodsReceiptPosted,
        ReturnOrderRestock,
        StockTransferDispatched,
        StockTransferReceived,
        StockTransferCancelled,
        StockCountAdjustment
    };

    public static string NormalizeReason(string reason)
    {
        var normalized = (reason ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("InventoryMovementReasonRequired");
        }

        if (normalized.Length > 64)
        {
            throw new ArgumentException("InventoryMovementReasonTooLong");
        }

        if (FoundationInputNormalizer.LooksSensitive(normalized))
        {
            throw new ArgumentException("InventoryMovementSensitiveReasonRejected");
        }

        return normalized;
    }

    public static void EnsureReferencePolicy(string reason, Guid? referenceId)
    {
        if (SystemOwnedReasons.Contains(reason) && (!referenceId.HasValue || referenceId.Value == Guid.Empty))
        {
            throw new ArgumentException("InventoryMovementReferenceRequired");
        }
    }

    public static async Task<bool> ExistsAsync(
        IAppDbContext db,
        Guid? referenceId,
        string reason,
        Guid warehouseId,
        Guid productVariantId,
        CancellationToken ct)
    {
        if (!referenceId.HasValue || referenceId.Value == Guid.Empty)
        {
            return false;
        }

        return await db.Set<InventoryTransaction>()
            .AsNoTracking()
            .AnyAsync(t => t.ReferenceId == referenceId
                && t.Reason == reason
                && t.ProductVariantId == productVariantId
                && t.WarehouseId == warehouseId
                && !t.IsDeleted, ct)
            .ConfigureAwait(false);
    }

    public static void AddLedgerRow(
        IAppDbContext db,
        Guid warehouseId,
        Guid productVariantId,
        int quantityDelta,
        string reason,
        Guid? referenceId)
    {
        db.Set<InventoryTransaction>().Add(new InventoryTransaction
        {
            WarehouseId = warehouseId,
            ProductVariantId = productVariantId,
            QuantityDelta = quantityDelta,
            Reason = reason,
            ReferenceId = referenceId
        });
    }
}
