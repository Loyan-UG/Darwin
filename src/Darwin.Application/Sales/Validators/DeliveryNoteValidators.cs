using Darwin.Application.Sales.DTOs;
using FluentValidation;

namespace Darwin.Application.Sales.Validators;

public sealed class DeliveryNoteCreateFromShipmentValidator : AbstractValidator<DeliveryNoteCreateFromShipmentDto>
{
    public DeliveryNoteCreateFromShipmentValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
        RuleFor(x => x.InternalNotes).MaximumLength(2000);
    }
}

public sealed class DeliveryNoteLifecycleValidator : AbstractValidator<DeliveryNoteLifecycleDto>
{
    public DeliveryNoteLifecycleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(512);
    }
}
