using Darwin.Domain.Enums;

namespace Darwin.Application.Sales.Services;

public sealed class ReturnOrderWorkflowPolicy
{
    public bool CanTransition(ReturnOrderStatus current, ReturnOrderStatus target)
        => (current, target) switch
        {
            (ReturnOrderStatus.Requested, ReturnOrderStatus.Approved) => true,
            (ReturnOrderStatus.Requested, ReturnOrderStatus.Rejected) => true,
            (ReturnOrderStatus.Requested, ReturnOrderStatus.Cancelled) => true,
            (ReturnOrderStatus.Approved, ReturnOrderStatus.ReturnShipmentQueued) => true,
            (ReturnOrderStatus.Approved, ReturnOrderStatus.Received) => true,
            (ReturnOrderStatus.Approved, ReturnOrderStatus.Cancelled) => true,
            (ReturnOrderStatus.ReturnShipmentQueued, ReturnOrderStatus.Received) => true,
            (ReturnOrderStatus.ReturnShipmentQueued, ReturnOrderStatus.Cancelled) => true,
            (ReturnOrderStatus.Received, ReturnOrderStatus.Inspected) => true,
            (ReturnOrderStatus.Inspected, ReturnOrderStatus.RefundReady) => true,
            (ReturnOrderStatus.Inspected, ReturnOrderStatus.Closed) => true,
            (ReturnOrderStatus.RefundReady, ReturnOrderStatus.Refunded) => true,
            (ReturnOrderStatus.RefundReady, ReturnOrderStatus.Closed) => true,
            (ReturnOrderStatus.Refunded, ReturnOrderStatus.Closed) => true,
            _ => false
        };
}
