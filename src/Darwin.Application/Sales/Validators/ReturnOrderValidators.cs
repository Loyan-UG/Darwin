using Darwin.Application.Sales.DTOs;
using FluentValidation;

namespace Darwin.Application.Sales.Validators;

public sealed class ReturnOrderCreateValidator : AbstractValidator<ReturnOrderCreateDto>
{
    public ReturnOrderCreateValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.InternalNotes).MaximumLength(2000);
        RuleFor(x => x.CustomerSnapshotJson).MaximumLength(16000);
        RuleFor(x => x.ShippingAddressJson).MaximumLength(16000);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(x => x.OrderLineId).NotEmpty();
            line.RuleFor(x => x.RequestedQuantity).GreaterThan(0);
        });
    }
}

public sealed class ReturnOrderLifecycleValidator : AbstractValidator<ReturnOrderLifecycleDto>
{
    public ReturnOrderLifecycleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(512);
    }
}

public sealed class ReturnOrderApproveValidator : AbstractValidator<ReturnOrderApproveDto>
{
    public ReturnOrderApproveValidator()
    {
        Include(new ReturnOrderLifecycleValidator());
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(x => x.LineId).NotEmpty();
            line.RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class ReturnOrderQueueShipmentValidator : AbstractValidator<ReturnOrderQueueShipmentDto>
{
    public ReturnOrderQueueShipmentValidator()
    {
        Include(new ReturnOrderLifecycleValidator());
    }
}

public sealed class ReturnOrderReceiveValidator : AbstractValidator<ReturnOrderReceiveDto>
{
    public ReturnOrderReceiveValidator()
    {
        Include(new ReturnOrderLifecycleValidator());
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(x => x.LineId).NotEmpty();
            line.RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class ReturnOrderInspectValidator : AbstractValidator<ReturnOrderInspectDto>
{
    public ReturnOrderInspectValidator()
    {
        Include(new ReturnOrderLifecycleValidator());
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(x => x.LineId).NotEmpty();
            line.RuleFor(x => x.AcceptedQuantity).GreaterThanOrEqualTo(0);
            line.RuleFor(x => x.RejectedQuantity).GreaterThanOrEqualTo(0);
            line.RuleFor(x => x.ScrappedQuantity).GreaterThanOrEqualTo(0);
            line.RuleFor(x => x.RestockQuantity).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class ReturnOrderLinkRefundValidator : AbstractValidator<ReturnOrderLinkRefundDto>
{
    public ReturnOrderLinkRefundValidator()
    {
        Include(new ReturnOrderLifecycleValidator());
        RuleFor(x => x.RefundId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
