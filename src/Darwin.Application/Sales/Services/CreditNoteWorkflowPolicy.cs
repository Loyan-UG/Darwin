using Darwin.Domain.Enums;

namespace Darwin.Application.Sales.Services;

public sealed class CreditNoteWorkflowPolicy
{
    public bool CanTransition(CreditNoteStatus current, CreditNoteStatus target)
        => (current, target) switch
        {
            (CreditNoteStatus.Draft, CreditNoteStatus.Issued) => true,
            (CreditNoteStatus.Draft, CreditNoteStatus.Cancelled) => true,
            (CreditNoteStatus.Issued, CreditNoteStatus.Voided) => true,
            _ => false
        };
}
