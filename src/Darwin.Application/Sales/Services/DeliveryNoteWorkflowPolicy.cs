using Darwin.Domain.Enums;

namespace Darwin.Application.Sales.Services;

public sealed class DeliveryNoteWorkflowPolicy
{
    public bool CanTransition(DeliveryNoteStatus current, DeliveryNoteStatus target)
        => (current, target) switch
        {
            (DeliveryNoteStatus.Draft, DeliveryNoteStatus.Prepared) => true,
            (DeliveryNoteStatus.Draft, DeliveryNoteStatus.Cancelled) => true,
            (DeliveryNoteStatus.Prepared, DeliveryNoteStatus.Issued) => true,
            (DeliveryNoteStatus.Prepared, DeliveryNoteStatus.Cancelled) => true,
            (DeliveryNoteStatus.Issued, DeliveryNoteStatus.Shipped) => true,
            (DeliveryNoteStatus.Issued, DeliveryNoteStatus.Cancelled) => true,
            (DeliveryNoteStatus.Shipped, DeliveryNoteStatus.Delivered) => true,
            _ => false
        };
}
